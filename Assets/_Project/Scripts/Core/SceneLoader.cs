using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AshenRoad.Core
{
    /// <summary>Loads the menu and level scenes asynchronously and reports progress.</summary>
    public static class SceneLoader
    {
        public const string MainMenuScene = "MainMenu";
        public const string LevelScene = "Level";

        public static float Progress { get; private set; } = 1f;
        public static bool IsLoading { get; private set; }

        public static bool IsLevelSceneActive => SceneManager.GetActiveScene().name == LevelScene;
        public static bool IsMenuSceneActive => SceneManager.GetActiveScene().name == MainMenuScene;

        public static IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            Progress = 0f;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
            {
                Progress = Mathf.Clamp01(op.progress / 0.9f);
                yield return null;
            }
            Progress = 1f;
            IsLoading = false;
        }
    }
}
