using System.Collections.Generic;
using UnityEngine;

namespace AshenRoad.Combat
{
    /// <summary>Hurts whatever in the target layers touches this body. The target's own invulnerability limits the rate.</summary>
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField] int damage = 1;
        [SerializeField] float knockback = 6f;
        [SerializeField] DamageType damageType = DamageType.Contact;
        [SerializeField] LayerMask targets;
        [SerializeField] Collider2D body;

        readonly List<Collider2D> results = new List<Collider2D>();

        public int Damage { get => damage; set => damage = value; }
        public float Knockback { get => knockback; set => knockback = value; }
        public DamageType Type { get => damageType; set => damageType = value; }
        public LayerMask Targets { get => targets; set => targets = value; }

        void Awake()
        {
            if (body == null) body = GetComponent<Collider2D>();
        }

        void FixedUpdate()
        {
            if (body == null || !body.enabled || damage <= 0) return;
            Bounds b = body.bounds;
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = targets };
            results.Clear();
            Physics2D.OverlapBox(b.center, (Vector2)b.size * 0.9f, 0f, filter, results);
            foreach (Collider2D col in results)
            {
                IDamageable target = col.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive || target is Component c && c.gameObject == gameObject) continue;
                float dirX = Mathf.Sign(col.bounds.center.x - b.center.x);
                if (dirX == 0f) dirX = 1f;
                var info = new DamageInfo(damage, damageType, gameObject, col.bounds.center, new Vector2(dirX, 0.7f).normalized, knockback);
                target.TakeDamage(info);
            }
        }
    }
}
