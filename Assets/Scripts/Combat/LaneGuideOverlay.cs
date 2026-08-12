using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// TEMPORARY manual-test tool (2026-08-12, user: "add a debug button for the '\' button to
/// toggle show on and off the lane lines... they can just be white doted lines that are where the
/// perimeter of the lanes would be") — draws white dotted horizontal lines across the full Stage
/// width at each lane/row boundary (LaneMovementSystem.GetLaneScreenTop +/- LaneRowHeightPx/2),
/// so the 7-row layout can be visually checked against the actual stage art/action buttons. Pure
/// custom draw via Painter2D (UI Toolkit has no native dashed-border USS property) — SetDashPattern
/// with a short dash + a round line cap reads as a dotted line rather than a dashed one.
///
/// Added as a direct child of BattleHUDController's Stage element (a sibling of PlayerStageArea/
/// EnemyStageArea, not a child of either) so one set of lines spans both sides using a single
/// shared coordinate space — pickingMode Ignore so it never blocks clicks on the creatures/orbs
/// beneath it. Boundary Y values are computed by BattleHUDController.SetLaneGuideLinesVisible
/// (it already owns the anchor/box-height numbers needed to convert LaneMovementSystem's box-local
/// coordinates into Stage-local ones) and pushed in via SetBoundaries.
///
/// DELETE THIS FILE once the real stage art (Combat_Directive's "pending art direction" lane
/// visuals) exists — this is a debug measuring aid only.
/// </summary>
public class LaneGuideOverlay : VisualElement
{
    private static readonly Color LineColor = Color.white;
    private const float LineWidth = 3f;
    private const float DashLength = 1f;
    private const float GapLength = 12f;

    private float[] _boundaryYs = System.Array.Empty<float>();

    public LaneGuideOverlay()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0;
        style.right = 0;
        style.top = 0;
        style.bottom = 0;
        generateVisualContent += OnGenerateVisualContent;
    }

    /// <summary>Stage-local Y values (px) to draw a full-width dotted line across. Triggers a repaint.</summary>
    public void SetBoundaries(float[] boundaryYs)
    {
        _boundaryYs = boundaryYs ?? System.Array.Empty<float>();
        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (_boundaryYs.Length == 0) return;

        float width = contentRect.width;
        if (width <= 0f) return;

        Painter2D painter = mgc.painter2D;
        painter.strokeColor = LineColor;
        painter.lineWidth = LineWidth;
        painter.lineCap = LineCap.Round;
        painter.SetDashPattern(DashLength, GapLength);

        foreach (float y in _boundaryYs)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, y));
            painter.LineTo(new Vector2(width, y));
            painter.Stroke();
        }
    }
}
