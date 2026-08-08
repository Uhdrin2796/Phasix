using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Tab-key overworld menu (2026-08 session — see DECISIONS.md -> [Combat]/[Input]). Currently
/// holds just the Aura-spend screen, moved here from the old post-battle Aura Allocation screen
/// per explicit user direction: spending shouldn't happen right after a battle — "Spending should
/// be part of some menu. For now put it into the tab menu but maybe in future we'll have a
/// dedicated 'shop' or NPC that allows us to spend." Toggled by pressing Tab (freed up from
/// DebugMovementPresetCycler, which moved to ~ this same session).
///
/// MonoBehaviour, living in the overworld scene (SampleScene) — separate scene lifetime from
/// BattleSummaryController/BattleHUDController, which live in BattleScene_Main. Not a singleton
/// like those two (nothing outside this class needs to reach it) — plain component wrapping its
/// own UIDocument.
///
/// Deliberately NOT built as a multi-tab/multi-section container — CLAUDE.md: don't build
/// abstractions beyond what's needed today. When a real shop/NPC or additional menu sections
/// exist, this is the natural place to extend, not a reason to pre-build tabs now. Reuses the
/// per-creature "+1" card UI originally built for the post-battle screen, adapted to operate on
/// PhasixRuntimeData directly (there's no BattleParticipant wrapper outside a battle) and to show
/// ALL party slots, not just "living" ones (there's no alive/dead concept in the overworld).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PartyMenuController : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _cardContainer;
    private bool _isOpen;

    private void Awake()
    {
        var document = GetComponent<UIDocument>();
        _root = document.rootVisualElement.Q<VisualElement>("PartyMenuRoot");
        _cardContainer = _root.Q<VisualElement>("PartyCardContainer");

        Close();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (_isOpen) Close();
            else Open();
        }
    }

    private void Open()
    {
        _cardContainer.Clear();

        for (int i = 0; i < PartySystem.MaxPartySize; i++)
        {
            PhasixRuntimeData phasix = PartySystem.Instance != null ? PartySystem.Instance.GetSlot(i) : null;
            if (phasix != null) _cardContainer.Add(BuildCard(phasix));
        }

        _root.style.display = DisplayStyle.Flex;
        _isOpen = true;
    }

    private void Close()
    {
        _root.style.display = DisplayStyle.None;
        _isOpen = false;
    }

    private static VisualElement BuildCard(PhasixRuntimeData phasix)
    {
        var card = new VisualElement();
        card.AddToClassList("aura-card");

        var header = new VisualElement();
        header.AddToClassList("aura-card-header");
        string displayName = phasix.speciesData != null ? phasix.speciesData.SpeciesName : "???";
        var nameLabel = new Label(displayName);
        nameLabel.AddToClassList("aura-card-name");
        header.Add(nameLabel);
        var auraLabel = new Label();
        auraLabel.AddToClassList("aura-card-aura");
        header.Add(auraLabel);
        card.Add(header);

        var statGrid = new VisualElement();
        statGrid.AddToClassList("aura-stat-grid");
        card.Add(statGrid);

        void RefreshAuraLabel() => auraLabel.text = $"Aura: {phasix.commonAura}";

        foreach (StatType stat in (StatType[])Enum.GetValues(typeof(StatType)))
        {
            statGrid.Add(BuildStatRow(phasix, stat, RefreshAuraLabel));
        }

        RefreshAuraLabel();
        return card;
    }

    private static VisualElement BuildStatRow(PhasixRuntimeData phasix, StatType stat, Action onSpent)
    {
        var row = new VisualElement();
        row.AddToClassList("aura-stat-row");

        var statNameLabel = new Label(stat.ToString());
        statNameLabel.AddToClassList("aura-stat-name");
        row.Add(statNameLabel);

        var valueLabel = new Label();
        valueLabel.AddToClassList("aura-stat-value");
        row.Add(valueLabel);

        var plusButton = new Button { text = "+1" };
        plusButton.AddToClassList("aura-stat-button");
        row.Add(plusButton);

        void RefreshValue() => valueLabel.text = GetStatValue(phasix.baseStats, stat).ToString();

        plusButton.clicked += () =>
        {
            int tier = phasix.speciesData != null ? phasix.speciesData.EvolutionTier : 1;
            if (AuraStatAllocationSystem.TryAllocateStatPoint(phasix, tier, stat))
            {
                RefreshValue();
                onSpent();
            }
            // Insufficient Aura or already at the tier ceiling — silent no-op, matching
            // AuraStatAllocationSystem.TryAllocateStatPoint's own "returns false, spends nothing" contract.
        };

        RefreshValue();
        return row;
    }

    private static int GetStatValue(StatBlock block, StatType stat)
    {
        switch (stat)
        {
            case StatType.Vitality: return block.Vitality;
            case StatType.Force: return block.Force;
            case StatType.Resonance: return block.Resonance;
            case StatType.Guard: return block.Guard;
            case StatType.Ward: return block.Ward;
            case StatType.Resolve: return block.Resolve;
            case StatType.Instinct: return block.Instinct;
            case StatType.Aura: return block.Aura;
            default: return 0;
        }
    }
}
