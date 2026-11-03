using System.Collections;
using AshenRoad.Data;
using AshenRoad.Input;
using AshenRoad.UI;
using UnityEngine;

namespace AshenRoad.Core
{
    /// <summary>
    /// Singleton that owns the game flow: which level is running, the last checkpoint, the game state,
    /// and the moves between menu, levels, pause, game over and the ending. It persists across scenes.
    /// Other objects ask it to do things (StartLevel, Retry...) and hear about results through GameEvents.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] LevelCatalog catalog;
        [SerializeField] InputReader input;
        [SerializeField] ScreenFader fader;
        [SerializeField] float gameOverDelay = 1.3f;

        Coroutine flowRoutine;

        public GameState State { get; private set; } = GameState.Boot;
        public int CurrentLevelIndex { get; private set; }
        /// <summary>Last checkpoint reached in the current level, or -1 for the level start.</summary>
        public int CheckpointIndex { get; private set; } = -1;
        public DeathCause LastDeathCause { get; private set; }
        public LevelResult LastResult { get; private set; }
        public SaveSystem Save { get; private set; }
        public LevelCatalog Catalog => catalog;
        public InputReader Input => input;
        public LevelData CurrentLevel => catalog != null ? catalog.Get(CurrentLevelIndex) : null;
        public int Deaths { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Save = new SaveSystem(new PlayerPrefsStore());
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEvents.PlayerDied += HandlePlayerDied;
            GameEvents.CheckpointActivated += HandleCheckpoint;
            if (input != null) input.PausePressed += TogglePause;
        }

        void OnDisable()
        {
            GameEvents.PlayerDied -= HandlePlayerDied;
            GameEvents.CheckpointActivated -= HandleCheckpoint;
            if (input != null) input.PausePressed -= TogglePause;
        }

        void Start()
        {
            if (SceneLoader.IsMenuSceneActive)
            {
                SetState(GameState.MainMenu);
                fader?.FadeIn();
            }
            // In the Level scene the LevelController reports ready and the flow starts from there.
        }

        // ------------------------------------------------------------------ state

        public bool SetState(GameState next)
        {
            if (!GameStateRules.CanTransition(State, next))
            {
                Debug.LogWarning($"GameManager: illegal transition {State} -> {next} ignored.");
                return false;
            }
            GameState previous = State;
            State = next;
            Time.timeScale = next == GameState.Paused ? 0f : 1f;
            if (input != null)
            {
                if (next == GameState.Playing) input.EnableGameplay();
                else if (next == GameState.Loading || next == GameState.Ending) input.DisableAll();
                else input.EnableUI();
            }
            GameEvents.RaiseGameStateChanged(previous, next);
            return true;
        }

        // ------------------------------------------------------------------ levels

        public void StartNewGame() => StartLevel(0);

        public void ContinueGame() => StartLevel(Mathf.Min(Save.HighestUnlockedLevel, catalog.Count - 1));

        public void StartLevel(int index) => LoadLevel(index, false);

        public void RetryFromCheckpoint() => LoadLevel(CurrentLevelIndex, true);

        public void RestartLevel() => LoadLevel(CurrentLevelIndex, false);

        public void NextLevel()
        {
            if (catalog.IsLast(CurrentLevelIndex)) GoToMainMenu();
            else StartLevel(CurrentLevelIndex + 1);
        }

        void LoadLevel(int index, bool keepCheckpoint)
        {
            if (!catalog.IsValidIndex(index))
            {
                Debug.LogError($"GameManager: no level {index}.");
                return;
            }
            if (!SetState(GameState.Loading)) return;
            if (index != CurrentLevelIndex || !keepCheckpoint) CheckpointIndex = -1;
            if (!keepCheckpoint) Deaths = 0;
            CurrentLevelIndex = index;
            RunFlow(LoadSceneRoutine(SceneLoader.LevelScene));
        }

        public void GoToMainMenu()
        {
            if (!SetState(GameState.Loading)) return;
            RunFlow(LoadMenuRoutine());
        }

        IEnumerator LoadSceneRoutine(string scene)
        {
            if (fader != null) yield return fader.FadeOutRoutine();
            yield return SceneLoader.LoadRoutine(scene);
            // LevelController calls NotifyLevelReady when the level is built.
        }

        IEnumerator LoadMenuRoutine()
        {
            if (fader != null) yield return fader.FadeOutRoutine();
            yield return SceneLoader.LoadRoutine(SceneLoader.MainMenuScene);
            SetState(GameState.MainMenu);
            fader?.FadeIn();
        }

        /// <summary>Called by the LevelController once the level has been built and the knight placed.</summary>
        public void NotifyLevelReady(int levelIndex)
        {
            if (State == GameState.Boot)
            {
                // Pressing Play directly in the Level scene in the editor.
                CurrentLevelIndex = levelIndex;
                SetState(GameState.Loading);
            }
            if (State != GameState.Loading) return;
            SetState(GameState.Playing);
            fader?.FadeIn();
            GameEvents.RaiseLevelStarted(CurrentLevelIndex);
        }

        public void CompleteLevel(float time, int heartsLeft, int maxHearts, int kills)
        {
            if (State != GameState.Playing) return;
            bool newBest = Save.RecordTime(CurrentLevelIndex, time);
            Save.UnlockLevel(CurrentLevelIndex + 1);
            LastResult = new LevelResult
            {
                LevelIndex = CurrentLevelIndex,
                Time = time,
                HeartsLeft = heartsLeft,
                MaxHearts = maxHearts,
                NewBestTime = newBest,
                BestTime = Save.GetBestTime(CurrentLevelIndex),
                EnemiesKilled = kills
            };
            CheckpointIndex = -1;
            SetState(GameState.LevelComplete);
            GameEvents.RaiseLevelCompleted(LastResult);
        }

        public void BeginEnding()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Ending);
        }

        public void FinishGame()
        {
            if (State != GameState.Ending) return;
            Save.GameFinished = true;
            Save.UnlockLevel(catalog.Count - 1);
            SetState(GameState.GameFinished);
        }

        // ------------------------------------------------------------------ pause

        public void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void Pause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ events

        void HandlePlayerDied(DeathCause cause)
        {
            if (State != GameState.Playing) return;
            LastDeathCause = cause;
            Deaths++;
            RunFlow(GameOverRoutine());
        }

        IEnumerator GameOverRoutine()
        {
            yield return new WaitForSecondsRealtime(gameOverDelay);
            if (State == GameState.Playing) SetState(GameState.GameOver);
        }

        void HandleCheckpoint(int index)
        {
            if (index > CheckpointIndex) CheckpointIndex = index;
        }

        void RunFlow(IEnumerator routine)
        {
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            flowRoutine = StartCoroutine(routine);
        }

        /// <summary>Test hook: keeps tests from touching the player's real save data.</summary>
        public void UseSaveStore(ISaveStore store) => Save = new SaveSystem(store);

        /// <summary>Test hook: puts the manager into a level without loading a scene.</summary>
        public void SetupForTests(LevelCatalog testCatalog, int levelIndex)
        {
            catalog = testCatalog;
            CurrentLevelIndex = levelIndex;
            CheckpointIndex = -1;
            if (State != GameState.Boot)
            {
                State = GameState.Boot;
                Time.timeScale = 1f;
            }
        }
    }
}
