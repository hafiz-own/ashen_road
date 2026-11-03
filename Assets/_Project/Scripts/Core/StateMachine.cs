using System;

namespace AshenRoad.Core
{
    /// <summary>
    /// Holds the current state, runs Exit/Enter on a change and forwards the update calls.
    /// Plain C# (not a MonoBehaviour) so its owner decides when it ticks, and so it is unit-testable.
    /// </summary>
    public class StateMachine
    {
        public IState Current { get; private set; }
        public IState Previous { get; private set; }

        /// <summary>Raised after a change as (from, to).</summary>
        public event Action<IState, IState> StateChanged;

        /// <summary>Changes state. Asking for the active state again is ignored unless forced.</summary>
        public bool ChangeState(IState next, bool force = false)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (next == Current && !force) return false;

            IState from = Current;
            Current?.Exit();
            Previous = from;
            Current = next;
            Current.Enter();
            StateChanged?.Invoke(from, next);
            return true;
        }

        public bool RevertToPrevious()
        {
            return Previous != null && ChangeState(Previous);
        }

        public bool IsIn<T>() where T : IState => Current is T;

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);

        public void FixedTick(float fixedDeltaTime) => Current?.FixedTick(fixedDeltaTime);
    }
}
