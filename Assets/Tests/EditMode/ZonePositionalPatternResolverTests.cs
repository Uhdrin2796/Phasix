using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers ZonePositionalPatternResolver.GetMarkedCells — the pure Row/Column/DiagonalX expansion plus SurroundingBurst/FacingArrowhead's target-relative real/fake cell math (Attack_Pattern_Directive Part 5 Group 3, 2026-08-20, Split Attention follow-up).</summary>
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

        [Test]
        public void GetMarkedCells_SurroundingBurst_CenterAndDiagonalsAreReal_EdgesAreFake()
        {
            var skill = MakeSkill();
            SetPrivateField(skill, "_zonePositionalPattern", ZonePositionalPatternType.SurroundingBurst);

            IReadOnlyList<ZoneCell> cells = ZonePositionalPatternResolver.GetMarkedCells(skill, targetLane: 4, targetPosition: 3);

            Assert.AreEqual(9, cells.Count); // full 3x3, no clamping needed away from any edge
            Assert.AreEqual(5, cells.Count(c => c.IsReal)); // center + 4 diagonals
            Assert.AreEqual(4, cells.Count(c => !c.IsReal)); // 4 orthogonal edges

            Assert.IsTrue(cells.Any(c => c.Lane == 4 && c.Position == 3 && c.IsReal)); // center is damaging
            Assert.IsTrue(cells.Any(c => c.Lane == 3 && c.Position == 2 && c.IsReal)); // diagonal
            Assert.IsTrue(cells.Any(c => c.Lane == 5 && c.Position == 4 && c.IsReal)); // diagonal
            Assert.IsTrue(cells.Any(c => c.Lane == 3 && c.Position == 3 && !c.IsReal)); // orthogonal edge, safe
            Assert.IsTrue(cells.Any(c => c.Lane == 4 && c.Position == 2 && !c.IsReal)); // orthogonal edge, safe
        }

        [Test]
        public void GetMarkedCells_SurroundingBurst_DropsOutOfRangeCellsNearCorner()
        {
            var skill = MakeSkill();
            SetPrivateField(skill, "_zonePositionalPattern", ZonePositionalPatternType.SurroundingBurst);

            // Target at the exact (Lane 1, Position 1) corner — half the 3x3 falls off the grid.
            IReadOnlyList<ZoneCell> cells = ZonePositionalPatternResolver.GetMarkedCells(skill, targetLane: 1, targetPosition: 1);

            Assert.AreEqual(4, cells.Count); // only the (lane 1-2, position 1-2) quadrant survives
            Assert.IsTrue(cells.All(c => c.Lane >= 1 && c.Position >= 1));
            Assert.AreEqual(2, cells.Count(c => c.IsReal)); // center + the one surviving diagonal
        }

        [Test]
        public void GetMarkedCells_FacingArrowhead_TenReal_TwoFake_AwayFromEdges()
        {
            var skill = MakeSkill();
            SetPrivateField(skill, "_zonePositionalPattern", ZonePositionalPatternType.FacingArrowhead);

            IReadOnlyList<ZoneCell> cells = ZonePositionalPatternResolver.GetMarkedCells(skill, targetLane: 4, targetPosition: 2);

            Assert.AreEqual(12, cells.Count); // full shape, nothing clamped this far from any edge
            Assert.AreEqual(10, cells.Count(c => c.IsReal));
            Assert.AreEqual(2, cells.Count(c => !c.IsReal));

            Assert.IsTrue(cells.Any(c => c.Lane == 4 && c.Position == 2 && !c.IsReal)); // target's own cell, safe
            Assert.IsTrue(cells.Any(c => c.Lane == 4 && c.Position == 3 && !c.IsReal)); // one step toward the tip, safe
            Assert.IsTrue(cells.Any(c => c.Lane == 4 && c.Position == 4 && c.IsReal)); // the tip itself, damaging
            Assert.IsTrue(cells.Any(c => c.Lane == 4 && c.Position == 1 && c.IsReal)); // wall directly behind, damaging
            Assert.IsTrue(cells.Any(c => c.Lane == 2 && c.Position == 1 && c.IsReal)); // wall's far lane edge, damaging
            Assert.IsTrue(cells.Any(c => c.Lane == 6 && c.Position == 1 && c.IsReal)); // wall's other far lane edge, damaging
        }

        [Test]
        public void GetMarkedCells_FacingArrowhead_DropsTipAndSafeStepNearPositionEdge()
        {
            var skill = MakeSkill();
            SetPrivateField(skill, "_zonePositionalPattern", ZonePositionalPatternType.FacingArrowhead);

            // Target already at Position 5 (max) — the safe step (+1) and the tip (+2) both fall off
            // the grid, leaving only the target's own cell as the sole fake cell in what's left.
            IReadOnlyList<ZoneCell> cells = ZonePositionalPatternResolver.GetMarkedCells(skill, targetLane: 4, targetPosition: 5);

            Assert.AreEqual(8, cells.Count);
            Assert.AreEqual(1, cells.Count(c => !c.IsReal));
            Assert.IsTrue(cells.Any(c => c.Lane == 4 && c.Position == 5 && !c.IsReal)); // target's own cell, still safe
            Assert.IsFalse(cells.Any(c => c.Position == 6 || c.Position == 7)); // never fabricated out-of-range cells
        }
    }
}
