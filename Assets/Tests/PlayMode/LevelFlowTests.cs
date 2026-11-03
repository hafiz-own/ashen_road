using System.Collections;
using System.IO;
using AshenRoad.Boss;
using AshenRoad.Core;
using AshenRoad.Level;
using AshenRoad.Player;
using AshenRoad.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AshenRoad.Tests
{
    /// <summary>Drives the real scenes through GameManager: loading, finishing, dying, checkpoints, pause, boss and ending.</summary>
    [Timeout(180000)]
    public class LevelFlowTests
    {
        GameManager gm;
        MemorySaveStore store;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            gm = GameManager.Instance;
            Assert.IsNotNull(gm, "Systems prefab should be created automatically");
            store = new MemorySaveStore();
            gm.UseSaveStore(store);
            gm.SetupForTests(TestWorld.Catalog, 0);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            gm.SetupForTests(TestWorld.Catalog, 0);
            Scene empty = SceneManager.CreateScene("Empty" + Random.Range(0, 1_000_000));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s != empty && s.name != "DontDestroyOnLoad" && s.isLoaded && !s.name.StartsWith("InitTestScene"))
                    yield return SceneManager.UnloadSceneAsync(s);
            }
            yield return null;
        }

        IEnumerator WaitForState(GameState state, float timeout)
        {
            float until = Time.realtimeSinceStartup + timeout;
            while (gm.State != state && Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(state, gm.State, $"expected {state}");
        }

        IEnumerator StartLevel(int index)
        {
            gm.StartLevel(index);
            yield return WaitForState(GameState.Playing, 20f);
            yield return null;
        }

        static LevelController Level => Object.FindFirstObjectByType<LevelController>();

        /// <summary>
        /// Renders the main camera plus the UI into a PNG. WaitForEndOfFrame never fires in a headless editor,
        /// so the canvas is switched to camera space for one manual render instead.
        /// </summary>
        static IEnumerator Screenshot(string name)
        {
            yield return null;
            var cam = UnityEngine.Camera.main;
            if (cam == null) yield break;
            string dir = Path.Combine(Application.dataPath, "..", "TestResults", "Screens");
            Directory.CreateDirectory(dir);

            var canvas = Object.FindFirstObjectByType<UIFlowController>()?.GetComponent<Canvas>();
            RenderMode mode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
            }
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            if (canvas != null) canvas.renderMode = mode;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            Object.Destroy(rt);
        }

        [UnityTest]
        public IEnumerator MainMenu_ShowsTitleScreen()
        {
            gm.GoToMainMenu();
            yield return WaitForState(GameState.MainMenu, 15f);
            yield return new WaitForSecondsRealtime(0.6f);
            var flow = Object.FindFirstObjectByType<UIFlowController>();
            Assert.IsInstanceOf<MainMenuPanel>(flow.VisiblePanel);
            yield return Screenshot("menu");
            flow.OpenLevelSelect();
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsInstanceOf<LevelSelectPanel>(flow.VisiblePanel);
            yield return Screenshot("level_select");
            flow.BackToMainMenu();
        }

        [UnityTest]
        public IEnumerator EveryLevel_LoadsAndRunsWithoutErrors()
        {
            for (int i = 0; i < 8; i++)
            {
                yield return StartLevel(i);
                LevelController level = Level;
                Assert.AreEqual(i, level.LevelIndex);
                Assert.IsNotNull(level.Player);
                Assert.IsNotNull(level.Front);
                Assert.Greater(level.Built.Spawns.Count, 0);
                Assert.AreEqual(level.Built.Spawns.Count, level.Factory.Live.Count + level.Factory.PendingCount, "every spawn entry is live or waiting");
                Assert.AreEqual(i == 7, level.Boss != null);
                level.Player.InputLocked = true;
                yield return new WaitForSeconds(1.5f);
                Assert.IsFalse(level.Player.IsDead, $"level {i + 1}: knight survived standing still for 1.5 s");
                Assert.IsTrue(level.Player.Movement.IsGrounded, $"level {i + 1}: knight is standing on ground");
                Assert.Less(level.Front.X, level.Player.Position.x - 5f);
                yield return Screenshot($"level{i + 1}");
            }
        }

        [UnityTest]
        public IEnumerator ReachingTheExit_CompletesTheLevel_AndUnlocksTheNext()
        {
            yield return StartLevel(0);
            LevelController level = Level;
            PlayerController k = level.Player;
            Vector2 gate = level.Built.Exit.transform.position;
            level.Front.Pause();
            k.Teleport(gate + new Vector2(-3f, 0.2f));
            k.InputLocked = true;
            k.SetScriptedMove(1f);
            yield return WaitForState(GameState.LevelComplete, 10f);
            Assert.IsTrue(gm.Save.IsUnlocked(1));
            Assert.Greater(gm.LastResult.Time, 0f);
            Assert.AreEqual(3, gm.LastResult.MaxHearts);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsInstanceOf<LevelCompletePanel>(Object.FindFirstObjectByType<UIFlowController>().VisiblePanel);
            yield return Screenshot("level_complete");
            gm.NextLevel();
            yield return WaitForState(GameState.Playing, 20f);
            Assert.AreEqual(1, Level.LevelIndex);
        }

        [UnityTest]
        public IEnumerator TheFront_CatchesAnIdleKnight_GameOverShowsTheCause()
        {
            yield return StartLevel(0);
            LevelController level = Level;
            level.Player.InputLocked = true;
            level.Front.ResetTo(level.Player.Position.x - 2f);
            yield return WaitForState(GameState.GameOver, 10f);
            Assert.AreEqual(DeathCause.Front, gm.LastDeathCause);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsInstanceOf<GameOverPanel>(Object.FindFirstObjectByType<UIFlowController>().VisiblePanel);
            yield return Screenshot("game_over");
        }

        [UnityTest]
        public IEnumerator Retry_RespawnsAtTheLastCheckpoint_WithTheFrontReset()
        {
            yield return StartLevel(0);
            LevelController level = Level;
            Checkpoint cp = level.Built.Checkpoints[0];
            cp.Activate();
            Assert.AreEqual(0, gm.CheckpointIndex);
            level.Player.Health.Kill(Combat.DamageType.Hazard);
            yield return WaitForState(GameState.GameOver, 10f);
            gm.RetryFromCheckpoint();
            yield return WaitForState(GameState.Playing, 20f);
            yield return null;
            LevelController again = Level;
            Assert.AreNotSame(level, again);
            Assert.AreEqual(again.Built.Checkpoints[0].SpawnPoint.x, again.Player.Position.x, 0.5f);
            Assert.IsTrue(again.Built.Checkpoints[0].IsActive);
            Assert.Less(again.Front.X, again.Player.Position.x - 10f);
            Assert.AreEqual(again.Player.Health.Max, again.Player.Health.Current, "full health after a retry");
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTime_AndResumeRestoresIt()
        {
            yield return StartLevel(0);
            gm.TogglePause();
            Assert.AreEqual(GameState.Paused, gm.State);
            Assert.AreEqual(0f, Time.timeScale);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsInstanceOf<PausePanel>(Object.FindFirstObjectByType<UIFlowController>().VisiblePanel);
            yield return Screenshot("pause");
            gm.TogglePause();
            Assert.AreEqual(GameState.Playing, gm.State);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Boss_EntersPhaseTwo_ThenFalls_AndTheEndingFinishesTheGame()
        {
            yield return StartLevel(7);
            LevelController level = Level;
            BossController boss = level.Boss;
            PlayerController k = level.Player;
            Assert.IsNotNull(boss);
            k.InputLocked = true;
            k.Teleport(new Vector2(level.Built.ArenaMinX + 4f, k.Position.y + 1f));

            float until = Time.time + 5f;
            while (!boss.FightStarted && Time.time < until) yield return null;
            Assert.IsTrue(boss.FightStarted, "walking into the arena starts the fight");
            Assert.IsTrue(level.Front.Paused, "the front stops for the boss");
            yield return Screenshot("boss_intro");

            until = Time.time + 5f;
            while (boss.Phase != 1 && Time.time < until) { k.Health.Heal(3); yield return null; }
            Assert.AreEqual(1, boss.Phase);

            var sword = new Combat.DamageInfo(1, Combat.DamageType.Sword, k.gameObject, boss.transform.position, Vector2.right, 1f);
            while (boss.Phase == 1)
            {
                boss.Health.TakeDamage(sword);
                k.Health.Heal(3);
                yield return new WaitForSeconds(0.2f);
            }
            Assert.AreEqual(2, boss.Phase);
            Assert.LessOrEqual(boss.Health.Current, boss.Health.Max / 2);

            while (boss.Health.IsAlive)
            {
                boss.Health.TakeDamage(sword);
                k.Health.Heal(3);
                yield return new WaitForSeconds(0.2f);
            }
            yield return WaitForState(GameState.Ending, 6f);
            yield return WaitForState(GameState.GameFinished, 20f);
            Assert.IsTrue(gm.Save.GameFinished);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsInstanceOf<CreditsPanel>(Object.FindFirstObjectByType<UIFlowController>().VisiblePanel);
            yield return Screenshot("credits");
        }
    }
}
