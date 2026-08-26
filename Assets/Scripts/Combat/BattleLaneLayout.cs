using UnityEngine;

/// <summary>
/// Placeholder world-space layout for the 7-lane depth system (Combat_Directive_v0_1_0.md Part
/// 2). Exact lane spacing/depth-scale values are explicitly "pending numerical calibration" —
/// this is a reasonable placeholder so BattleStageGizmos (Scene-view dev visualization only) and
/// any future lane-position code have something concrete to work with. Never rendered to
/// players — the runtime HUD is Sonny 2-style (header HP list + stage + action bar) with no
/// visible lane lines, per user direction. Lane 1 = front (closest to the opposing side), Lane 7
/// = back (furthest/safest).
/// </summary>
public static class BattleLaneLayout
{
    public const int LaneCount = 7;

    /// <summary>Spacing between adjacent lanes, in world units. Placeholder — TODO: pending NumericalCalibration.md.</summary>
    private const float LaneSpacing = 0.8f;

    /// <summary>Half the gap between the two sides' front lanes (Lane 1), in world units.</summary>
    private const float SideGapHalf = 1f;

    /// <summary>
    /// World-space position for a given lane, relative to a stage origin. isPlayerSide mirrors the
    /// layout across the origin so both sides' Lane 1 face each other in the middle.
    /// </summary>
    public static Vector3 GetLanePosition(Vector3 stageOrigin, int laneIndex, bool isPlayerSide)
    {
        int clampedLane = Mathf.Clamp(laneIndex, 1, LaneCount);
        float depth = SideGapHalf + (clampedLane - 1) * LaneSpacing;
        float x = isPlayerSide ? -depth : depth;
        return stageOrigin + new Vector3(x, 0f, 0f);
    }

    /// <summary>Spacing between adjacent in-lane positions (columns), in world units. Placeholder — TODO: pending NumericalCalibration.md, same status as LaneSpacing/SideGapHalf above.</summary>
    private const float PositionSpacing = 0.3f;

    /// <summary>
    /// World-space offset for a position (column) within a lane — mirrors
    /// LaneMovementSystem.GetPositionOffsetPx's pixel-space logic in world units, reusing that
    /// class's PositionsPerLane/DefaultStartingPosition constants as the single source of truth
    /// for the 5-column layout itself (only the per-column spacing differs, since that's a
    /// pixel-vs-world-unit concern).
    /// </summary>
    public static float GetPositionOffset(int position)
    {
        int clampedPosition = Mathf.Clamp(position, 1, LaneMovementSystem.PositionsPerLane);
        return (clampedPosition - LaneMovementSystem.DefaultStartingPosition) * PositionSpacing;
    }

    /// <summary>
    /// World-space position for a given lane AND position-within-lane — combines GetLanePosition's
    /// depth axis (X) with GetPositionOffset's column axis (Y, perpendicular to lane depth).
    /// Added for the first Phase 3 slice (the real Scene "shadow" stage creature) — GetLanePosition
    /// alone (lane depth only) predates this and stays as BattleStageGizmos' Scene-view-only entry
    /// point, unchanged.
    /// </summary>
    public static Vector3 GetStagePosition(Vector3 stageOrigin, int laneIndex, int positionIndex, bool isPlayerSide)
    {
        Vector3 lanePosition = GetLanePosition(stageOrigin, laneIndex, isPlayerSide);
        return lanePosition + new Vector3(0f, GetPositionOffset(positionIndex), 0f);
    }
}
