using System.Collections.Generic;
using AshenRoad.Boss.States;
using AshenRoad.Combat;
using AshenRoad.Core;
using AshenRoad.Data;
using AshenRoad.Enemies;
using AshenRoad.Level;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Boss
{
    /// <summary>
    /// The Ash Warden. Reuses the enemy building blocks (Health, EnemyMotor, EnemyPerception, Hitbox) and adds
    /// an arena, summons and two phases, each a state (State pattern).
    /// </summary>
    [RequireComponent(typeof(Health), typeof(EnemyMotor), typeof(EnemyPerception))]
    public class BossController : MonoBehaviour
    {
        [SerializeField] Hitbox swingHitbox;
        [SerializeField] Hitbox chargeHitbox;
        [SerializeField] ContactDamage contactDamage;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] BoxCollider2D body;

        readonly List<ArenaWall> walls = new List<ArenaWall>();
        EnemyFactory factory;
        bool lockedForTests;

        public BossData Data { get; private set; }
        public Health Health { get; private set; }
        public EnemyMotor Motor { get; private set; }
        public EnemyPerception Perception { get; private set; }
        public PlayerController Target { get; private set; }
        public StateMachine Machine { get; } = new StateMachine();
        public Hitbox SwingHitbox => swingHitbox;
        public Hitbox ChargeHitbox => chargeHitbox;
        public SpriteRenderer Visual => visual;
        public float ArenaMinX { get; private set; } = float.NegativeInfinity;
        public float ArenaMaxX { get; private set; } = float.PositiveInfinity;
        public bool FightStarted { get; private set; }
        public bool IsDefeated { get; private set; }
        public int Phase { get; private set; }
        /// <summary>0 to 1 while winding up an attack, -1 otherwise (for visuals).</summary>
        public float Telegraph { get; set; } = -1f;
        public bool Charging { get; set; }

        public BossIntroState Intro { get; private set; }
        public BossPhaseOneState PhaseOne { get; private set; }
        public BossPhaseTwoState PhaseTwo { get; private set; }
        public BossDefeatedState Defeated { get; private set; }

        void Awake()
        {
            Health = GetComponent<Health>();
            Motor = GetComponent<EnemyMotor>();
            Perception = GetComponent<EnemyPerception>();
            if (body == null) body = GetComponent<BoxCollider2D>();
        }

        public void Init(BossData data, PlayerController target, EnemyFactory enemyFactory)
        {
            Data = data;
            Target = target;
            factory = enemyFactory;
            name = data.displayName;

            Health.Configure(data.maxHealth, null, 0.15f);
            Motor.Configure(data.scale, 0.85f);
            Perception.Configure(target, 40f, 999f, data.colliderSize.y * 0.6f);
            if (body != null)
            {
                body.size = data.colliderSize;
                body.offset = new Vector2(0f, data.colliderSize.y * 0.5f);
            }
            if (visual != null)
            {
                if (data.sprite != null) visual.sprite = data.sprite;
                visual.color = data.tint;
            }
            ConfigureHitbox(swingHitbox, data.swingDamage, data.swingHitboxSize, data.swingHitboxOffset);
            ConfigureHitbox(chargeHitbox, data.swingDamage, new Vector2(1.2f, 1.1f), new Vector2(0.3f, 0.45f));
            if (contactDamage != null)
            {
                contactDamage.Damage = data.contactDamage;
                contactDamage.Knockback = data.knockback;
                contactDamage.Type = DamageType.Boss;
                contactDamage.Targets = Layers.PlayerMask;
            }

            Intro = new BossIntroState(this);
            PhaseOne = new BossPhaseOneState(this);
            PhaseTwo = new BossPhaseTwoState(this);
            Defeated = new BossDefeatedState(this);
            Motor.Face(-1);
        }

        void ConfigureHitbox(Hitbox box, int damage, Vector2 size, Vector2 offset)
        {
            if (box == null) return;
            box.SetOwner(gameObject);
            box.Damage = damage;
            box.Type = DamageType.Boss;
            box.Size = size;
            box.Offset = offset;
            box.Knockback = Data.knockback;
            box.Targets = Layers.PlayerMask;
        }

        public void SetArena(IEnumerable<ArenaWall> arenaWalls, float minX, float maxX)
        {
            walls.Clear();
            walls.AddRange(arenaWalls);
            ArenaMinX = minX;
            ArenaMaxX = maxX;
        }

        void OnEnable()
        {
            Health.Changed += OnHealthChanged;
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
        }

        void OnDisable()
        {
            Health.Changed -= OnHealthChanged;
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
        }

        void Update()
        {
            if (Data == null) return;
            if (!FightStarted && !lockedForTests && Target != null && !Target.IsDead && Target.Position.x > ArenaMinX + 2.5f)
                StartFight();
            Machine.Tick(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (Data == null) return;
            Perception.Sense();
            if (Machine.Current == null && !Motor.InKnockback) Motor.Stop();
            Machine.FixedTick(Time.fixedDeltaTime);
        }

        public void StartFight()
        {
            if (FightStarted) return;
            FightStarted = true;
            foreach (ArenaWall w in walls) w.Close();
            GameEvents.RaiseBossIntroStarted();
            GameEvents.RaiseBossHealthChanged(Health.Current, Health.Max);
            Machine.ChangeState(Intro);
        }

        /// <summary>Test hook: start straight in a phase without the intro.</summary>
        public void StartFightImmediately()
        {
            lockedForTests = true;
            FightStarted = true;
            EnterPhaseOne();
        }

        public void EnterPhaseOne()
        {
            Phase = 1;
            GameEvents.RaiseBossPhaseChanged(1);
            Machine.ChangeState(PhaseOne);
        }

        void EnterPhaseTwo()
        {
            Phase = 2;
            GameEvents.RaiseBossPhaseChanged(2);
            GameEvents.RaiseSound("boss_roar");
            GameEvents.RaiseCameraShake(0.4f, 0.6f);
            Machine.ChangeState(PhaseTwo);
        }

        public void ReleaseArena()
        {
            foreach (ArenaWall w in walls) w.Open();
        }

        public void Summon(int count)
        {
            if (factory == null || Data.summonType == null) return;
            for (int i = 0; i < count; i++)
            {
                if (factory.CountAlive(Data.summonType, true) >= Data.maxSummonsAlive) return;
                bool left = Random.value < 0.5f;
                float x = left ? ArenaMinX + 1.5f : ArenaMaxX - 1.5f;
                if (float.IsInfinity(x)) x = transform.position.x + (left ? -4f : 4f);
                factory.SpawnSummon(Data.summonType, new Vector2(x, transform.position.y + 0.5f));
            }
            GameEvents.RaiseSound("summon");
        }

        public void SetContactDamage(bool on)
        {
            if (contactDamage != null) contactDamage.enabled = on;
        }

        void OnHealthChanged(int current, int max) => GameEvents.RaiseBossHealthChanged(current, max);

        void OnDamaged(DamageInfo info)
        {
            if (!Health.IsAlive) return;
            GameEvents.RaiseSound("enemy_hit");
            if (Phase == 1 && Health.Current <= Mathf.CeilToInt(Health.Max * Data.phaseTwoThreshold))
                EnterPhaseTwo();
        }

        void OnDied(DamageInfo info)
        {
            if (IsDefeated) return;
            IsDefeated = true;
            Machine.ChangeState(Defeated);
        }
    }
}
