using System;
using AshenRoad.Combat;
using AshenRoad.Core;
using AshenRoad.Data;
using AshenRoad.Enemies.States;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Enemies
{
    /// <summary>
    /// The brain of every regular enemy. Shambler, Runner and Brute are this same component configured by
    /// different EnemyData (Component + data-driven design); behaviour lives in its states (State pattern).
    /// </summary>
    [RequireComponent(typeof(Health), typeof(EnemyMotor), typeof(EnemyPerception))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] Hitbox attackHitbox;
        [SerializeField] ContactDamage contactDamage;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] BoxCollider2D body;

        public EnemyData Data { get; private set; }
        public Health Health { get; private set; }
        public EnemyMotor Motor { get; private set; }
        public EnemyPerception Perception { get; private set; }
        public Hitbox AttackHitbox => attackHitbox;
        public SpriteRenderer Visual => visual;
        public StateMachine Machine { get; } = new StateMachine();
        public Vector2 SpawnAnchor { get; set; }
        public bool IsDead => Health == null || !Health.IsAlive;
        public bool IsSummoned { get; set; }

        public EnemyPatrolState Patrol { get; private set; }
        public EnemyChaseState Chase { get; private set; }
        public EnemyAttackState Attack { get; private set; }
        public EnemyStunnedState Stunned { get; private set; }
        public EnemyDeadState Dead { get; private set; }

        /// <summary>Raised when the enemy is removed from the level, so the factory can forget it.</summary>
        public event Action<EnemyAI> Removed;

        void Awake()
        {
            Health = GetComponent<Health>();
            Motor = GetComponent<EnemyMotor>();
            Perception = GetComponent<EnemyPerception>();
            if (body == null) body = GetComponent<BoxCollider2D>();
        }

        public void Init(EnemyData data, PlayerController target)
        {
            Data = data;
            name = data.displayName;
            SpawnAnchor = transform.position;

            Health.Configure(data.maxHealth, data.immunities, 0.1f);
            Motor.Configure(data.scale, data.knockbackResistance);
            Perception.Configure(target, data.sightRange, data.loseTargetTime, data.colliderSize.y * 0.6f);
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
            if (attackHitbox != null)
            {
                attackHitbox.SetOwner(gameObject);
                attackHitbox.Damage = data.attackDamage;
                attackHitbox.Type = DamageType.EnemyAttack;
                attackHitbox.Size = data.attackHitboxSize;
                attackHitbox.Offset = data.attackHitboxOffset;
                attackHitbox.Knockback = data.attackKnockback;
                attackHitbox.Targets = Layers.PlayerMask;
            }
            if (contactDamage != null)
            {
                contactDamage.Damage = data.contactDamage;
                contactDamage.Knockback = data.contactKnockback;
                contactDamage.Targets = Layers.PlayerMask;
            }

            Patrol = new EnemyPatrolState(this);
            Chase = new EnemyChaseState(this);
            Attack = new EnemyAttackState(this);
            Stunned = new EnemyStunnedState(this);
            Dead = new EnemyDeadState(this);
            Machine.ChangeState(Patrol);
        }

        void OnEnable()
        {
            Health.Damaged += OnDamaged;
            Health.Blocked += OnBlocked;
            Health.Died += OnDied;
        }

        void OnDisable()
        {
            Health.Damaged -= OnDamaged;
            Health.Blocked -= OnBlocked;
            Health.Died -= OnDied;
        }

        void Update()
        {
            if (Data == null) return;
            Machine.Tick(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (Data == null) return;
            if (!IsDead) Perception.Sense();
            Machine.FixedTick(Time.fixedDeltaTime);
        }

        public void SetContactDamage(bool on)
        {
            if (contactDamage != null) contactDamage.enabled = on;
        }

        void OnDamaged(DamageInfo info)
        {
            if (!Health.IsAlive) return;
            Motor.Knockback(info.KnockbackDirection, info.KnockbackForce);
            GameEvents.RaiseSound("enemy_hit");
            Machine.ChangeState(Stunned, true);
        }

        void OnBlocked(DamageInfo info)
        {
            // An immune hit (sword on a Brute): a small shove and a flinch, but no damage.
            Motor.Knockback(info.KnockbackDirection, info.KnockbackForce * 0.3f);
            if (Machine.Current != Attack) Machine.ChangeState(Stunned, true);
            Perception.Sense();
        }

        void OnDied(DamageInfo info)
        {
            Motor.Knockback(info.KnockbackDirection, Mathf.Max(3f, info.KnockbackForce));
            Machine.ChangeState(Dead);
            GameEvents.RaiseEnemyKilled(Data.displayName);
            GameEvents.RaiseSound(Data.deathSound);
            GameEvents.RaiseEffect(EffectType.Blood, (Vector2)transform.position + Vector2.up * 0.4f, info.KnockbackDirection);
        }

        public void Despawn()
        {
            Removed?.Invoke(this);
            Removed = null;
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            Removed?.Invoke(this);
        }
    }
}
