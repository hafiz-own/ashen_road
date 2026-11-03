namespace AshenRoad.Player.States
{
    /// <summary>In the air: reduced air control, coyote-time jumps, jump cut, air attacks, landing.</summary>
    public class PlayerAirborneState : PlayerState
    {
        public PlayerAirborneState(PlayerController player) : base(player) { }

        public override void FixedTick(float dt)
        {
            if (player.HasBufferedJump && Movement.CanJump)
            {
                player.ConsumeJump();
                Movement.Jump();
                if (!player.JumpHeld) Movement.CutJump();
            }
            if (TryStartAttack()) return;

            Movement.ApplyHorizontal(player.MoveInput, dt);
            Movement.ApplyGravity(dt);

            if (Movement.IsGrounded && !Movement.IsRising)
                ChangeTo(player.Grounded);
        }
    }
}
