using System;
using System.Collections.Generic;

/// <summary>
/// Serializable snapshot of PartySystem's roster (2026-08 session, see DECISIONS.md -> [Save]).
/// Index-aligned with PartySystem.MaxPartySize slots — an empty slot is a null entry in the list,
/// not a shorter list, so slot indices survive a save/load round trip unchanged.
/// </summary>
[Serializable]
public class PartySaveData
{
    public List<PhasixSaveData> slots = new List<PhasixSaveData>();
    public int activeSlotIndex = -1;

    /// <summary>Builds a snapshot from the live PartySystem singleton. A slot whose Phasix can't be resolved to a save-safe form (see PhasixSaveData.FromRuntime) is saved as null rather than aborting the whole save.</summary>
    public static PartySaveData FromPartySystem(PartySystem partySystem, SpeciesDatabase speciesDatabase)
    {
        var data = new PartySaveData { activeSlotIndex = partySystem.ActiveSlotIndex };

        for (int i = 0; i < PartySystem.MaxPartySize; i++)
        {
            PhasixRuntimeData runtime = partySystem.GetSlot(i);
            data.slots.Add(runtime != null ? PhasixSaveData.FromRuntime(runtime, speciesDatabase) : null);
        }

        return data;
    }
}
