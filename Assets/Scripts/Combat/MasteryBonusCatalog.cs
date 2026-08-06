using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Status mastery bonus triggers and effects, GDD §17.9 (DECISION LOCKED): "3+ specific statuses
/// simultaneously active -> bonus for rest of battle; multiple can stack; each combo triggers its
/// bonus once per battle; persists after triggering statuses expire."
///
/// EvaluateAll is a pure predicate over two status sets — it does NOT track "already triggered
/// this battle" itself (no active battle status-tracking system exists yet to hook into). Once
/// statuses are actually applied in live battles, the caller is responsible for calling this each
/// time a status changes and remembering which MasteryBonusType values have already fired this
/// battle (so a bonus that already triggered, then had its statuses cleansed, doesn't need to
/// keep matching — though per the GDD it doesn't matter if it does, since the effect "persists
/// after triggering statuses expire" and is a once-per-battle flag either way).
///
/// Self/target side for each trigger is a reasonable reading of the GDD's prose, not always
/// spelled out explicitly there — see DECISIONS.md -> [Combat] for the specific calls made
/// (Contrast: self positive + target negative; Enlightened: self positive buffs; all others:
/// target's own active statuses, since GDD §17.9 frames these as things applied "to a target").
/// </summary>
public static class MasteryBonusCatalog
{
    private static readonly Dictionary<MasteryBonusType, string> Triggers = new Dictionary<MasteryBonusType, string>
    {
        { MasteryBonusType.Hemorrhage, "Any 3 simultaneous DoTs (Bleed, Burn, Wither, Drown, Corrode) on the target." },
        { MasteryBonusType.Dominance, "Any 3 simultaneous controls (Stun, Root, Blind, Slow, Taunt, Suppress) on the target." },
        { MasteryBonusType.Collapse, "Any 3 simultaneous debuffs (Fracture, Weaken, Exposed, Fatigue, Mark) on the target." },
        { MasteryBonusType.Overmaster, "Any 3 simultaneous Signal statuses (Drain, Disrupt, Suppress, Overload) on the target." },
        { MasteryBonusType.Pressure, "Any 2 DoTs + 1 control on the target." },
        { MasteryBonusType.Contrast, "1 positive buff on self + 2 negative statuses on the target." },
        { MasteryBonusType.Convergence, "1 Physical + 1 Elemental + 1 Signal status on the same target." },
        { MasteryBonusType.Enlightened, "Any 3 positive buffs (Regenerate, Fortify, Haste, Empower, Barrier, Rally) on self." },
    };

    private static readonly Dictionary<MasteryBonusType, string> Effects = new Dictionary<MasteryBonusType, string>
    {
        { MasteryBonusType.Hemorrhage, "All DoT ticks +25%." },
        { MasteryBonusType.Dominance, "All control durations +2 turns." },
        { MasteryBonusType.Collapse, "Vulnerability window bonuses doubled." },
        { MasteryBonusType.Overmaster, "Catalyst Signal generates double Aura." },
        { MasteryBonusType.Pressure, "Target cannot cleanse for 2 turns." },
        { MasteryBonusType.Contrast, "Positive durations doubled AND negative damage +15%." },
        { MasteryBonusType.Convergence, "Next chain result this battle is permanent (cleanse-only)." },
        { MasteryBonusType.Enlightened, "All stats raised for 3 turns." },
    };

    public static string GetTriggerDescription(MasteryBonusType bonus) => Triggers[bonus];
    public static string GetEffectDescription(MasteryBonusType bonus) => Effects[bonus];

    /// <summary>Returns every mastery bonus currently satisfied by the given self/target status sets.</summary>
    public static List<MasteryBonusType> EvaluateAll(ICollection<StatusEffectType> selfStatuses, ICollection<StatusEffectType> targetStatuses)
    {
        var triggered = new List<MasteryBonusType>();

        int targetDoTCount = CountMatching(targetStatuses, e => e.IsDoTForMastery);
        int targetControlCount = CountMatching(targetStatuses, e => e.IsControlForMastery);
        int targetDebuffCount = CountMatching(targetStatuses, e => e.IsDebuffForMastery);
        int targetSignalCount = CountMatching(targetStatuses, e => e.Category == StatusEffectCategory.Signal);
        int selfPositiveCount = CountMatching(selfStatuses, e => e.IsPositive);
        int targetNegativeCount = CountMatching(targetStatuses, e => !e.IsPositive);

        if (targetDoTCount >= 3) triggered.Add(MasteryBonusType.Hemorrhage);
        if (targetControlCount >= 3) triggered.Add(MasteryBonusType.Dominance);
        if (targetDebuffCount >= 3) triggered.Add(MasteryBonusType.Collapse);
        if (targetSignalCount >= 3) triggered.Add(MasteryBonusType.Overmaster);
        if (targetDoTCount >= 2 && targetControlCount >= 1) triggered.Add(MasteryBonusType.Pressure);
        if (selfPositiveCount >= 1 && targetNegativeCount >= 2) triggered.Add(MasteryBonusType.Contrast);
        if (HasCategory(targetStatuses, StatusEffectCategory.Physical) &&
            HasCategory(targetStatuses, StatusEffectCategory.Elemental) &&
            HasCategory(targetStatuses, StatusEffectCategory.Signal))
        {
            triggered.Add(MasteryBonusType.Convergence);
        }
        if (selfPositiveCount >= 3) triggered.Add(MasteryBonusType.Enlightened);

        return triggered;
    }

    private static int CountMatching(ICollection<StatusEffectType> statuses, System.Func<StatusEffectCatalog.Entry, bool> predicate)
        => statuses.Count(s => predicate(StatusEffectCatalog.Get(s)));

    private static bool HasCategory(ICollection<StatusEffectType> statuses, StatusEffectCategory category)
        => statuses.Any(s => StatusEffectCatalog.Get(s).Category == category);
}
