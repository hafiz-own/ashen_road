using AshenRoad.Data;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>Sky plus two silhouette layers that scroll slower than the camera to give depth.</summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [SerializeField] SpriteRenderer sky;
        [SerializeField] SpriteRenderer far;
        [SerializeField] SpriteRenderer near;
        [SerializeField] float farFactor = 0.85f;
        [SerializeField] float nearFactor = 0.65f;
        [SerializeField] float layerHeight = 14f;

        public void Apply(LevelData data)
        {
            if (cameraTransform == null && UnityEngine.Camera.main != null) cameraTransform = UnityEngine.Camera.main.transform;
            Set(sky, data.sky, new Vector2(80f, 40f), false);
            Set(far, data.farLayer, new Vector2(400f, layerHeight), true);
            Set(near, data.nearLayer, new Vector2(400f, layerHeight), true);
        }

        static void Set(SpriteRenderer r, Sprite s, Vector2 size, bool tiled)
        {
            if (r == null || s == null) return;
            r.sprite = s;
            if (tiled)
            {
                r.drawMode = SpriteDrawMode.Tiled;
                r.tileMode = SpriteTileMode.Continuous;
                r.size = size;
            }
            else
            {
                r.drawMode = SpriteDrawMode.Simple;
                Vector2 native = s.bounds.size;
                r.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
            }
        }

        void LateUpdate()
        {
            if (cameraTransform == null) return;
            Vector3 c = cameraTransform.position;
            if (sky != null) sky.transform.position = new Vector3(c.x, c.y, 20f);
            if (far != null) far.transform.position = new Vector3(c.x * farFactor, c.y * 0.6f + 1.5f, 15f);
            if (near != null) near.transform.position = new Vector3(c.x * nearFactor, c.y * 0.75f, 10f);
        }
    }
}
