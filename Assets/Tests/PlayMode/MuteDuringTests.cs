using NUnit.Framework;
using UnityEngine;

namespace AshenRoad.Tests
{
    /// <summary>Silences the game while the automated play mode tests run.</summary>
    [SetUpFixture]
    public class MuteDuringTests
    {
        [OneTimeSetUp] public void Mute() => AudioListener.volume = 0f;
        [OneTimeTearDown] public void Unmute() => AudioListener.volume = 1f;
    }
}
