# Known Issues

Active bugs and blockers tracked here for Claude Code session continuity.
Full issue history lives on GitHub Issues: https://github.com/Uhdrin2796/Phasix/issues

---

## Active Issues

### [UI-001] — Skill tree carousel needs a visual pass
**Status:** Open — flagged by user after live use, not yet scoped
**Affects:** `Assets/Scripts/UI/OverworldMenuController.cs`, `Assets/UI/OverworldMenu.uss`
(`.tree-*` classes)
**Description:** The Skyrim-style paged skill tree (2026-08-09, see `CHANGELOG.md` → "Skill tray
reworked into a Skyrim-style paged tree carousel") is functionally complete — paging via buttons/
arrow keys/drag-swipe, node equip/unequip with the "stays visible as a copy" behavior, peek effect
at the page edges — but the visual presentation is still a plain functional placeholder: colored
circles for nodes, a thin flat bar for connectors, no constellation-style background/art, small
260×200px stage. User: "we need to fix the skill tree look." No specific direction given yet on
what to change (art pass, sizing, background, node styling) — needs a follow-up conversation to
scope before touching anything.
**Next:** Ask the user what specifically feels off next session rather than guessing at a fix.

---

## Closed Issues

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
