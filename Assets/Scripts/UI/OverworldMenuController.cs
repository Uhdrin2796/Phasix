using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Full overworld Tab-key menu (2026-08 session — see DECISIONS.md -> [UI]), replacing
/// PartyMenuController's single-purpose Aura-spend screen with Party / Save / Bag / Options tabs,
/// per explicit user direction. Bag/Options are placeholder "pending design" panels — no item or
/// settings system exists yet (CLAUDE.md lists both as pending).
///
/// Party tab: roster cards -> click opens a per-creature detail view with the existing Aura
/// stat-allocation rows (ported verbatim from PartyMenuController.BuildStatRow) plus the equipped
/// skill wheel, reusing the battle scene's own skill-ring orb classes/lettering/hover tooltip
/// (BattleHUD.uss's .skill-slot-placeholder/.skill-ring-color-N/.move-option-label/.hud-tooltip,
/// BattleHUDController.BuildSkillTooltipText, the shared HudTooltip class, and DragLineVisual)
/// so it reads identically to battle, per the user's explicit ask. Dragging one equipped orb onto
/// another swaps them (SkillLoadoutSystem.SwapEquipped); dragging a skill from the full-catalog
/// tray onto a wheel orb equips it there, occupied or empty (SkillLoadoutSystem.TryEquipAt) —
/// user's own words: "lets do both options." Right-click (UI Toolkit's ContextClickEvent) on an
/// equipped orb unequips it back to the tray (SkillLoadoutSystem.Unequip) — "let right click be to
/// unequip an equipped skill."
///
/// 2026-08 follow-up (user feedback after the first pass): (1) orb color is now DEDICATED TO THE
/// SKILL ITSELF (a stable hash of its GUID into the same 7-color palette), not owned by ring
/// position like battle's — "the colors and stuff should be dedicated to that specific skill not
/// just on the slot." (2) The tray shows the database's FULL skill catalog, not just what this
/// creature has already "learned" — "all the other skills we have right now... should be
/// displayed... a full list of the skills outlined as all the options." Dragging an unlearned
/// skill onto the wheel auto-adds it to learnedSkillGuids first (keeping SkillLoadoutSystem's own
/// "equip requires learned" contract intact — this class just does "learn, then equip" as one
/// gesture). (3) The wheel itself is now a literal 12-position replica of BattleHUDController's
/// skill ring (same radius/clock-hour math) — "the skill wheel in the party configurator should
/// be a replica of what we see in the battle scene, but we can just equip and unequip items
/// there." All 12 positions start at 1 o'clock and are real equip slots at max tier (2026-08
/// follow-up #4 — user: "I want it to start its first usable slot index to start from the 1
/// oclock position," then later: "at max tier they should be able to access all 12 slots" — see
/// WheelEquipSlotCount's own comment and SkillSlotCapacity.GetActiveSlotRange).
///
/// 2026-08 follow-up #2 (user feedback after the color/wheel/full-catalog pass): every orb/icon
/// label (ring AND tray) is now a short code — GetShortSkillLabel — instead of the full SkillName,
/// which was overflowing a 32px circle for the 34 of 36 placeholder skills that haven't been
/// individually renamed yet — "It should only show their icon maxium a letter and a number, then
/// they should all have the hover over description similar to the in battle game." The full name/
/// mechanics still show on hover via BattleHUDController.BuildSkillTooltipText, unchanged.
/// Already-short real names (e.g. "C1"/"C2") pass through as-is; everything else gets a generated
/// `{tree-initial}{index-within-tree}` code (e.g. "S1" for Synergy's first skill) — purely a
/// display transform, the underlying SkillData.SkillName is never touched.
///
/// 2026-08 follow-up #3: the 5 built-in moves (Attack/Charge/Heal/Regen/Capture) are no longer
/// special-cased here at all — user: "dont make them inherent i want them to also be selectable...
/// full customizability, for good or for worse." They're real, equippable Standard-tree SkillData
/// now (see BuiltInMoveType), so they flow through the SAME SkillDatabase.AllSkills tray loop as
/// every other skill: real per-skill hash color, real short label ("A"/"C"/"H"/"R"/"K", already
/// short enough to pass GetShortSkillLabel's <=3-char rule unchanged), real drag-to-equip, real
/// right-click-to-unequip. The old informational-only, non-draggable tray entries this class used
/// to build for them (BuiltInMoveLetters/BuiltInMoveColorClasses) are gone.
///
/// 2026-08 follow-up #5: the tray is now GROUPED by SkillTreeType (a header + wrapped icon row per
/// tree, SkillTreeColor.DisplayOrder) instead of one flat wrapped list of the whole catalog — user: "when
/// click into the skill menu... everything is just listed out... id like them to be on their own
/// tree. we can do the finer details of what unlocks into what later but id like to be able to
/// separate them out already." Pure display grouping by each skill's EXISTING TreeType tag — no
/// new unlock/progression logic, real skill-tree content is still pending (CLAUDE.md).
///
/// 2026-08 follow-up #6/#7 (superseded, see the pan/zoom web entry below): the flat grouped tray
/// was briefly replaced by a Skyrim-style PAGED carousel (buttons/arrows/drag-swipe between one
/// tree per page). Retired the same session it shipped — user, after live use: "we need to fix
/// the skill tree look... this looks awful." Kept only as changelog/decisions history.
///
/// 2026-08 follow-up #8 (current): the tray is now a free pan/zoom WEB VIEW, prototyping the
/// user's original Evolution Web concept (mockup: evolution_web.html) against the skill tree
/// first — no Phase 4 data blocker here, unlike Evolution — see DECISIONS.md -> [UI] for the full
/// architecture writeup. Every skill tree is a COLUMN (SkillTreeColor.DisplayOrder order, Standard
/// first), its skills a vertical row of nodes connected by a straight line, all drawn by
/// SkillWebEdgeVisual (Painter2D, same convention as DragLineVisual) sitting behind the real
/// VisualElement nodes so native hover/click/HudTooltip keeps working. A tree not yet unlocked
/// (SkillTreeUnlockSystem.GetEffectiveUnlockedTrees) renders as a dim, non-interactive silhouette
/// column instead of a fully browsable one — reusing the mockup's Discovered/Sighted visual
/// language, driven by tier-gating rather than true fog-of-war (skills aren't "encountered in the
/// wild"). SkillLoadoutSystem.TryEquip/TryEquipAt now also enforce this same unlock check for
/// real, not just cosmetically in the UI (closes a gap found while building this — previously
/// nothing checked unlockedTreeTypes at the equip layer at all). A debug tier stepper lets the
/// creature's EFFECTIVE tier (PhasixRuntimeData.DebugTierOverride) be walked 1-5 live to preview
/// unlocks/slot capacity without a real (Phase 4, unbuilt) evolution — GetEffectiveUnlockedTrees
/// is the single source of truth both this view and the equip gate read, so the debug override
/// never desyncs "looks unlocked" from "actually equippable."
///
/// Save tab: 3 slots, click to overwrite (SaveSystem.Save via GameManager.SaveToSlot) — save-only,
/// no explicit "load a different slot" UI (auto-continue from the newest slot happens on boot,
/// GameManager.TryAutoLoad).
///
/// Always-visible debug "New Game" button lives OUTSIDE the Tab-toggled menu root (DebugBar in
/// OverworldMenu.uxml) so it renders whether or not the menu is open — calls
/// GameManager.Instance?.ResetToNewGame().
///
/// Text sizing floor (2026-08 follow-up — user: "make sure font size is similar to standard font
/// size like whats in the battle log"): every label/button in OverworldMenu.uss is >= 13px body /
/// >= 15px section header, matching BattleHUD.uss's .battle-log-entry/.battle-log-title.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class OverworldMenuController : MonoBehaviour
{
    [Header("Skill Resolution")]
    [Tooltip("Assign Assets/Data/Skills/SkillDatabase.asset — resolves equipped/learned skill guids to SkillData for the Party detail view's skill ring and web.")]
    [SerializeField] private SkillDatabase _skillDatabase;

    [Header("Debug")]
    [Tooltip("Species spawned by the DEBUG: Add Party Member button — deliberately a different species than GameManager's Fallback Starter (Test_FireType) so a debug-added member looks/plays distinctly from slot 0. Assign a test asset from Assets/Data/Species/.")]
    [SerializeField] private PhasixData _debugPartyMemberSpecies;

    // Column display order for the skill web now lives on SkillTreeColor.DisplayOrder (2026-08-09
    // follow-up — shared with the battle scene's skill ring, see that class's own doc comment) —
    // one canonical order for both layout here and color everywhere a skill is shown.

    // Full 12-position wheel, same radius/clock-hour math as BattleHUDController's own skill ring
    // (SkillSlotCount/SkillSlotRadius there) — user: "the skill wheel in the party configurator
    // should be a replica of what we see in the battle scene." All 12 physical positions are real
    // equip slots (2026-08 follow-up — user: "at max tier they should be able to access all 12
    // slots" — SkillSlotCapacity.GetActiveSlotRange now reaches 12 at T5, see that method's own
    // doc comment; previously only 7 of 12 were wired, with the other 5 permanently decorative
    // since the old tier table never exceeded 7). Positions beyond the CURRENT creature's tier cap
    // still render tier-locked (dim, no drag, explanatory tooltip) via the maxSlots check below —
    // they're locked-until-a-higher-tier now, not permanently inert. Starts at 1 o'clock (index 0
    // = hour 1 — 2026-08 follow-up: originally offset 3/hours 4-10, moved to 1 o'clock per user:
    // "I want it to start its first usable slot index to start from the 1 oclock position").
    private const int WheelSlotCount = 12;
    private const float WheelRadius = 95f;
    private const int WheelEquipSlotOffset = 0;
    private const int WheelEquipSlotCount = 12;

    // .skill-ring-area box size/center (see OverworldMenu.uss) — sized to comfortably contain
    // WheelRadius (95) plus an orb's own half-size (16) on every side.
    private const float WheelCenter = 120f;

    // 2026-08 follow-up #8 — skill web view. WorldWidth/Height are the logical full extent of the
    // 19-column x 5-row node grid (every GDD tree now has exactly 5 placeholders, Standard has its
    // 5 real built-ins — a uniform grid, no ragged columns). WebStageWidth/Height is the fixed,
    // clipped viewport the player actually sees; the "world" container inside it is what pans
    // (style.translate) and zooms (style.scale) — see BuildSkillArea's own comment for the
    // transform-around-center math this depends on (verified live against a throwaway
    // parent/child VisualElement pair before this was built on top of the assumption).
    private const float WebColumnSpacing = 90f;
    private const float WebRowSpacing = 56f;
    private const float WebLeftPadding = 40f;
    private const float WebTopPadding = 40f;
    private const float WebNodeSize = 32f; // matches .skill-slot-placeholder's fixed size
    private const float WebStageWidth = 640f;
    private const float WebStageHeight = 340f;
    private const float WebMinScale = 0.4f;
    private const float WebMaxScale = 2.2f;
    private const int WebMaxRowsPerColumn = 5;
    private static readonly float WorldWidth = WebLeftPadding * 2f + SkillTreeColor.DisplayOrder.Length * WebColumnSpacing;
    private static readonly float WorldHeight = WebTopPadding * 2f + WebMaxRowsPerColumn * WebRowSpacing;

    private VisualElement _root;
    private HudTooltip _tooltip;
    private bool _isOpen;

    private Button _debugNewGameButton;
    private Button _debugAddPartyMemberButton;
    private VisualElement _menuRoot;

    private Button _tabButtonParty, _tabButtonSave, _tabButtonBag, _tabButtonOptions;
    private VisualElement _partyPanel, _savePanel, _bagPanel, _optionsPanel;

    private VisualElement _partyRosterView;
    private VisualElement _partyCardContainer;
    private VisualElement _partyDetailView;

    private VisualElement _saveSlotContainer;

    private void Awake()
    {
        var document = GetComponent<UIDocument>();
        _root = document.rootVisualElement;
        _tooltip = new HudTooltip(_root);

        _debugNewGameButton = _root.Q<Button>("DebugNewGameButton");
        _debugNewGameButton.clicked += () => GameManager.Instance?.ResetToNewGame();

        _debugAddPartyMemberButton = _root.Q<Button>("DebugAddPartyMemberButton");
        _debugAddPartyMemberButton.clicked += DebugAddPartyMember;

        _menuRoot = _root.Q<VisualElement>("OverworldMenuRoot");

        _tabButtonParty = _root.Q<Button>("TabButtonParty");
        _tabButtonSave = _root.Q<Button>("TabButtonSave");
        _tabButtonBag = _root.Q<Button>("TabButtonBag");
        _tabButtonOptions = _root.Q<Button>("TabButtonOptions");
        _tabButtonParty.clicked += () => SwitchTab(_partyPanel, _tabButtonParty);
        _tabButtonSave.clicked += () => SwitchTab(_savePanel, _tabButtonSave);
        _tabButtonBag.clicked += () => SwitchTab(_bagPanel, _tabButtonBag);
        _tabButtonOptions.clicked += () => SwitchTab(_optionsPanel, _tabButtonOptions);

        _partyPanel = _root.Q<VisualElement>("PartyPanel");
        _savePanel = _root.Q<VisualElement>("SavePanel");
        _bagPanel = _root.Q<VisualElement>("BagPanel");
        _optionsPanel = _root.Q<VisualElement>("OptionsPanel");

        _partyRosterView = _root.Q<VisualElement>("PartyRosterView");
        _partyCardContainer = _root.Q<VisualElement>("PartyCardContainer");
        _partyDetailView = _root.Q<VisualElement>("PartyDetailView");

        _saveSlotContainer = _root.Q<VisualElement>("SaveSlotContainer");

        Close();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (_isOpen) Close();
            else Open();
        }
        // No keyboard pan/zoom for the skill web (confirmed with user, 2026-08 follow-up #8) —
        // mouse drag + wheel zoom only, matching the Evolution Web reference mockup exactly. This
        // intentionally drops the old carousel's arrow-key paging; free-form pan/zoom has no clean
        // keyboard equivalent that isn't its own separate feature.
    }

    private void Open()
    {
        ShowRoster();
        RefreshRoster();
        SwitchTab(_partyPanel, _tabButtonParty);
        _menuRoot.style.display = DisplayStyle.Flex;
        _isOpen = true;
    }

    private void Close()
    {
        _menuRoot.style.display = DisplayStyle.None;
        _tooltip.Hide();
        _isOpen = false;
    }

    private void SwitchTab(VisualElement targetPanel, Button targetButton)
    {
        _partyPanel.style.display = targetPanel == _partyPanel ? DisplayStyle.Flex : DisplayStyle.None;
        _savePanel.style.display = targetPanel == _savePanel ? DisplayStyle.Flex : DisplayStyle.None;
        _bagPanel.style.display = targetPanel == _bagPanel ? DisplayStyle.Flex : DisplayStyle.None;
        _optionsPanel.style.display = targetPanel == _optionsPanel ? DisplayStyle.Flex : DisplayStyle.None;

        _tabButtonParty.EnableInClassList("tab-button-active", targetButton == _tabButtonParty);
        _tabButtonSave.EnableInClassList("tab-button-active", targetButton == _tabButtonSave);
        _tabButtonBag.EnableInClassList("tab-button-active", targetButton == _tabButtonBag);
        _tabButtonOptions.EnableInClassList("tab-button-active", targetButton == _tabButtonOptions);

        if (targetPanel == _savePanel) BuildSaveTab();
    }

    // --- Party tab: roster ---

    private void RefreshRoster()
    {
        _partyCardContainer.Clear();
        for (int i = 0; i < PartySystem.MaxPartySize; i++)
        {
            PhasixRuntimeData phasix = PartySystem.Instance != null ? PartySystem.Instance.GetSlot(i) : null;
            if (phasix != null) _partyCardContainer.Add(BuildRosterCard(phasix));
        }
    }

    /// <summary>
    /// DEBUG: Add Party Member (2026-08-10 follow-up — user: "can you add a debug where it says:
    /// new game to add a party member so i can test it out myself") — lets multi-Phasix scenarios
    /// (skill web, battle skill ring, etc.) be tested without first winning a real capture. Spawns
    /// via the same WildSpawnSystem.CreateWildInstance entry point every real creature goes
    /// through (identical seeded unlockedTreeTypes/learnedSkillGuids/equippedSkillGuids), so it
    /// exercises the real path rather than a hand-built shortcut. Uses _debugPartyMemberSpecies
    /// (Test_SteamType by default) rather than GameManager's Fallback Starter species so a
    /// debug-added member is visibly distinct from the slot-0 starter. No-ops with a console
    /// warning if the party is already full (PartySystem.AddToParty returns -1) — mirrors
    /// SeedFallbackStarter's own no-op-with-warning pattern for a missing species reference.
    /// </summary>
    private void DebugAddPartyMember()
    {
        if (_debugPartyMemberSpecies == null)
        {
            Debug.LogWarning("[OverworldMenuController] DEBUG: Add Party Member — no Debug Party Member Species assigned in the Inspector.");
            return;
        }

        if (PartySystem.Instance == null)
        {
            Debug.LogWarning("[OverworldMenuController] DEBUG: Add Party Member — no PartySystem.Instance found.");
            return;
        }

        PhasixRuntimeData runtime = WildSpawnSystem.CreateWildInstance(_debugPartyMemberSpecies, _skillDatabase);
        int slot = PartySystem.Instance.AddToParty(runtime);
        if (slot < 0)
        {
            Debug.LogWarning("[OverworldMenuController] DEBUG: Add Party Member — party is full.");
            return;
        }

        Debug.Log($"[OverworldMenuController] DEBUG: Added {_debugPartyMemberSpecies.SpeciesName} to party slot {slot}.");
        RefreshRoster();
    }

    private VisualElement BuildRosterCard(PhasixRuntimeData phasix)
    {
        var card = new Button();
        card.AddToClassList("roster-card");
        card.clicked += () => ShowDetail(phasix);

        string name = phasix.speciesData != null ? phasix.speciesData.SpeciesName : "???";
        var nameLabel = new Label(name);
        nameLabel.AddToClassList("roster-card-name");
        card.Add(nameLabel);

        int tier = phasix.speciesData != null ? phasix.speciesData.EvolutionTier : 1;
        var subLabel = new Label($"Tier {tier}  ·  Aura {phasix.commonAura}");
        subLabel.AddToClassList("roster-card-sub");
        card.Add(subLabel);

        return card;
    }

    private void ShowRoster()
    {
        _partyDetailView.style.display = DisplayStyle.None;
        _partyDetailView.Clear();
        _partyRosterView.style.display = DisplayStyle.Flex;
    }

    // --- Party tab: detail view ---

    private void ShowDetail(PhasixRuntimeData runtime)
    {
        _partyRosterView.style.display = DisplayStyle.None;
        _partyDetailView.Clear();
        _partyDetailView.style.display = DisplayStyle.Flex;

        var header = new VisualElement();
        header.AddToClassList("detail-header");
        var backButton = new Button();
        backButton.AddToClassList("detail-back-button");
        backButton.text = "< Back";
        backButton.clicked += ShowRoster;
        header.Add(backButton);
        var nameLabel = new Label(runtime.speciesData != null ? runtime.speciesData.SpeciesName : "???");
        nameLabel.AddToClassList("detail-name-label");
        header.Add(nameLabel);
        _partyDetailView.Add(header);

        var body = new VisualElement();
        body.AddToClassList("detail-body");
        body.Add(BuildStatColumn(runtime));
        int tier = runtime.speciesData != null ? runtime.speciesData.EvolutionTier : 1;
        body.Add(BuildSkillArea(runtime, tier));
        _partyDetailView.Add(body);

        _partyDetailView.Add(BuildFormationSection(runtime));
    }

    /// <summary>
    /// Pre-battle formation slot picker (2026-08-12, user: "lets just have 5 positions across a
    /// lane. Then you can preset which position you want to be in") — reuses
    /// FormationGridPicker.Build (a flex-flowed row/column grid, click-to-select) for THIS static
    /// screen. The in-battle Move redesign (same session, user: "drag and drop a player to a
    /// location... just like how we do the projectile") shows a DIFFERENT construction instead —
    /// BattleHUDController.ShowStagePositionMarkers, individually positioned to match real stage
    /// coordinates and hit-tested via drag-release rather than clicked — but both share
    /// FormationGridPicker.BuildCell for cell appearance/state, so a slot always LOOKS the same
    /// whether you're picking it here or dragging to it in battle. Occupancy
    /// is checked against every OTHER party slot's PhasixRuntimeData.preferredLaneIndex/
    /// preferredPositionIndex (comparing PhasixRuntimeData references directly — party slots hold
    /// distinct instances, no two slots can ever reference the same one). Picking a cell updates
    /// the runtime fields directly (read by BattleParticipant's constructor at battle start) and
    /// rebuilds the whole detail view via ShowDetail so the grid's "current" highlight moves — the
    /// same "just rebuild everything, this isn't a hot path" approach ShowDetail already uses for
    /// every other click in this view.
    /// </summary>
    private VisualElement BuildFormationSection(PhasixRuntimeData runtime)
    {
        var section = new VisualElement();
        section.AddToClassList("detail-formation-section");

        var title = new Label("Starting Formation");
        title.AddToClassList("formation-grid-title");
        section.Add(title);

        string GetOccupantLabel(int lane, int position)
        {
            if (PartySystem.Instance == null) return null;
            for (int i = 0; i < PartySystem.MaxPartySize; i++)
            {
                PhasixRuntimeData other = PartySystem.Instance.GetSlot(i);
                if (other == null || other == runtime) continue;
                if (other.preferredLaneIndex == lane && other.preferredPositionIndex == position)
                    return other.speciesData != null && other.speciesData.SpeciesName.Length > 0 ? other.speciesData.SpeciesName.Substring(0, 1) : "?";
            }
            return null;
        }

        void OnCellChosen(int lane, int position)
        {
            runtime.preferredLaneIndex = lane;
            runtime.preferredPositionIndex = position;
            ShowDetail(runtime);
        }

        section.Add(FormationGridPicker.Build(runtime.preferredLaneIndex, runtime.preferredPositionIndex, GetOccupantLabel, OnCellChosen));
        return section;
    }

    private static VisualElement BuildStatColumn(PhasixRuntimeData phasix)
    {
        var column = new VisualElement();
        column.AddToClassList("detail-stat-column");

        var auraLabel = new Label();
        auraLabel.AddToClassList("detail-aura-label");
        column.Add(auraLabel);

        void RefreshAuraLabel() => auraLabel.text = $"Aura: {phasix.commonAura}";

        foreach (StatType stat in (StatType[])Enum.GetValues(typeof(StatType)))
        {
            column.Add(BuildDetailStatRow(phasix, stat, RefreshAuraLabel));
        }

        RefreshAuraLabel();
        return column;
    }

    private static VisualElement BuildDetailStatRow(PhasixRuntimeData phasix, StatType stat, Action onSpent)
    {
        var row = new VisualElement();
        row.AddToClassList("detail-stat-row");

        var statNameLabel = new Label(stat.ToString());
        statNameLabel.AddToClassList("detail-stat-name");
        row.Add(statNameLabel);

        var valueLabel = new Label();
        valueLabel.AddToClassList("detail-stat-value");
        row.Add(valueLabel);

        var plusButton = new Button { text = "+1" };
        plusButton.AddToClassList("detail-stat-button");
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

    /// <summary>
    /// Builds the full 12-position skill wheel (replica of BattleHUDController's skill ring — see
    /// the class doc comment) plus a pan/zoom skill WEB below it (2026-08 follow-up #8 — prototype
    /// of the user's Evolution Web concept, built against the skill tree first since it has no
    /// Phase 4 data blocker). All drag/pan/zoom/right-click state is local to this one call
    /// (closures over `runtime`/`tier`/the wheel and world elements) since only one detail view is
    /// ever open at a time — rebuilt fresh every ShowDetail call, same as the rest of this class.
    ///
    /// `maxSlots`/`effectiveTier` are MUTABLE locals, not fixed at build time — the debug tier
    /// stepper can change PhasixRuntimeData.DebugTierOverride without leaving this view, so every
    /// place that used to read a tier-derived value once now re-reads these each RefreshSkillArea
    /// call, and every wheel slot's interactivity is checked at USE time (inside its own handler)
    /// against the current maxSlots rather than decided once at BUILD time — otherwise a slot that
    /// was tier-locked when the view first opened would never become usable after the debug
    /// stepper raises the tier, even though its visual state (RefreshSkillArea's own loop) already
    /// updates correctly.
    /// </summary>
    private VisualElement BuildSkillArea(PhasixRuntimeData runtime, int tier)
    {
        int maxSlots = SkillSlotCapacity.GetActiveSlotRange(runtime.DebugTierOverride ?? tier).max;
        int effectiveTier = runtime.DebugTierOverride ?? tier;

        var column = new VisualElement();
        column.AddToClassList("detail-skill-column");

        var ringArea = new VisualElement();
        ringArea.AddToClassList("skill-ring-area");
        column.Add(ringArea);

        var hub = new VisualElement();
        hub.AddToClassList("skill-ring-hub");
        ringArea.Add(hub);

        var dragLine = new DragLineVisual { style = { display = DisplayStyle.None } };
        ringArea.Add(dragLine);

        var equipSlots = new VisualElement[WheelEquipSlotCount];
        var equipLabels = new Label[WheelEquipSlotCount];

        int dragSourceEquipIndex = -1;
        string dragSourceTraySkillGuid = null;
        SkillTreeType dragSourceTraySkillTree = SkillTreeType.Standard;

        // --- Web header: debug tier stepper + Reset View (2026-08 follow-up #8) ---

        var webHeader = new VisualElement();
        webHeader.AddToClassList("web-header");
        column.Add(webHeader);

        var tierPrevButton = new Button { text = "◀" };
        tierPrevButton.AddToClassList("tree-nav-button");
        webHeader.Add(tierPrevButton);

        var tierLabel = new Label();
        tierLabel.AddToClassList("tree-nav-label");
        webHeader.Add(tierLabel);

        var tierNextButton = new Button { text = "▶" };
        tierNextButton.AddToClassList("tree-nav-button");
        webHeader.Add(tierNextButton);

        var resetViewButton = new Button { text = "Reset View" };
        resetViewButton.AddToClassList("tree-nav-button");
        webHeader.Add(resetViewButton);

        // Unlock All debug toggle (2026-08-09 follow-up — user: "can we also have an unlock all
        // debug so im able to see everything?"). Independent of the tier stepper: tier still
        // governs equip SLOT capacity; this only controls which TREES render as unlocked. Label
        // reflects current state so it reads as a toggle, not a one-shot action.
        var unlockAllButton = new Button();
        unlockAllButton.AddToClassList("tree-nav-button");
        webHeader.Add(unlockAllButton);

        // --- Web stage: fixed clipped viewport containing the pannable/zoomable world ---

        var webStage = new VisualElement();
        webStage.AddToClassList("web-stage");
        column.Add(webStage);

        var webWorld = new VisualElement();
        webWorld.AddToClassList("web-world");
        webWorld.style.width = WorldWidth;
        webWorld.style.height = WorldHeight;
        webStage.Add(webWorld);

        var edgeOverlay = new SkillWebEdgeVisual();
        edgeOverlay.style.width = WorldWidth;
        edgeOverlay.style.height = WorldHeight;
        webWorld.Add(edgeOverlay);

        var webNodesLayer = new VisualElement();
        webNodesLayer.AddToClassList("web-nodes-layer");
        webNodesLayer.style.width = WorldWidth;
        webNodesLayer.style.height = WorldHeight;
        webWorld.Add(webNodesLayer);

        float worldScale = 1f;
        Vector2 worldTranslate = Vector2.zero;

        void ApplyWorldTransform()
        {
            webWorld.style.scale = new Scale(new Vector3(worldScale, worldScale, 1f));
            webWorld.style.translate = new Translate(worldTranslate.x, worldTranslate.y, 0f);
        }

        // Reusable "world point (wx,wy) currently under screen point (sx,sy)" solve/re-solve pair
        // — the transform is a scale around webWorld's own center (its default USS transform
        // origin) composed with a translate, matching the exact math verified live before this
        // view was built: after = (before - worldCenter) * scale + worldCenter + translate.
        Vector2 WorldCenter() => new Vector2(WorldWidth / 2f, WorldHeight / 2f);

        Vector2 ScreenToWorld(Vector2 screen)
        {
            Vector2 c = WorldCenter();
            return new Vector2(
                (screen.x - c.x - worldTranslate.x) / worldScale + c.x,
                (screen.y - c.y - worldTranslate.y) / worldScale + c.y);
        }

        void SetTransformKeepingWorldPointAtScreen(Vector2 worldPoint, Vector2 screenPoint, float newScale)
        {
            Vector2 c = WorldCenter();
            worldScale = newScale;
            worldTranslate = new Vector2(
                screenPoint.x - c.x - (worldPoint.x - c.x) * worldScale,
                screenPoint.y - c.y - (worldPoint.y - c.y) * worldScale);
            ApplyWorldTransform();
        }

        // Default framing: centered on the currently-unlocked columns (Standard always counts),
        // scaled down just enough to fit their span in the stage if it's wider than the viewport.
        // Recomputed fresh each call (not cached) since which trees are unlocked can change live
        // via the debug tier stepper.
        void ApplyDefaultFraming()
        {
            IReadOnlyList<SkillTreeType> unlocked = SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(runtime);
            int minCol = -1, maxCol = -1;
            for (int c = 0; c < SkillTreeColor.DisplayOrder.Length; c++)
            {
                bool isUnlocked = SkillTreeColor.DisplayOrder[c] == SkillTreeType.Standard || unlocked.Contains(SkillTreeColor.DisplayOrder[c]);
                if (!isUnlocked) continue;
                if (minCol < 0) minCol = c;
                maxCol = c;
            }
            if (minCol < 0) { minCol = 0; maxCol = 0; }

            float spanPx = (maxCol - minCol + 1) * WebColumnSpacing;
            float fitScale = Mathf.Clamp(WebStageWidth / Mathf.Max(spanPx, WebColumnSpacing), WebMinScale, 1f);

            float centerX = WebLeftPadding + ((minCol + maxCol) / 2f) * WebColumnSpacing + WebNodeSize / 2f;
            float centerY = WebTopPadding + ((WebMaxRowsPerColumn - 1) / 2f) * WebRowSpacing + WebNodeSize / 2f;

            SetTransformKeepingWorldPointAtScreen(new Vector2(centerX, centerY),
                new Vector2(WebStageWidth / 2f, WebStageHeight / 2f), fitScale);
        }

        resetViewButton.clicked += ApplyDefaultFraming;

        webStage.RegisterCallback<WheelEvent>(evt =>
        {
            Vector2 screenPos = evt.localMousePosition;
            Vector2 worldPointUnderCursor = ScreenToWorld(screenPos);
            float factor = evt.delta.y > 0f ? 0.9f : 1.1f;
            float newScale = Mathf.Clamp(worldScale * factor, WebMinScale, WebMaxScale);
            SetTransformKeepingWorldPointAtScreen(worldPointUnderCursor, screenPos, newScale);
        });

        bool isPanning = false;
        Vector2 panStartPointer = Vector2.zero;
        Vector2 panStartTranslate = Vector2.zero;

        webStage.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;
            isPanning = true;
            panStartPointer = evt.position;
            panStartTranslate = worldTranslate;
            webStage.CapturePointer(evt.pointerId);
        });
        webStage.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!isPanning) return;
            Vector2 delta = (Vector2)evt.position - panStartPointer;
            worldTranslate = panStartTranslate + delta;
            ApplyWorldTransform();
        });
        webStage.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!isPanning) return;
            isPanning = false;
            webStage.ReleasePointer(evt.pointerId);
        });

        void RefreshSkillArea()
        {
            effectiveTier = runtime.DebugTierOverride ?? tier;
            maxSlots = SkillSlotCapacity.GetActiveSlotRange(effectiveTier).max;
            tierLabel.text = $"Tier {effectiveTier}" + (runtime.DebugTierOverride.HasValue ? " (debug)" : "");
            unlockAllButton.text = runtime.DebugUnlockAllTrees ? "Unlock All: ON" : "Unlock All: OFF";

            for (int i = 0; i < WheelEquipSlotCount; i++)
            {
                SkillData skill = null;
                string guid = null;
                if (_skillDatabase != null && i < runtime.equippedSkillGuids.Count)
                {
                    guid = runtime.equippedSkillGuids[i];
                    _skillDatabase.TryGetByGuid(guid, out skill);
                }

                equipLabels[i].text = skill != null ? GetShortSkillLabel(skill) : string.Empty;
                equipSlots[i].EnableInClassList("skill-slot-locked", skill == null);
                equipSlots[i].EnableInClassList("skill-slot-tier-locked", i >= maxSlots);
                // The skill tree is the master color source (2026-08-09 follow-up — user: "the
                // skill tree be the master color... on the wheel should match", then "i want the
                // skill wheel in skill tree menu to sync up with the battle scene") — SkillTreeColor
                // is now the one shared color source for the web, this wheel, AND the battle
                // scene's own skill ring (BattleHUDController), keyed off the skill's TreeType.
                SkillTreeColor.ApplyVisual(equipSlots[i], skill?.TreeType);
            }

            // Every skill in the database — including the 5 Standard built-in moves, real
            // equippable SkillData now (2026-08 follow-up, see BuiltInMoveType) — flows through
            // here, no special-casing (see the class doc comment). Grouped by SkillTreeType, one
            // column per tree, rebuilt fresh on every refresh.
            var byTree = new Dictionary<SkillTreeType, List<(SkillData skill, string guid)>>();
            if (_skillDatabase != null)
            {
                foreach ((SkillData skill, string guid) in _skillDatabase.AllSkills)
                {
                    if (skill == null) continue;
                    if (!byTree.TryGetValue(skill.TreeType, out var list))
                    {
                        list = new List<(SkillData, string)>();
                        byTree[skill.TreeType] = list;
                    }
                    list.Add((skill, guid));
                }
            }

            IReadOnlyList<SkillTreeType> unlockedTrees = SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(runtime);

            webNodesLayer.Clear();
            var edgeColumns = new List<SkillWebEdgeVisual.ColumnEdges>();

            for (int c = 0; c < SkillTreeColor.DisplayOrder.Length; c++)
            {
                SkillTreeType treeType = SkillTreeColor.DisplayOrder[c];
                bool unlocked = treeType == SkillTreeType.Standard || unlockedTrees.Contains(treeType);
                Color treeColor = SkillTreeColor.GetByIndex(c);

                byTree.TryGetValue(treeType, out var skillsInTree);
                int count = skillsInTree?.Count ?? 0;
                var centers = new Vector2[count];

                for (int r = 0; r < count; r++)
                {
                    float x = WebLeftPadding + c * WebColumnSpacing;
                    float y = WebTopPadding + r * WebRowSpacing;
                    centers[r] = new Vector2(x + WebNodeSize / 2f, y + WebNodeSize / 2f);

                    (SkillData skill, string guid) = skillsInTree[r];

                    var node = new VisualElement();
                    node.AddToClassList("skill-slot-placeholder");
                    node.AddToClassList("skill-web-node");
                    node.style.position = Position.Absolute;
                    node.style.left = x;
                    node.style.top = y;

                    if (!unlocked)
                    {
                        // Tier-gated silhouette (mockup's "Sighted" state, driven by tier instead
                        // of true fog-of-war — see the class doc comment) — no label, no color, no
                        // interactivity at all.
                        node.AddToClassList("skill-slot-tier-locked");
                        webNodesLayer.Add(node);
                        continue;
                    }

                    SkillTreeColor.ApplyVisual(node, treeType);
                    node.EnableInClassList("skill-tray-icon-equipped", runtime.equippedSkillGuids.Contains(guid));

                    var label = new Label(GetShortSkillLabel(skill));
                    label.AddToClassList("move-option-label");
                    label.pickingMode = PickingMode.Ignore;
                    node.Add(label);

                    string capturedGuid = guid;
                    SkillData capturedSkill = skill;
                    SkillTreeType capturedTreeType = treeType;
                    node.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        if (evt.button != 0) return;
                        evt.StopPropagation(); // don't let this also start a webStage pan drag
                        BeginTrayDrag(capturedGuid, capturedTreeType, node, evt);
                    });
                    node.RegisterCallback<PointerEnterEvent>(evt => _tooltip.Show(BattleHUDController.BuildSkillTooltipText(capturedSkill), node));
                    node.RegisterCallback<PointerLeaveEvent>(evt => _tooltip.Hide());

                    webNodesLayer.Add(node);
                }

                edgeColumns.Add(new SkillWebEdgeVisual.ColumnEdges
                {
                    NodeCenters = centers,
                    TreeColor = treeColor,
                    Unlocked = unlocked,
                });
            }

            edgeOverlay.Columns = edgeColumns;
            edgeOverlay.Refresh();
        }

        tierPrevButton.clicked += () =>
        {
            runtime.DebugTierOverride = Mathf.Clamp((runtime.DebugTierOverride ?? tier) - 1, 1, 5);
            RefreshSkillArea();
            ApplyDefaultFraming(); // unlocked-column span can change with tier — reframe so the view isn't left centered on a now-stale subset
        };
        tierNextButton.clicked += () =>
        {
            runtime.DebugTierOverride = Mathf.Clamp((runtime.DebugTierOverride ?? tier) + 1, 1, 5);
            RefreshSkillArea();
            ApplyDefaultFraming();
        };
        unlockAllButton.clicked += () =>
        {
            runtime.DebugUnlockAllTrees = !runtime.DebugUnlockAllTrees;
            RefreshSkillArea();
            ApplyDefaultFraming();
        };

        void StartDragLine(VisualElement source, PointerDownEvent evt)
        {
            Vector2 startWorld = source.worldBound.center;
            dragLine.Start = dragLine.WorldToLocal(startWorld);
            dragLine.End = dragLine.WorldToLocal(evt.position);
            dragLine.style.display = DisplayStyle.Flex;
            dragLine.Refresh();
            _root.CapturePointer(evt.pointerId);
            _root.RegisterCallback<PointerMoveEvent>(OnDragMove);
            _root.RegisterCallback<PointerUpEvent>(OnDragUp);
        }

        void BeginRingDrag(int equipIndex, PointerDownEvent evt)
        {
            dragSourceEquipIndex = equipIndex;
            dragSourceTraySkillGuid = null;
            StartDragLine(equipSlots[equipIndex], evt);
        }

        void BeginTrayDrag(string skillGuid, SkillTreeType treeType, VisualElement source, PointerDownEvent evt)
        {
            dragSourceEquipIndex = -1;
            dragSourceTraySkillGuid = skillGuid;
            dragSourceTraySkillTree = treeType;
            StartDragLine(source, evt);
        }

        void OnDragMove(PointerMoveEvent evt)
        {
            dragLine.End = dragLine.WorldToLocal(evt.position);
            dragLine.Refresh();
        }

        void EndDragCleanup()
        {
            _root.UnregisterCallback<PointerMoveEvent>(OnDragMove);
            _root.UnregisterCallback<PointerUpEvent>(OnDragUp);
            dragLine.style.display = DisplayStyle.None;
            dragSourceEquipIndex = -1;
            dragSourceTraySkillGuid = null;
        }

        void OnDragUp(PointerUpEvent evt)
        {
            _root.ReleasePointer(evt.pointerId);
            int fromEquipIndex = dragSourceEquipIndex;
            string fromTraySkill = dragSourceTraySkillGuid;
            SkillTreeType fromTraySkillTree = dragSourceTraySkillTree;
            EndDragCleanup();

            for (int j = 0; j < maxSlots; j++)
            {
                if (!equipSlots[j].worldBound.Contains(evt.position)) continue;

                if (fromEquipIndex >= 0)
                {
                    // SwapEquipped (2026-08-09 follow-up — user: "it just adds it to the next open
                    // spot instead of where im dragging and dropping it to") now lands EXACTLY at
                    // the dropped position whether or not it was previously empty — equippedSkillGuids
                    // is sparse (empty-string gap entries), not compact/front-packed, so no special
                    // "moves to the end" fallback is needed for an empty target anymore.
                    if (fromEquipIndex != j) SkillLoadoutSystem.SwapEquipped(runtime, fromEquipIndex, j);
                }
                else if (fromTraySkill != null)
                {
                    // Dragging from the web equips a skill this creature may not have "learned"
                    // yet — learn it first so SkillLoadoutSystem's own "equip requires learned"
                    // contract stays intact (equipped is always a subset of learned, everywhere
                    // else in the codebase).
                    if (!runtime.learnedSkillGuids.Contains(fromTraySkill)) runtime.learnedSkillGuids.Add(fromTraySkill);
                    SkillLoadoutSystem.TryEquipAt(runtime, fromTraySkill, fromTraySkillTree, j, effectiveTier);
                }
                RefreshSkillArea();
                return;
            }
            // Released on nothing valid — cancel, nothing changes (only cancel path, same as battle's drag).
        }

        for (int physIndex = 0; physIndex < WheelSlotCount; physIndex++)
        {
            var slot = new VisualElement();
            slot.AddToClassList("skill-slot-placeholder");
            slot.style.position = Position.Absolute;
            PositionWheelSlot(slot, physIndex);
            ringArea.Add(slot);

            int equipIndex = physIndex - WheelEquipSlotOffset;
            equipSlots[equipIndex] = slot;

            var label = new Label();
            label.AddToClassList("move-option-label");
            label.pickingMode = PickingMode.Ignore;
            slot.Add(label);
            equipLabels[equipIndex] = label;

            int capturedEquipIndex = equipIndex;

            // equippedSkillGuids is sparse (2026-08-09 follow-up) — an index within Count can
            // still be an empty "" gap entry, so every "does this physical slot actually hold a
            // skill" check needs both bounds.
            bool HasEquippedSkill(int index) => index < runtime.equippedSkillGuids.Count
                && !string.IsNullOrEmpty(runtime.equippedSkillGuids[index]);

            void UnequipThisSlot()
            {
                if (!HasEquippedSkill(capturedEquipIndex)) return;
                SkillLoadoutSystem.Unequip(runtime, runtime.equippedSkillGuids[capturedEquipIndex]);
                RefreshSkillArea();
            }

            // Interactivity is gated at USE time against the current (mutable) maxSlots, not at
            // build time — the debug tier stepper can raise/lower maxSlots without this loop
            // re-running, so a slot that starts tier-locked must still become usable the moment
            // maxSlots grows past it, and vice versa.
            slot.RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (capturedEquipIndex >= maxSlots)
                {
                    _tooltip.Show(GetTierLockedTooltip(capturedEquipIndex), slot);
                    return;
                }
                if (_skillDatabase == null || !HasEquippedSkill(capturedEquipIndex)) return;
                if (_skillDatabase.TryGetByGuid(runtime.equippedSkillGuids[capturedEquipIndex], out SkillData skill))
                    _tooltip.Show(BattleHUDController.BuildSkillTooltipText(skill), slot);
            });
            slot.RegisterCallback<PointerLeaveEvent>(evt => _tooltip.Hide());

            slot.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (capturedEquipIndex >= maxSlots) return; // tier-locked right now — hover already explained why

                // Right-click unequip (checked directly on PointerDownEvent.button — 1 = right/
                // secondary button — rather than relying solely on ContextClickEvent, whose real-
                // mouse synthesis in a runtime UIDocument panel isn't confirmed reliable in this
                // project).
                if (evt.button == 1)
                {
                    evt.StopPropagation();
                    UnequipThisSlot();
                    return;
                }

                if (evt.button != 0) return;
                if (!HasEquippedSkill(capturedEquipIndex)) return; // empty slot — nothing to drag
                evt.StopPropagation();
                BeginRingDrag(capturedEquipIndex, evt);
            });
            // Kept as a secondary trigger alongside the PointerDownEvent check above — harmless
            // if it also fires, and covers any input path that only raises ContextClickEvent.
            slot.RegisterCallback<ContextClickEvent>(evt => { if (capturedEquipIndex < maxSlots) UnequipThisSlot(); });
        }

        RefreshSkillArea();
        ApplyDefaultFraming();
        return column;
    }

    /// <summary>Short orb/tray label for a SkillData-backed skill — see SkillLabelFormatter (2026-08-10 follow-up, shared with the battle scene's skill ring; was a private copy here until then).</summary>
    private string GetShortSkillLabel(SkillData skill) => SkillLabelFormatter.GetShortLabel(skill, _skillDatabase);

    /// <summary>Hover text for an equip slot beyond this creature's current EFFECTIVE tier cap (real tier, or the debug override when set) — explains why the drop fails instead of leaving it a silent no-op. Finds the lowest tier whose SkillSlotCapacity max exceeds this slot's index; T6+ (fusion) slots aren't resolvable per SkillSlotCapacity's own contract, so those fall back to a generic message.</summary>
    private static string GetTierLockedTooltip(int equipIndex)
    {
        for (int t = 1; t <= 5; t++)
        {
            if (SkillSlotCapacity.GetActiveSlotRange(t).max > equipIndex)
                return $"Locked\nRequires evolution tier {t}+";
        }
        return "Locked\nRequires a fusion evolution";
    }

    /// <summary>Positions a wheel slot at its fixed clock-hour position — identical math to BattleHUDController.PositionSkillSlots, so the wheel is a literal replica of battle's.</summary>
    private static void PositionWheelSlot(VisualElement slot, int physIndex)
    {
        float hour = physIndex + 1; // 0 -> 1 o'clock ... 11 -> 12 o'clock
        float angleDegrees = 90f - 30f * hour;
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float dx = Mathf.Cos(angleRadians) * WheelRadius;
        float dy = -Mathf.Sin(angleRadians) * WheelRadius;

        // .skill-slot-placeholder is a fixed 32x32 circle — self-centering offset in px, same
        // approach as BattleHUDController.PositionSkillSlots.
        slot.style.left = WheelCenter - 16f + dx;
        slot.style.top = WheelCenter - 16f + dy;
    }

    // --- Save tab ---

    private void BuildSaveTab()
    {
        _saveSlotContainer.Clear();
        for (int i = 0; i < SaveSystem.SlotCount; i++)
        {
            int slot = i;
            var card = new VisualElement();
            card.AddToClassList("save-slot-card");

            var title = new Label($"Slot {slot + 1}");
            title.AddToClassList("save-slot-title");
            card.Add(title);

            var timestamp = new Label(FormatSlotTimestamp(slot));
            timestamp.AddToClassList("save-slot-timestamp");
            card.Add(timestamp);

            var saveButton = new Button { text = "Save" };
            saveButton.AddToClassList("save-slot-button");
            saveButton.clicked += () =>
            {
                GameManager.Instance?.SaveToSlot(slot);
                timestamp.text = FormatSlotTimestamp(slot);
            };
            card.Add(saveButton);

            _saveSlotContainer.Add(card);
        }
    }

    private static string FormatSlotTimestamp(int slot)
    {
        DateTime? timestamp = SaveSystem.GetSlotTimestamp(slot);
        return timestamp.HasValue ? $"Saved: {timestamp.Value.ToLocalTime():yyyy-MM-dd HH:mm}" : "Empty";
    }
}
