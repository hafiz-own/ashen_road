using AshenRoad.Core;
using UnityEngine;

namespace AshenRoad.Enemies
{
    /// <summary>Moves an enemy (or the boss) along the ground, turning at walls and ledges, and applies knockback.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMotor : MonoBehaviour
    {
        [SerializeField] Collider2D body;

        Rigidbody2D rb;
        float knockbackUntil;
        float baseScale = 1f;
        float knockbackResistance;

        public int Facing { get; private set; } = 1;
        public Vector2 Velocity => rb.linearVelocity;
        public bool InKnockback => Time.time < knockbackUntil;
        public Collider2D Body => body;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (body == null) body = GetComponent<Collider2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 3f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void Configure(float scale, float resistance)
        {
            baseScale = scale;
            knockbackResistance = Mathf.Clamp01(resistance);
            Face(Facing);
        }

        public void Face(int direction)
        {
            if (direction == 0) return;
            Facing = direction > 0 ? 1 : -1;
            transform.localScale = new Vector3(Facing * baseScale, baseScale, 1f);
        }

        public void Move(int direction, float speed)
        {
            if (InKnockback) return;
            Face(direction);
            rb.linearVelocity = new Vector2(Facing * speed, rb.linearVelocity.y);
        }

        public void Stop()
        {
            if (InKnockback) return;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        public void Knockback(Vector2 direction, float force)
        {
            float f = force * (1f - knockbackResistance);
            if (f <= 0.01f) return;
            float dirX = Mathf.Abs(direction.x) < 0.01f ? -Facing : Mathf.Sign(direction.x);
            rb.linearVelocity = new Vector2(dirX * f, Mathf.Max(rb.linearVelocity.y, f * 0.4f));
            knockbackUntil = Time.time + 0.18f;
        }

        public void Freeze()
        {
            rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, rb.linearVelocity.y));
        }

        public bool IsGrounded
        {
            get
            {
                Bounds b = body.bounds;
                return Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f, Layers.GroundMask).collider != null;
            }
        }

        public bool WallAhead
        {
            get
            {
                Bounds b = body.bounds;
                Vector2 origin = new Vector2(b.center.x, b.min.y + 0.25f);
                return Physics2D.Raycast(origin, new Vector2(Facing, 0f), b.extents.x + 0.15f, Layers.GroundMask).collider != null;
            }
        }

        public bool LedgeAhead
        {
            get
            {
                if (!IsGrounded) return false;
                Bounds b = body.bounds;
                Vector2 origin = new Vector2(b.center.x + Facing * (b.extents.x + 0.2f), b.min.y + 0.1f);
                return Physics2D.Raycast(origin, Vector2.down, 1.3f, Layers.GroundMask).collider == null;
            }
        }

        /// <summary>True when walking forward would hit a wall or step off a ledge.</summary>
        public bool BlockedAhead => WallAhead || LedgeAhead;
    }
}
