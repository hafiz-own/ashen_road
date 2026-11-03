using System;
using System.Collections.Generic;

namespace AshenRoad.Level
{
    /// <summary>
    /// Proves a layout can be finished: a breadth-first search over standable cells using the knight's real
    /// jump height and reach (from PlayerStats). It is deliberately a little conservative.
    /// </summary>
    public class LevelReachability
    {
        readonly LevelLayout layout;
        readonly int maxRise;
        readonly Func<int, int> reachForRise;
        readonly bool[,] support;

        /// <param name="jumpHeight">Knight's jump height in tiles.</param>
        /// <param name="horizontalReach">Horizontal tiles covered by a jump that ends dy tiles higher (dy may be negative).</param>
        public LevelReachability(LevelLayout layout, float jumpHeight, Func<float, float> horizontalReach, float platformRange)
        {
            this.layout = layout;
            maxRise = (int)Math.Floor(jumpHeight - 0.25f);
            reachForRise = dy => (int)Math.Floor(horizontalReach(dy) * 0.75f);
            support = BuildSupport(platformRange);
        }

        bool[,] BuildSupport(float platformRange)
        {
            var s = new bool[layout.Width, layout.Height];
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                    s[x, y] = layout.IsSolid(x, y) || layout.IsOneWay(x, y);
            int range = (int)Math.Floor(platformRange);
            foreach (LevelLayout.Marker m in layout.Markers)
            {
                if (m.Symbol == 'M')
                    for (int i = 0; i <= range; i++) Mark(s, m.X + i, m.Y);
                else if (m.Symbol == 'V')
                    for (int i = 0; i <= range; i++) Mark(s, m.X, m.Y + i);
            }
            return s;
        }

        void Mark(bool[,] s, int x, int y)
        {
            if (layout.InBounds(x, y)) s[x, y] = true;
        }

        bool Supports(int x, int y) => layout.InBounds(x, y) && support[x, y];

        bool Blocked(int x, int y) => layout.IsSolid(x, y);

        /// <summary>A cell the knight can stand in: empty, with support below, and not spikes.</summary>
        public bool IsStandable(int x, int y)
        {
            if (!layout.InBounds(x, y) || y == 0) return false;
            if (Blocked(x, y) || layout.At(x, y) == '^' || layout.At(x, y) == 'M' || layout.At(x, y) == 'V') return false;
            return Supports(x, y - 1) && layout.At(x, y - 1) != '^';
        }

        public bool CanReach(int fromX, int fromY, int toX, int toY)
        {
            if (!IsStandable(fromX, fromY)) return false;
            var visited = new bool[layout.Width, layout.Height];
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((fromX, fromY));
            visited[fromX, fromY] = true;
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if (x == toX && y == toY) return true;
                foreach (var next in Neighbours(x, y))
                {
                    if (visited[next.x, next.y]) continue;
                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        IEnumerable<(int x, int y)> Neighbours(int x, int y)
        {
            int maxReach = reachForRise(-layout.Height);
            for (int dx = -maxReach; dx <= maxReach; dx++)
            {
                int nx = x + dx;
                if (nx < 0 || nx >= layout.Width) continue;
                for (int ny = Math.Max(1, y - layout.Height); ny <= Math.Min(layout.Height - 1, y + maxRise); ny++)
                {
                    if (nx == x && ny == y) continue;
                    if (!IsStandable(nx, ny)) continue;
                    int dy = ny - y;
                    if (Math.Abs(dx) <= 1 && dy == 0) { yield return (nx, ny); continue; }
                    if (Math.Abs(dx) > reachForRise(dy)) continue;
                    if (PathClear(x, y, nx, ny)) yield return (nx, ny);
                }
            }
        }

        /// <summary>Rough arc check: rise in the start column, cross at the top, drop in the end column.</summary>
        bool PathClear(int x0, int y0, int x1, int y1)
        {
            int top = Math.Max(y0, y1) + (y1 >= y0 ? 1 : 0);
            top = Math.Min(top, layout.Height - 1);
            for (int y = y0; y <= top; y++)
                if (Blocked(x0, y)) return false;
            int step = Math.Sign(x1 - x0);
            for (int x = x0; x != x1; x += step)
                if (Blocked(x, top) || Blocked(x, Math.Max(y0, y1))) return false;
            for (int y = y1; y <= top; y++)
                if (Blocked(x1, y)) return false;
            return true;
        }
    }
}
