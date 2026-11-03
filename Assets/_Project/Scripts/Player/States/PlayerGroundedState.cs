using AshenRoad.Core;
using UnityEngine;

namespace AshenRoad.Player.States
{
    /// <summary>On the ground: run with momentum, and start jumps, rolls and swings.</summary>
    public class PlayerGroundedState : PlayerState
    {
        float nextFootstep;

        public PlayerGroundedState(PlayerController player) : base(player) { }

        public override void FixedTick(float dt)
        {
            if (!Movement.IsGrounded)
            {
                Movement.ApplyGravity(dt);
                ChangeTo(player.Airborne);
                return;
            }
            if (player.HasBufferedJump)
            {
                player.ConsumeJump();
                Movement.Jump();
                if (!player.JumpHeld) Movement.CutJump();
                ChangeTo(player.Airborne);
                return;
            }
            if (player.HasBufferedRoll && Movement.CanRoll)
            {
                player.ConsumeRoll();
                ChangeTo(player.Rolling);
                return;
            }
            if (TryStartAttack()) return;

            Movement.ApplyHorizontal(player.MoveInput, dt);
            Movement.ApplyGravity(dt);
            Movement.ApplyPlatformCarry();

            if (Mathf.Abs(Movement.Velocity.x) > 2f && Time.time >= nextFootstep)
            {
                nextFootstep = Time.time + 0.32f;
                GameEvents.RaisePlayerActed(PlayerAction.Footstep);
            }
        }
    }
}
