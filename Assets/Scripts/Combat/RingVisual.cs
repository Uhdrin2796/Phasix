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

    /// <summary>
    /// Charge & Release / Sustained Pressure only (2026-08-17) — false (default) leaves every other
    /// ring type (classic tap rings, Volley's circle/square) unchanged. true draws the TARGET and
    /// MARKER rings as triangles instead — a third shape, distinct from both existing ones, used for
    /// the new hold-input archetypes. Unlike MarkerIsSquare, the triangle marker is STATIC once set
    /// (the caller never animates MarkerRadius for this archetype family) — see FillRadius below for
    /// the piece that actually animates over a hold.
    /// </summary>
    public bool MarkerIsTriangle;

    /// <summary>
    /// Charge & Release / Sustained Pressure only (2026-08-17, user: "the press and hold should have
    /// the triangle fill from the center going outward and match the target ring for the release") —
    /// 0 (default, drawn nothing) for every other ring type. When MarkerIsTriangle is also true and
    /// this is &gt; 0, a SOLID filled triangle is drawn at this radius, growing from the center outward
    /// as the caller increases it over the hold — a second, independent visual on top of the static
    /// MarkerRadius/TargetRadius outlines above, not a replacement for either.
    /// </summary>
    public float FillRadius;

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
        if (MarkerIsTriangle)
        {
            DrawTriangleStroke(painter, center, TargetRadius, 2f, TargetColor);
            DrawTriangleStroke(painter, center, MarkerRadius, 3f, MarkerColor);
            if (FillRadius > 0f) DrawFilledTriangle(painter, center, FillRadius, MarkerColor);
        }
        else if (MarkerIsSquare)
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

    /// <summary>Three vertices (90/210/330 degrees, point-up) of an equilateral triangle inscribed in a circle of the given radius, same "same visual scale as a circle of that radius" convention DrawSquareStroke already uses. Y-down (UI Toolkit) space, same angle convention as CompassPoint's ComputeCompassOffset.</summary>
    private static (Vector2 top, Vector2 bottomRight, Vector2 bottomLeft) ComputeTriangleVertices(Vector2 center, float radius)
    {
        Vector2 top = center + new Vector2(Mathf.Cos(90f * Mathf.Deg2Rad), -Mathf.Sin(90f * Mathf.Deg2Rad)) * radius;
        Vector2 bottomRight = center + new Vector2(Mathf.Cos(330f * Mathf.Deg2Rad), -Mathf.Sin(330f * Mathf.Deg2Rad)) * radius;
        Vector2 bottomLeft = center + new Vector2(Mathf.Cos(210f * Mathf.Deg2Rad), -Mathf.Sin(210f * Mathf.Deg2Rad)) * radius;
        return (top, bottomRight, bottomLeft);
    }

    /// <summary>Same stroke shape as DrawStroke/DrawSquareStroke above, but a point-up triangle inscribed in radius instead of a circle or square.</summary>
    private static void DrawTriangleStroke(Painter2D painter, Vector2 center, float radius, float lineWidth, Color color)
    {
        if (radius <= 0f || lineWidth <= 0f) return;
        var (top, bottomRight, bottomLeft) = ComputeTriangleVertices(center, radius);
        painter.strokeColor = color;
        painter.lineWidth = lineWidth;
        painter.BeginPath();
        painter.MoveTo(top);
        painter.LineTo(bottomRight);
        painter.LineTo(bottomLeft);
        painter.ClosePath();
        painter.Stroke();
    }

    /// <summary>Solid-filled version of DrawTriangleStroke, used for FillRadius's center-out charge visual — no lineWidth, Fill() instead of Stroke().</summary>
    private static void DrawFilledTriangle(Painter2D painter, Vector2 center, float radius, Color color)
    {
        if (radius <= 0f) return;
        var (top, bottomRight, bottomLeft) = ComputeTriangleVertices(center, radius);
        painter.fillColor = color;
        painter.BeginPath();
        painter.MoveTo(top);
        painter.LineTo(bottomRight);
        painter.LineTo(bottomLeft);
        painter.ClosePath();
        painter.Fill();
    }
}
