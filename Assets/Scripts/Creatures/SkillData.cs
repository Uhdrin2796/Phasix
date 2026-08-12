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

    public string SkillName => _skillName;
    public string Description => _description;
    public SkillTreeType TreeType => _treeType;
    public int PlaceholderIndex => _placeholderIndex;
    public ComboRuleType GrantsComboRule => _grantsComboRule;
    public BuiltInMoveType BuiltInMove => _builtInMove;
    public IReadOnlyList<BeatType> BeatSequence => _beatSequence;
}
