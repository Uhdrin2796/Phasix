using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// `BuildCell` builds one styled clickable slot cell, shared by two very different layouts:
/// `BattleHUDController.ShowStagePositionMarkers` (the in-battle Move drag's real-stage-aligned
/// marker set, absolute-positioned to match actual stage coordinates, hit-tested via drag-release)
/// and `BuildLivePreview` below (the Party menu's pre-battle picker). Both trace back to the
/// original 2026-08-12 ask: user: "it could be similar to how the skill wheel is set up, but
/// instead it would look like a 7 by 5 grid," later refined to "drag and drop a player to a
/// location... the positions... should be hidden when in combat, but when you've selected to move
/// then it shows the possible positions."
///
/// Pure UI-builder, no state of its own — the caller supplies the occupancy data and receives
/// clicks; this class doesn't know or care whether it's looking at
/// PhasixRuntimeData.preferredLaneIndex/preferredPositionIndex (Party menu) or live
/// BattleParticipant.LaneIndex/PositionIndex (battle) — see FormationSystem.IsSlotOccupied for the
/// actual exclusivity check callers run before wiring occupancy/onCellChosen.
///
/// Styling: `.formation-grid-cell`/`.formation-preview-*` etc. live in BattleHUD.uss only —
/// OverworldMenu.uxml already references BattleHUD.uss as a second stylesheet (same reason its
/// skill-ring classes read identically to the battle scene's), so no duplication is needed for the
/// Party menu to pick these up too.
///
/// 2026-08-17 (user: "the party positional thing in the overworld is not a direct representation
/// of what the battle scene movement looks like. Can we align those?"): the original `Build()` —
/// a flat, abstract 7x5 flex grid with no depth-scaling or spacing relationship to the real stage —
/// was REMOVED (it had exactly one caller, `OverworldMenuController.BuildFormationSection`, dead
/// code once that switched over) in favor of `BuildLivePreview`, which mirrors
/// `BattleHUDController.LayoutPlayerStageCreaturesByLane`/`ApplyLaneLayout`'s exact depth-scale/
/// position math (scaled down to fit a menu panel) so the picker now looks like a small, genuine
/// preview of the actual battle stage instead of a disconnected abstraction.
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

    /// <summary>Shrinks every real-stage px constant (LaneMovementSystem.*, PreviewBaseCreatureSizePx) so the mini-stage preview fits a Party-menu panel instead of rendering at true battle-stage size (~672x531px unscaled). Placeholder value, trivial to retune — not a locked design decision.</summary>
    public const float PreviewScaleFactor = 0.43f;

    /// <summary>Fixed clickable hit-box size (px) for a preview cell's Button, independent of its computed depth-scaled visual circle size — so a back-lane cell (smallest depth scale) never becomes uncomfortably small to actually click, even though its decorative circle does shrink. Matches the old flat grid's cell size for continuity.</summary>
    public const float PreviewMinHitTargetPx = 28f;

    /// <summary>Local mirror of BattleHUDController.StageCreatureSizePx (72f, private there) — kept as its own constant deliberately, same precedent as LaneMovementSystem.PositionColumnSpacingPx being a dedicated copy of the old shared spacing value, so a future change to one doesn't silently retune the other.</summary>
    private const float PreviewBaseCreatureSizePx = 72f;

    /// <summary>
    /// Builds a "live-style" mini preview of the real battle stage (2026-08-17) — a depth-scaled,
    /// position-spaced grid of clickable slot cells mirroring
    /// BattleHUDController.LayoutPlayerStageCreaturesByLane/ApplyLaneLayout's exact math (those
    /// methods have no scale parameter, so this is a parallel implementation at PreviewScaleFactor,
    /// not a call-through), each showing the occupying creature's actual PrimalType-tinted circle
    /// (this project's real creature-visual convention — no per-species sprite art exists yet, see
    /// BattleHUDController.SetStageCreatureColor) rather than an abstract letter-in-a-box.
    ///
    /// currentLane/currentPosition/currentSpecies: the slot (and its own species, for its circle's
    /// color) to highlight as "you are here" — still clickable, re-choosing your own slot is a
    /// harmless no-op the caller can ignore.
    /// getOccupantSpecies(lane, position): return the OTHER occupant's species if some other
    /// creature already holds that slot, or null if it's free. Must already exclude whichever
    /// creature is doing the picking — this class has no notion of "self" beyond currentLane/Position.
    /// onCellChosen(lane, position): invoked on click for EVERY cell, free or occupied (2026-08-19,
    /// user: "i want to be able to move or adjust the position of the phasix before a fight as
    /// needed" — occupied cells used to be disabled/unclickable; now every slot is clickable so the
    /// caller can implement a swap when the target is already held by another party member, letting
    /// the whole formation be rearranged from any one creature's detail view). This class has no
    /// notion of "swap" itself — it's still a pure UI builder, purely reporting which cell was
    /// clicked; the caller decides what clicking an occupied cell means.
    /// </summary>
    public static VisualElement BuildLivePreview(int currentLane, int currentPosition, PhasixData currentSpecies,
        System.Func<int, int, PhasixData> getOccupantSpecies, System.Action<int, int> onCellChosen)
    {
        float edgePaddingPx = Mathf.Max(PreviewBaseCreatureSizePx * PreviewScaleFactor * LaneMovementSystem.MaxDepthScale, PreviewMinHitTargetPx);
        float scaledWidth = LaneMovementSystem.PositionRangeWidthPx * PreviewScaleFactor;
        float scaledHeight = LaneMovementSystem.RowRangeHeightPx * PreviewScaleFactor;

        var stage = new VisualElement();
        stage.AddToClassList("formation-preview-stage");
        stage.style.width = scaledWidth + edgePaddingPx;
        stage.style.height = scaledHeight + edgePaddingPx;

        for (int lane = 1; lane <= BattleLaneLayout.LaneCount; lane++)
        {
            float depthScale = LaneMovementSystem.GetDepthScale(lane);
            float visualSizePx = PreviewBaseCreatureSizePx * PreviewScaleFactor * depthScale;
            // Lane 1 (front) gets the largest GetLaneScreenTop value (bottom of the stage), Lane 7
            // (back) the smallest (top) — same convention BattleHUDController's real stage uses,
            // reused verbatim so this preview reads with the same front-at-bottom orientation.
            float top = LaneMovementSystem.GetLaneScreenTop(lane, isPlayerSide: true) * PreviewScaleFactor + edgePaddingPx / 2f;

            for (int position = 1; position <= LaneMovementSystem.PositionsPerLane; position++)
            {
                float left = LaneMovementSystem.GetPositionOffsetPx(position) * PreviewScaleFactor + scaledWidth / 2f + edgePaddingPx / 2f;

                bool isCurrent = lane == currentLane && position == currentPosition;
                PhasixData occupant = isCurrent ? currentSpecies : getOccupantSpecies?.Invoke(lane, position);

                int capturedLane = lane;
                int capturedPosition = position;

                // Always enabled, even when occupied by another party member — clicking an occupied
                // cell is how a swap is triggered (see this method's own doc comment). This is a
                // deliberate divergence from BuildCell's own occupied-means-disabled convention,
                // which is still correct for the in-battle Move skill (you can't swap positions
                // mid-battle via Move — see ShowStagePositionMarkers, untouched by this change).
                var cell = new Button();
                cell.AddToClassList("formation-preview-cell");
                cell.style.left = left - PreviewMinHitTargetPx / 2f;
                cell.style.top = top - PreviewMinHitTargetPx / 2f;
                cell.userData = (lane, position);
                cell.clicked += () => onCellChosen?.Invoke(capturedLane, capturedPosition);

                // Decorative circle, sized/colored independently of the fixed clickable Button box
                // above (see PreviewMinHitTargetPx's own doc comment) — pickingMode = Ignore is
                // load-bearing, not decorative: without it, a front-lane circle visually overflowing
                // its Button's box would itself receive pointer events in that overflow region,
                // since UI Toolkit picking is per-element bounds, not clipped by an ancestor's
                // layout box — clicks near the edge of an oversized circle could silently miss.
                var circle = new VisualElement { pickingMode = PickingMode.Ignore };
                circle.AddToClassList("formation-preview-circle");
                circle.style.width = visualSizePx;
                circle.style.height = visualSizePx;

                if (occupant != null)
                {
                    circle.AddToClassList("formation-preview-circle-occupied");
                    circle.style.backgroundColor = PrimalTypeColor.GetColor(occupant.PrimalType);
                }
                else
                {
                    circle.AddToClassList("formation-preview-circle-free");
                }

                if (isCurrent) circle.AddToClassList("formation-preview-circle-current");

                cell.Add(circle);
                stage.Add(cell);
            }
        }

        return stage;
    }
}
