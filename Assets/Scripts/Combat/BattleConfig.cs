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

    /// <summary>
    /// Chance a Flee attempt succeeds (2026-08-10, user-directed: "lets make it like 80% success
    /// rate for now"). Rolled once per click via BattleManager.PlayerTurn — a failed attempt still
    /// consumes the acting side's whole turn (same as End Turn), same convention as every other
    /// "uses the turn regardless of outcome" move in this file. Placeholder value, not playtested.
    /// </summary>
    public const float FleeSuccessChance = 0.8f;

    /// <summary>
    /// Enemy target-selection weighting (2026-08-10 — enemy AI heuristic pass, closing out the
    /// Combat_Directive_v0_1_0.md-flagged gap that EnemyTurn was pure Random.Range target choice).
    /// EnemyAI.ComputeTargetWeight scores each alive candidate as
    /// 1f + hpFactor * EnemyTargetLowHpWeight + typeFactor * EnemyTargetTypeEffectivenessWeight,
    /// then ChooseTarget draws a weighted-random pick over those scores — every target keeps a
    /// nonzero chance (the "+1f" floor), this only biases the odds. This is a heuristic upgrade,
    /// not the real AI decision-making framework Combat_Directive_v0_1_0.md flags as pending
    /// design (GDD §18.6) — see EnemyAI.cs's class doc comment. TODO: pending NumericalCalibration.md.
    /// </summary>
    public const float EnemyTargetLowHpWeight = 2f;
    public const float EnemyTargetTypeEffectivenessWeight = 1f;

    /// <summary>
    /// Enemy self-support move selection (2026-08-10 — see EnemyAI.ChooseSkill). Below this
    /// fraction of max HP, an enemy with an equipped self-support move (Charge/Heal/Regen, or a
    /// self-targeted status skill per PlaceholderSkillResolver) has EnemySelfCareChance odds of
    /// using it instead of attacking. Same placeholder-heuristic scope note as the weights above.
    /// TODO: pending NumericalCalibration.md.
    /// </summary>
    public const float EnemySelfCareHpThreshold = 0.35f;
    public const float EnemySelfCareChance = 0.5f;

    /// <summary>
    /// Enemy Zone/Positional dodge chance (2026-08-21, offense-direction follow-up to
    /// Attack_Pattern_Directive Group 3 item 7 — see EnemyAI.TryChooseDodgeStep). Base window
    /// percent fed into TimedInputConfig.ComputeWindowPercent (the SAME Instinct/bond-scaling curve
    /// Dodge/Parry already use — "higher Instinct = larger window", CLAUDE.md), then read as a
    /// direct percent-chance (5-60% range, matching ComputeWindowPercent's own clamp) rather than a
    /// ring-ratio tolerance. Multiplied by the encounter's EnemyDifficultyTier before rolling.
    /// AlwaysDodges bypasses this formula entirely (hard 100%, see EnemyAI.TryChooseDodgeStep).
    /// TODO: pending NumericalCalibration.md.
    /// </summary>
    public const float ZoneDodgeBaseWindowPercent = 20f;
    public const float ZoneDodgeDifficultyMultiplierWeak = 0.5f;
    public const float ZoneDodgeDifficultyMultiplierStandard = 1f;
    public const float ZoneDodgeDifficultyMultiplierElite = 1.75f;

    /// <summary>
    /// Placeholder projectile travel speed, in Stage-local pixels/second (2026-08-11 — combat
    /// feedback timing-sync pass). Combined with the real edge-to-edge distance between attacker
    /// and target, this derives how long a hit's timing-ring sweep needs to be
    /// (BattleHUDController.ComputeSweepDurationForTravelTime) so the ring's "perfect" instant
    /// always lines up with the moment the projectile visually connects — replacing the old flat
    /// TimedInputConfig.MarkerSweepDuration at the real attack call sites. A single flat value for
    /// every attack today, same "plumbing supports variation, values are placeholder" convention as
    /// every other constant in this file — different skills getting their own speed is future
    /// content (multi-hit/rhythm attacks), not built yet. TODO: pending NumericalCalibration.md.
    /// </summary>
    public const float ProjectileSpeed = 700f;

    /// <summary>
    /// Hard ceiling on a projectile's computed travel time, in seconds (2026-08-11 — fixes the
    /// "stuck" feeling on defense: since the timing ring's sweepDuration is derived from travel
    /// time, and the ring keeps running after the projectile visually arrives until the player
    /// clicks or times out, a long travel time also means a long silent wait afterward — roughly
    /// another ~0.93x the travel time again, since "perfect" sits at ~52% of the sweep. Capping
    /// travel time here caps that wait proportionally too, without touching the ring-perfect
    /// alignment math or the click-timing rules at all — a long-distance matchup's projectile
    /// just moves faster than ProjectileSpeed would otherwise imply, same as any other placeholder
    /// number in this file. TODO: pending NumericalCalibration.md.
    /// </summary>
    public const float MaxProjectileTravelDuration = 0.8f;
}
