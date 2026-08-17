using UnityEngine;

/// <summary>
/// Inspector-swappable audio clip catalog for BattleAudioVfxHooks' 9 combat feedback events
/// (2026-08-10 — Phase 3 close-out pass). Deliberately a real ScriptableObject asset with
/// [SerializeField] AudioClip fields, following PrimalTypeChart.cs's pattern rather than this
/// codebase's other "catalog" convention (StatusEffectCatalog/ChainResultCatalog/
/// MasteryBonusCatalog are static classes with compile-time Dictionary backing) — only a real SO
/// gives Inspector-swappable references. That's the whole point: every clip here starts as
/// placeholder-quality generated audio (see AudioManager's Inspector setup notes), and swapping
/// one out for a real, licensed/commissioned sound later is a drag-and-drop onto this asset, no
/// code changes. GDD §27 (audio design) is tagged "Design work not yet started" — this catalog
/// invents no creative content itself, only the plumbing to hold whatever content arrives later.
/// </summary>
[CreateAssetMenu(fileName = "AudioCueCatalog", menuName = "Phasix/Audio/Audio Cue Catalog", order = 30)]
public class AudioCueCatalog : ScriptableObject
{
    [Header("Battle Outcomes")]
    [SerializeField] private AudioClip _battleWonClip;
    [SerializeField] private AudioClip _battleLostClip;
    [SerializeField] private AudioClip _battleFledClip;

    [Header("Actions")]
    [Tooltip("Plays on any skill cast (built-in or tree skill), player-side only — mirrors Evolution Burst's existing player-only fill scope. TODO: pending design — per-skill-tree variants (GDD §27); one shared clip for now.")]
    [SerializeField] private AudioClip _skillUsedClip;

    [Tooltip("Plays on a successful timed input — player offense (attack/skill) AND a player's successful Dodge/Parry against an enemy hit.")]
    [SerializeField] private AudioClip _timedInputSuccessClip;

    [Tooltip("Multi-Hit Volley only (2026-08-15) — plays the instant a ring is promoted to the FIFO queue's front (becomes the one actually listening for input), paired with BattleHUDController's pop-in scale animation on the ring itself. Reinforces the visual promotion with an audio cue rather than relying on sight alone.")]
    [SerializeField] private AudioClip _volleyRingPromotedClip;

    [Header("Hit Impact — Primal-type-flavored (GDD §27.3)")]
    [Tooltip("Index order matches PrimalType's first 8 base values: Fire, Water, Earth, Wind, Light, Shadow, Life, Lightning. Duo types fall back to their first base parent (PrimalTypeColor.GetDuoParents), same tie-break convention PlaceholderSkillResolver/PrimalTypeChart already use for ambiguous base resolution.")]
    [SerializeField] private AudioClip[] _hitImpactClipsByBaseType = new AudioClip[8];

    [Tooltip("Used whenever the specific-type slot above is unauthored (null) — keeps hit-impact audible even before all 8 slots are filled in.")]
    [SerializeField] private AudioClip _hitImpactFallbackClip;

    [Header("Progression")]
    [SerializeField] private AudioClip _bondMilestoneClip;
    [SerializeField] private AudioClip _evolvedClip;
    [SerializeField] private AudioClip _capturedClip;

    public AudioClip BattleWonClip => _battleWonClip;
    public AudioClip BattleLostClip => _battleLostClip;
    public AudioClip BattleFledClip => _battleFledClip;
    public AudioClip SkillUsedClip => _skillUsedClip;
    public AudioClip TimedInputSuccessClip => _timedInputSuccessClip;
    public AudioClip VolleyRingPromotedClip => _volleyRingPromotedClip;
    public AudioClip BondMilestoneClip => _bondMilestoneClip;
    public AudioClip EvolvedClip => _evolvedClip;
    public AudioClip CapturedClip => _capturedClip;

    /// <summary>Resolves a (possibly duo-merge) PrimalType to its base-type hit-impact clip, falling back to the shared fallback clip when that slot is unauthored.</summary>
    public AudioClip GetHitImpactClip(PrimalType damagedCreatureType)
    {
        PrimalType baseType = (int)damagedCreatureType < _hitImpactClipsByBaseType.Length
            ? damagedCreatureType
            : PrimalTypeColor.GetDuoParents(damagedCreatureType).a;

        AudioClip clip = _hitImpactClipsByBaseType[(int)baseType];
        return clip != null ? clip : _hitImpactFallbackClip;
    }
}
