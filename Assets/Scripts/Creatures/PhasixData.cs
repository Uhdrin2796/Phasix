using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Design-time species/form template. One asset per species per evolution tier.
/// Read-only at runtime — never write to this asset during play (Hard Architecture Rule,
/// CLAUDE.md). Per-individual mutable state lives on PhasixRuntimeData instead.
///
/// Temper and Personality are deliberately NOT fields here despite being listed in
/// CLAUDE.md's schema block — both are rolled/assigned per individual and changeable at
/// runtime via a Temper Forge (GDD §6.4) or item (GDD §7), which would require writing to
/// this SO at play time. See DECISIONS.md for the full rationale.
/// </summary>
[CreateAssetMenu(fileName = "New PhasixData", menuName = "Phasix/Creature/Phasix Data", order = 1)]
public class PhasixData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Base species name for this tier form, before Temper compound naming is applied.")]
    [SerializeField] private string _speciesName;

    [Tooltip("Emotional root — grief, joy, anger, etc.")]
    [SerializeField] private string _emotionalType;

    [Tooltip("T1-T5 natural; T6-T7 fusion only.")]
    [SerializeField] private int _evolutionTier;

    [SerializeField] private PrimalType _primalType;
    [SerializeField] private OriginType _origin;

    [Tooltip("3-4 Signal types consistent with this creature's identity. Wild spawns can appear with any type from this pool.")]
    [SerializeField] private SignalType[] _signalPool;

    [SerializeField] private TempoType _tempoType;

    [Header("Base Stats — Tier Floor")]
    [Tooltip("Seed/floor values for this tier. A creature's current stats reset to these on devolution.\n" +
             "TODO: pending design — Evolution_System_Directive_v1_1_0.md specifies tier-floor stats living on the " +
             "not-yet-built EvolutionNodeSO.tierStatFloor (a StatBlock). These fields may migrate there once Phase 4's " +
             "evolution graph is built.\n" +
             "TODO: pending NumericalCalibration.md — actual values not yet calibrated.")]
    [SerializeField] private int _vitality;
    [SerializeField] private int _force;
    [SerializeField] private int _resonance;
    [SerializeField] private int _guard;
    [SerializeField] private int _ward;
    [SerializeField] private int _resolve;
    [SerializeField] private int _instinct;
    [SerializeField] private int _aura;

    [Header("Skill Trees")]
    [Tooltip("Superset of skill trees this species/form can ever unlock. Distinct from " +
             "PhasixRuntimeData.unlockedTreeTypes, which is an individual's current unlocked subset.")]
    [SerializeField] private List<SkillTreeType> _availableTreeTypes;

    public string SpeciesName => _speciesName;
    public string EmotionalType => _emotionalType;
    public int EvolutionTier => _evolutionTier;
    public PrimalType PrimalType => _primalType;
    public OriginType Origin => _origin;
    public SignalType[] SignalPool => _signalPool;
    public TempoType TempoType => _tempoType;

    public int Vitality => _vitality;
    public int Force => _force;
    public int Resonance => _resonance;
    public int Guard => _guard;
    public int Ward => _ward;
    public int Resolve => _resolve;
    public int Instinct => _instinct;
    public int Aura => _aura;

    public List<SkillTreeType> AvailableTreeTypes => _availableTreeTypes;
}
