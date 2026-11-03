using AshenRoad.Core;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Level.Hazards
{
    /// <summary>Drops rocks ahead of the knight at intervals (mines, ruined city), reusing them through an ObjectPool.</summary>
    public class AmbientDebris : MonoBehaviour
    {
        ObjectPool<FallingDebris> pool;
        PlayerController knight;
        LevelLayout layout;
        float interval;
        float nextDropAt;

        public void Setup(FallingDebris prefab, PlayerController player, LevelLayout levelLayout, float dropInterval)
        {
            knight = player;
            layout = levelLayout;
            interval = dropInterval;
            pool = new ObjectPool<FallingDebris>(prefab, transform, 4, 12);
            nextDropAt = Time.time + interval;
        }

        void Update()
        {
            if (pool == null || knight == null || knight.IsDead || Time.time < nextDropAt) return;
            nextDropAt = Time.time + interval * Random.Range(0.7f, 1.3f);

            int x = Mathf.FloorToInt(knight.Position.x + Random.Range(2f, 7f) * Mathf.Max(0.3f, knight.Facing));
            if (!TryFindFloor(x, Mathf.FloorToInt(knight.Position.y) + 4, out int floorY, out int ceiling)) return;

            FallingDebris rock = pool.Get(new Vector3(x + 0.5f, floorY, 0f));
            rock.Done -= ReturnToPool;
            rock.Done += ReturnToPool;
            rock.Drop(Mathf.Clamp(ceiling - floorY, 4f, 10f));
        }

        void ReturnToPool(FallingDebris rock)
        {
            rock.Done -= ReturnToPool;
            pool.Release(rock);
        }

        bool TryFindFloor(int x, int fromY, out int floorY, out int ceiling)
        {
            floorY = 0;
            ceiling = 0;
            if (layout == null || x < 0 || x >= layout.Width) return false;
            for (int y = Mathf.Min(fromY, layout.Height - 1); y > 0; y--)
            {
                if (!layout.IsSolid(x, y) && (layout.IsSolid(x, y - 1) || layout.IsOneWay(x, y - 1)))
                {
                    floorY = y;
                    ceiling = y + 1;
                    while (ceiling < layout.Height + 4 && !layout.IsSolid(x, ceiling)) ceiling++;
                    return true;
                }
            }
            return false;
        }
    }
}
