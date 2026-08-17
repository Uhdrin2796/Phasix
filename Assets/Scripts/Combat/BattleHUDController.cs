using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

/// <summary>
/// Radial nameplates (up to MaxNameplateSlots per side — 2026-08-06, user-directed, replacing the
/// old stacked HP/Aura/Burst bars, see DECISIONS.md -> [Combat]), placeholder stage creatures,
/// Sonny 2-style radial move selection + drag-to-target, the shared converging-ring action-command
/// timing visual (offense on the targeted enemy, defense on the defending player creature), a
/// fully auto-paced beat message (ShowTimedMessage — no click-to-proceed gate anywhere in the
/// battle as of 2026-08-06, see DECISIONS.md -> [Combat]), and a scrolling text battle log for
/// BattleScene_Main. MonoBehaviour singleton wrapping a UIDocument (see DECISIONS.md -> [UI] for
/// why UI Toolkit over uGUI).
/// The STAGE (creature balls + move wheels) is still fixed at 3 player slots
/// (BattleConfig.ActivePartySize) and 1 enemy slot — multi-enemy battles (trainer fights,
/// Roadmap_v2 Mo 14-15) aren't built yet — but the nameplate SIDEBAR is a separate, wider-capacity
/// system (MaxNameplateSlots = 7) that doesn't share that cap; see NameplateRefs' doc comment.
///
/// Layout is Sonny 2-style per user reference: top nameplate sidebar per side (radial HP/Aura/Evo
/// gauge around a portrait, name above, wrapping buff row below — BuildNameplate), middle stage
/// with staggered placeholder creature circles (player left, enemy right, same lane —
/// ApplyLaneLayout, 2026-08-06, now real 7-lane depth per LaneMovementSystem), bottom action bar — no visible lane lines
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

    /// <summary>Result of RunTimedInput — which of the two nested bands (if any) the click landed in. Miss now carries a real damage penalty (TimedInputConfig.MissDamageMultiplier), unlike DefenseOutcome.Miss (2026-08-11, user-directed — see DECISIONS.md -> [Combat]).</summary>
    public enum OffenseOutcome { Miss, Good, Perfect }


    // Converging-ring sizing (px) — fixed reference target ring, and the marker ring's start/end
    // radii as it shrinks past it. Placeholder visual sizing, not gameplay-tuned.
    private const float RingTargetRadius = 30f;
    private const float RingMarkerStartRadius = 60f;
    private const float RingMarkerMinRadius = 2f;

    /// <summary>
    /// Multi-Hit Volley's own target-ring size (2026-08-15, user: "make the target square smaller
    /// so theres more room for the moving ring to converge", then same-day follow-up: "you can make
    /// the target ring slightly bigger, i just wanted the shape that converges to the target ring to
    /// be a bit bigger" — so this settled a bit above its first pass (15) rather than back at the
    /// original shared 30, and VolleyMarkerStartRadius below grew instead to do the actual work of
    /// giving the marker "more room to converge"). Deliberately a SEPARATE constant from
    /// RingTargetRadius above, not a shared one, so neither of these Volley-only tweaks touches the
    /// classic single-ring system's already-tuned feel (RunTimedInput/RunDefenseTimedInput/
    /// ComputeSweepDurationForTravelTime all still use RingTargetRadius/RingMarkerStartRadius
    /// unchanged). Every RingTargetRadius reference inside RunVolleyRingOffense/Defense and
    /// ComputeVolleyRingSweepDuration uses this instead, so the displayed target ring, the timing-
    /// sync math, and the deviation/tolerance scoring all agree on the same value.
    /// </summary>
    private const float VolleyTargetRadius = 20f;

    /// <summary>
    /// Multi-Hit Volley's own marker starting size (2026-08-15, user: "i just wanted the shape that
    /// converges to the target ring to be a bit bigger") — bigger than the classic single-ring
    /// system's shared RingMarkerStartRadius (60), same "separate Volley-only constant" reasoning as
    /// VolleyTargetRadius above. The ring's own CSS box (.timing-ring-volley in BattleHUD.uss) and
    /// PositionVolleyRing's self-centering offset were both widened to match — a marker this size
    /// would otherwise clip against the old, smaller box.
    /// </summary>
    private const float VolleyMarkerStartRadius = 80f;

    // Marker flash colors on click resolution (2026-08-05, user-directed — see DECISIONS.md ->
    // [Combat]), reworked same day from per-move colors (Dodge=orange/Parry=green) to
    // per-OUTCOME-QUALITY colors shared by Dodge/Parry/offense alike: green for a normal success,
    // a bright neon purple for a "perfect" (a tighter sub-tolerance around dead-center — see
    // PerfectToleranceFraction), red for any Miss (wrong tolerance, wrong button, or a timeout
    // with no click at all). White is the marker's normal/in-flight color.
    private static readonly Color SuccessFlashColor = new Color(90f / 255f, 200f / 255f, 100f / 255f);
    private static readonly Color PerfectFlashColor = new Color(176f / 255f, 38f / 255f, 255f / 255f);
    private static readonly Color MissFlashColor = new Color(220f / 255f, 60f / 255f, 60f / 255f);

    /// <summary>
    /// Multi-Hit Volley only (2026-08-15, user: "its hard to differentiate which ring is the main
    /// ring i should be focusing on... make it more distinct"). First pass just swapped white vs.
    /// gray — not distinct enough on its own. This version leans on the same "the PROMOTION should
    /// be an event, not a static state" principle QTE-heavy action games use (a prompt appearing/
    /// changing is what grabs attention, not merely differing from its neighbors): the active ring
    /// gets its own distinct hue (gold, not reused by any outcome-flash color) AND punches a quick
    /// scale pop the instant it's promoted (see the punch-scale call in RunVolleyRingOffense/
    /// Defense) AND plays AudioManager.PlayVolleyRingPromoted() — three simultaneous, redundant
    /// cues instead of one subtle color swap. Waiting rings are pushed further into the background
    /// (much lower alpha) so they compete less for attention in the first place.
    /// </summary>
    private static readonly Color VolleyActiveRingColor = new Color(1f, 0.84f, 0.2f);
    private static readonly Color VolleyWaitingRingColor = new Color(0.35f, 0.35f, 0.35f, 0.28f);

    /// <summary>Peak scale of a Volley ring's promotion "punch" — jumps here instantly, then VisualElementTweening.TweenUniformScale eases back to 1 over VolleyPromotionPunchDurationSeconds.</summary>
    private const float VolleyPromotionPunchScale = 1.4f;

    /// <summary>Seconds for a promoted Volley ring's punch-scale to settle back to 1 — fast enough to read as a "pop," not a slow grow.</summary>
    private const float VolleyPromotionPunchDurationSeconds = 0.18f;

    /// <summary>A hit counts as "perfect" when the marker/target ratio deviation is within this fraction of the full tolerance half-width — e.g. 0.2 means the innermost 20% of the success window. Placeholder, not tuned.</summary>
    private const float PerfectToleranceFraction = 0.2f;

    // Full 12-slot skill ring (2026-08-06, user-directed — see DECISIONS.md -> [Combat]), one
    // clock-face position per hour. Originally the 5 built-in moves (A/C/H/R/K) were hardcoded
    // onto fixed hours (1/11/12/2/3) with only the remaining 7 as real equipped-skill slots; 2026-
    // 08 follow-up (user: "make them like any other skills... full customizability") made the 5
    // built-ins into real, equippable SkillData (Standard tree, see BuiltInMoveType) — so ALL 12
    // positions are now uniform equip slots, sized/positioned identically, no reserved subset.
    private const int SkillSlotCount = 12;
    private const float SkillSlotRadius = 95f; // px from the creature's center to each slot's center

    /// <summary>
    /// Hover tooltip text for the 5 built-in moves (A/C/H/R/K — now real SkillData, see
    /// BuiltInMoveType), keyed by move type rather than a fixed array index. Built from the same
    /// named constants (BattleConfig, DamageCalculator, CaptureSystem) that actually drive each
    /// move's resolution in BattleManager — same "use the real values" standard
    /// BuildSkillTooltipText applies to every other skill orb. Capture's chance range is computed
    /// via CaptureSystem.ComputeCaptureChancePercent at 0% and 100% target HP rather than
    /// hardcoded, so it can't drift out of sync with the real formula.
    /// </summary>
    public static string GetBuiltInMoveTooltipText(BuiltInMoveType move)
    {
        switch (move)
        {
            case BuiltInMoveType.Attack:
                return $"Attack\n{DamageCategory.Physical} damage — Power {DamageCalculator.BasicAttackPower}\nTarget: Enemy\nAura Cost: {BattleConfig.AttackAuraCost}";
            case BuiltInMoveType.Charge:
                return $"Charge\nRestores {BattleConfig.ChargeAuraRestore} Aura\nTarget: Self";
            case BuiltInMoveType.Heal:
                return $"Heal\nRestores {BattleConfig.HealAmount} HP instantly\nTarget: Self\nAura Cost: {BattleConfig.HealAuraCost}";
            case BuiltInMoveType.Regen:
                return $"Regen\nRestores {BattleConfig.RegenHealPerTurn} HP/turn for {BattleConfig.RegenDurationTurns} turns\nTarget: Self\nAura Cost: {BattleConfig.RegenAuraCost}";
            case BuiltInMoveType.Capture:
                float minCaptureChance = CaptureSystem.ComputeCaptureChancePercent(100, 100); // full target HP -> lowest chance
                float maxCaptureChance = CaptureSystem.ComputeCaptureChancePercent(0, 100);   // 0 target HP -> highest chance
                return $"Capture\nCapture chance: {Mathf.RoundToInt(minCaptureChance)}-{Mathf.RoundToInt(maxCaptureChance)}% (lower target HP = higher chance)\nTarget: Enemy";
            default:
                return string.Empty;
        }
    }

    /// <summary>Attack/Capture target the enemy; Charge/Heal/Regen are solo/self-only — matches the moves' pre-2026-08 hardcoded targeting exactly, now looked up by BuiltInMoveType instead of a fixed per-index array. Checked BEFORE ever calling PlaceholderSkillResolver.Resolve, which built-in-marked SkillData must never reach (see BuiltInMoveType's own doc comment).</summary>
    public static bool IsBuiltInMoveSelfTargeted(BuiltInMoveType move)
    {
        return move == BuiltInMoveType.Charge || move == BuiltInMoveType.Heal || move == BuiltInMoveType.Regen;
    }

    public static BattleHUDController Instance { get; private set; }

    private VisualElement _root;
    private VisualElement _stage;

    private VisualElement _actionAnnouncement;
    private Label _actionAnnouncementLabel;

    private VisualElement _continuePrompt;
    private Label _continuePromptLabel;

    private ScrollView _battleLogScrollView;
    private VisualElement _battleLogContent;

    /// <summary>Result of the most recently completed RunTimedInput call (Miss/Good/Perfect). Valid once that coroutine finishes. Source of truth for LastTimedInputSuccess/LastTimedInputWasPerfect below.</summary>
    public OffenseOutcome LastOffenseOutcome { get; private set; }

    /// <summary>True when LastOffenseOutcome is Good or Perfect (i.e. not a Miss). Kept as a computed convenience — EventBus.Raise_TimedInputSuccess, the burst-fill gain, and combo-streak tracking only ever cared about this binary signal, not which of the two success tiers it was.</summary>
    public bool LastTimedInputSuccess => LastOffenseOutcome != OffenseOutcome.Miss;

    /// <summary>True when LastOffenseOutcome is Perfect specifically. Drives BattleParticipant.RecordTimedInputPerfect / the TimedInputStreak combo rule.</summary>
    public bool LastTimedInputWasPerfect => LastOffenseOutcome == OffenseOutcome.Perfect;

    /// <summary>Result of the most recently completed RunDefenseTimedInput call. Valid once that coroutine finishes.</summary>
    public DefenseOutcome LastDefenseOutcome { get; private set; }

    /// <summary>True if the most recently completed RunDefenseTimedInput hit was a "perfect" (see PerfectToleranceFraction). Always false when LastDefenseOutcome is Miss. Not wired to any bonus yet — visual feedback only for now.</summary>
    public bool LastDefenseWasPerfect { get; private set; }

    private readonly VisualElement[] _playerStageCreatures = new VisualElement[BattleConfig.ActivePartySize];
    private VisualElement _playerStageArea;

    /// <summary>
    /// Cached LaneIndex per player stage slot, refreshed by GetPlayerSlotsGroupedByLane (called from
    /// both LayoutPlayerStageCreaturesByLane and ApplyLaneLayout) and by UpdatePlayerStageCreatureLane
    /// mid-battle. RestoreStageCreatureDepthOrder reads from this cache rather than needing a
    /// BattleParticipant list at every call site — HideMoveSelection, for one, has no participant
    /// reference at all.
    /// </summary>
    private readonly int[] _playerStageCreatureLaneIndex = new int[BattleConfig.ActivePartySize];

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

    /// <summary>Max simultaneous generic status-effect icons shown per nameplate (2026-08 follow-up — see NameplateRefs' own doc comment). A fixed, generous-for-real-play cap rather than a dynamic list — real gameplay rarely stacks more than a couple statuses on one target at once.</summary>
    private const int StatusIconPoolSize = 4;

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
        // fresh at hover time by the callback _tooltip.RegisterHover wires up).
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
        public string RegenTooltipText;
        public VisualElement BurstIcon;
        public Label BurstCounter;
        public string BurstTooltipText;

        // Generic status-effect (debuff/buff) icon pool (2026-08 follow-up — user report: "on
        // application for debuffs i dont see any debuffs on the enemy hud" — BattleParticipant.
        // ActiveStatuses was fully tracked via ApplyStatus/TickStatuses but never had ANY nameplate
        // visualization, unlike Regen/Burst which are separate, hardcoded, player-only mechanics.
        // Fixed pool size (StatusIconPoolSize) rather than dynamic — real gameplay rarely stacks
        // more than a couple statuses at once; RefreshStatusIcons hides any unused slots.
        public VisualElement[] StatusIcons;
        public Label[] StatusIconLabels;
        public Label[] StatusIconCounters;
        public string[] StatusIconTooltipText;
    }

    private readonly NameplateRefs[] _playerNameplates = new NameplateRefs[MaxNameplateSlots];
    private readonly NameplateRefs[] _enemyNameplates = new NameplateRefs[MaxNameplateSlots];

    /// <summary>[slotIndex][skillSlotIndex] — SkillSlotCount (12) placeholder circles per party member, one per clock hour. Every position is now a real, clickable equip slot (see PopulateSkillRing) — the old 5-fixed/7-equip split was removed once the built-in moves became real, equippable SkillData (2026-08 follow-up, see BuiltInMoveType).</summary>
    private readonly VisualElement[][] _playerSkillSlots = new VisualElement[BattleConfig.ActivePartySize][];

    /// <summary>[slotIndex][ringIndex 0..SkillSlotCount-1] — resolved equipped skill for each skill-ring slot, or null if that slot is empty/locked for this creature (2026-08 session, see DECISIONS.md -> [Combat]). Populated by PopulateSkillRing (called from Initialize); read by the click handlers registered in Awake.</summary>
    private readonly SkillData[][] _playerSkillSlotSkills = new SkillData[BattleConfig.ActivePartySize][];

    /// <summary>
    /// [slotIndex][ringIndex 0..SkillSlotCount-1] — small numeric badge on each skill-ring slot
    /// showing the current combo-streak count (2026-08 session — user-directed: "counter next to
    /// the skill on the skill wheel," see DECISIONS.md -> [Combat]). Created once per slot in
    /// Awake, hidden by default; BattleManager decides which slot to badge and with what count via
    /// SetSkillComboCounter/ClearAllSkillComboCounters — this class has no combo logic of its own,
    /// purely a dumb display.
    /// </summary>
    private readonly Label[][] _playerSkillSlotComboBadges = new Label[BattleConfig.ActivePartySize][];

    /// <summary>
    /// [slotIndex][ringIndex 0..SkillSlotCount-1] — lettering label on each skill-ring slot,
    /// showing the equipped skill's SHORT code via SkillLabelFormatter (2026-08-10 follow-up —
    /// full SkillName was here originally, but visibly overlapped for a full loadout; see
    /// PopulateSkillRing's own comment). Reuses `.move-option-label`'s exact styling (position/
    /// centering/font/color), one uniform visual language across every slot. Created once per slot
    /// in Awake like the combo badge; PopulateSkillRing just updates `.text` (empty for a locked slot).
    /// </summary>
    private readonly Label[][] _playerSkillSlotLabels = new Label[BattleConfig.ActivePartySize][];

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
    /// Fires from the always-visible-during-the-player's-turn Flee button (2026-08-10, user-
    /// directed — opposite side of End Turn, ~80% success rate: BattleConfig.FleeSuccessChance).
    /// Same "just a request flag, BattleManager.PlayerTurn's own loop resolves it" pattern as
    /// EndTurnClicked — see BattleManager's _fleeRequested field.
    /// </summary>
    public event Action FleeClicked;

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
    private Button _fleeButton;

    /// <summary>
    /// Per-slot "already acted this turn" flag (2026-08-06, user-directed — see DECISIONS.md ->
    /// [Combat]: "if the phasix already moved during its turn then it can still show, but will be
    /// greyed out for active skills"). Set by ShowMoveSelectionReadOnly, cleared by
    /// ShowMoveSelection — read by SetSkillRingVisible (applies `.move-option-disabled`) and
    /// BeginDragForSkill (refuses to start a drag for a read-only slot).
    /// </summary>
    private readonly bool[] _playerSlotReadOnly = new bool[BattleConfig.ActivePartySize];

    private VisualElement _enemyStageCreature;

    /// <summary>
    /// EnemyStageArea (BattleHUD.uxml) — the enemy-side analog of _playerStageArea, needed once
    /// ApplyEnemyLaneDepthScale started giving _enemyStageCreature a real lane-based `left` (2026-
    /// 08-11). Previously this container never needed explicit sizing (its one child had no `left`
    /// set, so it just centered via .stage-side-enemy's own flex/translate). Confirmed live via
    /// Play Mode screenshot: without resizing this to the full lane range, a non-Lane-1 `left` value
    /// pushed the enemy creature outside this box's small implicit bounds and off-screen — same fix
    /// LayoutPlayerStageCreaturesByLane already applies to _playerStageArea.
    /// </summary>
    private VisualElement _enemyStageArea;

    /// <summary>
    /// Placeholder per-hit projectile/flash VFX (2026-08-10 — Phase 3 close-out pass; combat had
    /// no visual feedback for attacks/skills landing before this). Constructed in Awake() once
    /// _stage/_playerStageCreatures/_enemyStageCreature all exist. Owned here rather than as its
    /// own scene singleton because it needs direct access to those UI Toolkit elements —
    /// BattleManager never touches UI Toolkit internals itself, only calls PlayHitVfx below.
    /// </summary>
    private CombatVfxController _vfxController;

    // Sonny 2-style click-and-drag move/target selection (2026-08-05/06, user-directed — see
    // DECISIONS.md -> [Combat]). ShowMoveSelection shows the acting player's skill ring; pressing
    // a populated slot starts a drag (DragLineVisual follows the cursor) that resolves against
    // whichever target is valid for that skill on release — enemy for Attack/Capture-type moves,
    // the caster's own creature for self-only ones (IsBuiltInMoveSelfTargeted /
    // PlaceholderSkillResolver.SelfTargeted, depending on whether the skill is a Standard built-in
    // move or a tree skill). Releasing anywhere invalid cancels back to the ring (OnDragPointerUp's
    // else branch) — the only cancel path that exists today, same for every skill. A single
    // onMoveConfirmed(ChosenMove) callback is what let move-selection unify onto one code path once
    // built-in moves became real SkillData (2026-08 follow-up) instead of a fixed 5-index array.
    private DragLineVisual _dragLine;
    private BattleParticipant _self; // the acting participant — the only valid target for self-only moves
    private List<BattleParticipant> _enemyTargets; // valid targets for Attack
    private Action<ChosenMove> _onMoveConfirmed;
    private int _draggingFromSlotIndex = -1;

    /// <summary>The skill currently being dragged (always non-null once a drag is in progress — every skill-ring slot, built-in move or tree skill, is real SkillData now).</summary>
    private SkillData _draggingSkill;

    /// <summary>
    /// Live player side, set once by Initialize. 2026-08-12: replaces the old, ring-open-scoped
    /// `_playerSideForFormationGrid` — the Move-drag flow (see below) needs occupancy data
    /// available at ANY time a Move icon might be pressed, independent of whether a skill ring
    /// happens to be open, so this is set once for the whole battle rather than per-ShowMoveSelection
    /// call. Always the same List reference BattleState.PlayerSide holds, so LaneIndex/PositionIndex
    /// reads are always live with no explicit refresh needed.
    /// </summary>
    private List<BattleParticipant> _playerSide;

    // --- In-battle Move (2026-08-12 redesign): a dedicated always-present per-creature icon, NOT a
    // skill-ring orb — dragging it reveals a set of stage-aligned position markers (hidden the rest
    // of the time) and drops onto one to reposition, reusing the same drag-line/pointer-capture
    // mechanics as skill orbs but as a fully independent flow (own fields below, never touching
    // _draggingFromSlotIndex/_draggingSkill) since Move no longer routes through ChosenMove/
    // _onMoveConfirmed at all — see BattleManager.HandleMoveConfirmed. ---
    private readonly VisualElement[] _playerMoveIcons = new VisualElement[BattleConfig.ActivePartySize];
    private SkillData _moveSkill;
    private int _moveDragSlotIndex = -1;
    private VisualElement _stagePositionMarkers;

    /// <summary>Fires (slotIndex, lane, position) when a Move-icon drag is released on a free stage position marker — BattleManager.HandleMoveConfirmed applies it directly via ResolveBuiltInMove, bypassing the ChosenMove/ResolveSkillAction pipeline entirely (Move isn't "a skill choice" anymore).</summary>
    public event Action<int, int, int> MoveConfirmed;

    // Shared converging-ring timing visual (2026-08-05, user-directed — see DECISIONS.md ->
    // [Combat]): reparented per use — above the targeted enemy for RunTimedInput (offense), above
    // the defending player creature for RunDefenseTimedInput. Never both at once (PlayerTurn and
    // EnemyTurn don't run concurrently), so one shared instance is enough.
    private RingVisual _timingRing;

    // Multi-Hit Volley (Attack_Pattern_Directive Part 5 Group 2, 2026-08-14, user: "the number of
    // rings shown should match the number of projectiles airborne. so multiple rings could be
    // closing if multiple projectiles are out") — UNLIKE _timingRing above, a Volley cast can have
    // several rings open/animating concurrently (dash cadence is faster than any one ring's own
    // sweep), so it needs its own small pool rather than reusing the single shared _timingRing.
    // Mirrors CombatVfxController's existing ObjectPool<CombatProjectileVisual> pattern exactly.
    private ObjectPool<RingVisual> _volleyRingPool;

    /// <summary>Distance (px) from a target creature's center to each of its 8 Multi-Hit Volley compass ring positions — same tier/placeholder status as SkillSlotRadius, pending NumericalCalibration.md. 140x140 ring boxes at this radius against a 72x72 creature will visually overlap between adjacent compass points — expected (semi-transparent strokes), not a bug, a later art/feel pass's concern.</summary>
    private const float VolleyRingRadius = 95f;

    // FIFO queue for a Multi-Hit Volley's concurrently-open rings (2026-08-14, user: "lets do fifo
    // but allow for different inputs based on the ring... its visual tracking, but then inputs
    // still need to match the order of the rings") — the one deliberate deviation from
    // RunTimedInput/RunDefenseTimedInput's own "register+unregister a local click handler per
    // call" shape: a Volley cast registers ONE handler for its whole duration (BeginVolleyInputSession/
    // EndVolleyInputSession), and every click always resolves against whichever ring is at index 0
    // (the oldest still-open one) — never whichever ring happened to open most recently.
    private readonly List<VolleySlot> _volleyQueue = new List<VolleySlot>();
    private EventCallback<PointerDownEvent> _volleyPointerHandler;

    /// <summary>
    /// Shared runtime hover tooltip (2026-08 follow-up fix — the original skill-orb implementation
    /// used UI Toolkit's native VisualElement.tooltip, which only renders inside Editor-hosted UI
    /// (Inspector/EditorWindow panels) and is silently a no-op for a runtime UIDocument panel like
    /// this one, in Play Mode or a real build alike. Replaced with a plain floating Label shown/
    /// hidden on PointerEnter/PointerLeave — same family of technique as _dragLine above. Added
    /// directly to _root (not _stage) and last among its siblings so it always paints on top,
    /// following UI Toolkit's document-order paint rule. Extracted into the standalone HudTooltip
    /// class (2026-08 follow-up, see DECISIONS.md -> [UI]) once the overworld Party menu needed
    /// the identical behavior for its own skill-ring orbs — see that class's own doc comment for
    /// the full history (cursor-following vs. anchor-relative positioning, etc.).
    /// </summary>
    private HudTooltip _tooltip;

    private void Awake()
    {
        Instance = this;

        var document = GetComponent<UIDocument>();
        _root = document.rootVisualElement.Q<VisualElement>("BattleHUDRoot");
        // Constructed immediately after _root — BuildNameplate (called later this method, for
        // the Bars nameplate style) calls _tooltip.RegisterHover directly, not just from inside a
        // deferred lambda, so _tooltip must already exist by the time that loop runs, unlike the
        // Enter/Leave lambda closures elsewhere in this method which only touch _tooltip when an
        // actual hover event fires, long after Awake has finished.
        _tooltip = new HudTooltip(_root);

        // [EDITOR-001]'s fix (2026-08-08, see KNOWN_ISSUES.md) reordered StatusHeader to paint/
        // pick ABOVE .stage so nameplate/burst-gauge clicks would win over .stage's own full-area
        // invisible backdrop. Side effect found live 2026-08-11 (user report): StatusHeader's own
        // bounds are broad enough (nearly the whole top of the screen) that for any stage-creature
        // whose upward stagger (now ApplyLaneLayout's in-lane spacing) pushes a skill-ring orb up into that
        // overlap — confirmed live via IPanel.Pick(): the middle party slot's Attack orb, whose
        // top ~60% falls inside StatusHeader's y-range — StatusHeader itself now wins the pick
        // and silently swallows the click, even though the orb is what's visually on top there.
        // StatusHeader is a pure layout container (position: absolute, pulled out of flex flow so
        // party-size changes don't shift .stage's anchor — see BattleHUD.uss's own comment); it
        // has no click behavior of its own anywhere in this codebase. Setting it to Ignore makes
        // it click-transparent WITHOUT affecting its children's own independent picking mode
        // (confirmed live: every nameplate bar/burst-track descendant still resolves correctly to
        // itself) — the orb underneath is now correctly picked instead.
        _root.Q<VisualElement>("StatusHeader").pickingMode = PickingMode.Ignore;

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

            // Full 12-slot skill ring — every position is a real equip slot now (2026-08 follow-up:
            // the old 5 hardcoded built-in-move positions became real, equippable SkillData, see
            // BuiltInMoveType). Reads _playerSkillSlotSkills at CLICK/HOVER time (not registration
            // time) so PopulateSkillRing can keep re-resolving skills freely across Initialize
            // calls without needing to re-register handlers; a null entry (empty/locked slot) is a
            // silent no-op, matching "no click handler, no functionality" for an unequipped slot.
            _playerSkillSlots[i] = new VisualElement[SkillSlotCount];
            _playerSkillSlotSkills[i] = new SkillData[SkillSlotCount];
            _playerSkillSlotComboBadges[i] = new Label[SkillSlotCount];
            _playerSkillSlotLabels[i] = new Label[SkillSlotCount];
            for (int k = 0; k < SkillSlotCount; k++)
            {
                VisualElement slot = _root.Q<VisualElement>($"PlayerStageSlot{i}_SkillSlot{k}");
                _playerSkillSlots[i][k] = slot;
                slot.style.display = DisplayStyle.None;

                var comboBadge = new Label();
                comboBadge.AddToClassList("skill-combo-badge");
                comboBadge.style.display = DisplayStyle.None;
                comboBadge.pickingMode = PickingMode.Ignore; // decoration only — must never intercept the slot's own drag click
                slot.Add(comboBadge);
                _playerSkillSlotComboBadges[i][k] = comboBadge;

                // Skill-name lettering (2026-08 follow-up — see _playerSkillSlotLabels' doc
                // comment). Text set by PopulateSkillRing; empty for a locked/unequipped slot.
                var skillLabel = new Label();
                skillLabel.AddToClassList("move-option-label");
                skillLabel.pickingMode = PickingMode.Ignore; // decoration only, same reasoning as comboBadge above
                slot.Add(skillLabel);
                _playerSkillSlotLabels[i][k] = skillLabel;

                int capturedSlotIndex = i; // avoid closing over the loop variable
                int capturedRingIndex = k;
                // StopPropagation so pressing a populated orb doesn't ALSO bubble up and fire
                // PlayerCreatureClicked on the parent .stage-creature (2026-08-06, user-directed
                // free-choice selection) — a specific orb press means "use this move," not "just
                // select this creature."
                slot.RegisterCallback<PointerDownEvent>(evt =>
                {
                    SkillData skill = _playerSkillSlotSkills[capturedSlotIndex][capturedRingIndex];
                    if (skill == null) return;
                    evt.StopPropagation();
                    BeginDragForSkill(capturedSlotIndex, evt, skill);
                });

                // Runtime hover tooltip (see HudTooltip's own doc comment for why this
                // replaced the old VisualElement.tooltip assignment). Reads _playerSkillSlotSkills at
                // hover time, same "read at event time, not registration time" pattern as the
                // click handler above, so PopulateSkillRing can keep re-resolving skills freely.
                // Content built by BuildSkillTooltipText from the skill's own RESOLVED behavior
                // (2026-08 follow-up — user-directed: "use the values from each skill orb to
                // generate your content"), not the shared placeholder Description text.
                slot.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    SkillData skill = _playerSkillSlotSkills[capturedSlotIndex][capturedRingIndex];
                    if (skill == null) return;
                    _tooltip.Show(BuildSkillTooltipText(skill), slot);
                });
                slot.RegisterCallback<PointerLeaveEvent>(evt => _tooltip.Hide());
            }
            PositionSkillSlots(_playerSkillSlots[i]);

            // Persistent Move icon (2026-08-12 redesign — user: "it should have its own icon that
            // exists for every player instead of it being a skill") — a small always-present badge,
            // separate from the 12-slot skill ring, parented directly under the stage-creature so it
            // automatically follows every ApplyLaneLayout position/scale change with zero extra
            // bookkeeping (no PositionSkillSlots-style per-frame math needed). Visibility is owned
            // by SetMoveIconVisible (tied to HasActedThisTurn/turn state, see BattleManager),
            // entirely independent of SetSkillRingVisible/ShowMoveSelection/HideMoveSelection's
            // ring-only lifecycle — the icon is available whether or not the ring is open.
            var moveIcon = new VisualElement();
            moveIcon.AddToClassList("move-icon");
            moveIcon.style.display = DisplayStyle.None; // hidden until SetMoveIconVisible(true) at turn start
            _playerStageCreatures[i].Add(moveIcon);
            _playerMoveIcons[i] = moveIcon;

            int capturedMoveSlotIndex = i;
            moveIcon.RegisterCallback<PointerDownEvent>(evt =>
            {
                // A Move-icon press must not also open the skill ring via PlayerCreatureClicked —
                // same StopPropagation reasoning as every skill-ring slot above.
                evt.StopPropagation();
                BeginMoveDrag(capturedMoveSlotIndex, evt);
            });
        }

        // Depth-scale/in-lane-spacing (formerly a fixed 3-slot stagger, 2026-08-06 — see
        // DECISIONS.md -> [Combat]) now depends on each BattleParticipant's real LaneIndex
        // (Combat_Directive Part 2/3, LaneMovementSystem), which isn't known yet at this Awake-time
        // point — moved to ApplyLaneLayout, called from Initialize once playerSide exists. Skill-ring
        // slot positions (PositionSkillSlots, just above) are computed relative to each creature's
        // own untransformed 72x72 box either way, so this ordering change doesn't affect them.

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
        _enemyStageArea = _root.Q<VisualElement>("EnemyStageArea");
        _vfxController = new CombatVfxController(this, _stage, _playerStageCreatures, _enemyStageCreature);

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

        // Opposite side of End Turn (2026-08-10, user-directed) — same visibility lifecycle,
        // driven by BattleManager via SetFleeButtonVisible alongside SetEndTurnButtonVisible.
        _fleeButton = _root.Q<Button>("FleeButton");
        _fleeButton.clicked += () => FleeClicked?.Invoke();
        _fleeButton.style.display = DisplayStyle.None;

        _battleLogScrollView = _root.Q<ScrollView>("BattleLogScrollView");
        _battleLogContent = _root.Q<VisualElement>("BattleLogContent");

        _dragLine = new DragLineVisual { style = { display = DisplayStyle.None } };
        _stage.Add(_dragLine);

        _timingRing = new RingVisual();
        _timingRing.AddToClassList("timing-ring");
        _timingRing.style.display = DisplayStyle.None;

        _volleyRingPool = new ObjectPool<RingVisual>(
            createFunc: () =>
            {
                var ring = new RingVisual();
                ring.AddToClassList("timing-ring-volley");
                return ring;
            },
            actionOnGet: ring => ring.style.display = DisplayStyle.Flex,
            actionOnRelease: ring =>
            {
                ring.style.display = DisplayStyle.None;
                ring.RemoveFromHierarchy();
            },
            actionOnDestroy: ring => ring.RemoveFromHierarchy(),
            collectionCheck: false,
            defaultCapacity: 4,
            maxSize: 8); // covers this pass's max hit count (basic-count pattern = 8) with zero slack

        _actionAnnouncement.style.display = DisplayStyle.None;
        _continuePrompt.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Clears Instance so BattleAudioVfxHooks' BattleHUDController.Instance?.PlayXVfx() calls
    /// correctly no-op after BattleScene_Main unloads (2026-08-10 fix — live-verified via EditMode
    /// tests: without this, Instance is a Unity "fake null" destroyed-but-not-cleared reference,
    /// and C#'s ?. operator does NOT catch that — it bypasses UnityEngine.Object's overloaded ==,
    /// so a stale Instance throws MissingReferenceException instead of silently no-oping the very
    /// first time a bond milestone fires outside of battle after any battle has ever run). Guarded
    /// on `== this` so a stale OnDestroy racing a freshly-constructed instance can't clear the new
    /// one out from under it.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
    /// callbacks against this controller's shared _tooltip.
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

            _tooltip.RegisterHover(hpTrack, () => np.HPTooltipText);
            _tooltip.RegisterHover(auraTrack, () => np.AuraTooltipText);
            _tooltip.RegisterHover(evoTrack, () => np.EvoTooltipText);

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

        // 2026-08-09 follow-up — user: "i tried using the regen ability... the buff icon shows
        // but no hover over description. Need one for that... anything in that bar that would
        // indicate a multi turn output should have the hoverover." Regen/Burst never had tooltip
        // wiring at all (unlike the generic StatusIcons pool below) — text is set alongside the
        // icon's own display/counter in SetRegenStatus/SetBurstStatus so it's always current.
        _tooltip.RegisterHover(regenIcon, () => np.RegenTooltipText);
        _tooltip.RegisterHover(burstIcon, () => np.BurstTooltipText);

        // Generic status-effect icon pool (2026-08 follow-up — see NameplateRefs' own doc
        // comment). Letter/color/tooltip are all set dynamically by RefreshStatusIcons since,
        // unlike Regen/Burst, any of the 28 StatusEffectType values could occupy a given slot.
        np.StatusIcons = new VisualElement[StatusIconPoolSize];
        np.StatusIconLabels = new Label[StatusIconPoolSize];
        np.StatusIconCounters = new Label[StatusIconPoolSize];
        np.StatusIconTooltipText = new string[StatusIconPoolSize];
        for (int i = 0; i < StatusIconPoolSize; i++)
        {
            (VisualElement icon, Label letterLabel, Label counter) = BuildStatusIconSlot(buffRow);
            np.StatusIcons[i] = icon;
            np.StatusIconLabels[i] = letterLabel;
            np.StatusIconCounters[i] = counter;

            int capturedIndex = i;
            _tooltip.RegisterHover(icon, () => np.StatusIconTooltipText[capturedIndex]);
        }

        return np;
    }

    /// <summary>Builds one empty (letter/color/counter all unset) status-icon slot, hidden by default — RefreshStatusIcons fills it in per-refresh. Mirrors BuildBuffIcon's structure but without a fixed letter/color, since the occupant varies.</summary>
    private static (VisualElement icon, Label letterLabel, Label counter) BuildStatusIconSlot(VisualElement parent)
    {
        var icon = new VisualElement();
        icon.AddToClassList("nameplate-buff-icon");

        var letterLabel = new Label();
        letterLabel.AddToClassList("nameplate-buff-icon-label");
        // Decorative — same reasoning as BuildNameplateBarRow's fill element (2026-08-09 follow-up
        // — user: "hovering over the buffs or debuffs on both player and enemy are both not
        // showing up"). .nameplate-buff-icon-label is absolutely positioned covering the ENTIRE
        // icon, so without this it — not `icon` — was the actual pick target, and HudTooltip's
        // RegisterHover(icon, ...) never fired. `icon` (its parent) is the intended, unambiguous
        // hover target.
        letterLabel.pickingMode = PickingMode.Ignore;
        icon.Add(letterLabel);

        var counter = new Label();
        counter.AddToClassList("nameplate-buff-icon-counter");
        counter.pickingMode = PickingMode.Ignore; // same reasoning as letterLabel above
        icon.Add(counter);

        parent.Add(icon);
        return (icon, letterLabel, counter);
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
        letterLabel.pickingMode = PickingMode.Ignore; // preventive — matches the identical fix on BuildStatusIconSlot's labels, in case a hover tooltip is ever registered on RegenIcon/BurstIcon
        icon.Add(letterLabel);

        var counter = new Label();
        counter.AddToClassList("nameplate-buff-icon-counter");
        counter.pickingMode = PickingMode.Ignore;
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
        // 2026-08-12: single source of truth for the Move-drag flow's occupancy checks
        // (ShowStagePositionMarkers/OnMoveDragPointerUp) — needs live player-side data available
        // whenever a Move icon might be pressed, not just while a skill ring happens to be open.
        _playerSide = playerSide;

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
        LayoutPlayerStageCreaturesByLane(playerSide);
        ApplyLaneLayout(playerSide);

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
        if (hasEnemy)
        {
            SetStageCreatureColor(_enemyStageCreature, enemySide[0]);
            ApplyEnemyLaneDepthScale(enemySide[0]);
        }

        RefreshBars(playerSide, enemySide);
    }

    /// <summary>
    /// Computes how long a hit's projectile will take to travel (CombatVfxController.
    /// ComputeTravelDuration), converts that into the matching ring sweepDuration
    /// (ComputeSweepDurationForTravelTime), and launches the projectile using that SAME travel
    /// duration — guarantees the ring and the projectile agree on timing rather than each
    /// computing it separately (2026-08-11 timing-sync pass). Returns the sweepDuration to pass
    /// into RunTimedInput/RunDefenseTimedInput in place of the old flat
    /// TimedInputConfig.MarkerSweepDuration constant.
    ///
    /// holdForOutcome=false (offense — always connects) resolves the projectile immediately on
    /// arrival. holdForOutcome=true (defense — outcome not known until the ring itself resolves)
    /// pauses it instead; the caller must follow up with exactly one of ResolveHitProjectile/
    /// ResolveDodgedProjectile/ResolveParryDeflect once LastDefenseOutcome is known. Falls back to
    /// TimedInputConfig.MarkerSweepDuration if the VFX controller isn't ready (shouldn't happen
    /// post-Awake — defensive only).
    /// </summary>
    public float LaunchSyncedProjectile(int attackerSlotIndex, bool attackerIsPlayerSide, int targetSlotIndex, bool targetIsPlayerSide, PrimalType colorType, bool holdForOutcome)
    {
        if (_vfxController == null) return TimedInputConfig.MarkerSweepDuration;

        float travelDuration = _vfxController.ComputeTravelDuration(attackerSlotIndex, attackerIsPlayerSide, targetSlotIndex, targetIsPlayerSide);
        _vfxController.LaunchProjectile(attackerSlotIndex, attackerIsPlayerSide, targetSlotIndex, targetIsPlayerSide, colorType, travelDuration, holdForOutcome);
        return ComputeSweepDurationForTravelTime(travelDuration);
    }

    /// <summary>
    /// Launches a projectile using an EXPLICIT travel duration instead of deriving one from real
    /// screen distance (LaunchSyncedProjectile's own ComputeTravelDuration) — for ranged Beat
    /// Sequence skills (Attack_Pattern_Directive Group 1: Instant Strike, Feint, Metronome, Jitter,
    /// 2026-08-12 follow-up, user: "I think i would like to see some projectile or animation that
    /// would be a better indicator... of what the action does"). Those skills' pre-emptive
    /// timed-input window is sized off the Windup beat's own authored duration (BeatSequenceRunner.
    /// ComputeWindupDuration), not projectile-speed physics — inverted from the classic pattern
    /// (there, travel time drives ring duration; here, Windup duration drives travel time, since
    /// ring/tell timing is this archetype's actual design-authored value). holdForOutcome behaves
    /// identically to LaunchSyncedProjectile: false auto-resolves (hit-flash) on arrival — used for
    /// player offense, which always connects; true holds the projectile for
    /// RunTimedInput/RunDefenseTimedInput's OWN existing Miss/Dodge/Parry dispatch to resolve once
    /// the ring itself closes — no separate resolution call needed here, since that dispatch already
    /// exists and already handles "hold" projectiles generically.
    /// </summary>
    public void LaunchRangedBeatSequenceProjectile(int attackerSlotIndex, bool attackerIsPlayerSide, int targetSlotIndex, bool targetIsPlayerSide, PrimalType colorType, float travelDuration, bool holdForOutcome)
    {
        _vfxController?.LaunchProjectile(attackerSlotIndex, attackerIsPlayerSide, targetSlotIndex, targetIsPlayerSide, colorType, travelDuration, holdForOutcome);
    }

    /// <summary>Resolves a held projectile (see LaunchSyncedProjectile) as a landed hit.</summary>
    public void ResolveHitProjectile() => _vfxController?.ResolveHeldProjectileAsHit();

    /// <summary>
    /// Resolves a held projectile (see LaunchSyncedProjectile) as a successful Dodge — the
    /// projectile continues THROUGH and past the defender (CombatVfxController.
    /// ResolveHeldProjectileAsPassThrough) exactly as the defender's own Phasix dissolves out of
    /// the way (DissolveVfxBridge's Shader Graph-equivalent material), both timed off
    /// DissolveVfxBridge.DissolveOutDuration so the two effects are genuinely synced, not just
    /// independently-timed effects that happen to overlap (2026-08-11, user-directed — defense
    /// always targets the player, so defenderSlotIndex is always a _playerStageCreatures index).
    /// </summary>
    public void ResolveDodgedProjectile(int defenderSlotIndex)
    {
        float dissolveOutDuration = DissolveVfxBridge.Instance != null ? DissolveVfxBridge.Instance.DissolveOutDuration : 0.2f;
        _vfxController?.ResolveHeldProjectileAsPassThrough(dissolveOutDuration);

        if (defenderSlotIndex >= 0 && defenderSlotIndex < _playerStageCreatures.Length)
            DissolveVfxBridge.Instance?.PlayDefenderDissolve(_playerStageCreatures[defenderSlotIndex]);
    }

    /// <summary>Resolves a held projectile (see LaunchSyncedProjectile) as a successful Parry — reverses it back toward the original attacker, re-tinted as counterColorType, doubling as the counter-attack's own hit VFX. Returns the projectile's real travel duration (0f if nothing was launched) so the caller can await it before applying the counter's damage, keeping the projectile's arrival flash and the damage/HP-bar update in the same beat.</summary>
    public float ResolveParryDeflect(PrimalType counterColorType) => _vfxController?.ResolveHeldProjectileAsParryDeflect(counterColorType) ?? 0f;

    /// <summary>Flashes the purple "you parried!" outline on the held projectile's defender WITHOUT resolving the projectile itself — called by RunDefenseTimedInput the instant Parry is detected, well before ResolveParryDeflect (which needs the counter-attacker's own type, not known at that point).</summary>
    public void FlashParryOutline() => _vfxController?.FlashHeldProjectileParryOutline();

    /// <summary>Passthrough to CombatVfxController's whole-Stage outcome pulse — called by BattleAudioVfxHooks on BattleWon/BattleLost (not Fled — see CombatVfxController.PlayOutcomeFlash's own doc comment).</summary>
    public void PlayBattleOutcomeVfx(bool won) => _vfxController?.PlayOutcomeFlash(won);

    /// <summary>Passthrough to CombatVfxController's whole-Stage bond-milestone pulse.</summary>
    public void PlayBondMilestoneVfx() => _vfxController?.PlayNameplateGlow();

    /// <summary>Passthrough to CombatVfxController's whole-Stage capture pulse.</summary>
    public void PlayCaptureVfx() => _vfxController?.PlayCaptureFlash();

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

        RefreshStatusIcons(np, p);

        np.Container.style.opacity = p.IsAlive ? 1f : 0.4f;
    }

    /// <summary>Position-matched to StatusEffectCategory's enum order (Physical/Elemental/Signal/Universal/Positive) — see BattleHUD.uss for the actual colors.</summary>
    private static readonly string[] StatusCategoryColorClasses =
    {
        "nameplate-status-physical", "nameplate-status-elemental", "nameplate-status-signal",
        "nameplate-status-universal", "nameplate-status-positive",
    };

    /// <summary>
    /// Fills up to StatusIconPoolSize generic status-effect icons from p.ActiveStatuses (2026-08
    /// follow-up — user report: "on application for debuffs i dont see any debuffs on the enemy
    /// hud"; see NameplateRefs' own doc comment for why this was missing entirely before). Any
    /// statuses beyond the pool size simply don't get an icon — a fixed, generous-for-real-play
    /// cap, not a hard gameplay limit (BattleParticipant itself has no cap on ActiveStatuses).
    /// Runs every RefreshNameplateStats call (both sides, both Initialize and RefreshBars), so an
    /// expired status (TickAllStatuses, once per round) clears its icon on the next refresh —
    /// which happens well before the player would notice a stale one, given how often bars refresh.
    /// </summary>
    private static void RefreshStatusIcons(NameplateRefs np, BattleParticipant p)
    {
        IReadOnlyList<ActiveStatusInstance> statuses = p.ActiveStatuses;

        for (int i = 0; i < StatusIconPoolSize; i++)
        {
            if (i >= statuses.Count)
            {
                np.StatusIcons[i].style.display = DisplayStyle.None;
                continue;
            }

            ActiveStatusInstance instance = statuses[i];
            StatusEffectCatalog.Entry entry = StatusEffectCatalog.Get(instance.Type);

            np.StatusIcons[i].style.display = DisplayStyle.Flex;
            np.StatusIconLabels[i].text = instance.Type.ToString().Substring(0, 1);
            np.StatusIconCounters[i].text = instance.TurnsRemaining.ToString();
            // 2026-08-09 follow-up — user report of no debuff hover text turned out to be the same
            // HudTooltip off-screen bug (fixed there), not missing wiring — this was already built.
            // Added the Buff/Debuff label here since StatusEffectCatalog.Entry.IsPositive was
            // already available and unused in this string — no new/invented status content.
            string polarity = entry.IsPositive ? "Buff" : "Debuff";
            np.StatusIconTooltipText[i] = $"{instance.Type} ({polarity} · {entry.Category})\n{instance.TurnsRemaining} turn{(instance.TurnsRemaining == 1 ? "" : "s")} remaining";

            foreach (string c in StatusCategoryColorClasses) np.StatusIcons[i].RemoveFromClassList(c);
            np.StatusIcons[i].AddToClassList(StatusCategoryColorClasses[(int)entry.Category]);
        }
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

    /// <summary>Matches `.stage-creature`'s fixed `width: 72px; height: 72px;` (BattleHUD.uss) — used by LayoutPlayerStageCreaturesByLane/ApplyLaneLayout/ApplyEnemyLaneDepthScale to size the stage areas around the 7-row range without hardcoding the 72 magic number in three places.</summary>
    private const float StageCreatureSizePx = 72f;

    /// <summary>
    /// Groups visible player stage slots by BattleParticipant.LaneIndex, and refreshes
    /// _playerStageCreatureLaneIndex for each — used by ApplyLaneLayout (row depth: top + scale) and
    /// RestoreStageCreatureDepthOrder (paint order), both of which only care about ROW, never
    /// column/PositionIndex. 2026-08-12: occupancy within a row is now EXCLUSIVE per (lane,
    /// position) pair (see LaneMovementSystem's class doc comment), but a single lane/row can still
    /// legitimately hold more than one entry (up to 5, one per column) — this grouping is still
    /// correct for that, it just no longer feeds horizontal placement (LayoutPlayerStageCreaturesByLane
    /// reads PositionIndex directly instead — see that method's own doc comment).
    /// </summary>
    private Dictionary<int, List<int>> GetPlayerSlotsGroupedByLane(List<BattleParticipant> playerSide)
    {
        var slotsByLane = new Dictionary<int, List<int>>();
        for (int i = 0; i < playerSide.Count && i < _playerStageCreatures.Length; i++)
        {
            int lane = playerSide[i].LaneIndex;
            if (!slotsByLane.TryGetValue(lane, out List<int> slots))
            {
                slots = new List<int>();
                slotsByLane[lane] = slots;
            }
            slots.Add(i);
            _playerStageCreatureLaneIndex[i] = lane;
        }
        return slotsByLane;
    }

    /// <summary>
    /// Places each player stage creature's HORIZONTAL position directly from
    /// BattleParticipant.PositionIndex (LaneMovementSystem.GetPositionOffsetPx) — 2026-08-12 rework:
    /// occupancy is now EXCLUSIVE (at most one combatant per (lane, position) pair, see
    /// LaneMovementSystem's class doc comment for the full "5-position formation grid" correction),
    /// so a column's screen offset no longer depends on how many others happen to share a row —
    /// unlike the removed occupant-count-based spread, this needs no grouping step at all, just a
    /// direct per-participant lookup. Sizes PlayerStageArea's WIDTH to exactly contain the 5-column
    /// range (LaneMovementSystem.PositionRangeWidthPx) plus one creature's own width, with a fixed
    /// centering-compensation term (half the range) so the padding doesn't shift the visible group
    /// off-anchor — same fix as DECISIONS.md -> [Combat] "a little too far left", now derived
    /// algebraically the same way GetLaneScreenTop's vertical centering term already is. HEIGHT is
    /// sized separately by ApplyLaneLayout below, once row/depth positions are known. Called once
    /// from Initialize, after party size is known; each creature's own internal box (skill slots —
    /// PositionSkillSlots) is a completely separate coordinate space and unaffected by this.
    ///
    /// Also applies LaneMovementSystem.PlayerNameplateClearanceShiftPx (2026-08-12, user: "the 2
    /// columns on the right are interferring with the health hud [player nameplates]... move the
    /// grid over by 2 columns") — a pure rightward positional shift, NOT a change to the grid's own
    /// width/spacing (explicitly not shrunk, per the user). Applied here AND in
    /// ShowStagePositionMarkers identically — they must move together, since a Move-drag marker's
    /// position is a promise of exactly where the creature will land if dropped there.
    /// </summary>
    private void LayoutPlayerStageCreaturesByLane(List<BattleParticipant> playerSide)
    {
        float centeringCompensationPx = LaneMovementSystem.PositionRangeWidthPx / 2f + LaneMovementSystem.PlayerNameplateClearanceShiftPx;

        for (int i = 0; i < playerSide.Count && i < _playerStageCreatures.Length; i++)
        {
            float spacingX = LaneMovementSystem.GetPositionOffsetPx(playerSide[i].PositionIndex);
            _playerStageCreatures[i].style.left = spacingX + centeringCompensationPx;
        }

        _playerStageArea.style.width = LaneMovementSystem.PositionRangeWidthPx + StageCreatureSizePx;
    }

    /// <summary>
    /// Applies real per-row VERTICAL position (LaneMovementSystem.GetLaneScreenTop) and depth scale
    /// (LaneMovementSystem.GetDepthScale) — 2026-08-12 correction: lanes are vertical rows, not
    /// horizontal positions (see LaneMovementSystem's class doc comment); this method used to set
    /// `style.scale` only, with horizontal lane position living in LayoutPlayerStageCreaturesByLane —
    /// now it owns the row/depth axis (`top` + `scale`) while that method owns pure in-row horizontal
    /// spacing (`left`), matching the corrected model. Sizes PlayerStageArea's HEIGHT to exactly
    /// contain the 7-row range (LaneMovementSystem.RowRangeHeightPx) plus one creature's own height.
    /// Also reorders the siblings back-to-front by row depth (see RestoreStageCreatureDepthOrder) —
    /// 2026-08-06, user caught the front lane's skill orbs rendering behind a further-back lane's
    /// creature; still applies once "further back" means a real row instead of a fixed offset. Safe
    /// to reorder freely now that .stage-creature is absolutely positioned (BringToFront no longer
    /// moves a creature within a flex row — see BattleHUD.uss comment on .stage-creature for the bug
    /// this fixed).
    /// </summary>
    private void ApplyLaneLayout(List<BattleParticipant> playerSide)
    {
        Dictionary<int, List<int>> slotsByLane = GetPlayerSlotsGroupedByLane(playerSide);

        foreach (KeyValuePair<int, List<int>> entry in slotsByLane)
        {
            float top = LaneMovementSystem.GetLaneScreenTop(entry.Key, isPlayerSide: true);
            float scale = LaneMovementSystem.GetDepthScale(entry.Key);
            foreach (int slotIndex in entry.Value)
            {
                _playerStageCreatures[slotIndex].style.top = top;
                _playerStageCreatures[slotIndex].style.scale = new Scale(new Vector3(scale, scale, 1f));
            }
        }

        _playerStageArea.style.height = LaneMovementSystem.RowRangeHeightPx + StageCreatureSizePx;

        RestoreStageCreatureDepthOrder();
    }

    /// <summary>
    /// Single-slot equivalent of LayoutPlayerStageCreaturesByLane + ApplyLaneLayout for the lone
    /// enemy stage slot — no in-row spacing needed since multi-enemy battles don't have per-enemy
    /// stage creatures yet (see class doc comment). Called from Initialize once the enemy side's row
    /// is known. `left` stays at the compensation baseline (single occupant, no spread needed) rather
    /// than 0, matching the player side's own baseline so a lone enemy isn't visually offset from
    /// where a lone player creature would sit.
    /// </summary>
    private void ApplyEnemyLaneDepthScale(BattleParticipant enemyParticipant)
    {
        if (_enemyStageArea != null)
        {
            _enemyStageArea.style.width = 2 * LaneMovementSystem.InLaneSpacingPx + StageCreatureSizePx;
            _enemyStageArea.style.height = LaneMovementSystem.RowRangeHeightPx + StageCreatureSizePx;
        }

        int lane = enemyParticipant.LaneIndex;
        _enemyStageCreature.style.left = LaneMovementSystem.InLaneSpacingPx;
        _enemyStageCreature.style.top = LaneMovementSystem.GetLaneScreenTop(lane, isPlayerSide: false);
        float scale = LaneMovementSystem.GetDepthScale(lane);
        _enemyStageCreature.style.scale = new Scale(new Vector3(scale, scale, 1f));
    }

    /// <summary>
    /// Accessor for beat-sequence code (BeatSequenceRunner) to animate a specific combatant's
    /// element without reaching into this class's private arrays. Enemy side ignores slotIndex —
    /// single stage slot only, see class doc comment.
    /// </summary>
    public VisualElement GetStageCreatureElement(int slotIndex, bool isPlayerSide)
    {
        return isPlayerSide ? _playerStageCreatures[slotIndex] : _enemyStageCreature;
    }

    /// <summary>
    /// Public re-entry point into LayoutPlayerStageCreaturesByLane + ApplyLaneLayout for anything
    /// that changes a player BattleParticipant's LaneIndex outside of a Beat Sequence (currently
    /// only DebugLaneCycler, 2026-08-12 — user: "is there a way that i can test the lane
    /// movement") and needs the row's screen position/depth scale/in-row spacing recomputed
    /// immediately, the same way Initialize does at battle start.
    /// </summary>
    public void RefreshPlayerLaneLayout(List<BattleParticipant> playerSide)
    {
        LayoutPlayerStageCreaturesByLane(playerSide);
        ApplyLaneLayout(playerSide);
    }

    private LaneGuideOverlay _laneGuideOverlay;

    /// <summary>
    /// TEMPORARY debug (2026-08-12, user: "add a debug button for the '\' button to toggle show
    /// on and off the lane lines") — shows/hides a full-Stage-width dotted line at every lane/row
    /// boundary. Lazily creates and parents a LaneGuideOverlay directly under Stage (a sibling of
    /// PlayerStageArea/EnemyStageArea, not inside either, so one set of lines covers both sides).
    /// Converts LaneMovementSystem's box-local `top` values into Stage-local ones using
    /// PlayerStageArea's own resolvedStyle (anchor `top` + box height) — both sides share the same
    /// USS anchor and box-sizing formula, so this single conversion is valid for either side (see
    /// LaneMovementSystem.GetLaneScreenTop's "identical formula for both sides" doc comment).
    /// Called by DebugLaneCycler on a `\` keypress — DELETE alongside that file once real stage art
    /// exists.
    /// </summary>
    public void SetLaneGuideLinesVisible(bool visible)
    {
        if (_laneGuideOverlay == null)
        {
            _laneGuideOverlay = new LaneGuideOverlay();
            _stage.Add(_laneGuideOverlay);
        }

        // Always the backmost Stage child (2026-08-12, user: "make sure the lane lines are also
        // the furthest back i want everything else on the layer in front") — re-asserted on every
        // call, not just at creation, since UI Toolkit's painter's algorithm paints later document
        // siblings on top and nothing else currently reorders Stage's own direct children, but
        // this guarantees it regardless.
        _laneGuideOverlay.SendToBack();

        if (visible)
        {
            float boxHeight = LaneMovementSystem.RowRangeHeightPx + StageCreatureSizePx;
            float boxTopInStageSpace = _playerStageArea.resolvedStyle.top - boxHeight / 2f;
            float halfRow = LaneMovementSystem.LaneRowHeightPx / 2f;

            // LaneCount+1 boundaries for LaneCount rows: index 0 is Lane 1's near (largest-top,
            // closest-to-viewer) edge; index N is row N's far (smaller-top) edge, which is also
            // row N+1's near edge for every N < LaneCount — rows are evenly spaced by
            // LaneRowHeightPx so adjacent rows' shared edge is the same value computed either way.
            var boundaries = new float[BattleLaneLayout.LaneCount + 1];
            boundaries[0] = boxTopInStageSpace + LaneMovementSystem.GetLaneScreenTop(1, isPlayerSide: true) + halfRow;
            for (int lane = 1; lane <= BattleLaneLayout.LaneCount; lane++)
            {
                float rowCenter = LaneMovementSystem.GetLaneScreenTop(lane, isPlayerSide: true);
                boundaries[lane] = boxTopInStageSpace + rowCenter - halfRow;
            }

            _laneGuideOverlay.SetBoundaries(boundaries);
        }

        _laneGuideOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// Public passthrough to CombatVfxController.FlashStageElement, resolving slotIndex/isPlayerSide
    /// to the right element via GetStageCreatureElement — the melee Beat Sequence's Attack beat
    /// (BattleManager.ResolveMeleeAttackBeatOffense/Defense) has no projectile to carry a hit-flash,
    /// so it calls this directly instead. See CombatVfxController.FlashStageElement's doc comment
    /// for the doc/code discrepancy this also corrects.
    /// </summary>
    public void FlashStageCreatureHit(int slotIndex, bool isPlayerSide, PrimalType colorType)
    {
        _vfxController?.FlashStageElement(GetStageCreatureElement(slotIndex, isPlayerSide), colorType);
    }

    /// <summary>
    /// Updates the cached lane index for one player stage slot and re-sorts paint order — called by
    /// BeatSequenceRunner as a melee Approach/Return moves a combatant between lanes mid-battle, so
    /// depth order (RestoreStageCreatureDepthOrder) stays correct without needing a full
    /// Initialize/ApplyLaneLayout pass. No enemy-side equivalent — RestoreStageCreatureDepthOrder
    /// only ever reorders player-side slots (the enemy side is a single element, nothing to reorder
    /// against); an enemy's own Approach/Return still moves its element (via GetStageCreatureElement)
    /// and updates BattleParticipant.LaneIndex, it just never needs a paint-order re-sort.
    /// </summary>
    public void UpdatePlayerStageCreatureLane(int slotIndex, int laneIndex)
    {
        if (slotIndex < 0 || slotIndex >= _playerStageCreatureLaneIndex.Length) return;
        _playerStageCreatureLaneIndex[slotIndex] = laneIndex;
        RestoreStageCreatureDepthOrder();
    }

    /// <summary>
    /// UI Toolkit draws siblings in document order (painter's algorithm) — the fixed 0,1,2 slot
    /// order doesn't match visual depth once lane layout is applied, so a "further back" creature
    /// (larger LaneIndex) could draw over a "front" creature's orb ring. BringToFront in
    /// descending-LaneIndex order rebuilds document order to match depth: furthest-back (Lane 7)
    /// drawn first, frontmost (Lane 1) drawn last (on top), tiebroken by slot index for determinism
    /// among same-lane occupants. Called after lane layout is applied and again whenever a move
    /// wheel closes, to undo the temporary override ShowMoveSelection/ShowMoveSelectionReadOnly
    /// apply below, and by UpdatePlayerStageCreatureLane whenever a Beat Sequence moves a combatant.
    /// </summary>
    private void RestoreStageCreatureDepthOrder()
    {
        var depthOrder = new int[_playerStageCreatures.Length];
        for (int i = 0; i < depthOrder.Length; i++) depthOrder[i] = i;
        Array.Sort(depthOrder, (a, b) =>
        {
            int laneCompare = _playerStageCreatureLaneIndex[b].CompareTo(_playerStageCreatureLaneIndex[a]);
            return laneCompare != 0 ? laneCompare : a.CompareTo(b);
        });
        foreach (int index in depthOrder)
            _playerStageCreatures[index].BringToFront();
    }

    /// <summary>
    /// Places all SkillSlotCount (12) skill-ring slots evenly around the creature, one per clock
    /// hour (index 0 = 1 o'clock ... index 11 = 12 o'clock). Computed once per slot in Awake since
    /// slot count/parent size are both fixed for the whole battle.
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
            // self-centering offsets computed directly in px since both sizes are known constants.
            slots[i].style.left = 36f - 16f + dx;
            slots[i].style.top = 36f - 16f + dy;
        }
    }

    /// <summary>
    /// Angle math for a Multi-Hit Volley compass ring (2026-08-14) — direct sibling of
    /// PositionSkillSlots' clock-face formula above, just re-based so index 0 (CompassPoint.N)
    /// lands at exactly 90 degrees ("up") instead of that method's +1 clock-hour offset, and
    /// stepping in 45-degree increments (8 points) instead of 30 (12 points).
    /// </summary>
    private static (float dx, float dy) ComputeCompassOffset(CompassPoint point, float radius)
    {
        float angleDegrees = 90f - 45f * (int)point;
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        return (Mathf.Cos(angleRadians) * radius, -Mathf.Sin(angleRadians) * radius);
    }

    /// <summary>Positions one Multi-Hit Volley ring (140x140, see .timing-ring-volley) at its compass offset relative to whichever creature element it's about to be parented into — same self-centering-offset math PositionSkillSlots already uses for its own (differently-sized) children.</summary>
    private static void PositionVolleyRing(VisualElement ring, CompassPoint point)
    {
        (float dx, float dy) = ComputeCompassOffset(point, VolleyRingRadius);
        // .stage-creature is 72x72; .timing-ring-volley is 180x180 (widened 2026-08-15 alongside
        // VolleyMarkerStartRadius growing to 80 — a marker that size would clip against the ring's
        // OLD 140x140 box) — self-centering offset is half of 180.
        ring.style.left = 36f - 90f + dx;
        ring.style.top = 36f - 90f + dy;
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
    ///
    /// A built-in move (skill.BuiltInMove != None — Attack/Charge/Heal/Regen/Capture, 2026-08
    /// follow-up) is checked FIRST and short-circuits to GetBuiltInMoveTooltipText — these 5 must
    /// NEVER reach PlaceholderSkillResolver.Resolve below, which derives behavior from
    /// SkillTreeCatalog's tree-based PrimaryAttribute table and would misresolve (or throw) for
    /// the Standard tree these assets live in.
    /// </summary>
    public static string BuildSkillTooltipText(SkillData skill)
    {
        if (skill.BuiltInMove != BuiltInMoveType.None) return GetBuiltInMoveTooltipText(skill.BuiltInMove);

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


    /// <summary>
    /// Resolves a participant's equippedSkillGuids into the SkillSlotCount (12) skill-ring slots
    /// (2026-08 session — Combo/Status/Chain/Mastery wiring, see DECISIONS.md -> [Combat]). Slots
    /// beyond the creature's equipped count (or all of them, if skillDatabase is null) stay tagged
    /// .skill-slot-locked (dim, no cursor) — their registered click handler already no-ops on a
    /// null entry, so nothing further is needed to make them inert. Call once per Initialize.
    /// </summary>
    private void PopulateSkillRing(int slotIndex, BattleParticipant participant, SkillDatabase skillDatabase)
    {
        for (int ring = 0; ring < SkillSlotCount; ring++)
        {
            VisualElement slot = _playerSkillSlots[slotIndex][ring];
            SkillData skill = null;

            if (skillDatabase != null && ring < participant.RuntimeData.equippedSkillGuids.Count)
            {
                string guid = participant.RuntimeData.equippedSkillGuids[ring];
                skillDatabase.TryGetByGuid(guid, out skill);
            }

            // Defensive: Move (2026-08-12) is no longer equippable (WildSpawnSystem never seeds it
            // into equippedSkillGuids anymore), but a save file or live party from earlier this
            // session's playtesting could still have its guid sitting in that list — render as an
            // empty/locked slot rather than a non-functional "M" orb (BeginDragForSkill no longer
            // has a Move-specific branch to send it to, so leaving it populated would silently do
            // nothing on click).
            if (skill != null && skill.BuiltInMove == BuiltInMoveType.Move) skill = null;

            _playerSkillSlotSkills[slotIndex][ring] = skill;
            slot.EnableInClassList("skill-slot-locked", skill == null);
            // Hover tooltip content (name, description, flat placeholder Aura cost) is read live
            // off _playerSkillSlotSkills by the PointerEnterEvent handler registered in Awake —
            // nothing to set here beyond keeping that array current, which the line above already does.

            // Lettering (2026-08-10 follow-up — user: "no names of skills should be there... only
            // during the hover over... and the letter that the skill has like C1, C2, etc." A full
            // 12-skill loadout's real SkillName strings visibly overlapped/crowded the small
            // clock-face orbs; SkillLabelFormatter is the same short-code source the Party menu's
            // skill web/equip wheel already used, so both screens can't diverge again). Empty for
            // a locked slot; full name/description still lives in the hover tooltip only.
            _playerSkillSlotLabels[slotIndex][ring].text = skill != null ? SkillLabelFormatter.GetShortLabel(skill, skillDatabase) : string.Empty;

            // Fill/border color is the skill's own TREE color (2026-08-09 follow-up — user: "i want
            // the skill wheel in skill tree menu to sync up with the battle scene... matching
            // colors" — supersedes the original ring-POSITION-owned scheme this slot used, see
            // SkillTreeColor's own doc comment). SkillTreeColor is the one shared color source for
            // the Party menu's skill web, its equip wheel, and this battle ring — none has its own
            // independent palette anymore.
            SkillTreeColor.ApplyVisual(slot, skill?.TreeType);
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
    /// Shows all of the acting player's equipped skill orbs above their stage creature — every
    /// populated slot, whether it's a Standard built-in move (Attack/Charge/Heal/Regen/Capture) or
    /// a tree skill, is press-and-drag: enemy-target moves (Attack/Capture-type) drop onto the
    /// ENEMY, self-only moves (Charge/Heal/Regen-type) drop onto the CASTER's own creature (2026-08
    /// follow-up — built-ins became real, equippable SkillData, unifying what was a separate fixed-
    /// 5 system into this one). Whichever is released over a valid target fires
    /// onMoveConfirmed(ChosenMove) — target is the enemy for an enemy-target skill, always `self`
    /// for a self-only one. Releasing anywhere invalid cancels back to the ring, same for every
    /// skill. Clears any prior ShowMoveSelectionReadOnly state for this slot — a creature that
    /// already acted only ever reaches THIS method again once a new player turn resets
    /// HasActedThisTurn. Call HideMoveSelection to cancel/clean up early.
    /// </summary>
    public void ShowMoveSelection(int attackerSlotIndex, BattleParticipant self, List<BattleParticipant> enemyTargets,
        Action<ChosenMove> onMoveConfirmed)
    {
        _self = self;
        _enemyTargets = enemyTargets;
        _onMoveConfirmed = onMoveConfirmed;
        _playerSlotReadOnly[attackerSlotIndex] = false;
        SetSkillRingVisible(attackerSlotIndex, true);
        _playerStageCreatures[attackerSlotIndex].BringToFront();
    }

    /// <summary>
    /// Shows a player's skill ring in READ-ONLY mode — every populated orb visible but greyed
    /// (`.move-option-disabled`) and non-interactive (BeginDragForSkill refuses to start a drag
    /// for a read-only slot). Used when the player clicks a Phasix that's already used its action
    /// this turn (2026-08-06, user-directed — see DECISIONS.md -> [Combat]: "if the phasix already
    /// moved during its turn then it can still show, but will be greyed out for active skills").
    /// No target/callback state is set — there's nothing to confirm in this mode. Deliberately
    /// greys out every populated slot; see BattleParticipant.HasActedThisTurn's doc comment
    /// for why this is a per-slot flag rather than something baked into a specific move, leaving
    /// room for a future synergy/passive exception without restructuring this method.
    /// </summary>
    public void ShowMoveSelectionReadOnly(int slotIndex)
    {
        _playerSlotReadOnly[slotIndex] = true;
        SetSkillRingVisible(slotIndex, true);
        _playerStageCreatures[slotIndex].BringToFront();
    }

    /// <summary>Hides the skill ring and cancels an in-progress drag, if any. Safe to call even when nothing is shown.</summary>
    public void HideMoveSelection()
    {
        for (int i = 0; i < BattleConfig.ActivePartySize; i++) SetSkillRingVisible(i, false);
        EndDrag();
        _self = null;
        _enemyTargets = null;
        _onMoveConfirmed = null;
        // Belt-and-suspenders: a slot hidden (display: None) out from under a hovering pointer
        // doesn't reliably fire PointerLeaveEvent, so the tooltip could otherwise linger after
        // the wheel that owned it disappears.
        _tooltip.Hide();
        // Undo ShowMoveSelection/ShowMoveSelectionReadOnly's BringToFront override — back to
        // static lane-depth order now that no wheel is open.
        RestoreStageCreatureDepthOrder();
    }

    /// <summary>Shows/hides the always-on-during-the-player's-turn End Turn button (2026-08-06, user-directed — see DECISIONS.md -> [Combat]). BattleManager calls this at the start/end of PlayerTurn.</summary>
    public void SetEndTurnButtonVisible(bool visible)
    {
        _endTurnButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>Shows/hides the Flee button (2026-08-10, user-directed), same lifecycle as SetEndTurnButtonVisible — visible only during the player's own turn.</summary>
    public void SetFleeButtonVisible(bool visible)
    {
        _fleeButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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
        // BattleConfig.RegenHealPerTurn is the real, locked heal-per-tick value (see BattleManager's
        // ApplyRegen call site) — not invented text, same number the actual tick applies.
        _playerNameplates[slotIndex].RegenTooltipText = active
            ? $"Regen\nHeals {BattleConfig.RegenHealPerTurn} HP per turn\n{turnsRemaining} turn{(turnsRemaining == 1 ? "" : "s")} remaining"
            : string.Empty;
    }

    /// <summary>Shows/updates/hides a player's Evolution Burst status icon and countdown — same pattern as SetRegenStatus (2026-08-06, wiring EvolutionBurstSystem into the live loop). turnsRemaining &lt;= 0 hides the icon.</summary>
    public void SetBurstStatus(int slotIndex, int turnsRemaining)
    {
        bool active = turnsRemaining > 0;
        _playerNameplates[slotIndex].BurstIcon.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        _playerNameplates[slotIndex].BurstCounter.text = active ? turnsRemaining.ToString() : "";
        // No specific in-battle effect exists to describe yet — EvolutionBurstSystem's
        // ApplyBurstEffects is still status-only/undesigned (see DECISIONS.md's own "Open note"
        // entry) — so this deliberately doesn't claim a stat bonus that doesn't exist.
        _playerNameplates[slotIndex].BurstTooltipText = active
            ? $"Evolution Burst\nActive — {turnsRemaining} turn{(turnsRemaining == 1 ? "" : "s")} remaining"
            : string.Empty;
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

    private void SetSkillRingVisible(int slotIndex, bool visible)
    {
        // Only grey out orbs while actually showing a read-only wheel — irrelevant while hiding.
        bool readOnly = visible && _playerSlotReadOnly[slotIndex];

        // Empty/populated skill slots all show/hide together — the whole radial wheel appears
        // and disappears as one unit (2026-08-06, user-directed — see DECISIONS.md -> [Combat]).
        foreach (VisualElement slot in _playerSkillSlots[slotIndex])
            slot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        // Populated slots grey out in read-only mode too (2026-08 session, see DECISIONS.md ->
        // [Combat]) — locked/empty slots are already visually dim via .skill-slot-locked
        // regardless of read-only state, so only touch populated ones here.
        for (int ring = 0; ring < SkillSlotCount; ring++)
        {
            SkillData skill = _playerSkillSlotSkills[slotIndex] != null ? _playerSkillSlotSkills[slotIndex][ring] : null;
            if (skill != null) _playerSkillSlots[slotIndex][ring].EnableInClassList("move-option-disabled", readOnly);
        }
    }

    /// <summary>
    /// Shows/hides one creature's Move icon (2026-08-12 redesign) — entirely independent of
    /// SetSkillRingVisible/ShowMoveSelection/HideMoveSelection's ring-only lifecycle, since Move is
    /// no longer a ring orb. BattleManager calls this at turn start (show for every alive,
    /// not-yet-acted player creature) and the instant a creature's HasActedThisTurn becomes true
    /// (hide, whether that happened via a normal skill or via Move itself).
    /// </summary>
    public void SetMoveIconVisible(int slotIndex, bool visible)
    {
        if (_playerMoveIcons[slotIndex] == null) return;
        _playerMoveIcons[slotIndex].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BeginDragForSkill(int slotIndex, PointerDownEvent evt, SkillData skill)
    {
        if (_playerSlotReadOnly[slotIndex]) return;

        SetSkillRingVisible(slotIndex, false);
        _draggingFromSlotIndex = slotIndex;
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

    private void OnDragPointerMove(PointerMoveEvent evt)
    {
        _dragLine.End = _dragLine.WorldToLocal(evt.position);
        _dragLine.Refresh();
    }

    private void OnDragPointerUp(PointerUpEvent evt)
    {
        _root.ReleasePointer(evt.pointerId);
        int fromSlotIndex = _draggingFromSlotIndex;
        SkillData draggingSkill = _draggingSkill;
        EndDrag();

        // Self-vs-enemy targeting: a Standard built-in move (Attack/Charge/Heal/Regen/Capture)
        // resolves via IsBuiltInMoveSelfTargeted; every other skill via PlaceholderSkillResolver
        // (2026-08 follow-up — built-ins are real SkillData now, unifying what was a separate
        // fixed-index array into one skill-driven path — see DECISIONS.md -> [Combat]).
        bool selfOnly = draggingSkill.BuiltInMove != BuiltInMoveType.None
            ? IsBuiltInMoveSelfTargeted(draggingSkill.BuiltInMove)
            : PlaceholderSkillResolver.Resolve(draggingSkill).SelfTargeted;
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
            callback?.Invoke(new ChosenMove(draggingSkill, target));
        }
        else if (fromSlotIndex >= 0)
        {
            // Released on nothing valid — let the player retry (the only cancel path today, same
            // for every move).
            SetSkillRingVisible(fromSlotIndex, true);
        }
    }

    private void EndDrag()
    {
        _root.UnregisterCallback<PointerMoveEvent>(OnDragPointerMove);
        _root.UnregisterCallback<PointerUpEvent>(OnDragPointerUp);
        _dragLine.style.display = DisplayStyle.None;
        _draggingFromSlotIndex = -1;
        _draggingSkill = null;
    }

    // --- In-battle Move drag flow (2026-08-12 redesign) — independent of the skill-ring drag above
    // (own fields, own pointer-up handler); reuses the shared _dragLine and the generic
    // OnDragPointerMove (it only touches _dragLine, nothing skill-specific) but hit-tests against
    // stage-aligned position markers instead of enemy/self creature bounds. ---

    /// <summary>
    /// Starts a Move-icon drag — "just like how we do the projectile," per the user, reusing the
    /// exact same drag-line/pointer-capture mechanics BeginDragForSkill uses for orbs, just against
    /// a different target set. Markers appear here, at drag START, not before — the "hidden during
    /// normal combat, shown once you've selected to move" requirement. Re-checks alive/not-acted
    /// directly rather than trusting the icon's own visibility (same "don't trust the UI alone"
    /// posture as FormationSystem/ResolveBuiltInMove elsewhere in this codebase).
    /// </summary>
    private void BeginMoveDrag(int slotIndex, PointerDownEvent evt)
    {
        if (_playerSide == null || slotIndex < 0 || slotIndex >= _playerSide.Count) return;
        BattleParticipant caster = _playerSide[slotIndex];
        if (!caster.IsAlive || caster.HasActedThisTurn) return;

        _moveDragSlotIndex = slotIndex;
        ShowStagePositionMarkers(slotIndex);

        Vector2 startWorld = _playerStageCreatures[slotIndex].worldBound.center;
        _dragLine.Start = _dragLine.WorldToLocal(startWorld);
        _dragLine.End = _dragLine.WorldToLocal(evt.position);
        _dragLine.style.display = DisplayStyle.Flex;
        _dragLine.Refresh();

        _root.CapturePointer(evt.pointerId);
        _root.RegisterCallback<PointerMoveEvent>(OnDragPointerMove);
        _root.RegisterCallback<PointerUpEvent>(OnMoveDragPointerUp);
    }

    private void OnMoveDragPointerUp(PointerUpEvent evt)
    {
        _root.ReleasePointer(evt.pointerId);
        int casterSlotIndex = _moveDragSlotIndex;
        EndMoveDrag();

        (int lane, int position)? hitSlot = null;
        if (_stagePositionMarkers != null)
        {
            foreach (VisualElement marker in _stagePositionMarkers.Children())
            {
                if (marker.worldBound.Contains(evt.position))
                {
                    hitSlot = ((int, int))marker.userData;
                    break;
                }
            }
        }

        HideStagePositionMarkers();

        // No marker hit (released outside all of them) — the cancel path: no move applied, icon
        // stays enabled, nothing else to undo (the ring was never touched by this flow).
        if (hitSlot == null || _playerSide == null || casterSlotIndex < 0 || casterSlotIndex >= _playerSide.Count) return;

        (int lane, int position) = hitSlot.Value;
        BattleParticipant caster = _playerSide[casterSlotIndex];

        // Live re-validation, not just trusting the marker's disabled-at-build-time state — the
        // same "safety re-check" posture ResolveBuiltInMove's own Move case already has.
        var others = new System.Collections.Generic.List<(int, int)>();
        foreach (BattleParticipant p in _playerSide)
        {
            if (p == caster || !p.IsAlive) continue;
            others.Add((p.LaneIndex, p.PositionIndex));
        }
        if (FormationSystem.IsSlotOccupied(others, lane, position)) return; // cancel: occupied

        MoveConfirmed?.Invoke(casterSlotIndex, lane, position);
    }

    private void EndMoveDrag()
    {
        _root.UnregisterCallback<PointerMoveEvent>(OnDragPointerMove);
        _root.UnregisterCallback<PointerUpEvent>(OnMoveDragPointerUp);
        _dragLine.style.display = DisplayStyle.None;
        _moveDragSlotIndex = -1;
    }

    /// <summary>
    /// Builds the 7x5 set of Move-drag target markers, ALIGNED TO THE REAL STAGE — each marker's
    /// `top`/`left` is computed via the exact same LaneMovementSystem.GetLaneScreenTop/
    /// GetPositionOffsetPx formulas LayoutPlayerStageCreaturesByLane/ApplyLaneLayout already use for
    /// real creatures (2026-08-12, user-directed: markers should sit "where the player could move
    /// to," not in a generic centered popup) — this also means orientation is correct automatically,
    /// with no separate row-ordering logic to get wrong the way FormationGridPicker's flex-grid
    /// layout did. Reuses FormationGridPicker.BuildCell for cell appearance/state (current/occupied/
    /// free) so the Party menu's grid and these markers never drift out of visual sync. Parented
    /// into _playerStageArea (the same container real creatures live in) and sent to the back so
    /// creatures render on top of markers, not behind them (same precedent as LaneGuideOverlay).
    /// </summary>
    private void ShowStagePositionMarkers(int casterSlotIndex)
    {
        HideStagePositionMarkers();
        if (_playerSide == null || casterSlotIndex < 0 || casterSlotIndex >= _playerSide.Count) return;

        BattleParticipant caster = _playerSide[casterSlotIndex];

        string GetOccupantLabel(int lane, int position)
        {
            foreach (BattleParticipant ally in _playerSide)
            {
                if (ally == caster || !ally.IsAlive) continue;
                if (ally.LaneIndex == lane && ally.PositionIndex == position)
                    return ally.DisplayName.Length > 0 ? ally.DisplayName.Substring(0, 1) : "?";
            }
            return null;
        }

        _stagePositionMarkers = new VisualElement();
        _stagePositionMarkers.AddToClassList("stage-position-markers");
        _stagePositionMarkers.pickingMode = PickingMode.Ignore; // hit-testing is manual (OnMoveDragPointerUp), not UI Toolkit picking

        // + PlayerNameplateClearanceShiftPx: must match LayoutPlayerStageCreaturesByLane's own
        // shift exactly, or a marker's position stops being a truthful promise of where the
        // creature will actually land (2026-08-12, user: "move the grid over by 2 columns").
        float centeringCompensationPx = LaneMovementSystem.PositionRangeWidthPx / 2f + LaneMovementSystem.PlayerNameplateClearanceShiftPx;
        const float markerSizePx = 28f; // matches .formation-grid-cell's width/height
        float centerOffsetPx = (StageCreatureSizePx - markerSizePx) / 2f; // aligns marker CENTER with where a creature's center would be, not just its top-left

        for (int lane = 1; lane <= BattleLaneLayout.LaneCount; lane++)
        {
            float top = LaneMovementSystem.GetLaneScreenTop(lane, isPlayerSide: true) + centerOffsetPx;

            for (int position = 1; position <= LaneMovementSystem.PositionsPerLane; position++)
            {
                bool isCurrent = lane == caster.LaneIndex && position == caster.PositionIndex;
                string occupantLabel = GetOccupantLabel(lane, position);
                Button marker = FormationGridPicker.BuildCell(lane, position, isCurrent, occupantLabel, onClick: null);

                marker.style.position = Position.Absolute;
                marker.style.top = top;
                marker.style.left = LaneMovementSystem.GetPositionOffsetPx(position) + centeringCompensationPx + centerOffsetPx;
                marker.style.marginTop = 0;
                marker.style.marginLeft = 0;
                marker.style.marginRight = 0;
                marker.style.marginBottom = 0;

                _stagePositionMarkers.Add(marker);
            }
        }

        _playerStageArea.Add(_stagePositionMarkers);
        _stagePositionMarkers.SendToBack();
    }

    private void HideStagePositionMarkers()
    {
        if (_stagePositionMarkers == null) return;
        _stagePositionMarkers.RemoveFromHierarchy();
        _stagePositionMarkers = null;
    }

    /// <summary>
    /// Given how long a projectile will take to visually travel (seconds), returns the
    /// timing-ring sweepDuration that makes the ring's "perfect" instant — MarkerRadius exactly
    /// equal to RingTargetRadius — land at that same moment (2026-08-11, combat feedback
    /// timing-sync pass: BattleManager launches a projectile and a ring concurrently, using this
    /// to pick the ring's speed so they always agree on when "perfect" happens). Pure ring-
    /// geometry math: RingMarkerStartRadius/RingTargetRadius/RingMarkerMinRadius are fixed
    /// constants, independent of stats/tolerance — Instinct/bond only widen the SUCCESS WINDOW
    /// around this instant (TimedInputConfig.ComputeToleranceHalfWidth), they never move the
    /// instant itself — so this is a simple derived ratio, not a live simulation.
    /// </summary>
    public float ComputeSweepDurationForTravelTime(float travelDurationSeconds)
    {
        float perfectFraction = (RingMarkerStartRadius - RingTargetRadius) / (RingMarkerStartRadius - RingMarkerMinRadius);
        return travelDurationSeconds / perfectFraction;
    }

    /// <summary>
    /// Multi-Hit Volley's own sync fix (2026-08-15, user: "the timing of the rings is better but i
    /// noticed that the timing of the projectiles isnt sync up... i want to maintain the projectile
    /// speed as it is then adjust the release or showing of the ring accordingly") — same geometry
    /// as ComputeSweepDurationForTravelTime above (stretch the ring's sweep so its "perfect" instant
    /// — MarkerRadius crossing RingTargetRadius — lands exactly when the projectile arrives): a
    /// converging ring's perfect instant is ~51.7% through an unstretched sweep (RingMarkerStartRadius
    /// shrinking down to RingMarkerMinRadius). The projectile keeps traveling for exactly
    /// travelDurationSeconds (the skill-authored SkillData.VolleyRingDurationsSeconds value,
    /// untouched); only the RING's own displayed sweepDuration is stretched to match it.
    ///
    /// 2026-08-15, same-day follow-up (user: "lets do circle... square for right click... Same
    /// timing criteria") — every Volley ring animates the same way now (always converging/shrinking);
    /// left vs. right click is communicated entirely by MARKER SHAPE (RingVisual.MarkerIsSquare) so
    /// it reads from a single glance instead of requiring the player to watch which direction a ring
    /// was moving. This method previously branched on an isConverging flag to pick between two
    /// different perfect-fractions (~51.7% vs ~48.3%) for an expanding-ring variant that no longer
    /// exists — removed rather than left as unreachable dead flexibility.
    /// </summary>
    public float ComputeVolleyRingSweepDuration(float travelDurationSeconds)
    {
        float perfectFraction = (VolleyMarkerStartRadius - VolleyTargetRadius) / (VolleyMarkerStartRadius - RingMarkerMinRadius);
        return travelDurationSeconds / perfectFraction;
    }

    /// <summary>
    /// Offensive action-command check (Combat_Directive_v0_1_0.md Part 4), reworked 2026-08-05
    /// (user-directed, Sonny 2-referenced — see DECISIONS.md -> [Combat]) to mirror
    /// RunDefenseTimedInput's converging-ring visual, positioned above the TARGETED ENEMY (not
    /// the attacker): a fixed target ring plus a white marker ring that starts wider and shrinks
    /// past it over sweepDuration. Reworked again 2026-08-11 (user-directed — see DECISIONS.md ->
    /// [Combat]) from a single tolerance with a cosmetic-only "perfect" sub-flash to two
    /// independent, nested bands mirroring defense outright: goodToleranceHalfWidth is the same
    /// value RunDefenseTimedInput uses for Dodge, perfectToleranceHalfWidth the same value it uses
    /// for Parry. Left-clicking ANYWHERE resolves against whichever band the marker/target radius
    /// ratio falls in at that instant: within perfectToleranceHalfWidth of 1.0 -> Perfect (brighter
    /// neon purple flash); else within goodToleranceHalfWidth -> Good (green flash); otherwise ->
    /// Miss (red flash) — including a timeout with no click before the marker finishes converging.
    /// Unlike defense, a Miss here is NOT a free failure: TimedInputConfig.MissDamageMultiplier
    /// applies a real damage penalty, matched against Good/Perfect's own multipliers by the caller.
    /// Sets LastOffenseOutcome (and, via it, LastTimedInputSuccess/LastTimedInputWasPerfect) — read
    /// them right after this coroutine completes. Deliberately just one window per call — a future
    /// multi-input attack/skill (some offensive actions may need more than one beat) would call
    /// this coroutine multiple times in sequence rather than needing a rewrite here.
    /// </summary>
    public IEnumerator RunTimedInput(string label, float goodToleranceHalfWidth, float perfectToleranceHalfWidth, float sweepDuration)
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
        LastOffenseOutcome = ClassifyOffenseOutcome(clicked, deviation, goodToleranceHalfWidth, perfectToleranceHalfWidth);
        _timingRing.MarkerColor = FlashColorForOffenseOutcome(LastOffenseOutcome);
        _timingRing.Refresh();

        // Brief hold so the player can see the flash (or where the marker landed on a miss) before it hides.
        yield return new WaitForSeconds(0.3f);
        _actionAnnouncement.style.display = DisplayStyle.None;
        _timingRing.style.display = DisplayStyle.None;
        _timingRing.RemoveFromHierarchy();
    }

    /// <summary>Shared Miss/Good/Perfect classification for a converging-or-expanding ring's final deviation — factored out of RunTimedInput (2026-08-14) so RunVolleyRingOffense can reuse the exact same rule instead of duplicating it.</summary>
    private static OffenseOutcome ClassifyOffenseOutcome(bool clicked, float deviation, float goodToleranceHalfWidth, float perfectToleranceHalfWidth)
    {
        return !clicked ? OffenseOutcome.Miss
            : deviation <= perfectToleranceHalfWidth ? OffenseOutcome.Perfect
            : deviation <= goodToleranceHalfWidth ? OffenseOutcome.Good
            : OffenseOutcome.Miss;
    }

    /// <summary>Shared flash-color lookup for an OffenseOutcome — factored out of RunTimedInput (2026-08-14) alongside ClassifyOffenseOutcome.</summary>
    private static Color FlashColorForOffenseOutcome(OffenseOutcome outcome)
    {
        return outcome == OffenseOutcome.Miss ? MissFlashColor
            : outcome == OffenseOutcome.Perfect ? PerfectFlashColor
            : SuccessFlashColor;
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
    ///
    /// 2026-08-11 (user-directed): resolves as a Miss EARLY, before sweepDuration fully elapses,
    /// the moment the marker/target ratio drops past Dodge's lower tolerance bound — from that
    /// point on no click could ever succeed, so there's nothing left to wait for. This is what
    /// keeps a defense-side held projectile (BattleHUDController.LaunchSyncedProjectile/
    /// CombatVfxController) from sitting frozen for the ring's full remaining sweep on a slow or
    /// missing click.
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

            // Once the marker has shrunk past Dodge's lower tolerance bound, no future click can
            // ever succeed — MarkerRadius only decreases from here, so the ratio can only get
            // worse. Ending the wait right here (2026-08-11, user-directed "stuck" feeling fix)
            // instead of running out the rest of sweepDuration is safe precisely because the
            // outcome is already mathematically certain, unlike a flat-delay auto-resolve, which
            // would risk contradicting a still-possible late success.
            if (_timingRing.MarkerRadius / RingTargetRadius < 1f - dodgeToleranceHalfWidth) break;

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

        // Fires the projectile's real outcome cue HERE, immediately — not after this method's own
        // 0.3s ring-flash hold below, and not after whatever BattleManager does once this
        // coroutine returns (damage resolution, RefreshBars, battle-log lines). Both used to wait
        // that long, and live playtesting confirmed it: Parry's outline read as happening "after
        // it shoots the parry attack" instead of at the actual hit, and Dodge's dissolve was easy
        // to miss entirely, buried behind everything that ran before it (2026-08-11 fix). Miss and
        // Dodge resolve fully here (they need no data BattleHUDController doesn't already have).
        // Parry only gets its outline flash here — the deflect-and-counter projectile itself still
        // needs the counter-attacker's own Primal type, which only BattleManager knows, so
        // ResolveParryDeflect stays a separate, later call.
        switch (LastDefenseOutcome)
        {
            case DefenseOutcome.Miss: ResolveHitProjectile(); break;
            case DefenseOutcome.Dodge: ResolveDodgedProjectile(targetSlotIndex); break;
            case DefenseOutcome.Parry: FlashParryOutline(); break;
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

    /// <summary>One ring's resolution outcome, filled in by RunVolleyRingOffense/Defense as it runs (2026-08-14) — a plain mutable holder (not a return value) so BattleManager can read it after `yield return StartCoroutine(...)` completes, the same pattern CombatVfxController.HeldProjectile already uses.</summary>
    public class VolleyRingOutcome
    {
        public bool WasClick;
        public int ClickButton = -1;
        public float FinalDeviation;

        /// <summary>Miss/Good/Perfect classification — only meaningful for an offense-side outcome (RunVolleyRingOffense sets it); left at its default (Miss) for defense, where DefenseOutcome (Dodge/Parry/Miss) is the caller's own concern, not this class's.</summary>
        public OffenseOutcome Quality;
    }

    /// <summary>One ring's entry in a Multi-Hit Volley's FIFO click queue (2026-08-14) — the shared _volleyPointerHandler only ever reads/writes the entry at index 0.</summary>
    private class VolleySlot
    {
        public bool RequiresLeftClick;
        public bool IsDefenseSlot;
        public bool Clicked;
        public int ClickButton = -1;
    }

    /// <summary>
    /// Opens the shared click-routing session for a whole Multi-Hit Volley cast (2026-08-14,
    /// Attack_Pattern_Directive Part 5 Group 2) — call once before the cast's per-hit loop, not
    /// once per ring, since every click for the whole cast must always resolve against whichever
    /// ring is currently oldest (_volleyQueue[0]), not whichever ring's own RunVolleyRingOffense/
    /// Defense call happens to be running most recently. This is the one deliberate deviation from
    /// RunTimedInput/RunDefenseTimedInput's own per-call local handler shape.
    /// </summary>
    public void BeginVolleyInputSession()
    {
        _volleyQueue.Clear();
        _volleyPointerHandler = evt =>
        {
            if (evt.button != 0 && evt.button != 1) return;
            if (_volleyQueue.Count == 0) return;

            VolleySlot front = _volleyQueue[0];
            if (front.Clicked) return; // already resolving this frame

            if (!front.IsDefenseSlot)
            {
                // Offense only — a click must match the front ring's own required type (converging
                // rings want left, expanding rings want right, user: "make the 1st 4 left click
                // rings... last 4 click rings"). A wrong-type click is ignored outright, no effect,
                // no consumption — it does NOT fall through to test against the next ring in queue.
                bool wantsLeft = front.RequiresLeftClick;
                if (wantsLeft && evt.button != 0) return;
                if (!wantsLeft && evt.button != 1) return;
            }

            front.Clicked = true;
            front.ClickButton = evt.button;
        };
        _root.RegisterCallback(_volleyPointerHandler);
    }

    /// <summary>Closes the click-routing session opened by BeginVolleyInputSession — call once after every hit in the cast has resolved.</summary>
    public void EndVolleyInputSession()
    {
        if (_volleyPointerHandler != null) _root.UnregisterCallback(_volleyPointerHandler);
        _volleyPointerHandler = null;
        _volleyQueue.Clear(); // defensive — should already be empty if every ring resolved
    }

    /// <summary>
    /// One ring in a Multi-Hit Volley's FIFO queue (offense side, 2026-08-14). Animates for its own
    /// sweepDuration regardless of queue position (user: "multiple rings could be closing if
    /// multiple projectiles are out") but only actually resolves — pops itself, computes an
    /// outcome — once it is the front of the shared queue AND either clicked-correctly or its own
    /// sweep has elapsed.
    ///
    /// 2026-08-15 (user: "lets do circle like we've done for left click, the square for right
    /// click... Same timing criteria") — every ring now animates identically (always converging/
    /// shrinking toward the target); requiresLeftClick only selects marker SHAPE
    /// (RingVisual.MarkerIsSquare) and which button resolves it, not animation direction anymore —
    /// direction-based encoding needed watching a ring over a couple frames to tell which kind it
    /// was, shape reads instantly. Deviation-ratio scoring (|MarkerRadius/TargetRadius - 1|) was
    /// already shape/direction-agnostic, so Good/Perfect/Miss tolerance checks need no changes.
    /// Does NOT touch LastOffenseOutcome/LastTimedInputSuccess — those are single-slot properties
    /// meant for one ring at a time; concurrently-open Volley rings write into their own
    /// VolleyRingOutcome instead so they can never race each other.
    /// </summary>
    public IEnumerator RunVolleyRingOffense(VisualElement targetElement, CompassPoint point, bool requiresLeftClick,
        float goodToleranceHalfWidth, float perfectToleranceHalfWidth, float sweepDuration, VolleyRingOutcome outcome)
    {
        RingVisual ring = _volleyRingPool.Get();
        PositionVolleyRing(ring, point);
        targetElement.Add(ring);
        ring.TargetRadius = VolleyTargetRadius;
        ring.MarkerColor = VolleyWaitingRingColor;
        ring.MarkerRadius = VolleyMarkerStartRadius;
        ring.MarkerIsSquare = !requiresLeftClick;
        ring.style.scale = new Scale(Vector3.one); // defensive reset — a pooled instance may still carry a leftover punch-scale from a prior use
        ring.Refresh();

        var slot = new VolleySlot { RequiresLeftClick = requiresLeftClick };
        _volleyQueue.Add(slot);

        float elapsed = 0f;
        bool wasFront = false;
        while (true)
        {
            bool isFront = _volleyQueue.Count > 0 && _volleyQueue[0] == slot;

            if (elapsed < sweepDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / sweepDuration);
                ring.MarkerRadius = Mathf.Lerp(VolleyMarkerStartRadius, RingMarkerMinRadius, progress);
            }

            // Color/Refresh live OUTSIDE the sweep-progress gate above — a ring whose own timer has
            // already run out but is still waiting in queue (not yet front) needs to keep repainting
            // every frame so it can flip from dimmed to bright the instant it's promoted, even though
            // its radius itself has stopped animating.
            ring.MarkerColor = isFront ? VolleyActiveRingColor : VolleyWaitingRingColor;

            // 2026-08-15 (user: "make it more distinct... give me some options") — the promotion
            // itself is the event, not the resulting color: fires ONCE, exactly on the false->true
            // transition, never while already front (that would re-punch every single frame).
            if (isFront && !wasFront)
            {
                ring.style.scale = new Scale(new Vector3(VolleyPromotionPunchScale, VolleyPromotionPunchScale, 1f));
                VisualElementTweening.TweenUniformScale(ring, 1f, VolleyPromotionPunchDurationSeconds);
                AudioManager.Instance?.PlayVolleyRingPromoted();
            }
            wasFront = isFront;

            ring.Refresh();

            if (isFront && (slot.Clicked || elapsed >= sweepDuration))
            {
                outcome.WasClick = slot.Clicked;
                outcome.ClickButton = slot.ClickButton;
                outcome.FinalDeviation = Mathf.Abs(ring.MarkerRadius / VolleyTargetRadius - 1f);
                _volleyQueue.Remove(slot);
                break;
            }

            yield return null;
        }

        outcome.Quality = ClassifyOffenseOutcome(outcome.WasClick, outcome.FinalDeviation, goodToleranceHalfWidth, perfectToleranceHalfWidth);
        ring.MarkerColor = FlashColorForOffenseOutcome(outcome.Quality);
        ring.Refresh();

        // Shorter hold than RunTimedInput's 0.3s — Volley's own hits overlap, a full 0.3s hold per
        // ring would visually clutter a fast sequence with several rings resolving close together.
        yield return new WaitForSeconds(0.15f);
        _volleyRingPool.Release(ring);
    }

    /// <summary>
    /// Defense-side counterpart to RunVolleyRingOffense (2026-08-14) — scoped OUT of the
    /// converging/expanding left/right-click split (that mechanic answers a question offense never
    /// had an answer to before: "how good was this hit," since a miss there just deals less damage
    /// rather than being fully avoided). Defense keeps today's existing Dodge(left)/Parry(right)
    /// choice unchanged per ring — every ring in a defensive Volley accepts either button, exactly
    /// like RunDefenseTimedInput already does, just gated to the FIFO queue's front like every
    /// other Volley ring. NOT yet exercised live — see BattleManager.RunVolleyHit's own doc comment
    /// for why the defense body is a documented stub this pass (CombatVfxController._held is a
    /// single-slot field, blocking multiple concurrently-held defense projectiles).
    /// </summary>
    public IEnumerator RunVolleyRingDefense(VisualElement targetElement, CompassPoint point,
        float dodgeToleranceHalfWidth, float parryToleranceHalfWidth, float sweepDuration, VolleyRingOutcome outcome)
    {
        RingVisual ring = _volleyRingPool.Get();
        PositionVolleyRing(ring, point);
        targetElement.Add(ring);
        ring.TargetRadius = VolleyTargetRadius;
        ring.MarkerColor = VolleyWaitingRingColor;
        ring.MarkerRadius = VolleyMarkerStartRadius;
        ring.style.scale = new Scale(Vector3.one); // defensive reset — a pooled instance may still carry a leftover punch-scale from a prior use
        ring.Refresh();

        var slot = new VolleySlot { IsDefenseSlot = true };
        _volleyQueue.Add(slot);

        float elapsed = 0f;
        bool wasFront = false;
        while (true)
        {
            bool isFront = _volleyQueue.Count > 0 && _volleyQueue[0] == slot;

            if (elapsed < sweepDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / sweepDuration);
                ring.MarkerRadius = Mathf.Lerp(VolleyMarkerStartRadius, RingMarkerMinRadius, progress);
            }

            // Same "keep repainting even after this ring's own timer runs out while still queued"
            // reasoning as RunVolleyRingOffense above, plus the same promotion pop+sound event.
            ring.MarkerColor = isFront ? VolleyActiveRingColor : VolleyWaitingRingColor;
            if (isFront && !wasFront)
            {
                ring.style.scale = new Scale(new Vector3(VolleyPromotionPunchScale, VolleyPromotionPunchScale, 1f));
                VisualElementTweening.TweenUniformScale(ring, 1f, VolleyPromotionPunchDurationSeconds);
                AudioManager.Instance?.PlayVolleyRingPromoted();
            }
            wasFront = isFront;

            ring.Refresh();

            if (isFront && (slot.Clicked || elapsed >= sweepDuration))
            {
                outcome.WasClick = slot.Clicked;
                outcome.ClickButton = slot.ClickButton;
                outcome.FinalDeviation = Mathf.Abs(ring.MarkerRadius / VolleyTargetRadius - 1f);
                _volleyQueue.Remove(slot);
                break;
            }

            yield return null;
        }

        DefenseOutcome quality;
        if (!outcome.WasClick) quality = DefenseOutcome.Miss;
        else if (outcome.ClickButton == 0) quality = outcome.FinalDeviation <= dodgeToleranceHalfWidth ? DefenseOutcome.Dodge : DefenseOutcome.Miss;
        else quality = outcome.FinalDeviation <= parryToleranceHalfWidth ? DefenseOutcome.Parry : DefenseOutcome.Miss;

        ring.MarkerColor = quality == DefenseOutcome.Miss ? MissFlashColor : SuccessFlashColor;
        ring.Refresh();

        yield return new WaitForSeconds(0.15f);
        _volleyRingPool.Release(ring);
    }

    /// <summary>
    /// TEMPORARY debug hook (2026-08-14) — deterministically resolves whichever Volley ring is
    /// currently at the front of the FIFO queue, without simulating a real PointerDownEvent.
    /// Reflecting into a private List&lt;VolleySlot&gt; from execute_code test scripts is fragile;
    /// this gives live-testing a stable, public entry point instead. button: 0 = left, 1 = right.
    /// No-op if the queue is empty. DELETE once Multi-Hit Volley no longer needs manual playtesting.
    /// </summary>
    internal void DebugForceResolveVolleyFront(int button)
    {
        if (_volleyQueue.Count == 0) return;
        VolleySlot front = _volleyQueue[0];
        front.Clicked = true;
        front.ClickButton = button;
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
