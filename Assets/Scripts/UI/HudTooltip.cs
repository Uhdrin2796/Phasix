using System;
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
/// One instance per screen (BattleHUDController and OverworldMenuController each own their own,
/// not a cross-screen singleton) — only one element can be hovered at a time on a given screen, so
/// a single shared Label per screen is enough.
/// </summary>
public class HudTooltip
{
    private const float AnchorGap = 8f;

    private readonly Label _label;
    private readonly VisualElement _root;

    /// <summary>Creates the tooltip's Label and adds it as the LAST child of root, so it always paints on top of every other root child (UI Toolkit's document-order paint rule).</summary>
    public HudTooltip(VisualElement root)
    {
        _root = root;
        _label = new Label { pickingMode = PickingMode.Ignore, style = { display = DisplayStyle.None } };
        _label.AddToClassList("hud-tooltip");
        _root.Add(_label);
    }

    public void Hide() => _label.style.display = DisplayStyle.None;

    public void Show(string text, VisualElement anchor)
    {
        _label.text = text;
        _label.style.display = DisplayStyle.Flex;
        PositionNear(anchor);
    }

    private void PositionNear(VisualElement anchor)
    {
        var localRect = _root.WorldToLocal(anchor.worldBound);
        _label.style.left = localRect.xMax + AnchorGap;
        _label.style.top = localRect.yMin;
    }

    /// <summary>Registers hover show/hide on `target`, pulling fresh text from `getText` at hover time (so a caller's live-updated cache, e.g. a per-nameplate cached string, is always current).</summary>
    public void RegisterHover(VisualElement target, Func<string> getText)
    {
        target.RegisterCallback<PointerEnterEvent>(evt => Show(getText(), target));
        target.RegisterCallback<PointerLeaveEvent>(evt => Hide());
    }
}
