using System.Collections;
using AshenRoad.Core;
using UnityEngine;

namespace AshenRoad.Level.Hazards
{
    /// <summary>Flames that flare up on a cycle: glow as a warning, burn (hurting anyone inside, enemies too), go out.</summary>
    public class FirePatch : Hazard
    {
        [SerializeField] SpriteRenderer flames;
        [SerializeField] SpriteRenderer warning;
        [SerializeField] Sprite[] flameFrames = new Sprite[0];
        [SerializeField] float telegraphTime = 0.6f;
        [SerializeField] float onTime = 1.6f;
        [SerializeField] float offTime = 1.8f;
        [SerializeField] float tickInterval = 0.5f;

        float phaseOffset;

        public void Configure(float on, float off, float offset)
        {
            onTime = on;
            offTime = off;
            phaseOffset = offset;
        }

        protected override void Awake()
        {
            base.Awake();
            if (flames != null) flames.enabled = false;
            if (warning != null) warning.enabled = false;
        }

        protected override IEnumerator Initial()
        {
            if (phaseOffset > 0f) yield return new WaitForSeconds(phaseOffset);
        }

        protected override IEnumerator Telegraph()
        {
            if (warning == null) yield return new WaitForSeconds(telegraphTime);
            else
            {
                warning.enabled = true;
                float end = Time.time + telegraphTime;
                while (Time.time < end)
                {
                    Color c = warning.color;
                    c.a = 0.4f + 0.4f * Mathf.Sin(Time.time * 25f);
                    warning.color = c;
                    yield return null;
                }
                warning.enabled = false;
            }
        }

        protected override void Activate()
        {
            if (flames != null) flames.enabled = true;
            hitbox?.Open();
            if (NearCamera()) GameEvents.RaiseSound("fire");
        }

        protected override IEnumerator WhileActive()
        {
            float end = Time.time + onTime;
            float nextTick = Time.time + tickInterval;
            int frame = 0;
            while (Time.time < end)
            {
                if (Time.time >= nextTick)
                {
                    nextTick = Time.time + tickInterval;
                    hitbox?.ResetHits(); // damages occupants at fixed intervals
                }
                if (flames != null && flameFrames.Length > 0)
                {
                    frame = (frame + 1) % (flameFrames.Length * 4);
                    flames.sprite = flameFrames[frame / 4];
                }
                yield return null;
            }
        }

        protected override void Deactivate()
        {
            if (flames != null) flames.enabled = false;
            hitbox?.Close();
        }

        protected override IEnumerator Rest()
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, offTime - telegraphTime));
        }
    }
}
