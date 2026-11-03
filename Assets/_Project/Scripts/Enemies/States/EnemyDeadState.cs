using AshenRoad.Core;
using UnityEngine;

namespace AshenRoad.Enemies.States
{
    /// <summary>Stops everything harmful, lets the body settle, fades it out and removes it.</summary>
    public class EnemyDeadState : EnemyState
    {
        float removeAt;

        public EnemyDeadState(EnemyAI enemy) : base(enemy) { }

        public override void Enter()
        {
            enemy.AttackHitbox?.Close();
            enemy.SetContactDamage(false);
            enemy.gameObject.layer = Layers.Corpse;
            removeAt = Time.time + enemy.Data.despawnDelay;
        }

        public override void FixedTick(float dt)
        {
            if (!Motor.InKnockback) Motor.Freeze();
        }

        public override void Tick(float dt)
        {
            if (enemy.Visual != null)
            {
                float left = Mathf.Clamp01((removeAt - Time.time) / 0.5f);
                Color c = enemy.Visual.color;
                c.a = left;
                enemy.Visual.color = c;
            }
            if (Time.time >= removeAt) enemy.Despawn();
        }
    }
}
