using AshenRoad.Core;
using AshenRoad.Enemies;

namespace AshenRoad.Boss.States
{
    /// <summary>Base for the boss's states: knows the BossController and offers a phase-change helper.</summary>
    public abstract class BossState : IState
    {
        protected readonly BossController boss;

        protected BossState(BossController boss)
        {
            this.boss = boss;
        }

        protected EnemyMotor Motor => boss.Motor;

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void FixedTick(float fixedDeltaTime) { }
        public virtual void Exit() { }

        protected void ChangeTo(IState next) => boss.Machine.ChangeState(next);

        public override string ToString() => GetType().Name.Replace("Boss", "").Replace("State", "");
    }
}
