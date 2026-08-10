/// <summary>
/// Variant growth-priority role within a species. GDD §6 — Locked v0.3.0.
/// Changeable at runtime via Re-Tempering (Temper Forge + Temper Cores, GDD §6.4) —
/// lives on PhasixRuntimeData, not PhasixData. Persists through evolution/devolution.
/// Internal roles are never shown to the player directly — GDD §6.3 gives each
/// species its own two-word compound display name per Temper (naming table pending).
/// </summary>
public enum Temper { Edge, Anchor, Flux }

/// <summary>
/// How this specimen came to exist. GDD §12 — Locked. Lives on PhasixData (species-form
/// template) — no evidence found of per-individual variance or runtime mutation.
/// Authority: Evolution_System_Directive_v1_1_0.md (used as speciesData.origin).
/// </summary>
public enum OriginType { Wild, Synthetic, Corrupted, Ascended, Hollow, Primordial }

/// <summary>Combat action-economy archetype. GDD §11 — Locked v0.5.1.</summary>
public enum TempoType { Strike, Flow, Hold, Split, Stance }

/// <summary>
/// 9 Signal types. GDD §10 — Locked v0.5.0. Names locked; interaction multiplier
/// NUMBERS pending NumericalCalibration.md (logic/taxonomy is NOT pending).
/// </summary>
public enum SignalType { Pulse, Static, Frequency, Silence, Overflow, Echo, Surge, Catalyst, Current }

/// <summary>
/// Personality traits — stat-growth nudge only, no skill effects. GDD §7 — Locked v0.7.6.
/// Rolled per individual ("shown on capture") and changeable via consumable item (any
/// personality to any other) — lives on PhasixRuntimeData, not PhasixData.
/// TODO: doc discrepancy — GDD prose/changelog says "16 traits" in two places, but the
/// actual §7.3 table has 18 rows (Reckless, Cautious, Timid, Naive are the 4 tradeoff
/// traits; the other 14 are pure-boost). Using the verified 18-row table here.
/// </summary>
public enum Personality
{
    Reckless, Fierce, Quirky, Calm, Cautious, Hardy, Hasty, Careful,
    Shrewd, Thorough, Stubborn, Gentle, Patient, Lively, Brave, Jolly, Timid, Naive
}

/// <summary>
/// 18-type skill tree taxonomy, A-R. GDD §14 — Taxonomy Locked, individual skill
/// content pending species roster. Note: SkillTreeType.Personality and the standalone
/// Personality enum share a name — different enum types, not a collision.
///
/// Standard (2026-08 follow-up) is a 19th, NON-GDD value — every creature always has it, unlike
/// the 18 taxonomy trees which unlock per-species/tier. Groups the 5 built-in moves (Attack/
/// Charge/Heal/Regen/Capture, see BuiltInMoveType) now that they're real, equippable SkillData
/// instead of hardcoded battle moves (user: "if theres not particular skill tree for them they
/// can all be grouped in their own as standard"). Same "new, not GDD content" precedent as
/// ComboRuleType/ChainResultType — see DECISIONS.md -> [Combat].
/// </summary>
public enum SkillTreeType
{
    Utility, Aura, Passive, Synergy, Reaction, Bond, Aspect, Resource,
    Corruption, Mirror, Evolve, Territory, Memory, Fusion, Personality, Typing, Bastion, Phantom,
    Standard
}
