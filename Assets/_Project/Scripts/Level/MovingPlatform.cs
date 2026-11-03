using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>A kinematic platform that travels back and forth; the knight rides it (see PlayerMovement).</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] Vector2 travel = new Vector2(4f, 0f);
        [SerializeField] float speed = 2f;
        [SerializeField] float pauseAtEnds = 0.4f;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] BoxCollider2D surface;

        Rigidbody2D rb;
        Vector2 start;
        float t;
        int direction = 1;
        float pausedUntil;

        public Vector2 Velocity { get; private set; }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            start = rb.position;
        }

        public void Setup(Vector2 travelVector, float moveSpeed)
        {
            travel = travelVector;
            speed = moveSpeed;
            start = transform.position;
            if (rb != null) rb.position = start;
        }

        /// <summary>Makes the platform the given number of tiles wide (left edge stays at the transform).</summary>
        public void SetWidth(int tiles)
        {
            tiles = Mathf.Max(1, tiles);
            if (visual != null)
            {
                visual.drawMode = SpriteDrawMode.Tiled;
                visual.size = new Vector2(tiles, 1f);
                visual.transform.localPosition = new Vector3(tiles * 0.5f, 0.5f, 0f);
            }
            if (surface != null)
            {
                surface.size = new Vector2(tiles, 0.4f);
                surface.offset = new Vector2(tiles * 0.5f, 0.8f);
            }
        }

        void FixedUpdate()
        {
            float length = travel.magnitude;
            if (length < 0.01f) return;
            if (Time.time < pausedUntil)
            {
                Velocity = Vector2.zero;
                return;
            }
            t += direction * speed * Time.fixedDeltaTime / length;
            if (t >= 1f || t <= 0f)
            {
                t = Mathf.Clamp01(t);
                direction = -direction;
                pausedUntil = Time.time + pauseAtEnds;
            }
            Vector2 next = start + travel * t;
            Velocity = (next - rb.position) / Time.fixedDeltaTime;
            rb.MovePosition(next);
        }
    }
}
