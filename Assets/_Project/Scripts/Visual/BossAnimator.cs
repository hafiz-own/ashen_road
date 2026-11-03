using AshenRoad.Boss;
using AshenRoad.Boss.States;
using AshenRoad.Combat;
using UnityEngine;

namespace AshenRoad.Visual
{
    /// <summary>Visual feedback for the boss: breathing idle, glowing wind-ups, a lean when charging, hit flashes.</summary>
    public class BossAnimator : MonoBehaviour
    {
        [SerializeField] BossController boss;
        [SerializeField] Transform body;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] SpriteAnimator frames;

        float flashUntil;

        void Reset() => boss = GetComponentInParent<BossController>();

        void OnEnable()
        {
            if (boss != null) boss.Health.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            if (boss != null) boss.Health.Damaged -= OnDamaged;
        }

        void OnDamaged(DamageInfo _) => flashUntil = Time.time + 0.1f;

        void LateUpdate()
        {
            if (boss == null || boss.Data == null || body == null || bodyRenderer == null) return;
            if (boss.Machine.Current is BossDefeatedState) return;

            if (frames != null && frames.HasArt)
            {
                if (boss.Telegraph >= 0f) frames.ShowNormalized("attack", boss.Telegraph * 0.6f);
                else if (boss.SwingHitbox != null && boss.SwingHitbox.IsOpen) frames.ShowNormalized("attack", 0.8f);
                else frames.Play("idle");
            }

            Vector3 pos = Vector3.zero;
            Vector3 scale = new Vector3(1f, 1f + Mathf.Sin(Time.time * 2.5f) * 0.03f, 1f);
            float angle = 0f;
            Color tint = boss.Data.tint;
            if (boss.Phase == 2) tint = Color.Lerp(tint, new Color(1f, 0.45f, 0.35f), 0.35f + 0.15f * Mathf.Sin(Time.time * 6f));

            if (boss.Telegraph >= 0f)
            {
                float t = boss.Telegraph;
                angle = 14f * t;
                scale = new Vector3(1f + 0.08f * t, 1f + 0.1f * t, 1f);
                pos.x = Mathf.Sin(Time.time * 50f) * 0.03f * t;
                tint = Color.Lerp(tint, new Color(1f, 0.2f, 0.1f), t);
            }
            else if (boss.Charging)
            {
                angle = -20f;
            }
            else if (Mathf.Abs(boss.Motor.Velocity.x) > 0.2f)
            {
                pos.y = Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.04f;
            }

            if (Time.time < flashUntil) tint = Color.white;

            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            Vector3 pivot = new Vector3(0f, 0.5f, 0f);
            body.localPosition = pos + pivot - rot * pivot;
            body.localRotation = rot;
            body.localScale = scale;
            bodyRenderer.color = tint;
        }
    }
}
