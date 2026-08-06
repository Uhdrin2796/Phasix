using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom-drawn (Painter2D) straight line from the acting player's stage creature to wherever the
/// player is currently dragging, for click-and-drag target selection (2026-08-05, user-directed,
/// Sonny 2-referenced — see DECISIONS.md -> [Combat]): click-drag the "Attack" placeholder onto
/// an enemy to both pick the move and choose the target in one gesture. Sized to cover the whole
/// Stage area (its parent) so Start/End can be set directly in Stage-local coordinates. pickingMode
/// is Ignore — purely decorative, must never intercept the drag's own pointer events.
/// </summary>
public class DragLineVisual : VisualElement
{
    public Vector2 Start;
    public Vector2 End;

    private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.85f);

    public DragLineVisual()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        generateVisualContent += OnGenerateVisualContent;
    }

    /// <summary>Call after changing Start/End to redraw.</summary>
    public void Refresh() => MarkDirtyRepaint();

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Painter2D painter = mgc.painter2D;
        painter.strokeColor = LineColor;
        painter.lineWidth = 4f;
        painter.BeginPath();
        painter.MoveTo(Start);
        painter.LineTo(End);
        painter.Stroke();
    }
}
