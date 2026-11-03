using System;
using UnityEngine;

namespace AshenRoad.Data
{
    /// <summary>One named animation: its frames in order and how fast they play.</summary>
    [Serializable]
    public class SpriteClip
    {
        public string name;
        public Sprite[] frames = new Sprite[0];
        public float fps = 10f;
        public bool loop = true;

        public float Duration => frames.Length / Mathf.Max(0.01f, fps);
    }

    /// <summary>The animation set for a character (knight, an enemy type, the boss, the princess).</summary>
    [CreateAssetMenu(menuName = "Ashen Road/Character Art")]
    public class CharacterArt : ScriptableObject
    {
        public SpriteClip[] clips = new SpriteClip[0];

        public SpriteClip Get(string clipName)
        {
            foreach (SpriteClip c in clips)
                if (c.name == clipName && c.frames.Length > 0) return c;
            return null;
        }

        public bool Has(string clipName) => Get(clipName) != null;
    }
}
