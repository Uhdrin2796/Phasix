/// <summary>
/// Prototype-tuning constants for the Phase 3 battle loop. Roadmap_v2.md Mo 5 Wk 1-2: "Use
/// BattleConfig.ActivePartySize = 3 as a prototype constant — never magic-number this value.
/// Revisit at Phase 3 gate before building full battle UI; confirm or revise to final value
/// then." Combat_Directive_v0_1_0.md Part 5 lists 3-5 active Phasix per side as still "pending
/// combat system design" — 3 is the chosen prototype value, matching PartySystem.MaxPartySize,
/// not a final decision.
/// </summary>
public static class BattleConfig
{
    public const int ActivePartySize = 3;

    /// <summary>
    /// Flat placeholder damage per basic attack. Superseded in live gameplay by
    /// DamageCalculator's real (AttackerStat / DefenderStat) x skillPower x primalTypeMultiplier
    /// formula (Step 3, Roadmap_v2 Mo 6 Wk 1-2) — BattleManager now passes DamageCalculator's
    /// output explicitly. This constant survives only as BattleEngine.QueueBasicAttack's default
    /// baseDamage value, for callers (mainly EditMode tests) that don't need the real formula.
    /// </summary>
    public const int PlaceholderAttackDamage = 5;

    /// <summary>
    /// How long a non-interactive beat message (e.g. "X is attacking!", a resolved attack's
    /// result) stays on screen before auto-advancing, in seconds. Added 2026-08-05 — user-
    /// directed: the Continue button should only gate the actual player-to-enemy turn
    /// transition, not every individual beat; everything else should just play out with enough
    /// time to read, not require a click. See DECISIONS.md -&gt; [Combat] and
    /// BattleHUDController.ShowTimedMessage. Placeholder value, not playtested.
    /// </summary>
    public const float AutoMessageDurationSeconds = 1.5f;

    /// <summary>
    /// Aura cost per attack (2026-08-05, user-directed — see DECISIONS.md -> [Combat]: "make
    /// them cost some aura"). Both placeholder attacks (MoveOptionsPerSlot) cost the same for now
    /// — real per-skill costs are a later pass once real skill content exists. Spending never
    /// blocks the attack (see BattleParticipant.SpendAura) — it just floors at 0 Aura. Placeholder
    /// value, not playtested.
    /// </summary>
    public const int AttackAuraCost = 2;

    /// <summary>
    /// Aura restored to the defender on a perfect Dodge or Parry (2026-08-05, user-directed — see
    /// DECISIONS.md -> [Combat]: "Perfect dodges and parrys restore aura") — the reward half of
    /// landing the tightest timing tier, on top of avoiding the hit (and, for Parry, the
    /// counter-attack). Placeholder value, not playtested.
    /// </summary>
    public const int PerfectDefenseAuraRestore = 2;

    /// <summary>
    /// Aura restored by the "C" (Charge) move option (2026-08-06, user-directed — see
    /// DECISIONS.md -> [Combat]: "when you click on it, the player does not attack but it
    /// restores 10 mana"). Uses the acting participant's whole turn — no attack, no timed input,
    /// no target selection. Clamped at MaxAura by BattleParticipant.RestoreAura. Placeholder
    /// value, not playtested.
    /// </summary>
    public const int ChargeAuraRestore = 10;

    /// <summary>
    /// Aura cost and instant HP restore for the "H" move option (2026-08-06, user-directed —
    /// see DECISIONS.md -> [Combat]: "the heal should cost 6 aura and heals 4 HP"). Applied
    /// immediately on cast, unlike Regen below — no over-time component. Placeholder values, not
    /// playtested.
    /// </summary>
    public const int HealAuraCost = 6;
    public const int HealAmount = 4;

    /// <summary>
    /// Aura cost, per-turn heal amount, and duration for the "R" (Regen) move option (2026-08-06,
    /// user-directed — see DECISIONS.md -> [Combat]: "a regen that costs 8 aura but heals 2 HP at
    /// the end of the players turn for 4 turns"). Ticks via BattleParticipant.TickRegen, called
    /// once per player turn at the END of PlayerTurn (before the Continue gate) — see
    /// BattleManager.PlayerTurn. Placeholder values, not playtested.
    /// </summary>
    public const int RegenAuraCost = 8;
    public const int RegenHealPerTurn = 2;
    public const int RegenDurationTurns = 4;

    /// <summary>
    /// Evolution Burst gauge fill amounts (2026-08-06 — wiring EvolutionBurstSystem into the live
    /// battle loop for the Phase 3 Gate playtest; see DECISIONS.md -> [Combat]). GDD §9.3 locks
    /// the three fill SOURCES ("skill use, timed inputs, and taking hits") but gives no numbers —
    /// these three amounts are placeholders sized so a full gauge (100) takes roughly a handful of
    /// turns of real play, not a single action. TODO: pending NumericalCalibration.md.
    /// </summary>
    public const float BurstFillPerSkillUse = 15f;
    public const float BurstFillPerTimedInputSuccess = 10f;
    public const float BurstFillPerHitTaken = 10f;

    /// <summary>
    /// Flat skill power/Aura cost for the 36 placeholder skill-ring skills (2026-08 session, see
    /// DECISIONS.md -> [Combat]) — PlaceholderSkillResolver derives WHICH mechanic a skill uses,
    /// but every damage skill deals the same flat power and every skill costs the same flat Aura,
    /// same category as every other constant in this file. Power sits below DamageCalculator.
    /// BasicAttackPower (10) so skills read as secondary options, not strictly better than a plain
    /// Attack; cost sits between AttackAuraCost (2) and HealAuraCost (6), reflecting "does more
    /// than a plain attack." TODO: pending NumericalCalibration.md. Not playtested.
    /// </summary>
    public const int PlaceholderSkillPower = 8;
    public const int PlaceholderSkillAuraCost = 3;

    /// <summary>
    /// Flat Common Aura reward granted to each surviving party member on a battle win (2026-08
    /// session — first real implementation of Progression_Directive_v0_1_0.md's "Common Aura
    /// drops from all Phasix in battle," which existed only as an unwired EventBus.OnAuraDropped
    /// stub before this; see DECISIONS.md -> [Combat]). Feeds the post-battle summary screen's
    /// "Aura Gained" line. Flat and per-individual rather than scaled by the enemy defeated —
    /// nothing in the Directive specifies a scaling curve yet. TODO: pending NumericalCalibration.md.
    /// </summary>
    public const int AuraRewardOnWin = 15;
}
