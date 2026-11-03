using AshenRoad.Combat;
using NUnit.Framework;
using UnityEngine;

namespace AshenRoad.Tests
{
    public class HealthModelTests
    {
        static DamageInfo Hit(int amount, DamageType type = DamageType.Sword) =>
            new DamageInfo(amount, type, null, Vector2.zero, Vector2.right, 1f);

        [Test]
        public void Damage_ReducesHealth_AndKillsAtZero()
        {
            var h = new HealthModel(3);
            Assert.AreEqual(DamageOutcome.Applied, h.ApplyDamage(Hit(1), 0f));
            Assert.AreEqual(2, h.Current);
            Assert.AreEqual(DamageOutcome.Killed, h.ApplyDamage(Hit(5), 0f));
            Assert.AreEqual(0, h.Current);
            Assert.IsFalse(h.IsAlive);
        }

        [Test]
        public void Damage_AfterDeath_IsRejected()
        {
            var h = new HealthModel(1);
            h.ApplyDamage(Hit(1), 0f);
            Assert.AreEqual(DamageOutcome.AlreadyDead, h.ApplyDamage(Hit(1), 1f));
        }

        [Test]
        public void Invulnerability_WindowBlocksDamage_ThenExpires()
        {
            var h = new HealthModel(3);
            h.StartInvulnerability(0f, 1f);
            Assert.AreEqual(DamageOutcome.Invulnerable, h.ApplyDamage(Hit(1), 0.5f));
            Assert.AreEqual(3, h.Current);
            Assert.AreEqual(DamageOutcome.Applied, h.ApplyDamage(Hit(1), 1.01f));
        }

        [Test]
        public void ManualInvulnerability_BlocksUntilCleared()
        {
            var h = new HealthModel(3) { ManualInvulnerable = true };
            Assert.AreEqual(DamageOutcome.Invulnerable, h.ApplyDamage(Hit(1), 10f));
            h.ManualInvulnerable = false;
            Assert.AreEqual(DamageOutcome.Applied, h.ApplyDamage(Hit(1), 10f));
        }

        [Test]
        public void Immunity_BlocksOnlyThatDamageType()
        {
            var h = new HealthModel(5);
            h.SetImmunities(new[] { DamageType.Sword });
            Assert.AreEqual(DamageOutcome.Immune, h.ApplyDamage(Hit(1, DamageType.Sword), 0f));
            Assert.AreEqual(DamageOutcome.Applied, h.ApplyDamage(Hit(1, DamageType.Hazard), 0f));
            Assert.AreEqual(4, h.Current);
        }

        [Test]
        public void Lethal_BypassesInvulnerabilityAndImmunity()
        {
            var h = new HealthModel(5) { ManualInvulnerable = true };
            h.SetImmunities(new[] { DamageType.Fall });
            Assert.AreEqual(DamageOutcome.Killed, h.ApplyDamage(DamageInfo.Lethal(DamageType.Fall, Vector2.zero), 0f));
        }

        [Test]
        public void Heal_IsCappedAtMax_AndReturnsAmountRestored()
        {
            var h = new HealthModel(3);
            h.ApplyDamage(Hit(2), 0f);
            Assert.AreEqual(1, h.Heal(1));
            Assert.AreEqual(1, h.Heal(5));
            Assert.AreEqual(3, h.Current);
            Assert.AreEqual(0, h.Heal(1));
        }

        [Test]
        public void Heal_DoesNotReviveTheDead()
        {
            var h = new HealthModel(1);
            h.ApplyDamage(Hit(1), 0f);
            Assert.AreEqual(0, h.Heal(1));
            Assert.IsFalse(h.IsAlive);
        }

        [Test]
        public void HealthComponent_RaisesEventsWithTheRightValues()
        {
            var go = new GameObject("health");
            try
            {
                var health = go.AddComponent<Health>();
                health.Configure(3, new[] { DamageType.Sword });
                int changedTo = -1, damaged = 0, died = 0, blocked = 0;
                health.Changed += (c, m) => changedTo = c;
                health.Damaged += _ => damaged++;
                health.Died += _ => died++;
                health.Blocked += _ => blocked++;

                Assert.IsFalse(health.TakeDamage(Hit(1, DamageType.Sword)));
                Assert.AreEqual(1, blocked);
                Assert.IsTrue(health.TakeDamage(Hit(1, DamageType.Hazard)));
                Assert.AreEqual(2, changedTo);
                health.Kill(DamageType.Fall);
                Assert.AreEqual(0, changedTo);
                Assert.AreEqual(2, damaged);
                Assert.AreEqual(1, died);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
