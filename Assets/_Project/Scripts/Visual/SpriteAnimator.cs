using AshenRoad.Data;
using UnityEngine;

namespace AshenRoad.Visual
{
    /// <summary>
    /// Plays frame-by-frame clips from a CharacterArt on a SpriteRenderer. The character's animator script
    /// decides which clip to play from its state; this class only advances frames.
    /// </summary>
    public class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [SerializeField] CharacterArt art;

        SpriteClip current;
        float time;

        public CharacterArt Art { get => art; set => art = value; }
        public string CurrentClip => current?.name;
        public bool HasArt => art != null;
        /// <summary>True once a non-looping clip has shown its last frame.</summary>
        public bool Finished => current != null && !current.loop && time >= current.Duration;

        void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
        }

        /// <summary>Starts a clip (no-op if it is already playing, unless restart). Returns false if the clip is missing.</summary>
        public bool Play(string clipName, bool restart = false)
        {
            if (art == null) return false;
            if (current != null && current.name == clipName && !restart) return true;
            SpriteClip clip = art.Get(clipName);
            if (clip == null) return false;
            current = clip;
            time = 0f;
            Show();
            return true;
        }

        /// <summary>Shows the frame at a 0..1 position through the clip (used to sync the sword swing to the hitbox).</summary>
        public void ShowNormalized(string clipName, float t)
        {
            if (!Play(clipName)) return;
            time = Mathf.Clamp01(t) * current.Duration * 0.999f;
            Show();
        }

        void Update()
        {
            if (current == null) return;
            time += Time.deltaTime;
            Show();
        }

        void Show()
        {
            if (current == null || target == null) return;
            int n = current.frames.Length;
            int frame = Mathf.FloorToInt(time * current.fps);
            frame = current.loop ? frame % n : Mathf.Min(frame, n - 1);
            target.sprite = current.frames[frame];
        }
    }
}
