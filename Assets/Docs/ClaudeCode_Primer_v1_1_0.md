# Phasix — Claude Code Session Primer
**Version:** 1.1.0 · **Updated:** March 2026  
**Paste this at the start of every Claude Code session.**  
**Read DOCUMENT_INDEX.md first — it defines what is current vs superseded.**

GDD v0.8.0 · Evolution Directive v1.1.0 · Progression Directive v0.1.0 · World Design Directive v0.1.0 · Technical Directive v0.1.0 · Unity Latest LTS · 2D URP

---

## 1. Your Role

You are a **Senior Unity Developer and C# Architect** working on Project Phasix. Every response must be **implementation-ready**: real scripts, real Inspector values, real tuning guidance. No pseudocode, no conceptual outlines.

**Every feature must be delivered in exactly three parts — no exceptions:**
1. **Clean C# Script** — fully commented, production-grade naming, no magic numbers
2. **Inspector Instructions** — step-by-step setup a cold reader can follow
3. **Variable Tuning** — recommended starting values, min/max ranges, rationale

---

## 2. Project Summary

2D top-down Monster Tamer RPG. Digimon-style branching evolution web. Player captures, raises, and evolves creatures called **Phasix** — crystallizations of emotional states and coping mechanisms. Phasix emerge from an emotional dimension that mirrors lived human experience. Player navigates a hub and discrete emotional realms, developing Phasix through Aura-driven progression.

**Engine:** Unity Latest LTS, 2D URP  
**Pixel resolution:** 320×180 reference (16:9), Pixel Perfect Camera  
**Art:** Asset Store sprites for now — no custom art pipeline yet

---

## 3. Non-Negotiable Architecture Rules

### Data model
- **ScriptableObjects are read-only at runtime.** Never write to a SO during play.
- Phasix species data → ScriptableObject assets
- Evolution tree nodes → ScriptableObject graph
- Skill/move data → ScriptableObject assets
- Aura type data → ScriptableObject assets (Common, Specific, RareVariant)
- Save data → JSON serialization only

### Performance
- **No heavy logic in `Update()`** — use timers, coroutines, or event callbacks
- **Event-driven over polling** — C# `event`/`Action` delegates or lightweight message bus
- **Object pooling** for anything that spawns/despawns. Never `Instantiate`/`Destroy` in a loop.

### World management
- World = **GameObject Chunks** (migrate to Additive Scene Streaming only if profiler shows memory pressure)
- Distant chunks `SetActive(false)`, never destroyed

### Pathfinding
- **A* Pathfinding Project (free/Lite)** — 2D grid graph, Seeker component

### Code style
```csharp
[SerializeField] private float _moveSpeed = 5f;
[Header("Movement")]
[Tooltip("Units per second")]
[RequireComponent(typeof(Rigidbody2D))]
// Cache in Awake(), not Update()
// Input → Update()   Physics → FixedUpdate()
// PascalCase public, _camelCase private
```

---

## 4. Phasix Data Schema (PhasixData ScriptableObject)

Every Phasix has **all of the following fields**. Do not invent fields not listed here.

### Identity
- `string speciesName`
- `string emotionalType` — emotional root (grief, joy, anger, etc.)
- `Temper temper` — enum: `Edge` | `Anchor` | `Flux`
- `int evolutionTier` — T1–T5 natural; T6–T7 fusion only
- `Personality personality` — 16 values, shown on capture
- `PrimalType primalType` — 8 base + 28 duo merges
- `Origin origin` — Wild | Synthetic | Corrupted | Ascended | Hollow | Primordial
- `SignalType[] signalPool` — 3–4 types per creature
- `TempoType tempoType` — Strike | Flow | Hold | Split | Stance

### Base Stats (resets to tier floor on devolution)
- `int vitality` — HP pool and HP-threshold triggers
- `int force` — physical damage output
- `int resonance` — elemental and skill damage output
- `int guard` — physical damage reduction
- `int ward` — elemental damage reduction
- `int resolve` — status resistance
- `int instinct` — turn order, timing windows, evasion, combo trigger
- `int aura` — energy pool and recovery rate

### Aptitude (persists — never resets)
- `int aptitude` — grows +1 per devolution cycle
  - **Function A:** raises stat ceiling per tier
  - **Function B:** unlocks exotic evolution branches at minimum thresholds
  - Higher Aptitude before devolving = larger unnamed pool gain (side effect)

### Unnamed Pool (persists — never resets)
- `int unnamedPool` — accumulated across all devolution cycles
- Growth formula: `excessStats × bondMultiplier` on each devolution
- **Display as `[POOL_NAME]` in all UI strings until GDD names it**

### Progression (Aura system — NO XP, NO LEVELS)
- `float bondPercent` — 0.0 to 100.0
- `float bondFloor` — last milestone floor; bond cannot drop below this
- `float phaseSaturation` — accumulates toward evolution thresholds
- ~~`int currentLevel`~~ — **REMOVED. Superseded by Aura system.**
- ~~`int currentXP`~~ — **REMOVED. Superseded by Aura system.**

### Skill System
- `List<SkillTreeType> unlockedTreeTypes`
- `List<SkillData> learnedSkills` — **never shrinks**
- `List<SkillData> equippedSkills` — T1=2, T2=3, T3=4, T4=5, T5=5–7 slots

---

## 5. Progression System (Aura-Driven — see Progression_Directive_v0_1_0.md)

**GDD §21 XP/leveling model is superseded. Do not implement it.**

### Aura Types
| Type | Source | Function |
|---|---|---|
| Common Aura | All Phasix in battle | Stat growth fuel — spend to allocate stat points freely |
| Specific Aura | Particular species / bosses | Evolution gate — tied to emotional type/region |
| Rare Variant Aura | Hidden encounters / boss drops | Exotic branch gate |

### Stat Allocation
- Player allocates stat points freely using Common Aura
- **Resonance Bonus:** points aligned with Phasix's emotional type generate passive bonuses (values pending NumericalCalibration.md)
- Stat ceiling per tier is capped — scales upward with Aptitude
- Hitting the ceiling = natural signal to evolve

### Evolution Gate — Three Layers Required Simultaneously
```
Layer 1 — AURA:      Correct Specific Aura types in required quantities
Layer 2 — STATS:     Minimum stat thresholds met within current tier
Layer 3 — CONDITIONS: Aptitude min (exotic) + Bond + story flags + other TBD
```

### Aura Requirements By Tier
```
T1 → T2    Common Aura only
T2 → T3    Specific Aura — current realm primary type
T3 → T4    Specific Aura — 2 realms
T4 → T5    Specific Aura — 3+ realms + Rare Variant Aura
T5 → T6+   Fusion only
```

### Devolution
- FREE — no conditions, no cost, no time limit (superseded by Evolution_System_Directive_v1_1_0.pdf)
- Unnamed pool grows on devo: `excessStats × bondMultiplier`
- Aptitude grows +1
- Skills and Bond preserved
- All values pending NumericalCalibration.md

### Loss State
Losing a battle = currency/items cost only. Zero Aura loss, zero bond loss from combat, zero stat regression.

---

## 6. Bond System

Bond is a **reward system, not a punishment system**.

| Zone | Range | Key Unlock |
|---|---|---|
| Stranger | 0–19% | No bonuses |
| Familiar | 20–39% | Type F trees activate; timing windows slightly larger |
| Companion | 40–59% | Evolution burst reliable; Type O trees activate |
| Partner | 60–79% | Losses halved; combo discovery rate increases |
| Bonded | 80–99% | Max pool growth; Bond-100 path unlocked; losses quartered |
| ★ Complete | 100% | Permanent. Cannot decrease by any means. |

**Floor system:** bond cannot drop below last milestone floor. Implement as `max(newValue, bondFloor)`.  
**Session cap:** maximum 5% bond loss per session regardless of event count.

---

## 7. Temper System

| Temper | Role | Growth Priority (of 100) |
|---|---|---|
| Edge | Striker | Force 88, Instinct 75, Resonance 58, Aura 52, Vitality 48, Guard 35, Ward 28, Resolve 22 |
| Anchor | Tank | Vitality 90, Guard 80, Ward 72, Resolve 68, Force 48, Aura 42, Instinct 35, Resonance 30 |
| Flux | Special | Resonance 88, Aura 75, Ward 62, Instinct 52, Vitality 44, Force 32, Guard 25, Resolve 22 |

Temper ~60% of growth direction. Personality ~25%. Player Aura allocation ~15%.  
Internal role names (Edge/Anchor/Flux) never shown to player — only species-specific compound names.

---

## 8. Skill Tree System (18 Types — Taxonomy Locked, Content Pending)

| Type | Name | Primary Attribute | Role |
|---|---|---|---|
| A | Utility | Force/Resonance | Direct output, versatile action skills |
| B | Aura | Aura | Energy management, cost reduction, recovery |
| C | Passive | All | Always-active bonuses, stat amplifiers |
| D | Synergy | All | Inter-skill connections, combo setups |
| E | Reaction | Instinct | Triggered responses, counter-attacks, parries |
| F | Bond | Bond % | Scales with bond %. Activates at 20% bond. |
| G | Aspect | Instinct | Mode-based skill sets. Primary for Stance Tempo. |
| H | Resource | Aura/Vitality | Economy skills, sustain, action banking. Primary for Hold Tempo. |
| I | Corruption | Resonance | Status application. Overuse affects bond. |
| J | Mirror | Instinct/Resonance | Repetition and reverberation effects |
| K | Evolve | Bond/Aptitude | Evolution burst, mid-battle transformation |
| L | Territory | Force/Resonance | Spatial control, area effects. Primary for Split Tempo. |
| M | Memory | Aptitude | History-dependent effects, past-state interactions |
| N | Fusion | All | Fusion creation mechanics |
| O | Personality | Bond/Temper | Activates at 40% bond. Personality-specific expressions. |
| P | Typing | Resonance | Type-specific power skills, Primal exploits |
| Q | Bastion | Guard/Vitality | Fortify, Counter, Absorb. Physical defense primary. |
| R | Phantom | Instinct | Evasion, Prediction, Ghost Step. Speed defense primary. |

**Slot count:** T1=2 trees/2 slots · T2=4/3 · T3=5/4 · T4=6/5 · T5=7/5–7  
**Skill library never shrinks.** Combo system: Duo→Trio→Quad, discovered through use.

---

## 9. Evolution System (see Evolution_System_Directive_v1_1_0.md)

### Three evolution types
| Type | Trigger | On Devolution |
|---|---|---|
| Standard | Aura gate + stat minimums + up to 2 conditionals | Returns; skills banked; Bond preserved; Aptitude +1; unnamed pool grows |
| Item-gated | Standard + specific key item consumed | Item returned; all branches reopen |
| Fusion | Two/three Phasix at matching tiers | Ingredients returned; primary carries everything |

### Critical rules
- Conditionals are **one-time checks** — persist across ALL devolution cycles forever
- Stat minimum replaces level floor as the anti-exploit gate
- Both stat layers (base + unnamed pool) count toward thresholds
- Fusion: valid at ALL tiers; same-tier ingredient requirement applies only to T6+

---

## 10. Type Systems (4 Axes)

| Axis | Governs | Revealed | Player Hint |
|---|---|---|---|
| Primal | Elemental damage matchups | Region 1 | Always fully visible |
| Signal | Energy rhythm + interference | Region 2 | Visual/audio cues only — no text |
| Tempo | Action economy per turn | Region 3 | Lore fragments only |
| Celestial | Rule modifications | Region 4 | No hints — community discovery |

### Primal type chart (8 base types — no immunities, minimum 0.5×)

| ATK↓ / DEF→ | Fire | Water | Earth | Wind | Light | Shadow | Life | Lightning |
|---|---|---|---|---|---|---|---|---|
| Fire | — | 0.5 | 1.25 | 2.0 | 0.75 | 1.25 | 2.0 | 1.0 |
| Water | 2.0 | — | 1.25 | 1.0 | 1.0 | 1.0 | 1.25 | 0.75 |
| Earth | 0.75 | 0.75 | — | 0.5 | 1.0 | 1.25 | 1.25 | 2.0 |
| Wind | 0.5 | 1.0 | 2.0 | — | 1.25 | 0.75 | 1.0 | 0.5 |
| Light | 1.25 | 1.0 | 1.0 | 0.75 | — | 2.0 | 1.25 | 1.0 |
| Shadow | 0.75 | 1.0 | 0.75 | 1.25 | 2.0 | — | 0.5 | 1.25 |
| Life | 0.5 | 2.0 | 0.75 | 1.0 | 0.75 | 1.25 | — | 1.0 |
| Lightning | 1.0 | 1.25 | 0.5 | 2.0 | 1.0 | 0.75 | 1.0 | — |

28 duo-merged Primal types exist. Treat as own enum values.

### Tempo — 5 action economy types
Strike · Flow · Hold · Split · Stance

### Signal — 9 energy rhythm types
Pulse · Static · Frequency · Silence · Overflow · Echo · Surge · Catalyst · Current

---

## 11. Battle System

- **Turn-based.** Think, don't grind.
- **Damage formula:** `(AttackerStat / DefenderStat) × skillPower × primalTypeMultiplier`
  - Physical: Force/Guard · Elemental: Resonance/Ward
  - Apply timed bonus after formula
- **Timed inputs:** Attack = bonus damage; Guard = damage reduction. Window scales with Instinct.
- **Status:** Apply → create vulnerability → exploit. Expiry triggers reward letting statuses run.
- **Status duration:** `base + resonanceModifier − resolveModifier`, min 1. Positive statuses NOT reduced by Resolve.

---

## 12. Personality Traits (16 Total — Stat Nudge Only)

**Offensive:** Reckless (Force++, Instinct+, Guard−) · Fierce (Force++, Vitality+)  
**Elemental:** Volatile (Resonance++, Aura+, Resolve−) · Intense (Resonance++, Ward+)  
**Defensive:** Sturdy (Guard++, Vitality+) · Calm (Resolve++, Ward+)  
**Technical:** Sharp (Instinct++, Aura+) · Cunning (Instinct++, Force+)  
**Resilient:** Patient (Resolve++, Guard+) · Cautious (Ward++, Resolve+, Force−)  
**Versatile:** Timid (Aura++, Ward+, Force−) · Naive (Vitality++, Resonance+, Instinct−) · Bold · Gentle · Lively · Quirky

---

## 13. System Status Quick Reference

| System | Directive / GDD Ref | Status |
|---|---|---|
| Aura progression (replaces XP/leveling) | Progression_Directive_v0_1_0 | Active |
| Aptitude (dual function) | Progression_Directive_v0_1_0 | Active |
| Evolution web (3 types, branch framework) | Evolution_Directive_v1_1_0 | Active |
| World structure (Hub + Realms) | WorldDesign_Directive_v0_1_0 | Active — details pending |
| Encounter initiation (3-layer) | WorldDesign_Directive_v0_1_0 | Active — details pending |
| Calendar/month system | WorldDesign_Directive_v0_1_0 | Active — details pending |
| Faction framework | WorldDesign_Directive_v0_1_0 | Working names — pending refinement |
| Attributes (9) + unnamed pool | GDD §5 | Locked |
| Temper (3 roles) | GDD §4 | Locked |
| Bond (6 zones + floors) | GDD §6 | Locked |
| Personality (16 traits) | GDD §7 | Locked |
| Primal types (8 + 28 merges) | GDD §9 | Locked |
| Signal types (9) | GDD §10 | Locked |
| Tempo types (5) | GDD §11 | Locked |
| Celestial (unique per creature) | GDD §13 | Locked — content pending species |
| Skill tree taxonomy (18 types) | GDD §14 | Taxonomy locked — content pending |
| Skill tree attribute scaling | GDD §15 | Locked |
| Battle system | GDD §16 | Partially locked |
| Status effects (24, 7 chains) | GDD §17 | Locked v0.7.8 |
| GDD §21 XP/leveling | **SUPERSEDED** | Do not implement |

---

## 14. What Is Pending — Scaffold Only, No Invented Content

Flag with `// TODO: pending design — [topic]`

- Species roster (§25) — use placeholder SOs
- Actual skill content (§14) — build framework, not content
- `[POOL_NAME]` — use token in all UI strings
- All NumericalCalibration.md values — pending calibration session
- Hub identity, realm count, realm emotional identities
- Faction refined names and lore
- Main quest narrative
- Economy and items (§22)
- NPC/dialogue content (§24)
- Survival/crafting (§20)
- Celestial properties — unique per species
- Signal interaction multiplier values
- Old lore (Fracture, Phase Dimension, Five Factions) — **DO NOT IMPLEMENT**
