using System.Collections.Generic;

/// <summary>
/// Nudge strength for one (Personality, StatType) pair — GDD §7.3's "++ / + / -" notation.
/// </summary>
public enum PersonalityNudgeTier
{
    Reduction,
    Boost,
    StrongBoost
}

/// <summary>
/// Which stats each of the 18 Personality traits nudges, transcribed verbatim from the
/// locked GDD §7.3 table ("The 16 Personality Traits" — table has 18 rows; see DECISIONS.md
/// → [Creatures] for the prose/table count discrepancy this project already resolved).
///
/// This is data only — it does not compute an actual growth-rate number. Personality is
/// ~25% of stat growth direction (GDD §7.1), but the numeric formula lives in the Aura
/// allocation system (Progression_Directive_v0_1_0.md), which isn't built yet. Unwired
/// scaffolding until that system exists to consume it — same forward-reference spirit as
/// PhasixRuntimeData's currentNodeGuid/discoveredNodeGuids.
/// </summary>
public static class PersonalityStatModifier
{
    private static readonly Dictionary<Personality, Dictionary<StatType, PersonalityNudgeTier>> Nudges =
        new Dictionary<Personality, Dictionary<StatType, PersonalityNudgeTier>>
    {
        // Group 1 — Offensive
        { Personality.Reckless, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Force, PersonalityNudgeTier.StrongBoost },
            { StatType.Instinct, PersonalityNudgeTier.Boost },
            { StatType.Guard, PersonalityNudgeTier.Reduction } } },
        { Personality.Fierce, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Force, PersonalityNudgeTier.StrongBoost },
            { StatType.Vitality, PersonalityNudgeTier.Boost } } },

        // Group 2 — Elemental
        { Personality.Quirky, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Resonance, PersonalityNudgeTier.StrongBoost },
            { StatType.Aura, PersonalityNudgeTier.Boost } } },
        { Personality.Calm, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Resonance, PersonalityNudgeTier.StrongBoost },
            { StatType.Ward, PersonalityNudgeTier.Boost } } },

        // Group 3 — Defensive
        { Personality.Cautious, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Guard, PersonalityNudgeTier.StrongBoost },
            { StatType.Resolve, PersonalityNudgeTier.Boost },
            { StatType.Force, PersonalityNudgeTier.Reduction } } },
        { Personality.Hardy, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Vitality, PersonalityNudgeTier.StrongBoost },
            { StatType.Guard, PersonalityNudgeTier.Boost } } },

        // Group 4 — Technical
        { Personality.Hasty, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Instinct, PersonalityNudgeTier.StrongBoost },
            { StatType.Aura, PersonalityNudgeTier.Boost } } },
        { Personality.Careful, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Aura, PersonalityNudgeTier.StrongBoost },
            { StatType.Instinct, PersonalityNudgeTier.Boost } } },
        { Personality.Shrewd, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Instinct, PersonalityNudgeTier.StrongBoost },
            { StatType.Resonance, PersonalityNudgeTier.Boost } } },
        { Personality.Thorough, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Aura, PersonalityNudgeTier.StrongBoost },
            { StatType.Resolve, PersonalityNudgeTier.Boost } } },

        // Group 5 — Resilient
        { Personality.Stubborn, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Resolve, PersonalityNudgeTier.StrongBoost },
            { StatType.Vitality, PersonalityNudgeTier.Boost } } },
        { Personality.Gentle, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Ward, PersonalityNudgeTier.StrongBoost },
            { StatType.Resolve, PersonalityNudgeTier.Boost } } },
        { Personality.Patient, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Resolve, PersonalityNudgeTier.StrongBoost },
            { StatType.Ward, PersonalityNudgeTier.Boost } } },
        { Personality.Lively, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Ward, PersonalityNudgeTier.StrongBoost },
            { StatType.Instinct, PersonalityNudgeTier.Boost } } },

        // Group 6 — Versatile (all boosts equal weight per GDD §7.3 — modeled as Boost, not StrongBoost)
        { Personality.Brave, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Force, PersonalityNudgeTier.Boost },
            { StatType.Vitality, PersonalityNudgeTier.Boost },
            { StatType.Resolve, PersonalityNudgeTier.Boost } } },
        { Personality.Jolly, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Instinct, PersonalityNudgeTier.Boost },
            { StatType.Aura, PersonalityNudgeTier.Boost },
            { StatType.Vitality, PersonalityNudgeTier.Boost } } },
        { Personality.Timid, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Instinct, PersonalityNudgeTier.StrongBoost },
            { StatType.Ward, PersonalityNudgeTier.Boost },
            { StatType.Force, PersonalityNudgeTier.Reduction } } },
        { Personality.Naive, new Dictionary<StatType, PersonalityNudgeTier> {
            { StatType.Resonance, PersonalityNudgeTier.StrongBoost },
            { StatType.Vitality, PersonalityNudgeTier.Boost },
            { StatType.Guard, PersonalityNudgeTier.Reduction } } },
    };

    /// <summary>Returns the nudge tier for a stat under a personality, or null if that stat isn't affected.</summary>
    public static PersonalityNudgeTier? GetNudge(Personality personality, StatType stat)
    {
        if (Nudges.TryGetValue(personality, out Dictionary<StatType, PersonalityNudgeTier> statMap)
            && statMap.TryGetValue(stat, out PersonalityNudgeTier tier))
            return tier;

        return null;
    }
}
