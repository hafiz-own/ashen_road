using System.Collections.Generic;
using AshenRoad.Boss;
using AshenRoad.Data;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Enemies
{
    /// <summary>
    /// Factory pattern: the only place enemies are instantiated. Builds them from EnemyData, streams a level's
    /// spawn table in as the knight advances, removes enemies the front has passed, and spawns boss summons.
    /// </summary>
    public class EnemyFactory : MonoBehaviour
    {
        [SerializeField] Transform container;

        readonly List<SpawnEntry> pending = new List<SpawnEntry>();
        readonly List<EnemyAI> live = new List<EnemyAI>();
        PlayerController target;

        public IReadOnlyList<EnemyAI> Live => live;
        public int PendingCount => pending.Count;

        void Awake()
        {
            if (container == null) container = transform;
        }

        public void Setup(PlayerController knight, IEnumerable<SpawnEntry> entries)
        {
            target = knight;
            pending.Clear();
            if (entries != null) pending.AddRange(entries);
            pending.Sort((a, b) => a.position.x.CompareTo(b.position.x));
        }

        public EnemyAI Create(EnemyData data, Vector2 position)
        {
            if (data == null || data.prefab == null)
            {
                Debug.LogError("EnemyFactory: EnemyData or its prefab is missing.");
                return null;
            }
            GameObject go = Instantiate(data.prefab, position, Quaternion.identity, container);
            EnemyAI ai = go.GetComponent<EnemyAI>();
            ai.Init(data, target);
            ai.Removed += OnRemoved;
            live.Add(ai);
            return ai;
        }

        public BossController CreateBoss(GameObject prefab, BossData data, Vector2 position)
        {
            GameObject go = Instantiate(prefab, position, Quaternion.identity, container);
            BossController boss = go.GetComponent<BossController>();
            boss.Init(data, target, this);
            return boss;
        }

        /// <summary>Spawns every pending entry at or before maxX. Returns how many were created.</summary>
        public int SpawnUpTo(float maxX)
        {
            int count = 0;
            while (pending.Count > 0 && pending[0].position.x <= maxX)
            {
                SpawnEntry e = pending[0];
                pending.RemoveAt(0);
                if (Create(e.enemy, e.position) != null) count++;
            }
            return count;
        }

        /// <summary>Removes enemies left behind the given x (swallowed by the front). Returns how many.</summary>
        public int DespawnBehind(float minX)
        {
            int count = 0;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                EnemyAI e = live[i];
                if (e == null) { live.RemoveAt(i); continue; }
                if (e.transform.position.x < minX)
                {
                    e.Despawn();
                    count++;
                }
            }
            return count;
        }

        /// <summary>Drops pending entries that are already behind the given x (used when respawning at a checkpoint).</summary>
        public void SkipPendingBefore(float x)
        {
            pending.RemoveAll(e => e.position.x < x);
        }

        public EnemyAI SpawnSummon(EnemyData data, Vector2 position)
        {
            EnemyAI ai = Create(data, position);
            if (ai != null)
            {
                ai.IsSummoned = true;
                Core.GameEvents.RaiseEffect(Core.EffectType.Dust, position, Vector2.up);
            }
            return ai;
        }

        public int CountAlive(EnemyData type, bool summonedOnly = false)
        {
            int n = 0;
            foreach (EnemyAI e in live)
                if (e != null && !e.IsDead && (type == null || e.Data == type) && (!summonedOnly || e.IsSummoned)) n++;
            return n;
        }

        void OnRemoved(EnemyAI enemy)
        {
            live.Remove(enemy);
        }
    }
}
