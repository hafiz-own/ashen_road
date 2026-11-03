using System.Collections.Generic;
using AshenRoad.Cameras;
using AshenRoad.Core;
using AshenRoad.Data;
using AshenRoad.Enemies;
using AshenRoad.Level;
using AshenRoad.Level.Hazards;
using AshenRoad.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using static AshenRoad.EditorTools.BuilderUtil;

namespace AshenRoad.EditorTools
{
    public static partial class ProjectAssetBuilder
    {
        public const string MenuScenePath = SceneRoot + "/MainMenu.unity";
        public const string LevelScenePath = SceneRoot + "/Level.unity";

        static void CreateScenes()
        {
            EnsureFolder(SceneRoot);
            CreateMenuScene();
            CreateLevelScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(LevelScenePath, true),
            };
        }

        static UnityEngine.Camera CreateCamera(Scene scene, Color background)
        {
            GameObject go = Go("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            go.transform.position = new Vector3(0f, 6f, -10f);
            go.AddComponent<AudioListener>();
            return cam;
        }

        static ParallaxBackground CreateBackground(Transform cameraTransform)
        {
            GameObject bg = Go("Background");
            var parallax = bg.AddComponent<ParallaxBackground>();
            SpriteRenderer sky = SpriteChild(bg.transform, "Sky", null, -100, Vector3.zero);
            SpriteRenderer far = SpriteChild(bg.transform, "Far", null, -90, Vector3.zero);
            SpriteRenderer near = SpriteChild(bg.transform, "Near", null, -80, Vector3.zero);
            Set(parallax).Ref("cameraTransform", cameraTransform).Ref("sky", sky).Ref("far", far).Ref("near", near).Apply();
            return parallax;
        }

        static void CreateMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            UnityEngine.Camera cam = CreateCamera(scene, new Color(0.1f, 0.05f, 0.08f));
            ParallaxBackground bg = CreateBackground(cam.transform);
            var backdrop = cam.gameObject.AddComponent<MenuBackdrop>();
            Set(backdrop).Ref("background", bg).Ref("look", AssetDatabase.LoadAssetAtPath<LevelData>(DataRoot + "/Levels/Level1.asset")).Apply();
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        static void CreateLevelScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            UnityEngine.Camera cam = CreateCamera(scene, new Color(0.08f, 0.05f, 0.07f));
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            ParallaxBackground bg = CreateBackground(cam.transform);

            // tilemaps
            GameObject gridGo = Go("Grid");
            gridGo.AddComponent<Grid>();
            Tilemap solid = CreateTilemap(gridGo.transform, "Solid", 0, false);
            Tilemap platforms = CreateTilemap(gridGo.transform, "Platforms", 0, true);

            // level root
            GameObject level = Go("Level");
            var builder = level.AddComponent<LevelBuilder>();
            var controller = level.AddComponent<LevelController>();
            var factory = level.AddComponent<EnemyFactory>();
            var ambient = level.AddComponent<AmbientDebris>();
            var ending = level.AddComponent<EndingController>();
            GameObject props = Go("Props", level.transform);
            GameObject enemies = Go("Enemies", level.transform);

            GameObject left = Go("LeftWall", level.transform, Layers.Ground);
            var leftCol = left.AddComponent<BoxCollider2D>();
            GameObject right = Go("RightWall", level.transform, Layers.Ground);
            var rightCol = right.AddComponent<BoxCollider2D>();
            GameObject kill = Go("KillZone", level.transform, Layers.Trigger);
            var killCol = kill.AddComponent<BoxCollider2D>();
            killCol.isTrigger = true;
            kill.AddComponent<KillZone>();

            // effects
            GameObject fx = Go("Effects");
            var effects = fx.AddComponent<EffectPlayer>();
            var so = new SerializedObject(effects);
            SerializedProperty list = so.FindProperty("effects");
            var entries = new List<(EffectType, string, int)>
            {
                (EffectType.Dust, "FX_Dust", 6), (EffectType.Sparks, "FX_Sparks", 4), (EffectType.Slash, "FX_Slash", 3),
                (EffectType.Blood, "FX_Blood", 3), (EffectType.Debris, "FX_Debris", 3),
            };
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("type").enumValueIndex = (int)entries[i].Item1;
                e.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PooledParticle>($"{PrefabRoot}/Effects/{entries[i].Item2}.prefab");
                e.FindPropertyRelative("prewarm").intValue = entries[i].Item3;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            Set(builder).Ref("solidMap", solid).Ref("platformMap", platforms).Ref("propsRoot", props.transform)
                .Ref("leftWall", leftCol).Ref("rightWall", rightCol).Ref("killZone", killCol).Apply();
            Set(factory).Ref("container", enemies.transform).Apply();
            Set(controller)
                .Ref("catalog", AssetDatabase.LoadAssetAtPath<LevelCatalog>(DataRoot + "/LevelCatalog.asset"))
                .Ref("prefabs", AssetDatabase.LoadAssetAtPath<LevelPrefabs>(DataRoot + "/LevelPrefabs.asset"))
                .Ref("builder", builder).Ref("factory", factory).Ref("cameraFollow", follow).Ref("background", bg)
                .Ref("ambientDebris", ambient).Ref("ending", ending).Apply();

            EditorSceneManager.SaveScene(scene, LevelScenePath);
        }

        static Tilemap CreateTilemap(Transform grid, string name, int order, bool oneWay)
        {
            GameObject go = Go(name, grid, Layers.Ground);
            var map = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            var col = go.AddComponent<TilemapCollider2D>();
            if (oneWay)
            {
                col.usedByEffector = true;
                var effector = go.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 160f;
                go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            }
            else
            {
                go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                var composite = go.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
                col.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
            return map;
        }
    }
}
