/// <summary>
/// Common Aura -> stat point allocation, Progression_Directive_v0_1_0.md "Free Allocation Model":
/// "Players spend Common Aura to allocate stat points freely across all available attributes. No
/// fixed track. No prescribed growth path." Pure rules-enforcement static class over
/// PhasixRuntimeData, matching BondSystem/PersonalitySystem's established convention for this
/// project — takes an externally-owned PhasixRuntimeData and mutates it, no state of its own.
///
/// Spends against phasix.commonAura, adds to phasix.baseStats (the Aura-spendable layer — NOT
/// unnamedPool, which only grows through devolution), gated by AuraTierCeiling so a low-tier
/// Phasix can't be stat-pumped past its tier. AuraCostPerStatPoint is a placeholder 1:1 exchange
/// rate — the Directive's own pending list includes "Common Aura cost per stat point by tier," so
/// this is not locked as flat/uniform, just built that way until a real curve exists.
/// </summary>
public static class AuraStatAllocationSystem
{
    /// <summary>Placeholder flat exchange rate. TODO: pending NumericalCalibration.md — may need to vary by tier once designed.</summary>
    public const int AuraCostPerStatPoint = 1;

    /// <summary>
    /// Spends AuraCostPerStatPoint Common Aura to add 1 point to the given stat, if the Phasix has
    /// enough Aura AND baseStats.Total is below its tier ceiling. Returns false (no-op, no Aura
    /// spent) if either condition fails.
    /// </summary>
    public static bool TryAllocateStatPoint(PhasixRuntimeData phasix, int evolutionTier, StatType stat)
    {
        if (phasix.commonAura < AuraCostPerStatPoint) return false;

        int ceiling = AuraTierCeiling.ComputeCeiling(evolutionTier, phasix.aptitude);
        if (phasix.baseStats.Total >= ceiling) return false;

        phasix.commonAura -= AuraCostPerStatPoint;
        phasix.baseStats = AddToStat(phasix.baseStats, stat, 1);
        return true;
    }

    /// <summary>How many more stat points can currently be allocated before hitting the tier ceiling.</summary>
    public static int GetRemainingCeilingRoom(PhasixRuntimeData phasix, int evolutionTier)
        => System.Math.Max(0, AuraTierCeiling.ComputeCeiling(evolutionTier, phasix.aptitude) - phasix.baseStats.Total);

    private static StatBlock AddToStat(StatBlock block, StatType stat, int amount)
    {
        switch (stat)
        {
            case StatType.Vitality: block.Vitality += amount; break;
            case StatType.Force: block.Force += amount; break;
            case StatType.Resonance: block.Resonance += amount; break;
            case StatType.Guard: block.Guard += amount; break;
            case StatType.Ward: block.Ward += amount; break;
            case StatType.Resolve: block.Resolve += amount; break;
            case StatType.Instinct: block.Instinct += amount; break;
            case StatType.Aura: block.Aura += amount; break;
        }
        return block;
    }
}
