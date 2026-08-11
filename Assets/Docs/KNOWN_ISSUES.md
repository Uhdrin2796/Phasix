# Known Issues

Active bugs and blockers tracked here for Claude Code session continuity.
Full issue history lives on GitHub Issues: https://github.com/Uhdrin2796/Phasix/issues

---

## Active Issues

_(none currently open)_

---

## Closed Issues

### [UI-003] — Middle party slot's Attack orb has a click dead-zone — CLOSED (fixed)
**Status:** Closed — fixed same session, verified live via `IPanel.Pick()` before/after against the real `Awake()` code path
**Affects:** `Assets/Scripts/Combat/BattleHUDController.cs`
**Description:** User report, playtested with a full 3-party battle: hovering the "A" (Attack)
skill orb on the MIDDLE party slot only registered clicks near its edges, not its center — every
other orb on every other slot, and every other orb on the same slot, worked fine. Root cause,
confirmed live via `IPanel.Pick()` at the orb's own on-screen coordinates: `[EDITOR-001]`'s fix
(2026-08-08, below) made `StatusHeader` paint/pick ABOVE `.stage` so nameplate/burst-gauge clicks
would win over `.stage`'s own full-area invisible backdrop — but `StatusHeader`'s bounds are
nearly the whole top of the screen (`(0, 28, 1920, 331)`), and the middle slot's upward stagger
(`ApplyStageCreatureStagger`, `StageCreatureStaggerY = {0, -45, 25}` — slot 1 pushed up 45px)
combined with the Attack orb's own upward-right ring position pushes that ONE orb's top ~60% into
`StatusHeader`'s overlap — `Pick()` at the orb's center returned `StatusHeader` itself, not the
orb underneath, even though the orb is what's visually on top there. No other slot's stagger/orb
combination reaches far enough into that overlap to trigger this.
**Fix:** `StatusHeader.pickingMode = PickingMode.Ignore`, set once in `Awake()`. `StatusHeader` is
a pure layout container (`position: absolute`, pulled out of flex flow — see `BattleHUD.uss`'s own
comment) with no click behavior of its own anywhere in the codebase, so making it click-transparent
is safe — confirmed live that every nameplate bar/burst-track descendant (the reason `[EDITOR-001]`
needed the reordering in the first place) still resolves correctly to itself, since a parent's
`pickingMode` doesn't affect its children's own independent picking.
**Verified:** `Pick()` at the middle slot's Attack orb center changed from resolving to
`StatusHeader` (wrong) to `PlayerStageSlot1_SkillSlot0` (correct) after the fix, using the real
`Awake()`-set value in a fresh Play Mode session (not just a manual runtime patch). A nameplate bar
center still correctly picks its own descendant, not `.stage`'s backdrop, confirming no regression
of `[EDITOR-001]`. 275/275 EditMode tests unaffected (no coverage change — UI Toolkit picking-mode
fix, no C# logic changed).
**Closed:** 2026-08-11, same session.

### [AUDIO-001] — BattleHUDController.Instance stale reference crashes EventBus subscribers outside battle — CLOSED (fixed)
**Status:** Closed — fixed same session, verified via the full EditMode suite
**Affects:** `Assets/Scripts/Combat/BattleHUDController.cs`, `Assets/Scripts/Audio/BattleAudioVfxHooks.cs`
**Description:** `BattleHUDController.Instance` was never cleared on destroy. Once
`BattleScene_Main` unloads, the reference becomes a Unity "fake null" — destroyed but not equal to
C# `null` — and the `?.` null-conditional operator does NOT catch that (it bypasses
`UnityEngine.Object`'s overloaded `==`, checking raw reference nullity only). Surfaced when wiring
`BattleAudioVfxHooks.OnBondMilestoneReached` to call `BattleHUDController.Instance?.
PlayBondMilestoneVfx()` — any bond milestone reached outside of battle (the normal case; bonding
happens in the overworld) after any battle had ever run would throw `MissingReferenceException` in
real gameplay, not just in the EditMode tests that caught it (`BondSystemTests`, unrelated to
Combat, broke immediately once this call site existed).
**Fix:** Added `BattleHUDController.OnDestroy()` clearing `Instance` (guarded `Instance == this`).
**Verified:** 272/272 EditMode tests pass after the fix; confirmed live in Play Mode that
`BattleHUDController.Instance` calls from `BattleAudioVfxHooks` no-op cleanly once the battle scene
is unloaded.

### [UI-002] — Orphaned components on UIRoot_BattleSummary in BattleScene_Main — CLOSED (fixed)
**Status:** Closed — fixed, verified via a fresh disk-load of the corrected scene
**Affects:** `Assets/Scenes/BattleScene_Main.unity` (`UIRoot_BattleSummary` GameObject)
**Description:** Live console (`read_console`) showed `"The referenced script (Unknown) on this
Behaviour is missing!"`, not previously tracked anywhere. Cross-referencing every `m_Script` guid
in the loaded/relevant scenes and prefabs against `.meta` files (in both `Assets/` and
`Library/PackageCache/`) found the real culprit on `UIRoot_BattleSummary`: a dead `MonoBehaviour`
whose script guid resolves to nothing, with `m_EditorClassIdentifier: Phasix.Runtime::
AuraAllocationController` still visible — a leftover from that class's earlier, already-documented
deletion in favor of `BattleSummaryController` (see `PhasixGuide.md`'s "Old PartyMenuController...
and AuraAllocationController... both deleted, fully superseded"). Same investigation also found a
second, separate bug on the same GameObject: **two** `BattleSummaryController` components (same
script, duplicated). `BattleSummaryController` is a static singleton set in `Awake()`; both
instances queried the same shared `UIDocument` and subscribed the same Continue button's click
event. Currently harmless by luck of component-list order (only whichever instance's `Awake()` ran
last ever received a real `_onDone` callback via `Show()`), but fragile and would silently
double-fire real logic if `Awake`/`OnEnable` ever grew additional behavior.
**Fix:** Removed the dead component via Unity's `GameObjectUtility.
RemoveMonoBehavioursWithMissingScript` API (the only way to target a component with no resolvable
type — `manage_components` can't select it by type name), and the duplicate `BattleSummaryController`
via `manage_components(action="remove")`. `manage_scene(action="save")` turned out to have its own
bug — it always writes to `Assets/<SceneName>.unity`, ignoring the scene's actual folder and any
explicit `path` argument — so the real fix was applied by hand-editing
`Assets/Scenes/BattleScene_Main.unity`'s YAML directly (after confirming via grep that neither
removed fileID was referenced anywhere else in the file), not through that tool.
**Verified:** A completely fresh disk-load of the corrected scene showed a clean console (only the
unrelated, pre-existing A* Pathfinding Project editor update-checker HTTP warnings remain) and
exactly 3 components on `UIRoot_BattleSummary` (`Transform`, `UIDocument`, one
`BattleSummaryController`). 256/256 EditMode tests still pass. A first attempt at a live "win a
battle, click Continue" Play Mode check triggered a genuine Unity Editor hang (see
`LESSONS_LEARNED.md` → `[Tooling] Spin-waiting on AsyncOperation.isDone...`) — user restarted the
Editor themselves. Re-verified live in a follow-up session (synchronous `SceneManager.LoadScene`
this time, no spin-wait): `BattleSummaryController.Show()` correctly displays the panel with real
dynamic label text; synthetic `ClickEvent`/`PointerDown`+`PointerUp` dispatch via `SendEvent`
didn't trigger `Button.clicked` at all (a UI Toolkit `SendEvent` limitation already documented
elsewhere in `LESSONS_LEARNED.md`, not a defect in the fix), so the exact regression this fix
targets — double-firing from two subscribed component instances — was instead confirmed directly
by reflecting into the Continue button's `Clickable.clicked` delegate: exactly one subscriber,
`BattleSummaryController.HandleContinueClicked`, bound to the single remaining controller instance.
**Closed:** 2026-08-10, same session (playtest follow-up in a later session the same day).

### [UI-001] — Skill tree carousel needs a visual pass — CLOSED (replaced)
**Status:** Closed — the paged carousel this issue was about no longer exists
**Affects:** `Assets/Scripts/UI/OverworldMenuController.cs`, `Assets/UI/OverworldMenu.uss`,
new `Assets/Scripts/UI/SkillWebEdgeVisual.cs`
**Description:** Originally flagged (2026-08-09) after live use of the Skyrim-style paged skill
tree carousel: functionally complete but visually a plain placeholder (colored circles, a thin
flat connector bar, no atmosphere, a cramped 260×200px stage). User: "we need to fix the skill
tree look... this looks awful," then shared their original Evolution Web design mockup and asked
whether the same pan/zoom node-graph concept could work here.
**Fix:** Not a visual pass on the carousel — the carousel itself was replaced entirely
(2026-08-09, see `CHANGELOG.md` → "Skill tray reworked again: paged carousel replaced by a
pan/zoom skill web") with a free pan/zoom web view prototyping the user's Evolution Web concept:
per-tree colored/glowing node columns connected by `Painter2D`-drawn lines, tier-gated silhouette
columns for locked trees, wheel-zoom + drag-pan, and a Reset View control.
**Verified live:** `manage_ui render_ui` screenshot + structural checks (95 nodes, exactly the
expected 15 unlocked/80 locked split for a real party creature) confirmed the new view renders
correctly. 249/249 EditMode tests passing.
**Closed:** 2026-08-09, same session the replacement shipped.

### [SAVE-001] — Debug "New Game" reset didn't re-seed the party — CLOSED (fixed)
**Status:** Closed — fixed, confirmed via live Play Mode test
**Affects:** `Assets/Scripts/Core/GameManager.cs`
**Description:** `GameManager.ResetToNewGame()` reloads the active scene while `GameManager`
itself survives via `DontDestroyOnLoad`. The original boot-load logic ran in `Start()`, which
only ever fires ONCE per component instance — it does not re-run just because the scene around a
surviving `DontDestroyOnLoad` object reloads. Live-verified: clicking the debug reset button
reloaded the scene, but the party stayed empty (no "seeded fallback starter" console log), and a
previously-saved slot's stats lingered instead of resetting.
**Fix:** Moved the load/seed logic from `Start()` into a handler registered on
`SceneManager.sceneLoaded` (subscribed in `OnEnable`, unsubscribed in `OnDisable`) — fires for
every scene load this object survives, including the very first one at boot, so no separate
first-boot path is needed. See DECISIONS.md → [Core] for the full rationale.
**Verified live:** After the fix, clicking debug reset produced the expected
`"[GameManager] No save found — seeded fallback starter..."` log, the party's stats reset to a
fresh baseline (Vitality 120, Aura 0, vs. the saved slot's 121/49), and the saved slot's file
remained untouched on disk (`SaveSystem.SlotExists` still `true` afterward).
**Closed:** 2026-08-08, same session — caught during the Full Overworld Menu session's own
verification pass, never shipped.

### [EDITOR-001] — Evolution Burst gauge not clickable; nameplate hover not working — CLOSED (fixed)
**Status:** Closed — fixed, confirmed via `panel.Pick()` hit-testing before/after
**Affects:** `Assets/UI/BattleHUD.uxml` (element order), `.status-header`/`.stage` in `BattleHUD.uss`
**Description:** Originally logged as "Evo gauge not clickable" plus separate console spam that
turned out to be an unrelated oversized `Editor.log` (see `LESSONS_LEARNED.md` → `[Tooling]
Editor.log grew to 3.75GB...`, closed independently). Re-investigated after a NEW report that
nameplate hover tooltips (2026-08 bars mockup) weren't triggering at all. Root cause, confirmed via
`IPanel.Pick(point)` at the nameplate's own on-screen coordinates: `.stage` (`position: relative;
flex-grow: 1`) fills the ENTIRE `.battle-root`, including the top region the nameplate visually
occupies, because `.status-header` was pulled out of flex flow (`position: absolute`) specifically
so party-size changes wouldn't shift `.stage`'s anchor (see the still-valid `.status-header` comment
in `BattleHUD.uss`). `.status-header` was declared BEFORE `.stage` in `BattleHUD.uxml`, so `.stage`
— the later sibling — painted and picked ON TOP of it across that whole overlap, invisibly
swallowing every pointer event meant for the nameplate: `Pick()` at the nameplate's own coordinates
returned `.stage`, not any nameplate descendant. This was almost certainly the actual cause of the
original "Evo gauge not clickable" report too, not Editor-process degradation as first suspected —
same click path, same occluded region.
**Fix:** Moved `StatusHeader` to AFTER `Stage` in `BattleHUD.uxml`. Since `.status-header` is
`position: absolute`, this only changes paint/pick z-order, not layout position — `.status-header`
is now the topmost sibling for hit-testing, so it (and everything inside it: nameplate bars, and
the Radial style's ring-wrap burst-click target) correctly wins over `.stage`'s invisible backdrop.
**Verified:** `Pick()` at the nameplate HP bar's exact on-screen center changed from resolving to
`.stage` (wrong) to resolving to `.nameplate-bar-track`/`.nameplate-bar-hp` (correct, `==track:
True`) after the reorder. Dispatching a real hover event at the now-correctly-picked element showed
the tooltip with the right text ("HP: 120/120"), and leaving correctly hid it. Skill-ring orb
hit-testing was separately confirmed sound (`Pick()` correctly resolves to the exact equipped-skill
slot once its move wheel is genuinely open) — the user's report of skill-orb hover also not working
was not reproduced once the wheel-open timing was accounted for; most likely explained by testing
before the wheel had been opened, or by generalizing from the (real) nameplate bug. 220/220 EditMode
tests still pass (no coverage change — this is a UXML/USS ordering fix, no C# logic changed besides
one defensive `pickingMode = Ignore` on the decorative bar-fill element).
**Closed:** 2026-08-08.

### [AUD-006] — No camera lookahead or movement bias — CLOSED (fixed)
**Status:** Closed — fixed, confirmed via live playtest
**Affects:** `CinemachineCamera`/`CinemachineFollow` in `SampleScene.unity`, new
`Assets/Scripts/Player/CameraFollow.cs`, new `CameraLookaheadTarget` GameObject
**Description:** The audit's suggested fix (`m_LookaheadTime` on a Cinemachine Composer) doesn't
apply — this project's Cinemachine 3.x `CinemachineFollow` (Body component) has no lookahead
field at all; that's a Cinemachine 2.x API. Standard 3.x workaround: don't point the camera's
Follow at the player directly — point it at a proxy Transform that eases toward "player position
+ an offset in the player's current movement direction, scaled by speed." Built `CameraFollow.cs`
(velocity-read + `Vector2.SmoothDamp`-eased offset) on a new `CameraLookaheadTarget` GameObject,
and reassigned `CinemachineCamera.Follow`/`LookAt` from `Mr_chimken` to that proxy.
**Verified live:** at rest, offset correctly eases to zero (camera centers exactly on the player).
Sustained sprint-speed (8 u/s) movement correctly drove the offset to its configured max (1.5
world units) in the direction of travel, and the actual Cinemachine-driven camera visibly leads
ahead of the player on screen (confirmed via Game View screenshot) — Cinemachine's own
`TrackerSettings` damping still applies on top, giving a smooth compound feel rather than the
proxy snapping the camera around.
**Testing note:** this session's MCP round-trip latency (~5s+ per tool call, confirmed via
`Time.realtimeSinceStartup` deltas) made simple "set input, sleep N, check position" playtesting
unreliable — the player would already be stopped against a wall by query time regardless of the
requested sleep duration. Worked around it with an `EditorApplication.update` hook driving a
"treadmill" (re-centers the player before it reaches a wall, continuously re-asserts velocity)
so a query landing at an arbitrary, uncontrolled real time later is still guaranteed to catch
genuine sustained motion.
**Closed:** 2026-08-04, same session.

### [AUD-005] — Overworld has one verb and nothing to avoid — CLOSED (fixed)
**Status:** Closed — fixed, confirmed via live playtest
**Affects:** `PlayerTopDownController.cs`, `WildEncounterCreature.cs`, `Phasix_WildEncounter.prefab`
**Design decision (confirmed with user before implementing):** built the simple vision-cone/
detection-radius version, NOT formal overworld "lanes." `Combat_Directive_v0_1_0.md` Part 3's
"Lane Avoidance — Overworld Carry-Over" section reads as a narrative/thematic bridge to the
battle stage's 7-lane system, not a technical spec — nothing else in the GDD, World Design
Directive, or Technical Directive references overworld lanes, and the overworld controller is
free 2D movement, not gridded. That section's "not yet implemented" note stays exactly as-is;
this pass doesn't resolve it, by design.
**Built:**
- **Sprint** on `PlayerTopDownController.cs` — hold-while-held `_sprintMultiplier` (1.6x) on the
  existing, previously-unused "Sprint" action already defined in `InputSystem_Actions.inputactions`
  (Left Shift / left-stick-press). No stamina system — not specified anywhere in the docs.
- **Patrol/Alert state machine** on `WildEncounterCreature.cs` — back-and-forth patrol along the
  local X axis via a new Kinematic `Rigidbody2D` + `MovePosition` (no AIPath/Seeker, unlike
  CompanionAI); a throttled (0.15s interval, not per-frame — CLAUDE.md's no-heavy-logic-in-
  Update() rule) radius + facing-cone + `Physics2D.Linecast` line-of-sight check switches it to
  Alert, chasing the player in a straight line at `_alertSpeed` (2, always below the player's base
  5) until contact or `_loseInterestDelay` (2s) without sight. A temporary `AlertIndicator` child
  (tinted circle, placeholder-first — no real "!" icon exists yet) toggles on/off with state, per
  user request mid-session so detection has *some* visible feedback.
- Procedurally generated `Assets/Sprites/AlertIcon.png` (soft circle, 100 PPU) for the indicator.
**Bug found and fixed during playtest (not caught until a live human watched it):** after wiring
patrol/chase, the creature would visibly reach the player but never trigger the encounter prompt
— "goes underneath the player" per live observation. Root cause: AUD-002's foot-anchoring fix
(same session) moved `Phasix_WildEncounter`'s `CircleCollider2D` down near its own pivot, but the
player's `CapsuleCollider2D` sits offset ~1.4 world units *above* the player's pivot (torso-only,
by design — see closed `[AUD-007]`). With both fixes applied, the two colliders no longer share
any vertical band, so `OnTriggerEnter2D` never fired despite visual overlap. AUD-002's
foot-anchoring rationale doesn't actually apply to this prefab's collider — it's a pure
`isTrigger` encounter-detection volume with a Kinematic body, never a solid wall/movement
collider, so there was no "sticky movement" reason to shrink and foot-anchor it in the first
place. Fixed by resizing it back up to `offset: {0,2}, radius: 1.2` (local; world offset ~1.0,
radius ~0.6) — sized to reliably overlap the player's actual collision band regardless of the two
prefabs' very different pivot conventions, not to visually match the creature's small placeholder
sprite. Re-verified live: teleporting the player directly onto a creature now reliably shows the
Flee/Engage prompt, and invoking Flee correctly unfreezes the player, hides the prompt, and
destroys the creature.
**Verified live (Play mode, `execute_code` driving positions/input directly since no real input
device is available in this session):** patrol movement advances over real time; a controlled
single-axis-blocked graze test (temporary pillar, since deleted) showed the creature correctly
enter Alert and chase in a straight line; contact reliably triggers the prompt; Flee resolves the
full cycle (unfreeze, hide, destroy); Sprint moved the player at the expected ~8u/s and stopped
correctly at a wall (no tunneling).
**Known limitations, out of scope for this pass:** Alert-state chase is a straight line with no
obstacle avoidance (Kinematic bodies aren't stopped by collisions) — acceptable since chase speed
is always below the player's own move speed, but would look wrong in a level with more interior
geometry between the creature and player. Patrol is a fixed local-X back-and-forth, not a
waypoint list — tune `_patrolHalfRange` per spawn point so the path doesn't clip walls/decorations.
**Closed:** 2026-08-04, same session.

### [AUD-003] — No ground shadows on any character — CLOSED (fixed)
**Status:** Closed — fixed
**Affects:** `Phasix_Placeholder.prefab`, `Phasix_WildEncounter.prefab`
**Description:** Procedurally generated a soft radial-gradient shadow sprite
(`Assets/Sprites/Shadow_Ellipse.png`, 64x32px, 100 PPU, black-to-transparent falloff) since no
`Assets/Sprites` directory or shadow art existed. Added a `Shadow` child `SpriteRenderer` to both
creature prefabs: `sortingLayerName: "Ground"`, `sortingOrder: 2` (draws above the floor tilemap's
`sortingOrder: 1` but is still layer-ordered before everything on the `"Characters"` layer,
regardless of position — no new Sorting Layer needed), `color.a: 0.4`. Positioned at local
`(0, -0.35)`, scale `1.2`, so it visibly peeks out past the placeholder circle's silhouette.
**Caveat found during verification:** the existing `Underglow` sibling sprite (a separate,
pre-existing placeholder — not part of this fix) is fully opaque and 1.6x the size of `Body`, so
it currently hides the shadow entirely in normal play. Confirmed the shadow itself renders
correctly (soft edges, correct position/opacity) by disabling `Underglow` on a scratch test
instance only (not the prefab). Not fixing `Underglow`'s opacity here — out of scope for a
shadow-only audit item — but flagging it so a future session doesn't wonder why the shadow isn't
visible in the current scene. Player object intentionally not touched — the audit's list was
scoped to the two creature prefabs.
**Closed:** 2026-08-04, same session.

### [AUD-002] — Colliders were sprite-centered, not foot-anchored — CLOSED (fixed)
**Status:** Closed — fixed
**Affects:** `Phasix_Placeholder.prefab`, `Phasix_WildEncounter.prefab` (`CircleCollider2D`)
**Description:** The audit asked to look at why the player's own `CapsuleCollider2D` uses a
non-zero offset (`{0,14}`, size `{9,15}`) before copying it as the model for creatures. Answer,
learned while playtesting AUD-007 against the player's real collider: the offset lifts the
capsule up so it covers the character's main round body mass and deliberately excludes the thin
decorative leg/foot stalk below it from collision (see the reference art — `Mr_chimken` is a
round blob body on a single thin bone-leg; including the leg in collision would make the
footprint asymmetric and janky). The creature prefabs don't have that same "thin leg" shape
(placeholder circle sprites), so the applicable lesson isn't the exact offset, just the
principle: anchor the collider to the lower portion of the sprite, not the geometric center.
Applied `m_Offset: {0,-0.2}` (local) and shrunk `m_Radius` from `0.4` to `0.25` on both prefabs'
`CircleCollider2D`. Verified numerically (not just by eye, since Scene View gizmos don't render
in headless MCP screenshots): the collider's world bounds now span the sprite's lower half
(y from -0.225 to 0.025 against a full sprite span of -0.25 to 0.25), with a small margin so the
collision edge doesn't poke out below the visible sprite. Revisit once the real species roster
and art exist (GDD §25, pending) — these values are tuned against the Unity default circle
placeholder, not final art.
**Workaround:** None needed — fixed.

### [AUD-012 asmdef] — EditMode test assembly didn't compile — CLOSED (fixed)
**Status:** Closed — fixed
**Affects:** `Assets/Tests/EditMode/Phasix.Tests.EditMode.asmdef`, `Assets/Scripts/**`
**Description:** First live compile of the `BondSystemTests.cs` test assembly (added blind, no
Editor attached) failed with `CS0246: The type or namespace name 'PhasixRuntimeData' could not
be found`. Root cause: the test asmdef referenced the string `"Assembly-CSharp"`, but Unity's
implicit default assembly can't be referenced by name from a custom asmdef — it's only reachable
by moving the referenced code into its own asmdef. Fixed by adding `Assets/Scripts/Phasix.Runtime.asmdef`
(covering all runtime scripts, referencing `AstarPathfindingProject` and `Unity.InputSystem`) and
`Assets/Scripts/Editor/Phasix.Editor.asmdef` (Editor-only, referencing `Unity.2D.Sprite.Editor`
for `PhasixSpriteSetup.cs`'s sprite-editor APIs), then pointed the test asmdef at `Phasix.Runtime`
instead of `Assembly-CSharp`. All 7 `BondSystemTests` pass under Test Runner after the fix.
**Closed:** 2026-08-04, same session.

### [AUD-007] — Corner-correction raycast origin was measured from the wrong point — CLOSED (fixed)
**Status:** Closed — fixed, confirmed via live playtest
**Affects:** `PlayerTopDownController.cs` → `ComputeCornerCorrection`
**Description:** The audit's own note said this mechanism was written blind and never played;
verifying it in-Editor found it was actually broken. `ComputeCornerCorrection` cast its proximity
rays from `_rb.position` (the transform pivot), but the player's `CapsuleCollider2D` is offset
`{0,14}` local (~0.67 world units) above that pivot — more than 3x the correction threshold. Live
playtest against the real `SampleScene` wall corner showed the player getting stuck (0.02 units
of movement over 1.5s of continuous diagonal input) instead of being nudged through, because once
the collider physically stopped against a wall, the pivot ended up on the far side of the
collision line and the ray never crossed back over it. A first fix attempt (casting from the
collider's center) also failed the same live test, since the center-to-edge distance exceeds the
threshold too. Fixed by casting from `CapsuleCollider2D.bounds`, offset by `bounds.extents` along
whichever axis is being tested — i.e. the collider's actual leading edge. Re-verified live: at
the real room corner (both axes genuinely blocked) it correctly returns no correction; against an
isolated test obstacle with one axis blocked, it returns the expected single-axis nudge and the
player visibly slides through instead of stalling.
**Closed:** 2026-08-04, same session.

### [AUD-001/004/012 verification] — Confirmed correct in-Editor, no changes needed — CLOSED
**Status:** Closed — verified, no fix required
**Affects:** `Renderer2D.asset`/`GraphicsSettings.asset` (AUD-001), `PlayerTopDownController.cs`
rename (AUD-004), `BondSystemTests.cs` (AUD-012)
**Description:** Three items fixed blind in the prior no-Editor session, confirmed correct now
that Unity is attached. AUD-001: spawned two overlapping test sprites at different Y in
`SampleScene`, screenshotted the Scene View — the lower-Y sprite correctly draws in front,
confirming `CustomAxis (0,1,0)` sort mode is live in both pipeline assets. AUD-004: the player
GameObject resolves `PlayerTopDownController` cleanly via `find_gameobjects(by_component)` and
its full component list — no missing-script entry. AUD-012: all 7 `BondSystemTests` pass under
Test Runner (see the asmdef fix above — needed for this to even compile first).
**Closed:** 2026-08-04, same session.

### [AST-001] — "Missing AstarPath graph" + missing-script errors on Play — CLOSED (misdiagnosis)
**Status:** Closed — misdiagnosis, not a real bug
**Affects:** Was thought to affect: Companion system (`PartySystem.cs`/`CompanionAI.cs`), `SampleScene.unity`
**Description:** Initially logged 2026-08-04 while Play-testing the Wild Encounter feature: entering Play mode showed three "referenced script is missing" errors plus `NullReferenceException: No AstarPath object found in the scene` and `There are no graphs in the scene`, read as a missing/never-configured `AstarPath` graph. Re-investigation found the scene's `AstarPath` GameObject already has a fully valid, previously-baked `GridGraph` (built in the 2026-07-31 Companion AI session, per `CHANGELOG.md`) — confirmed live via `AstarPath.active.data.graphs` returning `graphCount=1, graph0 type=GridGraph` on a clean re-entry into Play mode, with the companion's `AIPath` functioning normally. The original errors were a transient artifact of querying state too early after entering Play, before Unity's post-Play initialization settled — same root cause class as `LESSONS_LEARNED.md` → `[Tooling] Play Mode doesn't tick frames at all while the Editor window is unfocused`, with a new correction/addendum added there covering this specific "state looks null immediately after Play, but resolves correctly if you wait" symptom. The missing-script errors did not recur on the clean re-test either.
**Closed:** 2026-08-04, same session — no code or scene change was needed.

### [MCP-001] — unity_get_project_context HTTP 500 bug — CLOSED (moot)
**Status:** Closed — moot, not fixed
**Affects:** Was: MCP session startup under the old AnkleBreaker unity-mcp server
**Description:** Calling `unity_get_project_context` returned an HTTP 500 error under AnkleBreaker's MCP server. Resolved not by a bug fix but by the July 2026 migration to CoplayDev/unity-mcp — the CoplayDev tool catalog has no `unity_get_project_context` tool at all (confirmed against the live `tool-groups` resource), so the bug can't recur. Project context now comes from reading `PhasixGuide.md` directly (Claude Code has filesystem access) plus the `mcpforunity://editor/state` resource for live Unity status.
**Closed:** July 2026, alongside the MCP migration.

---

## How This File Works

- Added when a bug is found — include GitHub Issue number, description, and any workaround
- Removed when the issue is closed
- Keep it short — only things actively affecting current development

### Entry format
```
### #[issue-number] — [Short title]
**Status:** Active | Investigating | Has Workaround
**Affects:** [System or file]
**Description:** One or two sentences.
**Workaround:** If one exists, note it here. Otherwise: None.
```
