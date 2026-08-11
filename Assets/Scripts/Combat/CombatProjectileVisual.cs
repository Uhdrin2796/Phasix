using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom-drawn (Painter2D) placeholder projectile — a filled, outlined diamond that travels from
/// Start to End as SetProgress advances 0-&gt;1, and can fade out via SetAlpha (2026-08-11, Dodge
/// vanish). Patterned directly off DragLineVisual (absolute-positioned over its parent,
/// pickingMode.Ignore, Painter2D content generation, Stage-local Start/End set via WorldToLocal by
/// the owning controller) — the same established VisualElement-drawing convention in this
/// codebase, not a new one.
///
/// Radius bumped from the original 8px placeholder to 18px (2026-08-11, user-directed: "make the
/// attack blob bigger and more noticeable") plus a white outline stroke for contrast against
/// varied backgrounds — still deliberately simple geometry, no sprite art exists yet.
/// Radius is public/const so CombatVfxController can factor it into edge-to-edge arrival-timing
/// math without needing a live instance.
/// </summary>
public class CombatProjectileVisual : VisualElement
{
    public const float Radius = 18f;

    private static readonly Color OutlineColor = new Color(1f, 1f, 1f, 0.9f);
    private const float OutlineWidth = 3f;

    public Vector2 Start;
    public Vector2 End;
    public Color Tint = Color.white;

    private float _progress;
    private float _alpha = 1f;
    private float _pulseScale = 1f;

    public CombatProjectileVisual()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        generateVisualContent += OnGenerateVisualContent;
    }

    /// <summary>0 = at Start, 1 = at End. Call, then the element repaints on the next frame.</summary>
    public void SetProgress(float progress)
    {
        _progress = Mathf.Clamp01(progress);
        MarkDirtyRepaint();
    }

    /// <summary>Fade multiplier (1 = fully opaque, 0 = invisible) — drives the Dodge vanish-fade. Call, then the element repaints on the next frame.</summary>
    public void SetAlpha(float alpha)
    {
        _alpha = Mathf.Clamp01(alpha);
        MarkDirtyRepaint();
    }

    /// <summary>
    /// Multiplies the drawn radius (1 = normal size) — drives the held-and-waiting idle pulse
    /// (2026-08-11, user-directed fix: a projectile parked motionless at its target while
    /// RunDefenseTimedInput's real outcome is still pending read as "stuck/broken," not
    /// intentional). Call, then the element repaints on the next frame.
    /// </summary>
    public void SetPulseScale(float scale)
    {
        _pulseScale = Mathf.Max(0f, scale);
        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Vector2 current = Vector2.Lerp(Start, End, _progress);
        float radius = Radius * _pulseScale;

        Color fill = Tint;
        fill.a *= _alpha;
        Color outline = OutlineColor;
        outline.a *= _alpha;

        Painter2D painter = mgc.painter2D;
        painter.fillColor = fill;
        painter.strokeColor = outline;
        painter.lineWidth = OutlineWidth;
        painter.BeginPath();
        painter.MoveTo(current + new Vector2(0f, -radius));
        painter.LineTo(current + new Vector2(radius, 0f));
        painter.LineTo(current + new Vector2(0f, radius));
        painter.LineTo(current + new Vector2(-radius, 0f));
        painter.ClosePath();
        painter.Fill();
        painter.Stroke();
    }
}
