namespace AshenRoad.Core
{
    /// <summary>One state of a state machine (State pattern). Implemented by player, enemy and boss states.</summary>
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void FixedTick(float fixedDeltaTime);
        void Exit();
    }
}
