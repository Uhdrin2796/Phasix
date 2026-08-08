using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combo detection, GDD §4.2 (Taxonomy Locked section), transcribed verbatim: "Skills from
/// different trees create combos when used in sequence. Tiers: Duo (2 skills), Trio (3), Quad (4).
/// Combos are discovered through use — not all combinations are visible initially. Instinct
/// attribute increases combo trigger chance. Bond level increases combo discovery rate above 60%
/// bond."
///
/// The trigger MECHANIC (cross-tree sequencing) is locked; the exact numeric trigger-chance
/// formula and discovery-rate curve are NOT written anywhere in the GDD/Primer/NumericalCalibration
/// — fully open. TriggerChancePercent/DiscoveryBonusPercent below are placeholders mirroring
/// TimedInputConfig's existing style (a simple linear scale with a tagged-placeholder constant),
/// not invented balance numbers presented as final.
/// </summary>
public static class ComboEngine
{
    /// <summary>Placeholder base trigger chance at 0 Instinct. TODO: pending NumericalCalibration.md.</summary>
    private const float BaseTriggerChancePercent = 10f;

    /// <summary>Placeholder per-Instinct-point trigger chance scaling. TODO: pending NumericalCalibration.md.</summary>
    private const float PerInstinctTriggerChancePercent = 1f;

    private const float MaxTriggerChancePercent = 80f;

    /// <summary>Bond level below which discovery gets no bonus — the GDD's own "above 60% bond" threshold, this part IS locked.</summary>
    private const float DiscoveryBonusBondThreshold = 60f;

    /// <summary>Placeholder max discovery-rate bonus, reached at 100% bond. TODO: pending NumericalCalibration.md.</summary>
    private const float MaxDiscoveryBonusPercent = 30f;

    /// <summary>
    /// Detects the largest combo tier satisfied by the trailing end of recentTreeTypeSequence
    /// (most-recently-used skill last). A combo requires that many consecutive most-recent skills
    /// to all come from different SkillTreeType trees — a repeat within the window breaks it.
    /// Returns null if no combo (fewer than 2 skills used yet, or the last 2 share a tree).
    /// </summary>
    public static ComboTier? DetectCombo(IReadOnlyList<SkillTreeType> recentTreeTypeSequence)
    {
        if (HasDistinctTrailingWindow(recentTreeTypeSequence, 4)) return ComboTier.Quad;
        if (HasDistinctTrailingWindow(recentTreeTypeSequence, 3)) return ComboTier.Trio;
        if (HasDistinctTrailingWindow(recentTreeTypeSequence, 2)) return ComboTier.Duo;
        return null;
    }

    private static bool HasDistinctTrailingWindow(IReadOnlyList<SkillTreeType> sequence, int windowSize)
    {
        if (sequence.Count < windowSize) return false;

        var seen = new HashSet<SkillTreeType>();
        for (int i = sequence.Count - windowSize; i < sequence.Count; i++)
        {
            if (!seen.Add(sequence[i])) return false;
        }
        return true;
    }

    /// <summary>
    /// The raw current trailing distinct-tree streak length, counting back from the most recent
    /// skill until a repeat breaks it — NOT capped at Quad's window of 4 like DetectCombo (though
    /// in practice it never exceeds 4 either, since BattleParticipant already trims its history
    /// to that same window). Drives the live skill-wheel combo-counter badge (2026-08 session, see
    /// DECISIONS.md -> [Combat]) — DetectCombo answers "is a combo satisfied right now," this
    /// answers "how long is the current streak," which is what a running counter needs.
    /// </summary>
    public static int GetDistinctTrailingStreakLength(IReadOnlyList<SkillTreeType> sequence)
    {
        var seen = new HashSet<SkillTreeType>();
        int length = 0;
        for (int i = sequence.Count - 1; i >= 0; i--)
        {
            if (!seen.Add(sequence[i])) break;
            length++;
        }
        return length;
    }

    /// <summary>Instinct-scaled trigger chance (0-100), clamped. Placeholder linear formula — see class doc comment.</summary>
    public static float ComputeTriggerChancePercent(int instinct)
    {
        float chance = BaseTriggerChancePercent + instinct * PerInstinctTriggerChancePercent;
        return Mathf.Clamp(chance, 0f, MaxTriggerChancePercent);
    }

    /// <summary>Bond-scaled discovery-rate bonus (0-MaxDiscoveryBonusPercent) above 60% bond, 0 at/below it. Placeholder linear ramp — see class doc comment.</summary>
    public static float ComputeDiscoveryBonusPercent(float bondPercent)
    {
        if (bondPercent <= DiscoveryBonusBondThreshold) return 0f;

        float progressAboveThreshold = (bondPercent - DiscoveryBonusBondThreshold) / (100f - DiscoveryBonusBondThreshold);
        return Mathf.Clamp01(progressAboveThreshold) * MaxDiscoveryBonusPercent;
    }
}
