using AshenRoad.Core;
using NUnit.Framework;

namespace AshenRoad.Tests
{
    public class GameStateRulesTests
    {
        [TestCase(GameState.Boot, GameState.MainMenu)]
        [TestCase(GameState.Boot, GameState.Loading)]
        [TestCase(GameState.MainMenu, GameState.Loading)]
        [TestCase(GameState.Loading, GameState.Playing)]
        [TestCase(GameState.Loading, GameState.MainMenu)]
        [TestCase(GameState.Playing, GameState.Paused)]
        [TestCase(GameState.Playing, GameState.GameOver)]
        [TestCase(GameState.Playing, GameState.LevelComplete)]
        [TestCase(GameState.Playing, GameState.Ending)]
        [TestCase(GameState.Paused, GameState.Playing)]
        [TestCase(GameState.Paused, GameState.Loading)]
        [TestCase(GameState.GameOver, GameState.Loading)]
        [TestCase(GameState.LevelComplete, GameState.Loading)]
        [TestCase(GameState.Ending, GameState.GameFinished)]
        [TestCase(GameState.GameFinished, GameState.Loading)]
        public void AllowedTransitions(GameState from, GameState to)
        {
            Assert.IsTrue(GameStateRules.CanTransition(from, to));
        }

        [TestCase(GameState.MainMenu, GameState.Playing)]
        [TestCase(GameState.GameOver, GameState.Playing)]
        [TestCase(GameState.Paused, GameState.GameOver)]
        [TestCase(GameState.LevelComplete, GameState.Playing)]
        [TestCase(GameState.Ending, GameState.Paused)]
        [TestCase(GameState.Playing, GameState.Playing)]
        [TestCase(GameState.Boot, GameState.Playing)]
        public void IllegalTransitions(GameState from, GameState to)
        {
            Assert.IsFalse(GameStateRules.CanTransition(from, to));
        }

        [Test]
        public void EveryStateCanEventuallyReachTheMenu()
        {
            foreach (GameState s in System.Enum.GetValues(typeof(GameState)))
            {
                bool direct = GameStateRules.CanTransition(s, GameState.MainMenu);
                bool viaLoading = GameStateRules.CanTransition(s, GameState.Loading);
                bool viaFinish = GameStateRules.CanTransition(s, GameState.GameFinished);
                Assert.IsTrue(direct || viaLoading || viaFinish || s == GameState.MainMenu, s + " is a dead end");
            }
        }
    }
}
