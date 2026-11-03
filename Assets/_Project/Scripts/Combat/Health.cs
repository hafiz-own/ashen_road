using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenRoad.Combat
{
    /// <summary>
    /// Health component shared by the knight, every enemy and the boss (Component pattern).
    /// Owns the health rules through HealthModel and announces changes with C# events (Observer pattern);
    /// it never references the UI that displays it.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] int maxHealth = 3;
        [SerializeField] float invulnerabilityOnHit = 0f;

        HealthModel model;

        public event Action<int, int> Changed;
        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;
        public event Action<DamageInfo> Blocked;

        public int Current => Model.Current;
        public int Max => Model.Max;
        public bool IsAlive => Model.IsAlive;
        public bool IsInvulnerable => Model.IsInvulnerable(Time.time);
        public float InvulnerabilityOnHit { get => invulnerabilityOnHit; set => invulnerabilityOnHit = value; }

        HealthModel Model => model ??= new HealthModel(Mathf.Max(1, maxHealth));

        /// <summary>Sets max health and immunities, refilling health. Called by whoever configures this object.</summary>
        public void Configure(int max, IEnumerable<DamageType> immunities = null, float hitInvulnerability = 0f)
        {
            maxHealth = Mathf.Max(1, max);
            invulnerabilityOnHit = hitInvulnerability;
            Model.Reset(maxHealth);
            Model.SetImmunities(immunities);
            Changed?.Invoke(Current, Max);
        }

        public bool TakeDamage(DamageInfo info)
        {
            DamageOutcome outcome = Model.ApplyDamage(info, Time.time);
            switch (outcome)
            {
                case DamageOutcome.Applied:
                    if (invulnerabilityOnHit > 0f) Model.StartInvulnerability(Time.time, invulnerabilityOnHit);
                    Changed?.Invoke(Current, Max);
                    Damaged?.Invoke(info);
                    return true;
                case DamageOutcome.Killed:
                    Changed?.Invoke(Current, Max);
                    Damaged?.Invoke(info);
                    Died?.Invoke(info);
                    return true;
                case DamageOutcome.Immune:
                    Blocked?.Invoke(info);
                    return false;
                default:
                    return false;
            }
        }

        public int Heal(int amount)
        {
            int healed = Model.Heal(amount);
            if (healed > 0) Changed?.Invoke(Current, Max);
            return healed;
        }

        public void StartInvulnerability(float duration) => Model.StartInvulnerability(Time.time, duration);

        public void SetManualInvulnerable(bool on) => Model.ManualInvulnerable = on;

        public void Kill(DamageType type)
        {
            TakeDamage(DamageInfo.Lethal(type, transform.position));
        }
    }
}
