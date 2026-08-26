using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers BattleLaneLayout's pure world-space math — GetLanePosition predates this (Scene-view gizmos only); GetPositionOffset/GetStagePosition were added 2026-08-26 for Architecture_Directive_v0_1_0.md Part 3's first real Scene-creature slice.</summary>
    public class BattleLaneLayoutTests
    {
        [Test]
        public void GetPositionOffset_CenterColumn_ReturnsZero()
        {
            Assert.AreEqual(0f, BattleLaneLayout.GetPositionOffset(LaneMovementSystem.DefaultStartingPosition));
        }

        [Test]
        public void GetPositionOffset_IsMonotonicallyIncreasing_AcrossAllPositions()
        {
            float previous = BattleLaneLayout.GetPositionOffset(1);
            for (int position = 2; position <= LaneMovementSystem.PositionsPerLane; position++)
            {
                float current = BattleLaneLayout.GetPositionOffset(position);
                Assert.Greater(current, previous, $"Offset must increase from column {position - 1} to {position}.");
                previous = current;
            }
        }

        [Test]
        public void GetPositionOffset_SymmetricAroundCenter()
        {
            float leftOfCenter = BattleLaneLayout.GetPositionOffset(LaneMovementSystem.DefaultStartingPosition - 1);
            float rightOfCenter = BattleLaneLayout.GetPositionOffset(LaneMovementSystem.DefaultStartingPosition + 1);

            Assert.AreEqual(-leftOfCenter, rightOfCenter, 0.001f);
        }

        [TestCase(0, 1)]
        [TestCase(6, 5)]
        public void GetPositionOffset_ClampsOutOfRangePositions(int input, int expectedClamped)
        {
            Assert.AreEqual(BattleLaneLayout.GetPositionOffset(expectedClamped), BattleLaneLayout.GetPositionOffset(input));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetStagePosition_MatchesGetLanePosition_OnDepthAxis_AtCenterColumn(bool isPlayerSide)
        {
            Vector3 origin = new Vector3(1f, 2f, 3f);
            Vector3 lanePosition = BattleLaneLayout.GetLanePosition(origin, 4, isPlayerSide);
            Vector3 stagePosition = BattleLaneLayout.GetStagePosition(origin, 4, LaneMovementSystem.DefaultStartingPosition, isPlayerSide);

            Assert.AreEqual(lanePosition.x, stagePosition.x, 0.001f, "Depth axis (X) must be unaffected by the new position-within-lane offset.");
            Assert.AreEqual(0f, stagePosition.y - lanePosition.y, 0.001f, "Center column should add zero offset on the column axis (Y).");
        }

        [Test]
        public void GetStagePosition_DifferentPositions_ProduceDifferentColumnOffsets()
        {
            Vector3 origin = Vector3.zero;
            Vector3 left = BattleLaneLayout.GetStagePosition(origin, 1, 1, isPlayerSide: true);
            Vector3 right = BattleLaneLayout.GetStagePosition(origin, 1, LaneMovementSystem.PositionsPerLane, isPlayerSide: true);

            Assert.AreNotEqual(left.y, right.y, "Different in-lane positions must produce different column-axis offsets.");
        }
    }
}
