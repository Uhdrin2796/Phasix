# Known Issues

Active bugs and blockers tracked here for Claude Code session continuity.
Full issue history lives on GitHub Issues: https://github.com/Uhdrin2796/Phasix/issues

---

## Active Issues

Open items from the external repo audit (`AUDIT_202608.md`) that need a live Unity Editor/MCP
session to fix safely — see `CHANGELOG.md` → "Repo audit triage" (2026-08-04) for what was
already fixed blind this pass.

### [AUD-005] — Overworld has one verb and nothing to avoid
**Status:** Active — the audit's own highest-priority finding
**Affects:** `WildEncounterCreature.cs`, `EncounterTrigger.cs`, `PlayerTopDownController.cs`,
`Combat_Directive_v0_1_0.md` Part 3
**Description:** Player has no verb beyond walk (no dash/sprint/interact); wild creatures are
stationary contact-triggers with no patrol, vision cone, or detection radius. `Combat_Directive`
Part 3's lane-avoidance mechanic is unimplementable as written until patrol + overworld lanes
exist. Recommendation: one design session (second overworld verb, patrol + detection radius,
decide whether overworld lanes are a real spatial concept) before Phase 3's `BattleScene_Main`.
**Workaround:** None — this is a design gap, not a bug.

### [AUD-003] — No ground shadows on any character
**Status:** Active
**Affects:** `Phasix_Placeholder.prefab`, `Phasix_WildEncounter.prefab`, player object
**Description:** No shadow sprite/prefab exists anywhere in the project (confirmed — no
`Assets/Sprites` directory at all). Needs a new sprite asset + child `SpriteRenderer` wired into
prefab hierarchies with the correct sorting layer — held for an Editor/MCP session
(`unity-mcp`'s prefab/GameObject tools) rather than hand-written prefab YAML, which is easy to
corrupt.
**Workaround:** None.

### [AUD-002] — Colliders are sprite-centered, not foot-anchored
**Status:** Active
**Affects:** `Phasix_Placeholder.prefab`, `Phasix_WildEncounter.prefab` (`CircleCollider2D`,
`m_Offset: {0,0}`)
**Description:** Creature colliders are centered on the sprite instead of foot-anchored, which
in 3/4 perspective reads as sticky/imprecise movement. The player's own `CapsuleCollider2D`
already uses a non-zero offset (`{0,14}`, size `{9,15}`) — worth an Editor look at why before
treating it as the model to copy. Footprint sizing is inherently a "look at it against the
sprite" tuning pass.
**Workaround:** None.

### [AUD-006] — No camera lookahead or movement bias
**Status:** Active — audit's suggested fix does not apply to this project, see note
**Affects:** `CinemachineCamera` in `SampleScene.unity`
**Description:** No lookahead/dead-zone bias toward movement direction. **Correction to the
original audit:** its suggested fix (`m_LookaheadTime` on a Cinemachine Composer) is a
Cinemachine 2.x API — this project uses Cinemachine 3.x (`CinemachineFollow`), which has no
lookahead field at all. Needs a custom velocity-based follow-offset script instead, and camera
feel is fundamentally a "watch it move" tuning task.
**Workaround:** None.

---

## Closed Issues

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
