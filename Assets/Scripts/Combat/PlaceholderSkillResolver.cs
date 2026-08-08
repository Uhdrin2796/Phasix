using System;
using System.Collections.Generic;

/// <summary>
/// Derives generic, clickable mechanical behavior for the 36 placeholder SkillData assets
/// (2026-08 session, see DECISIONS.md -> [Combat]) WITHOUT inventing per-skill balance content —
/// SkillData itself stays a pure identity record (name/description/tree/placeholder index);
/// everything here is computed at lookup time from tables that are already GDD-locked elsewhere
/// (SkillTreeCatalog's per-tree PrimaryAttribute, StatusEffectCatalog's per-status Category), so
/// the ALGORITHM is the content, not its output.
///
/// Damage-vs-status split: a tree deals damage only if its PrimaryAttribute names one of the
/// damage formula's own locked stat pairs (CLAUDE.md: "Physical: Force/Guard, Elemental:
/// Resonance/Ward"), checked in that fixed priority order. Applying this to all 18 trees yields
/// exactly 6 damage trees (Utility, Territory, Bastion -> Physical; Corruption, Mirror, Typing ->
/// Elemental); the other 12 are status-only. Both placeholders of a damage tree are mechanically
/// identical — differentiating them would itself be invented content.
///
/// Status trees resolve a StatusEffectCategory via a second, similarly deterministic priority
/// chain, then pick a specific StatusEffectType by indexing into that category's members in fixed
/// enum-declaration order (same tie-break style ChainResultCatalog already uses for its own
/// ambiguous-match case) — SkillData.PlaceholderIndex (0 or 1) selects which. Every category has
/// >= 4 members, so index 0 vs 1 always differ.
/// </summary>
public static class PlaceholderSkillResolver
{
    public readonly struct SkillResolution
    {
        public readonly bool DealsDamage;
        public readonly DamageCategory Category;
        public readonly StatusEffectType? AppliedStatus;
        public readonly bool SelfTargeted;

        public SkillResolution(bool dealsDamage, DamageCategory category, StatusEffectType? appliedStatus, bool selfTargeted)
        {
            DealsDamage = dealsDamage;
            Category = category;
            AppliedStatus = appliedStatus;
            SelfTargeted = selfTargeted;
        }
    }

    /// <summary>True if this tree's PrimaryAttribute names a damage-formula stat (Force/Guard/Resonance/Ward).</summary>
    public static bool IsDamageSkill(SkillTreeType tree)
    {
        string attribute = SkillTreeCatalog.Get(tree).PrimaryAttribute;
        return attribute.Contains("Force") || attribute.Contains("Guard")
            || attribute.Contains("Resonance") || attribute.Contains("Ward");
    }

    /// <summary>Only valid when IsDamageSkill(tree) is true. Force/Guard -> Physical, Resonance/Ward -> Elemental, checked in that order.</summary>
    public static DamageCategory GetDamageCategory(SkillTreeType tree)
    {
        string attribute = SkillTreeCatalog.Get(tree).PrimaryAttribute;
        if (attribute.Contains("Force") || attribute.Contains("Guard")) return DamageCategory.Physical;
        if (attribute.Contains("Resonance") || attribute.Contains("Ward")) return DamageCategory.Elemental;
        throw new InvalidOperationException($"{tree} is not a damage tree — check IsDamageSkill first.");
    }

    /// <summary>Only meaningful for non-damage trees (IsDamageSkill(tree) == false). Priority chain: Force/Guard->Physical, Resonance/Ward->Elemental, "Aura"->Signal, "Bond"->Positive, else->Universal.</summary>
    public static StatusEffectCategory GetStatusCategory(SkillTreeType tree)
    {
        string attribute = SkillTreeCatalog.Get(tree).PrimaryAttribute;
        if (attribute.Contains("Force") || attribute.Contains("Guard")) return StatusEffectCategory.Physical;
        if (attribute.Contains("Resonance") || attribute.Contains("Ward")) return StatusEffectCategory.Elemental;
        if (attribute.Contains("Aura")) return StatusEffectCategory.Signal;
        if (attribute.Contains("Bond")) return StatusEffectCategory.Positive;
        return StatusEffectCategory.Universal;
    }

    private static readonly Dictionary<StatusEffectCategory, StatusEffectType[]> StatusesByCategory = BuildStatusesByCategory();

    private static Dictionary<StatusEffectCategory, StatusEffectType[]> BuildStatusesByCategory()
    {
        var buckets = new Dictionary<StatusEffectCategory, List<StatusEffectType>>();
        foreach (StatusEffectType type in Enum.GetValues(typeof(StatusEffectType)))
        {
            StatusEffectCategory category = StatusEffectCatalog.Get(type).Category;
            if (!buckets.TryGetValue(category, out List<StatusEffectType> list))
            {
                list = new List<StatusEffectType>();
                buckets[category] = list;
            }
            list.Add(type);
        }

        var result = new Dictionary<StatusEffectCategory, StatusEffectType[]>();
        foreach (KeyValuePair<StatusEffectCategory, List<StatusEffectType>> entry in buckets)
        {
            result[entry.Key] = entry.Value.ToArray();
        }
        return result;
    }

    /// <summary>Deterministic pick within GetStatusCategory(tree)'s members, in fixed enum-declaration order.</summary>
    public static StatusEffectType GetStatusForSkill(SkillTreeType tree, int placeholderIndex)
    {
        StatusEffectType[] options = StatusesByCategory[GetStatusCategory(tree)];
        int index = ((placeholderIndex % options.Length) + options.Length) % options.Length;
        return options[index];
    }

    /// <summary>Single entry point — resolves a placeholder SkillData into its damage or status behavior.</summary>
    public static SkillResolution Resolve(SkillData skill)
    {
        SkillTreeType tree = skill.TreeType;

        if (IsDamageSkill(tree))
        {
            return new SkillResolution(dealsDamage: true, category: GetDamageCategory(tree), appliedStatus: null, selfTargeted: false);
        }

        StatusEffectType status = GetStatusForSkill(tree, skill.PlaceholderIndex);
        bool selfTargeted = StatusEffectCatalog.Get(status).IsPositive;
        return new SkillResolution(dealsDamage: false, category: default, appliedStatus: status, selfTargeted: selfTargeted);
    }
}
