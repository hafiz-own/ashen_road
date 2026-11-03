using System.Collections;
using AshenRoad.Combat;
using AshenRoad.Enemies;
using AshenRoad.Enemies.States;
using AshenRoad.Level;
using AshenRoad.Level.Hazards;
using AshenRoad.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AshenRoad.Tests
{
    [Timeout(60000)]
    public class CombatTests
    {
        TestWorld world;

        [SetUp] public void SetUp() => world = new TestWorld();
        [TearDown] public void TearDown() => world.Cleanup();

        IEnumerator Swing(PlayerController k, EnemyAI e, float distance = 1.2f)
        {
            yield return new WaitForSeconds(0.45f); // let any previous swing cool down
            if (!e.IsDead) e.transform.position = new Vector3(k.Position.x + distance, e.transform.position.y);
            k.PressAttack();
            yield return new WaitForSeconds(0.35f);
        }

        IEnumerator Setup(string enemyType, float distance, System.Action<PlayerController, EnemyAI> done)
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 0.1f));
            // no target: the enemy can't see the knight, so it won't interrupt the swings by attacking
            EnemyAI e = world.Enemy(enemyType, new Vector2(distance, 0.1f), null);
            e.Motor.Face(1); // patrol away from the knight so its body never touches him
            yield return new WaitForSeconds(0.3f);
            done(k, e);
        }

        [UnityTest]
        public IEnumerator Sword_KillsAShambler_InTwoHits()
        {
            PlayerController k = null; EnemyAI e = null;
            yield return Setup("Shambler", 1.2f, (a, b) => { k = a; e = b; });
            yield return Swing(k, e);
            Assert.AreEqual(1, e.Health.Current, "first hit");
            yield return Swing(k, e);
            Assert.IsTrue(e.IsDead, "second hit kills");
            Assert.IsInstanceOf<EnemyDeadState>(e.Machine.Current);
        }

        [UnityTest]
        public IEnumerator Sword_KillsARunner_InOneHit()
        {
            PlayerController k = null; EnemyAI e = null;
            yield return Setup("Runner", 1.2f, (a, b) => { k = a; e = b; });
            yield return Swing(k, e);
            Assert.IsTrue(e.IsDead);
        }

        [UnityTest]
        public IEnumerator Sword_DoesNothingToABrute_ButItFlinches()
        {
            PlayerController k = null; EnemyAI e = null;
            yield return Setup("Brute", 1.3f, (a, b) => { k = a; e = b; });
            int blocked = 0;
            e.Health.Blocked += _ => blocked++;
            yield return Swing(k, e, 1.3f);
            Assert.AreEqual(e.Health.Max, e.Health.Current);
            Assert.AreEqual(1, blocked);
        }

        [UnityTest]
        public IEnumerator FirePatch_KillsABrute()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(-15f, 0.1f));
            EnemyAI brute = world.Enemy("Brute", new Vector2(0f, 0.1f), k);
            FirePatch fire = world.Prefab<FirePatch>("FirePatch", new Vector2(0f, 0f));
            fire.Configure(30f, 0.1f, 0f);
            fire.Begin();
            float until = Time.time + 6f;
            while (!brute.IsDead && Time.time < until)
            {
                brute.transform.position = new Vector3(0f, brute.transform.position.y); // keep it standing in the fire
                yield return null;
            }
            Assert.IsTrue(brute.IsDead, "hazards hurt enemies");
        }

        [UnityTest]
        public IEnumerator Enemy_SeesTheKnight_AndAttacks()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 0.1f));
            EnemyAI e = world.Enemy("Shambler", new Vector2(3f, 0.1f), k);
            e.Motor.Face(-1);
            bool chased = false, attacked = false;
            e.Machine.StateChanged += (from, to) =>
            {
                chased |= to is EnemyChaseState;
                attacked |= to is EnemyAttackState;
            };
            float until = Time.time + 4f;
            while (!attacked && Time.time < until) yield return null;
            Assert.IsTrue(chased, "patrol -> chase");
            Assert.IsTrue(attacked, "chase -> attack");
        }

        [UnityTest]
        public IEnumerator Enemy_Attack_DamagesTheKnight()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 0.1f));
            EnemyAI e = world.Enemy("Shambler", new Vector2(1f, 0.1f), k);
            e.Motor.Face(-1);
            float until = Time.time + 4f;
            while (k.Health.Current == k.Health.Max && Time.time < until) yield return null;
            Assert.Less(k.Health.Current, k.Health.Max);
        }

        [UnityTest]
        public IEnumerator Roll_PassesThroughAnEnemyUnharmed()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 0.1f));
            EnemyAI e = world.Enemy("Brute", new Vector2(2.5f, 0.1f), k);
            e.Motor.Face(-1);
            yield return new WaitForSeconds(0.2f);
            k.PressRoll();
            yield return new WaitForSeconds(k.Stats.rollDuration + 0.05f);
            Assert.Greater(k.Position.x, e.transform.position.x, "ended up on the other side");
            Assert.AreEqual(k.Health.Max, k.Health.Current);
        }

        [UnityTest]
        public IEnumerator KillZone_KillsAnythingThatFallsIn()
        {
            PlayerController k = world.Knight(new Vector2(0f, 3f));
            var zone = world.Track(new GameObject("KillZone") { layer = Core.Layers.Trigger });
            var col = zone.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(10f, 2f);
            zone.AddComponent<KillZone>();
            float until = Time.time + 3f;
            while (!k.IsDead && Time.time < until) yield return null;
            Assert.IsTrue(k.IsDead);
            Assert.AreEqual(DamageType.Fall, k.LastDamage.Type);
        }

        [UnityTest]
        public IEnumerator HeartPickup_HealsOnlyWhenHurt()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 0.1f));
            HeartPickup heart = world.Prefab<HeartPickup>("HeartPickup", new Vector2(0f, 0f));
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(heart != null, "stays when health is full");
            k.Health.TakeDamage(new DamageInfo(1, DamageType.Hazard, null, Vector2.zero, Vector2.left, 0f));
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(k.Health.Max, k.Health.Current);
            Assert.IsTrue(heart == null, "consumed");
        }

        [UnityTest]
        public IEnumerator FallingDebris_DropsWhenTheKnightApproaches_AndHurts()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 0.1f));
            FallingDebris debris = world.Prefab<FallingDebris>("FallingDebris", new Vector2(2f, 0f));
            debris.Setup(k, 6f);
            yield return new WaitForSeconds(0.1f);
            k.transform.position = new Vector3(2f, 0.05f, 0f);
            float until = Time.time + 3f;
            while (k.Health.Current == k.Health.Max && Time.time < until)
            {
                k.Movement.Teleport(new Vector2(2f, 0.05f));
                yield return null;
            }
            Assert.Less(k.Health.Current, k.Health.Max);
        }
    }
}
