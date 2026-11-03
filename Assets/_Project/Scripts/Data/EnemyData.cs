using AshenRoad.Combat;
using UnityEngine;

namespace AshenRoad.Data
{
    public enum AttackStyle
    {
        Swipe,
        Charge,
        Slam
    }

    /// <summary>
    /// One enemy type (Type Object / data-driven design). Shambler, Runner and Brute are three assets of this
    /// class built on the same prefab; the factory reads them to configure each spawned enemy.
    /// </summary>
    [CreateAssetMenu(menuName = "Ashen Road/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Shambler";
        public char layoutSymbol = 'S';
        public GameObject prefab;
        public Sprite sprite;
        [Tooltip("Animated frames. When set, these replace the single sprite.")]
        public CharacterArt art;
        public Color tint = Color.white;
        public float scale = 1f;
        public Vector2 colliderSize = new Vector2(0.7f, 0.9f);

        [Header("Stats")]
        public int maxHealth = 2;
        public float patrolSpeed = 0.8f;
        public float chaseSpeed = 1.4f;
        public float patrolRadius = 3f;
        public float patrolPause = 1f;
        public float sightRange = 5f;
        public float loseTargetTime = 2f;

        [Header("Attack")]
        public AttackStyle attackStyle = AttackStyle.Swipe;
        public float attackRange = 1.1f;
        public int attackDamage = 1;
        public float attackTelegraph = 0.5f;
        public float attackActive = 0.15f;
        public float attackRecovery = 0.8f;
        public float chargeSpeed = 9f;
        public Vector2 attackHitboxSize = new Vector2(1.1f, 1f);
        public Vector2 attackHitboxOffset = new Vector2(0.6f, 0f);
        public float attackKnockback = 6f;

        [Header("Contact")]
        public int contactDamage = 1;
        public float contactKnockback = 6f;

        [Header("Defence")]
        public DamageType[] immunities = new DamageType[0];
        public float stunDuration = 0.35f;
        public float knockbackResistance = 0f;

        [Header("Death")]
        public float despawnDelay = 1.2f;
        public string deathSound = "enemy_death";

        public bool IsImmuneTo(DamageType type)
        {
            foreach (DamageType t in immunities)
                if (t == type) return true;
            return false;
        }
    }
}
