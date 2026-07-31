# Phasix — Lessons Learned
Issues that required significant investigation to resolve. Read before debugging similar problems.

---

## Format
```
### [System] Issue title
- **Symptom:** What was observed
- **Root cause:** What actually caused it
- **Fix:** What resolved it
- **Date:** When resolved
```

---

## Physics & Colliders

### [Physics] Player Rigidbody2D ejected on Play start
- **Symptom:** Mr_chimken shot out of the room in a random direction the moment Play was hit. Happened regardless of spawn position and direction. Turning off all wall colliders did not stop it.
- **Root cause:** `RoomBounds` PolygonCollider2D was set as a solid collider (Is Trigger = false). On Play, the physics engine detected Mr_chimken's Rigidbody2D overlapping the solid polygon boundary and resolved the overlap by ejecting him.
- **Fix:** Set **Is Trigger = true** on the RoomBounds PolygonCollider2D. Cinemachine Confiner2D only reads the collider shape — it does not require a solid collider. The trigger flag removes it from physics simulation entirely.
- **Date:** April 2026
- **Key rule:** Any collider used exclusively for camera confinement (CinemachineConfiner2D) must always be **Is Trigger = true**.

---

## Tilemap

### [Tilemap] TilemapCollider2D "Used By Composite" option missing in Unity 6
- **Symptom:** Inspector for TilemapCollider2D had no "Used By Composite" checkbox as documented in older Unity tutorials.
- **Root cause:** Unity 6 renamed and redesigned this field. The checkbox was replaced by a dropdown.
- **Fix:** Set **Composite Operation** dropdown to **Merge** on the TilemapCollider2D component.
- **Date:** April 2026
- **Key rule:** Always query Context7 for Unity 6000.x docs before writing Inspector instructions — do not rely on training data or older tutorials.

---

## Cinemachine

### [Cinemachine] CinemachineRotationComposer not needed for 2D
- **Symptom:** Follow Camera prefab created by GameObject → Cinemachine → Targeted Cameras → Follow Camera includes a CinemachineRotationComposer component by default, which is not needed for 2D top-down games.
- **Root cause:** The Follow Camera template is designed for 3D games.
- **Fix:** Remove CinemachineRotationComposer component after creating the Follow Camera. Leave CinemachineFollow only.
- **Date:** April 2026

---

## 2D IK (LimbSolver2D)

### [IK] LimbSolver2D set up via C# produces no bone movement
- **Symptom:** `IKManager2D` + `LimbSolver2D` added programmatically. All validity checks pass (`chain.isValid=True`, `allChainsHaveTargets=True`). Moving the IK target in Scene view does nothing. Calling `UpdateIK(float)` and `DoUpdateIK(List<Vector3>)` via reflection also produces zero bone rotation change.
- **Root cause 1 — Zero-length tip bone:** Tip Transform created as child of `forearm_R` at `localPosition=(0,0,0)`. `LimbSolver2D` computes bone lengths as world-space distances between chain transforms. A zero-length second segment (`forearm→tip = 0 units`) silently breaks the analytical solve — no error, no output.
- **Root cause 2 — Wrong `UpdateIK` overload:** `UpdateIK(float globalWeight)` silently fails when `solveFromDefaultPose=true` and `StoreLocalRotations()` was never called (stored rotations default to `Quaternion.identity`, producing a degenerate restore→solve cycle). The overload `UpdateIK(List<Vector3> targetPositions, float globalWeight)` — passing target world positions explicitly — works correctly.
- **Root cause 3 — Edit mode disabled:** `IKManager2D.runInEditMode` defaults to `false`. Moving targets in Scene view has no effect until entering Play mode or explicitly setting `runInEditMode = true`.
- **Fix:**
  1. **Non-zero tip offset** — set `IK_Tip.localPosition = new Vector3(forearm.localPosition.x, 0, 0)` so tip is offset by the same length as the upper arm bone.
  2. **`solveFromDefaultPose = false`** on all `LimbSolver2D` components created via code.
  3. **`manager.runInEditMode = true`** on `IKManager2D` for Edit mode preview.
  4. Re-call `chain.Initialize()` after repositioning tip bones so `chain.lengths` recomputes.
- **Date:** April 2026
- **Checklist for any future IK setup via code:**
  - `chain.lengths` — both values must be `> 0` after `Initialize()`, else tip bone has zero offset
  - `solveFromDefaultPose = false` unless you've explicitly called `StoreLocalRotations()` first
  - `runInEditMode = true` on `IKManager2D` for Edit mode preview
  - `LimbSolver2D` always resolves to `transformCount=3` after `Initialize()`: `[shoulder, forearm, tip]` — expected, not a bug
  - Use `UpdateIK(List<Vector3>, float)` overload when forcing solves from code

---

## Art & Assets

### [Assets] Craftpix tower defense tileset (305231) is not a tile grid
- **Symptom:** Pack appeared to be a tileset but tiles could not be used in Unity Tile Palette.
- **Root cause:** The pack contains two types of content — (1) pre-composed full-scene background layers (land, road, river, decor) and (2) isometric path tiles (TAILS folder). Neither is a top-down terrain tile grid compatible with Unity's Rectangular Tilemap.
- **Fix:** Use the background layers as stacked SpriteRenderer GameObjects for backdrop art. Use TAILS tiles as decorative props. Source a separate top-down terrain tile grid PNG for the Tilemap.
- **Date:** April 2026

### [Assets] Craftpix monster packs (341189, 437811, 168163) are frame-by-frame, not sprite sheets
- **Symptom:** Monster sprites are individual PNG files, not sprite sheets.
- **Root cause:** Craftpix packages individual animation frames as separate PNGs rather than a single sprite sheet.
- **Fix:** Import individual PNGs directly. Build AnimationClips by dragging frames into the Animation window in sequence. No TexturePacker or Fresco step needed.
- **Date:** April 2026
- **Key rule:** Do NOT assemble these into a PSD bone rig — they are pre-animated frame-by-frame sprites, not rigs.

---

## Tooling

### [Tooling] CoplayDev unity-mcp migration — four stacked blockers before the bridge connected
- **Symptom:** After installing the `com.coplaydev.unity-mcp` package and clicking "Configure All Detected Clients," Claude Code still had zero working connection to Unity — no errors surfaced anywhere obvious.
- **Root cause (four separate issues, each hiding the next):**
  1. Claude Code was never in the "detected clients" list — its `ClaudeCodeConfigurator` shells out to the `claude` CLI binary, which isn't on PATH in this desktop-app-hosted setup. `.mcp.json` has to be hand-edited instead of relying on the auto-configure button.
  2. Project `.claude/settings.json` only allow-listed `context7` in `enabledMcpjsonServers` — any `.mcp.json` server not in that array is silently never loaded, regardless of how correct the entry is.
  3. The package's default HTTP transport (`http://127.0.0.1:8080`) collided with an unrelated pre-existing Windows service (`ApplicationWebServer.exe`) already bound to port 8080 — Unity's HTTP listener never actually came up, but the UI didn't make that obvious.
  4. The manually-written stdio command used the wrong package identifier — `mcp-for-unity` is the *executable/tool name*, not the PyPI package. The install source must be `--from mcpforunityserver==<version>` (see `AssetPathUtility.GetUvxCommandParts()` in the package source) with `mcp-for-unity` only as the trailing arg.
  Even after all of that, Unity's own Transport dropdown (Window → MCP for Unity) still defaulted to "HTTP Local" and needed to be switched to "Stdio" to match, then required one manual "Start Session" click for the already-open Editor session (stdio auto-starts via `[InitializeOnLoad]` on every subsequent Editor launch — this was a one-time nudge, not a recurring step).
- **Fix:** `.mcp.json` entry: `{"command": "uvx", "args": ["--from", "mcpforunityserver==10.1.0", "mcp-for-unity", "--transport", "stdio"], "type": "stdio"}`. Add `"unity-mcp"` to `enabledMcpjsonServers` in `.claude/settings.json`. Set Unity's MCP for Unity window Transport to Stdio.
- **Date:** July 2026
- **Key rule:** When a CoplayDev/unity-mcp client isn't in "detected clients," don't assume the package is broken — check whether the client's detector depends on a CLI binary being on PATH, and hand-edit `.mcp.json` instead. Always cross-check `.claude/settings.json`'s `enabledMcpjsonServers` array whenever a project-scoped MCP server silently fails to load.

### [Tooling] Unity domain reload silently doesn't complete while the Editor window lacks focus
- **Symptom:** After creating new scripts via `create_script` and requesting a compile via `refresh_unity`, `read_console` showed zero compile errors — but the new types were completely invisible to `unity_reflect` search and `manage_scriptable_object create` failed with `type_not_found`, even after repeated forced refreshes.
- **Root cause:** Two stacked issues, the second only discoverable by first ruling out the first:
  1. `mcpforunity://editor/state`'s `compilation.last_domain_reload_after_unix_ms` timestamp never advanced across multiple compiles, even though `last_compile_finished_unix_ms` kept updating — meaning compilation was succeeding but the domain reload that actually loads new types into the running AppDomain wasn't happening. `editor.is_focused` was `false` at the time; Unity can defer the post-compile domain reload while its window isn't in focus.
  2. Even after bringing the Editor into focus, the same thing kept happening — because the project's *entire* default assembly (no assembly definition files split it) still had a real compile error elsewhere (`EventBus.cs` referencing a still-undefined `BattleResult` type). A single compile error anywhere in an assembly blocks the *whole* assembly from reloading, not just the file with the error — so `read_console` correctly showed "only that one error," but that one error was enough to prevent every new type in the same assembly (including ones with zero errors of their own) from ever loading.
- **Fix:** Ask the user to click into/focus the Unity Editor window (resolves issue 1). Create a trivial empty stub for any type still blocking full-assembly compilation, even if it's out of scope for the current task — the project's own "Stub rule" (in `Evolution_System_Directive_v1_1_0.md`) explicitly sanctions this: forward-referenced types only need to exist, not be fully implemented, for the rest of the assembly to compile.
- **Date:** July 2026
- **Key rule:** `read_console` showing "only N pre-existing errors" is not proof the rest of your new code loaded successfully — in a project with no assembly definition splitting, any error anywhere blocks domain reload for everything. Cross-check with `unity_reflect search` (or an actual asset-creation attempt) before trusting a clean-looking error diff. Also check `editor.is_focused` in `mcpforunity://editor/state` before assuming a stuck domain reload is a code problem.

### [Tooling] Unity MCP stdio connection doesn't survive Unity being closed/reopened mid-session
- **Symptom:** After closing and reopening the Unity Editor while a Claude Code session kept running, `unity-mcp` tools either disappeared from the tool list entirely, or Unity's own "MCP for Unity" window showed a stale "No Session" (red) indicator even when the connection was actually working fine.
- **Root cause:** Two separate things. (1) The transport is stdio — the bridge process is spawned once when the Claude Code session starts and is piped directly to that one specific Unity process. When Unity closes, that pipe dies; the already-running bridge doesn't retry against a new Unity instance when Unity reopens — a fresh Claude Code session is required to spawn a new bridge process. (2) Independently, Unity's "MCP for Unity" window doesn't reliably repaint its Session indicator even after a new client successfully connects — the visual "No Session" state can lag behind the real, working connection.
- **Fix:** If Unity gets closed and reopened mid-session, restart the Claude Code app too (fully quit, reopen) — don't just retry tool calls and expect it to reconnect. Best practice: open Unity first, before starting the Claude Code session, and leave it running for the whole session — then this never comes up. To check connection state, trust a live tool call (e.g. `read_console`, `manage_scene get_active`) over Unity's own Session indicator.
- **Date:** July 2026
- **Key rule:** Unity closing/reopening mid-session always means restart Claude Code too, not just Unity. Never trust the "MCP for Unity" window's Session indicator alone — verify with an actual tool call before concluding the connection is up or down.
