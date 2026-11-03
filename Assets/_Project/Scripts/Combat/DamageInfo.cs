using UnityEngine;

namespace AshenRoad.Combat
{
    public enum DamageType
    {
        Sword,
        Contact,
        EnemyAttack,
        Hazard,
        Fall,
        Front,
        Boss
    }

    /// <summary>The message passed to anything that can be damaged: how much, what kind, from where.</summary>
    public struct DamageInfo
    {
        public int Amount;
        public DamageType Type;
        public GameObject Source;
        public Vector2 Point;
        public Vector2 KnockbackDirection;
        public float KnockbackForce;
        /// <summary>Kills regardless of health, invulnerability or immunity (pits, the disaster front).</summary>
        public bool IsLethal;

        public DamageInfo(int amount, DamageType type, GameObject source, Vector2 point, Vector2 knockbackDirection, float knockbackForce, bool isLethal = false)
        {
            Amount = amount;
            Type = type;
            Source = source;
            Point = point;
            KnockbackDirection = knockbackDirection;
            KnockbackForce = knockbackForce;
            IsLethal = isLethal;
        }

        public static DamageInfo Lethal(DamageType type, Vector2 point)
        {
            return new DamageInfo(999, type, null, point, Vector2.zero, 0f, true);
        }
    }
}
