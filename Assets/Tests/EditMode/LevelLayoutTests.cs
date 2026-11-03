using System;
using AshenRoad.Level;
using NUnit.Framework;
using UnityEngine;

namespace AshenRoad.Tests
{
    public class LevelLayoutTests
    {
        const string Simple =
            "..........\n" +
            "....===...\n" +
            ".P..S...E.\n" +
            "##########\n";

        [Test]
        public void Parse_ReadsSizeAndFlipsRows()
        {
            LevelLayout l = LevelLayout.Parse(Simple);
            Assert.AreEqual(10, l.Width);
            Assert.AreEqual(4, l.Height);
            Assert.IsTrue(l.IsSolid(0, 0));
            Assert.IsFalse(l.IsSolid(0, 1));
            Assert.IsTrue(l.IsOneWay(4, 2));
        }

        [Test]
        public void Parse_FindsMarkersAtWorldCoordinates()
        {
            LevelLayout l = LevelLayout.Parse(Simple);
            LevelLayout.Marker p = l.First('P');
            Assert.AreEqual(1, p.X);
            Assert.AreEqual(1, p.Y);
            Assert.AreEqual(1, l.Count('S'));
            Assert.AreEqual(new Vector2(1.5f, 1f), LevelLayout.CellToWorld(p.X, p.Y));
        }

        [Test]
        public void Parse_PadsShortLinesAndTreatsSpacesAsAir()
        {
            LevelLayout l = LevelLayout.Parse("P   E\n###\n");
            Assert.AreEqual(5, l.Width);
            Assert.IsFalse(l.IsSolid(4, 0));
            Assert.AreEqual('E', l.At(4, 1));
        }

        [Test]
        public void Parse_RejectsUnknownSymbols()
        {
            Assert.Throws<FormatException>(() => LevelLayout.Parse("P?E\n###"));
        }

        [Test]
        public void Parse_RejectsEmptyText()
        {
            Assert.Throws<FormatException>(() => LevelLayout.Parse("   \n "));
        }

        [Test]
        public void Validate_AcceptsAWellFormedLevel()
        {
            CollectionAssert.IsEmpty(LevelLayout.Parse(Simple).Validate(false));
        }

        [Test]
        public void Validate_ReportsMissingExitAndFloatingMarkers()
        {
            var errors = LevelLayout.Parse("P....S\n####..\n").Validate(false);
            Assert.IsTrue(errors.Exists(e => e.Contains("exactly one E")));
            Assert.IsTrue(errors.Exists(e => e.Contains("'S'") && e.Contains("ground")));
        }

        [Test]
        public void Validate_BossLevelNeedsBossPrincessAndArena()
        {
            var errors = LevelLayout.Parse("P..E\n####\n").Validate(true);
            Assert.IsTrue(errors.Exists(e => e.Contains("K")));
            Assert.IsTrue(errors.Exists(e => e.Contains("princess")));
        }

        static LevelReachability Reach(LevelLayout l) =>
            new LevelReachability(l, 3.2f, dy => dy > 3.2f ? 0f : 8f * (0.38f + Mathf.Sqrt(2f * (3.2f - dy) / 70.9f)), 4f);

        [Test]
        public void Reachability_SmallGapIsJumpable()
        {
            var l = LevelLayout.Parse("P....E\n##..##\n##..##\n");
            Assert.IsTrue(Reach(l).CanReach(0, 2, 5, 2));
        }

        [Test]
        public void Reachability_HugeGapIsNot()
        {
            var l = LevelLayout.Parse("P..........E\n#..........#\n");
            Assert.IsFalse(Reach(l).CanReach(0, 1, 11, 1));
        }

        [Test]
        public void Reachability_TallWallIsNot_ButAPlatformMakesItSo()
        {
            var wall = LevelLayout.Parse(
                ".......\n" +
                "......E\n" +
                "......#\n" +
                "......#\n" +
                "P.....#\n" +
                "#######\n");
            Assert.IsFalse(Reach(wall).CanReach(0, 1, 6, 4));

            var steps = LevelLayout.Parse(
                ".......\n" +
                "......E\n" +
                "......#\n" +
                "...===#\n" +
                "P.....#\n" +
                "#######\n");
            Assert.IsTrue(Reach(steps).CanReach(0, 1, 6, 4));
        }

        [Test]
        public void Reachability_MovingPlatformBridgesAWideGap()
        {
            var l = LevelLayout.Parse(
                "P.........E\n" +
                "#.MMM.....#\n");
            Assert.IsTrue(Reach(l).CanReach(0, 1, 10, 1));
        }
    }
}
