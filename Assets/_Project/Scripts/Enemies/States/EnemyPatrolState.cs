using UnityEngine;

namespace AshenRoad.Enemies.States
{
    /// <summary>Walks back and forth around the spawn point, pausing at each end, until it sees the knight.</summary>
    public class EnemyPatrolState : EnemyState
    {
        float pauseUntil;

        public EnemyPatrolState(EnemyAI enemy) : base(enemy) { }

        public override void Enter()
        {
            pauseUntil = Time.time + Random.Range(0f, enemy.Data.patrolPause);
        }

        public override void FixedTick(float dt)
        {
            if (Perception.CanSeeTarget)
            {
                ChangeTo(enemy.Chase);
                return;
            }
            if (Time.time < pauseUntil)
            {
                Motor.Stop();
                return;
            }

            float offset = enemy.transform.position.x - enemy.SpawnAnchor.x;
            bool pastEnd = (Motor.Facing > 0 && offset > enemy.Data.patrolRadius) || (Motor.Facing < 0 && offset < -enemy.Data.patrolRadius);
            if (pastEnd || Motor.BlockedAhead)
            {
                Motor.Stop();
                Motor.Face(-Motor.Facing);
                pauseUntil = Time.time + enemy.Data.patrolPause;
                return;
            }
            Motor.Move(Motor.Facing, enemy.Data.patrolSpeed);
        }
    }
}
