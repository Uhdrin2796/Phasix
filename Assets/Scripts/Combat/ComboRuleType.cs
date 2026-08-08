/// <summary>
/// Which combo-detection rule applies to a participant's skill-use history. CrossTreeSequence is
/// the GDD §4.2-locked base rule ("skills from different trees create combos when used in
/// sequence") — always active for every participant, evaluated via ComboEngine.DetectCombo exactly
/// as before this enum existed.
///
/// RepeatSameSkill and TimedInputStreak are NEW, user-directed mechanics (2026-08 session, see
/// DECISIONS.md -> [Combat]) — NOT a GDD transcription like everything else in the combat rules
/// layer. A passive skill can grant one of these to its owner while equipped (SkillData.
/// GrantsComboRule), letting specific skill trees alter how the combo mechanic behaves for that
/// creature instead of every creature sharing one fixed rule. Evaluated via ComboRuleEvaluator.
///
/// None is a sentinel meaning "this skill grants no alternate rule" — used only on
/// SkillData.GrantsComboRule, never a live entry in BattleParticipant.ActiveComboRules.
/// </summary>
public enum ComboRuleType
{
    None,
    CrossTreeSequence,
    RepeatSameSkill,
    TimedInputStreak
}
