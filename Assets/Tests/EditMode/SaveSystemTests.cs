using AshenRoad.Core;
using NUnit.Framework;

namespace AshenRoad.Tests
{
    public class SaveSystemTests
    {
        [Test]
        public void FreshSave_OnlyFirstLevelUnlocked()
        {
            var s = new SaveSystem(new MemorySaveStore());
            Assert.AreEqual(0, s.HighestUnlockedLevel);
            Assert.IsTrue(s.IsUnlocked(0));
            Assert.IsFalse(s.IsUnlocked(1));
        }

        [Test]
        public void UnlockLevel_NeverLocksBack()
        {
            var s = new SaveSystem(new MemorySaveStore());
            s.UnlockLevel(3);
            s.UnlockLevel(1);
            Assert.AreEqual(3, s.HighestUnlockedLevel);
        }

        [Test]
        public void RecordTime_KeepsOnlyTheBest()
        {
            var s = new SaveSystem(new MemorySaveStore());
            Assert.AreEqual(-1f, s.GetBestTime(2));
            Assert.IsTrue(s.RecordTime(2, 100f));
            Assert.IsFalse(s.RecordTime(2, 120f));
            Assert.IsTrue(s.RecordTime(2, 90f));
            Assert.AreEqual(90f, s.GetBestTime(2));
        }

        [Test]
        public void Volumes_AreClamped_AndSurviveResetProgress()
        {
            var s = new SaveSystem(new MemorySaveStore());
            s.MusicVolume = 2f;
            s.SfxVolume = 0.25f;
            s.UnlockLevel(4);
            s.GameFinished = true;
            s.ResetProgress();
            Assert.AreEqual(1f, s.MusicVolume);
            Assert.AreEqual(0.25f, s.SfxVolume);
            Assert.AreEqual(0, s.HighestUnlockedLevel);
            Assert.IsFalse(s.GameFinished);
        }

        [Test]
        public void LevelResult_FormatsTime()
        {
            Assert.AreEqual("01:05.50", LevelResult.FormatTime(65.5f));
            Assert.AreEqual("--:--", LevelResult.FormatTime(-1f));
        }
    }
}
