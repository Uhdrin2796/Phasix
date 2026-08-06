using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HP/Aura bars, name plates, placeholder stage creatures, Sonny 2-style radial move selection +
/// drag-to-target, the shared converging-ring action-command timing visual (offense on the
/// targeted enemy, defense on the defending player creature), a fully auto-paced beat message
/// (ShowTimedMessage — no click-to-proceed gate anywhere in the battle as of 2026-08-06, see
/// DECISIONS.md -> [Combat]), and a scrolling text battle log for BattleScene_Main. MonoBehaviour
/// singleton wrapping a UIDocument,
/// matching EncounterPromptController's convention (see DECISIONS.md -> [UI] for why UI Toolkit
/// over uGUI). Fixed at 3 player slots (BattleConfig.ActivePartySize) and 1 enemy slot —
/// multi-enemy battles (trainer fights, Roadmap_v2 Mo 14-15) aren't built yet.
///
/// Layout is Sonny 2-style per user reference: top HP/Aura list per side, middle stage with
/// placeholder creature circles (player left, enemy right, same lane), bottom action bar — no
/// visible lane lines (Combat_Directive_v0_1_0.md stage art/lane visuals remain "pending art
/// direction"; see BattleStageGizmos.cs for the Scene-view-only dev visualization instead). This
/// is a flat screen-space overlay on top of the frozen overworld, not real diorama art.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class BattleHUDController : MonoBehaviour
{
    /// <summary>Result of RunDefenseTimedInput — which live click (if any) landed in its zone.</summary>
    public enum DefenseOutcome { Miss, Dodge, Parry }


    // Converging-ring sizing (px) — fixed reference target ring, and the marker ring's start/end
    // radii as it shrinks past it. Placeholder visual sizing, not gameplay-tuned.
    private const float RingTargetRadius = 30f;
    private const float RingMarkerStartRadius = 60f;
    private const float RingMarkerMinRadius = 2f;

    // Marker flash colors on click resolution (2026-08-05, user-directed — see DECISIONS.md ->
    // [Combat]), reworked same day from per-move colors (Dodge=orange/Parry=green) to
    // per-OUTCOME-QUALITY colors shared by Dodge/Parry/offense alike: green for a normal success,
    // a bright neon purple for a "perfect" (a tighter sub-tolerance around dead-center — see
    // PerfectToleranceFraction), red for any Miss (wrong tolerance, wrong button, or a timeout
    // with no click at all). White is the marker's normal/in-flight color.
    private static readonly Color SuccessFlashColor = new Color(90f / 255f, 200f / 255f, 100f / 255f);
    private static readonly Color PerfectFlashColor = new Color(176f / 255f, 38f / 255f, 255f / 255f);
    private static readonly Color MissFlashColor = new Color(220f / 255f, 60f / 255f, 60f / 255f);

    /// <summary>A hit counts as "perfect" when the marker/target ratio deviation is within this fraction of the full tolerance half-width — e.g. 0.2 means the innermost 20% of the success window. Placeholder, not tuned.</summary>
    private const float PerfectToleranceFraction = 0.2f;

    // Sonny 2-style radial move option layout (2026-08-05, user-directed — see DECISIONS.md ->
    // [Combat]): fixed clock-face positions around the acting creature, further out than the
    // creature's own radius for a "floating orb" feel. Clock hour -> standard math degrees:
    // 12 o'clock = 90° (straight up), each hour = 30° clockwise, so hour h -> 90 - 30*h.
    private const int MoveOptionsPerSlot = 5;
    private const float MoveOptionRadius = 95f; // px from the creature's center to each option's center

    /// <summary>Clock-hour position for each move option, index-matched to MoveOptionsPerSlot — "A"=1, "C"=11 per the user's original ask; "H"=12 and "R"=2 (2026-08-06 additions); "K" (Capture, Phase 3 Gate wiring)=3.</summary>
    private static readonly float[] MoveOptionClockHours = { 1f, 11f, 12f, 2f, 3f };

    /// <summary>
    /// Index-matched to MoveOptionsPerSlot/MoveOptionClockHours — false means the move drops onto
    /// the ENEMY (Attack, Capture), true means it's solo/self-only and only drops onto the
    /// CASTER's own creature (Charge/Heal/Regen — "this one happens to only make it so you can
    /// select the character that is casting it", 2026-08-06 user-directed, see DECISIONS.md ->
    /// [Combat]). Drives OnDragPointerUp's hit-test target per move without a per-move enum/
    /// switch — adding a new self-only move is just one more `true` here plus one more callback
    /// slot; Capture reuses the SAME `false` (enemy-target) path Attack already has, no new logic.
    /// </summary>
    private static readonly bool[] MoveOptionIsSelfOnly = { false, true, true, true, false };

    // Empty skill slot ring (2026-08-06, user-directed — see DECISIONS.md -> [Combat]): 12 dark
    // grey placeholder circles around the acting creature, one per clock hour (1-12) — future
    // homes for real skills once the skill tree framework has content (CLAUDE.md "What Is
    // Pending": "Actual skill content (§14) — taxonomy locked, individual skills pending").
    // Purely visual for now — no click handler, no functionality. Deliberately the SAME radius
    // AND same size as "A"/"C" (MoveOptionRadius, .move-option-placeholder's 32x32) — user's
    // model: these 12 circles ARE the slots, and "A"/"C" are two skills already slotted into the
    // 1/11 o'clock positions, not smaller decorations peeking out from behind a bigger orb.
    // UXML orders the skill slots BEFORE the move options so "A"/"C" paint on top and fully
    // cover the grey slot underneath at those two positions. Shown/hidden in lockstep with the
    // move options (same SetMoveOptionsVisible call) so the whole radial wheel appears/
    // disappears together.
    private const int SkillSlotCount = 12;
    private const float SkillSlotRadius = MoveOptionRadius; // same path as "A"/"C", not a separate ring

    public static BattleHUDController Instance { get; private set; }

    private VisualElement _root;
    private VisualElement _stage;

    private VisualElement _actionAnnouncement;
    private Label _actionAnnouncementLabel;

    private VisualElement _continuePrompt;
    private Label _continuePromptLabel;

    private ScrollView _battleLogScrollView;
    private VisualElement _battleLogContent;

    /// <summary>Result of the most recently completed RunTimedInput call. Valid once that coroutine finishes.</summary>
    public bool LastTimedInputSuccess { get; private set; }

    /// <summary>True if the most recently completed RunTimedInput hit was a "perfect" (see PerfectToleranceFraction). Always false when LastTimedInputSuccess is false. Not wired to any bonus yet — visual feedback only for now.</summary>
    public bool LastTimedInputWasPerfect { get; private set; }

    /// <summary>Result of the most recently completed RunDefenseTimedInput call. Valid once that coroutine finishes.</summary>
    public DefenseOutcome LastDefenseOutcome { get; private set; }

    /// <summary>True if the most recently completed RunDefenseTimedInput hit was a "perfect" (see PerfectToleranceFraction). Always false when LastDefenseOutcome is Miss. Not wired to any bonus yet — visual feedback only for now.</summary>
    public bool LastDefenseWasPerfect { get; private set; }

    private readonly VisualElement[] _playerSlots = new VisualElement[BattleConfig.ActivePartySize];
    private readonly Label[] _playerNameLabels = new Label[BattleConfig.ActivePartySize];
    private readonly VisualElement[] _playerHPFills = new VisualElement[BattleConfig.ActivePartySize];
    private readonly Label[] _playerHPLabels = new Label[BattleConfig.ActivePartySize];
    private readonly VisualElement[] _playerAuraFills = new VisualElement[BattleConfig.ActivePartySize];
    private readonly Label[] _playerAuraLabels = new Label[BattleConfig.ActivePartySize];
    private readonly VisualElement[] _playerStageCreatures = new VisualElement[BattleConfig.ActivePartySize];

    /// <summary>[slotIndex][optionIndex] — MoveOptionsPerSlot placeholders per party member, radially positioned above their stage creature.</summary>
    private readonly VisualElement[][] _playerMoveOptions = new VisualElement[BattleConfig.ActivePartySize][];

    /// <summary>[slotIndex][skillSlotIndex] — SkillSlotCount empty placeholder circles per party member, one per clock hour. Purely visual until real skill content exists.</summary>
    private readonly VisualElement[][] _playerSkillSlots = new VisualElement[BattleConfig.ActivePartySize][];

    // Evolution Burst gauge bar (2026-08-06, user-directed — see DECISIONS.md -> [Combat]): a
    // visible purple fill bar directly under the Aura bar (order is now HP -> Aura -> Burst ->
    // status-icon row), instead of the gauge being invisible state the player could only infer
    // from the status icon appearing after an automatic trigger. Clickable at ALL times (not
    // gated to the owner's turn) — BurstBarClicked fires unconditionally on click; the "did this
    // actually do anything" gate lives in EvolutionBurstSystem.ActivateReady itself (only
    // succeeds once FillPercent reaches TriggerThreshold), not here. The back element gets
    // `.burst-bar-ready` (a yellow border) once SetBurstFillBar reports ready, matching
    // ActivateReady's own guard exactly (both read EvolutionBurstSystem.TriggerThreshold) so the
    // visual "clickable now" signal never lies about what a click will actually do.
    private readonly VisualElement[] _playerBurstFills = new VisualElement[BattleConfig.ActivePartySize];
    private readonly VisualElement[] _playerBurstBacks = new VisualElement[BattleConfig.ActivePartySize];

    /// <summary>Fires with the clicked player slot index whenever a Burst gauge bar is pressed — BattleManager subscribes and calls EvolutionBurstSystem.ActivateReady, which silently no-ops if that slot's gauge isn't actually full yet.</summary>
    public event Action<int> BurstBarClicked;

    // Status bar (2026-08-06, user-directed — see DECISIONS.md -> [Combat]): a small row under
    // each player's Burst bar (shifted down from directly under Aura once the Burst bar was
    // added — see above) showing active buff/debuff icons, mini versions of their move orb
    // (Regen -> a smaller "R", same purple). Hidden until the effect is actually active. The
    // subscript turn-counter sits OUTSIDE the icon's own frame at bottom-right for an END-of-turn
    // effect (Regen ticks at the end of the player's turn) — bottom-left is reserved for a future
    // START-of-turn effect, so the corner alone tells you which phase a status resolves in
    // without reading the icon itself.
    private readonly VisualElement[] _playerRegenIcons = new VisualElement[BattleConfig.ActivePartySize];
    private readonly Label[] _playerRegenCounters = new Label[BattleConfig.ActivePartySize];

    /// <summary>Mini "B" (Evolution Burst, orange) status icon + END-of-turn countdown — shown once ActivateReady succeeds (the "you're in an evolved state" indicator), same pattern as the Regen icons above.</summary>
    private readonly VisualElement[] _playerBurstIcons = new VisualElement[BattleConfig.ActivePartySize];
    private readonly Label[] _playerBurstCounters = new Label[BattleConfig.ActivePartySize];

    private VisualElement _enemySlot;
    private Label _enemyNameLabel;
    private VisualElement _enemyHPFill;
    private Label _enemyHPLabel;
    private VisualElement _enemyAuraFill;
    private Label _enemyAuraLabel;
    private VisualElement _enemyStageCreature;

    // Sonny 2-style click-and-drag move/target selection (2026-08-05/06, user-directed — see
    // DECISIONS.md -> [Combat]). ShowMoveSelection shows the acting player's move placeholders;
    // pressing one starts a drag (DragLineVisual follows the cursor) that resolves against
    // whichever targets are valid for that move on release — enemies for "A", the caster's own
    // creature only for "C"/"H"/"R" (MoveOptionIsSelfOnly). Releasing anywhere invalid cancels
    // back to the placeholders (OnDragPointerUp's else branch) — the only cancel path that
    // exists today, same for every move. A single onMoveConfirmed(optionIndex, target) callback
    // (rather than one Action per move) is what lets a new move option get added without
    // widening ShowMoveSelection's signature again — BattleManager switches on optionIndex.
    private DragLineVisual _dragLine;
    private BattleParticipant _self; // the acting participant — the only valid target for self-only moves
    private List<BattleParticipant> _enemyTargets; // valid targets for Attack
    private Action<int, BattleParticipant> _onMoveConfirmed;
    private int _draggingFromSlotIndex = -1;
    private int _draggingOptionIndex = -1;

    // Shared converging-ring timing visual (2026-08-05, user-directed — see DECISIONS.md ->
    // [Combat]): reparented per use — above the targeted enemy for RunTimedInput (offense), above
    // the defending player creature for RunDefenseTimedInput. Never both at once (PlayerTurn and
    // EnemyTurn don't run concurrently), so one shared instance is enough.
    private RingVisual _timingRing;

    private void Awake()
    {
        Instance = this;

        var document = GetComponent<UIDocument>();
        _root = document.rootVisualElement.Q<VisualElement>("BattleHUDRoot");
        _stage = _root.Q<VisualElement>("Stage");

        for (int i = 0; i < BattleConfig.ActivePartySize; i++)
        {
            _playerSlots[i] = _root.Q<VisualElement>($"PlayerSlot{i}");
            _playerNameLabels[i] = _root.Q<Label>($"PlayerSlot{i}_Name");
            _playerHPFills[i] = _root.Q<VisualElement>($"PlayerSlot{i}_HPFill");
            _playerHPLabels[i] = _root.Q<Label>($"PlayerSlot{i}_HPText");
            _playerAuraFills[i] = _root.Q<VisualElement>($"PlayerSlot{i}_AuraFill");
            _playerAuraLabels[i] = _root.Q<Label>($"PlayerSlot{i}_AuraText");
            _playerStageCreatures[i] = _root.Q<VisualElement>($"PlayerStageSlot{i}");

            _playerBurstFills[i] = _root.Q<VisualElement>($"PlayerSlot{i}_BurstFill");
            _playerBurstBacks[i] = _root.Q<VisualElement>($"PlayerSlot{i}_BurstBack");
            _playerBurstFills[i].style.width = Length.Percent(0f);
            int capturedBurstSlotIndex = i;
            _playerBurstBacks[i].RegisterCallback<PointerDownEvent>(evt => BurstBarClicked?.Invoke(capturedBurstSlotIndex));

            _playerRegenIcons[i] = _root.Q<VisualElement>($"PlayerSlot{i}_StatusIcon_Regen");
            _playerRegenCounters[i] = _root.Q<Label>($"PlayerSlot{i}_StatusIcon_Regen_Counter");
            _playerRegenIcons[i].style.display = DisplayStyle.None;

            _playerBurstIcons[i] = _root.Q<VisualElement>($"PlayerSlot{i}_StatusIcon_Burst");
            _playerBurstCounters[i] = _root.Q<Label>($"PlayerSlot{i}_StatusIcon_Burst_Counter");
            _playerBurstIcons[i].style.display = DisplayStyle.None;

            _playerMoveOptions[i] = new VisualElement[MoveOptionsPerSlot];
            for (int j = 0; j < MoveOptionsPerSlot; j++)
            {
                VisualElement option = _root.Q<VisualElement>($"PlayerStageSlot{i}_MoveOption{j}");
                _playerMoveOptions[i][j] = option;

                int capturedSlotIndex = i; // avoid closing over the loop variable
                int capturedOptionIndex = j;
                // All move options use the same click-and-drag gesture (BeginDrag) — only the
                // valid drop target differs per move (MoveOptionIsSelfOnly), resolved in
                // OnDragPointerUp. Adding a new move is just extending the arrays above plus one
                // more UXML element, no new registration branch needed here.
                option.RegisterCallback<PointerDownEvent>(evt => BeginDrag(capturedSlotIndex, evt, capturedOptionIndex));
                option.style.display = DisplayStyle.None;
            }
            PositionMoveOptions(_playerMoveOptions[i]);

            _playerSkillSlots[i] = new VisualElement[SkillSlotCount];
            for (int k = 0; k < SkillSlotCount; k++)
            {
                VisualElement slot = _root.Q<VisualElement>($"PlayerStageSlot{i}_SkillSlot{k}");
                _playerSkillSlots[i][k] = slot;
                slot.style.display = DisplayStyle.None;
            }
            PositionSkillSlots(_playerSkillSlots[i]);
        }

        _enemySlot = _root.Q<VisualElement>("EnemySlot0");
        _enemyNameLabel = _root.Q<Label>("EnemySlot0_Name");
        _enemyHPFill = _root.Q<VisualElement>("EnemySlot0_HPFill");
        _enemyHPLabel = _root.Q<Label>("EnemySlot0_HPText");
        _enemyAuraFill = _root.Q<VisualElement>("EnemySlot0_AuraFill");
        _enemyAuraLabel = _root.Q<Label>("EnemySlot0_AuraText");
        _enemyStageCreature = _root.Q<VisualElement>("EnemyStageSlot0");

        _actionAnnouncement = _root.Q<VisualElement>("ActionAnnouncement");
        _actionAnnouncementLabel = _root.Q<Label>("ActionAnnouncementLabel");

        _continuePrompt = _root.Q<VisualElement>("ContinuePrompt");
        _continuePromptLabel = _root.Q<Label>("ContinuePromptLabel");

        _battleLogScrollView = _root.Q<ScrollView>("BattleLogScrollView");
        _battleLogContent = _root.Q<VisualElement>("BattleLogContent");

        _dragLine = new DragLineVisual { style = { display = DisplayStyle.None } };
        _stage.Add(_dragLine);

        _timingRing = new RingVisual();
        _timingRing.AddToClassList("timing-ring");
        _timingRing.style.display = DisplayStyle.None;

        _actionAnnouncement.style.display = DisplayStyle.None;
        _continuePrompt.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Populates name plates, initial HP/Aura bars, and stage creature colors. Hides player slots
    /// beyond playerSide.Count. Aura bars are set here AND kept live by RefreshBars — attacks
    /// spend Aura and a perfect Dodge/Parry restores it (2026-08-05, user-directed — see
    /// DECISIONS.md -> [Combat]).
    /// </summary>
    public void Initialize(List<BattleParticipant> playerSide, List<BattleParticipant> enemySide)
    {
        for (int i = 0; i < BattleConfig.ActivePartySize; i++)
        {
            bool hasSlot = i < playerSide.Count;
            _playerSlots[i].style.display = hasSlot ? DisplayStyle.Flex : DisplayStyle.None;
            _playerStageCreatures[i].style.display = hasSlot ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasSlot) continue;

            BattleParticipant p = playerSide[i];
            _playerNameLabels[i].text = p.DisplayName;
            SetAuraFill(_playerAuraFills[i], _playerAuraLabels[i], p);
            SetStageCreatureColor(_playerStageCreatures[i], p);
            SetBurstFillBar(i, p.BurstGauge.FillPercent, ready: false);
        }

        // Single enemy slot only — see class doc comment.
        bool hasEnemy = enemySide.Count > 0;
        _enemySlot.style.display = hasEnemy ? DisplayStyle.Flex : DisplayStyle.None;
        _enemyStageCreature.style.display = hasEnemy ? DisplayStyle.Flex : DisplayStyle.None;

        if (hasEnemy)
        {
            BattleParticipant enemy = enemySide[0];
            _enemyNameLabel.text = enemy.DisplayName;
            SetAuraFill(_enemyAuraFill, _enemyAuraLabel, enemy);
            SetStageCreatureColor(_enemyStageCreature, enemy);
        }

        RefreshBars(playerSide, enemySide);
    }

    /// <summary>Refreshes every HP and Aura bar's fill width/numeric readout and fades the stage creature circle once a participant is down. Renamed from RefreshHP 2026-08-05 once Aura started changing during battle too (attack costs, perfect-defense restores).</summary>
    public void RefreshBars(List<BattleParticipant> playerSide, List<BattleParticipant> enemySide)
    {
        for (int i = 0; i < BattleConfig.ActivePartySize && i < playerSide.Count; i++)
        {
            SetHPFill(_playerHPFills[i], _playerHPLabels[i], playerSide[i]);
            SetAuraFill(_playerAuraFills[i], _playerAuraLabels[i], playerSide[i]);
            SetStageCreatureAliveState(_playerStageCreatures[i], playerSide[i]);
        }

        if (enemySide.Count > 0)
        {
            SetHPFill(_enemyHPFill, _enemyHPLabel, enemySide[0]);
            SetAuraFill(_enemyAuraFill, _enemyAuraLabel, enemySide[0]);
            SetStageCreatureAliveState(_enemyStageCreature, enemySide[0]);
        }
    }

    /// <summary>Sets the fill width AND the "current/max" numeric readout (2026-08-05, user-directed: "add a number in the health and aura so I can see how many points are in each").</summary>
    private static void SetHPFill(VisualElement fill, Label label, BattleParticipant participant)
    {
        float fraction = participant.MaxHP > 0 ? (float)participant.CurrentHP / participant.MaxHP : 0f;
        fill.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
        label.text = $"{participant.CurrentHP}/{participant.MaxHP}";
    }

    private static void SetAuraFill(VisualElement fill, Label label, BattleParticipant participant)
    {
        float fraction = participant.MaxAura > 0 ? (float)participant.CurrentAura / participant.MaxAura : 0f;
        fill.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
        label.text = $"{participant.CurrentAura}/{participant.MaxAura}";
    }

    private static void SetStageCreatureColor(VisualElement creature, BattleParticipant participant)
    {
        PhasixData species = participant.RuntimeData.speciesData;
        if (species == null) return;
        creature.style.backgroundColor = PrimalTypeColor.GetColor(species.PrimalType);
    }

    private static void SetStageCreatureAliveState(VisualElement creature, BattleParticipant participant)
    {
        creature.style.opacity = participant.IsAlive ? 1f : 0.25f;
    }

    /// <summary>
    /// Places each option at its fixed clock-hour position (MoveOptionClockHours, index-matched)
    /// at MoveOptionRadius from the creature's center — "1 and 11 o'clock" per the user's explicit
    /// ask, rather than a computed even split. Computed once per slot in Awake since option
    /// count/parent size are both fixed for the whole battle.
    /// </summary>
    private static void PositionMoveOptions(VisualElement[] options)
    {
        for (int i = 0; i < options.Length; i++)
        {
            float angleDegrees = 90f - 30f * MoveOptionClockHours[i];
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            float dx = Mathf.Cos(angleRadians) * MoveOptionRadius;
            float dy = -Mathf.Sin(angleRadians) * MoveOptionRadius;

            // .stage-creature is a fixed 72x72; .move-option-placeholder is a fixed 32x32 circle —
            // self-centering offsets computed directly in px rather than via percent+translate,
            // since both sizes are known constants (see BattleHUD.uss).
            options[i].style.left = 36f - 16f + dx;
            options[i].style.top = 36f - 16f + dy;
        }
    }

    /// <summary>
    /// Places all SkillSlotCount empty placeholder circles evenly around the creature, one per
    /// clock hour (index 0 = 1 o'clock ... index 11 = 12 o'clock), on the SAME SkillSlotRadius
    /// path as the move options — see the field doc comment for why these are unified, not a
    /// separate ring. Computed once per slot in Awake, same as PositionMoveOptions.
    /// </summary>
    private static void PositionSkillSlots(VisualElement[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            float hour = i + 1; // index 0 -> 1 o'clock ... index 11 -> 12 o'clock
            float angleDegrees = 90f - 30f * hour;
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            float dx = Mathf.Cos(angleRadians) * SkillSlotRadius;
            float dy = -Mathf.Sin(angleRadians) * SkillSlotRadius;

            // .stage-creature is a fixed 72x72; .skill-slot-placeholder is a fixed 32x32 circle —
            // same size and same self-centering-in-px approach as PositionMoveOptions above (see
            // the field doc comment for why they're now uniform).
            slots[i].style.left = 36f - 16f + dx;
            slots[i].style.top = 36f - 16f + dy;
        }
    }

    /// <summary>
    /// Shows all of the acting player's move placeholders above their stage creature: "A"
    /// (Attack, index 0) and "K" (Capture, index 4 — 2026-08-06 Phase 3 Gate wiring) press-and-
    /// drag onto an ENEMY; "C"/"H"/"R" (Charge/Heal/Regen) are solo/self-only, press-and-drag onto
    /// the CASTER's OWN creature — "this one happens to only make it so you can select the
    /// character that is casting it". Whichever is released over a valid target fires
    /// onMoveConfirmed(optionIndex, target) — target is the enemy for Attack/Capture, always
    /// `self` for a self-only move. Releasing anywhere invalid cancels back to the placeholders,
    /// same for every move. Call HideMoveSelection to cancel/clean up early.
    /// </summary>
    public void ShowMoveSelection(int attackerSlotIndex, BattleParticipant self, List<BattleParticipant> enemyTargets,
        Action<int, BattleParticipant> onMoveConfirmed)
    {
        _self = self;
        _enemyTargets = enemyTargets;
        _onMoveConfirmed = onMoveConfirmed;
        SetMoveOptionsVisible(attackerSlotIndex, true);
    }

    /// <summary>Hides any visible move options and cancels an in-progress drag, if any. Safe to call even when nothing is shown.</summary>
    public void HideMoveSelection()
    {
        for (int i = 0; i < BattleConfig.ActivePartySize; i++) SetMoveOptionsVisible(i, false);
        EndDrag();
        _self = null;
        _enemyTargets = null;
        _onMoveConfirmed = null;
    }

    /// <summary>
    /// Shows/updates/hides a player's Regen status icon and its bottom-right countdown subscript
    /// (2026-08-06, user-directed — see DECISIONS.md -> [Combat]: "a countdown counter is better
    /// for user intuition — 4 turns will be 4 then 3 then 2 etc"). turnsRemaining &lt;= 0 hides
    /// the icon entirely (effect expired or was never active).
    /// </summary>
    public void SetRegenStatus(int slotIndex, int turnsRemaining)
    {
        bool active = turnsRemaining > 0;
        _playerRegenIcons[slotIndex].style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        _playerRegenCounters[slotIndex].text = active ? turnsRemaining.ToString() : "";
    }

    /// <summary>Shows/updates/hides a player's Evolution Burst status icon and countdown — same pattern as SetRegenStatus (2026-08-06, wiring EvolutionBurstSystem into the live loop). turnsRemaining &lt;= 0 hides the icon.</summary>
    public void SetBurstStatus(int slotIndex, int turnsRemaining)
    {
        bool active = turnsRemaining > 0;
        _playerBurstIcons[slotIndex].style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        _playerBurstCounters[slotIndex].text = active ? turnsRemaining.ToString() : "";
    }

    /// <summary>
    /// Updates a player's Evolution Burst gauge bar fill width and its "ready to activate" state
    /// (2026-08-06, user-directed — see DECISIONS.md -> [Combat]). `ready` should be computed by
    /// the caller as `fillPercent &gt;= EvolutionBurstSystem.TriggerThreshold &amp;&amp;
    /// !gauge.IsActive` — the SAME threshold ActivateReady itself checks, so the yellow-outlined
    /// "clickable" state never promises something a click won't actually deliver.
    /// </summary>
    public void SetBurstFillBar(int slotIndex, float fillPercent, bool ready)
    {
        _playerBurstFills[slotIndex].style.width = Length.Percent(Mathf.Clamp(fillPercent, 0f, 100f));
        _playerBurstBacks[slotIndex].EnableInClassList("burst-bar-ready", ready);
    }

    private void SetMoveOptionsVisible(int slotIndex, bool visible)
    {
        foreach (VisualElement option in _playerMoveOptions[slotIndex])
            option.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        // Empty skill slots show/hide in lockstep with "A"/"C" — the whole radial wheel appears
        // and disappears together (2026-08-06, user-directed — see DECISIONS.md -> [Combat]).
        foreach (VisualElement slot in _playerSkillSlots[slotIndex])
            slot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BeginDrag(int slotIndex, PointerDownEvent evt, int optionIndex)
    {
        SetMoveOptionsVisible(slotIndex, false);
        _draggingFromSlotIndex = slotIndex;
        _draggingOptionIndex = optionIndex;

        Vector2 startWorld = _playerStageCreatures[slotIndex].worldBound.center;
        _dragLine.Start = _dragLine.WorldToLocal(startWorld);
        _dragLine.End = _dragLine.WorldToLocal(evt.position);
        _dragLine.style.display = DisplayStyle.Flex;
        _dragLine.Refresh();

        _root.CapturePointer(evt.pointerId);
        _root.RegisterCallback<PointerMoveEvent>(OnDragPointerMove);
        _root.RegisterCallback<PointerUpEvent>(OnDragPointerUp);
    }

    private void OnDragPointerMove(PointerMoveEvent evt)
    {
        _dragLine.End = _dragLine.WorldToLocal(evt.position);
        _dragLine.Refresh();
    }

    private void OnDragPointerUp(PointerUpEvent evt)
    {
        _root.ReleasePointer(evt.pointerId);
        int fromSlotIndex = _draggingFromSlotIndex;
        int optionIndex = _draggingOptionIndex;
        EndDrag();

        bool selfOnly = optionIndex >= 0 && MoveOptionIsSelfOnly[optionIndex];
        bool hit;
        BattleParticipant target = null;

        if (!selfOnly)
        {
            // Attack — single enemy slot only (see class doc comment): hit-test against it
            // directly rather than a general per-target lookup, which multi-enemy battles will
            // need once they exist.
            hit = _enemyTargets != null && _enemyTargets.Count > 0 && _enemyStageCreature.worldBound.Contains(evt.position);
            if (hit) target = _enemyTargets[0];
        }
        else
        {
            // Solo/self-only move (2026-08-06, user-directed — see DECISIONS.md -> [Combat]):
            // the only valid drop target is the CASTER's own stage creature, not the enemy —
            // hit-test against fromSlotIndex's own creature instead.
            hit = _self != null && fromSlotIndex >= 0 && _playerStageCreatures[fromSlotIndex].worldBound.Contains(evt.position);
            if (hit) target = _self;
        }

        if (hit)
        {
            Action<int, BattleParticipant> callback = _onMoveConfirmed;
            _self = null;
            _enemyTargets = null;
            _onMoveConfirmed = null;
            callback?.Invoke(optionIndex, target);
        }
        else if (fromSlotIndex >= 0)
        {
            // Released on nothing valid — let the player retry (the only cancel path today, same
            // for every move).
            SetMoveOptionsVisible(fromSlotIndex, true);
        }
    }

    private void EndDrag()
    {
        _root.UnregisterCallback<PointerMoveEvent>(OnDragPointerMove);
        _root.UnregisterCallback<PointerUpEvent>(OnDragPointerUp);
        _dragLine.style.display = DisplayStyle.None;
        _draggingFromSlotIndex = -1;
        _draggingOptionIndex = -1;
    }

    /// <summary>
    /// Offensive action-command check (Combat_Directive_v0_1_0.md Part 4), reworked 2026-08-05
    /// (user-directed, Sonny 2-referenced — see DECISIONS.md -> [Combat]) to mirror
    /// RunDefenseTimedInput's converging-ring visual, positioned above the TARGETED ENEMY (not
    /// the attacker): a fixed target ring plus a white marker ring that starts wider and shrinks
    /// past it over sweepDuration. Left-clicking ANYWHERE succeeds if, at that instant, the
    /// marker/target radius ratio is within toleranceHalfWidth of 1.0 — the marker flashes green
    /// on a normal success, a brighter neon purple on a "perfect" (within the innermost
    /// PerfectToleranceFraction of that tolerance), red on any miss (wrong moment or no click at
    /// all). Sets LastTimedInputSuccess/LastTimedInputWasPerfect — read them right after this
    /// coroutine completes. A miss by timeout (no click before the marker finishes converging)
    /// always resolves as a failure, same as clicking at the wrong moment. Deliberately just one
    /// window per call — a future multi-input attack/skill (some offensive actions may need more
    /// than one beat) would call this coroutine multiple times in sequence rather than needing a
    /// rewrite here.
    /// </summary>
    public IEnumerator RunTimedInput(string label, float toleranceHalfWidth, float sweepDuration)
    {
        _actionAnnouncementLabel.text = label;
        _actionAnnouncement.EnableInClassList("action-announcement-offense", true);
        _actionAnnouncement.EnableInClassList("action-announcement-defend", false);
        _actionAnnouncement.style.display = DisplayStyle.Flex;

        _enemyStageCreature.Add(_timingRing);
        _timingRing.style.display = DisplayStyle.Flex;
        _timingRing.TargetRadius = RingTargetRadius;
        _timingRing.MarkerColor = Color.white;
        _timingRing.MarkerRadius = RingMarkerStartRadius;
        _timingRing.Refresh();

        bool clicked = false;
        EventCallback<PointerDownEvent> onPointerDown = evt =>
        {
            if (clicked || evt.button != 0) return; // left-click only — no second outcome to distinguish for offense
            clicked = true;
        };
        _root.RegisterCallback(onPointerDown);

        float elapsed = 0f;

        while (elapsed < sweepDuration && !clicked)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / sweepDuration);
            _timingRing.MarkerRadius = Mathf.Lerp(RingMarkerStartRadius, RingMarkerMinRadius, progress);
            _timingRing.Refresh();
            yield return null;
        }

        _root.UnregisterCallback(onPointerDown);

        float deviation = Mathf.Abs(_timingRing.MarkerRadius / RingTargetRadius - 1f);
        LastTimedInputSuccess = clicked && deviation <= toleranceHalfWidth;
        LastTimedInputWasPerfect = LastTimedInputSuccess && deviation <= toleranceHalfWidth * PerfectToleranceFraction;
        _timingRing.MarkerColor = !LastTimedInputSuccess ? MissFlashColor : LastTimedInputWasPerfect ? PerfectFlashColor : SuccessFlashColor;
        _timingRing.Refresh();

        // Brief hold so the player can see the flash (or where the marker landed on a miss) before it hides.
        yield return new WaitForSeconds(0.3f);
        _actionAnnouncement.style.display = DisplayStyle.None;
        _timingRing.style.display = DisplayStyle.None;
        _timingRing.RemoveFromHierarchy();
    }

    /// <summary>
    /// Live-reaction Dodge/Parry defense as a converging ring (reworked 2026-08-05, user-directed,
    /// Sonny 2-referenced — see DECISIONS.md -> [Combat]): the ring is reparented above
    /// targetSlotIndex's stage creature, showing a fixed target ring plus a white marker ring that
    /// starts wider and shrinks past it over sweepDuration — no pre-drawn zone the player can see
    /// ahead of time, just the moving ring approaching (and passing through) the static one.
    /// Left-clicking ANYWHERE succeeds as a Dodge if the marker/target radius ratio at that
    /// instant is within dodgeToleranceHalfWidth of 1.0; right-clicking ANYWHERE succeeds as a
    /// Parry within the tighter parryToleranceHalfWidth. Flash color is by OUTCOME QUALITY, shared
    /// by Dodge/Parry alike: green for a normal success, neon purple for a "perfect" (within the
    /// innermost PerfectToleranceFraction of whichever tolerance applied), red for any Miss (wrong
    /// tolerance, wrong button, or a timeout with no click at all — full damage either way, same
    /// "reward, don't punish" rule as before). Sets LastDefenseOutcome/LastDefenseWasPerfect —
    /// read them right after this coroutine completes.
    /// </summary>
    public IEnumerator RunDefenseTimedInput(int targetSlotIndex, string label, float dodgeToleranceHalfWidth, float parryToleranceHalfWidth, float sweepDuration)
    {
        _actionAnnouncementLabel.text = label;
        _actionAnnouncement.EnableInClassList("action-announcement-offense", false);
        _actionAnnouncement.EnableInClassList("action-announcement-defend", true);
        _actionAnnouncement.style.display = DisplayStyle.Flex;

        _playerStageCreatures[targetSlotIndex].Add(_timingRing);
        _timingRing.style.display = DisplayStyle.Flex;
        _timingRing.TargetRadius = RingTargetRadius;
        _timingRing.MarkerColor = Color.white;
        _timingRing.MarkerRadius = RingMarkerStartRadius;
        _timingRing.Refresh();

        bool clicked = false;
        int clickButton = -1; // 0 = left, 1 = right
        EventCallback<PointerDownEvent> onPointerDown = evt =>
        {
            if (clicked || (evt.button != 0 && evt.button != 1)) return;
            clicked = true;
            clickButton = evt.button;
        };
        _root.RegisterCallback(onPointerDown);

        float elapsed = 0f;

        while (elapsed < sweepDuration && !clicked)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / sweepDuration);
            _timingRing.MarkerRadius = Mathf.Lerp(RingMarkerStartRadius, RingMarkerMinRadius, progress);
            _timingRing.Refresh();
            yield return null;
        }

        _root.UnregisterCallback(onPointerDown);

        float deviation = Mathf.Abs(_timingRing.MarkerRadius / RingTargetRadius - 1f);

        if (!clicked)
        {
            LastDefenseOutcome = DefenseOutcome.Miss;
            LastDefenseWasPerfect = false;
        }
        else if (clickButton == 0)
        {
            bool success = deviation <= dodgeToleranceHalfWidth;
            LastDefenseOutcome = success ? DefenseOutcome.Dodge : DefenseOutcome.Miss;
            LastDefenseWasPerfect = success && deviation <= dodgeToleranceHalfWidth * PerfectToleranceFraction;
        }
        else
        {
            bool success = deviation <= parryToleranceHalfWidth;
            LastDefenseOutcome = success ? DefenseOutcome.Parry : DefenseOutcome.Miss;
            LastDefenseWasPerfect = success && deviation <= parryToleranceHalfWidth * PerfectToleranceFraction;
        }

        _timingRing.MarkerColor = LastDefenseOutcome == DefenseOutcome.Miss ? MissFlashColor
            : LastDefenseWasPerfect ? PerfectFlashColor
            : SuccessFlashColor;
        _timingRing.Refresh();

        yield return new WaitForSeconds(0.3f);
        _actionAnnouncement.style.display = DisplayStyle.None;
        _timingRing.style.display = DisplayStyle.None;
        _timingRing.RemoveFromHierarchy();
    }

    /// <summary>
    /// Shows a message for a fixed duration before auto-hiding — used for every beat in the
    /// battle, including the player-to-enemy turn transition (2026-08-06, user-directed — see
    /// DECISIONS.md -> [Combat]: "the continue between the [turns] might not be needed anymore
    /// just a delay to let the player understand that the turns have switched", removing the
    /// last remaining click-to-proceed gate — WaitForContinue/ContinueButton are gone). Enough
    /// time to read what happened, without gating on a click.
    /// </summary>
    public IEnumerator ShowTimedMessage(string message, float durationSeconds)
    {
        _continuePromptLabel.text = message;
        _continuePrompt.style.display = DisplayStyle.Flex;

        yield return new WaitForSeconds(durationSeconds);

        _continuePrompt.style.display = DisplayStyle.None;
    }

    /// <summary>Clears every entry from the battle log. Call once when a new battle starts.</summary>
    public void ClearBattleLog() => _battleLogContent.Clear();

    /// <summary>Appends one line to the battle log and scrolls it into view.</summary>
    public void AppendBattleLog(string message)
    {
        var entry = new Label(message);
        entry.AddToClassList("battle-log-entry");
        _battleLogContent.Add(entry);
        _battleLogScrollView.ScrollTo(entry);
    }

    public void Show() => _root.style.display = DisplayStyle.Flex;
    public void Hide() => _root.style.display = DisplayStyle.None;
}
