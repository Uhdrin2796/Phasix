/// <summary>
/// One of 8 positions arranged around a TARGET creature that a Multi-Hit Volley ring can occupy
/// (Attack_Pattern_Directive_v0_1_0.md Part 5 Group 2, 2026-08-14, user: "8 positions around the
/// target that have the ring input... modular so for a skill you can choose which rings and
/// timings to hit") — NEW, non-GDD combat-rules wiring, same tier as StackingRhythmType/
/// ResponseTimingType. Order matches a real compass, clockwise from North.
///
/// Index (enum ordinal) feeds directly into BattleHUDController.ComputeCompassOffset, the same
/// "index -> angle -> dx/dy" pattern PositionSkillSlots already uses for the 12-slot skill wheel,
/// just re-based so index 0 (N) reads as straight "up" (90 degrees) instead of PositionSkillSlots'
/// clock-hour offset. No geometry lives on this enum itself — that stays in BattleHUDController,
/// same separation BeatType/BuiltInMoveType already keep from their own resolution logic.
/// </summary>
public enum CompassPoint
{
    N, NE, E, SE, S, SW, W, NW
}
