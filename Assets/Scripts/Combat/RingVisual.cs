using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom-drawn (Painter2D) circular timing visual — shared by both RunTimedInput (offense, on
/// the targeted enemy) and RunDefenseTimedInput (defense, on the defending player creature).
/// Reworked 2026-08-05 (user-directed — see DECISIONS.md -> [Combat]) from "two nested zone
/// bands + a marker" to a fixed reference TargetRing plus a single converging MarkerRing that
/// starts wider than the target and shrinks past it: success is judged by the marker/target
/// RADIUS RATIO at click time (e.g. Dodge succeeds within 0.75-1.25x the target, Parry within a
/// tighter 0.9-1.1x — see TimedInputConfig's tolerance constants), not by a pre-drawn zone the
/// player can see ahead of time. pickingMode is Ignore — purely decorative; click detection lives
/// on the HUD root, this element must never intercept a pointer event.
/// </summary>
public class RingVisual : VisualElement
{
    /// <summary>Fixed reference ring — what the shrinking MarkerRadius is trying to match.</summary>
    public float TargetRadius;

    /// <summary>The converging ring — starts larger than TargetRadius and shrinks over the sweep.</summary>
    public float MarkerRadius;

    /// <summary>White by default ("the white moving ring"); the caller flashes it orange/green on a successful Dodge/Parry/offense hit.</summary>
    public Color MarkerColor = Color.white;

    /// <summary>
    /// Multi-Hit Volley only (2026-08-15, user: "lets do circle like we've done for left click, the
    /// square for right click") — false (default) draws both strokes as circles, unchanged for
    /// every non-Volley caller (RunTimedInput/RunDefenseTimedInput never touch this field). true
    /// draws both as squares instead, replacing the earlier converging/expanding-DIRECTION encoding
    /// (which needed watching motion over a couple frames to read) with a SHAPE difference readable
    /// from one glance. Applies to both the static target ring and the moving marker so the whole
    /// prompt reads as one consistent shape, not a mismatched circle/square pair.
    /// </summary>
    public bool MarkerIsSquare;

    private static readonly Color TargetColor = new Color(1f, 1f, 1f, 0.45f);

    public RingVisual()
    {
        pickingMode = PickingMode.Ignore;
        generateVisualContent += OnGenerateVisualContent;
    }

    /// <summary>Call after changing any field to redraw.</summary>
    public void Refresh() => MarkDirtyRepaint();

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Vector2 center = new Vector2(resolvedStyle.width / 2f, resolvedStyle.height / 2f);
        if (center.x <= 0f || center.y <= 0f) return; // not laid out yet this frame

        Painter2D painter = mgc.painter2D;
        if (MarkerIsSquare)
        {
            DrawSquareStroke(painter, center, TargetRadius, 2f, TargetColor);
            DrawSquareStroke(painter, center, MarkerRadius, 3f, MarkerColor);
        }
        else
        {
            DrawStroke(painter, center, TargetRadius, 2f, TargetColor);
            DrawStroke(painter, center, MarkerRadius, 3f, MarkerColor);
        }
    }

    private static void DrawStroke(Painter2D painter, Vector2 center, float radius, float lineWidth, Color color)
    {
        if (radius <= 0f || lineWidth <= 0f) return;
        painter.strokeColor = color;
        painter.lineWidth = lineWidth;
        painter.BeginPath();
        painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
        painter.Stroke();
    }

    /// <summary>Same stroke shape as DrawStroke above, but a square (half-width == radius, so it reads as roughly the same visual scale as a circle of that radius) instead of a circle.</summary>
    private static void DrawSquareStroke(Painter2D painter, Vector2 center, float radius, float lineWidth, Color color)
    {
        if (radius <= 0f || lineWidth <= 0f) return;
        painter.strokeColor = color;
        painter.lineWidth = lineWidth;
        painter.BeginPath();
        painter.MoveTo(new Vector2(center.x - radius, center.y - radius));
        painter.LineTo(new Vector2(center.x + radius, center.y - radius));
        painter.LineTo(new Vector2(center.x + radius, center.y + radius));
        painter.LineTo(new Vector2(center.x - radius, center.y + radius));
        painter.ClosePath();
        painter.Stroke();
    }
}
