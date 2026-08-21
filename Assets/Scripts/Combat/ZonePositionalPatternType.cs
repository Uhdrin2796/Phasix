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
///
/// 2026-08-20 follow-up (Split Attention, item 8 — "it's Zone/Positional with two simultaneous
/// marks, one or both fake, same infrastructure"): SurroundingBurst and FacingArrowhead are the
/// first patterns computed RELATIVE to the locked target's live (Lane, Position) at cast time,
/// rather than from fixed authored/absolute data — see ZonePositionalPatternResolver.GetMarkedCells'
/// targetLane/targetPosition parameters. Both mix real and fake cells within a single telegraphed
/// shape (ZoneCell.IsReal), by a FIXED geometric rule decided once per pattern, not randomized per
/// cast — a deliberate choice (user-directed) so a player who learns the rule can play it correctly
/// every time, unlike a pure coin-flip fake would allow. SurroundingBurst marks a 3x3 area centered
/// on the target (center + the 4 diagonal corners are real, the 4 orthogonal edges are the safe
/// cells — forces an actual move, since standing still is damaging). FacingArrowhead marks a
/// chevron/arrowhead shape with its tip pointing toward the target's facing direction (this
/// codebase has no general facing-direction system — Combat_Directive_v0_1_0.md Part 9 explicitly
/// notes one "doesn't exist anywhere else in the design" — so this is scoped narrowly: for a
/// player-side target, facing is a fixed convention, "toward increasing Position," matching this
/// pass's enemy-casts/player-defends-only scope, not a new general system); the target's own cell
/// and one cell toward the tip are both safe, nested inside the chevron's hollow throat, while the
/// solid wall behind the target and the tapering body/tip around the safe pocket are all real.
/// </summary>
public enum ZonePositionalPatternType
{
    None,
    Row,
    Column,
    DiagonalX,
    SurroundingBurst,
    FacingArrowhead
}
