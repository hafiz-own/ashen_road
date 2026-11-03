using AshenRoad.Combat;
using UnityEngine;

namespace AshenRoad.Player.States
{
    /// <summary>Knocked back and briefly stunned after a hit; the invulnerability window runs in Health.</summary>
    public class PlayerHurtState : PlayerState
    {
        DamageInfo info;
        float endsAt;

        public PlayerHurtState(PlayerController player) : base(player) { }

        public void Setup(DamageInfo damage) => info = damage;

        public override void Enter()
        {
            Combat.Cancel();
            if (Movement.IsRolling) Movement.EndRoll();
            Vector2 strength = player.Stats.hurtKnockback * Mathf.Max(0.5f, info.KnockbackForce / 6f);
            Movement.Knockback(info.KnockbackDirection, strength);
            endsAt = Time.time + player.Stats.hurtStunTime;
        }

        public override void FixedTick(float dt)
        {
            Movement.Brake(dt, 10f);
            Movement.ApplyGravity(dt);
            if (Time.time >= endsAt) player.ReturnToFreeState();
        }
    }
}
