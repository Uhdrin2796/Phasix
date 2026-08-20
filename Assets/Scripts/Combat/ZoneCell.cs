/// <summary>
/// A single (Lane, Position) grid cell — Zone/Positional's marking granularity
/// (Attack_Pattern_Directive Part 5 Group 3). Row/Column patterns only ever vary one axis, but
/// DiagonalX needs true per-cell marking, so ZonePositionalPatternResolver always expands every
/// pattern down to this shared shape. 1-indexed on both axes, matching BattleParticipant.LaneIndex/
/// PositionIndex, LaneMovementSystem.ClampLane/ClampPosition, and FormationSystem.IsSlotOccupied —
/// no separate 0-indexed authoring scheme.
/// </summary>
public readonly struct ZoneCell
{
    public readonly int Lane;
    public readonly int Position;

    public ZoneCell(int lane, int position)
    {
        Lane = lane;
        Position = position;
    }
}
