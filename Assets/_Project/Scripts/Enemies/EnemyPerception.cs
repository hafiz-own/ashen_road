using AshenRoad.Core;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Enemies
{
    /// <summary>Senses the knight: range, a line-of-sight ray, and a short memory after losing sight.</summary>
    public class EnemyPerception : MonoBehaviour
    {
        [SerializeField] float sightRange = 5f;
        [SerializeField] float memory = 2f;
        [SerializeField] float rearFactor = 0.7f;
        [SerializeField] float verticalRange = 2.5f;
        [SerializeField] float eyeHeight = 0.5f;

        PlayerController target;
        EnemyMotor motor;
        float lastSeenTime = float.NegativeInfinity;

        public PlayerController Target => target;
        public bool CanSeeTarget { get; private set; }
        public bool HasTarget => target != null && !target.IsDead && (CanSeeTarget || Time.time - lastSeenTime <= memory);
        public Vector2 TargetPosition => target != null ? target.Center : (Vector2)transform.position;
        public float HorizontalDistance => target != null ? Mathf.Abs(TargetPosition.x - transform.position.x) : float.PositiveInfinity;
        public float VerticalOffset => target != null ? TargetPosition.y - (transform.position.y + eyeHeight) : 0f;
        public int DirectionToTarget => TargetPosition.x >= transform.position.x ? 1 : -1;

        void Awake()
        {
            motor = GetComponent<EnemyMotor>();
        }

        public void Configure(PlayerController knight, float range, float memorySeconds, float eye)
        {
            target = knight;
            sightRange = range;
            memory = memorySeconds;
            eyeHeight = eye;
        }

        public void Sense()
        {
            CanSeeTarget = false;
            if (target == null || target.IsDead) return;

            Vector2 eye = (Vector2)transform.position + Vector2.up * eyeHeight;
            Vector2 to = target.Center - eye;
            int facing = motor != null ? motor.Facing : 1;
            bool inFront = Mathf.Sign(to.x) == facing;
            float range = inFront ? sightRange : sightRange * rearFactor;
            if (Mathf.Abs(to.x) > range || Mathf.Abs(to.y) > verticalRange) return;
            if (Physics2D.Raycast(eye, to.normalized, to.magnitude, Layers.GroundMask).collider != null) return;

            CanSeeTarget = true;
            lastSeenTime = Time.time;
        }

        public void Forget() => lastSeenTime = float.NegativeInfinity;
    }
}
