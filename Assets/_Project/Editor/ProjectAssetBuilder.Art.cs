using System.Collections.Generic;
using System.IO;
using System.Linq;
using AshenRoad.Data;
using UnityEditor;
using UnityEngine;

namespace AshenRoad.EditorTools
{
    /// <summary>
    /// The Gothicvania (ansimuz, CC0) look: animated characters, themed tiles, layered backgrounds and scenery.
    /// Files come from Tools/extract_gothicvania.py. Anything missing falls back to the generated art.
    /// </summary>
    public static partial class ProjectAssetBuilder
    {
        public const string GV = ArtRoot + "/Gothicvania";

        static void ConfigureGothicvania(string path, TextureImporter importer, TextureImporterSettings settings)
        {
            string file = Path.GetFileNameWithoutExtension(path);
            if (path.Contains("/Characters/"))
            {
                importer.spritePixelsPerUnit = 32;
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spriteMeshType = SpriteMeshType.FullRect;
            }
            else if (path.Contains("/Effects/"))
            {
                bool fire = file.StartsWith("fire");
                importer.spritePixelsPerUnit = fire ? 64 : 32;
                settings.spriteAlignment = (int)(fire ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            }
            else if (path.Contains("/Tiles/") && file.Contains("_top_"))
            {
                // tall top tiles overhang upward: put the pivot at the centre of their bottom 16 pixels
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                float h = tex != null ? tex.height : 16f;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 8f / h);
            }
            else if (path.Contains("/Backgrounds/"))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
            }
        }

        static Sprite[] Sprites(string folder, string prefix)
        {
            string dir = $"{GV}/{folder}";
            if (!AssetDatabase.IsValidFolder(dir)) return new Sprite[0];
            return AssetDatabase.FindAssets("t:Sprite", new[] { dir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p).StartsWith(prefix))
                .OrderBy(p => int.TryParse(Path.GetFileNameWithoutExtension(p).Split('_').Last(), out int n) ? n : 0)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(s => s != null)
                .ToArray();
        }

        static Sprite GvSprite(string relative)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{GV}/{relative}.png");
        }

        static Sprite[] GvSprites(params string[] relative)
        {
            return relative.Select(GvSprite).Where(s => s != null).ToArray();
        }

        static CharacterArt MakeArt(string name, params (string clip, float fps, bool loop)[] clips)
        {
            var art = BuilderUtil.LoadOrCreate<CharacterArt>($"{DataRoot}/Art/{name}.asset");
            var list = new List<SpriteClip>();
            foreach (var c in clips)
            {
                Sprite[] frames = Sprites("Characters/" + name, c.clip + "_");
                if (frames.Length > 0) list.Add(new SpriteClip { name = c.clip, frames = frames, fps = c.fps, loop = c.loop });
            }
            art.clips = list.ToArray();
            EditorUtility.SetDirty(art);
            return art;
        }

        public static CharacterArt KnightArt => AssetDatabase.LoadAssetAtPath<CharacterArt>($"{DataRoot}/Art/knight.asset");
        public static CharacterArt BossArt => AssetDatabase.LoadAssetAtPath<CharacterArt>($"{DataRoot}/Art/boss.asset");
        public static CharacterArt PrincessArt => AssetDatabase.LoadAssetAtPath<CharacterArt>($"{DataRoot}/Art/princess.asset");

        /// <summary>Runs after the basic data is made: swaps in the animated characters and the themed scenery.</summary>
        static void ApplyGothicvaniaArt(PlayerStats stats, EnemyData shambler, EnemyData runner, EnemyData brute, BossData boss, List<LevelData> levels)
        {
            if (!AssetDatabase.IsValidFolder(GV)) return;

            MakeArt("knight", ("idle", 6f, true), ("run", 12f, true), ("jump", 8f, false), ("attack", 20f, false), ("hurt", 1f, true), ("crouch", 1f, true));
            CharacterArt shamblerArt = MakeArt("shambler", ("walk", 8f, true), ("rise", 10f, false));
            CharacterArt runnerArt = MakeArt("runner", ("walk", 14f, true));
            CharacterArt bruteArt = MakeArt("brute", ("walk", 7f, true));
            MakeArt("boss", ("idle", 7f, true), ("attack", 12f, false));
            MakeArt("princess", ("idle", 6f, true));

            // the knight: a taller sprite and a longer sword
            stats.swordHitboxSize = new Vector2(1.9f, 1.3f);
            stats.swordHitboxOffset = new Vector2(0.95f, 0.65f);
            EditorUtility.SetDirty(stats);

            Restyle(shambler, shamblerArt, Color.white, 1f, new Vector2(0.55f, 1.25f), new Vector2(1.1f, 1.1f), new Vector2(0.55f, 0.6f));
            Restyle(runner, runnerArt, Color.white, 1f, new Vector2(0.55f, 1.2f), new Vector2(0.9f, 1.1f), new Vector2(0.3f, 0.6f));
            Restyle(brute, bruteArt, Color.white, 1f, new Vector2(0.95f, 1.8f), new Vector2(1.8f, 1.8f), new Vector2(0.9f, 0.9f));
            brute.attackRange = 1.9f;

            boss.sprite = GvSprite("Characters/boss/idle_0") ?? boss.sprite;
            boss.tint = Color.white;
            boss.scale = 1f;
            boss.colliderSize = new Vector2(1.5f, 2.8f);
            boss.swingHitboxSize = new Vector2(3f, 2.8f);
            boss.swingHitboxOffset = new Vector2(1.6f, 1.4f);
            boss.swingRange = 2.8f;
            EditorUtility.SetDirty(boss);

            for (int i = 0; i < levels.Count && i < Looks.Length; i++) Look(levels[i], Looks[i]);
        }

        static void Restyle(EnemyData e, CharacterArt art, Color tint, float scale, Vector2 collider, Vector2 hitbox, Vector2 hitboxOffset)
        {
            if (art == null || art.clips.Length == 0) return;
            e.art = art;
            e.sprite = art.clips[0].frames[0];
            e.tint = tint;
            e.scale = scale;
            e.colliderSize = collider;
            e.attackHitboxSize = hitbox;
            e.attackHitboxOffset = hitboxOffset;
            EditorUtility.SetDirty(e);
        }

        struct LevelLook
        {
            public string tiles, sky, far, near;
            public Color skyTint, farTint, nearTint, backdropTint;
            public string[] decor, backdrop;
            public float spacing;
        }

        static readonly Color White = Color.white;

        static readonly LevelLook[] Looks =
        {
            // 1 Burning Village: the town at dusk, houses along the street, fire-tinted sky
            new LevelLook { tiles = "town", sky = "town_sky", far = "town_far", near = null,
                skyTint = new Color(1f, 0.75f, 0.6f), farTint = new Color(0.85f, 0.65f, 0.6f), nearTint = White, backdropTint = new Color(0.8f, 0.7f, 0.7f),
                decor = new[] { "town/barrel", "town/crate", "town/crate-stack", "town/street-lamp", "town/sign", "town/wagon", "town/well" },
                backdrop = new[] { "town/house-a", "town/house-b", "town/house-c" }, spacing = 16f },
            // 2 Broken Bridge: pink canyon, a broken castle and lone houses
            new LevelLook { tiles = "rocky", sky = "rocky_sky", far = "rocky_far", near = null,
                skyTint = White, farTint = new Color(0.85f, 0.8f, 0.85f), nearTint = White, backdropTint = new Color(0.75f, 0.7f, 0.75f),
                decor = new[] { "rocky/plant-1", "rocky/plant-2", "town/crate", "town/barrel" },
                backdrop = new[] { "bridge/castle", "cemetery/tree-3", "town/house-c" }, spacing = 20f },
            // 3 Drowned Fields: the swamp
            new LevelLook { tiles = "swamp", sky = "swamp_sky", far = "swamp_far", near = "swamp_near",
                skyTint = White, farTint = White, nearTint = new Color(0.85f, 0.9f, 0.85f), backdropTint = new Color(0.8f, 0.85f, 0.8f),
                decor = new[] { "swamp/plant-1", "swamp/plant-2", "swamp/plant-3" },
                backdrop = new[] { "cemetery/tree-1", "cemetery/tree-3", "cemetery/tree-2" }, spacing = 15f },
            // 4 Graveyard Gate: moonlit cemetery
            new LevelLook { tiles = "cemetery", sky = "cemetery_sky", far = "cemetery_far", near = "cemetery_near",
                skyTint = White, farTint = White, nearTint = White, backdropTint = new Color(0.85f, 0.85f, 0.95f),
                decor = new[] { "cemetery/stone-1", "cemetery/stone-2", "cemetery/stone-3", "cemetery/stone-4", "cemetery/bush-small", "cemetery/bush-large" },
                backdrop = new[] { "cemetery/tree-3", "cemetery/statue", "cemetery/tree-1", "cemetery/tree-2" }, spacing = 12f },
            // 5 Collapsing Mines: dark stone halls
            new LevelLook { tiles = "dark", sky = "dark_sky", far = null, near = null,
                skyTint = new Color(0.7f, 0.65f, 0.6f), farTint = White, nearTint = White, backdropTint = new Color(0.6f, 0.6f, 0.6f),
                decor = new[] { "town/crate", "town/barrel", "town/crate-stack", "rocky/crystal-1", "rocky/crystal-2" },
                backdrop = new[] { "castle/column" }, spacing = 10f },
            // 6 Storm Cliffs: night mountains and red rock under a storm
            new LevelLook { tiles = "rocky", sky = "night_sky", far = "night_mountains_lights", near = "rocky_near",
                skyTint = new Color(0.75f, 0.8f, 1f), farTint = new Color(0.7f, 0.75f, 0.95f), nearTint = new Color(0.45f, 0.45f, 0.6f), backdropTint = new Color(0.6f, 0.6f, 0.75f),
                decor = new[] { "rocky/plant-1", "rocky/plant-2", "rocky/crystal-1", "rocky/crystal-2" },
                backdrop = new[] { "cemetery/tree-3", "cemetery/tree-1" }, spacing = 22f },
            // 7 Ruined City: leaning, wrecked houses under a burning sky
            new LevelLook { tiles = "horror", sky = "horror_sky", far = "night_far", near = "horror_far",
                skyTint = new Color(1f, 0.75f, 0.6f), farTint = new Color(0.9f, 0.6f, 0.5f), nearTint = new Color(0.85f, 0.7f, 0.65f), backdropTint = new Color(0.85f, 0.75f, 0.7f),
                decor = new[] { "town/barrel", "town/crate", "town/wagon", "horror/brazier", "horror/shrine" },
                backdrop = new[] { "horror/wrecked-house", "horror/brick-house", "town/house-b" }, spacing = 18f },
            // 8 Castle Ramparts
            new LevelLook { tiles = "castle", sky = "castle_sky", far = "night_mountains", near = "night_town",
                skyTint = new Color(0.9f, 0.8f, 0.85f), farTint = new Color(0.6f, 0.55f, 0.65f), nearTint = new Color(0.55f, 0.5f, 0.6f), backdropTint = new Color(0.75f, 0.7f, 0.8f),
                decor = new[] { "town/barrel", "town/crate", "horror/brazier" },
                backdrop = new[] { "castle/column" }, spacing = 9f },
        };

        static void Look(LevelData level, LevelLook look)
        {
            Sprite[] tops = Sprites("Tiles", look.tiles + "_top_");
            if (tops.Length == 0) return;
            level.topTiles = tops;
            level.fillTiles = Sprites("Tiles", look.tiles + "_fill_");
            level.deepTiles = Sprites("Tiles", look.tiles + "_deep_");
            level.topTile = tops[0];
            level.fillTile = level.fillTiles.Length > 0 ? level.fillTiles[0] : level.fillTile;
            level.platformTile = GvSprite($"Tiles/{look.tiles}_platform") ?? level.platformTile;
            level.sky = look.sky != null ? GvSprite("Backgrounds/" + look.sky) : null;
            level.farLayer = look.far != null ? GvSprite("Backgrounds/" + look.far) : null;
            level.nearLayer = look.near != null ? GvSprite("Backgrounds/" + look.near) : null;
            level.skyTint = look.skyTint;
            level.farTint = look.farTint;
            level.nearTint = look.nearTint;
            level.backdropTint = look.backdropTint;
            level.backdropSpacing = look.spacing;
            level.decorations = GvSprites(look.decor.Select(d => "Props/" + d).ToArray());
            level.backdropProps = GvSprites(look.backdrop.Select(d => "Props/" + d).ToArray());
            EditorUtility.SetDirty(level);
        }
    }
}
