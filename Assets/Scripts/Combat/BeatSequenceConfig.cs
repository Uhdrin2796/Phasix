/// <summary>
/// Placeholder timing/distance constants for the melee Beat Sequence system (Attack_Pattern_
/// Directive_v0_1_0.md Part 7, BeatSequenceRunner). Same tier/convention as TimedInputConfig —
/// centralizes pending-calibration numbers in one place rather than scattering them across every
/// hand-authored SkillData.BeatSequence asset (which only stores the ordered BeatType list itself,
/// not timing). Every value here is a reasonable placeholder — TODO: pending design — numerical
/// calibration (NumericalCalibration.md).
/// </summary>
public static class BeatSequenceConfig
{
    /// <summary>Seconds for one Approach lane-step's position tween (BeatSequenceRunner.RunApproach).</summary>
    public const float ApproachStepDurationSeconds = 0.18f;

    /// <summary>
    /// Seconds for Approach's final "closing lunge" — the cross-stage travel from Lane 1 (the
    /// attacker's own front lane) to actually being next to the target, added 2026-08-12 after live
    /// playtest feedback that Approach visibly stopped partway across the screen instead of reaching
    /// the opponent. Slower than a single lane-step since it covers much more screen distance.
    /// </summary>
    public const float ClosingLungeDurationSeconds = 0.4f;

    /// <summary>
    /// Gap (px, in screen/world space) left between the attacker's and target's edges at the end of
    /// the closing lunge — BeatSequenceRunner.ComputeClosingLungeLocalLeft adds this on top of both
    /// elements' own half-widths so the attacker stops just short of the target, not on top of it.
    /// </summary>
    public const float MeleeContactGapPx = 20f;

    /// <summary>
    /// Windup (Real) duration — held before the Attack beat resolves. Longer than WindupFake by
    /// design: Part 7 says both share the exact same tween shape and only duration differs, and
    /// that difference IS the intended player-facing tell to read.
    /// </summary>
    public const float WindupRealDurationSeconds = 0.55f;

    /// <summary>
    /// Windup (Fake) duration — shorter than WindupReal, same tween shape. Not exercised by this
    /// pass's minimal Slash example (Real-only), but exists now so the framework is provably ready
    /// for a future Fake-using skill (e.g. the directive's Shadow teleport-strike max example)
    /// without needing a second config pass later.
    /// </summary>
    public const float WindupFakeDurationSeconds = 0.30f;

    /// <summary>How much a Windup beat squashes the attacker's own depth-scale, on top of its lane's LaneMovementSystem.GetDepthScale (e.g. 0.85 = 85% of normal size at the deepest point of the squash).</summary>
    public const float WindupSquashScaleDelta = 0.85f;

    /// <summary>Seconds for the Attack beat's forward lunge tween.</summary>
    public const float AttackLungeDurationSeconds = 0.12f;

    /// <summary>Pixel offset of the Attack beat's lunge, toward the target, before snapping back.</summary>
    public const float AttackLungeOffsetPx = 24f;

    /// <summary>Seconds for the automatic Return-to-origin "hop" (Part 7: "visible, not instant").</summary>
    public const float ReturnHopDurationSeconds = 0.35f;

    /// <summary>Peak height (px, negative Y = up in VisualElement.style.translate space) of the Return hop's arc.</summary>
    public const float ReturnHopHeightPx = -30f;

    // --- 2026-08-13 follow-up: warning hop + Metronome/Jitter stacking rhythm archetypes ---
    // (user: "when the skill is selected [for] the hop to occur then after a brief delay then the
    // projectile shoots"; "Metronome... shoot the warning, then show the timing on the player...
    // Jitter will follow a similar pattern... but instead has a different beat"). All placeholder
    // values, same "pending NumericalCalibration.md" status as every other constant in this class.

    /// <summary>Seconds for the warning-hop bounce (BeatSequenceRunner.RunWarningHop) — a quick in-place vertical bounce, NOT a position change, played at the START of a ranged skill's Windup as the "something is coming" tell, distinct from the squash-based RunWindup tell.</summary>
    public const float WarningHopDurationSeconds = 0.22f;

    /// <summary>Peak height (px, negative = up) of the warning hop's bounce — smaller than ReturnHopHeightPx since it's a quick cue, not the full return journey.</summary>
    public const float WarningHopHeightPx = -16f;

    /// <summary>Metronome's fixed per-beat ring/dash duration — every beat identical, the "1..2..3..4" steady feel. 0.5s -> 0.9s -> 2.7s -> 1.8s (2026-08-13, three rounds of user feedback: "too fast" -> "triple the distance, 1/3 the ring rate" -> "distance is good but make it a bit faster" — this round only shortens duration, MetronomeDashOffsetPx is untouched).</summary>
    public const float MetronomeBeatDurationSeconds = 1.8f;

    /// <summary>Metronome's fixed per-beat dash distance (px) — constant across all beats, matching its steady timing. 28px -> 50px -> 150px (2026-08-13, first two rounds of feedback); left at 150px in the third round ("distance is good").</summary>
    public const float MetronomeDashOffsetPx = 150f;

    /// <summary>
    /// Jitter's per-beat ring/dash duration pattern — cycles through these three values by beat
    /// index ((beatIndex - 1) % 3), repeating indefinitely rather than needing a bespoke value per
    /// stack tier (user: "given 1....2.3.4 timing... after turn 5 it just repeats"). Index 0 (long)
    /// is beat 1's tell/dash-forward; indices 1-2 (short) are the quick follow-up beats. Three
    /// rounds of 2026-08-13 user feedback ("too fast" -> "triple the distance, 1/3 the ring rate" ->
    /// "distance is good but make it a bit faster") — long/short ratio kept the same throughout;
    /// the third round only shortens duration, JitterBeatDashOffsetsPx is untouched.
    /// </summary>
    public static readonly float[] JitterBeatDurationsSeconds = { 2.2f, 1.0f, 1.0f };

    /// <summary>Jitter's per-beat dash distance (px), parallel to JitterBeatDurationsSeconds — "length of dash match the offbeat timing," so the long beat gets the long dash. Left at these values in the third round of feedback ("distance is good").</summary>
    public static readonly float[] JitterBeatDashOffsetsPx = { 210f, 96f, 96f };

    /// <summary>Travel duration for a projectile that fires AFTER its ring has already resolved — Instant Strike/Feint's real strike (Attack beat, decoupled from the Windup ring per the "hop, then delay/ring, then shoot" reorder) and Metronome/Jitter's final payoff shot once every beat in the combo succeeds.</summary>
    public const float ResolvedProjectileTravelSeconds = 0.35f;

    /// <summary>Damage multiplier added per stack tier above the first (tier 1 = 1.0x, tier 2 = 1.5x, tier 3 = 2.0x, ...) — "start at low damage, then ramp."</summary>
    public const float StackingRhythmTierDamageStep = 0.5f;
}
