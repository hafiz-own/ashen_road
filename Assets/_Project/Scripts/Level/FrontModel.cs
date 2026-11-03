using System;

namespace AshenRoad.Level
{
    /// <summary>
    /// The disaster front's movement rules, free of Unity so they can be tested: a speed that ramps up over time,
    /// a capped catch-up bonus when the knight is far ahead, an optional stop line, and it never moves back.
    /// </summary>
    public class FrontModel
    {
        public float X { get; private set; }
        public float Elapsed { get; private set; }
        public bool Paused { get; set; }
        public float? StopX { get; set; }

        public float BaseSpeed { get; set; }
        public float Ramp { get; set; }
        public float MaxSpeed { get; set; }
        public float CatchupDistance { get; set; }
        public float CatchupBonus { get; set; }

        public FrontModel(float startX, float baseSpeed, float ramp, float maxSpeed, float catchupDistance, float catchupBonus)
        {
            X = startX;
            BaseSpeed = baseSpeed;
            Ramp = ramp;
            MaxSpeed = maxSpeed;
            CatchupDistance = catchupDistance;
            CatchupBonus = catchupBonus;
        }

        public float SpeedFor(float targetX)
        {
            float speed = Math.Min(BaseSpeed + Ramp * Elapsed, MaxSpeed);
            float gap = targetX - X;
            if (CatchupDistance > 0f && gap > CatchupDistance)
            {
                float t = Math.Min(1f, (gap - CatchupDistance) / CatchupDistance);
                speed += CatchupBonus * t;
            }
            return Math.Max(0f, speed);
        }

        public void Step(float dt, float targetX)
        {
            if (Paused || dt <= 0f) return;
            Elapsed += dt;
            float next = X + SpeedFor(targetX) * dt;
            if (StopX.HasValue) next = Math.Min(next, StopX.Value);
            X = Math.Max(X, next);
        }

        /// <summary>Moves the front to a new position (respawn). The ramp restarts.</summary>
        public void ResetTo(float x)
        {
            X = x;
            Elapsed = 0f;
        }

        public bool Catches(float targetLeftEdge) => targetLeftEdge <= X;
    }
}
