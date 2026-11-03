using System;
using UnityEngine;

namespace AshenRoad.Core
{
    /// <summary>
    /// Global event channel (Observer pattern). Senders call Raise..., listeners subscribe in OnEnable
    /// and unsubscribe in OnDisable. It keeps no game state of its own.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameState, GameState> GameStateChanged;
        public static event Action<int> LevelStarted;
        public static event Action<LevelResult> LevelCompleted;
        public static event Action<DeathCause> PlayerDied;
        public static event Action<int, int> PlayerHealthChanged;
        public static event Action PlayerDamaged;
        public static event Action<PlayerAction> PlayerActed;
        public static event Action<int> CheckpointActivated;
        public static event Action HeartPickedUp;
        public static event Action<float> FrontDistanceChanged;
        public static event Action<string> EnemyKilled;
        public static event Action<Vector2, bool> HitLanded;
        public static event Action<float, float> CameraShakeRequested;
        public static event Action<EffectType, Vector2, Vector2> EffectRequested;
        public static event Action<string> SoundRequested;
        public static event Action<string> HintRequested;
        public static event Action BossIntroStarted;
        public static event Action<int> BossPhaseChanged;
        public static event Action<int, int> BossHealthChanged;
        public static event Action BossDefeated;
        public static event Action ExitReached;

        public static void RaiseGameStateChanged(GameState from, GameState to) => GameStateChanged?.Invoke(from, to);
        public static void RaiseLevelStarted(int index) => LevelStarted?.Invoke(index);
        public static void RaiseLevelCompleted(LevelResult result) => LevelCompleted?.Invoke(result);
        public static void RaisePlayerDied(DeathCause cause) => PlayerDied?.Invoke(cause);
        public static void RaisePlayerHealthChanged(int current, int max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaisePlayerDamaged() => PlayerDamaged?.Invoke();
        public static void RaisePlayerActed(PlayerAction action) => PlayerActed?.Invoke(action);
        public static void RaiseCheckpointActivated(int index) => CheckpointActivated?.Invoke(index);
        public static void RaiseHeartPickedUp() => HeartPickedUp?.Invoke();
        public static void RaiseFrontDistanceChanged(float distance) => FrontDistanceChanged?.Invoke(distance);
        public static void RaiseEnemyKilled(string enemyName) => EnemyKilled?.Invoke(enemyName);
        public static void RaiseHitLanded(Vector2 point, bool blocked) => HitLanded?.Invoke(point, blocked);
        public static void RaiseCameraShake(float amplitude, float duration) => CameraShakeRequested?.Invoke(amplitude, duration);
        public static void RaiseEffect(EffectType type, Vector2 position, Vector2 direction) => EffectRequested?.Invoke(type, position, direction);
        public static void RaiseSound(string soundId) => SoundRequested?.Invoke(soundId);
        public static void RaiseHint(string text) => HintRequested?.Invoke(text);
        public static void RaiseBossIntroStarted() => BossIntroStarted?.Invoke();
        public static void RaiseBossPhaseChanged(int phase) => BossPhaseChanged?.Invoke(phase);
        public static void RaiseBossHealthChanged(int current, int max) => BossHealthChanged?.Invoke(current, max);
        public static void RaiseBossDefeated() => BossDefeated?.Invoke();
        public static void RaiseExitReached() => ExitReached?.Invoke();

        /// <summary>Drops every listener. Used between tests so one test's objects can't hear another's events.</summary>
        public static void ClearAllListeners()
        {
            GameStateChanged = null;
            LevelStarted = null;
            LevelCompleted = null;
            PlayerDied = null;
            PlayerHealthChanged = null;
            PlayerDamaged = null;
            PlayerActed = null;
            CheckpointActivated = null;
            HeartPickedUp = null;
            FrontDistanceChanged = null;
            EnemyKilled = null;
            HitLanded = null;
            CameraShakeRequested = null;
            EffectRequested = null;
            SoundRequested = null;
            HintRequested = null;
            BossIntroStarted = null;
            BossPhaseChanged = null;
            BossHealthChanged = null;
            BossDefeated = null;
            ExitReached = null;
        }
    }
}
