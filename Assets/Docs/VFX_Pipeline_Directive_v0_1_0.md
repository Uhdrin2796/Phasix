# Phasix — VFX Pipeline Directive
**Version:** 0.1.0
**Date:** August 2026
**Status:** Working — design capture, not yet built. Companion to Architecture_Directive's Phase 4.
**Errata (2026-08-23):** Part 5 replaced — was a single Direct Projectile worked example, now a full
Skill Execution & Resolution Lifecycle breakdown covering all 9 distinct lifecycle families found by
walking every real built skill in `Assets/Data/Skills/` against it (not just the projectile). The old
worked example is preserved as Family 1's concrete example, not lost. Open Items renumbered to Part 6
and expanded with the family-specific gaps this pass surfaced, plus a flagged discrepancy: an
unresolved mismatch with `DOCUMENT_INDEX.md`'s claim that a "Direct Projectile" skill is already
built — no asset in the actual Skills folder matches that shape. Addition/refinement only — no
version bump.
**Errata (2026-08-23, later same session):** Part 2's Shader Graph section replaced entirely — full
research pass on whether Shader Graph node authoring is achievable via MCP at all (real history from
a predecessor tool, a definitive answer from CoplayDev's own published roadmap, and a full breakdown
of the practical hybrid workflow). **Text-only is now stated as the default**, not an equally-weighted
option — see below. New companion doc: `VFX_WorkedExamples_v0_1_0.md` — two contrasting end-to-end
examples (text-only Fireball, hybrid Corruption), superseding an earlier single-example doc that
paired the hybrid workflow with the wrong effect (Fireball doesn't need it). Addition/refinement
only — no version bump.
**Errata (2026-08-24):** New Standard Verification Workflow subsection (end of Part 2) and Part 4
principle 7 — video capture via an external, OS-level screen-recording tool is now the standard
check for any skill/effect review, not just Shader Graph work, replacing single-screenshot
verification as the default. Explicit requirement that captures be saved and presented as viewable
files in-session, not just privately analyzed and summarized in text. `VFX_WorkedExamples`'s testing
steps updated to match. Addition/refinement only — no version bump.
**Errata (2026-08-24, later same session):** Pinned a concrete tool for the verification workflow —
`DevStudio MCP` (real install commands, config entry), researched against several alternatives after
confirming the dev environment is Windows. One open caveat noted (window vs. monitor targeting
unconfirmed) with a practical mitigation (maximize the window). `windows-screenshot-mcp-server`
noted as the backup for the interval-screenshot fallback. Addition/refinement only — no version bump.
**Errata (2026-08-24, DevStudio MCP live-tested):** Two real findings from actually installing and
using DevStudio MCP end-to-end against a live battle, not just researching it: (1) it has a
Windows-only startup crash — a Unicode checkmark logged on tool registration can't encode on the
default cp1252 console codepage — fixed by setting `PYTHONIOENCODING=utf-8` in the `.mcp.json`
server entry's `env` block, not a code change; (2) confirmed the open caveat above the hard way —
simulated OS clicks/keystrokes (computer-use tooling) do **not** reliably reach Unity's Game view
during Play mode, so triggering the action to record needs a different approach than clicking
through the UI. New "Triggering the action, not just capturing it" subsection added below with the
working technique. Addition/refinement only — no version bump.
**Related:** Architecture_Directive_v0_1_0.md (Phase 3 world-layer migration is a prerequisite — this doc has nothing real to attach to until creatures/projectile are Scene GameObjects), Attack_Pattern_Directive_v0_1_0.md (skill archetypes this VFX serves; Part 7's Beat Sequence is the authority for Family 6 below, not re-documented here), VFX_WorkedExamples_v0_1_0.md (two full end-to-end tutorials — text-only and hybrid — applying Part 2's workflow)

---

## Overview

How skill/spell VFX actually get built: which tool for which job, the real authoring flow for each, the principles that keep authoring skill #40 as cheap as skill #4, and — the core of this revision — the actual sequence a skill executes and resolves through, which turns out to differ substantially by skill family rather than being one universal shape.

---

## Part 1 — Tool Comparison

| Tool | What it's for | Authoring constraint | Runtime scriptable? |
|---|---|---|---|
| **Shader Graph** | Per-pixel surface look — glow, dissolve, noise, distortion | `.shadergraph` is a proprietary node format, GUI-editor-only for the graph shell. **Confirmed, not assumed:** CoplayDev's own published roadmap lists "Shader Graph node creation" under "Confirmed Dead Ends (Do Not Implement)," reason stated as "Internal API, no public editor scripting." Not a gap they haven't gotten to — a deliberate policy, part of their stated "zero internal/private API hacks" design principle. See Part 2 for the full picture and the real hybrid that exists despite this. | Once a Material exists, yes — properties are driven via script normally (`Shader.PropertyToID`, `Material.SetFloat`, etc.). A Material has no memory of how its shader was authored — fully scriptable regardless. |
| **2D Rigging (2D Animation package)** | Skeletal deformation of a single illustrated sprite | Skinning Editor (bone placement, weight painting) is GUI-only, fundamentally visual/manual work | One-time setup per sprite; after that, bones are plain Transforms — fully scriptable (Animator, DOTween) |
| **Sprite Shape** | Geometry/path built from a spline, sprites tiled along it | None — the spline is a public scripting API end to end | Fully scriptable, authoring included |
| **LineRenderer** (built-in) | Simple line strip from a point list | None | Fully scriptable |

The load-bearing distinction: Shader Graph and 2D Rigging both have a **GUI-only step a human must do**; Sprite Shape and LineRenderer don't. This should directly influence which tool gets reached for first when something needs to ship without waiting on a human editor session.

---

## Part 2 — Authoring Flow, Per Tool

### Shader Graph

**Is node-level authoring exposed via MCP at all? Researched, not assumed.**
- Phasix's actual tool (`CoplayDev/unity-mcp`) exposes `manage_shader` — confirmed via its real parameter signature to be plain-text `.shader` CRUD only (`action`, `name`, `path`, `contents`: shader code). No node/graph parameters exist.
- Phasix's **prior** tool (AnkleBreaker, migrated away from July 2026) *did* build real node-level tools (`unity_shadergraph_create`/`add_node`/`connect`/`disconnect`). Initially shipped badly broken — four documented bugs, every generated graph corrupt or invalid — because the implementation hand-edited the `.shadergraph` JSON as text/regex. Later fixed by routing through Shader Graph's actual internal `GraphData`/`MultiJson`/`FileUtilities` editor model instead, so Unity's own serialization writes a valid asset. That capability was never carried over to CoplayDev.
- **CoplayDev's own published roadmap settles "will it ever exist here":** it's explicitly in their "Confirmed Dead Ends (Do Not Implement)" list — reason: "Internal API, no public editor scripting" — consistent with their stated policy of zero internal/private API usage across their entire roadmap. Same list also confirms 2D Animation bone rigging ("Deep, undocumented editor API") and VFX Graph node editing ("Internal visual graph API") for the identical reason — independent confirmation of this doc's guidance on both.

**Three paths, given that:**
1. **Text-only** — continue the proven hand-coded technique (`DissolveEffect.shader`'s noise-vs-threshold `Step` + glowing `Emission` edge). Zero new engineering, fully Claude-Code-buildable, zero GUI dependency, zero version-drift risk.
2. **Build the equivalent for CoplayDev** — not a fork: CoplayDev has a real, documented extension point (`[McpForUnityTool]` custom tools, auto-discovered via reflection over any `Editor/` folder, invoked via `execute_custom_tool`). Would mean replicating AnkleBreaker's fix — real multi-day engineering against an undocumented API, with the same inherent version-drift fragility their own fail-closed design had to guard against. **Real project-specific blocker to check first:** a known CoplayDev issue found custom tools undiscoverable in **stdio** transport mode — and `.mcp.json` here is configured for stdio. Verify that's resolved before investing the effort.
3. **The hybrid — Custom Function Node, File mode (the actual recommended default).** See below.

**Text vs. GUI-authored — what's actually equivalent, and the real asymmetry:**
- Functionally reproducible by hand with real effort — a `.shadergraph` is just HLSL Shader Graph would otherwise generate for you. Exposed properties (Blackboard) map cleanly to ShaderLab's `Properties {}` block — not a gap.
- Real gaps: pipeline-compliant boilerplate (Target/SubTarget/BlockNode structure, exactly where the naive AnkleBreaker bug failed) must be hand-correct; no live per-node preview.
- **The asymmetry that matters most:** a hand-coded `.shader` can never be opened in the Shader Graph window later — different asset type, no import path, no conversion button. A **genuine** `.shadergraph` (however it was created — human, or a correctly-built Option 2 tool) opens and edits in the GUI normally, because it's the real asset, not an imitation. **Editability is decided by which asset format was created, not by who/what created it.**

**Decision heuristic — text is the default; Shader Graph is the exception, not the alternative:**
Ask: *could I verify this is correct just by reading the shader math, or do I need to look at it?*
- **Formula-verifiable → text (the realistic default for almost everything).** Dissolve, elemental color variants (often just Material property swaps on the *same* shader — no new shader needed), windup crackle, glow pulsing (`SpriteGlowController` isn't even a shader question). Testable without any GUI session at all: automatic compile-error checking (a broken shader turns pink, Console shows the error), the Material Inspector's live preview swatch, and — since Unity MCP tooling can capture Scene/Game view screenshots — even the final in-context look can be verified without a human, by capturing and inspecting an image directly.
- **Needs-eyes-on-it → Shader Graph, and only when an effect genuinely earns it.** Layered/composited effects with a taste-driven balance (e.g. a Corruption status visual combining distortion + color-shift + outline), one-off hero/boss VFX where polish is the point, anything tuned against live in-game footage, anything meant to become a shared reusable template.
- Given most of Part 5's 9 families need formula-verifiable effects, **text-only should be treated as the stated default, not one of two equally-weighted options** — reach for the hybrid only when an effect specifically fails the "verify by reading" test, not as a default starting posture.

**The hybrid: Custom Function Node, File mode.**
A Custom Function Node lives inside a real `.shadergraph` graph, but in **File mode** it doesn't generate its function inline — it references a separate, plain-text `.hlsl` file and calls a named function from it. This splits the work cleanly:
- **One-time, human-only:** create the graph shell, place the Custom Function Node, define its input/output ports, set File mode + point Source at the `.hlsl` file, **wire it into the rest of the graph.** This wiring step is real and doesn't disappear — it's not eliminated, just narrowed to "wire an empty-bodied node" instead of "author and iterate on a whole shader."
- **Free, forever after, no GUI needed:** editing the `.hlsl` file's actual function body — the formula itself — needs no re-wiring. The node stays connected; only what's inside the referenced file changes. This is where Claude Code operates indefinitely once the shell exists.
- **Preview works as expected, with one gotcha:** Shader Graph tracks the external `.hlsl` file as a real dependency and recompiles/updates both the per-node thumbnail and Main Preview when it changes (this had bugs in early Shader Graph versions, confirmed fixed in modern ones). The gotcha: the **preview context can't access Render Pipeline libraries** — `.hlsl` code calling certain URP-specific functions can show a compile *error in the preview* while working correctly in the actual shipped shader. Guard such code with `#ifdef SHADERGRAPH_PREVIEW` and a fallback default value for the preview context.
- Functions must use Shader Graph's precision-suffix convention (`_float`/`_half` appended to the function name); each `.hlsl` file used this way needs a unique include-guard identifier if multiple exist in the project.

**Sequencing — start in one, polish in the other (both directions are legitimate):**
- **Text → Graph (fits Phasix best):** Claude Code proves the formula fast and autonomously in plain HLSL. Once behavior is locked, a human mechanically rebuilds the same, now-unchanging logic as nodes (or wires it in via a Custom Function Node directly) — GUI time is never spent on math that might still change.
- **Graph → Text:** for when the *look itself* needs visual exploration before the formula is known. Shader Graph's "View Generated Shader" gives a one-time HLSL starting point (a copy, not a live link — editing generated code never feeds back into the graph) to hand-clean into a lean, permanent shader once the look is locked.

**Extraction and composition, within Shader Graph itself:**
- **Convert to Sub Graph** (real, documented feature): select a node cluster in an existing graph, right-click, extract into its own standalone `.shadersubgraph` asset — works after the fact on something already built, not just planned upfront.
- Sub Graphs plug into a parent graph as ordinary nodes — the literal engine mechanism for "small reusable pieces → full effect," matching this doc's shared-asset-library principle (Part 4, principle 4) as a real Unity feature, not just a convention. E.g. a shared `Master_Elemental_Dissolve` Sub Graph, referenced by Fire/Water/Lightning's individual full graphs alongside their own element-specific coloring.
- **Precision worth holding onto:** this composability is *within* a shader. The *full VFX effect* (shader + Sprite Shape trail + particle burst + glow script) composes at the **prefab/GameObject level** instead — a different layer, not the same mechanism. Don't conflate the two.

**The actual payoff — how much of the full effect stays scriptable regardless of the above:**
Assembling the effect (GameObject, `SpriteRenderer`/`ParticleSystem`/`SpriteShapeController` components, the `ProjectileVfxController`/`SpriteGlowController` scripts, prefab, pooling) never touches a shader and is always fully Claude-Code-scriptable, independent of which Shader Graph path was used. Once *any* `.shadergraph`-derived Material exists, it's an ordinary Material — Claude Code can create variants, assign them, and set exposed properties entirely via script. **Net shape: one narrow, one-time GUI step at shader authoring (or per-Custom-Function-Node wiring); everything downstream — Material variants, prefab assembly, pooling — is ordinary scriptable work.**

### Standard verification workflow — video, not a single screenshot, for any skill/effect review

Applies broadly, not just to Shader Graph work: every effect in this pipeline is time-varying (dissolve sweeps, glow pulses, scrolling noise, domain warp, a full Beat Sequence Windup→Attack→Return), and a single static screenshot can look correct while the actual motion reads wrong. **Video capture is the standard check, not an optional upgrade.**

- **Tool (concrete, verified for this project's Windows dev environment): DevStudio MCP** (`nihitgupta2/DevStudio`) — a genuine MCP server, external to Unity's own `unity-mcp` entirely, so it never depends on whether a given Unity system exposes a scripting surface (the exact wall Shader Graph and VFX Graph hit). Ffmpeg is bundled via PyAV, no separate install needed.

  ```bash
  git clone https://github.com/nihitgupta2/DevStudio.git
  cd DevStudio
  uv sync              # or: pip install -e .
  devstudio-mcp        # or: python -m devstudio_mcp.server
  ```

  Register alongside `unity-mcp` in `.mcp.json`:
  ```json
  "devstudio": {
    "command": "devstudio-mcp",
    "type": "stdio"
  }
  ```

  Tools: `start_recording` / `stop_recording`, MP4 output, adjustable fps, optional system audio.

  **One open caveat, with a practical mitigation:** whether `start_recording` targets a specific window or the full monitor isn't confirmed as of this writing — verify on first use. If it turns out to be monitor-only, **maximize the Unity Editor/Game view before recording** so the monitor and the window of interest are effectively the same thing. Simple, fully closes the gap either way.

  **Backup, if the interval-screenshot fallback below ends up preferred over video:** `windows-screenshot-mcp-server` (amafjarkasi) — Windows-native, Go-based, production-ready, targets by title/class/PID/handle, purpose-built for this kind of visual-agent workflow.
- **What to capture:** one full cycle of whatever's being verified — a complete threshold sweep, one glow-pulse period, a full Beat Sequence from Approach through the automatic return-to-origin. Target the Unity Editor/Game view window specifically, not the full desktop.
- **Save as a file and present it — this is the part that's easy to skip and shouldn't be.** The point isn't just that Claude can privately analyze frames and summarize in text — the video should be saved and shared as an actual viewable artifact in the session, so the human collaborator can watch the same thing Claude is evaluating. A text summary of "the dissolve looked correct" is a weaker artifact than a 3-second clip either party can actually watch.
- **Lighter-weight fallback:** an interval screenshot sequence (a window-targeted screenshot tool, repeated capture) when a quick sanity check is enough and full video is overkill — e.g. confirming a Material property change took effect, not evaluating whether an animation's timing feels right.
- **This applies to every family in Part 5, not just projectile-style effects** — a Multi-Hit Volley's per-hit timing, a Sustained/Hold effect's continuous look, a Melee Beat Sequence's Approach-through-Return arc are all better evaluated as short clips than as any number of individual frames.

### Triggering the action, not just capturing it

Capturing a video is only half the workflow — something has to actually happen on screen first. **Confirmed 2026-08-24: don't try to trigger it by simulating clicks/keystrokes into the Unity Game view** (computer-use tooling) — the battle HUD's UI Toolkit buttons (skill wheel, target selection, Flee) never responded despite the simulated clicks visually landing on the correct on-screen coordinates, confirmed directly by watching the resulting (empty) video. `Escape` did stop Play mode during this attempt, but that's Unity's own editor-level shortcut firing, not evidence that input was reaching the running game.

**What actually works: drive the action through `unity-mcp`'s `execute_code`, not the UI.** `BattleManager.ResolveSkillAction(BattleParticipant attacker, int attackerSlotIndex, SkillData skill, BattleParticipant target)` is a private `IEnumerator` coroutine, but it's the exact same single dispatch point the real click-driven pipeline calls — it raises the same `EventBus` events (`Raise_SkillUsed`, projectile launch, etc.) that `BattleHUDController` subscribes to for animations, so invoking it directly fires the identical VFX/animation chain a real click would. Confirmed working end-to-end (skill hit, Battle Log updated, animation played, all captured on video):
1. Get references via reflection on the live `BattleHUDController` instance: its private `_self` field (attacker `BattleParticipant`) and `_enemyTargets` field (`List` of enemy `BattleParticipant`).
2. Resolve the `SkillData` by name from the project's `SkillDatabase` asset (`AssetDatabase.FindAssets("t:SkillDatabase")` → iterate `AllSkills`, match by `SkillName`).
3. Get `BattleManager.ResolveSkillAction` via `GetMethod(..., BindingFlags.NonPublic | BindingFlags.Instance)`, `Invoke` it (returns `IEnumerator`), then `battleManager.StartCoroutine(thatIEnumerator)` — `StartCoroutine` itself is public (inherited from `MonoBehaviour`), no reflection needed for that part.
4. Start the DevStudio recording *before* triggering, wait ~10s for windup + the built-in timed-input window (times out gracefully into a non-perfect hit if nothing presses it — doesn't hang) + resolve, then stop recording.
5. To get into a battle at all first: teleport the player onto an already-spawned `WildEncounterCreature` via `player.transform.position = target.transform.position` in `execute_code` — this still fires a real `OnTriggerEnter2D` contact (same as walking into it), just without needing working movement input either.

`execute_code`'s compiler here is Codedom (C# 6 only): no local functions, no tuple deconstruction in `foreach`, no `using` directives mid-snippet — use fully-qualified names and a manual stack/list-based traversal instead when this trips a compile error.

### 2D Rigging
1. Human: Sprite Editor → Skinning Editor → place bones, auto-generate mesh geometry, paint weights (Auto Weights as a start, Weight Brush/Slider to refine). One-time cost per species.
2. Once rigged, bones are GameObjects in a Transform hierarchy, bridged to the mesh via `Sprite Skin`. Everything from here is normal: Animation Clips, Animator Controller, or DOTween on individual bone Transforms — the same pattern Beat Sequence's placeholder tweening already uses, just targeting a bone instead of the whole sprite.
3. **Blocked on the Spine-vs-native decision** (Roadmap, already flagged, "deferred until custom art pipeline decision is made") — Spine is a separate third-party tool with its own GUI-only rigging step, not more Claude-Code-friendly than Unity's native Skinning Editor. This decision affects the whole art pipeline, not just skills, and should resolve before any rigging work starts.

### Sprite Shape
The art unit needed here is much smaller than rig art — not the whole effect, just a **repeatable segment**:
- An edge/fill sprite (a short strip — glowing energy, scales, rope texture) that tiles along the path's length
- Optionally a corner sprite (how it looks at sharp bends) and cap sprites (pointed tip vs. blunt end — useful for a bolt vs. a whip)

Flow: draw the small unit sprite(s) → create a **Sprite Shape Profile** asset (assigns sprites to angle ranges) → add a **Sprite Shape Controller** + the Profile to a GameObject → define the spline, either hand-placed in-editor (static shapes like a fixed zone-outline marker) or **entirely by script at runtime** (`spline.SetPosition()`, `InsertPointAt()` — for anything reacting to live gameplay: a lightning bolt between live attacker/target positions, a trail following a projectile's recent path) → apply a Material (Shader Graph or hand-coded) to the renderer like any other sprite.

### LineRenderer
Built-in, no package. Simpler than Sprite Shape when the effect doesn't need organic angle-based corner switching — a straightforward glowing beam or bolt. Reach for this before Sprite Shape unless the organic tiling specifically matters.

---

## Part 3 — Decision Guide

| Situation | Use |
|---|---|
| Static icon-style effect (basic projectile, impact burst) | Plain sprite, no shape tool needed |
| Organic path that bends, needs proper corner handling (whip, vine-like tendril, zone outline) | Sprite Shape |
| Simple straight/curved beam or bolt, no organic tiling need | `LineRenderer` |
| Creature body animation during attacks | 2D Rigging (blocked on Spine-vs-native) |

Not every skill needs a shape tool at all — see Family 1 below.

---

## Part 4 — Long-Term Scalability Principles

These determine whether authoring skill #40 is cheap or expensive:

1. **Data-driven VFX "recipes," not bespoke code per skill.** Mirrors how `SkillData` already works — VFX should be data fields (shape archetype, sprite/material reference, color, path parameters) read by a handful of generic components. Writing a new C# script per skill (`LightningBoltVfx.cs`, `FireballVfx.cs`) is the signal to stop and generalize instead.
2. **A small reusable component library**, not one component per skill — e.g. a generic path-driven controller (drives either Sprite Shape or `LineRenderer` off a config flag) and a generic projectile controller, each configured by many skills' data. Same principle as Beat Sequence: one state machine, many data-driven skills.
3. **Object pooling from day one**, not retrofitted later. Architecture_Directive scopes this as Phase 5, but for VFX specifically it matters earlier than that framing suggests — if the first few skills spawn/destroy ad hoc, every skill built after inherits that pattern, and pooling becomes a much bigger refactor once 20 skills exist than if the first controller is pool-aware from the start.
4. **Shared asset libraries, not per-skill duplicates.** A Sprite Shape Profile or Material is reusable — build a small shared set (e.g. "Electric_Bolt_Profile," "Ember_Trail_Material") many skills reference, rather than each skill getting its own copy.
5. **Path/shape math as pure, testable functions**, separate from MonoBehaviour/rendering glue — matching the existing discipline in `ZonePositionalPatternResolverTests.cs` (pure logic tested independent of Scene). "Given attacker and target positions, compute these spline control points" should be a plain, unit-testable function.
6. **Respect the Presentation-layer boundary** (Architecture_Directive Part 2) from the start. VFX code belongs in `Phasix.Presentation`, subscribing to `EventBus`/simulation state — never reaching into `BattleManager` internals directly. Easy to violate by accident; worth treating as a rule from the first VFX controller written, not a cleanup pass later.
7. **Verify with video, not a single screenshot, and always as a shared, viewable file.** Every effect in this pipeline is time-varying — a static frame can look right while the motion doesn't. Capture one full cycle via an OS-level screen-recording tool (external to Unity's own API, so it never depends on whether a given system exposes a scripting surface), save it, and present it in-session so both Claude and the human collaborator can watch the same clip — not just a text summary of what Claude privately observed. Full detail in Part 2's Standard Verification Workflow.

---

## Part 5 — Skill Execution & Resolution Lifecycle

Not every skill shares one lifecycle. Walking all 21 real built skills in `Assets/Data/Skills/` (see the summary table at the end of this Part) against the projectile lifecycle from earlier design discussion showed most don't fit that shape at all — there are at least **9 distinct families**, each with a different stage sequence and a different resolution type. This Part is the authority for "how does a skill actually execute and resolve," organized by family.

### Family 1 — Traveling Projectile
**Stages:** Windup → Launch → Travel → Arrival-wait → Hit/Dodge-Parry fork → optional lingering effect.
**Built skills using this shape:** none currently — see the flagged discrepancy in Part 6. This remains the reference shape for when one gets built.

**Worked example (preserved from the prior revision):**
1. **Prerequisite (Architecture_Directive Phase 3):** migrate the projectile off `CombatProjectileVisual`'s `Painter2D` drawing to a real Scene GameObject with `SpriteRenderer` — position driven by Transform tween (DOTween) instead of `SetProgress`.
2. **Shader:** plain orb/circle sprite texture + Material using the hand-coded dissolve/glow technique (Part 2) for spawn/impact dissolve.
3. **Pooling:** claimed from and returned to a pool (Part 4, principle 3), not `Instantiate`/`Destroy` per use — **and specifically two separate pools**, not one: a `ProjectileVfx` prefab (lives for the travel duration) and a separate `ImpactBurst` prefab (spawns once at impact, its own short lifecycle). Different lifetimes, different pools.
4. **Data-driven:** sprite, material/color, travel duration — fields on the skill's data asset, read by one generic `ProjectileVfxController`.
5. **Arrival-wait is a real, separate stage, not the same moment as impact.** `CombatProjectileVisual.cs`'s own code confirms this: `SetPulseScale` exists specifically because a projectile arriving and then sitting still while the defender's timed input is still resolving read as "stuck/broken" to players. There is a genuine gap between "visually arrived" and "outcome known."
6. **The fork is real, not cosmetic:** on a hit, impact burst + dissolve + status applied. On Dodge/Parry, the projectile does a vanish-fade instead (`SetAlpha`, also already-shipped code) — a Parry specifically also triggers the caster's automatic counter-attack per Combat_Directive's Dodge/Parry system, which is its own follow-up VFX beat, not just "nothing happens."
7. **Lingering effects run on a different clock entirely.** A status like Burn spreading to an adjacent lane fires on status *expiry*, inside the status/turn system — not part of the impact moment's VFX timing at all.

### Family 2 — Instant / Pre-Emptive
**Stages:** Windup (this *is* the read) → near-instant Resolve → Hit/Dodge-Parry fork. No meaningful Travel or Arrival-wait — `ResponseTiming: PreEmptive` means the outcome is read and reacted to *during* Windup, before anything resembling "impact" exists.
**Built skills:** `Ranged_InstantStrike`, `Ranged_Feint`, `Ranged_Jitter`, `Ranged_Metronome`.
**What's required beyond the base shape:** each of the three non-baseline variants needs a **visually distinct Windup treatment**, or the mechanic it's testing doesn't function —
- **Feint:** the fake tell must look nearly identical to the real one (that's the point) but distinguishable on close attention. Same beat, subtly different execution.
- **Jitter:** windup duration itself is randomized (`_windupJitterRangeSeconds`) — the visual must not accidentally telegraph the actual duration (e.g. a linear fill bar would leak the randomization; a glow/crackle intensity ramp doesn't).
- **Metronome:** the opposite requirement — visual must read as *consistent* across repeats within the same fight, since the whole point is that it's learnable.

### Family 3 — Charge & Release / Zone-Positional
**Stages:** Windup (charge buildup or zone-marking) → Resolve-in-place → optional lingering. **No Dodge/Parry fork at all.**
**Built skills:** `Ranged_ChargeRelease` ("Magma Burst"), and all five Zone/Positional skills (`ZoneRow`, `ZoneColumn`, `ZoneDiagonalX`, `ZoneBurst`, `ZoneArrowhead`).
**The gap this closes:** these resolve via **Lane Movement escape** (the defender moving out of marked lanes), not the timed-input Dodge/Parry system Family 1/2 use. This was never stated explicitly anywhere before — a skill in this family literally has no "Dodge/Parry fork" step in its lifecycle; substitute "did the defender's final lane position, checked at resolve time, fall inside the marked area" instead.

### Family 4 — Sustained / Hold
**Stages:** Windup → continuous Active-duration (not a discrete moment — potentially multiple tick-hits or one hold-duration-match check) → End/release.
**Built skills:** `Ranged_SustainedPressure` ("Flame Breath").
**The gap:** fully undocumented until now. This family has no discrete "impact frame" at all — VFX needs to represent an ongoing state (a beam/field actively present) rather than a single resolving event, closer in spirit to a status effect's visual than an attack's.

### Family 5 — Multi-Hit Volley
**Stages:** one shared Windup → **N repetitions** of [Launch → Travel → Arrival-wait → Hit/Dodge fork] → optional lingering after the final hit.
**Built skills:** `Ranged_MultiHitVolley` ("Volley"), `Ranged_MultiHitVolley_DoubleTap`, `Ranged_MultiHitVolley_Tracking`.
**The gap:** `Tracking Volley`'s name implies each shot may need to retarget mid-sequence if the defender moves (Lane Movement) between hits — the interaction between an in-progress multi-hit sequence and a defender's reactive repositioning is undesigned. Worth resolving before this specific asset gets real VFX.

### Family 6 — Melee (Beat Sequence)
**Stages:** Approach → Windup (real/fake) → Attack-resolve → automatic Return-to-origin.
**Built skills:** `Melee_Slash`.
**Not re-documented here** — `Attack_Pattern_Directive` Part 7 is the authority for this family. Listed for completeness so it isn't mistaken for a 10th undocumented gap.

### Family 7 — Self-Target
**Stages:** Windup (optional) → Resolve-on-self. No opponent involved at all — no Travel, no Arrival-wait, no Dodge/Parry fork.
**Built skills:** `Standard_Charge` (Aura restore), `Standard_Heal` (instant HP), `Standard_Regen` (HoT).
**Not a subset of Family 1/2** — genuinely simpler, but a distinct branch that needs stating rather than assuming.

### Family 8 — Pure Movement
**Not an attack lifecycle at all.** `Standard_Move` (repositions to a different formation slot) should never be routed through any of the above — its VFX is Lane Movement's own hop/traversal animation (Attack_Pattern_Directive Part 7's "Return is visible... hop" treatment), full stop.

### Family 9 — Capture
**Stages:** Windup (optional) → Resolve → **Capture-success / Capture-fail fork.**
**Built skill:** `Standard_Capture`.
**The gap:** a third resolution type distinct from both Hit/Dodge-Parry and Charge/Release's undodgeable resolve. Needs its own VFX language entirely — an envelop/containment effect, success reading as the target vanishing, failure reading as breaking free — none of which resembles a "hit."

### Quick-reference: every real skill, by family

| Skill | Family |
|---|---|
| `Melee_Slash` | 6 — Melee |
| `Ranged_InstantStrike`, `Ranged_Feint`, `Ranged_Jitter`, `Ranged_Metronome` | 2 — Instant/Pre-Emptive |
| `Ranged_ChargeRelease` | 3 — Charge & Release / Zone-Positional |
| `Ranged_ZoneRow`, `ZoneColumn`, `ZoneDiagonalX`, `ZoneBurst`, `ZoneArrowhead` | 3 — Charge & Release / Zone-Positional |
| `Ranged_SustainedPressure` | 4 — Sustained/Hold |
| `Ranged_MultiHitVolley`, `_DoubleTap`, `_Tracking` | 5 — Multi-Hit Volley |
| `Standard_Attack` | Likely 1 or 2 — not yet confirmed which; simplest built-in case |
| `Standard_Charge`, `Standard_Heal`, `Standard_Regen` | 7 — Self-Target |
| `Standard_Move` | 8 — Pure Movement |
| `Standard_Capture` | 9 — Capture |

Nothing currently maps to Family 1 — the flagged discrepancy in Part 6.

---

## Part 6 — Open Items

1. **Discrepancy: `DOCUMENT_INDEX.md` claims "Direct Projectile" is built, but no asset matches Family 1's shape.** Either the baseline pre-existing system (predating this design work) counts as the implementation and never got a dedicated placeholder asset, or the index entry is stale. Needs verifying against the actual codebase/DOCUMENT_INDEX maintainer before Family 1's worked example is treated as validated rather than aspirational.
2. **Blocked on Architecture_Directive Phase 3** — nothing in Part 5 is buildable until creatures/projectile are real Scene GameObjects.
3. **Spine vs Unity 2D Animation** (Roadmap, still open) — blocks all 2D Rigging work specifically, not the rest of this doc.
4. **Exact VFX "recipe" schema on `SkillData`** — not designed yet; Part 4 principle 1 states the goal, not the field list.
5. **Component names throughout are proposed, not committed** (`ProjectileVfxController`, etc.) — naming/exact API surface is an implementation-time decision.
6. **Family 3's Lane-Movement-based resolution** needs its exact trigger point specified — Part 5 states *that* it's different from Dodge/Parry, not the precise mechanical check.
7. **Family 5's mid-volley retargeting** (Tracking Volley) is unresolved — flagged in Part 5, needs a design decision before that asset's VFX is buildable.
8. **`Standard_Attack`'s family is unconfirmed** — likely Family 1 or 2, not verified against its actual resolution behavior.

---

## Status
Design capture, not a locked spec. Depends on Architecture_Directive's migration phases landing before any of this is real.
