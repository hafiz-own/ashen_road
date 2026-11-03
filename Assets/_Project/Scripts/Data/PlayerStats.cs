using UnityEngine;

namespace AshenRoad.Data
{
    /// <summary>Every tuning number for the knight. Shared, read-only data (Lecture 8b): never changed at runtime.</summary>
    [CreateAssetMenu(menuName = "Ashen Road/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Movement")]
        public float maxSpeed = 8f;
        public float acceleration = 60f;
        public float deceleration = 70f;
        [Range(0f, 1f)] public float airControl = 0.65f;

        [Header("Jump (designed from height and time, Lecture 12)")]
        public float jumpHeight = 3.2f;
        public float timeToApex = 0.38f;
        public float fallGravityMultiplier = 1.6f;
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.45f;
        public float maxFallSpeed = 18f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.12f;

        [Header("Roll")]
        public float rollDuration = 0.35f;
        public float rollSpeed = 12f;
        public float rollCooldown = 0.8f;

        [Header("Sword")]
        public int swordDamage = 1;
        public float swingWindup = 0.05f;
        public float swingActive = 0.12f;
        public float swingRecovery = 0.13f;
        public float swingCooldown = 0.35f;
        public float swingLunge = 2.5f;
        [Range(0f, 1f)] public float attackMoveMultiplier = 0.4f;
        public Vector2 swordHitboxSize = new Vector2(1.4f, 1.2f);
        public Vector2 swordHitboxOffset = new Vector2(0.8f, 0.05f);
        public float swordKnockback = 5f;

        [Header("Health")]
        public int maxHearts = 3;
        public float hurtInvulnerability = 1.0f;
        public float hurtStunTime = 0.3f;
        public Vector2 hurtKnockback = new Vector2(7f, 7f);

        /// <summary>Gravity that makes a jump reach jumpHeight in timeToApex: g = 2h / t².</summary>
        public float Gravity => 2f * jumpHeight / (timeToApex * timeToApex);

        /// <summary>Take-off speed for that jump: v = g·t.</summary>
        public float JumpVelocity => Gravity * timeToApex;

        public float FallGravity => Gravity * fallGravityMultiplier;

        /// <summary>
        /// Horizontal distance covered at full speed by a full jump that lands heightDelta above the start
        /// (negative = lower). Returns 0 if that height can't be reached.
        /// </summary>
        public float HorizontalReach(float heightDelta)
        {
            if (heightDelta > jumpHeight) return 0f;
            float drop = jumpHeight - heightDelta;
            float fallTime = Mathf.Sqrt(2f * drop / FallGravity);
            return maxSpeed * (timeToApex + fallTime);
        }
    }
}
