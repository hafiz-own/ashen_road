using System.Collections;
using AshenRoad.Boss;
using AshenRoad.Cameras;
using AshenRoad.Core;
using AshenRoad.Data;
using AshenRoad.Enemies;
using AshenRoad.Level.Hazards;
using AshenRoad.Player;
using AshenRoad.Visual;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>
    /// Runs one level scene: asks the builder for the level, spawns the knight at the start or the last checkpoint,
    /// places the disaster front, streams enemies through the factory and reports the finish to the GameManager.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [SerializeField] LevelCatalog catalog;
        [SerializeField] LevelPrefabs prefabs;
        [SerializeField] LevelBuilder builder;
        [SerializeField] EnemyFactory factory;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] ParallaxBackground background;
        [SerializeField] AmbientDebris ambientDebris;
        [SerializeField] EndingController ending;
        [Tooltip("Level used when you press Play directly in this scene.")]
        [SerializeField] int editorLevelIndex;
        [SerializeField] float spawnAhead = 24f;
        [SerializeField] float despawnBehind = 3f;
        [SerializeField] bool autoStart = true;

        float startTime;
        bool finished;

        public LevelData Data { get; private set; }
        public int LevelIndex { get; private set; }
        public BuiltLevel Built { get; private set; }
        public PlayerController Player { get; private set; }
        public DisasterFront Front { get; private set; }
        public BossController Boss { get; private set; }
        public EnemyFactory Factory => factory;
        public float ElapsedTime => Time.time - startTime;
        public bool Finished => finished;

        void OnEnable()
        {
            GameEvents.ExitReached += OnExitReached;
            GameEvents.BossIntroStarted += OnBossIntro;
        }

        void OnDisable()
        {
            GameEvents.ExitReached -= OnExitReached;
            GameEvents.BossIntroStarted -= OnBossIntro;
        }

        void Start()
        {
            if (!autoStart) return;
            GameManager gm = GameManager.Instance;
            bool fromMenuFlow = gm != null && gm.State == GameState.Loading;
            int index = fromMenuFlow ? gm.CurrentLevelIndex : editorLevelIndex;
            int checkpoint = fromMenuFlow ? gm.CheckpointIndex : -1;
            if (gm != null && gm.Catalog != null) catalog = gm.Catalog;
            Load(index, checkpoint);
            gm?.NotifyLevelReady(index);
        }

        /// <summary>Builds and starts the level. Public so tests can load a level directly.</summary>
        public void Load(int index, int checkpointIndex)
        {
            LevelIndex = Mathf.Clamp(index, 0, catalog.Count - 1);
            Data = catalog.Get(LevelIndex);
            Built = builder.Build(Data, prefabs, catalog);

            // Knight
            Vector2 spawn = Built.PlayerStart;
            for (int i = 0; i < Built.Checkpoints.Count; i++)
            {
                bool reached = i <= checkpointIndex;
                Built.Checkpoints[i].Setup(i, reached);
                if (reached && i == checkpointIndex) spawn = Built.Checkpoints[i].SpawnPoint;
            }
            Player = Instantiate(prefabs.player, spawn, Quaternion.identity).GetComponent<PlayerController>();
            Player.name = "Knight";
            if (catalog.playerStats != null) Player.Initialise(catalog.playerStats);
            if (GameManager.Instance != null) Player.BindInput(GameManager.Instance.Input);

            // Camera and backdrop
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(Player);
                cameraFollow.SetBounds(Built.Bounds);
                cameraFollow.SnapToTarget();
            }
            if (background != null) background.Apply(Data);

            // Disaster front
            Front = Instantiate(prefabs.disasterFront).GetComponent<DisasterFront>();
            float offset = checkpointIndex >= 0 ? Data.respawnFrontOffset : Data.frontStartOffset;
            Front.Setup(Data, spawn.x - offset, Player, Built.Layout.Height);
            if (Data.isBossLevel && !float.IsInfinity(Built.ArenaMinX)) Front.SetStopX(Built.ArenaMinX - 6f);

            // Props that need the knight
            if (Built.Exit != null) Built.Exit.Setup(Player);
            foreach (FallingDebris d in Built.Debris) d.Setup(Player, CeilingAbove(d.transform.position));

            // Enemies
            factory.Setup(Player, Built.Spawns);
            factory.SkipPendingBefore(spawn.x - 1f);
            factory.SpawnUpTo(spawn.x + spawnAhead);

            if (Data.isBossLevel && Built.BossPosition.HasValue && prefabs.boss != null)
            {
                Boss = factory.CreateBoss(prefabs.boss, catalog.boss, Built.BossPosition.Value);
                Boss.SetArena(Built.ArenaWalls, Built.ArenaMinX, Built.ArenaMaxX);
            }
            if (Data.isBossLevel && ending != null && Built.PrincessPosition.HasValue)
                ending.Setup(Player, Built.PrincessPosition.Value, prefabs.princess);

            if (ambientDebris != null && Data.ambientDebris && prefabs.fallingDebris != null)
                ambientDebris.Setup(prefabs.fallingDebris.GetComponent<FallingDebris>(), Player, Built.Layout, Data.ambientDebrisInterval);

            startTime = Time.time;
            finished = false;
            if (!string.IsNullOrEmpty(Data.displayName)) GameEvents.RaiseHint(Data.displayName);
        }

        float CeilingAbove(Vector2 position)
        {
            int x = Mathf.FloorToInt(position.x);
            int y = Mathf.FloorToInt(position.y) + 1;
            int top = y;
            while (top < Built.Layout.Height + 6 && !Built.Layout.IsSolid(x, top)) top++;
            return Mathf.Clamp(top - y + 1, 4f, 10f);
        }

        void Update()
        {
            if (Player == null || Built == null) return;
            factory.SpawnUpTo(Player.Position.x + spawnAhead);
            if (Front != null) factory.DespawnBehind(Front.X - despawnBehind);
        }

        void OnExitReached()
        {
            if (finished || Player == null || Player.IsDead) return;
            finished = true;
            StartCoroutine(FinishRoutine());
        }

        IEnumerator FinishRoutine()
        {
            float time = ElapsedTime;
            Player.InputLocked = true;
            Player.SetScriptedMove(0.4f);
            Front?.Pause();
            yield return new WaitForSeconds(0.6f);
            Player.SetScriptedMove(0f);
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.CompleteLevel(time, Player.Health.Current, Player.Health.Max, Player.Combat.Kills);
        }

        void OnBossIntro()
        {
            Front?.Pause();
        }
    }
}
