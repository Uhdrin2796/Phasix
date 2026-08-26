using System.Collections.Generic;

/// <summary>
/// Snapshot of one completed battle, passed to EventBus.OnBattleWon/OnBattleLost. Carries both
/// sides' final BattleParticipant state so subscribers (AuraManager and the loss-state handler,
/// both Roadmap_v2 Mo 8) can compute drops/costs without re-deriving battle state themselves.
/// Loss state is currency/item cost only per CLAUDE.md — Victory here just tells subscribers
/// which path to take, not how much is lost (that's Step 5).
/// </summary>
public class BattleResult
{
    public readonly bool Victory;
    public readonly List<BattleParticipant> PlayerParticipants;
    public readonly List<BattleParticipant> EnemyParticipants;

    public BattleResult(bool victory, List<BattleParticipant> playerParticipants, List<BattleParticipant> enemyParticipants)
    {
        Victory = victory;
        PlayerParticipants = playerParticipants;
        EnemyParticipants = enemyParticipants;
    }
}
