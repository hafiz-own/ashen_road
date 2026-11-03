using System.Collections;
using AshenRoad.Combat;
using AshenRoad.Core;
using AshenRoad.Data;
using UnityEngine;

namespace AshenRoad.Player
{
    /// <summary>The sword: wind-up, a short active window with the hitbox open, recovery and cooldown.</summary>
    public class PlayerCombat : MonoBehaviour
    {
        public enum SwingPhase { Idle, Windup, Active, Recovery }

        [SerializeField] Hitbox swordHitbox;

        PlayerStats stats;
        float phaseEndsAt;
        float cooldownUntil;
        static bool hitStopRunning;

        public SwingPhase Phase { get; private set; } = SwingPhase.Idle;
        public bool IsSwinging => Phase != SwingPhase.Idle;
        public bool CanSwing => !IsSwinging && Time.time >= cooldownUntil;
        public Hitbox SwordHitbox => swordHitbox;
        /// <summary>0 to 1 through the whole swing, for the sword animation.</summary>
        public float SwingProgress { get; private set; }
        public int Kills { get; set; }

        public void Init(PlayerStats playerStats)
        {
            stats = playerStats;
            if (swordHitbox == null) return;
            swordHitbox.Damage = stats.swordDamage;
            swordHitbox.Type = DamageType.Sword;
            swordHitbox.Size = stats.swordHitboxSize;
            swordHitbox.Offset = stats.swordHitboxOffset;
            swordHitbox.Knockback = stats.swordKnockback;
            swordHitbox.Targets = Layers.EnemyMask;
        }

        void OnEnable()
        {
            if (swordHitbox != null) swordHitbox.HitTarget += OnHit;
        }

        void OnDisable()
        {
            if (swordHitbox != null) swordHitbox.HitTarget -= OnHit;
        }

        public bool StartSwing()
        {
            if (!CanSwing) return false;
            Phase = SwingPhase.Windup;
            phaseEndsAt = Time.time + stats.swingWindup;
            SwingProgress = 0f;
            GameEvents.RaisePlayerActed(PlayerAction.Swing);
            return true;
        }

        public void Cancel()
        {
            if (swordHitbox != null) swordHitbox.Close();
            if (IsSwinging) cooldownUntil = Time.time + stats.swingCooldown;
            Phase = SwingPhase.Idle;
            SwingProgress = 0f;
        }

        public void Tick()
        {
            if (Phase == SwingPhase.Idle) return;
            float total = stats.swingWindup + stats.swingActive + stats.swingRecovery;
            float remainingAfterPhase = Phase == SwingPhase.Windup ? stats.swingActive + stats.swingRecovery
                : Phase == SwingPhase.Active ? stats.swingRecovery : 0f;
            SwingProgress = Mathf.Clamp01(1f - (phaseEndsAt - Time.time + remainingAfterPhase) / total);

            if (Time.time < phaseEndsAt) return;
            switch (Phase)
            {
                case SwingPhase.Windup:
                    Phase = SwingPhase.Active;
                    phaseEndsAt = Time.time + stats.swingActive;
                    swordHitbox?.Open(stats.swingActive);
                    GameEvents.RaiseEffect(EffectType.Slash, swordHitbox != null ? swordHitbox.WorldCenter : (Vector2)transform.position,
                        new Vector2(Mathf.Sign(transform.lossyScale.x), 0f));
                    break;
                case SwingPhase.Active:
                    swordHitbox?.Close();
                    Phase = SwingPhase.Recovery;
                    phaseEndsAt = Time.time + stats.swingRecovery;
                    break;
                case SwingPhase.Recovery:
                    Phase = SwingPhase.Idle;
                    SwingProgress = 0f;
                    cooldownUntil = Time.time + stats.swingCooldown - stats.swingRecovery;
                    break;
            }
        }

        void OnHit(IDamageable target, bool applied)
        {
            Vector2 point = target is Component c ? (Vector2)c.transform.position + Vector2.up * 0.4f : swordHitbox.WorldCenter;
            GameEvents.RaiseHitLanded(point, !applied);
            GameEvents.RaiseEffect(EffectType.Sparks, point, new Vector2(Mathf.Sign(transform.lossyScale.x), 0.2f));
            if (applied)
            {
                if (!target.IsAlive) Kills++;
                GameEvents.RaiseCameraShake(0.12f, 0.1f);
                if (isActiveAndEnabled) StartCoroutine(HitStop(0.045f));
            }
            else
            {
                GameEvents.RaiseCameraShake(0.06f, 0.08f);
            }
        }

        /// <summary>Freezes time for a few frames on a solid hit so it feels heavy.</summary>
        static IEnumerator HitStop(float duration)
        {
            if (hitStopRunning || Time.timeScale < 0.99f) yield break;
            hitStopRunning = true;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            if (Time.timeScale > 0f && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
                Time.timeScale = 1f;
            else if (GameManager.Instance == null && Time.timeScale > 0f)
                Time.timeScale = 1f;
            hitStopRunning = false;
        }
    }
}
