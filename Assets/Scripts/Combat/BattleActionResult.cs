/// <summary>
/// What actually happened when one queued BattleAction resolved — returned by
/// BattleEngine.ResolveQueuedActions so callers (BattleManager, for the battle log) know the real
/// applied damage without recomputing/duplicating the rounding logic themselves.
/// </summary>
public readonly struct BattleActionResult
{
    public readonly BattleParticipant Attacker;
    public readonly BattleParticipant Target;
    public readonly int DamageApplied;

    public BattleActionResult(BattleParticipant attacker, BattleParticipant target, int damageApplied)
    {
        Attacker = attacker;
        Target = target;
        DamageApplied = damageApplied;
    }
}
