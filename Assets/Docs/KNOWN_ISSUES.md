# Known Issues

Active bugs and blockers tracked here for Claude Code session continuity.
Full issue history lives on GitHub Issues: https://github.com/Uhdrin2796/Phasix/issues

---

## Active Issues

---

### [MCP-001] — unity_get_project_context HTTP 500 bug
**Status:** Has Workaround
**Affects:** MCP session startup — `unity_get_project_context` tool
**Description:** Calling `unity_get_project_context` returns an HTTP 500 error in the current MCP server version. The tool fails silently, leaving the MCP without project context for the entire session. This means architecture rules, pending systems, code style, and the no-NavMesh/SO-read-only constraints are unknown to the MCP.
**Workaround:** Use `unity_execute_code` to load PhasixGuide.md directly at session start:
```csharp
return System.IO.File.ReadAllText(Application.dataPath + "/MCP/Context/PhasixGuide.md");
```
This is documented in the CLAUDE.md Planning Session Checklist and in the PhasixGuide.md header.
**Resolution:** Will be fixed in a future MCP server version update. When fixed:
- Remove this entry
- Remove the workaround note from the PhasixGuide.md header
- Restore `unity_get_project_context` to the CLAUDE.md planning checklist

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
