using System.IO;
using UnityEditor;
using UnityEngine;

namespace AshenRoad.EditorTools
{
    /// <summary>Small helpers the project builder uses to create assets and set serialized fields.</summary>
    public static class BuilderUtil
    {
        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        public static Sprite Sprite(string path)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogError("Builder: missing sprite " + path);
            return s;
        }

        public static GameObject Go(string name, Transform parent = null, int layer = 0)
        {
            var go = new GameObject(name);
            go.layer = layer;
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public static SpriteRenderer SpriteChild(Transform parent, string name, Sprite sprite, int order, Vector3 localPosition)
        {
            GameObject go = Go(name, parent, parent.gameObject.layer);
            go.transform.localPosition = localPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        public static GameObject SavePrefab(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Fluent setter for private [SerializeField] fields.</summary>
        public static Fields Set(Object target) => new Fields(target);

        public class Fields
        {
            readonly SerializedObject so;

            public Fields(Object target)
            {
                so = new SerializedObject(target);
            }

            SerializedProperty P(string name)
            {
                SerializedProperty p = so.FindProperty(name);
                if (p == null) Debug.LogError($"Builder: field '{name}' not found on {so.targetObject.GetType().Name}");
                return p;
            }

            public Fields Ref(string name, Object value) { P(name).objectReferenceValue = value; return this; }
            public Fields Float(string name, float value) { P(name).floatValue = value; return this; }
            public Fields Int(string name, int value) { P(name).intValue = value; return this; }
            public Fields Bool(string name, bool value) { P(name).boolValue = value; return this; }
            public Fields Vec2(string name, Vector2 value) { P(name).vector2Value = value; return this; }
            public Fields Str(string name, string value) { P(name).stringValue = value; return this; }

            public Fields Refs(string name, Object[] values)
            {
                SerializedProperty p = P(name);
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                return this;
            }

            public void Apply() => so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
