using AshenRoad.Audio;
using AshenRoad.Boss;
using AshenRoad.Combat;
using AshenRoad.Core;
using AshenRoad.Data;
using AshenRoad.Enemies;
using AshenRoad.Input;
using AshenRoad.Level;
using AshenRoad.Level.Hazards;
using AshenRoad.Player;
using AshenRoad.UI;
using AshenRoad.Visual;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static AshenRoad.EditorTools.BuilderUtil;

namespace AshenRoad.EditorTools
{
    public static partial class ProjectAssetBuilder
    {
        // sorting orders
        const int OrderDecoration = 2, OrderProps = 5, OrderPickup = 6, OrderEnemy = 10, OrderBoss = 11, OrderPrincess = 12,
            OrderPlayer = 20, OrderSword = 21, OrderFront = 30, OrderEffects = 40;

        static PhysicsMaterial2D frictionless;

        static void CreatePrefabs()
        {
            EnsureFolder(PrefabRoot);
            frictionless = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(DataRoot + "/Frictionless.physicsMaterial2D");
            if (frictionless == null)
            {
                frictionless = new PhysicsMaterial2D("Frictionless") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(frictionless, DataRoot + "/Frictionless.physicsMaterial2D");
            }

            CreatePlayerPrefab();
            CreateEnemyPrefab();
            CreateBossPrefab();
            CreatePropPrefabs();
            CreateEffectPrefabs();
        }

        // ------------------------------------------------------------------ knight

        static void CreatePlayerPrefab()
        {
            GameObject root = Go("Knight", null, Layers.Player);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sharedMaterial = frictionless;
            var capsule = root.AddComponent<CapsuleCollider2D>();
            bool animated = KnightArt != null && KnightArt.clips.Length > 0;
            capsule.size = animated ? new Vector2(0.6f, 1.25f) : new Vector2(0.6f, 0.9f);
            capsule.offset = new Vector2(0f, capsule.size.y * 0.5f + 0.01f);
            capsule.sharedMaterial = frictionless;

            Health health = root.AddComponent<Health>();
            var movement = root.AddComponent<PlayerMovement>();
            var combat = root.AddComponent<PlayerCombat>();
            var controller = root.AddComponent<PlayerController>();

            SpriteRenderer body = SpriteChild(root.transform, "Visual", Sprite(Kenney + "/knight.png"), OrderPlayer, Vector3.zero);
            SpriteRenderer sword = SpriteChild(root.transform, "Sword", Sprite(Kenney + "/sword.png"), OrderSword, new Vector3(0.3f, 0.42f, 0f));
            GameObject hitboxGo = Go("SwordHitbox", root.transform, Layers.Player);
            var hitbox = hitboxGo.AddComponent<Hitbox>();

            var animator = root.AddComponent<PlayerAnimator>();
            SpriteAnimator knightFrames = null;
            if (animated)
            {
                knightFrames = body.gameObject.AddComponent<SpriteAnimator>();
                Set(knightFrames).Ref("target", body).Ref("art", KnightArt).Apply();
                body.sprite = KnightArt.Get("idle").frames[0];
            }

            Set(movement).Ref("body", capsule).Apply();
            Set(combat).Ref("swordHitbox", hitbox).Apply();
            Set(controller).Ref("stats", AssetDatabase.LoadAssetAtPath<PlayerStats>(DataRoot + "/PlayerStats.asset")).Apply();
            Set(animator).Ref("player", controller).Ref("body", body.transform).Ref("bodyRenderer", body).Ref("sword", sword.transform).Ref("frames", knightFrames).Apply();
            Set(health).Int("maxHealth", 3).Apply();
            SavePrefab(root, PrefabRoot + "/Knight.prefab");
        }

        // ------------------------------------------------------------------ enemies

        static void CreateEnemyPrefab()
        {
            GameObject root = Go("Enemy", null, Layers.Enemy);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sharedMaterial = frictionless;
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 0.9f);
            box.offset = new Vector2(0f, 0.45f);
            box.sharedMaterial = frictionless;

            root.AddComponent<Health>();
            var motor = root.AddComponent<EnemyMotor>();
            root.AddComponent<EnemyPerception>();
            var contact = root.AddComponent<ContactDamage>();
            var ai = root.AddComponent<EnemyAI>();

            SpriteRenderer body = SpriteChild(root.transform, "Visual", Sprite(Kenney + "/shambler.png"), OrderEnemy, Vector3.zero);
            var hitbox = Go("AttackHitbox", root.transform, Layers.Enemy).AddComponent<Hitbox>();
            var animator = root.AddComponent<EnemyAnimator>();
            var enemyFrames = body.gameObject.AddComponent<SpriteAnimator>();
            Set(enemyFrames).Ref("target", body).Apply();

            Set(motor).Ref("body", box).Apply();
            Set(contact).Ref("body", box).Apply();
            Set(ai).Ref("attackHitbox", hitbox).Ref("contactDamage", contact).Ref("visual", body).Ref("body", box).Apply();
            Set(animator).Ref("enemy", ai).Ref("body", body.transform).Ref("bodyRenderer", body).Ref("frames", enemyFrames).Apply();
            SavePrefab(root, PrefabRoot + "/Enemy.prefab");
        }

        static void CreateBossPrefab()
        {
            GameObject root = Go("AshWarden", null, Layers.Enemy);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.mass = 20f;
            rb.sharedMaterial = frictionless;
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 0.9f);
            box.offset = new Vector2(0f, 0.45f);
            box.sharedMaterial = frictionless;

            root.AddComponent<Health>();
            var motor = root.AddComponent<EnemyMotor>();
            root.AddComponent<EnemyPerception>();
            var contact = root.AddComponent<ContactDamage>();
            var boss = root.AddComponent<BossController>();

            SpriteRenderer body = SpriteChild(root.transform, "Visual", Sprite(Kenney + "/boss.png"), OrderBoss, Vector3.zero);
            var swing = Go("SwingHitbox", root.transform, Layers.Enemy).AddComponent<Hitbox>();
            var charge = Go("ChargeHitbox", root.transform, Layers.Enemy).AddComponent<Hitbox>();
            var animator = root.AddComponent<BossAnimator>();
            SpriteAnimator bossFrames = null;
            if (BossArt != null && BossArt.clips.Length > 0)
            {
                bossFrames = body.gameObject.AddComponent<SpriteAnimator>();
                Set(bossFrames).Ref("target", body).Ref("art", BossArt).Apply();
            }

            Set(motor).Ref("body", box).Apply();
            Set(contact).Ref("body", box).Apply();
            Set(boss).Ref("swingHitbox", swing).Ref("chargeHitbox", charge).Ref("contactDamage", contact).Ref("visual", body).Ref("body", box).Apply();
            Set(animator).Ref("boss", boss).Ref("body", body.transform).Ref("bodyRenderer", body).Ref("frames", bossFrames).Apply();
            SavePrefab(root, PrefabRoot + "/AshWarden.prefab");
        }

        // ------------------------------------------------------------------ props

        static void CreatePropPrefabs()
        {
            // princess (with a cage that lifts in the ending)
            {
                GameObject root = Go("Princess");
                root.AddComponent<Princess>();
                SpriteRenderer body = SpriteChild(root.transform, "Visual", Sprite(Kenney + "/princess.png"), OrderPrincess, Vector3.zero);
                SpriteRenderer cage = SpriteChild(root.transform, "Cage", Sprite(Gen + "/Props/gate_bars.png"), OrderPrincess + 1, new Vector3(0f, 1.3f, 0f));
                cage.transform.localScale = new Vector3(1.1f, 1f, 1f);
                SpriteAnimator princessFrames = null;
                if (PrincessArt != null && PrincessArt.clips.Length > 0)
                {
                    princessFrames = body.gameObject.AddComponent<SpriteAnimator>();
                    Set(princessFrames).Ref("target", body).Ref("art", PrincessArt).Apply();
                    body.sprite = PrincessArt.clips[0].frames[0];
                    cage.transform.localScale = new Vector3(1.2f, 1.1f, 1f);
                }
                Set(root.GetComponent<Princess>()).Ref("body", body.transform).Ref("frames", princessFrames).Apply();
                SavePrefab(root, PrefabRoot + "/Princess.prefab");
            }
            // checkpoint
            {
                GameObject root = Go("Checkpoint", null, Layers.Trigger);
                var col = root.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1.2f, 2.5f);
                col.offset = new Vector2(0f, 1.25f);
                SpriteRenderer flag = SpriteChild(root.transform, "Flag", Sprite(Gen + "/Props/checkpoint_off.png"), OrderProps, Vector3.zero);
                var cp = root.AddComponent<Checkpoint>();
                Set(cp).Ref("flag", flag).Ref("offSprite", Sprite(Gen + "/Props/checkpoint_off.png")).Ref("onSprite", Sprite(Gen + "/Props/checkpoint_on.png")).Apply();
                SavePrefab(root, PrefabRoot + "/Checkpoint.prefab");
            }
            // exit gate
            {
                GameObject root = Go("ExitGate", null, Layers.Trigger);
                var col = root.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1f, 3f);
                col.offset = new Vector2(0f, 1.5f);
                SpriteChild(root.transform, "Frame", Sprite(Gen + "/Props/gate_frame.png"), OrderProps - 1, Vector3.zero);
                SpriteRenderer bars = SpriteChild(root.transform, "Bars", Sprite(Gen + "/Props/gate_bars.png"), OrderProps, new Vector3(0f, 1.3f, 0f));
                var gate = root.AddComponent<ExitGate>();
                Set(gate).Ref("bars", bars.transform).Apply();
                SavePrefab(root, PrefabRoot + "/ExitGate.prefab");
            }
            // heart pickup
            {
                GameObject root = Go("HeartPickup", null, Layers.Trigger);
                var col = root.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.45f;
                col.offset = new Vector2(0f, 0.5f);
                SpriteRenderer v = SpriteChild(root.transform, "Visual", Sprite(Kenney + "/heart_potion.png"), OrderPickup, new Vector3(0f, 0.1f, 0f));
                var heart = root.AddComponent<HeartPickup>();
                Set(heart).Ref("visual", v.transform).Apply();
                SavePrefab(root, PrefabRoot + "/HeartPickup.prefab");
            }
            // fire patch
            {
                GameObject root = Go("FirePatch");
                SpriteChild(root.transform, "Embers", Sprite(Gen + "/Props/fire_base.png"), OrderProps - 1, Vector3.zero);
                SpriteRenderer flames = SpriteChild(root.transform, "Flames", Sprite(Gen + "/Props/fire_0.png"), OrderProps + 2, Vector3.zero);
                SpriteRenderer warn = SpriteChild(root.transform, "Warning", Sprite(Gen + "/Props/fire_warn.png"), OrderProps + 1, new Vector3(0f, 0.25f, 0f));
                var hitbox = Go("Hitbox", root.transform).AddComponent<Hitbox>();
                Set(hitbox).Vec2("size", new Vector2(0.9f, 1.3f)).Vec2("offset", new Vector2(0f, 0.65f)).Apply();
                var fire = root.AddComponent<FirePatch>();
                Sprite[] gvFire = Sprites("Effects", "fire_");
                Object[] fireFrames = gvFire.Length > 0 ? gvFire.Cast<Object>().ToArray()
                    : new Object[] { Sprite(Gen + "/Props/fire_0.png"), Sprite(Gen + "/Props/fire_1.png"), Sprite(Gen + "/Props/fire_2.png") };
                if (gvFire.Length > 0) flames.sprite = gvFire[0];
                Set(fire).Ref("hitbox", hitbox).Ref("flames", flames).Ref("warning", warn)
                    .Refs("flameFrames", fireFrames)
                    .Float("knockback", 5f).Apply();
                SavePrefab(root, PrefabRoot + "/FirePatch.prefab");
            }
            // spikes
            {
                GameObject root = Go("Spikes");
                SpriteChild(root.transform, "Visual", Sprite(Gen + "/Props/spikes.png"), OrderProps, Vector3.zero);
                var hitbox = Go("Hitbox", root.transform).AddComponent<Hitbox>();
                Set(hitbox).Vec2("size", new Vector2(0.85f, 0.5f)).Vec2("offset", new Vector2(0f, 0.25f)).Apply();
                var spikes = root.AddComponent<Spikes>();
                Set(spikes).Ref("hitbox", hitbox).Float("knockback", 8f).Apply();
                SavePrefab(root, PrefabRoot + "/Spikes.prefab");
            }
            // falling debris
            {
                GameObject root = Go("FallingDebris");
                SpriteRenderer shadow = SpriteChild(root.transform, "Shadow", Sprite(Gen + "/Props/shadow.png"), OrderProps - 1, new Vector3(0f, 0.08f, 0f));
                SpriteRenderer rock = SpriteChild(root.transform, "Rock", Sprite(Gen + "/Props/rock.png"), OrderProps + 3, new Vector3(0f, 9f, 0f));
                var hitbox = Go("Hitbox", root.transform).AddComponent<Hitbox>();
                Set(hitbox).Vec2("size", new Vector2(0.8f, 0.8f)).Vec2("offset", new Vector2(0f, 9f)).Apply();
                var debris = root.AddComponent<FallingDebris>();
                Set(debris).Ref("hitbox", hitbox).Ref("rock", rock.transform).Ref("shadow", shadow).Float("knockback", 6f).Apply();
                SavePrefab(root, PrefabRoot + "/FallingDebris.prefab");
            }
            // moving platform
            {
                GameObject root = Go("MovingPlatform", null, Layers.Ground);
                var rb = root.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                var col = root.AddComponent<BoxCollider2D>();
                col.size = new Vector2(3f, 0.4f);
                col.offset = new Vector2(1.5f, 0.8f);
                col.usedByEffector = true;
                var effector = root.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 160f;
                SpriteRenderer v = SpriteChild(root.transform, "Visual", Sprite(Gen + "/Props/moving_platform.png"), OrderProps - 2, new Vector3(1.5f, 0.5f, 0f));
                v.drawMode = SpriteDrawMode.Tiled;
                v.size = new Vector2(3f, 1f);
                var platform = root.AddComponent<MovingPlatform>();
                Set(platform).Ref("visual", v).Ref("surface", col).Apply();
                SavePrefab(root, PrefabRoot + "/MovingPlatform.prefab");
            }
            // tutorial sign
            {
                GameObject root = Go("Sign", null, Layers.Trigger);
                var col = root.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(2f, 3f);
                col.offset = new Vector2(0f, 1.5f);
                SpriteChild(root.transform, "Visual", Sprite(Gen + "/Props/sign.png"), OrderDecoration + 1, Vector3.zero);
                root.AddComponent<TutorialSign>();
                SavePrefab(root, PrefabRoot + "/Sign.prefab");
            }
            // arena wall
            {
                GameObject root = Go("ArenaWall", null, Layers.Ground);
                var col = root.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 10f);
                col.offset = new Vector2(0f, 5f);
                col.enabled = false;
                SpriteRenderer v = SpriteChild(root.transform, "Visual", Sprite(Gen + "/Props/arena_wall.png"), OrderProps + 1, Vector3.zero);
                v.drawMode = SpriteDrawMode.Tiled;
                var wall = root.AddComponent<ArenaWall>();
                Set(wall).Ref("blocker", col).Ref("visual", v).Float("wallHeight", 7f).Apply();
                SavePrefab(root, PrefabRoot + "/ArenaWall.prefab");
            }
            // disaster front
            {
                GameObject root = Go("DisasterFront");
                SpriteRenderer body = SpriteChild(root.transform, "Body", Sprite(Gen + "/Effects/white.png"), OrderFront - 1, Vector3.zero);
                SpriteRenderer wall = SpriteChild(root.transform, "Wall", Sprite(Gen + "/Fronts/front_fire_0.png"), OrderFront, Vector3.zero);
                SpriteRenderer glow = SpriteChild(root.transform, "Glow", Sprite(Gen + "/Effects/white.png"), OrderFront - 2, Vector3.zero);
                var front = root.AddComponent<DisasterFront>();
                Set(front).Ref("wall", wall).Ref("body", body).Ref("glow", glow).Apply();
                SavePrefab(root, PrefabRoot + "/DisasterFront.prefab");
            }
            // decoration
            {
                GameObject root = Go("Decoration");
                SpriteChild(root.transform, "Visual", Sprite(Kenney + "/barrel.png"), OrderDecoration, Vector3.zero);
                SavePrefab(root, PrefabRoot + "/Decoration.prefab");
            }
        }

        // ------------------------------------------------------------------ effects

        static Material particleMaterial;

        static void CreateEffectPrefabs()
        {
            particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(ArtRoot + "/Materials/Particles.mat");
            if (particleMaterial == null)
            {
                EnsureFolder(ArtRoot + "/Materials");
                particleMaterial = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(particleMaterial, ArtRoot + "/Materials/Particles.mat");
            }
            Effect("FX_Dust", Gen + "/Effects/dust.png", 7, new Vector2(0.25f, 0.45f), new Vector2(0.5f, 2.2f), new Vector2(0.15f, 0.3f), new Color(0.85f, 0.8f, 0.7f, 0.8f), -0.1f, 50f, false);
            Effect("FX_Sparks", Gen + "/Effects/spark.png", 10, new Vector2(0.15f, 0.35f), new Vector2(3f, 7f), new Vector2(0.15f, 0.3f), new Color(1f, 0.9f, 0.5f, 1f), 1f, 70f, false);
            Effect("FX_Slash", Gen + "/Effects/slash.png", 1, new Vector2(0.14f, 0.14f), Vector2.zero, new Vector2(1.6f, 1.6f), new Color(1f, 1f, 1f, 0.9f), 0f, 0f, true);
            Effect("FX_Blood", Gen + "/Effects/dust.png", 12, new Vector2(0.3f, 0.6f), new Vector2(2f, 5f), new Vector2(0.1f, 0.22f), new Color(0.45f, 0.75f, 0.35f, 1f), 2.5f, 60f, false);
            Effect("FX_Debris", Gen + "/Props/rock.png", 8, new Vector2(0.4f, 0.8f), new Vector2(2f, 6f), new Vector2(0.2f, 0.4f), new Color(0.7f, 0.62f, 0.55f, 1f), 3f, 60f, false);
        }

        static void Effect(string name, string spritePath, int count, Vector2 life, Vector2 speed, Vector2 size, Color color, float gravity, float coneAngle, bool local)
        {
            GameObject go = Go(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, local ? 0f : Mathf.PI * 2f);

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.enabled = coneAngle > 0f;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle;
            shape.radius = 0.1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = gradient;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, local ? 1.2f : 0.4f));

            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.AddSprite(Sprite(spritePath));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = OrderEffects;
            renderer.alignment = local ? ParticleSystemRenderSpace.Local : ParticleSystemRenderSpace.View;

            go.AddComponent<PooledParticle>();
            SavePrefab(go, PrefabRoot + "/Effects/" + name + ".prefab");
        }

        // ------------------------------------------------------------------ data ↔ prefab links

        static void LinkDataToPrefabs()
        {
            GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/Enemy.prefab");
            foreach (string n in new[] { "Shambler", "Runner", "Brute" })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataRoot}/Enemies/{n}.asset");
                data.prefab = enemy;
                EditorUtility.SetDirty(data);
            }

            var prefabs = LoadOrCreate<LevelPrefabs>(DataRoot + "/LevelPrefabs.asset");
            prefabs.player = P("Knight");
            prefabs.checkpoint = P("Checkpoint");
            prefabs.exitGate = P("ExitGate");
            prefabs.heartPickup = P("HeartPickup");
            prefabs.firePatch = P("FirePatch");
            prefabs.fallingDebris = P("FallingDebris");
            prefabs.spikes = P("Spikes");
            prefabs.movingPlatform = P("MovingPlatform");
            prefabs.sign = P("Sign");
            prefabs.boss = P("AshWarden");
            prefabs.princess = P("Princess");
            prefabs.arenaWall = P("ArenaWall");
            prefabs.disasterFront = P("DisasterFront");
            prefabs.decoration = P("Decoration");
            prefabs.decorationSprites = new[] { Sprite(Kenney + "/barrel.png"), Sprite(Kenney + "/gravestone.png") };
            EditorUtility.SetDirty(prefabs);
        }

        static GameObject P(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{name}.prefab");

        // ------------------------------------------------------------------ persistent systems

        static void CreateSystemsPrefab()
        {
            GameObject root = Go("Systems");
            var input = root.AddComponent<InputReader>();
            var gm = root.AddComponent<GameManager>();
            var audio = root.AddComponent<AudioManager>();

            GameObject ui = Go("UI", root.transform, 5);
            var canvas = ui.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = false;
            var scaler = ui.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            ui.AddComponent<GraphicRaycaster>();

            GameObject panels = Go("Panels", ui.transform, 5);
            var panelRect = panels.AddComponent<RectTransform>();
            UIFactory.Stretch(panelRect);

            GameObject faderGo = Go("Fader", ui.transform, 5);
            var faderRect = faderGo.AddComponent<RectTransform>();
            UIFactory.Stretch(faderRect);
            var faderImage = faderGo.AddComponent<Image>();
            faderImage.color = Color.black;
            var group = faderGo.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = true;
            var fader = faderGo.AddComponent<ScreenFader>();

            GameObject es = Go("EventSystem", root.transform);
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();

            var flow = ui.AddComponent<UIFlowController>();

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            Set(input).Ref("actions", actions).Apply();
            Set(gm).Ref("catalog", AssetDatabase.LoadAssetAtPath<LevelCatalog>(DataRoot + "/LevelCatalog.asset")).Ref("input", input).Ref("fader", fader).Apply();
            Set(audio).Ref("library", AssetDatabase.LoadAssetAtPath<AudioLibrary>(DataRoot + "/AudioLibrary.asset")).Apply();
            Set(flow).Ref("style", AssetDatabase.LoadAssetAtPath<UIStyle>(DataRoot + "/UIStyle.asset")).Ref("input", input)
                .Ref("inputModule", module).Ref("panelRoot", panelRect).Apply();
            SavePrefab(root, SystemsPath);
        }
    }
}
