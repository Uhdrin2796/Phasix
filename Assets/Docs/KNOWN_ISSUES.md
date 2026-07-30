# Known Issues

Active bugs and blockers tracked here for Claude Code session continuity.
Full issue history lives on GitHub Issues: https://github.com/Uhdrin2796/Phasix/issues

---

## Active Issues

*(none currently — MCP-001 below was closed when the Unity MCP bridge migrated from AnkleBreaker to CoplayDev; see CHANGELOG.md July 2026)*

---

## Closed Issues

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
