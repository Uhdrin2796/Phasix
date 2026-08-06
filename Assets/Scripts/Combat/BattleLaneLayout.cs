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
}
