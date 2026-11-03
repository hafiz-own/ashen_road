using UnityEngine;

namespace AshenRoad.Data
{
    public enum DisasterType
    {
        Fire,
        Collapse,
        Flood,
        Fissure,
        CaveIn,
        Lightning,
        FireAndDebris,
        Crumble
    }

    /// <summary>
    /// One level: its layout grid, look, music and disaster-front tuning.
    /// The layout is text, one character per 1x1 tile (legend in LevelLayout).
    /// </summary>
    [CreateAssetMenu(menuName = "Ashen Road/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        public int index;
        public string displayName = "Burning Village";
        public string subtitle = "";
        public bool isBossLevel;

        [Header("Layout")]
        [TextArea(8, 40)] public string layout = "";
        public float movingPlatformRange = 4f;
        public float movingPlatformSpeed = 2f;
        [TextArea(1, 3)] public string[] hints = new string[0];

        [Header("Look")]
        public Sprite topTile;
        public Sprite fillTile;
        public Sprite platformTile;
        [Tooltip("Optional variations, picked per cell so the ground doesn't look stamped.")]
        public Sprite[] topTiles = new Sprite[0];
        public Sprite[] fillTiles = new Sprite[0];
        [Tooltip("Used two or more tiles below the surface.")]
        public Sprite[] deepTiles = new Sprite[0];
        public Sprite sky;
        public Sprite farLayer;
        public Sprite nearLayer;
        public Color skyTint = Color.white;
        public Color farTint = Color.white;
        public Color nearTint = Color.white;
        public Color ambient = Color.white;

        [Header("Scenery")]
        [Tooltip("Small props for 'd' cells: barrels, stones, plants.")]
        public Sprite[] decorations = new Sprite[0];
        [Tooltip("Big props placed behind the road every few tiles: houses, trees, ruins.")]
        public Sprite[] backdropProps = new Sprite[0];
        public float backdropSpacing = 14f;
        public Color backdropTint = new Color(0.75f, 0.75f, 0.8f, 1f);

        [Header("Disaster front")]
        public DisasterType disaster = DisasterType.Fire;
        public string frontName = "wall of fire";
        public Sprite[] frontFrames = new Sprite[0];
        public Color frontGlow = new Color(1f, 0.5f, 0.1f, 0.35f);
        public float frontStartOffset = 14f;
        public float respawnFrontOffset = 14f;
        public float frontBaseSpeed = 2.4f;
        public float frontRamp = 0.01f;
        public float frontMaxSpeed = 4f;
        public float frontCatchupDistance = 24f;
        public float frontCatchupBonus = 2.5f;

        [Header("Hazards")]
        public float fireCycleOn = 1.6f;
        public float fireCycleOff = 1.8f;
        public bool ambientDebris;
        public float ambientDebrisInterval = 3.5f;

        [Header("Audio")]
        public string musicId = "level_a";

        [Header("Scoring")]
        public float parTime = 180f;
    }
}
