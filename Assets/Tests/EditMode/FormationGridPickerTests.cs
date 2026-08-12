using UnityEngine.UIElements;
using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers FormationGridPicker.Build/BuildCell — specifically the 2026-08-12 orientation fix
    /// (the grid used to render Lane 1 as its TOP row, inverted from the real stage, which renders
    /// Lane 1 at the BOTTOM — see LaneMovementSystem.GetLaneScreenTop's own doc comment) and the
    /// userData tagging the in-battle Move drag's hit-testing depends on.
    /// </summary>
    public class FormationGridPickerTests
    {
        [Test]
        public void Build_FirstRow_IsLane7_MatchingRealStageTopOrientation()
        {
            VisualElement grid = FormationGridPicker.Build(currentLane: 4, currentPosition: 3, getOccupantLabel: null, onCellChosen: null);

            VisualElement firstRow = grid[0];
            var (lane, _) = ((int, int))firstRow[0].userData;

            Assert.AreEqual(BattleLaneLayout.LaneCount, lane, "The grid's first (topmost) row must be Lane 7 (back) — the real stage renders Lane 7 at the top.");
        }

        [Test]
        public void Build_LastRow_IsLane1_MatchingRealStageBottomOrientation()
        {
            VisualElement grid = FormationGridPicker.Build(currentLane: 4, currentPosition: 3, getOccupantLabel: null, onCellChosen: null);

            VisualElement lastRow = grid[grid.childCount - 1];
            var (lane, _) = ((int, int))lastRow[0].userData;

            Assert.AreEqual(1, lane, "The grid's last (bottommost) row must be Lane 1 (front) — the real stage renders Lane 1 at the bottom.");
        }

        [Test]
        public void Build_CellUserData_MatchesItsRealLaneAndPosition()
        {
            VisualElement grid = FormationGridPicker.Build(currentLane: 4, currentPosition: 3, getOccupantLabel: null, onCellChosen: null);

            // Row 0 = Lane 7 (post-fix); its 4th cell (index 3) should be position 4.
            var (lane, position) = ((int, int))grid[0][3].userData;
            Assert.AreEqual(BattleLaneLayout.LaneCount, lane);
            Assert.AreEqual(4, position);
        }

        [Test]
        public void BuildCell_Current_IsStarredAndEnabled()
        {
            Button cell = FormationGridPicker.BuildCell(lane: 4, position: 3, isCurrent: true, occupantLabel: null, onClick: null);

            Assert.AreEqual("★", cell.text);
            Assert.IsTrue(cell.enabledSelf);
            Assert.IsTrue(cell.ClassListContains("formation-grid-cell-current"));
        }

        [Test]
        public void BuildCell_OccupiedByOther_IsDisabled()
        {
            Button cell = FormationGridPicker.BuildCell(lane: 4, position: 3, isCurrent: false, occupantLabel: "U", onClick: null);

            Assert.IsFalse(cell.enabledSelf);
            Assert.IsTrue(cell.ClassListContains("formation-grid-cell-occupied"));
        }

        [Test]
        public void BuildCell_Free_IsEnabledWithNoLabel()
        {
            Button cell = FormationGridPicker.BuildCell(lane: 4, position: 3, isCurrent: false, occupantLabel: null, onClick: null);

            Assert.IsTrue(cell.enabledSelf);
            Assert.IsFalse(cell.ClassListContains("formation-grid-cell-occupied"));
            Assert.IsFalse(cell.ClassListContains("formation-grid-cell-current"));
        }
    }
}
