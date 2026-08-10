/// <summary>
/// Marks a SkillData as one of the 5 always-mechanically-defined moves (Attack/Charge/Heal/Regen/
/// Capture) rather than a placeholder tree skill (2026-08 follow-up — see DECISIONS.md -> [Combat]
/// "Built-in moves become real, equippable skills"). None means "resolve through
/// PlaceholderSkillResolver like any other placeholder skill" — every other value means "skip
/// PlaceholderSkillResolver entirely and run this move's own dedicated, already-real mechanics"
/// (BattleManager.ResolveSkillAction dispatches on this field first).
/// </summary>
public enum BuiltInMoveType
{
    None,
    Attack,
    Charge,
    Heal,
    Regen,
    Capture,
}
