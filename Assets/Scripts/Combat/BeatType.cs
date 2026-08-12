/// <summary>
/// One beat in a melee Beat Sequence (Attack_Pattern_Directive_v0_1_0.md Part 7,
/// melee_beat_sequence.mermaid) — NEW, non-GDD combat-rules wiring, same tier as ComboRuleType/
/// BuiltInMoveType. A skill's SkillData.BeatSequence is just an ordered list of these tags; the
/// Approach/WindupReal/WindupFake/Attack state machine (BeatSequenceRunner, BattleManager.
/// ResolveMeleeBeatSequence) is data-driven off that list, not a fixed per-skill state machine.
///
/// WindupReal and WindupFake share the exact same tween shape and only differ in duration
/// (BeatSequenceConfig.WindupRealDurationSeconds vs WindupFakeDurationSeconds) — this is deliberate
/// per Part 7: "this is also how you'll actually test whether players can tell them apart."
///
/// No None/sentinel value — unlike ComboRuleType/BuiltInMoveType, this enum is only ever used as an
/// element inside a beat list (SkillData.BeatSequence), never as a single nullable field, so there's
/// no "no value" case to represent.
/// </summary>
public enum BeatType
{
    Approach,
    WindupReal,
    WindupFake,
    Attack
}
