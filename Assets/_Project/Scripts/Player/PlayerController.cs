using AshenRoad.Combat;
using AshenRoad.Core;
using AshenRoad.Data;
using AshenRoad.Input;
using AshenRoad.Player.States;
using UnityEngine;

namespace AshenRoad.Player
{
    /// <summary>
    /// Facade for the knight: wires its components, owns its state machine and remembers buffered input.
    /// Everything outside the knight (level, enemies, camera, UI) talks to this one class.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerCombat), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        const float ActionBuffer = 0.12f;

        [SerializeField] PlayerStats stats;

        InputReader input;
        float jumpPressedAt = float.NegativeInfinity;
        float rollPressedAt = float.NegativeInfinity;
        float attackPressedAt = float.NegativeInfinity;
        float scriptedMove;
        bool scriptedJumpHeld;
        bool initialised;

        public PlayerMovement Movement { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public Health Health { get; private set; }
        public StateMachine Machine { get; } = new StateMachine();
        public PlayerStats Stats => stats;

        public PlayerGroundedState Grounded { get; private set; }
        public PlayerAirborneState Airborne { get; private set; }
        public PlayerRollingState Rolling { get; private set; }
        public PlayerAttackingState Attacking { get; private set; }
        public PlayerHurtState Hurt { get; private set; }
        public PlayerDeadState Dead { get; private set; }

        /// <summary>When locked, the knight ignores the player's input (cut-scenes, death, tests driving it).</summary>
        public bool InputLocked { get; set; }
        public float MoveInput => InputLocked ? scriptedMove : (input != null ? input.MoveAxis : scriptedMove);
        public bool JumpHeld => InputLocked ? scriptedJumpHeld : (input != null ? input.JumpHeld : scriptedJumpHeld);
        public bool IsDead => Health != null && !Health.IsAlive;
        public int Facing => Movement.Facing;
        public Vector2 Position => transform.position;
        public Vector2 Center => (Vector2)transform.position + Vector2.up * 0.45f;
        public DamageInfo LastDamage { get; private set; }

        void Awake()
        {
            Movement = GetComponent<PlayerMovement>();
            Combat = GetComponent<PlayerCombat>();
            Health = GetComponent<Health>();
            if (stats != null) Initialise(stats);
        }

        public void Initialise(PlayerStats playerStats)
        {
            stats = playerStats;
            Movement.Init(stats);
            Combat.Init(stats);
            Health.Configure(stats.maxHearts, null, stats.hurtInvulnerability);

            Grounded = new PlayerGroundedState(this);
            Airborne = new PlayerAirborneState(this);
            Rolling = new PlayerRollingState(this);
            Attacking = new PlayerAttackingState(this);
            Hurt = new PlayerHurtState(this);
            Dead = new PlayerDeadState(this);
            if (!initialised) Machine.ChangeState(Airborne);
            initialised = true;
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
            UnbindInput();
        }

        void Start()
        {
            GameEvents.RaisePlayerHealthChanged(Health.Current, Health.Max);
        }

        // ------------------------------------------------------------ input

        public void BindInput(InputReader reader)
        {
            UnbindInput();
            input = reader;
            if (input == null) return;
            input.JumpPressed += PressJump;
            input.JumpReleased += ReleaseJump;
            input.RollPressed += PressRoll;
            input.AttackPressed += PressAttack;
        }

        void UnbindInput()
        {
            if (input == null) return;
            input.JumpPressed -= PressJump;
            input.JumpReleased -= ReleaseJump;
            input.RollPressed -= PressRoll;
            input.AttackPressed -= PressAttack;
            input = null;
        }

        // These run in Update time (input callbacks) and are latched for the next physics step (Lecture 12).
        public void PressJump()
        {
            if (InputLocked && input != null) return;
            jumpPressedAt = Time.time;
            scriptedJumpHeld = true;
        }

        public void ReleaseJump()
        {
            scriptedJumpHeld = false;
            if (!InputLocked || input == null) Movement.CutJump();
        }

        public void PressRoll()
        {
            if (InputLocked && input != null) return;
            rollPressedAt = Time.time;
        }

        public void PressAttack()
        {
            if (InputLocked && input != null) return;
            attackPressedAt = Time.time;
        }

        /// <summary>Drives the knight from code (ending walk, tests).</summary>
        public void SetScriptedMove(float axis) => scriptedMove = Mathf.Clamp(axis, -1f, 1f);

        public bool HasBufferedJump => Time.time - jumpPressedAt <= stats.jumpBuffer;
        public bool HasBufferedRoll => Time.time - rollPressedAt <= ActionBuffer;
        public bool HasBufferedAttack => Time.time - attackPressedAt <= ActionBuffer;

        public void ConsumeJump() => jumpPressedAt = float.NegativeInfinity;
        public void ConsumeRoll() => rollPressedAt = float.NegativeInfinity;
        public void ConsumeAttack() => attackPressedAt = float.NegativeInfinity;

        // ------------------------------------------------------------ loop

        void Update()
        {
            Machine.Tick(Time.deltaTime);
            Combat.Tick();
            transform.localScale = new Vector3(Movement.Facing, 1f, 1f);
        }

        void FixedUpdate()
        {
            if (!IsDead) Movement.UpdateGround();
            Machine.FixedTick(Time.fixedDeltaTime);
        }

        /// <summary>Picks the right "free" state for the current situation after an action ends.</summary>
        public void ReturnToFreeState()
        {
            Machine.ChangeState(Movement.IsGrounded ? (Core.IState)Grounded : Airborne);
        }

        public void Teleport(Vector2 position)
        {
            Movement.Teleport(position);
            ReturnToFreeState();
        }

        // ------------------------------------------------------------ health

        void OnHealthChanged(int current, int max) => GameEvents.RaisePlayerHealthChanged(current, max);

        void OnDamaged(DamageInfo info)
        {
            LastDamage = info;
            if (!Health.IsAlive) return;
            GameEvents.RaisePlayerDamaged();
            GameEvents.RaiseCameraShake(0.25f, 0.2f);
            Hurt.Setup(info);
            Machine.ChangeState(Hurt, true);
        }

        void OnDied(DamageInfo info)
        {
            LastDamage = info;
            Machine.ChangeState(Dead);
            GameEvents.RaiseCameraShake(0.35f, 0.3f);
            GameEvents.RaisePlayerDied(CauseOf(info.Type));
        }

        public static DeathCause CauseOf(DamageType type)
        {
            switch (type)
            {
                case DamageType.Fall: return DeathCause.Fall;
                case DamageType.Front: return DeathCause.Front;
                case DamageType.Hazard: return DeathCause.Hazard;
                case DamageType.Boss: return DeathCause.Boss;
                default: return DeathCause.Enemy;
            }
        }
    }
}
