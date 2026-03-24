# Phasix — Claude Code Project Memory
**Auto-loaded by Claude Code. Full spec: Assets/Docs/ClaudeCode_Primer.md**
GDD v0.7.9 · Technical Directive v0.1.0 · Unity Latest LTS · 2D URP

---

## Your Role
Senior Unity Developer and C# Architect. Every response: real scripts, real Inspector values, real tuning. No pseudocode.

**Every feature = 3 parts, no exceptions:**
1. Clean C# Script (fully commented, production naming)
2. Inspector Instructions (step-by-step, cold-reader proof)
3. Variable Tuning (starting values, min/max ranges, rationale)

---

## Project
2D top-down Monster Tamer RPG. Digimon-style branching evolution web. Creatures called **Phasix**. Player captures, raises, evolves creatures that follow in the overworld. Seamless open world. Unity Latest LTS, 2D URP, 320×180 Pixel Perfect Camera. Asset Store art pipeline.

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
  Docs/          ← GDD, Technical Directive, Primer, CHANGELOG, DECISIONS, Roadmap, NumericalCalibration, SpeciesRoster, LoreBible
  Docs/Prototypes/ ← Pre-dev encounter prototypes (Vorthex, dialogue tree) — revisit Phase 3
  Scripts/
    Core/        ← GameManager, EventBus, SaveManager
    Player/      ← PlayerController, CameraFollow
    Creatures/   ← MonsterData, BondSystem, EvolutionManager, CompanionAI
    Combat/      ← BattleManager, SkillSystem, StatusEngine, DamageCalculator
    World/       ← WorldChunkManager, EncounterTrigger, ZoneManager
    UI/          ← HUD, PartyScreen, SkillTreeUI, BondDisplay
    Audio/       ← AudioManager
    Save/        ← SaveSystem, SaveData
  Data/
    Species/     ← MonsterData SOs (placeholder until roster designed)
    Skills/      ← SkillData SOs (placeholder until species designed)
    Items/       ← ItemData SOs (pending §22)
    EvolutionBranches/ ← EvolutionBranchData SOs
    TypeCharts/  ← PrimalTypeChart SO (8×8 multiplier table)
```

---

## Creature Schema — MonsterData (ScriptableObject)
```csharp
// IDENTITY
string speciesName;
Temper temper;           // Edge | Anchor | Flux
int evolutionTier;       // 1–5 (6–7 reserved Celestial endgame)
Personality personality; // 16 values — shown on capture
PrimalType primalType;   // 8 base + 28 duo merges
Origin origin;           // Wild|Synthetic|Corrupted|Ascended|Hollow|Primordial
SignalType[] signalPool; // 3–4 types per creature
TempoType tempoType;     // Strike|Flow|Hold|Split|Stance

// BASE STATS (resets to tier floor on devolution)
int vitality, force, resonance, guard, ward, resolve, instinct, aura, aptitude;

// UNNAMED POOL (never resets — display as [POOL_NAME] in UI until GDD names it)
int unnamedPool; // grows per devo cycle: excessStats × bondMultiplier

// PROGRESSION
float bondPercent;    // 0–100
float bondFloor;      // clamp on loss: newValue = max(newValue, bondFloor)
float phaseSaturation;
int currentLevel, currentXP;

// SKILL SYSTEM
List<SkillTreeType> unlockedTreeTypes; // count by tier: T1=2,T2=4,T3=5,T4=6,T5=7
List<SkillData> learnedSkills;         // NEVER shrinks
List<SkillData> equippedSkills;        // active slots: T1=2,T2=3,T3=4,T4=5,T5=5–7
```

---

## Key System Rules (quick ref — full detail in Primer)

**Bond:** 6 zones (Stranger 0–19% / Familiar 20% / Companion 40% / Partner 60% / Bonded 80% / ★100%). Floor = last milestone reached. 100% = permanent, immune to all decrease. Session loss cap = 5%. Above 60% losses halved; above 80% quartered. Type F trees unlock at 20%. Type O trees unlock at 40%.

**Temper growth priority (of 100):**
- Edge: Force 88, Instinct 75, Resonance 58, Aura 52, Vitality 48, Guard 35, Ward 28, Resolve 22
- Anchor: Vitality 90, Guard 80, Ward 72, Resolve 68, Force 48, Aura 42, Instinct 35, Resonance 30
- Flux: Resonance 88, Aura 75, Ward 62, Instinct 52, Vitality 44, Force 32, Guard 25, Resolve 22

**Damage formula:** `(AttackerStat / DefenderStat) × skillPower × primalTypeMultiplier`
Physical: Force/Guard · Elemental: Resonance/Ward · Apply timed bonus after formula.

**Status duration:** `base + ResonanceModifier − ResolveModifier`, min 1. Positive statuses NOT reduced by Resolve.

**Evolution:** Both stat layers (base + unnamed pool) count toward thresholds. Conditionals persist forever across all devo cycles. Level floor = anti-exploit only. Skills learned at any tier always accessible — library never shrinks.

**Loss state:** Losing a battle = currency/items cost only. Zero XP loss, zero bond loss from combat outcome, zero stat regression.

**Primal type — no immunities.** Minimum modifier is 0.5×, every type deals damage to every other.

---

## What Is Pending — Scaffold Only, No Invented Content
Flag these with `// TODO: pending GDD §XX design phase` and leave data slots empty:
- Species roster (§25) — no species designed yet, use placeholder SOs
- Actual skill content (§14) — taxonomy locked, individual skills pending species
- `[POOL_NAME]` — unnamed pool has no player-facing name yet; use token in all UI strings
- XP curves and exact level thresholds (§29 Phase 2 calibration)
- Bond gain/loss exact % per action (§29 Phase 2)
- Aura costs and damage formula values (§29 Phase 2)
- Economy and items (§22 pending)
- NPC/dialogue content (§24 pending)
- Survival/crafting (§20 pending)
- Celestial properties (unique per species, designed during roster phase)
- Signal interaction multiplier values (logic locked, numbers pending calibration)

---

## Reference Files in This Project
- `Assets/Docs/ClaudeCode_Primer.md` — full 311-line spec, read for deep detail
- `Assets/Docs/GDD_CreatureRPG_v0.7.9.html` — master design doc
- `Assets/Docs/Phasix_TechnicalDirective_v0.1.0.html` — implementation patterns, existing scripts
- `Assets/Docs/CHANGELOG.md` — what was built and when
- `Assets/Docs/DECISIONS.md` — implementation decisions not in the GDD
- `Assets/Docs/Roadmap_v2.md` — 20-month phased development plan
- `Assets/Docs/NumericalCalibration.md` — tuning register (all values pending calibration)
- `Assets/Docs/SpeciesRoster.md` — species design template (no species designed yet)
- `Assets/Docs/LoreBible_Phasix.html` — world lore, factions, Five Frequencies, Phasix psychology
- `Assets/Docs/Prototypes/README.md` — index of pre-dev encounter prototypes (revisit Phase 3)
