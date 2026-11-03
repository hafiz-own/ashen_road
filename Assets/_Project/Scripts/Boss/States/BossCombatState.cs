using AshenRoad.Core;
using UnityEngine;

namespace AshenRoad.Boss.States
{
    /// <summary>
    /// The fighting loop shared by both phases: walk in, telegraph a swing, strike, recover, and summon on a timer.
    /// Subclasses supply the numbers and may add a charge (Template Method on top of the State pattern).
    /// </summary>
    public abstract class BossCombatState : BossState
    {
        protected enum Step { Approach, SwingTelegraph, SwingStrike, ChargeTelegraph, Charging, Recover }

        protected Step step;
        float stepEndsAt;
        float nextSummonAt;
        float nextChargeAt;

        protected BossCombatState(BossController boss) : base(boss) { }

        protected abstract float WalkSpeed { get; }
        protected abstract float SwingTelegraph { get; }
        protected abstract float Recovery { get; }
        protected abstract float SummonInterval { get; }
        protected virtual bool CanCharge => false;

        public override void Enter()
        {
            step = Step.Approach;
            nextSummonAt = Time.time + SummonInterval * 0.5f;
            nextChargeAt = Time.time + boss.Data.chargeInterval;
        }

        public override void FixedTick(float dt)
        {
            if (boss.Target == null || boss.Target.IsDead)
            {
                Motor.Stop();
                return;
            }
            if (Time.time >= nextSummonAt && step == Step.Approach)
            {
                nextSummonAt = Time.time + SummonInterval;
                boss.Summon(boss.Data.summonCount);
            }

            int dir = boss.Perception.DirectionToTarget;
            float distance = boss.Perception.HorizontalDistance;
            switch (step)
            {
                case Step.Approach:
                    boss.Telegraph = -1f;
                    if (CanCharge && Time.time >= nextChargeAt && distance > 3f)
                    {
                        Motor.Stop();
                        Motor.Face(dir);
                        Begin(Step.ChargeTelegraph, boss.Data.chargeTelegraph);
                        GameEvents.RaiseSound("boss_charge");
                    }
                    else if (distance <= boss.Data.swingRange)
                    {
                        Motor.Stop();
                        Motor.Face(dir);
                        Begin(Step.SwingTelegraph, SwingTelegraph);
                    }
                    else if (Motor.BlockedAhead && Mathf.Sign(dir) == Motor.Facing) Motor.Stop();
                    else Motor.Move(dir, WalkSpeed);
                    break;

                case Step.SwingTelegraph:
                    Motor.Stop();
                    boss.Telegraph = Progress(SwingTelegraph);
                    if (Time.time >= stepEndsAt)
                    {
                        boss.Telegraph = -1f;
                        boss.SwingHitbox.Open(boss.Data.swingActive);
                        GameEvents.RaiseSound("slam");
                        GameEvents.RaiseCameraShake(0.25f, 0.15f);
                        GameEvents.RaiseEffect(EffectType.Dust, (Vector2)boss.transform.position + new Vector2(Motor.Facing * 1.5f, 0f), Vector2.up);
                        Begin(Step.SwingStrike, boss.Data.swingActive);
                    }
                    break;

                case Step.SwingStrike:
                    Motor.Stop();
                    if (Time.time >= stepEndsAt) Begin(Step.Recover, Recovery);
                    break;

                case Step.ChargeTelegraph:
                    Motor.Stop();
                    boss.Telegraph = Progress(boss.Data.chargeTelegraph);
                    if (Time.time >= stepEndsAt)
                    {
                        boss.Telegraph = -1f;
                        boss.Charging = true;
                        boss.ChargeHitbox.Open(boss.Data.chargeDuration);
                        Begin(Step.Charging, boss.Data.chargeDuration);
                    }
                    break;

                case Step.Charging:
                    bool atEdge = Motor.Facing > 0 ? boss.transform.position.x >= boss.ArenaMaxX - 1.2f : boss.transform.position.x <= boss.ArenaMinX + 1.2f;
                    if (Motor.WallAhead || atEdge || Time.time >= stepEndsAt)
                    {
                        boss.Charging = false;
                        boss.ChargeHitbox.Close();
                        Motor.Stop();
                        GameEvents.RaiseCameraShake(0.3f, 0.2f);
                        nextChargeAt = Time.time + boss.Data.chargeInterval;
                        Begin(Step.Recover, Recovery);
                    }
                    else Motor.Move(Motor.Facing, boss.Data.chargeSpeed);
                    break;

                case Step.Recover:
                    Motor.Stop();
                    if (Time.time >= stepEndsAt) step = Step.Approach;
                    break;
            }
        }

        void Begin(Step next, float duration)
        {
            step = next;
            stepEndsAt = Time.time + duration;
        }

        float Progress(float duration) => 1f - Mathf.Clamp01((stepEndsAt - Time.time) / Mathf.Max(0.01f, duration));

        public override void Exit()
        {
            boss.Telegraph = -1f;
            boss.Charging = false;
            boss.SwingHitbox?.Close();
            boss.ChargeHitbox?.Close();
        }
    }
}
