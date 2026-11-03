using System;
using System.Collections.Generic;
using AshenRoad.Core;
using UnityEngine;

namespace AshenRoad.Visual
{
    /// <summary>
    /// Plays particle effects requested through GameEvents (Observer). Each effect type has its own ObjectPool,
    /// so the hundreds of dust puffs and sparks in a level reuse a handful of objects.
    /// </summary>
    public class EffectPlayer : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public EffectType type;
            public PooledParticle prefab;
            public int prewarm;
        }

        [SerializeField] Entry[] effects = new Entry[0];

        readonly Dictionary<EffectType, ObjectPool<PooledParticle>> pools = new Dictionary<EffectType, ObjectPool<PooledParticle>>();

        void Awake()
        {
            foreach (Entry e in effects)
                if (e.prefab != null && !pools.ContainsKey(e.type))
                    pools[e.type] = new ObjectPool<PooledParticle>(e.prefab, transform, e.prewarm, 32);
        }

        void OnEnable() => GameEvents.EffectRequested += Play;

        void OnDisable() => GameEvents.EffectRequested -= Play;

        public void Play(EffectType type, Vector2 position, Vector2 direction)
        {
            if (!pools.TryGetValue(type, out ObjectPool<PooledParticle> pool)) return;
            PooledParticle p = pool.Get(new Vector3(position.x, position.y, -2f));
            float angle = direction.sqrMagnitude > 0.001f ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg : 90f;
            p.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            p.Play(() => pool.Release(p));
        }

        public int ActiveCount(EffectType type) => pools.TryGetValue(type, out var pool) ? pool.CountActive : 0;
    }
}
