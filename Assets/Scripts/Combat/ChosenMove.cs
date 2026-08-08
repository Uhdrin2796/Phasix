/// <summary>
/// Result of a resolved move-wheel drag (2026-08 session — skill-ring wiring, see DECISIONS.md ->
/// [Combat]) — either one of the 5 built-in moves (Attack/Charge/Heal/Regen/Capture, identified by
/// BuiltInOptionIndex) or a resolved skill-ring SkillData. Exactly one of BuiltInOptionIndex/Skill
/// is non-null. Replaces the old Action&lt;int, BattleParticipant&gt; callback shape on
/// BattleHUDController.ShowMoveSelection, which could only ever describe a built-in move —
/// BattleManager.PlayerTurn branches on which one is set.
/// </summary>
public readonly struct ChosenMove
{
    public readonly int? BuiltInOptionIndex;
    public readonly SkillData Skill;
    public readonly BattleParticipant Target;

    public ChosenMove(int? builtInOptionIndex, SkillData skill, BattleParticipant target)
    {
        BuiltInOptionIndex = builtInOptionIndex;
        Skill = skill;
        Target = target;
    }
}
