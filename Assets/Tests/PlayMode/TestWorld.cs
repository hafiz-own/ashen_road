using System.Collections.Generic;
using AshenRoad.Core;
using AshenRoad.Data;
using AshenRoad.Enemies;
using AshenRoad.Player;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AshenRoad.Tests
{
    /// <summary>Builds small sandbox scenes for play mode tests and cleans them up afterwards.</summary>
    public class TestWorld
    {
        public const string Data = "Assets/_Project/Data/";
        public const string Prefabs = "Assets/_Project/Prefabs/";

        readonly List<GameObject> created = new List<GameObject>();

        public static T Load<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }

        public static LevelCatalog Catalog => Load<LevelCatalog>(Data + "LevelCatalog.asset");

        public GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }

        public GameObject Ground(float xMin, float xMax, float top = 0f, float depth = 2f)
        {
            var go = new GameObject("TestGround") { layer = Layers.Ground };
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(xMax - xMin, depth);
            go.transform.position = new Vector3((xMin + xMax) * 0.5f, top - depth * 0.5f, 0f);
            return Track(go);
        }

        public PlayerController Knight(Vector2 position)
        {
            GameObject go = Object.Instantiate(Load<GameObject>(Prefabs + "Knight.prefab"), position, Quaternion.identity);
            Track(go);
            var knight = go.GetComponent<PlayerController>();
            knight.InputLocked = true;
            return knight;
        }

        public EnemyAI Enemy(string type, Vector2 position, PlayerController target)
        {
            var data = Load<EnemyData>(Data + "Enemies/" + type + ".asset");
            GameObject go = Object.Instantiate(data.prefab, position, Quaternion.identity);
            Track(go);
            var ai = go.GetComponent<EnemyAI>();
            ai.Init(data, target);
            return ai;
        }

        public T Prefab<T>(string name, Vector2 position) where T : Component
        {
            GameObject go = Object.Instantiate(Load<GameObject>(Prefabs + name + ".prefab"), position, Quaternion.identity);
            Track(go);
            return go.GetComponent<T>();
        }

        public void Cleanup()
        {
            foreach (GameObject go in created)
                if (go != null) Object.Destroy(go);
            created.Clear();
            Time.timeScale = 1f;
        }
    }
}
