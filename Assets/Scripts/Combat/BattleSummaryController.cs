using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Read-only post-battle summary screen (2026-08 session — reworked from the old spend-here-and-
/// now Aura Allocation screen; see DECISIONS.md -> [Combat]). Shown from BattleManager.EndBattle
/// on a Won outcome before the battle scene unloads. Shows Aura gained, damage dealt, and healing
/// done this battle — informational only, no interaction beyond Continue. Aura spending moved to
/// the new Tab-key overworld menu (PartyMenuController), per explicit user direction: "It should
/// just be an after menu for aura gained, damage done, healed... not where we spend it."
///
/// MonoBehaviour singleton wrapping a UIDocument, matching BattleHUDController's convention.
/// Lives on a GameObject in BattleScene_Main alongside BattleHUDController, so it shares that
/// scene's lifetime.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class BattleSummaryController : MonoBehaviour
{
    public static BattleSummaryController Instance { get; private set; }

    private VisualElement _root;
    private Label _auraGainedLabel;
    private Label _damageDealtLabel;
    private Label _healingDoneLabel;
    private Button _continueButton;

    private Action _onDone;

    private void Awake()
    {
        Instance = this;

        var document = GetComponent<UIDocument>();
        _root = document.rootVisualElement.Q<VisualElement>("BattleSummaryRoot");
        _auraGainedLabel = _root.Q<Label>("AuraGainedLabel");
        _damageDealtLabel = _root.Q<Label>("DamageDealtLabel");
        _healingDoneLabel = _root.Q<Label>("HealingDoneLabel");
        _continueButton = _root.Q<Button>("ContinueButton");

        _continueButton.clicked += HandleContinueClicked;

        Hide();
    }

    /// <summary>Populates and shows the summary. onDone fires exactly once, when Continue is pressed.</summary>
    public void Show(BattleSummary summary, Action onDone)
    {
        _onDone = onDone;

        _auraGainedLabel.text = $"Aura Gained: {summary.TotalAuraGained}";
        _damageDealtLabel.text = $"Damage Dealt: {summary.TotalDamageDealt}";
        _healingDoneLabel.text = $"Healing Done: {summary.TotalHealingDone}";

        _root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        _root.style.display = DisplayStyle.None;
        _onDone = null;
    }

    private void HandleContinueClicked()
    {
        Action callback = _onDone;
        Hide();
        callback?.Invoke();
    }
}
