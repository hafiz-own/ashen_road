using System;
using System.Collections.Generic;

namespace AshenRoad.Combat
{
    public enum DamageOutcome
    {
        Applied,
        Killed,
        Invulnerable,
        Immune,
        AlreadyDead,
        Ignored
    }

    /// <summary>
    /// The health rules with no Unity dependencies, so they are unit-tested directly.
    /// Time is passed in by the caller instead of read from a clock.
    /// </summary>
    public class HealthModel
    {
        readonly HashSet<DamageType> immunities = new HashSet<DamageType>();
        float invulnerableUntil = float.NegativeInfinity;

        public int Max { get; private set; }
        public int Current { get; private set; }
        public bool IsAlive => Current > 0;
        /// <summary>Held on while rolling or during cut-scenes, independent of the timed window.</summary>
        public bool ManualInvulnerable { get; set; }

        public HealthModel(int max)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max));
            Max = max;
            Current = max;
        }

        public void SetImmunities(IEnumerable<DamageType> types)
        {
            immunities.Clear();
            if (types == null) return;
            foreach (DamageType t in types) immunities.Add(t);
        }

        public bool IsImmuneTo(DamageType type) => immunities.Contains(type);

        public bool IsInvulnerable(float now) => ManualInvulnerable || now < invulnerableUntil;

        public void StartInvulnerability(float now, float duration)
        {
            invulnerableUntil = Math.Max(invulnerableUntil, now + duration);
        }

        public DamageOutcome ApplyDamage(DamageInfo info, float now)
        {
            if (!IsAlive) return DamageOutcome.AlreadyDead;
            if (info.IsLethal)
            {
                Current = 0;
                return DamageOutcome.Killed;
            }
            if (info.Amount <= 0) return DamageOutcome.Ignored;
            if (IsImmuneTo(info.Type)) return DamageOutcome.Immune;
            if (IsInvulnerable(now)) return DamageOutcome.Invulnerable;

            Current = Math.Max(0, Current - info.Amount);
            return Current == 0 ? DamageOutcome.Killed : DamageOutcome.Applied;
        }

        /// <summary>Heals up to the maximum. Returns how much was actually restored.</summary>
        public int Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return 0;
            int before = Current;
            Current = Math.Min(Max, Current + amount);
            return Current - before;
        }

        public void Reset(int max)
        {
            Max = max;
            Current = max;
            invulnerableUntil = float.NegativeInfinity;
            ManualInvulnerable = false;
        }
    }
}
