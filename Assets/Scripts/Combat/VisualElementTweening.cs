using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// DOTween wrappers for VisualElement.style properties, used by BeatSequenceRunner for the melee
/// Beat Sequence animations (Attack_Pattern_Directive Part 7). The battle stage is 100% UI Toolkit
/// (no Transform/SpriteRenderer to tween), so these use DOTween's CORE generic
/// DOTween.To(getter, setter, endValue, duration) overload rather than the Transform-based
/// .DOMove()/.DOScale() shortcuts.
///
/// DOTween DOES ship an official UI Toolkit module with VisualElement extension methods
/// (Assets/Plugins/Demigiant/DOTween/Modules/DOTweenModuleUIToolkit.cs — DOMove/DOScale/DORotate),
/// gated behind the DOTWEEN_UITOOLKIT scripting define. At the time these wrappers were first
/// written it was NOT enabled (ProjectSettings.asset's scriptingDefineSymbols was empty), so they
/// were built on DOTween's always-available core API instead. Later the same session,
/// ProjectSettings.asset picked up `DOTWEEN;DOTWEEN_UITOOLKIT` for every platform (cause not fully
/// traced — likely the DOTween package's own first-import setup routine, not a manual action taken
/// here) — the official module is now actually active and coexists fine with these wrappers (no
/// naming conflict, different method names). Left as-is rather than refactored mid-session; the
/// official DOMove/DOScale/DORotate shortcuts would be a reasonable one-for-one replacement for a
/// future cleanup pass, now that they're confirmed available.
/// </summary>
public static class VisualElementTweening
{
    /// <summary>Tweens VisualElement.style.left (the real lane position, not a flourish offset) to endLeftPx.</summary>
    public static Tween TweenLeft(VisualElement element, float endLeftPx, float duration)
    {
        return DOTween.To(
            () => element.resolvedStyle.left,
            v => element.style.left = v,
            endLeftPx, duration);
    }

    /// <summary>
    /// Tweens VisualElement.style.top (the row/depth position) to endTopPx — added 2026-08-12 so a
    /// melee Approach can move DIAGONALLY when the target occupies a different row (user: "I was
    /// expecting it to move diagonally to get in front of the target then the melee comes out"),
    /// run concurrently with TweenLeft rather than as a separate sequential step.
    /// </summary>
    public static Tween TweenTop(VisualElement element, float endTopPx, float duration)
    {
        return DOTween.To(
            () => element.resolvedStyle.top,
            v => element.style.top = v,
            endTopPx, duration);
    }

    /// <summary>Tweens VisualElement.style.scale uniformly (x=y=z) to endScale — callers compose with LaneMovementSystem.GetDepthScale themselves (e.g. squash = baseDepthScale * WindupSquashScaleDelta); this wrapper just animates toward whatever absolute value it's given.</summary>
    public static Tween TweenUniformScale(VisualElement element, float endScale, float duration)
    {
        return DOTween.To(
            () => ((Vector3)element.resolvedStyle.scale.value).x,
            v => element.style.scale = new Scale(new Vector3(v, v, 1f)),
            endScale, duration);
    }

    /// <summary>
    /// Tweens the Y component of VisualElement.style.translate to endY, preserving whatever X the
    /// element already had (e.g. ApplyLaneLayout's in-lane occupancy spacing baseline) — captured
    /// once at tween start, not re-read every frame.
    /// </summary>
    public static Tween TweenTranslateY(VisualElement element, float endY, float duration)
    {
        float fixedX = ((Vector3)element.resolvedStyle.translate).x;
        return DOTween.To(
            () => ((Vector3)element.resolvedStyle.translate).y,
            y => element.style.translate = new Translate(fixedX, y),
            endY, duration);
    }

    /// <summary>Per-element tracking for TweenTranslateX's own force-complete-before-restart guard — see that method's doc comment. Not used by any other wrapper here; scoped narrowly so killing a stale translateX tween can never reach in and disturb an unrelated concurrent scale/hop/position tween on the same element.</summary>
    private static readonly Dictionary<VisualElement, Tween> _activeTranslateXTweens = new Dictionary<VisualElement, Tween>();

    /// <summary>
    /// Tweens the X component of VisualElement.style.translate to endX, preserving whatever Y the
    /// element already had — the purely-cosmetic counterpart to TweenLeft (2026-08-13, user:
    /// "the dash should just be a visual thing... should not actually change the players position").
    /// Used by BeatSequenceRunner.RunRhythmDash so a Metronome/Jitter beat's dash-forward/dash-back
    /// never touches style.left (the real, authoritative lane position) at all — it's a `transform`-
    /// level offset layered on top.
    ///
    /// 2026-08-14 fix (user: "for metronome one turn 1 and 3 its not returning back to its original
    /// position. its shifting to the next slot over") — none of these wrapper methods ever called
    /// DOTween's SetTarget, so DOTween has no way to recognize two TweenTranslateX calls on the same
    /// element as "the same logical animation" and won't auto-kill the older one; a beat's own dash
    /// tween (started fire-and-forget, awaited only via a separately-timed ring/WaitForSeconds, not
    /// the tween's own completion) and ResolveStackingRhythmAttack's final reset-to-0 tween could
    /// both end up actively driving translateX at once, each frame's write racing the other's —
    /// landing wherever they happened to disagree last, not exactly 0. Now force-completes (jumps
    /// straight to its own end value, `SetTarget`-style semantics without touching unrelated tweens
    /// on the same element) any translateX tween still active on this exact element before starting
    /// a new one, so every call is guaranteed to start from a known, settled value — no race possible
    /// regardless of timing.
    /// </summary>
    public static Tween TweenTranslateX(VisualElement element, float endX, float duration)
    {
        if (_activeTranslateXTweens.TryGetValue(element, out Tween existing) && existing.IsActive())
            existing.Complete(true);

        float fixedY = ((Vector3)element.resolvedStyle.translate).y;
        Tween tween = DOTween.To(
            () => ((Vector3)element.resolvedStyle.translate).x,
            x => element.style.translate = new Translate(x, fixedY),
            endX, duration);
        _activeTranslateXTweens[element] = tween;
        return tween;
    }
}
