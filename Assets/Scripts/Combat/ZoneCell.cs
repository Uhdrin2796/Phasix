/// <summary>
/// A single (Lane, Position) grid cell — Zone/Positional's marking granularity
/// (Attack_Pattern_Directive Part 5 Group 3). Row/Column patterns only ever vary one axis, but
/// DiagonalX needs true per-cell marking, so ZonePositionalPatternResolver always expands every
/// pattern down to this shared shape. 1-indexed on both axes, matching BattleParticipant.LaneIndex/
/// PositionIndex, LaneMovementSystem.ClampLane/ClampPosition, and FormationSystem.IsSlotOccupied —
/// no separate 0-indexed authoring scheme.
///
/// 2026-08-20 (Split Attention, item 8): gained IsReal, defaulting to true so every existing 2-arg
/// call site (Row/Column/DiagonalX) is unaffected. A cell with IsReal == false is still highlighted,
/// still glows, still gets the ground-strike flash — visually IDENTICAL to a real cell, by design
/// (Split Attention's whole point is that the telegraph looks uniformly dangerous) — it just never
/// deals damage at resolution, regardless of who's standing there. See
/// BattleManager.ResolveZonePositionalAttack's per-cell damage loop for the one place this is
/// actually checked; every visual method (highlight/glow/strike VFX) intentionally ignores it.
/// </summary>
public readonly struct ZoneCell
{
    public readonly int Lane;
    public readonly int Position;
    public readonly bool IsReal;

    public ZoneCell(int lane, int position, bool isReal = true)
    {
        Lane = lane;
        Position = position;
        IsReal = isReal;
    }
}
