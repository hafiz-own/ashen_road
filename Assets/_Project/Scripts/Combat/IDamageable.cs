namespace AshenRoad.Combat
{
    /// <summary>
    /// Anything that can take damage. Attackers only know this interface, so a sword, a trap or the front
    /// can hurt the knight, an enemy or the boss without knowing which (Lecture 8b, Lecture 10 low coupling).
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        /// <summary>Returns true if the damage was applied (false when invulnerable, immune or already dead).</summary>
        bool TakeDamage(DamageInfo info);
    }
}
