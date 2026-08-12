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
}
