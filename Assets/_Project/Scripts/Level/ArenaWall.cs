using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>A barrier that rises to lock the boss arena and sinks when the boss falls.</summary>
    public class ArenaWall : MonoBehaviour
    {
        [SerializeField] Collider2D blocker;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] float wallHeight = 6f;

        float shown;
        bool closed;

        public bool IsClosed => closed;

        void Awake()
        {
            SetVisual(0f);
            if (blocker != null) blocker.enabled = false;
        }

        public void Close()
        {
            closed = true;
            if (blocker != null) blocker.enabled = true;
        }

        public void Open()
        {
            closed = false;
            if (blocker != null) blocker.enabled = false;
        }

        void Update()
        {
            shown = Mathf.MoveTowards(shown, closed ? 1f : 0f, Time.deltaTime * 3f);
            SetVisual(shown);
        }

        void SetVisual(float amount)
        {
            if (visual == null) return;
            visual.enabled = amount > 0.01f;
            visual.drawMode = SpriteDrawMode.Tiled;
            visual.size = new Vector2(1f, Mathf.Max(0.01f, wallHeight * amount));
            visual.transform.localPosition = new Vector3(0f, visual.size.y * 0.5f, 0f);
        }
    }
}
