using UnityEngine;

namespace AshenRoad.Visual
{
    /// <summary>The princess at the end of the road: idles nervously until the knight arrives.</summary>
    public class Princess : MonoBehaviour
    {
        [SerializeField] Transform body;

        void LateUpdate()
        {
            if (body == null) return;
            body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.05f, 0f);
        }
    }
}
