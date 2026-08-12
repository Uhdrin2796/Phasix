using System.Collections.Generic;

/// <summary>
/// Exclusive-slot occupancy check for the 7x5 formation grid (LaneMovementSystem.PositionsPerLane) —
/// shared by the Party menu's pre-battle picker (checking PhasixRuntimeData.preferredLaneIndex/
/// preferredPositionIndex against party-mates) and the in-battle Move skill (checking
/// BattleParticipant.LaneIndex/PositionIndex against alive allies). 2026-08-12, user: "only one
/// position can be filled at a time." Pure math, no state of its own — callers project whichever
/// data type they hold (PhasixRuntimeData, BattleParticipant, ...) into plain (lane, position) pairs,
/// already excluding whichever occupant is asking (a creature checking its OWN current slot must not
/// see itself as blocking).
/// </summary>
public static class FormationSystem
{
    public static bool IsSlotOccupied(IEnumerable<(int lane, int position)> occupiedSlots, int lane, int position)
    {
        foreach ((int otherLane, int otherPosition) in occupiedSlots)
        {
            if (otherLane == lane && otherPosition == position) return true;
        }
        return false;
    }
}
