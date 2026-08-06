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
}
