using System.Collections;
using UnityEngine.UIElements;

/// <summary>
/// Static, side-agnostic coroutine methods for the individual beats of a melee Beat Sequence
/// (Attack_Pattern_Directive Part 7) — Approach, Windup (Real/Fake), Return-to-origin. The Attack
/// beat's own resolution (damage/status/hit-flash) lives in BattleManager.ResolveMeleeAttackBeat
/// instead, since it needs BattleManager's existing damage-resolution machinery (DamageCalculator,
/// BattleEngine, logging, burst fill) rather than pure animation.
///
/// Every method here is a plain iterator BattleManager (a MonoBehaviour) drives via StartCoroutine —
/// exactly how it already drives BattleHUDController.RunTimedInput. Takes isPlayerSide as data
/// rather than assuming which side is "the player," so the same code serves both
/// ResolveSkillAction's (player-attacking) and ResolveEnemyDamageAction's (enemy-attacking) new
/// branches.
///
/// 2026-08-12 rework (user, live playtest: "wait isn't the lane like 7 horizontal rows?" — see
/// LaneMovementSystem's class doc comment for the full correction): lanes are vertical rows
/// (style.top), and melee Approach/Return are purely HORIZONTAL (style.left) gap-closing moves that
/// never change which row the attacker occupies.
///
/// 2026-08-12 follow-up #1 (user: "I was expecting it to move diagonally to get in front of the
/// target"): Approach ALSO tweens `top` toward the target's row when it differs from the attacker's
/// own, so the move is genuinely diagonal, not just horizontal — see RunApproach.
///
/// 2026-08-12 follow-up #2 (user: "as the phasix moves across the diagonal i was expecting the size
/// of the phasix to scale with the vertical position... on the way back, it should return to its
/// original size"): scale now tweens IN PARALLEL with `top`/`left`, over the identical duration and
/// (DOTween's shared default) easing curve — since scale and `top` are both driven by the same
/// elapsed-time curve, tweening scale from its start value to the destination row's
/// LaneMovementSystem.GetDepthScale in parallel is visually identical to continuously deriving scale
/// from the live `top` position every frame, without needing the more complex continuous-position-
/// to-scale machinery (GetDepthScaleFromLeft/TweenLeftWithDepthScale) that a PREVIOUS version of this
/// lane system had and later removed — that removal is not being reversed, this is a simpler
/// parallel-tween approach that happens to produce the same result. RunWindup reads the element's
/// LIVE current scale (not attacker.LaneIndex) as its squash baseline, so it's correct whether or not
/// an Approach ran before it and regardless of which row the attacker visually ended up at.
///
/// restingLeft (the attacker's horizontal position — its row's shared baseline plus its own in-row
/// spacing offset — before any beat runs) is captured ONCE by the caller
/// (BattleManager.ResolveMeleeBeatSequence) and threaded through as an explicit parameter, the same
/// way spacingOffset used to be: RunReturn needs to restore this EXACT value, not re-derive it from
/// wherever the closing lunge left the attacker.
/// </summary>
public static class BeatSequenceRunner
{
    /// <summary>
    /// The "closing lunge" — a single continuous tween from the attacker's current position to just
    /// short of the target's near edge, computed from the target element's real on-screen position
    /// (VisualElement.worldBound) converted into the attacker's own parent-relative coordinate
    /// space, landing edge-to-edge with a BeatSequenceConfig.MeleeContactGapPx gap. No-ops if
    /// targetElement is null (e.g. a future non-Attack-targeting Beat Sequence use).
    ///
    /// Previously a two-part method (lane-stepping loop + this lunge); the lane-stepping half was
    /// removed in the 2026-08-12 rework — see this class's own doc comment — since Approach no
    /// longer changes row at all, only ever closes the horizontal gap to the target directly.
    ///
    /// DIAGONAL when attacker and target occupy different rows (2026-08-12 follow-up, user: "on the
    /// melee it just moves horizontal within the lane then melee animation comes out... I was
    /// expecting it to move diagonally to get in front of the target"): concurrently with the
    /// horizontal closing tween, also tweens `top` to the TARGET's row position
    /// (LaneMovementSystem.GetLaneScreenTop(target.LaneIndex, ...)) — both sides share the identical
    /// row-to-`top` mapping (no mirroring, see LaneMovementSystem.GetLaneScreenTop's own doc
    /// comment), so no cross-container worldBound conversion is needed for this axis the way the
    /// horizontal one requires; the attacker's OWN row/LaneIndex is untouched by this — it's a
    /// purely visual detour up/down to line up with the target, undone by RunReturn. A no-op when
    /// both occupy the same row (the common case so far), so this is additive, not a behavior change
    /// for same-row attacks.
    ///
    /// Depth scale ALSO tweens in parallel, from the attacker's current scale to
    /// LaneMovementSystem.GetDepthScale(target.LaneIndex) — see this class's own doc comment for why
    /// a parallel start-to-end tween (rather than deriving scale from the live `top` value every
    /// frame) is sufficient: it shares the exact same duration/easing curve as the `top` tween, so
    /// the two stay in lockstep throughout.
    /// </summary>
    public static IEnumerator RunApproach(BattleParticipant attacker, int attackerSlotIndex, bool attackerIsPlayerSide, BattleParticipant target, VisualElement targetElement)
    {
        if (targetElement == null) yield break;

        VisualElement element = BattleHUDController.Instance.GetStageCreatureElement(attackerSlotIndex, attackerIsPlayerSide);
        float closingLeft = ComputeClosingLungeLocalLeft(element, targetElement, attackerIsPlayerSide);
        float closingTop = LaneMovementSystem.GetLaneScreenTop(target.LaneIndex, attackerIsPlayerSide);
        float closingScale = LaneMovementSystem.GetDepthScale(target.LaneIndex);

        VisualElementTweening.TweenLeft(element, closingLeft, BeatSequenceConfig.ClosingLungeDurationSeconds);
        VisualElementTweening.TweenTop(element, closingTop, BeatSequenceConfig.ClosingLungeDurationSeconds);
        VisualElementTweening.TweenUniformScale(element, closingScale, BeatSequenceConfig.ClosingLungeDurationSeconds);
        yield return new UnityEngine.WaitForSeconds(BeatSequenceConfig.ClosingLungeDurationSeconds);
    }

    /// <summary>
    /// Converts the target element's real screen-space position (worldBound) into a `left` value
    /// expressed in the ATTACKER's own parent-relative coordinate space, stopping short of the
    /// target's near EDGE by a fixed gap (BeatSequenceConfig.MeleeContactGapPx) — edge-to-edge, not
    /// center-to-center, so the attacker's own width is only ever accounted for once. (2026-08-12
    /// correction: an earlier center-based version subtracted only one attacker-half-width when it
    /// needed two — verified via a live `execute_code` check against real battle worldBound values:
    /// the attacker's computed resting position overlapped the target by roughly one attacker-half-
    /// width instead of leaving the intended gap. Edge-to-edge math sidesteps the whole issue —
    /// nothing here is ever a center.)
    /// `worldBound.x - resolvedStyle.left` isolates the attacker's parent's own world-space origin
    /// (stable regardless of the attacker's current `left`), which is what lets a WORLD-space target
    /// position be converted back into a LOCAL `left` value TweenLeft can animate directly — no
    /// second coordinate system, no reparenting.
    /// </summary>
    private static float ComputeClosingLungeLocalLeft(VisualElement attackerElement, VisualElement targetElement, bool attackerIsPlayerSide)
    {
        float worldToLocalOffset = attackerElement.worldBound.x - attackerElement.resolvedStyle.left;
        float attackerWidth = attackerElement.worldBound.width;

        float desiredAttackerWorldLeftEdge;
        if (attackerIsPlayerSide)
        {
            // Player approaches from screen-left; target sits to the right. The attacker's RIGHT
            // edge (left edge + its own width) should land MeleeContactGapPx before the target's
            // LEFT edge.
            float targetLeftEdge = targetElement.worldBound.x;
            desiredAttackerWorldLeftEdge = targetLeftEdge - BeatSequenceConfig.MeleeContactGapPx - attackerWidth;
        }
        else
        {
            // Enemy approaches from screen-right; target (the player) sits to the left. The
            // attacker's LEFT edge should land MeleeContactGapPx after the target's RIGHT edge.
            float targetRightEdge = targetElement.worldBound.x + targetElement.worldBound.width;
            desiredAttackerWorldLeftEdge = targetRightEdge + BeatSequenceConfig.MeleeContactGapPx;
        }

        return desiredAttackerWorldLeftEdge - worldToLocalOffset;
    }

    /// <summary>
    /// Squash-and-hold tween on the attacker's CURRENT depth-scale (read live from the element, not
    /// re-derived from attacker.LaneIndex — 2026-08-12 fix: after a diagonal Approach the element's
    /// live scale reflects the TARGET's row, not the attacker's own canonical one, so re-deriving
    /// from attacker.LaneIndex would have snapped the squash's baseline to the wrong value the
    /// instant Windup started; reading the live value is correct whether or not an Approach ran
    /// before this, and whichever row the attacker visually ended up at) times
    /// WindupSquashScaleDelta, held for the Real or Fake duration — same tween shape either way, only
    /// the held duration differs (BeatSequenceConfig.WindupRealDurationSeconds vs
    /// WindupFakeDurationSeconds), per Part 7's explicit intent that this differentiation itself is
    /// the intended player-facing tell to read.
    /// </summary>
    public static IEnumerator RunWindup(BattleParticipant attacker, int attackerSlotIndex, bool attackerIsPlayerSide, bool isFake)
    {
        VisualElement element = BattleHUDController.Instance.GetStageCreatureElement(attackerSlotIndex, attackerIsPlayerSide);
        float duration = isFake ? BeatSequenceConfig.WindupFakeDurationSeconds : BeatSequenceConfig.WindupRealDurationSeconds;
        float baseScale = ((UnityEngine.Vector3)element.resolvedStyle.scale.value).x;
        float squashedScale = baseScale * BeatSequenceConfig.WindupSquashScaleDelta;

        float half = duration / 2f;
        VisualElementTweening.TweenUniformScale(element, squashedScale, half);
        yield return new UnityEngine.WaitForSeconds(half);

        VisualElementTweening.TweenUniformScale(element, baseScale, half);
        yield return new UnityEngine.WaitForSeconds(half);
    }

    /// <summary>
    /// Automatic Return-to-origin — a visible "hop": an arc translate (up then back down) concurrent
    /// with a single continuous tween back to restingLeft/restingTop (the attacker's pre-sequence
    /// position, captured once by the caller — see this class's own doc comment), over
    /// ReturnHopDurationSeconds. restingTop only differs from the attacker's current `top` when
    /// Approach's diagonal detour (see RunApproach) moved it to line up with a target on a different
    /// row — same no-op-when-equal behavior as that method.
    ///
    /// Scale ALSO tweens back to LaneMovementSystem.GetDepthScale(attacker.LaneIndex) — the
    /// attacker's own canonical row's scale, undoing whatever RunApproach set it to (2026-08-12,
    /// user: "on the way back, it should return to its original size after its attack"). Safe to
    /// derive straight from attacker.LaneIndex here (unlike RunWindup's live-read approach) since
    /// this is specifically about restoring the ORIGINAL value, not preserving whatever the element
    /// happens to be showing right now — and attacker.LaneIndex itself is never touched by any beat.
    /// </summary>
    public static IEnumerator RunReturn(BattleParticipant attacker, int attackerSlotIndex, bool attackerIsPlayerSide, float restingLeft, float restingTop)
    {
        VisualElement element = BattleHUDController.Instance.GetStageCreatureElement(attackerSlotIndex, attackerIsPlayerSide);
        float half = BeatSequenceConfig.ReturnHopDurationSeconds / 2f;
        float restingScale = LaneMovementSystem.GetDepthScale(attacker.LaneIndex);

        // Concurrent: horizontal/row/scale return spans the full hop; the vertical arc is up-then-down across the two halves.
        VisualElementTweening.TweenLeft(element, restingLeft, BeatSequenceConfig.ReturnHopDurationSeconds);
        VisualElementTweening.TweenTop(element, restingTop, BeatSequenceConfig.ReturnHopDurationSeconds);
        VisualElementTweening.TweenUniformScale(element, restingScale, BeatSequenceConfig.ReturnHopDurationSeconds);
        VisualElementTweening.TweenTranslateY(element, BeatSequenceConfig.ReturnHopHeightPx, half);
        yield return new UnityEngine.WaitForSeconds(half);

        VisualElementTweening.TweenTranslateY(element, 0f, half);
        yield return new UnityEngine.WaitForSeconds(half);
    }
}
