using AshenRoad.Level;
using NUnit.Framework;

namespace AshenRoad.Tests
{
    public class FrontModelTests
    {
        static FrontModel Make() => new FrontModel(0f, 2f, 0.1f, 3f, 20f, 2f);

        [Test]
        public void Speed_RampsUpOverTime_AndIsCapped()
        {
            var f = Make();
            Assert.AreEqual(2f, f.SpeedFor(5f), 1e-4);
            for (int i = 0; i < 50; i++) f.Step(0.1f, f.X + 5f); // 5 s
            Assert.AreEqual(2.5f, f.SpeedFor(f.X + 5f), 1e-3);
            for (int i = 0; i < 500; i++) f.Step(0.1f, f.X + 5f);
            Assert.AreEqual(3f, f.SpeedFor(f.X + 5f), 1e-4);
        }

        [Test]
        public void CatchUp_AddsSpeedOnlyWhenFarBehind_AndIsCapped()
        {
            var f = Make();
            Assert.AreEqual(2f, f.SpeedFor(20f), 1e-4);
            Assert.AreEqual(3f, f.SpeedFor(30f), 1e-4);
            Assert.AreEqual(4f, f.SpeedFor(40f), 1e-4);
            Assert.AreEqual(4f, f.SpeedFor(400f), 1e-4);
        }

        [Test]
        public void Step_NeverMovesBackwards_AndRespectsPauseAndStopLine()
        {
            var f = Make();
            f.Step(1f, 10f);
            float x = f.X;
            Assert.Greater(x, 0f);
            f.Paused = true;
            f.Step(1f, 10f);
            Assert.AreEqual(x, f.X);
            f.Paused = false;
            f.StopX = x + 0.5f;
            f.Step(5f, 10f);
            Assert.AreEqual(x + 0.5f, f.X, 1e-4);
        }

        [Test]
        public void ResetTo_MovesFrontAndRestartsRamp()
        {
            var f = Make();
            for (int i = 0; i < 100; i++) f.Step(0.1f, f.X + 5f);
            f.ResetTo(-10f);
            Assert.AreEqual(-10f, f.X);
            Assert.AreEqual(0f, f.Elapsed);
        }

        [Test]
        public void Catches_WhenTargetIsAtOrBehindTheFront()
        {
            var f = Make();
            Assert.IsTrue(f.Catches(-0.1f));
            Assert.IsTrue(f.Catches(0f));
            Assert.IsFalse(f.Catches(0.1f));
        }
    }
}
