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

## Log

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
