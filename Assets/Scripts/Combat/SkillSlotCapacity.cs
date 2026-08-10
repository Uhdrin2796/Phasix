using System;

/// <summary>
/// Skill tree count and active-slot capacity per evolution tier. GetTreeCount still reflects
/// Evolution_System_Directive_v1_1_0.md §1 (supersedes GDD §3's tier structure per
/// DOCUMENT_INDEX.md). GetActiveSlotRange's numbers were overridden 2026-08 (see that method's own
/// doc comment + DECISIONS.md -> [Progression]) to a flat 4/6/8/10/12 progression reaching all 12
/// wheel positions at T5 — no longer matches the Directive's original 2/3/4/5/5-7 table.
/// "Skill trees available" (how many of the 18 SkillTreeType categories are unlocked) is a
/// separate number from "active slots" (how many learned skills can be equipped at once) —
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

    /// <summary>
    /// Returns (min, max) active equipped-skill slots for the tier. 2026-08 follow-up — user:
    /// "at max tier they should be able to access all 12 slots... tier 1 they have access to 4
    /// slots, then increasing by 2 every tier" (settled on a start of 4, not the initially-floated
    /// 3, specifically so T5 lands exactly on 12 — the full 12-position wheel BattleHUDController/
    /// OverworldMenuController both render). This SUPERSEDES the flat 2/3/4/5/5-7 table originally
    /// sourced from Evolution_System_Directive_v1_1_0.pdf §1 (see DECISIONS.md -> [Progression]),
    /// same precedent as Progression_Directive_v0_1_0.md superseding GDD §21.
    ///
    /// Every tier is currently a fixed value (min == max) — "let's do flat tier for now, but
    /// please build in the option to have it vary once we get more granular on phasix specific
    /// design." The (min,max) tuple return shape already supports a future per-species range (T5
    /// used to be 5-7 "varies by species" under the old table) — reintroducing variance later is a
    /// matter of returning a real range for the affected tier(s), no signature/call-site change
    /// needed.
    /// </summary>
    public static (int min, int max) GetActiveSlotRange(int evolutionTier)
    {
        switch (evolutionTier)
        {
            case 1: return (4, 4);
            case 2: return (6, 6);
            case 3: return (8, 8);
            case 4: return (10, 10);
            case 5: return (12, 12);
            default: throw new NotSupportedException($"Tier {evolutionTier} slot range is fusion-dependent (Evolution_System_Directive §1) — not resolvable without the fusion system (Phase 4).");
        }
    }
}
