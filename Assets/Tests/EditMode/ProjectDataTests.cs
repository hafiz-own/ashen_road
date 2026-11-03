using System.Collections.Generic;
using System.Linq;
using AshenRoad.Combat;
using AshenRoad.Core;
using AshenRoad.Data;
using AshenRoad.EditorTools;
using AshenRoad.Level;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AshenRoad.Tests
{
    /// <summary>Checks the generated assets: every level is valid and finishable, data is wired, bindings exist.</summary>
    public class ProjectDataTests
    {
        static LevelCatalog Catalog => AssetDatabase.LoadAssetAtPath<LevelCatalog>(ProjectAssetBuilder.DataRoot + "/LevelCatalog.asset");

        static IEnumerable<int> LevelIndices() => Enumerable.Range(0, 8);

        [Test]
        public void Catalog_HasEightLevelsInOrder_WithBossLast()
        {
            LevelCatalog c = Catalog;
            Assert.IsNotNull(c);
            Assert.AreEqual(8, c.Count);
            for (int i = 0; i < c.Count; i++)
            {
                Assert.AreEqual(i, c.Get(i).index);
                Assert.AreEqual(i == 7, c.Get(i).isBossLevel, c.Get(i).displayName);
            }
            Assert.IsTrue(c.IsLast(7));
            Assert.IsNull(c.Get(8));
        }

        [TestCaseSource(nameof(LevelIndices))]
        public void Level_LayoutIsValid(int index)
        {
            LevelData level = Catalog.Get(index);
            LevelLayout layout = LevelLayout.Parse(level.layout);
            CollectionAssert.IsEmpty(layout.Validate(level.isBossLevel), level.displayName);
        }

        [TestCaseSource(nameof(LevelIndices))]
        public void Level_IsFinishableFromStartAndEveryCheckpoint(int index)
        {
            LevelCatalog c = Catalog;
            LevelData level = c.Get(index);
            LevelLayout layout = LevelLayout.Parse(level.layout);
            PlayerStats stats = c.playerStats;
            var reach = new LevelReachability(layout, stats.jumpHeight, stats.HorizontalReach, level.movingPlatformRange);
            LevelLayout.Marker goal = layout.First(level.isBossLevel ? 'Q' : 'E');
            var starts = new List<LevelLayout.Marker> { layout.First('P') };
            starts.AddRange(layout.All('C'));
            foreach (LevelLayout.Marker s in starts)
                Assert.IsTrue(reach.CanReach(s.X, s.Y, goal.X, goal.Y), $"{level.displayName}: can't reach the goal from {s}");
        }

        [TestCaseSource(nameof(LevelIndices))]
        public void Level_HasLookFrontAndContent(int index)
        {
            LevelCatalog c = Catalog;
            LevelData level = c.Get(index);
            LevelLayout layout = LevelLayout.Parse(level.layout);
            Assert.IsNotNull(level.topTile);
            Assert.IsNotNull(level.fillTile);
            Assert.IsNotNull(level.platformTile);
            Assert.IsNotNull(level.sky);
            Assert.AreEqual(3, level.frontFrames.Length);
            Assert.IsTrue(level.frontFrames.All(f => f != null));
            Assert.GreaterOrEqual(level.hints.Length, layout.Count('!'), "every sign needs a hint");
            Assert.GreaterOrEqual(layout.Count('C'), 2, "at least two checkpoints");
            Assert.GreaterOrEqual(layout.Count('H'), 2, "at least two hearts");
            foreach (LevelLayout.Marker m in layout.Markers)
                if (LevelLayout.EnemySymbols.IndexOf(m.Symbol) >= 0)
                    Assert.IsNotNull(c.EnemyForSymbol(m.Symbol), $"no enemy for {m.Symbol}");
        }

        [TestCaseSource(nameof(LevelIndices))]
        public void Level_FrontIsSlowerThanTheKnight(int index)
        {
            LevelCatalog c = Catalog;
            LevelData level = c.Get(index);
            float worst = level.frontMaxSpeed;
            Assert.Less(worst, c.playerStats.maxSpeed * 0.6f, "the front must leave time to fight");
            Assert.Greater(level.frontBaseSpeed, 1f, "the front must still be a threat");
            Assert.Greater(level.frontStartOffset, 8f);
        }

        [Test]
        public void NewEnemyTypes_IntroducedInTheRightLevels()
        {
            LevelCatalog c = Catalog;
            int FirstLevelWith(char symbol)
            {
                for (int i = 0; i < c.Count; i++)
                    if (LevelLayout.Parse(c.Get(i).layout).Count(symbol) > 0) return i;
                return -1;
            }
            Assert.AreEqual(0, FirstLevelWith('S'), "Shamblers from level 1");
            Assert.AreEqual(2, FirstLevelWith('R'), "Runners from level 3");
            Assert.AreEqual(3, FirstLevelWith('B'), "Brutes from level 4");
            Assert.AreEqual(5, FirstLevelWith('M'), "moving platforms in level 6");
        }

        [Test]
        public void Enemies_StatsMatchTheDesign()
        {
            LevelCatalog c = Catalog;
            EnemyData s = c.EnemyForSymbol('S'), r = c.EnemyForSymbol('R'), b = c.EnemyForSymbol('B');
            Assert.AreEqual(2, s.maxHealth);
            Assert.AreEqual(1, r.maxHealth);
            Assert.IsTrue(b.IsImmuneTo(DamageType.Sword));
            Assert.IsFalse(b.IsImmuneTo(DamageType.Hazard));
            Assert.IsFalse(s.IsImmuneTo(DamageType.Sword));
            Assert.Greater(r.chaseSpeed, s.chaseSpeed);
            Assert.AreEqual(AttackStyle.Charge, r.attackStyle);
            foreach (EnemyData e in c.enemyTypes)
            {
                Assert.IsNotNull(e.prefab, e.displayName);
                Assert.IsNotNull(e.sprite, e.displayName);
            }
            Assert.IsNotNull(c.boss);
            Assert.AreSame(s, c.boss.summonType);
        }

        [Test]
        public void PlayerStats_JumpMathMatchesTheDesign()
        {
            PlayerStats p = Catalog.playerStats;
            float g = p.Gravity, v = p.JumpVelocity;
            // apex height from v²/2g must equal the designed jump height
            Assert.AreEqual(p.jumpHeight, v * v / (2f * g), 1e-3);
            Assert.AreEqual(p.timeToApex, v / g, 1e-4);
            Assert.Greater(p.HorizontalReach(0f), 4f, "a full jump must clear a 3-tile gap");
            Assert.AreEqual(0f, p.HorizontalReach(p.jumpHeight + 1f));
        }

        [Test]
        public void LevelPrefabs_AreAllAssigned()
        {
            var prefabs = AssetDatabase.LoadAssetAtPath<LevelPrefabs>(ProjectAssetBuilder.DataRoot + "/LevelPrefabs.asset");
            foreach (var field in typeof(LevelPrefabs).GetFields())
            {
                if (field.FieldType != typeof(GameObject)) continue;
                Assert.IsNotNull(field.GetValue(prefabs), field.Name);
            }
        }

        [Test]
        public void Systems_PrefabHasEveryManager()
        {
            var systems = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetBuilder.SystemsPath);
            Assert.IsNotNull(systems);
            Assert.IsNotNull(systems.GetComponent<GameManager>());
            Assert.IsNotNull(systems.GetComponent<Input.InputReader>());
            Assert.IsNotNull(systems.GetComponent<Audio.AudioManager>());
            Assert.IsNotNull(systems.GetComponentInChildren<UI.UIFlowController>());
            Assert.IsNotNull(systems.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>());
            Assert.IsNotNull(Resources.Load<GameObject>(GameBootstrap.SystemsResource));
        }

        [Test]
        public void BuildSettings_ContainMenuThenLevel()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.AreEqual(2, scenes.Length);
            StringAssert.EndsWith("MainMenu.unity", scenes[0].path);
            StringAssert.EndsWith("Level.unity", scenes[1].path);
        }

        [Test]
        public void InputActions_HaveEveryActionAndTheAgreedKeys()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectAssetBuilder.InputPath);
            Assert.IsNotNull(asset);
            string[] actions = { "Gameplay/Move", "Gameplay/Jump", "Gameplay/Roll", "Gameplay/Attack", "Gameplay/Pause",
                "UI/Navigate", "UI/Submit", "UI/Cancel", "UI/Point", "UI/Click" };
            foreach (string a in actions) Assert.IsNotNull(asset.FindAction(a), a);

            List<string> Paths(string action) => asset.FindAction(action).bindings.Select(b => b.path).ToList();
            CollectionAssert.IsSubsetOf(new[] { "<Keyboard>/j", "<Keyboard>/0", "<Keyboard>/numpad0", "<Mouse>/leftButton" }, Paths("Gameplay/Attack"));
            CollectionAssert.IsSubsetOf(new[] { "<Keyboard>/space", "<Keyboard>/w", "<Keyboard>/upArrow" }, Paths("Gameplay/Jump"));
            CollectionAssert.Contains(Paths("Gameplay/Roll"), "<Keyboard>/leftShift");
            CollectionAssert.Contains(Paths("Gameplay/Pause"), "<Keyboard>/escape");
            CollectionAssert.IsSubsetOf(new[] { "<Keyboard>/a", "<Keyboard>/d", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow" }, Paths("Gameplay/Move"));
        }

        [Test]
        public void AudioLibrary_HasEverySoundTheGameAsksFor()
        {
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(ProjectAssetBuilder.DataRoot + "/AudioLibrary.asset");
            string[] sounds =
            {
                "jump", "land", "roll", "swing", "footstep", "hit", "enemy_hit", "clang", "hurt", "player_death", "enemy_death",
                "enemy_attack", "slam", "boss_roar", "boss_charge", "summon", "debris", "fire", "rumble", "flood", "thunder",
                "checkpoint", "heart", "level_complete", "game_over", "gate_open", "ui_move", "ui_click"
            };
            foreach (string id in sounds)
            {
                AudioLibrary.Sound s = lib.FindSound(id);
                Assert.IsNotNull(s, id);
                Assert.IsTrue(s.clips.Length > 0 && s.clips.All(c => c != null), id);
            }
            foreach (string m in new[] { "title", "level_a", "level_b", "level_c", "boss", "ending" })
                Assert.IsNotNull(lib.FindMusic(m)?.clip, m);
            foreach (LevelData level in Catalog.levels)
                Assert.IsNotNull(lib.FindMusic(level.musicId), level.displayName);
        }

        [Test]
        public void Layers_CollideOnlyWhereTheDesignSays()
        {
            Assert.IsTrue(Layers.ShouldCollide(Layers.Player, Layers.Ground));
            Assert.IsFalse(Layers.ShouldCollide(Layers.Player, Layers.Enemy), "contact damage, not body blocking");
            Assert.IsFalse(Layers.ShouldCollide(Layers.PlayerRolling, Layers.Enemy), "rolling passes through enemies");
            Assert.IsTrue(Layers.ShouldCollide(Layers.PlayerRolling, Layers.Trigger));
            Assert.IsTrue(Layers.ShouldCollide(Layers.Corpse, Layers.Ground));
            Assert.IsFalse(Layers.ShouldCollide(Layers.Corpse, Layers.Player));
            Assert.AreEqual(Layers.ShouldCollide(Layers.Player, Layers.Ground), !Physics2D.GetIgnoreLayerCollision(Layers.Player, Layers.Ground));
            Assert.AreEqual(Layers.ShouldCollide(Layers.Player, Layers.Enemy), !Physics2D.GetIgnoreLayerCollision(Layers.Player, Layers.Enemy));
        }
    }
}
