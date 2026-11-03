using UnityEngine;

namespace AshenRoad.Data
{
    /// <summary>The prefabs a level is built from. Assigned once on the LevelController.</summary>
    [CreateAssetMenu(menuName = "Ashen Road/Level Prefabs")]
    public class LevelPrefabs : ScriptableObject
    {
        public GameObject player;
        public GameObject checkpoint;
        public GameObject exitGate;
        public GameObject heartPickup;
        public GameObject firePatch;
        public GameObject fallingDebris;
        public GameObject spikes;
        public GameObject movingPlatform;
        public GameObject sign;
        public GameObject boss;
        public GameObject princess;
        public GameObject arenaWall;
        public GameObject disasterFront;
        public GameObject decoration;
        public Sprite[] decorationSprites = new Sprite[0];
    }
}
