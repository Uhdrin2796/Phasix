# Phasix — MCP Agent Context Guide
**Version:** 1.8.2 · 2026-08-09

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
| Player movement (top-down, Sprint + corner correction) | `PlayerTopDownController.cs` | `Assets/Scripts/Player/` | ✅ Done (Sprint/corner correction: AUD-005/007) |
| Camera lookahead proxy | `CameraFollow.cs` | `Assets/Scripts/Player/` | ✅ Done (AUD-006) |
| World chunk management | `WorldChunkManager.cs` | `Assets/Scripts/World/` | ✅ Done |
| Wild spawn point marker | `EncounterTrigger.cs` | `Assets/Scripts/World/` | ✅ Done (Wk 14-16) |
| Tilemap world | Ground/Walls/Decorations tilemaps | SampleScene → Grid | ✅ Done |
| A* GridGraph (60×38 nodes, `Obstacles` layer, `scanOnStartup`) | `AstarPath` MonoBehaviour | SampleScene → `A* Pathfinding` | ✅ Baked |
| Cinemachine follow camera (via `CameraFollow` lookahead proxy) | CinemachineCamera + Confiner2D | SampleScene | ✅ Done |
| Pixel Perfect Camera | 320×180 PPU on Main Camera | SampleScene | ✅ Done |
| Sprite setup editor tool | `PhasixSpriteSetup.cs` | `Assets/Scripts/Editor/` | ✅ Done |
| Animator generator tool | `PhasixAnimatorGenerator.cs` | `Assets/Scripts/Editor/` | ✅ Done |
| 2D IK foundation (arms) | `IKManager2D` + 2× `LimbSolver2D` | SampleScene → Mr_chimken/IK | ✅ Done |
| EventBus | `EventBus.cs` | `Assets/Scripts/Core/` | ✅ Done — includes `OnWildEncounterTriggered`/`Fled`/`EngageRequested` |
| GameManager (boot-load/fallback-seed owner, on `SceneManager.sceneLoaded`) | `GameManager.cs` | `Assets/Scripts/Core/` | ✅ Done (2026-08, absorbed DebugPartyBootstrap, see DECISIONS.md → [Core]) |
| GameStrings constants | `GameStrings.cs` | `Assets/Scripts/Core/` | ✅ Phase 2 Kickoff |
| BattleResult | `BattleResult.cs` | `Assets/Scripts/Core/` | ✅ Done — real class (`Victory`, `PlayerParticipants`, `EnemyParticipants`), built by `BattleManager.EndBattle` |
| PhasixData SO (species/form template) | `PhasixData.cs` | `Assets/Scripts/Creatures/` | ✅ Phase 2 Wk 9 |
| PhasixRuntimeData (per-individual state) | `PhasixRuntimeData.cs` | `Assets/Scripts/Creatures/` | ✅ Phase 2 Wk 9 |
| BondSystem (floor logic, session loss cap, 60/80% damping, 100% immunity) | `BondSystem.cs` | `Assets/Scripts/Creatures/` | ✅ Done — 7 EditMode tests, `Assets/Tests/EditMode/` |
| PersonalitySystem (capture-time roll + item-based swap) | `PersonalitySystem.cs` | `Assets/Scripts/Creatures/` | ✅ Done |
| PersonalityStatModifier (18-trait → StatType nudge table, GDD §7.3) | `PersonalityStatModifier.cs` | `Assets/Scripts/Creatures/` | ✅ Done |
| PrimalTypeColor (8 base + 28 duo-merge placeholder colors) | `PrimalTypeColor.cs` | `Assets/Scripts/Creatures/` | ✅ Done |
| PhasixPlaceholderVisual (placeholder-first Body/Underglow tint + effects) | `PhasixPlaceholderVisual.cs` | `Assets/Scripts/Creatures/` | ✅ Done |
| CompanionAI (7 movement patterns: Direct/Wavy/DashThrough/StopAndGo/Orbit/HiddenShadow/Blink) | `CompanionAI.cs` | `Assets/Scripts/Creatures/` | ✅ Done |
| PartySystem (up to 3 slots, single re-skinned active companion instance) | `PartySystem.cs` | `Assets/Scripts/Creatures/` | ✅ Done (Wk 12-13) |
| WildSpawnSystem (builds `PhasixRuntimeData` for a wild encounter) | `WildSpawnSystem.cs` | `Assets/Scripts/Creatures/` | ✅ Done (Wk 14-16) |
| WildEncounterCreature (contact detection auto-engages straight to battle; Patrol/Alert state machine) | `WildEncounterCreature.cs` | `Assets/Scripts/Creatures/` | ✅ Done (Patrol/Alert: AUD-005) — 2026-08-10: contact no longer shows a Flee/Engage prompt, auto-engages via `HandleEngage` (Flee moved into the battle itself, see BattleManager); `s_encounterInProgress` static guard added to prevent two concurrent battles from overlapping contacts, see LESSONS_LEARNED.md → [Combat & Encounter Flow] |
| DebugMovementPresetCycler (Tab-cycle companion movement presets in Play mode) | `DebugMovementPresetCycler.cs` | `Assets/Scripts/Creatures/` | ⚠️ Debug tool, keep for now |
| SpeciesDatabase (GUID-index lookup, mirrors SkillDatabase) | `SpeciesDatabase.cs` | `Assets/Scripts/Creatures/` | ✅ Done (2026-08, save/load species resolution) |
| SkillLoadoutSystem (equip/unequip/swap, tier-capped, unlockedTreeTypes-gated, positional/sparse slots) | `SkillLoadoutSystem.cs` | `Assets/Scripts/Creatures/` | ✅ Done (2026-08, Party menu skill ring; unlockedTreeTypes gate + sparse positional slots added 2026-08-09 — see DECISIONS.md → [Combat]) |
| SkillTreeUnlockSystem (bond-gated Type F/O unlocks + GetEffectiveUnlockedTrees) | `SkillTreeUnlockSystem.cs` | `Assets/Scripts/Combat/` | ✅ Done — `GetEffectiveUnlockedTrees` (2026-08-09) is the single source of truth for both the skill web view's display and SkillLoadoutSystem's equip gate: DebugUnlockAllTrees > DebugTierOverride > real unlockedTreeTypes, in priority order |
| SkillTreeColor (per-tree color: DisplayOrder/Get/ApplyVisual) | `SkillTreeColor.cs` | `Assets/Scripts/Combat/` | ✅ Done (2026-08-09) — the ONE shared color source for the skill web, Party menu equip wheel, AND BattleHUDController's skill ring; see DECISIONS.md → [Combat] |
| SaveSystem + PhasixSaveData/PartySaveData/SaveFile DTOs | `SaveSystem.cs` etc. | `Assets/Scripts/Save/` | ✅ Done (2026-08, real `Application.persistentDataPath` persistence, 3 slots, auto-continue by newest write time) |
| Supporting types (StatType, BondZone, Temper, OriginType, TempoType, SignalType, Personality, SkillTreeType, PrimalType, StatBlock, EvolutionHistoryEntry) | `PhasixEnums.cs`, `PrimalType.cs`, `StatType.cs`, `BondZone.cs`, `StatBlock.cs`, `EvolutionHistoryEntry.cs` | `Assets/Scripts/Creatures/` | ✅ Phase 2 Wk 9 |
| SkillData stub (+ BuiltInMoveType field, 2026-08) | `SkillData.cs` | `Assets/Scripts/Creatures/` | ✅ Stub (full skill content pending roster) |
| EncounterPromptController (first UI Toolkit screen — Flee/Engage prompt) | `EncounterPromptController.cs` | `Assets/Scripts/UI/` | ⚠️ Dead code (2026-08-10) — WildEncounterCreature no longer calls Show(); contact auto-engages instead (Flee moved into the battle, see BattleManager). Left in place, not deleted, this pass — flagged as a cleanup follow-up. |
| HudTooltip (shared runtime hover tooltip, extracted from BattleHUDController) | `HudTooltip.cs` | `Assets/Scripts/UI/` | ✅ Done (2026-08, shared by battle + Party menu; screen-edge clamping added 2026-08-09 — flips left/clamps vertically instead of always placing right of the anchor, fixing off-screen tooltips near the panel's right edge, e.g. the enemy nameplate) |
| OverworldMenuController (Tab-key Party/Save/Bag/Options menu, replaces PartyMenuController) | `OverworldMenuController.cs` | `Assets/Scripts/UI/` | ✅ Done (2026-08, see DECISIONS.md → [UI]) — Party detail view's skill tray is now a pan/zoom skill web (2026-08-09, replaced the paged carousel), with a debug tier stepper (`PhasixRuntimeData.DebugTierOverride`). Always-visible `DebugBar` also has a "DEBUG: Add Party Member" button (2026-08-10, spawns `Test_SteamType` via `WildSpawnSystem.CreateWildInstance` into `PartySystem`) |
| SkillWebEdgeVisual (Painter2D edge/glow overlay for the skill web) | `SkillWebEdgeVisual.cs` | `Assets/Scripts/UI/` | ✅ Done (2026-08-09), same `DragLineVisual` convention |
| EditMode test assembly (7 `BondSystem` tests) | `BondSystemTests.cs` + `Phasix.Tests.EditMode.asmdef` | `Assets/Tests/EditMode/` | ✅ Done (AUD-012) — first test assembly in the project |

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
    Phasix.Runtime.asmdef  ← covers everything below except Editor/ (added AUD-012 fix —
                              references AstarPathfindingProject + Unity.InputSystem; a custom
                              asmdef CANNOT reference "Assembly-CSharp" by name, see
                              LESSONS_LEARNED.md → [Tooling])
    Core/        ← GameManager (boot-load/fallback-seed owner, absorbed DebugPartyBootstrap
                   2026-08 — see DECISIONS.md → [Core]), EventBus (+ wild-encounter events),
                   GameStrings, BattleResult (real class, not a stub — see Combat/)
    Player/      ← PlayerTopDownController (Sprint, corner correction), CameraFollow
                   (lookahead proxy)
    Creatures/   ← PhasixData, PhasixRuntimeData, BondSystem, PersonalitySystem,
                   PersonalityStatModifier, PrimalTypeColor, PhasixPlaceholderVisual,
                   CompanionAI, PartySystem, WildSpawnSystem (incl. skill-seeding, 2026-08),
                   WildEncounterCreature, CaptureSystem, AuraStatAllocationSystem,
                   AuraTierCeiling, SpeciesDatabase (2026-08, GUID-index, mirrors
                   SkillDatabase), SkillLoadoutSystem (2026-08, equip/unequip/swap),
                   DebugMovementPresetCycler (debug tool), StatBlock, EvolutionHistoryEntry,
                   PhasixEnums, PrimalType, StatType, BondZone, SkillData (stub, + 2026-08
                   structural wiring fields — see Combat/) — all done; EvolutionManager
                   (pending — see note below). DebugPartyBootstrap DELETED 2026-08 (absorbed
                   into GameManager, per its own "delete once superseded" doc comment).
    Combat/      ← ✅ Phase 3 Gate, live in BattleScene_Main (was "not yet created" through
                   2026-08-04 — corrected 2026-08-07). BattleManager (live turn loop),
                   BattleEngine, BattleHUDController, BattleParticipant, BattleState/Action/
                   ActionResult/Outcome/Transition/Config, DamageCalculator, PrimalTypeChart,
                   EvolutionBurstSystem/Gauge, CaptureSystem (wired), StatusEffectCatalog/
                   Category/Type, ChainResultCatalog/Type, MasteryBonusCatalog/Type,
                   ComboEngine/Tier, ComboRuleType/ComboRuleEvaluator (2026-08, new — NOT GDD
                   content, see DECISIONS.md), SkillTreeCatalog/UnlockSystem, SkillSlotCapacity,
                   TimedInputConfig, PlaceholderSkillResolver/SkillDatabase/ChosenMove/
                   ActiveStatusInstance (2026-08, skill-ring wiring), BuiltInMoveType (2026-08
                   follow-up — Attack/Charge/Heal/Regen/Capture became real, equippable
                   Standard-tree SkillData instead of a hardcoded wheel, see DECISIONS.md),
                   BattleLaneLayout/
                   StageGizmos (dev-only), RadialGaugeVisual/RingVisual/DragLineVisual (UI
                   Toolkit custom elements), BattleLogFormatter. No `Evolution/` folder exists
                   — Evolution Burst (mid-battle) lives here; a full species-evolution-tree
                   system per Evolution_System_Directive is still pending (Phase 4).
    World/       ← WorldChunkManager, EncounterTrigger (now skill-database-aware, 2026-08);
                   ZoneManager (pending)
    UI/          ← EncounterPromptController (first UI Toolkit screen); BattleHUDController
                   (see Combat/); BattleSummaryController (2026-08, read-only post-battle recap
                   — Aura gained/damage dealt/healing done, NOT where Aura is spent);
                   HudTooltip (2026-08, shared runtime hover tooltip, extracted from
                   BattleHUDController so battle and the Party menu use the identical
                   behavior); OverworldMenuController (2026-08, Tab-key Party/Save/Bag/Options
                   menu — see DECISIONS.md -> [UI]); SkillWebEdgeVisual (2026-08-09, Painter2D
                   edge/glow overlay for the Party menu's skill web — see that class's own doc
                   comment). Old PartyMenuController (single-purpose Aura-spend screen) and
                   AuraAllocationController (same-session predecessor to BattleSummaryController)
                   both deleted, fully superseded. The Skyrim-style paged skill tree carousel
                   (shipped 2026-08-08, closed KNOWN_ISSUES UI-001) was itself replaced 2026-08-09
                   by the pan/zoom skill web — no code from that carousel remains. BondDisplay
                   (pending)
    Audio/       ← AudioManager ← Phase 3, not yet created
    Save/        ← SaveSystem, PhasixSaveData, PartySaveData, SaveFile ← ✅ Done (2026-08) —
                   real Application.persistentDataPath persistence, 3 manual slots,
                   auto-continue by newest file write time. No longer "not yet created."
    Editor/      ← PhasixSpriteSetup, PhasixAnimatorGenerator; own
                   Phasix.Editor.asmdef (Editor-only, references Unity.2D.Sprite.Editor —
                   needed once split out of the implicit default assembly)
  Tests/
    EditMode/    ← Phasix.Tests.EditMode.asmdef (references Phasix.Runtime, not
                   Assembly-CSharp) — 256 tests across 23 files (2026-08-09: +10 tree-lock-gate/
                   GetEffectiveUnlockedTrees cases, then +7 more for sparse/positional equip-slot
                   behavior and DebugUnlockAllTrees): AuraStatAllocationSystemTests,
                   BattleEngineTests, BattleLogFormatterTests, BattleParticipantTests,
                   BondSystemTests, CaptureSystemTests, ChainResultCatalogTests,
                   ComboEngineTests, ComboRuleEvaluatorTests, DamageCalculatorTests,
                   EvolutionBurstSystemTests, MasteryBonusCatalogTests,
                   PlaceholderSkillResolverTests, PrimalTypeChartTests,
                   ResonanceBonusEvaluatorTests, SaveSystemTests (2026-08, new),
                   SkillDatabaseTests, SkillLoadoutSystemTests (2026-08, new),
                   SkillSlotCapacityTests, SkillTreeUnlockSystemTests,
                   StatusDurationCalculatorTests, StatusEffectCatalogTests,
                   TimedInputConfigTests
  Prefabs/
    Creatures/   ← Phasix_Placeholder.prefab (companion — Rigidbody2D/Seeker/AIPath/
                   CompanionAI), Phasix_WildEncounter.prefab (stationary-spawn variant, now
                   with a Kinematic Rigidbody2D for Patrol/Alert movement — AUD-005). Both:
                   Body/Underglow sprites (foot-anchored CircleCollider2D — AUD-002) + Shadow
                   child (AUD-003)
  Sprites/       ← Shadow_Ellipse.png, AlertIcon.png — both procedurally generated
                   placeholder-first sprites (soft gradient shadow, wild-creature Alert
                   indicator), added AUD-003/005
  Scenes/        ← SampleScene.unity (overworld, always loaded), BattleScene_Main.unity
                   (battle — additively loaded via BattleTransition.StartWildBattle, unloaded
                   at battle end; overworld stays loaded underneath)
  Data/
    Species/     ← PhasixData SOs — Test_FireType, Test_SteamType (placeholder, no real
                   roster yet; both EvolutionTier=1, AvailableTreeTypes set for skill-seeding),
                   + SpeciesDatabase.asset (2026-08, GUID-index for save/load species resolution)
    Skills/      ← 95 placeholder SkillData assets (5 per GDD SkillTreeType as of 2026-08-09, up
                   from 2 — "add more placeholders... to see what it could look like at scale,"
                   see DECISIONS.md → [Creatures] — real but generic, "Do not treat as real skill
                   content"), + SkillDatabase.asset (2026-08, resolves equipped/learned GUIDs to
                   real SkillData at runtime)
    Items/       ← ItemData SOs (pending §22)
    EvolutionBranches/ ← EvolutionBranchData SOs (pending)
    TypeCharts/  ← PrimalTypeChart SO + PrimalTypeChart.asset — ✅ wired into BattleManager,
                   no longer pending
    Aura/        ← AuraTypeData SOs (pending)
  UI/            ← EncounterPrompt.uxml/.uss/PanelSettings (320×180 reference resolution,
                   matches the Pixel Perfect Camera); BattleHUD.uxml/.uss/PanelSettings;
                   BattleSummary.uxml/.uss (2026-08, read-only post-battle recap, reuses
                   AuraAllocationPanelSettings.asset — name is stale, asset itself is fine);
                   OverworldMenu.uxml/.uss (2026-08, Tab-key Party/Save/Bag/Options menu,
                   replaces PartyMenu.uxml/.uss — reuses BattleHUD.uss directly for skill-ring
                   orb classes, and the existing PartyMenuPanelSettings.asset unchanged, name
                   is stale but the asset itself is fine). OverworldMenu.uss's `.web-*` classes
                   (2026-08-09) are the Party detail view's pan/zoom skill web — per-tree node/
                   edge color is computed procedurally in C# (OverworldMenuController.GetTreeColor),
                   not a USS palette, so BattleHUD.uss's own `.skill-ring-color-N` palette (used
                   by the equip wheel) needed no changes. The old carousel's `.tree-stage`/
                   `.tree-strip`/`.tree-page`/`.tree-node-connector` classes are gone —
                   `.tree-nav-button`/`.tree-nav-label` were kept and repurposed for the new
                   debug tier stepper + Reset View button.
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
  SO: PhasixData, SkillData, SkillDatabase, PrimalTypeChart (all ✅ wired/populated), and
  future EvolutionBranchData/AuraTypeData assets
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
- **`run_tests`, `get_test_job`** (testing group) — 206 EditMode tests exist across 21 files
  (damage formula, battle engine, capture, evolution burst, status/chain/mastery/combo
  catalogs, skill resolution, Aura allocation, and more) — run these after any Combat/
  Creatures change, not just once "those systems exist"
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

> **Art strategy (July 2026):** Real art/animation is intentionally deferred until the game
> reaches a playable, systems-complete state. Until then, any new visual need (Phasix
> creatures, new NPCs) gets a colored Unity primitive sprite, not sourced art — Phasix
> color derives from `PrimalType` (table in `DECISIONS.md` → [Art]). Do not go looking for
> or suggesting sourcing real creature/NPC art before that milestone is reached. Mr_chimken
> and the tilemap's existing placeholder tiles are unaffected — already fine as they are.

- Species roster — no Phasix designed; use placeholder SOs
- Skill content — taxonomy locked, individual skills TBD. The 95 placeholder `SkillData` assets
  (5 per GDD tree as of 2026-08-09, up from 2 — data-only scale-up, see DECISIONS.md ->
  [Creatures], no new design content) ARE now clickable/mechanically resolvable in live battle
  (2026-08, `PlaceholderSkillResolver` — see DECISIONS.md -> [Combat]), but that's generic wiring
  derived from locked tables, not real skill content — still pending the actual design pass.
- Combo/Chain/Mastery numeric gameplay effects — `ComboEngine`/`ChainResultCatalog`/
  `MasteryBonusCatalog` are wired into live battle (detection + battle-log lines using their
  locked flavor text), but no numeric effect is applied for any of them yet (Combo: the GDD
  never defines one; Chain/Mastery: their real modifiers need a `DamageCalculator` change,
  explicitly deferred as separately-scoped follow-up work).
- Evolution Burst's actual gameplay effect (`ApplyBurstEffects` — what changes about the creature
  while a burst is active) — status-only today, genuinely undesigned in the GDD.
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
