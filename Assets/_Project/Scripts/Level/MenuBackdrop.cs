using AshenRoad.Data;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>Behind the main menu: a level's sky and silhouettes drifting slowly past.</summary>
    public class MenuBackdrop : MonoBehaviour
    {
        [SerializeField] ParallaxBackground background;
        [SerializeField] LevelData look;
        [SerializeField] float driftSpeed = 1.2f;

        void Start()
        {
            if (background != null && look != null) background.Apply(look);
        }

        void Update()
        {
            transform.position += Vector3.right * (driftSpeed * Time.unscaledDeltaTime);
        }
    }
}
