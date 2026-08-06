using System.Collections.Generic;

/// <summary>
/// Per-status metadata for all 28 StatusEffectType values (GDD §17, Locked v0.7.8). Data only —
/// no application/tick logic lives here (that's a future StatusEffectInstance/battle-loop
/// concern once skills actually apply statuses, which needs the skill content system this class
/// is scaffolding for).
///
/// BaseDurationRange values are placeholders within the GDD's own locked category ranges (§17.2:
/// DoTs 4-6, Debuffs 3-5, Controls 1-3 [Stun stays short], Signal 3-5, Chain results 2-4) —
/// per-status exact numbers are NOT locked anywhere and are "pending progression loop
/// calibration" per the GDD itself. Universal and Positive categories have no GDD-stated range;
/// 3-5 was chosen as a reasonable placeholder matching the Debuff range (both read as
/// sustained-but-not-permanent effects) — flagged in NumericalCalibration.md, not a hidden guess.
///
/// The three "ForMastery" tag sets (IsDoTForMastery, IsControlForMastery, IsDebuffForMastery)
/// are transcribed VERBATIM from GDD §17.9's own mastery-bonus trigger sets (Hemorrhage,
/// Dominance, Collapse) — they are NOT derived from category or flavor text. This matters:
/// Freeze reads as "DoT+Immobilise" in §17.4 but is excluded from Hemorrhage's 5-status DoT set;
/// Blind and Slow are categorized Elemental/Physical respectively but ARE in Dominance's control
/// set. Deriving these tags from category would have been wrong — the GDD's own bonus tables are
/// the only authoritative source for which statuses count as which "kind" mechanically.
/// </summary>
public static class StatusEffectCatalog
{
    public readonly struct Entry
    {
        public readonly StatusEffectCategory Category;
        public readonly bool IsPositive;
        public readonly int MinDurationTurns;
        public readonly int MaxDurationTurns;
        public readonly bool IsDoTForMastery;
        public readonly bool IsControlForMastery;
        public readonly bool IsDebuffForMastery;

        public Entry(StatusEffectCategory category, bool isPositive, int minDuration, int maxDuration,
            bool isDoTForMastery = false, bool isControlForMastery = false, bool isDebuffForMastery = false)
        {
            Category = category;
            IsPositive = isPositive;
            MinDurationTurns = minDuration;
            MaxDurationTurns = maxDuration;
            IsDoTForMastery = isDoTForMastery;
            IsControlForMastery = isControlForMastery;
            IsDebuffForMastery = isDebuffForMastery;
        }
    }

    private static readonly Dictionary<StatusEffectType, Entry> Entries = new Dictionary<StatusEffectType, Entry>
    {
        // Physical (§17.3) — DoT 4-6, Debuff 3-5, Control 1-3
        { StatusEffectType.Bleed,   new Entry(StatusEffectCategory.Physical, false, 4, 6, isDoTForMastery: true) },
        { StatusEffectType.Fracture, new Entry(StatusEffectCategory.Physical, false, 3, 5, isDebuffForMastery: true) },
        { StatusEffectType.Weaken,  new Entry(StatusEffectCategory.Physical, false, 3, 5, isDebuffForMastery: true) },
        { StatusEffectType.Stun,    new Entry(StatusEffectCategory.Physical, false, 1, 1, isControlForMastery: true) }, // "Stun stays short" — pinned to the range floor, not just clamped by it
        { StatusEffectType.Root,    new Entry(StatusEffectCategory.Physical, false, 1, 3, isControlForMastery: true) },
        { StatusEffectType.Exposed, new Entry(StatusEffectCategory.Physical, false, 3, 5, isDebuffForMastery: true) },
        { StatusEffectType.Slow,    new Entry(StatusEffectCategory.Physical, false, 3, 5) },

        // Elemental (§17.4) — DoT 4-6, Control 1-3
        { StatusEffectType.Burn,    new Entry(StatusEffectCategory.Elemental, false, 4, 6, isDoTForMastery: true) },
        { StatusEffectType.Freeze,  new Entry(StatusEffectCategory.Elemental, false, 1, 3) }, // DoT+Immobilise flavor, but NOT in Hemorrhage's DoT set per §17.9 — see class doc comment
        { StatusEffectType.Shock,   new Entry(StatusEffectCategory.Elemental, false, 1, 3, isControlForMastery: true) },
        { StatusEffectType.Drown,   new Entry(StatusEffectCategory.Elemental, false, 4, 6, isDoTForMastery: true) },
        { StatusEffectType.Wither,  new Entry(StatusEffectCategory.Elemental, false, 4, 6, isDoTForMastery: true) },
        { StatusEffectType.Blind,   new Entry(StatusEffectCategory.Elemental, false, 3, 5, isControlForMastery: true) },
        { StatusEffectType.Corrode, new Entry(StatusEffectCategory.Elemental, false, 4, 6, isDoTForMastery: true) },

        // Signal / Aura (§17.5) — Signal 3-5
        { StatusEffectType.Drain,   new Entry(StatusEffectCategory.Signal, false, 3, 5) },
        { StatusEffectType.Disrupt, new Entry(StatusEffectCategory.Signal, false, 3, 5) },
        { StatusEffectType.Suppress, new Entry(StatusEffectCategory.Signal, false, 3, 5, isControlForMastery: true) },
        { StatusEffectType.Overload, new Entry(StatusEffectCategory.Signal, false, 3, 5) },

        // Universal (§17.6) — no GDD-stated range, placeholder matches Debuff's 3-5
        { StatusEffectType.Mark,    new Entry(StatusEffectCategory.Universal, false, 3, 5, isDebuffForMastery: true) },
        { StatusEffectType.Curse,   new Entry(StatusEffectCategory.Universal, false, 3, 5) },
        { StatusEffectType.Fatigue, new Entry(StatusEffectCategory.Universal, false, 3, 5, isDebuffForMastery: true) },
        { StatusEffectType.Taunt,   new Entry(StatusEffectCategory.Universal, false, 3, 5, isControlForMastery: true) },

        // Positive / Buffs (§17.7) — no GDD-stated range, placeholder matches Debuff's 3-5
        { StatusEffectType.Regenerate, new Entry(StatusEffectCategory.Positive, true, 3, 5) },
        { StatusEffectType.Fortify,    new Entry(StatusEffectCategory.Positive, true, 3, 5) },
        { StatusEffectType.Haste,      new Entry(StatusEffectCategory.Positive, true, 3, 5) },
        { StatusEffectType.Empower,    new Entry(StatusEffectCategory.Positive, true, 3, 5) },
        { StatusEffectType.Barrier,    new Entry(StatusEffectCategory.Positive, true, 3, 5) },
        { StatusEffectType.Rally,      new Entry(StatusEffectCategory.Positive, true, 3, 5) },
    };

    public static Entry Get(StatusEffectType type) => Entries[type];
}
