using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers LaneMovementSystem's pure math (Combat_Directive Part 2/3, Attack_Pattern_Directive Part 8) — the real 7-lane traversal system built 2026-08-11, see DECISIONS.md -> [Combat].</summary>
    public class LaneMovementSystemTests
    {
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(7, 7)]
        [TestCase(8, 7)]
        [TestCase(-5, 1)]
        [TestCase(4, 4)]
        public void ClampLane_ClampsToValidRange(int input, int expected)
        {
            Assert.AreEqual(expected, LaneMovementSystem.ClampLane(input));
        }

        [Test]
        public void IsAdjacent_SameLaneIndex_ReturnsTrue()
        {
            Assert.IsTrue(LaneMovementSystem.IsAdjacent(4, 4));
        }

        [Test]
        public void IsAdjacent_DifferentLaneIndex_ReturnsFalse_BothDirections()
        {
            Assert.IsFalse(LaneMovementSystem.IsAdjacent(3, 4));
            Assert.IsFalse(LaneMovementSystem.IsAdjacent(4, 3));
        }

        [Test]
        public void StepToward_AlreadyAtTarget_ReturnsUnchanged()
        {
            Assert.AreEqual(4, LaneMovementSystem.StepToward(4, 4));
        }

        [Test]
        public void StepToward_MovesExactlyOneLaneTowardHigherTarget()
        {
            Assert.AreEqual(3, LaneMovementSystem.StepToward(2, 7));
        }

        [Test]
        public void StepToward_MovesExactlyOneLaneTowardLowerTarget()
        {
            Assert.AreEqual(5, LaneMovementSystem.StepToward(6, 1));
        }

        [Test]
        public void StepToward_RepeatedCalls_ConvergeOnTargetAndStay_NeverOvershoot()
        {
            int lane = 1;
            int target = 5;
            for (int i = 0; i < 10; i++) // far more steps than needed
                lane = LaneMovementSystem.StepToward(lane, target);

            Assert.AreEqual(target, lane);
        }

        [Test]
        public void GetDepthScale_Lane1_IsLargestValue()
        {
            float lane1 = LaneMovementSystem.GetDepthScale(1);
            float lane7 = LaneMovementSystem.GetDepthScale(BattleLaneLayout.LaneCount);

            Assert.Greater(lane1, lane7, "Lane 1 (front) should scale larger than Lane 7 (back) — Combat_Directive Part 2.");
        }

        [Test]
        public void GetDepthScale_IsMonotonicallyNonIncreasing_AcrossAllLanes()
        {
            float previous = LaneMovementSystem.GetDepthScale(1);
            for (int lane = 2; lane <= BattleLaneLayout.LaneCount; lane++)
            {
                float current = LaneMovementSystem.GetDepthScale(lane);
                Assert.LessOrEqual(current, previous, $"Depth scale must not increase from lane {lane - 1} to {lane}.");
                previous = current;
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetLaneScreenTop_Lane1_SitsLowerOnScreenThanLane7(bool isPlayerSide)
        {
            float lane1Top = LaneMovementSystem.GetLaneScreenTop(1, isPlayerSide);
            float lane7Top = LaneMovementSystem.GetLaneScreenTop(BattleLaneLayout.LaneCount, isPlayerSide);

            Assert.Greater(lane1Top, lane7Top,
                "Lane 1 (front) should sit lower on screen (larger `top`) than Lane 7 (back) — 2026-08-12 correction: lanes are vertical rows, front=lower/bigger, back=higher/smaller.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetLaneScreenTop_IsMonotonicallyDecreasing_AcrossAllLanes(bool isPlayerSide)
        {
            float previous = LaneMovementSystem.GetLaneScreenTop(1, isPlayerSide);
            for (int lane = 2; lane <= BattleLaneLayout.LaneCount; lane++)
            {
                float current = LaneMovementSystem.GetLaneScreenTop(lane, isPlayerSide);
                Assert.Less(current, previous, $"Screen top must decrease from lane {lane - 1} to {lane} (moving further back/higher up).");
                previous = current;
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetLaneScreenTop_NeverNegative(bool isPlayerSide)
        {
            for (int lane = 1; lane <= BattleLaneLayout.LaneCount; lane++)
                Assert.GreaterOrEqual(LaneMovementSystem.GetLaneScreenTop(lane, isPlayerSide), 0f,
                    $"Lane {lane}'s screen top should never be negative — the centering compensation term exists specifically to guarantee this.");
        }

        [Test]
        public void GetLaneScreenTop_BothSides_ProduceIdenticalMapping_NoMirroring()
        {
            for (int lane = 1; lane <= BattleLaneLayout.LaneCount; lane++)
            {
                Assert.AreEqual(LaneMovementSystem.GetLaneScreenTop(lane, isPlayerSide: true),
                    LaneMovementSystem.GetLaneScreenTop(lane, isPlayerSide: false), 0.001f,
                    $"Lane {lane}'s vertical position should be identical for both sides — unlike the old horizontal mapping, rows don't mirror by side.");
            }
        }

        [Test]
        public void GetLaneScreenTop_TotalRangeMatchesRowRangeHeightPx()
        {
            float lane1Top = LaneMovementSystem.GetLaneScreenTop(1, isPlayerSide: true);
            float lane7Top = LaneMovementSystem.GetLaneScreenTop(BattleLaneLayout.LaneCount, isPlayerSide: true);

            Assert.AreEqual(LaneMovementSystem.RowRangeHeightPx, lane1Top - lane7Top, 0.001f);
        }

        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(5, 5)]
        [TestCase(6, 5)]
        [TestCase(-3, 1)]
        [TestCase(3, 3)]
        public void ClampPosition_ClampsToValidRange(int input, int expected)
        {
            Assert.AreEqual(expected, LaneMovementSystem.ClampPosition(input));
        }

        [Test]
        public void GetPositionOffsetPx_CenterColumn_ReturnsZero()
        {
            Assert.AreEqual(0f, LaneMovementSystem.GetPositionOffsetPx(LaneMovementSystem.DefaultStartingPosition));
        }

        [Test]
        public void GetPositionOffsetPx_IsMonotonicallyIncreasing_AcrossAllPositions()
        {
            float previous = LaneMovementSystem.GetPositionOffsetPx(1);
            for (int position = 2; position <= LaneMovementSystem.PositionsPerLane; position++)
            {
                float current = LaneMovementSystem.GetPositionOffsetPx(position);
                Assert.Greater(current, previous, $"Offset must increase from column {position - 1} to {position}.");
                previous = current;
            }
        }

        [Test]
        public void GetPositionOffsetPx_SymmetricAroundCenter()
        {
            float leftOfCenter = LaneMovementSystem.GetPositionOffsetPx(LaneMovementSystem.DefaultStartingPosition - 1);
            float rightOfCenter = LaneMovementSystem.GetPositionOffsetPx(LaneMovementSystem.DefaultStartingPosition + 1);

            Assert.AreEqual(-leftOfCenter, rightOfCenter, 0.001f);
        }

        [Test]
        public void GetPositionOffsetPx_DoesNotDependOnOtherOccupants()
        {
            // Unlike the removed GetInLaneSpacingOffsetPx, a column's offset is a pure function of
            // its own index — calling it twice for the same position must always agree.
            Assert.AreEqual(LaneMovementSystem.GetPositionOffsetPx(2), LaneMovementSystem.GetPositionOffsetPx(2));
        }

        [Test]
        public void PositionRangeWidthPx_MatchesTotalSpanOfExtremeColumns()
        {
            float leftmost = LaneMovementSystem.GetPositionOffsetPx(1);
            float rightmost = LaneMovementSystem.GetPositionOffsetPx(LaneMovementSystem.PositionsPerLane);

            Assert.AreEqual(LaneMovementSystem.PositionRangeWidthPx, rightmost - leftmost, 0.001f);
        }
    }
}
