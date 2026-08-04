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

## Pending Editor Verification

Fixed via direct file edit this session (no live Unity Editor attached) — the edits are believed
correct but haven't been confirmed visually or via compile/Test Runner. Not bugs; remove each
line once confirmed.

- **AUD-001** (transparency sort mode/axis) — confirm two overlapping sprites at different Y
  actually draw in correct depth order, and confirm which pipeline asset (`Renderer2D.asset` vs
  `GraphicsSettings.asset`) is the one actually live for this project's URP renderer.
- **AUD-004** (player controller rename) — open `SampleScene.unity` and confirm no "missing
  script" warning on the player object (script GUID was preserved via `git mv`, so it should
  resolve fine, but this hasn't been opened in the Editor to check).
- **AUD-007** (corner correction) — confirm `PlayerTopDownController.ComputeCornerCorrection`'s
  *mechanism* actually helps at a real doorway/corner, not just that the placeholder threshold
  value needs calibration. Written and reasoned through, never played.
- **AUD-012** (EditMode tests) — `Assets/Tests/EditMode/BondSystemTests.cs`'s 7 assertions were
  hand-traced against `BondSystem.cs`'s source but never actually executed under Unity Test
  Runner. Run them (Editor or `unity-mcp`'s `run_tests` tool) and fix anything that doesn't
  actually pass.

---

## Closed Issues

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
