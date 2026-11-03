using UnityEngine;

namespace AshenRoad.Player.States
{
    /// <summary>Dodge roll: fast, invulnerable, passes through enemies, then a cooldown.</summary>
    public class PlayerRollingState : PlayerState
    {
        public PlayerRollingState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            float input = player.MoveInput;
            int direction = Mathf.Abs(input) > 0.1f ? (input > 0f ? 1 : -1) : Movement.Facing;
            Movement.StartRoll(direction);
            player.Health.SetManualInvulnerable(true);
        }

        public override void FixedTick(float dt)
        {
            Movement.RollTick();
            Movement.ApplyGravity(dt);
            if (Movement.RollFinished) player.ReturnToFreeState();
        }

        public override void Exit()
        {
            Movement.EndRoll();
            player.Health.SetManualInvulnerable(false);
        }
    }
}
