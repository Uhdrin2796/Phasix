using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Wild encounter Flee/Engage prompt. MonoBehaviour singleton wrapping a UIDocument, matching
/// PartySystem.Instance's convention. Wk 14-16 scaffold — no Signal/Tempo/Celestial fields,
/// Primal type is the only axis with UI here (GDD §8: Primal = "Full — always visible").
///
/// First script in Assets/Scripts/UI/, and the project's first UI Toolkit screen — see
/// DECISIONS.md → [UI] for why UI Toolkit over uGUI, and Assets/UI/EncounterPromptPanelSettings.asset
/// for the 320x180 scale config that keeps this in step with the Pixel Perfect Camera.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class EncounterPromptController : MonoBehaviour
{
    public static EncounterPromptController Instance { get; private set; }

    /// <summary>
    /// True while the prompt is currently shown. Read by WildEncounterCreature before calling
    /// Show() to guard against two simultaneous encounters clobbering each other's callbacks.
    /// </summary>
    public bool IsVisible { get; private set; }

    private VisualElement _root;
    private Label _speciesNameLabel;
    private Label _primalTypeLabel;
    private VisualElement _primalTypeSwatch;
    private Button _fleeButton;
    private Button _engageButton;

    private Action _onFlee;
    private Action _onEngage;

    private void Awake()
    {
        Instance = this;

        var document = GetComponent<UIDocument>();
        _root = document.rootVisualElement.Q<VisualElement>("EncounterPromptRoot");
        _speciesNameLabel = _root.Q<Label>("SpeciesNameLabel");
        _primalTypeLabel = _root.Q<Label>("PrimalTypeLabel");
        _primalTypeSwatch = _root.Q<VisualElement>("PrimalTypeSwatch");
        _fleeButton = _root.Q<Button>("FleeButton");
        _engageButton = _root.Q<Button>("EngageButton");

        // Registered once — Show() swaps which Action these forward to, rather than
        // re-registering a new closure on every encounter (which would stack listeners).
        _fleeButton.clicked += () => _onFlee?.Invoke();
        _engageButton.clicked += () => _onEngage?.Invoke();

        Hide();
    }

    /// <summary>
    /// Shows the prompt for one encounter. onFlee/onEngage replace whatever the previous
    /// encounter registered — never accumulate.
    /// </summary>
    public void Show(PhasixData species, Action onFlee, Action onEngage)
    {
        _speciesNameLabel.text = species.SpeciesName;
        _primalTypeLabel.text = species.PrimalType.ToString();
        _primalTypeSwatch.style.backgroundColor = PrimalTypeColor.GetColor(species.PrimalType);

        _onFlee = onFlee;
        _onEngage = onEngage;

        _root.style.display = DisplayStyle.Flex;
        IsVisible = true;
    }

    public void Hide()
    {
        _root.style.display = DisplayStyle.None;
        IsVisible = false;
        _onFlee = null;
        _onEngage = null;
    }
}
