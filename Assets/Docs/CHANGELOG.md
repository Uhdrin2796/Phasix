# Phasix — Development Changelog
One entry per session. Format: `[DATE] vX.X — What was built / decided / changed.`
Kept in version control. Claude Code reads this to avoid re-litigating settled work.

---

## Format
```
[YYYY-MM-DD] Phase X — Brief title
- Built: [what scripts/systems/assets were created]
- Decided: [any implementation choices made]
- Changed: [anything revised from prior plan]
- Blocked: [anything that couldn't be completed and why]
- Next: [what the next session should pick up]
```

---

[2026-08-12] Phase 3 follow-up — Playtest hooks: enemy Slash + live lane/row cycler
- **Context:** User, after confirming the corrected vertical-row Approach looked right: "Can we give
  the enemy a slash to try out so i can see if it works the same way and i can block. Also is there a
  way that i can test the lane movement." Two gaps: `ResolveMeleeAttackBeatDefense` (the enemy-
  attacking path, reusing the existing Dodge/Parry system) had been built but never live-tested — no
  wild enemy had ever been equipped with a Beat Sequence skill; and there was no way to preview a
  creature at a different row without a real Approach/Return actually moving it there.
- **Built:**
  - `WildSpawnSystem.ApplyDebugSingleSkillOverride(runtime, skillDatabase, skillName)` — clears a wild
    instance's learned/equipped skills down to just the named skill, so `EnemyAI.ChooseSkill` has
    nothing else to randomly pick instead. Mirrors `GameManager.ApplyDebugPlaytestLoadout`'s existing
    pattern for the player side, but on the enemy-spawn path instead
    (`EncounterTrigger.OnEnable` -> `WildSpawnSystem.CreateWildInstance`), since enemies never pass
    through the player's debug hook at all — confirmed by reading both spawn paths this session.
  - `EncounterTrigger` gained a new Debug Override Inspector toggle, `_debugForceSlashOnly` (default
    ON), calling the above with `"Slash"` right after the normal seeded loadout. Every wild encounter
    the user walks into now attacks with Slash only, until this is unchecked.
  - `DebugLaneCycler.cs` (new, `Assets/Scripts/Combat/`) — TEMPORARY manual-test tool, same convention
    as `DebugMovementPresetCycler`: `[ / ]` in Play Mode moves the slot-0 player creature's
    `BattleParticipant.LaneIndex` one row at a time and re-applies the real layout math via a new
    public `BattleHUDController.RefreshPlayerLaneLayout(playerSide)` (thin wrapper around the existing
    private `LayoutPlayerStageCreaturesByLane` + `ApplyLaneLayout`), so `GetLaneScreenTop`/
    `GetDepthScale`'s row position and depth scale can be previewed live without needing a real Beat
    Sequence to trigger movement — no formation/positioning mechanic exists yet to drive `LaneIndex`
    any other way. Attached to the same GameObject as `BattleManager` in `BattleScene_Main`
    (`[RequireComponent(typeof(BattleManager))]`, resolved via `GetComponent`, not
    `FindFirstObjectByType`). New `BattleManager.PlayerSide` read-only accessor added for this and any
    future debug tooling that needs the live player side.
- **Verified:** 317/317 EditMode tests pass (no new tests — both additions are debug-only scaffolding
  with no pure-math surface to unit test), clean compile via `read_console`. Live `execute_code`
  checks against a real battle (`BattleTransition.StartWildBattle`, `WildEncounterCreature` auto-
  engage disabled first to avoid the known duplicate-battle-scene race — see `LESSONS_LEARNED.md`):
  confirmed the override left the enemy with exactly one equipped skill resolving to "Slash", and
  `EnemyAI.ChooseSkill` returned Slash on 10/10 calls (Damage intent) with nothing else to compete
  against it; confirmed `BattleManager.PlayerSide` resolves correctly and `RefreshPlayerLaneLayout`
  applied to a manually-changed `LaneIndex` produced `style.top`/`style.scale` exactly matching
  `LaneMovementSystem.GetLaneScreenTop`/`GetDepthScale` for that row (150/1.05 for row 2), and that
  `DebugLaneCycler` is present and enabled on the live `BattleManager` instance. Did **not** live-fire
  a full enemy Approach/Windup/Attack/Return sequence end-to-end this session (the `[`/`]` keypress
  itself and `ResolveMeleeAttackBeatDefense`'s Dodge/Parry resolution were not exercised through a real
  frame-by-frame Play Mode pass) — same reasoning as prior sessions' honest caveats about coroutine-
  timing verification being unreliable via scripted round-trips; left for the user's own live test,
  which is the explicit point of both hooks.
- **Next:** User to live-test: walk into a wild encounter, let it attack, confirm Dodge/Parry works
  against a melee Slash the same way it does against a ranged attack; press `[`/`]` mid-battle to
  preview row depth. A normal interactive Play Mode pass (Editor focused, real clicks) remains the one
  verification style this session still can't do itself.
- **Follow-up (same session, user-directed):** Live-tested the `[`/`]` row preview and reported "the
  size seems a little small for the lanes... make the lanes wider by about double," "make the first
  lane above the top of the flee/end turn buttons," and asked for a `\`-key toggle to show/hide the
  lane boundaries as white dotted lines so the spacing could actually be checked visually. Built:
  `LaneMovementSystem.LaneRowHeightPx` doubled 30f -> 60f (also doubles `RowRangeHeightPx`, 180 ->
  360, since it derives from `LaneRowHeightPx`). New `LaneGuideOverlay.cs` (`VisualElement` subclass,
  custom-drawn via `Painter2D.SetDashPattern` + a round line cap — UI Toolkit has no native dashed-
  border USS property) — a single instance parented directly under `Stage` (sibling of
  `PlayerStageArea`/`EnemyStageArea`, not inside either) so one set of 8 boundary lines covers both
  sides at once, since both sides share the same anchor/box-sizing formula. New
  `BattleHUDController.SetLaneGuideLinesVisible(bool)` computes the 8 row-boundary Y values (Stage-
  local space, converted from `LaneMovementSystem`'s box-local `GetLaneScreenTop` values via
  `PlayerStageArea`'s own resolved anchor/height) and toggles the overlay. `DebugLaneCycler` now also
  handles the `\` keypress (checked before the player-side early-return, since the guide lines don't
  need a player side to exist).
- **Did the anchor need to move too?** No — measured live before changing anything: doubling
  `LaneRowHeightPx` alone still leaves the frontmost row (Lane 1, forced via `execute_code` as the
  worst case) ~130px of clearance above the End Turn button's top edge (`worldBound` measured at
  y=618.6..701.4 for the creature vs. y=832 for the button, on the 1920x1080 canvas) — comfortably
  "above" already, so `.stage-side`'s `top: 480px` anchor in `BattleHUD.uss` was left unchanged rather
  than moved for a problem that measurement showed wasn't actually going to happen.
- **Verified:** 317/317 EditMode tests pass, clean compile. Live-verified the worldBound/resolvedStyle
  staleness gotcha again mid-check (a `RefreshPlayerLaneLayout` call and an immediate `worldBound` read
  in the SAME `execute_code` call showed a stale position with only the new scale applied — position
  needs a frame to resolve, same as earlier sessions' findings) — resolved by splitting into a second
  round-trip, which then matched the hand-computed value exactly. Screenshotted the result (guide
  lines on, default row 4, both sides symmetric, front row clear of the buttons) and sent it to the
  user directly per their "let me see that" ask, rather than only describing it.
- **Follow-up #2 (same session, user-directed):** "move the lanes down more and make the lanes even
  bigger," then mid-turn, "make sure the lane lines are also the furthest back i want everything else
  on the layer in front." Built: `LaneMovementSystem.LaneRowHeightPx` raised again 60f -> 90f (1.5x).
  `.stage-side`'s `top` anchor in `BattleHUD.uss` moved 480px -> 500px (a modest +20px, NOT the +60px
  first tried — see the clearance conflict below). `BattleHUDController.SetLaneGuideLinesVisible` now
  calls `_laneGuideOverlay.SendToBack()` on every call (not just at creation), guaranteeing it stays
  the backmost `Stage` child regardless of future sibling changes — live-confirmed via `execute_code`
  reading `Stage`'s child order: `LaneGuideOverlay` at index 0, ahead of `PlayerStageArea`,
  `EnemyStageArea`, `EndTurnButton`, `FleeButton`, and the drag-line visual.
- **Clearance conflict, measured not assumed:** "bigger" and "further down" pull against the same
  budget — growing `LaneRowHeightPx` alone (before touching the anchor) already ate most of the
  previous ~130px clearance above the End Turn/Flee buttons, down to ~40.6px at the ORIGINAL 480px
  anchor. The first attempt at "move down more" (+60px, anchor 540px) was live-measured (forcing
  Lane 1, the worst case, via `execute_code`) at **-19.4px clearance — an actual overlap** with the
  End Turn button. Backed off to a +20px shift (anchor 500px), re-measured at +20.6px clearance —
  positive but tight. Left as-is rather than silently picking a different tradeoff (e.g. keeping the
  full +60px shift and shrinking `LaneRowHeightPx` back down) since the user asked for both
  explicitly; flagged to the user that pushing further down from here will need to trade off against
  row size, or the buttons themselves would need to move.
- **Verified:** 317/317 EditMode tests pass, clean compile. Live-verified via `execute_code` in two
  passes (accounting for the same worldBound-lags-a-frame staleness noted above): first the -19.4px
  overlap at the rejected +60px shift, then +20.6px clearance at the accepted +20px shift, both with
  Lane 1 forced as the worst case. Screenshotted the final state (bigger rows, shifted down, guide
  lines confirmed drawing behind the stage creatures/buttons) and sent it to the user.
- **Follow-up #3 (same session, user-directed) — row resize + diagonal melee Approach:** "the lanes
  can be reduced by around 15% then also move the L1 up by about 20px" (freeing up the clearance
  budget from Follow-up #2), plus a separate, more substantial report: "when i move to a different
  lane the projectiles shoot without any issues, but on the melee it just moves horizontal within the
  lane then melee animation comes out. I was expecting it to move diagonally to get to the in front
  of the target then the melee comes out."
  - **Sizing:** `LaneMovementSystem.LaneRowHeightPx` reduced ~15% (90f -> 76.5f); `.stage-side`'s `top`
    anchor in `BattleHUD.uss` moved back to 480px (the same value as before Follow-up #2, but for a
    different reason — the row shrink freed up room again, this isn't a revert). Re-measured Lane 1's
    worst-case clearance above the End Turn button live: 81.1px, well clear.
  - **Diagonal Approach (real gap, not a request for polish):** the closing lunge only ever tweened
    `style.left` — fine when attacker and target share a row (the only case tested so far, since both
    sides default to Lane 4), but an attacker on a different row than its target would slide sideways
    at its OWN row's height and pop the Attack from there, never actually lining up with the target.
    Projectile-based attacks never had this problem since a projectile just flies to the target's real
    on-screen position regardless of row. Fixed: `BeatSequenceRunner.RunApproach` now ALSO tweens
    `style.top` toward `LaneMovementSystem.GetLaneScreenTop(target.LaneIndex, ...)`, concurrently with
    the existing horizontal tween — both sides share the identical row-to-`top` mapping (no mirroring),
    so no cross-container worldBound math is needed for this axis, unlike the horizontal one. A no-op
    when rows already match, so same-row attacks are visually unchanged. New
    `VisualElementTweening.TweenTop` (mirrors `TweenLeft`). `RunReturn` gained a `restingTop` parameter
    (captured once by `BattleManager.ResolveMeleeBeatSequence`, alongside the existing `restingLeft`)
    so the automatic Return-to-origin undoes the same detour afterward — the attacker's actual
    `LaneIndex`/depth scale are never touched by any of this, purely a visual line-up.
  - **Verified:** 317/317 EditMode tests pass, clean compile. Live-verified via `execute_code`: forced
    the player creature to row 2 and the enemy to row 6, ran `BeatSequenceRunner.RunApproach` directly
    — attacker's `style.top` moved from 382.5 (its own row 2) to exactly 76.5 (the target's row 6)
    while `style.left` closed the horizontal gap concurrently, confirming real diagonal movement; ran
    `RunReturn` with the captured resting values and confirmed both `left` and `top` landed back on
    their exact pre-Approach values. Screenshotted the resized rows with guide lines on and sent it to
    the user.
- **Follow-up #4 (same session, user-directed) — scale now follows the diagonal, not just position:**
  "as the phasix moves across the diagonal i was expecting the size of the phasix to scale with the
  vertical position... it maintained the same size from its original position. And same on the way
  back, it should return to its original size after its attack." The diagonal fix above moved `top`
  but left scale untouched, so an attacker crossing rows would visually detour to the target's height
  while staying its own original size the whole time — same class of bug as the original "why does
  the size change during purely left-right movement" report from earlier this session, just the
  mirror image (size NOT changing when it should have this time). Built: `RunApproach` now ALSO
  tweens `style.scale` to `LaneMovementSystem.GetDepthScale(target.LaneIndex)`, in parallel with the
  `top`/`left` tweens over the identical duration — since all three share DOTween's same default
  easing curve, a parallel start-to-end scale tween is visually identical to continuously deriving
  scale from the live `top` value every frame, without resurrecting the more complex continuous-
  position-to-scale machinery (`GetDepthScaleFromLeft`/`TweenLeftWithDepthScale`) removed earlier
  this session — that removal isn't being reversed, this is a simpler approach that happens to
  produce the same result. `RunReturn` now tweens scale back to
  `LaneMovementSystem.GetDepthScale(attacker.LaneIndex)` (the attacker's own canonical row) alongside
  the position return. Also fixed a related bug this surfaced on read: `RunWindup` was computing its
  squash baseline from `attacker.LaneIndex` (the attacker's OWN row) rather than the element's actual
  current on-screen scale — harmless before this fix (attacker never visually left its own row), but
  would have snapped the squash to the wrong value the instant Windup started once Approach began
  changing scale. Changed to read the element's live `resolvedStyle.scale` instead, correct whether
  or not an Approach ran before it.
- **Verified:** 317/317 EditMode tests pass, clean compile. Live-verified via `execute_code`: forced
  player to row 7 (scale 0.55) and enemy target to row 3 (scale 0.95), ran `RunApproach` — attacker's
  scale grew from 0.55 to exactly 0.95 in step with `top` reaching the target's row; ran `RunReturn` —
  scale returned to exactly 0.55 (its own row 7), position returned to its own row's `top` too.
- **Follow-up #5 (same session, user-directed) — narrowed the depth-scale range, row spacing
  untouched:** "keep lane size the same, but make the scale at L7 to be 0.85 and scale at L1 to be
  1.10." `LaneMovementSystem.MaxDepthScale` (Lane 1) 1.15f -> 1.10f, `MinDepthScale` (Lane 7) 0.55f ->
  0.85f — `LaneRowHeightPx` (76.5f, row spacing/position) deliberately left alone, since this ask was
  scoped to size only. Live-verified: `GetDepthScale(1)=1.10`, `GetDepthScale(7)=0.85`,
  `GetDepthScale(4)=0.975` (midpoint, as expected), `LaneRowHeightPx` confirmed unchanged at 76.5.
  317/317 EditMode tests still pass (no test hardcodes the old scale constants, only relative
  ordering/monotonicity, so nothing needed updating there). Screenshotted Lane 1 next to a default-row
  enemy — the front-to-back size difference now reads noticeably subtler than the old 0.55–1.15 range
  — and sent it to the user.

---

[2026-08-12] Phase 3 follow-up #4 — Recommended build order for Part 5/6 skill archetypes
- **Context:** User asked which of `Attack_Pattern_Directive`'s still-unbuilt Part 5/6 archetypes to
  build in what order for the next session, and whether any should be built together vs. isolated.
  Worked through it in chat, then asked to persist it rather than leave it stuck in conversation.
- **Built (docs only):** Added a "Recommended build order" subsection to `Attack_Pattern_Directive
  _v0_1_0.md` Part 1, right after the existing Status tracker. Groups the 13 unbuilt archetypes/
  systems by how much they reuse existing infrastructure: Group 1 (Instant Strike, Feint, Metronome/
  Jitter, Direct Projectile) reuses Slash's Windup-Real/Fake and ring-timing almost as-is; Group 2
  (Multi-Hit Volley, then Charge & Release + Sustained Pressure together) each need one new timing
  primitive; Group 3 (Zone/Positional, then Split Attention) builds directly on this session's
  formation-grid work for the defender's response; Group 4 (Counter-Bait, Windup-Applies-Status) are
  small but isolated to verify no regression; Multi-Turn Buildup, Lane Displacement Attack, and
  Strike Points are called out to save for last as the genuinely bigger lifts.
- **Next:** Start Group 1 next session — cheapest, validates that ranged skills don't need an
  Approach beat before anything else gets built on top of that assumption.

---

[2026-08-12] Phase 3 follow-up #3 — Synced updated directives, tracked enemy-side position gap
- **Context:** User asked to verify the formation grid/Move work built this session actually aligns
  with the two updated directive docs they'd attached earlier (`Combat_Directive_v0_1_0_1.md`,
  `Attack_Pattern_Directive_v0_1_0.md`, both dated 2026-08-12) — important since upcoming skill
  authoring work needs to build against a directive that matches reality. Comparison surfaced one
  real gap (enemy-side positions) and confirmed everything else lines up; user asked for it to be
  clearly tracked (roadmap/decisions) and the docs pushed.
- **Synced:** The two directive files actually in `Assets/Docs/` were still the stale 2026-08-11
  versions — the updated Downloads copies had only ever been read into context, never committed.
  Both now replaced with the current content. `DOCUMENT_INDEX.md`'s status notes updated to match
  (Combat_Directive's position-exclusive 7×5 refinement, Attack_Pattern_Directive's Move/formation
  build status and the enemy-side gap).
- **Confirmed aligned:** 7×5 lane×position occupancy model (position-exclusive, lane-shared) —
  exact match to what was built. Movement cost being context-dependent (Move costs a full turn) —
  matches `Combat_Directive` Part 3's cost-agnostic design. Depth scaling/anchor-lane model —
  unaffected. UI interaction model (drag icon) — directives don't specify one, no conflict either way.
- **Gap found and tracked, not fixed this session:** `Attack_Pattern_Directive`'s 2026-08-12 errata
  says enemy-side position support is "un-deferred" (needs building alongside player-side) — the
  actual implementation is player-side only, confirmed as a deliberate choice during this session's
  planning (`AskUserQuestion`: "Leave deferred for now"). Full rationale, the concrete list of what's
  needed to close the gap (enemy stage rendering as an array, per-enemy position layout, target
  selection UI, multi-enemy spawning — the real gate, enemy AI repositioning logic, the still-unbuilt
  reactive Lane Displacement Attack dodge, starting formation assignment), and the revisit trigger
  (a specific skill/encounter that needs 2+ simultaneous enemies) are all in `DECISIONS.md` -> [Combat]
  "Enemy-side position support." Also added as a row in `Roadmap_v2.md`'s "What Is Not In This
  Roadmap" table.
- **Next:** Start real skill content against `Attack_Pattern_Directive` Part 5/6 archetypes — nothing
  is blocking that now. Enemy-side positions stay parked until a specific skill genuinely needs
  multiple simultaneous enemies (see the DECISIONS.md entry above for the full build-out list when
  that day comes).

---

[2026-08-12] Phase 3 follow-up #2 — Nameplate shift tuning (1.25 columns) + cold-boot save/fallback bugfix
- **Context:** User asked to tune `PlayerNameplateClearanceShiftPx` down twice after live-testing
  (2 columns felt like an overcompensation → 1.5 → 1.25, final), then separately reported an empty
  party on a normal Editor Play press ("im just press the play button on the unity editor and ive
  never had an issue... at first it was showing no phasix in the party").
- **Fixed [SAVE-001]:** Reproduced directly via `manage_editor play` — zero `[GameManager]` console
  output on a fresh boot, empty `PartySystem` despite valid, resolvable saves already on disk and a
  correctly-assigned fallback starter species. Root cause: Unity's Editor doesn't fire
  `SceneManager.sceneLoaded` for the scene already open when Play is pressed (only for runtime
  `LoadScene` calls, like the debug "New Game" reload) — a standalone, pre-existing Editor
  limitation, unrelated to this session's earlier `LoadSceneMode.Single` guard. `GameManager` now
  also runs the boot sequence from `Start()` (fires reliably on a cold boot, unlike `sceneLoaded`),
  alongside the existing `HandleSceneLoaded` path (still needed for reloads after the first, since
  `Start()` never re-fires for this `DontDestroyOnLoad` survivor). Full writeup in KNOWN_ISSUES.md.
- **Verified:** 339/339 EditMode tests pass. Live-verified: before the fix, a fresh Play press
  produced zero `[GameManager]` logs and an empty party; after, `[GameManager] Loaded save slot 0...`
  logs correctly and the real saved Phasix resolves into slot 0. `read_console` clean.

---

[2026-08-12] Phase 3 follow-up — Formation grid bugfixes + Move redesigned as drag-to-stage-position icon
- **Context:** User live-tested the formation grid pass directly below and reported three real bugs/
  requests, with two updated design docs attached (`Combat_Directive_v0_1_0_1.md`,
  `Attack_Pattern_Directive_v0_1_0.md`, confirming the 7x5 exclusive-position model as locked): the
  grid rendered upside-down; formation didn't survive into battle (party stacked on top of each
  other); and Move should stop being a skill-ring orb and become a dedicated per-creature icon you
  drag to a destination, with legal destinations hidden until a drag starts and aligned to the real
  stage. Went through Plan Mode (2 Explore agents traced root causes, a Plan agent drafted the shape,
  2 rounds of `AskUserQuestion` resolved: dedicated Move icon as the drag handle not the creature
  sprite itself; drag-time grid aligned to real stage coordinates not a centered popup; Move keeps
  triggering combo-streak/`SkillUsed` bookkeeping like it used to). Full design/rationale recorded in
  DECISIONS.md's `[Combat] Formation grid orientation/persistence bugfixes...` entry — see that entry
  for the complete file-by-file breakdown; this entry summarizes.
- **Fixed:**
  - Grid orientation: `FormationGridPicker.Build`'s row loop reversed (7→1) to match the real stage's
    front=bottom/back=top convention; cells now carry `userData = (lane, position)`.
  - Formation persistence, two independent compounding causes: `GameManager.HandleSceneLoaded` now
    ignores non-`Single` scene loads (the additive battle-scene load was silently re-applying the
    last save over the live party); `PhasixSaveData` now actually serializes
    `preferredLaneIndex`/`preferredPositionIndex` (previously dropped entirely, silently resetting
    everyone to the default slot on any save/load).
- **Built (Move redesign):** Move removed from the equippable Standard skill pool entirely. New
  always-present `.move-icon` per player creature (visible for alive, not-yet-acted creatures at turn
  start); dragging it reveals 35 stage-aligned position markers built from the same
  `GetLaneScreenTop`/`GetPositionOffsetPx` formulas real creatures use (correct orientation for free,
  hidden again on drop/reject/cancel); dropping on a free marker fires a new
  `BattleHUDController.MoveConfirmed` event handled directly by `BattleManager.HandleMoveConfirmed`
  (bypasses the normal skill-selection wait-loop, reuses `ResolveBuiltInMove`'s existing `Move` case
  unchanged). `PlayerTurn`'s open-ring tracking promoted to a field so a Move completing mid-ring-open
  reconciles cleanly. New `LaneMovementSystem.PlayerNameplateClearanceShiftPx` (a follow-up user
  request: "the 2 columns on the right are interfering with the health hud... its the player
  nameplates... move it over by 2 columns", applied identically to creature and marker positioning,
  grid width/spacing left untouched per "Dont shrink the grid"). Old click-to-confirm centered
  overlay plumbing deleted (not flagged off) — fully superseded.
- **Verified:** Full EditMode suite green (new `FormationGridPickerTests.cs`; `SaveSystemTests`'s
  round-trip extended with non-default lane/position values; `WildSpawnSystemTests` extended to
  assert Move is never learned/equipped). Live `execute_code` verification against a real 3-member
  party seeded to distinct (lane, position) slots and a real additive battle transition (worked around
  the known duplicate-`BattleScene_Main` race by filtering for the populated `PlayerSide` instance):
  confirmed `PlayerSide[i].LaneIndex`/`PositionIndex` matched exactly what was set pre-battle;
  inspected the marker overlay's raw style values directly (correct front/back orientation, full
  5-column spacing preserved, shifted clear of the nameplates); screenshotted the live stage
  mid-drag-equivalent state confirming no nameplate overlap and correct marker/creature layering.
  `read_console` clean (only pre-existing unrelated A* Pathfinding Project network-check warnings).
- **Next:** Enemy-side position support remains deferred (confirmed out of scope — only one enemy
  stage slot exists, no multi-enemy battles yet); `PlayerNameplateClearanceShiftPx`'s doc comment
  flags the opposite-sign mirror a future enemy-side pass would need. Move's reposition is still
  instant/non-animated, same precedent as Charge/Heal/Regen.

---

[2026-08-12] Phase 3 — Exclusive 5-position formation grid: pre-battle picker + in-battle Move skill
- **Context:** User, after confirming the melee framework "feels good so far": no way to pre-slot
  which lane each of 3 party members starts a battle in, and no way to move lanes during battle.
  Discussion landed on: movement should cost the whole turn (like a skill), "jump to any row in one
  turn" rather than one-row-per-turn (user's choice, via AskUserQuestion), then a further
  clarification — "lets just have 5 positions across a lane. Then you can preset which position you
  want to be in... only one position can be filled at a time... similar to how the skill wheel is
  set up, but instead it would look like a 7 by 5 grid... The inbattle move could use the same...
  system that the preslot uses for selection."
- **Built:**
  - `LaneMovementSystem.cs` gained `PositionsPerLane` (5), `DefaultStartingPosition` (3, center),
    `ClampPosition`, `GetPositionOffsetPx` (fixed column offset — replaces the removed
    `GetInLaneSpacingOffsetPx`'s occupant-count-based spread, since occupancy is exclusive now),
    `PositionRangeWidthPx`.
  - New `FormationSystem.cs` — pure `IsSlotOccupied(occupiedSlots, lane, position)` exclusivity
    check, decoupled from which concrete type is asking (works for both `PhasixRuntimeData` and
    `BattleParticipant` via caller-side projection).
  - New `FormationGridPicker.cs` — builds the shared 7x5 clickable grid (current slot starred,
    others' occupied slots disabled with a short label), used identically by both the Party menu and
    the in-battle Move skill.
  - `PhasixRuntimeData` gained `preferredLaneIndex`/`preferredPositionIndex` (persistent, default
    center of each range). `BattleParticipant` gained `PositionIndex`; its constructor now seeds
    both `LaneIndex`/`PositionIndex` from the runtime data's preferred fields.
  - `BattleHUDController.LayoutPlayerStageCreaturesByLane` reworked to place each creature directly
    from its own `PositionIndex` (no more grouping/spreading — a column's offset is now a pure
    per-participant lookup). New `ShowFormationGridForMove`/`HideFormationGridForMove` — a centered
    overlay panel shown when the Move orb is pressed (bypassing the normal drag-to-a-creature flow
    entirely, since Move targets a grid slot, not a creature), wired into `BeginDragForSkill`.
    `ShowMoveSelection` gained an optional `playerSideForFormationGrid` parameter so the picker can
    check occupancy against live allies.
  - `ChosenMove` gained optional `DestinationLane`/`DestinationPosition` (only ever set for Move).
  - New `BuiltInMoveType.Move` + `Standard_Move.asset` (`_skillName: "M"`), seeded alongside the
    other Standard built-ins (`WildSpawnSystem.SeedInitialSkills`, `GameManager`'s debug loadout),
    hard-excluded from `EnemyAI.ChooseSkill` (same treatment as Capture — no AI logic for deciding
    when an enemy should reposition). `BattleManager.ResolveBuiltInMove` gained a `Move` case:
    validates the destination isn't ally-occupied (defense in depth on top of the UI already
    disabling those cells), updates `LaneIndex`/`PositionIndex`, refreshes the layout, logs, done —
    same "instant, no travel animation" treatment Charge/Heal/Regen already have.
  - `OverworldMenuController.ShowDetail` gained a formation section reusing the same
    `FormationGridPicker` inline (not as an overlay) — picking a cell updates the runtime's
    preferred fields and rebuilds the whole detail view so the highlight moves.
  - `.formation-grid*` CSS lives in `BattleHUD.uss` only (`OverworldMenu.uxml` already references it
    as a second stylesheet, so the Party menu picks the same classes up with no duplication).
- **Superseded:** DECISIONS.md's "Lane occupancy — non-exclusive, in-lane visual spacing" (2026-08-11)
  — the exact "exclusive occupancy" alternative that entry rejected is what got built here, once
  pre-battle placement turned into a real feature. Marked superseded, not deleted.
- **Verified:** 332/332 EditMode tests pass (added position-math, `FormationSystem`,
  `PhasixRuntimeData`, and `BattleParticipant` seeding/clamping coverage). Live-verified against a
  real 3-member party in a real battle via `execute_code`: a move onto an ally-occupied slot was
  correctly rejected (battle log confirmed), a move to a free slot succeeded with `LaneIndex`/
  `PositionIndex`/`style.top`/`style.left` all matching expected values exactly. Inspected the
  constructed grid overlay's element tree directly (7x5, correct current/occupied cell states).
  Party menu picker verified structurally (opening it mid-battle for the screenshot attempt
  confused the two simultaneously-active UIDocuments — not a real-game scenario, so verified via
  direct element-tree inspection instead). Screenshotted the in-battle grid overlay and sent it to
  the user.
- **Next:** The Move skill's reposition is instant (no travel animation) — a natural upgrade once
  real art/animation exists, matching Charge/Heal/Regen's own "instant" precedent for now. No
  per-enemy formation/positioning exists (scoped out, same as every other lane-system pass this
  session — no multi-enemy battles yet).

---

[2026-08-11] Phase 3 — Attack Pattern Directive implementation: real 7-lane system + melee Beat Sequence (Slash)
- **Context:** First implementation pass against `Attack_Pattern_Directive_v0_1_0.md` (added as a
  design doc in the prior session, nothing built yet). User decided: import real DOTween now, build
  the real 7-lane system (not a minimal shim), and build exactly one attack — a minimal "Slash"
  (`Approach -> Windup-Real -> Attack`) — per the directive's own Part 1 validation approach.
- **Built:**
  - **DOTween** actually imported (`Assets/Plugins/Demigiant/DOTween/`, precompiled DLL) — confirmed
    no ASMDEF needed (`Phasix.Runtime.asmdef`'s `overrideReferences: false` auto-includes it);
    `"DOTween.dll"` added to `Phasix.Tests.EditMode.asmdef`'s `precompiledReferences` allowlist so a
    future test can reference `DG.Tweening` without a confusing compile error.
  - **Lane system**: `BeatType.cs` (new enum), `LaneMovementSystem.cs` (new — pure math: default
    lane, clamping, adjacency, cost-agnostic stepping, depth scale, screen-position mapping, in-lane
    spacing). `BattleParticipant` gained `LaneIndex`/`PreSequenceOriginLane` +
    `BeginBeatSequenceIfNeeded`/`ClearBeatSequenceOrigin`. `BattleHUDController`'s stage layout
    reworked from party-slot-index columns to real per-lane positioning
    (`LayoutPlayerStageCreaturesByLane`, `ApplyLaneLayout`, `ApplyEnemyLaneDepthScale`,
    `RestoreStageCreatureDepthOrder` now sorts by lane depth) plus new
    `GetStageCreatureElement`/`UpdatePlayerStageCreatureLane` accessors for beat-sequence code.
  - **SkillData** gained `_beatSequence` (`BeatType[]`, empty-by-default — inert for all 95
    pre-existing assets). New `BeatSequenceConfig.cs` centralizes beat timing constants (windup
    durations, lunge distance, hop height — all pending-calibration placeholders), keeping per-skill
    assets free of numeric fields.
  - **Beat Sequence runtime**: `VisualElementTweening.cs` (DOTween `.To()` wrappers for
    `VisualElement.style`, since the stage has no Transform to tween), `BeatSequenceRunner.cs`
    (Approach/Windup/Return coroutines). `BattleManager` gained `ResolveMeleeBeatSequence`,
    `ResolveMeleeAttackBeatOffense`/`Defense` (mirror the existing offense Good/Perfect and defense
    Dodge/Parry+counter systems, minus the projectile, plus a lunge+hit-flash), and
    `RunMeleeLungeAndFlash`. Two new dispatch branches: `ResolveSkillAction` (player attacking) and
    `ResolveEnemyDamageAction` (enemy attacking) both check `skill.BeatSequence` and route into the
    new path when non-empty; every other skill's behavior is unchanged.
  - `CombatVfxController.FlashStageElement`/`BattleHUDController.FlashStageCreatureHit` — real public
    hit-flash methods, corrects a doc/code discrepancy found this session (see DECISIONS.md ->
    [Combat]): last session's "Parry counter-attack hit-flash" entries described these as already
    built; they weren't.
  - **`Melee_Slash` skill asset** authored and wired into `SkillDatabase` (GUID
    `59259b9cd92745c418cc0d819a507408`) — `TreeType Utility`, `BeatSequence [Approach, WindupReal,
    Attack]`. Verified via `execute_code`: resolves correctly by GUID, and `Utility` is naturally
    unlocked for `Test_FireType` at tier 1 (first entry in `AvailableTreeTypes`) — no debug-harness
    workaround needed for manual Play Mode testing.
- **Decided:** See DECISIONS.md -> `[Combat]` for the full set: cross-side lane adjacency (same lane
  index = adjacent, inferred from `BattleLaneLayout`'s already-mirrored math), enemy-side lane
  symmetry (mechanics real for both sides, occupancy-spacing visuals player-only until multi-enemy
  battles exist), Approach/Return movement cost (folded into the skill's existing Aura cost, no new
  cost), and the FlashStageElement discrepancy correction.
- **Verified:** Full EditMode suite passes both before and after the fixes below (317/317, zero
  regressions) — new coverage: `LaneMovementSystemTests.cs`, `SkillDataTests.cs`, plus additions to
  `BattleParticipantTests.cs`. Compile-checked after every file addition via `read_console`, zero
  errors throughout. **Live Play Mode screenshot check (user-requested)** caught two real bugs
  neither tests nor compilation could — see `LESSONS_LEARNED.md` -> `[Combat]` "Real 7-lane
  positioning shipped with the enemy off-screen and party members stacked" for the full writeup: the
  enemy rendered off-screen (its stage container was never resized to match its new lane-based
  position — fixed via a new `_enemyStageArea` sizing step in `ApplyEnemyLaneDepthScale`), and same-
  lane party members read as one blob (in-lane spacing was too small — `LaneMovementSystem.
  InLaneSpacingPx` raised 20f -> 90f after two iterative screenshot checks). Both confirmed fixed via
  a follow-up screenshot; enemy now on-screen, party members show clear visual separation.
- **Also noted:** `ProjectSettings.asset` picked up `DOTWEEN;DOTWEEN_UITOOLKIT` scripting defines for
  every platform partway through this session, unprompted (see `DECISIONS.md` -> `[Tweening]` update)
  — DOTween's official `VisualElement` extension methods are now active alongside (not conflicting
  with) this session's custom `VisualElementTweening.cs` wrappers.
- **Follow-up (same session, user-directed):** In-lane spacing moved from vertical (`translate.y`) to
  horizontal (`left`) and widened 90f -> 150f, referencing a Sonny 2 screenshot — same-lane party
  members now read as a horizontal row, and an open skill-ring wheel clears its neighbor without
  overlapping (150px vs. the ~147px `SkillSlotRadius`-derived minimum). `BeatSequenceRunner.
  RunApproach`/`RunReturn` and `BattleManager.RunMeleeLungeAndFlash` updated to preserve an
  occupant's live spacing offset instead of snapping it back to the lane's unspaced base position
  mid-action. See `DECISIONS.md` -> `[Combat]` "In-lane spacing moved from vertical to horizontal."
  Re-verified: 317/317 EditMode tests still pass, clean compile, and a follow-up screenshot confirms
  the horizontal spread with no overlap.
- **Follow-up #2 (same session, user-reported "a little too far left"):** The `2*InLaneSpacingPx`
  width padding added to `PlayerStageArea` (for centering-math safety on extreme same-lane-occupant
  spreads) had an unintended side effect: `.stage-side-player`'s `translate: -50% -50%` re-centers
  around whatever width the box reports, so padding the width WITHOUT also shifting children right
  by half that padding shifted the entire visible party group ~150px left of its intended position.
  Fixed with a `centeringCompensationPx` offset in `LayoutPlayerStageCreaturesByLane` (adds back
  exactly half the padding to every child's `left`) — box stays padded for safety, visible position
  matches what an unpadded box would have shown. Re-verified: 317/317 tests pass, clean compile,
  follow-up screenshot confirms the group sits back at its intended position.
- **Follow-up #3 (same session, user-directed):** Two more asks — "move it in a bit more" (concern:
  back-lane creatures clipping the nameplate column) and "does moving forward/back adjust size to
  depict depth?" The second question caught a real gap on code review: `BeatSequenceRunner.
  RunApproach`/`RunReturn` only ever tweened `left` (position), never `scale` — a mover's visual size
  stayed frozen at whatever it was when the sequence started, only catching up to its real lane's
  depth at the next full `Initialize` (i.e. not until the battle restarts). Fixed: both methods now
  tween `LaneMovementSystem.GetDepthScale` toward each lane as they move, matching Combat_Directive
  Part 2 ("sprites automatically scale as they move between lanes"). Live-verified via `execute_code`
  driving `BeatSequenceRunner.RunApproach` directly against a real battle: Lane 4 (scale 0.85) ->
  Lane 1 (scale 1.15) moved AND grew correctly, screenshotted. Separately, `LaneMovementSystem.
  LaneEdgeGapPx` raised 28f -> 90f (moves the whole lane range, especially back-lane/Lane 7 — the
  case closest to the nameplate column — further from screen edge) per the "move it in more" ask.
  Re-verified: 317/317 tests pass, clean compile.
- **Follow-up #4 (same session, user-directed):** User pasted a generic Transform/SpriteRenderer
  continuous-depth-scaling spec (localScale, sortingOrder, FixedUpdate, furthestY/closestY/minScale/
  maxScale fields, diagonal tracking) and asked to apply it — clarified first via AskUserQuestion
  since none of those APIs exist on our UI Toolkit stage. Landed on: keep the 7-lane mechanical model
  (Combat_Directive's locked targeting/movement authority) but make the VISUAL scale a genuinely
  continuous function of live position instead of snapping between two tween-endpoint values. Built
  `LaneMovementSystem.GetDepthScaleFromLeft` (continuous, position-based — companion to the existing
  discrete `GetDepthScale(laneIndex)`, proven to agree exactly at every lane's own position) and
  `VisualElementTweening.TweenLeftWithDepthScale` (one combined tween drives both `left` and `scale`
  from the same live value every frame, replacing the prior two-independent-tweens approach in
  `BeatSequenceRunner.RunApproach`/`RunReturn`). `1.15f`/`0.55f` extracted into named
  `MaxDepthScale`/`MinDepthScale` constants (kept as `const` on the existing static class rather than
  Inspector `[SerializeField]`s — flagged to user, see DECISIONS.md -> [Combat] for why). Translated
  the spec's other 3 points explicitly rather than force-fitting them: diagonal tracking doesn't
  apply (only `left`/X is depth-relevant here; `translate.y` is a separate decorative flourish),
  dynamic per-frame sorting doesn't apply (`RestoreStageCreatureDepthOrder` already re-runs on every
  actual lane change — UI Toolkit's paint order is discrete, not a continuous `sortingOrder`),
  no-Update()-polling was already satisfied (DOTween-driven throughout, no new polling loop added).
  10 new EditMode tests (327/327 total) including a direct continuity proof (scale at the exact
  midpoint between two lane positions equals the midpoint of those lanes' scales). A live Play Mode
  attempt to sample scale mid-tween via rapid `execute_code` calls hit `resolvedStyle` timing lag
  (a measurement artifact of scripted rapid-fire calls with no frame in between, not a runtime bug) —
  noted honestly rather than reported as a clean confirmation; the EditMode tests plus the
  single-tween architecture (position and scale share one DOTween setter, cannot desync by
  construction) are the actual basis for confidence here.
- **Next:** Type E (Reaction) still has no trigger point against melee sequences (flagged prior
  session, still open); Strike Points, ranged archetypes, and additional melee patterns (Part 5/6/9)
  remain unbuilt by design — this pass deliberately proved the framework with one minimal example
  only. A live interactive pass (actually clicking Slash's skill-ring orb, timing the Attack beat's
  ring, watching the full Approach/Windup/Attack/Return play out) is still worth doing by hand — this
  session's verification drove the battle programmatically (`execute_code`) and screenshotted the
  resulting layout, but did not click through the actual skill-selection/timed-input UI.
- **Follow-up #5 (same session, live-tested by user) — two real bugs found and fixed:** User asked
  for a way to actually playtest Slash without scripting; extended the existing `GameManager.
  ApplyDebugPlaytestLoadout` debug hook (already used for exactly this purpose last month) to also
  force-equip Slash on the fallback starter. First real live test then surfaced two genuine bugs:
  1. **"Expecting to move the phasix close to the target... that never happened."** Root cause:
     `RunApproach`'s destination was `target.LaneIndex`, and `LaneMovementSystem.IsAdjacent`
     considered same-lane-index-across-sides as "adjacent" — but BOTH sides default to Lane 4, so
     attacker and target were ALWAYS trivially "already adjacent" the instant a battle started,
     skipping Approach's animation on literally every fresh attack. Fixed: Approach's destination is
     now always Lane 1 (the attacker's own front lane, explicitly "closest to the opposing side" per
     Combat_Directive), not a function of the target's lane at all. `LaneMovementSystem.IsAdjacent`
     itself is unchanged and still correct — only the call site's choice of destination was wrong.
     See DECISIONS.md -> [Combat] "Cross-side lane adjacency... SUPERSEDED."
  2. **"The phasix only goes ha[l]fway through the screen... expecting it to get right in front of
     the target."** Root cause: the player and enemy stage areas are two separate, non-overlapping
     UI Toolkit containers anchored to opposite screen edges with a large gap between them — Lane 1
     only reaches the edge of the attacker's OWN container, nowhere near the opponent. Fixed:
     `RunApproach` now runs a "closing lunge" after reaching Lane 1, computing the target's real
     on-screen position (`worldBound`) and converting it into the attacker's own local coordinate
     space, landing edge-to-edge with a 20px gap. Caught and fixed a real bug in the first version of
     this formula too (center-based math was off by one attacker-half-width, verified via live
     `execute_code` inspection against real battle `worldBound` values showing ~30px overlap instead
     of the intended 20px gap) — rewrote as pure edge-to-edge math, re-verified exact. See
     DECISIONS.md -> [Combat] "Approach's 'closing lunge'."
  `spacingOffset` (in-lane occupancy spread) refactored from something each method re-derived from
  live position into a value captured once by `BattleManager.ResolveMeleeBeatSequence` and threaded
  through explicitly — a live re-derivation would have misread the closing lunge's travel as
  permanent spacing. Verified: 327/327 EditMode tests pass throughout both fixes, clean compile,
  and the full Approach -> Windup -> Attack -> Return pipeline confirmed running end-to-end multiple
  times with real damage landing in the battle log. Repeated attempts to screenshot the lunge
  mid-animation failed because the unfocused Editor throttles background frame rate enough that the
  whole ~2-second sequence completed within a single tool round-trip every time — a testing-
  environment artifact (confirmed via direct `worldBound` math instead, not a runtime bug).
- **Next:** Same open items as above, plus: a normal interactive Play Mode pass (Editor focused,
  clicking through the real UI) is the one verification step this session still couldn't do itself —
  worth a live look to confirm the lunge reads well now that the math is confirmed correct.
- **Follow-up #6 (same session, live-tested by user) — major correction: lanes are vertical rows,
  not horizontal positions.** Two more live reports: "2 hitches in the first half of the movement...
  second half looks clean and the bounce back looks clean too" (a real bug — the lane-stepping loop
  tweened one lane at a time with real stops between each, unlike the already-continuous closing
  lunge/Return), and "the player is just moving left to right, no vertical movement so why is the
  size of the phasix also changing?" The second question led to a bigger realization: "wait isn't
  the lane like 7 horizontal rows?" — yes. Combat_Directive's "3/4 perspective" framing and the
  user's OWN original depth-scaling request several turns earlier (explicitly Y-position-based)
  both pointed at vertical rows all along; the horizontal-lane implementation was the actual mistake,
  not something needing a vertical cue bolted on. **Full rework:** `LaneMovementSystem.
  GetLaneScreenLeft` -> `GetLaneScreenTop` (now `style.top`, Lane 1 lower/bigger, Lane 7 higher/
  smaller, Lane 4 at the original baseline). Melee Approach/Return are now purely horizontal and
  never change row, so depth scale stays constant throughout a Beat Sequence — which also
  automatically fixes both the hitching (the lane-stepping loop that caused it no longer exists —
  Approach is just the closing lunge now) and the "why does size change" question (it doesn't
  anymore). Removed as dead weight once the row never changes during movement: `LaneMovementSystem.
  GetDepthScaleFromLeft`, `VisualElementTweening.TweenLeftWithDepthScale` (10 tests removed with
  them), and `BattleParticipant.PreSequenceOriginLane`/`BeginBeatSequenceIfNeeded`/
  `ClearBeatSequenceOrigin` (replaced by a plain `restingLeft` float threaded through
  `ResolveMeleeBeatSequence`). Verified: 317/317 tests pass; live `execute_code` check against a real
  battle confirmed `top`/`scale` read the identical resting values (90, 0.85) before the attack,
  after the closing lunge, and after the full Return — depth truly never changes during movement now
  — and the full pipeline still lands real damage in the battle log. See DECISIONS.md -> [Combat]
  "Lane axis corrected: 7 vertical rows (Y), not 7 horizontal positions (X)."
- **Next:** A normal interactive Play Mode pass is still the one thing this session couldn't verify
  itself — worth checking that the vertical row spacing (30px/row, placeholder) reads well visually,
  and that the now-purely-horizontal Approach/lunge feels right without any vertical travel.

---

[2026-08-11] Phase 3 follow-up — Battle log damage breakdown (base/type/timing, colored)
- **Context:** User-directed: "show the (base damage + type advantage damage + timing bonus
  damage) just for visibility rn" — a temporary debug-style breakdown of how each attack's final
  damage number was actually built, with base white, decreases red, increases green, and the total
  shown explicitly.
- **Built:** `DamageCalculator.ComputeBaseDamage(attacker, target, category, skillPower)` — the same
  stat-ratio formula `ComputeDamage` already uses, but with the Primal type multiplier fixed at
  1.0x, so callers get the pure pre-type number on its own. Refactored `ComputeDamage`'s internals
  into a shared private `ComputeStatRatio` helper so both methods stay in sync automatically;
  `ComputeDamage`'s own return value and rounding are bit-for-bit unchanged.
  `BattleLogFormatter.FormatAttack`/`FormatSkillAttack`/`FormatDefenseOutcome` now take
  `pureBaseDamage`/`damageAfterType`/`finalDamage` (three already-resolved ints) instead of a single
  `damage` int, and build a `"(N base + delta type [+ delta timing]) = N total damage"` segment via
  a new private `FormatDamageBreakdown`/`FormatDeltaTerm` pair — using UI Toolkit's built-in rich-
  text `<color=#RRGGBB>` tags (`TextElement.enableRichText` defaults `true`, confirmed via Unity
  docs rather than assumed — no scene/USS changes needed). Colors: base `#FFFFFF` (white), increase
  `#5AC864` (the exact same green as `BattleHUDController.SuccessFlashColor`), decrease `#DC3C3C`
  (the exact same red as `MissFlashColor`) — deliberately reusing the ring-flash palette so "green
  reads as good, red reads as bad" stays one consistent color language across the whole HUD, not two
  competing ones.
- **Decided:** typeDelta/timingDelta are defined as differences between already-rounded numbers
  (`damageAfterType - pureBaseDamage`, `finalDamage - damageAfterType`), not independently computed
  and re-rounded — guarantees the three terms always sum to exactly the shown total, regardless of
  how any individual step rounded internally. The timing term is entirely omitted (not shown as
  "+0") whenever no timed-input check actually ran — the Parry counter-attack, and any incoming
  enemy hit that lands (those are always flat 1x, only Dodge/Parry's full-avoidance is at stake for
  them, not a graduated bonus/penalty like the player's own attack timing).
- **Changed:** All 4 damage-log call sites in `BattleManager` (`ResolveBuiltInMove`'s Attack case,
  `ResolveSkillAction`'s damage branch, the Parry counter-attack block, `ResolveEnemyDamageAction`'s
  incoming-hit log) now compute `pureBaseDamage` via the new method and pass all three numbers
  through. `LogResults`/`LogDefenseResult` helper signatures updated to carry them.
- **Verified:** 287/287 EditMode tests pass (7 new: `BattleLogFormatterTests` now directly asserts
  the white/green/red color tags and sign behavior for both directions of type and timing deltas,
  plus that the timing term is omitted entirely for a null/no-timing-check outcome).
- **Next:** Live Play Mode confirmation the colors actually render in the real battle log (rich
  text was verified as enabled by default via Unity's own docs, not by running the scene).

---

[2026-08-11] Phase 3 follow-up — Parry counter-attack's hit-flash now synced to its real damage
- **Context:** User-reported: after a successful Parry, the counter-attack's damage wasn't showing
  a hit-flash on the attacker, even though the visual system already had a flash wired up for it.
- **Found:** `ResolveParryDeflect` (the return-fire deflect projectile, "doubling as the counter-
  attack's own hit VFX" per its own doc comment) fires right when Parry is detected — well under a
  second of projectile travel + a 0.2s flash. But the counter's actual damage doesn't apply until
  much later: after a full `AutoMessageDurationSeconds` (1.5s) "defended!" message wait, plus all
  the intervening logging/aura/burst-fill work. The flash was reliably finishing over a second
  before the HP bar it was supposed to accompany ever moved, so it read as no animation at all.
- **Fixed:** New `CombatVfxController.FlashStageElement`/`BattleHUDController.FlashStageCreatureHit`
  — a direct, no-projectile flash by slot/side, independent of the held-projectile system. Called
  explicitly in `BattleManager`'s counter-attack block right where the counter's damage is actually
  applied (`ResolveEnemyDamageAction`), guaranteeing the flash and the HP-bar change land together
  regardless of how the earlier deflect-projectile/message timings shake out. The original deflect
  projectile is untouched — it still plays its own early "the parry connected" beat.
- **Checked, not changed:** The other three damage-application paths (player basic Attack, player
  skill attack, enemy attack via Dodge/Parry/Miss) all already await their projectile/ring before
  applying damage (`RunTimedInput`/`RunDefenseTimedInput`'s sweepDuration is sized to the real
  projectile travel time), so their flash and their damage land within the same beat by
  construction — only the counter-attack skipped that awaited step, since it has no timing check
  of its own to await.
- **Verified:** 280/280 EditMode tests pass (no test coverage change — this is pure VFX timing, not
  covered by the existing damage-multiplier/log-text EditMode suite).
- **Next:** Live Play Mode confirmation that the counter-attack's flash now reads clearly against
  the HP-bar drop.

---

[2026-08-11] Phase 3 follow-up — Parry counter-attack double-blink fix
- **Context:** User-reported, immediately after the previous entry's fix went in: the counter-
  attack's target now blinked TWICE instead of once.
- **Found:** Self-inflicted by the previous fix. The new explicit `FlashStageCreatureHit` call was
  added alongside the existing `ResolveParryDeflect` deflect-projectile flash, not in place of it —
  so the attacker now got flashed once early (projectile arrival, on Parry detection) AND once more
  on-time (the new call, at the counter's real damage). Both were individually correct in isolation;
  together they double-fired for the same hit.
- **Fixed:** `CombatVfxController.AnimateAndResolveImmediately` gained a `flashOnArrival` bool.
  The normal offense call site (`LaunchProjectile`) still passes `true` — that flash is the ONLY
  one for that hit and stays as-is. `ResolveHeldProjectileAsParryDeflect`'s call now passes `false`
  — the deflect projectile still visually flies back on Parry (kept, it reads as "the parry
  connected"), but no longer flashes on arrival, since `FlashStageCreatureHit` alone now owns the
  attacker's flash for that counter-attack.
- **Verified:** Clean compile (Play Mode was active in-Editor at fix time, blocking an EditMode
  re-run — no logic/test-covered code changed, purely a flash-trigger wiring fix).
- **Next:** Live Play Mode confirmation — exactly one blink on the counter-attack's target now.

---

[2026-08-11] Phase 3 follow-up — Parry counter-attack: real root-cause fix, damage now awaits the projectile
- **Context:** User-directed, after the double-blink fix: "I need the damage to register the moment
  the projectile hits the target." The two prior same-day fixes had been patching symptoms (missing
  flash, then a duplicate flash) around the actual root cause without fixing it: the deflect
  projectile's launch and the counter's damage application were never actually coupled in time —
  they just happened to read as roughly-OK once the flash was moved to fire at the right moment.
  What was still wrong: the projectile itself was launched way back on Parry detection and had
  already finished flying (and, briefly, double-flashing) a full "defended!" message-wait BEFORE
  the counter's damage/HP-bar update ever ran — so the projectile visually arrived, then nothing
  happened on-screen for over a second, then the HP bar silently dropped with its own separate
  flash. Never actually "the damage registers the moment the projectile hits."
- **Reverted:** The additive `FlashStageElement`/`FlashStageCreatureHit` methods and
  `AnimateAndResolveImmediately`'s `flashOnArrival` flag from the previous two fixes — both fully
  removed, no longer needed once the real timing is fixed at the source.
- **Fixed:** `CombatVfxController.ResolveHeldProjectileAsParryDeflect` (and
  `BattleHUDController.ResolveParryDeflect`, pass-through) now returns the projectile's real travel
  duration instead of `void`. The launch call itself moved out of its old early position (right on
  detecting Parry) down into `BattleManager.ResolveEnemyDamageAction`'s counter-attack section,
  immediately followed by `yield return new WaitForSeconds(deflectTravelDuration)` — the exact same
  "await the travel time, then apply damage" pattern `RunTimedInput`/`RunDefenseTimedInput` already
  use for every other damage-application path in this file. The projectile's own on-arrival flash
  (unchanged, always fires) now lands inside that awaited window, so projectile-hits-target,
  flash, and HP-bar-update are all the same beat, by construction, not by coincidence. The launch+
  await sits in its own `if (isParry)` block ahead of the `attacker.IsAlive`-gated damage block, so
  the held projectile is still always resolved/released the moment Parry happens, never left stuck
  even in the (currently unreachable) case the attacker somehow died first.
- **Verified:** 280/280 EditMode tests pass (Play Mode had since exited, ran clean).
- **Next:** Live Play Mode confirmation that the projectile's arrival, its flash, and the counter's
  HP-bar drop now genuinely happen together, with the "defended!"/"counter-attacks!" messages
  bracketing that beat rather than swallowing it.

---

[2026-08-11] Phase 3 follow-up — Parry deflect projectile no longer sits "stuck" before bouncing back
- **Context:** User-reported, immediately after the previous entry: awaiting the deflect
  projectile's travel time right before applying the counter's damage was correct for SYNC, but the
  launch call itself was still sitting in its post-"defended!"-message position — so the held
  projectile now visibly sat idle-pulsing at the player's position for the full 1.5s
  `AutoMessageDurationSeconds` "defended!" message before it finally started bouncing back at all.
  User: "if I parry on success just have the attack bounce back" (immediately), plus a note that a
  configurable "how long it stays stuck" timer per parry type would be nice later, but isn't needed
  now.
- **Fixed:** Moved the `ResolveParryDeflect` launch+await block in
  `BattleManager.ResolveEnemyDamageAction` from after the "defended!" message to immediately after
  `LogDefenseResult`/burst-fill — i.e. right when Parry is detected, same spot the original
  pre-today code always launched it from. The counter-attack's damage computation/application moved
  inside that same `if (isParry)` block (gated internally on `attacker.IsAlive`), so it still
  applies synced to the projectile's real arrival (unchanged from the previous fix) — it just now
  all happens BEFORE the "defended!"/"counter-attacks!" messages instead of being sandwiched between
  them. `ShowTimedMessage` uses a single shared UI element (`_continuePrompt`), so the two messages
  stay sequential/non-overlapping as before, just both moved to display after the actual bounce-and-
  counter action instead of interleaved with it.
- **Deferred, tracked:** A `// TODO: pending design` marker at the launch call notes the
  configurable "stuck duration per parry type/quality" idea for later — not built now, no numbers
  invented for it.
- **Verified:** 280/280 EditMode tests pass.
- **Next:** Live Play Mode confirmation — the deflect should bounce back the instant Parry lands,
  with zero visible pause, while the counter's damage still lands exactly on arrival.

---

[2026-08-11] Docs — Attack Pattern Directive ingested; lane occupancy/movement-cost locked
- **Context:** User delivered `combat_update.zip` (three files) with a new melee "Beat Sequence" /
  telegraph-knob framework meant to guide Phase 5 skill authoring. Reviewed against live repo state
  before merging: the delivered `Combat_Directive_v0_1_0.md` draft turned out to be based on a stale
  pre-2026-08-05 snapshot, missing the AUD-008 errata, the AUD-005 pending note, and — critically —
  this repo's already-locked Part 4 Dodge/Parry supersession. Replacing the file outright would have
  regressed shipped mechanics, so only the genuinely new content was merged in.
- **Built (docs):** Added `Assets/Docs/Attack_Pattern_Directive_v0_1_0.md` (telegraph knobs, ranged/
  melee archetypes, Melee Beat Sequence system, Strike Points, lane movement/zone targeting — status:
  design capture, nothing built yet) and its companion `Assets/Docs/melee_beat_sequence.mermaid`.
- **Changed:** `Combat_Directive_v0_1_0.md` Part 3 gained two new locked paragraphs (Lane occupancy —
  non-exclusive, in-lane spacing; Movement cost model — context-decided, cost-agnostic), sourced from
  the delivered draft. The "Pending Design" list's "free action or costs an action" line was removed
  now that the cost *model* is decided (exact per-type values remain pending calibration, unchanged).
  Part 4, the AUD-008/AUD-005 notes, and everything else in the file were left untouched.
- **Decided:** Per user direction — (1) the delivered draft's claim that removing Approach-interrupt
  "overrides the earlier 'Approach is interruptible' decision" was factually wrong (no such prior
  decision exists anywhere in DECISIONS.md/CHANGELOG.md); reworded in both the new `.md` and the
  `.mermaid` note block to state it as a first decision, not a reversal. (2) Lane occupancy and the
  movement-cost model are locked now, not left pending — see `DECISIONS.md` -> `[Combat]` (three new
  entries: Lane occupancy, Lane movement cost, Melee Beat Sequences). Also updated `DOCUMENT_INDEX.md`
  with a new Attack_Pattern_Directive row and a note on the Combat_Directive Part 3/Part 4 history.
- **Blocked:** Nothing built — Attack_Pattern_Directive's own status line marks every system as
  spec'd, not implemented. No `SkillData.cs`/`BattleManager.cs`/`BattleHUDController.cs` changes this
  pass.
- **Next:** Type E (Reaction) has no trigger point against melee sequences now that Approach isn't
  interruptible — open gap, `Attack_Pattern_Directive_v0_1_0.md` Part 7/Part 10 item 1. Needs a
  decision before Type E melee content can be authored. After that: extend `SkillData.cs` with the
  Part 2 telegraph knobs (structural fields only, same pattern as the existing PlaceholderIndex/
  GrantsComboRule/BuiltInMove fields — see that file's class doc comment), then build the Beat
  Sequence state machine per Part 1's dependency chain.

---

[2026-08-11] Phase 3 follow-up — Battle log wired for Good/Perfect/Miss; Good lowered to baseline
- **Context:** Second pass after live playtesting the Good/Perfect timing rework (below). Two gaps
  found: the battle log text never actually distinguished the three tiers (it still said "timing
  was perfect!" for a merely-Good hit, and skill attacks logged no timing info at all), and the
  user re-described the intended value spread as "perfect = double, green = standard damage, red =
  reduced" — clarified as wanting Good truly at baseline (1.0x) rather than the elevated 1.5x
  chosen in the first pass, so only Perfect grants any bonus.
- **Changed:** `TimedInputConfig.GoodDamageMultiplier` 1.5f -> 1.0f — Miss 0.5x / Good 1.0x /
  Perfect 2.0x is now the full spread. `BattleLogFormatter.FormatAttack` and the previously
  timing-blind `FormatSkillAttack` both now take a `BattleHUDController.OffenseOutcome?` (nullable
  — null represents the Parry counter-attack, which runs no timing check at all) and only emit
  flavor text for the two tiers that deviate from baseline: Perfect appends "timing was perfect —
  critical hit!", Miss appends "timing was off, weakening the blow!", Good/null stay silent (the
  new baseline needs no comment). `BattleManager.LogResults` and both offense call sites updated to
  pass the real `LastOffenseOutcome` through instead of a bare success bool.
- **Verified:** 280/280 EditMode tests pass (5 new: `BattleLogFormatterTests` now covers Perfect/
  Miss/Good/null-outcome text explicitly, plus a skill-attack Perfect/Miss pair).
- **Next:** Live Play Mode pass to confirm the log lines read naturally against the ring's own
  flash color, now that Good is silent by design.

---

[2026-08-11] Phase 3 follow-up — Offense timing reworked to Good/Perfect tiers, Miss now punished
- **Context:** User-directed follow-up to the Dodge/Parry precision-tier work earlier this session:
  the player's own attack timing check had a "perfect" sub-tier since 2026-08-05 but it was purely
  cosmetic (no bonus), and a missed attack carried zero penalty. User wanted offense to mirror
  defense's two-tier structure exactly and finally punish misses, to "reward players for being
  skilled" at the timing mechanic on both sides of combat.
- **Built:** `BattleHUDController.RunTimedInput` now takes two tolerances instead of one
  (`goodToleranceHalfWidth`, `perfectToleranceHalfWidth`) and classifies into a new
  `OffenseOutcome { Miss, Good, Perfect }` enum. Good reuses `TimedInputConfig.
  DodgeToleranceHalfWidth`/`DodgeBaseWindowPercent` (Defend's own values); Perfect reuses
  `ParryToleranceHalfWidth`/`ParryBaseWindowPercent` (Parry's own values) — not new
  offense-specific numbers, so the two sides stay identical by construction. `LastTimedInputSuccess`/
  `LastTimedInputWasPerfect` are now computed properties over the new `LastOffenseOutcome`, so
  every downstream consumer (EventBus success event, burst-fill gain, `TimedInputStreak` combo
  tracking) needed no changes. `BattleManager`'s two offense call sites (`ResolveSkillAction`,
  `ResolveBuiltInMove`'s Attack case) now compute both tolerances and pick the damage multiplier
  from a 3-way switch: `TimedInputConfig.MissDamageMultiplier = 0.5f` (new) /
  `GoodDamageMultiplier = 1.5f` (renamed from `SuccessDamageMultiplier`, same value) /
  `PerfectDamageMultiplier = 2.0f` (new).
- **Decided:** Perfect's reward is damage-only for this pass — see `DECISIONS.md` -> `[Combat]
  Offense timing reworked...` for why a bonus-status reward on Perfect was deferred rather than
  built (no damage-dealing skill has an inherent status payload yet). Removed
  `OffenseToleranceHalfWidth`/`OffenseBaseWindowPercent` and the 2-arg `ComputeWindowPercent`
  overload built on them — dead after this change, confirmed via grep before deleting.
- **Changed:** Two existing EditMode tests referenced the removed/renamed symbols
  (`TimedInputConfigTests`'s four `ComputeWindowPercent` calls, `BattleEngineTests`'s
  `SuccessDamageMultiplier` references) — updated in place, not skipped.
- **Verified:** 275/275 EditMode tests pass after the rename/removal. Confirmed via grep that no
  other call site in `Assets/Scripts` or `Assets/Tests` still references the removed symbols.
- **Next:** Live Play Mode pass to confirm the Perfect band feels as tight as Parry currently does
  on defense, and that a Miss's reduced damage reads clearly against Good's boosted hit.

---

[2026-08-11] Phase 3 follow-up — Projectile pass-through, [UI-003] Attack orb dead-zone
- **Context:** Fifth same-session round. Two more playtest items: the Dodge dissolve should read
  as the incoming attack passing through empty space where the creature used to be, not two
  separately-timed effects; and a specific, reproducible UI bug on the middle party slot's Attack
  orb.
- **Projectile pass-through:** `CombatVfxController.ResolveHeldProjectileAsPassThrough` replaces
  the old in-place fade — the held projectile now continues from its near-edge position, through
  the defender, to the mirrored far edge, before fading and releasing. Timed to take EXACTLY
  `DissolveVfxBridge.DissolveOutDuration` (a new public property, read by `BattleHUDController.
  ResolveDodgedProjectile`) rather than a distance/speed-derived duration — guarantees the pass-
  through and the dissolve-out are genuinely synced, not two independently-timed effects that
  happen to overlap.
- **[UI-003] fixed** — see `KNOWN_ISSUES.md` for the full root-cause writeup. Short version:
  `[EDITOR-001]`'s 2026-08-08 fix made `StatusHeader` win pick priority over `.stage` (needed for
  nameplate/burst-gauge clicks), but `StatusHeader`'s own bounds are broad enough that the middle
  party slot's upward stagger pushes its Attack orb's top ~60% into that overlap, so
  `StatusHeader` itself silently swallowed clicks meant for the orb underneath. Fixed with
  `StatusHeader.pickingMode = PickingMode.Ignore` in `Awake()` — confirmed via live `IPanel.Pick()`
  testing (both the orb resolving correctly now, and nameplates still resolving correctly,
  confirming no regression of the original fix this builds on).
- **Verified:** 275/275 EditMode tests. Live Play Mode for both: pass-through triggered without
  exceptions; orb fix confirmed via direct `Pick()` calls using the real `Awake()`-set value in a
  fresh session (an earlier same-call combined test gave a misleading result due to a stale-layout
  race — redone with a real frame gap between opening the move wheel and querying bounds).
- **Next:** Playtest the pass-through timing/feel, and confirm the orb fix holds for all 3 party
  slots across a real (not scripted) move-wheel interaction. Attack visual pattern variety
  (different attacks reading as visually distinct, not just color-tinted) was raised but
  deliberately deferred, not implemented — see `DECISIONS.md` -> `[Combat] Open note — Attack
  visual pattern variety` for the scoped-out approach and, critically, the one real design
  question that needs answering before writing code: does "Melee" mean a different projectile
  shape, or does the attacker's own sprite lunge instead of using a projectile at all.

[2026-08-10] Phase 3 follow-up — Post-playtest tuning: dissolve speed/granularity, Dodge/Parry spread
- **Context:** Fourth same-session round, both quick numeric retunes from live playtest feedback
  on the previous two entries' work — no structural changes.
- **Dissolve feel:** `DissolveVfxBridge`'s three timing fields sped up 0.3s/0.15s/0.3s ->
  0.2s/0.1s/0.2s (out/hold/reappear). `DissolveEffect.mat`'s `_NoiseScale` raised 8 -> 18 for a
  more granular fragmentation pattern (confirmed via pixel sampling: 12 distinct visible/invisible
  transitions across a center scanline at 50% dissolve, vs. a couple large blobs before).
- **Dodge/Parry spread:** user observed the Dodge window (30% total spread, i.e. the 0.15
  half-width from the prior retune) still felt too wide live. Tightened both further:
  `TimedInputConfig.DodgeToleranceHalfWidth` 0.15 -> 0.10 (20% total spread), `ParryToleranceHalfWidth`
  0.05 -> 0.025 (5% total spread) — both symmetrical around the ring's 1.0 ratio by construction.
- **Verified:** 275/275 EditMode tests after each change.
- **Next:** Another playtest pass to confirm the tighter Dodge/Parry windows and faster/more
  granular dissolve feel right together, not just individually.

[2026-08-10] Phase 3 follow-up — Cue-timing fix, real Shader Graph-equivalent Dodge dissolve
- **Context:** Third same-session round. Playtest found the purple Parry outline still read as
  delayed ("flashes purple immediately after it shoots the parry attack") and the Dodge cue wasn't
  visible at all — root cause: both fired from BattleManager's dispatch, which only runs after
  RunDefenseTimedInput's own ~0.3s ring-flash hold PLUS all of BattleManager's subsequent damage/
  logging work, not at the actual moment of the hit. Separately, the user asked for the Dodge cue
  to become a real Shader Graph dissolve — but clarified twice: first that Unity MCP tooling can't
  author an actual `.shadergraph` node asset (only raw shader code), confirmed proceeding with a
  hand-coded URP-equivalent; then, more importantly, that the dissolve should apply to the
  DEFENDER'S OWN creature (not the projectile, which was the original build) — the Phasix itself
  phases out and back in, not the incoming attack.
- **Fixed — cue timing:** Moved the Hit-flash/Dodge-resolve/Parry-outline-flash triggers from
  BattleManager's post-coroutine dispatch into `RunDefenseTimedInput` itself, firing immediately
  once `LastDefenseOutcome` is set, before its own 0.3s hold. Only the Parry deflect-and-counter
  projectile (needs the counter-attacker's own Primal type, which only BattleManager knows) still
  resolves after the coroutine returns.
- **Built — real Shader Graph-equivalent dissolve on the defender:**
  - `Assets/Shaders/DissolveEffect.shader` — hand-coded URP shader (noise-clip + edge glow, the
    standard dissolve technique) with a circular mask baked into the fragment shader via
    UV-distance-from-center, so a plain Quad primitive renders as a circle with no separate sprite
    asset needed.
  - Discovered mid-build that `BattleHUDPanelSettings.asset` is Screen Space Overlay, which Unity
    always composites on top of every camera's output — a normal world-space MeshRenderer would be
    completely invisible behind the HUD. Bridged around this with a RenderTexture pipeline: a
    dedicated `_DissolveVfxCamera` (culled to a new `CombatDissolveVfx` layer only) renders
    `_DissolveVfxQuad` (carrying the new `DissolveEffect.mat`) into
    `Assets/Textures/DissolveCaptureRT.renderTexture`, which `DissolveVfxBridge.cs` then displays
    AS the defender's own stage-creature `VisualElement`'s `background-image` for the effect's
    duration — from the panel's perspective it's just another UI image, composited correctly.
  - `DissolveVfxBridge` (new, singleton in `SampleScene`, same `DontDestroyOnLoad` pattern as
    `AudioManager`) drives `_DissolveAmount` 0→1→0 via `MaterialPropertyBlock` (out/hold/reappear),
    tinted by whatever color the defender's element already has (its own Primal type — read
    directly, no extra data needs to be threaded in from BattleManager).
  - Projectile's own Dodge behavior simplified to a quick, unremarkable fade — the real "you
    dodged!" cue is now the defender dissolving, not the incoming attack.
- **Bug found and fixed mid-build:** the RenderTexture was created with `depth=0`, which URP's
  Render Graph API flagged: "the output Render Texture must have a depth buffer." Fixed by giving
  it a real depth-stencil format (`D32_SFloat`, Unity's closest match to the requested 24-bit).
- **Verified:** 275/275 EditMode tests. Live Play Mode, checked with real pixel reads (not just
  visual inspection): confirmed the shader's dissolve clip is monotonic (799→575→253→33→0 visible
  sample points as `_DissolveAmount` rises 0→1) and the circular mask correctly clips corners to
  transparent. Confirmed the full trigger→dissolve-out→hold→reappear→revert cycle restores the
  defender's element to its exact original color and clears the `background-image` override, with
  zero console errors throughout. One debugging note for future sessions: `style.X.value` can read
  stale/default immediately after setting an inline style in the same frame — `resolvedStyle.X` is
  the reliable read, confirmed twice this session (border width earlier, backgroundImage here).
- **Next:** Manual playtest of the full Dodge/Parry feel now that both cues fire at the right
  moment and Dodge has a real dissolve. Consider whether Parry deserves a similarly "real" visual
  treatment later (currently still the UI Toolkit border-flash + deflected projectile), and whether
  the dissolve's `_dissolveOutDuration`/`_dissolveHoldDuration`/`_dissolveInDuration` (0.3s/0.15s/
  0.3s placeholders) feel right at actual play speed.

[2026-08-10] Phase 3 follow-up — Dodge/Parry mechanic tuning, real dissolve/outline cues
- **Context:** Second round of same-session user feedback. `MaxProjectileTravelDuration` bumped
  0.6s -> 0.8s per request (speed felt too fast at 0.6s). Bigger issue: the "held, waiting" state
  still looked wrong regardless of tuning, and the Dodge/Parry visual cues built earlier were
  never actually seen in testing — likely because the "stuck" complaints came from slow/no-click
  testing, which only ever resolves as a Miss, never a Dodge or Parry.
- **Decided (three changes, all user-specified with real numbers, not open questions this time):**
  1. **Tighter tolerances** — `TimedInputConfig.DodgeToleranceHalfWidth` 0.25 -> 0.15, `ParryToleranceHalfWidth`
     0.10 -> 0.05 (Dodge ±15%, Parry ±5% of the target ring), now that ring timing is synced to a
     real projectile arrival instant rather than an arbitrary fixed sweep.
  2. **Early ring termination** — `RunDefenseTimedInput` now breaks its wait loop the moment the
     marker/target ratio drops past Dodge's lower tolerance bound, resolving as Miss immediately
     instead of running out the rest of `sweepDuration`. This is NOT a guess or a fixed delay (the
     user's own earlier "auto-hit after 0.25-0.5s" idea was explicitly rejected for risking a late-
     but-valid Dodge/Parry contradicting an already-committed hit) — since MarkerRadius only
     shrinks over time, once the ratio passes that bound no future click can ever succeed, so the
     outcome is already mathematically certain. Fixes the "stuck" feeling at its root rather than
     just papering over it with animation, on top of last entry's idle-pulse/travel-cap fixes.
  3. **Real Dodge/Parry cues** — Dodge's fade became an actual dissolve (`DissolveAndRelease`:
     shrinks via the existing `SetPulseScale` alongside the alpha fade, lengthened to 0.35s so it
     reads as deliberate). Parry gained a new immediate cue: a bright purple outline flash
     (`ParryOutlineFlashRoutine`, new `SetUniformBorder` helper) around the DEFENDER's own stage
     element the instant the parry registers — separate from and prior to the existing
     deflect-and-return-projectile beat, which still plays out afterward as the counter-attack's
     hit feedback.
- **Verified:** 275/275 EditMode tests throughout (no regressions from the tolerance/early-exit
  changes). Live Play Mode: confirmed Dodge dissolve and Parry deflect+outline trigger without
  exceptions; confirmed the outline's inline `style.borderTopWidth`/color are set correctly at the
  moment of resolution (a `resolvedStyle` read in the same frame showed 0 width — a known one-
  frame-stale layout-pass artifact, not a real bug, confirmed by checking the inline `style` value
  directly instead). Could NOT empirically pin down the early-exit's exact timing via Unity MCP —
  round-trip latency between tool calls proved too variable to reliably catch a specific few-
  second window, the same category of limitation already flagged for click-timing-feel
  verification. The early-exit logic is a single conditional using the same live ring values the
  existing Miss/Dodge/Parry classification already depends on, so correctness rests on code review
  + the unchanged EditMode suite rather than an empirical timing measurement this session.
- **Next:** Manual playtest — this is the first pass where a real Dodge/Parry/Miss are all
  actually reachable and visually distinct; worth confirming live that Parry's outline reads
  clearly and that the tighter tolerances (±5%/±15%) don't feel unfairly hard now that the ring is
  timing-synced.

[2026-08-10] Phase 3 follow-up — Projectile-driven timing sync, bigger blob, Dodge/Parry resolution
- **Context:** Same-session follow-up to the combat feedback pass below, user-directed: the
  placeholder projectile was too small to read clearly, and its timing was fully decoupled from
  the existing Dodge/Parry/offense timing ring (RunTimedInput/RunDefenseTimedInput) — it just
  played as a fixed-duration effect after the ring already resolved, with no relationship between
  "when the ring calls perfect" and "when the projectile visually lands."
- **Decided (after two rounds of confirming the mechanic before touching code, per the user's
  explicit ask):** Reversed the dependency the user originally proposed — rather than sizing the
  projectile's speed off the ring's fixed timing, `BattleConfig.ProjectileSpeed` (px/sec) is now
  the real tunable, and the ring's `sweepDuration` is DERIVED from the projectile's actual
  edge-to-edge travel time so the ring's "perfect" instant always lines up with the moment the
  projectile connects, regardless of matchup distance. New `BattleHUDController.
  ComputeSweepDurationForTravelTime` exposes the ring-geometry math
  (`(RingMarkerStartRadius-RingTargetRadius)/(RingMarkerStartRadius-RingMarkerMinRadius)`,
  independent of stats/tolerance) needed to convert a travel time into a matching sweep duration.
  `CombatVfxController.ComputeTravelDuration`/`LaunchProjectile` compute real edge-to-edge distance
  (center distance minus both the projectile's and target's radii) and are called BEFORE the ring
  starts, so both launch already agreeing on timing.
- **Built:**
  - `CombatProjectileVisual.Radius` bumped 8px -> 18px, plus a white outline stroke for contrast;
    added `SetAlpha` for the new Dodge vanish-fade.
  - Offense (`ResolveSkillAction`'s damage branch, `ResolveBuiltInMove`'s Attack case): projectile
    now launches concurrently with `RunTimedInput`, resolves immediately on arrival (offense always
    connects, so no hold needed).
  - Defense (`ResolveEnemyDamageAction`): projectile launches concurrently with
    `RunDefenseTimedInput` in a new "held" state — it can't resolve on arrival because whether the
    hit actually lands isn't known until the ring itself finishes (Dodge's wide window means a
    valid click can land well after the projectile's scheduled arrival). Three new
    `BattleHUDController` methods resolve it once `LastDefenseOutcome` is known: `ResolveHitProjectile`
    (flash), `ResolveDodgedProjectile` (fade out in place), `ResolveParryDeflect` (reuses the SAME
    projectile instance, reverses it back toward the original attacker re-tinted as the
    counter-attacker's own type — this visual IS the counter-attack's hit feedback now, so the
    separate projectile call that used to fire for the counter block was removed).
- **Bug found and fixed during Play Mode verification:** the hit-flash's revert step was restoring
  the struck creature's stage color to the ATTACKER's Primal type (the flash tint's source) instead
  of the struck creature's OWN type — live-verified via a screenshot showing an enemy permanently
  repainted blue after a Water-tinted test hit. Fixed by capturing the element's real
  `resolvedStyle.backgroundColor` at flash-start and reverting to that literal captured value
  instead of recomputing a color from any `PrimalType` — correct regardless of caller, no more
  "which type should this revert to" ambiguity.
- **Verified:** 275/275 EditMode tests (3 new `BattleHUDControllerTests` covering the pure
  sweep-duration formula in isolation, no UIDocument required). Live Play Mode: launched real
  projectiles against actual scene geometry (confirmed sane, non-degenerate sweep durations at
  BattleConfig.ProjectileSpeed=700px/s), exercised all three resolve paths (hit/dodge/parry-deflect)
  including redundant no-held-projectile calls (correctly no-op), and confirmed the color-revert fix
  with a direct before/after color-value comparison (not just visual inspection).
- **Blocked:** the actual click-timing FEEL (does landing a real click when the projectile visually
  connects genuinely read as "perfect"?) can't be verified via Unity MCP — this project's own
  synthetic-pointer-event dispatch is a documented limitation (see LESSONS_LEARNED.md), so
  RunTimedInput/RunDefenseTimedInput's live click-driven loop can't be automated. Needs a manual
  playtest.
- **Next:** Manual playtest of the timing feel — BattleConfig.ProjectileSpeed=700px/s currently
  produces ~3.1s ring sweeps at this scene's actual stage-element spacing (vs. the old flat 1.2s),
  which may read as too slow; it's a single constant to retune if so.

- **Same-session follow-up (user feedback from a real playtest):** the held-projectile "stuck"
  feeling on defense — a frozen blob sitting at the target for however long the player took to
  click, worst case up to the full ring sweep on a timeout — was exactly the hold-until-resolved
  design working as built, just uncomfortable in practice. Two fixes, both purely additive (no
  click-timing rules touched):
  - New `BattleConfig.MaxProjectileTravelDuration` (0.6s placeholder) caps how long a projectile's
    computed travel can be; since sweepDuration derives from travel time, this proportionally caps
    the post-arrival wait tail too — cut the worst-case sweep from ~3.1s to ~1.16s in this scene.
  - Held projectiles now play a gentle breathing pulse (`CombatProjectileVisual.SetPulseScale`)
    instead of freezing solid while waiting on `RunDefenseTimedInput`'s real outcome — reads as
    "poised, about to land" rather than broken.
  - Fixed a latent bug surfaced while building this: resolving a held projectile never stopped its
    travel/idle coroutine, so an unusually fast click (resolving before the projectile's own
    animation naturally finished) could leave that coroutine still running on an element the pool
    had already released back out for a new, unrelated launch. Every `Resolve*` method now stops
    the tracked coroutine first — live-verified via 15 rapid launch-then-immediately-resolve cycles
    (5 each of hit/dodge/parry-deflect) with zero exceptions.
  - Explicitly did NOT implement the originally-proposed "auto-count as a hit ~0.25-0.5s after
    arrival" — flagged to the user that it would make a legitimately late (but still valid) Dodge/
    Parry click visually contradict an already-committed hit-flash, which is worse than the
    freeze it was meant to fix. `ProjectileSpeed`/`MaxProjectileTravelDuration` remain the tuning
    levers if the feel still isn't right after a playtest.
  - 275/275 EditMode tests unaffected (no new automatable coverage here — this is animation/timing
    feel, verified live via Play Mode + direct field inspection instead).

[2026-08-10] Phase 3 close-out — Enemy AI heuristics, combat audio/VFX feedback layer
- **Context:** Combat (Phase 3) was functionally complete per the roadmap gate — real battle
  loop, damage formula, status/combo/skill-tree infrastructure, 256/256 tests, zero open bugs —
  but genuinely thin in two specific ways CLAUDE.md's original folder structure had scoped as
  Phase 3 work and never finished: `Assets/Scripts/Audio/` was a single fully-stubbed file, and
  `EnemyTurn` was pure `Random.Range` target choice with one hardcoded basic attack. Planning
  originally also scoped an items-in-battle framework for this pass; research found it has zero
  design backing anywhere (not even the Combat Directive mentions it), and even the one item named
  in the docs (Signal Swap Item, GDD §16.3) turned out to be explicitly tagged `PENDING` at GDD
  §22.2 — items were dropped from scope entirely, see the new `DECISIONS.md` → `[Items]` open note.
- **Built:**
  - `EnemyAI.cs` (new, `Assets/Scripts/Combat/`) — `ComputeTargetWeight`/`ChooseTarget` (weighted
    toward lower-HP%/type-effective targets via the existing `PrimalTypeChart.GetMultiplier`) and
    `ChooseSkill` (buckets an enemy's real equipped skills — seeded via
    `WildSpawnSystem.SeedInitialSkills` — into Damage/SelfSupport/Debuff via `BuiltInMoveType` and
    `PlaceholderSkillResolver`, hard-excluding Capture). 4 new placeholder `BattleConfig` constants.
    Deliberately scoped as a heuristic upgrade, not the real AI decision framework
    `Combat_Directive_v0_1_0.md` flags as pending design (GDD §18.6).
  - `BattleManager.EnemyTurn` restructured into `ResolveEnemyDamageAction`/
    `ResolveEnemySelfSupportAction`/`ResolveEnemyDebuffAction`, dispatched by `EnemyAI`. The
    `skillOrNull == null` fallback path reproduces the old hardcoded-Attack behavior byte-for-byte —
    verified via the full existing EditMode suite staying green throughout.
  - `AudioManager.cs` + `AudioCueCatalog.cs` (new, `Assets/Scripts/Audio/`) — pooled
    `AudioSource` playback (`UnityEngine.Pool.ObjectPool<T>`, first pooling implementation in the
    codebase, per Technical Directive §12.4) reading clips from a real `[CreateAssetMenu]`
    ScriptableObject asset (`Assets/Data/Audio/AudioCueCatalog.asset`) rather than this codebase's
    other static-Dictionary "catalog" convention — the whole point is Inspector-swappable clip
    references, zero code changes to replace a placeholder with a real asset later.
  - `CombatProjectileVisual.cs`/`CombatVfxController.cs` (new, `Assets/Scripts/Combat/`) — a
    pooled placeholder projectile (simple `Painter2D`-drawn diamond, patterned off the existing
    `DragLineVisual`) that travels between stage-creature positions and flashes the target on
    arrival, tinted by the acting creature's Primal type (`PrimalTypeColor.GetColor`/
    `GetUnderglowColor`). Owned by `BattleHUDController` (new `PlayHitVfx` public method,
    called by `BattleManager` at its 4 real damage-landing call sites — player skill/attack,
    enemy attack/skill, and the Parry counter-attack), not a separate scene singleton, since it
    needs direct UI Toolkit element access and `BattleManager` never touches those directly.
  - `BattleAudioVfxHooks.cs` — all 9 previously-empty handler bodies filled in, fanning out to
    `AudioManager` (all 9) and `BattleHUDController`'s whole-Stage VFX pulses (outcome/bond-
    milestone/capture — the per-hit projectile/flash goes through the direct `PlayHitVfx` call
    sites above instead, since `OnDamageTaken` only carries the damaged creature, not attacker+
    target+position). Two previously-dead `EventBus` events (`Raise_SkillUsed`,
    `Raise_TimedInputSuccess`) got their first real call sites, in `BattleManager`.
- **Bug found and fixed during verification:** the very first EditMode run after wiring
  `OnBondMilestoneReached` crashed unrelated `BondSystemTests` with a `MissingReferenceException`
  on `BattleHUDController`. Root cause: `BattleHUDController.Instance` was never cleared on
  destroy, so after `BattleScene_Main` unloads it's a Unity "fake null" — and C#'s `?.` operator
  does NOT catch that (it bypasses `UnityEngine.Object`'s overloaded `==`), so any bond milestone
  reached outside of battle after any battle has ever run would have thrown this in real gameplay,
  not just in tests. Fixed with a new `BattleHUDController.OnDestroy()` clearing `Instance` (guarded
  on `== this`). Confirms the value of running the full suite, not just new tests, after wiring a
  previously-dead event into new subscribers.
- **Verified:** 272/272 EditMode tests green (256 existing + 16 new — `EnemyAITests`,
  `AudioCueCatalogTests`) throughout. Live Play Mode verification via Unity MCP against real
  project assets (not synthetic test fixtures): enemy skill selection shows genuine variety across
  a real creature's actual equipped skills (Attack/Utility Skill 1/Utility Skill 2 all chosen over
  300 trials), low-HP targeting picked ~74% vs. a 50% uniform baseline, Capture never AI-selected,
  all 9 audio hook call paths fire without error end-to-end via real `EventBus` events, and all 4
  VFX passthroughs (including an out-of-range slot no-op) execute cleanly in a real loaded
  `BattleScene_Main` with no leftover visual artifacts after animations complete.
- **Blocked:** placeholder SFX generation (`mcp__unity-mcp__generate_audio`, fal.ai-backed) — no
  API key configured in the Unity MCP Asset Generation tab. `AudioCueCatalog`'s fields are all
  still empty; `AudioManager.PlayClip` is a designed-in null-safe no-op for this case, so nothing
  is broken, just silent until a key is configured and clips are generated/assigned.
- **Next:** Configure the fal.ai key (MCP for Unity → Asset Generation tab) and generate/assign
  the 9 placeholder SFX into `Assets/Data/Audio/AudioCueCatalog.asset` to make the audio layer
  actually audible. Items/Economy (§22) still needs a real design pass from the user before any
  item system is built — see `DECISIONS.md` → `[Items]` open note for the specific questions.

[2026-08-10] Bugfix — Orphaned components removed from UIRoot_BattleSummary in BattleScene_Main
- **Context:** Asked "what's next" again with no specific feature in mind — zero open bugs,
  256/256 tests passing per the last session. Ran the CLAUDE.md planning checklist, and
  `read_console` surfaced a live, previously-untracked error: `"The referenced script (Unknown) on
  this Behaviour is missing!"`. User picked diagnosing it over the other backlog candidate (the
  blocked Phase 4 Evolution design gaps).
- **Root-caused:** cross-referenced every `m_Script` guid in `SampleScene.unity` (all 18 resolved
  cleanly — a red herring from an earlier `Library/PackageCache` grep timeout, not a real
  problem) against `.meta` files in `Assets/` and `Library/PackageCache/`, then did the same for
  `BattleScene_Main.unity` and both project prefabs. Found the real culprit on
  `UIRoot_BattleSummary` in `BattleScene_Main.unity`: a dead `MonoBehaviour` whose script guid no
  longer resolves anywhere, with a stale `m_EditorClassIdentifier` reading
  `Phasix.Runtime::AuraAllocationController` — a leftover from that class's documented deletion
  when superseded by `BattleSummaryController`. The GameObject was never cleaned up when the
  script was removed.
- **Second bug found in the same investigation:** the same GameObject also carried **two**
  `BattleSummaryController` components (same script, duplicated). `BattleSummaryController` is a
  static singleton set in `Awake()`; both instances queried the same shared `UIDocument` and
  subscribed the same Continue button's click. This happened not to visibly break anything (only
  whichever instance's `Awake()` ran last ever got a real `_onDone` callback via `Show()`), but was
  fragile scene cruft from the same earlier refactor, not scope creep to fix alongside the missing
  script.
- **Fixed:** Removed both — the dead component via Unity's own
  `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` API (via `execute_code`, the correct
  tool for a component with no resolvable type, since `manage_components` can't target a component
  it can't identify by type name), the duplicate via `manage_components(action="remove")`. The
  `manage_scene(action="save")` MCP tool turned out to have a path bug — it always writes to
  `Assets/<SceneName>.unity` regardless of the scene's real folder or an explicit `path` argument,
  which created (and I deleted) a stray duplicate `Assets/BattleScene_Main.unity` twice before I
  gave up on it and hand-edited the real `Assets/Scenes/BattleScene_Main.unity` YAML directly
  instead (verified first that neither removed fileID was referenced anywhere else in the file).
- **Verified:** a completely fresh disk-load of the corrected scene showed a clean console (only
  the unrelated, pre-existing A* Pathfinding Project editor update-checker HTTP warnings remain)
  and exactly 3 components on `UIRoot_BattleSummary` (`Transform`, `UIDocument`, one
  `BattleSummaryController`). 256/256 EditMode tests still pass (scene-only change, no C# touched).
- **Blocked:** the planned live Play Mode test (win a battle, click Continue on the real summary
  screen) hit a snag mid-session — driving it via `execute_code` triggered a genuine Unity Editor
  hang (see `LESSONS_LEARNED.md` → `[Tooling] Spin-waiting on AsyncOperation.isDone...`), and the
  user opted to restart the Editor themselves rather than have it force-killed.
- **Follow-up (same day, after Editor restart):** Re-ran the Play Mode check using synchronous
  `SceneManager.LoadScene` instead of the async+spin-wait pattern that caused the hang — no repeat
  incident. `Show()` correctly displayed the summary panel with real dynamic label text. Synthetic
  `ClickEvent`/`PointerDown`+`PointerUp` dispatch via `SendEvent` did not trigger `Button.clicked`
  at all — a known UI Toolkit `SendEvent` limitation already documented in `LESSONS_LEARNED.md`,
  not a defect in the fix. Confirmed the actual thing this fix targets — double-firing from two
  subscribed component instances — by reflecting into the Continue button's `Clickable.clicked`
  delegate directly instead: exactly one subscriber, bound to the single remaining controller
  instance. See `KNOWN_ISSUES.md` → `[UI-002]` for the full write-up; that entry is now fully
  closed, no outstanding follow-up.

[2026-08-10] Cleanup + Docs — Dead EncounterPromptController deleted, Evolution Directive inconsistencies fixed
- **Context:** Asked "what's next" with no specific feature in mind. Ran the CLAUDE.md
  planning-session checklist (PhasixGuide.md, Unity editor state, console) plus a scan of
  CHANGELOG/KNOWN_ISSUES/DECISIONS to compile the real backlog — zero open bugs, 256/256 tests
  passing. User picked two follow-up items already flagged in earlier sessions.
- **Built/Deleted (Task 1):** Deleted `EncounterPromptController.cs`, `EncounterPrompt.uxml/.uss`,
  `EncounterPromptPanelSettings.asset` (+ `.meta`s), and the `UIRoot_EncounterPrompt` scene object
  — confirmed genuinely dead (grepped the whole `Assets/Scripts` tree, nothing calls `.Show()`
  since the 2026-08-10 auto-engage change earlier this session). Reworded the two stale "matching
  EncounterPromptController's convention" doc comments in `BattleHUDController.cs`/
  `BattleSummaryController.cs`; left the already-correctly-historical mentions in
  `WildEncounterCreature.cs`/`BattleManager.cs` untouched.
- **Verified live (Task 1):** Clean compile, 256/256 EditMode tests, Play Mode — teleported the
  player onto a wild spawn point via `execute_code`, confirmed `BattleScene_Main` still loads
  (auto-engage intact) with no missing-component errors from the deleted GameObject.
- **Fixed (Task 2):** Read the full 2005-line `Evolution_System_Directive_v1_1_0.md` end to end
  and resolved every declared-vs-used field/method mismatch DECISIONS.md had flagged (and several
  more found in the process — see that file's `[Creatures] Evolution_System_Directive_v1_1_0.md
  internal inconsistencies — RESOLVED` entry for the full list: `EvolutionNodeSO`/
  `EvolutionBranchSO` field renames, the missing `EvolutionGraphSO.GetBranchesFrom`, a full
  `ConditionalType`/`EvaluateConditional` rewrite to match §4's locked table, a duplicate `§15`
  heading). Doc-only change — no `Evolution/` code exists yet, zero compile/runtime surface.
- **Blocked:** Two conditional types (`CreatureCaptured`, `SkillTreeUnlocked`) still need a real
  design answer (a roster-query interface doesn't exist yet; per-creature vs. account-wide scope
  is ambiguous) — left as `// TODO: pending design` inline rather than invented. The canonical
  `.pdf` also still needs a manual sync — outside Claude Code's reach, same as the pre-existing
  Active Slots table gap.
- **Next:** Someone needs to manually port the `.md` fixes into the PDF before Phase 4 treats it
  as authoritative. The two open conditional-type design gaps need a real answer before Phase 4
  implementation can build `EvolutionEvaluator` against them.

[2026-08-10] Docs — Evolution Web mockup added to the repo as a reference doc
- **Context:** User asked whether `evolution_web.html` (their original design mockup, shared
  earlier this session and used as the direct inspiration for the skill web pan/zoom rework) had
  been added to the project's reference docs — it hadn't; it only existed as prose mentions in
  `CHANGELOG.md`/`DECISIONS.md` and as a local file in Downloads.
- **Built:** Copied verbatim into `Assets/Docs/evolution_web_mockup.html` (self-contained
  HTML/canvas/JS, no external deps besides a Google Fonts import). Registered in
  `DOCUMENT_INDEX.md` under a new "Design Mockups — Interaction/Visual Reference Only, NOT Data"
  section — explicit about the fact that node names/stat requirements/branch pairs inside it are
  randomly generated or hand-picked flavor for the demo, not real design data; only the
  interaction pattern (pan/zoom, glowing nodes, curved dashed crossover edges, 3-state fog-of-war,
  BFS "Plan Mode" path highlighting) is meaningful reference. Updated the existing
  `DECISIONS.md` → [UI] mention to point at the new path.
- **Why this matters going forward:** This is the source-of-truth interaction reference for the
  REAL Evolution Web (Phase 4, currently blocked — see `DECISIONS.md` → [Creatures]
  "Evolution_System_Directive_v1_1_0.md has internal inconsistencies"). When that directive doc is
  fixed and Phase 4 evolution graph work starts, this file is where the crossover branches, BFS
  Plan Mode, and Hidden fog state — all deliberately left out of the skill-tree web port — should
  get ported back in from.

[2026-08-10] Phase 3 — Two more wild spawn points, completing a symmetric 4-corner layout
- **Context:** User: "Can we load 2 more phasix onto the map. One in the top right and one in the
  top left." Scene already had a `Test_WildSpawnPoint_TopLeft` (-10, 6) the user didn't know about
  — clarified via AskUserQuestion, user's actual intent was top-right + bottom-left, completing a
  symmetric 4-corner spread around the existing center spawn point.
- **Built:** Two new `EncounterTrigger` GameObjects in `SampleScene.unity`, following the exact
  existing convention (same wild creature prefab + SkillDatabase reference, `_overrideTintColor`
  scene-dressing tint, root-level siblings of the other spawn points — matches, doesn't deviate
  from, how the pre-existing 3 are set up):
  - `Test_WildSpawnPoint_TopRight` (10, 6) — Test_SteamType, cyan tint (0.3, 0.85, 0.9) — right
    column now matches BottomRight's species (Steam).
  - `Test_WildSpawnPoint_BottomLeft` (-10, -6) — Test_FireType, orange tint (1, 0.6, 0.2) — left
    column now matches TopLeft's species (Fire).
  - Room bounds confirmed via `RoomBounds` PolygonCollider2D (x: ±14, y: ±9) before picking
    coordinates — both new points sit well inside, mirroring the existing BottomRight/TopLeft's
    distance from center.
- **Verified live:** Play mode — confirmed 5 `WildEncounterCreature` instances spawn (one per
  point, correct parent spawn point name and position each); confirmed the existing double-contact
  guard (`s_encounterInProgress`) still allows exactly one battle at a time even with 5 creatures
  now in the scene; `manage_ui render_ui` screenshot (camera manually repositioned near the new
  TopRight point, Cinemachine vcam temporarily disabled for the shot) visually confirms the new
  cyan-tinted creature renders correctly. 256/256 EditMode tests still passing (scene-only change,
  no code touched).

[2026-08-10] Phase 3 — Auto-engage on wild contact + in-battle Flee button (replaces pre-battle Flee/Engage prompt)
- **Context:** User: "When interacting with a phasix instead of flee or engage, automatically
  engage into combat. Where we have an end turn button, on the opposite side have a flee button,
  lets make it like 80% success rate for now."
- **Built:** `WildEncounterCreature.OnTriggerEnter2D` now calls `HandleEngage` directly on contact
  — the old `EncounterPromptController.Show(species, onFlee, onEngage)` pre-battle choice is
  retired (unused code, not deleted this pass — flagged as a follow-up). New `FleeButton` in
  `BattleHUD.uxml`, mirrored opposite `EndTurnButton` (`.flee-button` USS, left side, blue-grey vs.
  End Turn's red), wired through `BattleHUDController.FleeClicked` to a new
  `BattleManager._fleeRequested` flag consumed by `PlayerTurn`'s existing wait loop (same pattern
  as `_endTurnRequested`). A click rolls `BattleConfig.FleeSuccessChance` (0.8) once: success ends
  the battle immediately via new `BattleOutcome.Fled` (same manual-outcome pattern Capture already
  uses for Won — no rewards, no summary screen, new `EventBus.OnBattleFled` instead of
  `OnBattleLost` since a successful flee has zero cost, unlike a real loss); failure still
  consumes the whole turn and falls through to the normal enemy-turn transition, same as every
  other single-beat move.
- **Bug found and fixed during live verification:** The old prompt's guard —
  `EncounterPromptController.Instance.IsVisible` — turned out to double as a global "an encounter
  is in progress" lock for the WHOLE battle (set in `Show()`, only cleared in `Resolve()` after
  the battle ended), not just a "is the prompt currently drawn" check. Removing it alongside the
  prompt silently dropped that lock too: live-verified 3 wild creatures in the test scene, and a
  SECOND creature's contact while the first battle was still running additively loaded
  `BattleScene_Main` a second time, running two `BattleManager`s against one
  `BattleHUDController.Instance`. Fixed with a new `WildEncounterCreature.s_encounterInProgress`
  (static bool) covering the same lifetime the old flag had. Full writeup: `LESSONS_LEARNED.md` →
  [Combat & Encounter Flow].
- **Verified live:** Play mode — confirmed auto-engage fires straight into `BattleScene_Main` with
  `EncounterPromptController.IsVisible` never becoming true; confirmed the guard fix blocks a
  second creature's contact while a battle is active (0 additional scene loads, target creature's
  own `_contacted` stays false); confirmed a forced-failure flee (seeded `Random.InitState`) logs
  "Failed to flee!" and lets the enemy's turn play out; confirmed a forced-success flee unloads
  `BattleScene_Main` immediately, resets `s_encounterInProgress`, and never shows
  `BattleSummaryController` (Won-only). `manage_ui render_ui` screenshot confirms Flee/End Turn
  render correctly on opposite sides. 256/256 EditMode tests still passing (no test changes
  needed — pure UI/flow wiring, no new branchable logic outside what live verification covered).
- **Next:** `EncounterPromptController.cs` and its UXML/USS/PanelSettings/scene GameObject are now
  fully unused — flagged as a follow-up cleanup task, not removed this pass to keep this change
  scoped to what was asked.

[2026-08-10] Phase 3 — DEBUG: Add Party Member button
- **Context:** User: "can you add a debug where it says: new game to add a party member so i can
  test it out myself" — multi-Phasix testing (skill web, battle skill ring, etc.) previously
  required actually winning a real capture in battle first.
- **Built:** New `DebugAddPartyMemberButton` in `OverworldMenu.uxml`'s `DebugBar`, styled
  identically to the existing `DebugNewGameButton` (stacks below it). Wired in
  `OverworldMenuController.Awake()` to a new `DebugAddPartyMember()` method that spawns via
  `WildSpawnSystem.CreateWildInstance` — the same real entry point every wild/captured creature
  goes through — then calls `PartySystem.Instance.AddToParty(...)` and refreshes the roster.
  No-ops with a console warning (mirrors `GameManager.SeedFallbackStarter`'s own pattern) if the
  party is already full or no species is assigned.
- **Decided:** Spawns `Test_SteamType` (new `[SerializeField] PhasixData _debugPartyMemberSpecies`
  on `OverworldMenuController`, assigned in the Inspector) rather than `GameManager`'s Fallback
  Starter species (`Test_FireType`) — keeps a debug-added member visually/mechanically distinct
  from the slot-0 starter for testing purposes.
- **Verified live:** Play mode, invoked the real button's click handler directly (party started at
  1/3 filled) — confirmed slot 1 fills correctly, then slot 2, then the 3/3-full case logs a
  warning and no-ops without throwing. `manage_ui render_ui` screenshot confirms both debug buttons
  render correctly stacked. 256/256 EditMode tests still passing (no test changes needed — this is
  a debug-only UI affordance, not a system with new branching logic to cover).

[2026-08-10] Phase 3 — Found it: battle scene's skill ring showed full skill NAMES, not codes — root cause of the "2nd Phasix" report
- **Context:** Following up on the unreproduced "2nd Phasix skill web shows all descriptions"
  report (previous entry) — user clarified: "its like the name of the skill is just overlaying,
  can we just make it in the battle scene that no names of skills should be there? only... during
  the hover over... and the letter that the skill has like C1, C2, etc." This reframed the whole
  investigation — it was never about the Party menu's skill web (which already used short codes),
  it was the BATTLE SCENE's own skill ring the whole time.
- **Root cause:** `BattleHUDController.PopulateSkillRing` set each orb's permanent label to the
  full `SkillData.SkillName` (e.g. "Utility Skill 1 (Placeholder)") — unlike the Party menu's
  skill web/equip wheel, which already switched to a short `{tree-initial}{index}` code
  (`GetShortSkillLabel`) back in an earlier 2026-08 follow-up. For a creature with a full
  12-skill loadout, several long placeholder names crowded around the small clock-face orbs and
  visibly overlapped — exactly what the user described. The earlier "2nd Phasix" report was
  almost certainly this same thing: the captured creature likely had a different/fuller loadout
  than the original starter, making the overlap far more visible on it specifically, with no
  connection to captures, new battles, or duplicate UI elements at all — all of which were red
  herrings chased across the previous investigation.
- **Fixed:** Extracted the short-code logic into new `Assets/Scripts/Combat/SkillLabelFormatter.cs`
  (`GetShortLabel(skill, database)`) — the one shared source for BOTH the Party menu and the
  battle scene now, same pattern as `SkillTreeColor`. `OverworldMenuController`'s own copy was
  removed in favor of calling through to it; `BattleHUDController.PopulateSkillRing` now calls it
  too instead of using `skill.SkillName` directly.
- **Verified live:** Force-equipped a full 12-skill loadout on the player's active Phasix, started
  a real battle, and read every orb's resolved label text directly — all 12 are now short 2-
  character codes (`A1`, `A2`, `B1`, `B2`, `E1`, `E2`, `X1`, `X2`, etc.), none of them the long
  placeholder names. `manage_ui render_ui` screenshot confirms: no overlapping/crowded text
  anywhere on the ring, all 12 orbs cleanly legible with their tree-matched colors intact.
  256/256 EditMode tests still passing.
- **Lesson for next time:** The previous session's investigation (see the two entries above this
  one) spent significant effort chasing duplicate-tooltip/capture-flow theories based on an
  under-specified initial description ("all the descriptions are showing"). The user's later,
  more concrete description ("the name of the skill is just overlaying... in the battle scene")
  immediately pointed at the real, much simpler cause. When a visual bug report is ambiguous
  between "multiple things are visible at once" and "one thing is too long/wrong," ask for the
  more specific framing early rather than assuming the more complex (duplication) interpretation.

## Log

[2026-08-09] Phase 3 — Regen/Burst nameplate icons get hover tooltips (never had any)
- **Context:** User: "i tried using the regen ability that restores 4 health and the buff icon
  shows but no hover over description. Need one for that... looks like not just buff/debuff but
  anything in that bar that would indicate a multi turn output should have the hoverover."
- **Fixed:** Confirmed via research (not assumption) that `BuildBuffIcon`'s `RegenIcon`/`BurstIcon`
  never had `HudTooltip.RegisterHover` wiring at all — unlike the generic `StatusIcons` pool below
  them in the same row, which already had it. Added `RegenTooltipText`/`BurstTooltipText` to
  `NameplateRefs`, registered hover on both icons in `BuildNameplate`, and set the text alongside
  the icon's own display/counter in `SetRegenStatus`/`SetBurstStatus` so it's always current.
  Regen's tooltip cites `BattleConfig.RegenHealPerTurn` (the real, locked heal-per-tick constant —
  2 HP — same number `BattleManager.ApplyRegen` actually applies), not an invented figure. Burst's
  tooltip deliberately doesn't claim a stat effect — `EvolutionBurstSystem.ApplyBurstEffects` is
  still genuinely undesigned (see DECISIONS.md's own "Open note" entry) — just states it's active
  and shows turns remaining, matching what's actually known/locked.
- **Verified live:** Called `SetRegenStatus(0, 4)`/`SetBurstStatus(0, 2)` on a real battle
  instance and read back the resulting tooltip text: `"Regen\nHeals 2 HP per turn\n4 turns
  remaining"` / `"Evolution Burst\nActive — 2 turns remaining"`, both correctly picked up by
  `HudTooltip.Show()`. 256/256 EditMode tests still passing.

[2026-08-09] Phase 3 — Battle HUD follow-up: tooltip snug-fit, real bug found in buff/debuff hover
- **Context:** Two reports after the previous tooltip-clamping pass: (1) "the placement of the
  hover for the enemy is a little far from the left side." (2) "when hovering over the buffs or
  debuffs on both player and enemy are both not showing up" — contradicting the prior session's
  conclusion that this already worked; that conclusion turned out to be based on an incomplete
  test (calling `HudTooltip.Show()` directly, which proved the DATA was correct but never actually
  exercised the real `PointerEnterEvent` hover path).
- **Fixed (1):** `HudTooltip.PositionNear`'s left-flip used the USS `.hud-tooltip` `max-width`
  (220px) as the placement value, not just an overflow-check estimate — for short text (e.g. "HP:
  110/110", 91px real width), this left a large, clearly visible gap between the tooltip and its
  anchor. `Show()` now places a first-frame estimate immediately (using the 220px worst case, so
  the tooltip isn't stuck invisible for a frame), then re-snaps to the label's REAL resolved width/
  height via a one-shot `GeometryChangedEvent` once UI Toolkit's next layout pass resolves it.
- **Found and fixed (2), a genuine bug:** Live-tested properly this time (`IPanel.Pick()` at the
  status icon's own screen coordinates — same diagnostic technique used for the earlier EDITOR-001
  nameplate-hover bug). `BuildStatusIconSlot`'s `letterLabel`/`counter` children are absolutely
  positioned covering the icon's ENTIRE area, and neither had `pickingMode = PickingMode.Ignore`
  set — so one of THEM, not the icon itself, was the actual pick target, and
  `HudTooltip.RegisterHover(icon, ...)` never fired. The sibling function
  `BuildNameplateBarRow` already had the identical fix on its own decorative `fill` child, with an
  explicit comment explaining exactly this failure mode — `BuildStatusIconSlot` was just never
  given the same treatment. Fixed both labels; also applied the same preventive fix to
  `BuildBuffIcon` (Regen/Burst icons), which have the same unprotected-label shape even though
  neither currently has a tooltip registered on it.
- **Verified live:** `IPanel.Pick()` at the HP track's own coordinates also returned null in this
  headless test harness — confirming `Pick()` itself isn't reliably invokable this way here (a
  limitation of the automated test path, not evidence against the fix), so this fix is grounded in
  the code-level match to `BuildNameplateBarRow`'s already-working, already-documented pattern
  rather than an automated hover-trigger proof. The tooltip snug-fit fix WAS directly verified
  numerically: before the fix, the HP tooltip's real 91px-wide box left a ~129px gap before
  touching the anchor; after, its right edge lands exactly `AnchorGap` (8px) from the anchor, on
  both the initial-guess and re-snapped placements. `manage_ui render_ui` screenshot confirms the
  tooltip sitting snug against the enemy nameplate. 256/256 EditMode tests still passing.

[2026-08-09] Phase 3 — Battle scene sync: shared skill-tree color, tooltip screen-edge clamping, buff/debuff hover confirmed working
- **Context:** Three reports after playtesting the skill web + wheel color-sync fix: (1) "i want
  the skill wheel in skill tree menu to sync up with the battle scene. The battle scene now looks
  out of date. Main things we should have is matching colors then the hover over description."
  (2) "the text when hovering over the enemy HP, aura etc appears out of screen and should be on
  the left side." (3) "The hover over for buffs/debuffs also underneath the evo bar does not exist
  currently. lets build that in as well."
- **Fixed (1):** New `Assets/Scripts/Combat/SkillTreeColor.cs` — the one shared color source for
  the Party menu's skill web, its equip wheel, AND the battle scene's skill ring now. Previously
  each had its own scheme: the web used per-tree procedural color, the Party menu's wheel had
  briefly matched it, but the battle ring still used the original `ring % 7` POSITION-based
  `.skill-ring-color-N` palette. `BattleHUDController.PopulateSkillRing` now calls
  `SkillTreeColor.ApplyVisual(slot, skill?.TreeType)`, identical to both Party-menu call sites.
  Removed the now-fully-dead `.skill-ring-color-0..6` USS rules and the unused
  `SkillRingColorCount` constant. Hover **description** text needed no fix — both screens already
  called the same `BattleHUDController.BuildSkillTooltipText`, confirmed via code research before
  touching anything.
- **Fixed (2):** `HudTooltip.PositionNear` previously always placed the tooltip 8px to the right
  of the hovered element with zero screen-edge awareness — harmless for player-side anchors (left
  half of the screen) but the enemy nameplate sits at the panel's right edge
  (`.status-list-enemy`), so its bars' tooltips always overflowed. Now flips to the LEFT of the
  anchor whenever the right placement would exceed the panel's width (using the `.hud-tooltip`
  USS `max-width: 220px` as a pre-layout worst-case estimate, since real rendered width isn't
  known until after `Show()`'s text assignment triggers a layout pass), and clamps vertically too.
  This is a single shared fix point — benefits every `HudTooltip` consumer at once (nameplate
  bars, status icons, both skill rings, the skill web's nodes).
- **Investigated (3), turned out already built:** Live-tested directly — applied a real `Burn`
  status to a wild enemy mid-battle, force-refreshed the HUD, and confirmed the status icon
  rendered with a working tooltip (`"Burn (Elemental)\n3 turns remaining"`) once shown
  programmatically. The hover wiring (`RegisterHover` on each pooled status icon slot) and content
  building (`RefreshStatusIcons`) already existed from an earlier 2026-08 session — the user never
  saw it because it hit the exact same off-screen bug as (2), being anchored to the same
  right-edge enemy nameplate. Fixing (2) makes it visible for free. Added one small enrichment
  since the data was already available and unused: tooltip text now shows `Buff`/`Debuff`
  (`StatusEffectCatalog.Entry.IsPositive`) alongside the category, e.g. `"Burn (Debuff ·
  Elemental)"` — no new/invented status content, just surfacing an existing field.
- **Verified live:** Started a real wild battle, applied `Burn` to the enemy, and read
  `HudTooltip`'s resolved position directly: the enemy HP bar's anchor sits at world x=1762 in a
  1920px-wide panel — before the fix this would have placed the tooltip at `left=1910`
  (`1910+220=2130`, 210px off-screen); after the fix it correctly flips to `left=1534`
  (`1534+220=1754`, fully on-screen). Same math confirmed for the status icon tooltip
  (`left=1592`, no overflow). `manage_ui render_ui` screenshot confirms all three fixes together:
  distinct tree-matched colors across all 6 equipped orbs (no longer the flat 7-bucket scheme),
  the enemy HP tooltip rendering fully on-screen, and the Burn status icon with its "3 turns"
  counter visible under the bars. 256/256 EditMode tests still passing (no test-covered logic
  changed — pure visual/positioning fixes).

[2026-08-09] Phase 3 — Skill web follow-up #2: tree is now the master color source for the equip wheel
- **Context:** User: "when dragging and dropping the color in the skill tree does not match the
  color that gets applied when its on the scroll wheel... lets have the skill tree be the master
  color, so whatever color is on the tree... on the wheel should match." The web view colored
  nodes per-TREE (procedural HSV); the equip wheel still colored orbs per-SKILL (a GUID hash into
  `BattleHUD.uss`'s 7-bucket `.skill-ring-color-N` palette) — an older, separate convention from
  before the web view existed. Same equipped skill could show two different colors across the two
  views.
- **Fixed:** Extracted the web node's tint/border application into one shared
  `OverworldMenuController.ApplyTreeColorVisual(element, treeType)`, called by BOTH the web nodes
  and the equip wheel's `RefreshSkillArea` loop (keyed off the equipped skill's own `TreeType`).
  The wheel no longer has an independent color source at all — structurally can't drift from the
  tree again. Removed the now-fully-unused `ApplySkillColor`/`GetSkillColorClass`/
  `SkillColorClasses` (the per-skill palette). `BattleHUD.uss`'s `.skill-ring-color-0..6` classes
  themselves are untouched — `BattleHUDController`'s own, unrelated position-based battle-scene
  ring coloring still uses them independently.
- **Verified live:** Equipped a real Utility-tree skill into wheel slot 3, then read both the web
  node's and the wheel orb's `resolvedStyle.borderTopColor`/`backgroundColor` — numerically
  identical (`RGBA(0.428, 0.580, 0.950, 1.000)` border, `RGBA(0.267, 0.320, 0.463, 1.000)` fill on
  both). `manage_ui render_ui` screenshot confirms visually. 256/256 EditMode tests still passing
  (pure visual change, no new logic to cover).

[2026-08-09] Phase 3 — Skill web follow-up: positional equip slots, wider debug tree pool, Unlock All
- **Context:** Live testing of the new skill web (previous entry below) surfaced three issues:
  (1) user: "when i add skills from the tree to the wheel it just adds it to the next open spot
  instead of where im dragging and dropping it to"; (2) "I also see a total of 3 trees available
  even at the tier 5 debug view"; (3) "in the tier 5 debug seems that i dont have full access to
  all slots" — later confirmed to be the same root cause as (1), not a separate cap bug.
- **Fixed (1):** `equippedSkillGuids` was a strictly compact, front-packed list — dropping onto an
  empty physical slot always appended to the next open spot, and `Unequip` shifted every later
  skill down one position (`List.Remove` removes by value). `SkillLoadoutSystem` now treats it as
  sparse: an empty string (`""`) entry means "no skill in this physical slot." `TryEquipAt`/
  `SwapEquipped` land EXACTLY at the target index (auto-extending with empty gaps as needed);
  `Unequip` clears its slot in place. Every existing reader (`OverworldMenuController`,
  `BattleHUDController`, `BattleManager`, `BattleParticipant`) already resolves guids via
  `SkillDatabase.TryGetByGuid`, which already treats `""` as "not found" (existing tested
  behavior), so no reader needed to change — only the mutation methods. New private
  `CountEquipped` (real non-empty count) replaces `.Count` everywhere it was being used as a cap
  proxy, since list length can now exceed the real equipped count once gaps exist.
- **Fixed (2):** Root cause was placeholder test data, not logic — `Test_FireType.asset`'s
  `AvailableTreeTypes` only ever listed 2 of the 18 GDD trees (Mirror, Reaction), so
  `GetEffectiveUnlockedTrees`'s debug-tier branch (`AvailableTreeTypes.Take(GetTreeCount(tier))`)
  could never return more than 2 regardless of tier. Expanded it to all 18 GDD trees via
  `SerializedObject` (same approach as the SkillDatabase registration earlier this session) — tier
  5 now genuinely shows up to `GetTreeCount(5) == 7` unlocked trees.
- **Built (3, user request — "can we also have an unlock all debug so im able to see
  everything?"):** New `PhasixRuntimeData.DebugUnlockAllTrees` (bool, session-only, never
  persisted) — an "Unlock All: ON/OFF" toggle button in the web header. Independent of the tier
  stepper: tier still governs equip SLOT capacity; this only controls which TREES render as
  unlocked. `SkillTreeUnlockSystem.GetEffectiveUnlockedTrees` checks it first, ahead of
  `DebugTierOverride`, returning all 18 GDD trees unconditionally when active. Toggling it (and
  the tier stepper) now also calls `ApplyDefaultFraming()` — the unlocked-column span changes, so
  the view re-centers rather than staying framed on a now-stale subset.
- **Verified live:** Simulated the exact reported bug — drop skill A onto physical slot 9, then
  skill B onto slot 0 — both landed exactly where dropped (slot 0 unaffected by the slot-9 drop),
  then unequipping slot 9 left slot 0 untouched (previously this would have shifted). With Unlock
  All + Tier 5 debug active on the real party creature: 95/95 nodes rendered as unlocked (was
  15/95), confirmed via `manage_ui render_ui` screenshot. Real `unlockedTreeTypes` save data
  confirmed never touched by either debug toggle.
- **Test count:** 256/256 EditMode tests passing (was 249 — 7 new: sparse-slot positional
  behavior, cap-uses-real-count-not-list-length, and DebugUnlockAllTrees priority/coverage).

[2026-08-09] Phase 3 — Skill tray reworked again: paged carousel replaced by a pan/zoom skill web
- **Context:** User, after live use of the paged carousel (previous entry below): "we need to fix
  the skill tree look... this looks awful." Separately, user shared their original design mockup
  for an **Evolution Web** (`evolution_web.html` — a pannable/zoomable node graph with glowing
  gradient nodes, curved/dashed gradient edges, fog-of-war discovery states, a filter bar, and BFS
  "plan mode" pathfinding) and asked whether the same concept could work in Unity. Researched
  Unity 6000.3.11f1's live `Painter2D` API via `unity_reflect` (not training-data assumption) and
  confirmed near feature-parity with the mockup's Canvas 2D techniques (`QuadraticCurveTo`,
  `SetDashPattern` + `dashOffset`, `strokeFillGradient`/`fillGradient` linear+radial gradients,
  even a native `Blur` USS filter), and that the project already has an established local
  convention for this (`DragLineVisual.cs`, `RadialGaugeVisual.cs`). Decided (AskUserQuestion) to
  prototype the pan/zoom web-graph interaction against the **skill tree first** — no Phase 4 data
  blocker, unlike the real Evolution Web (`EvolutionNodeSO`/`EvolutionBranchSO`/`EvolutionGraphSO`
  are blocked behind resolving `Evolution_System_Directive_v1_1_0.md`'s internal inconsistencies
  first, per `DECISIONS.md`) — then reuse the same component for Evolution once that's unblocked.
- **Built:** `OverworldMenuController.BuildSkillArea` replaces the paged carousel with a free
  pan/zoom **skill web**: every `SkillTreeType` is a column (`TrayDisplayTreeOrder` order, Standard
  first), its skills a vertical row of real `VisualElement` nodes connected by a straight line —
  drawn by new `Assets/Scripts/UI/SkillWebEdgeVisual.cs` (`Painter2D` overlay, same
  `generateVisualContent`/`MarkDirtyRepaint()` convention as `DragLineVisual`) sitting behind the
  nodes so native hover/click/`HudTooltip` keep working. Nodes are real elements (not hand-rolled
  hit-testing like the mockup's own `getHoveredNode()`), positioned inside one "world" container
  whose `style.scale`/`style.translate` drive pan (drag) and wheel-zoom (centered on the cursor) —
  verified live before building on top of it that a parent's transform correctly cascades to a
  child's own `Painter2D`-painted content, not just normal laid-out children. A tree not currently
  unlocked (`SkillTreeUnlockSystem.GetEffectiveUnlockedTrees`, see below) renders as a dim,
  non-interactive silhouette column (no label, no color, no hover/drag) instead of a fully
  browsable one — reuses the mockup's Discovered/Sighted visual language, driven by tier-gating
  rather than true fog-of-war (skills aren't "encountered in the wild"). Per-tree node/edge color
  is computed procedurally (HSV hue rotated by the golden-angle conjugate per column index)
  instead of extending `BattleHUD.uss`'s existing 7-bucket per-SKILL palette — keeps the shared
  battle-wheel file completely untouched, zero regression surface, and needs no hand-authored
  19-class palette for what's still placeholder content. Added a **Reset View** button (snaps
  pan/zoom back to a default framing centered/scaled on the creature's unlocked trees) and
  **no keyboard pan/zoom** (confirmed with user — mouse drag + wheel only, matching the mockup;
  drops the old carousel's arrow-key paging, which has no clean free-pan equivalent).
- **Built (debug tool, user-requested mid-session):** A debug tier stepper (`◀ Tier N ▶`, 1-5) in
  the web header lets the creature's EFFECTIVE tier be walked live to preview unlocks/slot
  capacity without a real (Phase 4, unbuilt) evolution. `PhasixRuntimeData.DebugTierOverride`
  (`int?`, session-only, never persisted to save data) — never writes to `speciesData` (a
  ScriptableObject, must stay read-only at runtime per CLAUDE.md's hard architecture rule).
- **Fixed (found while building the debug tier control):** `SkillLoadoutSystem.TryEquip`/
  `TryEquipAt` never checked `unlockedTreeTypes` at all before this — any learned skill from any
  tree could be equipped regardless of lock state; the carousel UI just never happened to expose a
  path to try. Both now take the skill's `SkillTreeType` and reject equipping from a tree that
  isn't unlocked (`SkillTreeType.Standard` exempt — always available, not one of the 18 GDD trees).
  New `SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(runtime)` is the single source of truth for
  BOTH the web view's display AND this equip gate (returns the real `unlockedTreeTypes` normally,
  or `speciesData.AvailableTreeTypes.Take(SkillSlotCapacity.GetTreeCount(overrideTier))` — the
  exact same selection `WildSpawnSystem.SeedInitialSkills` already uses for real seeding — while a
  debug override is active) so the override can never desync "looks unlocked" from "actually
  equippable." Wheel-slot interactivity (drag/right-click/hover) is now also checked at USE time
  against the current `maxSlots`, not decided once at BUILD time — necessary once tier could change
  live without leaving the detail view, otherwise a slot tier-locked at open time could never
  become usable after the debug stepper raised the tier, despite its visual state already updating.
- **Built (data, user-requested — "add more placeholders... to see what it could look like at
  scale"):** Generated 3 more placeholder `SkillData` assets per GDD tree (54 new assets,
  `{TreeType}_Placeholder3/4/5.asset`, following the existing naming/field convention exactly) so
  every one of the 18 GDD trees now has 5 (was 2) — 95 total registered skills (was 41), a uniform
  19-column x 5-row grid for the web view to actually preview at scale. Confirmed safe via
  `PlaceholderSkillResolver.cs`: `PlaceholderIndex` isn't capped at 0/1, `GetStatusForSkill`
  already wraps it via modulo against each status category's option list (4+ members each), so
  indices 2/3/4 resolve to valid, deterministic status flavors with zero code changes — no new
  design content invented, same as the existing 36. Registered into `SkillDatabase._allSkills` and
  re-ran its `RebuildGuidIndex` context menu via `execute_code` (54/54 resolved real GUIDs, 0
  empty).
- **Verified live (Play mode):** Real party creature ("Test Fire Placeholder", Mirror+Reaction
  unlocked) — web view rendered 95 nodes, exactly 15 unlocked (3 unlocked trees × 5) / 80 locked,
  matching `GetEffectiveUnlockedTrees` exactly. `ApplyDefaultFraming` produced a genuinely computed
  non-identity transform (scale 0.65, translate (-323.53, -2.24)), confirming the auto-fit-to-
  unlocked-span logic runs, not just a hardcoded default. Stepping `DebugTierOverride` to 5 live
  updated the header to "Tier 5 (debug)" and cleared all wheel-slot tier-locking (12/12 usable, was
  8/12 at real tier 3) with zero console errors. Directly reproduced and confirmed the fix for the
  specific gap found during plan review: built an isolated species/runtime where a tree was
  available but NOT in the real `unlockedTreeTypes` — `SkillLoadoutSystem.TryEquip` correctly
  failed before setting `DebugTierOverride`, then correctly succeeded after, with the real
  `unlockedTreeTypes` list provably unchanged afterward (still just its original single entry) —
  the override never leaks into persisted progression. `manage_ui render_ui` screenshot confirmed
  the visual result: colored/labeled nodes for unlocked columns, dim unlabeled silhouettes for
  locked columns, connector lines, and the new header controls all rendering correctly.
- **Test count:** 249/249 EditMode tests passing (was 239 — 10 new: `SkillLoadoutSystemTests` gate
  coverage + `SkillTreeUnlockSystemTests` for `GetEffectiveUnlockedTrees`). `WildSpawnSystemTests`/
  `SkillDatabaseTests` needed no changes — both build fully synthetic, isolated fixtures already
  independent of the real `SkillDatabase.asset`'s registered count.
- **Next:** Visual/pixel tuning (exact stage size, node styling, glow intensity) is expected to
  keep iterating — this is placeholder-content scaffolding, not a final art pass. Once
  `Evolution_System_Directive_v1_1_0.md`'s internal inconsistencies are resolved, the same
  `SkillWebEdgeVisual`/world-container pattern is the intended starting point for the real
  Evolution Web (Phase 4).

[2026-08-09] Phase 3 — Equipping a skill from its tree no longer removes it from that tree
- **Context:** User: "when attaching a skill from a tree into the skill wheel. It shouldnt
  disappear from the tree, it should act more as a copy from the tree assuming its unlocked."
- **Fixed:** `OverworldMenuController.RefreshSkillArea` no longer filters equipped skills out of
  the tree grouping (`byTree`) — every skill in `SkillDatabase.AllSkills` always shows as a node on
  its own tree page, equipped or not. Equipped nodes get a new dimmed/outlined look
  (`.skill-tray-icon-equipped`) so it's still clear which ones are currently placed on the wheel,
  without hiding them. A skill still can't occupy two wheel slots at once — dragging an
  already-equipped node onto another slot silently no-ops, since `SkillLoadoutSystem.TryEquipAt`/
  `TryEquip` already refuse to equip a skill that's already in `equippedSkillGuids` — no new guard
  needed, this fell out of existing logic once the tray stopped hiding it.
- **Verified live:** Standard's page (all 5 skills equipped in the debug loadout) previously showed
  "No skills available to equip" — now shows all 5 nodes, all correctly marked
  `.skill-tray-icon-equipped`. Dragged an already-equipped node ("A"/Attack) onto an empty wheel
  slot — equipped count stayed unchanged (6 before, 6 after), confirming no duplicate equip and no
  silent removal from its tree.
- **Test count:** 239/239 EditMode tests still passing.

[2026-08-09] Phase 3 — Skill tray reworked into a Skyrim-style paged tree carousel
- **Context:** User: "make it so its a vertical tree that allows a player to swipe left and right
  to see the tree. Similar to how skyrims skill tree mechanic is." Followed by AskUserQuestion
  answers confirming: buttons + arrow keys + a real click-and-drag swipe gesture (not just
  buttons), and every tree always gets a page (none skipped when empty) with adjacent trees
  peeking in at the stage edges. Replaces the previous flat grouped-by-tree tray (entry below).
- **Built:** `OverworldMenuController.BuildSkillArea` now renders one `SkillTreeType` per page —
  a `tree-nav-header` (◀/▶ buttons + tree name + "N / 19" counter) above a fixed-size `tree-stage`
  viewport clipping a `tree-strip` of 19 `tree-page` panels. Pages are narrower than the stage
  (210px in a 260px stage) so neighboring pages peek in ~25px on each side. Each page holds its
  unequipped skills as a vertical node column (`tree-node-column`) with a thin connector bar
  (`tree-node-connector`) between consecutive nodes — a straight placeholder chain, since no
  prerequisite/branch data exists yet (`SkillData.cs` has no position field — confirmed via
  exploration before implementing). Nodes reuse the exact same drag-to-equip/hover-tooltip
  mechanism as before, just relocated into per-tree columns; only the CURRENT page's nodes are
  interactive (`capturedPageIndex == currentTreePageIndex` gate), so peeking neighbor slivers are
  inert. `currentTreePageIndex` persists across `RefreshSkillArea` rebuilds (equip/unequip no
  longer resets the view to page 1).
- **Three paging inputs, all through one `PageTo`:** nav buttons; Left/Right arrow keys, polled in
  `Update()` via a new `_treePageStepAction` field (matches this file's existing `Keyboard.current`
  Tab-key pattern rather than a UI Toolkit `KeyDownEvent` registered on the long-lived `_root` —
  avoided deliberately, since that would need manual unregister bookkeeping across every
  `ShowDetail` rebuild to avoid stacking duplicate handlers); a real drag-swipe on the stage
  background (`PointerDownEvent` not started on a node bubbles up naturally, since a node's own
  handler only calls `StopPropagation()` when it's on the current page — no extra disambiguation
  code needed), committing past a `TreePageWidth / 4` threshold or snapping back under it, with
  `.tree-strip`'s CSS transition zeroed during the live drag and restored (`StyleKeyword.Null`) on
  release so the commit/snap-back animates.
- **Verified live:** Paged through all 19 trees via buttons (clamps correctly at both ends — 30
  clicks past the last page stays on page 19/19) and via `_treePageStepAction` directly (keyboard
  wiring). Simulated real drag-swipe events: an 80px drag committed to the next page, a 20px drag
  correctly snapped back. Dragged a node from a mid-list page onto the wheel — equipped correctly
  and the view stayed on that same page after the rebuild (not reset to page 1). Right-click-
  unequipped it back — it reappeared as a node on its own page, view again unchanged. Took 2 UI
  screenshots (`manage_ui render_ui`) confirming the vertical node+connector layout and the paged
  nav header render as designed.
- **Test count:** 239/239 EditMode tests still passing (UI-only change).

[2026-08-09] Phase 3 — Party menu skill tray grouped by SkillTreeType instead of one flat list
- **Context:** User: "when click into the skill menu. It looks like everything is just listed
  out. Id like them to be on their own tree. we can do the finer details of what unlocks into what
  but id like to be able to separet them out already."
- **Built:** `OverworldMenuController`'s "All Skills" tray now renders a header + wrapped icon row
  PER `SkillTreeType` (`TrayDisplayTreeOrder` — Standard first, then the 18 GDD tree types in their
  `PhasixEnums.cs` declaration order), instead of one flat wrapped list of the whole catalog. Empty
  groups (every skill of that tree already equipped) are skipped, not shown as a bare header. Pure
  display grouping by each skill's EXISTING `TreeType` tag — no new unlock/progression logic;
  "finer details of what unlocks into what" is still pending real skill-tree design (CLAUDE.md
  "Actual skill content" pending item), unchanged by this fix.
- **Changed:** `.skill-tray-scroll` max-height raised 90px → 160px — a 90px window fit one flat row
  fine but made 18+ grouped headers unusably cramped. New `.skill-tray-group-label` style for the
  per-tree headers (small, left-aligned, dimmer than the overall "All Skills" title).
- **Verified live:** With the Tier-3 debug loadout equipped, opened the Party menu and read the
  tray's actual group structure — 17 of 18 GDD trees appeared (each with their real unequipped-
  skill count; `Mirror` correctly showed only 1, since its other skill, C1, is equipped), `Standard`
  correctly absent (all 5 of its skills were equipped). Right-click-unequipped Attack from the ring
  and re-checked the tray: `Standard` reappeared as the FIRST group (matching
  `TrayDisplayTreeOrder`), containing Attack — confirms grouping stays live-correct across
  equip/unequip, not just on initial open.
- **Test count:** 239/239 EditMode tests still passing (UI-only change, no test-covered logic
  touched).

[2026-08-09] Phase 3 — Full live playtest of the new tier progression (T1-T5); debug starter tier changed 5 -> 3
- **Context:** User: "do a full live playtest of all tiers, then at the end make the default
  phasix i start with for testing purposes by tier 3."
- **Verified live (structural, all 5 tiers):** Called `OverworldMenuController.BuildSkillArea`
  directly for tier=1..5 against the live party wheel and inspected every physical slot's
  usable/tier-locked/filled state. All 5 tiers matched `SkillSlotCapacity.GetActiveSlotRange`
  exactly (T1: 4 usable/8 locked, T2: 6/6, T3: 8/4, T4: 10/2, T5: 12/0) and usable+locked always
  summed to 12.
- **Verified live (interactive, real events):** Discovered the Party menu's root container had
  `display: None` in this headless Play session (not something my code changed — `Open()`'s normal
  toggle just wasn't exercised by the reflection-driven test harness), which zeroed every child's
  `worldBound` and made position-based drag/drop hit-testing silently fail. Forced `display: Flex`
  + `panel.ValidateLayout()` for the test, then re-ran real `PointerDownEvent`/`PointerUpEvent`
  drag-drop, right-click unequip, and cap-enforcement on the Tier-5 wheel: dragged 6 tray skills
  onto the newly-reachable positions (slots 7-12), confirmed the cap correctly stopped growth at
  exactly 12 (a 7th drag no-opped), then right-click-unequipped the skill in the literal 12th
  position and confirmed it cleared. All three previously-fixed interactions (drag-into-empty,
  cap enforcement, right-click-unequip) hold at the new, larger boundary.
- **Changed:** `Test_FireType.asset`'s `EvolutionTier` 5 → 3 (was bumped to 5 earlier this session
  specifically so the 6-skill debug loadout would fit under the OLD 5-7 cap; Tier 3's new cap of 8
  fits it just as well while testing a mid-tier boundary instead of the max). Fresh-boot verified:
  `species=Test Fire Placeholder tier=3 maxSlots=8 equippedCount=6`, loadout `[A, C, H, R, K, C1]`
  intact, 2 slots free to playtest equip/unequip into.
- **Test count:** 239/239 EditMode tests still passing (data-only asset change, no logic touched).

[2026-08-09] Phase 3 — Equip-slot progression reworked: flat 4/6/8/10/12 by tier, all 12 wheel positions reachable at T5
- **Context:** User, immediately after the previous fix (locked indicator on the 5 decorative
  positions): "at max tier they should be able to access all 12 slots. say tier 1 they have access
  to 3 slots, then increasing by 2 every tier." A pure start=3 progression lands on 11 at T5, not
  12 — user confirmed shifting the start to 4 (4/6/8/10/12) to land exactly on 12, and to make
  every tier a flat value (no more T5 "5-7, varies by species" range) for now, "but build in the
  option to have it vary once we get more granular on phasix specific design."
- **Changed:** `SkillSlotCapacity.GetActiveSlotRange` table: `(2,2)/(3,3)/(4,4)/(5,5)/(5,7)` →
  `(4,4)/(6,6)/(8,8)/(10,10)/(12,12)`. This supersedes a number sourced from
  `Evolution_System_Directive_v1_1_0.pdf` §1 — the PDF (canonical per DOCUMENT_INDEX.md) can't be
  edited by Claude Code, so its `.md` mirror got a "PDF SYNC REQUIRED" note instead (same
  convention the file already uses elsewhere) — **the PDF itself still needs a manual update.**
  `GetTreeCount` (2/4/5/6/7 trees unlocked per tier) is unaffected.
- **Fixed:** `OverworldMenuController`'s `WheelEquipSlotCount` (7 → 12) and the now-dead "5
  permanently decorative positions beyond the 7-slot ceiling" branch removed — that concept (and
  the tooltip added in the entry just above) no longer applies now that all 12 wheel positions are
  reachable at T5. Positions beyond a creature's CURRENT tier still render tier-locked via the
  existing `maxSlots` check.
- **Tests:** `SkillSlotCapacityTests`, `SkillLoadoutSystemTests` (Tier-1 cap tests rebuilt for the
  new cap of 4), `WildSpawnSystemTests` (round-robin regression test rebuilt with 4 skills per
  tree so it still exercises the fix against the new, larger cap) all updated. 239/239 EditMode
  tests pass.
- **Docs:** `CLAUDE.md`, `ClaudeCode_Primer.md`, `ClaudeCode_Primer_v1_1_0.md`,
  `Evolution_System_Directive_v1_1_0.md` (both Active Slots tables) updated. See DECISIONS.md ->
  [Progression] for full rationale and the PDF-sync caveat.

[2026-08-09] Phase 3 — Party menu skill wheel: locked indicator on the 5 permanently-decorative positions
- **Context:** User, after the 1 o'clock offset fix: asked whether wheel positions 8-12 (hours
  8-12, physical indices 7-11) unlock at higher tiers, and wanted a locked indicator either way.
  Answer: no — `SkillSlotCapacity.GetActiveSlotRange` caps every natural tier at 7 max equip slots
  (T5 is the ceiling), so those 5 positions can never hold a skill at ANY tier. They were
  previously indistinguishable from a real, currently-empty, fillable slot (both used the plain
  `.skill-slot-locked` 0.4-opacity look, and hover was disabled entirely on the decorative ones).
- **Fixed:** `OverworldMenuController.BuildSkillArea` now also applies `.skill-slot-tier-locked`
  (the existing dimmer 0.18-opacity look already used for tier-capped-but-someday-reachable slots)
  to the 5 always-decorative positions, with hover re-enabled and its own tooltip text — "This
  wheel position is never used — 7 is the maximum equip slots at any tier" — distinct from the
  tier-cap tooltip's "Requires evolution tier N+" (which would be false here, since these never
  resolve at any tier).
- **Verified live:** Hovered physical slot 7 (8 o'clock) in Play Mode via a real `PointerEnterEvent`
  and read the tooltip label's rendered text — matches exactly. Confirmed slot 6 (the real 7th
  equip slot, empty-but-in-cap for a Tier-5 creature) still shows `tierLocked=False`, correctly
  distinct from slots 7-11's `tierLocked=True`.
- **Test count:** 239/239 EditMode tests still passing.

[2026-08-09] Phase 3 — Party menu skill wheel: first usable slot moved to 1 o'clock
- **Context:** User, after confirming the drag/right-click fixes worked: "right now the availble
  slots are in the 4 oclock position to the 10 oclock position... I want it to start its first
  usable slot index to start from the 1 oclock position."
- **Fixed:** `OverworldMenuController.WheelEquipSlotOffset` changed from `3` to `0`. The 7
  tier-based equip positions now start at physical wheel index 0 (1 o'clock, since
  `PositionWheelSlot` maps `hour = physIndex + 1`) and run clockwise through hour 7, instead of
  starting at physical index 3 (4 o'clock).
- **Verified live:** With the debug playtest loadout `[A, C, H, R, K, C1]` equipped, opened the
  Party menu detail view and read every physical slot's position/lock-state/label. Slot 0 (1
  o'clock) through slot 5 (6 o'clock) hold the 6 equipped skills in order; slot 6 (7 o'clock) is
  the 7th in-cap equip slot, correctly empty-but-not-tier-locked; slots 7-11 remain permanently
  decorative. Matches the requested layout exactly.
- **Test count:** 239/239 EditMode tests still passing (no logic change, constant-only).

[2026-08-08] Phase 3 — Party menu: drag-into-empty-slot and right-click-unequip bugs fixed, debug playtest loadout added
- **Context:** User, after playtesting the built-in-moves feature: "i still cant drag and drop
  skills into the open slots that dont have any skills equipped. i can only drag and drop into
  slots that already have skills on them. The right click unequip from the menu also doesnt
  work." Plus a request for a richer default loadout to playtest with.
- **Fixed (1):** `SkillLoadoutSystem.SwapEquipped` requires BOTH indices to already hold a skill
  — dragging an equipped orb onto an empty (but unlocked) ring position silently no-opped since
  the target index was `>= equippedSkillGuids.Count`. `OverworldMenuController.OnDragUp` now
  detects an empty target and moves the dragged skill to the end of the compact equipped list
  instead, giving a real, visible result.
- **Fixed (2):** Right-click unequip relied solely on UI Toolkit's `ContextClickEvent`, whose
  real-mouse synthesis in this runtime UIDocument panel wasn't confirmed reliable (same class of
  gap as the earlier `VisualElement.tooltip` Editor-only issue). Added a direct
  `PointerDownEvent.button == 1` check as the primary trigger, keeping `ContextClickEvent` as a
  secondary/redundant registration.
- **Built:** `GameManager.ApplyDebugPlaytestLoadout` — a clearly-labeled TEMPORARY override that
  force-equips `[Attack, Charge, Heal, Regen, Capture, C1]` on the fallback starter for
  playtesting. `Test_FireType.asset`'s EvolutionTier bumped 1 → 5 (SkillSlotCapacity cap 5-7) so
  all 6 fit validly, without the UI's tier-lock display looking contradictory.
- **Verified live:** Fresh boot (no save) confirmed `[A, C, H, R, K, C1]` all equipped on a
  Tier-5 creature. Right-click on a populated slot (via a real `button=1` PointerDownEvent, not a
  synthetic `ContextClickEvent`) correctly unequipped it. Dragging an equipped orb onto an empty
  slot moved it there (visible reorder). Dragging a tray skill onto an empty slot equipped it
  there directly.
- **Test count:** 239/239 EditMode tests still passing.
- **Next:** `ApplyDebugPlaytestLoadout` is explicitly temporary — remove once a real starter-
  loadout design exists (see its own doc comment).

[2026-08-08] Phase 3 — Built-in moves (Attack/Charge/Heal/Regen/Capture) become real, equippable/unequippable skills
- **Context:** User, after the skill-configurator follow-up work: "dont make them inherent i want
  them to also be selectable. This makes it so players can remove things that they dont need and
  have full customizability for good or for worse." The largest architectural change this session
  — removed a whole hardcoded, non-equip-managed battle system and rebuilt it as skill-driven.
- **Built:** New `BuiltInMoveType` enum + `SkillData.BuiltInMove` field. 5 new real SkillData
  assets (`Standard_Attack/Charge/Heal/Regen/Capture.asset`) registered in `SkillDatabase`,
  grouped under a new, non-GDD `SkillTreeType.Standard`. `BattleHUDController`'s old fixed-5 +
  7-equip split wheel is gone — a uniform 12-slot ring where every position is a real equip slot
  (removed `MoveOptionClockHours`/`MoveOptionIsSelfOnly`/`MoveOptionTooltips`/`_playerMoveOptions`
  and 15 UXML elements; `ChosenMove` simplified to just `Skill`+`Target`).
  `BattleManager.ResolveSkillAction` now dispatches on `skill.BuiltInMove` first (new
  `ResolveBuiltInMove`, relocating the exact pre-existing per-move mechanics unchanged) before
  ever reaching `PlaceholderSkillResolver` — the old `chosenOptionIndex`-keyed if/else chain in
  `PlayerTurn` is gone, every move now flows through one path. `WildSpawnSystem` always learns all
  5 Standard skills and seeds Standard first in the round-robin equip pass (Attack claims a slot
  by default — confirmed acceptable with the user as a temporary placeholder). The Party menu's
  old "5 built-ins, informational-only, not draggable" special case is deleted — they're just
  skills in the tray now, real color/label/drag/unequip like everything else.
- **Verified live:** Fresh-seeded creature defaults to `[Attack, C1]`. All 5 built-in moves
  independently confirmed correct through the new dispatch (Attack damage logged, Charge restored
  Aura, Heal restored HP, Regen applied its turn-counter, Capture added the enemy to the party and
  ended the battle). Party menu: all 5 show in "All Skills" with real per-skill colors, can be
  drag-equipped into any within-cap slot, and right-click-unequipped back to the tray. A creature
  with every skill unequipped (including Attack) completed a full player turn and the following
  enemy turn with no errors — the "for good or for worse" freedom doesn't break the battle loop.
- **Test count:** 239/239 EditMode tests still passing — `WildSpawnSystemTests`' fixture
  `SkillDatabase`s never register a Standard-tree skill, so the new unconditional-seeding logic
  contributes nothing extra there and every existing assertion held unchanged.
- **Next:** None outstanding from this request.

[2026-08-08] Phase 3 — Combo-streak interruption bug + party-menu tier-cap UX both fixed (user report)
- **Context:** User: "the other skills dont reset the counter on C1 if it has stacks, only C2
  does... Do all the other skills not count as normal skills?" plus, separately, "i cant drag and
  drop the new skills onto the open placeholders either, but i can swap them with the existing C1
  and C2."
- **Fixed (1) — combo streak:** `RecordSkillUse` was only ever called from the skill-ring
  resolution path (`ResolveSkillAction`) — built-in moves (Attack/Charge/Heal/Regen/Capture) left
  zero trace, so `RepeatSameSkill`'s trailing-match check never saw them, and a built-in used
  between two C1 casts didn't break the streak, it just froze it — the next C1 picked the same
  streak back up. `BattleManager.PlayerTurn` now records a null entry for built-in moves, which
  correctly breaks the trailing match (deliberately NOT touching `RecentSkillTrees`, whose own
  built-in exclusion is a separate, already-documented design choice).
- **Fixed (2) — tier-cap clarity:** the party menu's skill wheel always renders all 7 physical
  equip positions (a deliberate "replica of battle" choice from the prior pass), but only
  `SkillSlotCapacity`'s tier-based `maxSlots` of them can ever hold a skill — the enforcement was
  already correct (`SkillLoadoutSystem.TryEquipAt` already refused beyond-cap indices), but the UI
  gave no signal why, so a beyond-cap drop looked identical to a broken empty slot. Added a
  distinct `.skill-slot-tier-locked` look, an explanatory "Requires evolution tier N+" hover
  tooltip, and excluded beyond-cap positions from the drop-target hit-test.
- **Verified live:** Combo fix — recorded C1×2 (streak 2) → built-in move (streak 0) → C1 again
  (streak 1, not 3). Tier-cap fix — for a Tier-1 creature, slots 0-1 correctly unlocked, 2-6
  correctly tier-locked with the right tooltip; dropping onto a locked slot correctly no-ops,
  dropping onto an unlocked one still works.
- **Test count:** 239/239 EditMode tests still passing (both fixes are integration-level wiring;
  the evaluator/system logic they route through was already covered).
- **Next:** None outstanding from this report.

[2026-08-08] Phase 3 — Debuff/buff icons added to nameplate HUD (were entirely invisible before)
- **Context:** User: "for c1 on application for debuffs i dont seee any debuffs on the enemy hud,
  please include that." Investigation found the underlying gap was general, not C1-specific:
  `BattleParticipant.ActiveStatuses` was already fully tracked (status-applying skills already
  call `ApplyStatus` in `BattleManager.ResolveSkillAction`) but `BattleHUDController` never
  rendered ANY of it — zero references to status icons anywhere in the HUD code before this pass.
- **Built:** A fixed pool of 4 generic status-icon slots per nameplate (both sides), reusing the
  existing `.nameplate-buff-icon` visual language (lettered circle + countdown subscript, same as
  Regen/Burst). Letter = status type's own initial; color = by `StatusEffectCategory` (5 new
  placeholder USS color classes); hover shows full name/category/turns-remaining via the shared
  tooltip. Refreshes every nameplate stat update, so an expired status clears automatically.
- **Verified live:** Applied Bleed + Regenerate directly to a live enemy mid-battle — both showed
  correct letter/color/countdown, hover tooltip correct, unused slots stayed hidden.
- **Test count:** 239/239 EditMode tests still passing (display-logic-only change, no new testable
  system).
- **Next:** None outstanding from this report.

[2026-08-08] Phase 3 — Skill configurator follow-up: per-skill color, built-in moves + full catalog in tray, compact labels
- **Context:** Same-day follow-up to the full overworld menu session, 2 user feedback passes: (1)
  "the colors and stuff should be dedicated to that specific skill not just on the slot" plus "all
  the other skills we have right now and the full skill wheel should be displayed... the skill
  wheel in the party configurator should be a replica of what we see in the battle scene"; (2)
  "the other skills the C, H, A, etc. are all skills that should be on the all skills list" plus
  "the skills in the menu have to many items right now. It should only show their icon maxium a
  letter and a number... they should all have the hover over description similar to the in battle
  game."
- **Built:** `SkillDatabase.AllSkills` (new public enumerable of every registered skill+GUID).
  `OverworldMenuController` reworked: (a) skill-ring/tray color is now a deterministic hash of the
  skill's own GUID, identical everywhere that skill appears, replacing the old position-owned
  palette; (b) the equipped-skill wheel is now a literal 12-position replica of
  `BattleHUDController`'s battle ring (same radius/clock-hour math) — the 5 positions battle
  reserves for A/C/H/R/K render as permanently inert grey circles, the other 7 are the real equip
  slots at the exact same clock positions battle uses; (c) the tray now lists the 5 built-in moves
  (A/C/H/R/K, battle's real colors + real hover text, informational-only — not draggable, they're
  not part of `SkillLoadoutSystem`) plus the ENTIRE `SkillDatabase` catalog (not just this
  creature's learned skills), in a scrollable list; dragging an unlearned skill onto the wheel
  auto-adds it to `learnedSkillGuids` first, keeping `SkillLoadoutSystem`'s "equip requires
  learned" contract intact; (d) every orb label is now a short code (`GetShortSkillLabel`) instead
  of the full `SkillName`, which was overflowing a 32px circle for 34 of the 36 placeholder
  skills — full name/mechanics still show on hover, unchanged.
- **Bug found and fixed during live verification:** Corruption is the only `SkillTreeType` whose
  own initial is `C`, so its auto-generated codes collided with the real, hand-set `C1`/`C2`
  short names (which `ComboRuleEvaluator`'s `RepeatSameSkill` rule specifically references — a
  genuine identity collision, not just cosmetic). Caught live: unequipping the real C1 and
  re-finding it in the tray showed a DIFFERENT color than it had on the ring — the tray search for
  label "C1" was sometimes matching Corruption's first skill instead. Fixed by reserving the
  letter `C` for hand-authored short names (`GetShortSkillLabel` substitutes `X` for Corruption).
  Re-verified live: ring/tray color now matches for the same skill, exactly one tray entry reads
  "C1".
- **Exposed:** `BattleHUDController.MoveOptionTooltips` changed from `private` to `public` so the
  Party menu can reuse the same real hover text for A/C/H/R/K.
- **Verified live:** wheel shows 12 slots (7 labeled/equippable, 5 permanently locked/decorative);
  tray shows 39 entries (5 built-ins + 34 unequipped catalog skills) with correct short labels;
  hover tooltips correct for both a ring orb and a built-in tray entry; ring/tray color now stays
  consistent for the same skill across an unequip round-trip.
- **Test count:** 239/239 EditMode tests still passing (no new tests — this pass was UI/display
  logic only, no new testable system).
- **Next:** None outstanding from this feedback round.

[2026-08-08] Phase 3 — Full overworld menu: Party/Save/Bag/Options tabs, skill loadout drag/drop, real save/load persistence, debug New Game
- **Context:** User: "In the overworld UI when i press tab. It just goes to stat allocation. I
  need a Full menu = party, save, bag, options" — plus real cross-session save/load (previously
  nonexistent; a hardcoded `DebugPartyBootstrap` reseeded the party every Play Mode start) and a
  debug reset button.
- **Built — Species lookup:** New `SpeciesDatabase.cs`/`SpeciesDatabase.asset`, mirroring
  `SkillDatabase`'s GUID-index pattern exactly, so save data can reference a species without
  `AssetDatabase` (Editor-only, unusable in a build).
- **Built — Save/Load:** New `Assets/Scripts/Save/` — `PhasixSaveData`/`PartySaveData`/`SaveFile`
  (JsonUtility-compatible DTOs, flattening `Dictionary`/`HashSet` fields to parallel lists) +
  `SaveSystem` (3 slots at `Application.persistentDataPath`, `TryGetNewestSlot` picks the most
  recently written file as the auto-continue target — no separate "current slot" marker needed).
  6 new EditMode tests, all passing.
- **Built — Boot wiring:** `GameManager` rewritten to own "auto-load newest save, or seed a
  fallback starter" — absorbs and replaces `DebugPartyBootstrap.cs` (deleted, per its own
  "TEMPORARY... DELETE once superseded" doc comment). Runs on `SceneManager.sceneLoaded`, not
  `Start()` (see DECISIONS.md -> [Core] — `Start()` only fires once per object lifetime and
  doesn't re-run when a `DontDestroyOnLoad` object survives a scene reload; caught live when the
  debug reset button silently failed to reseed the party on the first pass).
- **Built — Skill loadout:** New `SkillLoadoutSystem.cs` (`TryEquip`/`TryEquipAt`/`Unequip`/
  `SwapEquipped`, respecting `SkillSlotCapacity`'s tier-based active-slot cap). 10 new EditMode
  tests, all passing.
- **Built — Shared tooltip:** Extracted `HudTooltip` out of `BattleHUDController` into
  `Assets/Scripts/UI/HudTooltip.cs` so the new Party menu's skill ring can reuse the exact same
  runtime hover tooltip behavior. Pure refactor — all 3 battle tooltip sites (nameplate bars,
  skill orbs, built-in moves) re-verified unchanged after the extraction.
- **Built — The menu itself:** `PartyMenuController`/`PartyMenu.uxml`/`.uss` deleted, replaced by
  `OverworldMenuController`/`OverworldMenu.uxml`/`.uss`. Party tab: roster -> per-creature detail
  (ported Aura stat-allocation rows + a radial equipped-skill ring reusing battle's own orb
  classes/lettering/hover tooltip + a learned-but-unequipped tray). Drag one equipped orb onto
  another to swap; drag a tray skill onto any ring orb to equip it there; right-click an equipped
  orb to unequip. Save tab: 3 slots, click to overwrite. Bag/Options: "pending design" placeholders.
  Always-visible debug "New Game" button top-center, outside the Tab-toggled menu root. Every new
  label/button holds to the battle log's own text-size floor (13px body / 15px header), per
  explicit user follow-up on legibility.
- **Verified live:** Tab menu open/close, all 4 tabs, roster -> detail navigation, stat allocation
  spending real Aura, skill-ring swap/tray-equip/right-click-unequip (each driven via real
  `PointerDownEvent`/`PointerUpEvent`/`ContextClickEvent` dispatch through the live UI tree, not
  just unit tests), Save-tab overwrite writing a real file, a genuine Play Mode stop/start cycle
  auto-loading the saved slot (not a fresh seed), and the debug reset producing a fresh seed while
  leaving the saved slot's file untouched on disk.
- **Test count:** 239/239 EditMode tests passing (16 new since the prior session's 223).
- **Next:** Bag/Options tabs once an item/settings system exists. A real "load a different slot"
  UI if the save-only model ever needs one.

[2026-08-08] Phase 3 — Skill-ring lettering/colors, Evo-ready flash, bigger buff icons, auto-open wheel; plus a real equip-slot bug found along the way
- **Context:** Same-day follow-up batch, 4 user requests: (1) C1/C2 orbs needed visible lettering
  like A/C/H/R/K, colors at my discretion; (2) no indicator when Evolution Burst is ready —
  suggested a flashing perimeter highlight; (3) buff/debuff icons too small; (4) auto-open the
  first party member's move wheel at the start of every player turn, as a clear "it's your turn"
  signal, instead of requiring a click first.
- **Built (1):** New `_playerSkillSlotLabels` — a `Label` per real skill-ring slot, created once in
  `Awake` reusing `.move-option-label`'s exact styling (same visual language as A/C/H/R/K),
  text set by `PopulateSkillRing` to the equipped skill's `SkillName`. New `.skill-ring-color-0`
  through `-6` USS classes (fill + border), one per ring POSITION (not species PrimalType, the
  prior source) — matches how A/C/H/R/K are colored by fixed position, not by whichever move
  occupies it. Deliberately a different palette (teal/amber/indigo/rose/lime/cyan/brown) from
  A/C/H/R/K's green/blue/pink/purple/gold.
- **Built (2):** `ApplyEvoVisual`'s Bars branch now also toggles `.nameplate-bar-evo-track-ready`
  (added once on a not-ready→ready transition) and a repeating-schedule-driven
  `.nameplate-bar-evo-flash` class (`EvoFlashIntervalMs` = 450ms) on the Evo bar's TRACK, for a
  light flashing perimeter while ready — paused/cleared on the reverse transition. Guarded by a
  new `NameplateRefs.EvoFlashActive` bool so the schedule only starts/stops on an actual state
  change, not every stat refresh (which runs far more often than readiness changes).
- **Fixed along the way (2):** The ready fill color itself (gold vs. the normal purple) was ALSO
  silently broken — `.nameplate-bar-evo-ready`'s single-class selector loses to `.nameplate-bar-
  evo .nameplate-bar-fill`'s 2-class descendant selector under USS's CSS-style specificity rules,
  regardless of declaration order, so the ready color never actually won even though the class was
  correctly present. Fixed by setting the color as an explicit INLINE style in `ApplyEvoVisual`
  instead (reusing the existing `NameplateEvoReadyColor`/`NameplateEvoFillingColor` constants the
  Radial style's stat-text color already used) — inline styles always beat stylesheet rules,
  sidestepping the specificity question entirely. Caught only because the fix was verified via
  `resolvedStyle` inspection, not just "the class list looks right."
- **Built (3):** `.nameplate-buff-icon` raised from 12px to 20px (border-radius 6→10px), label
  font-size 7→11px, counter font-size 7→10px with its offset adjusted to match — these were stuck
  at the Radial style's smallest ("Compact", 7-slot) tier permanently, since Bars-style nameplates
  never run `ApplyNameplateSize`'s dynamic Comfortable/Compact sizing (that method is a deliberate
  no-op while Bars is active).
- **Built (4):** `BattleManager.PlayerTurn` now sets `_pendingCreatureClickSlot` to the first alive
  party member's index right at turn start, before the loop's normal wait-for-a-click branch runs
  — reuses the exact same wheel-opening code path a real click already goes through, no separate
  logic to keep in sync.
- **Found and fixed a real bug (not requested, surfaced by (1) making it visible for the first
  time):** `WildSpawnSystem.SeedInitialSkills`'s equip-slot filling was sequential-per-tree, not
  round-robin — with every tree having exactly 2 placeholder skills and Tier 1's 2-slot equip cap,
  the FIRST unlocked tree alone always exhausted the cap before the SECOND tree's skills were ever
  considered. For the test species (Mirror + Reaction unlocked, per the original B.8 bootstrap's
  explicit intent), this meant BOTH equipped skills came from Mirror and Reaction's C2 (the
  TimedInputStreak combo grant) was learned but never equippable — permanently unreachable in live
  play despite being correctly wired. Fixed by equipping one skill per unlocked tree per pass,
  cycling trees until the cap is hit. New `WildSpawnSystemTests.cs` (3 tests) covers the exact
  regression case plus the single-tree fallback.
- **Verified:** 223/223 EditMode tests pass (up from 220 — 3 new `WildSpawnSystemTests`). Live: C1
  (teal) and C2 (amber) both now show correct short lettering with no text overflow (previously
  C2's slot showed Mirror's un-renamed second skill, overflowing its circle); the move wheel opens
  automatically with no click at battle start; forcing the Evo gauge to 100%/ready showed the bar
  turn gold (confirmed via `resolvedStyle.backgroundColor` exactly matching `NameplateEvoReadyColor`)
  and the track's flash class toggling on a live schedule.

[2026-08-08] Phase 3 — Hover tooltips added to the 5 built-in moves (A/C/H/R/K)
- **Context:** Same-day follow-up: only the real skill-ring orbs (C1/C2) had a hover tooltip —
  user reported the 5 built-in moves (Attack, Charge, Heal, Regen, Capture) showed nothing.
- **Built:** New `BattleHUDController.MoveOptionTooltips` — a static array, index-matched to
  `MoveOptionsPerSlot`/`MoveOptionClockHours`, built once from the same named constants that
  actually drive each move in `BattleManager` (`BattleConfig.AttackAuraCost`, `DamageCalculator.
  BasicAttackPower`, `BattleConfig.ChargeAuraRestore`, `HealAmount`/`HealAuraCost`,
  `RegenHealPerTurn`/`RegenDurationTurns`/`RegenAuraCost`, and `CaptureSystem.
  ComputeCaptureChancePercent` called at 0%/100% target HP for Capture's real range) — same
  "use the real values" standard as the skill-ring tooltip fix, just for the 5 moves that aren't
  `SkillData`-backed. Registered `PointerEnterEvent`/`PointerLeaveEvent` on each move option
  element, reusing the same shared `_hudTooltip`/`PositionHudTooltipNear` infrastructure.
- **Caught a real discrepancy:** Capture's chance range is NOT 10-95% as a naive read of
  `CaptureSystem`'s clamp ceiling might suggest — with the actual `BaseCaptureChancePercent` (10)
  and `MaxLowHPBonusPercent` (60) constants, the true achievable range is 10-70%; the 95% clamp
  never triggers. Computing the range from the real method instead of hand-typing it caught this.
- **Verified:** 220/220 EditMode tests pass (no coverage change). Live: hovering "A" (Attack) in
  `BattleScene_Main` correctly showed "Attack / Physical damage — Power 10 / Target: Enemy / Aura
  Cost: 2" next to the orb (confirmed via Game View screenshot); all 5 tooltip strings queried
  directly and confirmed correct.

[2026-08-08] Phase 3 — Skill-orb tooltip content now derived from each skill's resolved mechanics
- **Context:** Same-day follow-up — user asked for the tooltip content itself to "use the values
  from each skill orb to generate your content," rather than the shared placeholder Description
  text every one of the 36 assets carries verbatim ("Placeholder skill — content pending species
  roster design... Do not treat as real skill content" — a dev-facing disclaimer, identical across
  every skill, not useful in-battle information).
- **Built:** New `BattleHUDController.BuildSkillTooltipText(SkillData)` — calls
  `PlaceholderSkillResolver.Resolve(skill)` (already-built, GDD-locked-data-derived resolution used
  by the actual turn-loop) and formats a per-skill-differentiated summary: damage skills show their
  resolved Physical/Elemental category and the shared placeholder Power; status skills show the
  resolved status name and its real duration range from `StatusEffectCatalog` (base range — the
  exact live value still depends on caster Resonance/target Resolve at cast time, unknowable at
  hover time since no target is chosen yet), plus Self/Enemy targeting either way. Nothing invented
  — every line traces to data the resolver already derives from locked tables.
- **Verified:** 220/220 EditMode tests pass (no coverage change — tooltip content is UI-only,
  matching existing precedent that MonoBehaviour/UIDocument controllers aren't EditMode-tested
  here). Queried `BuildSkillTooltipText` directly across 10 placeholder skills spanning 5 different
  trees — confirmed real differentiation: Corruption/Utility show damage category+power,
  Reaction/Bond/Personality show distinct applied statuses with correct duration ranges and
  Self-vs-Enemy targeting matching each status's `IsPositive` flag. Live: hovering C1 in
  `BattleScene_Main` shows "Elemental damage — Power 8 / Target: Enemy / Aura Cost: 3" positioned
  next to the orb (confirmed via Game View screenshot).

[2026-08-08] Phase 3 — Skill-orb tooltip repositioned to anchor next to the orb, not the cursor
- **Context:** Same-day follow-up: after the nameplate occlusion fix, user confirmed nameplate
  hover now works but skill-orb hover (over an equipped skill in the move wheel, opened by
  clicking the player creature) still appeared not to. User clarified expected behavior: the
  tooltip should appear "near/next to the skill we're hovering."
- **Investigated:** Exhaustively re-verified the skill-orb hover mechanism itself via
  `IPanel.Pick()`, manually-targeted event dispatch, AND a genuine untargeted `PointerMoveEvent`
  (the closest scriptable approximation of real mouse movement, letting the panel's own picking
  resolve the target rather than presetting one) — all three confirmed the tooltip correctly
  populates and shows. The likely real issue: the original design positioned the tooltip near the
  *cursor* (a fixed +18/+18 px diagonal offset), and skill-ring orbs sit low/left of the stage
  creature — a cursor-anchored tooltip there could easily land off-screen, behind another panel
  element, or just far enough from the orb to go unnoticed, reading as "doesn't work" even though
  it was technically firing.
- **Fixed:** `BattleHUDController.PositionHudTooltipNear(VisualElement anchor)` replaces the old
  cursor-following `PositionHudTooltip(Vector2)` — positions the tooltip immediately to the right
  of, and top-aligned with, whichever element is hovered (skill orb or nameplate bar), computed
  once on `PointerEnterEvent` rather than tracked continuously (the anchor doesn't move while
  hovered, so the `PointerMoveEvent` repositioning handlers on both hover sites were removed as
  unneeded — a simplification, not just a fix).
- **Verified:** 220/220 EditMode tests pass (no coverage change). Live: hovering C1 now shows the
  tooltip flush against the orb's right edge, top-aligned, fully on-screen (confirmed via Game
  View screenshot) — clearly read as attached to that specific orb.

[2026-08-08] Phase 3 — Fixed nameplate pointer occlusion; closes out the old Evo-gauge-not-clickable issue
- **Context:** Follow-up to the same-week nameplate work: user reported the HUD needed to move
  down (overworld debug text was bleeding through and covering it) and that hover wasn't working
  at all, on both the new nameplate bars and the skill orbs.
- **Fixed — HUD position:** `.status-header` pushed from `top: 0` to `top: 28px` in `BattleHUD.uss`
  — the overworld's `DebugMovementPresetCycler` text renders at screen (0,0) and bleeds through
  since `BattleScene_Main` loads additively on top of the overworld (both stay active).
- **Fixed — hover:** Root-caused via `IPanel.Pick(point)` (not the earlier session's `SendEvent`-
  based test, which gave a false positive — see `LESSONS_LEARNED.md`): `.stage` fills the entire
  `.battle-root` and, being the LATER sibling of `.status-header` in `BattleHUD.uxml`'s document
  order, silently won pointer picking across their overlap despite having no visible content there
  — every hover/click meant for the nameplate was being swallowed. Fixed by moving `<StatusHeader>`
  to after `<Stage>` in the UXML (position-absolute, so this only changes pick/paint z-order, not
  layout). Also set `pickingMode = Ignore` on the decorative bar-fill element so its parent track
  is the unambiguous hover target. Skill-ring orb hit-testing was separately confirmed already
  sound via the same `Pick()` technique — not reproduced as broken once wheel-open timing was
  accounted for.
- **Bonus:** This is almost certainly the real cause of the long-open `KNOWN_ISSUES.md`
  `[EDITOR-001]` "Evolution Burst gauge not clickable" report (same click path, same occluded
  region) — closed retroactively.
- **Verified:** 220/220 EditMode tests pass (no coverage change — UXML/USS ordering fix). Live:
  `Pick()` at the nameplate's on-screen coordinates changed from resolving to `.stage` (wrong) to
  the nameplate's own bar track (correct); dispatching a hover at the now-correctly-picked element
  showed the tooltip with the right text, and leaving hid it correctly.
- **Next:** None — this closes both the hover report and the old Evo-gauge issue. If a user still
  sees the gauge fail to respond specifically when it's actually full/ready, that would point to
  `EvolutionBurstSystem.ActivateReady` itself, not this occlusion bug.

[2026-08-07] Phase 3 — Nameplate bars mockup (HP/Aura/Evo), scoped HUD scale-up reverted
- **Context:** Follow-up correction to the same-day HUD scale-up: the global `PanelSettings.scale`
  bump enlarged everything, but the user only wanted the health/Aura/Evo readout bigger — the
  player stage circles and skill wheel should have stayed at their original size. Separately,
  requested a new mockup for that readout itself: 3 horizontal rectangles stacked vertically (HP,
  Aura, Evo) instead of the circular radial gauge, with current/total shown on hover — and asked
  to keep the circular version intact in case of a revert.
- **Built:** Reverted `BattleHUDPanelSettings.scale` back to 1. Added `BattleHUDController.
  NameplateStyle` enum (`Radial`/`Bars`) gated by a single `ActiveNameplateStyle` const — currently
  `Bars`. `BuildNameplate` (now an instance method, was static) branches construction: Radial
  builds the exact same ring/portrait/gauge/stat-label cluster as before, byte-for-byte unchanged;
  Bars builds 3 stacked `.nameplate-bar-track`/`.nameplate-bar-fill` rows instead, no numbers shown
  by default. `RefreshNameplateStats` and a new shared `ApplyEvoVisual` helper (used by both the
  refresh loop and `SetBurstFillBar`'s independent call site) branch the same way. Generalized the
  same-day skill-orb tooltip infrastructure (`_skillTooltip` -> `_hudTooltip`, `.skill-tooltip` ->
  `.hud-tooltip`) so the nameplate bars' hover-to-reveal ("HP: 120/120", "Aura: 24/24", "Evo: 45%"
  or "Evo: ready") reuses the exact same shown-on-PointerEnter/repositioned-on-PointerMove/
  hidden-on-PointerLeave Label rather than a second parallel implementation. New USS: `.nameplate-
  name-bars`, `.nameplate-bars-wrap`, `.nameplate-bar-row/track/fill`, `.nameplate-bar-hp/aura/evo`,
  `.nameplate-bar-evo-ready`. Nothing about the Radial style's code was deleted — flipping
  `ActiveNameplateStyle` back to `Radial` is the entire revert.
- **Verified:** 220/220 EditMode tests pass (unchanged — no test coverage exists for either
  nameplate visual, matching existing precedent that MonoBehaviour/UIDocument controllers aren't
  EditMode-tested here). Live Play Mode: player stage circles and skill wheel confirmed back to
  their pre-scale-up size; nameplate now shows 3 stacked bars (green/blue/dark, correctly full/
  full/empty for a fresh battle); hovering the HP bar's track correctly showed "HP: 120/120" via
  the shared tooltip.
- **Next:** This is an explicit first-pass mockup, not a locked design — the Bars style currently
  has no party-count-based size interpolation (unlike Radial's Comfortable/Compact lerp) and no
  species-color accent; revisit either if the user wants after seeing it live.

[2026-08-07] Phase 3 — Runtime skill tooltip fix, battle HUD scale-up, Aura +1 allocation fix
- **Context:** User playtesting turned up three separate issues in the battle scene: (1) hovering a
  skill orb showed no tooltip despite the same-day "richer hover tooltip" work; (2) the HUD text
  read as too small; (3) opening the Tab-key Aura-spend menu and clicking "+1" silently did nothing
  even with Aura available.
- **Investigated:** (1) traced to `VisualElement.tooltip` — Unity's native UI Toolkit tooltip only
  renders inside Editor-hosted UI (Inspector/EditorWindow panels), never in a runtime `UIDocument`
  panel; the earlier session's implementation compiled and looked correct but was a no-op by
  design in both Play Mode and a real build. (3) traced to `AuraStatAllocationSystem.
  TryAllocateStatPoint` gating on `phasix.baseStats.Total >= AuraTierCeiling.ComputeCeiling(...)` —
  real species `baseStats` (e.g. `Test_FireType`'s Vitality=120) already total well past the
  tier-1 placeholder ceiling (40) before any Aura is ever spent, so the gate was permanently
  closed from the very first allocation attempt for any real species; only ever tested in
  isolation against synthetic zero-baseline `StatBlock`s, which never exposed the mismatch.
- **Built:** `BattleHUDController` now maintains its own floating `_skillTooltip` Label, shown/
  hidden/repositioned off `PointerEnterEvent`/`PointerMoveEvent`/`PointerLeaveEvent` on each
  skill-ring slot instead of the dead `.tooltip` assignment; new `.skill-tooltip` USS class.
  `BattleHUDPanelSettings.scale` raised from 1 to 1.35 (ConstantPixelSize mode) — a single
  panel-wide multiplier rather than touching the many individually-tuned pixel constants across
  `BattleHUD.uss`/`BattleHUDController.cs`. New `PhasixRuntimeData.auraAllocatedPoints` — a
  running total of points actually purchased via Aura, never touched by baseStats; `
  AuraStatAllocationSystem.TryAllocateStatPoint`/`GetRemainingCeilingRoom` now gate on this field
  instead of `baseStats.Total`, matching Progression_Directive_v0_1_0's literal wording ("Stat
  growth through Common Aura is capped per tier" — growth, not total stat value). Neither
  placeholder ceiling constant (`BaseCeilingPerTier`/`CeilingIncreasePerAptitudePoint`) changed.
- **Verified:** 220/220 EditMode tests pass (up from 218 — two new regression tests covering a
  species whose baseStats already exceed the ceiling). Live Play Mode: hovering an equipped skill
  orb shows its name/description/Aura cost; HUD text/orbs visibly larger and legible in a Game
  View screenshot; `AuraStatAllocationSystem.TryAllocateStatPoint` against the real party member's
  live `PhasixRuntimeData` (baseStats.Total=177, ceiling=40) now succeeds and correctly increments
  `auraAllocatedPoints`/decrements `commonAura`/adds to the target stat, where it silently failed
  before the fix.
- **Next:** No further action needed on these three; flagged for whoever eventually builds
  devolution to decide whether `auraAllocatedPoints` should reset alongside `baseStats` then.

[2026-08-07] Phase 3 — C1/C2 skill naming, RepeatSameSkill/TimedInputStreak mechanic refinements, richer skill tooltips
- **Context:** Live-testing feedback on the same-day combo-counter/battle-summary work: (1)
  console spam (`"Access version should be odd when acquiring lock"`) and the Evo bar not being
  clickable; (2) the two combo-granting skills should be named C1/C2, with `RepeatSameSkill`
  restricted to C1 specifically (not "any repeated skill") and `TimedInputStreak` (C2) requiring
  PERFECT hits specifically (not merely successful ones); (3) the combo badge needed to sit
  further from the skill orb; (4) skill orbs should show a richer hover tooltip. Full record:
  DECISIONS.md -> [Combat/UI].
- **Investigated:** The console error traced to a Unity Editor-process-level issue (no file/
  line/stack trace, persisted through a clean stop+recompile with no Play Mode involved) — most
  likely this session's very large number of forced recompiles/domain reloads degraded the
  Editor process. Recommended a manual Editor restart (confirmed safe — nothing dirty, all work
  saved). Did not find any code-level regression in the burst-bar click path itself.
- **Built:** Renamed `Mirror_Placeholder1`/`Reaction_Placeholder1` to `C1`/`C2`.
  `ComboRuleEvaluator.EvaluateRepeatSameSkill`/`GetRepeatTrailingStreakLength` now take an
  explicit `grantingSkill` parameter and only count repeats of that specific skill.
  `BattleParticipant.RecentTimedInputSuccesses`/`RecordTimedInputResult` renamed to
  `RecentTimedInputPerfects`/`RecordTimedInputPerfect`, fed from `LastTimedInputWasPerfect`
  instead of `LastTimedInputSuccess` — a non-perfect success now resets the streak same as a
  miss. `BattleHUDController.PopulateSkillRing` tooltip now shows name + description + Aura
  cost. `.skill-combo-badge` offset pushed from -4px to -14px for clearer separation from the orb.
- **Verified:** Live Play Mode — C1/C2 names correct; repeating the OTHER equipped skill produced
  zero streak and no combo log line; repeating C1 correctly logged "Duo combo" and badged C1's
  slot; tooltip shows the richer text; badge visually confirmed at the new radius. 218/218
  EditMode tests pass (up from 214).
- **Next:** User to restart the Unity Editor and re-test the Evo bar click fresh — if the console
  error recurs after a real restart, that rules out the "degraded session" theory.

[2026-08-07] Phase 3 — Skill-wheel combo counter; post-battle screen reworked to a read-only summary; Aura spending moved to a new Tab-key menu
- **Context:** Follow-up to the same-day Combo/Status/Chain/Mastery wiring session, driven by two
  user asks: (1) combos had zero UI feedback beyond a battle-log line — after exploring "add it
  to the buff/debuff bar" and "a standalone nameplate indicator," landed on a counter badge
  directly on the skill wheel; (2) the post-battle Aura Allocation screen was "too small" and
  spending Aura right after a battle wasn't wanted — it should be a read-only recap (Aura gained,
  damage dealt, healed), with spending moved to "some menu," specifically the Tab key for now.
  Full record: DECISIONS.md -> [Combat/UI].
- **Built:** `ComboEngine.GetDistinctTrailingStreakLength`/`ComboRuleEvaluator.
  GetRepeatTrailingStreakLength`/`GetTimedInputTrailingStreakLength` (raw current streak length,
  not the capped Duo/Trio/Quad tier). New `.skill-combo-badge` on each real skill-ring slot
  (`BattleHUDController.SetSkillComboCounter`/`ClearAllSkillComboCounters`), badging the just-used
  skill for `CrossTreeSequence`/`RepeatSameSkill` or the granting passive for `TimedInputStreak`
  (`BattleManager.RefreshComboCounterBadges`, called after every skill use). Deleted
  `AuraAllocationController`/`AuraAllocation.uxml/.uss`; new `BattleSummaryController`
  (`BattleSummary.uxml/.uss`) — small read-only panel, Continue only, no spend buttons. New
  `BattleSummary` data class + `BattleManager` running totals (`_totalDamageDealt`/
  `_totalHealingDone`, accumulated at every player-side damage/heal call site) and a first real
  implementation of Aura-drop-on-win (`BattleConfig.AuraRewardOnWin`, flat 15 — `EventBus.
  OnAuraDropped` had existed as an unwired stub with no producer until now). New
  `PartyMenuController` (`PartyMenu.uxml/.uss`) in the overworld scene, toggled by Tab — reuses
  the per-creature "+1" Aura-spend cards ported from the deleted `AuraAllocationController`,
  adapted to `PhasixRuntimeData` directly since there's no `BattleParticipant` outside battle.
- **Verified:** Unity MCP reconnected same session. Completed the Editor-side wiring (renamed
  `UIRoot_AuraAllocation` -> `UIRoot_BattleSummary` with `BattleSummaryController` attached and
  re-pointed at `BattleSummary.uxml`; created `UIRoot_PartyMenu` in `SampleScene` with a new
  `PartyMenuPanelSettings.asset`), then live Play Mode: the icon badge showed "2" on exactly the
  repeated skill's ring slot after using it twice; winning a battle showed the summary screen
  with correct totals (Aura Gained 15, Damage Dealt 6, Healing Done 0); the Tab menu opened with
  the real party member's stats and Aura. 214/214 EditMode tests pass (up from 206). Caught and
  fixed one bad test assertion along the way (`GetDistinctTrailingStreakLength` was correct at 3,
  the test's expected value of 2 was wrong).
- **Follow-up:** User found the first-pass text too small to read. Standardized `BattleSummary.
  uss`/`PartyMenu.uss` on `.battle-log-entry`'s 13px as the body-text baseline (titles ~16px,
  matching `.battle-log-title`'s 15px), growing both panels/cards to fit. Redesigned the combo
  badge from a bare floating number into a proper small solid-circle icon, matching
  `.nameplate-buff-icon`'s visual language (sized up for the bigger 32px skill slot). Re-verified
  live after each change.

[2026-08-07] Phase 3 — Combo/Status/Chain/Mastery + Aura Allocation wired into live play; PhasixGuide.md doc fix
- **Context:** Routine "what's next" check surfaced two things: `PhasixGuide.md` was badly stale
  (claimed `Combat/`/`Evolution/` were "Phase 3, not yet created" — false, Combat/ was fully
  built). Separately, `ComboEngine`/`StatusEffectCatalog`/`ChainResultCatalog`/
  `MasteryBonusCatalog` (rules-layer, tested) and `AuraStatAllocationSystem` (progression) had no
  live call site — `DECISIONS.md` had twice deferred building the skill-selection UI they need.
  User explicitly chose to override that "wait" decision and build the wiring now, accepting
  rework once real skill content lands. Full record: DECISIONS.md -> [Combat].
- **Built:** `PlaceholderSkillResolver` — derives damage-category/status behavior for the 36
  placeholder `SkillData` assets algorithmically from already-locked tables (`SkillTreeCatalog`,
  `StatusEffectCatalog`, the damage formula's own Force/Guard/Resonance/Ward split), so nothing
  per-skill is hand-invented. New `SkillData.PlaceholderIndex`/`GrantsComboRule` structural
  fields. New `SkillDatabase` (GUID↔SkillData/tree lookups). `BattleHUDController`'s previously-
  decorative 12-slot skill ring is now half-live — 7 of 12 slots (hours 4-10) are real, draggable
  equipped-skill slots (`PopulateSkillRing`), the other 5 stay under the built-in A/C/H/R/K moves.
  New `BattleManager.ResolveSkillAction` resolves a skill drag into damage or a status
  application, then checks Combo/Chain/Mastery and logs any hit (detection + log only — no
  numeric bonus for any of them this pass, an explicit scope decision). New active-status
  tracking on `BattleParticipant` (`ApplyStatus`/`TickStatuses`, ticked once per round). New
  pluggable combo-rule framework — `ComboRuleType`/`ComboRuleEvaluator`
  (`RepeatSameSkill`/`TimedInputStreak`) — NEW, user-directed mechanics (not GDD content) letting
  specific skills grant a creature an alternate combo rule while equipped, alongside the
  unchanged GDD-locked base cross-tree rule; pre-wired on `Mirror_Placeholder1`/
  `Reaction_Placeholder1` since those trees' own locked role text fits. New post-battle
  `AuraAllocationController` (UI Toolkit screen, spends `commonAura` via the already-built
  `AuraStatAllocationSystem`), shown from a newly-coroutine `BattleManager.EndBattle` on a Won
  outcome before the scene unloads. `WildSpawnSystem.SeedInitialSkills` (shared by
  `EncounterTrigger`/`DebugPartyBootstrap`) auto-seeds a species' unlocked trees/skills up to
  `SkillSlotCapacity`'s locked tier caps — explicit placeholder for a real skill-learning flow.
- **Fixed (found live):** `Test_FireType`/`Test_SteamType` both had `EvolutionTier` defaulted to
  0 (never set since Wk 9) — crashed `SkillSlotCapacity.GetTreeCount` the first time anything
  actually read tier for capacity math. Fixed the test data (tier 1) and added a defensive
  tier-range guard in `SeedInitialSkills` so a similar gap in future placeholder species can't
  crash a spawn.
- **Changed:** `PhasixGuide.md` (v1.4.0 -> v1.5.0) — corrected the false "Combat/Evolution not
  yet created" claim, added the new `Scenes/`/`SkillDatabase`/`AuraAllocation` entries, updated
  the test roster and "What Is Pending" section to reflect detection-only Combo/Chain/Mastery.
- **Verified:** Live Play Mode via `BattleTransition.StartWildBattle` — skill ring showed exactly
  2 live slots (both Mirror) + 5 locked for the seeded test companion; a Mirror skill resolved
  real damage with correct type-effectiveness text; using it twice logged a "Duo combo —
  repeating the same skill" line; applying Bleed+Weaken to the enemy logged "combine into Rend!"
  with the catalog's exact locked text (confirmed non-repeating on a second identical check);
  adding a 3rd DoT logged "achieves Hemorrhage!" (confirmed once-per-battle); ending the battle
  Won showed the Aura Allocation screen with correct party/stat/Aura data, and Continue correctly
  returned to the overworld with the battle scene unloaded cleanly. 206/206 EditMode tests pass
  (up from 133 at session start — 73 new tests across `PlaceholderSkillResolverTests`,
  `ComboRuleEvaluatorTests`, `SkillDatabaseTests`, and additions to `BattleParticipantTests`/
  `BattleLogFormatterTests`).
- **Next:** Chain/Mastery's full numeric effects (a `DamageCalculator` modifier) are an explicitly
  flagged, separately-scoped follow-up. Combo bonus effects remain entirely undesigned (GDD never
  specifies one). Real skill content/species roster design (Phase 5) will need to replace
  `PlaceholderSkillResolver`'s derivation and the 36 placeholder assets' structural fields, not
  extend them.

[2026-08-06] Phase 8 — Free-choice creature selection, staggered stage layout, End Turn button
- Built: `BattleParticipant.HasActedThisTurn`; `BattleHUDController.PlayerCreatureClicked`/
  `EndTurnClicked` events, `ShowMoveSelectionReadOnly`, `SetEndTurnButtonVisible`,
  `ApplyStageCreatureStagger`; new `EndTurnButton` (UXML/USS) and `.move-option-disabled` style;
  `BattleManager.PlayerTurn` rewritten as a free-choice `while` loop driven by compound
  `WaitUntil`s (end-turn requested / move confirmed / different creature clicked).
- Decided: player creatures are staggered vertically (translate-only, layout untouched) and can be
  selected in any order, any number of times — already-acted creatures stay clickable but show a
  greyed (`opacity: 0.35`) move wheel via a plain per-participant `HasActedThisTurn` flag, left
  deliberately un-baked into any one move so a future synergy/passive system can grant a second
  action later without restructuring. Turn ends only via the new dedicated End Turn button, not
  automatically once everyone has acted.
- Verified: live Play Mode with a synthesized 2nd party member — staggered non-overlapping layout,
  click-to-open wheel, switching creatures mid-selection (both directions), already-acted greyed
  state (and that greyed orbs refuse `BeginDrag`), End Turn button visible/positioned correctly.
  `Button.clicked` doesn't fire from synthetic `PointerDown`/`PointerUp` dispatch (UI Toolkit test-
  simulation limitation, not a real bug — see DECISIONS.md) — confirmed the real code path instead
  by invoking the `EndTurnClicked` delegate directly and screenshotting the resulting enemy-turn ->
  new-player-turn transition. 133/133 EditMode tests pass.
- Next: multi-creature-per-turn playtest (both party members acting, several turn cycles) and a
  stage-layering fix for skill orbs drawing behind a further-back lane's creature — see follow-up
  entry below once verified.

[2026-08-06] Phase 8b — Stage-creature layering fix + multi-creature turn cycle verification
- Built: `BattleHUDController.RestoreStageCreatureDepthOrder` (sorts player stage creatures
  back-to-front by their stagger offset and reorders siblings via `BringToFront`, since UI Toolkit
  draws children in document order and the earlier stagger-via-`translate` didn't reorder them);
  `ShowMoveSelection`/`ShowMoveSelectionReadOnly` now `BringToFront()` the selected creature so its
  wheel always renders above every other lane; `HideMoveSelection` restores depth order once the
  wheel closes.
- Decided: user caught the front lane's skill orbs rendering behind a further-back lane's creature
  — fixed the static ordering first, then corrected mid-verification to also force the actively
  selected creature's wheel to the very front regardless of lane depth ("the selected character's
  skill wheel should always be in the front over everything so you can see it clearly").
- Verified: live Play Mode, real 2-member party — selecting the back-lane creature now correctly
  draws its wheel over the front-lane creature; selecting the front-lane creature still reads
  correctly at rest. Ran two full turn cycles with both party members acting independently each
  turn (direct event invocation, since synthetic pointer/Button dispatch has the known UI Toolkit
  test-simulation limitation) — `HasActedThisTurn` correctly toggled and reset each cycle, greyed
  read-only wheel confirmed via CSS class check on re-click, enemy took damage from both attackers
  each cycle, both party members took enemy damage between cycles. 133/133 EditMode tests pass.

[2026-08-06] Phase 8c — Stage creatures fixed-position (no more jump-on-select); background click closes wheel
- Built: `.stage-creature` switched from a flex-laid-out child to `position: absolute` with a new
  `BattleHUDController.LayoutPlayerStageCreatures` computing each visible slot's explicit `left`
  (called once from `Initialize`); new `StageBackgroundClicked` event (fires only on a genuine
  empty-background click, via `evt.target == _stage`) wired into `BattleManager.PlayerTurn` as an
  extra way to close an open wheel.
- Decided: the prior phase's `BringToFront()` layering fix had an unintended side effect — since
  `.stage-creature` was still flex-flowed, reordering for paint order also reordered LAYOUT
  position, so clicking a creature visibly shoved it to a different spot in the row. Fixed at the
  root by taking creatures out of flex flow entirely (explicit `left` per slot, matching the old
  spacing exactly) so `BringToFront()` only ever affects paint order now. Also added the
  background-click-to-dismiss behavior the user asked for, with an explicit fix for a stale-flag
  edge case (a background click while nothing was selected must not silently close the next wheel
  opened afterward). Formation/repositioning ("place Phasix in various positions for strategies
  later on") is explicitly deferred — current layout just anchors the pair where they already
  were, but the new method is the intended hook for that later.
- Verified: live Play Mode — `worldBound` recorded for both creatures before/after selecting each
  one, confirmed byte-identical (no jump); background click closes an open wheel; a background
  click with nothing selected doesn't leak into instantly closing the next wheel opened. 133/133
  EditMode tests pass.

[2026-08-06] Phase 8d — Decoupled status header from stage position; compact rows; halved bottom panels
- Built: `.status-header` switched to `position: absolute` (out of `.battle-root`'s flex flow, so
  `.stage` always fills the full battle-root height); `.stage-side`'s anchor changed from
  `top: 30%` to a fixed `top: 480px`; `.status-row` and children resized smaller across the board
  (name/HP/Aura/Burst/status-icon fonts and bar heights all reduced); `.action-panel`/
  `.battle-log-panel` height halved 300px→150px with matching font/padding reductions;
  `.end-turn-button`'s `bottom` recalculated to match.
- Decided: user caught that adding a 3rd party member moved the ENEMY too — root cause was
  `.status-header` growing with each row and shrinking `.stage` (same flex-column parent), which
  changed what `.stage-side`'s `top: 30%` resolved to in absolute pixels. Fixed by taking the
  header out of flex flow entirely rather than hardcoding a height that would need to stay in sync
  with `BattleConfig.ActivePartySize`. Also compacted row sizing and halved the Battle Log/action
  panel height per the user's direct "about 50% of its current height so theres more room" ask —
  both aimed at the same underlying complaint (things feel squished, no room for the eventual
  larger party).
- Verified: live Play Mode — enemy `worldBound` identical between a 1-member-party battle and a
  fresh 3-member-party battle (`x:1553,y:480` both times). Screenshot confirmed 3 status rows fit
  cleanly with no overlap into the creature stage, and the shorter bottom row/End Turn button don't
  overlap anything. 133/133 EditMode tests pass (pure USS, no C# logic touched).

[2026-08-06] Phase 9 — Radial nameplate HUD (7 slots/side), replacing stacked HP/Aura/Burst bars
- Built: new `RadialGaugeVisual` (custom Painter2D element) drawing HP/Aura/Evo as 3 gapped arcs
  around a portrait, with a closed gold outline fully encasing the Evo band when ready; new
  `BattleHUDController.NameplateRefs`/`BuildNameplate` building each nameplate procedurally in C#
  (name on top, ring, 3 stat labels, a `flex-wrap` buff row) inside an invisible fixed-width
  container; `MaxNameplateSlots = 7` per side, independent of `BattleConfig.ActivePartySize`
  (still 3 — real gameplay party size is unrelated to nameplate display capacity). Removed the old
  `.hp-bar-back`/`.aura-bar-back`/`.burst-bar-back`/`.status-bar`/`.status-icon` USS entirely.
- Decided: worked from an explicit design question ("is there another style for hud... cleaner and
  more straightforward?") through several rounds of Artifact-tool mockup iteration BEFORE writing
  any game code — buffs moved to their own full-width wrapping row ("we'll be putting a lot"), the
  ring shape (HP top half, Aura bottom-left quarter, Evo bottom-right quarter around a portrait)
  and its gaps/ready-outline fixes came from direct user corrections on the mockup, and the
  "invisible container, stack them side by side" architecture was confirmed with the user before
  any implementation started, per their explicit "confirm before implementing" ask. `SetRegenStatus`/
  `SetBurstStatus`/`SetBurstFillBar`/`Initialize`/`RefreshBars` kept identical public signatures,
  so `BattleManager.cs` needed no changes despite the full HUD-side rewrite.
- Verified: live Play Mode, `Initialize` called directly with 7 synthesized participants per side
  (bypassing PartySystem's real 3-cap for this HUD-capacity-only check) — varied HP/Aura/Evo fill,
  two forced to a full "ready" Evo gauge, several with active Regen/Burst buff icons (one slot
  with both at once). First pass (64px rings) overflowed the 7th slot into the Battle Log panel;
  re-sized (46px rings, tighter fonts/padding) and re-verified — all 14 nameplates now render
  fully within the header, clear of the stage and bottom row, gaps holding at every fill level.
  133/133 EditMode tests pass.

[2026-08-06] Phase 9b — Nameplates size dynamically (bigger at today's real party size, not fixed for 7)
- Built: new `BattleHUDController.ApplyNameplateSize`, called from `Initialize` per side using
  that side's own visible count — linearly interpolates ring/portrait/name-font/stat-font/buff-
  icon-size/padding/margin between a "Comfortable" size (3 or fewer visible) and the already-
  verified "Compact" size (7 visible), applied as inline styles over the `.nameplate-*` USS
  defaults.
- Decided: user asked directly whether the 7-slot-verified sizing was too small, given real
  gameplay only ever shows 3 today — confirmed with a side-by-side mockup before implementing.
  Capped "Comfortable" at 72px rather than the 90px shown in the mockup, since 90px would grow
  the header taller than `.stage-side`'s fixed `top: 480px` clearance was calibrated for at a
  3-member party — 72px stays inside the already-verified-safe budget without re-touching stage
  positioning this pass.
- Verified: live Play Mode, real 3-member party — screenshot confirmed visibly larger rings/name/
  stat text than the previous fixed-46px version, no overlap with the stage creatures. 133/133
  EditMode tests pass. Not re-verified at 7 — `t=1` reproduces the exact numbers already verified
  in the prior entry, so no behavior change there.

[2026-08-06] Phase 3 — Evolution Burst gauge made visible + player-activated
- **Context:** User asked how to access Evolution Burst and found there was no visible gauge —
  it filled and triggered entirely invisibly. Asked for a visible purple fill bar under the Aura
  bar, changed from auto-trigger to a player click on the bar itself once full (yellow border =
  ready), confirming separately that activation should only work at full gauge. Full record:
  DECISIONS.md -> [Combat].
- **Built:** New `.burst-bar-back`/`.burst-bar-fill` purple gauge bar under each player's Aura bar
  (status-icon row shifted down to make room); `.burst-bar-ready` yellow-border modifier once
  full. New `BattleHUDController.BurstBarClicked` event + `SetBurstFillBar`. New
  `EvolutionBurstSystem.ActivateReady` (public `TriggerThreshold`) — unlike the old `TryTrigger`,
  has NO bond-based reliability chance, since a click on a UI-marked-ready bar must always work.
  `BattleManager.AddBurstFillAndCheckTrigger` renamed to `AddBurstFill` (fill + UI update only,
  no longer auto-triggers); new `HandleBurstBarClicked` calls `ActivateReady`, safely a no-op on
  an early click, a dead party member, or an already-active gauge.
- **Verified:** Live Play Mode — filled to 85, a live Charge cast crossed the threshold, screenshot
  confirmed full purple bar with yellow border; clicking it produced the "ignites!" log line, the
  status icon, and a bar reset. A 30%-fill click confirmed as a complete no-op. 133/133 EditMode
  tests (4 new, covering ActivateReady's full/not-full/already-active/bond-independence).

[2026-08-06] Phase 3 — CaptureSystem + EvolutionBurstSystem wired into the live battle loop
- **Context:** Asked whether anything was left in the Phase 3 plan. Found that the plan's final
  Phase 3 Gate playtest couldn't actually run — 5 Step 4/5 systems (Capture, Combo, Status
  Effects, Evolution Burst, Aura Stat Allocation) existed as tested classes but none were reachable
  from live play. User chose to wire only Capture + Evolution Burst, leaving the other 3 alone per
  this project's own DECISIONS.md notes (Combo/Status/Mastery/Chain explicitly deferred to a real
  skill-selection UI; Aura Stat Allocation is a post-battle progression system, not a mid-battle
  one). Full record: DECISIONS.md -> [Combat].
- **Built:** New 5th move option "K" (Capture, gold, 3 o'clock) — targets the enemy, attempts
  `CaptureSystem.AttemptCapture`; success adds the creature to the party
  (`PartySystem.AddToParty`) and ends the battle immediately (`EndBattle(Won)` + new
  `_battleEndedEarly` flag so `RunBattleLoop` doesn't also run `EnemyTurn` on an unloading scene);
  failure logs it and consumes the turn like Charge/Heal/Regen. New
  `BattleParticipant.BurstGauge` + `BattleManager.AddBurstFillAndCheckTrigger`/`TickPlayerBurst`
  wire `EvolutionBurstSystem` at all 3 GDD-locked fill sources (skill use, timed-input success,
  taking an undefended hit) — status-only (new orange "B" status icon, Regen's exact countdown
  pattern), deliberately no stat/damage effect since "ApplyBurstEffects" is genuinely undesigned
  in the GDD. New `BattleConfig.BurstFillPerSkillUse/PerTimedInputSuccess/PerHitTaken` constants.
- **Verified:** Live Play Mode — Capture failure path confirmed (10% floor chance at full HP);
  isolated `AttemptCapture` retry loop confirmed the success path correctly grows the party.
  Evolution Burst: filled to 100, confirmed a real 60%-chance trigger miss at 0% bond then a
  success (2-turn duration correct), status icon + countdown rendered correctly, `TickPlayerBurst`
  counted down and expired correctly with a log line. A live Charge cast confirmed the actual
  `BattleManager` call sites fire (fill grew by skill-use + the same round's auto-played hit-taken
  fill, both firing correctly together). 129/129 EditMode tests unaffected.

[2026-08-06] Phase 3 — Continue button removed (fully auto-paced turns); "R" log text -> "Aura Regen"
- **Context:** After playtesting several turns, user judged the Continue-button click between
  PlayerTurn and EnemyTurn unnecessary — a delay is enough to read that turns switched. Also
  asked for the Regen cast's battle log line to spell out "Aura Regen" instead of the bare "R".
  Full record: DECISIONS.md -> [Combat] (2 entries).
- **Built:** Deleted `BattleHUDController.WaitForContinue` and its backing
  `ContinueButton`/`_continueButton`/`_continuePressed` — the turn-transition call site in
  `BattleManager.PlayerTurn` now uses `ShowTimedMessage("Enemy's turn...", ...)`, same as every
  other beat. Deleted the now-dead `.prompt-button` CSS class and the `ContinueButton` UXML
  element. `AppendBattleLog`'s Regen line changed to `"{name} uses Aura Regen!"` (the brief
  on-stage announcement keeps the short "R" form on purpose).
- **Verified:** Live Play Mode — confirmed `ContinueButton` is genuinely gone from the UI tree
  (not just hidden); cast Regen and let the battle run with zero further clicks — the turn
  transitioned into EnemyTurn and the enemy's attack resolved entirely on its own. Battle log
  confirmed reading "uses Aura Regen!". 129/129 EditMode tests unaffected.

[2026-08-06] Phase 3 — Status-bar polish: legible countdown + fixed reserved height
- **Context:** User feedback on the just-added status icon: the countdown subscript overlapped
  the icon and was hard to read, and the status-row box visibly resized whenever a status
  activated/expired. Full record: DECISIONS.md -> [Combat].
- **Built:** Countdown subscript offset increased (`bottom:-5px` -> `-13px`) for real visual
  separation from the icon. `.status-bar` given a fixed `height: 26px` so the row never
  collapses/expands based on whether its icon is visible. Added an empty `EnemySlot0_StatusBar`
  with the same class so player and enemy status-row boxes reserve identical height.
- **Verified:** Live Play Mode — measured PlayerSlot0's row height before/after casting Regen
  (161px both times, unchanged); EnemySlot0 matched at 161px. Screenshot confirmed clear
  separation between the "R" icon and its "3" counter. 129/129 EditMode tests unaffected.

[2026-08-06] Phase 3 — "H" gets a real effect, new "R" (Regen) orb, status-icon countdown system
- **Context:** User specced "H"'s long-pending effect (6 Aura -> 4 HP instant heal) and asked for
  a 4th move, "R" (Regen, purple, 2 o'clock): 8 Aura for a 2 HP/turn heal-over-time lasting 4
  turns, plus a small status-bar icon under the HP/Aura box that shows a countdown subscript
  (confirmed as counting DOWN — "4 then 3 then 2 etc" — not up) outside its own frame at
  bottom-right, with bottom-left reserved for future start-of-turn effects. Full record:
  DECISIONS.md -> [Combat].
- **Built:** `BattleParticipant.Heal`/`ApplyRegen`/`TickRegen` (new). `BattleConfig`:
  `HealAuraCost`/`HealAmount`/`RegenAuraCost`/`RegenHealPerTurn`/`RegenDurationTurns`. New
  `BattleManager.TickPlayerRegen()` — ticks every alive party member's active Regen once per
  PlayerTurn, right before the Continue gate, so a status cast this turn gets its first tick
  immediately. Refactored `BattleHUDController`'s per-move callback params (`onAttackConfirmed`/
  `onChargeConfirmed`/`onHealConfirmed`) into a single `onMoveConfirmed(optionIndex, target)` plus
  a `MoveOptionIsSelfOnly` array, since a 4th self-only move made one-callback-per-move stop
  scaling. New `.status-bar`/`.status-icon`/`.status-icon-purple`/`.status-icon-counter-br`
  (+ unused `-bl` reserved for later) in `BattleHUD.uss`; `BattleHUDController.SetRegenStatus`
  shows/updates/hides the icon and its counter text.
- **Verified:** Live Play Mode — cast "R", confirmed the immediate first tick (battle log: "uses
  R!" then "regenerates 2 HP!"), HP/Aura math correct, status icon appeared with countdown "3"
  positioned outside its frame at bottom-right. 129/129 EditMode tests (9 new, covering
  Heal/ApplyRegen/TickRegen including max-HP clamping and post-expiry inertness).

[2026-08-06] Phase 3 — "H" orb added, "C" reworked to self-target drag, unified MoveKind targeting
- **Context:** User asked for a new solo/self-only skill orb ("H", pastel pink), and for "C"
  (Charge) to use the same "click it, then choose who to select" gesture as the new orb — with
  the constraint that the only valid selection for a solo skill is the caster itself. Full record:
  DECISIONS.md -> [Combat].
- **Built:** New `.move-option-pink` "H" orb at 12 o'clock. New private `MoveKind {Attack,
  Charge, Heal}` unifies `BeginDrag`/`OnDragPointerUp` across all three moves — Attack's valid
  drop target is the enemy, Charge/Heal's valid drop target is ONLY the caster's own creature.
  Dragging Charge or Heal onto the enemy is now rejected and cancels back to the move options,
  same as Attack dragged onto empty space. `ShowMoveSelection` gained a `BattleParticipant self`
  parameter; `onChargeSelected` split into `onChargeConfirmed`/`onHealConfirmed`
  (`Action<BattleParticipant>`). Removed the now-obsolete immediate-click `SelectCharge`. "H"'s
  actual gameplay effect is still undecided — placeholder logs a message and ends the turn, no
  stat change (content intentionally not invented, per CLAUDE.md).
- **Verified:** Live Play Mode — dragged "H" onto the caster, confirmed correctly (battle log:
  "uses H!"); dragged "C" onto the enemy, correctly rejected with no state change and the move
  options reappearing for a retry. 120/120 EditMode tests unaffected.

[2026-08-06] Phase 3 — Label centering fix, "S" -> "C" Charge mechanic, 12-slot skill ring, companion pathfinding paused during battle
- **Context:** Follow-up polish session after live playtesting. Four separate user asks in
  sequence: (1) the move-orb letters still weren't centered after the prior pass, (2) turn the
  blue "S" orb into a "C" Charge mechanic — no attack, restores Aura instead, (3) noticed the
  companion's A* pathfinding still computing during battle, asked whether that's needed, (4) add
  12 dark grey placeholder skill slots around the player, refined across 2 more rounds of
  feedback into "A"/"C" reading as 2 filled slots among the 12, not decorations. Full record:
  DECISIONS.md -> [Combat] (4 entries) and [Combat/Performance].
- **Built:**
  - `.move-option-label` centering bug root-caused via live layout inspection (`execute_code`):
    Unity's default runtime theme applies non-zero, ASYMMETRIC margin/padding to every Label —
    added explicit `margin:0; padding:0`.
  - "S" (Strike) -> "C" (Charge): new `BattleHUDController.SelectCharge` (single click, no drag,
    no target) restores `BattleConfig.ChargeAuraRestore` (10) Aura and ends that attacker's turn
    without an attack; new `ShowMoveSelection(... onChargeSelected)` overload;
    `BattleManager.PlayerTurn`'s new `chargeSelected` branch.
  - New `CompanionAI.SetPaused(bool)` (disables both CompanionAI and its AIPath component — AIPath
    repaths on its own timer independent of CompanionAI) and `PartySystem.ActiveCompanionAI`;
    `BattleManager` pauses the companion in `Start()`, resumes it in `EndBattle()`, alongside the
    existing overworld-camera hide/restore.
  - New `.skill-slot-placeholder` — 12 per player creature, one per clock hour, same radius AND
    same 32x32 size as "A"/"C" (iterated from a separate outer ring, to same-radius-smaller, to
    final same-radius-same-size after 2 rounds of live feedback) — `BattleHUD.uxml` orders them
    before the move options so "A"/"C" paint fully on top at hours 1/11.
- **Verified:** Live Play Mode throughout — Aura drain-then-Charge-click showed 5/20 -> 15/20 with
  enemy HP unchanged (no attack); `CompanionAI.enabled`/`AIPath.enabled` confirmed `true` before
  battle, `false` immediately after engaging; final skill-ring screenshot (1920px) shows 12
  uniform slots, "A"/"C" fully filling 1/11 o'clock with no grey visible underneath. 120/120
  EditMode tests passing after every change in this batch.

[2026-08-05] Phase 3 — Move orbs fixed at 1/11 o'clock, centered labels, blue/green orb colors
- **Context:** User asked to put the two move orbs at the 1 and 11 o'clock positions (instead of
  the full-circle top/bottom split from the immediately prior session entry), make sure the
  letters are centered on the orb, and color one blue and one green. Full record: DECISIONS.md ->
  [Combat].
- **Built:** New `MoveOptionClockHours = { 1f, 11f }` array; `PositionMoveOptions` converts clock
  hour -> math degrees (`90 - 30*hour`) instead of the generalized full-360° split.
  `.move-option-label` switched to absolute-fill + `-unity-text-align:middle-center` for
  guaranteed centering. New `.move-option-blue` class applied to the "S" orb only; "A" keeps its
  original green styling.
- **Verified:** Live Play Mode screenshot (1920px) — blue "S" at 11 o'clock, green "A" at 1
  o'clock, both letters centered. 120/120 EditMode tests unaffected.

[2026-08-05] Phase 3 — Move orbs now distributed around a full circle, not an arc above the creature
- **Context:** User asked to make sure the attack orbs sit in a circle around the Phasix — the
  prior layout confined them to an 80° arc above it, so both orbs still read as clustered at the
  top. Full record: DECISIONS.md -> [Combat].
- **Built:** `PositionMoveOptions` now spaces options across the full 360° (`90 + 360*i/total`)
  instead of a fixed arc — "A" sits directly above the creature, "S" directly below, opposite
  points on the circle. Generalizes to a future 3rd/4th option automatically.
- **Verified:** Live Play Mode screenshot — orbs symmetric above/below with no UI overlap.
  120/120 EditMode tests unaffected.

[2026-08-05] Phase 3 — Higher starting Aura, numeric HP/Aura readouts, move orbs pushed out and shrunk
- **Context:** Follow-up polish after seeing the Aura cost/restore mechanic live. Full record:
  DECISIONS.md -> [Combat].
- **Built:** `Test_FireType`/`Test_SteamType` Aura bumped 5 -> 20 (room to see multiple attacks'
  cost/restore over a real battle). New `.bar-value-label` overlay shows "current/max" on both
  the HP and Aura bars for all 4 status rows; `SetHPFill`/`SetAuraFill` now set the label text
  alongside the fill width in one call. `MoveOptionRadius` 60px -> 95px and
  `.move-option-placeholder` 48x48 -> 32x32 (floating-orb separation from the creature, smaller
  icons); labels shortened from "ATK"/"ATK" to "A"/"S" (Attack/Strike).
- **Verified:** Live Play Mode screenshot — HP reads "120/120", Aura reads "20/20" on both status
  rows; the two move orbs render smaller with clear separation from the creature. 120/120
  EditMode tests unaffected (pure UI/data-value change).

[2026-08-05] Phase 3 — Attacks cost Aura; a perfect Dodge/Parry restores it
- **Context:** Final step of the sequence the user laid out: add a second attack, see the
  layout, restyle the icons, then wire real Aura costs. Full record: DECISIONS.md -> [Combat].
- **Built:** `BattleParticipant.SpendAura`/`.RestoreAura` (clamped at 0/MaxAura respectively,
  spending never blocks the attack). `BattleConfig.AttackAuraCost` (2, both placeholder attacks
  cost the same) spent in `PlayerTurn` right after target confirmation. `BattleConfig.PerfectDefenseAuraRestore`
  (2) restored to the defender in `EnemyTurn` whenever `LastDefenseWasPerfect` is true on a
  successful Dodge/Parry, with a "X restores Aura!" battle log line. `BattleHUDController.RefreshHP`
  renamed to `RefreshBars` and extended to also refresh Aura fill bars, since Aura now changes
  during battle instead of staying static after `Initialize`.
- **Tests:** New `BattleParticipantTests` (6 tests) covering `SpendAura`/`RestoreAura` clamping
  and no-op-on-zero/negative behavior. 120/120 EditMode tests passing (was 114).
- **Verified:** Live Play Mode — a real drag-to-target attack dropped Aura from 5/5 to 3/5
  (screenshot-confirmed bar shortening); `RestoreAura` confirmed against the live battle-state
  participant, correctly clamping back to 5/5 (screenshot-confirmed bar refilling). The
  perfect-detection signal itself (`LastDefenseWasPerfect`) was already proven correct via manual
  coroutine-stepping in the prior ring-color session — this pass wires `RestoreAura` on top of
  that.

[2026-08-05] Phase 3 — Second attack option; move icons now plain Sonny 2-style circles labeled "ATK"
- **Context:** User wanted a second attack option to see the 2-icon radial layout before wiring
  Aura costs, then asked for the icon style itself to change to match Sonny 2's actual reference
  art (plain circles, minimal text) instead of pill-shaped buttons with descriptive labels. Full
  record: DECISIONS.md -> [Combat].
- **Built:** `BattleHUDController.PositionMoveOptions` now computes each move option's position
  via trigonometry (spaced evenly across a configurable arc, `MoveOptionsPerSlot = 2`) instead of
  one hardcoded offset — generalizes to a 3rd/4th option later with no new positioning code.
  `.move-option-placeholder` changed from a 70x38 pill to a 48x48 circle; both options now
  labeled "ATK" (identical text, position is the only distinguishing signal) instead of "Attack
  1"/"Attack 2" — both still mechanically identical basic attacks, real skill differentiation is
  a separate later pass.
- **Verified:** Live Play Mode screenshots at each step — first confirming the 2-pill layout
  (pills touched slightly at the shared radius), then the circular "ATK" icons sitting cleanly
  apart with no overlap. 114/114 EditMode tests unaffected (pure UI sizing/positioning change).
- **Next:** Aura costs on attacks, and perfect Dodge/Parry restoring Aura — the user's follow-up
  ask, queued for once this layout was confirmed.

[2026-08-05] Phase 3 — Ring flash colors redesigned around a miss/success/"perfect" quality ladder
- **Context:** User asked whether Dodge's flash was already green (it wasn't — it was orange, a
  per-move color scheme) and asked for a redesign: red stays for a miss, green for a normal
  success, and a new bright neon purple "perfect" tier for a near-dead-center hit, shared
  identically across Dodge/Parry/offense rather than colored per-move. Full record: DECISIONS.md
  -> [Combat].
- **Built:** `SuccessFlashColor`/`PerfectFlashColor`/`MissFlashColor` replace the old
  `DodgeFlashColor`/`ParryFlashColor`/`OffenseFlashColor` — one shared 3-tier palette instead of
  per-move colors. New `PerfectToleranceFraction` (0.2 placeholder): a hit counts as "perfect"
  when its ratio deviation from dead-center is within the innermost 20% of whichever tolerance
  applied. New `LastTimedInputWasPerfect`/`LastDefenseWasPerfect` bool properties expose the tier
  (not wired to any gameplay bonus yet — visual feedback only for this pass, deliberately not
  inventing what a "perfect" should mechanically do beyond what was asked).
- **Verified:** A new verification technique for this precision-dependent case — manually driving
  the `RunDefenseTimedInput` coroutine via direct `IEnumerator.MoveNext()` calls instead of
  `StartCoroutine`, to deterministically land the marker at an exact chosen ratio before a
  synthetic click, sidestepping this session's recurring unfocused-Editor large-`Time.deltaTime`
  timing issue. Confirmed exact RGBA matches for all three tiers: a ~0.00002 deviation (dead
  center) resolved as a perfect Parry with the exact purple value; a 0.15 deviation (inside
  Dodge's tolerance but outside its perfect band) resolved as a normal Dodge success with the
  exact green value, screenshot-confirmed rendering correctly.

[2026-08-05] Phase 3 — Offense joins the converging ring; ring redesigned to target+marker ratio with flash feedback
- **Context:** Two rounds of live follow-up feedback on the just-shipped converging-ring defense
  timing. First: "make the attack input similar to the defend input but on the targeted enemy."
  Then, mid-implementation: simplify the ring itself — no pre-drawn zone bands, just a fixed
  target ring and a white marker ring that starts wide and shrinks past it, judged by their size
  ratio (Dodge within 0.75-1.25x, Parry within 0.9-1.1x) with orange/green flash feedback on
  success. Then: flash red on an absolute miss. Full record: DECISIONS.md -> [Combat] (two
  entries — "Sonny 2-style radial move selection..." for the first pass, "Unified converging-ring
  timing..." for this one).
- **Built:** `RunTimedInput` (offense) dropped the horizontal bar entirely and now uses the same
  `RingVisual` as defense, reparented above the TARGETED ENEMY. `RingVisual` simplified from
  "two nested zone bands + marker" to `TargetRadius`/`MarkerRadius`/`MarkerColor` — a fixed
  reference ring plus one converging marker, no visible pre-drawn success zone. New
  `TimedInputConfig.ComputeToleranceHalfWidth` reuses the existing Instinct/bond
  `ComputeWindowPercent` curve as a proportional scale factor on the new ratio-tolerance
  constants (`OffenseToleranceHalfWidth`/`DodgeToleranceHalfWidth`/`ParryToleranceHalfWidth`),
  preserving "higher Instinct = larger window" under the new mechanic. Marker flashes orange
  (Dodge success), green (Parry or offense success), or red (any Miss — wrong tolerance, wrong
  button, or timeout) before hiding. Deleted the now-fully-unused horizontal-bar UXML/USS/fields
  (`TimingBarTrack`/`Zone`/`Marker`/`Button` and `.timing-bar-*`) rather than leaving them as dead
  code; renamed the surviving text-only label host `TimingBarContainer`/`Label` ->
  `ActionAnnouncement`/`ActionAnnouncementLabel` since "TimingBar" was now a misnomer.
- **Tests:** 3 new EditMode tests for `ComputeToleranceHalfWidth` (zero-stat exact-base case,
  Instinct widens it, Parry stays narrower than Dodge). 114/114 EditMode tests passing.
- **Verified:** Live Play Mode across each iteration — a real click-drag attack landing correctly
  on the enemy with the ring there (12 damage), isolated synthetic-click proofs for every outcome
  (Dodge/Parry/offense success via a deliberately huge tolerance to make timing deterministic;
  Miss via a deliberately tiny one, confirming the exact red RGBA flash value), and a screenshot
  of the simplified target+marker visual rendering correctly above the defending creature.

[2026-08-05] Phase 3 — Sonny 2-style radial move selection + drag-to-target; converging-ring defense
- **Context:** User referenced Sonny 2's radial move-options-around-the-character (instead of a
  boxed menu) and asked for click-and-drag targeting, plus a circular converging-ring visual for
  defense timing instead of the horizontal bar, positioned above the defending Phasix. Full
  record: DECISIONS.md -> [Combat].
- **Built:** `DragLineVisual`/`RingVisual` (new Painter2D-drawn custom `VisualElement`s —
  straight line and stroked-circle/annulus-band drawing respectively). The boxed `MoveMenu` is
  gone; each player stage slot has an "Attack" placeholder positioned above its creature
  (`BattleHUDController.ShowMoveSelection`), built as the first slot of a radial system (only
  one move exists today). Pressing it starts a click-and-drag — `DragLineVisual` follows the
  cursor from the creature to wherever the player drags; releasing over the enemy confirms the
  target via the same callback pattern `ShowMoveMenu` used, releasing elsewhere cancels back to
  retry. `RunDefenseTimedInput` now positions a `RingVisual` above the defending stage slot
  instead of using the (now offense-only) horizontal bar — same nested Dodge/Parry zone math,
  drawn as concentric bands with a marker ring that converges inward over the sweep instead of a
  marker sweeping left-to-right.
- **Bug caught during verification:** hiding only the offense zone highlight (not the whole
  timing-bar track) during defense left the track's background bar and a stale marker visibly
  leaking through behind the DEFEND label. Fixed by hiding the entire `TimingBarTrack` for
  defense, only showing it for offense.
- **Verified:** Live Play Mode — screenshot-confirmed the "Attack" placeholder position, a
  synthetic drag correctly drawing the line from player to enemy and resolving into a real
  13-damage attack on release, and the converging ring rendering correctly (both zone bands
  visible, no track leak) after the fix. 111/111 EditMode tests unaffected (UI/interaction-layer
  change only).

[2026-08-05] Phase 3 — Continue button now only gates the player-to-enemy turn transition
- **Context:** User wanted Continue to only appear "when transitioning from player to enemy" —
  once clicked, the rest of the enemy's turn should just play out with enough time to read, not
  require a click per beat. Full record: DECISIONS.md -> [Combat] "Battle pacing" entry.
- **Built:** New `BattleHUDController.ShowTimedMessage(message, durationSeconds)` — shows the
  same message panel `WaitForContinue` uses, minus the button, auto-hiding after a fixed
  duration instead of waiting for a click. New `BattleConfig.AutoMessageDurationSeconds` (1.5s
  placeholder) shared by every auto-paced beat. `PlayerTurn`'s single `WaitForContinue` moved
  from per-attacker to once after the whole loop (the actual player-to-enemy handoff);
  in-between attacker beats now use `ShowTimedMessage`. `EnemyTurn` has no `WaitForContinue`
  calls left at all — attack announcement, defended/hit result, and counter-attack result all
  auto-pace via `ShowTimedMessage`; the live Dodge/Parry click remains the one real interactive
  moment in the enemy's turn.
- **Verified:** Live Play Mode — confirmed the single Continue gate appears exactly once (after
  the player's attack resolves, screenshot-verified with the button visible), and clicking it
  plays the enemy's entire turn (announcement -> defense window -> result) through automatically
  with zero further clicks, landing back on the next move menu. 111/111 EditMode tests still
  passing (pure pacing/UI change, no combat math touched).

[2026-08-05] Phase 3 — Dodge/Parry reworked to a live click, no more choose-then-time menu
- **Context:** User wanted the Dodge/Parry defense to "feel more live" — left-click to attempt
  Dodge, right-click to attempt Parry, clickable anywhere on screen rather than aiming at a
  button, for as long as the timing bar is running. Full record: DECISIONS.md -> [Combat]
  "Defense model" entry's 2026-08-05 Update.
- **Changed:** `BattleHUDController.ChooseDefense` (the Dodge/Parry choice-button prompt) and the
  `Dodge`/`Parry` values of `RunTimedInput`'s `TimedInputMode` are gone. New
  `RunDefenseTimedInput` shows ONE bar with the wide Dodge zone and, nested inside it as a
  sub-range, the narrower Parry zone — both drawn at once, single shared marker sweep. A
  `PointerDownEvent` callback registered on the HUD's screen-covering root element (not any
  specific button) reads which mouse button clicked and where the marker was at that instant:
  left-click inside the Dodge zone -> Dodge; right-click inside the (nested) Parry zone -> Parry;
  anything else (wrong zone, wrong button, or no click before the sweep ends) -> Miss, full
  damage, same as before. `TimedInputConfig.ParryMarkerSweepDuration` removed — Parry's
  difficulty is now entirely the nested zone's narrower WIDTH, since one shared marker can't
  sweep at two different speeds for two different possible actions. Removed the now-dead
  `DefenseChoicePrompt`/`DodgeChoiceButton`/`ParryChoiceButton` UXML and their CSS.
- **Verified:** Live Play Mode — the real battle flow still works end-to-end (a timeout correctly
  resolves to a Miss with full damage and no counter-attack); isolated proofs confirmed a
  synthetic left-click resolves to `Dodge` and a synthetic right-click resolves to `Parry`
  (precisely timing a live click via MCP is unreliable, so used the same guaranteed-width-window
  isolation technique as earlier `RunTimedInput` verification); screenshot confirms both zones
  render correctly nested on one shared bar with the new instructional label ("Left-Click Dodge ·
  Right-Click Parry"). 111/111 EditMode tests still passing (no test changes needed — this was a
  pure input/UI rework, the underlying `BattleEngine`/`BattleLogFormatter` logic was untouched).

[2026-08-05] Phase 3 — Aura stat allocation, capture, evolution burst, audio/VFX hooks (Step 5 scaffolding)
- **Context:** Continuing the approved Phase 3 plan past Step 4. Research first surfaced a real
  contradiction between two equally-"Locked" GDD sections on whether losing a battle costs bond
  (§21.6: no bond loss at all; §14.5: Micro -0.5-1% bond loss on losing) — asked the user
  directly rather than guessing; resolved to **zero bond loss on losing a battle** (§21.6 wins).
  No code change needed — `BattleManager.EndBattle` already never touches bond on loss; this just
  confirms that's correct going forward. Full record in DECISIONS.md -> [Combat].
- **Built:** `AuraTierCeiling`/`AuraStatAllocationSystem` (Common Aura -> free stat point
  allocation, gated by a tier+Aptitude stat ceiling, per Progression_Directive_v0_1_0.md's "Free
  Allocation Model"/"Tier Stat Ceiling"). `ResonanceBonusEvaluator` (alignment-based bonus
  multiplier — see decision note below on its Temper-based proxy). `CaptureSystem` (capture
  chance formula + attempt, raises `EventBus.OnPhasixCaptured` on success). `EvolutionBurstGauge`/
  `EvolutionBurstSystem` (Type K mid-battle evolution burst — GDD §9.3's gauge fill/trigger/expiry
  state machine; reliable trigger at >=40% bond per GDD §14.2, unreliable-but-possible below it).
  `BattleAudioVfxHooks` (empty `EventBus` subscriber stubs for 8 battle events, wired via
  `[RuntimeInitializeOnLoadMethod]` matching `SkillTreeUnlockSystem`'s pattern from the Step 4
  pass — no actual audio/VFX content, since GDD §27 Audio Design is entirely "Pending, design
  work not yet started").
- **Honesty note on this pass vs. Step 4:** Step 4's skill taxonomy/status/chain/mastery content
  was almost entirely locked GDD content with only the numbers pending. Step 5 is the opposite —
  Progression_Directive locks the Aura *mechanics* but leaves every number pending, capture has
  **no locked formula of any kind** (not even a mechanic shape beyond "every enemy is
  capturable"), evolution burst is 4 bullet points total, and audio/VFX has zero content
  anywhere. Built as clearly-flagged placeholder scaffolding throughout — see
  NumericalCalibration.md's new "Step 5 scaffolding" section and DECISIONS.md -> [Progression/Combat]
  for exactly which pieces are placeholder-shape-and-number vs. locked-shape-placeholder-number.
- **Tests:** 25 new EditMode tests (`AuraStatAllocationSystemTests`, `ResonanceBonusEvaluatorTests`,
  `CaptureSystemTests`, `EvolutionBurstSystemTests`). 111/111 EditMode tests passing (was 86).
- **Verified:** Live Play Mode via unity-mcp — confirmed the new `[RuntimeInitializeOnLoadMethod]`
  subscribers (`SkillTreeUnlockSystem` from Step 4, `BattleAudioVfxHooks` from this pass)
  coexist cleanly with the existing battle flow, no console errors, full encounter->battle loop
  still works end-to-end (screenshot-verified).
- **Next:** Both Step 4 and Step 5's data/rules layers are now built and tested. What's left of
  the Phase 3 plan is UI/integration work with no remaining locked-content research to do: a real
  skill-selection battle UI (to replace "Attack" as the only move and actually exercise the
  combo/status/chain/mastery/burst systems live), and the Phase 3 Gate's full playtest pass. Both
  are UI-heavy and better done with the user's iterative feel/UX feedback loop (as the rest of
  this session's combat HUD work was) rather than built blind.

[2026-08-05] Phase 3 — Skill Tree Framework, closes out the mechanical scope of Step 4 (Roadmap_v2 Mo 6 Wk 3 – Mo 7 Wk 4)
- **Context:** Continuing the approved Phase 3 plan after the Dodge/Parry work. Extracted exact
  locked design content from the GDD first (§4 taxonomy/combo, §14.2 bond zones, §15 attribute
  scaling, §17 status effects) rather than inventing any of it — found and fixed a real doc bug
  in the process (see below). See DECISIONS.md -> [Combat] for the full decision record,
  including what's deliberately NOT built yet.
- **Built:** `SkillTreeCatalog` (per-type primary attribute/role/bond-gate metadata for all 18
  `SkillTreeType` values, reusing the already-existing locked enum) and `SkillSlotCapacity`
  (tier -> tree count / active slot table, T1=2...T5=5-7, throws rather than inventing numbers
  for the fusion-dependent T6/T7). `SkillTreeUnlockSystem` wires Type F (Bond, unlocks at 20%
  bond) and Type O (Personality, unlocks at 40% bond) to `EventBus.OnBondMilestoneReached` via
  `[RuntimeInitializeOnLoadMethod]`. `StatusEffectType`/`StatusEffectCategory`/
  `StatusEffectCatalog` (all 28 statuses across 5 categories, with the exact mastery-bonus tag
  sets transcribed verbatim from GDD §17.9 rather than derived from category/flavor text) and
  `StatusDurationCalculator` (the locked `base + Resonance - Resolve, min 1, positive statuses
  skip Resolve` formula). `ChainResultType`/`ChainResultCatalog` (all 7 chain results, several
  with multiple valid recipes) and `MasteryBonusType`/`MasteryBonusCatalog` (all 8 bonuses,
  `EvaluateAll` returns every bonus currently satisfied by a self/target status pair — stacking
  supported). `ComboTier`/`ComboEngine` (Duo/Trio/Quad detection via trailing-window distinct-tree
  checking, plus placeholder Instinct-scaled trigger chance and bond->60%-gated discovery bonus
  curves). 36 placeholder `SkillData` assets (2 per tree type, `Assets/Data/Skills/`, generic
  names/descriptions, created via a bulk `AssetDatabase` script rather than 36 manual Inspector
  passes).
- **Doc fix found along the way:** `ClaudeCode_Primer_v1_1_0.md` said "24" statuses; the GDD's
  own §17 tables actually sum to 28 (verified by reading every row, not assumed). Fixed the
  Primer rather than building the wrong count into code.
- **NOT built this pass:** live in-battle skill selection. `BattleManager`/`BattleHUD` only
  support "Attack" — there's no move menu to pick a specific skill from yet, so there was
  nothing to wire the combo/status/chain/mastery systems INTO live gameplay with. This is a
  rules-layer/data-model pass, verified the same way `DamageCalculator`/`PrimalTypeChart` were —
  thorough EditMode tests, not a new UI with nothing real behind it yet.
- **Tests:** 43 new EditMode tests across `StatusEffectCatalogTests`, `StatusDurationCalculatorTests`,
  `ChainResultCatalogTests`, `MasteryBonusCatalogTests`, `ComboEngineTests`, `SkillSlotCapacityTests`,
  `SkillTreeUnlockSystemTests`. 86/86 EditMode tests passing (was 43).
- **Docs:** `NumericalCalibration.md` gained a "Skill tree / status / combo framework" section
  covering every placeholder number introduced (duration ranges, stat-to-modifier conversion,
  combo trigger chance/discovery bonus curves, chain-result tie-break). `DECISIONS.md` ->
  [Combat] has the full record, including the specific interpretation calls made where the GDD's
  prose didn't fully spell out self/target sides for a couple of mastery bonuses.
- **Next:** The live skill-selection battle UI (a real move menu beyond "Attack," wiring actual
  skill use into `BattleManager`, then the plan's "manually trigger a combo in Play Mode"
  playtest checkpoint) is the natural follow-up once there's a reason to build it. Plan Step 5
  (bond gauge/capture/Aura system/VFX hooks) is next in the approved plan.

[2026-08-05] Phase 3 — Battle HUD: action panel + battle log split 50/50 across the screen width
- **Built:** `.action-panel`/`.battle-log-panel` (`BattleHUD.uss`) switched from fixed `420px`
  widths to `flex-grow: 1; flex-basis: 0` inside `.stage-bottom-row` (already `left:0; right:0`,
  edge-to-edge) — each panel now fills exactly half the screen width, with only a small
  margin-based gap between them. Height stays fixed at `300px` on both, unchanged.
- **Verified:** Live Play Mode screenshot via unity-mcp — both panels span from their screen edge
  to the center line.

[2026-08-05] Phase 3 — Dodge/Parry defense system, replaces flat damage-reduction defense (Expedition 33-inspired, user-directed)
- **Context:** User played the earlier flat-multiplier defense live and asked to mimic Clair
  Obscur: Expedition 33's combat feel instead. Clarified via AskUserQuestion: full avoidance
  (not a reduction multiplier), and two distinct options (not one) — Dodge (safe, avoid-only)
  vs. Parry (risky, avoid + auto-counter). See DECISIONS.md -> [Combat] for the full decision
  record and Combat_Directive_v0_1_0.md Part 4 for the superseded/updated design text.
- **Built:** `BattleHUDController.ChooseDefense` — a new choice prompt (Dodge/Parry buttons,
  blue/orange respectively) shown before the timing check on every enemy attack.
  `RunTimedInput` reworked from an `isDefensive` bool to a `TimedInputMode { Offense, Dodge,
  Parry }` enum plus an explicit `sweepDuration` param (Parry sweeps faster — 0.7s vs. 1.2s —
  via `TimedInputConfig.ParryMarkerSweepDuration`), each mode with its own bar color/button
  text (green/"Time It!", blue/"Dodge!", orange/"Parry!"). `TimedInputConfig` gained
  `DodgeBaseWindowPercent` (20%, wide/easy) and `ParryBaseWindowPercent` (6%, narrow/hard) plus
  an overloaded `ComputeWindowPercent(baseWindowPercent, instinct, bondPercent)`;
  `SuccessDefenseMultiplier` was removed entirely (defense is binary avoid/full-damage now, not
  a multiplier). `BattleManager.EnemyTurn` rewritten: choose defense -> run the matching timing
  check -> `damageMultiplier: 0f` on success (fully avoids the hit, same
  `BattleEngine.QueueBasicAttack`/`ResolveQueuedActions` path every other attack uses) or `1f`
  on failure (full hit, same as a missed offensive press — "reward, don't punish", no extra
  penalty for a failed Parry attempt) -> if the defense was a successful Parry, an automatic
  counter-attack fires (`target` attacks `attacker`, no timing check of its own, same
  `QueueBasicAttack`/`LogResults` pattern as the player's own attacks). `BattleLogFormatter`
  gained `FormatDefenseOutcome` (avoided: "X dodges Y's attack!" / "X parries Y's attack —
  opening for a counter!", no damage number at all; failed: identical text to a normal attack)
  and had `FormatAttack` simplified back down (dropped the now-dead `isDefensiveTiming` param —
  every remaining caller was always passing `false` once defense moved to its own formatter).
  New/updated `BattleHUD.uxml`/`.uss`: `DefenseChoicePrompt` block, `.defense-choice-prompt`/
  `.dodge-choice-button`/`.parry-choice-button`, and the timing-bar color classes renamed/split
  from offense/defense to offense/dodge/parry (green/blue/orange).
- **Forward-looking note (user-flagged):** future attacks/skills may need multiple
  action-command beats per attack (multi-hit offense, multi-check defense). `RunTimedInput`
  stayed a single-window primitive on purpose so a future multi-beat attack can just call it
  more than once in sequence — no rework needed for that later.
- **Tests:** `BattleEngineTests`'s defense-multiplier test renamed/rewritten to
  `ResolveQueuedActions_ZeroMultiplier_FullyAvoidsDamage` (0 multiplier = full avoidance).
  `BattleLogFormatterTests` updated for `FormatAttack`'s new signature and 3 new
  `FormatDefenseOutcome` cases (dodged/parried/failed). `TimedInputConfigTests` gained a test
  confirming Parry's window is narrower than Dodge's at equal Instinct/bond. 43/43 EditMode
  tests passing.
- **Verified:** Live Play Mode via unity-mcp — teleported onto a wild encounter, engaged,
  confirmed the offensive attack flow end-to-end (13 damage, log entry, HP bar), the Dodge/Parry
  choice prompt rendering correctly (screenshot), the Parry timing bar's label/button/color, a
  failed Parry taking full damage and logging as a plain attack with no counter firing, and (via
  an isolated `RunTimedInput` call with a guaranteed-width window, since MCP round-trip latency
  makes precisely timing a click during a live 0.7s sweep unreliable — same workaround used
  earlier this session) that Parry mode's success detection resolves correctly. The
  defended-and-counter wiring in `EnemyTurn` reuses the exact call pattern already proven live
  in `PlayerTurn`, just with attacker/target swapped.
- **Docs:** `Combat_Directive_v0_1_0.md` Part 4 updated in place (defense section marked
  superseded with the new model spelled out). `NumericalCalibration.md`'s action-command
  sections updated with Dodge/Parry windows, sweep durations, and damage modifiers (all still
  placeholder). `DECISIONS.md` -> [Combat] has the full decision record.
- **Next:** Steps 5-6 of the Phase 3 plan (skill tree framework; bond gauge/capture/Aura/VFX
  hooks) are still pending — user has been focused on iterative combat feel/UX rather than
  moving forward, so follow their explicit direction on when to resume those.

[2026-08-05] Phase 3 — Damage formula + Primal type chart, closes out Step 3 (Roadmap_v2 Mo 6 Wk 1-2)
- **Context:** Replaces `BattleConfig.PlaceholderAttackDamage`'s flat 5-damage stand-in with the
  real formula. Unlike most Phase 3 numbers, the type chart itself is NOT a placeholder — GDD
  §9 has a full 8x8 matchup table marked "Locked v0.2.0"; found it before inventing anything.
- **Built:** `DamageCalculator.ComputeDamage` — `(AttackerStat / DefenderStat) x skillPower x
  primalTypeMultiplier`, Physical = Force/Guard, Elemental = Resonance/Ward (CLAUDE.md). Basic
  Attack treated as Physical with placeholder `skillPower = 10` (real skill content is Step 4).
  `PrimalTypeChart` ScriptableObject — the 8 base types' locked 8x8 matrix transcribed verbatim
  from the GDD, minimum 0.5x (no immunities). Duo-merge types (28 of 36 `PrimalType` values, e.g.
  the existing `Test_SteamType` test asset) aren't individually charted in the GDD — resolved by
  averaging across each duo's base parents, reusing `PrimalTypeColor`'s existing parent-pair data
  (now exposed via a new `GetDuoParents` method) rather than duplicating it. New
  `Assets/Data/TypeCharts/PrimalTypeChart.asset`, wired into `BattleManager` via a new
  `[SerializeField] PrimalTypeChart _typeChart` (falls back to a neutral 1.0x if left unassigned,
  rather than crashing).
- **`BattleEngine`/`BattleAction` change:** `BaseDamage` is now caller-supplied (default
  `BattleConfig.PlaceholderAttackDamage` for callers that don't need the real formula, e.g. tests)
  instead of `BattleEngine` hardcoding the flat constant — `BattleManager` computes it via
  `DamageCalculator` before queueing. `DamageMultiplier` (the Step 2 timed-input bonus/reduction)
  still applies on top, unchanged — matches CLAUDE.md's "apply timed bonus after formula."
- **Logged in `NumericalCalibration.md`:** all the Step 2/3 placeholder values that were still
  marked PENDING there (timing windows, damage modifiers, `skillPower`) now have their actual code
  defaults recorded, clearly flagged as unplaytested placeholders, not final balance.
- **Verified:** 11 new EditMode tests (`PrimalTypeChartTests` — spot-checks against the locked GDD
  values including the Water-beats-Fire/Light-Shadow-mutual-2x callouts, duo-type averaging, and a
  full-chart no-immunities sweep; `DamageCalculatorTests` — stat-pair selection by category,
  null-chart fallback, minimum-1-damage floor) — 33 total, all passing. Live Play Mode run: both
  combatants happened to roll the same test species (Force 8, Guard 6) — computed
  `(8/6) x 10 x 1.0 = 13.33 -> 13` and confirmed both sides' HP dropped by exactly 13, not the old
  flat 5.
- **This closes out Step 3.** Next: Step 4 — skill tree framework (Roadmap_v2 Mo 6 Wk 3 – Mo 7 Wk
  4): `SkillTreeData` SO (18 types), bond-gated Type F/O unlocks, combo engine, 24-status effect
  engine.
- **Added same day, per user request — text battle log:** new `BattleActionResult` (what
  `BattleEngine.ResolveQueuedActions` actually applied — attacker, target, final damage; the method
  now returns `List<BattleActionResult>` instead of `void`) and `BattleLogFormatter` (pure text
  formatting: damage number, Primal-type effectiveness flavor text mapped from the multiplier
  — "super effective"/"effective"/neutral (no note)/"not very effective"/"barely effective" — and,
  when the relevant action-command timing succeeded, "timing was perfect!" for offense or "blocked
  just in time!" for defense). `DamageCalculator.ComputeTypeMultiplier` made public so the log can
  report the same multiplier `ComputeDamage` used internally, without recomputing or risking drift.
  New scrolling `BattleLogPanel`/`ScrollView` in `BattleHUD.uxml`/`.uss`.
  `BattleHUDController.AppendBattleLog`/`ClearBattleLog` added; `BattleManager` clears the
  log at battle start and appends one line per resolved attack. 7 new EditMode tests
  (`BattleLogFormatterTests`) — 40 total, all passing. Verified live: log correctly showed both
  attacks with exact damage numbers; separately proved the "super effective!"/"timing was perfect!"
  text branches via an isolated Water-vs-Fire/successful-timing scenario.
- **Post-review layout pass (same day):** log was centered on the stage with small text — hard to
  notice/read. Removed the old fixed-height `BottomBar` entirely and replaced it with a
  `StageBottomRow` inside `Stage`: `ActionPanel` (current turn/move menu/timing bar) on the left,
  `BattleLogPanel` on the right, both fixed 420x300px bordered panels so toggling between MoveMenu
  and TimingBarContainer still doesn't reflow anything (same discipline as the original bottom-bar
  fix). Creatures moved from `top: 50%` to `top: 30%` to keep clear of the new bottom row. Bumped
  battle log font 15px→19px and title 16px→22px; timing bar track narrowed 420px→360px to fit the
  smaller panel width. Re-verified live via screenshot — both panels clearly visible side by side,
  log text legible.
- **HP + timing-bar clarity pass (same day):** user reported attacks "just happening on their own,
  or happens twice" — turned out to be the two *separate* per-round action-command checks (offense
  on your own attack, defense blocking the enemy's) not reading as distinct, compounded by combat
  ending in 1-2 hits (Vitality 18-20 vs ~13 damage/hit) leaving no room to actually see a timing
  bonus/reduction play out. Fixed both:
  - `Test_FireType`/`Test_SteamType` Vitality bumped 20/18 → 120/110 so a fight runs ~6-9 rounds
    instead of 1-2.
  - `RunTimedInput` now takes an `isDefensive` flag; toggles `.timing-bar-offense` (green
    track/zone/button, "Time It!") vs `.timing-bar-defense` (blue, "Block!") on the container, so
    the two checks are visually unmistakable rather than only distinguished by reading the label.
    Labels themselves reworded too: "YOUR ATTACK — {name}" vs "INCOMING — Block {name}!". Verified
    the class-toggle and button-text swap directly for both modes (screenshot timing was unreliable
    here since a manual verification coroutine ended up racing the live battle loop — not a real
    bug, just a testing artifact).
- **Continue button — replaces fixed pacing timers entirely (same day, per user discussion):**
  auto-advancing on `AttackBeatPause`/`EnemyAttackAnnouncePause` meant guessing a single "right"
  delay for everyone, which needed re-tuning three times already and still didn't feel right.
  Traditional turn-based RPGs (Final Fantasy, Pokemon, Sonny 2) instead wait for player input
  between beats, so the player sets their own pace. New `BattleHUDController.WaitForContinue` — a
  Continue button/prompt shown after every single beat (the enemy's "is attacking!" announcement,
  and every resolved attack, player's or enemy's) — replaces both fixed-timer constants, which are
  now deleted. `ShowTurnMessage` also removed (no longer had any caller once `WaitForContinue`
  absorbed its job). Verified live: after one resolved attack, `ContinuePrompt` stays visible and
  the battle state stays frozen (enemy at 107/120, player still full) until a Continue click is
  simulated — confirmed clicking it correctly advances straight to the next beat (the enemy's
  attack announcement), not further.

[2026-08-05] Phase 3 — Battle scene + turn state machine (Roadmap_v2 Mo 5 Wk 1-2)
- **Context:** Phase 2 gate met (Wk 14-16 wild encounter closed it out); the repo audit fully
  closed the same day in a separate session. First real code in `Scripts/Combat/`.
- **Built:** `BattleConfig` (ActivePartySize=3 prototype constant, PlaceholderAttackDamage=5),
  `BattleParticipant`/`BattleAction`/`BattleState`/`BattleOutcome` (plain C# battle-only state),
  `BattleEngine` (static turn-resolution rules — queueing, damage, win/loss — matching
  `BondSystem.cs`'s testable static-logic pattern), `BattleManager` (MonoBehaviour coroutine state
  machine: PlayerTurn → EnemyTurn → ResolveActions → CheckWinLoss → EndBattle), `BattleHUDController`
  (UI Toolkit HP bars/name plates/move menu, matching `EncounterPromptController`'s convention),
  `BattleTransition` (static additive-scene-load bridge, matching Combat_Directive Part 1's
  "overworld remains loaded underneath combat"). Expanded the `BattleResult` stub with real fields.
  New `Assets/Scenes/BattleScene_Main.unity` (added to Build Settings), new
  `Assets/UI/BattleHUD.uxml`/`.uss`.
- **Wired:** `WildEncounterCreature.HandleEngage` now actually starts a battle via
  `BattleTransition.StartWildBattle` instead of resolving as Flee (the Wk 14-16 scaffold TODO).
- **Fixed in passing:** `DebugPartyBootstrap` never set `baseStats` on its test companion —
  `BattleParticipant.MaxHP` clamped to 1 from a 0-Vitality PhasixRuntimeData, which would've made
  any battle playtest end in one hit. Mirrors `WildSpawnSystem.CreateWildInstance`'s identical line.
  Also gave `Test_FireType`/`Test_SteamType` real (if placeholder) non-zero stats via
  `SerializedObject` — both were still all-zero from creation, same root cause.
- **Damage is a flat placeholder** (`BattleConfig.PlaceholderAttackDamage`) — the real
  `(AttackerStat / DefenderStat) x skillPower x primalTypeMultiplier` formula is Step 3 (Mo 6 Wk
  1-2). Move menu currently offers only "Attack" — real skill content is Step 4+.
- **Verified live:** 9 new EditMode tests (`BattleEngineTests.cs`, alongside `BondSystemTests.cs`)
  all pass under Test Runner. Full Play Mode run: teleported player onto a wild creature, invoked
  Engage, additive `BattleScene_Main` load succeeded, HUD showed HP/move menu, drove 4 rounds via
  the actual `BattleManager`/`BattleHUDController` callback path (no real input device in this
  session — same `execute_code`-driven technique as prior sessions), HP tracked correctly each
  round (20→15→10→5→0 both sides), EndBattle fired, scene unloaded, player unfroze, encounter
  prompt hid, and the engaged creature was destroyed while the other two spawn points' creatures
  were untouched.
- **Tooling note:** mid-playtest, `editor_state` showed a domain reload firing *during* Play Mode
  (`play_mode.is_changing` stuck `true` for several minutes), which reset `PartySystem.Instance`
  to null and left `BattleScene_Main`'s just-loaded GameObjects' `Awake()` un-dispatched, throwing
  a `NullReferenceException` in `BattleManager.Start()` the first time through. Stopping and
  re-entering Play Mode produced a clean run with no such reload. Likely external file-system
  interference (another concurrent session in this same working directory) rather than a code bug
  — see `LESSONS_LEARNED.md` for the full writeup and a faster-diagnosis path for next time.
- **Next:** Step 3 — damage formula + Primal type chart (Mo 6 Wk 1-2), then the skill tree
  framework (Step 4) and Aura/capture/VFX (Step 5) per the plan at
  `C:\Users\aluca\.claude\plans\i-have-another-session-gleaming-squid.md`.
- **Post-review fixes and HUD redesign (same day, after the user playtested it live — several rounds of feedback):**
  - **Panel scale bug:** the move menu visually overlapped the player name plate. Root cause:
    `UIRoot_BattleHUD`'s `UIDocument` reused `EncounterPromptPanelSettings.asset`, which is
    `ScaleWithScreenSize` at a 320x180 reference resolution — fine for one small compact widget,
    but on a real screen every UI pixel blew up ~2.7x. Fixed by giving the battle HUD its own
    `Assets/UI/BattleHUDPanelSettings.asset` (`ConstantPixelSize`, scale 1) so USS values map 1:1
    to screen pixels.
  - **Layout redesign (superseding the first color-band lane backdrop):** user reference was
    Sonny 2's battle screen, not a literal lane-striped background. Rebuilt `BattleHUD.uxml`/`.uss`
    into that shape — top HP+Aura list per side (party left, enemy right), a middle stage with
    placeholder creature circles (party left / enemy right / same lane, tinted via the existing
    `PrimalTypeColor` table), bottom turn label + action bar. All existing element names/IDs were
    kept identical so `BattleHUDController.cs`'s query logic didn't need to change.
  - **Added `BattleParticipant.MaxAura`/`CurrentAura`** — the Aura *base stat*
    (`EffectiveStat(StatType.Aura)`), not the Aura *resource*/currency system (still Step 5).
    Static full bar for now; nothing spends it in battle yet.
  - **Added `BattleLaneLayout.cs` + `BattleStageGizmos.cs`** — a placeholder world-space layout for
    the 7 lanes and a Scene-view-only gizmo drawing them (matches `CompanionAI`'s existing
    pattern-gizmo convention — `OnDrawGizmos` never renders to players or in a build). No visible
    lane lines in the actual HUD, per user direction.
  - **Scene-cut fix:** the battle was reading as a translucent HUD floating over the paused
    overworld, not a real scene transition. `BattleManager.Start()` now disables the overworld's
    `Camera.main` (component only, not the GameObject — leaves its `AudioListener` etc. alone;
    Screen Space Overlay UI doesn't need a camera to render) and `EndBattle()` re-enables it. Also
    gave `.battle-root` a solid (non-transparent) background as a belt-and-suspenders backdrop.
  - **Readability pass:** every HP/Aura bar, name label, and button was too small to read at 1:1
    pixel scale — bumped font sizes (12px→18-20px), bar heights (4-6px→10-16px), and paddings
    across the board.
  - Re-verified live via screenshot after each round: no more overlap, real scene cut with no
    overworld visible, bars/text clearly legible, creatures positioned correctly.
  - **Further sizing/spacing pass:** `.status-list` width changed from a fixed 280px to `33%` so
    HP/Aura bars scale with actual screen width instead of a flat pixel guess; bar heights bumped
    again (HP 16→24px, Aura 10→16px). Stage creatures pulled in from edge-anchored
    (`justify-content: space-between`) to `justify-content: center` + fixed side margins, so both
    sides sit noticeably closer to the middle instead of hugging the screen edges. Re-verified live.
  - **Final placement/font pass:** center-anchored margins overcorrected — creatures ended up too
    close together. Switched `.stage-side` from flex margins to explicit `position: absolute` with
    `left: 20%`/`right: 20%` (translated back 50% of the element's own size so the anchor is the
    group's center) so each side sits at a precise, screen-width-relative 1/5-in from its edge
    regardless of resolution. Bumped remaining text sizes further: name labels 20→30px, turn label
    18→26px, Attack button 16→24px. Re-verified live via screenshot.
  - **Turn-resolution bug:** clicking Attack was deducting HP from *both* sides at once. Root
    cause: `RunBattleLoop` queued the player's attack in `PlayerTurn`, then queued the enemy's
    counter-attack in `EnemyTurn`, then resolved *both* together in one batch — one click, one
    full round. Fixed by resolving each attack immediately as it's queued (`PlayerTurn`/
    `EnemyTurn` now each call `BattleEngine.ResolveQueuedActions` + refresh the HUD right after
    queueing their own single action, instead of `RunBattleLoop` batching both phases together).
    `BattleEngine` itself needed no changes — verified the resolve-only-affects-its-own-pair
    mechanism directly via `execute_code` (isolated `BattleState`/`BattleParticipant` proof, since
    MCP round-trip latency is too coarse to reliably catch the intermediate in-scene frame between
    the two turns). All 16 EditMode tests still pass unchanged.
  - **Pacing:** even after the turn-resolution fix, the enemy's counter-attack resolved in the same
    instant as the player's click, since `EnemyTurn` had no delay at all. Added
    `BattleManager.AttackBeatPause` (0.6s, placeholder — real animation-driven pacing is Step 5's
    battle VFX/audio hooks, Roadmap_v2 Mo 9) after each individual attack resolves in both
    `PlayerTurn` and `EnemyTurn`, so there's a readable beat between "my attack landed" and "the
    enemy is now attacking back."
  - **Found and fixed in passing:** `WildEncounterCreature.OnTriggerEnter2D` threw a
    `NullReferenceException` on `EncounterPromptController.Instance.IsVisible` — observed live at
    least once, root cause not fully isolated (possibly a trigger firing before that singleton's
    `Awake()` had run). Added a null guard; a silently-skipped encounter is a far better failure
    mode than an uncaught exception on a physics callback.
  - **Real bug found from a user-supplied screen recording:** the whole HUD (bottom bar, both
    stage creatures) visibly jumped down right as the battle scene loaded. Diagnosed by extracting
    frames from the recording (`cv2`, no `ffmpeg` on this machine) and tracking pixel positions —
    the jump was a one-time settle, not continuous drift, and it lined up exactly with the Editor
    Game View's "Display 1: No cameras rendering" state. Root cause: the earlier scene-cut fix
    fully disabled `Camera.main`, leaving *zero* enabled cameras in the scene — with none active,
    the Game View has no defined render-target reference, and the Screen Space Overlay panel's
    canvas size resolved inconsistently for the first frame or two before settling. Fixed by
    keeping the camera enabled but rendering nothing (`cullingMask = 0`,
    `clearFlags = SolidColor`, black) instead of disabling it outright — a valid camera always
    exists, so the Game View never enters that state. All three properties are cached and restored
    in `EndBattle`. Re-verified live: no jump, panel stable from the first frame.
  - **Turn clarity:** added `BattleHUDController.ShowTurnMessage()` — a plain status label with no
    button, shown during `EnemyTurn` (`"{name} is attacking!"`) so there's a visible cue for whose
    turn is active. Previously the enemy's turn had zero on-screen indication at all, which read as
    the pause doing nothing.
  - **Reflow bug:** hiding the Attack button for `ShowTurnMessage` shrank `.move-menu`'s content,
    which shrank the content-sized `.bottom-bar`, which reflowed `.stage` (`flex-grow: 1`) to fill
    the freed space every single turn — the visible symptom was the whole stage appearing to
    grow/shrink as the button toggled. Fixed by giving `.bottom-bar` a fixed `height: 150px`
    instead of letting it size to content, plus `justify-content: center` so the label/button stay
    vertically centered within that fixed space either way. Verified via resolved layout heights
    directly (`bottomBar.resolvedStyle.height` / `stage.resolvedStyle.height`), confirmed identical
    (150px / 761px) with the button shown vs. hidden.
  - **Pacing tweak:** `AttackBeatPause` (governs the pause after every attack, both player's and
    enemy's) bumped from 0.6s to 1.4s, then settled at 1.0s per direct user feedback on the live
    feel. Shared constant, so this affects both directions equally; flagged in case the two ever
    need to be split into separate values.

[2026-08-05] Phase 3 — Timed input system, closes out Step 2 (Roadmap_v2 Mo 5 Wk 3-4)
- **Context:** Combat_Directive_v0_1_0.md Part 4 — "Mario RPG / Paper Mario model": a timed press
  during an attack boosts outgoing damage (offensive action command), a timed press during an
  incoming hit reduces it (defensive action command). Window size scales with Instinct + bond per
  CLAUDE.md. Exact timing/threshold/modifier values are explicitly "pending numerical calibration"
  — everything below is a reasonable, clearly-tagged placeholder, not a locked design decision.
- **Built:** `TimedInputConfig` (window-size formula, marker sweep duration, success multipliers —
  all placeholder constants). `BattleHUDController.RunTimedInput` — a marker sweeps left-to-right
  across a bar over `MarkerSweepDuration`; a randomly-positioned success zone (width =
  `ComputeWindowPercent(instinct, bondPercent)`) is where the player must click "Time It!" while
  the marker is inside it; a miss by timeout resolves as a failure, same as clicking outside the
  zone. New `TimingBarContainer`/`TimingBarTrack`/`TimingBarZone`/`TimingBarMarker`/
  `TimingBarButton` elements in `BattleHUD.uxml`/`.uss`, toggled independently of `MoveMenu`.
- **Wired into `BattleManager`:** every attack — player's or enemy's — now runs the minigame before
  resolving. Offense uses the *attacker's* Instinct/bond for the window; defense uses the
  *defender's*. Success multiplies damage by `SuccessDamageMultiplier` (1.5x) on offense or
  `SuccessDefenseMultiplier` (0.5x) on defense; failure leaves damage unchanged (1x) — skilled play
  is rewarded, not punished, per the Directive's design intent ("keeps combat active... rewarded
  without making it inaccessible").
- **`BattleEngine` change:** `QueueBasicAttack`/`BattleAction` gained an optional `damageMultiplier`
  parameter (default 1, so all existing calls/tests are unaffected) — `BattleManager` computes the
  combined offense/defense multiplier and passes it in; `BattleEngine` itself stays timing-agnostic,
  just a single multiply. Matches CLAUDE.md's "apply timed bonus after formula" shape, so this
  carries over unchanged once Step 3's real damage formula replaces the flat placeholder.
- **Verified:** 6 new EditMode tests (`TimedInputConfigTests` — window scaling with Instinct/bond,
  min/max clamping; 2 new `BattleEngineTests` — damage multiplier applied correctly for both
  offense and defense) — 22 total, all passing. Live Play Mode run: full round (attack + defend)
  completes cleanly through both timing minigames via timeout (auto-fail) with no errors; separately
  proved the *success* path fires correctly by running the real coroutine with a 100%-wide window
  (guarantees any click succeeds) and confirming `LastTimedInputSuccess` came back `true`.
- **This closes out Step 2** (Roadmap_v2 Mo 5 Wk 1-4 — battle scene, turn state machine, and timed
  input, all built, playtested, and iterated on user feedback). Next: Step 3 — damage formula +
  Primal type chart (Mo 6 Wk 1-2), replacing `BattleConfig.PlaceholderAttackDamage` with the real
  `(AttackerStat / DefenderStat) x skillPower x primalTypeMultiplier` formula.

[2026-08-04] Repo audit — AUD-006: camera lookahead, closing out the audit
- **Context:** Last open item from `AUDIT_202608.md`. The audit's suggested fix
  (`m_LookaheadTime` on a Cinemachine Composer) is a Cinemachine 2.x API this project's
  Cinemachine 3.x `CinemachineFollow` doesn't have.
- **Built:** `Assets/Scripts/Player/CameraFollow.cs` — reads a target Rigidbody2D's velocity,
  eases a `Vector2.SmoothDamp`'d offset in the movement direction (scaled 0→1 by speed up to a
  max at the player's Sprint top speed, 8 u/s). New `CameraLookaheadTarget` GameObject in
  `SampleScene.unity` carries this script; `CinemachineCamera.Follow`/`LookAt` (previously the
  player directly) now point at this proxy instead.
- **Verified live:** offset eases to zero at rest; a sustained sprint-speed treadmill test (see
  below) drove the offset to its configured max in the direction of travel, and a Game View
  screenshot confirms the camera visibly leads ahead of the player on screen.
- **Testing methodology note:** this session's MCP tool round-trip latency measured at ~5s+ per
  call (via `Time.realtimeSinceStartup`), making naive "set input → sleep N → check" playtesting
  unreliable for anything faster than that — the player would already be stopped against a wall
  by query time regardless of requested sleep duration. Worked around it with an
  `EditorApplication.update` hook running a "treadmill" (recenters the player before it reaches a
  wall, continuously re-asserts velocity) so a query landing at an unpredictable later real time
  still reliably catches genuine sustained motion. Worth reusing this pattern for any future
  Play-mode velocity/timing playtest in this environment.
- **This closes every finding from `AUDIT_202608.md`** — see `KNOWN_ISSUES.md`'s Closed Issues
  section for the full AUD-001 through AUD-012 trail.
- **Date:** 2026-08-04

[2026-08-04] Repo audit — AUD-005: overworld Sprint + wild creature patrol/detection
- **Design decision (confirmed with user before implementing):** simple vision-cone/detection-
  radius system, not formal overworld "lanes" — Combat_Directive Part 3's lane-carry-over section
  is a thematic bridge to the battle stage's 7-lane system, not a technical spec; nothing else in
  the design docs references overworld lanes. That section stays unresolved/not-implemented by
  design, not by omission.
- **Built:** Sprint (hold-while-held 1.6x multiplier) on `PlayerTopDownController.cs`, using the
  existing unused "Sprint" InputSystem action. Patrol/Alert state machine on
  `WildEncounterCreature.cs` (Kinematic Rigidbody2D + MovePosition, throttled detection check —
  radius + facing cone + line-of-sight raycast). Temporary `AlertIndicator` child (tinted circle)
  toggles on Alert — added mid-session per user request, since nothing signaled detection to the
  player otherwise. New `Assets/Sprites/AlertIcon.png`.
- **Bug found via live playtest, not caught blind:** wired patrol/chase, but a human watching the
  Editor reported the creature reaches the player and "goes underneath" without ever triggering
  the encounter prompt. Root cause: this same session's AUD-002 foot-anchoring fix moved
  `Phasix_WildEncounter`'s trigger collider down near its own pivot, but the player's collider
  sits ~1.4 world units *above* its pivot (torso-only, by design) — the two colliders stopped
  sharing any vertical band. AUD-002's rationale never actually applied to this prefab (a pure
  `isTrigger` volume, never a solid movement collider), so fixed by resizing the trigger back up
  (`offset: {0,2}, radius: 1.2` local) to reliably overlap the player's real collision band
  instead of matching the creature's small placeholder sprite. Re-verified live.
- **Verified live (Play mode, via `execute_code` driving position/input — no real input device in
  this session):** patrol advances over real time; a controlled single-axis-blocked corner-graze
  test showed correct Alert transition and straight-line chase; contact now reliably shows the
  Flee/Engage prompt; Flee resolves the full cycle; Sprint moved the player at the expected speed
  and stopped correctly at a wall.
- **Known limitations:** Alert-state chase has no obstacle avoidance (Kinematic bodies aren't
  physically stopped by collisions) — acceptable since chase speed stays below the player's own,
  but would look wrong with more interior geometry between creature and player. Patrol is a fixed
  local-X back-and-forth, not a waypoint list.
- **Date:** 2026-08-04

[2026-08-04] Repo audit — live Editor verification pass (AUD-001/004/007/012)
- **Context:** Continuation of the prior no-Editor audit triage session, now with `unity-mcp`
  attached (`instance_count: 1`, Unity 6000.3.11f1). Worked through `KNOWN_ISSUES.md` →
  "Pending Editor Verification" before starting Tier C.
- **Found and fixed (not just verified):**
  - First compile after checking out the branch failed: `Phasix.Tests.EditMode.asmdef` referenced
    `"Assembly-CSharp"` by name, which doesn't work for Unity's implicit default assembly. Added
    `Assets/Scripts/Phasix.Runtime.asmdef` (all runtime scripts, references
    `AstarPathfindingProject` + `Unity.InputSystem`) and `Assets/Scripts/Editor/Phasix.Editor.asmdef`
    (Editor-only, references `Unity.2D.Sprite.Editor`), then pointed the test asmdef at
    `Phasix.Runtime`. See `KNOWN_ISSUES.md` → closed `[AUD-012 asmdef]`.
  - AUD-007's corner correction was actually broken, not just uncalibrated: live playtest at a
    real wall corner showed the player getting stuck instead of nudged through, because
    `ComputeCornerCorrection` cast its rays from the transform pivot while the player's
    `CapsuleCollider2D` sits ~0.67 world units above it. Fixed by casting from the collider's
    bounds edge instead. Re-verified live against both a true dead-end (correctly no-ops) and an
    isolated test obstacle (correctly nudges the player through). See `KNOWN_ISSUES.md` → closed
    `[AUD-007]`.
- **Verified correct, no change needed:** AUD-001 (screenshotted two overlapping test sprites in
  `SampleScene` — lower-Y sprite draws in front, confirming `CustomAxis (0,1,0)` is live), AUD-004
  (player GameObject resolves `PlayerTopDownController` cleanly, no missing-script warning),
  AUD-012 (all 7 `BondSystemTests` pass under Test Runner, once the asmdef fix above let them
  compile).
- **Process note:** teleporting the player around the scene via `execute_code` during testing
  crossed the `Test_WildSpawnPoint*` trigger zones and spawned real `WildEncounterCreature`
  instances, which called `FreezeMovement()` on contact and silently stalled later test
  measurements until diagnosed. Corner-correction tests after that point disabled the
  `EncounterTrigger` GameObjects for the duration of the test and re-enabled them after.
- **Date:** 2026-08-04

[2026-08-04] Repo audit triage — fixed everything that doesn't need a live Unity Editor
- **Context:** Received an external repo audit (`AUDIT_202608.md`, 12 findings AUD-001–AUD-012,
  reviewed against commit `c07b6cc`). No Unity Editor was attached this session
  (`mcpforunity://instances` confirmed `instance_count: 0`), so findings were triaged into what's
  safe to fix via direct file edits vs. what needs Editor/MCP visual verification or scene/prefab
  surgery. Two Explore passes verified every audit claim against the actual repo before acting on
  it — two corrections came out of that: AUD-006's suggested fix assumes a Cinemachine 2.x API
  (`m_LookaheadTime`) that doesn't exist on this project's Cinemachine 3.x `CinemachineFollow`;
  and AUD-004's "wrong header path" bug is in `PlayerController_SideScroll.cs`, not the reverse.
- **Built:** `Assets/Tests/EditMode/Phasix.Tests.EditMode.asmdef` + `BondSystemTests.cs` — first
  test assembly in the project, 7 tests covering `BondSystem.cs`'s floor logic, session loss cap,
  60%/80% damping, 100% immunity, milestone-driven floor raises, and gain clamping (AUD-012).
- **Fixed:**
  - AUD-008: `Combat_Directive_v0_1_0.md` Part 3 said "5-lane logic"; Part 2's 7-lane system is
    canonical. Corrected + added an errata note (no version bump — filename is version-pinned).
  - AUD-009: marked the 9 not-yet-built directories in `CLAUDE.md`'s Folder Structure with
    `← Phase 3, not yet created` so a cold reader doesn't assume they exist.
  - AUD-011: wrote a real `README.md` (was 8 bytes, `# Phasix` only, on a public repo).
  - AUD-001: `Renderer2D.asset` and `GraphicsSettings.asset` disagreed with each other on
    `m_TransparencySortAxis` and both had `m_TransparencySortMode: 0` (Default, ignores the axis
    entirely). Set both to `CustomAxis` (3) with axis `(0,1,0)`.
  - AUD-004: deleted the orphaned 8-directional `PlayerController.cs` (not referenced anywhere in
    `SampleScene.unity`); renamed the live 4-directional `PlayerController_SideScroll.cs` →
    `PlayerTopDownController.cs` via `git mv` (preserves the script GUID / scene reference);
    fixed its mis-pathed header comment; updated `WildEncounterCreature.cs` (4 refs) and
    `CompanionAI.cs` (3 comment refs). See `DECISIONS.md`.
  - AUD-007: added a raycast-gated corner-correction nudge to `PlayerTopDownController.cs`
    (`ComputeCornerCorrection`) — only fires when exactly one of two perpendicular directions is
    blocked within a placeholder threshold (a real corner clip, not a flat wall or a dead-end).
    Threshold + wall mask logged in `NumericalCalibration.md` → new "Overworld Movement" section.
- **Blocked (needs a live Unity Editor/MCP session — logged as open in `KNOWN_ISSUES.md`):**
  AUD-002 (collider re-anchoring), AUD-003 (ground shadows), AUD-005 (overworld dash + wild
  creature patrol/detection — audit's own top-priority item), AUD-006 (camera lookahead, and
  needs a different technical approach than the audit describes).
- **Verify once Editor/MCP is available:** AUD-001's visual sort-order result; AUD-004's scene
  opens with no "missing script" warning on the player object; AUD-007's corner-correction
  mechanism (not just its threshold value) actually behaves as intended; AUD-012's tests
  actually pass under Test Runner (hand-traced against `BondSystem.cs` source, not yet executed).
- **Not actioned this pass:** AUD-010 (`Assets/_Recovery/0.unity` — real autosave scene, still
  git-tracked despite already being gitignored) — holding for explicit user confirmation before
  untracking, since it looks like real recovered editor work rather than junk.
- **Date:** 2026-08-04

[2026-08-04] Scene dressing — two more wild spawn points, pastel pink + vibrant purple
- **User request:** two more spawn points, different species, one top-left corner and one
  bottom-right corner of the room; then asked for pastel pink and vibrant purple coloring.
- **Built:** `PhasixPlaceholderVisual.SetColorOverride(Color)` and `EncounterTrigger`'s new
  `_overrideTintColor`/`_tintColorOverride` fields — a debug-only tint override that bypasses
  `PrimalTypeColor` for the in-world sprite only, since pastel pink/vibrant purple aren't part
  of the locked Primal palette. See `DECISIONS.md` → `[Creatures] Debug-only sprite tint
  override...` for the full rationale and the one deliberate limitation (the encounter prompt
  UI swatch is unaffected and still shows the species' real PrimalType color).
- **Scene:** `Test_WildSpawnPoint_TopLeft` at `(-10, 6, 0)` — `Test_FireType` only, tint
  overridden to pastel pink `(1, 0.435, 0.690)`. `Test_WildSpawnPoint_BottomRight` at
  `(10, -6, 0)` — `Test_SteamType` only, tint overridden to vibrant purple
  `(0.651, 0.302, 1.0)`.
- **Verified:** Play-mode check via Unity MCP — both spawned at the correct corner positions
  with the exact overridden colors on their Body sprites.
- **Date:** 2026-08-04

[2026-08-04] Investigation — AstarPath/missing-script Play errors were a false alarm, not a real bug
- **Context:** While Play-testing the Wild Encounter feature (below), entering Play mode
  logged three "referenced script is missing" errors plus `AstarPath`
  `NullReferenceException: No AstarPath object found in the scene` and `There are no graphs
  in the scene` — looked like a pre-existing gap where the companion's `Seeker`/`AIPath`
  had no `AstarPath` graph to path against, so it got logged in `KNOWN_ISSUES.md` and flagged
  for follow-up.
- **Investigated:** Checked `CHANGELOG.md`'s own 2026-07-31 entry first, which documented a
  fully-built `AstarPath` + `GridGraph` (60×38 nodes, `Obstacles` layer, `scanOnStartup =
  true`) — confirmed still present and correctly configured directly in
  `SampleScene.unity`'s serialized `AstarPath` MonoBehaviour (cached graph blob intact).
  Re-entered Play mode cleanly (no early state-querying this time) and confirmed
  `AstarPath.active.data.graphs` returns `graphCount=1, graph0 type=GridGraph`, and the
  companion's `AIPath` (`canMove=true`, `reachedDestination=true`) functions normally. The
  missing-script errors also did not recur.
- **Root cause:** A timing artifact, not a real config gap — querying live state
  (`AstarPath.active`, `EncounterPromptController.Instance`, etc.) immediately after
  `manage_editor(action="play")` reliably returns null/default for the first several real
  seconds while Unity's post-Play initialization settles
  (`mcpforunity://editor/state`'s `play_mode.is_changing` stays `true` that whole time). The
  first Wild Encounter Play-test session hit this exact same class of transient null with
  `EncounterPromptController.Instance` (see below) and worked around it correctly there by
  waiting; the AstarPath check that session was made too early and got misread as a real bug.
- **Documented:** Added a correction/addendum to `LESSONS_LEARNED.md` →
  `[Tooling] Play Mode doesn't tick frames...` covering this specific "state looks null
  immediately after Play, but resolves correctly if you wait — don't manually force-run
  `Awake()` via reflection as a workaround" pattern. Closed the `KNOWN_ISSUES.md` entry as
  `[AST-001]` (misdiagnosis, not fixed because nothing was broken).
- **Changed:** None — no code or scene changes were needed.
- **Date:** 2026-08-04

[2026-08-04] Feature — Wild Encounter Trigger + Primal Type Reveal (Phase 2, Wk 14-16 scaffold)
- **Built:** `EncounterTrigger.cs` (spawn-point marker, `World/`), `WildEncounterCreature.cs`
  (contact detection + Flee/Engage resolution, `Creatures/`), `WildSpawnSystem.cs` (static
  `PhasixRuntimeData` builder, `Creatures/`), `EncounterPromptController.cs` (first script in
  `Scripts/UI/`, UI Toolkit singleton wrapping a `UIDocument`). New assets:
  `Assets/UI/EncounterPrompt.uxml`/`.uss`/`EncounterPromptPanelSettings.asset` (the project's
  first UI Toolkit screen — new `Assets/UI/` folder, added to CLAUDE.md's Folder Structure),
  `Assets/Prefabs/Creatures/Phasix_WildEncounter.prefab` (stationary variant of
  `Phasix_Placeholder.prefab` — no `Rigidbody2D`/`Seeker`/`AIPath`/`CompanionAI`). New
  `EventBus.cs` section: `OnWildEncounterTriggered`/`OnWildEncounterFled`/
  `OnWildEncounterEngageRequested`. Scene: `UIRoot_EncounterPrompt` and `Test_WildSpawnPoint`
  added to `SampleScene.unity`.
- **Decided:** Contact-based trigger model (a visible wild Phasix you walk into, matching
  `Combat_Directive_v0_1_0.md`) instead of the Roadmap's own simpler "invisible zone collider"
  wording — see `DECISIONS.md` → `[Encounter] Contact-based trigger model` for the full
  rationale, plus five more entries in that same session block covering the UI Toolkit choice,
  chunk-tied repopulation, no-pooling justification, `origin = OriginType.Wild`, and the
  confirmed Engage-resolves-like-Flee stub behavior.
- **Known scaffold limitation (intentional, not a bug):** Engage has no `BattleManager`/
  `BattleScene_Main` to hand off to yet (Phase 3) — it logs a `// TODO` line, fires
  `OnWildEncounterEngageRequested` (zero subscribers today), then resolves identically to
  Flee (despawn, hide prompt, unfreeze player). This was confirmed as the intended behavior,
  not a placeholder oversight.
- **Verified:** Full Play-mode pass via Unity MCP — wild creature spawns visibly tinted before
  contact (confirmed the exact Fire+Water Steam-type color blend + underglow lighten math);
  contact freezes the player (`FreezeMovement()`'s first real exercise anywhere in the
  project — previously had zero callers) and shows the prompt with correct species/type
  labels and a matching color swatch; Flee destroys the creature and resumes player input
  (`UnfreezeMovement()`'s first real exercise); toggling the spawn point inactive/active
  confirms fresh repopulation via `OnEnable()` with newly-rolled personality/species; Engage
  logs the TODO line, fires its event exactly once, and resolves like Flee; a companion
  standing on the creature (player elsewhere) does *not* trigger it, confirming the
  `PlayerController_SideScroll` component filter; a deliberately duplicated second spawn
  point confirmed the `EncounterPromptController.IsVisible` clobber guard silently blocks a
  second simultaneous `Show()` call with no error.
- **Also:** added a documentation-only backlog row to `Roadmap_v2.md` → `What Is Not In This
  Roadmap` for map expansion (using only the existing `Assets/Tiles` tile assets, chunked from
  the start via `WorldChunkManager`) — unrelated to this feature, requested alongside it.
- **Blocked:** none — feature complete and verified end-to-end.
- **Next:** Phase 3, Mo 5 Wk 1-2 — Battle scene + turn state machine (`BattleScene_Main`,
  additive load), which is what Engage is stubbed to eventually hand off to.
- **Date:** 2026-08-04

[2026-08-04] Process — self-enforcing convention: every AIPath-bypassing pattern must have a gizmo
- **User request:** wanted a standing rule so any future companion movement pattern built
  outside standard AIPath-following always gets a gizmo, given how many rounds it took to get
  Orbit/HiddenShadow/Blink's gizmos actually working correctly.
- **Built:** `OnDrawGizmos()`'s `switch` now has a `default` case that logs a
  `Debug.LogWarning` if the active pattern has `aiPath.canMove == false` (bypasses AIPath) but
  didn't match any of the existing gizmo cases — a runtime safeguard, not just documentation,
  so a missing gizmo surfaces the moment the new pattern is tested in the Editor.
- **Documented:** the convention itself on `CompanionMovementPatternType`'s doc comment and at
  the `_aiPath.canMove` exclusion list in `ApplyMovementPreset` (both point to each other and
  to the warning), plus a full decision entry in `DECISIONS.md` → `[Creatures] Convention —
  every AIPath-bypassing companion movement pattern must have a gizmo`.
- **Verified:** simulated the gap via reflection (forced `canMove = false` with an unhandled
  pattern, manually invoked `OnDrawGizmos()`) and confirmed the warning fires with a clear,
  actionable message.
- **Date:** 2026-08-04

[2026-08-04] Tuning — Blink cycles faster
- **User feedback:** wanted the Blink pattern to teleport more frequently; initial pass to
  `0.4f/0.8f` was then dialed back slightly to `0.6f/1.2f`.
- **Changed:** `BlinkIntervalMin`/`BlinkInterval` (`DebugMovementPresetCycler.cs`'s Blink
  preset, and `CompanionAI`'s matching private-field defaults) `1.5f/3f` → `0.6f/1.2f`. Left
  `BlinkVanishDuration` (0.12s) and flash timing untouched — still enough visible window
  (~0.48-1.08s) between blinks to read the vanish/reappear beat clearly at this faster cadence.
- **Date:** 2026-08-04

[2026-08-04] Cleanup + Fix — removed leftover TestOrbit* scene objects; Blink's 2nd preview spot is now a real, visited commitment
- **User feedback:** asked about two orphaned `TestOrbitTarget2`/`TestOrbitCompanion2`
  GameObjects sitting in the scene; also reported the Blink pattern's 2nd preview reticle
  "never gets moved to."
- **Removed:** `TestOrbitTarget2` (bare Transform at origin) and `TestOrbitCompanion2`
  (standalone `CompanionAI`/`AIPath`/`Seeker` rig, no visual, `_target` pointing at the other
  object) — traced via `git log -S` to commit `bc112d7`, the original `CompanionAI`/Orbit
  build, predating `DebugMovementPresetCycler`. Not referenced by `PartySystem` or any script;
  fully superseded by testing patterns live on the real companion via the cycler. Deleted and
  scene saved.
- **Root cause (Blink preview never visited):** by design — `_blinkPreviewDestination` was an
  independent random sample, re-rolled every cycle and never fed into the actual
  `MovePosition` call. Confirmed this was working exactly as (mis-)designed, not a bug in the
  literal sense, but a bad design: a marker that's shown as "upcoming" but never comes true is
  actively misleading for a debug visualization.
- **Fix:** converted to a real 2-deep destination queue. `_blinkNextDestination` and
  `_blinkPreviewDestination` are both seeded together the moment Blink becomes active
  (`ApplyMovementPreset`). Each time a teleport actually happens, the queue is promoted
  (`_blinkNextDestination = _blinkPreviewDestination`) and the back is refilled with a fresh
  sample — both slots are real, committed future stops at all times, never decorative.
  `DrawBlinkGizmos()` no longer gates on `_blinkPhase == Vanished` — both are valid all the
  time now, so it draws continuously while Blink is active (solid = imminent, faint = after
  that).
- **Verified** via `Time.timeScale = 0` + manual reflection-invoked `FixedUpdate()` stepping
  (no natural ticking to race against): confirmed the promoted `_blinkNextDestination` exactly
  equals the pre-teleport `_blinkPreviewDestination`, and the refilled `_blinkPreviewDestination`
  is a fresh third value. Caught and corrected a false negative in this same test pass — see
  `LESSONS_LEARNED.md` → `[Tooling]` addendum on `Rigidbody2D.MovePosition` deferring its
  effect to the next physics step.
- **Date:** 2026-08-04

[2026-08-04] Fix + Feature — leftover Seeker path-line gizmo cleared on pattern switch; Blink now previews 2 upcoming spots
- **User feedback:** old paths still visibly showing when swapping to Orbit/HiddenShadow/Blink;
  requested Blink show the next TWO future blink locations, not just one.
- **Root cause (leftover paths):** a third always-on gizmo, separate from both AIBase's
  destination-circle (already fixed) and our own pattern gizmos — `Seeker.OnDrawGizmos()`
  (`Pathfinding/Core/AI/Seeker.cs`) draws a solid green line along
  `lastCompletedVectorPath`, gated only by its own public `drawGizmos` bool. Resetting
  `aiPath.destination` does nothing to this — it isn't tied to destination, it's whatever path
  was last actually calculated, and Orbit/HiddenShadow/Blink never calculate a new one to
  replace it, so the last real path from before the switch just sat there.
- **Fix:** `ApplyMovementPreset()` now also sets `_seeker.drawGizmos = _aiPath.canMove` —
  suppressed for the three patterns that never path, restored for the ones that do. Verified
  via reflection: forced a real path via `Seeker.StartPath` on Direct, confirmed
  `lastCompletedVectorPath` had entries and `drawGizmos == true`, switched to Orbit, confirmed
  `drawGizmos` flipped to `false` (the cached path list itself is untouched — `drawGizmos` is
  what actually gates whether `Seeker.OnDrawGizmos` renders it at all).
- **Built (2nd Blink preview):** `MoveAlongBlink()` now samples a second, independent candidate
  (`_blinkPreviewDestination`) at the same moment it commits `_blinkNextDestination` — both
  around the player's position at that instant. Only `_blinkNextDestination` is ever used for
  the actual teleport; the preview is sampled fresh again next cycle and re-syncs naturally,
  since the real second blink will resample around wherever the player actually is by then.
  `DrawBlinkGizmos()` now draws both: the real target at full opacity/size, the preview at
  reduced opacity/size, so the two are visually distinguishable — solid = happening, faint =
  "roughly here next." Verified via Scene View screenshot with `Time.timeScale = 0`: both
  reticles render, faint one further out, no leftover green path line anywhere in frame.
- **Date:** 2026-08-04

[2026-08-04] Fix — Blink's target reticle was the same color as AIPath's own always-on gizmo
- **User feedback:** still looked "wrong" after the OnDrawGizmos + destination-reset fix;
  reported the position/order looked inverted. Confirmed via rigorous testing (frozen
  `Time.timeScale = 0`, temporary bright marker objects dropped at the exact
  `_blinkNextDestination` and companion-current-position world coordinates, screenshotted with
  a manually wide/clean `SceneView` framing instead of relying on `view_target` auto-zoom) that
  the reticle's actual POSITION was already correct — it was pixel-exact on the marker placed
  at `_blinkNextDestination`.
- **Root cause:** `DrawBlinkGizmos()`'s reticle color, `(1, 0.85, 0.2)`, is nearly identical to
  A* Pathfinding Project's own `AIBase.ShapeGizmoColor` (`(0.94, 0.84, 0.12)`) — a yellow-gold
  circle AIBase draws unconditionally at the companion's CURRENT position, for every pattern,
  always. With both gizmos being simple yellow-gold circles and thin crosshair lines easy to
  miss at normal zoom, there were effectively two same-colored markers on screen at once — one
  at the old position (AIBase's, permanent) and one at the new (this one) — trivially misread
  as "showing the wrong position" or "the order is inverted."
- **Fix:** changed the Blink reticle to bright magenta `(1, 0.1, 0.9)` — unmistakably distinct
  from AIBase's yellow-gold at any zoom level. Orbit's blue and HiddenShadow's lavender were
  already distinct and didn't need changing.
- **Date:** 2026-08-04

[2026-08-04] Fix — pattern gizmos were never actually rendering; wrong callback + stale AIPath destination gizmo mistaken for "still broken"
- **User feedback:** "still not working" after the repaint fix — cycling presets via
  `DebugMovementPresetCycler` (Default → ... → Blink) showed no correct paths for any of the
  3 patterns, still looking like "the following pathing gizmo."
- **Root cause (two compounding issues):**
  1. Our gizmos used `OnDrawGizmosSelected` — requires the companion to be manually selected
     in the Hierarchy. Normal testing (cycling presets with Tab, just watching the game) never
     selects it, so our gizmos silently never rendered at all — not a repaint problem, they
     just never ran in the first place.
  2. What the user WAS actually seeing is AIPath's own base class gizmo
     (`AstarPathfindingProject/Core/AI/AIBase.cs` → `OnDrawGizmos()`, unconditional, no
     selection needed) — it draws a blue circle at `aiPath.destination` whenever that isn't its
     positive-infinity "unset" sentinel. Orbit/HiddenShadow/Blink never write to `destination`
     (they bypass AIPath entirely), so it just sat frozen at whatever the last Direct/Wavy/
     DashThrough/StopAndGo pattern left it — reading exactly like "the following pathing gizmo
     still showing," which is precisely how the user described it.
- **Fix:** switched our gizmo methods from `OnDrawGizmosSelected` to `OnDrawGizmos` (always-on,
  matching AIBase's own convention — safe here since PartySystem only ever has one companion
  instance). `ApplyMovementPreset()` now also resets `aiPath.destination` to
  `Vector3.positiveInfinity` whenever `canMove` is false (Orbit/HiddenShadow/Blink), which
  suppresses AIBase's own stale destination-circle gizmo entirely. Also renamed the
  repaint-driver method (`RequestSceneViewRepaintIfGizmoRelevant`) and dropped its Selection
  check, since the gizmo itself no longer requires selection.
- Verified end-to-end without ever touching `Selection` (confirmed via
  `Selection.activeGameObject` reading an unrelated object throughout the test) — both Orbit's
  circle and Blink's reticle rendered correctly, and `aiPath.destination` read back as
  `(Infinity, Infinity, Infinity)` for both, confirming the stale marker is gone.
- **Date:** 2026-08-04

[2026-08-04] Fix — pattern gizmos still frozen; RepaintAll() from inside the gizmo draw itself doesn't chain into continuous repaints
- **User feedback:** sent a screen recording (~8s) showing the Blink reticle sitting at its
  very first drawn position the entire clip, while the companion visibly blinked to several
  different spots in the Game View over that time. Reported all 3 pattern gizmos "still don't
  look right."
- **Root cause:** the previous fix called `SceneView.RepaintAll()` from inside
  `OnDrawGizmosSelected()` itself, assuming that would chain into continuous repaints. It
  doesn't — `OnDrawGizmosSelected` only re-runs as a RESULT of a repaint that already happened
  through some other trigger; requesting one from within it is too weak/circular, and (likely
  worsened by the Scene tab not being focused during Play testing) repaints happened rarely,
  leaving the gizmo showing genuinely stale data for long stretches. A single still screenshot
  from the previous verification pass couldn't have caught this — it only exposes staleness
  across time, which is exactly why the user's video was the tool that actually found it.
- **Fix:** drive the repaint from `EditorApplication.update` instead (subscribed in
  `OnEnable`/unsubscribed in `OnDisable`, `#if UNITY_EDITOR`-guarded) — a genuine per-Editor-tick
  callback, independent of whether a repaint happened to occur elsewhere. Only requests a
  repaint while this companion is actually selected and the active pattern has a gizmo to draw
  (Orbit/HiddenShadow/Blink), so it's not forcing repaints for nothing while other patterns are
  active. Verified this time via a *sequence* of Scene View screenshots taken seconds apart
  (not a single shot) — confirmed the Blink reticle's position visibly changes between
  consecutive captures, proving live updates rather than a frozen snapshot.
- **Date:** 2026-08-04

[2026-08-03] Redesign — pattern gizmos simplified to purpose-built indicators per pattern
- **User feedback:** the generic circle/sphere gizmos still weren't reading clearly — asked for
  each pattern's gizmo to visualize exactly one thing specific to that pattern, not a bundle of
  markers: Orbit → just the path the circle follows as the player moves; HiddenShadow → the
  idle/sway target, but ONLY while actually swaying (nothing while Locked to the player's feet
  — no separate "path" exists in that state); Blink → the next teleport target, decided and
  shown *before* the move happens, not just where it already landed.
- **Changed:** `DrawOrbitGizmos()` now draws only the wire circle (dropped the extra
  ideal-angle-point marker). `DrawHiddenShadowGizmos()` early-returns unless `_shadowPhase ==
  Emerged`. `DrawBlinkGizmos()` early-returns unless `_blinkPhase == Vanished`, and draws a
  reticle (circle + crosshair) at a new `_blinkNextDestination` field instead of the old
  min/max radius circles.
- **Built:** `MoveAlongBlink()` now calls `PickBlinkDestination()` once, the moment Vanished
  begins (caching the result in `_blinkNextDestination`), instead of at the end of the vanish
  window — the actual teleport at the end just reuses that cached point. This both gives the
  gizmo a committed target to show ahead of the move, and guarantees the gizmo and the actual
  landing spot can never disagree (no second random roll).
- Verified via Scene View screenshots during Play mode: Orbit's circle renders cleanly centered
  on the player with no extra clutter; Blink's reticle renders at a point distinct from the
  player, matching the pre-committed `_blinkNextDestination`. HiddenShadow's Emerged-only gate
  confirmed via live reflection read of `_shadowPhase` (screenshot was inconclusive since the
  gizmo necessarily overlaps the companion's own sprite at the idle anchor).
- **Date:** 2026-08-03

[2026-08-03] Fix — Blink still slid after the first fix; disabled Rigidbody2D interpolation for Blink
- **User feedback (2nd pass):** even after hiding the sprite during the vanish window, the
  companion still visibly "dashed"/eased between the old and new spot on reappearing.
- **Root cause:** hiding the sprite doesn't actually prevent the slide — `SetVisible(true)`
  and `Rigidbody2D.MovePosition` both happen in the same FixedUpdate tick, but Unity's
  Rigidbody2D `Interpolate` mode (enabled on the companion prefab, used for normal-movement
  smoothness) blends the RENDERED transform between last tick's recorded position and this
  tick's over the next render frame(s), regardless of when the sprite became visible. This is
  a well-known Rigidbody2D-interpolation teleport gotcha, not something the earlier
  vanish-window fix could address on its own — `transform.position` reads the correct
  instantaneous value the whole time (which is why the earlier position-polling verification
  passed even though the rendered slide was still there); interpolation only affects the
  hidden internal render transform Unity actually draws to screen.
- **Fix:** `ApplyMovementPreset()` now sets `_rigidbody2D.interpolation =
  RigidbodyInterpolation2D.None` whenever `Blink` is the active pattern, and restores
  `.Interpolate` for every other pattern (which do rely on it for smooth motion). Blink's
  motion is never meant to be smooth — it's either fully hidden or instantly correct — so
  removing interpolation entirely for the duration Blink is active is the correct fix, not a
  timing workaround. Verified with actual Game View screenshots across a blink cycle: companion
  visible → fully hidden (no ghost/streak) → visible again at a new position, zero visible
  travel between the two.

[2026-08-03] Fix — Blink readability + pattern gizmos not tracking live in Play mode
- **User feedback:** Blink looked like a sped-up version of the DashThrough ("Eager Runner")
  dash rather than a teleport; Orbit/HiddenShadow/Blink's new Scene-view gizmos appeared stuck
  at a fixed, wrong point instead of tracking the player.
- **Root cause (Blink look):** `Phasix_Placeholder.prefab`'s `Rigidbody2D` has `Interpolate`
  enabled — Unity smooths any `Rigidbody2D.MovePosition` jump across the following render
  frames for normal-movement smoothness, so an instant teleport still visibly slid across the
  gap instead of popping.
- **Fix (Blink look):** Blink now fully hides the companion (`PhasixPlaceholderVisual.
  SetVisible`, new — toggles `SpriteRenderer.enabled` on Body/Underglow) for a new
  `BlinkVanishDuration` (default 0.12s — long enough for the Rigidbody2D's interpolation to
  settle before anything renders again) before performing the actual teleport, then shows it
  again with the existing pop-scale flash. New `BlinkPhase { Visible, Vanished }` runtime state
  in `CompanionAI.cs`, same convention as `ShadowPhase`. Verified live in Play mode: forced a
  long (2.5s) vanish window and confirmed both renderers report `enabled=false` for the full
  duration, then `true` again at a new position.
  **Note: this fix alone turned out to be insufficient — see the follow-up entry above.**
- **Root cause (gizmo tracking):** not a math bug — the gizmo code already read the same
  `_target.position` + offset fields the real movement methods use correctly. The Scene View
  simply doesn't repaint every frame during Play Mode by default (only the Game View does), so
  whatever `OnDrawGizmosSelected` drew at the last repaint stayed on screen inert.
- **Fix (gizmo tracking):** `OnDrawGizmosSelected` now calls `UnityEditor.SceneView.
  RepaintAll()` (guarded by `#if UNITY_EDITOR`, required since `CompanionAI` ships in builds)
  after drawing, so the gizmo keeps re-triggering its own repaints continuously while the
  object stays selected — no manual "Always Refresh" toolbar toggle needed. Verified via a
  direct Scene View screenshot (`manage_camera`, `capture_source: scene_view`) during Play mode
  with Orbit active: the wire circle is clearly centered on the player, not floating at an
  unrelated point.
- **Date:** 2026-08-03

[2026-08-03] Creatures — Blink companion movement pattern + pattern gizmos
- **Built:** `CompanionMovementPatternType.Blink` — a 7th companion follow style. Periodically
  (randomized between `BlinkIntervalMin`/`BlinkInterval`) teleports the companion via
  `Rigidbody2D.MovePosition` (an instant snap, not a lerp, is what reads as a blink rather than
  a dash) to a random point in the `[BlinkMinRadius, BlinkRadius]` annulus around the player,
  then waits before blinking again. Plays a brief pop-scale flash on arrival
  (`PhasixPlaceholderVisual.SetBlinkFlashScale`) so the teleport reads as an event.
- **Built:** `PickBlinkDestination()` validates every sampled point against the A* GridGraph
  via `AstarPath.active.GetNearest` (rejects unwalkable nodes and points that snapped too far
  from the sample, meaning it likely landed inside/beyond a wall), retrying up to 8 times before
  falling back to the player's own position — a raw random point has no walkability guarantee,
  unlike Orbit/HiddenShadow which always stay glued to the (necessarily walkable) player.
- **Built:** `CompanionAI.OnDrawGizmosSelected()` — Orbit, HiddenShadow, and Blink all bypass
  `AIPath.destination` entirely, so Seeker's built-in path gizmo has nothing to draw for any of
  them. Added a stand-in Scene-view gizmo per pattern (orbit circle, HiddenShadow locked/idle
  markers + sway range, Blink's min/max radius annulus), toggleable via a new
  `_showPatternGizmos` field. Not Blink-specific — fixes the same gap for all three
  non-pathfinding patterns while adding Blink.
- **Added:** "Blink" entry to `DebugMovementPresetCycler.cs` for live comparison (Tab in Play mode).
- **Decided:** not wired to Personality/species yet — matches how Orbit and HiddenShadow
  shipped. See `DECISIONS.md` → `[Creatures] Blink pattern`.
- **Date:** 2026-08-03

[2026-08-03] Tuning/Fix — Hidden Shadow sway amount + companion render order
- **User feedback:** wanted more left-right movement from the idle sway, and wanted the
  companion to render behind the player instead of in front.
- **Sway:** `ShadowSwayAmplitude` in the "Hidden Shadow" `DebugMovementPresetCycler` preset
  raised 0.2 → 0.5 (world units) — confirmed applied live via reflection read-back in Play mode.
- **Render order (general, not HiddenShadow-specific):** `Phasix_Placeholder.prefab`'s `Body`/
  `Underglow` `m_SortingOrder` were `1`/`0` — both above `Mr_chimken`'s `SortingGroup`
  (`sortingOrder: 0`), so the companion always drew in front of the player regardless of relative
  position. Checked the camera first (`Main Camera`'s `transparencySortMode` is `0`/Default, not
  Y-axis) — confirmed this is a plain static order issue, not something a Y-sort would already be
  handling. Changed to `-1`/`-2` (Underglow still behind Body, both now behind the player).
  Verified with a Play-mode screenshot forcing an overlap between the companion and the player's
  body: the player's sprite now visibly occludes the companion. See `DECISIONS.md` → `[Art]
  Placeholder Phasix visual` (Update, August 2026).
- **Date:** 2026-08-03

[2026-08-03] Tuning — Hidden Shadow's Locked position was offset from Mr_chimken's visible feet
- **User feedback (screenshot):** the squashed shadow sat a visible gap away from the character's
  feet instead of directly under them.
- **Root cause:** `MoveAlongHiddenShadow()`'s Locked-state position had no offset at all — it
  matched the player's raw Transform position exactly, which is not where the visible feet render
  (same pivot mismatch `OrbitCenterOffset` already compensates for on the Orbit pattern).
- **Fix:** added `ShadowLockedOffset` (`Vector2`) to `CompanionMovementPreset`, applied to both
  Locked-state position writes in `MoveAlongHiddenShadow()`. Also fixed a bug found in the same
  pass: the "still within debounce" branch was still using `_target.position` directly instead of
  the offset-adjusted position, so the offset had no effect until the debounce window elapsed.
- **Tuned empirically** against `Mr_chimken`'s actual rig, not guessed: took Play-mode screenshots
  at candidate offsets via `manage_camera` (Unity MCP), then measured the exact pixel gap between
  the visible foot and the shadow with a Python/PIL pixel-color scan (cross-checked independently
  against `Camera.main.WorldToScreenPoint`/`ScreenToWorldPoint`, which agreed within ~0.01 units).
  Landed on `ShadowLockedOffset = (0, 0.65)` — shadow now starts almost exactly where the foot
  sprite ends (~5px gap in a 4320px-tall capture, negligible).
- **Date:** 2026-08-03

[2026-08-03] Fix — Hidden Shadow's squashed shape appeared far from the player on return
- **User feedback:** the squashed shadow's position looked too far from the player.
- **Root cause:** `MoveAlongHiddenShadow()` called `ApplyShadowSquash(true)` the instant the
  player resumed moving — before the return-lerp (`ShadowReturnLerpDuration`) had actually closed
  the gap from the idle anchor. For that whole lerp window, a flat squashed shape was visible
  sitting apart from the player instead of the full companion visibly returning to them.
- **Fix:** squash is now applied only once `_shadowReturning` clears (the lerp has actually
  landed on the player), verified deterministically via reflection-driven ticks in Play mode —
  `bodyScaleY` stays 1 for every tick while `returning == true` and flips to the squashed value
  on the exact tick `returning` clears.
- **Date:** 2026-08-03

[2026-08-03] Creatures — Hidden Shadow companion movement pattern
- Built: `CompanionMovementPatternType.HiddenShadow` in `CompanionAI.cs` — bypasses AIPath
  entirely (same approach as `Orbit`), locking onto the player's position every physics tick
  while they move, then drifting to a swaying idle anchor (`ShadowIdleAnchorOffset`) after
  `ShadowStationaryDebounce` seconds stationary, and lerping back onto the player over
  `ShadowReturnLerpDuration` once movement resumes. Internal phase tracked via a private
  `ShadowPhase { Locked, Emerged }` enum, kept separate from `CompanionMovementState` (that one
  is distance-driven and Animator-facing; this one is player-velocity-driven).
- Built: `PhasixPlaceholderVisual.SetShadowSquash(float)` — flattens Body/Underglow scale.y
  relative to each renderer's own cached original scale, restoring exactly on `1f`. `CompanionAI`
  now holds an optional `PhasixPlaceholderVisual` reference (`GetComponent` in `Awake`) to drive
  this, same nullable convention as the existing `_animator` field.
- Built: new "Hidden Shadow" entry in `DebugMovementPresetCycler.cs` for live Tab-cycle
  comparison.
- Decided: two open questions (snap-vs-lerp on return, idle-anchor re-lock on player
  displacement) left undecided on purpose — see `DECISIONS.md` → `[Creatures] Hidden Shadow
  pattern — snap-vs-lerp and displacement re-lock`.
- **Date:** 2026-08-03

[2026-07-31] Tuning — reference companion resized to half scale
- `Phasix_Placeholder.prefab` root `localScale` 1→0.5. `CircleCollider2D`'s world-space
  bounds shrink automatically with transform scale (no manual radius change needed,
  confirmed via read-back: bounds went from 0.8×0.8 to 0.4×0.4). `AIPath.radius` (a plain
  data field the pathfinding math uses directly, NOT auto-scaled by Transform like a real
  Collider2D) manually halved 0.4→0.2 to stay proportional to the smaller visual/physical
  size.
- `DebugMovementPresetCycler`'s floating label scale doubled (0.075→0.15) to compensate —
  it's a child of the companion, so it would otherwise have shrunk along with the halved
  parent, undoing the size already agreed on earlier this session.
- Verified visually via Scene View screenshot after confirming (via a fresh Play session —
  the first check accidentally read a stale pre-edit instance still running from before)
  that the live companion's `transform.localScale` actually reads `(0.5, 0.5, 0.5)`.
- **Date:** 2026-07-31

[2026-07-31] Fix — DashThrough cut off mid-run, Orbit lagged once the player moved
- **User feedback (video):** Eager Runner's path visibly changed mid-transit before
  completing a dash. Orbit tracked correctly while the player stood still, but lagged
  behind once they started moving — user's own diagnosis: "maybe the orbit needs an
  additive so it moves in relation to the player velocity?" — confirmed correct.
- **DashThrough root cause:** re-targeting was gated purely on a fixed clock
  (`_dashIntervalMin`/`_dashInterval`, 0.35-0.9s) with no regard for whether the current
  dash had actually been reached — at short intervals this cut dashes off well before
  arrival. **Fix:** re-targets on `AIPath.reachedDestination` instead; the interval fields
  are now a safety-net maximum wait (in case a target becomes unreachable), widened to
  2-3s so they essentially never fire under normal conditions.
- **Orbit root cause:** purely reactive chasing of a moving target point always leaves
  residual lag once the target itself is moving, regardless of catch-up speed — the
  destination never stops moving, so a `MoveTowards`-style pursuit is structurally always
  a step behind. Compounded by running the position update in `Update()` (render-tick)
  while the player's own movement applies in `FixedUpdate()` (physics-tick) — a cadence
  mismatch. **Fix:** moved Orbit's movement to `FixedUpdate`, and added a feedforward term
  — the player's current `Rigidbody2D.linearVelocity` is applied directly to the
  companion's motion each physics step (zero-lag translation, matching how the player
  moves themselves), with `OrbitCatchUpSpeed` now only handling the small residual
  correction (the orbit's own rotational sweep, drift, pattern-switch settling) instead of
  the whole job.
- **Verified:** DashThrough — remaining distance to each dash target now shrinks steadily
  to near-zero before a new one is picked (2.393→1.991→0.684, then 5.316→...→0.039),
  instead of being cut off early. Orbit — with the player under **continuous** velocity
  (not stationary), distance from the orbit center held between 1.93-2.07 (radius 2)
  across every sample, essentially eliminating the lag visible in the reported video.
  Both verified on isolated throwaway test objects.
- **Process note:** `Physics2D.simulationMode` was correctly restored to `FixedUpdate`
  after this round of testing (see the earlier lesson learned about this) — confirmed via
  both a direct read-back and a `git diff` check on `ProjectSettings/Physics2DSettings.asset`
  showing no unintended change.
- **Date:** 2026-07-31

[2026-07-31] Tuning — Eager Runner erratic on both axes, Orbit tightened + center-corrected
- **User feedback:** Eager Runner needed to feel more erratic (speed + dash length, not just
  fast in one direction); Orbit needed to be faster, centered correctly (was orbiting the
  player's feet-pivot Transform, not their visible body — needed an upward shift), and
  needed near-zero lag ("almost non-existent" delay between the orbit and the player's
  current position) while still supporting a looser/trailing variant later if wanted.
- **DashThrough now randomizes two independent axes per cycle**, not just angle: added
  `DashIntervalMin`/`DashOvershootMin` alongside the existing values (now treated as maxes)
  — both the timing between dashes AND the overshoot distance are randomized each cycle.
  Eager Runner: `RunSpeed` 9→14, `WalkSpeed` 4→6, interval randomized 0.35-0.9s, overshoot
  randomized 1.5-5 units, angle spread widened to ±150°.
- **Orbit reworked to bypass AIPath's own movement entirely.** Root cause of the lag: AIPath's
  gradual acceleration/steering was the delay itself, not a tuning problem — no amount of
  speed/turn-speed tuning on AIPath's own steering could make it feel "tight." Fixed by
  disabling `AIPath.canMove` for this pattern and directly driving the Rigidbody2D via
  `Vector3.MoveTowards` at a new tunable `OrbitCatchUpSpeed` (world units/sec) each frame —
  high values (30-40+) read as near-instant tracking; low values (3-8) deliberately preserve
  the option for a future looser/trailing orbit variant, per the user's explicit ask to keep
  that door open.
- **Added `OrbitCenterOffset` (Vector2)** — orbits around `target.position + offset` instead
  of the raw Transform position, compensating for the player's feet-pivot rig (same root
  cause as the CapsuleCollider2D offset found earlier this session). Orbiting Moon preset:
  `OrbitAngularSpeed` 60→220 deg/sec, offset `(0, 1.4)`, `OrbitCatchUpSpeed` = 40.
- **Verified on isolated throwaway test objects** (not the live session): orbit distance
  from `target.position + offset` held at **exactly** 2.000 across every sampled frame while
  visibly sweeping through all four quadrants over one loop (confirms both the faster
  angular speed and the corrected center simultaneously); `AIPath.canMove` confirmed `false`
  while Orbit is active (confirms AIPath is fully bypassed, not fighting the manual
  positioning).
- **Date:** 2026-07-31

[2026-07-31] Feature — real Tier 2 movement patterns (not just parameter tuning)
- **User feedback, from live hands-on comparison:** Tier 1 presets (same trailing-point
  algorithm, different numbers) didn't read as genuinely different behavior — Close Shadow
  vs Eager Runner, and Wide Wanderer vs Steady Anchor, felt like the same thing at
  different speeds. This is exactly the Tier 1/Tier 2 distinction DECISIONS.md's outline
  already called out, now confirmed by actually trying it rather than guessed.
- Built: `CompanionMovementPatternType` enum (`Direct`, `Wavy`, `DashThrough`,
  `StopAndGo`, `Orbit`) plus pattern-specific tuning fields added to
  `CompanionMovementPreset`. `CompanionAI.ComputeDestination()` now dispatches to a
  different algorithm per pattern instead of always using the same trailing-point formula:
  - **Wavy** — trailing point + a perpendicular sine-wave offset (weaving path)
  - **DashThrough** — ignores the trailing point; every `DashInterval` seconds picks a new
    angle (current travel direction ± up to 120°, randomized) and targets a point
    `DashOvershootDistance` past the player along it — runs toward and past the player,
    overshoots, repeats from a new angle
  - **StopAndGo** — trailing point as normal, but alternates `MoveDuration`/`PauseDuration`
    between moving and a full halt (`aiPath.maxSpeed = 0`)
  - **Orbit** — ignores the trailing point; continuously circles the player at a fixed
    `OrbitRadius`, angle advancing at `OrbitAngularSpeed` degrees/sec every frame regardless
    of whether the player is moving (a moon around a planet, not a following companion)
- Updated presets: Close Shadow tightened further (even closer, per feedback), Wide
  Wanderer → Wavy, Eager Runner → DashThrough, Steady Anchor → StopAndGo, new 6th preset
  "Orbiting Moon" → Orbit.
- Also: floating preset-name label above the companion (added last session) shrunk to half
  size per feedback.
- **Verified:** Wavy confirmed via direct testing — genuine sine oscillation (destination
  swung from 0.96 up to a 1.20 peak and back down through -0.69), not a one-time drift.
  Orbit confirmed via an isolated throwaway test object (not the live session, to avoid
  disrupting a hands-on test in progress) — distance from target held at **exactly** 2.000
  across every sampled call while the angle smoothly advanced counterclockwise as expected.
- **Date:** 2026-07-31

[2026-07-31] Tool — live movement-preset cycler for comparing follow-feel options
- Built: `CompanionMovementPreset` (struct, in `CompanionAI.cs`) bundles the follow-feel
  tunables (`_trailDistance`, `_directionTurnSpeed`, `_walkSpeed`/`_runSpeed`,
  `_idleDistance`/`_runDistance`, `_repelDistance`/`_repelStrength`) so a whole movement
  "style" can be swapped in one call — `CompanionAI.ApplyMovementPreset(preset)`. This is a
  real, reusable API addition, not debug-only — any future per-species/Personality hook
  will need exactly this entry point.
- Built: `Assets/Scripts/Creatures/DebugMovementPresetCycler.cs` — TEMPORARY tool, attached
  to `_PartySystem`. Press **Tab** in Play mode to cycle the active companion through the 5
  Tier 1 presets drafted in `DECISIONS.md` → [Creatures] (Default, Close Shadow, Wide
  Wanderer, Eager Runner, Steady Anchor), with an on-screen `OnGUI` label showing which one
  is active. Lets these be compared side-by-side on the existing placeholder companion
  before any of them are tied to Personality or another hook.
- **DELETE `DebugMovementPresetCycler.cs`** once a real per-species/Personality movement
  hook exists and applies presets itself.
- Verified: applied all 3 distinct presets via the same code path the Tab key uses and
  confirmed `CompanionAI`'s actual fields updated correctly for each (trail distance and
  walk speed spot-checked per preset, matched expected values exactly).
- Also clarified in `DECISIONS.md`: the movement-pattern outline is explicitly **open-world
  only** (overworld `CompanionAI` following) — no relationship to Tempo's in-battle action
  economy or any combat system.
- **Date:** 2026-07-31
- **Follow-up (same day):** added a floating world-space label (`TextMesh`, child of the
  companion, sorted above its sprites) showing the short preset name above its head, so
  which preset is active is visible at a glance without checking the corner overlay — the
  Tab-cycle overlay text was hard to associate with the moving companion at a glance.
  Verified via screenshot both before and after shortening the label text (full descriptive
  names overflowed at normal zoom; now shows just e.g. "Close Shadow").

[2026-07-31] Design correction — companion collider is now a trigger, never physically blocks the player
- **User clarification:** the companion must never physically influence player movement at
  all — the player is always 100% authoritative, and the companion is entirely responsible
  for accommodating/getting out of the way, never the reverse. Earlier in this session the
  companion's `CircleCollider2D` was solid (non-trigger), which meant Unity's physics engine
  would still physically shove the player on contact regardless of how good the scripted
  repel/avoidance logic was — a solid Kinematic body always displaces a Dynamic body it
  touches, independent of any AI logic driving it. Not an A* Pathfinding Project limitation
  — plain Unity 2D physics, would happen with any movement code driving a solid Kinematic
  collider.
- **Fix:** `Phasix_Placeholder.prefab`'s `CircleCollider2D.isTrigger = true`. Still detects
  contact (`OnTriggerEnter2D`, useful later for encounter systems) but never physically
  blocks or displaces anything — all avoidance is now 100% the companion's own scripted
  responsibility (trailing + repel logic, both already built).
- **Verified:** player holding a continuous "move right" input straight through where the
  companion was standing kept a perfectly clean, constant velocity the entire time
  (`(5, 0)`, never disturbed) — while the companion visibly slid out of the path
  (`(3,0) → (2.6,0.2) → (1.7,0.55) → (0.78,0.9) → (0,1.2)`) and settled once clear.
- Decision logged: `DECISIONS.md` → [Creatures].
- **Date:** 2026-07-31

[2026-07-31] Fix — companion pushing the player around with zero input (screen-recording confirmed)
- **Symptom (user-reported, with screen recording):** starting the game fresh, with no
  input at all, showed the companion visibly pushing Mr_chimken around.
- **Root cause 1 (the actual one — confirmed by reproduction):**
  `PartySystem.EnsureCompanionInstance()` spawned the companion at
  `_playerTransform.position` — exactly coincident with the player. Two fully-overlapping
  circle/capsule colliders force the physics engine into a large separation response at
  the very first physics step, and since the companion is also actively path-following
  toward a destination near the player, that separation compounds with the companion's own
  movement rather than resolving in one clean step — reproduced directly: with the old
  spawn behavior, a stationary player (zero input, `PlayerController_SideScroll` fully
  active) still drifted ~4-6 units over about 2 seconds before settling.
- **Root cause 2 (a real, related bug found and fixed while investigating, though not the
  primary cause of the video):** `CompanionAI` derived "which way is the player moving"
  from raw `Transform.position` deltas. Position deltas are contaminated by ANY external
  displacement — including the companion's own collider nudging the player on contact —
  and `.normalized()` turns even a tiny, unintended position shift into a full-strength
  direction signal, indistinguishable from real WASD input. This is a textbook feedback
  loop: companion nudges player → tiny position shift → misread as "player moved" →
  companion reacts and nudges again.
- **Fix 1:** `PartySystem` now spawns the companion offset from the player
  (`_spawnOffset`, default `(0, -1.2, 0)` — comfortably beyond the ~0.8 combined collider
  radii) instead of exactly on top of them.
- **Fix 2:** `CompanionAI` now reads the target's `Rigidbody2D.linearVelocity` instead of a
  position delta. `PlayerController_SideScroll` re-asserts its own intended velocity every
  `FixedUpdate` regardless of what physics did to it in between, so velocity reflects the
  player's real control intent and isn't susceptible to the position-based feedback loop.
  Falls back to a flattened position delta only if the target has no Rigidbody2D.
- **Verified:** reproduced the original bug faithfully first (confirmed ~4-6 units of
  zero-input drift with the old spawn behavior, even with the target's own `FixedUpdate`
  properly driven in the test — ruling out a test-methodology false positive). After both
  fixes: 5 full simulated seconds of zero input from a natural fresh spawn produced
  **exactly zero** drift and zero velocity throughout — completely stable at rest.
- Full writeup: `LESSONS_LEARNED.md` → [Physics]. Decision: `DECISIONS.md` → [Creatures].
- **Date:** 2026-07-31

[2026-07-31] Fix — repel wasn't actually kicking in during real play (Z-axis corruption)
- **Symptom (user-reported):** after the repel feature landed, walking toward the companion
  still didn't make it step away — and the companion appeared able to be pushed/dragged by
  the player instead.
- **Root cause:** live inspection of the running game found `CompanionAI`'s
  `_smoothedTargetDirection` with a Z component of `-0.86` — nearly as large as X. Since
  this is a 2D top-down game, that should be structurally impossible; `Rigidbody2D` only
  ever touches X/Y and `Animator.applyRootMotion` is `false` (confirmed, not just assumed).
  The exact upstream source of the Z noise on the target Transform wasn't conclusively
  identified, but regardless of source, `CompanionAI`'s own direction math had no guard
  against it — normalizing a mostly-flat delta with even a small Z component can swing the
  resulting direction vector wildly, and because the trail direction blends gradually
  (`RotateTowards`), that corruption persists across many frames, collapsing the computed
  destination down to nearly the companion's own current position.
- **Fix:** `CompanionAI` now explicitly flattens to the XY plane (`FlattenToXY`) at the
  point of computing `playerDelta` and `awayFromPlayer` — the two vectors that get
  normalized into directions. This is the correct constraint for a 2D top-down game
  regardless of the Z-noise source, and verified to fully neutralize it (fed a simulated
  Z=0.9 target position through a real `Update()` call — resulting smoothed direction
  stayed at `z=0` exactly).
- **Also found (not a bug, but worth knowing):** when the player and the actively-moving
  Kinematic companion do make contact, Unity's physics engine naturally redirects the
  player's `Rigidbody2D` velocity on collision — same as bumping into any solid object. A
  manual test exaggerated how persistent this feels, because it required disabling
  `PlayerController_SideScroll` for clean velocity control, which also removed the
  continuous per-frame input correction that normally self-corrects a knock like this in
  real play. The actual in-game feel of this needs a human playtest, not another script.
- **Verified:** all 7 repel-direction test cases from the previous entry re-passed after
  this change, plus an 8th case injecting Z noise into both position and delta produced an
  identical result to its clean counterpart.
- Full writeup: `LESSONS_LEARNED.md` → [Pathfinding] / [Physics].
- **Date:** 2026-07-31

[2026-07-31] Feature — companion personal-space repel (avoid corner-wedging)
- Built: `CompanionAI` now nudges its destination directly away from the player's current
  position whenever the player is closer than `_repelDistance` (default 0.7) AND still
  closing the distance — recomputed fresh every frame from the live relative position, so
  it works correctly from any of the 360°, not just left/right (this is a free-movement
  top-down game). No repel if the player is far away, stationary, or already moving away.
  Two new tunables: `_repelDistance`, `_repelStrength` (default 0.8).
- Found & fixed a real bug in the first draft of this: the "is the player closing in"
  dot-product check had the comparison sign backwards (`< 0f` instead of `> 0f`), which
  meant it repelled exactly when it shouldn't have (player moving away) and stayed inert
  exactly when it should have fired (player closing in) — the reverse of the intended
  behavior in every case.
- Verified: not just compiled clean — tested `ComputeRepelOffset` in isolation (bypassing
  the stateful trail-direction smoothing, which would have made a full-`Update()` test
  ambiguous) against 7 scenarios: closing in from west/north/southwest/east all correctly
  pushed away in the corresponding opposite direction; far away, stationary, and
  moving-away cases all correctly produced zero offset. All 7 matched exactly once the sign
  bug was fixed — the first run (with the bug) failed in exactly the way the bug predicts.
- **Date:** 2026-07-31

[2026-07-31] Verified — player can physically collide with the active companion
- No code change needed: the Kinematic Rigidbody2D + CircleCollider2D added to
  `Phasix_Placeholder.prefab` for the earlier gravity fix already gives correct
  Dynamic-vs-Kinematic collision resolution against Mr_chimken's own Rigidbody2D, the same
  as any stump/wall.
- Verification wasn't clean on the first two tries — both were mistakes in the *test*
  script, not the game: didn't account for `Mr_chimken`'s `CapsuleCollider2D` having a
  vertical offset from its root (a rigged torso collider), then read stale
  `Collider2D.bounds` before calling `Physics2D.SyncTransforms()`. Once corrected, the
  player was blocked at the expected distance (~0.86 units, matching the sum of both
  collider radii). Full writeup: `LESSONS_LEARNED.md` → [Physics].
- **Date:** 2026-07-31

[2026-07-31] Fix — companion was standing on top of decorations it should route around
- **Symptom (user-reported, with screenshot):** companion visibly sitting on top of a
  stump/rock decoration — something Mr_chimken physically cannot do (he collides with the
  same object via real Rigidbody2D physics). Also reported: companion getting stuck in the
  same specific spots while navigating around objects.
- **Root cause 1:** `GraphCollision.diameter` is scaled by the graph's `nodeSize`
  internally (`finalRadius = diameter * nodeSize * 0.5`, confirmed by reading the A*
  source directly) — it's not a plain world-unit value. `diameter = 1` with our
  `nodeSize = 0.5` produced a real check radius of only 0.25 world units, small enough to
  miss decoration colliders that don't perfectly fill their tile cell. Verified against all
  96 painted `Decorations` tiles — 4 were misclassified as walkable at `diameter = 1`.
- **Root cause 2:** `GridGraph.cutCorners` defaults to `true`, which allows diagonal
  movement between two nodes even when both shared cardinal neighbors are blocked — a
  connection a real ~0.4-radius agent can't fit through cleanly, producing jittery/stuck
  movement right at obstacle corners.
- **Root cause 3:** `CompanionAI`'s trail direction snapped instantly to the player's
  latest movement delta rather than actually smoothing — whipsawing the follow destination
  every time the player's heading changed quickly, exactly what happens while curving
  around an obstacle.
- **Fix:** `collision.diameter = 2` (empirically the smallest value producing zero
  misclassified decoration tiles), `cutCorners = false`, and `CompanionAI` now turns its
  trail direction at a capped rate (`Vector3.RotateTowards`, tunable
  `_directionTurnSpeed`) instead of snapping.
- **Also corrected:** the earlier `cacheStartup = false` decision (previous entry) was
  itself wrong — without it, the graph's *configuration* doesn't survive a domain reload
  either, not just node data. Now `cacheStartup = true` (persists config via
  `SerializeGraphs`/`SetData`) **and** `scanOnStartup = true` (keeps walkability fresh
  against `WorldChunkManager`'s dynamic chunks) together. Verified by forcing an actual
  domain reload and reading the values back, not assumed.
- **Verified:** 0/96 decoration tiles misclassified (was 4/96). A real cross-room `ABPath`
  (corner to corner, past several decorations) computed cleanly, 50 waypoints, no error.
  Settings confirmed to survive a forced domain reload.
- Full investigation and root causes: `LESSONS_LEARNED.md` → [Pathfinding] (three new
  entries). Decisions: `DECISIONS.md` → [Pathfinding].
- **Date:** 2026-07-31

[2026-07-31] Fix — companion was falling due to AIPath's default fake gravity
- **Symptom (user-reported):** two Phasix visible in the Hierarchy, one visibly sinking
  over time.
- **Root cause:** `AIPath`'s built-in fake-gravity/ground-check system (meant for 3D/sloped
  terrain) defaults to using `Physics.gravity` unless a non-kinematic Rigidbody is present
  or gravity is explicitly zeroed. Neither was true — the companion had no Rigidbody2D at
  all, so `CompanionAI` never disabled it.
- **Fix:** `CompanionAI.Awake()` now sets `_aiPath.gravity = Vector3.zero`. Also added a
  `Rigidbody2D` + `CircleCollider2D` to `Phasix_Placeholder.prefab` matching
  `PlayerController_SideScroll`'s structure (`gravityScale = 0`, frozen rotation, continuous
  collision, interpolation) — Kinematic body type (AI-driven, not force-driven), so AIPath
  now moves it via `Rigidbody2D.MovePosition` instead of raw Transform writes, matching
  Mr_chimken's physics category and giving it real Physics2D collision/trigger participation
  for future systems (encounter triggers, etc.). Hit a `RigidbodyType2D` enum-ordering
  mistake along the way (set `bodyType=2` intending Kinematic; 2 is actually Static —
  Dynamic=0, Kinematic=1, Static=2) — caught and fixed via a live property read-back, not
  assumed. See LESSONS_LEARNED.md → [Pathfinding] for both this and a related manual-testing
  gotcha (`Rigidbody2D.MovePosition` needs an actual physics step to take effect — manual
  verification now also drives `Physics2D.Simulate()`, not just `MovementUpdate`/
  `FinalizeMovement`).
- **Also cleaned up:** deleted the leftover `Phasix_Test_Fire`/`Phasix_Test_Steam` GameObjects
  from the earlier sprite-verification session — they were confusing to see in the
  Hierarchy and are superseded by `DebugPartyBootstrap`'s live test path.
- **Verified:** idle-simulated 2s → zero drift (previously would have sunk). Player-moved
  1s-simulated follow → companion closed distance to the player correctly, `Run`→approach
  state transition as expected.
- **Date:** 2026-07-31

[2026-07-31] Debug — temporary party bootstrap for manual playtesting
- Built: `Assets/Scripts/Creatures/DebugPartyBootstrap.cs` — adds one test Phasix
  (`Test_FireType.asset`) to `PartySystem` on Play start, purely so the user can hit Play
  and walk Mr_chimken around with real keyboard input to see `CompanionAI` follow live,
  since no capture system exists yet to populate the party normally. Attached to the
  `_PartySystem` GameObject in `SampleScene`.
- **DELETE this file and its component once a real capture flow exists** (Phase 3, Mo 8
  Wk 3 per Roadmap_v2.md) and calls `PartySystem.AddToParty()` itself — this is scaffolding
  for manual verification only, not a real game system.
- **Date:** 2026-07-31

[2026-07-31] Phase 2 Wk 12-13 — Companion Following AI implemented and verified
- Built: Imported A* Pathfinding Project (free) — user downloaded directly from
  arongranberg.com (Asset Store no longer distributes a free tier, Pro-only listing now).
  Confirmed live via `unity_reflect` (`AstarPath`, `Seeker`, `AIPath`, `GridGraph` all
  present) and a synchronous `ABPath` test (21 waypoints, no error) before building on it.
- Built: New `Obstacles` physics layer — `Walls` and `Decorations` tilemap colliders moved
  onto it so the Grid Graph's collision scan doesn't treat the player's own collider (same
  layer as everything else previously) as an obstacle.
- Built: `GridGraph` scanning the test room (60×38 nodes at the already-locked 0.5-unit A*
  cell size), 2D collision mode against the `Obstacles` layer only. `AstarPath.scanOnStartup
  = true` — deliberately does NOT rely on a cached/serialized bake, since
  `WorldChunkManager`'s dynamic chunk `SetActive` toggling would make a permanent bake stale.
  See DECISIONS.md → [Pathfinding].
- Built: `Assets/Scripts/Creatures/CompanionAI.cs` — Seeker+AIPath-driven follow. Trails
  behind the player's recent movement direction (not the player's exact point) by a tunable
  distance; computes Idle/Walk/Run from follow-distance and switches `AIPath.maxSpeed`
  tiers accordingly (Run is faster than Walk, to actually catch up when it falls behind).
  Animator wiring (`IsMoving`/`IsRunning`, matching `PlayerController_SideScroll`'s existing
  parameter names) is framework-only — no real animation content exists yet
  (placeholder-first pipeline), per the Roadmap's own "build the framework, leave content
  slots empty" two-track rule.
- Built: `Assets/Scripts/Creatures/PartySystem.cs` — up to 3 slots; only the active slot has
  a physical GameObject (the same persistent `Phasix_Placeholder`-based instance gets
  re-skinned/re-targeted on slot switches, never destroyed/recreated, so this never
  conflicts with the "no Instantiate/Destroy in a loop" architecture rule). A MonoBehaviour
  singleton, not static like BondSystem/PersonalitySystem — it owns an Inspector-assigned
  prefab reference and a live spawned instance, the same category as GameManager. See
  DECISIONS.md → [Creatures].
- Changed: Added `Seeker` + `AIPath` + `CompanionAI` components to
  `Assets/Prefabs/Creatures/Phasix_Placeholder.prefab`.
- Verified: not just compiled clean — full live test in Play Mode. Added a test PhasixData
  to the party via `PartySystem.AddToParty`, confirmed the companion spawned correctly
  tinted. Hit a real environment snag (see LESSONS_LEARNED.md → [Tooling], "Play Mode
  doesn't tick frames while unfocused") where Unity's frame loop wasn't advancing at all in
  this automated session — worked around it by driving the exact same code paths
  synchronously: manually invoked `CompanionAI.Update()` via reflection (confirmed correct
  Run-state + trailing-destination computation), then synchronously solved and assigned a
  real `ABPath` and manually stepped `AIPath.MovementUpdate`/`FinalizeMovement` 60 times
  (~1 simulated second) — companion physically moved from (0,0,0) to (3.55, 1.99, 0),
  closing distance toward the player as expected. Confirmed via Scene View screenshot.
  None of this manual-stepping workaround is needed in normal play — it was purely to
  verify correctness without a focused Editor window.
- Next: no capture system exists yet to naturally populate the party outside of manual
  test code — that's Phase 3 scope (Mo 8 Wk 3 — Capture mechanic) per Roadmap_v2.md.
  Immediate next roadmap item is Wk 14-16 — Wild encounter trigger + Primal type reveal.

[2026-07-31] Phase 2 Wk 11 — Personality's full mechanic implemented and verified
- Built: `Assets/Scripts/Creatures/PersonalitySystem.cs` — static rules layer, matching
  BondSystem.cs's pattern. `RollRandom()` (uniform roll across all 18 traits, for
  capture-time assignment, GDD §7.2 "shown on capture") and `ChangePersonality()`
  (immediate, unconditional swap — "any personality to any other," GDD §7.2; no-op if
  already that personality). Item consumption for the swap is intentionally out of scope
  — pending the Item system (§22), same division of responsibility as Origin Change's
  bond-cost logic living outside BondSystem.
- Built: `Assets/Scripts/Creatures/PersonalityStatModifier.cs` — static data table
  transcribing the locked GDD §7.3 stat-nudge table (which stats each of the 18 traits
  gets ++/+/- on) verbatim. Deliberately unwired scaffolding for now — the numeric
  growth-rate formula that would consume this lives in the not-yet-built Aura allocation
  system (Progression_Directive_v0_1_0.md). Built now per user decision (July 2026 session)
  since the trait→stat mapping itself is fully locked content, not invented.
- Changed: `Assets/Scripts/Core/EventBus.cs` — added `OnPersonalityChanged` event +
  `Raise_PersonalityChanged()`, fired by `PersonalitySystem.ChangePersonality()`. Not
  fired on the initial capture-time roll (no "change" to react to yet).
- Verified: not just compiled clean — 6 scripted scenarios via `execute_code`: `RollRandom`
  distribution (18/18 distinct values over 2000 rolls), `ChangePersonality` mutation +
  event firing, no-op same-personality call correctly suppresses the event, null-phasix
  call doesn't throw, and `GetNudge` spot-checks against the GDD table (Reckless/Force =
  StrongBoost, Reckless/Guard = Reduction, Reckless/Aura = none, Brave/Force = Boost,
  Naive/Guard = Reduction, Naive/Resonance = StrongBoost) — all matched.
- Next: no capture system or Item system exist yet to actually call `RollRandom()`/
  `ChangePersonality()` from real gameplay — both are rules-layer scaffolding, same
  status BondSystem was in before combat/activity systems existed to drive it. Next
  natural pickup is whatever Phase 2/3 roadmap item comes after this — check
  `Assets/Docs/CHANGELOG.md`'s own history and the project roadmap doc for the next item.

---

[2026-07-31] Art — PrimalType-driven placeholder sprite (first visible Phasix)
- Built: `Assets/Scripts/Creatures/PrimalTypeColor.cs` — static color lookup. 8 base hex
  colors transcribed verbatim from the locked `DECISIONS.md` table; 28 duo-merge parent
  pairs transcribed verbatim from the GDD §9 "All 28 duo merged types" table (not
  invented). `GetColor()` returns the base hex directly or a 50/50 `Color.Lerp` of the two
  parents for duo types; `GetUnderglowColor()` lightens+fades that same color for the halo
  layer.
- Built: `Assets/Scripts/Creatures/PhasixPlaceholderVisual.cs` — MonoBehaviour with
  `SetPrimalType()`/`ApplyFromSpeciesData()`, tinting a Body SpriteRenderer and a larger,
  lighter/translucent Underglow SpriteRenderer behind it.
- Built: `Assets/Prefabs/Creatures/Phasix_Placeholder.prefab` (new `Assets/Prefabs/`
  folder) — Body + Underglow children, both using Unity's built-in 2D `Circle` sprite
  (`Packages/com.unity.2d.sprite/.../Textures/v2/Circle.png`, 256 PPU — at scale 1 this
  renders exactly 1 world unit, matching the locked tile size for free).
- Decided (with user): one shape (Circle) for all Phasix, color is the sole
  differentiator — no per-type shape variation. Underglow halo layer added; no separate
  ground-shadow layer. Full rationale in `DECISIONS.md` → `[Art]`.
- Verified: not just compiled clean — created two test `PhasixData` assets (`Test_FireType`,
  `Test_SteamType` — a base type and a duo type), instantiated both, called
  `ApplyFromSpeciesData` via `execute_code`, and read back both renderers' actual `.color`
  values against `PrimalTypeColor`'s computed output — exact match on all 4 (Fire
  body/glow, Steam body/glow). Confirmed visually via Scene View screenshot after fixing a
  sorting-layer bug (see below).
- Found & fixed: the prefab's SpriteRenderers were created on the `Default` sorting layer
  (value 0), which renders **behind** the tilemap's `Ground` layer (value 1) — the
  placeholder was invisible in Scene View despite having the correct color. Moved both to
  the existing `Characters` sorting layer (value 2, already used by Mr_chimken) on the
  prefab and on the two live test instances.
- Next: Wk 11 — Personality's full mechanic (rolling on capture, item-based swap).

---

[2026-07-30] Phase 2 Wk 10 — Bond System implemented and verified
- Built: `Assets/Scripts/Creatures/BondSystem.cs` — static rules-enforcement layer for bond
  gain/loss: floor logic (`newBond = max(newBond, bondFloor)`), session loss cap (5% max),
  damping above 60%/80% (halved/quartered), 100% permanent immunity, multi-milestone
  detection in one call. Wires `EventBus.Raise_BondChanged`/`Raise_BondMilestoneReached` —
  `EventBus.cs`'s own comment said these were "wired in BondSystem.cs" before this existed
- Changed: Added `sessionBondLoss` field to `PhasixRuntimeData` (not in the literal schema,
  same category as `activeSignalType` — needed so the locked session-cap rule has
  somewhere to track cumulative loss; needs wiring to a future hub-visit/bank reset, not
  built yet)
- Verified: Not just compiled clean — exercised via `execute_code` with 8 scenarios
  (basic gain/loss, floor clamping, session cap engaging partially and fully, both damping
  thresholds, a multi-milestone jump in one call, 100% immunity). All 8 matched expected
  values exactly.
- Decided: `BondSystem` deliberately does not know gain/loss *amounts* (those are pending
  NumericalCalibration.md, owned by future combat/activity systems) — it only enforces the
  structural rules on whatever raw delta it's given. Also does not handle Origin Change
  (GDD §14.4), the one mechanic allowed to break through a floor — that must set
  `bondFloor` directly rather than going through `ApplyBondChange`.
- Also locked (per the placeholder-first art pipeline unblocking Wk 7-8): tile base size
  (16x16 equivalent, 1 world unit/tile, matches the existing Grid's cellSize already in the
  scene) and A* cell size (0.5 units, for smoother companion-following pathing). Full
  rationale in `DECISIONS.md`.
- Next: the PrimalType-driven colored placeholder sprite for Phasix (the visual piece of
  the placeholder-first art pipeline decision below — not built yet, will be the first
  thing from recent sessions actually visible on screen), then Wk 11 — Personality's full
  mechanic (rolling on capture, item-based swap)

---
### Session close — 2026-07-30
Pushed through `0fe3d2c`. Everything built today (PhasixData, PhasixRuntimeData, Bond
System, supporting types) is backend/data logic only — no GameObjects, no scene changes,
nothing visible in the Editor yet, which is expected, not a bug. Pick up tomorrow with the
PrimalType placeholder-visual piece (see "Next" above) — that's the first thing that'll
actually render something. `git status` is clean except for the pre-existing, unrelated
`SampleScene.unity` modification and untracked `Screenshots/` from an earlier IK session —
neither is this session's concern.
---

[2026-07-30] Art — placeholder-first pipeline decided (colored primitives, real art deferred)
- Decided: New visual needs (Phasix creatures, future NPCs) use Unity built-in primitive
  sprites tinted by `PrimalType`-derived color, not sourced art — extends the same approach
  already used for tilemap placeholder tiles. Mr_chimken and the existing tilemap are
  unchanged. Full rationale, the 8-color base table, and the merge-blending rule are in
  `DECISIONS.md` → [Art]
- Investigated: `Assets/Artwork/Tilesets/tileset.PNG` — a candidate real tileset the user
  thought was already in the project. Turned out to be a 334×512px promotional cover
  thumbnail, not sliceable tile content; rejected as a source
- Next: real art/animation work is intentionally deferred until the game reaches a
  playable, systems-complete state — not scheduled yet

[2026-07-30] Creatures — Origin moved from PhasixData to PhasixRuntimeData
- Changed: `origin` field removed from `PhasixData` (SO), added to `PhasixRuntimeData`,
  matching how `temper`/`personality` already live there
- Decided: Found during a design discussion about a possible Temper/Origin/Signal
  synchronization mechanic — checking whether wild spawns roll a random Origin per
  individual led to reading GDD §14.4 "Origin Change," a locked mechanic that lets Origin
  change at runtime by spending Bond% (cost scales with wheel distance). This confirms
  Origin is per-individual, mutable state, not a fixed species-form property — the same
  category as Temper/Personality. Full rationale in `DECISIONS.md` → [Creatures]
- Verified: Compiles clean, confirmed live via `unity_reflect get_type PhasixRuntimeData`
  (origin now present) — no need to re-test the SO round-trip since PhasixData's other
  fields are unaffected
- Next: Evaluating a Temper/Origin/Signal synchronization system (party-composition vs.
  per-creature combo vs. TFT-style trait stacking) — design discussion in progress, not
  yet implemented

[2026-07-29] Phase 2 Wk 9 — PhasixData ScriptableObject implemented (MCP-driven build)
- Built: `Assets/Scripts/Creatures/StatType.cs`, `BondZone.cs`, `PhasixEnums.cs` (Temper,
  OriginType, TempoType, SignalType, Personality, SkillTreeType), `PrimalType.cs`,
  `StatBlock.cs`, `EvolutionHistoryEntry.cs`, `SkillData.cs` (stub), `PhasixData.cs`
  (the SO), `PhasixRuntimeData.cs` (plain C# runtime state) — created and verified live via
  the newly-connected Unity MCP tools (`create_script`, `read_console`,
  `manage_scriptable_object`), not by hand-editing files blind
- Built: `Assets/Scripts/Core/BattleResult.cs` (empty stub) — required for the entire
  default assembly to compile at all; without it, `EventBus.cs`'s pre-existing forward-ref
  errors blocked domain reload for every script in the project, not just the new ones
- Decided: Real locked names used throughout instead of placeholders — PrimalType (8 base +
  28 duo merges), SignalType (9), SkillTreeType (18, A–R) — all verified directly against
  `GDD_CreatureRPG_v0_8_0.html`, not invented. Found and used the actual 18-row Personality
  table despite the GDD's own prose/changelog saying "16 traits" in two places (verified
  discrepancy, not an invented count)
- Decided: `Temper` and `Personality` both live on `PhasixRuntimeData`, not `PhasixData` —
  both are per-individual and runtime-changeable (Re-Tempering GDD §6.4, personality item
  GDD §7), which would violate the Hard Architecture Rule if baked into the shared SO.
  `PhasixRuntimeData`'s shape (StatBlock stats, GUID-based skill refs, evolutionHistory,
  currentNodeGuid/speciesData pointer) matches `Evolution_System_Directive_v1_1_0.md`'s
  spec exactly, to avoid a rework when Phase 4's evolution graph is built. Full rationale
  in `DECISIONS.md` → [Tooling/Creatures]
- Verified: Compiles clean (only the expected, out-of-scope `EventBus.cs` errors were
  present before this work — all now resolved). Created and round-trip-tested a scratch
  `_Test_PhasixData.asset` via `manage_scriptable_object` (set fields, saved, read back the
  serialized YAML directly to confirm), then deleted it — not real roster content
- Blocked (resolved): Unity's post-compile domain reload was silently not completing while
  the Editor window lacked focus — `read_console` showed zero errors but new types were
  invisible to `unity_reflect`/`manage_scriptable_object` until the user clicked into the
  Unity window. Logged in `LESSONS_LEARNED.md` → [Tooling]
- Next: Phase 2 continues — Bond System (Wk 10, needs `BondZone` wiring which now exists)
  or Phase 1 Wk 7–8 tilemap polish (still blocked on sourcing a real tileset asset)

[2026-07-29] MCP tooling — Unity MCP bridge migrated to CoplayDev (verified live)
- Built: Removed AnkleBreaker Studio unity-mcp — `Packages/manifest.json`/`packages-lock.json`
  dependency and the `unity-mcp` entry in `.mcp.json` (the latter had been missed in an earlier
  session despite being checked off as done — verified and fixed this session)
- Built: Installed `uv` (0.12.0) and Python 3.10 was already present but missing from PATH —
  added it. Added CoplayDev's `com.coplaydev.unity-mcp` git package to `Packages/manifest.json`
  (resolved to v10.1.0)
- Built: `.mcp.json` unity-mcp entry now `{"command": "uvx", "args": ["--from",
  "mcpforunityserver==10.1.0", "mcp-for-unity", "--transport", "stdio"], "type": "stdio"}`.
  Added `"unity-mcp"` to `enabledMcpjsonServers` in `.claude/settings.json` (was missing —
  project-scoped MCP servers silently don't load without this). Unity's MCP for Unity window
  Transport switched from default HTTP Local (port 8080, which collided with an unrelated
  pre-existing service) to Stdio (port 6400)
- Decided: CoplayDev chosen over AnkleBreaker and Unity's official beta MCP — sustainability
  (company backing, MIT license, community size) prioritized over raw tool count, since
  project risk profile is low (solo dev, no critical/proprietary data). Full reasoning in
  `DECISIONS.md` → [Tooling]
- Verified: Live connection confirmed working — `read_console` and `manage_scene
  get_hierarchy` both returned real data from the open Phasix project (SampleScene, 8 root
  GameObjects). Full troubleshooting chain (4 stacked blockers) logged in
  `LESSONS_LEARNED.md` → [Tooling] for future reference
- Next: None — migration complete.

[2026-04-12] Phase 1 Wk 7–8 — Asset organisation + Tilemap + Cinemachine (in progress)
- Built: `Assets/Scripts/World/WorldChunkManager.cs` — chunk activation/deactivation by player proximity via coroutine (not Update); _activateRadius=30, _deactivateRadius=40, _checkInterval=0.5s
- Built: `Assets/Artwork/Characters/` — player/companion rig subfolders moved here (Dark Fluffy, Dark Uhdrin, MercuryVI, mr_bot, mr_chimken_new, mr_chimken_obs)
- Built: `Assets/Artwork/Creatures/Pack_A_2DMonsters/` — craftpix-341189; 10 monsters renamed Monster_01–10; non-Unity source files in _SourceFiles/
- Built: `Assets/Artwork/Creatures/Pack_B_MonsterEnemies/` — craftpix-437811; same structure
- Built: `Assets/Artwork/Creatures/Pack_C_TowerDefense/` — craftpix-168163; 10 monsters renamed Monster_01–10
- Decided: Cinemachine 3.1.x — CinemachineCamera + CinemachineConfiner2D + CinemachinePixelPerfect extension
- Decided: Placeholder tiles for test room — real tileset PNG not yet sourced; swap in later without script changes
- Decided: Pixel Perfect Camera PPU = 16 locked (320×180 reference resolution)
- Decided: craftpix-305231 tileset pack = layered background art (not tile grid); use as SpriteRenderer layers
- Deleted: `__MACOSX` junk folder from craftpix-305231 pack
- Built: Placeholder tiles `S_Ground_Placeholder` + `S_Wall_Placeholder` in `Assets/Tiles/` (Unity built-in square sprites, green + grey)
- Built: `WorldPalette` Tile Palette in `Assets/Tiles/`
- Built: `Chunk_0_0` with Grid → Ground + Walls tilemaps; Sorting Layer: Ground, Orders 0 and 1
- Built: Walls tilemap → TilemapCollider2D + CompositeCollider2D (Composite Operation: Merge, Rigidbody2D Static)
- Built: `RoomBounds` PolygonCollider2D (Is Trigger: true) — 28×18 unit room centered on (0,0) — for CinemachineConfiner2D
- Built: Pixel Perfect Camera on Main Camera — PPU 16, 320×180, Pixel Snapping
- Built: CinemachineCamera (3.1.6) with CinemachineFollow + CinemachineConfiner2D → RoomBounds; tracking Mr_chimken
- Built: `WorldManager` GameObject with WorldChunkManager — Player: Mr_chimken, Chunk: Chunk_0_0
- Decided: RoomBounds PolygonCollider2D must be Is Trigger = true — solid collider ejects player Rigidbody2D on spawn
- Decided: Wall tiles block player movement via TilemapCollider2D; RoomBounds is trigger-only for camera confinement
- Decided: Orthographic size left at 5.625 (correct baseline for 320×180 at 16 PPU)
- Decided: Assets/Artwork/Creatures/ and Assets/Artwork/Tilesets/ excluded from git — store in Google Drive/OneDrive locally
- Next: Source proper top-down terrain tileset PNG; repaint room with real tiles; lock tile pixel size in DECISIONS.md; derive A* cell size

[2026-04-12] Phase 1 Wk 5–6 — mr_chimken player controller + animation flip
- Built: `Assets/Scripts/Player/PlayerController_SideScroll.cs` — new script replacing 8-directional PlayerController for mr_chimken
  - 4-directional movement (unchanged physics — accel/decel, Rigidbody2D, new Input System)
  - Left/right sprite flip via `transform.localScale.x` negation on root (correct for bone rigs — SpriteRenderer.flipX only flips one sprite part)
  - `_rigFacesRight` Inspector bool — set per character based on native art orientation; no code change needed on import
  - `_pixelsPerUnit` Inspector int — camera-level PPU (default 16, matches Pixel Perfect Camera when added)
  - `_targetHeightPixels` Inspector float — set character height in virtual canvas pixels (320×180); auto-scales uniformly via CapsuleCollider2D native height; restores manual scale when set back to 0
  - `OnValidate` — scale applies immediately in Edit mode without entering Play mode
  - `LogDimensions` context menu — right-click component → logs native size, current scale, world size, and target pixel height to Console
  - Original `PlayerController.cs` kept untouched as legacy fallback
- Built: `Assets/Animations/Creatures/MrChimken/MrChimken.controller` — configured with IsMoving Bool param, Idle (default) and Moving states, instant transitions
- Built: `Assets/Animations/Creatures/MrChimken/moving.anim` — converted from Legacy to Mecanim, assigned to Moving state
- Changed: mr_chimken scene GameObject — removed Legacy Animation component, removed old PlayerController, added Animator + PlayerController_SideScroll, deleted stray Mr_chimken (1) duplicate
- Decided: Bone-rigged characters flip via root `transform.localScale.x` negation, not `SpriteRenderer.flipX`
- Decided: Vector/PSD bone rigs import at 100 PPU; pixel art tiles at 16 PPU (matches camera PPU)
- Decided: Camera PPU = 16 (to be set on Pixel Perfect Camera in Wk 7–8)
- Next: Phase 1 Wk 7–8 — Tilemap world + Cinemachine camera; update DECISIONS.md with tile base size and A* cell size

[2026-04-11] Doc Sync — Evolution Directive, Combat Directive, World Design update
- Built: Evolution_System_Directive_v1_1_0.pdf → Assets/Docs/ (new — supersedes GDD §3, primary evolution authority)
- Built: Combat_Directive_v0_1_0.md → Assets/Docs/ (new — 7-lane combat stage, action commands, turn structure)
- Changed: WorldDesign_Directive_v0_1_0.md — added Parts 7–9 (Blackout/Banking, Perspective/Rig, Narrative Arc)
- Changed: DECISIONS.md — updated World structure entry (single Hub → Multiple Hubs); appended 3 new April 2026 entries (Blackout/Banking, Perspective model, Bone rig)
- Changed: DOCUMENT_INDEX.md — updated to v1.1.0; added Evolution and Combat Directives; updated hierarchy; updated Superseded section
- Changed: CLAUDE.md — updated evolution authority reference (Primer §9 → Evolution Directive); fixed fusion scope (T6/T7 only → all tiers, same-tier required only for T6+); added Multiple Hubs to world description; added Blackout/Banking rules; added Scripts/Evolution/ folder; updated Reference Files block
- Changed: ClaudeCode_Primer_v1_1_0.md §5 — devolution cost line updated (free, not Specific Aura cost); §9 — fusion scope corrected
- Changed: Progression_Directive_v0_1_0.md — added supersession note to Devolution Aura Cost section (Evolution Directive wins: devolution is free)
- Changed: NumericalCalibration.md — added Combat System Values section (lane depth scales, action command timing, damage modifiers)
- Decided: Devolution is FREE per Evolution_System_Directive — supersedes Progression_Directive devolution cost rule
- Decided: Multiple Hubs (not single Hub) — each with functional specialization
- Decided: Blackout/Banking system — unbanked resources lost on party wipe, Phasix always kept
- Decided: 3/4 oblique overworld + side-profile diorama combat perspective
- Decided: Two bone rigs per Phasix (3/4 oblique + side-profile); three overworld directions for Phase 1
- Decided: Narrative arc: Innocent → Lost → Home (working, not locked)
- Next: Phase 4 implementation — Evolution system Unity scripts per Evolution_System_Directive_v1_1_0.pdf §9

[2026-03-30] Design Session — Encounter, Progression, World Structure
- Built: Progression_Directive_v0_1_0.md, WorldDesign_Directive_v0_1_0.md, DECISIONS new entries
- Decided: Aura-driven progression replaces XP/leveling entirely
- Decided: Common Aura = stat growth fuel; Specific Aura = evolution gate; Rare Variant = exotic branch gate
- Decided: Free stat allocation + Resonance Bonus layer for emotionally aligned investment
- Decided: Aptitude dual function — (A) raises stat ceiling per tier, (B) unlocks exotic evolution branches
- Decided: Evolution requires three simultaneous gates: Aura + stat minimums + conditionals
- Decided: Hub + Realms world structure with light conditional Hub evolution
- Decided: Phasix visibility = sensitivity/allergy framing (not chosen-one superpower)
- Decided: Three-layer encounter system replaces random encounters entirely
- Decided: Calendar system — story-beat-driven soft time currency, emotional seasonal context
- Decided: Four-faction working framework (Suppressors/Amplifiers/Avoiders/Integrators)
- Decided: Old lore (Fracture, Five Factions) = reference only, requires full revisit
- Next: Doc sync + commit all v1_1_0 working files

[2026-03-27] Phase 1 Wk 5–6 — PhasixAnimatorGenerator (auto-generate Animator Controllers)
- Built: `Assets/Scripts/Editor/PhasixAnimatorGenerator.cs` — EditorWindow under Phasix/Animator Generator…
  - Auto-discovers .anim clips in a target folder by keyword (idle, walk, walk_back, run, attack)
  - Optional clip prefix filter (e.g. "uhdrin") to exclude unrelated clips in shared folders
  - Auto-selects state machine type: 4-state (Idle/Walk/Run/Attack) or 5-state (adds WalkBack) based on clip presence
  - 4-state params: IsMoving (bool), IsRunning (bool), Attack (trigger)
  - 5-state params: IsMoving (bool), IsRunning (bool), IsWalkingBack (bool), IsAttacking (trigger)
  - Idempotent: skips existing controllers rather than overwriting; reports missing clips in status bar
  - Outputs `{CreatureName}_AC.controller` into the chosen animation folder
- Decided: Helpers (EnsureFolder, EnsureParameter, EnsureState, AddBoolTransition) duplicated from PhasixSpriteSetup.cs — both scripts kept self-contained; no shared utility class introduced
- Next: Phase 1 Wk 7–8 — Tilemap world + Cinemachine camera follow (Roadmap)

[2026-03-27] Phase 1 Wk 5–6 — DarkUhdrin animation pipeline (manual, end-to-end)
- Built: `Assets/Animations/Creatures/DarkUhdrin/` — full animation set for DarkUhdrin
  - 5 AnimationClips: uhdrin_idle, uhdrin_walk_forward, uhdrin_walk_back, uhdrin_run, uhdrin_attack (all at 8fps, Samples visible via Show Sample Rate)
  - DarkUhdrin_AC.controller — 5 states, 4 parameters, all transitions wired
- Decided: Switched from Dark Fluffy to Dark Uhdrin (cleaner sprite sheet, uniform grid)
- Decided: Composite sheet sliced via Grid By Cell Count (8×4) — solid background prevents Automatic slicing
- Decided: No uhdrin_walk generic state — replaced with directional walk_forward / walk_back
- Decided: SpriteRenderer.flipX handles left/right mirroring at runtime — no separate left-facing clips needed
- Decided: Run animation loops at full speed only — deceleration handled by Animator transitions in code, not clip frames
- Fixed: DarkUhdrin_AC.controller — added Walk_Back → Idle (IsMoving=false), Run → Walk_Back (IsRunning=false + IsWalkingBack=true), Run → Walk_Forward now requires IsWalkingBack=false, Idle → Walk_Back now requires both IsMoving=true + IsWalkingBack=true
- Deleted: uhdrin_walk.anim (unused generic walk clip)
- Next: Write Editor script to auto-generate Animator Controllers for future creatures (standard state machine pattern now locked in)

[2026-03-24] Phase 1 Wk 5–6 — Sprite sheet import pipeline + Animator Controller setup
- Built: `Assets/Scripts/Editor/PhasixSpriteSetup.cs` — Editor utility with 5 menu steps under Phasix/Sprite Setup/
  - Step 1: Configures Point filter, Multiple mode, PPU=32, no compression on all Dark Fluffy sheets; auto grid-slices running sheets (4×2)
  - Step 3: Creates 4 placeholder AnimationClips (idle 8fps loop, walk 12fps loop, run 16fps loop, attack 10fps no-loop)
  - Step 4: Creates DarkFluffy_AC.controller with Idle/Walk/Run/Attack states, IsMoving+IsRunning (bool) + Attack (trigger) params, all transitions wired
  - Step 5: Drops DarkFluffy_Test GameObject into scene with SpriteRenderer (Sorting Layer: Characters) + Animator
- Built: `Assets/Animations/Creatures/DarkFluffy/` — folder ready for clips and controller
- Decided: PPU = 32 (starting value; adjust after visual check in Unity Editor — see DECISIONS.md)
- Decided: Composite sheets (v1/v2) use Sprite Editor Automatic slice (Step 2, manual); running sheets grid-sliced via script
- Decided: Dark Fluffy v1 vs v2 not yet chosen — user is reviewing both; defer to DECISIONS.md once locked
- Next: In Unity — run Steps 1–5 via Phasix menu; fill clip timelines with frames via Animation window; then verify smoke test

[2026-04-15] Tooling — 2D IK foundation on Mr_chimken
- Built: `IKManager2D` + 2× `LimbSolver2D` on `Mr_chimken` root (right arm + left arm chains)
- Built: `IK_Tip_Arm_R/L` — empty tip Transforms as children of `forearm_R/L` with non-zero localPosition offset (matches upper arm bone length)
- Built: `Mr_chimken/IK/IK_Target_Arm_R` and `IK_Target_Arm_L` — moveable target GOs; drag in Scene view to drive arms
- Decided: No driving script yet — foundation only; targets moved manually in Play/Edit mode
- Decided: `solveFromDefaultPose=false`, `runInEditMode=true` — required for code-created IK to function
- Decided: Scale-flip caveat noted (PlayerController negates localScale.x on flip; targets inherit flip correctly as children of root)
- Docs: `LESSONS_LEARNED.md` — new §2D IK (LimbSolver2D) with root causes, fix steps, pre-flight checklist
- Docs: `PhasixGuide.md` v1.1.0 — IK hierarchy + gotcha callout added for future session context

[2026-03-24] Phase 1 Wk 3–4 — PlayerController + Input System
- Built: `Assets/Scripts/Player/PlayerController.cs` — 8-directional top-down movement, Rigidbody2D, new Input System, smooth accel/decel, Animator support, FreezeMovement/UnfreezeMovement API for Phase 3
- Built: `.mcp.json` — Context7 MCP server configured for Unity 6000.3.x docs lookup per session
- Built: `.claude/settings.json` — project-level Claude Code settings, Context7 MCP enabled
- Decided: Input architecture = `InputActionAsset` + manual action subscription (see DECISIONS.md)
- Decided: Using existing `Assets/InputSystem_Actions.inputactions` (Unity default) — has `Player` map + `Move` action already
- Decided: `rb.linearVelocity` used (Unity 6 renamed from legacy `rb.velocity`)
- Decided: `FreezeMovement()` / `UnfreezeMovement()` stubbed as public API for Phase 3 BattleManager
- Next: Inspector setup in Unity Editor — create Player GameObject, test tilemap room, wire script fields. Then Phase 1 Wk 5–6: import Asset Store character pack and wire Animator blend trees

[2026-03] Pre-development planning complete
- Built: GDD v0.7.9, Technical Directive v0.1.0, Roadmap v2 (GDD-aligned, 20-month plan)
- Built: ClaudeCode_Primer.md (311 lines, full GDD constraint set)
- Built: CLAUDE.md (auto-load summary for Claude Code sessions)
- Built: DECISIONS.md (implementation decisions register, initially seeded)
- Built: CHANGELOG.md (this file)
- Decided: Unity 2D URP, Asset Store art pipeline, A* Pathfinding Project (free), DOTween for tweening
- Decided: Development path = Asset Store sprites first, no custom art pipeline until post-demo
- Decided: Claude Code used for scripting with CLAUDE.md auto-loaded per session
- Decided: Creature classifier working name = Phasix (not yet written into GDD — pending confirmation)
- Decided: Art style TBD — Hades-style pre-render pipeline deferred, not ruled out
- Pending: Unity project not yet created — Phase 1 begins next
- Next: Create Unity LTS project, configure 2D URP, set up Git repo, create folder structure

[2026-03] Game renamed
- Decided: Game title changed from "Project Mon-Farm" to "Phasix"
- Note: "Phasix" now serves as both the game title AND the creature classifier name
- Note: GDD still references Mon-Farm internally — pending GDD update to reflect rename
- Note: All Docs/ files updated to reflect new name
