using AshenRoad.Data;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>
    /// Sky plus two scenery layers that scroll slower than the camera to give depth. Each layer repeats sideways
    /// and is scaled to a set height, so any background art fits whatever its pixel size.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [SerializeField] SpriteRenderer sky;
        [SerializeField] SpriteRenderer far;
        [SerializeField] SpriteRenderer near;
        [SerializeField] float skyFactor = 0.95f;
        [SerializeField] float farFactor = 0.85f;
        [SerializeField] float nearFactor = 0.65f;
        [SerializeField] float skyHeight = 17f;
        [SerializeField] float farHeight = 13f;
        [SerializeField] float nearHeight = 10f;

        public void Apply(LevelData data)
        {
            if (cameraTransform == null && UnityEngine.Camera.main != null) cameraTransform = UnityEngine.Camera.main.transform;
            Set(sky, data.sky, skyHeight, data.skyTint);
            Set(far, data.farLayer, farHeight, data.farTint);
            Set(near, data.nearLayer, nearHeight, data.nearTint);
        }

        static void Set(SpriteRenderer r, Sprite s, float height, Color tint)
        {
            if (r == null) return;
            r.enabled = s != null;
            if (s == null) return;
            r.sprite = s;
            r.color = tint;
            float scale = height / s.bounds.size.y;
            r.transform.localScale = new Vector3(scale, scale, 1f);
            r.drawMode = SpriteDrawMode.Tiled;
            r.tileMode = SpriteTileMode.Continuous;
            // 400 world units wide, in the renderer's local (unscaled) units; one sprite tall so it never tiles vertically
            r.size = new Vector2(400f / scale, s.bounds.size.y);
        }

        void LateUpdate()
        {
            if (cameraTransform == null) return;
            Vector3 c = cameraTransform.position;
            float bottom = c.y - 7.5f;
            Place(sky, c.x * skyFactor, c.y, 20f, false);
            Place(far, c.x * farFactor, bottom * 0.5f + 2.5f, 15f, true);
            Place(near, c.x * nearFactor, bottom * 0.7f + 0.5f, 10f, true);
        }

        static void Place(SpriteRenderer r, float x, float y, float z, bool bottomAligned)
        {
            if (r == null || !r.enabled || r.sprite == null) return;
            // sprites are centre-pivoted: lift bottom-aligned layers by half their height
            float lift = bottomAligned ? r.bounds.size.y * 0.5f : 0f;
            r.transform.position = new Vector3(x, y + lift, z);
        }
    }
}
