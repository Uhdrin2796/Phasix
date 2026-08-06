using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Resonance Bonus alignment check, Progression_Directive_v0_1_0.md "Resonance Bonus Layer"
/// section: "Stat points allocated to attributes that align with a Phasix's emotional type
/// generate Resonance Bonuses... A player who consistently invests in grief-aligned stats on a
/// grief-type Phasix gets meaningfully more out of those points than a player who cross-allocates
/// freely." The Directive names the mechanic and gives three flavor-category bullets for what a
/// bonus might be (aligned passives, improved scaling, type-specific bonuses) but locks no
/// alignment table and no bonus magnitude — both explicitly "pending NumericalCalibration.md".
///
/// PROXY DECISION: the Directive's alignment concept is per-species emotionalType (e.g. "grief"),
/// which has no locked stat-alignment mapping anywhere — emotionalType is an open per-species
/// string, not an enumerated/mapped taxonomy. Rather than inventing a new emotional-type -> stat
/// table with no basis, this uses the Phasix's already-locked Temper growth-priority ranking
/// (CLAUDE.md "Temper growth priority") as a stand-in alignment signal: a stat is "aligned" if
/// it's among that Temper's top 3 priority stats. This is a reasonable substitute (Temper already
/// represents "which stats this individual naturally grows toward"), NOT the Directive's literal
/// emotional-type concept — flagged in DECISIONS.md, revisit once emotional-type stat alignment
/// is actually designed.
/// </summary>
public static class ResonanceBonusEvaluator
{
    /// <summary>How many of a Temper's top-priority stats count as "aligned" for Resonance Bonus purposes. Placeholder.</summary>
    private const int AlignedStatCount = 3;

    /// <summary>Placeholder bonus multiplier for an aligned allocation. TODO: pending NumericalCalibration.md — the Directive gives no magnitude at all.</summary>
    public const float AlignedBonusMultiplier = 1.15f;

    public const float UnalignedBonusMultiplier = 1.0f;

    private static readonly Dictionary<Temper, StatType[]> PriorityOrder = new Dictionary<Temper, StatType[]>
    {
        { Temper.Edge, new[] { StatType.Force, StatType.Instinct, StatType.Resonance, StatType.Aura, StatType.Vitality, StatType.Guard, StatType.Ward, StatType.Resolve } },
        { Temper.Anchor, new[] { StatType.Vitality, StatType.Guard, StatType.Ward, StatType.Resolve, StatType.Force, StatType.Aura, StatType.Instinct, StatType.Resonance } },
        { Temper.Flux, new[] { StatType.Resonance, StatType.Aura, StatType.Ward, StatType.Instinct, StatType.Vitality, StatType.Force, StatType.Guard, StatType.Resolve } },
    };

    public static bool IsAligned(Temper temper, StatType stat)
        => PriorityOrder[temper].Take(AlignedStatCount).Contains(stat);

    /// <summary>Returns AlignedBonusMultiplier if stat is one of temper's top-priority stats, otherwise UnalignedBonusMultiplier.</summary>
    public static float ComputeBonusMultiplier(Temper temper, StatType stat)
        => IsAligned(temper, stat) ? AlignedBonusMultiplier : UnalignedBonusMultiplier;
}
