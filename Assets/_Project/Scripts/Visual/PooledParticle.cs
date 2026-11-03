using System;
using UnityEngine;

namespace AshenRoad.Visual
{
    /// <summary>A particle effect that tells its pool when it has finished playing.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledParticle : MonoBehaviour
    {
        ParticleSystem system;
        Action onFinished;
        float releaseAt;

        void Awake()
        {
            system = GetComponent<ParticleSystem>();
            var main = system.main;
            main.stopAction = ParticleSystemStopAction.None;
            main.playOnAwake = false;
        }

        public void Play(Action finished)
        {
            onFinished = finished;
            system.Clear(true);
            system.Play(true);
            var main = system.main;
            releaseAt = Time.time + main.duration + main.startLifetime.constantMax + 0.1f;
        }

        void Update()
        {
            if (onFinished != null && Time.time >= releaseAt)
            {
                Action done = onFinished;
                onFinished = null;
                done();
            }
        }
    }
}
