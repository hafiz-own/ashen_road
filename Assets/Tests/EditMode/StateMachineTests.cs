using System.Collections.Generic;
using AshenRoad.Core;
using NUnit.Framework;

namespace AshenRoad.Tests
{
    public class StateMachineTests
    {
        class Probe : IState
        {
            readonly string name;
            readonly List<string> log;
            public int Ticks, FixedTicks;

            public Probe(string name, List<string> log) { this.name = name; this.log = log; }
            public void Enter() => log.Add("enter " + name);
            public void Exit() => log.Add("exit " + name);
            public void Tick(float dt) => Ticks++;
            public void FixedTick(float dt) => FixedTicks++;
        }

        [Test]
        public void ChangeState_ExitsOldBeforeEnteringNew()
        {
            var log = new List<string>();
            var a = new Probe("a", log);
            var b = new Probe("b", log);
            var sm = new StateMachine();
            sm.ChangeState(a);
            sm.ChangeState(b);
            CollectionAssert.AreEqual(new[] { "enter a", "exit a", "enter b" }, log);
            Assert.AreSame(b, sm.Current);
            Assert.AreSame(a, sm.Previous);
        }

        [Test]
        public void ChangeState_ToActiveState_IsIgnoredUnlessForced()
        {
            var log = new List<string>();
            var a = new Probe("a", log);
            var sm = new StateMachine();
            sm.ChangeState(a);
            Assert.IsFalse(sm.ChangeState(a));
            Assert.AreEqual(1, log.Count);
            Assert.IsTrue(sm.ChangeState(a, true));
            CollectionAssert.AreEqual(new[] { "enter a", "exit a", "enter a" }, log);
        }

        [Test]
        public void RevertToPrevious_ReturnsToLastState()
        {
            var log = new List<string>();
            var a = new Probe("a", log);
            var b = new Probe("b", log);
            var sm = new StateMachine();
            sm.ChangeState(a);
            sm.ChangeState(b);
            Assert.IsTrue(sm.RevertToPrevious());
            Assert.AreSame(a, sm.Current);
        }

        [Test]
        public void Ticks_AreForwardedToCurrentStateOnly()
        {
            var log = new List<string>();
            var a = new Probe("a", log);
            var b = new Probe("b", log);
            var sm = new StateMachine();
            sm.ChangeState(a);
            sm.Tick(0.1f);
            sm.FixedTick(0.02f);
            sm.ChangeState(b);
            sm.Tick(0.1f);
            Assert.AreEqual(1, a.Ticks);
            Assert.AreEqual(1, a.FixedTicks);
            Assert.AreEqual(1, b.Ticks);
        }

        [Test]
        public void StateChanged_ReportsFromAndTo()
        {
            var log = new List<string>();
            var a = new Probe("a", log);
            var b = new Probe("b", log);
            var sm = new StateMachine();
            IState from = null, to = null;
            sm.ChangeState(a);
            sm.StateChanged += (f, t) => { from = f; to = t; };
            sm.ChangeState(b);
            Assert.AreSame(a, from);
            Assert.AreSame(b, to);
        }

        [Test]
        public void IsIn_ChecksType()
        {
            var sm = new StateMachine();
            sm.ChangeState(new Probe("a", new List<string>()));
            Assert.IsTrue(sm.IsIn<Probe>());
        }
    }
}
