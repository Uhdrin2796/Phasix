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
/// BattleState, then hands all turn-resolution rules to the static BattleEngine.
///
/// 2026-08-10 close-out pass (see DECISIONS.md -> [Combat]): enemy target/skill selection now
/// runs through EnemyAI's weighted heuristics instead of pure Random.Range target choice with a
/// single hardcoded basic attack — enemies use their actual equipped skills (seeded via
/// WildSpawnSystem.SeedInitialSkills) when a SkillDatabase is assigned, falling back to today's
/// exact basic-attack behavior otherwise. This is a heuristic upgrade only, not the real AI
/// decision-making framework Combat_Directive_v0_1_0.md flags as pending design (GDD §18.6) — see
/// EnemyAI.cs's class doc comment.
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
/// "ApplyBurstEffects" is explicitly undesigned in the GDD.
///
/// 2026-08 session (see DECISIONS.md -> [Combat]): user explicitly overrode the prior "wait for
/// real skill content" stance and had ComboEngine/StatusEffectCatalog/ChainResultCatalog/
/// MasteryBonusCatalog wired into live play via a placeholder skill-selection UI (the
/// BattleHUDController skill ring, hours 4-10) — see ResolveSkillAction/EvaluateCombosAndLog/
/// EvaluateChainAndMastery below, and PlaceholderSkillResolver for how the 36 generic placeholder
/// skills get mechanically resolved without inventing balance content. Chain/Mastery are
/// detection + log only this pass, not their full numeric effects (a separately-scoped follow-up).
/// AuraStatAllocationSystem remains a post-battle progression system (see the Aura Allocation
/// screen shown from EndBattle on a Won outcome), not a mid-battle mechanic.
///
/// 2026-08-10 follow-up (see DECISIONS.md -> [Combat]): the old pre-battle Flee/Engage choice
/// (EncounterPromptController) is retired — WildEncounterCreature now auto-engages on contact.
/// Fleeing moved INTO the battle itself: a Flee button opposite End Turn, ~80% success
/// (BattleConfig.FleeSuccessChance) rolled once per click in PlayerTurn. Success ends the battle
/// early via EndBattle(BattleOutcome.Fled) — same manual-outcome pattern Capture already uses for
/// Won; failure still consumes the whole turn, same as every other single-beat move in this file.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("Combat Data")]
    [Tooltip("Assign Assets/Data/TypeCharts/PrimalTypeChart.asset. If left empty, damage falls back to a neutral 1.0x type multiplier instead of crashing.")]
    [SerializeField] private PrimalTypeChart _typeChart;

    [Tooltip("Assign the project's SkillDatabase asset — resolves equipped skill GUIDs to real SkillData for the skill ring (2026-08 session, see DECISIONS.md -> [Combat]). If left empty, the skill ring simply shows no equipped skills.")]
    [SerializeField] private SkillDatabase _skillDatabase;

    private BattleState _state;
    private bool _playerActionChosen;
    private BattleParticipant _pendingTarget;
    private SkillData _pendingSkill; // always non-null once a move is confirmed — every orb (built-in move or tree skill) is real SkillData now, see BuiltInMoveType
    private bool _battleEndedEarly; // set true by a successful Capture — EndBattle already ran, RunBattleLoop must not also call EnemyTurn

    // Running totals for the post-battle summary screen (2026-08 session — replaces the old
    // spend-here-and-now Aura Allocation screen, see DECISIONS.md -> [Combat]). Player-side only —
    // damage the ENEMY deals to the player, or Aura/healing the enemy might hypothetically gain,
    // are not tracked here.
    private int _totalDamageDealt;
    private int _totalHealingDone;

    // Free-choice creature selection (2026-08-06, user-directed — see DECISIONS.md -> [Combat]):
    // set by BattleHUDController's click events, consumed by PlayerTurn's own loop. Plain fields
    // rather than a queue — only the MOST RECENT click matters; rapid double-clicks just resolve
    // to whichever was last when PlayerTurn's WaitUntil next checks.
    private int _pendingCreatureClickSlot = -1;
    private bool _endTurnRequested;
    private bool _backgroundClickRequested;

    // Flee button (2026-08-10, user-directed — opposite side of End Turn, ~80% success rate).
    // Same "just a request flag consumed by PlayerTurn's own loop" pattern as _endTurnRequested.
    private bool _fleeRequested;

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

        // Combo-rule membership doesn't change mid-battle (equipped skills are fixed for the
        // battle's duration) — computed once here rather than re-checked every skill use.
        // 2026-08 session, see DECISIONS.md -> [Combat].
        foreach (BattleParticipant p in playerSide) p.RefreshActiveComboRules(_skillDatabase);

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
        BattleHUDController.Instance.Initialize(playerSide, enemySide, _skillDatabase);
        BattleHUDController.Instance.ClearBattleLog();

        // Evolution Burst activation is a free, click-anytime action on the gauge bar itself
        // (2026-08-06, user-directed — see DECISIONS.md -> [Combat]: "I think the activation can
        // be on the bar itself"), NOT a move-selection-gated option — so it's wired via this
        // event rather than through ShowMoveSelection's per-turn callback. BattleHUDController
        // and BattleManager share BattleScene_Main's lifetime (both die together on scene
        // unload), so no explicit unsubscribe is needed.
        BattleHUDController.Instance.BurstBarClicked += HandleBurstBarClicked;

        // Free-choice creature selection (2026-08-06, user-directed — see DECISIONS.md ->
        // [Combat]): PlayerTurn's own loop consumes these two fields via WaitUntil rather than
        // acting on them directly in the handler — keeps all the actual turn-flow logic in one
        // place (the coroutine) instead of splitting it across event callbacks.
        BattleHUDController.Instance.PlayerCreatureClicked += slot => _pendingCreatureClickSlot = slot;
        BattleHUDController.Instance.EndTurnClicked += () => _endTurnRequested = true;
        BattleHUDController.Instance.FleeClicked += () => _fleeRequested = true;
        BattleHUDController.Instance.StageBackgroundClicked += () => _backgroundClickRequested = true;

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
            if (_battleEndedEarly) yield break; // EndBattle already ran (and was awaited) inside PlayerTurn's Capture branch

            BattleOutcome outcomeAfterPlayerTurn = BattleEngine.CheckOutcome(_state);
            if (outcomeAfterPlayerTurn != BattleOutcome.InProgress)
            {
                yield return StartCoroutine(EndBattle(outcomeAfterPlayerTurn));
                yield break;
            }

            yield return StartCoroutine(EnemyTurn());

            BattleOutcome outcomeAfterEnemyTurn = BattleEngine.CheckOutcome(_state);
            if (outcomeAfterEnemyTurn != BattleOutcome.InProgress)
            {
                yield return StartCoroutine(EndBattle(outcomeAfterEnemyTurn));
                yield break;
            }

            TickAllStatuses();
        }
    }

    /// <summary>
    /// Ticks every active status effect (ChainResultCatalog/MasteryBonusCatalog wiring, 2026-08
    /// session — see DECISIONS.md -> [Combat]) down by one turn, on BOTH sides, once per full
    /// round. Symmetric — the simplest rule, since nothing in the docs asks for asymmetric decay.
    /// </summary>
    private void TickAllStatuses()
    {
        foreach (BattleParticipant p in _state.PlayerSide) p.TickStatuses();
        foreach (BattleParticipant p in _state.EnemySide) p.TickStatuses();
    }

    /// <summary>
    /// Free-choice creature selection (2026-08-06, user-directed — see DECISIONS.md -> [Combat]),
    /// replacing the old strict-turn-order foreach: the player can click ANY of their alive
    /// Phasix, in any order, to open its skill ring — click-and-drag any populated orb onto its
    /// valid target (enemy for Attack/Capture-type moves, the caster's own creature for Charge/
    /// Heal/Regen-type ones — 2026-08 follow-up: every orb, built-in move or tree skill, is now
    /// real SkillData routed through ResolveSkillAction, see BuiltInMoveType). Clicking a
    /// DIFFERENT creature while one's wheel is open (whether it's still choosing a move, or just
    /// being viewed read-only) closes the current one and opens the new one — no move is lost,
    /// since nothing is committed until a drag actually confirms a target. A creature that's
    /// already acted this turn (BattleParticipant.HasActedThisTurn) still opens on click, but
    /// read-only/greyed (ShowMoveSelectionReadOnly) — every current move requires the turn's one
    /// action, so there's nothing to actually pick, but the wheel stays inspectable. The player
    /// ends their own turn explicitly via the dedicated End Turn button (EndTurnClicked) rather
    /// than the turn auto-ending once everyone's acted — matching how the free-choice model
    /// doesn't force every creature to act every round.
    ///
    /// Stops immediately (skipping the end-of-turn ticks/message below) if the enemy side is
    /// wiped mid-turn, checked at the top of every loop iteration — same "no swinging at a dead
    /// target" intent the old foreach had, just re-derived for a player-driven loop instead of a
    /// fixed order.
    /// </summary>
    private IEnumerator PlayerTurn()
    {
        foreach (BattleParticipant p in _state.PlayerSide) p.HasActedThisTurn = false;

        _endTurnRequested = false;
        _fleeRequested = false;
        _pendingCreatureClickSlot = -1;
        _backgroundClickRequested = false;
        BattleHUDController.Instance.SetEndTurnButtonVisible(true);
        BattleHUDController.Instance.SetFleeButtonVisible(true);

        // Auto-open the first living party member's move wheel (2026-08 follow-up — user-directed:
        // "instead of having to click the wheel open, Auto open the phasix that is first in the
        // roster... so its a good indicator that its the players turn"). Sets the SAME field a
        // real click would set, so the loop's normal "activeSlot < 0" branch below opens it through
        // its existing logic — no separate wheel-opening code path to keep in sync.
        int firstAliveSlot = _state.PlayerSide.FindIndex(p => p.IsAlive);
        if (firstAliveSlot >= 0) _pendingCreatureClickSlot = firstAliveSlot;

        int activeSlot = -1; // -1 = no wheel currently open

        while (!_endTurnRequested && !_fleeRequested)
        {
            if (_state.EnemySide.TrueForAll(e => !e.IsAlive)) yield break; // wiped mid-turn — battle is already over, TryEndBattle picks this up right after PlayerTurn returns

            if (activeSlot < 0)
            {
                yield return new WaitUntil(() => _endTurnRequested || _fleeRequested || _pendingCreatureClickSlot >= 0);
                if (_endTurnRequested || _fleeRequested) break;

                // A background click that landed while nothing was selected is a no-op (nothing
                // to close) — clear it here so it can't linger and instantly close the wheel
                // we're about to open below.
                _backgroundClickRequested = false;

                activeSlot = _pendingCreatureClickSlot;
                _pendingCreatureClickSlot = -1;

                BattleParticipant clicked = _state.PlayerSide[activeSlot];
                if (!clicked.IsAlive) { activeSlot = -1; continue; }

                if (clicked.HasActedThisTurn)
                {
                    BattleHUDController.Instance.ShowMoveSelectionReadOnly(activeSlot);
                }
                else
                {
                    _playerActionChosen = false;
                    _pendingSkill = null;
                    List<BattleParticipant> aliveEnemiesForWheel = _state.EnemySide.FindAll(e => e.IsAlive);
                    BattleHUDController.Instance.ShowMoveSelection(activeSlot, clicked, aliveEnemiesForWheel,
                        chosen =>
                        {
                            _pendingSkill = chosen.Skill; // always non-null now — every orb (built-in move or tree skill) is real SkillData, see BuiltInMoveType
                            _pendingTarget = chosen.Target;
                            _playerActionChosen = true;
                        });
                }
            }

            // Wait for whichever comes first: End Turn, a move confirmed (only possible in the
            // functional branch above), the player switching to a DIFFERENT creature (valid from
            // either the functional or read-only branch — "click on another phasix the current
            // phasix orb menu closes, then the new clicked phasix menu shows"), or a click on the
            // empty stage background (2026-08-06, user-directed: "clicking outside of that should
            // hide any open skill wheels").
            int openedSlot = activeSlot;
            yield return new WaitUntil(() =>
                _endTurnRequested ||
                _fleeRequested ||
                _playerActionChosen ||
                _backgroundClickRequested ||
                (_pendingCreatureClickSlot >= 0 && _pendingCreatureClickSlot != openedSlot));

            if (_endTurnRequested || _fleeRequested) break;

            if (_backgroundClickRequested)
            {
                _backgroundClickRequested = false;
                BattleHUDController.Instance.HideMoveSelection();
                activeSlot = -1;
                continue; // nothing to open — next iteration's "activeSlot < 0" branch just waits again
            }

            if (_pendingCreatureClickSlot >= 0 && _pendingCreatureClickSlot != openedSlot)
            {
                BattleHUDController.Instance.HideMoveSelection();
                activeSlot = -1;
                continue; // next iteration's "activeSlot < 0" branch picks up the pending click
            }

            if (!_playerActionChosen) continue; // read-only wheel, nothing else happened yet — keep waiting

            BattleParticipant attacker = _state.PlayerSide[activeSlot];
            int attackerSlotIndex = activeSlot;
            _playerActionChosen = false;
            activeSlot = -1; // the wheel this action came from is already hidden by BeginDragForSkill's own confirm/drag flow
            attacker.HasActedThisTurn = true;

            // Every orb — built-in move or tree skill — is real SkillData now (2026-08 follow-up:
            // Attack/Charge/Heal/Regen/Capture became equippable Standard-tree SkillData, see
            // BuiltInMoveType), so _pendingSkill is always non-null here and ResolveSkillAction is
            // the single dispatch point for every move a player can make. If capture succeeds,
            // ResolveSkillAction/EndBattle sets _battleEndedEarly and this loop stops.
            yield return StartCoroutine(ResolveSkillAction(attacker, attackerSlotIndex, _pendingSkill, _pendingTarget));
            if (_battleEndedEarly) yield break;
        }

        BattleHUDController.Instance.SetEndTurnButtonVisible(false);
        BattleHUDController.Instance.SetFleeButtonVisible(false);
        BattleHUDController.Instance.HideMoveSelection();

        // Flee resolution (2026-08-10, user-directed — ~80% success rate, BattleConfig.
        // FleeSuccessChance). A successful attempt ends the battle immediately, same manual-
        // outcome pattern the Capture built-in move already uses (_battleEndedEarly + EndBattle).
        // A FAILED attempt still consumes the whole turn — same "uses the turn regardless of
        // outcome" convention as every other single-beat move — so it just falls through to the
        // normal end-of-turn ticks/enemy-turn transition below, exactly as if End Turn was pressed.
        if (_fleeRequested)
        {
            bool fleeSuccess = Random.value < BattleConfig.FleeSuccessChance;
            yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                fleeSuccess ? "Got away safely!" : "Couldn't get away!", BattleConfig.AutoMessageDurationSeconds));

            if (fleeSuccess)
            {
                BattleHUDController.Instance.AppendBattleLog("Fled from battle!");
                _battleEndedEarly = true;
                yield return StartCoroutine(EndBattle(BattleOutcome.Fled));
                yield break;
            }

            BattleHUDController.Instance.AppendBattleLog("Failed to flee!");
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
            _totalHealingDone += healed;

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

    /// <summary>Accumulates player-side damage dealt this battle, for the post-battle summary screen (2026-08 session, see DECISIONS.md -> [Combat]). Only counts results where the ATTACKER is player-side — the Parry counter-attack (attacker = the defending player creature) counts; ordinary enemy attacks against the player don't.</summary>
    private void AccumulateDamageDealt(List<BattleActionResult> results)
    {
        foreach (BattleActionResult result in results)
        {
            if (result.Attacker.IsPlayerSide) _totalDamageDealt += result.DamageApplied;
        }
    }

    /// <summary>Primal type used for hit-VFX tinting (BattleHUDController.PlayHitVfx) — falls back to Fire when speciesData isn't set (edge case, see PhasixRuntimeData.speciesData's own doc comment; matches BattleAudioVfxHooks.OnDamageTaken's identical defensive fallback).</summary>
    private static PrimalType GetPrimalTypeOrDefault(BattleParticipant participant)
        => participant.RuntimeData.speciesData != null ? participant.RuntimeData.speciesData.PrimalType : PrimalType.Fire;

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
    /// Resolves a skill-ring drag — every orb, built-in move or tree skill (2026-08 session —
    /// Combo/Status/Chain/Mastery wiring; 2026-08 follow-up — built-ins became real, equippable
    /// SkillData, see BuiltInMoveType; see DECISIONS.md -> [Combat] for both). `target` is already
    /// correctly resolved to either the enemy or the caster's own creature by
    /// BattleHUDController's drag-drop hit-test (it uses the same IsBuiltInMoveSelfTargeted/
    /// PlaceholderSkillResolver.Resolve(skill).SelfTargeted split this method's own dispatch
    /// depends on), so no further self-vs-enemy branching is needed here — `target` IS the
    /// recipient either way.
    ///
    /// RecordSkillTreeUse/RecordSkillUse run BEFORE the built-in-vs-tree-skill dispatch, uniformly
    /// for every skill — this is what makes a built-in move correctly interrupt an in-progress
    /// RepeatSameSkill streak the same way any other different skill would (2026-08 follow-up —
    /// user: "the other skills dont reset the counter on C1... Do all the other skills not count
    /// as normal skills?"). A built-in move (BuiltInMove != None) then short-circuits to
    /// ResolveBuiltInMove — its own dedicated mechanics, unchanged from before this refactor
    /// beyond being triggered by skill identity instead of a fixed wheel-position index — and
    /// returns without running PlaceholderSkillResolver or the combo/chain/mastery tail below,
    /// exactly as approved in the implementation plan for this change.
    /// </summary>
    private IEnumerator ResolveSkillAction(BattleParticipant attacker, int attackerSlotIndex, SkillData skill, BattleParticipant target)
    {
        attacker.RecordSkillTreeUse(skill.TreeType);
        attacker.RecordSkillUse(skill);
        EventBus.Raise_SkillUsed(attacker.RuntimeData, skill);

        if (skill.BuiltInMove != BuiltInMoveType.None)
        {
            yield return StartCoroutine(ResolveBuiltInMove(attacker, attackerSlotIndex, skill.BuiltInMove, target));
            yield break;
        }

        PlaceholderSkillResolver.SkillResolution resolution = PlaceholderSkillResolver.Resolve(skill);
        attacker.SpendAura(BattleConfig.PlaceholderSkillAuraCost);

        bool timedInputHappened = false;

        if (resolution.DealsDamage)
        {
            float toleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.OffenseToleranceHalfWidth, TimedInputConfig.OffenseBaseWindowPercent,
                attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);

            // Launches the projectile now, concurrently with the ring below — sweepDuration is
            // sized off the projectile's own real travel time so the ring's "perfect" instant
            // lines up with when it visually connects (2026-08-11 timing-sync pass).
            int targetSlotIndex = _state.EnemySide.IndexOf(target);
            float sweepDuration = BattleHUDController.Instance.LaunchSyncedProjectile(
                attackerSlotIndex, true, targetSlotIndex, false, GetPrimalTypeOrDefault(attacker), holdForOutcome: false);
            yield return StartCoroutine(BattleHUDController.Instance.RunTimedInput(
                $"{skill.SkillName} — {attacker.DisplayName}", toleranceHalfWidth, sweepDuration));

            bool timedSuccess = BattleHUDController.Instance.LastTimedInputSuccess;
            bool timedPerfect = BattleHUDController.Instance.LastTimedInputWasPerfect;
            // TimedInputStreak (C2) specifically tracks PERFECT hits, not merely successful ones
            // — user-directed: "works with any... skill that gets perfect, after a miss it
            // rests." See BattleParticipant.RecentTimedInputPerfects.
            attacker.RecordTimedInputPerfect(timedPerfect);
            timedInputHappened = true;
            if (timedSuccess) EventBus.Raise_TimedInputSuccess(attacker.RuntimeData);

            float attackMultiplier = timedSuccess ? TimedInputConfig.SuccessDamageMultiplier : 1f;
            int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, resolution.Category, BattleConfig.PlaceholderSkillPower);
            float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

            BattleEngine.QueueBasicAttack(_state, attacker, target, attackMultiplier, baseDamage);
            List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);
            AccumulateDamageDealt(results);
            BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);

            foreach (BattleActionResult result in results)
            {
                string line = BattleLogFormatter.FormatSkillAttack(result.Attacker, result.Target, skill.SkillName, result.DamageApplied, typeMultiplier);
                BattleHUDController.Instance.AppendBattleLog(line);
            }

            float attackBurstFill = BattleConfig.BurstFillPerSkillUse
                + (timedSuccess ? BattleConfig.BurstFillPerTimedInputSuccess : 0f);
            AddBurstFill(attacker, attackerSlotIndex, attackBurstFill);
        }
        else
        {
            StatusEffectType status = resolution.AppliedStatus.Value;
            StatusEffectCatalog.Entry entry = StatusEffectCatalog.Get(status);
            int duration = StatusDurationCalculator.ComputeDuration(entry.MinDurationTurns,
                attacker.RuntimeData.EffectiveStat(StatType.Resonance), target.RuntimeData.EffectiveStat(StatType.Resolve), entry.IsPositive);

            target.ApplyStatus(status, duration);
            BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
            BattleHUDController.Instance.AppendBattleLog(BattleLogFormatter.FormatStatusApplied(target, status, duration));

            AddBurstFill(attacker, attackerSlotIndex, BattleConfig.BurstFillPerSkillUse);
        }

        // Combo detection across every rule active for this attacker — log only, no numeric bonus
        // for any rule type (see DECISIONS.md -> [Combat]: neither the GDD nor this session's new
        // RepeatSameSkill/TimedInputStreak mechanics define a mechanical payoff yet).
        EvaluateCombosAndLog(attacker, timedInputHappened);
        RefreshComboCounterBadges(attacker, attackerSlotIndex, skill);

        // Chain/Mastery — detection + log only this pass (full numeric effects are an explicitly
        // flagged, separately-scoped follow-up).
        EvaluateChainAndMastery(attacker, target);

        yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
            $"{attacker.DisplayName} uses {skill.SkillName}!", BattleConfig.AutoMessageDurationSeconds));
    }

    /// <summary>
    /// Runs one of the 5 built-in moves' own dedicated mechanics (2026-08 follow-up — relocated
    /// verbatim from PlayerTurn's old chosenOptionIndex-keyed if/else chain, now triggered by
    /// skill identity via ResolveSkillAction's dispatch instead of a fixed wheel-position index —
    /// see BuiltInMoveType, DECISIONS.md -> [Combat]). No mechanical changes from the pre-refactor
    /// behavior: same Aura costs, same HP/status effects, same battle-log/announcement text, same
    /// early-EndBattle-on-successful-capture path (sets _battleEndedEarly, checked by PlayerTurn
    /// right after this coroutine returns).
    /// </summary>
    private IEnumerator ResolveBuiltInMove(BattleParticipant attacker, int attackerSlotIndex, BuiltInMoveType move, BattleParticipant target)
    {
        switch (move)
        {
            case BuiltInMoveType.Charge:
            {
                // No attack, no timed input, just restores Aura and ends this attacker's action
                // (2026-08-06, user-directed — see DECISIONS.md -> [Combat]).
                attacker.RestoreAura(BattleConfig.ChargeAuraRestore);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} charges, restoring Aura!");
                AddBurstFill(attacker, attackerSlotIndex, BattleConfig.BurstFillPerSkillUse);

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} charges!", BattleConfig.AutoMessageDurationSeconds));
                yield break;
            }

            case BuiltInMoveType.Heal:
            {
                // Instant Aura-for-HP trade, no timed input (2026-08-06, user-directed — see
                // DECISIONS.md -> [Combat]: "the heal should cost 6 aura and heals 4 HP").
                attacker.SpendAura(BattleConfig.HealAuraCost);
                int hpBeforeHeal = attacker.CurrentHP;
                attacker.Heal(BattleConfig.HealAmount);
                _totalHealingDone += attacker.CurrentHP - hpBeforeHeal;
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} heals {BattleConfig.HealAmount} HP!");
                AddBurstFill(attacker, attackerSlotIndex, BattleConfig.BurstFillPerSkillUse);

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} uses H!", BattleConfig.AutoMessageDurationSeconds));
                yield break;
            }

            case BuiltInMoveType.Regen:
            {
                // Spends Aura to apply an over-time heal, ticking at the END of the player's turn
                // for BattleConfig.RegenDurationTurns turns (2026-08-06, user-directed — see
                // DECISIONS.md -> [Combat]: "costs 8 aura but heals 2 HP at the end of the players
                // turn for 4 turns"). No immediate HP change on cast — SetRegenStatus shows the
                // status icon right away with the full countdown so the player sees it took
                // effect, but the first heal tick happens in TickPlayerRegen, same as every
                // subsequent turn's tick.
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
                yield break;
            }

            case BuiltInMoveType.Capture:
            {
                // Attempts CaptureSystem's placeholder chance-roll against the targeted enemy
                // (2026-08-06, wiring CaptureSystem into the live loop for the Phase 3 Gate
                // playtest — see DECISIONS.md -> [Combat]). No timed input, no Aura cost — no
                // capture-item system exists yet (CLAUDE.md: "Economy and items (§22 pending)"),
                // so this is a free attempt rather than inventing a cost mechanism.
                bool captured = CaptureSystem.AttemptCapture(target.RuntimeData, target.CurrentHP, target.MaxHP);

                if (captured)
                {
                    PartySystem.Instance?.AddToParty(target.RuntimeData);
                    BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} captured {target.DisplayName}!");

                    yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                        $"{target.DisplayName} was captured!", BattleConfig.AutoMessageDurationSeconds));

                    // Single-enemy battles only (see class doc comment) — capturing the only
                    // enemy IS winning. Ends the battle immediately rather than relying on
                    // RunBattleLoop's HP-based BattleEngine.CheckOutcome, which has no way to
                    // detect a capture (the enemy's HP never changed).
                    _battleEndedEarly = true;
                    yield return StartCoroutine(EndBattle(BattleOutcome.Won));
                    yield break;
                }

                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName}'s capture attempt failed!");

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    "Capture failed!", BattleConfig.AutoMessageDurationSeconds));
                yield break;
            }

            case BuiltInMoveType.Attack:
            default:
            {
                // Aura cost (2026-08-05, user-directed — see DECISIONS.md -> [Combat]: "make them
                // cost some aura"). Spending never blocks the attack, it just floors at 0 Aura
                // (BattleParticipant.SpendAura).
                attacker.SpendAura(BattleConfig.AttackAuraCost);

                // Offensive action command (Combat_Directive Part 4): a successful timed press
                // boosts this attack's damage. Ring tolerance scales with the attacker's own
                // Instinct + bond.
                float toleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                    TimedInputConfig.OffenseToleranceHalfWidth, TimedInputConfig.OffenseBaseWindowPercent,
                    attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);

                // Launches the projectile now, concurrently with the ring below — see
                // ResolveSkillAction's identical pattern for the full rationale.
                int targetSlotIndex = _state.EnemySide.IndexOf(target);
                float sweepDuration = BattleHUDController.Instance.LaunchSyncedProjectile(
                    attackerSlotIndex, true, targetSlotIndex, false, GetPrimalTypeOrDefault(attacker), holdForOutcome: false);
                yield return StartCoroutine(BattleHUDController.Instance.RunTimedInput(
                    $"YOUR ATTACK — {attacker.DisplayName}", toleranceHalfWidth, sweepDuration));
                float attackMultiplier = BattleHUDController.Instance.LastTimedInputSuccess
                    ? TimedInputConfig.SuccessDamageMultiplier
                    : 1f;
                if (BattleHUDController.Instance.LastTimedInputSuccess) EventBus.Raise_TimedInputSuccess(attacker.RuntimeData);

                // Real formula (Step 3): (AttackerStat / DefenderStat) x skillPower x
                // primalTypeMultiplier. Basic Attack is treated as Physical (Force/Guard) — real
                // skill categories arrive with the skill tree framework (Step 4).
                int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, DamageCategory.Physical, DamageCalculator.BasicAttackPower);
                float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

                BattleEngine.QueueBasicAttack(_state, attacker, target, attackMultiplier, baseDamage);
                List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);
                AccumulateDamageDealt(results);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                LogResults(results, typeMultiplier, BattleHUDController.Instance.LastTimedInputSuccess);

                // "Skill use" fill always applies; a successful offense timing adds the extra
                // "timed input" fill on top — both are GDD §9.3's locked fill sources.
                float attackBurstFill = BattleConfig.BurstFillPerSkillUse
                    + (BattleHUDController.Instance.LastTimedInputSuccess ? BattleConfig.BurstFillPerTimedInputSuccess : 0f);
                AddBurstFill(attacker, attackerSlotIndex, attackBurstFill);

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} attacks!", BattleConfig.AutoMessageDurationSeconds));
                yield break;
            }
        }
    }

    /// <summary>
    /// Checks every combo rule active for this attacker (CrossTreeSequence always; RepeatSameSkill/
    /// TimedInputStreak only if a granting skill is equipped — see BattleParticipant.
    /// ActiveComboRules) and logs any detected tier. includeTimedInputStreak is false for
    /// status-only skills, which never ran a timed input this action — evaluating it anyway would
    /// re-check stale history from an earlier turn's damage skill. 2026-08 session, see
    /// DECISIONS.md -> [Combat].
    /// </summary>
    private void EvaluateCombosAndLog(BattleParticipant attacker, bool includeTimedInputStreak)
    {
        foreach (ComboRuleType rule in attacker.ActiveComboRules)
        {
            if (rule == ComboRuleType.TimedInputStreak && !includeTimedInputStreak) continue;

            ComboTier? tier = rule switch
            {
                ComboRuleType.CrossTreeSequence => ComboEngine.DetectCombo(attacker.RecentSkillTrees),
                ComboRuleType.RepeatSameSkill => ComboRuleEvaluator.EvaluateRepeatSameSkill(attacker.RecentSkillsUsed, FindGrantingSkill(attacker, ComboRuleType.RepeatSameSkill)),
                ComboRuleType.TimedInputStreak => ComboRuleEvaluator.EvaluateTimedInputStreak(attacker.RecentTimedInputPerfects),
                _ => null,
            };

            if (tier.HasValue)
            {
                // TODO: pending design — combo bonus effect. Neither the GDD (cross-tree rule) nor
                // this session's new rules (RepeatSameSkill/TimedInputStreak) define what a combo
                // mechanically DOES yet — detection + log only.
                BattleHUDController.Instance.AppendBattleLog(BattleLogFormatter.FormatComboDetected(attacker, tier.Value, rule));
            }
        }
    }

    /// <summary>
    /// Updates the skill-wheel combo-streak badges for this attacker after a skill use (2026-08
    /// session — user-directed: "counter next to the skill on the skill wheel," see DECISIONS.md
    /// -> [Combat]). Clears all of this creature's badges first, then re-shows whichever are
    /// currently >= 2 — simplest way to guarantee a broken streak's stale badge never lingers,
    /// since the correct target skill can change between calls (e.g. RepeatSameSkill's badge
    /// moves to whichever skill was just used). CrossTreeSequence/RepeatSameSkill badge the
    /// just-used skill itself (they're keyed on skill identity/tree); TimedInputStreak — not tied
    /// to any one skill's identity — badges the passive that grants it instead.
    /// </summary>
    private void RefreshComboCounterBadges(BattleParticipant attacker, int attackerSlotIndex, SkillData justUsedSkill)
    {
        BattleHUDController.Instance.ClearAllSkillComboCounters(attackerSlotIndex);

        foreach (ComboRuleType rule in attacker.ActiveComboRules)
        {
            switch (rule)
            {
                case ComboRuleType.CrossTreeSequence:
                {
                    int length = ComboEngine.GetDistinctTrailingStreakLength(attacker.RecentSkillTrees);
                    if (length >= 2) BattleHUDController.Instance.SetSkillComboCounter(attackerSlotIndex, justUsedSkill, length);
                    break;
                }
                case ComboRuleType.RepeatSameSkill:
                {
                    // Badge the GRANTING skill itself (e.g. "C1"), not necessarily justUsedSkill —
                    // the streak is specifically about repeating that one skill (see
                    // ComboRuleEvaluator.EvaluateRepeatSameSkill's doc comment).
                    SkillData grantingSkill = FindGrantingSkill(attacker, ComboRuleType.RepeatSameSkill);
                    int length = ComboRuleEvaluator.GetRepeatTrailingStreakLength(attacker.RecentSkillsUsed, grantingSkill);
                    if (length >= 2 && grantingSkill != null) BattleHUDController.Instance.SetSkillComboCounter(attackerSlotIndex, grantingSkill, length);
                    break;
                }
                case ComboRuleType.TimedInputStreak:
                {
                    int length = ComboRuleEvaluator.GetTimedInputTrailingStreakLength(attacker.RecentTimedInputPerfects);
                    if (length >= 2)
                    {
                        SkillData grantingSkill = FindGrantingSkill(attacker, ComboRuleType.TimedInputStreak);
                        if (grantingSkill != null) BattleHUDController.Instance.SetSkillComboCounter(attackerSlotIndex, grantingSkill, length);
                    }
                    break;
                }
            }
        }
    }

    /// <summary>Scans attacker's equipped skills for the one that grants `rule` (see SkillData.GrantsComboRule) — used to badge the PASSIVE itself for rules not tied to any one attack skill's identity (TimedInputStreak).</summary>
    private SkillData FindGrantingSkill(BattleParticipant attacker, ComboRuleType rule)
    {
        if (_skillDatabase == null) return null;

        foreach (string guid in attacker.RuntimeData.equippedSkillGuids)
        {
            if (_skillDatabase.TryGetByGuid(guid, out SkillData skill) && skill.GrantsComboRule == rule)
            {
                return skill;
            }
        }
        return null;
    }

    /// <summary>
    /// ChainResultCatalog/MasteryBonusCatalog detection + logging only (2026-08 session, see
    /// DECISIONS.md -> [Combat]) — no numeric gameplay effect applied for either, an explicitly
    /// flagged, separately-scoped follow-up. Chain logs only on a *change* from target's last
    /// logged result; mastery bonuses log once per bonus per battle via
    /// TriggeredMasteryBonusesThisBattle.
    /// </summary>
    private void EvaluateChainAndMastery(BattleParticipant attacker, BattleParticipant target)
    {
        List<StatusEffectType> targetStatuses = target.ActiveStatusTypes;

        if (ChainResultCatalog.TryResolve(targetStatuses, out ChainResultType chain))
        {
            if (target.ActiveChainResult != chain)
            {
                target.ActiveChainResult = chain;
                BattleHUDController.Instance.AppendBattleLog(BattleLogFormatter.FormatChainResultTriggered(target, chain));
            }
        }
        else
        {
            target.ActiveChainResult = null;
        }

        List<StatusEffectType> selfStatuses = attacker.ActiveStatusTypes;
        foreach (MasteryBonusType bonus in MasteryBonusCatalog.EvaluateAll(selfStatuses, targetStatuses))
        {
            if (attacker.TriggeredMasteryBonusesThisBattle.Add(bonus))
            {
                BattleHUDController.Instance.AppendBattleLog(BattleLogFormatter.FormatMasteryBonusTriggered(attacker, bonus));
            }
        }
    }

    /// <summary>
    /// Each alive enemy picks a target (EnemyAI.ChooseTarget — weighted toward lower-HP%/type-
    /// effective targets, replacing the old pure Random.Range choice) and a move (EnemyAI.
    /// ChooseSkill — resolves the attacker's actual equipped skills when a SkillDatabase is
    /// assigned, bucketed into Damage/SelfSupport/Debuff), then dispatches to the matching
    /// Resolve* coroutine below. Stops early if the player side is wiped mid-turn. Every beat
    /// auto-paces via BattleHUDController.ShowTimedMessage instead of gating on a click — the
    /// enemy's whole turn plays out on its own once the player has clicked Continue at the end of
    /// their own turn (2026-08-05, user-directed — see DECISIONS.md -> [Combat]).
    /// </summary>
    private IEnumerator EnemyTurn()
    {
        foreach (BattleParticipant attacker in _state.EnemySide)
        {
            if (!attacker.IsAlive) continue;

            List<BattleParticipant> aliveTargets = _state.PlayerSide.FindAll(p => p.IsAlive);
            if (aliveTargets.Count == 0) yield break;

            BattleParticipant target = EnemyAI.ChooseTarget(attacker, aliveTargets, _typeChart);
            int targetSlotIndex = _state.PlayerSide.IndexOf(target);

            SkillData chosenSkill = EnemyAI.ChooseSkill(attacker, _skillDatabase, out EnemyAI.EnemyMoveIntent intent);

            switch (intent)
            {
                case EnemyAI.EnemyMoveIntent.SelfSupport when chosenSkill != null:
                    yield return StartCoroutine(ResolveEnemySelfSupportAction(attacker, chosenSkill));
                    break;

                case EnemyAI.EnemyMoveIntent.Debuff when chosenSkill != null:
                    yield return StartCoroutine(ResolveEnemyDebuffAction(attacker, target, chosenSkill));
                    break;

                default:
                    yield return StartCoroutine(ResolveEnemyDamageAction(attacker, target, targetSlotIndex, chosenSkill));
                    break;
            }
        }

        BattleHUDController.Instance.HideMoveSelection();
    }

    /// <summary>
    /// Resolves an enemy's damaging action against target — a full-avoidance Dodge/Parry defense
    /// (Combat_Directive Part 4, Expedition 33-inspired — see DECISIONS.md -> [Combat]) followed
    /// by the hit itself, same flow regardless of which move produced it. skillOrNull null OR a
    /// BuiltInMoveType.Attack skill reproduces the exact pre-EnemyAI behavior (Physical/
    /// DamageCalculator.BasicAttackPower, generic "is attacking!" announcement, no Aura spend) —
    /// this is the critical backward-compat path. A named, non-built-in tree skill mirrors
    /// ResolveSkillAction's player-side damage-skill branch (PlaceholderSkillResolver's category,
    /// BattleConfig.PlaceholderSkillPower/PlaceholderSkillAuraCost, named announcement).
    /// </summary>
    private IEnumerator ResolveEnemyDamageAction(BattleParticipant attacker, BattleParticipant target, int targetSlotIndex, SkillData skillOrNull)
    {
        bool isNamedTreeSkill = skillOrNull != null && skillOrNull.BuiltInMove == BuiltInMoveType.None;
        if (isNamedTreeSkill) attacker.SpendAura(BattleConfig.PlaceholderSkillAuraCost);

        yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
            isNamedTreeSkill ? $"{attacker.DisplayName} uses {skillOrNull.SkillName}!" : $"{attacker.DisplayName} is attacking!",
            BattleConfig.AutoMessageDurationSeconds));

        // Both tolerances scale off the DEFENDER's own Instinct + bond, from their respective
        // bases (Dodge wide/easy, Parry narrow/hard).
        float dodgeToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
            TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
            target.RuntimeData.EffectiveStat(StatType.Instinct), target.RuntimeData.bondPercent);
        float parryToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
            TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
            target.RuntimeData.EffectiveStat(StatType.Instinct), target.RuntimeData.bondPercent);
        // Launches the incoming projectile now, concurrently with the ring below, and pauses it
        // at the player's edge on arrival (holdForOutcome: true) — whether it actually lands isn't
        // known until the ring resolves, since the player can click (and therefore Dodge/Parry) at
        // any point in the sweep, including after the projectile's own scheduled arrival instant.
        // Resolved into a hit-flash/vanish/deflect below once the real outcome is known
        // (2026-08-11 timing-sync pass).
        float sweepDuration = BattleHUDController.Instance.LaunchSyncedProjectile(
            _state.EnemySide.IndexOf(attacker), false, targetSlotIndex, true, GetPrimalTypeOrDefault(attacker), holdForOutcome: true);
        yield return StartCoroutine(BattleHUDController.Instance.RunDefenseTimedInput(
            targetSlotIndex, $"DEFEND — {attacker.DisplayName}! Left-Click Dodge · Right-Click Parry",
            dodgeToleranceHalfWidth, parryToleranceHalfWidth, sweepDuration));

        BattleHUDController.DefenseOutcome outcome = BattleHUDController.Instance.LastDefenseOutcome;
        bool defended = outcome != BattleHUDController.DefenseOutcome.Miss;
        bool isParry = outcome == BattleHUDController.DefenseOutcome.Parry;
        bool wasPerfect = BattleHUDController.Instance.LastDefenseWasPerfect;
        float defenseMultiplier = defended ? 0f : 1f;
        if (defended) EventBus.Raise_TimedInputSuccess(target.RuntimeData);

        DamageCategory category = isNamedTreeSkill ? PlaceholderSkillResolver.Resolve(skillOrNull).Category : DamageCategory.Physical;
        int power = isNamedTreeSkill ? BattleConfig.PlaceholderSkillPower : DamageCalculator.BasicAttackPower;
        int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, category, power);
        float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

        BattleEngine.QueueBasicAttack(_state, attacker, target, defenseMultiplier, baseDamage);
        List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);

        // Perfect Dodge/Parry reward (2026-08-05, user-directed — see DECISIONS.md ->
        // [Combat]: "Perfect dodges and parrys restore aura"), on top of avoiding the hit
        // (and, for Parry, the counter-attack below).
        if (defended && wasPerfect) target.RestoreAura(BattleConfig.PerfectDefenseAuraRestore);

        BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);

        // Hit-flash and Dodge-dissolve for the held projectile launched above already fired
        // inside RunDefenseTimedInput itself, the instant the outcome was determined (2026-08-11
        // fix — waiting until here added a real, playtest-confirmed delay). Parry's outline flash
        // fired there too; only the deflect-and-counter projectile — which needs the
        // counter-attacker's own Primal type, not known to BattleHUDController — still resolves
        // here, and doubles as the counter-attack's own hit VFX below.
        if (isParry) BattleHUDController.Instance.ResolveParryDeflect(GetPrimalTypeOrDefault(target));

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
            AccumulateDamageDealt(counterResults);
            BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
            // No separate projectile here — ResolveParryDeflect (above) already launched the
            // visual return-fire this counter-attack's damage is resolving against.
            LogResults(counterResults, counterTypeMultiplier, timedInputSuccess: false);

            yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                $"{target.DisplayName} counter-attacks!", BattleConfig.AutoMessageDurationSeconds));
        }
    }

    /// <summary>
    /// Resolves an enemy's self-targeted support move — the 3 self-support built-ins mirror
    /// ResolveBuiltInMove's Charge/Heal/Regen cases exactly (same Aura costs/effects), minus the
    /// player-only HUD status-icon calls (SetRegenStatus/AddBurstFill index into player-only
    /// nameplate arrays — see BattleHUDController — and Burst is explicitly player-only scoped,
    /// see BattleParticipant.BurstGauge). A self-targeted tree skill (BuiltInMove == None) mirrors
    /// ResolveSkillAction's self-targeted-status branch, applied to attacker instead of target.
    /// </summary>
    private IEnumerator ResolveEnemySelfSupportAction(BattleParticipant attacker, SkillData skill)
    {
        switch (skill.BuiltInMove)
        {
            case BuiltInMoveType.Charge:
                attacker.RestoreAura(BattleConfig.ChargeAuraRestore);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} charges, restoring Aura!");
                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} charges!", BattleConfig.AutoMessageDurationSeconds));
                yield break;

            case BuiltInMoveType.Heal:
                attacker.SpendAura(BattleConfig.HealAuraCost);
                attacker.Heal(BattleConfig.HealAmount);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} heals {BattleConfig.HealAmount} HP!");
                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} uses H!", BattleConfig.AutoMessageDurationSeconds));
                yield break;

            case BuiltInMoveType.Regen:
                attacker.SpendAura(BattleConfig.RegenAuraCost);
                attacker.ApplyRegen(BattleConfig.RegenHealPerTurn, BattleConfig.RegenDurationTurns);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} uses Aura Regen!");
                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} uses R!", BattleConfig.AutoMessageDurationSeconds));
                yield break;

            default: // BuiltInMoveType.None -> self-targeted status tree skill
            {
                attacker.SpendAura(BattleConfig.PlaceholderSkillAuraCost);
                PlaceholderSkillResolver.SkillResolution resolution = PlaceholderSkillResolver.Resolve(skill);
                StatusEffectType status = resolution.AppliedStatus.Value;
                StatusEffectCatalog.Entry entry = StatusEffectCatalog.Get(status);
                int duration = StatusDurationCalculator.ComputeDuration(entry.MinDurationTurns,
                    attacker.RuntimeData.EffectiveStat(StatType.Resonance), attacker.RuntimeData.EffectiveStat(StatType.Resolve), entry.IsPositive);

                attacker.ApplyStatus(status, duration);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                BattleHUDController.Instance.AppendBattleLog(BattleLogFormatter.FormatStatusApplied(attacker, status, duration));

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} uses {skill.SkillName}!", BattleConfig.AutoMessageDurationSeconds));
                yield break;
            }
        }
    }

    /// <summary>
    /// Resolves an enemy's non-self-targeted status tree skill against target — mirrors
    /// ResolveSkillAction's non-self-targeted status branch, no defense/timed-input flow (status
    /// skills were never subject to Dodge/Parry on the player side either).
    /// </summary>
    private IEnumerator ResolveEnemyDebuffAction(BattleParticipant attacker, BattleParticipant target, SkillData skill)
    {
        attacker.SpendAura(BattleConfig.PlaceholderSkillAuraCost);

        PlaceholderSkillResolver.SkillResolution resolution = PlaceholderSkillResolver.Resolve(skill);
        StatusEffectType status = resolution.AppliedStatus.Value;
        StatusEffectCatalog.Entry entry = StatusEffectCatalog.Get(status);
        int duration = StatusDurationCalculator.ComputeDuration(entry.MinDurationTurns,
            attacker.RuntimeData.EffectiveStat(StatType.Resonance), target.RuntimeData.EffectiveStat(StatType.Resolve), entry.IsPositive);

        target.ApplyStatus(status, duration);
        BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
        BattleHUDController.Instance.AppendBattleLog(BattleLogFormatter.FormatStatusApplied(target, status, duration));

        yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
            $"{attacker.DisplayName} uses {skill.SkillName}!", BattleConfig.AutoMessageDurationSeconds));
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

    /// <summary>
    /// Ends the battle. On a Won outcome, grants each surviving party member a flat Aura reward
    /// (BattleConfig.AuraRewardOnWin) and blocks on the read-only post-battle summary screen
    /// (2026-08 session — reworked from the old spend-here-and-now Aura Allocation screen per
    /// user direction: Aura spending moved to the new Tab-key overworld menu instead; see
    /// DECISIONS.md -> [Combat]) before hiding the HUD/unloading the scene. Lost and Fled outcomes
    /// both skip straight to cleanup, same as before this screen existed — a successful Flee is
    /// deliberately NOT treated as a Loss (see EventBus.OnBattleFled's own doc comment): zero
    /// currency/item cost, matching CLAUDE.md's "Loss state" rule only actually applying to a real
    /// Lost outcome.
    /// </summary>
    private IEnumerator EndBattle(BattleOutcome outcome)
    {
        var result = new BattleResult(outcome == BattleOutcome.Won, _state.PlayerSide, _state.EnemySide);

        if (outcome == BattleOutcome.Won) EventBus.Raise_BattleWon(result);
        else if (outcome == BattleOutcome.Fled) EventBus.Raise_BattleFled(result);
        else EventBus.Raise_BattleLost(result);

        if (outcome == BattleOutcome.Won)
        {
            var summary = new BattleSummary
            {
                TotalDamageDealt = _totalDamageDealt,
                TotalHealingDone = _totalHealingDone,
            };

            foreach (BattleParticipant p in _state.PlayerSide)
            {
                if (!p.IsAlive) continue;
                p.RuntimeData.commonAura += BattleConfig.AuraRewardOnWin;
                summary.TotalAuraGained += BattleConfig.AuraRewardOnWin;
            }

            bool summaryDone = false;
            BattleSummaryController.Instance.Show(summary, () => summaryDone = true);
            yield return new WaitUntil(() => summaryDone);
        }

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
