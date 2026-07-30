# Phasix — MCP Agent Context Guide
**Version:** 1.3.0 · July 2026

> Unity MCP bridge is CoplayDev/unity-mcp (migrated from AnkleBreaker July 2026 — see
> DECISIONS.md → [Tooling]). Load this file by reading it directly (Claude Code has
> filesystem access); no Unity round-trip tool call needed. For live Unity status, read the
> `mcpforunity://editor/state` resource.

---

## Project Summary
2D top-down Monster Tamer RPG. Digimon-style branching evolution web. Creatures called **Phasix** — crystallizations of emotional states. Player captures, raises, and evolves Phasix in an emotional dimension mirroring lived human experience.
- Unity 6000.x LTS · 2D URP · 320×180 Pixel Perfect Camera
- Tilemap world (NOT 3D terrain) · A* Pathfinding Project (NOT Unity NavMesh)
- Asset Store art pipeline · No custom art yet

---

## Hard Architecture Rules (enforce every feature)
- **ScriptableObjects = read-only at runtime.** Never write to SO during play. Runtime state → plain C# → JSON.
- **No heavy logic in Update().** Distance checks, AI, pathfinding → timers, coroutines, event callbacks.
- **Event-driven over polling.** C# `event`/`Action` delegates or lightweight EventBus.
- **Object pooling** for anything that spawns/despawns. Never `Instantiate`/`Destroy` in a loop.
- **World = GameObject Chunks.** `SetActive(false)` for distant chunks — never destroy them.
- **Pathfinding = A* Pathfinding Project (free/Lite).** Not Unity NavMesh.

## Code Style
```csharp
[SerializeField] private float _moveSpeed = 5f;   // private fields: _camelCase
[Header("Movement")]                                // group with Header
[Tooltip("Units per second")]                       // explain non-obvious fields
[RequireComponent(typeof(Rigidbody2D))]            // declare dependencies explicitly
// Cache in Awake(), never in Update()
// Input → Update()   Physics → FixedUpdate()
// Public members: PascalCase
```

---

## What's Already Built
| System | Script | Location | Status |
|---|---|---|---|
| Player movement (side-scroll variant) | `PlayerController_SideScroll.cs` | `Assets/Scripts/Player/` | ✅ Done |
| World chunk management | `WorldChunkManager.cs` | `Assets/Scripts/World/` | ✅ Done |
| Tilemap world | Ground/Walls/Decorations tilemaps | SampleScene → Grid | ✅ Done |
| Cinemachine follow camera | CinemachineCamera + Confiner2D | SampleScene | ✅ Done |
| Pixel Perfect Camera | 320×180 PPU on Main Camera | SampleScene | ✅ Done |
| Sprite setup editor tool | `PhasixSpriteSetup.cs` | `Assets/Scripts/Editor/` | ✅ Done |
| Animator generator tool | `PhasixAnimatorGenerator.cs` | `Assets/Scripts/Editor/` | ✅ Done |
| 2D IK foundation (arms) | `IKManager2D` + 2× `LimbSolver2D` | SampleScene → Mr_chimken/IK | ✅ Done |
| EventBus | `EventBus.cs` | `Assets/Scripts/Core/` | ✅ Phase 2 Kickoff |
| GameManager skeleton | `GameManager.cs` | `Assets/Scripts/Core/` | ✅ Phase 2 Kickoff |
| GameStrings constants | `GameStrings.cs` | `Assets/Scripts/Core/` | ✅ Phase 2 Kickoff |
| BattleResult stub | `BattleResult.cs` | `Assets/Scripts/Core/` | ✅ Stub (Phase 3 pending) |
| PhasixData SO (species/form template) | `PhasixData.cs` | `Assets/Scripts/Creatures/` | ✅ Phase 2 Wk 9 |
| PhasixRuntimeData (per-individual state) | `PhasixRuntimeData.cs` | `Assets/Scripts/Creatures/` | ✅ Phase 2 Wk 9 |
| Supporting types (StatType, BondZone, Temper, OriginType, TempoType, SignalType, Personality, SkillTreeType, PrimalType, StatBlock, EvolutionHistoryEntry) | `PhasixEnums.cs`, `PrimalType.cs`, `StatType.cs`, `BondZone.cs`, `StatBlock.cs`, `EvolutionHistoryEntry.cs` | `Assets/Scripts/Creatures/` | ✅ Phase 2 Wk 9 |
| SkillData stub | `SkillData.cs` | `Assets/Scripts/Creatures/` | ✅ Stub (full skill content pending roster) |

**Active scene:** SampleScene
**Player object:** `Mr_chimken` (placeholder character, bone rig, left-facing → `_rigFacesRight = false`)

> **IK hierarchy on Mr_chimken** (added April 2026)
> ```
> Mr_chimken  ← IKManager2D (runInEditMode=true, weight=1)
>   body/shoulder_R/forearm_R/IK_Tip_Arm_R   ← effector (localPos offset = forearm.localPos.x)
>   body/shoulder_L/forearm_L/IK_Tip_Arm_L   ← effector
>   IK/IK_Target_Arm_R   ← move this to drive right arm (LimbSolver2D, solveFromDefaultPose=false)
>   IK/IK_Target_Arm_L   ← move this to drive left arm
> ```
> **IK gotchas — read LESSONS_LEARNED.md §2D IK before modifying:**
> - Tip bones MUST have non-zero `localPosition` or the solve silently fails
> - Use `UpdateIK(List<Vector3>, float)` overload when forcing solves from code
> - `solveFromDefaultPose` must be `false` unless `StoreLocalRotations()` was called first

---

## Folder Structure
```
Assets/
  Scripts/
    Core/        ← GameManager, EventBus, BattleResult (stub); SaveManager (pending)
    Player/      ← PlayerController, PlayerController_SideScroll
    Creatures/   ← PhasixData, PhasixRuntimeData, StatBlock, EvolutionHistoryEntry,
                   PhasixEnums, PrimalType, StatType, BondZone, SkillData (stub) — done;
                   BondSystem, EvolutionManager, CompanionAI (pending)
    Evolution/   ← EvolutionEvaluator, Executor, Pathfinder, WebController (pending)
    Combat/      ← BattleManager, SkillSystem, StatusEngine, DamageCalculator (pending)
    World/       ← WorldChunkManager, EncounterTrigger, ZoneManager (pending)
    UI/          ← HUD, PartyScreen, SkillTreeUI, BondDisplay (pending)
    Audio/       ← AudioManager (pending)
    Save/        ← SaveSystem, SaveData (pending)
    Editor/      ← PhasixSpriteSetup, PhasixAnimatorGenerator
  Data/
    Species/     ← PhasixData SOs (placeholder, no roster yet)
    Skills/      ← SkillData SOs (pending roster)
    Items/       ← ItemData SOs (pending §22)
    EvolutionBranches/ ← EvolutionBranchData SOs (pending)
    TypeCharts/  ← PrimalTypeChart SO (8×8 multiplier table, pending)
    Aura/        ← AuraTypeData SOs (pending)
  MCP/
    Context/     ← This file (read directly by Claude Code, no Unity round-trip)
```

---

## Applicable MCP Tools for Phasix (by priority)

Rewritten July 2026 against CoplayDev/unity-mcp's actual tool catalog (verified live via
the `tool-groups` resource) — the previous version described AnkleBreaker's tool
categories, which no longer apply. All groups below were observed already active in this
project; if a tool is ever missing, `manage_tools(action='activate', group='<name>')`
turns its group on.

### 🔴 Core — Used Every Session (always on, 25 tools)
`manage_scene`, `manage_gameobject`, `manage_prefabs`, `manage_asset`, `manage_components`,
`manage_material`, `manage_camera`, `manage_physics`, `manage_editor` (play mode, tags,
layers, undo/redo), `manage_build`, `manage_packages`, `create_script`, `manage_script`,
`script_apply_edits`, `apply_text_edits`, `delete_script`, `validate_script`, `find_gameobjects`,
`find_in_file`, `get_sha`, `read_console`, `refresh_unity`, `execute_menu_item`, `batch_execute`

### 🔴 High Priority (Phase 2+)
- **`manage_scriptable_object`, `execute_code`** (scripting_ext group) — every Creatures/Data
  SO: PhasixData, SkillData, and future EvolutionBranchData/AuraTypeData/PrimalTypeChart assets
- **`manage_animation`** — Phasix animator controllers, battle transitions, UI animation
- **`manage_ui`** (UI Toolkit — UXML/USS/UIDocument) — battle UI, stat allocation UI, bond
  display, evolution menu
- **`unity_docs`, `unity_reflect`** (docs group) — verify Unity APIs live before writing
  code; training data may be stale, use before assuming a class/member exists

### 🟡 Medium Priority (specific systems)
- **`manage_shader`, `manage_texture`, `manage_vfx`** (vfx group) — aura glow, type-hit
  flash, evolution/devolution effects, 16 PPU pixel art import. Confirm whether `manage_vfx`
  targets VFX Graph or legacy Particle System before using it for 2D effects — not yet
  verified which this project's setup prefers.
- **`run_tests`, `get_test_job`** (testing group) — EditMode tests for damage formula/bond
  math once those systems exist
- **`manage_profiler`** (profiling group) — perf/memory work, low priority until real
  content exists to profile

### 🟢 Situational
- **`generate_audio`, `generate_image`, `generate_model`, `import_model`,
  `import_model_file`** (asset_gen group) — AI-assisted generation/import, bring-your-own-key.
  CLAUDE.md's stated pipeline is Asset Store art, not AI-generated — treat this group as a
  prototyping/placeholder option, not the primary art pipeline, unless told otherwise.

### ❌ NOT APPLICABLE — Never suggest these for Phasix
- **`manage_probuilder`** — 3D modeling; Phasix is 2D Tilemap, no 3D level geometry
- **NavMesh** — not in this MCP's tool catalog at all; project uses A* Pathfinding Project
- **MPPM** — no multiplayer in this project
- **UMA** — custom pixel-art sprites, not UMA humanoid generation
- **LOD** — 2D game, not applicable

---

## What Is Pending — Scaffold Only
Flag all pending work with `// TODO: pending design — [topic]`

- Species roster — no Phasix designed; use placeholder SOs
- Skill content — taxonomy locked, individual skills TBD
- `GameStrings.PoolName` — unnamed pool UI label TBD; reference this constant in all UI strings, never hardcode
- All NumericalCalibration.md values — pending calibration session
- Hub count/identity, realm count/emotional identities — pending world design
- Main quest narrative — pending story session
- Economy/items (§22), NPC/dialogue (§24), survival/crafting (§20) — all pending
- Celestial properties — per-species, pending roster
- Signal interaction multiplier values — logic locked, numbers pending

---

## UI String Convention
Never hardcode pending player-facing display names. Always reference `GameStrings` constants:

| Constant | Value (placeholder) | Pending |
|---|---|---|
| `GameStrings.PoolName` | `[POOL_NAME]` | Unnamed pool player-facing label — pending naming session |
| `GameStrings.SensitivityName` | `[SENSITIVITY_NAME]` | Sensitivity-haver player-facing term — pending naming session |

**When a name is decided:** update the constant value in `Assets/Scripts/Core/GameStrings.cs` — the entire game updates automatically with no find-replace required.

---

## What's Still Manual (User Does These in Unity)
| Task | Reason |
|---|---|
| Tile painting | Tilemap Brush is mouse-paint only |
| Animation curve feel/timing | Needs visual preview |
| PlayMode feel testing | Game feel = hands only |
| Art import (drag/drop) | OS → Project window |
| Cinemachine path splines | Visual spline handles |
| A* Pathfinding grid painting | Visual grid graph |
| Scene artistic composition | Creative eye only |

---

## Key Reference Docs
```
Assets/Docs/DOCUMENT_INDEX.md                       ← Read first
Assets/Docs/ClaudeCode_Primer_v1_1_0.md             ← Full spec
Assets/Docs/GDD_CreatureRPG_v0_8_0.html              ← Master GDD — Primal/Signal/Origin/
                                                        Tempo/Personality/Skill Tree systems
Assets/Docs/Evolution_System_Directive_v1_1_0.md    ← Supersedes GDD §3 (full .md mirror of
                                                        the canonical .pdf; has internal
                                                        inconsistencies — see DECISIONS.md)
Assets/Docs/Progression_Directive_v0_1_0.md         ← Supersedes GDD §21
Assets/Docs/Combat_Directive_v0_1_0.md              ← Combat + 7-lane stage
Assets/Docs/WorldDesign_Directive_v0_1_0.md         ← World, calendar, factions
Assets/Docs/NumericalCalibration.md                 ← All pending numerical values
Assets/Docs/DECISIONS.md                            ← Implementation decisions not in GDD —
                                                        includes Creatures architecture
                                                        rationale + unbuilt future-systems notes
Assets/Docs/LESSONS_LEARNED.md                      ← Debugging traps hit before — check first
Assets/Docs/CHANGELOG.md                            ← Session log
Assets/Docs/KNOWN_ISSUES.md                         ← Active bugs
```
