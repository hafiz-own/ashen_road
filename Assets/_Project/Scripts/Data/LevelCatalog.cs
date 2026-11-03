using System.Collections.Generic;
using UnityEngine;

namespace AshenRoad.Data
{
    /// <summary>The ordered list of levels plus the enemy types their layouts refer to.</summary>
    [CreateAssetMenu(menuName = "Ashen Road/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        public List<LevelData> levels = new List<LevelData>();
        public EnemyData[] enemyTypes = new EnemyData[0];
        public BossData boss;
        public PlayerStats playerStats;

        public int Count => levels.Count;

        public LevelData Get(int index)
        {
            if (index < 0 || index >= levels.Count) return null;
            return levels[index];
        }

        public bool IsLast(int index) => index == levels.Count - 1;

        public bool IsValidIndex(int index) => index >= 0 && index < levels.Count;

        public EnemyData EnemyForSymbol(char symbol)
        {
            foreach (EnemyData e in enemyTypes)
                if (e != null && e.layoutSymbol == symbol) return e;
            return null;
        }
    }
}
