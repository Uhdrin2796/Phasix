using System;
using System.Collections.Generic;
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
/// tree, TrayDisplayTreeOrder) instead of one flat wrapped list of the whole catalog — user: "when
/// click into the skill menu... everything is just listed out... id like them to be on their own
/// tree. we can do the finer details of what unlocks into what later but id like to be able to
/// separate them out already." Pure display grouping by each skill's EXISTING TreeType tag — no
/// new unlock/progression logic, real skill-tree content is still pending (CLAUDE.md).
///
/// Save tab: 3 slots, click to overwrite (SaveSystem.Save via GameManager.SaveToSlot) — save-only,
/// no explicit "load a different slot" UI (auto-continue from the newest slot happens on boot,
/// GameManager.TryAutoLoad).
///
/// Always-visible debug "New Game" button lives OUTSIDE the Tab-toggled menu root (DebugBar in
/// OverworldMenu.uxml) so it renders whether or not the menu is open — calls
/// GameManager.Instance.ResetToNewGame().
///
/// Text sizing floor (2026-08 follow-up — user: "make sure font size is similar to standard font
/// size like whats in the battle log"): every label/button in OverworldMenu.uss is >= 13px body /
/// >= 15px section header, matching BattleHUD.uss's .battle-log-entry/.battle-log-title.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class OverworldMenuController : MonoBehaviour
{
    [Header("Skill Resolution")]
    [Tooltip("Assign Assets/Data/Skills/SkillDatabase.asset — resolves equipped/learned skill guids to SkillData for the Party detail view's skill ring and tray.")]
    [SerializeField] private SkillDatabase _skillDatabase;

    // Skill-identity color palette — BattleHUD.uss defines .skill-ring-color-0..6. Unlike battle
    // (where the class is picked by ring POSITION), here it's picked by a stable hash of the
    // skill's own GUID, so a given skill always shows the same color everywhere it appears (ring
    // orb or tray icon), regardless of which slot it currently occupies.
    private static readonly string[] SkillColorClasses =
    {
        "skill-ring-color-0", "skill-ring-color-1", "skill-ring-color-2", "skill-ring-color-3",
        "skill-ring-color-4", "skill-ring-color-5", "skill-ring-color-6",
    };

    // 2026-08 follow-up #5 — user: "everything is just listed out... id like them to be on their
    // own tree." Display order for the tray's per-tree groups — Standard first (the 5 always-
    // available built-in moves, see BuiltInMoveType, are the closest thing to a baseline every
    // creature has), then the 18 GDD tree types in their PhasixEnums.cs declaration order. Groups
    // with zero unequipped skills for this creature are simply skipped (RefreshSkillArea), not
    // shown empty. This is a pure DISPLAY grouping by each skill's existing TreeType tag — which
    // specific skill unlocks into which other is still pending real skill-tree design (CLAUDE.md
    // "Actual skill content" pending item), not something this array decides.
    private static readonly SkillTreeType[] TrayDisplayTreeOrder =
    {
        SkillTreeType.Standard,
        SkillTreeType.Utility, SkillTreeType.Aura, SkillTreeType.Passive, SkillTreeType.Synergy,
        SkillTreeType.Reaction, SkillTreeType.Bond, SkillTreeType.Aspect, SkillTreeType.Resource,
        SkillTreeType.Corruption, SkillTreeType.Mirror, SkillTreeType.Evolve, SkillTreeType.Territory,
        SkillTreeType.Memory, SkillTreeType.Fusion, SkillTreeType.Personality, SkillTreeType.Typing,
        SkillTreeType.Bastion, SkillTreeType.Phantom,
    };

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

    // 2026-08 follow-up #6 — user: "make it so its a vertical tree that allows a player to swipe
    // left and right to see the tree. Similar to how skyrims skill tree mechanic is." TreeStage is
    // the visible viewport; TreePageWidth is deliberately narrower than TreeStageWidth so the
    // previous/next tree pages peek in at the edges (confirmed via AskUserQuestion: "show a tree
    // as a page, with the other adjacent trees slightly showing"). Matches OverworldMenu.uss's
    // .tree-stage/.tree-page px values — keep both in sync if either changes.
    private const float TreeStageWidth = 260f;
    private const float TreeStageHeight = 200f;
    private const float TreePageWidth = 210f;

    private VisualElement _root;
    private HudTooltip _tooltip;
    private bool _isOpen;

    // Set by BuildSkillArea while the Party detail view is showing a skill tree carousel, cleared
    // implicitly by the _partyDetailView.style.display guard in Update() once the player leaves
    // that view — see Update()'s own comment for why this is polled (Keyboard.current, matching
    // this file's existing Tab-key pattern) rather than a UI Toolkit KeyDownEvent registered on
    // the long-lived _root (which would need manual unregister bookkeeping across every ShowDetail
    // rebuild to avoid stacking duplicate handlers; polling sidesteps that entirely).
    private System.Action<int> _treePageStepAction;

    private Button _debugNewGameButton;
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

        // Left/Right arrow-key paging for the skill tree carousel (2026-08 follow-up #6) — only
        // while the Party detail view is actually the visible one (_partyDetailView.style.display
        // is toggled by ShowDetail/ShowRoster), so keys don't silently page an invisible/torn-down
        // tree while the menu is closed or a different tab/view is showing.
        if (_isOpen && _treePageStepAction != null && _partyDetailView.style.display == DisplayStyle.Flex && Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame) _treePageStepAction(-1);
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame) _treePageStepAction(1);
        }
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
    /// the class doc comment) plus a scrollable tray of the ENTIRE SkillDatabase catalog below it.
    /// All drag/drop/right-click state is local to this one call (closures over `runtime`/`tier`/
    /// the wheel element arrays) since only one detail view is ever open at a time — rebuilt fresh
    /// every ShowDetail call, same as the rest of this class.
    /// </summary>
    private VisualElement BuildSkillArea(PhasixRuntimeData runtime, int tier)
    {
        // The wheel always renders all 12 physical equip positions (replica of battle's own,
        // which does the same regardless of tier) but only `maxSlots` of them ever accept a
        // skill — SkillLoadoutSystem.TryEquipAt already enforces this internally, but before this
        // fix the UI gave no visual signal WHY a drop beyond the cap silently failed, reading as
        // broken rather than tier-gated (2026-08 follow-up — user: "i cant drag and drop the new
        // skills onto the open placeholders either, but i can swap them with the existing C1 and
        // C2"). Slots at equip-index >= maxSlots now get a distinct .skill-slot-tier-locked look,
        // no drag/right-click registration, and an explanatory hover tooltip instead. All 12
        // become reachable once a creature hits Tier 5 (SkillSlotCapacity.GetActiveSlotRange).
        int maxSlots = SkillSlotCapacity.GetActiveSlotRange(tier).max;

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

        // 2026-08 follow-up #6 — user: "make it so its a vertical tree that allows a player to
        // swipe left and right to see the tree. Similar to how skyrims skill tree mechanic is."
        // Replaces the flat grouped tray (follow-up #5) with a paged, per-SkillTreeType vertical
        // node column — one tree visible at a time, TrayDisplayTreeOrder's fixed order/count (19
        // pages, ALWAYS present even when a tree has 0 unequipped skills right now — confirmed via
        // AskUserQuestion: "show a tree as a page" — so the page count/order never shifts as the
        // player equips things). Node vertical order within a page is just the order skills of
        // that tree already appear in SkillDatabase.AllSkills — no prerequisite/branch data exists
        // yet (see TreeStageWidth's own comment + SkillData.cs), so this is a straight placeholder
        // chain, not real branching.
        var navHeader = new VisualElement();
        navHeader.AddToClassList("tree-nav-header");
        column.Add(navHeader);

        var prevPageButton = new Button { text = "◀" };
        prevPageButton.AddToClassList("tree-nav-button");
        navHeader.Add(prevPageButton);

        var treeNameLabel = new Label();
        treeNameLabel.AddToClassList("tree-nav-label");
        navHeader.Add(treeNameLabel);

        var treePageCountLabel = new Label();
        treePageCountLabel.AddToClassList("tree-nav-label");
        navHeader.Add(treePageCountLabel);

        var nextPageButton = new Button { text = "▶" };
        nextPageButton.AddToClassList("tree-nav-button");
        navHeader.Add(nextPageButton);

        var treeStage = new VisualElement();
        treeStage.AddToClassList("tree-stage");
        column.Add(treeStage);

        var treeStrip = new VisualElement();
        treeStrip.AddToClassList("tree-strip");
        treeStrip.style.width = TreePageWidth * TrayDisplayTreeOrder.Length;
        treeStage.Add(treeStrip);

        var treePages = new VisualElement[TrayDisplayTreeOrder.Length];
        for (int p = 0; p < TrayDisplayTreeOrder.Length; p++)
        {
            var page = new VisualElement();
            page.AddToClassList("tree-page");
            treeStrip.Add(page);
            treePages[p] = page;
        }

        var equipSlots = new VisualElement[WheelEquipSlotCount];
        var equipLabels = new Label[WheelEquipSlotCount];

        int dragSourceEquipIndex = -1;
        string dragSourceTraySkillGuid = null;
        int currentTreePageIndex = 0;

        void UpdateTreeNavLabels()
        {
            treeNameLabel.text = TrayDisplayTreeOrder[currentTreePageIndex].ToString();
            treePageCountLabel.text = $"{currentTreePageIndex + 1} / {TrayDisplayTreeOrder.Length}";
            prevPageButton.SetEnabled(currentTreePageIndex > 0);
            nextPageButton.SetEnabled(currentTreePageIndex < TrayDisplayTreeOrder.Length - 1);
        }

        void SetStripPosition(int index)
        {
            treeStrip.style.left = -(index * TreePageWidth) + (TreeStageWidth - TreePageWidth) / 2f;
        }

        // Shared by the nav buttons, arrow keys (Update()'s _treePageStepAction), and a committed
        // drag-swipe past its threshold — every paging input funnels through here.
        void PageTo(int index)
        {
            currentTreePageIndex = Mathf.Clamp(index, 0, TrayDisplayTreeOrder.Length - 1);
            UpdateTreeNavLabels();
            SetStripPosition(currentTreePageIndex);
        }

        void RefreshSkillArea()
        {
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
                ApplySkillColor(equipSlots[i], skill != null ? guid : null);
            }

            // Every skill in the database — including the 5 Standard built-in moves, real
            // equippable SkillData now (2026-08 follow-up, see BuiltInMoveType) — flows through
            // here, no special-casing (see the class doc comment). Grouped by SkillTreeType, one
            // page per tree, rebuilt fresh on every refresh (same "clear and rebuild" pattern the
            // flat tray used) — currentTreePageIndex is NOT reset here, so equipping/unequipping a
            // skill re-centers on whatever page the player was already viewing.
            //
            // 2026-08 follow-up #7 — user: "when attaching a skill from a tree into the skill
            // wheel. It shouldnt disappear from the tree, it should act more as a copy from the
            // tree assuming its unlocked." Equipped skills are NO LONGER filtered out of their
            // tree page — every skill in the database always shows here, dimmed/outlined
            // (.skill-tray-icon-equipped) if currently equipped. Dragging an already-equipped
            // node still just no-ops on drop — SkillLoadoutSystem.TryEquipAt/TryEquip already
            // refuse to equip a skill that's already in equippedSkillGuids, so a skill can never
            // occupy two wheel slots at once even though its tree "copy" stays visible.
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

            for (int p = 0; p < TrayDisplayTreeOrder.Length; p++)
            {
                VisualElement page = treePages[p];
                page.Clear();
                byTree.TryGetValue(TrayDisplayTreeOrder[p], out var skillsInTree);

                if (skillsInTree == null || skillsInTree.Count == 0)
                {
                    var emptyLabel = new Label("No skills available to equip");
                    emptyLabel.AddToClassList("tree-page-empty-label");
                    page.Add(emptyLabel);
                    continue;
                }

                var nodeColumn = new ScrollView(ScrollViewMode.Vertical);
                nodeColumn.AddToClassList("tree-node-column");
                page.Add(nodeColumn);

                for (int i = 0; i < skillsInTree.Count; i++)
                {
                    (SkillData skill, string guid) = skillsInTree[i];

                    if (i > 0)
                    {
                        var connector = new VisualElement();
                        connector.AddToClassList("tree-node-connector");
                        nodeColumn.Add(connector);
                    }

                    var icon = new VisualElement();
                    icon.AddToClassList("skill-slot-placeholder");
                    icon.AddToClassList("skill-tray-icon");
                    icon.style.position = Position.Relative; // overrides .skill-slot-placeholder's absolute positioning — inline always wins, see OverworldMenu.uss's note
                    icon.EnableInClassList("skill-tray-icon-equipped", runtime.equippedSkillGuids.Contains(guid));
                    ApplySkillColor(icon, guid);

                    var label = new Label(GetShortSkillLabel(skill));
                    label.AddToClassList("move-option-label");
                    label.pickingMode = PickingMode.Ignore;
                    icon.Add(label);

                    string capturedGuid = guid;
                    SkillData capturedSkill = skill;
                    int capturedPageIndex = p;
                    icon.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        if (evt.button != 0) return;
                        // Peeking neighbor-page slivers are inert for equip purposes even though
                        // they're technically in the DOM (see the class doc comment's node-gating
                        // note) — deliberately does NOT StopPropagation here, so a press starting
                        // on a non-current page's node still bubbles up to treeStage's swipe-drag
                        // handler below instead of silently doing nothing.
                        if (capturedPageIndex != currentTreePageIndex) return;
                        evt.StopPropagation();
                        BeginTrayDrag(capturedGuid, icon, evt);
                    });
                    icon.RegisterCallback<PointerEnterEvent>(evt => _tooltip.Show(BattleHUDController.BuildSkillTooltipText(capturedSkill), icon));
                    icon.RegisterCallback<PointerLeaveEvent>(evt => _tooltip.Hide());

                    nodeColumn.Add(icon);
                }
            }

            UpdateTreeNavLabels();
            SetStripPosition(currentTreePageIndex);
        }

        prevPageButton.clicked += () => PageTo(currentTreePageIndex - 1);
        nextPageButton.clicked += () => PageTo(currentTreePageIndex + 1);
        _treePageStepAction = delta => PageTo(currentTreePageIndex + delta);

        // Real drag-swipe (confirmed via AskUserQuestion — "Buttons + keys + real drag gesture").
        // Registered on treeStage (the viewport), not individual pages/nodes — a press that starts
        // on a node either StopPropagation's (current-page node, begins an equip-drag instead) or
        // bubbles here unhandled (peeking-page node, see that handler's own comment), so this and
        // the equip-drag path are mutually exclusive by construction, no extra disambiguation flag
        // needed. transitionDuration is zeroed for the live 1:1 drag-follow and restored
        // (StyleKeyword.Null → falls back to .tree-strip's USS transition) so the commit/snap-back
        // on release animates smoothly.
        float dragStartPointerX = 0f;
        float dragStartStripLeft = 0f;
        bool isDraggingStrip = false;

        treeStage.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;
            isDraggingStrip = true;
            dragStartPointerX = evt.position.x;
            dragStartStripLeft = treeStrip.resolvedStyle.left;
            treeStrip.style.transitionDuration = new List<TimeValue> { new TimeValue(0f) };
            treeStage.CapturePointer(evt.pointerId);
        });
        treeStage.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!isDraggingStrip) return;
            treeStrip.style.left = dragStartStripLeft + (evt.position.x - dragStartPointerX);
        });
        treeStage.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!isDraggingStrip) return;
            isDraggingStrip = false;
            treeStage.ReleasePointer(evt.pointerId);
            treeStrip.style.transitionDuration = StyleKeyword.Null;

            float totalDelta = evt.position.x - dragStartPointerX;
            float swipeThreshold = TreePageWidth / 4f;
            if (totalDelta <= -swipeThreshold) PageTo(currentTreePageIndex + 1);      // dragged left -> next page
            else if (totalDelta >= swipeThreshold) PageTo(currentTreePageIndex - 1); // dragged right -> previous page
            else PageTo(currentTreePageIndex);                                       // under threshold -> snap back
        });

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

        void BeginTrayDrag(string skillGuid, VisualElement source, PointerDownEvent evt)
        {
            dragSourceEquipIndex = -1;
            dragSourceTraySkillGuid = skillGuid;
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
            EndDragCleanup();

            for (int j = 0; j < maxSlots; j++)
            {
                if (!equipSlots[j].worldBound.Contains(evt.position)) continue;

                if (fromEquipIndex >= 0)
                {
                    if (fromEquipIndex != j)
                    {
                        if (j < runtime.equippedSkillGuids.Count)
                        {
                            SkillLoadoutSystem.SwapEquipped(runtime, fromEquipIndex, j);
                        }
                        else
                        {
                            // Target is empty (within cap, no skill to swap with) — SwapEquipped
                            // requires BOTH indices to already hold a skill, so it silently no-ops
                            // here otherwise (2026-08 follow-up bugfix — user: "i still cant drag
                            // and drop skills into the open slots that dont have any skills
                            // equipped... i can only drag and drop into slots that already have
                            // skills on them"). equippedSkillGuids is a compact, front-packed
                            // list — there's no real "slot 4" independent of the list's current
                            // length — so dropping onto any empty position moves the dragged
                            // skill to the end of that compact block instead, giving a visible,
                            // predictable result rather than a silent no-op.
                            string movingGuid = runtime.equippedSkillGuids[fromEquipIndex];
                            runtime.equippedSkillGuids.RemoveAt(fromEquipIndex);
                            runtime.equippedSkillGuids.Add(movingGuid);
                        }
                    }
                }
                else if (fromTraySkill != null)
                {
                    // Dragging from the full-catalog tray equips a skill this creature may not
                    // have "learned" yet — learn it first so SkillLoadoutSystem's own "equip
                    // requires learned" contract stays intact (equipped is always a subset of
                    // learned, everywhere else in the codebase).
                    if (!runtime.learnedSkillGuids.Contains(fromTraySkill)) runtime.learnedSkillGuids.Add(fromTraySkill);
                    SkillLoadoutSystem.TryEquipAt(runtime, fromTraySkill, j, tier);
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

            // Every physical position is a real equip slot now (WheelEquipSlotOffset/Count span
            // the full WheelSlotCount — see those constants' own comments); positions beyond this
            // creature's CURRENT tier cap are handled by the maxSlots check just below instead of
            // a separate "permanently decorative" branch.
            int equipIndex = physIndex - WheelEquipSlotOffset;
            equipSlots[equipIndex] = slot;

            var label = new Label();
            label.AddToClassList("move-option-label");
            label.pickingMode = PickingMode.Ignore;
            slot.Add(label);
            equipLabels[equipIndex] = label;

            int capturedEquipIndex = equipIndex;

            if (capturedEquipIndex >= maxSlots)
            {
                // Beyond this creature's tier cap — permanently inert, not a drag source or a
                // valid drop target (see OnDragUp's `j < maxSlots` bound). Hover explains why,
                // rather than the drop just silently failing.
                string lockedTooltip = GetTierLockedTooltip(capturedEquipIndex);
                slot.RegisterCallback<PointerEnterEvent>(evt => _tooltip.Show(lockedTooltip, slot));
                slot.RegisterCallback<PointerLeaveEvent>(evt => _tooltip.Hide());
                continue;
            }

            void UnequipThisSlot()
            {
                if (capturedEquipIndex >= runtime.equippedSkillGuids.Count) return;
                SkillLoadoutSystem.Unequip(runtime, runtime.equippedSkillGuids[capturedEquipIndex]);
                RefreshSkillArea();
            }

            slot.RegisterCallback<PointerDownEvent>(evt =>
            {
                // Right-click unequip (2026-08 follow-up bugfix — user: "The right click unequip
                // from the menu also doesnt work"). Checked directly on PointerDownEvent.button
                // (1 = right/secondary button) rather than relying solely on UI Toolkit's
                // ContextClickEvent, whose real-mouse synthesis in a runtime UIDocument panel
                // isn't confirmed reliable in this project (VisualElement.tooltip had an
                // analogous Editor-only gap this same session) — PointerDownEvent is already
                // confirmed working for real mouse input via the left-click drag path below.
                if (evt.button == 1)
                {
                    evt.StopPropagation();
                    UnequipThisSlot();
                    return;
                }

                if (evt.button != 0) return;
                if (capturedEquipIndex >= runtime.equippedSkillGuids.Count) return; // empty slot — nothing to drag
                evt.StopPropagation();
                BeginRingDrag(capturedEquipIndex, evt);
            });
            // Kept as a secondary trigger alongside the PointerDownEvent check above — harmless
            // if it also fires, and covers any input path that only raises ContextClickEvent.
            slot.RegisterCallback<ContextClickEvent>(evt => UnequipThisSlot());
            slot.RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (_skillDatabase == null || capturedEquipIndex >= runtime.equippedSkillGuids.Count) return;
                if (_skillDatabase.TryGetByGuid(runtime.equippedSkillGuids[capturedEquipIndex], out SkillData skill))
                    _tooltip.Show(BattleHUDController.BuildSkillTooltipText(skill), slot);
            });
            slot.RegisterCallback<PointerLeaveEvent>(evt => _tooltip.Hide());
        }

        RefreshSkillArea();
        return column;
    }

    /// <summary>Removes any existing skill-identity color class, then applies the one for skillGuid (or leaves the element uncolored if null — an empty/locked slot).</summary>
    private static void ApplySkillColor(VisualElement element, string skillGuid)
    {
        foreach (string c in SkillColorClasses) element.RemoveFromClassList(c);
        if (skillGuid != null) element.AddToClassList(GetSkillColorClass(skillGuid));
    }

    /// <summary>Deterministic skill -> color mapping (stable hash of the GUID) so a given skill always shows the same color everywhere it appears, regardless of which slot it occupies.</summary>
    private static string GetSkillColorClass(string skillGuid)
    {
        int hash = skillGuid.GetHashCode();
        int index = ((hash % SkillColorClasses.Length) + SkillColorClasses.Length) % SkillColorClasses.Length;
        return SkillColorClasses[index];
    }

    /// <summary>
    /// Short orb/tray label for a SkillData-backed skill (2026-08 follow-up — user: "It should
    /// only show their icon maxium a letter and a number... they should all have the hover over
    /// description similar to the in battle game"). An already-short real name (e.g. "C1"/"C2",
    /// the 2 skills renamed earlier this session) passes through as-is; every other placeholder
    /// gets a generated `{tree-initial}{index-within-tree}` code — a pure display transform, never
    /// written back to the asset. Full identity/mechanics still live in the hover tooltip
    /// (BattleHUDController.BuildSkillTooltipText), unchanged.
    /// </summary>
    /// <summary>Hover text for an equip slot beyond this creature's current tier cap — explains why the drop fails instead of leaving it a silent no-op (see BuildSkillArea's own doc comment). Finds the lowest tier whose SkillSlotCapacity max exceeds this slot's index; T6+ (fusion) slots aren't resolvable per SkillSlotCapacity's own contract, so those fall back to a generic message.</summary>
    private static string GetTierLockedTooltip(int equipIndex)
    {
        for (int t = 1; t <= 5; t++)
        {
            if (SkillSlotCapacity.GetActiveSlotRange(t).max > equipIndex)
                return $"Locked\nRequires evolution tier {t}+";
        }
        return "Locked\nRequires a fusion evolution";
    }

    private string GetShortSkillLabel(SkillData skill)
    {
        if (skill.SkillName.Length <= 3) return skill.SkillName;

        char treeInitial = char.ToUpperInvariant(skill.TreeType.ToString()[0]);

        // 'C' is reserved for hand-authored short names (C1/C2, see this method's own doc comment)
        // — Corruption is the only tree whose own initial is 'C', and without this override its
        // first two skills would generate "C1"/"C2" too, colliding with the real C1/C2 (live-
        // verified: Corruption_Placeholder1 rendered identically to the real C1, which
        // ComboRuleEvaluator's RepeatSameSkill rule specifically references — not just a cosmetic
        // clash, a genuinely different skill wearing the real one's label).
        if (treeInitial == 'C') treeInitial = 'X';

        int indexInTree = 1;
        if (_skillDatabase != null)
        {
            var treeSkills = _skillDatabase.GetByTreeType(skill.TreeType);
            for (int i = 0; i < treeSkills.Count; i++)
            {
                if (treeSkills[i] == skill) { indexInTree = i + 1; break; }
            }
        }

        return $"{treeInitial}{indexInTree}";
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
