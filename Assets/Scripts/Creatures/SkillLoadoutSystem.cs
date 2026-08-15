using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Skill loadout mutation rules over PhasixRuntimeData.learnedSkillGuids/equippedSkillGuids
/// (2026-08 session, see DECISIONS.md -> [UI]), following this project's established
/// "static rules class over PhasixRuntimeData" convention (AuraStatAllocationSystem, BondSystem).
/// Built for the overworld Party menu's skill ring: drag an equipped orb onto another to swap
/// (SwapEquipped), drag a learned-but-unequipped tray skill onto an equipped orb to re-equip
/// (TryEquip), right-click an equipped orb to send it back to the tray (Unequip, not "unlearn" —
/// learnedSkillGuids is never touched here, it NEVER shrinks per PhasixRuntimeData's own contract).
///
/// 2026-08 follow-up (skill web view): TryEquip/TryEquipAt now take the skill's SkillTreeType and
/// reject equipping from a tree that isn't unlocked (SkillTreeUnlockSystem.GetEffectiveUnlockedTrees)
/// — previously this class never checked unlockedTreeTypes at all, so any learned skill could be
/// equipped regardless of tree-lock state; the carousel UI just never happened to expose a way to
/// try. SkillTreeType.Standard/Testing are exempt (always available, neither is one of the 18 GDD
/// taxonomy trees).
///
/// 2026-08 follow-up #2 (user report — "when i add skills from the tree to the wheel it just adds
/// it to the next open spot instead of where im dragging and dropping it to"): equippedSkillGuids
/// is no longer treated as a strictly compact, front-packed list. An empty string ("") entry now
/// means "no skill in this physical slot" — positions are stable: equipping into an empty slot
/// lands EXACTLY there, and unequipping clears exactly that slot without shifting anything else.
/// Every read site elsewhere in the codebase (OverworldMenuController, BattleHUDController,
/// BattleManager, BattleParticipant) already resolves guids via SkillDatabase.TryGetByGuid, which
/// already treats "" as "not found" (existing, tested behavior — SkillDatabaseTests
/// .EmptyOrNullGuidEntries_AreSkipped_DoNotThrow) — so no reader needed to change, only the
/// mutation methods here. CountEquipped (below) — not equippedSkillGuids.Count — is the real
/// "how many skills are actually equipped" figure once gaps can exist.
/// </summary>
public static class SkillLoadoutSystem
{
    /// <summary>Real count of occupied slots — NOT equippedSkillGuids.Count, which is just how far the sparse list currently extends and may include empty ("") gap entries.</summary>
    private static int CountEquipped(PhasixRuntimeData runtime) =>
        runtime.equippedSkillGuids.Count(g => !string.IsNullOrEmpty(g));

    /// <summary>Pads list with empty-string gap entries so index minLength - 1 is addressable.</summary>
    private static void EnsureCapacity(List<string> list, int minLength)
    {
        while (list.Count < minLength) list.Add(string.Empty);
    }

    /// <summary>Equips skillGuid into the first empty slot (or appends) if it's learned, not already equipped, its tree is unlocked (or Standard), and there's a free slot for evolutionTier. No-op (returns false) otherwise.</summary>
    public static bool TryEquip(PhasixRuntimeData runtime, string skillGuid, SkillTreeType treeType, int evolutionTier)
    {
        if (runtime == null || string.IsNullOrEmpty(skillGuid)) return false;
        if (!runtime.learnedSkillGuids.Contains(skillGuid)) return false;
        if (runtime.equippedSkillGuids.Contains(skillGuid)) return false;
        if (treeType != SkillTreeType.Standard && treeType != SkillTreeType.Testing
            && !SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(runtime).Contains(treeType)) return false;

        int maxSlots = SkillSlotCapacity.GetActiveSlotRange(evolutionTier).max;
        if (CountEquipped(runtime) >= maxSlots) return false;

        int firstEmpty = runtime.equippedSkillGuids.FindIndex(string.IsNullOrEmpty);
        if (firstEmpty >= 0) runtime.equippedSkillGuids[firstEmpty] = skillGuid;
        else runtime.equippedSkillGuids.Add(skillGuid);
        return true;
    }

    /// <summary>Clears skillGuid's slot back to empty — it stays in learnedSkillGuids and can be re-equipped later. The right-click action. Clears IN PLACE; does not shift any other equipped skill's position.</summary>
    public static void Unequip(PhasixRuntimeData runtime, string skillGuid)
    {
        if (runtime == null) return;
        int index = runtime.equippedSkillGuids.IndexOf(skillGuid);
        if (index < 0) return;
        runtime.equippedSkillGuids[index] = string.Empty;
    }

    /// <summary>Swaps whatever occupies indexA/indexB (either or both may currently be empty) — the equipped-orb-to-equipped-orb drag action. Auto-extends the list with empty gap entries if either index is beyond its current length, so dragging onto a not-yet-reached physical position still lands exactly there. No-op only for a negative index or indexA == indexB.</summary>
    public static void SwapEquipped(PhasixRuntimeData runtime, int indexA, int indexB)
    {
        if (runtime == null) return;
        if (indexA < 0 || indexB < 0) return;
        if (indexA == indexB) return;

        var list = runtime.equippedSkillGuids;
        EnsureCapacity(list, System.Math.Max(indexA, indexB) + 1);
        (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
    }

    /// <summary>
    /// Equips skillGuid directly into ring position slotIndex — the overworld Party menu's
    /// tray-to-ring drag action (dragging a learned-but-unequipped skill onto a specific orb
    /// position, occupied or empty). Lands EXACTLY at slotIndex either way. If slotIndex already
    /// holds a different skill, that skill is simply overwritten out of equippedSkillGuids (it
    /// stays in learnedSkillGuids, so it reappears in the tray — same "unequip, not unlearn"
    /// contract as Unequip). Fails (no-op, returns false) if skillGuid isn't learned, is already
    /// equipped somewhere, its tree isn't unlocked (and isn't Standard), slotIndex is outside the
    /// tier's active-slot range, or (only when slotIndex is currently empty) the tier's cap is
    /// already full elsewhere.
    /// </summary>
    public static bool TryEquipAt(PhasixRuntimeData runtime, string skillGuid, SkillTreeType treeType, int slotIndex, int evolutionTier)
    {
        if (runtime == null || string.IsNullOrEmpty(skillGuid)) return false;
        if (!runtime.learnedSkillGuids.Contains(skillGuid)) return false;
        if (runtime.equippedSkillGuids.Contains(skillGuid)) return false;
        if (treeType != SkillTreeType.Standard && treeType != SkillTreeType.Testing
            && !SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(runtime).Contains(treeType)) return false;

        int maxSlots = SkillSlotCapacity.GetActiveSlotRange(evolutionTier).max;
        if (slotIndex < 0 || slotIndex >= maxSlots) return false;

        bool targetOccupied = slotIndex < runtime.equippedSkillGuids.Count
            && !string.IsNullOrEmpty(runtime.equippedSkillGuids[slotIndex]);
        if (!targetOccupied && CountEquipped(runtime) >= maxSlots) return false;

        EnsureCapacity(runtime.equippedSkillGuids, slotIndex + 1);
        runtime.equippedSkillGuids[slotIndex] = skillGuid;
        return true;
    }
}
