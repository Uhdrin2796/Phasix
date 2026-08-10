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

    public string SkillName => _skillName;
    public string Description => _description;
    public SkillTreeType TreeType => _treeType;
    public int PlaceholderIndex => _placeholderIndex;
    public ComboRuleType GrantsComboRule => _grantsComboRule;
    public BuiltInMoveType BuiltInMove => _builtInMove;
}
