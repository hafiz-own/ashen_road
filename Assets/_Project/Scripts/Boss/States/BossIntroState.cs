using AshenRoad.Core;
using UnityEngine;

namespace AshenRoad.Boss.States
{
    /// <summary>Arena locked, the boss stands still and roars, then the fight begins.</summary>
    public class BossIntroState : BossState
    {
        float endsAt;

        public BossIntroState(BossController boss) : base(boss) { }

        public override void Enter()
        {
            endsAt = Time.time + boss.Data.introDuration;
            boss.SetContactDamage(false);
            GameEvents.RaiseSound("boss_roar");
            GameEvents.RaiseCameraShake(0.35f, boss.Data.introDuration * 0.6f);
            GameEvents.RaiseHint(boss.Data.displayName.ToUpperInvariant());
        }

        public override void FixedTick(float dt)
        {
            Motor.Stop();
            Motor.Face(boss.Perception.DirectionToTarget);
            if (Time.time >= endsAt) boss.EnterPhaseOne();
        }

        public override void Exit()
        {
            boss.SetContactDamage(true);
        }
    }
}
