using System;
using System.Collections.Generic;

/// <summary>
/// Per-individual runtime state for one Phasix instance. Plain C# — never a ScriptableObject
/// or MonoBehaviour. This is the mutable counterpart to PhasixData: values here change during
/// play and are what actually gets saved (via a future PhasixSaveData DTO, not built in this
/// pass — see Evolution_System_Directive_v1_1_0.md §12).
///
/// Field shape matches Evolution_System_Directive_v1_1_0.md's PhasixRuntimeData spec so no
/// rework is needed when Phase 4's evolution graph (EvolutionNodeSO/EvolutionGraphSO) is built.
/// currentNodeGuid, speciesData, evolutionHistory, and discoveredNodeGuids are unwired
/// scaffolding until that graph exists — same forward-reference spirit as EventBus.cs.
///
/// Uses plain public fields (not the usual [SerializeField] private + property pattern) to
/// match the authoritative Directive verbatim — this class is never Inspector-serialized.
/// </summary>
public class PhasixRuntimeData
{
    /// <summary>Unique identifier for this specific creature instance.</summary>
    public string instanceId;

    /// <summary>
    /// GUID of the current EvolutionNodeSO. Drives which PhasixData SO to load.
    /// TODO: unwired until EvolutionGraphSO exists (Phase 4).
    /// </summary>
    public string currentNodeGuid;

    /// <summary>
    /// Cached reference to the current species/form data SO. NOT serialized — reconstructed
    /// on load by looking up currentNodeGuid in the global EvolutionGraphSO once it exists.
    /// </summary>
    [NonSerialized] public PhasixData speciesData;

    /// <summary>Current stat values. Resets to the tier floor on devolution.</summary>
    public StatBlock baseStats;

    /// <summary>
    /// Never resets — display via GameStrings.PoolName in UI until named. Grows per devolution:
    /// excess stats above the tier floor, scaled by bond multiplier.
    /// TODO: pending NumericalCalibration.md for the actual multiplier values.
    /// </summary>
    public StatBlock unnamedPool;

    /// <summary>
    /// Persists through devolution — never resets. Grows +1 per devolution cycle. Raises stat
    /// ceiling per tier and unlocks exotic evolution branches at minimum thresholds.
    /// </summary>
    public int aptitude;

    /// <summary>
    /// Variant growth-priority role. Changeable at runtime via Re-Tempering (GDD §6.4).
    /// Persists through evolution/devolution.
    /// </summary>
    public Temper temper;

    /// <summary>
    /// Shown on capture, fixed until changed by item (any personality to any other, GDD §7).
    /// Stat-growth nudge only, no skill effects.
    /// </summary>
    public Personality personality;

    /// <summary>
    /// Rolled per individual on capture. Changeable at runtime via "Origin Change" (GDD
    /// §14.4) — costs Bond% based on wheel distance (Adjacent = cheap, Opposite =
    /// expensive, e.g. Wild to Corrupted costs 15%). The only way to break through a bond
    /// floor. Immune to change once bondPercent reaches 100.
    /// </summary>
    public OriginType origin;

    /// <summary>
    /// The individual's currently manifested Signal type, chosen from speciesData.SignalPool.
    /// Changeable via a swap item (GDD §16.3).
    /// TODO: pending design — swap item (GDD §16.3)
    /// </summary>
    public SignalType activeSignalType;

    /// <summary>
    /// Index 0 = oldest, last index = most recent previous form. Drives devolution — a
    /// creature devolves back to whichever node it actually came from, not a fixed parent.
    /// </summary>
    public List<EvolutionHistoryEntry> evolutionHistory = new List<EvolutionHistoryEntry>();

    /// <summary>
    /// Starts with currentNodeGuid. Adjacent nodes revealed on evolution or scouting.
    /// TODO: unwired until EvolutionGraphSO exists (Phase 4).
    /// </summary>
    public HashSet<string> discoveredNodeGuids = new HashSet<string>();

    /// <summary>0-100. Cannot drop below bondFloor.</summary>
    public float bondPercent;

    /// <summary>Highest bond milestone floor reached. Permanent — never decreases.</summary>
    public float bondFloor;

    /// <summary>
    /// Cumulative bond loss applied this session (magnitude, not signed). Not in the
    /// literal schema — added so BondSystem can enforce the locked "session loss cap: 5%
    /// max regardless of event count" rule. Reset by BondSystem.ResetSessionLoss(), which
    /// should be called on hub visit / bank once that system exists (Blackout and Banking,
    /// WorldDesign_Directive_v0_1_0.md) — currently nothing calls it, so this only tracks
    /// within a single continuous play session.
    /// </summary>
    public float sessionBondLoss;

    /// <summary>Accumulates toward evolution thresholds. Resets to 0 on evolution.</summary>
    public float phaseSaturation;

    /// <summary>Drives stat growth, farmable from all Phasix. TODO: pending NumericalCalibration.md</summary>
    public int commonAura;

    /// <summary>Key = emotionalType string. Gates evolution, tied to emotional type/region. TODO: pending NumericalCalibration.md</summary>
    public Dictionary<string, int> specificAura = new Dictionary<string, int>();

    /// <summary>Gates exotic evolution branches, boss drops. TODO: pending NumericalCalibration.md</summary>
    public int rareVariantAura;

    /// <summary>Unlocked skill tree types for this individual. NEVER shrinks. Subset of speciesData.AvailableTreeTypes.</summary>
    public List<SkillTreeType> unlockedTreeTypes = new List<SkillTreeType>();

    /// <summary>
    /// All skills permanently learned, referenced by Unity asset GUID (not object reference —
    /// matches the save-data pattern in Evolution_System_Directive_v1_1_0.md). NEVER shrinks.
    /// </summary>
    public List<string> learnedSkillGuids = new List<string>();

    /// <summary>
    /// Active equipped skill slots, referenced by Unity asset GUID. Capacity: T1=2, T2=3,
    /// T3=4, T4=5, T5-T7=5-7 (derived from speciesData.EvolutionTier, not stored separately).
    /// </summary>
    public List<string> equippedSkillGuids = new List<string>();

    public PhasixRuntimeData(string nodeGuid)
    {
        instanceId = Guid.NewGuid().ToString();
        currentNodeGuid = nodeGuid;
        discoveredNodeGuids.Add(nodeGuid);
    }

    /// <summary>Base stat + unnamed pool contribution for a given attribute.</summary>
    public int EffectiveStat(StatType stat)
    {
        return GetStatValue(baseStats, stat) + GetStatValue(unnamedPool, stat);
    }

    private static int GetStatValue(StatBlock block, StatType stat)
    {
        switch (stat)
        {
            case StatType.Vitality: return block.Vitality;
            case StatType.Force: return block.Force;
            case StatType.Resonance: return block.Resonance;
            case StatType.Guard: return block.Guard;
            case StatType.Ward: return block.Ward;
            case StatType.Resolve: return block.Resolve;
            case StatType.Instinct: return block.Instinct;
            case StatType.Aura: return block.Aura;
            default: return 0;
        }
    }

    public bool HasReachedBondZone(BondZone zone) => bondFloor >= (float)zone;
}
