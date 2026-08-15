/// <summary>
/// Which per-battle stacking rhythm combo a skill uses (2026-08-13, user: "Metronome... shoot the
/// warning, then show the timing on the player, only after a success then shoot the projectile...
/// Then on next cast have it do 2 ring inputs back to back... Jitter will follow a similar pattern...
/// but instead has a different beat") — NEW, non-GDD combat-rules wiring, same tier as BeatType/
/// ResponseTimingType. A skill with a non-None value bypasses the generic Beat Sequence engine
/// (ResolveMeleeBeatSequence) entirely — BattleManager.ResolveStackingRhythmAttack owns its whole
/// flow instead: warning hop, N alternating dash-forward/dash-back beats (N = this skill's current
/// per-battle use count + 1, tracked on BattleParticipant), a scaled-up payoff projectile if every
/// beat succeeds, and an unconditional return-to-origin.
///
/// Metronome vs Jitter differ only in each beat's duration/dash-distance pattern (BeatSequenceConfig.
/// MetronomeBeatDurationSeconds — one fixed value repeated — vs JitterBeatDurationsSeconds/
/// JitterBeatDashOffsetsPx — a repeating [long, short, short] cycle) — the combo/stacking/scaling
/// logic itself is identical for both, driven off this single flag.
/// </summary>
public enum StackingRhythmType
{
    None,
    Metronome,
    Jitter
}
