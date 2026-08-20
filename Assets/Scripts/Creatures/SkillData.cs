using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal stub — individual skill content is pending the skill design phase (GDD §14,
/// requires the full species roster). Exists only so PhasixRuntimeData's skill lists and
/// EventBus.OnSkillUsed compile. Do not flesh out skill content here.
/// TODO: pending design — skill content (GDD §14)
///
/// 2026-08 session (see DECISIONS.md -> [Combat]): two STRUCTURAL fields were added so the 36
/// generic placeholder assets can be clicked and mechanically resolved in live battle without
/// inventing per-skill balance content — PlaceholderIndex and GrantsComboRule below are wiring,
/// not game design. Neither carries a hand-picked number or effect; PlaceholderSkillResolver
/// derives all actual damage/status behavior from data that's already GDD-locked elsewhere
/// (SkillTreeCatalog, StatusEffectCatalog). Do NOT add real balance fields (power, cost, specific
/// status assignment) here — that's still genuinely pending the skill design phase.
///
/// 2026-08 follow-up: BuiltInMove is a THIRD structural field, same tier as the two above — marks
/// the 5 Standard-tree assets (Attack/Charge/Heal/Regen/Capture) that used to be hardcoded,
/// non-equippable battle moves and are now real, equippable/unequippable SkillData like any other
/// (user: "make them like any other skills... full customizability, for good or for worse"). A
/// non-None value tells BattleManager.ResolveSkillAction to skip PlaceholderSkillResolver entirely
/// and run that move's own dedicated mechanics instead — see BuiltInMoveType's own doc comment.
///
/// 2026-08-11 session: BeatSequence is a FOURTH structural field, same tier as the three above —
/// Attack_Pattern_Directive_v0_1_0.md Part 2's telegraph-knob schema, minimal version. An empty
/// array (the default, applied automatically to all existing assets) means "not a Beat Sequence
/// skill, resolve through the existing ranged/projectile path unchanged" — zero risk to any
/// pre-existing skill. A non-empty ordered BeatType list (e.g. [Approach, WindupReal, Attack] for
/// the minimal "Slash" example) tells BattleManager.ResolveSkillAction/ResolveEnemyDamageAction to
/// run BattleManager.ResolveMeleeBeatSequence instead — see BeatType's own doc comment. Per-beat
/// TIMING values (windup seconds, lunge distance, etc.) are deliberately NOT fields here — they're
/// centralized in BeatSequenceConfig instead, so pending-calibration numbers live in one place
/// rather than scattered across every hand-authored beat-sequence asset.
///
/// 2026-08-12 session (Group 1 archetypes — Instant Strike, Feint, Metronome, Jitter, see
/// Attack_Pattern_Directive_v0_1_0.md Part 5/Part 1's build order): ResponseTiming and
/// WindupJitterRangeSeconds are a FIFTH and SIXTH structural field, same tier as BeatSequence.
/// ResponseTiming defaults to Reactive, which is byte-for-byte today's existing behavior (timed
/// input on the Attack beat) — zero risk to Melee_Slash or any other existing asset. PreEmptive
/// moves the timed-input window onto the WindupReal/WindupFake beat(s) instead, per Part 5's
/// "reacted to pre-emptively" archetypes. WindupJitterRangeSeconds defaults to 0 (fixed duration,
/// i.e. the Metronome archetype needs no override at all since every Beat Sequence skill already
/// plays a fixed duration by default); a nonzero value randomizes that skill's Windup duration by
/// ± the given range each time it plays (the Jitter archetype) — still just data, no new balance
/// numbers beyond the placeholder itself (pending NumericalCalibration.md, same as every other
/// BeatSequenceConfig value).
///
/// 2026-08-14 session (Multi-Hit Volley — Attack_Pattern_Directive Part 5 Group 2's first
/// archetype, "several small hits in sequence, each its own small window"): VolleyRingSequence and
/// VolleyRingDurationsSeconds are a SEVENTH and EIGHTH structural field, same tier as BeatSequence/
/// StackingRhythm. Empty (default, applied automatically to all existing assets) means "not a
/// Volley skill" — zero risk to any pre-existing asset. A non-empty CompassPoint list tells
/// BattleManager to run ResolveMultiHitVolleyAttack instead of BeatSequence/StackingRhythm/the
/// placeholder path — bypasses all three entirely, same "own dedicated resolution path" pattern
/// StackingRhythm already established. Per-hit click-type requirement (left-click vs right-click,
/// offense only — user: "make the 1st 4 left click rings... last 4 click rings") is deliberately
/// NOT its own field — it's derived at runtime from each hit's position within
/// VolleyRingSequence's own length (first half = left-click, second half = right-click), so a
/// differently-sized future volley pattern needs zero code changes, pure new-asset authoring.
/// Originally encoded visually via the ring's animation direction (converging vs. expanding);
/// 2026-08-15 same-day follow-up replaced that with marker SHAPE (circle vs. square,
/// RingVisual.MarkerIsSquare) since direction needed a couple frames of motion to read, shape
/// reads instantly — see BattleHUDController.RunVolleyRingOffense.
///
/// 2026-08-15 follow-up (exploring alternate Volley "feels" — "Double Tap"/"Tracking Volley"):
/// VolleyDashForwardDurationsSeconds and VolleyDashBackDurationsSeconds are a NINTH and TENTH
/// structural field (originally one shared array, split in a same-day follow-up — see below).
/// Empty (default) falls back to the flat global dash cadence every hit previously used
/// unconditionally — zero risk to the original "Basic Count" asset. A non-empty array lets a skill
/// pace WHEN each hit launches, independently of VolleyRingDurationsSeconds (which only controls
/// how long a hit's ring stays open once it has already launched) — the missing knob needed for a
/// real pause between groups of hits rather than one continuous rapid stream.
///
/// 2026-08-15, same-day follow-up (user, after trying "Double Tap": "the 2nd two seem to have a
/// bigger delay than the 1st two attacks") — the original design used ONE shared value per hit for
/// both its forward and back dash leg. That can't express "pause only before this hit" without also
/// pausing after it (the same value drives both legs), so a single long entry meant to create one
/// mid-sequence gap always produced two. Split into independent forward/back arrays so the pause
/// can live entirely in one hit's forward leg while its own back leg — and therefore the gap after
/// it — stays exactly as fast as every other hit's.
///
/// 2026-08-17 session (Charge & Release + Sustained Pressure — Attack_Pattern_Directive Part 5
/// Group 2's second/third archetypes, "build these two together: both are 'hold input' instead of
/// 'tap input,' diverging only in scoring... Share one new hold-input primitive"): HoldInputArchetype
/// and its four tuning fields are an ELEVENTH through FIFTEENTH structural field, same tier as
/// StackingRhythm/VolleyRingSequence. None (default, applied automatically to all existing assets)
/// means "not a hold-input skill" — zero risk to any pre-existing asset. ChargeRelease is an OFFENSE
/// archetype (hold to charge, release for a damage bonus); SustainedPressure is a DEFENSE archetype
/// ("hold-to-guard" against an incoming attack, producing BattleHUDController.DefenseOutcome.Guard).
/// Both are scored on TWO instants — how well-timed the press was against an authored tell, and how
/// well-timed the release was — via BattleHUDController.RunHoldGesture's shared press-then-release
/// primitive; see HoldInputArchetype's own doc comment and BattleManager.ResolveChargeReleaseAttack.
/// Charge & Release additionally departs from every other skill's Miss handling: a miss on EITHER
/// scored instant cancels the attack for zero damage (not the usual reduced-but-nonzero
/// TimedInputConfig.MissDamageMultiplier), and a pass on both instants yields a continuous
/// quality-scaled damage range rather than a discrete Miss/Good/Perfect tier — deliberate, scoped to
/// this archetype only.
///
/// 2026-08-20 session (Zone/Positional — Attack_Pattern_Directive Part 5 Group 3's first archetype,
/// the first Lane Selection/no-timing input model in this codebase): ZonePositionalPattern and its
/// four supporting fields are a SIXTEENTH through TWENTIETH structural field, same tier as
/// HoldInputArchetype. None (default, applied automatically to all existing assets) means "not a
/// Zone/Positional skill" — zero risk to any pre-existing asset. A non-None pattern tells
/// BattleManager.ResolveEnemyDamageAction to run ResolveZonePositionalAttack instead of the normal
/// Dodge/Parry/Guard defense flow entirely — this archetype has no timing roll of any kind; the
/// defender's only response is real-time arrow-key movement during a highlight window
/// (BattleHUDController.RunZonePositionalWarning), and full damage applies to whoever is still
/// standing in a marked (Lane, Position) cell once that window closes. Row and Column patterns are
/// naturally lane-only/position-only; DiagonalX needs true per-cell granularity and uses a single
/// shared, hand-authored 13-cell table (ZonePositionalPatternResolver) rather than a per-skill
/// field, since every DiagonalX skill marks the same X shape. See ZonePositionalPatternType's own
/// doc comment and BattleManager.ResolveZonePositionalAttack.
/// </summary>
[CreateAssetMenu(fileName = "New SkillData", menuName = "Phasix/Combat/Skill Data (Stub)", order = 10)]
public class SkillData : ScriptableObject
{
    [Header("Stub — pending full skill design")]
    [SerializeField] private string _skillName;
    [SerializeField] [TextArea] private string _description;
    [SerializeField] private SkillTreeType _treeType;

    [Header("Placeholder wiring (structural, not balance — see class doc comment)")]
    [Tooltip("Which of this tree's 2 placeholder skills this is (0 or 1). Used only to pick a " +
             "deterministic index into a locked status/tree table — never a balance value.")]
    [SerializeField] private int _placeholderIndex;

    [Tooltip("If not None, equipping this skill grants the owner this alternate combo-detection " +
             "rule for the rest of the battle (see ComboRuleType, BattleParticipant.ActiveComboRules). " +
             "Assign by hand per-asset — this is a designer choice, not derived data.")]
    [SerializeField] private ComboRuleType _grantsComboRule = ComboRuleType.None;

    [Tooltip("None for every real placeholder skill (resolves through PlaceholderSkillResolver as usual). " +
             "Set only on the 5 Standard-tree assets (Attack/Charge/Heal/Regen/Capture) — tells " +
             "BattleManager to run that move's own dedicated mechanics instead of the generic " +
             "tree-derived damage/status logic.")]
    [SerializeField] private BuiltInMoveType _builtInMove = BuiltInMoveType.None;

    [Header("Melee Beat Sequence (Attack_Pattern_Directive Part 2 & 7 — structural, not balance)")]
    [Tooltip("Ordered beat list (Approach/WindupReal/WindupFake/Attack) — the exact data shape Part " +
             "7 specifies. Empty (default) means this skill is NOT a Beat Sequence skill and resolves " +
             "through the existing ranged/projectile path unchanged. Non-empty routes " +
             "BattleManager.ResolveSkillAction/ResolveEnemyDamageAction into the new melee path " +
             "instead. Authored by hand per-asset, same tier as GrantsComboRule.")]
    [SerializeField] private BeatType[] _beatSequence = System.Array.Empty<BeatType>();

    [Tooltip("Reactive (default) = timed-input ring opens on the Attack beat, today's existing " +
             "behavior. PreEmptive = the ring opens on the WindupReal/WindupFake beat(s) instead " +
             "(Attack_Pattern_Directive Part 5's 'reacted to pre-emptively' archetypes — Instant " +
             "Strike, Feint). Ignored when BeatSequence is empty.")]
    [SerializeField] private ResponseTimingType _responseTiming = ResponseTimingType.Reactive;

    [Tooltip("0 (default) = this skill's Windup duration is always the fixed BeatSequenceConfig " +
             "value (the Metronome archetype — already the default behavior, needs no override). " +
             ">0 = each time a Windup beat plays, its duration is randomized by +/- this many " +
             "seconds (the Jitter archetype). Ignored when BeatSequence is empty.")]
    [SerializeField] private float _windupJitterRangeSeconds;

    [Tooltip("None (default) = not a stacking-rhythm skill, resolves through the normal Beat " +
             "Sequence engine as usual. Metronome/Jitter = BattleManager.ResolveStackingRhythmAttack " +
             "owns this skill's ENTIRE resolution instead (BeatSequence above is ignored) — a " +
             "per-battle, per-skill use-count combo (BattleParticipant's stack tracking) that grows " +
             "by one required ring-beat each successful cast. See StackingRhythmType's own doc " +
             "comment for the full mechanic.")]
    [SerializeField] private StackingRhythmType _stackingRhythm = StackingRhythmType.None;

    [Header("Multi-Hit Volley (Attack_Pattern_Directive Part 5 Group 2 — structural, not balance)")]
    [Tooltip("Empty (default) = not a Multi-Hit Volley skill, resolves through the normal existing " +
             "paths unchanged. A non-empty ordered CompassPoint list tells BattleManager to run " +
             "ResolveMultiHitVolleyAttack instead — one hit per entry, in this exact order.")]
    [SerializeField] private CompassPoint[] _volleyRingSequence = System.Array.Empty<CompassPoint>();

    [Tooltip("Per-hit ring sweep duration (seconds), index-parallel to VolleyRingSequence — entry i " +
             "is hit i+1's ring duration. Falls back to BeatSequenceConfig.VolleyDefaultRingDurationSeconds " +
             "if shorter than the sequence (defensive only — author a full-length array). Ignored " +
             "when VolleyRingSequence is empty.")]
    [SerializeField] private float[] _volleyRingDurationsSeconds = System.Array.Empty<float>();

    [Tooltip("Per-hit dash-FORWARD leg duration (seconds), index-parallel to VolleyRingSequence — " +
             "entry i is hit i+1's own approach, played BEFORE it fires. Paces how long the player " +
             "waits for this hit to launch (unlike VolleyRingDurationsSeconds above, which only " +
             "affects how long that hit's OWN ring stays open once launched). Empty (default) or " +
             "shorter than the sequence falls back to the flat BeatSequenceConfig." +
             "VolleyDashLegDurationSeconds for any missing entry — the original \"Basic Count\" " +
             "pattern's continuous rapid cadence, unchanged.")]
    [SerializeField] private float[] _volleyDashForwardDurationsSeconds = System.Array.Empty<float>();

    [Tooltip("Per-hit dash-BACK leg duration (seconds), index-parallel to VolleyRingSequence — " +
             "entry i is hit i+1's own return, played AFTER it fires and BEFORE the next hit's own " +
             "forward leg begins. Deliberately a SEPARATE array from VolleyDashForwardDurationsSeconds " +
             "(2026-08-15 fix, user: \"the 2nd two seem to have a bigger delay than the 1st two\") — " +
             "a single shared forward+back value per hit can't express \"pause only before this hit\" " +
             "without ALSO pausing after it, since that same value drives both legs; keeping a long " +
             "forward value on the entry that should carry a real mid-sequence pause (e.g. \"Double " +
             "Tap\"'s hit 3) while its OWN back value stays short is what actually produces a single, " +
             "one-sided pause instead of doubling it. Same empty/short-falls-back-to-default rule as " +
             "the forward array above.")]
    [SerializeField] private float[] _volleyDashBackDurationsSeconds = System.Array.Empty<float>();

    [Header("Hold Input — Charge & Release / Sustained Pressure (Attack_Pattern_Directive Part 5 Group 2 — structural, not balance)")]
    [Tooltip("None (default) = not a hold-input skill, resolves through the normal existing paths " +
             "unchanged. ChargeRelease routes BattleManager.ResolveSkillAction into " +
             "ResolveChargeReleaseAttack (offense: hold-charge-release). SustainedPressure routes " +
             "ResolveEnemyDamageAction's existing Dodge/Parry/Miss flow into a new graduated Guard " +
             "outcome instead (defense: hold-to-guard).")]
    [SerializeField] private HoldInputArchetype _holdInputArchetype = HoldInputArchetype.None;

    [Tooltip("Seconds after the warning hop that mark the ideal PRESS instant for a Charge & Release " +
             "cast. Ignored unless HoldInputArchetype == ChargeRelease. 0 (default) falls back to " +
             "BeatSequenceConfig.ChargeReleaseDefaultTellSeconds.")]
    [SerializeField] private float _chargeReleaseTellSeconds;

    [Tooltip("How long the player should hold before releasing for a Charge & Release cast to land " +
             "\"Perfect\" on the release instant. Ignored unless HoldInputArchetype == ChargeRelease. " +
             "0 (default) falls back to BeatSequenceConfig.ChargeReleaseDefaultTargetHoldSeconds.")]
    [SerializeField] private float _chargeReleaseTargetHoldSeconds;

    [Tooltip("Seconds after the warning hop that mark the ideal PRESS instant for a Sustained " +
             "Pressure defense (when this skill is cast AT the player). Ignored unless " +
             "HoldInputArchetype == SustainedPressure. 0 (default) falls back to " +
             "BeatSequenceConfig.SustainedPressureDefaultTellSeconds.")]
    [SerializeField] private float _sustainedPressureTellSeconds;

    [Tooltip("This attack's own authored duration — defines the ideal RELEASE instant " +
             "(TellSeconds + this) for a Sustained Pressure defense. Ignored unless " +
             "HoldInputArchetype == SustainedPressure. 0 (default) falls back to " +
             "BeatSequenceConfig.SustainedPressureDefaultHoldSeconds.")]
    [SerializeField] private float _sustainedPressureHoldSeconds;

    [Header("Zone/Positional (Attack_Pattern_Directive Part 5 Group 3 — Lane Selection archetype, structural not balance)")]
    [Tooltip("None (default) = not a Zone/Positional skill, resolves through the normal existing " +
             "defense paths unchanged. Row/Column/DiagonalX route BattleManager." +
             "ResolveEnemyDamageAction into ResolveZonePositionalAttack instead of Dodge/Parry/Guard " +
             "entirely — this archetype has no timing roll at all.")]
    [SerializeField] private ZonePositionalPatternType _zonePositionalPattern = ZonePositionalPatternType.None;

    [Tooltip("Lanes marked when ZonePositionalPattern == Row (every position within each listed " +
             "lane is marked). Ignored otherwise. Worked example: [1, 3, 5, 7].")]
    [SerializeField] private int[] _zonePositionalRowLanes = System.Array.Empty<int>();

    [Tooltip("Positions marked when ZonePositionalPattern == Column (every lane at each listed " +
             "position is marked). Ignored otherwise. Worked example: [1, 3, 5].")]
    [SerializeField] private int[] _zonePositionalColumnPositions = System.Array.Empty<int>();

    [Tooltip("Seconds the attacker's warning glow plays before the zone highlight appears. 0 " +
             "(default) falls back to BeatSequenceConfig.ZonePositionalGlowSeconds.")]
    [SerializeField] private float _zonePositionalGlowSeconds;

    [Tooltip("Seconds the zone highlight is visible and the arrow-key response window stays open. " +
             "0 (default) falls back to BeatSequenceConfig.ZonePositionalHighlightSeconds.")]
    [SerializeField] private float _zonePositionalHighlightSeconds;

    public string SkillName => _skillName;
    public string Description => _description;
    public SkillTreeType TreeType => _treeType;
    public int PlaceholderIndex => _placeholderIndex;
    public ComboRuleType GrantsComboRule => _grantsComboRule;
    public BuiltInMoveType BuiltInMove => _builtInMove;
    public IReadOnlyList<BeatType> BeatSequence => _beatSequence;
    public ResponseTimingType ResponseTiming => _responseTiming;
    public float WindupJitterRangeSeconds => _windupJitterRangeSeconds;
    public StackingRhythmType StackingRhythm => _stackingRhythm;
    public IReadOnlyList<CompassPoint> VolleyRingSequence => _volleyRingSequence;
    public IReadOnlyList<float> VolleyRingDurationsSeconds => _volleyRingDurationsSeconds;
    public IReadOnlyList<float> VolleyDashForwardDurationsSeconds => _volleyDashForwardDurationsSeconds;
    public IReadOnlyList<float> VolleyDashBackDurationsSeconds => _volleyDashBackDurationsSeconds;
    public HoldInputArchetype HoldInputArchetype => _holdInputArchetype;
    public float ChargeReleaseTellSeconds => _chargeReleaseTellSeconds;
    public float ChargeReleaseTargetHoldSeconds => _chargeReleaseTargetHoldSeconds;
    public float SustainedPressureTellSeconds => _sustainedPressureTellSeconds;
    public float SustainedPressureHoldSeconds => _sustainedPressureHoldSeconds;
    public ZonePositionalPatternType ZonePositionalPattern => _zonePositionalPattern;
    public IReadOnlyList<int> ZonePositionalRowLanes => _zonePositionalRowLanes;
    public IReadOnlyList<int> ZonePositionalColumnPositions => _zonePositionalColumnPositions;
    public float ZonePositionalGlowSeconds => _zonePositionalGlowSeconds;
    public float ZonePositionalHighlightSeconds => _zonePositionalHighlightSeconds;
}
