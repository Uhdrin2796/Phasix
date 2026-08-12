/// <summary>
/// Marks a SkillData as one of the always-mechanically-defined moves (Attack/Charge/Heal/Regen/
/// Capture/Move) rather than a placeholder tree skill (2026-08 follow-up — see DECISIONS.md ->
/// [Combat] "Built-in moves become real, equippable skills"). None means "resolve through
/// PlaceholderSkillResolver like any other placeholder skill" — every other value means "skip
/// PlaceholderSkillResolver entirely and run this move's own dedicated, already-real mechanics"
/// (BattleManager.ResolveSkillAction dispatches on this field first).
///
/// Move (2026-08-12, user: "5 positions across a lane... only one position can be filled at a
/// time... The inbattle move could use the same or system that the preslot uses for selection") —
/// repositions the caster to a different (lane, position) formation slot instead of dealing damage
/// or a status; consumes the turn's action like any other move (BattleManager sets
/// HasActedThisTurn = true generically before dispatch, same as every other skill). Hard-excluded
/// from EnemyAI.ChooseSkill for now, same as Capture — no AI logic exists yet for deciding when an
/// enemy should reposition.
/// </summary>
public enum BuiltInMoveType
{
    None,
    Attack,
    Charge,
    Heal,
    Regen,
    Capture,
    Move,
}
