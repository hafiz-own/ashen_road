using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenRoad.Combat
{
    /// <summary>
    /// A damage area that is opened for an attack's active frames and hits each target at most once per opening.
    /// Uses an overlap query each physics step instead of trigger colliders, so it never depends on layer callbacks.
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] Vector2 size = Vector2.one;
        [SerializeField] Vector2 offset = Vector2.zero;
        [SerializeField] LayerMask targets;
        [SerializeField] int damage = 1;
        [SerializeField] DamageType damageType = DamageType.Sword;
        [SerializeField] float knockback = 5f;
        [SerializeField] bool lethal;

        readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();
        readonly List<Collider2D> results = new List<Collider2D>();
        GameObject owner;
        IDamageable ownerTarget;
        float closeAt = float.PositiveInfinity;

        public bool IsOpen { get; private set; }
        public int Damage { get => damage; set => damage = value; }
        public DamageType Type { get => damageType; set => damageType = value; }
        public LayerMask Targets { get => targets; set => targets = value; }
        public float Knockback { get => knockback; set => knockback = value; }
        public bool Lethal { get => lethal; set => lethal = value; }
        public Vector2 Size { get => size; set => size = value; }
        public Vector2 Offset { get => offset; set => offset = value; }

        /// <summary>Raised per target as (target, applied). Applied is false when the target was immune.</summary>
        public event Action<IDamageable, bool> HitTarget;

        void Awake()
        {
            ownerTarget = GetComponentInParent<IDamageable>();
            owner = ownerTarget is Component c ? c.gameObject : gameObject;
        }

        public void SetOwner(GameObject newOwner)
        {
            owner = newOwner;
            ownerTarget = newOwner != null ? newOwner.GetComponent<IDamageable>() : null;
        }

        public void Open(float duration = float.PositiveInfinity)
        {
            alreadyHit.Clear();
            IsOpen = true;
            closeAt = float.IsPositiveInfinity(duration) ? float.PositiveInfinity : Time.time + duration;
            Sweep();
        }

        public void Close()
        {
            IsOpen = false;
            closeAt = float.PositiveInfinity;
        }

        /// <summary>Lets every target be hit again during the same opening (used by continuous hazards).</summary>
        public void ResetHits() => alreadyHit.Clear();

        void FixedUpdate()
        {
            if (!IsOpen) return;
            if (Time.time >= closeAt)
            {
                Close();
                return;
            }
            Sweep();
        }

        public Vector2 WorldCenter
        {
            get
            {
                Vector3 s = transform.lossyScale;
                Vector2 o = new Vector2(offset.x * Mathf.Sign(s.x), offset.y);
                return (Vector2)transform.position + o;
            }
        }

        /// <summary>Checks the area now and damages anything new inside it.</summary>
        public int Sweep()
        {
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = targets };
            results.Clear();
            Physics2D.OverlapBox(WorldCenter, size, 0f, filter, results);
            int hits = 0;
            foreach (Collider2D col in results)
            {
                IDamageable target = col.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive || alreadyHit.Contains(target)) continue;
                if (ownerTarget != null && ReferenceEquals(target, ownerTarget)) continue;

                alreadyHit.Add(target);
                Vector2 dir = ((Vector2)col.bounds.center - (Vector2)transform.position);
                dir.x = Mathf.Abs(dir.x) < 0.05f ? Mathf.Sign(transform.lossyScale.x) : Mathf.Sign(dir.x);
                dir = new Vector2(dir.x, 0.6f).normalized;
                var info = new DamageInfo(damage, damageType, owner, col.bounds.center, dir, knockback, lethal);
                bool applied = target.TakeDamage(info);
                HitTarget?.Invoke(target, applied);
                hits++;
            }
            return hits;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = IsOpen ? Color.red : new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireCube(Application.isPlaying ? (Vector3)WorldCenter : transform.position + (Vector3)offset, size);
        }
    }
}
