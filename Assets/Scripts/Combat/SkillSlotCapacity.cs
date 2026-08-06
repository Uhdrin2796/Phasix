using System;

/// <summary>
/// Skill tree count and active-slot capacity per evolution tier, Evolution_System_Directive_v1_1_0.md
/// §1 (supersedes GDD §3's tier structure per DOCUMENT_INDEX.md, though both give the same
/// numbers). "Skill trees available" (how many of the 18 SkillTreeType categories are unlocked)
/// is a separate number from "active slots" (how many learned skills can be equipped at once) —
/// PhasixRuntimeData already tracks both via unlockedTreeTypes/equippedSkillGuids, this class is
/// just the tier-&gt;capacity lookup table those lists are capped against.
///
/// T6/T7 (fusion tiers) are NOT covered here — the Directive states their capacity "inherits
/// from ingredients / all four lineage parents, max of both/all parents," which needs the fusion
/// system itself (Phase 4, not built) to resolve. GetTreeCount/GetActiveSlotRange throw for
/// tier &gt;= 6 rather than inventing a number.
/// </summary>
public static class SkillSlotCapacity
{
    public static int GetTreeCount(int evolutionTier)
    {
        switch (evolutionTier)
        {
            case 1: return 2;
            case 2: return 4;
            case 3: return 5;
            case 4: return 6;
            case 5: return 7;
            default: throw new NotSupportedException($"Tier {evolutionTier} tree count is fusion-dependent (Evolution_System_Directive §1) — not resolvable without the fusion system (Phase 4).");
        }
    }

    /// <summary>Returns (min, max) active equipped-skill slots for the tier. T1-T4 have a single fixed value (min == max); T5 is a 5-7 range ("varies by species").</summary>
    public static (int min, int max) GetActiveSlotRange(int evolutionTier)
    {
        switch (evolutionTier)
        {
            case 1: return (2, 2);
            case 2: return (3, 3);
            case 3: return (4, 4);
            case 4: return (5, 5);
            case 5: return (5, 7);
            default: throw new NotSupportedException($"Tier {evolutionTier} slot range is fusion-dependent (Evolution_System_Directive §1) — not resolvable without the fusion system (Phase 4).");
        }
    }
}
