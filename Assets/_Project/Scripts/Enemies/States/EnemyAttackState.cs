using AshenRoad.Core;
using AshenRoad.Data;
using UnityEngine;

namespace AshenRoad.Enemies.States
{
    /// <summary>
    /// Stop, telegraph, strike, recover. Swipe and Slam open the hitbox in place; Charge (the Runner)
    /// dashes forward with the hitbox open.
    /// </summary>
    public class EnemyAttackState : EnemyState
    {
        enum Step { Telegraph, Strike, Recover }

        Step step;
        float stepEndsAt;

        public EnemyAttackState(EnemyAI enemy) : base(enemy) { }

        /// <summary>0 to 1 during the wind-up, -1 otherwise. Read by the enemy's visuals.</summary>
        public float TelegraphProgress { get; private set; } = -1f;
        public bool IsStriking => step == Step.Strike;

        public override void Enter()
        {
            step = Step.Telegraph;
            stepEndsAt = Time.time + enemy.Data.attackTelegraph;
            Motor.Stop();
            TelegraphProgress = 0f;
        }

        public override void FixedTick(float dt)
        {
            EnemyData d = enemy.Data;
            switch (step)
            {
                case Step.Telegraph:
                    Motor.Stop();
                    TelegraphProgress = 1f - Mathf.Clamp01((stepEndsAt - Time.time) / d.attackTelegraph);
                    if (Time.time >= stepEndsAt) BeginStrike(d);
                    break;
                case Step.Strike:
                    if (d.attackStyle == AttackStyle.Charge)
                    {
                        if (Motor.BlockedAhead) Motor.Stop();
                        else Motor.Move(Motor.Facing, d.chargeSpeed);
                    }
                    else Motor.Stop();
                    if (Time.time >= stepEndsAt)
                    {
                        enemy.AttackHitbox?.Close();
                        step = Step.Recover;
                        stepEndsAt = Time.time + d.attackRecovery;
                    }
                    break;
                case Step.Recover:
                    Motor.Stop();
                    if (Time.time >= stepEndsAt)
                    {
                        enemy.Chase.SetCooldown(0.2f);
                        Resume();
                    }
                    break;
            }
        }

        void BeginStrike(EnemyData d)
        {
            step = Step.Strike;
            TelegraphProgress = -1f;
            stepEndsAt = Time.time + d.attackActive;
            enemy.AttackHitbox?.Open(d.attackActive);
            GameEvents.RaiseSound(d.attackStyle == AttackStyle.Slam ? "slam" : "enemy_attack");
            if (d.attackStyle == AttackStyle.Slam)
            {
                GameEvents.RaiseCameraShake(0.2f, 0.15f);
                GameEvents.RaiseEffect(EffectType.Dust, (Vector2)enemy.transform.position + new Vector2(Motor.Facing * 0.8f, 0f), Vector2.up);
            }
        }

        public override void Exit()
        {
            TelegraphProgress = -1f;
            enemy.AttackHitbox?.Close();
        }
    }
}
