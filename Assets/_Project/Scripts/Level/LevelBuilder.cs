using System.Collections.Generic;
using AshenRoad.Data;
using AshenRoad.Level.Hazards;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AshenRoad.Level
{
    /// <summary>
    /// Turns a LevelData's text grid into the playable level: tiles with merged colliders, one-way platforms,
    /// props, hazards and the spawn table. It places things but does not run the level (LevelController does).
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        [SerializeField] Tilemap solidMap;
        [SerializeField] Tilemap platformMap;
        [SerializeField] Transform propsRoot;
        [SerializeField] BoxCollider2D leftWall;
        [SerializeField] BoxCollider2D rightWall;
        [SerializeField] BoxCollider2D killZone;

        Tile[] topTiles, fillTiles, deepTiles;
        Tile platformTile;

        public BuiltLevel Build(LevelData data, LevelPrefabs prefabs, LevelCatalog catalog)
        {
            LevelLayout layout = LevelLayout.Parse(data.layout);
            var built = new BuiltLevel { Layout = layout };

            topTiles = MakeTiles(data.topTiles, data.topTile);
            fillTiles = MakeTiles(data.fillTiles, data.fillTile);
            deepTiles = MakeTiles(data.deepTiles, data.fillTile);
            if (deepTiles.Length == 0) deepTiles = fillTiles;
            platformTile = MakeTile(data.platformTile, Tile.ColliderType.Grid);

            PaintTiles(layout);
            BuildBoundaries(layout, built);
            PlaceMarkers(data, prefabs, catalog, layout, built);
            PlaceBackdrop(data, layout);
            return built;
        }

        static Tile[] MakeTiles(Sprite[] variants, Sprite fallback)
        {
            var list = new List<Tile>();
            if (variants != null)
                foreach (Sprite s in variants)
                    if (s != null) list.Add(MakeTile(s));
            if (list.Count == 0 && fallback != null) list.Add(MakeTile(fallback));
            return list.ToArray();
        }

        /// <summary>Stable pseudo-random pick so the same level always looks the same.</summary>
        static T Pick<T>(T[] options, int x, int y)
        {
            int h = (x * 73856093) ^ (y * 19349663);
            return options[(h & 0x7fffffff) % options.Length];
        }

        static Tile MakeTile(Sprite sprite, Tile.ColliderType collider = Tile.ColliderType.Grid)
        {
            Tile t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sprite;
            t.colliderType = collider;
            return t;
        }

        void PaintTiles(LevelLayout layout)
        {
            solidMap.ClearAllTiles();
            platformMap.ClearAllTiles();
            var solidPositions = new List<Vector3Int>();
            var solidTiles = new List<TileBase>();
            var platformPositions = new List<Vector3Int>();
            for (int x = 0; x < layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    if (layout.IsSolid(x, y))
                    {
                        solidPositions.Add(new Vector3Int(x, y, 0));
                        int depth = 0;
                        while (depth < 3 && layout.IsSolid(x, y + depth + 1)) depth++;
                        solidTiles.Add(depth == 0 ? Pick(topTiles, x, y) : depth == 1 ? Pick(fillTiles, x, y) : Pick(deepTiles, x, y));
                    }
                    else if (layout.IsOneWay(x, y))
                    {
                        platformPositions.Add(new Vector3Int(x, y, 0));
                    }
                }
                // Extend the ground below the bottom row so pits look deep and solid columns don't float.
                if (layout.IsSolid(x, 0))
                {
                    for (int y = -1; y >= -6; y--)
                    {
                        solidPositions.Add(new Vector3Int(x, y, 0));
                        solidTiles.Add(Pick(deepTiles, x, y));
                    }
                }
            }
            solidMap.SetTiles(solidPositions.ToArray(), solidTiles.ToArray());
            var pt = new TileBase[platformPositions.Count];
            for (int i = 0; i < pt.Length; i++) pt[i] = platformTile;
            platformMap.SetTiles(platformPositions.ToArray(), pt);

            RefreshColliders(solidMap);
            RefreshColliders(platformMap);
        }

        static void RefreshColliders(Tilemap map)
        {
            var tilemapCollider = map.GetComponent<TilemapCollider2D>();
            if (tilemapCollider != null) tilemapCollider.ProcessTilemapChanges();
            var composite = map.GetComponent<CompositeCollider2D>();
            if (composite != null) composite.GenerateGeometry();
        }

        void BuildBoundaries(LevelLayout layout, BuiltLevel built)
        {
            float h = layout.Height + 30f;
            if (leftWall != null)
            {
                leftWall.size = new Vector2(2f, h);
                leftWall.transform.position = new Vector3(-1f, h * 0.5f - 8f, 0f);
            }
            if (rightWall != null)
            {
                rightWall.size = new Vector2(2f, h);
                rightWall.transform.position = new Vector3(layout.Width + 1f, h * 0.5f - 8f, 0f);
            }
            built.KillY = -3.5f;
            if (killZone != null)
            {
                killZone.size = new Vector2(layout.Width + 60f, 3f);
                killZone.transform.position = new Vector3(layout.Width * 0.5f, built.KillY, 0f);
            }
            built.Bounds = new Rect(0f, -1f, layout.Width, layout.Height + 6f);
        }

        void PlaceMarkers(LevelData data, LevelPrefabs prefabs, LevelCatalog catalog, LevelLayout layout, BuiltLevel built)
        {
            var checkpoints = new List<LevelLayout.Marker>(layout.All('C'));
            checkpoints.Sort((a, b) => a.X.CompareTo(b.X));
            int hintIndex = 0;
            int fireIndex = 0;
            var arenaXs = new List<int>();
            var platformCells = new HashSet<Vector2Int>();

            foreach (LevelLayout.Marker m in layout.Markers)
            {
                Vector2 pos = LevelLayout.CellToWorld(m.X, m.Y);
                switch (m.Symbol)
                {
                    case 'P':
                        built.PlayerStart = pos + new Vector2(0f, 0.05f);
                        break;
                    case 'E':
                        built.Exit = Spawn(prefabs.exitGate, pos).GetComponent<ExitGate>();
                        break;
                    case 'C':
                        var cp = Spawn(prefabs.checkpoint, pos).GetComponent<Checkpoint>();
                        cp.Setup(checkpoints.FindIndex(c => c.X == m.X && c.Y == m.Y), false);
                        built.Checkpoints.Add(cp);
                        break;
                    case 'H':
                        Spawn(prefabs.heartPickup, pos);
                        break;
                    case '^':
                        Spawn(prefabs.spikes, pos);
                        break;
                    case 'F':
                        var fire = Spawn(prefabs.firePatch, pos).GetComponent<FirePatch>();
                        fire.Configure(data.fireCycleOn, data.fireCycleOff, (fireIndex++ % 3) * 0.7f);
                        built.Fires.Add(fire);
                        break;
                    case 'D':
                        var debris = Spawn(prefabs.fallingDebris, pos).GetComponent<FallingDebris>();
                        built.Debris.Add(debris);
                        break;
                    case 'M':
                    case 'V':
                        if (platformCells.Contains(new Vector2Int(m.X, m.Y))) break;
                        int width = 0;
                        while (layout.At(m.X + width, m.Y) == m.Symbol)
                        {
                            platformCells.Add(new Vector2Int(m.X + width, m.Y));
                            width++;
                        }
                        var platform = Spawn(prefabs.movingPlatform, new Vector2(m.X, m.Y)).GetComponent<MovingPlatform>();
                        platform.SetWidth(width);
                        Vector2 travel = m.Symbol == 'M' ? new Vector2(data.movingPlatformRange, 0f) : new Vector2(0f, data.movingPlatformRange);
                        platform.Setup(travel, data.movingPlatformSpeed);
                        break;
                    case 'S':
                    case 'R':
                    case 'B':
                        EnemyData enemy = catalog.EnemyForSymbol(m.Symbol);
                        if (enemy != null) built.Spawns.Add(new SpawnEntry(enemy, pos + new Vector2(0f, 0.05f)));
                        else Debug.LogWarning($"LevelBuilder: no EnemyData for symbol {m.Symbol}.");
                        break;
                    case 'K':
                        built.BossPosition = pos + new Vector2(0f, 0.05f);
                        break;
                    case 'Q':
                        built.PrincessPosition = pos;
                        break;
                    case '|':
                        arenaXs.Add(m.X);
                        built.ArenaWalls.Add(Spawn(prefabs.arenaWall, pos + new Vector2(0f, 0f)).GetComponent<ArenaWall>());
                        break;
                    case '!':
                        var sign = Spawn(prefabs.sign, pos).GetComponent<TutorialSign>();
                        sign.Text = hintIndex < data.hints.Length ? data.hints[hintIndex] : "";
                        hintIndex++;
                        break;
                    case 'd':
                        GameObject deco = Spawn(prefabs.decoration, pos);
                        var sr = deco.GetComponentInChildren<SpriteRenderer>();
                        Sprite[] decor = data.decorations != null && data.decorations.Length > 0 ? data.decorations : prefabs.decorationSprites;
                        if (sr != null && decor.Length > 0) sr.sprite = decor[(m.X * 7 + m.Y * 3) % decor.Length];
                        break;
                }
            }

            if (arenaXs.Count == 2)
            {
                arenaXs.Sort();
                built.ArenaMinX = arenaXs[0] + 1f;
                built.ArenaMaxX = arenaXs[1];
            }
        }

        /// <summary>
        /// Big scenery (houses, trees, ruins) standing behind the road every few tiles. Only placed where the ground
        /// under the whole prop is flat, so nothing hangs over a pit.
        /// </summary>
        void PlaceBackdrop(LevelData data, LevelLayout layout)
        {
            if (data.backdropProps == null || data.backdropProps.Length == 0) return;
            Transform root = new GameObject("Backdrop").transform;
            root.SetParent(propsRoot != null ? propsRoot : transform, false);
            int index = 0;
            float x = 6f;
            while (x < layout.Width - 4)
            {
                Sprite s = data.backdropProps[index % data.backdropProps.Length];
                if (s == null) { index++; continue; }
                int half = Mathf.Max(1, Mathf.CeilToInt(s.bounds.extents.x * 0.6f));
                int cx = Mathf.FloorToInt(x);
                int ground = SurfaceAt(layout, cx);
                bool flat = ground >= 0;
                for (int dx = -half; dx <= half && flat; dx++)
                    flat = SurfaceAt(layout, cx + dx) == ground;
                if (flat)
                {
                    var go = new GameObject("Backdrop_" + s.name);
                    go.transform.SetParent(root, false);
                    go.transform.position = new Vector3(cx + 0.5f, ground, 2f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = s;
                    sr.color = data.backdropTint;
                    sr.sortingOrder = -10;
                    index++;
                    x += data.backdropSpacing + ((cx * 31) % 5);
                }
                else x += 2f;
            }
        }

        /// <summary>Height a character stands at in column x (top of the highest ground), or -1 for a pit.</summary>
        static int SurfaceAt(LevelLayout layout, int x)
        {
            if (x < 0 || x >= layout.Width) return -1;
            for (int y = layout.Height - 1; y >= 0; y--)
                if (layout.IsSolid(x, y)) return y + 1;
            return -1;
        }

        GameObject Spawn(GameObject prefab, Vector2 position)
        {
            if (prefab == null)
            {
                Debug.LogError("LevelBuilder: a prefab in LevelPrefabs is missing.");
                return new GameObject("Missing prefab");
            }
            return Instantiate(prefab, position, Quaternion.identity, propsRoot != null ? propsRoot : transform);
        }
    }
}
