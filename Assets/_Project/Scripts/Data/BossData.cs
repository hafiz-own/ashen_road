using UnityEngine;

namespace AshenRoad.Data
{
    /// <summary>Tuning for the Ash Warden, the level 8 boss.</summary>
    [CreateAssetMenu(menuName = "Ashen Road/Boss Data")]
    public class BossData : ScriptableObject
    {
        public string displayName = "The Ash Warden";
        public Sprite sprite;
        public Color tint = Color.white;
        public float scale = 2.5f;
        public Vector2 colliderSize = new Vector2(0.7f, 0.9f);

        [Header("Health")]
        public int maxHealth = 20;
        [Range(0f, 1f)] public float phaseTwoThreshold = 0.5f;

        [Header("Intro")]
        public float introDuration = 2.2f;

        [Header("Phase one")]
        public float walkSpeed = 1.6f;
        public int swingDamage = 1;
        public float swingTelegraph = 0.7f;
        public float swingActive = 0.2f;
        public float swingRecovery = 1.0f;
        public float swingRange = 2.4f;
        public Vector2 swingHitboxSize = new Vector2(2.6f, 2.4f);
        public Vector2 swingHitboxOffset = new Vector2(1.3f, 0f);
        public float summonInterval = 12f;
        public int summonCount = 2;
        public int maxSummonsAlive = 3;

        [Header("Phase two")]
        public float phaseTwoWalkSpeed = 2.4f;
        public float phaseTwoTelegraph = 0.45f;
        public float phaseTwoRecovery = 0.6f;
        public float chargeTelegraph = 0.6f;
        public float chargeSpeed = 11f;
        public float chargeDuration = 0.9f;
        public float chargeInterval = 5f;
        public float phaseTwoSummonInterval = 18f;

        [Header("Contact")]
        public int contactDamage = 1;
        public float knockback = 8f;

        [Header("Summon type")]
        public EnemyData summonType;
    }
}
