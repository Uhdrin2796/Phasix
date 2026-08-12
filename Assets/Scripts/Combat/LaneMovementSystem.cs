using UnityEngine;

/// <summary>
/// The real (not placeholder-gizmo-only) 7-lane formation math — Combat_Directive_v0_1_0.md Part
/// 2/3, Attack_Pattern_Directive_v0_1_0.md Part 8. Pure static math, no MonoBehaviour/UI dependency,
/// fully EditMode-testable.
///
/// 2026-08-12 correction (user, live playtest: "wait isn't the lane like 7 horizontal rows?"):
/// lanes are 7 horizontal ROWS stacked vertically (Y-axis) — Lane 1 (front) nearer the bottom of the
/// stage and largest, Lane 7 (back) higher up and smallest — NOT 7 positions along a single
/// horizontal line, which is what this class originally implemented (GetLaneScreenLeft, producing
/// VisualElement.style.left values). That earlier version directly caused two live-playtest issues:
/// depth scale changing during purely-horizontal Approach movement made no visual sense (nothing
/// about a horizontal-only move should change apparent depth), and matched neither
/// Combat_Directive's "3/4 perspective illusion" framing nor the user's own original depth-scaling
/// request (explicitly Y-position-based: "as they move down the screen, decreasing Y, they get
/// bigger"). Renamed GetLaneScreenLeft -> GetLaneScreenTop, producing style.top values instead.
/// Melee Approach/Return (BeatSequenceRunner) are horizontal (X) gap-closing moves that do NOT
/// change which row a combatant occupies — see that class's own doc comment — so depth scale, tied
/// to row, now stays constant throughout a Beat Sequence instead of needing the continuous-during-
/// tween machinery the horizontal-lane version required (GetDepthScaleFromLeft/
/// VisualElementTweening.TweenLeftWithDepthScale — both removed this same pass, since nothing
/// changes row during movement in the current design).
///
/// Movement cost is deliberately NOT represented anywhere in this class — Combat_Directive Part 3's
/// "movement cost model [DECISION LOCKED]" rule says cost is decided by the calling context. Neither
/// IsAdjacent nor StepToward (kept as general row-index utilities, even though the melee Beat
/// Sequence path no longer calls them — see BeatSequenceRunner) takes a cost parameter.
/// </summary>
public static class LaneMovementSystem
{
    /// <summary>Combat_Directive Part 2's stated default starting lane ("Mid").</summary>
    public const int DefaultStartingLane = 4;

    /// <summary>
    /// Pixel height of one lane "row," VisualElement.style.top-space. Placeholder — TODO: pending
    /// design — numerical calibration (NumericalCalibration.md). Doubled 30f -> 60f, then raised
    /// again 60f -> 90f, then reduced ~15% 90f -> 76.5f (2026-08-12, user: "the lanes can be reduced
    /// by around 15%" — see DECISIONS.md -> [Combat]).
    /// </summary>
    public const float LaneRowHeightPx = 76.5f;

    /// <summary>
    /// Symmetric spread (in px) between occupants sharing a lane/row, per Combat_Directive's "non-
    /// exclusive occupancy" rule. Placeholder — TODO: pending numerical calibration. Value (150f)
    /// and its full derivation history predate the row/column axis swap (see DECISIONS.md ->
    /// [Combat] "In-lane spacing moved from vertical to horizontal," "Approach's 'closing lunge'")
    /// — still horizontal (style.left) either way, since occupants sharing a row spread out ALONG
    /// that row, which is the horizontal axis regardless of which axis represents depth.
    /// </summary>
    public const float InLaneSpacingPx = 150f;

    public static int ClampLane(int lane) => Mathf.Clamp(lane, 1, BattleLaneLayout.LaneCount);

    /// <summary>
    /// "Already in the same row" — kept as a general row-index utility (same-index equality) even
    /// though the melee Beat Sequence path no longer calls it (Approach/Return are horizontal-only
    /// and never change row — see BeatSequenceRunner's class doc comment). Still meaningful for any
    /// future mechanic that DOES need a row-adjacency check (e.g. a ranged skill's lane-distance
    /// requirement, Attack_Pattern_Directive Part 10 item 2).
    /// </summary>
    public static bool IsAdjacent(int attackerLane, int targetLane) => ClampLane(attackerLane) == ClampLane(targetLane);

    /// <summary>
    /// Moves exactly one lane toward targetLane, clamped to the valid range. Returns currentLane
    /// unchanged once it equals targetLane. Cost-agnostic by construction — callers decide what, if
    /// anything, a row change costs. Kept as a general utility — see IsAdjacent's doc comment.
    /// </summary>
    public static int StepToward(int currentLane, int targetLane)
    {
        int clampedCurrent = ClampLane(currentLane);
        int clampedTarget = ClampLane(targetLane);
        if (clampedCurrent == clampedTarget) return clampedCurrent;
        return clampedCurrent < clampedTarget ? clampedCurrent + 1 : clampedCurrent - 1;
    }

    /// <summary>Depth scale at Lane 1 (front/closest) — TODO: pending numerical calibration. 1.15f -> 1.10f (2026-08-12, user-directed — see DECISIONS.md -> [Combat]).</summary>
    public const float MaxDepthScale = 1.10f;

    /// <summary>Depth scale at Lane 7 (back/furthest) — TODO: pending numerical calibration. 0.55f -> 0.85f (2026-08-12, user-directed — see DECISIONS.md -> [Combat]) — narrows the front-to-back size range considerably, independent of LaneRowHeightPx (row SPACING is unchanged).</summary>
    public const float MinDepthScale = 0.85f;

    /// <summary>
    /// Depth scale for a lane/row INDEX — Lane 1 (front) largest, Lane 7 (back) smallest, per
    /// Combat_Directive Part 2 ("scaling is smooth and continuous — not stepped" — continuous across
    /// the 7 discrete row values via this linear interpolation, not continuous-during-movement
    /// anymore, since movement no longer changes row — see this file's class doc comment).
    /// </summary>
    public static float GetDepthScale(int laneIndex)
    {
        float t = (ClampLane(laneIndex) - 1) / (float)(BattleLaneLayout.LaneCount - 1);
        return Mathf.Lerp(MaxDepthScale, MinDepthScale, t);
    }

    /// <summary>
    /// Total pixel span of the 7-row range (row 1's position to row 7's, not counting either row's
    /// own creature size) — `(LaneCount - 1) * LaneRowHeightPx`. Exposed so
    /// BattleHUDController can size PlayerStageArea/EnemyStageArea's height to exactly contain it
    /// (plus one creature's own height) without duplicating the arithmetic.
    /// </summary>
    public static float RowRangeHeightPx => (BattleLaneLayout.LaneCount - 1) * LaneRowHeightPx;

    /// <summary>
    /// VisualElement.style.top value (within that side's own stage-area box) for a lane/row — Lane 1
    /// (front) nearer the bottom of the stage (larger `top`), Lane 7 (back) higher up (smaller
    /// `top`). Includes a fixed `RowRangeHeightPx / 2` centering term so the result is always >= 0
    /// and lands correctly against a container sized to `RowRangeHeightPx + creatureHeight` with
    /// `.stage-side`'s `translate: -50% -50%` centering — the same "padding a box's size without
    /// compensating children's position shifts them off-anchor" issue already hit once on the
    /// horizontal axis (DECISIONS.md -> [Combat] "a little too far left") applies identically here,
    /// just worked out algebraically up front instead of as a separate compensation step, since here
    /// the "padding" isn't optional margin — it's exactly the row range itself, so the compensation
    /// term is a fixed constant `(LaneCount - 1) / 2f * LaneRowHeightPx`, independent of creature
    /// size (the creature-height terms cancel out of the derivation).
    ///
    /// Identical formula for both sides (no mirroring) — unlike the old horizontal version, a row's
    /// depth reads the same regardless of which side's formation it belongs to; both sides' front
    /// rows should read as equally prominent, not mirrored top-vs-bottom.
    /// </summary>
    public static float GetLaneScreenTop(int laneIndex, bool isPlayerSide)
    {
        int clampedLane = ClampLane(laneIndex);
        return (DefaultStartingLane - clampedLane) * LaneRowHeightPx + RowRangeHeightPx / 2f;
    }

    /// <summary>
    /// Symmetric horizontal spread (px) for the occupantIndexInLane-th of occupantCountInLane
    /// combatants sharing one lane/row, per Combat_Directive's non-exclusive-occupancy rule ("spaced
    /// apart along the lane so they read as distinct... appearing in a line"). A single occupant
    /// gets 0 offset; multiple occupants spread symmetrically around 0, along the row (horizontal
    /// axis) they share.
    /// </summary>
    public static float GetInLaneSpacingOffsetPx(int occupantIndexInLane, int occupantCountInLane)
    {
        if (occupantCountInLane <= 1) return 0f;
        return (occupantIndexInLane - (occupantCountInLane - 1) / 2f) * InLaneSpacingPx;
    }
}
