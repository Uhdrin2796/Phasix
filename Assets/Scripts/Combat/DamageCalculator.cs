using UnityEngine;

/// <summary>
/// The real damage formula (CLAUDE.md / Roadmap_v2 Mo 6 Wk 1-2), replacing
/// BattleConfig.PlaceholderAttackDamage's flat placeholder:
/// (AttackerStat / DefenderStat) x skillPower x primalTypeMultiplier.
/// Physical = Force/Guard, Elemental = Resonance/Ward. Timed-input bonus is applied by the caller
/// (BattleManager, via BattleAction.DamageMultiplier) AFTER this returns — CLAUDE.md: "Apply timed
/// bonus after formula."
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// Placeholder skill power for the built-in "Attack" move — real skill content (with its own
    /// power values) is Step 4 (Roadmap_v2 Mo 6 Wk 3+). TODO: pending NumericalCalibration.md.
    /// </summary>
    public const int BasicAttackPower = 10;

    /// <summary>
    /// Computes base damage (before any timed-input multiplier). typeChart may be null (e.g. not
    /// wired up yet in a given scene) — falls back to a neutral 1.0x type multiplier rather than
    /// throwing, since a missing chart reference shouldn't hard-crash a battle.
    /// </summary>
    public static int ComputeDamage(BattleParticipant attacker, BattleParticipant target, PrimalTypeChart typeChart, DamageCategory category, int skillPower)
    {
        float statRatio = ComputeStatRatio(attacker, target, category);
        float typeMultiplier = ComputeTypeMultiplier(attacker, target, typeChart);

        float rawDamage = statRatio * skillPower * typeMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(rawDamage));
    }

    /// <summary>
    /// The stat-ratio-only damage BEFORE the Primal type multiplier — i.e. ComputeDamage's own
    /// formula with typeMultiplier fixed at 1.0. Public so callers (the battle log's base/type/
    /// timing damage breakdown — 2026-08-11, user-directed: "show the (base damage + type
    /// advantage damage + timing bonus damage)... for visibility") can report the pure "base"
    /// component on its own, with the type advantage then reported separately as the delta between
    /// this and ComputeDamage's result. Display-only — never used to actually apply damage, so it
    /// rounds independently rather than sharing ComputeDamage's single rounding pass; the delta is
    /// still always exact since it's computed as a difference of two already-rounded values.
    /// </summary>
    public static int ComputeBaseDamage(BattleParticipant attacker, BattleParticipant target, DamageCategory category, int skillPower)
    {
        float statRatio = ComputeStatRatio(attacker, target, category);
        return Mathf.Max(1, Mathf.RoundToInt(statRatio * skillPower));
    }

    private static float ComputeStatRatio(BattleParticipant attacker, BattleParticipant target, DamageCategory category)
    {
        int attackerStat = category == DamageCategory.Physical
            ? attacker.RuntimeData.EffectiveStat(StatType.Force)
            : attacker.RuntimeData.EffectiveStat(StatType.Resonance);
        int defenderStat = category == DamageCategory.Physical
            ? target.RuntimeData.EffectiveStat(StatType.Guard)
            : target.RuntimeData.EffectiveStat(StatType.Ward);

        // Guards a divide-by-zero on an unset/placeholder defense stat — not a design rule, just
        // arithmetic safety until real species stats exist.
        defenderStat = Mathf.Max(1, defenderStat);

        return (float)attackerStat / defenderStat;
    }

    /// <summary>
    /// The Primal type multiplier alone, clamped to the no-immunities floor. Public so callers can
    /// report effectiveness (e.g. the battle log's "It's super effective!") without recomputing the
    /// whole damage formula — ComputeDamage calls this internally too, so the two never disagree.
    /// </summary>
    public static float ComputeTypeMultiplier(BattleParticipant attacker, BattleParticipant target, PrimalTypeChart typeChart)
    {
        if (typeChart == null) return 1f;

        PhasixData attackerSpecies = attacker.RuntimeData.speciesData;
        PhasixData targetSpecies = target.RuntimeData.speciesData;
        if (attackerSpecies == null || targetSpecies == null) return 1f;

        float multiplier = typeChart.GetMultiplier(attackerSpecies.PrimalType, targetSpecies.PrimalType);
        return Mathf.Max(PrimalTypeChart.MinimumMultiplier, multiplier);
    }
}
