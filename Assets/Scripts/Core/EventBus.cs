using System;

/// <summary>
/// Central event hub for all cross-system communication in Phasix.
/// Static class — no MonoBehaviour, no scene dependency. Any script can
/// subscribe or raise events without holding a reference to this object.
///
/// Usage:
///   Subscribe:   EventBus.OnBondChanged += MyHandler;
///   Unsubscribe: EventBus.OnBondChanged -= MyHandler;
///   Raise:       EventBus.Raise_BondChanged(phasix, newValue);
///
/// Always unsubscribe in OnDisable() or OnDestroy() to prevent memory leaks.
/// </summary>
public static class EventBus
{
    // -------------------------------------------------------------------------
    // PHASE 2 — Bond System (live — wired in BondSystem.cs)
    // -------------------------------------------------------------------------

    /// <summary>Fires on every bond value change, however small.</summary>
    public static event Action<PhasixRuntimeData, float> OnBondChanged;

    /// <summary>
    /// Fires when bond crosses a milestone threshold (20 / 40 / 60 / 80 / 100%).
    /// Used by skill tree gating: Type F unlocks at Familiar (20%), Type O at Companion (40%).
    /// </summary>
    public static event Action<PhasixRuntimeData, BondZone> OnBondMilestoneReached;

    public static void Raise_BondChanged(PhasixRuntimeData phasix, float newBondPercent)
        => OnBondChanged?.Invoke(phasix, newBondPercent);

    public static void Raise_BondMilestoneReached(PhasixRuntimeData phasix, BondZone newZone)
        => OnBondMilestoneReached?.Invoke(phasix, newZone);


    // -------------------------------------------------------------------------
    // PHASE 2 — Personality System (live — wired in PersonalitySystem.cs)
    // -------------------------------------------------------------------------

    /// <summary>Fires when a Phasix's personality changes (item-based swap, GDD §7.2). Not fired on initial capture-time roll.</summary>
    public static event Action<PhasixRuntimeData, Personality> OnPersonalityChanged;

    public static void Raise_PersonalityChanged(PhasixRuntimeData phasix, Personality newPersonality)
        => OnPersonalityChanged?.Invoke(phasix, newPersonality);


    // -------------------------------------------------------------------------
    // PHASE 3 — Battle System (stubs — wired when BattleManager is built, Mo 5)
    // -------------------------------------------------------------------------

    /// <summary>Fires when the player wins a battle. Used by AuraManager for drop calculation.</summary>
    public static event Action<BattleResult> OnBattleWon;

    /// <summary>Fires when the player loses a battle. Loss state: currency/item cost only — no Aura/bond loss.</summary>
    public static event Action<BattleResult> OnBattleLost;

    /// <summary>Fires when the player successfully flees a battle via the Flee button (2026-08-10). Distinct from OnBattleLost — fleeing has zero cost, unlike a real loss.</summary>
    public static event Action<BattleResult> OnBattleFled;

    /// <summary>Fires when any Phasix uses a skill in battle.</summary>
    public static event Action<PhasixRuntimeData, SkillData> OnSkillUsed;

    /// <summary>Fires when a timed input action command succeeds. Used for bond gain and combo tracking.</summary>
    public static event Action<PhasixRuntimeData> OnTimedInputSuccess;

    /// <summary>Fires when a Phasix takes damage. Used by bond gauge fill logic.</summary>
    public static event Action<PhasixRuntimeData, int> OnDamageTaken;

    public static void Raise_BattleWon(BattleResult result)                               => OnBattleWon?.Invoke(result);
    public static void Raise_BattleLost(BattleResult result)                              => OnBattleLost?.Invoke(result);
    public static void Raise_BattleFled(BattleResult result)                              => OnBattleFled?.Invoke(result);
    public static void Raise_SkillUsed(PhasixRuntimeData phasix, SkillData skill)         => OnSkillUsed?.Invoke(phasix, skill);
    public static void Raise_TimedInputSuccess(PhasixRuntimeData phasix)                  => OnTimedInputSuccess?.Invoke(phasix);
    public static void Raise_DamageTaken(PhasixRuntimeData phasix, int damage)            => OnDamageTaken?.Invoke(phasix, damage);


    // -------------------------------------------------------------------------
    // PHASE 4 — Evolution / Capture / Aura (stubs — wired when those systems are built, Mo 10+)
    // -------------------------------------------------------------------------

    /// <summary>Fires when a Phasix evolves to a new form.</summary>
    public static event Action<PhasixRuntimeData, PhasixData> OnEvolved;

    /// <summary>
    /// Fires when a Phasix devolves. Devolution is FREE — no cost, no conditions.
    /// Authority: Evolution_System_Directive_v1_1_0. Triggers unnamed pool growth and Aptitude +1.
    /// </summary>
    public static event Action<PhasixRuntimeData, PhasixData> OnDevolved;

    /// <summary>Fires when a wild Phasix is successfully captured and added to party/storage.</summary>
    public static event Action<PhasixRuntimeData> OnPhasixCaptured;

    /// <summary>
    /// Fires when Aura drops after a battle win.
    /// auraTypeId: "common" | "specific_{emotionalType}" | "rareVariant"
    /// </summary>
    public static event Action<string, int> OnAuraDropped;

    public static void Raise_Evolved(PhasixRuntimeData phasix, PhasixData newForm)        => OnEvolved?.Invoke(phasix, newForm);
    public static void Raise_Devolved(PhasixRuntimeData phasix, PhasixData previousForm)  => OnDevolved?.Invoke(phasix, previousForm);
    public static void Raise_PhasixCaptured(PhasixRuntimeData phasix)                     => OnPhasixCaptured?.Invoke(phasix);
    public static void Raise_AuraDropped(string auraTypeId, int amount)                   => OnAuraDropped?.Invoke(auraTypeId, amount);


    // -------------------------------------------------------------------------
    // PHASE 2 — Wild Encounter Scaffold (live — wired in WildEncounterCreature.cs)
    // Wk 14-16 scaffold only — superseded once the three-layer encounter system
    // (WorldDesign_Directive) replaces this trigger model. Contact now auto-engages straight into
    // BattleScene_Main (2026-08-10, user-directed) — there is no more pre-battle Flee choice, so
    // OnWildEncounterFled was removed as dead code alongside WildEncounterCreature.HandleFlee.
    // Fleeing lives entirely inside the battle itself now (BattleManager/EventBus.OnBattleFled).
    // -------------------------------------------------------------------------

    /// <summary>Fires the instant the player makes contact with a wild Phasix.</summary>
    public static event Action<PhasixRuntimeData> OnWildEncounterTriggered;

    /// <summary>Fires when contact auto-engages into BattleScene_Main.</summary>
    public static event Action<PhasixRuntimeData> OnWildEncounterEngageRequested;

    public static void Raise_WildEncounterTriggered(PhasixRuntimeData phasix)       => OnWildEncounterTriggered?.Invoke(phasix);
    public static void Raise_WildEncounterEngageRequested(PhasixRuntimeData phasix) => OnWildEncounterEngageRequested?.Invoke(phasix);
}
