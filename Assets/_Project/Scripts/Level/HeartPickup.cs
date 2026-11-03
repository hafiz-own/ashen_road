using AshenRoad.Core;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>Restores one heart if the knight is hurt; otherwise it stays for later.</summary>
    public class HeartPickup : MonoBehaviour
    {
        [SerializeField] Transform visual;
        [SerializeField] int amount = 1;

        Vector3 basePosition;

        void Start()
        {
            if (visual != null) basePosition = visual.localPosition;
        }

        void Update()
        {
            if (visual != null)
                visual.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f + transform.position.x) * 0.12f);
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

        void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        void TryCollect(Collider2D other)
        {
            PlayerController knight = other.GetComponentInParent<PlayerController>();
            if (knight == null || knight.IsDead) return;
            if (knight.Health.Current >= knight.Health.Max) return;
            knight.Health.Heal(amount);
            GameEvents.RaiseHeartPickedUp();
            GameEvents.RaiseEffect(EffectType.Sparks, transform.position + Vector3.up * 0.5f, Vector2.up);
            Destroy(gameObject);
        }
    }
}
