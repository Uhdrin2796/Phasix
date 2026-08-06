using UnityEngine;

/// <summary>
/// Status duration formula, GDD §17.2 (Locked v0.7.8), transcribed verbatim: "Net = base +
/// Resonance modifier − Resolve modifier, minimum 1 turn. Positive statuses are NOT reduced by
/// target Resolve — Resolve only resists negative conditions." Matches CLAUDE.md's summary
/// exactly.
///
/// The GDD locks the FORMULA SHAPE but not how a raw Resonance/Resolve stat value converts into
/// a "modifier" turn count — that conversion isn't written anywhere. StatPerModifierPoint below
/// is a placeholder (10 stat points = ±1 turn), tagged pending NumericalCalibration.md, not a
/// locked design decision.
/// </summary>
public static class StatusDurationCalculator
{
    /// <summary>Placeholder: how many points of Resonance/Resolve produce one turn of modifier. TODO: pending NumericalCalibration.md.</summary>
    private const float StatPerModifierPoint = 10f;

    /// <summary>
    /// baseDurationTurns should come from StatusEffectCatalog.Get(type).MinDurationTurns/MaxDurationTurns
    /// (caller picks a value in that range, or rolls one) — this method just applies the
    /// Resonance/Resolve modifier and the minimum-1 floor on top of whatever base is supplied.
    /// </summary>
    public static int ComputeDuration(int baseDurationTurns, int applierResonance, int targetResolve, bool isPositiveStatus)
    {
        int resonanceModifier = Mathf.FloorToInt(applierResonance / StatPerModifierPoint);
        int resolveModifier = isPositiveStatus ? 0 : Mathf.FloorToInt(targetResolve / StatPerModifierPoint);

        return Mathf.Max(1, baseDurationTurns + resonanceModifier - resolveModifier);
    }
}
