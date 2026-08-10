using System.Collections.Generic;

/// <summary>
/// Per-type metadata for all 18 SkillTreeType values, GDD §4.1 "Skill Tree Taxonomy" (Taxonomy
/// Locked) + §15 "Skill Tree Attribute Scaling" (Locked v0.3.0), transcribed verbatim. Static
/// data class rather than one ScriptableObject asset per type — matches the precedent already
/// established by PersonalityStatModifier.cs for this exact shape of locked-taxonomy lookup
/// table (18-entry, no per-instance Inspector tuning needed).
///
/// BondGatePercent is 0 for the 16 tree types with no bond gate, 20 for Bond (Type F, unlocks at
/// the Familiar milestone), and 40 for Personality (Type O, unlocks at the Companion milestone) —
/// see SkillTreeUnlockSystem, which wires these two gates to EventBus.OnBondMilestoneReached.
/// </summary>
public static class SkillTreeCatalog
{
    public readonly struct Entry
    {
        public readonly string PrimaryAttribute;
        public readonly string Role;
        public readonly int BondGatePercent;

        public Entry(string primaryAttribute, string role, int bondGatePercent = 0)
        {
            PrimaryAttribute = primaryAttribute;
            Role = role;
            BondGatePercent = bondGatePercent;
        }
    }

    private static readonly Dictionary<SkillTreeType, Entry> Entries = new Dictionary<SkillTreeType, Entry>
    {
        { SkillTreeType.Utility, new Entry("Force/Resonance", "Direct output, versatile action skills") },
        { SkillTreeType.Aura, new Entry("Aura", "Energy management, cost reduction, recovery") },
        { SkillTreeType.Passive, new Entry("All", "Always-active bonuses, stat amplifiers") },
        { SkillTreeType.Synergy, new Entry("All", "Inter-skill connections, combo setups") },
        { SkillTreeType.Reaction, new Entry("Instinct", "Triggered responses, counter-attacks, parries") },
        { SkillTreeType.Bond, new Entry("Bond level", "Scales with bond %. Activates at 20% bond.", bondGatePercent: 20) }, // Type F
        { SkillTreeType.Aspect, new Entry("Instinct", "Mode-based skill sets. Primary tree for Stance Tempo creatures.") },
        { SkillTreeType.Resource, new Entry("Aura/Vitality", "Economy skills, sustain, action banking. Primary for Hold Tempo.") },
        { SkillTreeType.Corruption, new Entry("Resonance", "Status application, corruption effects. Overuse affects bond.") },
        { SkillTreeType.Mirror, new Entry("Instinct/Resonance", "Repetition and reverberation effects") },
        { SkillTreeType.Evolve, new Entry("Bond/Aptitude", "Evolution burst mechanics, mid-battle transformation") },
        { SkillTreeType.Territory, new Entry("Force/Resonance", "Spatial control, area effects. Primary for Split Tempo.") },
        { SkillTreeType.Memory, new Entry("Aptitude", "Past-state interactions, history-dependent effects") },
        { SkillTreeType.Fusion, new Entry("All", "Fusion creation mechanics — enhances fusion form, governs primary's retention on devolution") },
        { SkillTreeType.Personality, new Entry("Bond/Temper", "Activates at 40% bond. Personality-specific trait expressions.", bondGatePercent: 40) }, // Type O
        { SkillTreeType.Typing, new Entry("Resonance", "Type-specific power skills, Primal interaction exploits") },
        { SkillTreeType.Bastion, new Entry("Guard/Vitality", "Fortify, Counter, Absorb. Physical defense primary tree.") },
        { SkillTreeType.Phantom, new Entry("Instinct", "Evasion, Prediction, Ghost Step. Speed-based defense primary tree.") },

        // Standard (2026-08 follow-up) — NOT part of the GDD's 18-tree taxonomy above; groups the
        // 5 built-in moves (see SkillTreeType.Standard's own doc comment). This entry exists only
        // as a defensive fallback so a stray SkillTreeCatalog.Get(Standard) call never throws —
        // the real mechanism keeping built-ins out of PlaceholderSkillResolver's tree-derived
        // damage/status logic is the SkillData.BuiltInMove != None check, applied BEFORE any
        // SkillTreeCatalog/PlaceholderSkillResolver call site touches a built-in move's SkillData.
        { SkillTreeType.Standard, new Entry("N/A", "Built-in moves (Attack/Charge/Heal/Regen/Capture) — not a real skill tree, never resolved through this catalog's PrimaryAttribute derivation.") },
    };

    public static Entry Get(SkillTreeType type) => Entries[type];

    /// <summary>True for Bond (Type F) and Personality (Type O) — the only two bond-gated tree types.</summary>
    public static bool IsBondGated(SkillTreeType type) => Entries[type].BondGatePercent > 0;
}
