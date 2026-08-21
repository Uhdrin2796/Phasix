using System;
using System.Collections.Generic;

/// <summary>
/// Pure static math expanding a SkillData's ZonePositionalPattern into the full set of marked
/// (Lane, Position) cells (Attack_Pattern_Directive Part 5 Group 3). Same tier as
/// LaneMovementSystem/FormationSystem — no MonoBehaviour, no state, fully EditMode-testable.
///
/// Row/Column are computed as a cross-product against the OTHER axis's full range (e.g. Row
/// [1, 3, 5, 7] x every Position 1..5 = 20 cells) — this is what gives Row/Column their "hits the
/// whole lane/column, not just one cell" feel. DiagonalX returns a single shared, hand-authored
/// 13-cell table instead of computing anything, since every DiagonalX skill marks the identical X.
///
/// DiagonalXCells derivation (2026-08-20, confirmed against LaneMovementSystem's real screen
/// mapping — Lane 1 = front/bottom of stage, Lane 7 = back/top; Position 1 = left, Position 5 =
/// right, per GetLaneScreenTop/GetPositionOffsetPx): proportional round-off across the 7-lane x
/// 5-position grid, position(lane) = 1 + (7-lane)/6*4 for the top-left-to-bottom-right line and
/// position(lane) = 5 - (7-lane)/6*4 for the top-right-to-bottom-left line, one cell per lane row so
/// the X reads as a continuous line across all 7 rows. Both lines share cell (4,3), the grid's exact
/// center — expected for an X, not a bug. Hand-authored (fixed data), not computed at runtime, so it
/// can be adjusted directly here without touching the formula if it doesn't read right live.
///
/// SurroundingBurstOffsets/FacingArrowheadOffsets (Split Attention, 2026-08-20) are also
/// hand-authored, fixed (laneOffset, positionOffset, isReal) tables, but computed RELATIVE to a
/// live target position at resolution time (targetLane/targetPosition below) rather than from
/// SkillData's own authored fields — the first patterns in this codebase to depend on where the
/// defender actually is when the skill resolves. Offsets that land outside the real 1-7/1-5 grid
/// (target near a stage edge) are dropped, not clamped — clamping risked silently duplicating an
/// offset cell onto one already produced by a different offset.
/// </summary>
public static class ZonePositionalPatternResolver
{
    private static readonly ZoneCell[] DiagonalXCells =
    {
        // Top-left -> bottom-right
        new ZoneCell(7, 1), new ZoneCell(6, 2), new ZoneCell(5, 2), new ZoneCell(4, 3),
        new ZoneCell(3, 4), new ZoneCell(2, 4), new ZoneCell(1, 5),
        // Top-right -> bottom-left (shares the center cell (4,3) with the line above)
        new ZoneCell(7, 5), new ZoneCell(6, 4), new ZoneCell(5, 4),
        new ZoneCell(3, 2), new ZoneCell(2, 2), new ZoneCell(1, 1),
    };

    // 3x3 centered on the target. Center + the 4 diagonal corners are real (forces an actual move —
    // standing still is damaging); the 4 orthogonal edges are the safe cells.
    private static readonly (int laneOffset, int positionOffset, bool isReal)[] SurroundingBurstOffsets =
    {
        (-1, -1, true),  (-1, 0, false), (-1, 1, true),
        ( 0, -1, false), ( 0, 0, true),  ( 0, 1, false),
        ( 1, -1, true),  ( 1, 0, false), ( 1, 1, true),
    };

    // Chevron/arrowhead, tip toward increasing Position ("facing" — see this file's class doc
    // comment and ZonePositionalPatternType's own doc comment for why that's a fixed, scoped
    // convention rather than a general facing system). A solid 5-cell wall directly behind the
    // target (positionOffset -1, every laneOffset -2..+2), tapering through a hollow throat that
    // holds the target's own cell (0,0) and one safe step toward the tip (0,+1), down to a single
    // point two steps out (0,+2). Mirror positionOffset's sign if this is ever needed for an
    // enemy-side target (out of scope this pass — enemies face the opposite direction).
    private static readonly (int laneOffset, int positionOffset, bool isReal)[] FacingArrowheadOffsets =
    {
        (-2, -1, true),
        (-1, -1, true), (-1, 0, true), (-1, 1, true),
        ( 0, -1, true), ( 0, 0, false), ( 0, 1, false), ( 0, 2, true),
        ( 1, -1, true), ( 1, 0, true), ( 1, 1, true),
        ( 2, -1, true),
    };

    /// <summary>
    /// Returns the full marked-cell set for a skill's configured Zone/Positional pattern. Empty if
    /// ZonePositionalPattern is None. targetLane/targetPosition are only consulted by the
    /// target-relative patterns (SurroundingBurst, FacingArrowhead) — ignored otherwise, so existing
    /// callers/tests that only care about Row/Column/DiagonalX don't need to supply them.
    /// </summary>
    public static IReadOnlyList<ZoneCell> GetMarkedCells(SkillData skill, int targetLane = 0, int targetPosition = 0)
    {
        if (skill == null) return Array.Empty<ZoneCell>();

        switch (skill.ZonePositionalPattern)
        {
            case ZonePositionalPatternType.Row:
                return AllCellsWhereLaneIn(skill.ZonePositionalRowLanes);
            case ZonePositionalPatternType.Column:
                return AllCellsWherePositionIn(skill.ZonePositionalColumnPositions);
            case ZonePositionalPatternType.DiagonalX:
                return DiagonalXCells;
            case ZonePositionalPatternType.SurroundingBurst:
                return BuildOffsetCells(SurroundingBurstOffsets, targetLane, targetPosition);
            case ZonePositionalPatternType.FacingArrowhead:
                return BuildOffsetCells(FacingArrowheadOffsets, targetLane, targetPosition);
            default:
                return Array.Empty<ZoneCell>();
        }
    }

    private static List<ZoneCell> BuildOffsetCells((int laneOffset, int positionOffset, bool isReal)[] offsets, int targetLane, int targetPosition)
    {
        var cells = new List<ZoneCell>();
        foreach ((int laneOffset, int positionOffset, bool isReal) in offsets)
        {
            int lane = targetLane + laneOffset;
            int position = targetPosition + positionOffset;
            if (lane < 1 || lane > BattleLaneLayout.LaneCount) continue;
            if (position < 1 || position > LaneMovementSystem.PositionsPerLane) continue;
            cells.Add(new ZoneCell(lane, position, isReal));
        }
        return cells;
    }

    private static List<ZoneCell> AllCellsWhereLaneIn(IReadOnlyList<int> lanes)
    {
        var cells = new List<ZoneCell>();
        if (lanes == null) return cells;
        for (int i = 0; i < lanes.Count; i++)
        {
            int lane = LaneMovementSystem.ClampLane(lanes[i]);
            for (int position = 1; position <= LaneMovementSystem.PositionsPerLane; position++)
            {
                cells.Add(new ZoneCell(lane, position));
            }
        }
        return cells;
    }

    private static List<ZoneCell> AllCellsWherePositionIn(IReadOnlyList<int> positions)
    {
        var cells = new List<ZoneCell>();
        if (positions == null) return cells;
        for (int i = 0; i < positions.Count; i++)
        {
            int position = LaneMovementSystem.ClampPosition(positions[i]);
            for (int lane = 1; lane <= BattleLaneLayout.LaneCount; lane++)
            {
                cells.Add(new ZoneCell(lane, position));
            }
        }
        return cells;
    }
}
