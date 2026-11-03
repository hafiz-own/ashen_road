using UnityEngine;

namespace AshenRoad.Enemies.States
{
    /// <summary>Closes in on the knight and attacks once in range; gives up after losing it for a while.</summary>
    public class EnemyChaseState : EnemyState
    {
        float attackReadyAt;

        public EnemyChaseState(EnemyAI enemy) : base(enemy) { }

        public void SetCooldown(float seconds) => attackReadyAt = Time.time + seconds;

        public override void FixedTick(float dt)
        {
            if (!Perception.HasTarget)
            {
                Resume();
                return;
            }
            int dir = Perception.DirectionToTarget;
            bool inRange = Perception.HorizontalDistance <= enemy.Data.attackRange && Mathf.Abs(Perception.VerticalOffset) < 1.4f;
            if (inRange && Time.time >= attackReadyAt)
            {
                Motor.Face(dir);
                ChangeTo(enemy.Attack);
                return;
            }
            if (Perception.HorizontalDistance < 0.3f)
            {
                Motor.Stop();
                return;
            }
            Motor.Face(dir);
            if (Motor.BlockedAhead) Motor.Stop();
            else Motor.Move(dir, enemy.Data.chaseSpeed);
        }
    }
}
