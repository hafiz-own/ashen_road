using System.Collections.Generic;

namespace AshenRoad.Core
{
    public enum GameState
    {
        Boot,
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver,
        LevelComplete,
        Ending,
        GameFinished
    }

    /// <summary>The table of legal game-state transitions. GameManager refuses anything not listed here.</summary>
    public static class GameStateRules
    {
        static readonly Dictionary<GameState, GameState[]> Allowed = new Dictionary<GameState, GameState[]>
        {
            { GameState.Boot,          new[] { GameState.MainMenu, GameState.Loading } },
            { GameState.MainMenu,      new[] { GameState.Loading } },
            { GameState.Loading,       new[] { GameState.Playing, GameState.MainMenu } },
            { GameState.Playing,       new[] { GameState.Paused, GameState.GameOver, GameState.LevelComplete, GameState.Ending, GameState.Loading } },
            { GameState.Paused,        new[] { GameState.Playing, GameState.Loading } },
            { GameState.GameOver,      new[] { GameState.Loading } },
            { GameState.LevelComplete, new[] { GameState.Loading } },
            { GameState.Ending,        new[] { GameState.GameFinished } },
            { GameState.GameFinished,  new[] { GameState.Loading } },
        };

        public static bool CanTransition(GameState from, GameState to)
        {
            if (!Allowed.TryGetValue(from, out GameState[] targets)) return false;
            foreach (GameState t in targets)
                if (t == to) return true;
            return false;
        }

        /// <summary>States where the knight is under the player's control.</summary>
        public static bool IsGameplay(GameState state) => state == GameState.Playing;
    }
}
