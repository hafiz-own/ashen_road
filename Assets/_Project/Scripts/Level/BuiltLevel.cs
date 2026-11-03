using System.Collections.Generic;
using AshenRoad.Data;
using AshenRoad.Level.Hazards;
using UnityEngine;

namespace AshenRoad.Level
{
    /// <summary>What LevelBuilder produced: positions and the objects the LevelController needs to wire up.</summary>
    public class BuiltLevel
    {
        public LevelLayout Layout;
        public Vector2 PlayerStart;
        public readonly List<Checkpoint> Checkpoints = new List<Checkpoint>();
        public ExitGate Exit;
        public readonly List<SpawnEntry> Spawns = new List<SpawnEntry>();
        public Vector2? BossPosition;
        public Vector2? PrincessPosition;
        public readonly List<ArenaWall> ArenaWalls = new List<ArenaWall>();
        public float ArenaMinX = float.NegativeInfinity;
        public float ArenaMaxX = float.PositiveInfinity;
        public readonly List<FallingDebris> Debris = new List<FallingDebris>();
        public readonly List<FirePatch> Fires = new List<FirePatch>();
        public Rect Bounds;
        public float KillY;
    }
}
