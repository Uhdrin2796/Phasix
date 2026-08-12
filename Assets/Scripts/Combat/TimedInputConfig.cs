using UnityEngine;

/// <summary>
/// Placeholder constants and formulas for the action-command system. Defense was changed
/// (2026-08-05, user-directed — see DECISIONS.md -> [Combat]) from a damage-reduction multiplier
/// to full-avoidance Dodge/Parry, inspired by Clair Obscur: Expedition 33: Dodge is a wider/easier
/// window that avoids the hit outright; Parry is a narrower/harder window (nested inside the Dodge
/// window on the shared bar — see BattleHUDController.RunDefenseTimedInput) that avoids the hit
/// AND triggers an automatic counter-attack. Both fail the same way as a total miss (full damage,
/// no extra penalty) — "reward, don't punish" per Combat_Directive's own design intent.
///
/// Offense was reworked to a matching two-tier structure (2026-08-11, user-directed — see
/// DECISIONS.md -> [Combat]): rather than a single success/miss check with a cosmetic-only
/// "perfect" sub-flash, offense now shares Dodge's own tolerance/window ("Good" tier — guarantees
/// the hit at normal strength) and Parry's own tolerance/window ("Perfect" tier — nested inside
/// Good, grants a damage spike) outright, so the two precision bands are identical by
/// construction, not independently tuned lookalikes. Unlike defense, offense does NOT get a free
/// miss: landing outside both bands now applies a real damage penalty — "reward perfects, punish
/// misses" was an explicit, deliberate departure from defense's "reward, don't punish" rule for
/// this session's change. "Exact timing windows, success thresholds, and damage modifiers are
/// pending numerical calibration" per the Directive — every value here is a reasonable placeholder
/// tagged for NumericalCalibration.md, not a locked design decision.
/// </summary>
public static class TimedInputConfig
{
    /// <summary>Dodge is the "safe" defensive option — wider window than Parry, avoid-only. Also reused as offense's "Good" tier base (see class doc comment).</summary>
    public const float DodgeBaseWindowPercent = 20f;

    /// <summary>Parry is the "risky" defensive option — narrower window than offense, avoid + counter-attack.</summary>
    public const float ParryBaseWindowPercent = 6f;

    /// <summary>
    /// Ring-ratio tolerance half-width: the converging marker ring's radius, divided by the fixed
    /// target ring's radius, must land within [1 - halfWidth, 1 + halfWidth] at click time to
    /// succeed — i.e. the TOTAL success spread is 2x halfWidth. Retuned twice on 2026-08-11
    /// (user-directed, tied to the projectile-timing-sync pass — see DECISIONS.md -> [Combat]):
    /// first pass Dodge/Parry 0.25/0.10 -> 0.15/0.05 (30%/10% total spread); second pass, after
    /// live playtesting the first still felt wide, Dodge/Parry -> 0.10/0.025 (20%/5% total spread,
    /// both symmetrical around 1.0 by construction). Offense's Good/Perfect tiers (added
    /// 2026-08-11) reuse these two values directly rather than defining offense-specific copies —
    /// see class doc comment. Both remain placeholders, not tuned balance numbers.
    /// </summary>
    public const float DodgeToleranceHalfWidth = 0.10f;
    public const float ParryToleranceHalfWidth = 0.025f;

    /// <summary>Window growth per point of Instinct — "higher Instinct = larger window" (CLAUDE.md).</summary>
    private const float PerInstinctWindowBonusPercent = 0.6f;

    /// <summary>Max window bonus from bond, reached at 100% bond — "Bond level adds minor flat bonus to window" (CLAUDE.md).</summary>
    private const float MaxBondWindowBonusPercent = 6f;

    /// <summary>Window is clamped to this range so it's never trivial or effectively impossible.</summary>
    private const float MinWindowPercent = 5f;
    private const float MaxWindowPercent = 60f;

    /// <summary>
    /// Seconds for the marker to sweep once across the bar. Shared by offense and defense —
    /// defense used to sweep Parry faster than Dodge (ParryMarkerSweepDuration, removed
    /// 2026-08-05) back when Dodge/Parry ran as two separate timed-input passes; now both zones
    /// are drawn on ONE shared bar with ONE marker (RunDefenseTimedInput), so a single shared
    /// sweep speed is the only option — Parry's difficulty is expressed entirely by its narrower,
    /// nested zone width instead.
    /// </summary>
    public const float MarkerSweepDuration = 1.2f;

    /// <summary>Outgoing damage multiplier when the offensive action command lands outside both the Good and Perfect bands — the new "punish a miss" penalty (2026-08-11, user-directed).</summary>
    public const float MissDamageMultiplier = 0.5f;

    /// <summary>Outgoing damage multiplier when the offensive action command lands in the Good band (Dodge's tolerance) — the baseline/"standard" hit, no bonus and no penalty. Renamed from SuccessDamageMultiplier; lowered from 1.5f to 1.0f (2026-08-11, user-directed, second pass after playtesting) so only Perfect ever grants a damage bonus.</summary>
    public const float GoodDamageMultiplier = 1.0f;

    /// <summary>Outgoing damage multiplier when the offensive action command lands in the tighter Perfect band (Parry's tolerance) — the reward for precision timing (2026-08-11, user-directed).</summary>
    public const float PerfectDamageMultiplier = 2.0f;

    /// <summary>Computes a window from an explicit base (e.g. DodgeBaseWindowPercent, ParryBaseWindowPercent) plus Instinct/bond scaling.</summary>
    public static float ComputeWindowPercent(float baseWindowPercent, int instinct, float bondPercent)
    {
        float bondBonus = Mathf.Clamp01(bondPercent / 100f) * MaxBondWindowBonusPercent;
        float total = baseWindowPercent + instinct * PerInstinctWindowBonusPercent + bondBonus;
        return Mathf.Clamp(total, MinWindowPercent, MaxWindowPercent);
    }

    /// <summary>
    /// Scales a ring-ratio tolerance half-width (DodgeToleranceHalfWidth/ParryToleranceHalfWidth) by
    /// the same Instinct/bond curve ComputeWindowPercent already uses
    /// — "higher Instinct = larger window" (CLAUDE.md) still applies, just to a ratio tolerance
    /// instead of a bar-position window now. Proportional: at 0 Instinct/bond this returns exactly
    /// baseToleranceHalfWidth; at 2x the computed window percent, it returns 2x the tolerance.
    /// </summary>
    public static float ComputeToleranceHalfWidth(float baseToleranceHalfWidth, float baseWindowPercent, int instinct, float bondPercent)
    {
        float computedWindowPercent = ComputeWindowPercent(baseWindowPercent, instinct, bondPercent);
        return baseToleranceHalfWidth * (computedWindowPercent / baseWindowPercent);
    }
}
