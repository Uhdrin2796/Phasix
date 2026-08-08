using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// One combatant's battle-only state — current HP and which side it's fighting on. Plain C#,
/// never persisted or saved; built fresh from a PhasixRuntimeData at battle start and discarded
/// when the battle ends (matches the Hard Architecture Rule: runtime state is plain C#, not the
/// ScriptableObject/SO species template). MaxHP reads directly from EffectiveStat(Vitality) —
/// TODO: pending NumericalCalibration.md if Vitality-as-HP ever needs its own scaling multiplier
/// instead of a 1:1 mapping.
/// </summary>
public class BattleParticipant
{
    public PhasixRuntimeData RuntimeData { get; }
    public bool IsPlayerSide { get; }
    public int MaxHP { get; }
    public int CurrentHP { get; private set; }
    public bool IsAlive => CurrentHP > 0;

    /// <summary>
    /// The Aura BASE STAT (EffectiveStat(StatType.Aura) — one of the 8 base attributes), not the
    /// Aura RESOURCE (commonAura/specificAura/rareVariantAura currency, which lives on
    /// PhasixRuntimeData and is spent via the Step 5 Aura system, Roadmap_v2 Mo 8 Wk 4). Nothing
    /// consumes this in battle yet — it's a static full bar for now, matching the stat's own
    /// nature until a real in-battle Aura-cost mechanic exists.
    /// </summary>
    public int MaxAura { get; }
    public int CurrentAura { get; private set; }

    /// <summary>
    /// Turns left on an active Regen status ("R" move option, 2026-08-06, user-directed — see
    /// DECISIONS.md -> [Combat]) — 0 means no Regen active. Counts DOWN (4, 3, 2, 1, then 0/gone)
    /// per the user's explicit ask for an intuitive countdown display, not a count-up.
    /// </summary>
    public int RegenTurnsRemaining { get; private set; }

    /// <summary>HP healed by each TickRegen call while RegenTurnsRemaining &gt; 0. 0 when no Regen is active.</summary>
    public int RegenHealPerTurn { get; private set; }

    /// <summary>
    /// Mid-battle Evolution Burst state (GDD §9.3, EvolutionBurstSystem) — wired into the live
    /// battle loop 2026-08-06 for the Phase 3 Gate playtest (see DECISIONS.md -> [Combat]). Every
    /// participant gets one (plain data, harmless if never filled) rather than gating it behind
    /// IsPlayerSide, matching how MaxHP/MaxAura aren't gated either — BattleManager only actually
    /// fills/ticks this for player-side participants for now (see TickPlayerBurst), same scope
    /// discipline Regen already established.
    /// </summary>
    public EvolutionBurstGauge BurstGauge { get; } = new EvolutionBurstGauge();

    /// <summary>
    /// Whether this participant has already used their action this player turn — reset to false
    /// at the start of every PlayerTurn (2026-08-06, user-directed — see DECISIONS.md -> [Combat]:
    /// free-choice creature selection replaced strict turn order, so BattleManager needs to know
    /// who's already gone). Drives BattleHUDController.ShowMoveSelectionReadOnly's greyed-out
    /// "already acted" state when the player re-clicks this creature. Currently every move greys
    /// out once this is true — plain public state, not baked into any one move's logic, so a
    /// future synergy skill or passive that allows a second action per turn only needs to check
    /// this flag differently for that specific option, not restructure how it's tracked.
    /// </summary>
    public bool HasActedThisTurn { get; set; }

    /// <summary>
    /// Trailing-history window shared by every combo-detection rule (cross-tree, repeat-skill,
    /// timed-input-streak) — matches ComboEngine.DetectCombo's own Quad ceiling (4), not a new
    /// number. 2026-08 session, see DECISIONS.md -> [Combat].
    /// </summary>
    private const int ComboHistoryWindow = 4;

    // --- Active status effects (ChainResultCatalog/MasteryBonusCatalog wiring, 2026-08 session — see DECISIONS.md -> [Combat]) ---

    private readonly List<ActiveStatusInstance> _activeStatuses = new List<ActiveStatusInstance>();

    /// <summary>Currently-active status effects on this participant. Read-only externally — mutate via ApplyStatus/TickStatuses.</summary>
    public IReadOnlyList<ActiveStatusInstance> ActiveStatuses => _activeStatuses;

    /// <summary>Convenience projection for ChainResultCatalog.TryResolve/MasteryBonusCatalog.EvaluateAll, which take a plain type collection.</summary>
    public List<StatusEffectType> ActiveStatusTypes => _activeStatuses.Select(s => s.Type).ToList();

    /// <summary>
    /// MasteryBonusCatalog's own doc comment states it does NOT track "already triggered this
    /// battle" itself — this is that caller-side bookkeeping, per bonus, per battle.
    /// </summary>
    public HashSet<MasteryBonusType> TriggeredMasteryBonusesThisBattle { get; } = new HashSet<MasteryBonusType>();

    /// <summary>Last chain result logged against this participant — lets BattleManager log only a *change*, not every subsequent turn the same pair of statuses stays active.</summary>
    public ChainResultType? ActiveChainResult { get; set; }

    /// <summary>Starts (or refreshes) a status — overwrite-not-stack on the same type, matching ApplyRegen's existing precedent (re-applying resets the countdown, no independent second timer).</summary>
    public void ApplyStatus(StatusEffectType type, int durationTurns)
    {
        if (durationTurns <= 0) return;

        ActiveStatusInstance existing = _activeStatuses.Find(s => s.Type == type);
        if (existing != null)
        {
            existing.TurnsRemaining = durationTurns;
            return;
        }

        _activeStatuses.Add(new ActiveStatusInstance(type, durationTurns));
    }

    /// <summary>Decrements every active status by one turn and removes any that reach 0. Returns the types that just expired, for logging.</summary>
    public List<StatusEffectType> TickStatuses()
    {
        var expired = new List<StatusEffectType>();

        for (int i = _activeStatuses.Count - 1; i >= 0; i--)
        {
            _activeStatuses[i].TurnsRemaining--;
            if (_activeStatuses[i].TurnsRemaining <= 0)
            {
                expired.Add(_activeStatuses[i].Type);
                _activeStatuses.RemoveAt(i);
            }
        }

        return expired;
    }

    // --- Combo-rule tracking (ComboEngine + the new pluggable ComboRuleEvaluator rules, 2026-08 session — see DECISIONS.md -> [Combat]) ---

    private readonly List<SkillTreeType> _recentSkillTrees = new List<SkillTreeType>();
    private readonly List<SkillData> _recentSkillsUsed = new List<SkillData>();
    private readonly List<bool> _recentTimedInputPerfects = new List<bool>();

    /// <summary>Feeds ComboEngine.DetectCombo (GDD §4.2-locked base rule). Only recorded for tree-tagged skill-ring uses, never the 5 built-in A/C/H/R/K moves (no SkillTreeType).</summary>
    public IReadOnlyList<SkillTreeType> RecentSkillTrees => _recentSkillTrees;

    /// <summary>Feeds ComboRuleEvaluator.EvaluateRepeatSameSkill (checked against the specific granting skill, e.g. "C1" — see BattleManager.FindGrantingSkill).</summary>
    public IReadOnlyList<SkillData> RecentSkillsUsed => _recentSkillsUsed;

    /// <summary>
    /// Feeds ComboRuleEvaluator.EvaluateTimedInputStreak. Despite the historical field/method
    /// naming ("Result"/"success" below), this specifically tracks PERFECT timed-input hits, not
    /// merely successful ones (2026-08 follow-up, user-directed: "works with any other attacking
    /// skill that gets perfect, after a miss it rests" — a non-perfect success doesn't extend the
    /// streak either). Populate from BattleHUDController.LastTimedInputWasPerfect, not
    /// LastTimedInputSuccess.
    /// </summary>
    public IReadOnlyList<bool> RecentTimedInputPerfects => _recentTimedInputPerfects;

    /// <summary>
    /// Which combo rules currently apply to this participant. CrossTreeSequence is always
    /// included; RepeatSameSkill/TimedInputStreak are only included if a currently-equipped skill
    /// grants them (see RefreshActiveComboRules). New, user-directed mechanic — not GDD content.
    /// </summary>
    public HashSet<ComboRuleType> ActiveComboRules { get; } = new HashSet<ComboRuleType> { ComboRuleType.CrossTreeSequence };

    public void RecordSkillTreeUse(SkillTreeType tree)
    {
        _recentSkillTrees.Add(tree);
        TrimToWindow(_recentSkillTrees);
    }

    public void RecordSkillUse(SkillData skill)
    {
        _recentSkillsUsed.Add(skill);
        TrimToWindow(_recentSkillsUsed);
    }

    /// <summary>Records whether this action's timed input was PERFECT (not merely successful) — see RecentTimedInputPerfects.</summary>
    public void RecordTimedInputPerfect(bool wasPerfect)
    {
        _recentTimedInputPerfects.Add(wasPerfect);
        TrimToWindow(_recentTimedInputPerfects);
    }

    private static void TrimToWindow<T>(List<T> list)
    {
        while (list.Count > ComboHistoryWindow) list.RemoveAt(0);
    }

    /// <summary>
    /// Recomputes ActiveComboRules by scanning this participant's currently-equipped skills for
    /// any with a non-None SkillData.GrantsComboRule. Call once at battle start — equipped skills
    /// don't change mid-battle today, so there's no other point this needs re-running.
    /// </summary>
    public void RefreshActiveComboRules(SkillDatabase database)
    {
        ActiveComboRules.Clear();
        ActiveComboRules.Add(ComboRuleType.CrossTreeSequence);

        if (database == null) return;

        foreach (string guid in RuntimeData.equippedSkillGuids)
        {
            if (database.TryGetByGuid(guid, out SkillData skill) && skill.GrantsComboRule != ComboRuleType.None)
            {
                ActiveComboRules.Add(skill.GrantsComboRule);
            }
        }
    }

    public string DisplayName => RuntimeData.speciesData != null ? RuntimeData.speciesData.SpeciesName : "???";

    public BattleParticipant(PhasixRuntimeData runtimeData, bool isPlayerSide)
    {
        RuntimeData = runtimeData;
        IsPlayerSide = isPlayerSide;
        MaxHP = Mathf.Max(1, runtimeData.EffectiveStat(StatType.Vitality));
        CurrentHP = MaxHP;
        MaxAura = Mathf.Max(0, runtimeData.EffectiveStat(StatType.Aura));
        CurrentAura = MaxAura;
    }

    /// <summary>Applies damage, clamped so HP never goes negative. Negative/zero amounts are ignored.</summary>
    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
    }

    /// <summary>
    /// Restores HP, clamped at MaxHP — used by the instant-effect "H" move option (2026-08-06,
    /// user-directed — see DECISIONS.md -> [Combat]: "heals 4 HP") and by TickRegen below.
    /// Negative/zero amounts are ignored.
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
    }

    /// <summary>
    /// Starts (or refreshes) a Regen status — "R" move option (2026-08-06, user-directed — see
    /// DECISIONS.md -> [Combat]: "a regen that costs 8 aura but heals 2 HP at the end of the
    /// players turn for 4 turns"). Overwrites any Regen already in progress rather than stacking
    /// — re-casting Regen just resets the countdown, it doesn't add a second independent timer
    /// (no stacking rule requested; simplest behavior until one is). Negative/zero arguments are
    /// ignored.
    /// </summary>
    public void ApplyRegen(int healPerTurn, int turns)
    {
        if (healPerTurn <= 0 || turns <= 0) return;
        RegenHealPerTurn = healPerTurn;
        RegenTurnsRemaining = turns;
    }

    /// <summary>
    /// Heals RegenHealPerTurn HP (clamped at MaxHP via Heal) and counts RegenTurnsRemaining DOWN
    /// by one — call once per player turn, at the END of the turn (BattleManager.PlayerTurn, right
    /// before the Continue gate). Returns the actual HP healed this tick (0 if no Regen is
    /// active, or if already at full HP). Clears RegenHealPerTurn once the countdown reaches 0 so
    /// a later ApplyRegen doesn't need to also reset it.
    /// </summary>
    public int TickRegen()
    {
        if (RegenTurnsRemaining <= 0) return 0;

        int before = CurrentHP;
        Heal(RegenHealPerTurn);
        int healed = CurrentHP - before;

        RegenTurnsRemaining--;
        if (RegenTurnsRemaining <= 0) RegenHealPerTurn = 0;

        return healed;
    }

    /// <summary>
    /// Spends Aura for an attack (2026-08-05, user-directed — see DECISIONS.md -> [Combat]:
    /// "make them cost some aura"), clamped at 0 — never blocks the attack itself, a Phasix can
    /// always attack even at 0 Aura, the cost just floors out rather than going negative.
    /// Negative/zero amounts are ignored.
    /// </summary>
    public void SpendAura(int amount)
    {
        if (amount <= 0) return;
        CurrentAura = Mathf.Max(0, CurrentAura - amount);
    }

    /// <summary>
    /// Restores Aura, clamped at MaxAura — used for a perfect Dodge/Parry's reward (2026-08-05,
    /// user-directed — see DECISIONS.md -> [Combat]: "Perfect dodges and parrys restore aura").
    /// Negative/zero amounts are ignored.
    /// </summary>
    public void RestoreAura(int amount)
    {
        if (amount <= 0) return;
        CurrentAura = Mathf.Min(MaxAura, CurrentAura + amount);
    }
}
