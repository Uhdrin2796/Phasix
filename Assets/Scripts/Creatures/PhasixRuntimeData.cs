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

    /// <summary>
    /// Running total of stat points ever purchased via AuraStatAllocationSystem.TryAllocateStatPoint
    /// (2026-08 follow-up fix — see DECISIONS.md -> [Combat]). This, not baseStats.Total, is what
    /// AuraTierCeiling gates: Progression_Directive_v0_1_0.md says "Stat growth through Common Aura
    /// is capped per tier" — growth, i.e. Aura-purchased points, not the creature's total stat value
    /// including whatever it started with. Gating on baseStats.Total directly made every allocation
    /// attempt fail instantly for any species whose innate stats already exceed the tier-1 placeholder
    /// ceiling (e.g. a 120-Vitality starter against a ceiling of 40) — this field fixes that without
    /// touching either placeholder ceiling constant, both still pending NumericalCalibration.md.
    /// Never resets today; devolution isn't built yet, so whether this should reset alongside
    /// baseStats on devolution is unresolved — flagged for whoever builds that system.
    /// </summary>
    public int auraAllocatedPoints;

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

    /// <summary>
    /// Debug-only, session-scoped tier simulation for the Party menu's skill web view (2026-08).
    /// When set, view/equip logic should prefer this over speciesData.EvolutionTier so unlocks and
    /// slot capacity can be play-tested without a real (not-yet-built, Phase 4) evolution. Never
    /// persisted to PhasixSaveData/SaveSystem — resets on load/restart, same convention as
    /// DebugMovementPresetCycler's presets. Must never be used to mutate speciesData (a
    /// ScriptableObject) or the real unlockedTreeTypes list — see
    /// SkillTreeUnlockSystem.GetEffectiveUnlockedTrees, the single source of truth for both the
    /// web view's display and SkillLoadoutSystem's real equip gate.
    /// </summary>
    public int? DebugTierOverride;

    /// <summary>
    /// Debug-only, session-scoped "show every GDD skill tree as unlocked" toggle for the skill web
    /// view — independent of DebugTierOverride (tier still gates equip SLOT capacity; this only
    /// affects which TREES render as unlocked/interactive). Never persisted to
    /// PhasixSaveData/SaveSystem, same convention as DebugTierOverride. See
    /// SkillTreeUnlockSystem.GetEffectiveUnlockedTrees, which checks this first, before tier.
    /// </summary>
    public bool DebugUnlockAllTrees;

    /// <summary>
    /// Preset battle starting position — which of the 7 depth lanes (rows) and which of the 5
    /// fixed horizontal positions within that row (Combat_Directive Part 2/3, LaneMovementSystem)
    /// this individual starts a battle in. Set via the Party menu's formation grid picker
    /// (2026-08-12, user: "lets just have 5 positions across a lane... you can preset which
    /// position you want to be in"). Read by BattleParticipant's constructor to seed
    /// LaneIndex/PositionIndex; NOT the same as those live, battle-only fields — this is the
    /// persistent pre-battle preference, they're the (possibly since-moved) in-battle state.
    /// Defaults to the center of each range (lane 4 = "Mid", position 3 = center column), matching
    /// LaneMovementSystem.DefaultStartingLane. Exclusive per party member — SetPreferredSlot on
    /// the Party menu side (not built here, this is just storage) is responsible for preventing two
    /// party members from sharing the same (lane, position) pair.
    /// </summary>
    public int preferredLaneIndex = LaneMovementSystem.DefaultStartingLane;

    /// <summary>See preferredLaneIndex's doc comment. Defaults to the center column (3 of 5).</summary>
    public int preferredPositionIndex = LaneMovementSystem.DefaultStartingPosition;

    /// <summary>
    /// Zone/Positional offense-direction follow-up (2026-08-21) — per-encounter multiplier on this
    /// creature's own Instinct/bond-scaled dodge chance when it's the enemy defending against a
    /// player-cast Zone/Positional skill (EnemyAI.TryChooseDodgeStep, BattleParticipant.
    /// DifficultyTier). Set by EncounterTrigger's Inspector field for wild spawns; meaningless for
    /// player-side participants (no code path ever reads it there), same "applies to both sides
    /// uniformly, harmless no-op for the side that never uses it" convention as preferredLaneIndex/
    /// preferredPositionIndex above.
    /// </summary>
    public EnemyDifficultyTier enemyDifficultyTier = EnemyDifficultyTier.Standard;

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
