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

    /// <summary>Read-only accessor for debug/test tooling (e.g. DebugLaneCycler) that needs the live player side without a real gameplay reason to touch it. Null before Start() runs.</summary>
    public List<BattleParticipant> PlayerSide => _state?.PlayerSide;

    private bool _playerActionChosen;
    private BattleParticipant _pendingTarget;
    private SkillData _pendingSkill; // always non-null once a move is confirmed — every orb (built-in move or tree skill) is real SkillData now, see BuiltInMoveType
    private bool _battleEndedEarly; // set true by a successful Capture — EndBattle already ran, RunBattleLoop must not also call EnemyTurn

    /// <summary>Standard_Move.asset, resolved once in Start() by BuiltInMove lookup (not equipped by any creature, see WildSpawnSystem — the Move icon works unconditionally) — cached for HandleMoveConfirmed's combo-streak bookkeeping.</summary>
    private SkillData _moveSkill;

    /// <summary>
    /// Which player creature's skill ring is currently open in PlayerTurn's loop, -1 if none.
    /// Promoted from a local variable to a field (2026-08-12) so HandleMoveConfirmed can read/
    /// react to it — a Move-icon drag can now complete for a creature while ITS OWN ring happens
    /// to be open (the icon is independent of ring state), which PlayerTurn's WaitUntil predicate
    /// wouldn't otherwise notice. Read/written only inside PlayerTurn and HandleMoveConfirmed.
    /// </summary>
    private int _activePlayerRingSlot = -1;

    /// <summary>Set by HandleMoveConfirmed when it force-closes the currently-open ring (see _activePlayerRingSlot) — wakes PlayerTurn's WaitUntil so its own bookkeeping resets instead of going stale.</summary>
    private bool _ringForcedClosedByMove;

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

        // In-battle Move (2026-08-12 redesign) — a dedicated always-present icon per creature, not
        // a skill-ring orb, so it's wired the same way BurstBarClicked is: an instant, independent
        // HUD event, not routed through PlayerTurn's _pendingSkill/ChosenMove wait-loop.
        BattleHUDController.Instance.MoveConfirmed += HandleMoveConfirmed;
        if (_skillDatabase != null)
        {
            foreach ((SkillData skill, string _) in _skillDatabase.AllSkills)
            {
                if (skill.BuiltInMove == BuiltInMoveType.Move) { _moveSkill = skill; break; }
            }
        }

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

    /// <summary>
    /// Handles a completed Move-icon drag (2026-08-12 redesign — BattleHUDController.MoveConfirmed,
    /// fired once OnMoveDragPointerUp validates the drop against a free stage position marker).
    /// Bypasses ResolveSkillAction/the ChosenMove pipeline entirely, same "instant, independent
    /// HUD-event handler" shape as HandleBurstBarClicked above — Move is no longer "a skill the
    /// player picked from a menu," it's a dedicated action with its own always-present icon.
    /// Re-checks alive/not-acted directly (BattleHUDController's own drag-start guard already does
    /// this, but this handler doesn't trust the UI alone either, matching this codebase's general
    /// posture elsewhere — e.g. FormationSystem/ResolveBuiltInMove's own occupancy re-checks).
    /// </summary>
    private void HandleMoveConfirmed(int slotIndex, int lane, int position)
    {
        if (slotIndex < 0 || slotIndex >= _state.PlayerSide.Count) return;
        BattleParticipant attacker = _state.PlayerSide[slotIndex];
        if (!attacker.IsAlive || attacker.HasActedThisTurn) return;

        // User-directed: Move still counts as "using a skill" for combo-streak/SkillUsed purposes,
        // same bookkeeping ResolveSkillAction runs for every other move — reproduced explicitly
        // here since this path deliberately bypasses that method.
        if (_moveSkill != null)
        {
            attacker.RecordSkillTreeUse(_moveSkill.TreeType);
            attacker.RecordSkillUse(_moveSkill);
            EventBus.Raise_SkillUsed(attacker.RuntimeData, _moveSkill);
        }

        attacker.HasActedThisTurn = true;
        BattleHUDController.Instance.SetMoveIconVisible(slotIndex, false);

        // If this creature's own skill ring happened to be open, PlayerTurn's WaitUntil loop has
        // no other way to notice this action landed — force-close it and flag PlayerTurn to reset
        // its bookkeeping (_activePlayerRingSlot) on its next wake, same as every other "something
        // closed the ring out from under the loop" case it already handles.
        if (slotIndex == _activePlayerRingSlot)
        {
            BattleHUDController.Instance.HideMoveSelection();
            _ringForcedClosedByMove = true;
        }

        StartCoroutine(ResolveBuiltInMove(attacker, slotIndex, BuiltInMoveType.Move, null, lane, position));
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

        // Move icons (2026-08-12 redesign) reset alongside HasActedThisTurn above — every alive
        // creature's icon becomes visible again at the start of its side's turn.
        for (int i = 0; i < _state.PlayerSide.Count; i++)
        {
            if (_state.PlayerSide[i].IsAlive) BattleHUDController.Instance.SetMoveIconVisible(i, true);
        }

        // Auto-open the first living party member's move wheel (2026-08 follow-up — user-directed:
        // "instead of having to click the wheel open, Auto open the phasix that is first in the
        // roster... so its a good indicator that its the players turn"). Sets the SAME field a
        // real click would set, so the loop's normal "_activePlayerRingSlot < 0" branch below opens
        // it through its existing logic — no separate wheel-opening code path to keep in sync.
        int firstAliveSlot = _state.PlayerSide.FindIndex(p => p.IsAlive);
        if (firstAliveSlot >= 0) _pendingCreatureClickSlot = firstAliveSlot;

        _activePlayerRingSlot = -1; // -1 = no wheel currently open
        _ringForcedClosedByMove = false;

        while (!_endTurnRequested && !_fleeRequested)
        {
            if (_state.EnemySide.TrueForAll(e => !e.IsAlive)) yield break; // wiped mid-turn — battle is already over, TryEndBattle picks this up right after PlayerTurn returns

            if (_activePlayerRingSlot < 0)
            {
                yield return new WaitUntil(() => _endTurnRequested || _fleeRequested || _pendingCreatureClickSlot >= 0);
                if (_endTurnRequested || _fleeRequested) break;

                // A background click that landed while nothing was selected is a no-op (nothing
                // to close) — clear it here so it can't linger and instantly close the wheel
                // we're about to open below.
                _backgroundClickRequested = false;

                _activePlayerRingSlot = _pendingCreatureClickSlot;
                _pendingCreatureClickSlot = -1;

                BattleParticipant clicked = _state.PlayerSide[_activePlayerRingSlot];
                if (!clicked.IsAlive) { _activePlayerRingSlot = -1; continue; }

                if (clicked.HasActedThisTurn)
                {
                    BattleHUDController.Instance.ShowMoveSelectionReadOnly(_activePlayerRingSlot);
                }
                else
                {
                    _playerActionChosen = false;
                    _pendingSkill = null;
                    List<BattleParticipant> aliveEnemiesForWheel = _state.EnemySide.FindAll(e => e.IsAlive);
                    BattleHUDController.Instance.ShowMoveSelection(_activePlayerRingSlot, clicked, aliveEnemiesForWheel,
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
            // phasix orb menu closes, then the new clicked phasix menu shows"), a click on the
            // empty stage background (2026-08-06, user-directed: "clicking outside of that should
            // hide any open skill wheels"), or — 2026-08-12 — this slot's Move icon completing a
            // drag while its ring happened to be open (HandleMoveConfirmed force-closes the ring
            // and sets _ringForcedClosedByMove so this loop notices and resets, rather than
            // leaving _activePlayerRingSlot stale).
            int openedSlot = _activePlayerRingSlot;
            yield return new WaitUntil(() =>
                _endTurnRequested ||
                _fleeRequested ||
                _playerActionChosen ||
                _backgroundClickRequested ||
                _ringForcedClosedByMove ||
                (_pendingCreatureClickSlot >= 0 && _pendingCreatureClickSlot != openedSlot));

            if (_endTurnRequested || _fleeRequested) break;

            if (_ringForcedClosedByMove)
            {
                _ringForcedClosedByMove = false;
                _activePlayerRingSlot = -1;
                continue; // ring already hidden by HandleMoveConfirmed — next iteration just waits again
            }

            if (_backgroundClickRequested)
            {
                _backgroundClickRequested = false;
                BattleHUDController.Instance.HideMoveSelection();
                _activePlayerRingSlot = -1;
                continue; // nothing to open — next iteration's "_activePlayerRingSlot < 0" branch just waits again
            }

            if (_pendingCreatureClickSlot >= 0 && _pendingCreatureClickSlot != openedSlot)
            {
                BattleHUDController.Instance.HideMoveSelection();
                _activePlayerRingSlot = -1;
                continue; // next iteration's "_activePlayerRingSlot < 0" branch picks up the pending click
            }

            if (!_playerActionChosen) continue; // read-only wheel, nothing else happened yet — keep waiting

            BattleParticipant attacker = _state.PlayerSide[_activePlayerRingSlot];
            int attackerSlotIndex = _activePlayerRingSlot;
            _playerActionChosen = false;
            _activePlayerRingSlot = -1; // the wheel this action came from is already hidden by BeginDragForSkill's own confirm/drag flow
            attacker.HasActedThisTurn = true;
            BattleHUDController.Instance.SetMoveIconVisible(attackerSlotIndex, false);

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
    /// True if a Beat Sequence skill has an Approach beat (melee — closes lane distance) somewhere
    /// in its authored list, false for a ranged skill (no Approach — Attack_Pattern_Directive Part
    /// 5's Reflex-based ranged archetypes: Instant Strike, Feint, Metronome, Jitter). Drives whether
    /// ResolveMeleeBeatSequence launches a real projectile (ranged) or keeps the melee lunge+flash
    /// (RunMeleeLungeAndFlash) at the Attack beat — 2026-08-12 follow-up, user: "if they're ranged
    /// how come i dont see a projectile."
    /// </summary>
    private static bool HasApproachBeat(SkillData skill)
    {
        if (skill.BeatSequence == null) return false;
        for (int i = 0; i < skill.BeatSequence.Count; i++)
            if (skill.BeatSequence[i] == BeatType.Approach) return true;
        return false;
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
            // Move (BuiltInMoveType.Move) never reaches here — it's no longer a ring-orb choice
            // (2026-08-12 redesign), see HandleMoveConfirmed, which calls ResolveBuiltInMove
            // directly with a real destination instead of routing through this method.
            yield return StartCoroutine(ResolveBuiltInMove(attacker, attackerSlotIndex, skill.BuiltInMove, target));
            yield break;
        }

        attacker.SpendAura(BattleConfig.PlaceholderSkillAuraCost);
        bool timedInputHappened = false;

        // Melee Beat Sequence branch (Attack_Pattern_Directive Part 7, 2026-08-11) — a skill with a
        // non-empty BeatSequence skips PlaceholderSkillResolver entirely; its damage resolution
        // happens inside the Attack beat itself (ResolveMeleeAttackBeatOffense), not here. Still
        // falls through to the same combo/chain/mastery/announcement tail below every other named-
        // skill path runs, so a Beat Sequence skill behaves identically to a normal skill for
        // everything except how its own damage/status gets applied.
        if (skill.StackingRhythm != StackingRhythmType.None)
        {
            int stackingTargetSlotIndex = _state.EnemySide.IndexOf(target);
            yield return StartCoroutine(ResolveStackingRhythmAttack(attacker, attackerSlotIndex, true, target, stackingTargetSlotIndex, false, skill));
            timedInputHappened = true;
        }
        else if (skill.VolleyRingSequence != null && skill.VolleyRingSequence.Count > 0)
        {
            int volleyTargetSlotIndex = _state.EnemySide.IndexOf(target);
            yield return StartCoroutine(ResolveMultiHitVolleyAttack(attacker, attackerSlotIndex, true, target, volleyTargetSlotIndex, false, skill));
            timedInputHappened = true;
        }
        else if (skill.HoldInputArchetype == HoldInputArchetype.ChargeRelease)
        {
            int chargeTargetSlotIndex = _state.EnemySide.IndexOf(target);
            yield return StartCoroutine(ResolveChargeReleaseAttack(attacker, attackerSlotIndex, true, target, chargeTargetSlotIndex, false, skill));
            timedInputHappened = true;
        }
        else if (skill.BeatSequence != null && skill.BeatSequence.Count > 0)
        {
            int meleeTargetSlotIndex = _state.EnemySide.IndexOf(target);
            yield return StartCoroutine(ResolveMeleeBeatSequence(attacker, attackerSlotIndex, true, target, meleeTargetSlotIndex, false, skill));
            timedInputHappened = true;
        }
        else
        {
            PlaceholderSkillResolver.SkillResolution resolution = PlaceholderSkillResolver.Resolve(skill);

            if (resolution.DealsDamage)
            {
                // Good/Perfect bands mirror Defend's own Dodge/Parry tolerances exactly (2026-08-11,
                // user-directed — see DECISIONS.md -> [Combat]), scaled by the ATTACKER's own
                // Instinct/bond (offense has always used the attacker's stats here; defense uses the
                // defender's).
                float goodToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                    TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
                    attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);
                float perfectToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                    TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
                    attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);

                // Launches the projectile now, concurrently with the ring below — sweepDuration is
                // sized off the projectile's own real travel time so the ring's "perfect" instant
                // lines up with when it visually connects (2026-08-11 timing-sync pass).
                int targetSlotIndex = _state.EnemySide.IndexOf(target);
                float sweepDuration = BattleHUDController.Instance.LaunchSyncedProjectile(
                    attackerSlotIndex, true, targetSlotIndex, false, GetPrimalTypeOrDefault(attacker), holdForOutcome: false);
                yield return StartCoroutine(BattleHUDController.Instance.RunTimedInput(
                    $"{skill.SkillName} — {attacker.DisplayName}", goodToleranceHalfWidth, perfectToleranceHalfWidth, sweepDuration));

                BattleHUDController.OffenseOutcome timedOutcome = BattleHUDController.Instance.LastOffenseOutcome;
                bool timedSuccess = BattleHUDController.Instance.LastTimedInputSuccess;
                bool timedPerfect = BattleHUDController.Instance.LastTimedInputWasPerfect;
                // TimedInputStreak (C2) specifically tracks PERFECT hits, not merely successful ones
                // — user-directed: "works with any... skill that gets perfect, after a miss it
                // rests." See BattleParticipant.RecentTimedInputPerfects.
                attacker.RecordTimedInputPerfect(timedPerfect);
                timedInputHappened = true;
                if (timedSuccess) EventBus.Raise_TimedInputSuccess(attacker.RuntimeData);

                // TODO: pending design — Perfect currently only grants bonus damage. Damage-dealing
                // skills have no inherent status payload today (PlaceholderSkillResolver's damage/
                // status tree split), so a "Perfect also applies a bonus status" reward is deferred
                // until real skill content exists (2026-08-11, user-directed — see DECISIONS.md ->
                // [Combat]).
                float attackMultiplier = timedOutcome switch
                {
                    BattleHUDController.OffenseOutcome.Perfect => TimedInputConfig.PerfectDamageMultiplier,
                    BattleHUDController.OffenseOutcome.Good => TimedInputConfig.GoodDamageMultiplier,
                    _ => TimedInputConfig.MissDamageMultiplier
                };
                int pureBaseDamage = DamageCalculator.ComputeBaseDamage(attacker, target, resolution.Category, BattleConfig.PlaceholderSkillPower);
                int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, resolution.Category, BattleConfig.PlaceholderSkillPower);
                float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

                BattleEngine.QueueBasicAttack(_state, attacker, target, attackMultiplier, baseDamage);
                List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);
                AccumulateDamageDealt(results);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);

                foreach (BattleActionResult result in results)
                {
                    string line = BattleLogFormatter.FormatSkillAttack(result.Attacker, result.Target, skill.SkillName, pureBaseDamage, baseDamage, result.DamageApplied, typeMultiplier, timedOutcome);
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
    /// Runs a melee skill's full authored beat list (Attack_Pattern_Directive Part 7) — Approach/
    /// WindupReal/WindupFake beats via BeatSequenceRunner, the Attack beat via
    /// ResolveMeleeAttackBeatOffense/Defense below (whichever side is attacking), then an
    /// unconditional automatic Return-to-origin. No interrupt branch exists — confirmed via grep, no
    /// Stun/turn-loss logic exists anywhere in this file today, so "the sequence always completes"
    /// requires no defensive code here.
    ///
    /// 2026-08-12 rework (see LaneMovementSystem/BeatSequenceRunner's own class doc comments):
    /// Approach/Return never change attacker.LaneIndex itself (lanes are vertical rows now, and
    /// melee doesn't actually cross rows) — restingLeft/restingTop (captured once, before any beat
    /// runs) are the position to close the gap FROM and automatically Return TO, replacing the old
    /// lane-index-based origin tracking entirely.
    ///
    /// 2026-08-12 follow-up (user: "I was expecting it to move diagonally to get in front of the
    /// target then the melee comes out"): Approach's closing lunge now ALSO tweens `top` to the
    /// TARGET's row alongside the horizontal gap-close, so attacker and target visually line up
    /// (diagonal movement when rows differ) before the Attack beat fires — restingTop lets Return
    /// undo that same detour afterward, same as restingLeft always has for the horizontal axis.
    ///
    /// 2026-08-12 follow-up #2 (Group 1 archetypes — Instant Strike, Feint, Metronome, Jitter, see
    /// Attack_Pattern_Directive_v0_1_0.md Part 5): on a skill.ResponseTiming == PreEmptive skill, a
    /// WindupReal/WindupFake beat starts its tween WITHOUT awaiting it, then immediately awaits a
    /// timed-input window (RunTimedInput for a player-side attacker, RunDefenseTimedInput for an
    /// enemy-side one) sized to the SAME duration — the exact start-one-coroutine-then-await-a-
    /// second-one-sized-to-it pattern already established by ResolveSkillAction's
    /// LaunchSyncedProjectile + RunTimedInput pairing, reused here rather than inventing new
    /// concurrency. WindupReal's outcome is captured into a local variable for the Attack beat to
    /// consume (skipping ITS OWN ring in that case); WindupFake's outcome is deliberately discarded —
    /// Part 7's Feint archetype means reacting to a fake tell has no effect either way this pass.
    /// Reactive skills (the default — every skill that existed before this follow-up) are completely
    /// unaffected: Windup is still a pure visual wait, and the Attack beat still opens its own ring.
    /// </summary>
    private IEnumerator ResolveMeleeBeatSequence(BattleParticipant attacker, int attackerSlotIndex, bool attackerIsPlayerSide,
        BattleParticipant target, int targetSlotIndex, bool targetIsPlayerSide, SkillData skill)
    {
        UnityEngine.UIElements.VisualElement attackerElement = BattleHUDController.Instance.GetStageCreatureElement(attackerSlotIndex, attackerIsPlayerSide);
        float restingLeft = attackerElement.resolvedStyle.left;
        float restingTop = attackerElement.resolvedStyle.top;
        UnityEngine.UIElements.VisualElement targetElement = BattleHUDController.Instance.GetStageCreatureElement(targetSlotIndex, targetIsPlayerSide);

        bool preEmptive = skill.ResponseTiming == ResponseTimingType.PreEmptive;
        bool ranged = !HasApproachBeat(skill);
        bool preEmptiveOutcomeCaptured = false;
        BattleHUDController.OffenseOutcome capturedOffenseOutcome = BattleHUDController.OffenseOutcome.Miss;
        BattleHUDController.DefenseOutcome capturedDefenseOutcome = BattleHUDController.DefenseOutcome.Miss;
        bool capturedDefenseWasPerfect = false;

        foreach (BeatType beat in skill.BeatSequence)
        {
            switch (beat)
            {
                case BeatType.Approach:
                    yield return StartCoroutine(BeatSequenceRunner.RunApproach(attacker, attackerSlotIndex, attackerIsPlayerSide, target, targetElement));
                    break;

                case BeatType.WindupReal:
                case BeatType.WindupFake:
                {
                    bool isFake = beat == BeatType.WindupFake;
                    float duration = BeatSequenceRunner.ComputeWindupDuration(isFake, skill.WindupJitterRangeSeconds);

                    if (!preEmptive)
                    {
                        yield return StartCoroutine(BeatSequenceRunner.RunWindup(attacker, attackerSlotIndex, attackerIsPlayerSide, duration));
                        break;
                    }

                    // 2026-08-13 follow-up (user: "for the feint, do the fake shoot, then do the
                    // hop for the warning, then do the actual strike") — a ranged Feint's fake beat
                    // is now a quick, harmless fake projectile shot with NO hop and NO ring at all —
                    // there's nothing to react to since no tell precedes it. WindupReal (below)
                    // still gets the full hop -> tell -> ring treatment as its own following beat.
                    if (ranged && isFake)
                    {
                        BattleHUDController.Instance.LaunchRangedBeatSequenceProjectile(
                            attackerSlotIndex, attackerIsPlayerSide, targetSlotIndex, targetIsPlayerSide,
                            GetPrimalTypeOrDefault(attacker), duration, holdForOutcome: false);
                        yield return new WaitForSeconds(duration);
                        break;
                    }

                    // 2026-08-13 follow-up (user: "when the skill is selected for the hop to occur
                    // then after a brief delay then the projectile shoots") — the warning hop plays
                    // FIRST and is AWAITED; the squash tween + ring then open together exactly as
                    // before, their shared `duration` window now serving as the "brief delay." The
                    // projectile itself no longer launches here — it moved to the Attack beat
                    // (ResolveMeleeAttackBeatOffense/Defense), decoupled from the ring so it plays
                    // only once the tell has already been reacted to, not tracked during it (per
                    // the user's confirmed answer: the ring stays on the tell, not the shot).
                    if (ranged)
                        yield return StartCoroutine(BeatSequenceRunner.RunWarningHop(attacker, attackerSlotIndex, attackerIsPlayerSide));

                    // Fire-and-forget — the tween runs concurrently with the ring below, both sized to `duration`.
                    StartCoroutine(BeatSequenceRunner.RunWindup(attacker, attackerSlotIndex, attackerIsPlayerSide, duration));

                    if (attackerIsPlayerSide)
                    {
                        float goodToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                            TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
                            attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);
                        float perfectToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                            TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
                            attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);

                        yield return StartCoroutine(BattleHUDController.Instance.RunTimedInput(
                            $"{skill.SkillName} — {attacker.DisplayName}", goodToleranceHalfWidth, perfectToleranceHalfWidth, duration));

                        if (!isFake)
                        {
                            capturedOffenseOutcome = BattleHUDController.Instance.LastOffenseOutcome;
                            preEmptiveOutcomeCaptured = true;
                        }
                    }
                    else
                    {
                        float dodgeToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                            TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
                            target.RuntimeData.EffectiveStat(StatType.Instinct), target.RuntimeData.bondPercent);
                        float parryToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                            TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
                            target.RuntimeData.EffectiveStat(StatType.Instinct), target.RuntimeData.bondPercent);

                        yield return StartCoroutine(BattleHUDController.Instance.RunDefenseTimedInput(
                            targetSlotIndex, $"DEFEND — {attacker.DisplayName}! Left-Click Dodge · Right-Click Parry",
                            dodgeToleranceHalfWidth, parryToleranceHalfWidth, duration));

                        if (!isFake)
                        {
                            capturedDefenseOutcome = BattleHUDController.Instance.LastDefenseOutcome;
                            capturedDefenseWasPerfect = BattleHUDController.Instance.LastDefenseWasPerfect;
                            preEmptiveOutcomeCaptured = true;
                        }
                    }
                    break;
                }

                case BeatType.Attack:
                    // TODO: if target died mid-sequence (e.g. a status tick between Approach and
                    // Attack), damage is skipped but the sequence still completes its Return below —
                    // Beat Sequences aren't specified to abort on target death.
                    if (target.IsAlive)
                    {
                        if (attackerIsPlayerSide)
                            yield return StartCoroutine(ResolveMeleeAttackBeatOffense(attacker, attackerSlotIndex, target, targetSlotIndex, skill,
                                restingLeft, restingTop, preEmptive, preEmptiveOutcomeCaptured ? capturedOffenseOutcome : (BattleHUDController.OffenseOutcome?)null));
                        else
                            yield return StartCoroutine(ResolveMeleeAttackBeatDefense(attacker, target, targetSlotIndex, skill,
                                restingLeft, restingTop, preEmptive, preEmptiveOutcomeCaptured ? capturedDefenseOutcome : (BattleHUDController.DefenseOutcome?)null, capturedDefenseWasPerfect));
                    }
                    break;
            }
        }

        // 2026-08-14: Return-to-origin moved into ResolveMeleeAttackBeatOffense/Defense themselves
        // (each now takes restingLeft/restingTop directly) — Defense specifically needed it to fire
        // BEFORE its own isParry counter-attack block, not after the whole Attack beat (including
        // the counter) had already resolved (user: "have it bounce back after attack then shoot the
        // parry animation"). Nothing left to do here for either side.
    }

    /// <summary>
    /// The Attack beat's shared visual flourish — a quick lunge toward the opponent (player lunges
    /// further right, enemy further left — both "toward center," matching the direction their own
    /// closing lunge already traveled, see BeatSequenceRunner.RunApproach), a hit-flash on the
    /// target once the lunge connects, then a snap back to the attacker's current position. Shared
    /// by ResolveMeleeAttackBeatOffense/Defense since the visual itself is identical regardless of
    /// which side is attacking.
    ///
    /// 2026-08-12 bug fix (user: "when the enemy attacks it looks like the color on the phasix
    /// disappears. It happens when i use the dodge mechanic") — showHitFlash lets
    /// ResolveMeleeAttackBeatDefense skip the flash entirely on a successful Dodge/Parry. Root
    /// cause: on Dodge, RunDefenseTimedInput already fires DissolveVfxBridge.PlayDefenderDissolve
    /// on the defender (a ~0.5s background-image swap + revert), started via its own StartCoroutine
    /// and NOT awaited — so it's still mid-flight when this method's own hit-flash used to fire
    /// unconditionally ~0.3s later. CombatVfxController.HitFlashRoutine captures
    /// `resolvedStyle.backgroundColor` as its "resting color" to revert to — but mid-dissolve, that
    /// value is temporarily `Color.clear` (the dissolve swaps to a backgroundImage and zeroes
    /// backgroundColor for the duration), not the real Primal-type color. Both coroutines then write
    /// to the SAME element's backgroundColor around the same instant with no ordering guarantee —
    /// whichever one's final write lands last wins, and the hit-flash's (wrongly captured as clear)
    /// sometimes won, leaving the defender permanently colorless. A hit that was actually dodged
    /// shouldn't show a hit-flash at all, so the real fix is not flashing in that case to begin with.
    /// </summary>
    private IEnumerator RunMeleeLungeAndFlash(BattleParticipant attacker, int attackerSlotIndex, bool attackerIsPlayerSide,
        int targetSlotIndex, bool targetIsPlayerSide, bool showHitFlash = true)
    {
        UnityEngine.UIElements.VisualElement attackerElement = BattleHUDController.Instance.GetStageCreatureElement(attackerSlotIndex, attackerIsPlayerSide);
        // Read the attacker's actual current position (post-closing-lunge, right next to the
        // target) rather than recomputing it from any lane formula — lanes are vertical rows now
        // (LaneMovementSystem's class doc comment) and no longer determine horizontal position at
        // all, so the attacker's real current `left` is the only source of truth here.
        float currentLeft = attackerElement.resolvedStyle.left;
        float lungeLeft = attackerIsPlayerSide
            ? currentLeft + BeatSequenceConfig.AttackLungeOffsetPx
            : currentLeft - BeatSequenceConfig.AttackLungeOffsetPx;

        VisualElementTweening.TweenLeft(attackerElement, lungeLeft, BeatSequenceConfig.AttackLungeDurationSeconds);
        yield return new WaitForSeconds(BeatSequenceConfig.AttackLungeDurationSeconds);

        if (showHitFlash) BattleHUDController.Instance.FlashStageCreatureHit(targetSlotIndex, targetIsPlayerSide, GetPrimalTypeOrDefault(attacker));

        VisualElementTweening.TweenLeft(attackerElement, currentLeft, BeatSequenceConfig.AttackLungeDurationSeconds);
        yield return new WaitForSeconds(BeatSequenceConfig.AttackLungeDurationSeconds);
    }

    /// <summary>
    /// A ranged + PreEmptive skill's real Attack-beat visual (Instant Strike, Feint's real strike) —
    /// player OFFENSE side: the outcome always "connects" (Good/Perfect/Miss only scale damage, never
    /// avoid it), so this just launches a projectile and auto-resolves it (hit-flash) on arrival,
    /// same holdForOutcome:false pattern ResolveSkillAction's own ranged path already uses.
    /// </summary>
    private IEnumerator RunResolvedRangedProjectileOffense(BattleParticipant attacker, int attackerSlotIndex, int targetSlotIndex)
    {
        BattleHUDController.Instance.LaunchRangedBeatSequenceProjectile(
            attackerSlotIndex, true, targetSlotIndex, false, GetPrimalTypeOrDefault(attacker),
            BeatSequenceConfig.ResolvedProjectileTravelSeconds, holdForOutcome: false);
        yield return new WaitForSeconds(BeatSequenceConfig.ResolvedProjectileTravelSeconds);
    }

    /// <summary>
    /// A ranged + PreEmptive skill's real Attack-beat visual — enemy DEFENSE side. Unlike the
    /// classic ranged path (where the projectile is launched BEFORE the ring and RunDefenseTimedInput's
    /// own switch resolves it the instant the ring closes), here the outcome was already captured
    /// earlier during the Windup beat's ring, well before this projectile even exists — so there's
    /// no "currently held" projectile for that switch to have resolved against. This method launches
    /// AND explicitly dispatches against the already-known outcome itself: Miss (hit lands) ->
    /// ResolveHitProjectile, Dodge -> ResolveDodgedProjectile, Parry -> outline flash now (the
    /// deflect-bounce visual for the counter is handled separately, by ResolveMeleeAttackBeatDefense's
    /// own isParry block, which needs the counter-attacker's own Primal type).
    /// </summary>
    private IEnumerator RunResolvedRangedProjectileDefense(BattleParticipant attacker, int attackerSlotIndex, int targetSlotIndex, BattleHUDController.DefenseOutcome outcome)
    {
        BattleHUDController.Instance.LaunchRangedBeatSequenceProjectile(
            attackerSlotIndex, false, targetSlotIndex, true, GetPrimalTypeOrDefault(attacker),
            BeatSequenceConfig.ResolvedProjectileTravelSeconds, holdForOutcome: true);

        if (outcome == BattleHUDController.DefenseOutcome.Parry) BattleHUDController.Instance.FlashParryOutline();

        yield return new WaitForSeconds(BeatSequenceConfig.ResolvedProjectileTravelSeconds);

        switch (outcome)
        {
            case BattleHUDController.DefenseOutcome.Miss: BattleHUDController.Instance.ResolveHitProjectile(); break;
            case BattleHUDController.DefenseOutcome.Dodge: BattleHUDController.Instance.ResolveDodgedProjectile(targetSlotIndex); break;
        }
    }

    /// <summary>
    /// The Attack beat's own resolution when a PLAYER-side participant is the attacker — mirrors
    /// ResolveSkillAction's damage block (Good/Perfect timed input -> DamageCalculator ->
    /// BattleEngine -> logging -> burst fill) with the projectile removed (melee doesn't travel) and
    /// RunMeleeLungeAndFlash's lunge+hit-flash in its place. sweepDuration falls back to the fixed
    /// TimedInputConfig.MarkerSweepDuration since there's no real "travel time" to size the ring off
    /// for an already-adjacent attacker — the same fallback LaunchSyncedProjectile itself already
    /// uses when there's no VFX controller to compute a real one from.
    ///
    /// DamageCategory is hardcoded to Physical rather than read from PlaceholderSkillResolver (which
    /// this path never calls) — Attack_Pattern_Directive Part 3 frames Physical as melee's default/
    /// expected case; TODO: pending design — an Elemental melee archetype would need this to become
    /// a real per-skill choice.
    ///
    /// 2026-08-12 follow-up (Group 1 archetypes): preEmptive/preEmptiveOutcome let
    /// ResolveMeleeBeatSequence hand in an outcome already resolved during the WindupReal beat's own
    /// ring (Attack_Pattern_Directive Part 5's "reacted to pre-emptively" archetypes) — when supplied,
    /// this method skips opening a SECOND ring here and just applies the given outcome. Both default
    /// to the Reactive case (no outcome supplied), so this method's behavior is unchanged for every
    /// pre-existing skill.
    /// </summary>
    private IEnumerator ResolveMeleeAttackBeatOffense(BattleParticipant attacker, int attackerSlotIndex, BattleParticipant target, int targetSlotIndex, SkillData skill,
        float restingLeft, float restingTop, bool preEmptive = false, BattleHUDController.OffenseOutcome? preEmptiveOutcome = null)
    {
        BattleHUDController.OffenseOutcome timedOutcome;
        bool timedSuccess;
        bool timedPerfect;

        if (preEmptive && preEmptiveOutcome.HasValue)
        {
            timedOutcome = preEmptiveOutcome.Value;
            timedSuccess = timedOutcome != BattleHUDController.OffenseOutcome.Miss;
            timedPerfect = timedOutcome == BattleHUDController.OffenseOutcome.Perfect;
        }
        else
        {
            float goodToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
                attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);
            float perfectToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
                attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);

            yield return StartCoroutine(BattleHUDController.Instance.RunTimedInput(
                $"{skill.SkillName} — {attacker.DisplayName}", goodToleranceHalfWidth, perfectToleranceHalfWidth, TimedInputConfig.MarkerSweepDuration));

            timedOutcome = BattleHUDController.Instance.LastOffenseOutcome;
            timedSuccess = BattleHUDController.Instance.LastTimedInputSuccess;
            timedPerfect = BattleHUDController.Instance.LastTimedInputWasPerfect;
        }

        attacker.RecordTimedInputPerfect(timedPerfect);
        if (timedSuccess) EventBus.Raise_TimedInputSuccess(attacker.RuntimeData);

        float attackMultiplier = timedOutcome switch
        {
            BattleHUDController.OffenseOutcome.Perfect => TimedInputConfig.PerfectDamageMultiplier,
            BattleHUDController.OffenseOutcome.Good => TimedInputConfig.GoodDamageMultiplier,
            _ => TimedInputConfig.MissDamageMultiplier
        };

        int pureBaseDamage = DamageCalculator.ComputeBaseDamage(attacker, target, DamageCategory.Physical, BattleConfig.PlaceholderSkillPower);
        int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, DamageCategory.Physical, BattleConfig.PlaceholderSkillPower);
        float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

        // Ranged + PreEmptive: the ring already resolved on the tell during Windup (2026-08-13
        // reorder) — the real strike's projectile fires HERE, decoupled from the ring. Melee (or a
        // hypothetical future Reactive ranged skill, none exist yet) still gets the lunge+flash.
        if (HasApproachBeat(skill) || !preEmptive)
            yield return StartCoroutine(RunMeleeLungeAndFlash(attacker, attackerSlotIndex, true, targetSlotIndex, false));
        else
            yield return StartCoroutine(RunResolvedRangedProjectileOffense(attacker, attackerSlotIndex, targetSlotIndex));

        BattleEngine.QueueBasicAttack(_state, attacker, target, attackMultiplier, baseDamage);
        List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);
        AccumulateDamageDealt(results);
        BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);

        foreach (BattleActionResult result in results)
        {
            string line = BattleLogFormatter.FormatSkillAttack(result.Attacker, result.Target, skill.SkillName, pureBaseDamage, baseDamage, result.DamageApplied, typeMultiplier, timedOutcome);
            BattleHUDController.Instance.AppendBattleLog(line);
        }

        float attackBurstFill = BattleConfig.BurstFillPerSkillUse + (timedSuccess ? BattleConfig.BurstFillPerTimedInputSuccess : 0f);
        AddBurstFill(attacker, attackerSlotIndex, attackBurstFill);

        // Return-to-origin now lives here (2026-08-14, moved out of ResolveMeleeBeatSequence's own
        // tail alongside the same fix for Defense below) rather than changing offense's own timing —
        // this is still the LAST thing that happens for offense, same as before. Ranged skips it
        // (never moved in the first place, same reasoning as the Instant Strike hop fix).
        if (HasApproachBeat(skill))
            yield return StartCoroutine(BeatSequenceRunner.RunReturn(attacker, attackerSlotIndex, true, restingLeft, restingTop));
    }

    /// <summary>
    /// The Attack beat's own resolution when an ENEMY-side participant is the attacker (a melee
    /// skill used against the player) — mirrors ResolveEnemyDamageAction's Dodge/Parry defense block
    /// (RunDefenseTimedInput -> DamageCalculator -> BattleEngine -> logging -> burst fill -> Parry
    /// counter-attack) with the projectile/deflect-projectile visuals removed in favor of
    /// RunMeleeLungeAndFlash — a Parry counter has nothing to "bounce back" when attacker and
    /// defender are already adjacent (melee), so the counter's damage applies immediately instead of
    /// awaiting a deflect travel time.
    ///
    /// 2026-08-12 follow-up (Group 1 archetypes): preEmptive/preEmptiveOutcome/preEmptiveWasPerfect
    /// let ResolveMeleeBeatSequence hand in an outcome already resolved during the WindupReal beat's
    /// own ring — when supplied, this method skips opening a SECOND ring here and just applies the
    /// given outcome. Both default to the Reactive case (no outcome supplied), so this method's
    /// behavior is unchanged for every pre-existing skill.
    /// </summary>
    private IEnumerator ResolveMeleeAttackBeatDefense(BattleParticipant attacker, BattleParticipant target, int targetSlotIndex, SkillData skill,
        float restingLeft, float restingTop, bool preEmptive = false, BattleHUDController.DefenseOutcome? preEmptiveOutcome = null, bool preEmptiveWasPerfect = false)
    {
        int attackerSlotIndex = _state.EnemySide.IndexOf(attacker);

        BattleHUDController.DefenseOutcome outcome;
        bool wasPerfect;

        if (preEmptive && preEmptiveOutcome.HasValue)
        {
            outcome = preEmptiveOutcome.Value;
            wasPerfect = preEmptiveWasPerfect;
        }
        else
        {
            float dodgeToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
                target.RuntimeData.EffectiveStat(StatType.Instinct), target.RuntimeData.bondPercent);
            float parryToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
                target.RuntimeData.EffectiveStat(StatType.Instinct), target.RuntimeData.bondPercent);

            yield return StartCoroutine(BattleHUDController.Instance.RunDefenseTimedInput(
                targetSlotIndex, $"DEFEND — {attacker.DisplayName}! Left-Click Dodge · Right-Click Parry",
                dodgeToleranceHalfWidth, parryToleranceHalfWidth, TimedInputConfig.MarkerSweepDuration));

            outcome = BattleHUDController.Instance.LastDefenseOutcome;
            wasPerfect = BattleHUDController.Instance.LastDefenseWasPerfect;
        }

        bool defended = outcome != BattleHUDController.DefenseOutcome.Miss;
        bool isParry = outcome == BattleHUDController.DefenseOutcome.Parry;
        float defenseMultiplier = defended ? 0f : 1f;
        if (defended) EventBus.Raise_TimedInputSuccess(target.RuntimeData);

        int pureBaseDamage = DamageCalculator.ComputeBaseDamage(attacker, target, DamageCategory.Physical, BattleConfig.PlaceholderSkillPower);
        int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, DamageCategory.Physical, BattleConfig.PlaceholderSkillPower);
        float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

        // Ranged + PreEmptive: the ring already resolved on the tell during Windup (2026-08-13
        // reorder) — the real strike's projectile fires HERE, explicitly dispatched against the
        // already-known outcome (RunResolvedRangedProjectileDefense's own doc comment explains why
        // RunDefenseTimedInput's usual auto-dispatch can't apply here). Melee (or a hypothetical
        // future Reactive ranged skill, none exist yet) still gets the lunge+flash.
        // showHitFlash: !defended — a hit that was Dodged or Parried never lands, so it shouldn't
        // show a hit-flash at all; also sidesteps the Dodge-dissolve race this fixed (see
        // RunMeleeLungeAndFlash's own doc comment).
        bool ranged = !HasApproachBeat(skill);
        if (!ranged || !preEmptive)
            yield return StartCoroutine(RunMeleeLungeAndFlash(attacker, attackerSlotIndex, false, targetSlotIndex, true, showHitFlash: !defended));
        else
            yield return StartCoroutine(RunResolvedRangedProjectileDefense(attacker, attackerSlotIndex, targetSlotIndex, outcome));

        BattleEngine.QueueBasicAttack(_state, attacker, target, defenseMultiplier, baseDamage);
        List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);

        // Perfect Dodge/Parry reward, same as ResolveEnemyDamageAction (2026-08-05, user-directed —
        // see DECISIONS.md -> [Combat]).
        if (defended && wasPerfect) target.RestoreAura(BattleConfig.PerfectDefenseAuraRestore);

        BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);

        LogDefenseResult(results, attacker, target, pureBaseDamage, baseDamage, typeMultiplier, defended, isParry);
        if (defended && wasPerfect) BattleHUDController.Instance.AppendBattleLog($"{target.DisplayName} restores Aura!");

        if (!defended) AddBurstFill(target, targetSlotIndex, BattleConfig.BurstFillPerHitTaken);

        // 2026-08-14 fix (user: "when parrying the slash attack from the enemy the parry animation
        // comes out first before the enemy jumps back to its original position. Have it bounce back
        // after attack then shoot the parry animation") — the enemy's own return-to-origin now
        // happens HERE, right after its attack fully resolves (visual, damage, log) and BEFORE the
        // isParry counter block below, instead of back in ResolveMeleeBeatSequence's tail (which ran
        // AFTER the whole counter-attack, including its damage and messaging, had already played).
        // Ranged skips it, same reasoning as offense/Instant Strike — never moved in the first place.
        if (!ranged)
            yield return StartCoroutine(BeatSequenceRunner.RunReturn(attacker, attackerSlotIndex, false, restingLeft, restingTop));

        if (isParry && attacker.IsAlive)
        {
            // Ranged: the incoming projectile is still held (its own ring already resolved earlier
            // during Windup, and RunResolvedRangedProjectileDefense above deliberately left it
            // unresolved on Parry — see that method's doc comment) — bounce it back for real via
            // the same deflect visual the classic ranged path uses, awaiting its travel before the
            // counter damage lands.
            //
            // 2026-08-14 fix (user: "the parry works now and the battle log says it, but i didnt
            // see a projectile go out") — melee had NO visual at all for the counter, damage just
            // applied silently (the doc comment above used to justify this as "nothing to bounce
            // back when already adjacent," but that left the counter invisible rather than just
            // skipping the deflect). Now plays the same lunge+flash every other melee hit uses, with
            // the roles reversed — the DEFENDER (target) lunges into the now-vulnerable attacker.
            if (ranged)
            {
                float deflectTravelDuration = BattleHUDController.Instance.ResolveParryDeflect(GetPrimalTypeOrDefault(target));
                if (deflectTravelDuration > 0f) yield return new WaitForSeconds(deflectTravelDuration);
            }
            else
            {
                yield return StartCoroutine(RunMeleeLungeAndFlash(target, targetSlotIndex, true, attackerSlotIndex, false));
            }

            int counterPureBaseDamage = DamageCalculator.ComputeBaseDamage(target, attacker, DamageCategory.Physical, DamageCalculator.BasicAttackPower);
            int counterDamage = DamageCalculator.ComputeDamage(target, attacker, _typeChart, DamageCategory.Physical, DamageCalculator.BasicAttackPower);
            float counterTypeMultiplier = DamageCalculator.ComputeTypeMultiplier(target, attacker, _typeChart);

            BattleEngine.QueueBasicAttack(_state, target, attacker, damageMultiplier: 1f, counterDamage);
            List<BattleActionResult> counterResults = BattleEngine.ResolveQueuedActions(_state);
            AccumulateDamageDealt(counterResults);
            BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
            LogResults(counterResults, counterPureBaseDamage, counterDamage, counterTypeMultiplier, offenseOutcome: null);
        }

        yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
            defended ? $"{target.DisplayName} defended!" : $"{target.DisplayName} was hit!",
            BattleConfig.AutoMessageDurationSeconds));

        if (isParry && attacker.IsAlive)
        {
            yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                $"{target.DisplayName} counter-attacks!", BattleConfig.AutoMessageDurationSeconds));
        }
    }

    /// <summary>
    /// Owns the ENTIRE resolution for a Metronome/Jitter skill (SkillData.StackingRhythm != None,
    /// 2026-08-13, user's detailed spec) — bypasses the generic BeatSequence engine
    /// (ResolveMeleeBeatSequence) entirely rather than trying to express a dynamic, per-battle-
    /// growing beat count inside a system built around a fixed, pre-authored beat list.
    ///
    /// Flow: (1) warning hop; (2) N alternating dash-forward/dash-back beats, N = the attacker's
    /// current stack tier for this skill + 1 (BattleParticipant.GetStackingRhythmTier) — beat 1
    /// dashes forward, beat 2 back, beat 3 forward, etc., each beat's dash running concurrently with
    /// its own ring, both sized to that beat's duration (Metronome: one fixed value repeated, the
    /// "1..2..3..4" feel; Jitter: a repeating [long, short, short] duration/dash-distance cycle, the
    /// "1...2.3.4" feel — see BeatSequenceConfig); (3) the first beat that fails stops the sequence
    /// immediately — the whole attack whiffs, no damage, stack tier unchanged (user: "on miss you
    /// should stay at the existing counter that you're at"); (4) every beat succeeding fires a
    /// stronger payoff projectile (damage scaled by BeatSequenceConfig.StackingRhythmTierDamageStep
    /// per tier above the first — "start at low damage, then ramp") and advances the stack tier by
    /// one; (5) a final translateX-to-0 safety reset regardless of outcome — BeatSequenceRunner.
    /// RunRhythmDash is purely cosmetic (transform-level, never touches style.left — 2026-08-13
    /// redesign, user: "the dash should just be a visual thing... should not actually change the
    /// players position"), so there's no real position drift to undo, just a mid-combo "forward"
    /// beat's leftover offset to clear.
    ///
    /// "Beat succeeds" is inverted between offense and defense, both matching this file's existing
    /// "the RESPONDING side doing well continues the encounter" shape: offense = the ATTACKER's own
    /// ring lands Good/Perfect; defense = the enemy's rhythm only continues if the PLAYER FAILS to
    /// Dodge/Parry that beat — a successful player defense on ANY beat interrupts the enemy's combo
    /// immediately (treated as the whole attack whiffing, same reward as a normal full avoidance).
    /// No separate Parry-counter mechanic for a mid-combo Parry in this pass — a deliberate scope
    /// cut, not an oversight; TODO: pending design if that turns out to matter in practice.
    /// </summary>
    private IEnumerator ResolveStackingRhythmAttack(BattleParticipant attacker, int attackerSlotIndex, bool attackerIsPlayerSide,
        BattleParticipant target, int targetSlotIndex, bool targetIsPlayerSide, SkillData skill)
    {
        UnityEngine.UIElements.VisualElement attackerElement = BattleHUDController.Instance.GetStageCreatureElement(attackerSlotIndex, attackerIsPlayerSide);

        int tier = attacker.GetStackingRhythmTier(skill);
        int beatsRequired = tier + 1;
        bool isJitter = skill.StackingRhythm == StackingRhythmType.Jitter;

        yield return StartCoroutine(BeatSequenceRunner.RunWarningHop(attacker, attackerSlotIndex, attackerIsPlayerSide));

        bool comboSucceeded = true;
        int beatReached = 0;

        for (int beatIndex = 1; beatIndex <= beatsRequired; beatIndex++)
        {
            beatReached = beatIndex;
            bool forward = beatIndex % 2 == 1;

            float beatDuration;
            float beatDashOffsetPx;
            if (isJitter)
            {
                int patternIndex = (beatIndex - 1) % BeatSequenceConfig.JitterBeatDurationsSeconds.Length;
                beatDuration = BeatSequenceConfig.JitterBeatDurationsSeconds[patternIndex];
                beatDashOffsetPx = BeatSequenceConfig.JitterBeatDashOffsetsPx[patternIndex];
            }
            else
            {
                beatDuration = BeatSequenceConfig.MetronomeBeatDurationSeconds;
                beatDashOffsetPx = BeatSequenceConfig.MetronomeDashOffsetPx;
            }

            // Fire-and-forget — the dash runs concurrently with the ring below, both sized to
            // beatDuration, so the dash's arrival coincides with the ring closing.
            StartCoroutine(BeatSequenceRunner.RunRhythmDash(attacker, attackerSlotIndex, attackerIsPlayerSide, forward, beatDashOffsetPx, beatDuration));

            bool beatSucceeded;
            if (attackerIsPlayerSide)
            {
                float goodToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                    TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
                    attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);
                float perfectToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                    TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
                    attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);

                yield return StartCoroutine(BattleHUDController.Instance.RunTimedInput(
                    $"{skill.SkillName} {beatIndex}/{beatsRequired} — {attacker.DisplayName}", goodToleranceHalfWidth, perfectToleranceHalfWidth, beatDuration));

                beatSucceeded = BattleHUDController.Instance.LastTimedInputSuccess;
            }
            else
            {
                float dodgeToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                    TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
                    target.RuntimeData.EffectiveStat(StatType.Instinct), target.RuntimeData.bondPercent);
                float parryToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                    TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
                    target.RuntimeData.EffectiveStat(StatType.Instinct), target.RuntimeData.bondPercent);

                yield return StartCoroutine(BattleHUDController.Instance.RunDefenseTimedInput(
                    targetSlotIndex, $"DEFEND {beatIndex}/{beatsRequired} — {attacker.DisplayName}! Left-Click Dodge · Right-Click Parry",
                    dodgeToleranceHalfWidth, parryToleranceHalfWidth, beatDuration));

                beatSucceeded = BattleHUDController.Instance.LastDefenseOutcome == BattleHUDController.DefenseOutcome.Miss;
            }

            if (!beatSucceeded)
            {
                comboSucceeded = false;
                break;
            }
        }

        if (comboSucceeded)
        {
            float tierMultiplier = 1f + (beatsRequired - 1) * BeatSequenceConfig.StackingRhythmTierDamageStep;

            int pureBaseDamage = DamageCalculator.ComputeBaseDamage(attacker, target, DamageCategory.Physical, BattleConfig.PlaceholderSkillPower);
            int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, DamageCategory.Physical, BattleConfig.PlaceholderSkillPower);
            float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

            if (attackerIsPlayerSide)
                yield return StartCoroutine(RunResolvedRangedProjectileOffense(attacker, attackerSlotIndex, targetSlotIndex));
            else
                yield return StartCoroutine(RunResolvedRangedProjectileDefense(attacker, attackerSlotIndex, targetSlotIndex, BattleHUDController.DefenseOutcome.Miss));

            BattleEngine.QueueBasicAttack(_state, attacker, target, tierMultiplier, baseDamage);
            List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);
            if (attackerIsPlayerSide) AccumulateDamageDealt(results);
            BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);

            foreach (BattleActionResult result in results)
            {
                string line = BattleLogFormatter.FormatStackingRhythmAttack(result.Attacker, result.Target, skill.SkillName, beatsRequired, tierMultiplier, pureBaseDamage, baseDamage, result.DamageApplied, typeMultiplier);
                BattleHUDController.Instance.AppendBattleLog(line);
            }

            attacker.AdvanceStackingRhythmTier(skill);

            if (attackerIsPlayerSide)
            {
                EventBus.Raise_TimedInputSuccess(attacker.RuntimeData);
                AddBurstFill(attacker, attackerSlotIndex, BattleConfig.BurstFillPerSkillUse + BattleConfig.BurstFillPerTimedInputSuccess);
            }
            else
            {
                AddBurstFill(target, targetSlotIndex, BattleConfig.BurstFillPerHitTaken);
            }
        }
        else
        {
            BattleHUDController.Instance.AppendBattleLog(BattleLogFormatter.FormatStackingRhythmWhiff(attacker, skill.SkillName, beatReached, beatsRequired));

            if (!attackerIsPlayerSide)
            {
                // The player successfully broke the enemy's rhythm — same "avoided a hit" reward
                // path every other defense success in this file uses.
                EventBus.Raise_TimedInputSuccess(target.RuntimeData);
            }
            else
            {
                // Player's own combo dropped — still counts as having used the skill for burst-fill
                // purposes, same as any other miss, just without the timed-input-success bonus.
                AddBurstFill(attacker, attackerSlotIndex, BattleConfig.BurstFillPerSkillUse);
            }
        }

        // 2026-08-13 redesign (user: "the dash should just be a visual thing... should not actually
        // change the players position") — RunRhythmDash now only ever touches the cosmetic
        // `transform` translateX, never `style.left`, so there's no real position left to restore
        // via RunReturn (and no reason to replay its vertical hop-arc flourish either, after just
        // fixing the exact same "extra hop" complaint for Instant Strike). This is purely a safety
        // net for a combo that broke mid-sequence on a "forward" beat, leaving translateX non-zero.
        VisualElementTweening.TweenTranslateX(attackerElement, 0f, BeatSequenceConfig.AttackLungeDurationSeconds);
        yield return new WaitForSeconds(BeatSequenceConfig.AttackLungeDurationSeconds);
    }

    /// <summary>
    /// Owns the ENTIRE resolution for a Multi-Hit Volley skill (SkillData.VolleyRingSequence
    /// non-empty, Attack_Pattern_Directive Part 5 Group 2, 2026-08-14) — bypasses BeatSequence/
    /// StackingRhythm entirely, same "own dedicated resolution path" shape as
    /// ResolveStackingRhythmAttack, but structurally different in one key way: EVERY hit resolves
    /// and deals damage independently (a miss on hit 3 doesn't cancel hits 4-8 — Part 5: "several
    /// small hits in sequence, each its own small window. Tests rhythm/consistency"), unlike
    /// Metronome/Jitter's all-or-nothing combo gate.
    ///
    /// Flow: (1) one warning hop, once, not per-hit (user: "its one warning for player"); (2) for
    /// each hit in VolleyRingSequence, in order: a STRICTLY SEQUENTIAL small dash-forward (awaited,
    /// same race-fixed VisualElementTweening.TweenTranslateX-backed dash Metronome/Jitter already
    /// use), then that hit's own ring+projectile+damage resolution (RunVolleyHit) is started
    /// FIRE-AND-FORGET — NOT awaited — before a strictly sequential dash-back plays and the loop
    /// moves on to the next hit's dash-forward (user: "dash shoot, return to position, dash shoot,
    /// return to position... this would happen fast bc the number of projectile should be coming
    /// out in quick succession to feel like a volley"). Because the dash cadence
    /// (BeatSequenceConfig.VolleyDashLegDurationSeconds x2 per hit) is faster than any one hit's own
    /// ring sweep, several hits' rings end up open/animating around the target concurrently (user:
    /// "the number of rings shown should match the number of projectiles airborne"); (3) once every
    /// hit has fired its dash, wait for every hit's fire-and-forget resolution to actually finish
    /// before returning; (4) flush every hit's battle-log line in one batch, in order (user: "let
    /// the damage calculate on ring input, then for the battle log just add them all at the end" —
    /// damage itself already applied per-hit, in real time, inside RunVolleyHit; only the LOG lines
    /// are deferred to this final step).
    /// </summary>
    private IEnumerator ResolveMultiHitVolleyAttack(BattleParticipant attacker, int attackerSlotIndex, bool attackerIsPlayerSide,
        BattleParticipant target, int targetSlotIndex, bool targetIsPlayerSide, SkillData skill)
    {
        UnityEngine.UIElements.VisualElement attackerElement = BattleHUDController.Instance.GetStageCreatureElement(attackerSlotIndex, attackerIsPlayerSide);
        UnityEngine.UIElements.VisualElement targetElement = BattleHUDController.Instance.GetStageCreatureElement(targetSlotIndex, targetIsPlayerSide);

        yield return StartCoroutine(BeatSequenceRunner.RunWarningHop(attacker, attackerSlotIndex, attackerIsPlayerSide));

        IReadOnlyList<CompassPoint> sequence = skill.VolleyRingSequence;
        int hitCount = sequence.Count;
        var hitLogLines = new string[hitCount];
        var pendingHits = new List<Coroutine>(hitCount);

        BattleHUDController.Instance.BeginVolleyInputSession();

        for (int i = 0; i < hitCount; i++)
        {
            CompassPoint point = sequence[i];
            float ringDuration = i < skill.VolleyRingDurationsSeconds.Count
                ? skill.VolleyRingDurationsSeconds[i]
                : BeatSequenceConfig.VolleyDefaultRingDurationSeconds;
            // Derived split — first half requires left-click (circle marker), second half requires
            // right-click (square marker, 2026-08-15 — shape replaced the earlier converging/
            // expanding animation-direction encoding, which needed watching motion to read).
            bool requiresLeftClick = i < hitCount / 2;
            // 2026-08-15 fix (user: "the 2nd two seem to have a bigger delay than the 1st two
            // attacks") — forward and back legs are looked up independently now, not one shared
            // value for both, so a pause authored on one hit's forward leg doesn't also stretch its
            // own return (which would otherwise create a second, unintended pause before the next
            // hit — see SkillData.VolleyDashBackDurationsSeconds' own doc comment for the full math).
            float dashForwardDuration = i < skill.VolleyDashForwardDurationsSeconds.Count
                ? skill.VolleyDashForwardDurationsSeconds[i]
                : BeatSequenceConfig.VolleyDashLegDurationSeconds;
            float dashBackDuration = i < skill.VolleyDashBackDurationsSeconds.Count
                ? skill.VolleyDashBackDurationsSeconds[i]
                : BeatSequenceConfig.VolleyDashLegDurationSeconds;

            yield return StartCoroutine(BeatSequenceRunner.RunRhythmDash(
                attacker, attackerSlotIndex, attackerIsPlayerSide, forward: true, BeatSequenceConfig.VolleyDashOffsetPx, dashForwardDuration));

            int hitNumber = i + 1;
            int hitIndex = i;
            pendingHits.Add(StartCoroutine(RunVolleyHit(
                attacker, attackerSlotIndex, attackerIsPlayerSide, target, targetSlotIndex, targetIsPlayerSide,
                targetElement, point, requiresLeftClick, ringDuration, skill, hitNumber, hitCount, hitLogLines, hitIndex)));

            yield return StartCoroutine(BeatSequenceRunner.RunRhythmDash(
                attacker, attackerSlotIndex, attackerIsPlayerSide, forward: false, BeatSequenceConfig.VolleyDashOffsetPx, dashBackDuration));
        }

        foreach (Coroutine pending in pendingHits) yield return pending;

        BattleHUDController.Instance.EndVolleyInputSession();

        foreach (string line in hitLogLines)
        {
            if (!string.IsNullOrEmpty(line)) BattleHUDController.Instance.AppendBattleLog(line);
        }

        VisualElementTweening.TweenTranslateX(attackerElement, 0f, BeatSequenceConfig.AttackLungeDurationSeconds);
        yield return new WaitForSeconds(BeatSequenceConfig.AttackLungeDurationSeconds);
    }

    /// <summary>
    /// Resolves ONE hit of a Multi-Hit Volley — fired fire-and-forget by ResolveMultiHitVolleyAttack's
    /// own loop, so several of these can be running concurrently. Writes its formatted battle-log
    /// line into hitLogLines[hitIndex] instead of appending it directly (the caller flushes the
    /// whole array once every hit has resolved — user: "battle log calculation can happen at the
    /// final step") — damage itself still applies immediately, right here, the instant this hit's
    /// own ring resolves, not deferred with the log.
    ///
    /// Offense: launches the projectile immediately (holdForOutcome:false — Volley hits always
    /// connect, same as every other offense ranged path; Good/Perfect/Miss only scale damage, same
    /// as RunTimedInput elsewhere), opens this hit's own ring at its compass position, and once the
    /// ring resolves, applies damage via the same DamageCalculator/BattleEngine.QueueBasicAttack
    /// path every other offense attack in this file uses — a Miss still applies
    /// TimedInputConfig.MissDamageMultiplier, never zero, matching that same existing convention.
    ///
    /// Defense: dispatch wiring only — a documented TODO stub. CombatVfxController._held is an
    /// explicit single-slot field ("only one projectile is ever held at once"), so several
    /// concurrently-held Volley projectiles aren't supported yet; giving this skill to an enemy
    /// today would silently clobber one hit's held projectile with the next's. Scoped out of this
    /// pass deliberately (see the plan's Context section) rather than bundled in — a dedicated
    /// follow-up, consistent with Group 2's own "one dedicated pass each" framing.
    /// </summary>
    private IEnumerator RunVolleyHit(BattleParticipant attacker, int attackerSlotIndex, bool attackerIsPlayerSide,
        BattleParticipant target, int targetSlotIndex, bool targetIsPlayerSide, UnityEngine.UIElements.VisualElement targetElement,
        CompassPoint point, bool requiresLeftClick, float ringDuration, SkillData skill, int hitNumber, int hitCount,
        string[] hitLogLines, int hitIndex)
    {
        if (attackerIsPlayerSide)
        {
            float goodToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
                attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);
            float perfectToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
                attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);

            // 2026-08-15 fix (user: "the timing of the projectiles isnt sync up... i want to
            // maintain the projectile speed as it is then adjust the release or showing of the ring
            // accordingly") — the ring's own "perfect" instant does NOT land at the very end of its
            // sweep (see ComputeVolleyRingSweepDuration's own doc comment), so displaying the ring
            // with sweepDuration == ringDuration directly made its perfect moment land well before
            // the projectile actually arrived. The projectile keeps traveling for exactly ringDuration
            // (the skill-authored, already-tuned value, untouched — "maintain the projectile speed as
            // it is"); only the RING's own displayed sweep is stretched so its perfect instant
            // coincides with that same arrival moment.
            float ringSweepDuration = BattleHUDController.Instance.ComputeVolleyRingSweepDuration(ringDuration);
            BattleHUDController.Instance.LaunchRangedBeatSequenceProjectile(
                attackerSlotIndex, true, targetSlotIndex, false, GetPrimalTypeOrDefault(attacker), ringDuration, holdForOutcome: false);

            var outcome = new BattleHUDController.VolleyRingOutcome();
            yield return StartCoroutine(BattleHUDController.Instance.RunVolleyRingOffense(
                targetElement, point, requiresLeftClick, goodToleranceHalfWidth, perfectToleranceHalfWidth, ringSweepDuration, outcome));

            BattleHUDController.OffenseOutcome quality = outcome.Quality;
            float attackMultiplier = quality switch
            {
                BattleHUDController.OffenseOutcome.Perfect => TimedInputConfig.PerfectDamageMultiplier,
                BattleHUDController.OffenseOutcome.Good => TimedInputConfig.GoodDamageMultiplier,
                _ => TimedInputConfig.MissDamageMultiplier
            };
            // 2026-08-15 (user: "lower the damage for the volley") — a normal Miss/Good/Perfect
            // multiplier alone would let a full 8-hit connect deal ~8x a single normal attack's
            // damage, since nothing else accounts for 8 independent hits vs 1. See this constant's
            // own doc comment for the target ratios.
            attackMultiplier *= BeatSequenceConfig.VolleyPerHitDamageMultiplier;

            int pureBaseDamage = DamageCalculator.ComputeBaseDamage(attacker, target, DamageCategory.Physical, BattleConfig.PlaceholderSkillPower);
            int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, DamageCategory.Physical, BattleConfig.PlaceholderSkillPower);
            float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

            BattleEngine.QueueBasicAttack(_state, attacker, target, attackMultiplier, baseDamage);
            List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);
            AccumulateDamageDealt(results);
            BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);

            foreach (BattleActionResult result in results)
            {
                hitLogLines[hitIndex] = BattleLogFormatter.FormatVolleyHit(result.Attacker, result.Target, skill.SkillName, hitNumber, hitCount, pureBaseDamage, baseDamage, result.DamageApplied, typeMultiplier, quality);
            }

            if (quality != BattleHUDController.OffenseOutcome.Miss) EventBus.Raise_TimedInputSuccess(attacker.RuntimeData);

            // ASSUMPTION (flagged, not confirmed) — full per-hit burst fill, same amount as any
            // other single skill use, so an 8-hit Volley builds burst substantially faster than one
            // normal cast. A real balance call for later, easy to scale down if it proves too fast.
            float hitBurstFill = BattleConfig.BurstFillPerSkillUse + (quality != BattleHUDController.OffenseOutcome.Miss ? BattleConfig.BurstFillPerTimedInputSuccess : 0f);
            AddBurstFill(attacker, attackerSlotIndex, hitBurstFill);
        }
        else
        {
            // TODO: blocked on CombatVfxController._held becoming a handle-keyed collection instead
            // of a single slot (see this method's own doc comment) — not safe to hold multiple
            // concurrent projectiles yet, so this branch intentionally does not launch one.
            var outcome = new BattleHUDController.VolleyRingOutcome();
            yield return StartCoroutine(BattleHUDController.Instance.RunVolleyRingDefense(
                targetElement, point, TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.ParryToleranceHalfWidth, ringDuration, outcome));
        }
    }

    /// <summary>
    /// Charge & Release's offense resolution (2026-08-17, Attack_Pattern_Directive Part 5 Group 2's
    /// second/third archetypes) — single-hit, no FIFO/pooling needed (only ever one hold-input skill
    /// resolving at a time, same "never both at once" reasoning as the classic single-ring system).
    /// Fires the projectile at the EXACT instant of release (user: "fire at the moment of release"),
    /// via RunChargeReleaseInput's onRelease callback, not after the whole gesture resolves.
    ///
    /// Damage is a deliberate departure from every other skill's Miss handling (user, this session):
    /// a Miss on EITHER the press or release instant (BattleHUDController.LastChargeReleaseCancelled)
    /// cancels the attack for ZERO damage — no DamageCalculator/BattleEngine call at all, just a
    /// "fizzled" log line. A pass on both instants uses a CONTINUOUS multiplier interpolated from
    /// LastChargeReleaseQuality (0..1) between the existing Good/Perfect damage multipliers, not a
    /// discrete tier — "a perfect on the start and the release means the most damage. Things in
    /// between as long as passing should have a damage range."
    /// </summary>
    private IEnumerator ResolveChargeReleaseAttack(BattleParticipant attacker, int attackerSlotIndex, bool attackerIsPlayerSide,
        BattleParticipant target, int targetSlotIndex, bool targetIsPlayerSide, SkillData skill)
    {
        UnityEngine.UIElements.VisualElement attackerElement = BattleHUDController.Instance.GetStageCreatureElement(attackerSlotIndex, attackerIsPlayerSide);

        yield return StartCoroutine(BeatSequenceRunner.RunWarningHop(attacker, attackerSlotIndex, attackerIsPlayerSide));

        float tellSeconds = skill.ChargeReleaseTellSeconds > 0f ? skill.ChargeReleaseTellSeconds : BeatSequenceConfig.ChargeReleaseDefaultTellSeconds;
        float targetHoldSeconds = skill.ChargeReleaseTargetHoldSeconds > 0f ? skill.ChargeReleaseTargetHoldSeconds : BeatSequenceConfig.ChargeReleaseDefaultTargetHoldSeconds;

        PrimalType colorType = GetPrimalTypeOrDefault(attacker);
        void OnRelease() => BattleHUDController.Instance.LaunchRangedBeatSequenceProjectile(
            attackerSlotIndex, attackerIsPlayerSide, targetSlotIndex, targetIsPlayerSide, colorType,
            BeatSequenceConfig.ResolvedProjectileTravelSeconds, holdForOutcome: false);

        yield return StartCoroutine(BattleHUDController.Instance.RunChargeReleaseInput(
            attackerSlotIndex, $"{skill.SkillName} — {attacker.DisplayName}", tellSeconds, targetHoldSeconds, OnRelease));

        bool cancelled = BattleHUDController.Instance.LastChargeReleaseCancelled;
        float combinedQuality = BattleHUDController.Instance.LastChargeReleaseQuality;

        if (cancelled)
        {
            BattleHUDController.Instance.AppendBattleLog(BattleLogFormatter.FormatChargeReleaseFizzle(attacker, skill.SkillName));
        }
        else
        {
            float attackMultiplier = Mathf.Lerp(TimedInputConfig.GoodDamageMultiplier, TimedInputConfig.PerfectDamageMultiplier, combinedQuality);

            int pureBaseDamage = DamageCalculator.ComputeBaseDamage(attacker, target, DamageCategory.Physical, BattleConfig.PlaceholderSkillPower);
            int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, DamageCategory.Physical, BattleConfig.PlaceholderSkillPower);
            float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

            BattleEngine.QueueBasicAttack(_state, attacker, target, attackMultiplier, baseDamage);
            List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);
            AccumulateDamageDealt(results);
            BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);

            foreach (BattleActionResult result in results)
            {
                BattleHUDController.Instance.AppendBattleLog(BattleLogFormatter.FormatChargeReleaseHit(
                    result.Attacker, result.Target, skill.SkillName, pureBaseDamage, baseDamage, result.DamageApplied, typeMultiplier,
                    BattleHUDController.Instance.LastChargeReleasePressOutcome, BattleHUDController.Instance.LastChargeReleaseReleaseOutcome));
            }

            EventBus.Raise_TimedInputSuccess(attacker.RuntimeData);
            AddBurstFill(attacker, attackerSlotIndex, BattleConfig.BurstFillPerSkillUse + BattleConfig.BurstFillPerTimedInputSuccess);
        }

        VisualElementTweening.TweenTranslateX(attackerElement, 0f, BeatSequenceConfig.AttackLungeDurationSeconds);
        yield return new WaitForSeconds(BeatSequenceConfig.AttackLungeDurationSeconds);
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
    private IEnumerator ResolveBuiltInMove(BattleParticipant attacker, int attackerSlotIndex, BuiltInMoveType move, BattleParticipant target,
        int? destinationLane = null, int? destinationPosition = null)
    {
        switch (move)
        {
            case BuiltInMoveType.Move:
            {
                // No damage, no timed input, no target — repositions the caster to a different
                // formation slot (2026-08-12, user: "5 positions across a lane... only one
                // position can be filled at a time"). destinationLane/Position always non-null
                // here in practice (called only from HandleMoveConfirmed, itself only invoked by
                // BattleHUDController.MoveConfirmed after a successful marker drop — see
                // OnMoveDragPointerUp) — falls back to the caster's OWN current slot (a no-op
                // move) if somehow missing, rather than throwing.
                int lane = LaneMovementSystem.ClampLane(destinationLane ?? attacker.LaneIndex);
                int position = LaneMovementSystem.ClampPosition(destinationPosition ?? attacker.PositionIndex);

                // Safety re-check, not just UI-level prevention — FormationGridPicker already
                // disables occupied cells, but party state could in principle change between the
                // grid being built and the click landing (nothing does today, this is defense in
                // depth matching the project's general "don't trust the UI alone" posture).
                bool occupied = FormationSystem.IsSlotOccupied(
                    _state.PlayerSide.FindAll(p => p.IsAlive && p != attacker).ConvertAll(p => (p.LaneIndex, p.PositionIndex)),
                    lane, position);

                if (occupied)
                {
                    BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} couldn't move there — already occupied!");
                    yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                        "Slot already occupied!", BattleConfig.AutoMessageDurationSeconds));
                    yield break;
                }

                attacker.LaneIndex = lane;
                attacker.PositionIndex = position;
                BattleHUDController.Instance.RefreshPlayerLaneLayout(_state.PlayerSide);
                BattleHUDController.Instance.AppendBattleLog($"{attacker.DisplayName} moves to row {lane}, position {position}!");
                AddBurstFill(attacker, attackerSlotIndex, BattleConfig.BurstFillPerSkillUse);

                yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                    $"{attacker.DisplayName} repositions!", BattleConfig.AutoMessageDurationSeconds));
                yield break;
            }

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

                // Offensive action command (Combat_Directive Part 4): a timed press boosts this
                // attack's damage. Good/Perfect bands mirror Defend's own Dodge/Parry tolerances
                // exactly (2026-08-11, user-directed — see DECISIONS.md -> [Combat]), scaled by
                // the attacker's own Instinct + bond.
                float goodToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                    TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent,
                    attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);
                float perfectToleranceHalfWidth = TimedInputConfig.ComputeToleranceHalfWidth(
                    TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent,
                    attacker.RuntimeData.EffectiveStat(StatType.Instinct), attacker.RuntimeData.bondPercent);

                // Launches the projectile now, concurrently with the ring below — see
                // ResolveSkillAction's identical pattern for the full rationale.
                int targetSlotIndex = _state.EnemySide.IndexOf(target);
                float sweepDuration = BattleHUDController.Instance.LaunchSyncedProjectile(
                    attackerSlotIndex, true, targetSlotIndex, false, GetPrimalTypeOrDefault(attacker), holdForOutcome: false);
                yield return StartCoroutine(BattleHUDController.Instance.RunTimedInput(
                    $"YOUR ATTACK — {attacker.DisplayName}", goodToleranceHalfWidth, perfectToleranceHalfWidth, sweepDuration));

                // TODO: pending design — Perfect currently only grants bonus damage; see
                // ResolveSkillAction's identical TODO for why a bonus-status reward is deferred.
                float attackMultiplier = BattleHUDController.Instance.LastOffenseOutcome switch
                {
                    BattleHUDController.OffenseOutcome.Perfect => TimedInputConfig.PerfectDamageMultiplier,
                    BattleHUDController.OffenseOutcome.Good => TimedInputConfig.GoodDamageMultiplier,
                    _ => TimedInputConfig.MissDamageMultiplier
                };
                if (BattleHUDController.Instance.LastTimedInputSuccess) EventBus.Raise_TimedInputSuccess(attacker.RuntimeData);

                // Real formula (Step 3): (AttackerStat / DefenderStat) x skillPower x
                // primalTypeMultiplier. Basic Attack is treated as Physical (Force/Guard) — real
                // skill categories arrive with the skill tree framework (Step 4).
                int pureBaseDamage = DamageCalculator.ComputeBaseDamage(attacker, target, DamageCategory.Physical, DamageCalculator.BasicAttackPower);
                int baseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, DamageCategory.Physical, DamageCalculator.BasicAttackPower);
                float typeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

                BattleEngine.QueueBasicAttack(_state, attacker, target, attackMultiplier, baseDamage);
                List<BattleActionResult> results = BattleEngine.ResolveQueuedActions(_state);
                AccumulateDamageDealt(results);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                LogResults(results, pureBaseDamage, baseDamage, typeMultiplier, BattleHUDController.Instance.LastOffenseOutcome);

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

        // Metronome/Jitter beat-stack badge (2026-08-14, user: "add a counter for that skill...
        // so that the player knows what beat counter they are on") — reuses the same skill-wheel
        // badge as the combo rules above rather than a separate UI element. Shows
        // GetStackingRhythmTier + 1: both "how many successful casts you've stacked" AND "how many
        // ring beats the NEXT cast will require" (ResolveStackingRhythmAttack's own
        // beatsRequired = tier + 1), so one number answers both questions. Unlike the combo rules
        // above, shown from the very first use (badge "1") rather than only once >= 2 — the beat
        // count is core information for timing the next cast, not a bonus streak indicator.
        //
        // 2026-08-14 follow-up (user: "the counter for each jitter and metronome seems to disappear
        // when using another skill... i want that to be persistent even when switching skills
        // between turns") — originally only badged justUsedSkill, so ClearAllSkillComboCounters
        // above wiped a stacking-rhythm skill's badge the moment any OTHER skill was used, with
        // nothing re-setting it since that skill wasn't the one just cast. Each skill's own tier is
        // already persisted per-battle (BattleParticipant._stackingRhythmTiers), so instead of
        // keying off justUsedSkill, badge EVERY equipped stacking-rhythm skill on every refresh —
        // every turn re-applies every badge from that persisted state regardless of which skill
        // triggered the refresh, so a skill's badge survives switching to and using others.
        if (_skillDatabase != null)
        {
            foreach (string guid in attacker.RuntimeData.equippedSkillGuids)
            {
                if (!_skillDatabase.TryGetByGuid(guid, out SkillData equippedSkill)) continue;
                if (equippedSkill.StackingRhythm == StackingRhythmType.None) continue;

                int stackCount = attacker.GetStackingRhythmTier(equippedSkill) + 1;
                BattleHUDController.Instance.SetSkillComboCounter(attackerSlotIndex, equippedSkill, stackCount);
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

        // Melee Beat Sequence branch (Attack_Pattern_Directive Part 7, 2026-08-11) — an enemy skill
        // with a non-empty BeatSequence runs the full authored beat list (Approach/Windup/Attack/
        // Return, via ResolveMeleeAttackBeatDefense for its Attack beat — includes the same Dodge/
        // Parry defense and Parry counter-attack this method's own block below runs) instead of the
        // single-beat Dodge/Parry+projectile flow below.
        if (skillOrNull != null && skillOrNull.StackingRhythm != StackingRhythmType.None)
        {
            int stackingAttackerSlotIndex = _state.EnemySide.IndexOf(attacker);
            yield return StartCoroutine(ResolveStackingRhythmAttack(attacker, stackingAttackerSlotIndex, false, target, targetSlotIndex, true, skillOrNull));
            yield break;
        }

        if (skillOrNull != null && skillOrNull.BeatSequence != null && skillOrNull.BeatSequence.Count > 0)
        {
            int meleeAttackerSlotIndex = _state.EnemySide.IndexOf(attacker);
            yield return StartCoroutine(ResolveMeleeBeatSequence(attacker, meleeAttackerSlotIndex, false, target, targetSlotIndex, true, skillOrNull));
            yield break;
        }

        // 2026-08-14: dispatches correctly, but the defense-side body inside ResolveMultiHitVolleyAttack/
        // RunVolleyHit is a documented stub — CombatVfxController._held is a single-slot field, so
        // multiple concurrently-held Volley projectiles aren't actually supported yet. Don't equip
        // this skill on any enemy loadout until that follow-up lands (see RunVolleyHit's own doc
        // comment).
        if (skillOrNull != null && skillOrNull.VolleyRingSequence != null && skillOrNull.VolleyRingSequence.Count > 0)
        {
            int volleyAttackerSlotIndex = _state.EnemySide.IndexOf(attacker);
            yield return StartCoroutine(ResolveMultiHitVolleyAttack(attacker, volleyAttackerSlotIndex, false, target, targetSlotIndex, true, skillOrNull));
            yield break;
        }

        // Sustained Pressure ("hold-to-guard", 2026-08-17) — a new outcome value INSIDE this same
        // single-beat defense flow, not a wholly separate dedicated coroutine like the three
        // branches above. It's fundamentally the same decision point (one incoming hit, one
        // defensive response) with a third, graduated outcome alongside Dodge/Parry/Miss, so it
        // reuses this method's own damage-application tail below instead of duplicating it.
        if (skillOrNull != null && skillOrNull.HoldInputArchetype == HoldInputArchetype.SustainedPressure)
        {
            int sustainedAttackerSlotIndex = _state.EnemySide.IndexOf(attacker);
            yield return StartCoroutine(BeatSequenceRunner.RunWarningHop(attacker, sustainedAttackerSlotIndex, false));

            float sustainedTellSeconds = skillOrNull.SustainedPressureTellSeconds > 0f ? skillOrNull.SustainedPressureTellSeconds : BeatSequenceConfig.SustainedPressureDefaultTellSeconds;
            float sustainedHoldSeconds = skillOrNull.SustainedPressureHoldSeconds > 0f ? skillOrNull.SustainedPressureHoldSeconds : BeatSequenceConfig.SustainedPressureDefaultHoldSeconds;

            // Held (holdForOutcome:true) same as the classic Dodge/Parry flow below — resolved by
            // RunSustainedPressureInput itself the instant its outcome is known (ResolveHitProjectile,
            // same "fire the real cue immediately" pattern RunDefenseTimedInput already established).
            BattleHUDController.Instance.LaunchSyncedProjectile(
                sustainedAttackerSlotIndex, false, targetSlotIndex, true, GetPrimalTypeOrDefault(attacker), holdForOutcome: true);
            yield return StartCoroutine(BattleHUDController.Instance.RunSustainedPressureInput(
                targetSlotIndex, $"GUARD — {attacker.DisplayName}! Hold to brace, release when it ends", sustainedTellSeconds, sustainedHoldSeconds));

            BattleHUDController.DefenseOutcome sustainedOutcome = BattleHUDController.Instance.LastDefenseOutcome;
            bool sustainedDefended = sustainedOutcome != BattleHUDController.DefenseOutcome.Miss;
            bool sustainedWasPerfect = BattleHUDController.Instance.LastDefenseWasPerfect;
            float blockFraction = BattleHUDController.Instance.LastGuardBlockFraction;
            // Generalizes cleanly once Guard is a real enum member: full avoidance (0f) for
            // Dodge/Parry, full damage (1f) for Miss, and now a graduated value in between for Guard
            // — no other downstream logic in this method needed to change.
            float sustainedDefenseMultiplier = sustainedOutcome == BattleHUDController.DefenseOutcome.Guard ? 1f - blockFraction : (sustainedDefended ? 0f : 1f);
            if (sustainedDefended) EventBus.Raise_TimedInputSuccess(target.RuntimeData);

            DamageCategory sustainedCategory = isNamedTreeSkill ? PlaceholderSkillResolver.Resolve(skillOrNull).Category : DamageCategory.Physical;
            int sustainedPower = isNamedTreeSkill ? BattleConfig.PlaceholderSkillPower : DamageCalculator.BasicAttackPower;
            int sustainedPureBaseDamage = DamageCalculator.ComputeBaseDamage(attacker, target, sustainedCategory, sustainedPower);
            int sustainedBaseDamage = DamageCalculator.ComputeDamage(attacker, target, _typeChart, sustainedCategory, sustainedPower);
            float sustainedTypeMultiplier = DamageCalculator.ComputeTypeMultiplier(attacker, target, _typeChart);

            BattleEngine.QueueBasicAttack(_state, attacker, target, sustainedDefenseMultiplier, sustainedBaseDamage);
            List<BattleActionResult> sustainedResults = BattleEngine.ResolveQueuedActions(_state);

            if (sustainedDefended && sustainedWasPerfect) target.RestoreAura(BattleConfig.PerfectDefenseAuraRestore);

            BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);

            if (sustainedOutcome == BattleHUDController.DefenseOutcome.Guard)
            {
                foreach (BattleActionResult result in sustainedResults)
                {
                    BattleHUDController.Instance.AppendBattleLog(BattleLogFormatter.FormatGuardOutcome(
                        result.Attacker, result.Target, sustainedPureBaseDamage, sustainedBaseDamage, result.DamageApplied, sustainedTypeMultiplier, blockFraction * 100f));
                }
            }
            else
            {
                LogDefenseResult(sustainedResults, attacker, target, sustainedPureBaseDamage, sustainedBaseDamage, sustainedTypeMultiplier, sustainedDefended, isParry: false);
            }
            if (sustainedDefended && sustainedWasPerfect) BattleHUDController.Instance.AppendBattleLog($"{target.DisplayName} restores Aura!");

            // Scaled partial burst fill (2026-08-17, this session's explicit decision) — a Guard
            // still let SOME damage through, unlike a full Dodge/Parry, so it grants a fill scaled
            // by the UNBLOCKED damage fraction instead of the flat full/zero gate every other
            // outcome here uses.
            if (sustainedOutcome == BattleHUDController.DefenseOutcome.Guard)
                AddBurstFill(target, targetSlotIndex, BattleConfig.BurstFillPerHitTaken * (1f - blockFraction));
            else if (!sustainedDefended)
                AddBurstFill(target, targetSlotIndex, BattleConfig.BurstFillPerHitTaken);

            yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
                sustainedOutcome == BattleHUDController.DefenseOutcome.Guard ? $"{target.DisplayName} braces against the attack!"
                    : sustainedDefended ? $"{target.DisplayName} defended!" : $"{target.DisplayName} was hit!",
                BattleConfig.AutoMessageDurationSeconds));

            yield break;
        }

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
        int pureBaseDamage = DamageCalculator.ComputeBaseDamage(attacker, target, category, power);
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
        // fired there too.
        LogDefenseResult(results, attacker, target, pureBaseDamage, baseDamage, typeMultiplier, defended, isParry);
        if (defended && wasPerfect) BattleHUDController.Instance.AppendBattleLog($"{target.DisplayName} restores Aura!");

        // "Taking hits" (GDD §9.3's third Evolution Burst fill source) — only when the hit
        // actually landed (defended means 0 damage was applied, so a full Dodge/Parry
        // shouldn't count as "taking a hit").
        if (!defended) AddBurstFill(target, targetSlotIndex, BattleConfig.BurstFillPerHitTaken);

        // Parry's reward half: a successful Parry triggers an automatic counter-attack against the
        // now-vulnerable attacker. Resolved HERE, immediately — before the "defended!" message
        // below, not after it — so the deflect projectile bounces back the instant Parry lands
        // instead of sitting stuck, idle-pulsing at the player's position for the length of that
        // message (2026-08-11, user-directed: "if I parry on success just have the attack bounce
        // back" [right away] — a fixed-duration "how long it stays stuck" knob for different parry
        // types is a nice future addition, tracked as a TODO below, but isn't needed for this).
        // Damage still applies exactly when the projectile visually connects — ResolveParryDeflect
        // returns its real travel duration so this can await it, same "await the travel time, then
        // apply damage" pattern every other damage-application path in this file already uses. No
        // timing check on the counter itself — it's a bonus for landing the harder input, not
        // another QTE. The launch/await runs whenever isParry is true (not gated on
        // attacker.IsAlive) so the held projectile is always resolved/released the moment Parry
        // happens and never left stuck, even in the (currently unreachable) case the attacker
        // somehow died first; the counter's actual damage still only applies if attacker.IsAlive.
        if (isParry)
        {
            // TODO: pending design — a configurable "how long the projectile stays stuck before
            // bouncing" per parry type/quality (e.g. a Perfect parry could hold longer for a bigger
            // punish window) would go here. For now it always bounces immediately on any Parry.
            float deflectTravelDuration = BattleHUDController.Instance.ResolveParryDeflect(GetPrimalTypeOrDefault(target));
            if (deflectTravelDuration > 0f) yield return new WaitForSeconds(deflectTravelDuration);

            if (attacker.IsAlive)
            {
                int counterPureBaseDamage = DamageCalculator.ComputeBaseDamage(target, attacker, DamageCategory.Physical, DamageCalculator.BasicAttackPower);
                int counterDamage = DamageCalculator.ComputeDamage(target, attacker, _typeChart, DamageCategory.Physical, DamageCalculator.BasicAttackPower);
                float counterTypeMultiplier = DamageCalculator.ComputeTypeMultiplier(target, attacker, _typeChart);

                BattleEngine.QueueBasicAttack(_state, target, attacker, damageMultiplier: 1f, counterDamage);
                List<BattleActionResult> counterResults = BattleEngine.ResolveQueuedActions(_state);
                AccumulateDamageDealt(counterResults);
                BattleHUDController.Instance.RefreshBars(_state.PlayerSide, _state.EnemySide);
                LogResults(counterResults, counterPureBaseDamage, counterDamage, counterTypeMultiplier, offenseOutcome: null);
            }
        }

        yield return StartCoroutine(BattleHUDController.Instance.ShowTimedMessage(
            defended ? $"{target.DisplayName} defended!" : $"{target.DisplayName} was hit!",
            BattleConfig.AutoMessageDurationSeconds));

        if (isParry && attacker.IsAlive)
        {
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

    /// <summary>Appends one battle log line per resolved offensive action — normally exactly one, since only a single attack is ever queued per call site. offenseOutcome is null for the Parry counter-attack, which runs no timing check at all. pureBaseDamage/damageAfterType are the same for every entry in results (there's only ever one per call site) — passed through to FormatAttack's base/type/timing breakdown.</summary>
    private static void LogResults(List<BattleActionResult> results, int pureBaseDamage, int damageAfterType, float typeMultiplier, BattleHUDController.OffenseOutcome? offenseOutcome)
    {
        foreach (BattleActionResult result in results)
        {
            string line = BattleLogFormatter.FormatAttack(result.Attacker, result.Target, pureBaseDamage, damageAfterType, result.DamageApplied, typeMultiplier, offenseOutcome);
            BattleHUDController.Instance.AppendBattleLog(line);
        }
    }

    /// <summary>Appends one battle log line per resolved defended action, via FormatDefenseOutcome instead of FormatAttack.</summary>
    private static void LogDefenseResult(List<BattleActionResult> results, BattleParticipant attacker, BattleParticipant target, int pureBaseDamage, int damageAfterType, float typeMultiplier, bool defended, bool isParry)
    {
        foreach (BattleActionResult result in results)
        {
            string line = BattleLogFormatter.FormatDefenseOutcome(result.Attacker, result.Target, pureBaseDamage, damageAfterType, result.DamageApplied, typeMultiplier, defended, isParry);
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
