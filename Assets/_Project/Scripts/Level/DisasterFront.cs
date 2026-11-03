using AshenRoad.Combat;
using AshenRoad.Core;
using AshenRoad.Data;
using AshenRoad.Player;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>
    /// The advancing wall (fire, flood, collapse...). Moves every physics step using FrontModel, kills the knight
    /// when it catches up, and publishes the distance so the HUD can show how close it is.
    /// </summary>
    public class DisasterFront : MonoBehaviour
    {
        [SerializeField] SpriteRenderer wall;
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer glow;
        [SerializeField] float frameTime = 0.11f;

        FrontModel model;
        PlayerController target;
        Sprite[] frames = new Sprite[0];
        float nextFrameAt;
        int frame;
        float nextRumbleAt;
        float nextBoltAt;
        DisasterType disaster;
        float height = 40f;

        public float X => model != null ? model.X : transform.position.x;
        public bool Paused => model != null && model.Paused;
        public FrontModel Model => model;
        public float Gap => target != null ? target.Position.x - X : float.PositiveInfinity;

        public void Setup(LevelData data, float startX, PlayerController knight, float levelHeight)
        {
            target = knight;
            disaster = data.disaster;
            frames = data.frontFrames ?? new Sprite[0];
            height = levelHeight + 24f;
            model = new FrontModel(startX, data.frontBaseSpeed, data.frontRamp, data.frontMaxSpeed, data.frontCatchupDistance, data.frontCatchupBonus);
            if (glow != null) glow.color = data.frontGlow;
            if (body != null)
            {
                Color c = data.frontGlow;
                body.color = new Color(c.r * 0.25f, c.g * 0.2f, c.b * 0.2f, 1f);
            }
            Layout();
            Place();
        }

        void Layout()
        {
            if (wall != null)
            {
                wall.drawMode = SpriteDrawMode.Tiled;
                wall.size = new Vector2(3f, height);
                wall.transform.localPosition = new Vector3(-1.5f, height * 0.5f - 12f, 0f);
                if (frames.Length > 0) wall.sprite = frames[0];
            }
            if (body != null)
            {
                // body uses a 1x1 white sprite stretched over everything the front has swallowed
                body.drawMode = SpriteDrawMode.Simple;
                body.transform.localScale = new Vector3(60f, height, 1f);
                body.transform.localPosition = new Vector3(-32.9f, height * 0.5f - 12f, 0f);
            }
            if (glow != null)
            {
                glow.transform.localScale = new Vector3(8f, height, 1f);
                glow.transform.localPosition = new Vector3(1.5f, height * 0.5f - 12f, 0f);
            }
        }

        public void SetStopX(float x)
        {
            if (model != null) model.StopX = x;
        }

        public void Pause() { if (model != null) model.Paused = true; }

        public void Resume() { if (model != null) model.Paused = false; }

        public void ResetTo(float x)
        {
            model?.ResetTo(x);
            Place();
        }

        void FixedUpdate()
        {
            if (model == null || target == null) return;
            if (!target.IsDead) model.Step(Time.fixedDeltaTime, target.Position.x);
            Place();

            if (!target.IsDead && model.Catches(target.Position.x - 0.3f))
                target.Health.TakeDamage(DamageInfo.Lethal(DamageType.Front, target.Position));
        }

        void Update()
        {
            if (model == null) return;
            float gap = Gap;
            GameEvents.RaiseFrontDistanceChanged(gap);

            if (frames.Length > 1 && Time.time >= nextFrameAt)
            {
                nextFrameAt = Time.time + frameTime;
                frame = (frame + 1) % frames.Length;
                if (wall != null) wall.sprite = frames[frame];
            }
            if (glow != null)
            {
                Color c = glow.color;
                c.a = 0.25f + 0.1f * Mathf.Sin(Time.time * 7f);
                glow.color = c;
            }
            if (!model.Paused && gap < 9f && Time.time >= nextRumbleAt)
            {
                nextRumbleAt = Time.time + 1.4f;
                GameEvents.RaiseSound(disaster == DisasterType.Lightning ? "thunder" : disaster == DisasterType.Flood ? "flood" : "rumble");
                GameEvents.RaiseCameraShake(0.06f + 0.1f * (1f - gap / 9f), 0.4f);
            }
            if ((disaster == DisasterType.Lightning || disaster == DisasterType.FireAndDebris) && !model.Paused && Time.time >= nextBoltAt)
            {
                nextBoltAt = Time.time + Random.Range(1.2f, 2.6f);
                Vector2 p = new Vector2(X + Random.Range(0f, 2.5f), target != null ? target.Position.y + Random.Range(0f, 4f) : transform.position.y);
                GameEvents.RaiseEffect(disaster == DisasterType.Lightning ? EffectType.Sparks : EffectType.Debris, p, Vector2.down);
            }
        }

        void Place()
        {
            if (model == null) return;
            float y = target != null ? 0f : transform.position.y;
            transform.position = new Vector3(model.X, y, -1f);
        }
    }
}
