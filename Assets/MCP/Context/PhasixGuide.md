# Phasix — MCP Agent Context Guide
**Version:** 1.0.0 · April 2026
**Loaded automatically via `unity_get_project_context` at the start of every planning session.**

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

**Active scene:** SampleScene
**Player object:** `Mr_chimken` (placeholder character, bone rig, left-facing → `_rigFacesRight = false`)

---

## Folder Structure
```
Assets/
  Scripts/
    Core/        ← GameManager, EventBus, SaveManager (pending)
    Player/      ← PlayerController, PlayerController_SideScroll
    Creatures/   ← PhasixData, BondSystem, EvolutionManager, CompanionAI (pending)
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
    Context/     ← This file (loaded by unity_get_project_context)
```

---

## Applicable MCP Tools for Phasix (by priority)

### 🔴 Core — Used Every Session
Scripts, Components, GameObjects, Prefabs, ScriptableObjects, Console, Compilation errors, Scene management

### 🔴 High Priority (Phase 2+)
- **Animation (24 tools)** — Phasix animator controllers, battle transition, UI animations
- **Input System (8 tools)** — Action commands (timed battle inputs), battle/hub menu input
- **Tags & Layers (5 tools)** — 7-lane sorting layers (Lane1–Lane7), physics layer matrix
- **Textures (5 tools)** — 16 PPU pixel art import on every new art asset
- **UI (5 tools)** — Battle UI, stat allocation UI, bond display, evolution menu

### 🟡 Medium Priority (specific systems)
- **Physics (6 tools)** — collision matrix, lane raycasts for battle targeting
- **Sprite Atlas (7 tools)** — creature/UI sprite batching (Phase 4+)
- **Shader Graph (14 tools)** — aura glow, type hit flash, evolution/devolution effects
- **Particle System (6 tools)** — action command feedback, aura drop VFX, evolution sequence
- **Lighting (5 tools)** — per-realm emotional atmosphere (2D Light2D)
- **Audio (3 tools)** — AudioManager, Signal Type cues, action command SFX
- **Assembly Definitions (8 tools)** — Combat/Creatures/World/UI/Core/Save/Editor asmdefs
- **Settings (9 tools)** — URP renderer, Physics2D (zero gravity top-down), time step

### 🟢 Low Priority (optimization/dev tooling)
Profiler & Memory, Debugger, Testing (EditMode tests for damage formula/bond math)

### ❌ NOT APPLICABLE — Never suggest these for Phasix
- **Terrain** — 2D Tilemap world, no 3D terrain
- **NavMesh** — using A* Pathfinding Project
- **MPPM** — no multiplayer
- **UMA** — custom pixel art sprites
- **Amplify Shader** — using URP Shader Graph
- **LOD** — 2D game
- **VFX Graph** — Particle System preferred for 2D

---

## What Is Pending — Scaffold Only
Flag all pending work with `// TODO: pending design — [topic]`

- Species roster — no Phasix designed; use placeholder SOs
- Skill content — taxonomy locked, individual skills TBD
- `[POOL_NAME]` — unnamed pool UI label TBD; use token in all UI strings
- All NumericalCalibration.md values — pending calibration session
- Hub count/identity, realm count/emotional identities — pending world design
- Main quest narrative — pending story session
- Economy/items (§22), NPC/dialogue (§24), survival/crafting (§20) — all pending
- Celestial properties — per-species, pending roster
- Signal interaction multiplier values — logic locked, numbers pending

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
Assets/Docs/Evolution_System_Directive_v1_1_0.pdf   ← Supersedes GDD §3
Assets/Docs/Progression_Directive_v0_1_0.md         ← Supersedes GDD §21
Assets/Docs/Combat_Directive_v0_1_0.md              ← Combat + 7-lane stage
Assets/Docs/WorldDesign_Directive_v0_1_0.md         ← World, calendar, factions
Assets/Docs/NumericalCalibration.md                 ← All pending numerical values
Assets/Docs/CHANGELOG.md                            ← Session log
Assets/Docs/KNOWN_ISSUES.md                         ← Active bugs
```
