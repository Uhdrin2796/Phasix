using UnityEngine.UIElements;
using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers FormationGridPicker.BuildLivePreview/BuildCell. BuildLivePreview (2026-08-17,
    /// replacing the old flat Build()) lays every one of the 35 (lane, position) slots out as
    /// absolutely-positioned children of one flat container (no row grouping, unlike the old
    /// Build()), so these tests locate cells by their userData tag rather than by child/row index —
    /// still covering the same orientation invariant the original 2026-08-12 fix established (Lane
    /// 1 renders at the BOTTOM of the real stage, Lane 7 at the TOP — see
    /// LaneMovementSystem.GetLaneScreenTop's own doc comment) and the userData tagging the
    /// in-battle Move drag's hit-testing depends on.
    /// </summary>
    public class FormationGridPickerTests
    {
        private static VisualElement FindCell(VisualElement stage, int lane, int position)
        {
            foreach (VisualElement child in stage.Children())
            {
                var (l, p) = ((int, int))child.userData;
                if (l == lane && p == position) return child;
            }
            Assert.Fail($"No cell found for lane {lane}, position {position}.");
            return null;
        }

        [Test]
        public void BuildLivePreview_Lane7Cell_RendersAboveLane1Cell_MatchingRealStageOrientation()
        {
            VisualElement stage = FormationGridPicker.BuildLivePreview(currentLane: 4, currentPosition: 3,
                currentSpecies: null, getOccupantSpecies: null, onCellChosen: null);

            VisualElement lane7Cell = FindCell(stage, BattleLaneLayout.LaneCount, 3);
            VisualElement lane1Cell = FindCell(stage, 1, 3);

            Assert.Less(lane7Cell.style.top.value.value, lane1Cell.style.top.value.value,
                "Lane 7 (back) must render ABOVE Lane 1 (front) — the real stage renders Lane 1 at the bottom.");
        }

        [Test]
        public void BuildLivePreview_ContainsAllThirtyFiveSlots_EachWithCorrectUserData()
        {
            VisualElement stage = FormationGridPicker.BuildLivePreview(currentLane: 4, currentPosition: 3,
                currentSpecies: null, getOccupantSpecies: null, onCellChosen: null);

            Assert.AreEqual(BattleLaneLayout.LaneCount * LaneMovementSystem.PositionsPerLane, stage.childCount);

            VisualElement cell = FindCell(stage, BattleLaneLayout.LaneCount, 4);
            var (lane, position) = ((int, int))cell.userData;
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
