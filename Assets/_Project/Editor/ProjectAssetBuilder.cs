using System.IO;
using AshenRoad.Core;
using AshenRoad.Input;
using UnityEditor;
using UnityEngine;

namespace AshenRoad.EditorTools
{
    /// <summary>
    /// Generates the whole project from code: settings, import settings, input actions, data assets, prefabs and scenes.
    /// Menu: Ashen Road > Rebuild Project Assets. Batch mode: -executeMethod AshenRoad.EditorTools.ProjectAssetBuilder.BuildAll
    /// Level layouts come from Assets/_Project/Levels/levelN.txt, so edit those and rebuild.
    /// </summary>
    public static partial class ProjectAssetBuilder
    {
        public const string Root = "Assets/_Project";
        public const string ArtRoot = Root + "/Art";
        public const string Gen = ArtRoot + "/Generated";
        public const string Kenney = ArtRoot + "/Kenney";
        public const string DataRoot = Root + "/Data";
        public const string PrefabRoot = Root + "/Prefabs";
        public const string SceneRoot = Root + "/Scenes";
        public const string InputPath = Root + "/Input/AshenRoadControls.inputactions";
        public const string SystemsPath = Root + "/Resources/Systems.prefab";

        [MenuItem("Ashen Road/Rebuild Project Assets")]
        public static void BuildAll()
        {
            ConfigureProject();
            ConfigureImports();
            CreateInputAsset();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            CreateData();
            AssetDatabase.SaveAssets();
            CreatePrefabs();
            AssetDatabase.SaveAssets();
            LinkDataToPrefabs();
            CreateSystemsPrefab();
            AssetDatabase.SaveAssets();
            CreateScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Ashen Road: project assets rebuilt.");
        }

        // ------------------------------------------------------------------ project settings

        static void ConfigureProject()
        {
            PlayerSettings.companyName = "EC-334 Group";
            PlayerSettings.productName = "Ashen Road";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.visibleInBackground = true;

            // layers
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 0; i < Layers.Numbers.Length; i++)
                layers.GetArrayElementAtIndex(Layers.Numbers[i]).stringValue = Layers.Names[i];
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            // 2D collision matrix from Layers.ShouldCollide
            for (int a = 0; a < 32; a++)
                for (int b = a; b < 32; b++)
                {
                    bool ours = System.Array.IndexOf(Layers.Numbers, a) >= 0 || System.Array.IndexOf(Layers.Numbers, b) >= 0;
                    if (!ours) continue;
                    Physics2D.IgnoreLayerCollision(a, b, !Layers.ShouldCollide(a, b));
                }
            Physics2D.queriesStartInColliders = false;
            Physics2D.queriesHitTriggers = true;
            Object physics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0];
            EditorUtility.SetDirty(physics);

            // 60 physics steps per second for responsive platforming
            Object time = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset")[0];
            Time.fixedDeltaTime = 1f / 60f;
            EditorUtility.SetDirty(time);

            // new Input System only
            Object ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
            var pso = new SerializedObject(ps);
            SerializedProperty handler = pso.FindProperty("activeInputHandler");
            if (handler != null) handler.intValue = 1;
            pso.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ imports

        static void ConfigureImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool ui = path.Contains("/UI/");
                bool bottomPivot = path.StartsWith(Kenney) || path.Contains("/Props/");
                bool tiled = path.Contains("/Tiles/") || path.Contains("/Backgrounds/") || path.Contains("/Fronts/")
                             || path.EndsWith("arena_wall.png") || path.EndsWith("moving_platform.png") || path.EndsWith("white.png");

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.spritePixelsPerUnit = path.EndsWith("white.png") ? 4 : (ui ? 25 : 16);

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = tiled || ui ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
                settings.spriteAlignment = (int)(bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
                if (path.EndsWith("shadow.png") || path.EndsWith("fire_warn.png") || path.EndsWith("gate_bars.png"))
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                if (path.Contains("/Tiles/")) settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);
                if (ui && (path.EndsWith("panel.png") || path.Contains("button")))
                    importer.spriteBorder = new Vector4(6, 6, 6, 6);
                importer.SaveAndReimport();
            }

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Root + "/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                bool music = path.Contains("/Music/");
                AudioImporterSampleSettings s = importer.defaultSampleSettings;
                s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = music ? 0.6f : 0.8f;
                importer.defaultSampleSettings = s;
                importer.forceToMono = !music;
                importer.SaveAndReimport();
            }
        }

        static void CreateInputAsset()
        {
            BuilderUtil.EnsureFolder(Path.GetDirectoryName(InputPath));
            File.WriteAllText(InputPath, InputBindings.Json);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
