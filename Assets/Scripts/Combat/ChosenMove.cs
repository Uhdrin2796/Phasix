/// <summary>
/// Result of a resolved skill-ring choice — always a real SkillData now (2026-08 follow-up: the
/// built-in moves Attack/Charge/Heal/Regen/Capture became real, equippable Standard-tree SkillData
/// instead of a separate hardcoded system, see BuiltInMoveType — so the old split between
/// BuiltInOptionIndex and Skill no longer applies; every ChosenMove carries one real skill).
/// BattleManager.ResolveSkillAction dispatches on Skill.BuiltInMove to tell a built-in move from a
/// tree skill.
///
/// Move (BuiltInMoveType.Move) never constructs one of these — 2026-08-12's redesign made it a
/// dedicated always-present icon with its own drag-to-stage-position flow
/// (BattleHUDController.MoveConfirmed -> BattleManager.HandleMoveConfirmed), bypassing the
/// ChosenMove/ResolveSkillAction pipeline entirely rather than routing a destination lane/position
/// through it (an earlier version of this struct briefly carried optional DestinationLane/
/// DestinationPosition fields for exactly that purpose — removed once Move stopped being a
/// ring-orb choice at all).
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
