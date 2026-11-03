using System.Collections.Generic;
using System.IO;
using AshenRoad.Combat;
using AshenRoad.Data;
using AshenRoad.UI;
using UnityEditor;
using UnityEngine;

namespace AshenRoad.EditorTools
{
    public static partial class ProjectAssetBuilder
    {
        public const string LevelsFolder = Root + "/Levels";

        struct LevelSpec
        {
            public string name, subtitle, theme, front, frontName, music;
            public DisasterType disaster;
            public float baseSpeed, ramp, maxSpeed;
            public bool boss, ambientDebris;
            public float debrisInterval, fireOn, fireOff, par;
            public Color glow;
            public string[] hints;
        }

        static readonly LevelSpec[] Specs =
        {
            new LevelSpec { name = "Burning Village", subtitle = "Where the Ashfall began its march", theme = "village", front = "fire", frontName = "wall of fire",
                disaster = DisasterType.Fire, baseSpeed = 2.2f, ramp = 0.008f, maxSpeed = 3.4f, music = "level_a", fireOn = 1.4f, fireOff = 2.2f, par = 120f,
                glow = new Color(1f, 0.45f, 0.1f, 0.35f),
                hints = new[] { "A/D or Arrows to run. The fire is coming: keep moving!", "J, 0 or Left Click swings your sword.", "Space jumps. Hold it to jump higher." } },
            new LevelSpec { name = "Broken Bridge", subtitle = "The old crossing gives way", theme = "bridge", front = "collapse", frontName = "collapsing bridge",
                disaster = DisasterType.Collapse, baseSpeed = 2.5f, ramp = 0.009f, maxSpeed = 3.7f, music = "level_b", fireOn = 1.4f, fireOff = 2f, par = 130f,
                glow = new Color(0.8f, 0.7f, 0.55f, 0.3f),
                hints = new[] { "The bridge is falling apart behind you.", "Shift to roll: you can't be hurt mid-roll, and you pass through the undead." } },
            new LevelSpec { name = "Drowned Fields", subtitle = "The river has broken its banks", theme = "fields", front = "flood", frontName = "rising flood",
                disaster = DisasterType.Flood, baseSpeed = 2.7f, ramp = 0.009f, maxSpeed = 3.9f, music = "level_c", fireOn = 1.4f, fireOff = 2f, par = 140f,
                glow = new Color(0.3f, 0.7f, 1f, 0.3f),
                hints = new[] { "The flood rises from the west.", "Runners charge when they see you. Dodge, then strike." } },
            new LevelSpec { name = "Graveyard Gate", subtitle = "The dead do not rest here", theme = "graveyard", front = "fissure", frontName = "opening fissures",
                disaster = DisasterType.Fissure, baseSpeed = 2.8f, ramp = 0.01f, maxSpeed = 4f, music = "level_a", fireOn = 1.6f, fireOff = 1.8f, par = 140f,
                glow = new Color(1f, 0.4f, 0.15f, 0.3f),
                hints = new[] { "The ground splits open behind you.", "Brutes shrug off your sword. Jump over them or roll through.", "Lure Brutes into the flames to destroy them." } },
            new LevelSpec { name = "Collapsing Mines", subtitle = "Timber groans overhead", theme = "mines", front = "cavein", frontName = "cave-in",
                disaster = DisasterType.CaveIn, baseSpeed = 2.9f, ramp = 0.01f, maxSpeed = 4.1f, music = "level_b", ambientDebris = true, debrisInterval = 3.2f,
                fireOn = 1.5f, fireOff = 1.8f, par = 150f, glow = new Color(0.7f, 0.6f, 0.5f, 0.3f),
                hints = new[] { "The mine is caving in. Watch the roof for falling rocks." } },
            new LevelSpec { name = "Storm Cliffs", subtitle = "Lightning walks the high road", theme = "cliffs", front = "lightning", frontName = "lightning storm",
                disaster = DisasterType.Lightning, baseSpeed = 3.0f, ramp = 0.011f, maxSpeed = 4.3f, music = "level_c", fireOn = 1.5f, fireOff = 1.8f, par = 150f,
                glow = new Color(0.6f, 0.7f, 1f, 0.35f),
                hints = new[] { "Lightning chases you along the cliffs.", "Ride the moving platforms across the gaps." } },
            new LevelSpec { name = "Ruined City", subtitle = "Fire and stone fall together", theme = "city", front = "firedebris", frontName = "firestorm",
                disaster = DisasterType.FireAndDebris, baseSpeed = 3.1f, ramp = 0.012f, maxSpeed = 4.5f, music = "level_a", ambientDebris = true, debrisInterval = 4f,
                fireOn = 1.7f, fireOff = 1.6f, par = 160f, glow = new Color(1f, 0.5f, 0.2f, 0.35f), hints = new string[0] },
            new LevelSpec { name = "Castle Ramparts", subtitle = "The Ash Warden waits", theme = "castle", front = "crumble", frontName = "crumbling wall",
                disaster = DisasterType.Crumble, baseSpeed = 2.8f, ramp = 0.01f, maxSpeed = 4f, music = "level_b", boss = true, fireOn = 1.5f, fireOff = 1.8f, par = 180f,
                glow = new Color(0.75f, 0.75f, 0.85f, 0.3f), hints = new string[0] },
        };

        static void CreateData()
        {
            // knight
            var stats = BuilderUtil.LoadOrCreate<PlayerStats>(DataRoot + "/PlayerStats.asset");
            EditorUtility.SetDirty(stats);

            // enemies
            var shambler = BuilderUtil.LoadOrCreate<EnemyData>(DataRoot + "/Enemies/Shambler.asset");
            shambler.displayName = "Shambler";
            shambler.layoutSymbol = 'S';
            shambler.sprite = BuilderUtil.Sprite(Kenney + "/shambler.png");
            shambler.tint = new Color(0.62f, 0.85f, 0.55f);
            shambler.scale = 1f;
            shambler.colliderSize = new Vector2(0.6f, 0.9f);
            shambler.maxHealth = 2;
            shambler.patrolSpeed = 0.8f;
            shambler.chaseSpeed = 1.5f;
            shambler.sightRange = 5f;
            shambler.attackStyle = AttackStyle.Swipe;
            shambler.attackRange = 1.1f;
            shambler.attackTelegraph = 0.5f;
            shambler.attackActive = 0.15f;
            shambler.attackRecovery = 0.9f;
            shambler.attackHitboxSize = new Vector2(1.1f, 1f);
            shambler.attackHitboxOffset = new Vector2(0.55f, 0.5f);
            shambler.attackKnockback = 6f;
            shambler.contactDamage = 1;
            shambler.immunities = new DamageType[0];
            shambler.stunDuration = 0.35f;
            shambler.knockbackResistance = 0f;
            EditorUtility.SetDirty(shambler);

            var runner = BuilderUtil.LoadOrCreate<EnemyData>(DataRoot + "/Enemies/Runner.asset");
            runner.displayName = "Runner";
            runner.layoutSymbol = 'R';
            runner.sprite = BuilderUtil.Sprite(Kenney + "/runner.png");
            runner.tint = new Color(0.85f, 0.8f, 0.5f);
            runner.scale = 1f;
            runner.colliderSize = new Vector2(0.6f, 0.85f);
            runner.maxHealth = 1;
            runner.patrolSpeed = 1.5f;
            runner.chaseSpeed = 4.5f;
            runner.sightRange = 9f;
            runner.loseTargetTime = 1.5f;
            runner.attackStyle = AttackStyle.Charge;
            runner.attackRange = 5f;
            runner.attackTelegraph = 0.45f;
            runner.attackActive = 0.6f;
            runner.attackRecovery = 1.0f;
            runner.chargeSpeed = 9f;
            runner.attackHitboxSize = new Vector2(0.9f, 0.9f);
            runner.attackHitboxOffset = new Vector2(0.3f, 0.45f);
            runner.attackKnockback = 7f;
            runner.contactDamage = 1;
            runner.immunities = new DamageType[0];
            runner.stunDuration = 0.25f;
            EditorUtility.SetDirty(runner);

            var brute = BuilderUtil.LoadOrCreate<EnemyData>(DataRoot + "/Enemies/Brute.asset");
            brute.displayName = "Brute";
            brute.layoutSymbol = 'B';
            brute.sprite = BuilderUtil.Sprite(Kenney + "/brute.png");
            brute.tint = new Color(0.6f, 0.62f, 0.75f);
            brute.scale = 1.5f;
            brute.colliderSize = new Vector2(0.65f, 0.9f);
            brute.maxHealth = 5;
            brute.patrolSpeed = 0.7f;
            brute.chaseSpeed = 2f;
            brute.sightRange = 6f;
            brute.attackStyle = AttackStyle.Slam;
            brute.attackRange = 1.5f;
            brute.attackTelegraph = 0.8f;
            brute.attackActive = 0.2f;
            brute.attackRecovery = 1.1f;
            brute.attackHitboxSize = new Vector2(1.6f, 1.4f);
            brute.attackHitboxOffset = new Vector2(0.6f, 0.45f);
            brute.attackKnockback = 10f;
            brute.contactDamage = 1;
            brute.contactKnockback = 9f;
            brute.immunities = new[] { DamageType.Sword };
            brute.stunDuration = 0.2f;
            brute.knockbackResistance = 0.8f;
            brute.despawnDelay = 1.5f;
            EditorUtility.SetDirty(brute);

            var boss = BuilderUtil.LoadOrCreate<BossData>(DataRoot + "/Boss/AshWarden.asset");
            boss.displayName = "The Ash Warden";
            boss.sprite = BuilderUtil.Sprite(Kenney + "/boss.png");
            boss.tint = new Color(0.85f, 0.75f, 0.8f);
            boss.scale = 2.5f;
            boss.colliderSize = new Vector2(0.6f, 0.9f);
            boss.summonType = shambler;
            EditorUtility.SetDirty(boss);

            // levels
            var levels = new List<LevelData>();
            for (int i = 0; i < Specs.Length; i++)
            {
                LevelSpec s = Specs[i];
                var level = BuilderUtil.LoadOrCreate<LevelData>($"{DataRoot}/Levels/Level{i + 1}.asset");
                level.index = i;
                level.displayName = s.name;
                level.subtitle = s.subtitle;
                level.isBossLevel = s.boss;
                string layoutPath = $"{LevelsFolder}/level{i + 1}.txt";
                level.layout = File.Exists(layoutPath) ? File.ReadAllText(layoutPath) : "";
                level.hints = s.hints;
                level.topTile = BuilderUtil.Sprite($"{Gen}/Tiles/{s.theme}_top.png");
                level.fillTile = BuilderUtil.Sprite($"{Gen}/Tiles/{s.theme}_fill.png");
                level.platformTile = BuilderUtil.Sprite($"{Gen}/Tiles/{s.theme}_platform.png");
                level.sky = BuilderUtil.Sprite($"{Gen}/Backgrounds/{s.theme}_sky.png");
                level.farLayer = BuilderUtil.Sprite($"{Gen}/Backgrounds/{s.theme}_far.png");
                level.nearLayer = BuilderUtil.Sprite($"{Gen}/Backgrounds/{s.theme}_near.png");
                level.disaster = s.disaster;
                level.frontName = s.frontName;
                level.frontFrames = new[]
                {
                    BuilderUtil.Sprite($"{Gen}/Fronts/front_{s.front}_0.png"),
                    BuilderUtil.Sprite($"{Gen}/Fronts/front_{s.front}_1.png"),
                    BuilderUtil.Sprite($"{Gen}/Fronts/front_{s.front}_2.png"),
                };
                level.frontGlow = s.glow;
                level.frontStartOffset = 14f;
                level.respawnFrontOffset = 14f;
                level.frontBaseSpeed = s.baseSpeed;
                level.frontRamp = s.ramp;
                level.frontMaxSpeed = s.maxSpeed;
                level.frontCatchupDistance = 24f;
                level.frontCatchupBonus = 2.5f;
                level.fireCycleOn = s.fireOn;
                level.fireCycleOff = s.fireOff;
                level.ambientDebris = s.ambientDebris;
                level.ambientDebrisInterval = s.debrisInterval > 0f ? s.debrisInterval : 3.5f;
                level.movingPlatformRange = 4f;
                level.movingPlatformSpeed = 2.2f;
                level.musicId = s.music;
                level.parTime = s.par;
                EditorUtility.SetDirty(level);
                levels.Add(level);
            }

            ApplyGothicvaniaArt(stats, shambler, runner, brute, boss, levels);

            var catalog = BuilderUtil.LoadOrCreate<LevelCatalog>(DataRoot + "/LevelCatalog.asset");
            catalog.levels = levels;
            catalog.enemyTypes = new[] { shambler, runner, brute };
            catalog.boss = boss;
            catalog.playerStats = stats;
            EditorUtility.SetDirty(catalog);

            CreateAudioLibrary();
            CreateUIStyle();
        }

        static void CreateAudioLibrary()
        {
            const string kenney = Root + "/Audio/Sfx/Kenney/";
            const string gen = Root + "/Audio/Sfx/Generated/";
            const string music = Root + "/Audio/Music/";
            var lib = BuilderUtil.LoadOrCreate<AudioLibrary>(DataRoot + "/AudioLibrary.asset");
            lib.sounds = new List<AudioLibrary.Sound>
            {
                Sound("jump", 0.55f, gen + "jump.wav"),
                Sound("land", 0.45f, gen + "land.wav"),
                Sound("roll", 0.6f, gen + "roll.wav", kenney + "cloth1.ogg"),
                Sound("swing", 0.8f, kenney + "drawKnife1.ogg", kenney + "drawKnife2.ogg", kenney + "drawKnife3.ogg"),
                Sound("footstep", 0.35f, kenney + "footstep00.ogg", kenney + "footstep01.ogg", kenney + "footstep02.ogg", kenney + "footstep03.ogg"),
                Sound("hit", 0.9f, kenney + "knifeSlice.ogg", kenney + "knifeSlice2.ogg"),
                Sound("enemy_hit", 0.6f, kenney + "chop.ogg"),
                Sound("clang", 0.9f, kenney + "metalPot1.ogg", kenney + "metalPot2.ogg"),
                Sound("hurt", 0.8f, gen + "hurt.wav"),
                Sound("player_death", 0.8f, gen + "player_death.wav"),
                Sound("enemy_death", 0.6f, gen + "enemy_death.wav"),
                Sound("enemy_attack", 0.5f, gen + "enemy_attack.wav"),
                Sound("slam", 0.7f, gen + "debris_crash.wav"),
                Sound("boss_roar", 0.9f, gen + "boss_roar.wav"),
                Sound("boss_charge", 0.6f, gen + "enemy_attack.wav"),
                Sound("summon", 0.5f, gen + "fire.wav"),
                Sound("debris", 0.6f, gen + "debris_crash.wav"),
                Sound("fire", 0.35f, gen + "fire.wav"),
                Sound("rumble", 0.3f, gen + "debris_crash.wav"),
                Sound("flood", 0.35f, gen + "roll.wav"),
                Sound("thunder", 0.5f, gen + "thunder.wav"),
                Sound("checkpoint", 0.7f, gen + "checkpoint.wav"),
                Sound("heart", 0.7f, gen + "heart.wav"),
                Sound("level_complete", 0.8f, gen + "level_complete.wav"),
                Sound("game_over", 0.8f, gen + "game_over.wav"),
                Sound("gate_open", 0.7f, kenney + "doorOpen_1.ogg"),
                Sound("ui_move", 0.5f, kenney + "select_001.ogg"),
                Sound("ui_click", 0.7f, kenney + "click_001.ogg"),
                Sound("ui_back", 0.6f, kenney + "back_001.ogg"),
            };
            lib.music = new List<AudioLibrary.Music>
            {
                Music("title", music + "title.ogg", 0.55f),
                Music("level_a", music + "level_a.ogg", 0.45f),
                Music("level_b", music + "level_b.ogg", 0.45f),
                Music("level_c", music + "level_c.ogg", 0.45f),
                Music("boss", music + "level_c.ogg", 0.55f),
                Music("ending", music + "ending.ogg", 0.6f),
            };
            EditorUtility.SetDirty(lib);
        }

        static AudioLibrary.Sound Sound(string id, float volume, params string[] paths)
        {
            var clips = new List<AudioClip>();
            foreach (string p in paths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                if (clip != null) clips.Add(clip);
                else Debug.LogError("Builder: missing audio " + p);
            }
            float interval = id == "footstep" ? 0.12f : id == "ui_move" ? 0.05f : 0.03f;
            return new AudioLibrary.Sound { id = id, clips = clips.ToArray(), volume = volume, pitchVariance = 0.06f, minInterval = interval };
        }

        static AudioLibrary.Music Music(string id, string path, float volume)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogError("Builder: missing music " + path);
            return new AudioLibrary.Music { id = id, clip = clip, volume = volume, loop = true };
        }

        static void CreateUIStyle()
        {
            var style = BuilderUtil.LoadOrCreate<UIStyle>(DataRoot + "/UIStyle.asset");
            style.font = AssetDatabase.LoadAssetAtPath<Font>(ArtRoot + "/Fonts/kenpixel.ttf");
            style.titleFont = AssetDatabase.LoadAssetAtPath<Font>(ArtRoot + "/Fonts/kenpixel_high.ttf");
            style.panel = BuilderUtil.Sprite(Gen + "/UI/panel.png");
            style.button = BuilderUtil.Sprite(Gen + "/UI/button.png");
            style.buttonHighlight = BuilderUtil.Sprite(Gen + "/UI/button_hover.png");
            style.heartFull = BuilderUtil.Sprite(Gen + "/UI/heart_full.png");
            style.heartEmpty = BuilderUtil.Sprite(Gen + "/UI/heart_empty.png");
            style.white = BuilderUtil.Sprite(Gen + "/Effects/white.png");
            EditorUtility.SetDirty(style);
        }
    }
}
