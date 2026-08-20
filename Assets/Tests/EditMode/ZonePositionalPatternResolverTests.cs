using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers ZonePositionalPatternResolver.GetMarkedCells — the pure Row/Column/DiagonalX expansion (Attack_Pattern_Directive Part 5 Group 3, 2026-08-20).</summary>
    public class ZonePositionalPatternResolverTests
    {
        private static SkillData MakeSkill()
        {
            return ScriptableObject.CreateInstance<SkillData>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            typeof(SkillData).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(target, value);
        }

        [Test]
        public void GetMarkedCells_NullSkill_ReturnsEmpty()
        {
            Assert.IsEmpty(ZonePositionalPatternResolver.GetMarkedCells(null));
        }

        [Test]
        public void GetMarkedCells_PatternNone_ReturnsEmpty()
        {
            var skill = MakeSkill();
            Assert.IsEmpty(ZonePositionalPatternResolver.GetMarkedCells(skill));
        }

        [Test]
        public void GetMarkedCells_Row_ExpandsToEveryPositionInEachListedLane()
        {
            var skill = MakeSkill();
            SetPrivateField(skill, "_zonePositionalPattern", ZonePositionalPatternType.Row);
            SetPrivateField(skill, "_zonePositionalRowLanes", new[] { 1, 3, 5, 7 });

            IReadOnlyList<ZoneCell> cells = ZonePositionalPatternResolver.GetMarkedCells(skill);

            Assert.AreEqual(20, cells.Count); // 4 lanes x 5 positions
            Assert.IsTrue(cells.Any(c => c.Lane == 1 && c.Position == 1));
            Assert.IsTrue(cells.Any(c => c.Lane == 7 && c.Position == 5));
            Assert.IsFalse(cells.Any(c => c.Lane == 2)); // lane 2 was never listed
            Assert.IsFalse(cells.Any(c => c.Lane == 4));
        }

        [Test]
        public void GetMarkedCells_Column_ExpandsToEveryLaneAtEachListedPosition()
        {
            var skill = MakeSkill();
            SetPrivateField(skill, "_zonePositionalPattern", ZonePositionalPatternType.Column);
            SetPrivateField(skill, "_zonePositionalColumnPositions", new[] { 1, 3, 5 });

            IReadOnlyList<ZoneCell> cells = ZonePositionalPatternResolver.GetMarkedCells(skill);

            Assert.AreEqual(21, cells.Count); // 3 positions x 7 lanes
            Assert.IsTrue(cells.Any(c => c.Lane == 1 && c.Position == 1));
            Assert.IsTrue(cells.Any(c => c.Lane == 7 && c.Position == 5));
            Assert.IsFalse(cells.Any(c => c.Position == 2)); // position 2 was never listed
            Assert.IsFalse(cells.Any(c => c.Position == 4));
        }

        [Test]
        public void GetMarkedCells_DiagonalX_ReturnsSharedThirteenCellTable()
        {
            var skill = MakeSkill();
            SetPrivateField(skill, "_zonePositionalPattern", ZonePositionalPatternType.DiagonalX);

            IReadOnlyList<ZoneCell> cells = ZonePositionalPatternResolver.GetMarkedCells(skill);

            Assert.AreEqual(13, cells.Count);
            // Corners and the shared center cell where both diagonal lines cross.
            Assert.IsTrue(cells.Any(c => c.Lane == 7 && c.Position == 1)); // top-left
            Assert.IsTrue(cells.Any(c => c.Lane == 1 && c.Position == 5)); // bottom-right
            Assert.IsTrue(cells.Any(c => c.Lane == 7 && c.Position == 5)); // top-right
            Assert.IsTrue(cells.Any(c => c.Lane == 1 && c.Position == 1)); // bottom-left
            Assert.AreEqual(1, cells.Count(c => c.Lane == 4 && c.Position == 3)); // center, not duplicated
        }

        [Test]
        public void GetMarkedCells_Row_ClampsOutOfRangeLane()
        {
            var skill = MakeSkill();
            SetPrivateField(skill, "_zonePositionalPattern", ZonePositionalPatternType.Row);
            SetPrivateField(skill, "_zonePositionalRowLanes", new[] { 99 });

            IReadOnlyList<ZoneCell> cells = ZonePositionalPatternResolver.GetMarkedCells(skill);

            Assert.AreEqual(5, cells.Count);
            Assert.IsTrue(cells.All(c => c.Lane == BattleLaneLayout.LaneCount));
        }
    }
}
