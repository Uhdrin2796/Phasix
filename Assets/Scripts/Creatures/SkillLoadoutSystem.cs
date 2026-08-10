/// <summary>
/// Skill loadout mutation rules over PhasixRuntimeData.learnedSkillGuids/equippedSkillGuids
/// (2026-08 session, see DECISIONS.md -> [UI]), following this project's established
/// "static rules class over PhasixRuntimeData" convention (AuraStatAllocationSystem, BondSystem).
/// Built for the overworld Party menu's skill ring: drag an equipped orb onto another to swap
/// (SwapEquipped), drag a learned-but-unequipped tray skill onto an equipped orb to re-equip
/// (TryEquip), right-click an equipped orb to send it back to the tray (Unequip, not "unlearn" —
/// learnedSkillGuids is never touched here, it NEVER shrinks per PhasixRuntimeData's own contract).
/// </summary>
public static class SkillLoadoutSystem
{
    /// <summary>Equips skillGuid if it's learned, not already equipped, and there's a free slot for evolutionTier. No-op (returns false) otherwise.</summary>
    public static bool TryEquip(PhasixRuntimeData runtime, string skillGuid, int evolutionTier)
    {
        if (runtime == null || string.IsNullOrEmpty(skillGuid)) return false;
        if (!runtime.learnedSkillGuids.Contains(skillGuid)) return false;
        if (runtime.equippedSkillGuids.Contains(skillGuid)) return false;

        int maxSlots = SkillSlotCapacity.GetActiveSlotRange(evolutionTier).max;
        if (runtime.equippedSkillGuids.Count >= maxSlots) return false;

        runtime.equippedSkillGuids.Add(skillGuid);
        return true;
    }

    /// <summary>Removes skillGuid from equippedSkillGuids only — it stays in learnedSkillGuids and can be re-equipped later. The right-click action.</summary>
    public static void Unequip(PhasixRuntimeData runtime, string skillGuid)
    {
        if (runtime == null) return;
        runtime.equippedSkillGuids.Remove(skillGuid);
    }

    /// <summary>Swaps the equipped skills at indexA/indexB. No-op if either index is out of range. The equipped-orb-to-equipped-orb drag action.</summary>
    public static void SwapEquipped(PhasixRuntimeData runtime, int indexA, int indexB)
    {
        if (runtime == null) return;
        var list = runtime.equippedSkillGuids;
        if (indexA < 0 || indexA >= list.Count || indexB < 0 || indexB >= list.Count) return;
        if (indexA == indexB) return;

        (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
    }

    /// <summary>
    /// Equips skillGuid directly into ring position slotIndex — the overworld Party menu's
    /// tray-to-ring drag action (dragging a learned-but-unequipped skill onto a specific orb
    /// position, occupied or empty). If slotIndex already holds a different skill, that skill is
    /// simply overwritten out of equippedSkillGuids (it stays in learnedSkillGuids, so it
    /// reappears in the tray — same "unequip, not unlearn" contract as Unequip). Fails (no-op,
    /// returns false) if skillGuid isn't learned, is already equipped somewhere, or slotIndex is
    /// outside the tier's active-slot range.
    /// </summary>
    public static bool TryEquipAt(PhasixRuntimeData runtime, string skillGuid, int slotIndex, int evolutionTier)
    {
        if (runtime == null || string.IsNullOrEmpty(skillGuid)) return false;
        if (!runtime.learnedSkillGuids.Contains(skillGuid)) return false;
        if (runtime.equippedSkillGuids.Contains(skillGuid)) return false;

        int maxSlots = SkillSlotCapacity.GetActiveSlotRange(evolutionTier).max;
        if (slotIndex < 0 || slotIndex >= maxSlots) return false;

        if (slotIndex < runtime.equippedSkillGuids.Count)
        {
            runtime.equippedSkillGuids[slotIndex] = skillGuid;
        }
        else if (runtime.equippedSkillGuids.Count < maxSlots)
        {
            runtime.equippedSkillGuids.Add(skillGuid);
        }
        else
        {
            return false;
        }

        return true;
    }
}
