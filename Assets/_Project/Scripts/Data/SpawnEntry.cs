using System;
using UnityEngine;

namespace AshenRoad.Data
{
    /// <summary>One enemy to spawn in a level: which type and where.</summary>
    [Serializable]
    public struct SpawnEntry
    {
        public EnemyData enemy;
        public Vector2 position;

        public SpawnEntry(EnemyData enemy, Vector2 position)
        {
            this.enemy = enemy;
            this.position = position;
        }
    }
}
