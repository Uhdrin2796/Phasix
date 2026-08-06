/// <summary>
/// 28 status effects, GDD §17 — Locked v0.7.8. ClaudeCode_Primer_v1_1_0.md's "24" figure
/// (line 314) does not match the GDD's own tables (7+7+4+4+6 = 28); the GDD wins per
/// DOCUMENT_INDEX.md's precedence rule (specific/active GDD sections beat the Primer summary).
/// Grouped by StatusEffectCategory — see StatusEffectCatalog for per-status metadata
/// (category, positive/negative, mastery-bonus tag sets, placeholder duration range).
/// </summary>
public enum StatusEffectType
{
    // Physical (§17.3)
    Bleed, Fracture, Weaken, Stun, Root, Exposed, Slow,

    // Elemental (§17.4)
    Burn, Freeze, Shock, Drown, Wither, Blind, Corrode,

    // Signal / Aura (§17.5)
    Drain, Disrupt, Suppress, Overload,

    // Universal (§17.6)
    Mark, Curse, Fatigue, Taunt,

    // Positive / Buffs (§17.7)
    Regenerate, Fortify, Haste, Empower, Barrier, Rally
}
