using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom-drawn (Painter2D) radial nameplate gauge — replaces the old stacked HP/Aura/Burst bars
/// (2026-08-06, user-directed — see DECISIONS.md -> [Combat]: "so its cleaner and more straight
/// forward... circular one. Where its like arches around the player portrait. So half circle for
/// health over the top half, then bottom left half could be the aura and the bottom right half
/// could be the evo gauge"). Three arcs share one ring around a portrait circle: HP spans the top
/// (HPStartDeg-HPEndDeg), Aura the bottom-left quarter, Evo the bottom-right quarter. A small
/// angular gap separates each pair of neighbors — deliberately baked into the TRACK bounds
/// themselves (not just where the fill happens to stop), and drawn with butt line caps rather than
/// round, so the gap holds even when a segment is at 100% fill (round caps were found to bulge
/// past their angle and silently close the gap — see DECISIONS.md). EvoReady draws a closed gold
/// outline tracing the Evo band's entire annular-sector perimeter (outer arc, both straight side
/// cuts, inner arc) — "have the purple encased around the whole section's perimeter", not just a
/// floating arc near it.
///
/// All radii/gaps are fractions of the element's own resolvedStyle size, so the whole gauge scales
/// with whatever width/height BattleHUD.uss gives .nameplate-gauge — no separate C# constant to
/// keep in sync if the nameplate container is resized later. pickingMode is NOT set to Ignore
/// (unlike RingVisual/DragLineVisual) — clicking anywhere on the gauge is the Evolution Burst
/// activation gesture (BattleHUDController wires PointerDownEvent on the ring-wrap that contains
/// this element, see BuildNameplate), so it must remain hit-testable.
/// </summary>
public class RadialGaugeVisual : VisualElement
{
    public float HPPercent;
    public float AuraPercent;
    public float EvoPercent;
    public bool EvoReady;

    // Fractions of Mathf.Min(width, height) — see class doc comment.
    private const float ArcRadiusFraction = 0.367f;
    private const float ArcStrokeWidthFraction = 0.083f;
    private const float ReadyStrokeWidthFraction = 0.06f;

    // Angles in degrees, standard math convention (0 = 3 o'clock, increasing clockwise in this
    // y-down UI space) — matches Painter2D.Arc's HTML-canvas-style convention. A 3-degree inset at
    // each of the 3 boundaries (180, 90, 0/360) gives a 6-degree gap between every pair of
    // neighboring segments.
    private const float HPStartDeg = 183f, HPEndDeg = 357f;
    private const float AuraStartDeg = 93f, AuraEndDeg = 177f;
    private const float EvoStartDeg = 3f, EvoEndDeg = 87f;

    private static readonly Color TrackHPColor = new Color32(50, 50, 56, 255);
    private static readonly Color TrackAuraColor = new Color32(40, 40, 52, 255);
    private static readonly Color TrackEvoColor = new Color32(40, 40, 52, 255);
    private static readonly Color FillHPColor = new Color32(90, 200, 100, 255);
    private static readonly Color FillAuraColor = new Color32(90, 140, 230, 255);
    private static readonly Color FillEvoColor = new Color32(150, 90, 220, 255);
    private static readonly Color ReadyColor = new Color32(230, 210, 60, 255);

    public RadialGaugeVisual()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    /// <summary>Call after changing any field to redraw.</summary>
    public void Refresh() => MarkDirtyRepaint();

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        float w = resolvedStyle.width;
        float h = resolvedStyle.height;
        if (w <= 0f || h <= 0f) return; // not laid out yet this frame

        Vector2 center = new Vector2(w / 2f, h / 2f);
        float size = Mathf.Min(w, h);
        float arcRadius = size * ArcRadiusFraction;
        float strokeWidth = size * ArcStrokeWidthFraction;

        Painter2D painter = mgc.painter2D;

        DrawArc(painter, center, arcRadius, strokeWidth, HPStartDeg, HPEndDeg, TrackHPColor);
        DrawArc(painter, center, arcRadius, strokeWidth, AuraStartDeg, AuraEndDeg, TrackAuraColor);
        DrawArc(painter, center, arcRadius, strokeWidth, EvoStartDeg, EvoEndDeg, TrackEvoColor);

        DrawArc(painter, center, arcRadius, strokeWidth, HPStartDeg, LerpAngle(HPStartDeg, HPEndDeg, HPPercent), FillHPColor);
        DrawArc(painter, center, arcRadius, strokeWidth, AuraStartDeg, LerpAngle(AuraStartDeg, AuraEndDeg, AuraPercent), FillAuraColor);
        DrawArc(painter, center, arcRadius, strokeWidth, EvoStartDeg, LerpAngle(EvoStartDeg, EvoEndDeg, EvoPercent), FillEvoColor);

        if (EvoReady)
        {
            float readyStroke = Mathf.Max(1.5f, size * ReadyStrokeWidthFraction);
            DrawReadyOutline(painter, center, arcRadius, strokeWidth, readyStroke, EvoStartDeg, EvoEndDeg, ReadyColor);
        }
    }

    private static float LerpAngle(float startDeg, float endDeg, float percent) =>
        startDeg + (endDeg - startDeg) * Mathf.Clamp01(percent / 100f);

    private static void DrawArc(Painter2D painter, Vector2 center, float radius, float strokeWidth, float startDeg, float endDeg, Color color)
    {
        if (endDeg <= startDeg) return;
        painter.strokeColor = color;
        painter.lineWidth = strokeWidth;
        painter.lineCap = LineCap.Butt;
        painter.BeginPath();
        painter.Arc(center, radius, Angle.Degrees(startDeg), Angle.Degrees(endDeg));
        painter.Stroke();
    }

    /// <summary>
    /// Traces a CLOSED loop around the arc band's full annular-sector perimeter (outer arc, far
    /// side cut, inner arc, near side cut) — a border fully encasing the segment's own shape,
    /// rather than a single arc floating outside it.
    /// </summary>
    private static void DrawReadyOutline(Painter2D painter, Vector2 center, float arcRadius, float arcStrokeWidth, float readyStrokeWidth, float startDeg, float endDeg, Color color)
    {
        float outerR = arcRadius + arcStrokeWidth / 2f;
        float innerR = arcRadius - arcStrokeWidth / 2f;
        Vector2 outerStart = PolarPoint(center, outerR, startDeg);
        Vector2 innerEnd = PolarPoint(center, innerR, endDeg);

        painter.strokeColor = color;
        painter.lineWidth = readyStrokeWidth;
        painter.lineJoin = LineJoin.Round;
        painter.BeginPath();
        painter.MoveTo(outerStart);
        painter.Arc(center, outerR, Angle.Degrees(startDeg), Angle.Degrees(endDeg));
        painter.LineTo(innerEnd);
        painter.Arc(center, innerR, Angle.Degrees(endDeg), Angle.Degrees(startDeg), ArcDirection.CounterClockwise);
        painter.ClosePath();
        painter.Stroke();
    }

    private static Vector2 PolarPoint(Vector2 center, float radius, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(center.x + radius * Mathf.Cos(rad), center.y + radius * Mathf.Sin(rad));
    }
}
