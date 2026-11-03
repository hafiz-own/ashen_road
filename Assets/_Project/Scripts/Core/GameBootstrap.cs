using UnityEngine;

namespace AshenRoad.Core
{
    /// <summary>
    /// Creates the persistent Systems object (GameManager, AudioManager, input, UI) before the first scene loads,
    /// so Play works from any scene and there is no separate boot scene to forget.
    /// </summary>
    public static class GameBootstrap
    {
        public const string SystemsResource = "Systems";

        /// <summary>Tests that build their own scene set this to skip the automatic Systems object.</summary>
        public static bool Disabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateSystems()
        {
            if (Disabled || GameManager.Instance != null) return;
            GameObject prefab = Resources.Load<GameObject>(SystemsResource);
            if (prefab == null)
            {
                Debug.LogWarning("GameBootstrap: Resources/Systems prefab missing. Run 'Ashen Road > Rebuild Project Assets'.");
                return;
            }
            GameObject systems = Object.Instantiate(prefab);
            systems.name = SystemsResource;
        }
    }
}
