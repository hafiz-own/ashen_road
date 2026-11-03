using System.Collections.Generic;
using UnityEngine;

namespace AshenRoad.Core
{
    /// <summary>Where save data is stored. PlayerPrefs in the game, memory in tests.</summary>
    public interface ISaveStore
    {
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
        float GetFloat(string key, float fallback);
        void SetFloat(string key, float value);
        void DeleteAll();
        void Flush();
    }

    public class PlayerPrefsStore : ISaveStore
    {
        const string Prefix = "AshenRoad.";
        static readonly string[] KnownKeys = { "unlocked", "music", "sfx", "finished" };

        public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(Prefix + key, fallback);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(Prefix + key, value);
        public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(Prefix + key, fallback);
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(Prefix + key, value);
        public void Flush() => PlayerPrefs.Save();

        public void DeleteAll()
        {
            foreach (string k in KnownKeys) PlayerPrefs.DeleteKey(Prefix + k);
            for (int i = 0; i < 32; i++) PlayerPrefs.DeleteKey(Prefix + "best" + i);
            PlayerPrefs.Save();
        }
    }

    public class MemorySaveStore : ISaveStore
    {
        readonly Dictionary<string, float> values = new Dictionary<string, float>();

        public int GetInt(string key, int fallback) => values.TryGetValue(key, out float v) ? (int)v : fallback;
        public void SetInt(string key, int value) => values[key] = value;
        public float GetFloat(string key, float fallback) => values.TryGetValue(key, out float v) ? v : fallback;
        public void SetFloat(string key, float value) => values[key] = value;
        public void DeleteAll() => values.Clear();
        public void Flush() { }
    }

    /// <summary>Progress and settings: unlocked levels, best times, volumes, whether the game was finished.</summary>
    public class SaveSystem
    {
        readonly ISaveStore store;

        public SaveSystem(ISaveStore store)
        {
            this.store = store;
        }

        /// <summary>Highest level index the player may start (0 = only the first level).</summary>
        public int HighestUnlockedLevel => store.GetInt("unlocked", 0);

        public bool IsUnlocked(int levelIndex) => levelIndex <= HighestUnlockedLevel;

        public void UnlockLevel(int levelIndex)
        {
            if (levelIndex > HighestUnlockedLevel)
            {
                store.SetInt("unlocked", levelIndex);
                store.Flush();
            }
        }

        public float GetBestTime(int levelIndex) => store.GetFloat("best" + levelIndex, -1f);

        /// <summary>Stores the time if it beats the best. Returns true for a new best.</summary>
        public bool RecordTime(int levelIndex, float seconds)
        {
            float best = GetBestTime(levelIndex);
            if (best >= 0f && seconds >= best) return false;
            store.SetFloat("best" + levelIndex, seconds);
            store.Flush();
            return true;
        }

        public float MusicVolume
        {
            get => store.GetFloat("music", 0.7f);
            set { store.SetFloat("music", Mathf.Clamp01(value)); store.Flush(); }
        }

        public float SfxVolume
        {
            get => store.GetFloat("sfx", 0.8f);
            set { store.SetFloat("sfx", Mathf.Clamp01(value)); store.Flush(); }
        }

        public bool GameFinished
        {
            get => store.GetInt("finished", 0) == 1;
            set { store.SetInt("finished", value ? 1 : 0); store.Flush(); }
        }

        public void ResetProgress()
        {
            float music = MusicVolume, sfx = SfxVolume;
            store.DeleteAll();
            MusicVolume = music;
            SfxVolume = sfx;
        }
    }
}
