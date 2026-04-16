# Phasix — Claude Code Project Memory
**Version:** 1.1.0 · **Updated:** March 2026
**Auto-loaded by Claude Code. Full spec: Assets/Docs/ClaudeCode_Primer.md**
**Read Assets/Docs/DOCUMENT_INDEX.md first — defines what is current vs superseded.**

GDD v0.8.0 · Evolution System Directive v1.1.0 · Progression Directive v0.1.0 · World Design Directive v0.1.0 · Combat Directive v0.1.0 · Technical Directive v0.1.0 · Unity Latest LTS · 2D URP

---

## Your Role
Senior Unity Developer and C# Architect. Every response: real scripts, real Inspector values, real tuning. No pseudocode.

**Every feature = 3 parts, no exceptions:**
1. Clean C# Script (fully commented, production naming)
2. Inspector Instructions (step-by-step, cold-reader proof)
3. Variable Tuning (starting values, min/max ranges, rationale)

---

## Project
2D top-down Monster Tamer RPG. Digimon-style branching evolution web. Creatures called **Phasix** — crystallizations of emotional states and coping mechanisms. Player captures, raises, evolves Phasix in an emotional dimension that mirrors lived human experience. World: Multiple Hubs (each with functional specialization) + discrete Realms. Unity Latest LTS, 2D URP, 320×180 Pixel Perfect Camera. Asset Store art pipeline.

---

## Hard Architecture Rules
- **ScriptableObjects = read-only at runtime.** Never write to SO during play. Runtime state → plain C# → JSON.
- **No heavy logic in Update().** Distance checks, AI, pathfinding → timers, coroutines, or event callbacks.
- **Event-driven over polling.** C# `event`/`Action` delegates or lightweight message bus.
- **Object pooling** for anything that spawns/despawns during gameplay. Never `Instantiate`/`Destroy` in a loop.
- **World = GameObject Chunks.** Toggle `SetActive(false)` for distant chunks — never destroy them.
- **Pathfinding = A* Pathfinding Project (free/Lite).**

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

## Folder Structure
```
Assets/
  Docs/          ← All design documents (see DOCUMENT_INDEX.md)
  Scripts/
    Core/        ← GameManager, EventBus, SaveManager
    Player/      ← PlayerController, CameraFollow
    Creatures/   ← PhasixData, BondSystem, EvolutionManager, CompanionAI
    Evolution/   ← EvolutionEvaluator, EvolutionExecutor, EvolutionPathfinder, EvolutionWebController
    Combat/      ← BattleManager, SkillSystem, StatusEngine, DamageCalculator
    World/       ← WorldChunkManager, EncounterTrigger, ZoneManager
    UI/          ← HUD, PartyScreen, SkillTreeUI, BondDisplay
    Audio/       ← AudioManager
    Save/        ← SaveSystem, SaveData
  Data/
    Species/     ← PhasixData SOs (placeholder until roster designed)
    Skills/      ← SkillData SOs (placeholder until species designed)
    Items/       ← ItemData SOs (pending §22)
    EvolutionBranches/ ← EvolutionBranchData SOs
    TypeCharts/  ← PrimalTypeChart SO (8×8 multiplier table)
    Aura/        ← AuraTypeData SOs (Common, Specific, RareVariant)
```

---

## Phasix Schema — PhasixData (ScriptableObject)
```csharp
// IDENTITY
string speciesName;
string emotionalType;        // emotional root — grief, joy, anger, etc.
Temper temper;               // Edge | Anchor | Flux
int evolutionTier;           // T1–T5 natural; T6–T7 fusion only
Personality personality;     // 16 values — shown on capture
PrimalType primalType;       // 8 base + 28 duo merges
Origin origin;               // Wild | Synthetic | Corrupted | Ascended | Hollow | Primordial
SignalType[] signalPool;     // 3–4 types per creature
TempoType tempoType;         // Strike | Flow | Hold | Split | Stance

// BASE STATS (resets to tier floor on devolution)
int vitality, force, resonance, guard, ward, resolve, instinct, aura;

// APTITUDE (persists through devolution — never resets)
// Grows +1 per devolution cycle
// Function A: raises stat ceiling per tier
// Function B: unlocks exotic evolution branches at minimum thresholds
// Side effect: higher Aptitude before devolving = larger unnamed pool gain
int aptitude;

// UNNAMED POOL (never resets — display via GameStrings.PoolName in UI until named)
int unnamedPool; // grows per devo: excessStats × bondMultiplier

// AURA RESOURCES (runtime — stored in save data, not on SO)
// Common Aura: drives stat growth, farmable from all Phasix
// Specific Aura: gates evolution, tied to emotional type/region
// RareVariant Aura: gates exotic branches, boss drops
// All values pending NumericalCalibration.md

// PROGRESSION (no XP, no levels — superseded by Aura system)
float bondPercent;           // 0–100
float bondFloor;             // last milestone floor — cannot drop below this
float phaseSaturation;       // accumulates toward evolution thresholds
// NOTE: currentLevel and currentXP are REMOVED — see Progression_Directive_v0_1_0.md

// SKILL SYSTEM
List<SkillTreeType> unlockedTreeTypes;
List<SkillData> learnedSkills;      // NEVER shrinks
List<SkillData> equippedSkills;     // active slots: T1=2, T2=3, T3=4, T4=5, T5=5–7
```

---

## Key System Rules (quick ref — full detail in Primer and Directives)

**Progression (Aura system — see Progression_Directive_v0_1_0.md):**
- Common Aura drops from all Phasix in battle → spent to allocate stats freely within tier
- Specific Aura drops from particular species/bosses → required for evolution
- Rare Variant Aura → gates exotic evolution branches
- Free stat allocation with Resonance Bonus layer — aligned investment scales better
- Stat ceiling per tier scales with Aptitude — higher Aptitude = more room to develop
- Evolution requires: Aura gate + stat minimum + conditionals simultaneously
- **No XP. No levels. No level floors. GDD §21 leveling model is superseded.**

**Aptitude:**
- Grows +1 per devolution cycle
- Raises stat ceiling per tier (Function A)
- Unlocks exotic evolution branches at minimum thresholds (Function B)
- Side effect: higher Aptitude before devolving = larger unnamed pool gain

**Evolution (see Evolution_System_Directive_v1_1_0.pdf):**
- Three types: Standard, Item-gated, Fusion
- Conditionals persist forever across all devo cycles
- Both stat layers (base + unnamed pool) count toward thresholds
- Stat minimum replaces level floor as the anti-exploit gate
- Devolution is FREE — no conditions, no cost, no time limit
- Fusion: valid at ALL tiers; same-tier ingredient requirement only for T6+

**Aura requirements by tier:**
```
T1 → T2    Common Aura only
T2 → T3    Specific Aura — current realm primary type
T3 → T4    Specific Aura — 2 realms
T4 → T5    Specific Aura — 3+ realms + Rare Variant Aura
T5 → T6+   Fusion only
```

**Bond:** 6 zones (Stranger 0–19% / Familiar 20% / Companion 40% / Partner 60% / Bonded 80% / ★100%). Floor = last milestone reached. 100% = permanent. Session loss cap = 5%. Above 60% losses halved; above 80% quartered. Type F trees unlock at 20%. Type O trees unlock at 40%.

**Temper growth priority (of 100):**
- Edge: Force 88, Instinct 75, Resonance 58, Aura 52, Vitality 48, Guard 35, Ward 28, Resolve 22
- Anchor: Vitality 90, Guard 80, Ward 72, Resolve 68, Force 48, Aura 42, Instinct 35, Resonance 30
- Flux: Resonance 88, Aura 75, Ward 62, Instinct 52, Vitality 44, Force 32, Guard 25, Resolve 22

**Damage formula:** `(AttackerStat / DefenderStat) × skillPower × primalTypeMultiplier`
Physical: Force/Guard · Elemental: Resonance/Ward · Apply timed bonus after formula.

**Status duration:** `base + ResonanceModifier − ResolveModifier`, min 1. Positive statuses NOT reduced by Resolve.

**Loss state:** Losing = currency/items cost only. Zero Aura loss, zero bond loss from combat outcome, zero stat regression.

**Primal type — no immunities.** Minimum modifier is 0.5×, every type deals damage to every other.

**Blackout & Banking:**
- Blackout (party wipe) returns player to last hub visited
- Phasix always kept — no permadeath, no forced devolution on blackout
- Aura/loot/currency collected since last hub visit lost unless banked
- Banking at a hub makes resources permanent; players choose *which* hub to bank at

---

## What Is Pending — Scaffold Only, No Invented Content
Flag with `// TODO: pending design — [topic]`

- Species roster (§25) — no species designed, use placeholder SOs
- Actual skill content (§14) — taxonomy locked, individual skills pending
- `GameStrings.PoolName` — unnamed pool has no player-facing name yet; reference this constant in all UI strings (defined in `Assets/Scripts/Core/GameStrings.cs`)
- All NumericalCalibration.md values — pending calibration session
- Hub identity, realm count, realm emotional identities — pending world design session
- Faction names and lore details — working names only, pending refinement
- Main quest narrative — pending story design session
- Economy and items (§22 pending)
- NPC/dialogue content (§24 pending)
- Survival/crafting (§20 pending)
- Celestial properties — unique per species, pending roster
- Signal interaction multiplier values — logic locked, numbers pending
- Elemental Frequencies (Ignis/Virel/Aether/Veil/Flux) — pending lore revisit session
- Old lore (Fracture, Phase Dimension, Five Factions) — DO NOT IMPLEMENT, reference only

---

## Planning Session Checklist
At the start of every planning session, run these three calls **before writing a single line of plan:**
1. Read `Assets/MCP/Context/PhasixGuide.md` via `unity_execute_code` — full architecture, what's built, what's pending, applicable MCP tools. Use: `return System.IO.File.ReadAllText(Application.dataPath + "/MCP/Context/PhasixGuide.md");`
   *(Note: `unity_get_project_context` has a known HTTP 500 bug in this MCP server version — use the execute_code workaround above)*
2. `unity_editor_state` — confirms Unity is open, which scene is active, and compilation status
3. `unity_get_compilation_errors` — surface any broken state before planning new features

---

## After Every Feature or Bug Fix
- **`Assets/Docs/CHANGELOG.md`** — log what was built/fixed, with date
- **`Assets/Docs/KNOWN_ISSUES.md`** — add new bugs (with GitHub Issue #), remove closed ones
- **`CLAUDE.md`** — update ONLY if folder structure or architecture rules changed
- Commit all updated docs alongside the feature/fix

## Reference Files in This Project
```
Assets/Docs/DOCUMENT_INDEX.md                       ← Read first — document hierarchy and status
Assets/Docs/ClaudeCode_Primer_v1_1_0.md             ← Full system spec (§9 defers to Evolution Directive)
Assets/Docs/GDD_CreatureRPG_v0_8_0.html             ← Master GDD v0.8.0
Assets/Docs/Evolution_System_Directive_v1_1_0.pdf   ← Supersedes GDD §3 (evolution web, devolution, fusion)
Assets/Docs/Progression_Directive_v0_1_0.md         ← Supersedes GDD §21 (XP/leveling)
Assets/Docs/WorldDesign_Directive_v0_1_0.md         ← Supplements GDD §19, §24 (world, calendar, factions)
Assets/Docs/Combat_Directive_v0_1_0.md              ← Supplements GDD §18 (combat, 7-lane stage, action commands)
Assets/Docs/Phasix_TechnicalDirective_v0.1.0.html  ← Implementation patterns
Assets/Docs/CHANGELOG.md                            ← Session log
Assets/Docs/DECISIONS.md                            ← Implementation decisions not in GDD
Assets/Docs/NumericalCalibration.md                 ← All pending numerical values
Assets/Docs/LoreBible_Phasix_(obsolete).html        ← REFERENCE ONLY — old lore, requires revisit
Assets/Docs/Prototypes/README.md                    ← Pre-dev encounter prototypes (revisit Phase 3)
Assets/Docs/KNOWN_ISSUES.md                         ← Active bugs/blockers
```
