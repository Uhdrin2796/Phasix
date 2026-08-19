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

    // --- 2026-08-14: Multi-Hit Volley (Attack_Pattern_Directive Part 5 Group 2's first archetype)
    // (user: "one warning for player, then dash shoot, return to position, dash shoot, return to
    // position, dash shoot, etc. But this would happen fast bc the number of projectile should be
    // coming out in quick succession to feel like a volley"). All placeholder values, same "pending
    // NumericalCalibration.md" status as every other constant in this class.

    /// <summary>Seconds for ONE leg (forward or back) of a Volley hit's dash — much smaller than Metronome/Jitter's dash beats since this is "a small forward dash," not a full rhythm beat. A full forward+back cycle is 2x this value.</summary>
    public const float VolleyDashLegDurationSeconds = 0.10f;

    /// <summary>Pixel offset of a Volley hit's forward dash — small, per user: "small forward dash."</summary>
    public const float VolleyDashOffsetPx = 20f;

    /// <summary>Fallback ring sweep duration for a Volley hit whose skill-authored SkillData.VolleyRingDurationsSeconds array is shorter than its VolleyRingSequence — defensive only, every real asset should author a full-length array.</summary>
    public const float VolleyDefaultRingDurationSeconds = 0.45f;

    /// <summary>
    /// Extra per-hit damage multiplier applied on top of the normal Miss/Good/Perfect multiplier
    /// (2026-08-15, user: "lower the damage for the volley") — each Volley hit computes full damage
    /// from BattleConfig.PlaceholderSkillPower same as any other skill, but a full 8-hit cast landing
    /// every ring would otherwise deal roughly 8x a single normal attack's damage, since nothing
    /// scaled per-hit output down to account for there being 8 independent hits instead of 1. At
    /// 0.3, an all-Good 8-hit connect totals ~2.4x a normal single hit (a strong payoff for landing
    /// the whole sequence) rather than 8x; an all-Miss cast totals ~1.2x rather than ~4x. Placeholder,
    /// pending NumericalCalibration.md like every other value in this class.
    /// </summary>
    public const float VolleyPerHitDamageMultiplier = 0.3f;

    // --- 2026-08-17: Charge & Release + Sustained Pressure (Attack_Pattern_Directive Part 5 Group
    // 2's second/third archetypes — "build these two together: both are 'hold input' instead of
    // 'tap input,' diverging only in scoring... Share one new hold-input primitive.") All placeholder
    // values, same "pending NumericalCalibration.md" status as every other constant in this class.

    /// <summary>Safety-valve timeout for BattleHUDController.RunHoldGesture — if the player never releases at all, the hold loop force-resolves as if released so a forgotten hold can't stall the battle coroutine forever.</summary>
    public const float HoldInputMaxTimeoutSeconds = 3.0f;

    /// <summary>Fallback gap (seconds) between the warning hop and Charge & Release's ideal PRESS instant, when a skill's own SkillData.ChargeReleaseTellSeconds is 0. Also drives the outer triangle's pre-press convergence sweep duration (BattleHUDController.RunHoldGesture), so this doubles as a visual-pacing knob. 0.5 -> 0.8 (2026-08-17 follow-up, user: "the timing on the converging is a little too fast").</summary>
    public const float ChargeReleaseDefaultTellSeconds = 0.8f;

    /// <summary>Fallback target hold duration (seconds) for Charge & Release's ideal RELEASE instant, when a skill's own SkillData.ChargeReleaseTargetHoldSeconds is 0 — "long obvious windup" per the archetype's own Part 5 one-liner.</summary>
    public const float ChargeReleaseDefaultTargetHoldSeconds = 1.2f;

    /// <summary>Tolerance window (seconds, absolute deviation) for Charge & Release's PRESS-instant quality — same absolute-seconds shape as Sustained Pressure's own press/release tolerances, since it's judged against a real-time tell instant rather than a ring-radius ratio.</summary>
    public const float ChargeReleasePressToleranceSeconds = 0.35f;

    /// <summary>Tolerance (deviation RATIO, not seconds) for Charge & Release's RELEASE-instant quality — same |heldDuration/targetHoldSeconds - 1| shape every other ring in this codebase already uses, since it's judging a held duration against a target duration, not an absolute wall-clock instant.</summary>
    public const float ChargeReleaseReleaseToleranceRatio = 0.35f;

    /// <summary>Fallback gap (seconds) between the warning hop and Sustained Pressure's ideal PRESS instant, when a skill's own SkillData.SustainedPressureTellSeconds is 0. Also drives the outer triangle's pre-press convergence sweep duration (BattleHUDController.RunHoldGesture), so this doubles as a visual-pacing knob. 0.5 -> 0.8 (2026-08-17 follow-up, user: "the timing on the converging is a little too fast") — matches ChargeReleaseDefaultTellSeconds's identical change, same reasoning.</summary>
    public const float SustainedPressureDefaultTellSeconds = 0.8f;

    /// <summary>Fallback attack duration (seconds) for Sustained Pressure, when a skill's own SkillData.SustainedPressureHoldSeconds is 0 — deliberately longer than Charge & Release's own default, "hold-to-guard, boss-scale feel" per the Flame Breath worked example.</summary>
    public const float SustainedPressureDefaultHoldSeconds = 1.5f;

    /// <summary>Tolerance window (seconds, absolute deviation) for Sustained Pressure's PRESS-instant quality.</summary>
    public const float SustainedPressurePressToleranceSeconds = 0.35f;

    /// <summary>Tolerance window (seconds, absolute deviation) for Sustained Pressure's RELEASE-instant quality.</summary>
    public const float SustainedPressureReleaseToleranceSeconds = 0.35f;

    /// <summary>Hard cap on Guard's block fraction — even a flawless double-perfect Guard blocks at most this much, never the full 100% Dodge/Parry avoidance grants, keeping Guard a genuinely graduated third outcome rather than a renamed Parry.</summary>
    public const float SustainedPressureMaxBlockFraction = 0.8f;

    /// <summary>The (blockFraction / SustainedPressureMaxBlockFraction) ratio above which a Guard counts as "perfect" for BattleHUDController.LastDefenseWasPerfect — feeds the same perfect-aura-restore path Dodge/Parry's own Perfect already grants.</summary>
    public const float SustainedPressurePerfectQualityThreshold = 0.9f;
}
