using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coroutine-driven turn state machine for BattleScene_Main: PlayerTurn -> EnemyTurn ->
/// CheckWinLoss -> EndBattle, looping back to PlayerTurn while InProgress. Each individual
/// attack (player or enemy) resolves and refreshes the HUD immediately as it happens, rather
/// than batching a whole round together — clicking Attack only damages the enemy; the enemy's
/// counter-attack is its own separate beat afterward, not bundled into the same click. Every
/// attack — player's or enemy's — runs an action-command timing minigame first
/// (BattleHUDController.RunTimedInput, Combat_Directive Part 4): offense on the player's own
/// attacks boosts damage on success. The enemy's attacks instead go through a full-avoidance
/// Dodge/Parry defense (Expedition 33-inspired — DECISIONS.md -> [Combat]): a single live click
/// (left = Dodge, right = Parry, BattleHUDController.RunDefenseTimedInput) either fully avoids
/// the hit or lets it through at full damage; a successful Parry also triggers an automatic
/// counter-attack. Pacing (2026-08-05/06, user-directed — see DECISIONS.md -> [Combat]): every
/// beat, including the player-to-enemy turn transition, auto-advances via
/// BattleHUDController.ShowTimedMessage — shows the message for BattleConfig.
/// AutoMessageDurationSeconds and moves on with no click required. The player-to-enemy
/// transition used to gate on a Continue button (BattleHUDController.WaitForContinue); removed
/// 2026-08-06 once the user judged the extra click unnecessary — a delay is enough for the
/// turn-switch to read clearly.
/// Reads BattleTransition.PendingEnemy + PartySystem.Instance (still valid — overworld stays
/// loaded underneath combat, additive load per Combat_Directive_v0_1_0.md Part 1) to build the
/// BattleState, then hands all turn-resolution rules to the static BattleEngine. Enemy uses
/// random target selection for now (Roadmap_v2 Mo 5 Wk 1-2: "Enemy uses random move selection
/// for now") — there's only one move (Attack) until the skill tree framework exists, so "random
/// move" reduces to "random target" here.
///
/// Phase 3 Gate wiring (2026-08-06 — see DECISIONS.md -> [Combat]): CaptureSystem ("K" move
/// option — a successful attempt ends the battle immediately via EndBattle(Won), see
/// _battleEndedEarly) and EvolutionBurstSystem are both now reachable in live play.
/// EvolutionBurstSystem's gauge fills automatically (AddBurstFill, GDD §9.3's three locked fill
/// sources), shown as a visible purple bar under each player's Aura bar
/// (BattleHUDController.SetBurstFillBar), but does NOT auto-trigger — once full, the bar itself
/// outlines yellow and becomes clickable; HandleBurstBarClicked (subscribed to
/// BattleHUDController.BurstBarClicked) calls EvolutionBurstSystem.ActivateReady, which only
/// succeeds on a genuinely full gauge. Status-only once active — no stat/damage effect, since
/// "ApplyBurstEffects" is explicitly undesigned in the GDD. ComboEngine/StatusEffectCatalog/
/// ChainResultCatalog/MasteryBonusCatalog and AuraStatAllocationSystem are DELIBERATELY still not
/// wired in — DECISIONS.md's [Combat] "Skill tree framework" entry explicitly defers the former
/// to a future real skill-selection UI, and the latter is a post-battle progression/menu system,
/// not a mid-battle mechanic.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("Combat Data")]
    [Tooltip("Assign Assets/Data/TypeCharts/PrimalTypeChart.asset. If left empty, damage falls back to a neutral 1.0x type multiplier instead of crashing.")]
    [SerializeField] private PrimalTypeChart _typeChart;

    private BattleState _state;
    private bool _playerActionChosen;
    private BattleParticipant _pendingTarget;
    private bool _battleEndedEarly; // set true by a successful Capture — EndBattle already ran, RunBattleLoop must not also call EnemyTurn
    private Camera _overworldCamera;
    private int _overworldCullingMask;
    private CameraClearFlags _overworldClearFlags;
    private Color _overworldBackgroundColor;

    private void Start()
    {
        PhasixRuntimeData enemyData = BattleTransition.PendingEnemy;
        BattleTransition.ClearPending();

        if (enemyData == null)
        {
            Debug.LogError("[BattleManager] BattleScene_Main loaded with no PendingEnemy set — aborting.");
            return;
        }

        List<BattleParticipant> playerSide = BuildPlayerSide();
        var enemySide = new List<BattleParticipant> { new BattleParticipant(enemyData, isPlayerSide: false) };
        _state = new BattleState(playerSide, enemySide);

        // Overworld stays loaded underneath (additive load, Combat_Directive Part 1) but must not
        // be VISIBLE — otherwise the battle just reads as a HUD floating over the paused overworld
        // instead of a real scene cut. IMPORTANT: don't disable the Camera component entirely —
        // tried that first and it left zero enabled cameras in the scene, which made the Unity
        // Editor's Game View fall into its "Display 1: No cameras rendering" state. That state
        // caused the Screen Space Overlay HUD's effective canvas size to resolve inconsistently
        // for the first couple of frames (observed live: the whole HUD — bottom bar, both stage
        // creatures — visibly jumped down right as the battle scene loaded, before settling).
        // Instead, keep the camera enabled but tell it to render nothing: cullingMask = 0 (no
        // layers) + clearFlags = SolidColor, so the Game View always has a valid active camera and
        // the panel's canvas size stays stable. Cache everything before touching it so EndBattle
        // can restore the overworld exactly as it was.
        _overworldCamera = Camera.main;
        if (_overworldCamera != null)
        {
            _overworldCullingMask = _overworldCamera.cullingMask;
            _overworldClearFlags = _overworldCamera.clearFlags;
            _overworldBackgroundColor = _overworldCamera.backgroundColor;

            _overworldCamera.cullingMask = 0;
            _overworldCamera.clearFlags = CameraClearFlags.SolidColor;
            _overworldCamera.backgroundColor = Color.black;
        }

        // Companion keeps following via A* Pathfinding (CompanionAI/AIPath) even though the
        // player is frozen and the overworld camera is hidden for the whole battle — AIPath
        // repaths on its own internal timer regardless of whether the destination actually
        // moved, so left running it's pure wasted computation with nothing visible to show for
        // it (2026-08-06, user-noticed live in the console). Paused here, resumed in EndBattle.
        PartySystem.Instance?.ActiveCompanionAI?.SetPaused(true);

        BattleHUDController.Instance.Show();
        BattleHUDController.Instance.Initialize(playerSide, enemySide);
        BattleHUDController.Instance.ClearBattleLog();

        // Evolution Burst activation is a free, click-anytime action on the gauge bar itself
        // (2026-08-06, user-directed — see DECISIONS.md -> [Combat]: "I think the activation can
        // be on the bar itself"), NOT a move-selection-gated option — so it's wired via this
        // event rather than through ShowMoveSelection's per-turn callback. BattleHUDController
        // and BattleManager share BattleScene_Main's lifetime (both die together on scene
        // unload), so no explicit unsubscribe is needed.
        BattleHUDController.Instance.BurstBarClicked += HandleBurstBarClicked;

        StartCoroutine(RunBattleLoop());
    }

    /// <summary>
    /// Manually activates a player's Evolution Burst if (and only if) their gauge has actually
    /// reached full — "they can only activate when the gauge is full" (2026-08-06, user-
    /// confirmed). EvolutionBurstSystem.ActivateReady's own guard makes this safe to call for any
    /// click, ready or not: an early/no-op click (still filling, already active, or a dead
    /// party member) just does nothing, no error, no wasted turn.
    /// </summary>
    private void HandleBurstBarClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _state.PlayerSide.Count) return;

        BattleParticipant p = _state.PlayerSide[slotIndex];
        if (!p.IsAlive) return;
        if (!EvolutionBurstSystem.ActivateReady(p.BurstGauge, p.RuntimeData.bondPercent)) return;

        BattleHUDController.Instance.SetBurstFillBar(slotIndex, p.BurstGauge.FillPercent, ready: false);
        BattleHUDController.Instance.SetBurstStatus(slotIndex, p.BurstGauge.RemainingDurationTurns);
        BattleHUDController.Instance.AppendBattleLog($"{p.DisplayName}'s Evolution Burst ignites!");
    }

    /// <summary>Builds player-side participants from PartySystem's filled slots, capped at BattleConfig.ActivePartySize.</summary>
    private List<BattleParticipant> BuildPlayerSide()
    {
        var participants = new List<BattleParticipant>();
        for (int i = 0; i < BattleConfig.ActivePartySize; i++)
        {
            PhasixRuntimeData slot = PartySystem.Instance != null ? PartySystem.Instance.GetSlot(i) : null;
            if (slot != null) participants.Add(new BattleParticipant(slot, isPlayerSide: true));
        }
        return participants;
    }

    private IEnumerator RunBattleLoop()
    {
        while (true)
        {
            yield return StartCoroutine(PlayerTurn());
            if (_battleEndedEarly || TryEndBattle()) yield break;

            yield return StartCoroutine(EnemyTurn());
            if (TryEndBattle()) yield break;
        }
    }

    /// <summary>Move option indices, index-matched to BattleHUDController.MoveOptionClockHours/MoveOptionIsSelfOnly — kept in one place so PlayerTurn's switch reads by name, not magic number.</summary>
    private const int MoveOptionAttack = 0;
    private const int MoveOptionCharge = 1;
    private const int MoveOptionHeal = 2;
    private const int MoveOptionRegen = 3;
    private const int MoveOptionCapture = 4;

    /// <summary>
    /// Each alive player participant picks a move via the HUD's Sonny 2-style radial placeholders
    /// (2026-08-05/06, user-directed — see DECISIONS.md -> [Combat]): click-and-drag the "A"
    /// (Attack) placeholder onto an ENEMY to select the move and target in one gesture, or
    /// click-and-drag "C" (Charge), "H" (Heal), or "R" (Regen) onto the CASTER's OWN creature —
    /// all three are solo/self-only skills, so the only valid drop target is the attacker itself
    /// (BattleHUDController.ShowMoveSelection's self-only handling). Only one enemy exists per
    /// wild encounter, so Attack's target choice currently just gates the pace of the turn rather
    /// than offering a real choice — the UI structure is real, richer content arrives with the
    /// skill tree framework (Step 4). Stops early if the enemy side is wiped mid-turn (remaining
    /// party members don't get to swing at a dead target).
    ///
    /// Between successive party members' attacks (if more than one is alive), pacing is
    /// automatic — BattleHUDController.ShowTimedMessage, no click. Once every alive party member
    /// has acted, any active Regen statuses tick (heal + countdown, see TickPlayerRegen) before
    /// the single Continue click for this whole turn, which gates the transition into the enemy's
    /// turn (2026-08-05, user-directed — see DECISIONS.md -> [Combat]).
    /// </summary>
    private IEnumerator PlayerTurn()
    {
        foreach (BattleParticipant attacker in _state.PlayerSide)
        {
            if (!attacker.IsAlive) continue;

            List<BattleParticipant> aliveEnemies = _state.EnemySide.FindAll(e => e.IsAlive);
            if (aliveEnemies.Count == 0) yield break; // enemy side already wiped earlier this turn

            _playerActionChosen = false;
            int chosenOptionIndex = -1;
            int attackerSlotIndex = _state.PlayerSide.IndexOf(attacker);
            BattleHUDController.Instance.ShowMoveSelection(attackerSlotIndex, attacker, aliveEnemies,
                (optionIndex, chosenTarget) =>
                {
                    chosenOptionIndex = optionIndex;
                    _pendingTarget = chosenTarget; // only meaningful for Attack; self-only moves ignore it
                    _playerActionChosen = true;
                });

            yield return new WaitUntil(() => _playerActionChosen);

            // "C" (Charge) — no attack, no timed input, just restores Aura and ends this
            // attacker's action (2026-08-06, user-directed — see DECISIONS.md -> [Combat]).
            if (chosenOptionIndex == MoveOptionCharge)
            {
                attacker.RestoreAura(BattleConfig.ChargeAuraRestore);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} charges, restoring Aura!");
                AddBurstFill(attacker, attackerSlotIndex, BattleConfig.BurstFillPerSkillUse);

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} charges!", BattleConfig.AutoMessageDurationSeconds));
                continue;
            }

            // "H" (Heal) — instant Aura-for-HP trade, no timed input (2026-08-06, user-directed —
            // see DECISIONS.md -> [Combat]: "the heal should cost 6 aura and heals 4 HP").
            if (chosenOptionIndex == MoveOptionHeal)
            {
                attacker.SpendAura(BattleConfig.HealAuraCost);
                attacker.Heal(BattleConfig.HealAmount);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} heals {BattleConfig.HealAmount} HP!");
                AddBurstFill(attacker, attackerSlotIndex, BattleConfig.BurstFillPerSkillUse);

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} uses H!", BattleConfig.AutoMessageDurationSeconds));
                continue;
            }

            // "R" (Regen) — spends Aura to apply an over-time heal, ticking at the END of the
            // player's turn for BattleConfig.RegenDurationTurns turns (2026-08-06, user-directed
            // — see DECISIONS.md -> [Combat]: "costs 8 aura but heals 2 HP at the end of the
            // players turn for 4 turns"). No immediate HP change on cast — SetRegenStatus shows
            // the status icon right away with the full countdown so the player sees it took
            // effect, but the first heal tick happens in TickPlayerRegen below, same as every
            // subsequent turn's tick.
            if (chosenOptionIndex == MoveOptionRegen)
            {
                attacker.SpendAura(BattleConfig.RegenAuraCost);
                attacker.ApplyRegen(BattleConfig.RegenHealPerTurn, BattleConfig.RegenDurationTurns);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                BattleHUDController.Instance.SetRegenStatus(attackerSlotIndex, attacker.RegenTurnsRemaining);
                // Battle log spells it out ("Aura Regen") rather than the bare orb letter
                // (2026-08-06, user-directed) — the on-stage announcement below keeps the short
                // "R" form, matching the orb the player just pressed.
                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} uses Aura Regen!");
                AddBurstFill(attacker, attackerSlotIndex, BattleConfig.BurstFillPerSkillUse);

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} uses R!", BattleConfig.AutoMessageDurationSeconds));
                continue;
            }

            // "K" (Capture) — attempts CaptureSystem's placeholder chance-roll against the
            // targeted enemy (2026-08-06, wiring CaptureSystem into the live loop for the Phase 3
            // Gate playtest — see DECISIONS.md -> [Combat]). Targets the enemy, same as Attack
            // (MoveOptionIsSelfOnly = false for this option). No timed input, no Aura cost — no
            // capture-item system exists yet (CLAUDE.md: "Economy and items (§22 pending)"), so
            // this is a free attempt rather than inventing a cost mechanism that wasn't asked for.
            if (chosenOptionIndex == MoveOptionCapture)
            {
                bool captured = CaptureSystem.AttemptCapture(_pendingTarget.RuntimeData, _pendingTarget.CurrentHP, _pendingTarget.MaxHP);

                if (captured)
                {
                    PartySystem.Instance?.AddToParty(_pendingTarget.RuntimeData);
                    BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} captured {_pendingTarget.DisplayName}!");

                    yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                        $"{_pendingTarget.DisplayName} was captured!", BattleConfig.AutoMessageDurationSeconds));

                    // Single-enemy battles only (see class doc comment) — capturing the only
                    // enemy IS winning. Ends the battle immediately rather than relying on
                    // TryEndBattle's HP-based BattleEngine.CheckOutcome, which has no way to
                    // detect a capture (the enemy's HP never changed).
                    _battleEndedEarly = true;
                    EndBattle(BattleOutcome.Won);
                    yield break;
                }

                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName}'s capture attempt failed!");

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    "Capture failed!", BattleConfig.AutoMessageDurationSeconds));
                continue;
            }

            // Aura cost (2026-08-05, user-directed — see DECISIONS.md -> [Combat]: "make them
            // cost some aura"). Both placeholder attacks cost the same for now; spending never
            // blocks the attack, it just floors at 0 Aura (BattleParticipant.SpendAura).
            attacker.SpendAura(BattleConfig.AttackAuraCost);

            // Offensive action command (Combat_Directive Part 4): a successful timed press boosts
            // this attack's damage. Ring tolerance scales with the attacker's own Instinct + bond.
            float toleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.OffenseToleranceHalfWidth, TimedInputConfig.OffenseBaseWindowPercent,
                attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);
            yield return StartCoroutine(BattleHUDController.Instance.RunTimedInput(
                $"YOUR ATTACK — {attacker.DisplayName}", toleranceHalfWidth, TimedInputConfig.MarkerSweepDuration));
            float attackMultiplier = BattleHUDController.Instance.LastTimedInputSuccess
                ? TimedInputConfig.SuccessDamageMultiplier
                : 1f;

            // Real formula (Step 3): (AttackerStat / DefenderStat) x skillPower x
            // primalTypeMultiplier. Basic Attack is treated as Physical (Force/Guard) — real
            // skill categories arrive with the skill tree framework (Step 4).
            int baseDamage = DamageCalculator.ComputeDamage(attacker, _pendingTarget, _typeChart, DamageCategory.Physical, DamageCalculator.BasicAttackPower);
            float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, _pendingTarget, _typeChart);

            BattleEngine.QueueBasicAttack(_state, attacker, _pendingTarget, attackMultiplier, baseDamage);
            List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);
            BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
            LogResults(results, typeMultiplier, BattleHUDController.Instance.LastTimedInputSuccess);

            // "Skill use" fill always applies; a successful offense timing adds the extra "timed
            // input" fill on top — both are GDD §9.3's locked fill sources.
            float attackBurstFill = BattleConfig.BurstFillPerSkillUse
                + (BattleHUDController.Instance.LastTimedInputSuccess ? BattleConfig.BurstFillPerTimedInputSuccess : 0f);
            AddBurstFill(attacker, attackerSlotIndex, attackBurstFill);

            yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                $"{attacker.DisplayName} attacks!", BattleConfig.AutoMessageDurationSeconds));
        }

        // Regen and Evolution Burst both tick once per player turn, for EVERY alive party member
        // (not just whoever acted this turn) — see TickPlayerRegen/TickPlayerBurst.
        TickPlayerRegen();
        TickPlayerBurst();

        // Turn transition — used to gate on a Continue button; now just a delay so the switch
        // still reads clearly, no click required (2026-08-06, user-directed — see DECISIONS.md ->
        // [Combat]: "the continue between the [turns] might not be needed anymore just a delay").
        yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
            "Enemy's turn...", BattleConfig.AutoMessageDurationSeconds));
    }

    /// <summary>
    /// Heals + counts down every alive player-side participant's active Regen status by one turn
    /// (2026-08-06, user-directed — see DECISIONS.md -> [Combat]: "heals 2 HP at the end of the
    /// players turn for 4 turns"). Called once at the end of PlayerTurn, after every party member
    /// has acted — a status cast THIS turn still gets its first tick immediately (same as every
    /// later turn's tick), rather than waiting a full extra turn before its first heal. No timed
    /// message per tick — this stays a quick batched update, not another paced beat, so multiple
    /// active Regens don't stack up extra Continue-less waits.
    /// </summary>
    private void TickPlayerRegen()
    {
        bool anyTicked = false;

        foreach (BattleParticipant p in _state.PlayerSide)
        {
            if (!p.IsAlive || p.RegenTurnsRemaining <= 0) continue;

            int slotIndex = _state.PlayerSide.IndexOf(p);
            int healed = p.TickRegen();
            anyTicked = true;

            if (healed > 0) BattleHUDController.Instance.AppendBattleLog($"{p.DisplayName} regenerates {healed} HP!");
            BattleHUDController.Instance.SetRegenStatus(slotIndex, p.RegenTurnsRemaining);
        }

        if (anyTicked) BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
    }

    /// <summary>
    /// Adds Evolution Burst gauge fill for a "skill use"/"timed input"/"taking a hit" (GDD §9.3's
    /// three locked fill sources) and refreshes the visible gauge bar (2026-08-06 — see
    /// DECISIONS.md -> [Combat]). Does NOT trigger the burst itself — that's now a deliberate
    /// player click on the bar once it's full (HandleBurstBarClicked/ActivateReady), not an
    /// automatic check on every fill ("instead of auto triggering, please make it so it becomes
    /// an activatable option... the activation can be on the bar itself").
    /// </summary>
    private void AddBurstFill(BattleParticipant participant, int slotIndex, float fillAmount)
    {
        EvolutionBurstSystem.AddFill(participant.BurstGauge, fillAmount);
        bool ready = !participant.BurstGauge.IsActive && participant.BurstGauge.FillPercent >= EvolutionBurstSystem.TriggerThreshold;
        BattleHUDController.Instance.SetBurstFillBar(slotIndex, participant.BurstGauge.FillPercent, ready);
    }

    /// <summary>
    /// Counts down every alive player-side participant's active Evolution Burst by one turn,
    /// logging + updating the status icon when one expires (2026-08-06 — see DECISIONS.md ->
    /// [Combat]). EvolutionBurstSystem.TickTurn is already a no-op while inactive, so calling this
    /// for every player-side participant every turn (not just those with an active burst) is
    /// harmless. Called alongside TickPlayerRegen, same end-of-turn timing.
    /// </summary>
    private void TickPlayerBurst()
    {
        foreach (BattleParticipant p in _state.PlayerSide)
        {
            if (!p.IsAlive) continue;

            bool wasActive = p.BurstGauge.IsActive;
            EvolutionBurstSystem.TickTurn(p.BurstGauge);

            if (wasActive && !p.BurstGauge.IsActive)
            {
                int slotIndex = _state.PlayerSide.IndexOf(p);
                BattleHUDController.Instance.SetBurstStatus(slotIndex, 0);
                BattleHUDController.Instance.AppendBattleLog($"{p.DisplayName}'s Evolution Burst fades.");
            }
            else if (p.BurstGauge.IsActive)
            {
                int slotIndex = _state.PlayerSide.IndexOf(p);
                BattleHUDController.Instance.SetBurstStatus(slotIndex, p.BurstGauge.RemainingDurationTurns);
            }
        }
    }

    /// <summary>
    /// Each alive enemy attacks a random alive player-side target, resolving immediately so only
    /// that target takes damage. Stops early if the player side is wiped mid-turn. Every beat
    /// (attack announcement, resolved result, counter-attack) auto-paces via
    /// BattleHUDController.ShowTimedMessage instead of gating on a click — the enemy's whole turn
    /// plays out on its own once the player has clicked Continue at the end of their own turn
    /// (2026-08-05, user-directed — see DECISIONS.md -> [Combat]).
    ///
    /// Defense is full-avoidance Dodge/Parry (Combat_Directive Part 4, Expedition 33-inspired —
    /// see DECISIONS.md -> [Combat]), not a damage-reduction multiplier, and is a single LIVE
    /// click: BattleHUDController.RunDefenseTimedInput shows a converging ring — a white marker
    /// ring shrinks past a fixed target ring — above the defending creature; left-click anywhere
    /// to attempt Dodge (succeeds within a wider ratio tolerance), right-click anywhere to attempt
    /// Parry (succeeds within a tighter tolerance), no menu. This is the one real interactive
    /// moment in the enemy's turn; success fully avoids the hit (0 damage multiplier), failure
    /// takes the full hit, same as a missed offensive timing — no extra penalty for attempting the
    /// harder Parry and missing.
    /// </summary>
    private IEnumerator EnemyTurn()
    {
        foreach (BattleParticipant attacker in _state.EnemySide)
        {
            if (!attacker.IsAlive) continue;

            List<BattleParticipant> aliveTargets = _state.PlayerSide.FindAll(p => p.IsAlive);
            if (aliveTargets.Count == 0) yield break;

            BattleParticipant target = aliveTargets[Random.Range(0, aliveTargets.Count)];
            int targetSlotIndex = _state.PlayerSide.IndexOf(target);

            yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                $"{attacker.DisplayName} is attacking!", BattleConfig.AutoMessageDurationSeconds));

            // Both tolerances scale off the DEFENDER's own Instinct + bond, from their respective
            // bases (Dodge wide/easy, Parry narrow/hard).
            float dodgeToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
                target.RuntimeData.EffectiveStat(StatType.Instinct), target.RuntimeData.bondPercent);
            float parryToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
                target.RuntimeData.EffectiveStat(StatType.Instinct), target.RuntimeData.bondPercent);
            yield return StartCoroutine(BattleHUDController.Instance.RunDefenseTimedInput(
                targetSlotIndex, $"DEFEND — {attacker.DisplayName}! Left-Click Dodge · Right-Click Parry",
                dodgeToleranceHalfWidth, parryToleranceHalfWidth, TimedInputConfig.MarkerSweepDuration));

            BattleHUDController.DefenseOutcome outcome = BattleHUDController.Instance.LastDefenseOutcome;
            bool defended = outcome != BattleHUDController.DefenseOutcome.Miss;
            bool isParry = outcome == BattleHUDController.DefenseOutcome.Parry;
            bool wasPerfect = BattleHUDController.Instance.LastDefenseWasPerfect;
            float defenseMultiplier = defended ? 0f : 1f;

            int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, DamageCategory.Physical, DamageCalculator.BasicAttackPower);
            float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

            BattleEngine.QueueBasicAttack(_state, attacker, target, defenseMultiplier, baseDamage);
            List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);

            // Perfect Dodge/Parry reward (2026-08-05, user-directed — see DECISIONS.md ->
            // [Combat]: "Perfect dodges and parrys restore aura"), on top of avoiding the hit
            // (and, for Parry, the counter-attack below).
            if (defended && wasPerfect) target.RestoreAura(BattleConfig.PerfectDefenseAuraRestore);

            BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
            LogDefenseResult(results, attacker, target, typeMultiplier, defended, isParry);
            if (defended && wasPerfect) BattleHUDController.Instance.AppendBattleLog($"{target.DisplayName} restores Aura!");

            // "Taking hits" (GDD §9.3's third Evolution Burst fill source) — only when the hit
            // actually landed (defended means 0 damage was applied, so a full Dodge/Parry
            // shouldn't count as "taking a hit").
            if (!defended) AddBurstFill(target, targetSlotIndex, BattleConfig.BurstFillPerHitTaken);

            yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                defended ? $"{target.DisplayName} defended!" : $"{target.DisplayName} was hit!",
                BattleConfig.AutoMessageDurationSeconds));

            // Parry's reward half: a successful Parry triggers an automatic counter-attack against
            // the now-vulnerable attacker. No timing check on the counter — it's a bonus for
            // landing the harder input, not another QTE.
            if (defended && isParry && attacker.IsAlive)
            {
                int counterDamage = DamageCalculator.ComputeDamage(target, attacker, _typeChart, DamageCategory.Physical, DamageCalculator.BasicAttackPower);
                float counterTypeMultiplier = DamageCalculator.ComputeTypeMultiplier(target, attacker, _typeChart);

                BattleEngine.QueueBasicAttack(_state, target, attacker, damageMultiplier: 1f, counterDamage);
                List<BattleActionResult> counterResults = BattleEngine.ResolveQueuedActions(_state);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                LogResults(counterResults, counterTypeMultiplier, timedInputSuccess: false);

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{target.DisplayName} counter-attacks!", BattleConfig.AutoMessageDurationSeconds));
            }
        }

        BattleHUDController.Instance.HideMoveSelection();
    }

    /// <summary>Appends one battle log line per resolved offensive action — normally exactly one, since only a single attack is ever queued per call site.</summary>
    private static void LogResults(List<BattleActionResult> results, float typeMultiplier, bool timedInputSuccess)
    {
        foreach (BattleActionResult result in results)
        {
            string line = BattleLogFormatter.FormatAttack(result.Attacker, result.Target, result.DamageApplied, typeMultiplier, timedInputSuccess);
            BattleHUDController.Instance.AppendBattleLog(line);
        }
    }

    /// <summary>Appends one battle log line per resolved defended action, via FormatDefenseOutcome instead of FormatAttack.</summary>
    private static void LogDefenseResult(List<BattleActionResult> results, BattleParticipant attacker, BattleParticipant target, float typeMultiplier, bool defended, bool isParry)
    {
        foreach (BattleActionResult result in results)
        {
            string line = BattleLogFormatter.FormatDefenseOutcome(result.Attacker, result.Target, result.DamageApplied, typeMultiplier, defended, isParry);
            BattleHUDController.Instance.AppendBattleLog(line);
        }
    }

    /// <summary>Checks the outcome and ends the battle if it's no longer InProgress. Returns whether it ended.</summary>
    private bool TryEndBattle()
    {
        BattleOutcome outcome = BattleEngine.CheckOutcome(_state);
        if (outcome == BattleOutcome.InProgress) return false;

        EndBattle(outcome);
        return true;
    }

    private void EndBattle(BattleOutcome outcome)
    {
        var result = new BattleResult(outcome == BattleOutcome.Won, _state.PlayerSide, _state.EnemySide);

        if (outcome == BattleOutcome.Won) EventBus.Raise_BattleWon(result);
        else EventBus.Raise_BattleLost(result);

        BattleHUDController.Instance.Hide();
        if (_overworldCamera != null)
        {
            _overworldCamera.cullingMask = _overworldCullingMask;
            _overworldCamera.clearFlags = _overworldClearFlags;
            _overworldCamera.backgroundColor = _overworldBackgroundColor;
        }
        PartySystem.Instance?.ActiveCompanionAI?.SetPaused(false);
        BattleTransition.CompleteBattle(result);

        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}
