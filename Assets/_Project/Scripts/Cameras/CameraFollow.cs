using AshenRoad.Core;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Cameras
{
    /// <summary>
    /// Follows the knight with smoothing and look-ahead in the facing direction, clamped to the level bounds
    /// (Lecture 12), plus a screen shake that anything can request through GameEvents.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] float smoothTime = 0.18f;
        [SerializeField] float lookAhead = 2.8f;
        [SerializeField] float lookAheadSpeed = 3f;
        [SerializeField] float verticalOffset = 1.6f;
        [SerializeField] float verticalSmoothTime = 0.3f;

        UnityEngine.Camera cam;
        PlayerController target;
        Rect bounds = new Rect(-1000f, -1000f, 2000f, 2000f);
        float currentLookAhead;
        Vector2 velocity;
        float shakeAmplitude;
        float shakeUntil;
        float shakeDuration;

        public Vector2 Focus { get; private set; }

        void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
        }

        void OnEnable() => GameEvents.CameraShakeRequested += Shake;

        void OnDisable() => GameEvents.CameraShakeRequested -= Shake;

        public void SetTarget(PlayerController knight) => target = knight;

        public void SetBounds(Rect levelBounds) => bounds = levelBounds;

        public void SnapToTarget()
        {
            if (target == null) return;
            Focus = Clamp(target.Position + Vector2.up * verticalOffset);
            transform.position = new Vector3(Focus.x, Focus.y, -10f);
            velocity = Vector2.zero;
        }

        public void Shake(float amplitude, float duration)
        {
            if (amplitude < shakeAmplitude && Time.unscaledTime < shakeUntil) return;
            shakeAmplitude = amplitude;
            shakeDuration = Mathf.Max(0.01f, duration);
            shakeUntil = Time.unscaledTime + duration;
        }

        void LateUpdate()
        {
            if (target != null)
            {
                float desiredAhead = target.IsDead ? 0f : target.Facing * lookAhead * Mathf.Clamp01(Mathf.Abs(target.Movement.Velocity.x) / 3f + 0.3f);
                currentLookAhead = Mathf.MoveTowards(currentLookAhead, desiredAhead, lookAheadSpeed * Time.deltaTime);
                Vector2 goal = Clamp(target.Position + new Vector2(currentLookAhead, verticalOffset));
                float x = Mathf.SmoothDamp(Focus.x, goal.x, ref velocity.x, smoothTime);
                float y = Mathf.SmoothDamp(Focus.y, goal.y, ref velocity.y, verticalSmoothTime);
                Focus = Clamp(new Vector2(x, y));
            }

            Vector2 shake = Vector2.zero;
            if (Time.unscaledTime < shakeUntil)
            {
                float strength = shakeAmplitude * ((shakeUntil - Time.unscaledTime) / shakeDuration);
                shake = Random.insideUnitCircle * strength;
            }
            else shakeAmplitude = 0f;
            transform.position = new Vector3(Focus.x + shake.x, Focus.y + shake.y, -10f);
        }

        Vector2 Clamp(Vector2 p)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float minX = bounds.xMin + halfW, maxX = bounds.xMax - halfW;
            float minY = bounds.yMin + halfH, maxY = bounds.yMax - halfH;
            p.x = minX > maxX ? bounds.center.x : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? bounds.center.y : Mathf.Clamp(p.y, minY, maxY);
            return p;
        }
    }
}
