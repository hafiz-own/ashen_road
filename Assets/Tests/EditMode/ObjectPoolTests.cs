using AshenRoad.Core;
using NUnit.Framework;
using UnityEngine;

namespace AshenRoad.Tests
{
    public class ObjectPoolTests
    {
        GameObject prefab;
        GameObject parent;

        [SetUp]
        public void SetUp()
        {
            prefab = new GameObject("PooledThing");
            parent = new GameObject("PoolParent");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Prewarm_CreatesInactiveCopies()
        {
            var pool = new ObjectPool<Transform>(prefab.transform, parent.transform, 3);
            Assert.AreEqual(3, pool.CountAll);
            Assert.AreEqual(3, pool.CountInactive);
        }

        [Test]
        public void ReleasedItems_AreReused()
        {
            var pool = new ObjectPool<Transform>(prefab.transform, parent.transform, 0);
            Transform a = pool.Get(Vector3.one);
            Assert.IsTrue(a.gameObject.activeSelf);
            Assert.AreEqual(Vector3.one, a.position);
            pool.Release(a);
            Assert.IsFalse(a.gameObject.activeSelf);
            Transform b = pool.Get(Vector3.zero);
            Assert.AreSame(a, b);
            Assert.AreEqual(1, pool.CountAll);
        }

        [Test]
        public void FullPool_RecyclesInsteadOfGrowing()
        {
            var pool = new ObjectPool<Transform>(prefab.transform, parent.transform, 0, 2);
            pool.Get(Vector3.zero);
            pool.Get(Vector3.zero);
            pool.Get(Vector3.zero);
            Assert.AreEqual(2, pool.CountAll);
        }

        [Test]
        public void DoubleRelease_IsHarmless()
        {
            var pool = new ObjectPool<Transform>(prefab.transform, parent.transform, 0);
            Transform a = pool.Get(Vector3.zero);
            pool.Release(a);
            pool.Release(a);
            Assert.AreEqual(1, pool.CountInactive);
        }
    }
}
