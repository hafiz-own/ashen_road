using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenRoad.Data
{
    /// <summary>Every sound and music track, looked up by a string id so gameplay code never holds clips.</summary>
    [CreateAssetMenu(menuName = "Ashen Road/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class Sound
        {
            public string id;
            public AudioClip[] clips = new AudioClip[0];
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0f, 0.5f)] public float pitchVariance = 0.05f;
            public float minInterval = 0.03f;
        }

        [Serializable]
        public class Music
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.6f;
            public bool loop = true;
        }

        public List<Sound> sounds = new List<Sound>();
        public List<Music> music = new List<Music>();

        public Sound FindSound(string id)
        {
            foreach (Sound s in sounds)
                if (s.id == id) return s;
            return null;
        }

        public Music FindMusic(string id)
        {
            foreach (Music m in music)
                if (m.id == id) return m;
            return null;
        }
    }
}
