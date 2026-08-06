using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Turn-resolution rules for the Phase 3 battle loop — queueing, damage application, and win/loss
/// detection. Static, operates on an external BattleState, matching BondSystem.cs's pattern (no
/// MonoBehaviour, no scene dependency, easy to EditMode-test in isolation). No speed/priority
/// ordering yet — Combat_Directive_v0_1_0.md Part 5: "full turn order model... pending combat
/// system design" — actions resolve strictly in queue order for this pass.
/// </summary>
public static class BattleEngine
{
    /// <summary>
    /// Queues a basic attack. No-op if either combatant is null or already down. baseDamage
    /// defaults to BattleConfig.PlaceholderAttackDamage (used by callers that don't compute real
    /// damage, e.g. tests) — real gameplay callers pass DamageCalculator's output explicitly.
    /// damageMultiplier folds in both the offensive and defensive action-command outcomes
    /// (Combat_Directive Part 4) — defaults to 1 (no change) for callers that don't use the timing
    /// system. BattleEngine itself never computes damage; it just applies what it's given.
    /// </summary>
    public static void QueueBasicAttack(BattleState state, BattleParticipant attacker, BattleParticipant target, float damageMultiplier = 1f, int baseDamage = BattleConfig.PlaceholderAttackDamage)
    {
        if (state == null || attacker == null || target == null) return;
        if (!attacker.IsAlive || !target.IsAlive) return;

        state.QueuedActions.Add(new BattleAction(attacker, target, damageMultiplier, baseDamage));
    }

    /// <summary>
    /// Resolves every queued action in order, applying BaseDamage x DamageMultiplier. Skips an
    /// action if either side died earlier in this same resolution pass. Clears the queue when done.
    /// Returns what was actually applied (attacker/target/final damage) so callers — e.g.
    /// BattleManager, for the battle log — know the real numbers without recomputing/duplicating
    /// this rounding logic themselves. Skipped actions produce no result entry.
    /// </summary>
    public static List<BattleActionResult> ResolveQueuedActions(BattleState state)
    {
        var results = new List<BattleActionResult>();
        if (state == null) return results;

        foreach (var action in state.QueuedActions)
        {
            if (!action.Attacker.IsAlive || !action.Target.IsAlive) continue;

            int damage = Mathf.Max(0, Mathf.RoundToInt(action.BaseDamage * action.DamageMultiplier));
            action.Target.ApplyDamage(damage);
            EventBus.Raise_DamageTaken(action.Target.RuntimeData, damage);
            results.Add(new BattleActionResult(action.Attacker, action.Target, damage));
        }

        state.QueuedActions.Clear();
        return results;
    }

    /// <summary>Won = every enemy down. Lost = every player-side participant down. Else InProgress.</summary>
    public static BattleOutcome CheckOutcome(BattleState state)
    {
        if (state.EnemySide.Count > 0 && state.EnemySide.All(p => !p.IsAlive)) return BattleOutcome.Won;
        if (state.PlayerSide.Count > 0 && state.PlayerSide.All(p => !p.IsAlive)) return BattleOutcome.Lost;

        return BattleOutcome.InProgress;
    }
}
