using System.Collections.Generic;

/// <summary>
/// Live state for one battle: both sides' participants and the actions queued for the current
/// turn. Plain C# — owned by BattleManager in Play Mode, but equally constructible with no scene
/// dependency for EditMode tests, matching PhasixRuntimeData/BondSystem's pattern.
/// </summary>
public class BattleState
{
    public readonly List<BattleParticipant> PlayerSide;
    public readonly List<BattleParticipant> EnemySide;
    public readonly List<BattleAction> QueuedActions = new List<BattleAction>();

    public BattleState(List<BattleParticipant> playerSide, List<BattleParticipant> enemySide)
    {
        PlayerSide = playerSide;
        EnemySide = enemySide;
    }
}
