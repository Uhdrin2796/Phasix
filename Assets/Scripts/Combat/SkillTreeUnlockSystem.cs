using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Wires the two bond-gated skill trees (CLAUDE.md: "Type F trees unlock at 20%. Type O trees
/// unlock at 40%.") to EventBus.OnBondMilestoneReached. Bond (Type F) unlocks at the Familiar
/// milestone (20%), Personality (Type O) at Companion (40%) — GDD §14.2 confirms both thresholds
/// from the bond-zone side too. unlockedTreeTypes never shrinks (PhasixRuntimeData's own doc
/// comment), so this only ever adds, never removes, and is a no-op if the type is already present
/// (e.g. a Phasix captured with the tree pre-unlocked some other way, once such a path exists).
///
/// Subscribes itself once via RuntimeInitializeOnLoadMethod rather than needing a scene object —
/// no other EventBus subscriber in this codebase has a natural MonoBehaviour home either (Aura
/// drops, evolution, etc. are all still unbuilt Phase 4 stubs), so this establishes the pattern
/// for "static rule that just needs to always be listening."
/// </summary>
public static class SkillTreeUnlockSystem
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Subscribe()
    {
        EventBus.OnBondMilestoneReached += HandleBondMilestoneReached;
    }

    /// <summary>Public so EditMode tests can call it directly without relying on RuntimeInitializeOnLoadMethod having fired.</summary>
    public static void HandleBondMilestoneReached(PhasixRuntimeData phasix, BondZone zone)
    {
        if (zone == BondZone.Familiar) Unlock(phasix, SkillTreeType.Bond);
        else if (zone == BondZone.Companion) Unlock(phasix, SkillTreeType.Personality);
    }

    private static void Unlock(PhasixRuntimeData phasix, SkillTreeType treeType)
    {
        if (!phasix.unlockedTreeTypes.Contains(treeType)) phasix.unlockedTreeTypes.Add(treeType);
    }

    /// <summary>All 18 GDD taxonomy trees (every SkillTreeType value except Standard and Testing, neither of which is part of the taxonomy — see those values' own doc comments), in enum-declaration order.</summary>
    private static readonly SkillTreeType[] AllGddTrees = System.Enum.GetValues(typeof(SkillTreeType))
        .Cast<SkillTreeType>()
        .Where(t => t != SkillTreeType.Standard && t != SkillTreeType.Testing)
        .ToArray();

    /// <summary>
    /// Single source of truth for "which of the 18 GDD skill trees count as unlocked right now" —
    /// used by BOTH the Party menu's skill web view (display) and SkillLoadoutSystem's equip gate
    /// (2026-08, added alongside the debug tier control), so a tier override can never desync the
    /// two (display shows a tree as unlocked but equipping from it still silently fails).
    ///
    /// Checked in priority order:
    /// 1. DebugUnlockAllTrees (2026-08-09 follow-up — user: "can we also have an unlock all
    ///    debug so im able to see everything?") — every one of the 18 GDD trees, unconditionally,
    ///    ignoring both the real unlockedTreeTypes and any DebugTierOverride/species tree list.
    ///    Independent of tier on purpose: this is purely "let me see every tree," while
    ///    DebugTierOverride separately still governs equip SLOT capacity.
    /// 2. DebugTierOverride — simulates what WOULD be unlocked at that tier by reusing the exact
    ///    same selection WildSpawnSystem.SeedInitialSkills already uses for real seeding —
    ///    speciesData.AvailableTreeTypes.Take(SkillSlotCapacity.GetTreeCount(tier)) — rather than
    ///    inventing a second, independent ordering.
    /// 3. Otherwise, the real, save-persisted unlockedTreeTypes.
    ///
    /// Neither debug branch reads or writes the real unlockedTreeTypes list, so both are always
    /// safe to toggle repeatedly while debugging — nothing here can leak into persisted progression.
    ///
    /// SkillTreeType.Standard/Testing are intentionally never included/checked here — neither is one
    /// of the 18 GDD taxonomy trees (see their own doc comments) and both are always available
    /// regardless of tier or unlock state; callers must not gate Standard/Testing against this list.
    /// </summary>
    public static IReadOnlyList<SkillTreeType> GetEffectiveUnlockedTrees(PhasixRuntimeData runtime)
    {
        if (runtime == null) return System.Array.Empty<SkillTreeType>();

        if (runtime.DebugUnlockAllTrees) return AllGddTrees;

        if (runtime.DebugTierOverride.HasValue)
        {
            if (runtime.speciesData == null || runtime.speciesData.AvailableTreeTypes == null)
                return System.Array.Empty<SkillTreeType>();

            int treeCount = SkillSlotCapacity.GetTreeCount(runtime.DebugTierOverride.Value);
            return runtime.speciesData.AvailableTreeTypes.Take(treeCount).ToList();
        }

        return runtime.unlockedTreeTypes;
    }
}
