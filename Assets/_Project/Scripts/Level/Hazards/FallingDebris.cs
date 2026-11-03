using System;
using System.Collections;
using AshenRoad.Core;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Level.Hazards
{
    /// <summary>
    /// A rock that drops when the knight comes near: a shadow warns where it lands, it falls, hits once and breaks.
    /// One-shot (Repeats = false). Ambient debris reuses these through an object pool.
    /// </summary>
    public class FallingDebris : Hazard
    {
        [SerializeField] Transform rock;
        [SerializeField] SpriteRenderer shadow;
        [SerializeField] float triggerDistance = 3.5f;
        [SerializeField] float telegraphTime = 0.8f;
        [SerializeField] float dropHeight = 9f;
        [SerializeField] float gravity = 30f;

        PlayerController knight;
        bool armed = true;
        bool triggeredByCode;

        /// <summary>Raised when the rock has broken, so a pool can take this object back.</summary>
        public event Action<FallingDebris> Done;

        protected override bool StartsAutomatically => triggeredByCode;
        protected override bool Repeats => false;

        public void Setup(PlayerController player, float height)
        {
            knight = player;
            dropHeight = height;
            armed = true;
        }

        /// <summary>Drops immediately (ambient debris and tests).</summary>
        public void Drop(float height)
        {
            dropHeight = height;
            armed = false;
            triggeredByCode = true;
            ResetVisuals();
            Begin();
        }

        protected override void Awake()
        {
            base.Awake();
            ResetVisuals();
        }

        void ResetVisuals()
        {
            if (rock != null)
            {
                rock.gameObject.SetActive(false);
                rock.localPosition = new Vector3(0f, dropHeight, 0f);
            }
            if (shadow != null) shadow.enabled = false;
        }

        void Update()
        {
            if (!armed || knight == null || knight.IsDead) return;
            float dx = transform.position.x - knight.Position.x;
            if (dx < triggerDistance && dx > -1f)
            {
                armed = false;
                Begin();
            }
        }

        protected override IEnumerator Telegraph()
        {
            if (shadow != null) shadow.enabled = true;
            if (rock != null)
            {
                rock.gameObject.SetActive(true);
                rock.localPosition = new Vector3(0f, dropHeight, 0f);
            }
            float end = Time.time + telegraphTime;
            while (Time.time < end)
            {
                if (shadow != null)
                {
                    float t = 1f - (end - Time.time) / telegraphTime;
                    shadow.transform.localScale = new Vector3(0.5f + t * 0.7f, 1f, 1f);
                }
                if (rock != null) rock.localPosition = new Vector3(Mathf.Sin(Time.time * 60f) * 0.04f, dropHeight, 0f);
                yield return null;
            }
        }

        protected override void Activate()
        {
            hitbox?.Open();
        }

        protected override IEnumerator WhileActive()
        {
            float y = dropHeight;
            float speed = 0f;
            while (y > 0.4f)
            {
                speed += gravity * Time.deltaTime;
                y -= speed * Time.deltaTime;
                if (rock != null) rock.localPosition = new Vector3(0f, Mathf.Max(0.4f, y), 0f);
                if (hitbox != null) hitbox.Offset = new Vector2(0f, Mathf.Max(0.4f, y));
                yield return null;
            }
        }

        protected override void Deactivate()
        {
            hitbox?.Close();
            if (rock != null) rock.gameObject.SetActive(false);
            if (shadow != null) shadow.enabled = false;
            if (NearCamera()) GameEvents.RaiseSound("debris");
            GameEvents.RaiseEffect(EffectType.Debris, (Vector2)transform.position + Vector2.up * 0.4f, Vector2.up);
            if (NearCamera(10f)) GameEvents.RaiseCameraShake(0.15f, 0.15f);
        }

        protected override void Finished()
        {
            triggeredByCode = false;
            Done?.Invoke(this);
        }
    }
}
