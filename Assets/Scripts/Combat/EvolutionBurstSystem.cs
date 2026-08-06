using UnityEngine;

/// <summary>
/// Mid-battle evolution burst, GDD §9.3 "Bond Gauge and Evolution Burst" — the entire locked
/// design content is 4 bullets: "Bond gauge fills through skill use, timed inputs, and taking
/// hits. Higher bond = faster fill and longer burst duration. Mid-battle evolution burst governed
/// by Type K (Evolve) skill trees. Burst is temporary — creature returns to base form after
/// duration expires." Plus, from the Bond zone table (§14.2, Locked v0.4.0), Companion (40% bond)
/// -> "Evolution burst reliable" — implying burst is possible-but-unreliable below 40%, though
/// that's an inference (the GDD doesn't state a hard minimum gate).
///
/// NOT implemented (genuinely undesigned, not just placeholder-numbered): WHAT changes about the
/// creature during a burst (stat boost? higher-tier stat block? new moves? — the GDD never says).
/// This class only scaffolds the gauge fill/trigger/expiry state machine — the actual
/// "ApplyBurstEffects" step is an open hook a future skill-content pass has to fill in, not
/// something to invent here. Primordial Origin's "harder to trigger, longer once fired" passive
/// (GDD §13.3) is also not wired in — Origin isn't threaded through combat participants yet.
///
/// BattleManager's live integration (2026-08-06, user-directed — see DECISIONS.md -> [Combat])
/// uses ActivateReady, NOT TryTrigger — the gauge is shown as a visible purple bar under the
/// Aura bar (BattleHUDController), outlined yellow and clickable once FillPercent reaches
/// TriggerThreshold; the player deliberately clicks it to activate, rather than it firing
/// automatically off a hidden bond-gated chance roll. TryTrigger's reliability-chance behavior
/// still exists and is still tested, just unused by the current battle loop.
///
/// TODO: pending design — once "WHAT changes about the creature during a burst" above is
/// actually decided, the HUD needs to ADVISE the player what activating a ready burst will do
/// (2026-08-06, user-directed) — right now the yellow-ready bar promises "something happens" with
/// no preview of what, which won't hold up once there's a real effect to describe. Worth tracking
/// as an open direction, not yet decided: burst effects might end up CONFIGURABLE per creature via
/// the skill tree (Type K / Evolve trees) rather than one fixed universal formula — i.e. which
/// stat(s) a burst boosts, by how much, could be a player choice unlocked through Evolve-tree
/// skills. See DECISIONS.md -> [Combat] for the same note recorded against this session's context.
/// </summary>
public static class EvolutionBurstSystem
{
    /// <summary>Public (2026-08-06 — see DECISIONS.md -> [Combat]) so BattleHUDController can compare a gauge's live FillPercent against this same threshold to decide when to show the "ready to activate" state, without duplicating the number.</summary>
    public const float TriggerThreshold = 100f;

    /// <summary>The GDD's own "Companion" bond threshold (40%) — burst is reliable at/above this.</summary>
    private const float ReliableBondThreshold = 40f;

    /// <summary>Placeholder trigger chance when gauge is full but bond is below the reliable threshold. TODO: pending NumericalCalibration.md — the GDD only implies unreliability below 40%, gives no number.</summary>
    private const float UnreliableTriggerChancePercent = 40f;

    /// <summary>Placeholder base burst duration at 0% bond. TODO: pending NumericalCalibration.md.</summary>
    private const int BaseDurationTurns = 2;

    /// <summary>Placeholder max duration bonus from bond, reached at 100% bond. TODO: pending NumericalCalibration.md.</summary>
    private const float MaxBondDurationBonusTurns = 3f;

    /// <summary>Adds gauge fill from skill use / timed-input success / taking a hit. No-op while already bursting.</summary>
    public static void AddFill(EvolutionBurstGauge gauge, float amount)
    {
        if (gauge.IsActive) return;
        gauge.FillPercent = Mathf.Clamp(gauge.FillPercent + amount, 0f, TriggerThreshold);
    }

    /// <summary>
    /// Attempts to trigger the burst once the gauge is full. Reliable (always triggers) at/above
    /// ReliableBondThreshold bond; below it, only a placeholder chance to trigger even at full
    /// gauge. Resets fill to 0 and sets the duration on success.
    /// </summary>
    public static bool TryTrigger(EvolutionBurstGauge gauge, float bondPercent)
    {
        if (gauge.IsActive || gauge.FillPercent < TriggerThreshold) return false;

        bool reliable = bondPercent >= ReliableBondThreshold;
        if (!reliable && Random.Range(0f, 100f) >= UnreliableTriggerChancePercent) return false;

        gauge.IsActive = true;
        gauge.FillPercent = 0f;
        gauge.RemainingDurationTurns = ComputeDurationTurns(bondPercent);
        return true;
    }

    /// <summary>
    /// Manually activates a FULL gauge — the player-clicked path (2026-08-06, user-directed —
    /// see DECISIONS.md -> [Combat]: "instead of auto triggering, please make it so it becomes an
    /// activatable option... the activation can be on the bar itself"). Unlike TryTrigger, this
    /// has NO bond-based reliability chance — the whole point of a deliberate click on a bar the
    /// UI has already marked "ready" (BattleHUDController's yellow-outline state, gated on this
    /// same TriggerThreshold) is that it just works; gating a visually-ready action behind a
    /// hidden coin-flip would read as broken, not as a feature. Bond still scales the resulting
    /// burst's DURATION via ComputeDurationTurns — only the trigger-reliability roll is removed,
    /// not bond's role generally. Only succeeds when the gauge has actually reached
    /// TriggerThreshold and isn't already active — "they can only activate when the gauge is
    /// full" (2026-08-06, user-confirmed). TryTrigger is left untouched (still used nowhere in
    /// this pass, but keeps its own existing test coverage/contract intact) rather than
    /// repurposed, since its bond-gated-chance behavior is a genuinely different mechanic now.
    /// </summary>
    public static bool ActivateReady(EvolutionBurstGauge gauge, float bondPercent)
    {
        if (gauge.IsActive || gauge.FillPercent < TriggerThreshold) return false;

        gauge.IsActive = true;
        gauge.FillPercent = 0f;
        gauge.RemainingDurationTurns = ComputeDurationTurns(bondPercent);
        return true;
    }

    /// <summary>Higher bond = longer burst duration (locked mechanic, placeholder curve).</summary>
    public static int ComputeDurationTurns(float bondPercent)
    {
        float bondBonus = Mathf.Clamp01(bondPercent / 100f) * MaxBondDurationBonusTurns;
        return Mathf.RoundToInt(BaseDurationTurns + bondBonus);
    }

    /// <summary>Call once per turn a burst is active. Reverts to base form (IsActive = false) once duration expires.</summary>
    public static void TickTurn(EvolutionBurstGauge gauge)
    {
        if (!gauge.IsActive) return;

        gauge.RemainingDurationTurns--;
        if (gauge.RemainingDurationTurns <= 0)
        {
            gauge.IsActive = false;
            gauge.RemainingDurationTurns = 0;
        }
    }
}
