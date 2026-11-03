using AshenRoad.Combat;
using AshenRoad.Enemies;
using AshenRoad.Enemies.States;
using UnityEngine;

namespace AshenRoad.Visual
{
    /// <summary>Walk bob, a shaking red wind-up before attacks (the telegraph), and a flash when hit.</summary>
    public class EnemyAnimator : MonoBehaviour
    {
        [SerializeField] EnemyAI enemy;
        [SerializeField] Transform body;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] SpriteAnimator frames;

        float flashUntil;
        bool blockedFlash;

        void Reset() => enemy = GetComponentInParent<EnemyAI>();

        void OnEnable()
        {
            if (enemy == null) return;
            enemy.Health.Damaged += OnDamaged;
            enemy.Health.Blocked += OnBlocked;
        }

        void OnDisable()
        {
            if (enemy == null) return;
            enemy.Health.Damaged -= OnDamaged;
            enemy.Health.Blocked -= OnBlocked;
        }

        void OnDamaged(DamageInfo _) { flashUntil = Time.time + 0.1f; blockedFlash = false; }

        void OnBlocked(DamageInfo _) { flashUntil = Time.time + 0.12f; blockedFlash = true; }

        void LateUpdate()
        {
            if (enemy == null || enemy.Data == null || body == null) return;
            var state = enemy.Machine.Current;
            if (frames != null)
            {
                if (frames.Art != enemy.Data.art) frames.Art = enemy.Data.art;
                if (frames.HasArt)
                {
                    bool moving = Mathf.Abs(enemy.Motor.Velocity.x) > 0.2f && !(state is EnemyDeadState);
                    if (moving || state is EnemyAttackState) frames.Play("walk");
                    else frames.ShowNormalized("walk", 0f);
                }
            }
            Vector3 pos = Vector3.zero;
            float angle = 0f;
            Vector3 scale = Vector3.one;
            Color tint = enemy.Data.tint;

            if ((state is EnemyPatrolState || state is EnemyChaseState) && Mathf.Abs(enemy.Motor.Velocity.x) > 0.2f)
            {
                float speed = state is EnemyChaseState ? 14f : 9f;
                pos.y = Mathf.Abs(Mathf.Sin(Time.time * speed + transform.position.x)) * 0.06f;
                angle = Mathf.Sin(Time.time * speed * 0.5f) * 4f;
            }
            else if (state is EnemyAttackState attack)
            {
                if (attack.TelegraphProgress >= 0f)
                {
                    float t = attack.TelegraphProgress;
                    pos.x = Mathf.Sin(Time.time * 60f) * 0.04f * t;
                    angle = 12f * t;
                    scale = new Vector3(1f + 0.1f * t, 1f - 0.08f * t, 1f);
                    tint = Color.Lerp(tint, new Color(1f, 0.3f, 0.2f, tint.a), t * 0.8f);
                }
                else if (attack.IsStriking)
                {
                    angle = -18f;
                    pos.x = 0.12f;
                }
            }
            else if (state is EnemyStunnedState)
            {
                angle = 10f;
            }
            else if (state is EnemyDeadState)
            {
                angle = 80f;
                tint = new Color(0.5f, 0.5f, 0.5f, bodyRenderer != null ? bodyRenderer.color.a : 1f);
            }

            if (Time.time < flashUntil && !(state is EnemyDeadState))
                tint = blockedFlash ? new Color(0.7f, 0.8f, 1f, 1f) : new Color(1f, 1f, 1f, 1f);

            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            Vector3 pivot = new Vector3(0f, 0.5f, 0f);
            body.localPosition = pos + pivot - rot * pivot;
            body.localRotation = rot;
            body.localScale = scale;
            if (bodyRenderer != null && !(state is EnemyDeadState)) bodyRenderer.color = tint;
            else if (bodyRenderer != null) bodyRenderer.color = new Color(tint.r, tint.g, tint.b, bodyRenderer.color.a);
        }
    }
}
