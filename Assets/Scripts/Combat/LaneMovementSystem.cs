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
///
/// 2026-08-12 follow-up — EXCLUSIVE 5-position formation grid (SUPERSEDES the "non-exclusive
/// occupancy, symmetric auto-spread" model this class originally shipped with — see DECISIONS.md ->
/// [Combat] "Lane occupancy — non-exclusive, in-lane visual spacing," now superseded): each of the 7
/// rows has exactly 5 fixed horizontal positions (columns), and at most ONE combatant may occupy a
/// given (lane, position) pair at a time — user: "lets just have 5 positions across a lane. Then you
/// can preset which position you want to be in... only one position can be filled at a time." A
/// position's screen offset (GetPositionOffsetPx) is now a FIXED lookup, not derived from "how many
/// others currently share this row" — GetInLaneSpacingOffsetPx (occupant-count-based spread) is
/// removed as dead weight now that occupancy is exclusive, not shared. Exclusivity itself is NOT
/// enforced by this class (pure math, no state) — see FormationSystem.IsSlotOccupied for the
/// occupied-check used by both the Party menu's pre-battle picker and the in-battle Move skill.
/// </summary>
public static class LaneMovementSystem
{
    /// <summary>Combat_Directive Part 2's stated default starting lane ("Mid").</summary>
    public const int DefaultStartingLane = 4;

    /// <summary>Number of fixed horizontal positions (columns) within a single row — 2026-08-12, user-directed ("5 positions across a lane").</summary>
    public const int PositionsPerLane = 5;

    /// <summary>Center column of the 5 — the default starting position, mirroring DefaultStartingLane's "Mid" framing.</summary>
    public const int DefaultStartingPosition = 3;

    /// <summary>
    /// Pixel height of one lane "row," VisualElement.style.top-space. Placeholder — TODO: pending
    /// design — numerical calibration (NumericalCalibration.md). Doubled 30f -> 60f, then raised
    /// again 60f -> 90f, then reduced ~15% 90f -> 76.5f (2026-08-12, user: "the lanes can be reduced
    /// by around 15%" — see DECISIONS.md -> [Combat]).
    /// </summary>
    public const float LaneRowHeightPx = 76.5f;

    /// <summary>
    /// Enemy-side single-occupant centering baseline ONLY (2026-08-12: renamed in intent, not
    /// value, from the removed occupant-spread system — kept as its own constant, untouched, so
    /// reworking the player side's column grid can't accidentally shift the enemy side, which stays
    /// scoped out of this pass — multi-enemy battles don't exist yet, see
    /// BattleHUDController.ApplyEnemyLaneDepthScale). Do NOT reuse this for the player's 5-column
    /// grid math — see PositionColumnSpacingPx.
    /// </summary>
    public const float InLaneSpacingPx = 150f;

    /// <summary>
    /// Fixed pixel distance between adjacent columns in the 5-position formation grid. Placeholder —
    /// TODO: pending numerical calibration. Same tuned value as the old InLaneSpacingPx (150f,
    /// derived from Sonny 2's skill-wheel radius so two adjacent occupants' wheels never overlap —
    /// see DECISIONS.md -> [Combat] "In-lane spacing moved from vertical to horizontal") — a
    /// SEPARATE constant, not a reuse of InLaneSpacingPx, so the enemy side's baseline (unaffected
    /// by this pass) and the player's real column grid can be tuned independently later.
    /// </summary>
    public const float PositionColumnSpacingPx = 150f;

    public static int ClampLane(int lane) => Mathf.Clamp(lane, 1, BattleLaneLayout.LaneCount);

    /// <summary>Clamps a column/position index to the valid 1..PositionsPerLane range.</summary>
    public static int ClampPosition(int position) => Mathf.Clamp(position, 1, PositionsPerLane);

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
    /// FIXED horizontal offset (px, relative to the row's center) for one of the 5 columns —
    /// column DefaultStartingPosition (3, center) is 0; columns spread symmetrically to either side
    /// at PositionColumnSpacingPx intervals. Unlike the removed GetInLaneSpacingOffsetPx, this does
    /// NOT depend on how many other combatants are present — a given column's screen position is
    /// always the same, since occupancy is now exclusive (at most one combatant per (lane, position)
    /// pair — see this file's class doc comment).
    /// </summary>
    public static float GetPositionOffsetPx(int position)
    {
        return (ClampPosition(position) - DefaultStartingPosition) * PositionColumnSpacingPx;
    }

    /// <summary>
    /// Total pixel span of the 5-column position range (column 1's offset to column 5's) —
    /// `(PositionsPerLane - 1) * PositionColumnSpacingPx`. Exposed so BattleHUDController can size
    /// PlayerStageArea's width to exactly contain every column (plus one creature's own width)
    /// without duplicating the arithmetic — mirrors RowRangeHeightPx's role on the vertical axis.
    /// </summary>
    public static float PositionRangeWidthPx => (PositionsPerLane - 1) * PositionColumnSpacingPx;

    /// <summary>
    /// Extra horizontal offset (1.25 columns' worth) applied on top of the normal centering
    /// compensation for the PLAYER side only — 2026-08-12, user: "the 2 columns on the right are
    /// interferring with the health hud [player nameplates]... move the grid over by 2 columns."
    /// Then, same session, after live-testing that fix: "seems like i overcompensated on the move.
    /// lets move the grid to the left by half a column" (2 -> 1.5), then again "adjust it to be
    /// 1.25F i think that is better" (1.5 -> 1.25, final value).
    /// The player nameplate sidebar sits in the top-left corner of the screen; without this shift
    /// the formation grid's leftmost columns render close enough to overlap it. Applied uniformly
    /// to BOTH real creature positioning (BattleHUDController.LayoutPlayerStageCreaturesByLane) and
    /// the Move-drag markers (ShowStagePositionMarkers) — they must move together, since a marker's
    /// position is a promise of exactly where the creature will end up if dropped there.
    ///
    /// Deliberately NOT baked into PositionRangeWidthPx/PositionColumnSpacingPx itself (per the
    /// user's explicit "don't shrink the grid") — this is a pure positional shift, the grid's own
    /// width/spacing is untouched.
    ///
    /// Enemy-side mirroring (flagged by the user, not yet built — enemy stage/Move UI stays
    /// deferred, see DECISIONS.md -> [Combat]): the enemy nameplate sidebar sits in the top-RIGHT
    /// corner instead, so a future enemy-side version of this same fix would need the opposite
    /// sign (shift LEFT, toward center, not right) — same magnitude, mirrored direction, matching
    /// how GetLaneScreenTop already produces an identical (non-mirrored) vertical mapping for both
    /// sides while GetPositionOffsetPx's caller-applied horizontal centering has always been
    /// side-specific plumbing (the two stage-side containers anchor at opposite screen edges).
    /// </summary>
    public const float PlayerNameplateClearanceShiftPx = 1.25f * PositionColumnSpacingPx;
}
