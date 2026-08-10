using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Shared runtime hover tooltip, extracted from BattleHUDController (2026-08 session, see
/// DECISIONS.md -> [UI]) once the overworld Party menu needed the identical behavior for its own
/// skill-ring orbs. UI Toolkit's native VisualElement.tooltip only renders inside Editor-hosted UI
/// (Inspector/EditorWindow panels) and is silently a no-op for a runtime UIDocument panel, in Play
/// Mode or a real build alike — this is a plain floating Label shown/hidden on PointerEnter/
/// PointerLeave instead.
///
/// Positioned relative to the HOVERED ELEMENT'S own bounds, not the live cursor position (an
/// earlier pass tried cursor-following; user feedback was "expecting it be near/next to the [thing]
/// we're hovering") — since the anchor doesn't move while hovered, position is computed once on
/// Enter, no PointerMove tracking needed.
///
/// 2026-08-09 follow-up — user: "the text when hovering over the enemy HP, aura etc appears out of
/// screen and should be on the left side." PositionNear previously always placed the tooltip to
/// the anchor's right with zero screen-edge awareness — harmless for player-side anchors (left
/// half of the screen) but the enemy nameplate sits at the panel's right edge
/// (BattleHUD.uss .status-list-enemy), so its bars' tooltips always overflowed. Now flips to the
/// LEFT of the anchor whenever placing it on the right would exceed the panel's width, and clamps
/// vertically so it can't run off the top or bottom either.
///
/// 2026-08-09 follow-up #2 — user: "the placement of the hover for the enemy is a little far from
/// the left side." The initial flip used the USS .hud-tooltip max-width (220px) as a pre-layout
/// placement estimate (real rendered width isn't known until AFTER Show() sets the text and a
/// layout pass runs), which left a large, visible gap for short text (e.g. "HP: 110/110" renders
/// far narrower than 220px). Show() now re-snaps the tooltip once real layout resolves, via a
/// one-shot GeometryChangedEvent — the 220px estimate is still used for the very first frame (so
/// the tooltip has A position immediately, before any layout pass), but gets corrected to the
/// actual rendered size a frame later, snug against the anchor either way.
///
/// One instance per screen (BattleHUDController and OverworldMenuController each own their own,
/// not a cross-screen singleton) — only one element can be hovered at a time on a given screen, so
/// a single shared Label per screen is enough.
/// </summary>
public class HudTooltip
{
    private const float AnchorGap = 8f;

    // Matches .hud-tooltip's USS max-width (BattleHUD.uss) — used only for the very first,
    // pre-layout placement guess (see class doc comment); OnLabelGeometryChanged corrects it to
    // the real rendered size a frame later.
    private const float AssumedMaxWidth = 220f;
    private const float AssumedMaxHeight = 120f;

    private readonly Label _label;
    private readonly VisualElement _root;
    private VisualElement _pendingAnchor;

    /// <summary>Creates the tooltip's Label and adds it as the LAST child of root, so it always paints on top of every other root child (UI Toolkit's document-order paint rule).</summary>
    public HudTooltip(VisualElement root)
    {
        _root = root;
        _label = new Label { pickingMode = PickingMode.Ignore, style = { display = DisplayStyle.None } };
        _label.AddToClassList("hud-tooltip");
        _root.Add(_label);
    }

    public void Hide()
    {
        _label.UnregisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged);
        _pendingAnchor = null;
        _label.style.display = DisplayStyle.None;
    }

    public void Show(string text, VisualElement anchor)
    {
        _label.UnregisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged); // cancel any reposition still pending from a previous Show()
        _label.text = text;
        _label.style.display = DisplayStyle.Flex;
        PositionNear(anchor, AssumedMaxWidth, AssumedMaxHeight);

        // Re-snap once real layout is known (see class doc comment) — the assumed size above is
        // usually much wider/taller than short tooltip text actually renders at.
        _pendingAnchor = anchor;
        _label.RegisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged);
    }

    private void OnLabelGeometryChanged(GeometryChangedEvent evt)
    {
        _label.UnregisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged);
        if (_pendingAnchor == null) return;
        PositionNear(_pendingAnchor, _label.resolvedStyle.width, _label.resolvedStyle.height);
    }

    private void PositionNear(VisualElement anchor, float tooltipWidth, float tooltipHeight)
    {
        var localRect = _root.WorldToLocal(anchor.worldBound);
        float panelWidth = _root.resolvedStyle.width;
        float panelHeight = _root.resolvedStyle.height;

        float rightPlacement = localRect.xMax + AnchorGap;
        bool panelWidthKnown = !float.IsNaN(panelWidth) && panelWidth > 0f;
        bool fitsOnRight = !panelWidthKnown || rightPlacement + tooltipWidth <= panelWidth;

        float left = fitsOnRight
            ? rightPlacement
            : localRect.xMin - AnchorGap - tooltipWidth;
        if (panelWidthKnown) left = Mathf.Clamp(left, 0f, Mathf.Max(0f, panelWidth - tooltipWidth));

        float top = localRect.yMin;
        bool panelHeightKnown = !float.IsNaN(panelHeight) && panelHeight > 0f;
        if (panelHeightKnown) top = Mathf.Clamp(top, 0f, Mathf.Max(0f, panelHeight - tooltipHeight));

        _label.style.left = left;
        _label.style.top = top;
    }

    /// <summary>Registers hover show/hide on `target`, pulling fresh text from `getText` at hover time (so a caller's live-updated cache, e.g. a per-nameplate cached string, is always current).</summary>
    public void RegisterHover(VisualElement target, Func<string> getText)
    {
        target.RegisterCallback<PointerEnterEvent>(evt => Show(getText(), target));
        target.RegisterCallback<PointerLeaveEvent>(evt => Hide());
    }
}
