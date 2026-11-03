namespace AshenRoad.Player.States
{
    /// <summary>Swinging the sword: facing is locked and movement slowed until the swing ends.</summary>
    public class PlayerAttackingState : PlayerState
    {
        public PlayerAttackingState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            Combat.StartSwing();
            Movement.FacingLocked = true;
            Movement.AddLunge(player.Stats.swingLunge);
        }

        public override void FixedTick(float dt)
        {
            Movement.ApplyHorizontal(player.MoveInput, dt, player.Stats.attackMoveMultiplier);
            Movement.ApplyGravity(dt);
            Movement.ApplyPlatformCarry();
            if (!Combat.IsSwinging) player.ReturnToFreeState();
        }

        public override void Exit()
        {
            Movement.FacingLocked = false;
            if (Combat.IsSwinging) Combat.Cancel();
        }
    }
}
