using System;
using System.IO;
using UnityEngine;

/// <summary>
/// JSON save/load to Application.persistentDataPath, 3 manual slots (2026-08 session, see
/// DECISIONS.md -> [Save]). Format decision (JsonUtility, not Newtonsoft) already locked in
/// DECISIONS.md's "[Save] Save format" entry from March 2026 — JsonUtility was chosen there as
/// one of two acceptable options; picked here specifically to avoid adding a new package
/// dependency, since PhasixSaveData/PartySaveData/SaveFile are already hand-flattened to be fully
/// JsonUtility-compatible (no Dictionary/HashSet at the DTO layer).
///
/// "Auto-load on boot" (GameManager.Awake) doesn't need a separate "which slot is current"
/// marker — TryGetNewestSlot just compares each slot file's own last-write time and picks the
/// newest, so saving to any slot naturally becomes "continue from here" next launch. Only
/// SpeciesDatabase is needed for round-tripping (not SkillDatabase) — learnedSkillGuids/
/// equippedSkillGuids are already plain GUID strings on PhasixRuntimeData, resolved to SkillData
/// only where the UI displays them, never as part of the save data itself.
/// </summary>
public static class SaveSystem
{
    public const int SlotCount = 3;

    /// <summary>Test-only override for the save directory — EditMode tests point this at a temp
    /// folder so they never read/write the same files real gameplay saves to (Application.
    /// persistentDataPath resolves to the same fixed folder in every Editor Play/test session for
    /// this project). Never set outside test code; null (the default) uses the real path.</summary>
    public static string SaveDirectoryOverride;

    private static string SaveDirectory => string.IsNullOrEmpty(SaveDirectoryOverride) ? Application.persistentDataPath : SaveDirectoryOverride;

    private static string SlotPath(int slot) => Path.Combine(SaveDirectory, $"save_slot_{slot}.json");

    public static bool SlotExists(int slot) => File.Exists(SlotPath(slot));

    /// <summary>Timestamp of a slot's file, for the Save tab's "last saved" display. Returns null if the slot doesn't exist.</summary>
    public static DateTime? GetSlotTimestamp(int slot)
        => SlotExists(slot) ? File.GetLastWriteTimeUtc(SlotPath(slot)) : (DateTime?)null;

    /// <summary>Snapshots partySystem's current roster and writes it to the given slot, overwriting whatever was there.</summary>
    public static void Save(int slot, PartySystem partySystem, SpeciesDatabase speciesDatabase)
    {
        Directory.CreateDirectory(SaveDirectory); // Application.persistentDataPath always exists already; only matters for a fresh test temp dir

        var saveFile = new SaveFile
        {
            party = PartySaveData.FromPartySystem(partySystem, speciesDatabase),
            savedAtIso8601 = DateTime.UtcNow.ToString("o"),
        };

        string json = JsonUtility.ToJson(saveFile, prettyPrint: true);
        File.WriteAllText(SlotPath(slot), json);
    }

    /// <summary>Reads and deserializes a slot's file. False (out param null) if the slot doesn't exist or fails to parse.</summary>
    public static bool TryLoad(int slot, out SaveFile saveFile)
    {
        saveFile = null;
        string path = SlotPath(slot);
        if (!File.Exists(path)) return false;

        string json = File.ReadAllText(path);
        saveFile = JsonUtility.FromJson<SaveFile>(json);
        return saveFile != null;
    }

    /// <summary>Finds whichever of the 3 slots has the newest file-write time — the entire "auto-continue" mechanism, no separate marker needed. False if no slot has ever been saved to.</summary>
    public static bool TryGetNewestSlot(out int slot)
    {
        slot = -1;
        DateTime newest = DateTime.MinValue;

        for (int i = 0; i < SlotCount; i++)
        {
            if (!SlotExists(i)) continue;

            DateTime writeTime = File.GetLastWriteTimeUtc(SlotPath(i));
            if (writeTime > newest)
            {
                newest = writeTime;
                slot = i;
            }
        }

        return slot >= 0;
    }

    /// <summary>Rebuilds partySystem's slots from a loaded SaveFile, resolving each PhasixSaveData back to a live PhasixRuntimeData via speciesDatabase. A slot whose species can't be resolved (e.g. removed from the database since saving) loads as empty rather than throwing.</summary>
    public static void ApplyToPartySystem(SaveFile saveFile, PartySystem partySystem, SpeciesDatabase speciesDatabase)
    {
        if (saveFile?.party == null || partySystem == null) return;

        for (int i = 0; i < PartySystem.MaxPartySize; i++)
        {
            PhasixSaveData slotData = i < saveFile.party.slots.Count ? saveFile.party.slots[i] : null;
            PhasixRuntimeData runtime = slotData?.ToRuntime(speciesDatabase);
            partySystem.SetSlot(i, runtime);
        }

        if (saveFile.party.activeSlotIndex >= 0)
        {
            partySystem.SetActiveSlot(saveFile.party.activeSlotIndex);
        }
    }

    /// <summary>Deletes all 3 save files — the debug "New Game" reset. Does not touch PartySystem itself; GameManager.ResetToNewGame reloads the scene separately, which naturally gives every subsystem (including PartySystem) a fresh start.</summary>
    public static void DeleteAllSlots()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            string path = SlotPath(i);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
