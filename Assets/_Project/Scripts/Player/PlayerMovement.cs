using AshenRoad.Core;
using AshenRoad.Data;
using AshenRoad.Level;
using UnityEngine;

namespace AshenRoad.Player
{
    /// <summary>
    /// The knight's physics: momentum, jump from height and time, coyote time, roll and knockback.
    /// States decide WHEN to call these; this class only knows HOW (Lecture 12: act in FixedUpdate).
    /// Gravity is applied here instead of by Rigidbody2D so the jump matches its designed height exactly.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] Collider2D body;
        [SerializeField] float groundProbe = 0.08f;

        PlayerStats stats;
        Rigidbody2D rb;
        float ownVelocityX;
        float lastGroundedTime = float.NegativeInfinity;
        float jumpedAt = float.NegativeInfinity;
        float rollEndsAt;
        float rollCooldownUntil;
        bool jumpCut;
        MovingPlatform platform;
        readonly RaycastHit2D[] hits = new RaycastHit2D[4];

        public bool IsGrounded { get; private set; }
        public bool IsRolling { get; private set; }
        public int Facing { get; private set; } = 1;
        public bool FacingLocked { get; set; }
        public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
        public bool IsRising => Velocity.y > 0.01f;
        public float RollCooldownRemaining => Mathf.Max(0f, rollCooldownUntil - Time.time);
        public float RollProgress => IsRolling ? 1f - Mathf.Clamp01((rollEndsAt - Time.time) / stats.rollDuration) : 0f;
        public PlayerStats Stats => stats;
        public bool JustLanded { get; private set; }

        public void Init(PlayerStats playerStats)
        {
            stats = playerStats;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (body == null) body = GetComponent<Collider2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // ------------------------------------------------------------ ground

        public void UpdateGround()
        {
            bool wasGrounded = IsGrounded;
            IsGrounded = false;
            platform = null;
            JustLanded = false;
            if (body == null || !body.enabled) return;

            // Just after take-off the probe still touches the floor; ignore it while rising.
            if (Velocity.y > 0.5f && Time.time - jumpedAt < 0.2f) return;

            Bounds b = body.bounds;
            Vector2 origin = new Vector2(b.center.x, b.min.y + 0.05f);
            Vector2 size = new Vector2(b.size.x * 0.85f, 0.05f);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.GroundMask, useTriggers = false };
            int count = Physics2D.BoxCast(origin, size, 0f, Vector2.down, filter, hits, groundProbe + 0.05f);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].normal.y < 0.6f) continue;
                if (Velocity.y > 0.5f) continue; // passing up through a one-way platform
                IsGrounded = true;
                platform = hits[i].collider.GetComponentInParent<MovingPlatform>();
                break;
            }
            if (IsGrounded)
            {
                lastGroundedTime = Time.time;
                jumpCut = false;
                if (!wasGrounded)
                {
                    JustLanded = true;
                    GameEvents.RaisePlayerActed(PlayerAction.Land);
                    GameEvents.RaiseEffect(EffectType.Dust, new Vector2(b.center.x, b.min.y), Vector2.up);
                }
            }
        }

        // ------------------------------------------------------------ horizontal

        public void ApplyHorizontal(float input, float dt, float speedMultiplier = 1f)
        {
            if (!FacingLocked && Mathf.Abs(input) > 0.1f) Facing = input > 0f ? 1 : -1;

            float target = Mathf.Clamp(input, -1f, 1f) * stats.maxSpeed * speedMultiplier;
            bool speedingUp = Mathf.Abs(target) > 0.01f && (Mathf.Sign(target) == Mathf.Sign(ownVelocityX) || Mathf.Abs(ownVelocityX) < 0.01f);
            float rate = speedingUp ? stats.acceleration : stats.deceleration;
            if (!speedingUp && Mathf.Abs(target) > 0.01f) rate = stats.acceleration + stats.deceleration; // turning around
            if (!IsGrounded) rate *= stats.airControl;
            ownVelocityX = Mathf.MoveTowards(ownVelocityX, target, rate * dt);
            Commit();
        }

        /// <summary>Slows horizontal motion to zero, as used while hurt or dead.</summary>
        public void Brake(float dt, float rate)
        {
            ownVelocityX = Mathf.MoveTowards(ownVelocityX, 0f, rate * dt);
            Commit();
        }

        public void SetFacing(int direction)
        {
            if (direction != 0) Facing = direction > 0 ? 1 : -1;
        }

        // ------------------------------------------------------------ vertical

        public void ApplyGravity(float dt)
        {
            Vector2 v = rb.linearVelocity;
            if (IsGrounded && v.y <= 0f)
            {
                v.y = -1f; // keeps contact on slopes and moving platforms
            }
            else
            {
                float g = v.y > 0f ? stats.Gravity : stats.FallGravity;
                v.y = Mathf.Max(v.y - g * dt, -stats.maxFallSpeed);
            }
            rb.linearVelocity = v;
        }

        public bool CanJump => IsGrounded || Time.time - lastGroundedTime <= stats.coyoteTime;

        public void Jump()
        {
            Vector2 v = rb.linearVelocity;
            v.y = stats.JumpVelocity;
            rb.linearVelocity = v;
            jumpedAt = Time.time;
            lastGroundedTime = float.NegativeInfinity; // coyote time is spent
            IsGrounded = false;
            jumpCut = false;
            GameEvents.RaisePlayerActed(PlayerAction.Jump);
            Bounds b = body.bounds;
            GameEvents.RaiseEffect(EffectType.Dust, new Vector2(b.center.x, b.min.y), Vector2.up);
        }

        /// <summary>Variable jump height: releasing the button while rising cuts the upward speed once.</summary>
        public void CutJump()
        {
            if (jumpCut || !IsRising) return;
            Vector2 v = rb.linearVelocity;
            v.y *= stats.jumpCutMultiplier;
            rb.linearVelocity = v;
            jumpCut = true;
        }

        // ------------------------------------------------------------ roll

        public bool CanRoll => !IsRolling && Time.time >= rollCooldownUntil;

        public void StartRoll(int direction)
        {
            SetFacing(direction);
            IsRolling = true;
            rollEndsAt = Time.time + stats.rollDuration;
            gameObject.layer = Layers.PlayerRolling;
            ownVelocityX = Facing * stats.rollSpeed;
            Commit();
            GameEvents.RaisePlayerActed(PlayerAction.Roll);
            Bounds b = body.bounds;
            GameEvents.RaiseEffect(EffectType.Dust, new Vector2(b.center.x, b.min.y), new Vector2(-Facing, 0.3f));
        }

        public bool RollFinished => Time.time >= rollEndsAt;

        public void RollTick()
        {
            ownVelocityX = Facing * stats.rollSpeed;
            Commit();
        }

        public void EndRoll()
        {
            if (!IsRolling) return;
            IsRolling = false;
            gameObject.layer = Layers.Player;
            rollCooldownUntil = Time.time + stats.rollCooldown;
            ownVelocityX = Mathf.Clamp(ownVelocityX, -stats.maxSpeed, stats.maxSpeed);
            Commit();
        }

        // ------------------------------------------------------------ forces

        public void Knockback(Vector2 direction, Vector2 strength)
        {
            float dirX = Mathf.Abs(direction.x) < 0.01f ? -Facing : Mathf.Sign(direction.x);
            ownVelocityX = dirX * strength.x;
            Vector2 v = rb.linearVelocity;
            v.x = ownVelocityX;
            v.y = Mathf.Max(v.y, strength.y);
            rb.linearVelocity = v;
            IsGrounded = false;
            jumpedAt = Time.time;
        }

        public void AddLunge(float speed)
        {
            if (!IsGrounded) return;
            ownVelocityX = Facing * Mathf.Max(Mathf.Abs(ownVelocityX), speed);
            Commit();
        }

        public void StopAll()
        {
            ownVelocityX = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        public void Teleport(Vector2 position)
        {
            rb.position = position;
            transform.position = position;
            StopAll();
        }

        void Commit()
        {
            Vector2 v = rb.linearVelocity;
            v.x = ownVelocityX + (IsGrounded && platform != null ? platform.Velocity.x : 0f);
            rb.linearVelocity = v;
        }

        /// <summary>Vertical platform support: ride the platform when standing on it.</summary>
        public void ApplyPlatformCarry()
        {
            if (!IsGrounded || platform == null) return;
            Vector2 v = rb.linearVelocity;
            float py = platform.Velocity.y;
            if (py < 0f) v.y = py - 1f;
            else if (py > 0f) v.y = Mathf.Max(v.y, py);
            rb.linearVelocity = v;
        }
    }
}
