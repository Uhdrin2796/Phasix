using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Radial nameplates (up to MaxNameplateSlots per side — 2026-08-06, user-directed, replacing the
/// old stacked HP/Aura/Burst bars, see DECISIONS.md -> [Combat]), placeholder stage creatures,
/// Sonny 2-style radial move selection + drag-to-target, the shared converging-ring action-command
/// timing visual (offense on the targeted enemy, defense on the defending player creature), a
/// fully auto-paced beat message (ShowTimedMessage — no click-to-proceed gate anywhere in the
/// battle as of 2026-08-06, see DECISIONS.md -> [Combat]), and a scrolling text battle log for
/// BattleScene_Main. MonoBehaviour singleton wrapping a UIDocument, matching
/// EncounterPromptController's convention (see DECISIONS.md -> [UI] for why UI Toolkit over uGUI).
/// The STAGE (creature balls + move wheels) is still fixed at 3 player slots
/// (BattleConfig.ActivePartySize) and 1 enemy slot — multi-enemy battles (trainer fights,
/// Roadmap_v2 Mo 14-15) aren't built yet — but the nameplate SIDEBAR is a separate, wider-capacity
/// system (MaxNameplateSlots = 7) that doesn't share that cap; see NameplateRefs' doc comment.
///
/// Layout is Sonny 2-style per user reference: top nameplate sidebar per side (radial HP/Aura/Evo
/// gauge around a portrait, name above, wrapping buff row below — BuildNameplate), middle stage
/// with staggered placeholder creature circles (player left, enemy right, same lane —
/// ApplyStageCreatureStagger, 2026-08-06), bottom action bar — no visible lane lines
/// (Combat_Directive_v0_1_0.md stage art/lane visuals remain "pending art direction"; see
/// BattleStageGizmos.cs for the Scene-view-only dev visualization instead). This is a flat
/// screen-space overlay on top of the frozen overworld, not real diorama art.
///
/// Free-choice creature selection (2026-08-06, user-directed — see DECISIONS.md -> [Combat]):
/// during the player's turn, clicking any player stage creature (PlayerCreatureClicked) opens
/// ITS move wheel — functional (ShowMoveSelection) if it hasn't acted yet, read-only/greyed
/// (ShowMoveSelectionReadOnly) if it has. Clicking a DIFFERENT creature while one's wheel is open
/// closes the old and opens the new; BattleManager owns this switching logic, this class just
/// fires the click event and exposes the two show modes. EndTurnClicked (a dedicated, always-
/// visible-during-the-player's-turn button, deliberately NOT one of the auto-hiding
/// ShowTimedMessage dialogue boxes) lets the player end their turn without every creature having
/// acted.
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

    /// <summary>
    /// Hover tooltip text for the 5 built-in moves, index-matched to MoveOptionsPerSlot/
    /// MoveOptionClockHours (A/C/H/R/K — 2026-08 follow-up, user report they showed nothing on
    /// hover, unlike the real skill-ring orbs). Built once from the same named constants
    /// (BattleConfig, DamageCalculator, CaptureSystem) that actually drive each move's resolution
    /// in BattleManager — same "use the real values" standard BuildSkillTooltipText applies to the
    /// skill-ring orbs, just for the 5 moves that aren't SkillData-backed and so have no
    /// PlaceholderSkillResolver entry to read from. Capture's chance range is computed via
    /// CaptureSystem.ComputeCaptureChancePercent at 0% and 100% target HP rather than hardcoded,
    /// so it can't drift out of sync with the real formula.
    /// </summary>
    private static readonly string[] MoveOptionTooltips = BuildMoveOptionTooltips();

    private static string[] BuildMoveOptionTooltips()
    {
        float minCaptureChance = CaptureSystem.ComputeCaptureChancePercent(100, 100); // full target HP -> lowest chance
        float maxCaptureChance = CaptureSystem.ComputeCaptureChancePercent(0, 100);   // 0 target HP -> highest chance

        return new[]
        {
            $"Attack\n{DamageCategory.Physical} damage — Power {DamageCalculator.BasicAttackPower}\nTarget: Enemy\nAura Cost: {BattleConfig.AttackAuraCost}",
            $"Charge\nRestores {BattleConfig.ChargeAuraRestore} Aura\nTarget: Self",
            $"Heal\nRestores {BattleConfig.HealAmount} HP instantly\nTarget: Self\nAura Cost: {BattleConfig.HealAuraCost}",
            $"Regen\nRestores {BattleConfig.RegenHealPerTurn} HP/turn for {BattleConfig.RegenDurationTurns} turns\nTarget: Self\nAura Cost: {BattleConfig.RegenAuraCost}",
            $"Capture\nCapture chance: {Mathf.RoundToInt(minCaptureChance)}-{Mathf.RoundToInt(maxCaptureChance)}% (lower target HP = higher chance)\nTarget: Enemy",
        };
    }

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

    // Real skill-ring wiring (2026-08 session — Combo/Status/Chain/Mastery wiring, see
    // DECISIONS.md -> [Combat]): the 5 built-in moves occupy skill-slot array indices 0,1,2,10,11
    // (hours 1,2,3,11,12 — MoveOptionClockHours), so the remaining indices 3-9 (hours 4-10) are
    // exactly the 7 slots this ring has left over. That happens to comfortably cover every tier's
    // active-slot range (SkillSlotCapacity.GetActiveSlotRange: T1=2 ... T5 up to 7) — not a
    // coincidence, the leftover count was chosen to line up with it.
    private const int SkillRingSlotOffset = 3;
    private const int SkillRingSlotCount = 7;

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

    private readonly VisualElement[] _playerStageCreatures = new VisualElement[BattleConfig.ActivePartySize];
    private VisualElement _playerStageArea;

    /// <summary>
    /// Max nameplates the status sidebar supports per side (2026-08-06, user-directed — see
    /// DECISIONS.md -> [Combat]: "i want to be able to stack 7 containers maximum on each side").
    /// Deliberately separate from BattleConfig.ActivePartySize (still 3, the real gameplay party
    /// cap — Combat_Directive_v0_1_0.md's 3-5 range is still pending) — this only governs how many
    /// nameplate SLOTS the HUD can visually display, not how many creatures a battle can actually
    /// field. A real battle today only ever fills the first 3 (or 1, enemy side); the rest simply
    /// stay hidden (Initialize's hasSlot check).
    /// </summary>
    private const int MaxNameplateSlots = 7;

    /// <summary>
    /// Which nameplate visual the HP/Aura/Evo readout uses. Radial = the original circular gauge
    /// (RadialGaugeVisual ring + 3 tiny stat labels). Bars = a 2026-08 follow-up mockup — 3 stacked
    /// horizontal rectangles, current/total shown on hover instead of always-on text — built as an
    /// explicit alternative rather than a replacement, per the user's ask to keep the circular
    /// version available to switch back to. Flip this one const to switch every nameplate; both
    /// BuildNameplate and RefreshNameplateStats/ApplyEvoVisual/ApplyNameplateSize branch on it, and
    /// neither branch's code was deleted when the other was added.
    /// </summary>
    private const NameplateStyle ActiveNameplateStyle = NameplateStyle.Bars;

    private enum NameplateStyle { Radial, Bars }

    /// <summary>Toggle interval (ms) for the Evo-ready flashing perimeter — see ApplyEvoVisual. A light, readable flash rate, not tuned/playtested.</summary>
    private const long EvoFlashIntervalMs = 450;

    // Dynamic nameplate sizing (2026-08-06, user-directed — see DECISIONS.md -> [Combat]): the
    // fixed 46px-ring size verified to fit all 7 slots looked "too small" once the user compared
    // it against today's real 3-member party — most of the sidebar's vertical budget sat unused.
    // ApplyNameplateSize linearly interpolates between these two calibrated endpoints based on how
    // many nameplates are ACTUALLY visible on that side: NameplateSizeMinCount (3) or fewer gets
    // the "Comfortable" size, MaxNameplateSlots (7) gets the "Compact" size (unchanged from the
    // verified-fits-7 pass). Comfortable is deliberately smaller than the 90px shown in the
    // approved mockup — 72px keeps the 3-member case within the header-height budget
    // `.stage-side`'s fixed `top: 480px` was already calibrated against (see that entry), so this
    // doesn't require re-touching stage-creature clearance math too.
    private const int NameplateSizeMinCount = 3;
    private const float NameplateRingSizeComfortable = 72f;
    private const float NameplateRingSizeCompact = 46f;
    private const float NameplateNameFontComfortable = 13f;
    private const float NameplateNameFontCompact = 10f;
    private const float NameplateStatFontComfortable = 9f;
    private const float NameplateStatFontCompact = 7f;
    private const float NameplateBuffIconComfortable = 14f;
    private const float NameplateBuffIconCompact = 12f;
    private const float NameplatePaddingComfortable = 3f;
    private const float NameplatePaddingCompact = 2f;
    private const float NameplateMarginComfortable = 6f;
    private const float NameplateMarginCompact = 3f;
    private const float NameplatePortraitFraction = 0.53f; // portrait diameter as a fraction of ring size, matches the original 24/46 ratio

    /// <summary>
    /// Sizes one nameplate's ring/name/stats/buff-icons/padding/margin based on how many are
    /// actually visible on its side — see the constants above for the calibrated endpoints.
    /// Applied as inline styles (overriding the .nameplate-* USS defaults) since USS can't read a
    /// runtime count. Player and enemy sides size independently off their own visible counts.
    /// Radial-style only — the Bars mockup doesn't (yet) have a party-count-based size curve of its
    /// own, its sizing lives entirely in the static .nameplate-bar-* USS classes, so this is a
    /// deliberate no-op while Bars is active rather than something left unported.
    /// </summary>
    private static void ApplyNameplateSize(NameplateRefs np, int visibleCount)
    {
        if (ActiveNameplateStyle == NameplateStyle.Bars) return;

        int clamped = Mathf.Clamp(visibleCount, NameplateSizeMinCount, MaxNameplateSlots);
        float t = (clamped - NameplateSizeMinCount) / (float)(MaxNameplateSlots - NameplateSizeMinCount);

        float ring = Mathf.Lerp(NameplateRingSizeComfortable, NameplateRingSizeCompact, t);
        float nameFont = Mathf.Lerp(NameplateNameFontComfortable, NameplateNameFontCompact, t);
        float statFont = Mathf.Lerp(NameplateStatFontComfortable, NameplateStatFontCompact, t);
        float buffIcon = Mathf.Lerp(NameplateBuffIconComfortable, NameplateBuffIconCompact, t);
        float padding = Mathf.Lerp(NameplatePaddingComfortable, NameplatePaddingCompact, t);
        float margin = Mathf.Lerp(NameplateMarginComfortable, NameplateMarginCompact, t);
        float portrait = ring * NameplatePortraitFraction;
        float portraitOffset = (ring - portrait) / 2f;

        np.Container.style.paddingTop = padding;
        np.Container.style.paddingBottom = padding;
        np.Container.style.paddingLeft = padding;
        np.Container.style.paddingRight = padding;
        np.Container.style.marginBottom = margin;

        np.NameLabel.style.fontSize = nameFont;

        np.RingWrap.style.width = ring;
        np.RingWrap.style.height = ring;
        np.Gauge.style.width = ring;
        np.Gauge.style.height = ring;

        np.Portrait.style.width = portrait;
        np.Portrait.style.height = portrait;
        np.Portrait.style.left = portraitOffset;
        np.Portrait.style.top = portraitOffset;
        SetUniformBorderRadius(np.Portrait, portrait / 2f);

        np.HPStat.style.fontSize = statFont;
        np.AuraStat.style.fontSize = statFont;
        np.EvoStat.style.fontSize = statFont;

        SetUniformSize(np.RegenIcon, buffIcon);
        SetUniformSize(np.BurstIcon, buffIcon);
    }

    private static void SetUniformSize(VisualElement el, float size)
    {
        el.style.width = size;
        el.style.height = size;
        SetUniformBorderRadius(el, size / 2f);
    }

    private static void SetUniformBorderRadius(VisualElement el, float radius)
    {
        el.style.borderTopLeftRadius = radius;
        el.style.borderTopRightRadius = radius;
        el.style.borderBottomLeftRadius = radius;
        el.style.borderBottomRightRadius = radius;
    }

    /// <summary>
    /// One "invisible container" (2026-08-06, user-directed — see DECISIONS.md -> [Combat]) and
    /// its sub-elements, built procedurally by BuildNameplate rather than hand-authored in UXML —
    /// at up to 7 per side, with every sub-part (portrait, ring, 3 stat labels, buff row + its 2
    /// icons), hand-authoring the UXML would mean ~14 elements x 7 slots x 2 sides. Plain field
    /// bag, not a MonoBehaviour/VisualElement subclass — purely a bookkeeping struct for
    /// BattleHUDController's own arrays.
    /// </summary>
    private class NameplateRefs
    {
        public VisualElement Container;
        public Label NameLabel;

        // Radial-style only (null while ActiveNameplateStyle == Bars).
        public VisualElement RingWrap;
        public VisualElement Portrait;
        public RadialGaugeVisual Gauge;
        public Label HPStat;
        public Label AuraStat;
        public Label EvoStat;

        // Bars-style only (null while ActiveNameplateStyle == Radial). Each *BarFill is the inner
        // colored rect whose width gets set to a percentage; hovering its parent track shows the
        // matching *TooltipText string (kept current by RefreshNameplateStats/ApplyEvoVisual, read
        // fresh at hover time by the callback RegisterHudTooltipHover wires up).
        public VisualElement HPBarFill;
        public VisualElement AuraBarFill;
        public VisualElement EvoBarFill;
        public string HPTooltipText;
        public string AuraTooltipText;
        public string EvoTooltipText;

        // Evo-ready flash (2026-08 follow-up — user-directed: "highlighting the perimeter of the
        // evo box and have it flash lightly"). EvoBarTrack is the flash TARGET (its border, not
        // the fill's background, since a highlighted PERIMETER was the ask); EvoFlashActive/
        // EvoFlashSchedule track whether the repeating toggle is currently running, so
        // ApplyEvoVisual only starts/stops it on an actual ready-state TRANSITION, not every
        // refresh (RefreshNameplateStats runs far more often than the ready state actually changes).
        public VisualElement EvoBarTrack;
        public bool EvoFlashActive;
        public IVisualElementScheduledItem EvoFlashSchedule;

        public VisualElement RegenIcon;
        public Label RegenCounter;
        public VisualElement BurstIcon;
        public Label BurstCounter;
    }

    private readonly NameplateRefs[] _playerNameplates = new NameplateRefs[MaxNameplateSlots];
    private readonly NameplateRefs[] _enemyNameplates = new NameplateRefs[MaxNameplateSlots];

    /// <summary>[slotIndex][optionIndex] — MoveOptionsPerSlot placeholders per party member, radially positioned above their stage creature.</summary>
    private readonly VisualElement[][] _playerMoveOptions = new VisualElement[BattleConfig.ActivePartySize][];

    /// <summary>[slotIndex][skillSlotIndex] — SkillSlotCount placeholder circles per party member, one per clock hour. Indices 0,1,2,10,11 sit under the 5 built-in move orbs; SkillRingSlotOffset..+SkillRingSlotCount-1 are the real, clickable equipped-skill slots (see PopulateSkillRing).</summary>
    private readonly VisualElement[][] _playerSkillSlots = new VisualElement[BattleConfig.ActivePartySize][];

    /// <summary>[slotIndex][ringIndex 0..SkillRingSlotCount-1] — resolved equipped skill for each real skill-ring slot, or null if that slot is empty/locked for this creature (2026-08 session, see DECISIONS.md -> [Combat]). Populated by PopulateSkillRing (called from Initialize); read by the click handlers registered in Awake.</summary>
    private readonly SkillData[][] _playerSkillSlotSkills = new SkillData[BattleConfig.ActivePartySize][];

    /// <summary>
    /// [slotIndex][ringIndex 0..SkillRingSlotCount-1] — small numeric badge on each real
    /// skill-ring slot showing the current combo-streak count (2026-08 session — user-directed:
    /// "counter next to the skill on the skill wheel," see DECISIONS.md -> [Combat]). Created
    /// once per slot in Awake, hidden by default; BattleManager decides which slot to badge and
    /// with what count via SetSkillComboCounter/ClearAllSkillComboCounters — this class has no
    /// combo logic of its own, purely a dumb display.
    /// </summary>
    private readonly Label[][] _playerSkillSlotComboBadges = new Label[BattleConfig.ActivePartySize][];

    /// <summary>
    /// [slotIndex][ringIndex 0..SkillRingSlotCount-1] — lettering label on each real skill-ring
    /// slot, showing the equipped skill's SkillName (2026-08 follow-up — user-directed: "make
    /// sure the orb has the lettering like all the other orbs and is visible"). Reuses
    /// `.move-option-label`'s exact styling (position/centering/font/color) so C1/C2 read
    /// identically to A/C/H/R/K, not a separate visual language. Created once per slot in Awake
    /// like the combo badge; PopulateSkillRing just updates `.text` (empty for a locked slot).
    /// </summary>
    private readonly Label[][] _playerSkillSlotLabels = new Label[BattleConfig.ActivePartySize][];

    /// <summary>
    /// Fill+border color per real skill-ring position (2026-08 follow-up — user-directed: "select
    /// whatever colors you see fit for those"), index-matched to ring position 0..SkillRingSlotCount-1
    /// (SkillRingSlotOffset+0 = C1's slot, +1 = C2's, etc.) — the POSITION owns the color, same as
    /// how A/C/H/R/K are colored by their fixed clock position, not by whichever move happens to be
    /// there. Deliberately a different palette from A/C/H/R/K's green/blue/pink/purple/gold so the
    /// two orb families stay visually distinct. USS classes are `.skill-ring-color-0` etc. (see
    /// BattleHUD.uss) — only the first 2 are reachable today (SkillSlotCapacity's Tier 1 cap), the
    /// rest are forward-looking for higher tiers.
    /// </summary>
    private const int SkillRingColorCount = 7;

    /// <summary>
    /// Fires with the clicked player slot index whenever a nameplate's radial gauge ring is
    /// pressed (2026-08-06 — see BuildNameplate's ring-wrap click registration; previously a
    /// dedicated purple bar, now the Evo arc segment of the radial gauge — "the activation can be
    /// on the bar itself" still holds, just on the ring instead). BattleManager subscribes and
    /// calls EvolutionBurstSystem.ActivateReady, which silently no-ops if that slot's gauge isn't
    /// actually full yet.
    /// </summary>
    public event Action<int> BurstBarClicked;

    // Free-choice creature selection (2026-08-06, user-directed — see DECISIONS.md -> [Combat]):
    // replaced the old strict-turn-order foreach with "click whichever Phasix you want, in any
    // order, until you End Turn." PlayerCreatureClicked fires whenever a player stage creature
    // (the ball itself, or any of its empty grey skill slots — anything in that visual cluster
    // that ISN'T a specific colored move orb) is pressed; move-orb clicks call
    // evt.StopPropagation() in Awake below so they don't ALSO bubble up and fire this — a specific
    // orb press means "use this move," not "just select this creature." EndTurnClicked fires from
    // the dedicated always-visible-during-the-player's-turn button (kept deliberately separate
    // from the auto-hiding ShowTimedMessage dialogue boxes, which the user noted are planned for
    // eventual removal once there's more UI feedback — this button is not).
    public event Action<int> PlayerCreatureClicked;
    public event Action EndTurnClicked;

    /// <summary>
    /// Fires on a pointer-down that lands directly on the empty Stage background — not on a
    /// creature, an orb, or any other Stage child, all of which either StopPropagation (orbs) or
    /// are checked by `evt.target` here (2026-08-06, user-directed: "clicking outside of that
    /// should hide any open skill wheels"). Registered on `_stage` rather than `_root` since
    /// `.stage` (`flex-grow: 1`) already fills essentially the whole play area below the status
    /// header — the practical "background" the player sees.
    /// </summary>
    public event Action StageBackgroundClicked;

    private Button _endTurnButton;

    /// <summary>
    /// Per-slot "already acted this turn" flag (2026-08-06, user-directed — see DECISIONS.md ->
    /// [Combat]: "if the phasix already moved during its turn then it can still show, but will be
    /// greyed out for active skills"). Set by ShowMoveSelectionReadOnly, cleared by
    /// ShowMoveSelection — read by SetMoveOptionsVisible (applies `.move-option-disabled`) and
    /// BeginDrag (refuses to start a drag for a read-only slot).
    /// </summary>
    private readonly bool[] _playerSlotReadOnly = new bool[BattleConfig.ActivePartySize];

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
    private Action<ChosenMove> _onMoveConfirmed;
    private int _draggingFromSlotIndex = -1;
    private int _draggingOptionIndex = -1;

    /// <summary>Non-null while dragging a skill-ring slot (as opposed to one of the 5 built-in moves) — 2026-08 session, see DECISIONS.md -> [Combat].</summary>
    private SkillData _draggingSkill;

    // Shared converging-ring timing visual (2026-08-05, user-directed — see DECISIONS.md ->
    // [Combat]): reparented per use — above the targeted enemy for RunTimedInput (offense), above
    // the defending player creature for RunDefenseTimedInput. Never both at once (PlayerTurn and
    // EnemyTurn don't run concurrently), so one shared instance is enough.
    private RingVisual _timingRing;

    /// <summary>
    /// Shared runtime hover tooltip (2026-08 follow-up fix — the original skill-orb implementation
    /// used UI Toolkit's native VisualElement.tooltip, which only renders inside Editor-hosted UI
    /// (Inspector/EditorWindow panels) and is silently a no-op for a runtime UIDocument panel like
    /// this one, in Play Mode or a real build alike. Replaced with a plain floating Label shown/
    /// hidden on PointerEnter/PointerLeave — same family of technique as _dragLine above. Added
    /// directly to _root (not _stage) and last among its siblings so it always paints on top,
    /// following UI Toolkit's document-order paint rule. Generalized (renamed from _skillTooltip)
    /// the same session it was added, once the nameplate bar mockup below needed the exact same
    /// "show text on hover" behavior — one shared Label works fine since only one element can be
    /// hovered at a time. Positioned relative to the HOVERED ELEMENT'S own bounds (2026-08 follow-
    /// up — user explicit ask: "expecting it be near/next to the skill we're hovering"), not the
    /// live cursor position as the first pass did — since the anchor element doesn't move while
    /// hovered, this only needs to be computed once on Enter, so PointerMoveEvent tracking (the
    /// original design) was removed as unneeded.
    /// </summary>
    private Label _hudTooltip;

    /// <summary>Gap (px) between the hovered element's edge and the tooltip's near edge.</summary>
    private const float HudTooltipAnchorGap = 8f;

    private void Awake()
    {
        Instance = this;

        var document = GetComponent<UIDocument>();
        _root = document.rootVisualElement.Q<VisualElement>("BattleHUDRoot");
        _stage = _root.Q<VisualElement>("Stage");
        _playerStageArea = _root.Q<VisualElement>("PlayerStageArea");
        _stage.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.target == _stage) StageBackgroundClicked?.Invoke();
        });

        for (int i = 0; i < BattleConfig.ActivePartySize; i++)
        {
            _playerStageCreatures[i] = _root.Q<VisualElement>($"PlayerStageSlot{i}");

            int capturedCreatureSlotIndex = i;
            _playerStageCreatures[i].RegisterCallback<PointerDownEvent>(evt => PlayerCreatureClicked?.Invoke(capturedCreatureSlotIndex));

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
                // more UXML element, no new registration branch needed here. StopPropagation so
                // pressing a specific orb doesn't ALSO bubble up and fire PlayerCreatureClicked
                // on the parent .stage-creature (2026-08-06, user-directed free-choice selection
                // — see DECISIONS.md -> [Combat]) — a specific orb press means "use this move,"
                // not "just select this creature."
                option.RegisterCallback<PointerDownEvent>(evt =>
                {
                    evt.StopPropagation();
                    BeginDrag(capturedSlotIndex, evt, capturedOptionIndex);
                });

                // Hover tooltip for the 5 built-in moves (2026-08 follow-up — user report: "C, H,
                // A, R, K are not displaying at all," i.e. only the real skill-ring orbs had a
                // tooltip before this). Content comes from MoveOptionTooltips, precomputed once
                // from the same named BattleConfig/DamageCalculator/CaptureSystem constants that
                // actually drive each move in BattleManager — same "use the real values" standard
                // as BuildSkillTooltipText, just for the 5 moves that aren't SkillData-backed.
                option.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    _hudTooltip.text = MoveOptionTooltips[capturedOptionIndex];
                    _hudTooltip.style.display = DisplayStyle.Flex;
                    PositionHudTooltipNear(option);
                });
                option.RegisterCallback<PointerLeaveEvent>(evt => _hudTooltip.style.display = DisplayStyle.None);

                option.style.display = DisplayStyle.None;
            }
            PositionMoveOptions(_playerMoveOptions[i]);

            _playerSkillSlots[i] = new VisualElement[SkillSlotCount];
            _playerSkillSlotSkills[i] = new SkillData[SkillRingSlotCount];
            _playerSkillSlotComboBadges[i] = new Label[SkillRingSlotCount];
            _playerSkillSlotLabels[i] = new Label[SkillRingSlotCount];
            for (int k = 0; k < SkillSlotCount; k++)
            {
                VisualElement slot = _root.Q<VisualElement>($"PlayerStageSlot{i}_SkillSlot{k}");
                _playerSkillSlots[i][k] = slot;
                slot.style.display = DisplayStyle.None;

                // Real equipped-skill slots only (SkillRingSlotOffset..+SkillRingSlotCount-1) —
                // the other 5 sit under the built-in move orbs and stay purely decorative. Reads
                // _playerSkillSlotSkills at CLICK time (not registration time) so PopulateSkillRing
                // can be called freely from Initialize without needing to re-register handlers;
                // a null entry (empty/locked slot) is a silent no-op, matching "no click handler,
                // no functionality" for a slot the creature doesn't have equipped.
                if (k >= SkillRingSlotOffset && k < SkillRingSlotOffset + SkillRingSlotCount)
                {
                    var comboBadge = new Label();
                    comboBadge.AddToClassList("skill-combo-badge");
                    comboBadge.style.display = DisplayStyle.None;
                    comboBadge.pickingMode = PickingMode.Ignore; // decoration only — must never intercept the slot's own drag click
                    slot.Add(comboBadge);
                    _playerSkillSlotComboBadges[i][k - SkillRingSlotOffset] = comboBadge;

                    // Skill-name lettering (2026-08 follow-up — see _playerSkillSlotLabels' doc
                    // comment). Reuses .move-option-label as-is, same visual language as A/C/H/R/K.
                    // Text set by PopulateSkillRing; empty for a locked/unequipped slot.
                    var skillLabel = new Label();
                    skillLabel.AddToClassList("move-option-label");
                    skillLabel.pickingMode = PickingMode.Ignore; // decoration only, same reasoning as comboBadge above
                    slot.Add(skillLabel);
                    _playerSkillSlotLabels[i][k - SkillRingSlotOffset] = skillLabel;

                    int capturedSlotIndex = i;
                    int capturedRingIndex = k - SkillRingSlotOffset;
                    slot.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        SkillData skill = _playerSkillSlotSkills[capturedSlotIndex][capturedRingIndex];
                        if (skill == null) return;
                        evt.StopPropagation();
                        BeginDragForSkill(capturedSlotIndex, evt, skill);
                    });

                    // Runtime hover tooltip (see _hudTooltip's doc comment for why this replaced
                    // the old VisualElement.tooltip assignment). Reads _playerSkillSlotSkills at
                    // hover time, same "read at event time, not registration time" pattern as the
                    // click handler above, so PopulateSkillRing can keep re-resolving skills freely.
                    // Content built by BuildSkillTooltipText from the skill's own RESOLVED behavior
                    // (2026-08 follow-up — user-directed: "use the values from each skill orb to
                    // generate your content"), not the shared placeholder Description text.
                    slot.RegisterCallback<PointerEnterEvent>(evt =>
                    {
                        SkillData skill = _playerSkillSlotSkills[capturedSlotIndex][capturedRingIndex];
                        if (skill == null) return;
                        _hudTooltip.text = BuildSkillTooltipText(skill);
                        _hudTooltip.style.display = DisplayStyle.Flex;
                        PositionHudTooltipNear(slot);
                    });
                    slot.RegisterCallback<PointerLeaveEvent>(evt => _hudTooltip.style.display = DisplayStyle.None);
                }
            }
            PositionSkillSlots(_playerSkillSlots[i]);
        }

        // Staggered so each Phasix — and its own orb ring — has clear, unobstructed space (2026-
        // 08-06, user-directed — see DECISIONS.md -> [Combat]: "they need to be offset/staggered
        // so you can see the full character similar to sonny 2 or slay the spire 2"). A pure
        // visual transform, applied AFTER each creature's own children (move options/skill slots)
        // are already positioned relative to its untransformed 72x72 box — translate doesn't
        // affect that inner math, it just shifts the whole already-laid-out cluster.
        ApplyStageCreatureStagger(_playerStageCreatures);

        // Radial nameplates (2026-08-06, user-directed — see DECISIONS.md -> [Combat]) — built
        // procedurally rather than hand-authored in UXML (see NameplateRefs' doc comment), one
        // "invisible container" per slot, up to MaxNameplateSlots per side. Appended into the
        // PlayerStatusList/EnemyStatusList containers UXML still declares (now empty).
        VisualElement playerStatusList = _root.Q<VisualElement>("PlayerStatusList");
        VisualElement enemyStatusList = _root.Q<VisualElement>("EnemyStatusList");
        for (int i = 0; i < MaxNameplateSlots; i++)
        {
            int capturedPlayerSlotIndex = i;
            _playerNameplates[i] = BuildNameplate(() => BurstBarClicked?.Invoke(capturedPlayerSlotIndex));
            playerStatusList.Add(_playerNameplates[i].Container);

            _enemyNameplates[i] = BuildNameplate(onRingClicked: null); // enemies don't activate Evolution Burst yet
            enemyStatusList.Add(_enemyNameplates[i].Container);
        }

        _enemyStageCreature = _root.Q<VisualElement>("EnemyStageSlot0");

        _actionAnnouncement = _root.Q<VisualElement>("ActionAnnouncement");
        _actionAnnouncementLabel = _root.Q<Label>("ActionAnnouncementLabel");

        _continuePrompt = _root.Q<VisualElement>("ContinuePrompt");
        _continuePromptLabel = _root.Q<Label>("ContinuePromptLabel");

        // Deliberately separate from the auto-hiding ContinuePrompt/ActionAnnouncement dialogue
        // boxes above (2026-08-06, user-directed — see DECISIONS.md -> [Combat]: "an end turn
        // button thats separate from the dialogue boxes but is clear... the dialog boxes are
        // there for now, but i think they will be removed once there is more UI feedback to
        // player" — this button is not planned for removal, unlike those). Visibility is
        // BattleManager-controlled (SetEndTurnButtonVisible), shown only during the player's turn.
        _endTurnButton = _root.Q<Button>("EndTurnButton");
        _endTurnButton.clicked += () => EndTurnClicked?.Invoke();
        _endTurnButton.style.display = DisplayStyle.None;

        _battleLogScrollView = _root.Q<ScrollView>("BattleLogScrollView");
        _battleLogContent = _root.Q<VisualElement>("BattleLogContent");

        _dragLine = new DragLineVisual { style = { display = DisplayStyle.None } };
        _stage.Add(_dragLine);

        _hudTooltip = new Label { pickingMode = PickingMode.Ignore, style = { display = DisplayStyle.None } };
        _hudTooltip.AddToClassList("hud-tooltip");
        _root.Add(_hudTooltip); // added last -> paints above every other root child

        _timingRing = new RingVisual();
        _timingRing.AddToClassList("timing-ring");
        _timingRing.style.display = DisplayStyle.None;

        _actionAnnouncement.style.display = DisplayStyle.None;
        _continuePrompt.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Builds one "invisible container" nameplate (2026-08-06, user-directed — see DECISIONS.md
    /// -> [Combat]) — name on top, an HP/Aura/Evo readout in whichever visual ActiveNameplateStyle
    /// currently selects (Radial: RadialGaugeVisual ring + 3 tiny always-on stat labels. Bars:
    /// 2026-08 follow-up mockup, 3 stacked horizontal rectangles with current/total shown on
    /// hover), and a wrapping buff row underneath with Regen/Burst icon sockets (both hidden until
    /// SetRegenStatus/SetBurstStatus actually activate them — see those methods, shared by both
    /// styles unchanged). onRingClicked fires on any pointer-down over whichever element activates
    /// Evolution Burst for the active style (ring-wrap, or the Evo bar's track); pass null for
    /// slots that shouldn't be clickable (enemies don't activate Evolution Burst yet). Instance
    /// method (not static, unlike before the Bars mockup) so the Bars branch can register hover
    /// callbacks against this controller's shared _hudTooltip.
    /// </summary>
    private NameplateRefs BuildNameplate(Action onRingClicked)
    {
        var np = new NameplateRefs();

        var container = new VisualElement();
        container.AddToClassList("nameplate");
        np.Container = container;

        var name = new Label();
        name.AddToClassList("nameplate-name");
        if (ActiveNameplateStyle == NameplateStyle.Bars) name.AddToClassList("nameplate-name-bars");
        container.Add(name);
        np.NameLabel = name;

        if (ActiveNameplateStyle == NameplateStyle.Bars)
        {
            var barsWrap = new VisualElement();
            barsWrap.AddToClassList("nameplate-bars-wrap");
            container.Add(barsWrap);

            np.HPBarFill = BuildNameplateBarRow(barsWrap, "nameplate-bar-hp", out VisualElement hpTrack);
            np.AuraBarFill = BuildNameplateBarRow(barsWrap, "nameplate-bar-aura", out VisualElement auraTrack);
            np.EvoBarFill = BuildNameplateBarRow(barsWrap, "nameplate-bar-evo", out VisualElement evoTrack);
            np.EvoBarTrack = evoTrack;

            RegisterHudTooltipHover(hpTrack, () => np.HPTooltipText);
            RegisterHudTooltipHover(auraTrack, () => np.AuraTooltipText);
            RegisterHudTooltipHover(evoTrack, () => np.EvoTooltipText);

            if (onRingClicked != null)
                evoTrack.RegisterCallback<PointerDownEvent>(evt => onRingClicked());
        }
        else
        {
            var ringWrap = new VisualElement();
            ringWrap.AddToClassList("nameplate-ring-wrap");
            if (onRingClicked != null)
                ringWrap.RegisterCallback<PointerDownEvent>(evt => onRingClicked());

            var portrait = new VisualElement();
            portrait.AddToClassList("nameplate-portrait");
            ringWrap.Add(portrait);

            var gauge = new RadialGaugeVisual();
            gauge.AddToClassList("nameplate-gauge");
            ringWrap.Add(gauge);

            container.Add(ringWrap);
            np.RingWrap = ringWrap;
            np.Portrait = portrait;
            np.Gauge = gauge;

            var stats = new VisualElement();
            stats.AddToClassList("nameplate-stats");
            var hpStat = new Label();
            hpStat.AddToClassList("nameplate-stat");
            hpStat.AddToClassList("nameplate-stat-hp");
            stats.Add(hpStat);
            var auraStat = new Label();
            auraStat.AddToClassList("nameplate-stat");
            auraStat.AddToClassList("nameplate-stat-aura");
            stats.Add(auraStat);
            var evoStat = new Label();
            evoStat.AddToClassList("nameplate-stat");
            evoStat.AddToClassList("nameplate-stat-evo");
            stats.Add(evoStat);
            container.Add(stats);
            np.HPStat = hpStat;
            np.AuraStat = auraStat;
            np.EvoStat = evoStat;
        }

        var buffRow = new VisualElement();
        buffRow.AddToClassList("nameplate-buffs");
        container.Add(buffRow);

        (VisualElement regenIcon, Label regenCounter) = BuildBuffIcon(buffRow, "R", "nameplate-buff-regen");
        (VisualElement burstIcon, Label burstCounter) = BuildBuffIcon(buffRow, "B", "nameplate-buff-burst");
        np.RegenIcon = regenIcon;
        np.RegenCounter = regenCounter;
        np.BurstIcon = burstIcon;
        np.BurstCounter = burstCounter;

        return np;
    }

    /// <summary>Builds one Bars-style stat row (track + inner fill rect) inside `parent`, tagged with `colorClass` (nameplate-bar-hp/aura/evo — drives fill color via USS). Returns the fill element (width set to a percentage per-update); outputs the track (the hover/click target — stable full-width, unlike the fill which shrinks).</summary>
    private static VisualElement BuildNameplateBarRow(VisualElement parent, string colorClass, out VisualElement track)
    {
        var row = new VisualElement();
        row.AddToClassList("nameplate-bar-row");
        parent.Add(row);

        track = new VisualElement();
        track.AddToClassList("nameplate-bar-track");
        track.AddToClassList(colorClass);
        row.Add(track);

        var fill = new VisualElement();
        fill.AddToClassList("nameplate-bar-fill");
        fill.pickingMode = PickingMode.Ignore; // decorative — track (its parent) is the unambiguous hover/click target, not whichever of the two happens to be topmost at a given point
        track.Add(fill);

        return fill;
    }

    /// <summary>One small lettered circle + a bottom-right countdown subscript, matching the old status-icon convention — appended into parent, hidden by default.</summary>
    private static (VisualElement icon, Label counter) BuildBuffIcon(VisualElement parent, string letter, string colorClass)
    {
        var icon = new VisualElement();
        icon.AddToClassList("nameplate-buff-icon");
        icon.AddToClassList(colorClass);

        var letterLabel = new Label(letter);
        letterLabel.AddToClassList("nameplate-buff-icon-label");
        icon.Add(letterLabel);

        var counter = new Label();
        counter.AddToClassList("nameplate-buff-icon-counter");
        icon.Add(counter);

        parent.Add(icon);
        return (icon, counter);
    }

    /// <summary>
    /// Populates name plates, initial stat readouts, and stage creature colors. Hides nameplate/
    /// stage slots beyond playerSide.Count/enemySide.Count. Gauges are set here AND kept live by
    /// RefreshBars — attacks spend Aura and a perfect Dodge/Parry restores it (2026-08-05, user-
    /// directed — see DECISIONS.md -> [Combat]).
    /// </summary>
    public void Initialize(List<BattleParticipant> playerSide, List<BattleParticipant> enemySide, SkillDatabase skillDatabase = null)
    {
        for (int i = 0; i < MaxNameplateSlots; i++)
        {
            bool hasSlot = i < playerSide.Count;
            _playerNameplates[i].Container.style.display = hasSlot ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hasSlot) continue;

            ApplyNameplateSize(_playerNameplates[i], playerSide.Count);
            BattleParticipant p = playerSide[i];
            _playerNameplates[i].NameLabel.text = p.DisplayName;
            SetNameplatePortraitColor(_playerNameplates[i], p);
            RefreshNameplateStats(_playerNameplates[i], p);
        }

        for (int i = 0; i < BattleConfig.ActivePartySize; i++)
        {
            bool hasSlot = i < playerSide.Count;
            _playerStageCreatures[i].style.display = hasSlot ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasSlot)
            {
                SetStageCreatureColor(_playerStageCreatures[i], playerSide[i]);
                PopulateSkillRing(i, playerSide[i], skillDatabase);
            }
        }
        LayoutPlayerStageCreatures(playerSide.Count);

        for (int i = 0; i < MaxNameplateSlots; i++)
        {
            bool hasSlot = i < enemySide.Count;
            _enemyNameplates[i].Container.style.display = hasSlot ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hasSlot) continue;

            ApplyNameplateSize(_enemyNameplates[i], enemySide.Count);
            BattleParticipant e = enemySide[i];
            _enemyNameplates[i].NameLabel.text = e.DisplayName;
            SetNameplatePortraitColor(_enemyNameplates[i], e);
            RefreshNameplateStats(_enemyNameplates[i], e);
        }

        // Single enemy STAGE slot only — see class doc comment. Separate from the (up to 7)
        // enemy nameplates above; multi-enemy battles don't have stage creatures for each yet.
        bool hasEnemy = enemySide.Count > 0;
        _enemyStageCreature.style.display = hasEnemy ? DisplayStyle.Flex : DisplayStyle.None;
        if (hasEnemy) SetStageCreatureColor(_enemyStageCreature, enemySide[0]);

        RefreshBars(playerSide, enemySide);
    }

    /// <summary>Refreshes every nameplate's gauge/stat readout and fades the stage creature circle once a participant is down. Renamed from RefreshHP 2026-08-05 once Aura started changing during battle too (attack costs, perfect-defense restores).</summary>
    public void RefreshBars(List<BattleParticipant> playerSide, List<BattleParticipant> enemySide)
    {
        for (int i = 0; i < MaxNameplateSlots && i < playerSide.Count; i++)
            RefreshNameplateStats(_playerNameplates[i], playerSide[i]);

        for (int i = 0; i < BattleConfig.ActivePartySize && i < playerSide.Count; i++)
            SetStageCreatureAliveState(_playerStageCreatures[i], playerSide[i]);

        for (int i = 0; i < MaxNameplateSlots && i < enemySide.Count; i++)
            RefreshNameplateStats(_enemyNameplates[i], enemySide[i]);

        if (enemySide.Count > 0)
            SetStageCreatureAliveState(_enemyStageCreature, enemySide[0]);
    }

    /// <summary>
    /// Drives the HP/Aura/Evo readout (whichever style is active) off a live participant, plus
    /// fades the whole nameplate when down — mirrors SetStageCreatureAliveState for the stage ball.
    /// Evo goes through the shared ApplyEvoVisual helper (see its doc comment) since it also has a
    /// second, independent call site in SetBurstFillBar.
    /// </summary>
    private static void RefreshNameplateStats(NameplateRefs np, BattleParticipant p)
    {
        float hpPercent = p.MaxHP > 0 ? (float)p.CurrentHP / p.MaxHP * 100f : 0f;
        float auraPercent = p.MaxAura > 0 ? (float)p.CurrentAura / p.MaxAura * 100f : 0f;
        float evoPercent = p.BurstGauge.FillPercent;
        bool ready = !p.BurstGauge.IsActive && evoPercent >= EvolutionBurstSystem.TriggerThreshold;

        if (ActiveNameplateStyle == NameplateStyle.Bars)
        {
            np.HPBarFill.style.width = Length.Percent(hpPercent);
            np.AuraBarFill.style.width = Length.Percent(auraPercent);
            np.HPTooltipText = $"HP: {p.CurrentHP}/{p.MaxHP}";
            np.AuraTooltipText = $"Aura: {p.CurrentAura}/{p.MaxAura}";
        }
        else
        {
            np.Gauge.HPPercent = hpPercent;
            np.Gauge.AuraPercent = auraPercent;
            np.HPStat.text = $"{p.CurrentHP}/{p.MaxHP}";
            np.AuraStat.text = $"{p.CurrentAura}/{p.MaxAura}";
        }

        ApplyEvoVisual(np, evoPercent, ready); // also does the Radial branch's single Gauge.Refresh() call, after HP/Aura are already set above

        np.Container.style.opacity = p.IsAlive ? 1f : 0.4f;
    }

    private static readonly Color NameplateEvoFillingColor = new Color(150f / 255f, 90f / 255f, 220f / 255f);
    private static readonly Color NameplateEvoReadyColor = new Color(230f / 255f, 210f / 255f, 60f / 255f);

    /// <summary>Text switches to "ready" in the same gold as the gauge's ready outline (2026-08-06 — verified live: the stat text was staying purple even once ready, inconsistent with the ring) so the two readouts always agree.</summary>
    private static void SetEvoStatText(Label evoStat, bool ready, float evoPercent)
    {
        evoStat.text = ready ? "ready" : $"{Mathf.RoundToInt(evoPercent)}%";
        evoStat.style.color = ready ? NameplateEvoReadyColor : NameplateEvoFillingColor;
    }

    /// <summary>
    /// Shared Evo-readout update for whichever style is active — has two call sites
    /// (RefreshNameplateStats above, and SetBurstFillBar, called independently by BattleManager
    /// after a burst-gauge fill) that must never disagree about what a click will do, so both route
    /// through here instead of duplicating the branch. Bars: sets the Evo bar's fill width and
    /// toggles its "ready" gold color class; Radial: unchanged prior behavior (gauge fields +
    /// Gauge.Refresh() + the stat label's text/color).
    /// </summary>
    private static void ApplyEvoVisual(NameplateRefs np, float fillPercent, bool ready)
    {
        fillPercent = Mathf.Clamp(fillPercent, 0f, 100f);

        if (ActiveNameplateStyle == NameplateStyle.Bars)
        {
            np.EvoBarFill.style.width = Length.Percent(fillPercent);
            // Set as an explicit inline style, not just the (also-present) .nameplate-bar-evo-ready
            // class — USS's own descendant rule .nameplate-bar-evo .nameplate-bar-fill has equal
            // selector-count specificity to a same-element compound class rule, and in practice
            // that descendant rule kept winning regardless of declaration order (verified live: the
            // class was present but the resolved background stayed purple). An inline style always
            // wins over any stylesheet rule, sidestepping the ambiguity entirely. Reuses the same
            // NameplateEvoReadyColor/NameplateEvoFillingColor the Radial style's stat-text color
            // already uses, so both styles agree on what "ready" looks like.
            np.EvoBarFill.style.backgroundColor = ready ? NameplateEvoReadyColor : NameplateEvoFillingColor;
            np.EvoBarFill.EnableInClassList("nameplate-bar-evo-ready", ready);
            np.EvoTooltipText = ready ? "Evo: ready" : $"Evo: {Mathf.RoundToInt(fillPercent)}%";

            // Flashing perimeter highlight while ready (2026-08 follow-up — user-directed:
            // "highlighting the perimeter of the evo box and have it flash lightly"). Only
            // starts/stops the schedule on an actual ready-state TRANSITION (guarded by
            // EvoFlashActive) — this method runs on every stat refresh, far more often than
            // readiness actually changes, so re-scheduling every call would stack redundant
            // repeating callbacks.
            if (ready && !np.EvoFlashActive)
            {
                np.EvoFlashActive = true;
                np.EvoBarTrack.AddToClassList("nameplate-bar-evo-track-ready");
                np.EvoFlashSchedule = np.EvoBarTrack.schedule
                    .Execute(() => np.EvoBarTrack.ToggleInClassList("nameplate-bar-evo-flash"))
                    .Every(EvoFlashIntervalMs);
            }
            else if (!ready && np.EvoFlashActive)
            {
                np.EvoFlashActive = false;
                np.EvoFlashSchedule?.Pause();
                np.EvoBarTrack.RemoveFromClassList("nameplate-bar-evo-track-ready");
                np.EvoBarTrack.RemoveFromClassList("nameplate-bar-evo-flash");
            }
            return;
        }

        np.Gauge.EvoPercent = fillPercent;
        np.Gauge.EvoReady = ready;
        np.Gauge.Refresh();
        SetEvoStatText(np.EvoStat, ready, fillPercent);
    }

    private static void SetNameplatePortraitColor(NameplateRefs np, BattleParticipant participant)
    {
        if (np.Portrait == null) return; // Bars style has no portrait element
        PhasixData species = participant.RuntimeData.speciesData;
        if (species == null) return;
        np.Portrait.style.backgroundColor = PrimalTypeColor.GetColor(species.PrimalType);
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
    /// Per-index vertical stagger, in px — index-matched to slot order, not sized to any
    /// particular party count, so it applies equally to a future multi-slot EnemyStageArea (2026-
    /// 08-06, user-directed — see DECISIONS.md -> [Combat]: "Each phasix needs to have its own
    /// orb slots revolving around it, both enemy and player"). A simple front/back/front zigzag —
    /// not a recreation of any specific reference game's exact formation, just enough spread that
    /// 3 creatures in a row read as staggered rather than flat-aligned. Indices beyond this
    /// array's length fall back to 0 (see ApplyStageCreatureStagger).
    /// </summary>
    private static readonly float[] StageCreatureStaggerY = { 0f, -45f, 25f };

    /// <summary>Column width in px for LayoutPlayerStageCreatures — 72 (.stage-creature width) + 28+28 (the old flex margin-left/right), preserved so the switch to absolute positioning didn't change the on-screen spacing.</summary>
    private const float StageCreatureColumnWidth = 128f;
    private const float StageCreatureEdgeGap = 28f;

    /// <summary>
    /// Places each player stage creature at an explicit `left`, compacted left-to-right over
    /// however many are actually in the party this battle (party always fills from slot 0, so
    /// "visible" is always a contiguous 0..visibleCount-1 range — no gap-skipping needed) — and
    /// sizes PlayerStageArea to match, since `.stage-side-player`'s own `translate: -50% -50%`
    /// centering depends on its box having a real size once its children are absolutely
    /// positioned (2026-08-06 — see LayoutPlayerStageCreatures' sibling doc comment on
    /// .stage-creature in BattleHUD.uss for why absolute over flex). Called once from Initialize,
    /// after party size is known; each creature's own OWN internal box (move options/skill
    /// slots — PositionMoveOptions/PositionSkillSlots) is a completely separate coordinate space
    /// and unaffected by this. Also the intended hook for a future "place this Phasix here"
    /// formation/positioning feature — flagged as deferred in DECISIONS.md, this method is where
    /// per-slot placement would stop being "always column i" and become player-chosen.
    /// </summary>
    private void LayoutPlayerStageCreatures(int visibleCount)
    {
        for (int i = 0; i < visibleCount && i < _playerStageCreatures.Length; i++)
            _playerStageCreatures[i].style.left = i * StageCreatureColumnWidth + StageCreatureEdgeGap;

        _playerStageArea.style.width = Mathf.Max(visibleCount, 1) * StageCreatureColumnWidth;
        _playerStageArea.style.height = 72f;
    }

    /// <summary>
    /// Applies StageCreatureStaggerY as a `translate` — a pure rendering-time transform, not a
    /// layout change, so it doesn't disturb LayoutPlayerStageCreatures' `left` positions and
    /// doesn't affect PositionMoveOptions/PositionSkillSlots' math (both already computed relative
    /// to each creature's own untransformed 72x72 box before this runs). Also reorders the
    /// siblings back-to-front by stagger offset (see below) — 2026-08-06, user caught the front
    /// lane's skill orbs rendering behind a further-back lane's creature. Safe to reorder freely
    /// now that .stage-creature is absolutely positioned (BringToFront no longer moves a creature
    /// within a flex row — see BattleHUD.uss comment on .stage-creature for the bug this fixed).
    /// </summary>
    private void ApplyStageCreatureStagger(VisualElement[] creatures)
    {
        for (int i = 0; i < creatures.Length; i++)
        {
            float offsetY = i < StageCreatureStaggerY.Length ? StageCreatureStaggerY[i] : 0f;
            creatures[i].style.translate = new Translate(0, offsetY);
        }
        RestoreStageCreatureDepthOrder();
    }

    /// <summary>
    /// UI Toolkit draws siblings in document order (painter's algorithm) — the flex-row's fixed
    /// 0,1,2 order doesn't match visual depth once stagger is applied, so a "further back"
    /// creature (smaller/negative Y) could draw over a "front" creature's orb ring. BringToFront
    /// in ascending-Y order rebuilds document order to match depth: furthest-back drawn first,
    /// frontmost drawn last (on top). Called after stagger setup and again whenever a move wheel
    /// closes, to undo the temporary override ShowMoveSelection/ShowMoveSelectionReadOnly apply
    /// below.
    /// </summary>
    private void RestoreStageCreatureDepthOrder()
    {
        var depthOrder = new int[_playerStageCreatures.Length];
        for (int i = 0; i < depthOrder.Length; i++) depthOrder[i] = i;
        Array.Sort(depthOrder, (a, b) =>
        {
            float ay = a < StageCreatureStaggerY.Length ? StageCreatureStaggerY[a] : 0f;
            float by = b < StageCreatureStaggerY.Length ? StageCreatureStaggerY[b] : 0f;
            return ay.CompareTo(by);
        });
        foreach (int index in depthOrder)
            _playerStageCreatures[index].BringToFront();
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
    /// Builds skill-orb tooltip text from the skill's own RESOLVED mechanical behavior (2026-08
    /// follow-up — user-directed: "use the values from each skill orb to generate your content"),
    /// not the shared placeholder SkillData.Description string (identical dev-facing disclaimer
    /// text across all 36 assets — not player-facing information, and not differentiated per
    /// skill). Every line here IS differentiated per skill, derived via PlaceholderSkillResolver
    /// from data that's already GDD-locked elsewhere (SkillTreeCatalog, StatusEffectCatalog) — see
    /// that class's own doc comment for why this counts as wiring, not invented balance content.
    /// Damage skills show their resolved Physical/Elemental category and the shared placeholder
    /// Power; status skills show the resolved status and its REAL duration range from
    /// StatusEffectCatalog (the exact number still depends on live Resonance/Resolve at cast time —
    /// StatusDurationCalculator — so this is the base range, not a promise of the exact value).
    /// </summary>
    private static string BuildSkillTooltipText(SkillData skill)
    {
        PlaceholderSkillResolver.SkillResolution resolution = PlaceholderSkillResolver.Resolve(skill);

        string effectLine;
        string targetLine;
        if (resolution.DealsDamage)
        {
            effectLine = $"{resolution.Category} damage — Power {BattleConfig.PlaceholderSkillPower}";
            targetLine = "Target: Enemy";
        }
        else
        {
            StatusEffectType status = resolution.AppliedStatus.Value;
            StatusEffectCatalog.Entry entry = StatusEffectCatalog.Get(status);
            string durationText = entry.MinDurationTurns == entry.MaxDurationTurns
                ? $"{entry.MinDurationTurns} turns"
                : $"{entry.MinDurationTurns}-{entry.MaxDurationTurns} turns";
            effectLine = $"Applies {status} ({durationText})";
            targetLine = resolution.SelfTargeted ? "Target: Self" : "Target: Enemy";
        }

        return $"{skill.SkillName}\n{effectLine}\n{targetLine}\nAura Cost: {BattleConfig.PlaceholderSkillAuraCost}";
    }

    /// <summary>Positions _hudTooltip just to the right of `anchor`'s own bounds, top-aligned with it, in _root's local space — "near/next to" the hovered element (2026-08 follow-up, user-directed: "expecting it be near/next to the skill we're hovering") rather than following the cursor, which the first pass did.</summary>
    private void PositionHudTooltipNear(VisualElement anchor)
    {
        Rect localRect = _root.WorldToLocal(anchor.worldBound);
        _hudTooltip.style.left = localRect.xMax + HudTooltipAnchorGap;
        _hudTooltip.style.top = localRect.yMin;
    }

    /// <summary>Registers hover show/hide on `track`, pulling fresh text from `getText` at hover time (so a caller's live-updated cache, e.g. NameplateRefs.HPTooltipText, is always current — same "read at event time" pattern as the skill-ring tooltip above). Position is set once on Enter, not tracked on Move — `track` itself doesn't move while hovered, so there's nothing to re-follow.</summary>
    private void RegisterHudTooltipHover(VisualElement track, Func<string> getText)
    {
        track.RegisterCallback<PointerEnterEvent>(evt =>
        {
            _hudTooltip.text = getText();
            _hudTooltip.style.display = DisplayStyle.Flex;
            PositionHudTooltipNear(track);
        });
        track.RegisterCallback<PointerLeaveEvent>(evt => _hudTooltip.style.display = DisplayStyle.None);
    }

    /// <summary>
    /// Resolves a participant's equippedSkillGuids into the SkillRingSlotCount real skill-ring
    /// slots (2026-08 session — Combo/Status/Chain/Mastery wiring, see DECISIONS.md -> [Combat]).
    /// Slots beyond the creature's equipped count (or all of them, if skillDatabase is null) stay
    /// tagged .skill-slot-locked (dim, no cursor) — their registered click handler already no-ops
    /// on a null entry, so nothing further is needed to make them inert. Call once per Initialize.
    /// </summary>
    private void PopulateSkillRing(int slotIndex, BattleParticipant participant, SkillDatabase skillDatabase)
    {
        for (int ring = 0; ring < SkillRingSlotCount; ring++)
        {
            VisualElement slot = _playerSkillSlots[slotIndex][SkillRingSlotOffset + ring];
            SkillData skill = null;

            if (skillDatabase != null && ring < participant.RuntimeData.equippedSkillGuids.Count)
            {
                string guid = participant.RuntimeData.equippedSkillGuids[ring];
                skillDatabase.TryGetByGuid(guid, out skill);
            }

            _playerSkillSlotSkills[slotIndex][ring] = skill;
            slot.EnableInClassList("skill-slot-locked", skill == null);
            // Hover tooltip content (name, description, flat placeholder Aura cost) is read live
            // off _playerSkillSlotSkills by the PointerEnterEvent handler registered in Awake —
            // nothing to set here beyond keeping that array current, which the line above already does.

            // Lettering (2026-08 follow-up — see _playerSkillSlotLabels' doc comment): shows the
            // equipped skill's own SkillName, empty for a locked slot.
            _playerSkillSlotLabels[slotIndex][ring].text = skill != null ? skill.SkillName : string.Empty;

            // Fill/border color owned by RING POSITION, not species PrimalType (2026-08 follow-up
            // — user-directed: "select whatever colors you see fit for those" — see
            // SkillRingColorCount's doc comment for why position-owned, matching A/C/H/R/K).
            slot.EnableInClassList($"skill-ring-color-{ring % SkillRingColorCount}", skill != null);
        }
    }

    /// <summary>
    /// Shows a small combo-streak counter badge on whichever real skill-ring slot currently holds
    /// `skill` (2026-08 session — user-directed: "counter next to the skill on the skill wheel,"
    /// see DECISIONS.md -> [Combat]). No-op if that skill isn't currently equipped/visible in this
    /// slot's ring (e.g. it was unequipped mid-battle, which doesn't happen today but shouldn't
    /// throw if it ever does).
    /// </summary>
    public void SetSkillComboCounter(int slotIndex, SkillData skill, int count)
    {
        SkillData[] skills = _playerSkillSlotSkills[slotIndex];
        for (int ring = 0; ring < skills.Length; ring++)
        {
            if (skills[ring] != skill) continue;

            Label badge = _playerSkillSlotComboBadges[slotIndex][ring];
            badge.text = count.ToString();
            badge.style.display = DisplayStyle.Flex;
            return;
        }
    }

    /// <summary>
    /// Hides every combo-streak badge on this slot's skill ring. BattleManager calls this before
    /// re-showing whichever badges are currently true after each skill use, so a broken streak's
    /// stale badge (on a skill no longer contributing to any active combo) doesn't linger.
    /// </summary>
    public void ClearAllSkillComboCounters(int slotIndex)
    {
        foreach (Label badge in _playerSkillSlotComboBadges[slotIndex])
        {
            if (badge != null) badge.style.display = DisplayStyle.None;
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
    /// same for every move. Clears any prior ShowMoveSelectionReadOnly state for this slot — a
    /// creature that already acted only ever reaches THIS method again once a new player turn
    /// resets HasActedThisTurn. Call HideMoveSelection to cancel/clean up early.
    /// </summary>
    public void ShowMoveSelection(int attackerSlotIndex, BattleParticipant self, List<BattleParticipant> enemyTargets,
        Action<ChosenMove> onMoveConfirmed)
    {
        _self = self;
        _enemyTargets = enemyTargets;
        _onMoveConfirmed = onMoveConfirmed;
        _playerSlotReadOnly[attackerSlotIndex] = false;
        SetMoveOptionsVisible(attackerSlotIndex, true);
        _playerStageCreatures[attackerSlotIndex].BringToFront();
    }

    /// <summary>
    /// Shows a player's move wheel in READ-ONLY mode — every orb visible but greyed
    /// (`.move-option-disabled`) and non-interactive (BeginDrag refuses to start a drag for a
    /// read-only slot). Used when the player clicks a Phasix that's already used its action this
    /// turn (2026-08-06, user-directed — see DECISIONS.md -> [Combat]: "if the phasix already
    /// moved during its turn then it can still show, but will be greyed out for active skills").
    /// No target/callback state is set — there's nothing to confirm in this mode. Deliberately
    /// greys out ALL current move options; see BattleParticipant.HasActedThisTurn's doc comment
    /// for why this is a per-slot flag rather than something baked into a specific move, leaving
    /// room for a future synergy/passive exception without restructuring this method.
    /// </summary>
    public void ShowMoveSelectionReadOnly(int slotIndex)
    {
        _playerSlotReadOnly[slotIndex] = true;
        SetMoveOptionsVisible(slotIndex, true);
        _playerStageCreatures[slotIndex].BringToFront();
    }

    /// <summary>Hides any visible move options and cancels an in-progress drag, if any. Safe to call even when nothing is shown.</summary>
    public void HideMoveSelection()
    {
        for (int i = 0; i < BattleConfig.ActivePartySize; i++) SetMoveOptionsVisible(i, false);
        EndDrag();
        _self = null;
        _enemyTargets = null;
        _onMoveConfirmed = null;
        // Belt-and-suspenders: a slot hidden (display: None) out from under a hovering pointer
        // doesn't reliably fire PointerLeaveEvent, so the tooltip could otherwise linger after
        // the wheel that owned it disappears.
        _hudTooltip.style.display = DisplayStyle.None;
        // Undo ShowMoveSelection/ShowMoveSelectionReadOnly's BringToFront override — back to
        // static lane-depth order now that no wheel is open.
        RestoreStageCreatureDepthOrder();
    }

    /// <summary>Shows/hides the always-on-during-the-player's-turn End Turn button (2026-08-06, user-directed — see DECISIONS.md -> [Combat]). BattleManager calls this at the start/end of PlayerTurn.</summary>
    public void SetEndTurnButtonVisible(bool visible)
    {
        _endTurnButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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
        _playerNameplates[slotIndex].RegenIcon.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        _playerNameplates[slotIndex].RegenCounter.text = active ? turnsRemaining.ToString() : "";
    }

    /// <summary>Shows/updates/hides a player's Evolution Burst status icon and countdown — same pattern as SetRegenStatus (2026-08-06, wiring EvolutionBurstSystem into the live loop). turnsRemaining &lt;= 0 hides the icon.</summary>
    public void SetBurstStatus(int slotIndex, int turnsRemaining)
    {
        bool active = turnsRemaining > 0;
        _playerNameplates[slotIndex].BurstIcon.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        _playerNameplates[slotIndex].BurstCounter.text = active ? turnsRemaining.ToString() : "";
    }

    /// <summary>
    /// Updates a player's Evolution Burst readout (the radial nameplate's Evo arc, or the Evo bar
    /// in Bars style, 2026-08-06 — see DECISIONS.md -> [Combat]) fill and its "ready to activate"
    /// state. `ready` should be computed by the caller as `fillPercent &gt;=
    /// EvolutionBurstSystem.TriggerThreshold &amp;&amp; !gauge.IsActive` — the SAME threshold
    /// ActivateReady itself checks, so the "ready" visual never promises something a click won't
    /// actually deliver. Routes through the shared ApplyEvoVisual — see its doc comment for why.
    /// </summary>
    public void SetBurstFillBar(int slotIndex, float fillPercent, bool ready)
    {
        ApplyEvoVisual(_playerNameplates[slotIndex], fillPercent, ready);
    }

    private void SetMoveOptionsVisible(int slotIndex, bool visible)
    {
        // Only grey out orbs while actually showing a read-only wheel — irrelevant while hiding.
        bool readOnly = visible && _playerSlotReadOnly[slotIndex];
        foreach (VisualElement option in _playerMoveOptions[slotIndex])
        {
            option.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            option.EnableInClassList("move-option-disabled", readOnly);
        }

        // Empty skill slots show/hide in lockstep with "A"/"C" — the whole radial wheel appears
        // and disappears together (2026-08-06, user-directed — see DECISIONS.md -> [Combat]).
        foreach (VisualElement slot in _playerSkillSlots[slotIndex])
            slot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        // Real, populated skill-ring slots grey out in read-only mode too, same as the 5 built-in
        // moves (2026-08 session, see DECISIONS.md -> [Combat]) — locked/empty slots are already
        // visually dim via .skill-slot-locked regardless of read-only state, so only touch
        // populated ones here.
        for (int ring = 0; ring < SkillRingSlotCount; ring++)
        {
            SkillData skill = _playerSkillSlotSkills[slotIndex] != null ? _playerSkillSlotSkills[slotIndex][ring] : null;
            if (skill != null) _playerSkillSlots[slotIndex][SkillRingSlotOffset + ring].EnableInClassList("move-option-disabled", readOnly);
        }
    }

    private void BeginDragForSkill(int slotIndex, PointerDownEvent evt, SkillData skill)
    {
        if (_playerSlotReadOnly[slotIndex]) return;

        SetMoveOptionsVisible(slotIndex, false);
        _draggingFromSlotIndex = slotIndex;
        _draggingOptionIndex = -1;
        _draggingSkill = skill;

        Vector2 startWorld = _playerStageCreatures[slotIndex].worldBound.center;
        _dragLine.Start = _dragLine.WorldToLocal(startWorld);
        _dragLine.End = _dragLine.WorldToLocal(evt.position);
        _dragLine.style.display = DisplayStyle.Flex;
        _dragLine.Refresh();

        _root.CapturePointer(evt.pointerId);
        _root.RegisterCallback<PointerMoveEvent>(OnDragPointerMove);
        _root.RegisterCallback<PointerUpEvent>(OnDragPointerUp);
    }

    private void BeginDrag(int slotIndex, PointerDownEvent evt, int optionIndex)
    {
        // Greyed-out "already acted" wheel (ShowMoveSelectionReadOnly) — look, don't touch.
        if (_playerSlotReadOnly[slotIndex]) return;

        SetMoveOptionsVisible(slotIndex, false);
        _draggingFromSlotIndex = slotIndex;
        _draggingOptionIndex = optionIndex;
        _draggingSkill = null;

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
        SkillData draggingSkill = _draggingSkill;
        EndDrag();

        // Skill-ring drags resolve self-vs-enemy targeting the same way built-in moves do
        // (MoveOptionIsSelfOnly), just via PlaceholderSkillResolver instead of a fixed per-index
        // array — see DECISIONS.md -> [Combat], 2026-08 session.
        bool selfOnly = draggingSkill != null
            ? PlaceholderSkillResolver.Resolve(draggingSkill).SelfTargeted
            : optionIndex >= 0 && MoveOptionIsSelfOnly[optionIndex];
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
            Action<ChosenMove> callback = _onMoveConfirmed;
            _self = null;
            _enemyTargets = null;
            _onMoveConfirmed = null;
            ChosenMove chosen = draggingSkill != null
                ? new ChosenMove(null, draggingSkill, target)
                : new ChosenMove(optionIndex, null, target);
            callback?.Invoke(chosen);
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
        _draggingSkill = null;
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
