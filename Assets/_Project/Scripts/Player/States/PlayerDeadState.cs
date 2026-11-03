using AshenRoad.Core;
using UnityEngine;

namespace AshenRoad.Player.States
{
    /// <summary>Dead: input ignored, sword put away, the body only collides with the ground.</summary>
    public class PlayerDeadState : PlayerState
    {
        public PlayerDeadState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            Combat.Cancel();
            if (Movement.IsRolling) Movement.EndRoll();
            player.InputLocked = true;
            player.SetScriptedMove(0f);
            player.gameObject.layer = Layers.Corpse;
            Vector2 v = Movement.Velocity;
            Movement.StopAll();
            if (v.y > -2f) Movement.Knockback(new Vector2(-Movement.Facing, 1f), new Vector2(2f, 5f));
        }

        public override void FixedTick(float dt)
        {
            Movement.UpdateGround();
            Movement.Brake(dt, 12f);
            Movement.ApplyGravity(dt);
        }
    }
}
