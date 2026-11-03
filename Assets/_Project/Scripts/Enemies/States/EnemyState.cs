using AshenRoad.Core;

namespace AshenRoad.Enemies.States
{
    /// <summary>Base for enemy states: knows its EnemyAI and offers a change-state helper.</summary>
    public abstract class EnemyState : IState
    {
        protected readonly EnemyAI enemy;

        protected EnemyState(EnemyAI enemy)
        {
            this.enemy = enemy;
        }

        protected EnemyMotor Motor => enemy.Motor;
        protected EnemyPerception Perception => enemy.Perception;

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void FixedTick(float fixedDeltaTime) { }
        public virtual void Exit() { }

        protected void ChangeTo(IState next) => enemy.Machine.ChangeState(next);

        /// <summary>Chase if the knight is known about, otherwise go back to patrolling.</summary>
        protected void Resume()
        {
            if (Perception.HasTarget) ChangeTo(enemy.Chase);
            else
            {
                enemy.SpawnAnchor = enemy.transform.position;
                ChangeTo(enemy.Patrol);
            }
        }

        public override string ToString() => GetType().Name.Replace("Enemy", "").Replace("State", "");
    }
}
