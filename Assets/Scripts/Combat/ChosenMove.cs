/// <summary>
/// Result of a resolved skill-ring drag — always a real SkillData now (2026-08 follow-up: the 5
/// built-in moves Attack/Charge/Heal/Regen/Capture became real, equippable Standard-tree SkillData
/// instead of a separate hardcoded system, see BuiltInMoveType — so the old split between
/// BuiltInOptionIndex and Skill no longer applies; every ChosenMove carries one real skill).
/// BattleManager.ResolveSkillAction dispatches on Skill.BuiltInMove to tell a built-in move from a
/// tree skill.
/// </summary>
public readonly struct ChosenMove
{
    public readonly SkillData Skill;
    public readonly BattleParticipant Target;

    public ChosenMove(SkillData skill, BattleParticipant target)
    {
        Skill = skill;
        Target = target;
    }
}
