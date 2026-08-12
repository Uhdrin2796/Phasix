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
}
