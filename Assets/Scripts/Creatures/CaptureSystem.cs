using UnityEngine;

/// <summary>
/// Wild Phasix capture. GDD confirms the mechanic exists ("Every enemy is capturable") and that
/// difficulty should vary by species/rarity (§18.5 "Capture difficulty scaling" is listed as an
/// explicitly undesigned bullet; the Celestial creature access-paths table separately notes
/// capture rates are "extremely low" for that rarity tier with no number given) — but NO capture
/// formula, NO capture item, and NO percentage exists anywhere in the docs. This is fully open,
/// blocked on the still-pending §22 Economy design session (NumericalCalibration.md: "Capture
/// item costs and probabilities: PENDING").
///
/// ComputeCaptureChancePercent below is a minimal, clearly-placeholder formula (lower target HP%
/// -> higher chance, the standard genre convention) — NOT a locked design decision, and
/// deliberately has no item-modifier parameter since no capture item type has been designed yet.
/// Revisit entirely once §22 exists rather than treating this as a foundation to build on.
/// </summary>
public static class CaptureSystem
{
    /// <summary>Placeholder floor chance even at full target HP. TODO: pending §22 design + NumericalCalibration.md.</summary>
    private const float BaseCaptureChancePercent = 10f;

    /// <summary>Placeholder max bonus chance at 0% target HP. TODO: pending §22 design + NumericalCalibration.md.</summary>
    private const float MaxLowHPBonusPercent = 60f;

    /// <summary>Computes a 0-100 capture chance from the target's current HP fraction. Lower HP -> higher chance.</summary>
    public static float ComputeCaptureChancePercent(int targetCurrentHP, int targetMaxHP)
    {
        float hpFraction = targetMaxHP > 0 ? Mathf.Clamp01((float)targetCurrentHP / targetMaxHP) : 0f;
        float chance = BaseCaptureChancePercent + (1f - hpFraction) * MaxLowHPBonusPercent;
        return Mathf.Clamp(chance, 0f, 95f); // never a guaranteed capture — matches "every enemy is capturable" as "possible," not "trivial"
    }

    /// <summary>Rolls against ComputeCaptureChancePercent. Raises EventBus.OnPhasixCaptured on success.</summary>
    public static bool AttemptCapture(PhasixRuntimeData target, int targetCurrentHP, int targetMaxHP)
    {
        float chance = ComputeCaptureChancePercent(targetCurrentHP, targetMaxHP);
        bool success = Random.Range(0f, 100f) < chance;

        if (success) EventBus.Raise_PhasixCaptured(target);
        return success;
    }
}
