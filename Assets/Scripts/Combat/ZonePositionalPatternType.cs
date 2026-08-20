/// <summary>
/// Which Zone/Positional pattern a skill uses (2026-08-20, Attack_Pattern_Directive Part 5 Group
/// 3's first archetype — the first Lane Selection/no-timing input model in this codebase). Same
/// tier as HoldInputArchetype/StackingRhythmType. A skill with a non-None value bypasses the normal
/// tap-timing rings and RunHoldGesture entirely — BattleManager.ResolveZonePositionalAttack owns
/// this skill's entire defense resolution, and the defender's only response is real-time arrow-key
/// movement during BattleHUDController.RunZonePositionalWarning's highlight window, not a ring or
/// hold gesture.
///
/// Row and Column mark whole lanes/positions (every cell where the given axis value matches, any
/// value on the other axis) via SkillData.ZonePositionalRowLanes/ZonePositionalColumnPositions.
/// DiagonalX marks a single shared, hand-authored 13-cell table (ZonePositionalPatternResolver) —
/// every DiagonalX skill marks the identical X shape, so it needs no per-skill field at all.
/// </summary>
public enum ZonePositionalPatternType
{
    None,
    Row,
    Column,
    DiagonalX
}
