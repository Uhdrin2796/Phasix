/// <summary>
/// One queued attack for the current turn. Attacker and target are resolved at queue time — before
/// any damage from this turn is applied — so actions decided earlier in a turn don't retarget onto
/// a combatant that died later in the same resolution pass; BattleEngine re-checks IsAlive at
/// resolve time instead and simply skips stale actions. Only a basic attack exists until the skill
/// tree framework (Roadmap_v2 Mo 6 Wk 3+) replaces this with real SkillData-driven actions.
///
/// BaseDamage is computed by the caller (BattleManager, via DamageCalculator's real formula) before
/// queueing — BattleEngine itself never computes damage, just applies what it's given. Defaults to
/// BattleConfig.PlaceholderAttackDamage for callers that don't use DamageCalculator, e.g. tests.
///
/// DamageMultiplier folds together both the attacker's offensive action-command bonus and the
/// target's defensive action-command reduction (Combat_Directive_v0_1_0.md Part 4) into one factor
/// — also computed by the caller before queueing, applied after BaseDamage per CLAUDE.md's "apply
/// timed bonus after formula." Defaults to 1 (no bonus/reduction).
/// </summary>
public readonly struct BattleAction
{
    public readonly BattleParticipant Attacker;
    public readonly BattleParticipant Target;
    public readonly float DamageMultiplier;
    public readonly int BaseDamage;

    public BattleAction(BattleParticipant attacker, BattleParticipant target, float damageMultiplier = 1f, int baseDamage = BattleConfig.PlaceholderAttackDamage)
    {
        Attacker = attacker;
        Target = target;
        DamageMultiplier = damageMultiplier;
        BaseDamage = baseDamage;
    }
}
