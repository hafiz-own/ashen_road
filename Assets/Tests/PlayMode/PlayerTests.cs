using System.Collections;
using AshenRoad.Combat;
using AshenRoad.Player;
using AshenRoad.Player.States;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AshenRoad.Tests
{
    [Timeout(60000)]
    public class PlayerTests
    {
        TestWorld world;

        [SetUp] public void SetUp() => world = new TestWorld();
        [TearDown] public void TearDown() => world.Cleanup();

        IEnumerator Settle(PlayerController k)
        {
            float until = Time.time + 2f;
            while (!k.Movement.IsGrounded && Time.time < until) yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Knight_FallsOntoGround_AndBecomesGrounded()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 2f));
            yield return Settle(k);
            Assert.IsTrue(k.Movement.IsGrounded);
            Assert.IsInstanceOf<PlayerGroundedState>(k.Machine.Current);
            Assert.AreEqual(0f, k.Position.y, 0.08f);
        }

        [UnityTest]
        public IEnumerator Knight_AcceleratesToMaxSpeed_AndStopsWithMomentum()
        {
            world.Ground(-5f, 60f);
            PlayerController k = world.Knight(new Vector2(0f, 0.2f));
            yield return Settle(k);
            k.SetScriptedMove(1f);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(k.Stats.maxSpeed, k.Movement.Velocity.x, 0.3f);
            k.SetScriptedMove(0f);
            yield return new WaitForFixedUpdate();
            Assert.Greater(k.Movement.Velocity.x, 1f, "releasing should slow down gradually, not stop dead");
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(0f, k.Movement.Velocity.x, 0.05f);
        }

        [UnityTest]
        public IEnumerator Knight_FullJump_ReachesDesignedHeight()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 0.2f));
            yield return Settle(k);
            float startY = k.Position.y;
            k.PressJump();
            float peak = startY;
            for (float t = 0f; t < 1f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, k.Position.y);
            }
            Assert.AreEqual(k.Stats.jumpHeight, peak - startY, k.Stats.jumpHeight * 0.1f);
            Assert.IsTrue(k.Movement.IsGrounded, "landed again");
        }

        [UnityTest]
        public IEnumerator Knight_ShortJump_IsLowerThanFullJump()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 0.2f));
            yield return Settle(k);
            float startY = k.Position.y;
            k.PressJump();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            k.ReleaseJump();
            float peak = startY;
            for (float t = 0f; t < 1f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, k.Position.y);
            }
            Assert.Less(peak - startY, k.Stats.jumpHeight * 0.6f);
        }

        [UnityTest]
        public IEnumerator Knight_CoyoteTime_AllowsJumpJustAfterLeavingALedge()
        {
            world.Ground(-10f, 0f);
            PlayerController k = world.Knight(new Vector2(-1f, 0.2f));
            yield return Settle(k);
            k.SetScriptedMove(1f);
            float until = Time.time + 2f;
            while (k.Movement.IsGrounded && Time.time < until) yield return new WaitForFixedUpdate();
            Assert.IsFalse(k.Movement.IsGrounded, "walked off the ledge");
            k.PressJump();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.Greater(k.Movement.Velocity.y, 5f, "coyote jump fired");
        }

        [UnityTest]
        public IEnumerator Knight_Roll_IsFast_Invulnerable_AndHasACooldown()
        {
            world.Ground(-5f, 40f);
            PlayerController k = world.Knight(new Vector2(0f, 0.2f));
            yield return Settle(k);
            k.PressRoll();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsInstanceOf<PlayerRollingState>(k.Machine.Current);
            Assert.Greater(k.Movement.Velocity.x, k.Stats.maxSpeed);
            var hit = new DamageInfo(1, DamageType.EnemyAttack, null, Vector2.zero, Vector2.left, 5f);
            Assert.IsFalse(k.Health.TakeDamage(hit), "invulnerable while rolling");
            yield return new WaitForSeconds(k.Stats.rollDuration + 0.05f);
            Assert.IsNotInstanceOf<PlayerRollingState>(k.Machine.Current);
            Assert.IsFalse(k.Movement.CanRoll, "cooldown running");
            yield return new WaitForSeconds(k.Stats.rollCooldown);
            Assert.IsTrue(k.Movement.CanRoll);
        }

        [UnityTest]
        public IEnumerator Knight_Hurt_KnockedBack_ThenInvulnerable_ThenRecovers()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 0.2f));
            yield return Settle(k);
            var hit = new DamageInfo(1, DamageType.EnemyAttack, null, Vector2.zero, new Vector2(-1f, 0.5f), 6f);
            Assert.IsTrue(k.Health.TakeDamage(hit));
            Assert.AreEqual(2, k.Health.Current);
            yield return new WaitForFixedUpdate();
            Assert.IsInstanceOf<PlayerHurtState>(k.Machine.Current);
            Assert.Less(k.Movement.Velocity.x, 0f, "pushed away from the hit");
            Assert.IsFalse(k.Health.TakeDamage(hit), "invulnerable after a hit");
            yield return new WaitForSeconds(k.Stats.hurtInvulnerability + 0.1f);
            Assert.IsNotInstanceOf<PlayerHurtState>(k.Machine.Current, "stun is over");
            Assert.IsTrue(k.Health.TakeDamage(hit), "vulnerable again");
        }

        [UnityTest]
        public IEnumerator Knight_Dies_AtZeroHealth_AndReportsTheCause()
        {
            world.Ground(-20f, 20f);
            PlayerController k = world.Knight(new Vector2(0f, 0.2f));
            yield return Settle(k);
            Core.DeathCause cause = Core.DeathCause.None;
            System.Action<Core.DeathCause> listener = c => cause = c;
            Core.GameEvents.PlayerDied += listener;
            try
            {
                k.Health.TakeDamage(DamageInfo.Lethal(DamageType.Hazard, Vector2.zero));
                yield return new WaitForFixedUpdate();
                Assert.IsTrue(k.IsDead);
                Assert.IsInstanceOf<PlayerDeadState>(k.Machine.Current);
                Assert.AreEqual(Core.DeathCause.Hazard, cause);
            }
            finally
            {
                Core.GameEvents.PlayerDied -= listener;
            }
        }

        // The level checker assumes these moves are possible; these tests prove them with real physics.

        [UnityTest]
        public IEnumerator RunningJump_ClearsAThreeTileGap()
        {
            world.Ground(-10f, 0f);
            world.Ground(3f, 20f);
            PlayerController k = world.Knight(new Vector2(-6f, 0.2f));
            yield return Settle(k);
            k.SetScriptedMove(1f);
            float until = Time.time + 3f;
            while (k.Position.x < -0.4f && Time.time < until) yield return new WaitForFixedUpdate();
            k.PressJump();
            until = Time.time + 2f;
            while (k.Position.x < 4f && Time.time < until && k.Position.y > -2f) yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(k.Position.x, 3.2f, "landed on the far side");
            Assert.AreEqual(0f, k.Position.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator Jump_ReachesALedgeTwoTilesUp()
        {
            world.Ground(-10f, 2f);
            world.Ground(2f, 12f, 2f, 4f);
            PlayerController k = world.Knight(new Vector2(-2f, 0.2f));
            yield return Settle(k);
            k.SetScriptedMove(1f);
            k.PressJump();
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(k.Position.x, 2.5f);
            Assert.AreEqual(2f, k.Position.y, 0.1f, "standing on the ledge");
        }

        [UnityTest]
        public IEnumerator MovingPlatform_CarriesTheKnight()
        {
            world.Ground(-10f, 0f);
            Level.MovingPlatform platform = world.Prefab<Level.MovingPlatform>("MovingPlatform", new Vector2(0f, -1f));
            platform.SetWidth(3);
            platform.Setup(new Vector2(6f, 0f), 2f);
            PlayerController k = world.Knight(new Vector2(1.5f, 0.3f));
            yield return Settle(k);
            float startX = k.Position.x;
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(k.Position.x, startX + 1.5f, "rode along");
            Assert.IsTrue(k.Movement.IsGrounded);
        }
    }
}
