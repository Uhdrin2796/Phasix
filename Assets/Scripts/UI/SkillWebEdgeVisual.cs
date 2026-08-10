using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Painter2D overlay for the skill web view (2026-08 — see OverworldMenuController.BuildSkillArea
/// and DECISIONS.md -> [UI]) — draws the within-tree connector line and discovered-node glow for
/// every column, in one pass, sitting behind the real VisualElement nodes as a sibling inside the
/// same pannable/zoomable "world" container. Same local convention as
/// Assets/Scripts/Combat/DragLineVisual.cs: pickingMode Ignore (never intercepts pan/zoom or node
/// pointer events), generateVisualContent + MarkDirtyRepaint() to redraw on demand rather than
/// every frame.
///
/// Node positions are passed in the SAME local coordinate space as the sibling node
/// VisualElements' own style.left/top (both are direct children of the world container), so no
/// separate coordinate mapping is needed here — callers just hand over what they already used to
/// place the nodes.
/// </summary>
public class SkillWebEdgeVisual : VisualElement
{
    /// <summary>One skill tree's column: its nodes' local centers (top to bottom, index order), its accent color, and whether it's currently unlocked (Discovered) vs. tier-locked (Sighted).</summary>
    public struct ColumnEdges
    {
        public Vector2[] NodeCenters;
        public Color TreeColor;
        public bool Unlocked;
    }

    private static readonly Color LockedLineColor = new Color(0.35f, 0.35f, 0.4f, 0.4f);
    private const float GlowRadius = 22f;

    public List<ColumnEdges> Columns = new List<ColumnEdges>();

    public SkillWebEdgeVisual()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        generateVisualContent += OnGenerateVisualContent;
    }

    /// <summary>Call after replacing Columns to redraw.</summary>
    public void Refresh() => MarkDirtyRepaint();

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Painter2D painter = mgc.painter2D;

        // Connectors first (drawn under the glow/nodes).
        foreach (ColumnEdges column in Columns)
        {
            if (column.NodeCenters == null || column.NodeCenters.Length < 2) continue;

            painter.strokeColor = column.Unlocked ? column.TreeColor : LockedLineColor;
            painter.lineWidth = column.Unlocked ? 2.5f : 1f;

            for (int i = 0; i < column.NodeCenters.Length - 1; i++)
            {
                painter.BeginPath();
                painter.MoveTo(column.NodeCenters[i]);
                painter.LineTo(column.NodeCenters[i + 1]);
                painter.Stroke();
            }
        }

        // Discovered-node glow — locked (Sighted) columns get no glow, matching the mockup's
        // silhouette state.
        foreach (ColumnEdges column in Columns)
        {
            if (!column.Unlocked || column.NodeCenters == null) continue;

            Color glowStart = new Color(column.TreeColor.r, column.TreeColor.g, column.TreeColor.b, 0.35f);
            Color glowEnd = new Color(column.TreeColor.r, column.TreeColor.g, column.TreeColor.b, 0f);

            foreach (Vector2 center in column.NodeCenters)
            {
                painter.fillGradient = FillGradient.MakeRadialGradient(glowStart, glowEnd, center, GlowRadius, center);
                painter.BeginPath();
                painter.Arc(center, GlowRadius, new Angle(0f), new Angle(360f));
                painter.Fill();
            }
        }
    }
}
