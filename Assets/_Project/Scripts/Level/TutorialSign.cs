using AshenRoad.Core;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>A signpost that shows a one-line hint on the HUD the first time the knight passes it.</summary>
    public class TutorialSign : MonoBehaviour
    {
        [SerializeField] [TextArea] string text;

        bool shown;

        public string Text { get => text; set => text = value; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (shown || string.IsNullOrEmpty(text)) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            shown = true;
            GameEvents.RaiseHint(text);
        }
    }
}
