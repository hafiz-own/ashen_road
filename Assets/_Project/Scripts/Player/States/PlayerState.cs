using AshenRoad.Core;

namespace AshenRoad.Player.States
{
    /// <summary>Base for the knight's states: knows its owner and provides empty hooks to override.</summary>
    public abstract class PlayerState : IState
    {
        protected readonly PlayerController player;

        protected PlayerState(PlayerController player)
        {
            this.player = player;
        }

        protected PlayerMovement Movement => player.Movement;
        protected PlayerCombat Combat => player.Combat;

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void FixedTick(float fixedDeltaTime) { }
        public virtual void Exit() { }

        protected void ChangeTo(IState next) => player.Machine.ChangeState(next);

        /// <summary>Shared by Grounded and Airborne: the actions any free state may start.</summary>
        protected bool TryStartAttack()
        {
            if (!player.HasBufferedAttack || !Combat.CanSwing) return false;
            player.ConsumeAttack();
            ChangeTo(player.Attacking);
            return true;
        }

        public override string ToString() => GetType().Name.Replace("Player", "").Replace("State", "");
    }
}
