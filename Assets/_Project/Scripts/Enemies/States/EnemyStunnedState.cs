using UnityEngine;

namespace AshenRoad.Enemies.States
{
    /// <summary>A short flinch after being hit (or after a blocked hit on a Brute).</summary>
    public class EnemyStunnedState : EnemyState
    {
        float endsAt;

        public EnemyStunnedState(EnemyAI enemy) : base(enemy) { }

        public override void Enter()
        {
            endsAt = Time.time + enemy.Data.stunDuration;
        }

        public override void FixedTick(float dt)
        {
            if (!Motor.InKnockback) Motor.Stop();
            if (Time.time >= endsAt)
            {
                if (Perception.Target != null)
                {
                    Perception.Sense();
                    if (!Perception.CanSeeTarget && !Perception.Target.IsDead)
                        Motor.Face(Perception.DirectionToTarget); // hit from behind: turn round
                }
                Resume();
            }
        }
    }
}
