using System.Collections.Generic;
using UnityEngine;

namespace AshenRoad.Core
{
    /// <summary>
    /// Object Pool pattern: reuses inactive copies of a prefab instead of Instantiate/Destroy for things
    /// that appear constantly (sparks, dust, slashes, debris chips).
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        readonly T prefab;
        readonly Transform parent;
        readonly Stack<T> inactive = new Stack<T>();
        readonly List<T> all = new List<T>();
        readonly int maxSize;

        public int CountAll => all.Count;
        public int CountInactive => inactive.Count;
        public int CountActive => all.Count - inactive.Count;

        public ObjectPool(T prefab, Transform parent, int prewarm = 0, int maxSize = 64)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxSize = maxSize;
            for (int i = 0; i < prewarm; i++)
            {
                T item = Create();
                item.gameObject.SetActive(false);
                inactive.Push(item);
            }
        }

        T Create()
        {
            T item = Object.Instantiate(prefab, parent);
            item.name = prefab.name;
            all.Add(item);
            return item;
        }

        public T Get(Vector3 position)
        {
            T item;
            if (inactive.Count > 0) item = inactive.Pop();
            else if (all.Count < maxSize) item = Create();
            else
            {
                // pool is full: recycle the oldest active item
                item = all[0];
                all.RemoveAt(0);
                all.Add(item);
            }
            item.transform.position = position;
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null || !item.gameObject.activeSelf) return;
            item.gameObject.SetActive(false);
            inactive.Push(item);
        }
    }
}
