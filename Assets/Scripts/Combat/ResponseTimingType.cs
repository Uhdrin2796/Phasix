/// <summary>
/// When a melee Beat Sequence skill's timed-input window opens (Attack_Pattern_Directive_v0_1_0.md
/// Part 2's "Response timing" knob) — NEW, non-GDD combat-rules wiring, same tier as BeatType/
/// BuiltInMoveType/ResponseTimingType's siblings.
///
/// Reactive (default) is every skill built before this enum existed, unchanged: the timed-input
/// ring opens on the Attack beat itself, after Windup has already fully played
/// (BattleManager.ResolveMeleeAttackBeatOffense/Defense). PreEmptive moves the ring onto the
/// WindupReal/WindupFake beat(s) instead — the tell itself becomes the reactive moment, per Part 5's
/// Instant Strike/Read-the-Tell ("reacted to pre-emptively... not tracked") and Feint (a WindupFake's
/// window opens identically but its outcome is discarded, never applied).
/// </summary>
public enum ResponseTimingType
{
    Reactive,
    PreEmptive
}
