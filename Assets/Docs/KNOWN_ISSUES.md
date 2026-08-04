# Known Issues

Active bugs and blockers tracked here for Claude Code session continuity.
Full issue history lives on GitHub Issues: https://github.com/Uhdrin2796/Phasix/issues

---

## Active Issues

*(none currently — MCP-001 below was closed when the Unity MCP bridge migrated from AnkleBreaker to CoplayDev, and AST-001 below turned out to be a misdiagnosis; see CHANGELOG.md)*

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
