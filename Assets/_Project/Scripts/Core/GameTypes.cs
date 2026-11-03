using UnityEngine;

namespace AshenRoad.Core
{
    public enum DeathCause
    {
        None,
        Enemy,
        Hazard,
        Fall,
        Front,
        Boss
    }

    public enum PlayerAction
    {
        Jump,
        Land,
        Roll,
        Swing,
        Footstep
    }

    public enum EffectType
    {
        Dust,
        Sparks,
        Slash,
        Blood,
        Debris
    }

    /// <summary>What a finished level reports to the level-complete screen and the save file.</summary>
    public struct LevelResult
    {
        public int LevelIndex;
        public float Time;
        public int HeartsLeft;
        public int MaxHearts;
        public bool NewBestTime;
        public float BestTime;
        public int EnemiesKilled;

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) return "--:--";
            int total = Mathf.FloorToInt(seconds);
            int hundredths = Mathf.FloorToInt((seconds - total) * 100f);
            return $"{total / 60:00}:{total % 60:00}.{hundredths:00}";
        }
    }

    public static class DeathText
    {
        public static string For(DeathCause cause, string frontName)
        {
            switch (cause)
            {
                case DeathCause.Front: return "Caught by the " + frontName;
                case DeathCause.Fall: return "Fell into the depths";
                case DeathCause.Hazard: return "Lost to the ruins";
                case DeathCause.Boss: return "Struck down by the Ash Warden";
                case DeathCause.Enemy: return "Overrun by the undead";
                default: return "The knight has fallen";
            }
        }
    }
}
