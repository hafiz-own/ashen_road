using AshenRoad.Core;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>A banner the knight touches to set where the level restarts after a death.</summary>
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] SpriteRenderer flag;
        [SerializeField] Sprite offSprite;
        [SerializeField] Sprite onSprite;

        public int Index { get; private set; }
        public bool IsActive { get; private set; }
        public Vector2 SpawnPoint => (Vector2)transform.position + new Vector2(0.6f, 0.05f);

        public void Setup(int index, bool alreadyActive)
        {
            Index = index;
            IsActive = alreadyActive;
            Refresh();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActive) return;
            PlayerController knight = other.GetComponentInParent<PlayerController>();
            if (knight == null || knight.IsDead) return;
            Activate();
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            Refresh();
            GameEvents.RaiseCheckpointActivated(Index);
            GameEvents.RaiseHint("Checkpoint");
            GameEvents.RaiseEffect(EffectType.Sparks, (Vector2)transform.position + Vector2.up * 1.5f, Vector2.up);
        }

        void Refresh()
        {
            if (flag != null) flag.sprite = IsActive ? onSprite : offSprite;
        }
    }
}
