using UnityEngine.UIElements;

/// <summary>
/// `Build` lays out a 7-row x LaneMovementSystem.PositionsPerLane-column clickable grid via flex
/// rows/columns — used by the Party menu's pre-battle picker (OverworldMenuController) for its
/// static, click-to-select screen. The in-battle Move drag (BattleHUDController) needs a DIFFERENT
/// layout (each slot individually positioned to match real stage coordinates, hit-tested via
/// drag-release rather than clicked — see ShowStagePositionMarkers) but the SAME per-cell
/// appearance/state logic, so it calls `BuildCell` directly instead of `Build` — both paths trace
/// back to the original 2026-08-12 ask: user: "it could be similar to how the skill wheel is set
/// up, but instead it would look like a 7 by 5 grid," later refined to "drag and drop a player to
/// a location... the positions... should be hidden when in combat, but when you've selected to
/// move then it shows the possible positions."
///
/// Pure UI-builder, no state of its own — the caller supplies the occupancy data (via
/// getOccupantLabel) and receives clicks (via onCellChosen); this class doesn't know or care
/// whether it's looking at PhasixRuntimeData.preferredLaneIndex/preferredPositionIndex (Party menu)
/// or live BattleParticipant.LaneIndex/PositionIndex (battle) — see FormationSystem.IsSlotOccupied
/// for the actual exclusivity check callers run before wiring getOccupantLabel/onCellChosen.
///
/// Styling: `.formation-grid`/`.formation-grid-row`/`.formation-grid-cell` etc. live in
/// BattleHUD.uss only — OverworldMenu.uxml already references BattleHUD.uss as a second stylesheet
/// (same reason its skill-ring classes read identically to the battle scene's), so no duplication
/// is needed for the Party menu to pick these up too.
///
/// `BuildCell` (2026-08-12) is factored out of `Build` so `BattleHUDController.ShowStagePositionMarkers`
/// — the in-battle Move drag's real-stage-aligned marker set, a different LAYOUT (absolute-positioned
/// to match actual stage coordinates) but identical cell APPEARANCE/state logic — can reuse the exact
/// same "current/occupied/free" styling without duplicating it.
/// </summary>
public static class FormationGridPicker
{
    /// <summary>
    /// Builds one styled cell for the given (lane, position). `isCurrent` marks "you are here"
    /// (star, still enabled — re-choosing your own slot is a harmless no-op). `occupantLabel`
    /// non-empty (and not the current slot) marks the cell occupied-by-another (disabled). Tags
    /// `cell.userData` with `(lane, position)` so callers doing drop/hit-testing (rather than
    /// `Button.clicked`, which doesn't reliably fire during a captured-pointer drag) can read back
    /// which slot a cell represents. `onClick` may be null (the in-battle drag flow doesn't use it).
    /// </summary>
    public static Button BuildCell(int lane, int position, bool isCurrent, string occupantLabel, System.Action onClick)
    {
        bool occupiedByOther = !isCurrent && !string.IsNullOrEmpty(occupantLabel);

        var cell = new Button { text = isCurrent ? "★" : occupantLabel }; // filled star marks "you are here"
        cell.AddToClassList("formation-grid-cell");
        if (isCurrent) cell.AddToClassList("formation-grid-cell-current");
        if (occupiedByOther) cell.AddToClassList("formation-grid-cell-occupied");
        cell.SetEnabled(!occupiedByOther);
        cell.userData = (lane, position);
        if (onClick != null) cell.clicked += onClick;

        return cell;
    }

    /// <summary>
    /// currentLane/currentPosition: the slot to highlight as "you are here" (still clickable —
    /// re-choosing your own slot is a harmless no-op the caller can ignore).
    /// getOccupantLabel(lane, position): return a short label (e.g. an initial) if some OTHER
    /// occupant already holds that slot, or null/empty if it's free. Must already exclude whichever
    /// creature is doing the picking — this class has no notion of "self."
    /// onCellChosen(lane, position): invoked on click for any cell that isn't occupied by another.
    /// </summary>
    public static VisualElement Build(int currentLane, int currentPosition,
        System.Func<int, int, string> getOccupantLabel, System.Action<int, int> onCellChosen)
    {
        var grid = new VisualElement();
        grid.AddToClassList("formation-grid");

        // Iterate lane 7 -> 1 (2026-08-12 fix): the real stage renders Lane 1 (front) at the
        // BOTTOM of the screen and Lane 7 (back) at the TOP (LaneMovementSystem.GetLaneScreenTop —
        // front = larger `top` = lower on screen). Appending rows in lane-1-first order (the
        // original bug) put Lane 1 at the grid's TOP instead, inverted from the stage it
        // represents. Reversing the loop so Lane 7's row is appended first fixes this without
        // touching anything else in the method — currentLane/currentPosition/getOccupantLabel/
        // onCellChosen are all keyed by the real lane/position int, never by loop or child order.
        for (int lane = BattleLaneLayout.LaneCount; lane >= 1; lane--)
        {
            var row = new VisualElement();
            row.AddToClassList("formation-grid-row");

            for (int position = 1; position <= LaneMovementSystem.PositionsPerLane; position++)
            {
                bool isCurrent = lane == currentLane && position == currentPosition;
                string occupantLabel = getOccupantLabel?.Invoke(lane, position);
                int capturedLane = lane;
                int capturedPosition = position;

                Button cell = BuildCell(lane, position, isCurrent, occupantLabel,
                    () => onCellChosen?.Invoke(capturedLane, capturedPosition));

                row.Add(cell);
            }

            grid.Add(row);
        }

        return grid;
    }
}
