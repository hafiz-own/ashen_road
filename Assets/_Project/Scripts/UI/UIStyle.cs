using UnityEngine;

namespace AshenRoad.UI
{
    /// <summary>Fonts, sprites and colours shared by every screen, so the look is changed in one asset.</summary>
    [CreateAssetMenu(menuName = "Ashen Road/UI Style")]
    public class UIStyle : ScriptableObject
    {
        public Font font;
        public Font titleFont;
        public Sprite panel;
        public Sprite button;
        public Sprite buttonHighlight;
        public Sprite heartFull;
        public Sprite heartEmpty;
        public Sprite white;
        public Color text = new Color(0.95f, 0.9f, 0.82f);
        public Color textDim = new Color(0.65f, 0.6f, 0.55f);
        public Color accent = new Color(1f, 0.62f, 0.25f);
        public Color danger = new Color(0.95f, 0.3f, 0.25f);
        public Color overlay = new Color(0.05f, 0.02f, 0.04f, 0.78f);
    }
}
