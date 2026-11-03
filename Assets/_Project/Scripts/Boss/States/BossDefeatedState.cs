using AshenRoad.Core;
using UnityEngine;

namespace AshenRoad.Boss.States
{
    /// <summary>Disables the boss's attacks, plays the defeat, opens the arena and announces the win.</summary>
    public class BossDefeatedState : BossState
    {
        float announceAt;
        bool announced;

        public BossDefeatedState(BossController boss) : base(boss) { }

        public override void Enter()
        {
            boss.SwingHitbox?.Close();
            boss.ChargeHitbox?.Close();
            boss.SetContactDamage(false);
            boss.gameObject.layer = Layers.Corpse;
            boss.Telegraph = -1f;
            announceAt = Time.time + 1.6f;
            GameEvents.RaiseSound("boss_roar");
            GameEvents.RaiseCameraShake(0.5f, 1.2f);
        }

        public override void FixedTick(float dt)
        {
            Motor.Freeze();
        }

        public override void Tick(float dt)
        {
            if (boss.Visual != null)
            {
                float t = Mathf.Clamp01(1f - (announceAt - Time.time) / 1.6f);
                Color c = boss.Visual.color;
                c.a = 1f - t;
                boss.Visual.color = c;
                if (Random.value < 0.3f) GameEvents.RaiseEffect(EffectType.Debris, (Vector2)boss.transform.position + Random.insideUnitCircle * 1.2f + Vector2.up, Vector2.up);
            }
            if (!announced && Time.time >= announceAt)
            {
                announced = true;
                boss.ReleaseArena();
                GameEvents.RaiseBossDefeated();
                if (boss.Visual != null) boss.Visual.enabled = false;
            }
        }
    }
}
