using System;
using System.Collections.Generic;

/// <summary>
/// Serializable DTO mirroring PhasixRuntimeData for JSON persistence (2026-08 session, see
/// DECISIONS.md -> [Save]). PhasixRuntimeData itself isn't used directly as the save format
/// because two of its fields aren't JsonUtility-friendly: speciesData is [NonSerialized] (a
/// ScriptableObject reference can't round-trip through a save file — resolved here via
/// speciesGuid + SpeciesDatabase instead, same pattern the skill lists already use for
/// learnedSkillGuids/equippedSkillGuids) and specificAura is a Dictionary, which JsonUtility
/// doesn't support at all — flattened here into parallel key/value lists.
///
/// Every other field maps straight across: primitives, enums, StatBlock (already [Serializable]),
/// List&lt;string&gt;/List&lt;SkillTreeType&gt;/List&lt;EvolutionHistoryEntry&gt; are all natively
/// JsonUtility-compatible. discoveredNodeGuids is a HashSet&lt;string&gt; on the runtime side
/// (JsonUtility doesn't support HashSet either) — flattened to a List&lt;string&gt; here too.
/// </summary>
[Serializable]
public class PhasixSaveData
{
    public string instanceId;
    public string currentNodeGuid;
    public string speciesGuid;

    public StatBlock baseStats;
    public StatBlock unnamedPool;

    public int aptitude;
    public Temper temper;
    public Personality personality;
    public OriginType origin;
    public SignalType activeSignalType;

    public List<EvolutionHistoryEntry> evolutionHistory = new List<EvolutionHistoryEntry>();
    public List<string> discoveredNodeGuids = new List<string>();

    public float bondPercent;
    public float bondFloor;
    public float sessionBondLoss;
    public float phaseSaturation;

    public int commonAura;
    public int auraAllocatedPoints;

    public List<string> specificAuraKeys = new List<string>();
    public List<int> specificAuraValues = new List<int>();

    public int rareVariantAura;

    public List<SkillTreeType> unlockedTreeTypes = new List<SkillTreeType>();
    public List<string> learnedSkillGuids = new List<string>();
    public List<string> equippedSkillGuids = new List<string>();

    /// <summary>Builds a save-ready DTO from a live PhasixRuntimeData. Returns null if speciesData can't be resolved to a GUID (e.g. not registered in the SpeciesDatabase) — a Phasix that can't be saved shouldn't silently corrupt the slot with a broken reference.</summary>
    public static PhasixSaveData FromRuntime(PhasixRuntimeData runtime, SpeciesDatabase speciesDatabase)
    {
        if (runtime == null) return null;
        if (runtime.speciesData == null || speciesDatabase == null
            || !speciesDatabase.TryGetGuid(runtime.speciesData, out string speciesGuid))
        {
            return null;
        }

        var data = new PhasixSaveData
        {
            instanceId = runtime.instanceId,
            currentNodeGuid = runtime.currentNodeGuid,
            speciesGuid = speciesGuid,
            baseStats = runtime.baseStats,
            unnamedPool = runtime.unnamedPool,
            aptitude = runtime.aptitude,
            temper = runtime.temper,
            personality = runtime.personality,
            origin = runtime.origin,
            activeSignalType = runtime.activeSignalType,
            evolutionHistory = new List<EvolutionHistoryEntry>(runtime.evolutionHistory),
            discoveredNodeGuids = new List<string>(runtime.discoveredNodeGuids),
            bondPercent = runtime.bondPercent,
            bondFloor = runtime.bondFloor,
            sessionBondLoss = runtime.sessionBondLoss,
            phaseSaturation = runtime.phaseSaturation,
            commonAura = runtime.commonAura,
            auraAllocatedPoints = runtime.auraAllocatedPoints,
            rareVariantAura = runtime.rareVariantAura,
            unlockedTreeTypes = new List<SkillTreeType>(runtime.unlockedTreeTypes),
            learnedSkillGuids = new List<string>(runtime.learnedSkillGuids),
            equippedSkillGuids = new List<string>(runtime.equippedSkillGuids),
        };

        foreach (KeyValuePair<string, int> entry in runtime.specificAura)
        {
            data.specificAuraKeys.Add(entry.Key);
            data.specificAuraValues.Add(entry.Value);
        }

        return data;
    }

    /// <summary>Reconstructs a live PhasixRuntimeData from this DTO. Returns null if speciesGuid can't be resolved (e.g. the species asset was removed from the SpeciesDatabase since saving).</summary>
    public PhasixRuntimeData ToRuntime(SpeciesDatabase speciesDatabase)
    {
        if (speciesDatabase == null || !speciesDatabase.TryGetByGuid(speciesGuid, out PhasixData species))
        {
            return null;
        }

        var runtime = new PhasixRuntimeData(currentNodeGuid)
        {
            instanceId = instanceId,
            speciesData = species,
            baseStats = baseStats,
            unnamedPool = unnamedPool,
            aptitude = aptitude,
            temper = temper,
            personality = personality,
            origin = origin,
            activeSignalType = activeSignalType,
            evolutionHistory = new List<EvolutionHistoryEntry>(evolutionHistory),
            discoveredNodeGuids = new HashSet<string>(discoveredNodeGuids),
            bondPercent = bondPercent,
            bondFloor = bondFloor,
            sessionBondLoss = sessionBondLoss,
            phaseSaturation = phaseSaturation,
            commonAura = commonAura,
            auraAllocatedPoints = auraAllocatedPoints,
            rareVariantAura = rareVariantAura,
            unlockedTreeTypes = new List<SkillTreeType>(unlockedTreeTypes),
            learnedSkillGuids = new List<string>(learnedSkillGuids),
            equippedSkillGuids = new List<string>(equippedSkillGuids),
        };

        int pairCount = Math.Min(specificAuraKeys.Count, specificAuraValues.Count);
        for (int i = 0; i < pairCount; i++)
        {
            runtime.specificAura[specificAuraKeys[i]] = specificAuraValues[i];
        }

        return runtime;
    }
}
