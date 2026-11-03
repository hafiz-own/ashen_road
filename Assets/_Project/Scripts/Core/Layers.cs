using UnityEngine;

namespace AshenRoad.Core
{
    /// <summary>Physics layer numbers and masks used across the game (set up by the project builder).</summary>
    public static class Layers
    {
        public const int Ground = 6;
        public const int Player = 7;
        public const int Enemy = 8;
        public const int Trigger = 9;
        public const int Corpse = 10;
        public const int PlayerRolling = 11;

        public static readonly int GroundMask = 1 << Ground;
        public static readonly int PlayerMask = (1 << Player) | (1 << PlayerRolling);
        public static readonly int EnemyMask = 1 << Enemy;

        public static readonly string[] Names =
        {
            "Ground", "Player", "Enemy", "Trigger", "Corpse", "PlayerRolling"
        };

        public static readonly int[] Numbers = { Ground, Player, Enemy, Trigger, Corpse, PlayerRolling };

        /// <summary>The single source of truth for which layers physically collide (Lecture 11).</summary>
        public static bool ShouldCollide(int a, int b)
        {
            if (a > b) { int t = a; a = b; b = t; }
            switch (a)
            {
                case Ground:
                    return b == Ground || b == Player || b == Enemy || b == Corpse || b == PlayerRolling;
                case Player:
                    return b == Trigger;
                case Enemy:
                    return b == Trigger;
                case Trigger:
                    return b == PlayerRolling;
                default:
                    return false;
            }
        }
    }
}
