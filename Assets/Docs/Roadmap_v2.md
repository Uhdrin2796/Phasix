# Phasix — Development Roadmap
**Version:** 2.0  
**Date:** March 2026  
**Engine:** Unity Latest LTS · 2D URP  
**Path:** Asset Store sprites · Claude Code scripting  
**Scope:** Complete beginner · 5–10 hrs/week  
**GDD ref:** v0.7.9 · Technical Directive v0.1.0

---

## Overview

| Metric | Value |
|--------|-------|
| Total phases | 5 |
| Full roadmap | ~20 months |
| First playable (vertical slice) | Month 9 (Phase 3 gate) |
| Alpha build (external playtest) | Month 15 (Phase 4 gate) |
| Public demo (itch.io) | Month 20 (Phase 5 gate) |

**Timeline assumes:** 5–10 hrs/week with ~30% buffer for learning curve, Unity troubleshooting, and scope creep. All timelines include beginner ramp-up — the first two months are split between learning Unity and building.

**Two-track rule:** Several GDD systems have locked *architecture* but pending *content* (skill trees, status values, species roster). Build the framework code in the phase it belongs — leave content slots empty. Content fills once the roster and species design phase completes. This is intentional, not a gap.

**Key constraint:** Do not skip phases. A broken player controller cascades bugs into every subsequent system. Each phase gate must be solid before proceeding.

---

## Phase 1 — Foundation & Unity Basics
**Timeline:** Months 1–2  
**Goal:** Learn the engine while building the skeleton

### Milestones

#### Wk 1 — Unity LTS setup + Git
- Install Unity Hub, create 2D URP project named `Phasix`
- Configure Git + GitHub, add Unity `.gitignore`
- Learn Editor layout: Scene, Game, Inspector, Project, Hierarchy
- Create full folder structure: `Assets/Docs/`, `Assets/Scripts/` (8 subfolders), `Assets/Data/` (5 subfolders)
- Drop all Docs files into place (CLAUDE.md at project root)
- **Tags:** Unity Hub · 2D URP · Git

#### Wk 2 — C# fundamentals crash course
- Variables, methods, classes, MonoBehaviour lifecycle (Awake, Start, Update)
- Goal: read and modify scripts confidently — not mastery
- **Tags:** C# · MonoBehaviour

#### Wk 3–4 — Top-down player movement
- Rigidbody2D + new Input System (not legacy Input.GetAxis)
- WASD/joystick 8-directional movement, normalised diagonal speed
- Collision with TilemapCollider2D
- Write this script yourself — do not copy-paste
- **Tags:** Rigidbody2D · Input System · Tilemap

#### Wk 5–6 — First Asset Store pack integration
- Purchase and import a top-down character pack with idle/walk/run animations pre-rigged
- Wire asset to movement script
- Learn Sprite atlases and Animator Controller
- Update DECISIONS.md: tile base size (16×16 or 32×32), A* cell size
- **Tags:** Asset Store · Animator · Sprite Renderer

#### Wk 7–8 — Tilemap world + Cinemachine camera
- Build a small test map with Unity Tilemap and Rule Tiles
- Cinemachine virtual camera with follow + confine
- First walkable world
- **Tags:** Tilemap · Cinemachine · Rule Tiles

### Phase 1 Gate
> Player walks a tiled world, camera follows, asset art is visible on screen. You can read and modify your own scripts without help. Record a screen capture — your first milestone proof.

---

## Phase 2 — Creature Data & Companion AI
**Timeline:** Months 3–4  
**Goal:** Full creature data schema live, companion follows player  
**GDD sections:** §3 (Evolution — Locked), §5 (Attributes — Locked), §6 (Bond — Locked), §7 (Personality — Locked), §8 (Typing — Locked)

> All systems in this phase map to fully locked GDD sections. Build exactly what the GDD specifies — no invention.

### Milestones

#### Wk 9 — MonsterData ScriptableObject — full schema
- Implement all 9 attributes: Vitality, Force, Resonance, Guard, Ward, Resolve, Instinct, Aura, Aptitude
- Temper enum (Edge / Anchor / Flux)
- Personality enum (16 traits)
- Two-layer stat system: base stats layer + unnamed pool layer — both always visible in UI
- Use `[POOL_NAME]` token for unnamed pool in all UI strings until GDD names it
- Bond float (0–100), bondFloor float, phaseSaturation float
- **Tags:** ScriptableObject · 9 attributes · Unnamed pool · GDD §5 Locked

#### Wk 10 — Bond system — 6 milestones + floor logic
- 6 zones: Stranger (0–19%) / Familiar (20%) / Companion (40%) / Partner (60%) / Bonded (80%) / ★Complete (100%)
- Floor system: `newBond = max(newBond, bondFloor)` on every decrease event
- 100% bond permanently immune to all decrease
- Session loss cap: 5% max regardless of event count
- Above 60%: losses halved. Above 80%: losses quartered.
- **Tags:** Bond floors · Session cap · GDD §6 Locked

#### Wk 11 — Personality system — 16 traits
- All 16 traits as attribute growth rate modifiers — stat nudge only, no skill effects
- Shown on capture, fixed until changed by item
- Store as enum on MonsterData
- **Tags:** 16 traits · Growth modifier · GDD §7 Locked

#### Wk 12–13 — Companion following AI
- Active Phasix follows player using A* Pathfinding Project (free/Lite)
- Idle / Walk / Run animation states driven by distance to player
- Offset from player, obstacle avoidance
- Party system: up to 3 slots, active companion follows, others stored
- **Tags:** A* Pathfinding · Animator · Party system

#### Wk 14–16 — Wild encounter trigger + Primal type reveal
- Zone collider triggers wild encounter
- Spawn wild Phasix with MonsterData populated
- Primal type (Region 1 axis) always fully visible — no discovery mechanic here
- Signal / Tempo / Celestial axes hidden — no UI for those yet
- Flee or engage choice
- **Tags:** Primal type visible · Trigger2D · GDD §8 Locked

### Phase 2 Gate
> Full creature data schema live. Companion follows via pathfinding. Bond zones and floors functional. Wild encounters trigger. All data in ScriptableObjects, matching GDD §3–§7 exactly.

---

## Phase 3 — Combat Loop + Skill Tree Framework
**Timeline:** Months 5–9  
**Goal:** Full encounter-to-battle-to-capture loop playable — vertical slice  
**GDD sections:** §14 (Skill taxonomy — locked, content pending) · §15 (Scaling — Locked) · §16 (Battle — Partial) · §17 (Status — Locked v0.7.8)

> Skill tree *architecture* is fully buildable (taxonomy locked, §14–§15). Actual skill *content* is pending species roster. Build the framework, slots, combo engine, and UI — populate with 2–3 placeholder skills per type to validate the system. Real content fills in Phase 5.

### Combat Foundation

#### Mo 5 Wk 1–2 — Battle scene + turn state machine
- Additive battle scene load (BattleScene_Main)
- State machine: PlayerTurn → EnemyTurn → ResolveActions → CheckWinLoss → EndBattle
- Coroutine-driven sequence
- HP bars, name plates, move menu UI
- Enemy uses random move selection for now
- **Tags:** State machine · Coroutines · GDD §16 Partial

#### Mo 5 Wk 3–4 — Timed input system
- Timed attack press: bonus damage on correct timing
- Timed guard: damage reduction on correct timing
- Timing window size scales with Instinct attribute — higher Instinct = larger window
- Bond level adds minor flat bonus to window
- **Tags:** Instinct scaling · Input timing · GDD §16 Locked

#### Mo 6 Wk 1–2 — Damage formula + Primal type chart
- Formula: `(AttackerStat / DefenderStat) × skillPower × primalTypeMultiplier`
  - Physical: Force / Guard
  - Elemental: Resonance / Ward
  - Apply timed input bonus after formula
- Primal type chart: 8 base types, full 8×8 multiplier table as ScriptableObject
- No immunities — every type deals at least some damage (minimum 0.5×)
- **Tags:** 8 Primal types · Type chart SO · GDD §5 + §8 Locked

### Skill Tree Framework

#### Mo 6 Wk 3–4 — SkillTreeData ScriptableObject — 18 types
- Type enum A–R with name, primary attribute, secondary attribute, role
- SkillData schema: name, type, primaryAttributeScale, secondaryAttributeScale, power, auraCost
- Every skill must scale with at least one attribute (GDD mandate — flat skills forbidden)
- Slot system by tier: T1=2 trees/2 active, T2=4/3, T3=5/4, T4=6/5, T5=7/5–7
- Skill library never shrinks — learnedSkills list only grows
- Populate each of the 18 types with 2–3 placeholder skills for testing
- **Tags:** GDD §14 Taxonomy locked · 18 tree types · Slot system · Placeholder content

#### Mo 7 Wk 1 — Bond-gated tree unlocks (Type F + Type O)
- Type F (Bond) trees unlock at 20% bond milestone event
- Type O (Personality) trees unlock at 40% bond milestone event
- Gate check fires on bond milestone event via event bus — not a polling loop
- UI shows locked / unlocked state per tree type
- **Tags:** Type F at 20% · Type O at 40% · Event bus · GDD §6 + §14 Locked

#### Mo 7 Wk 2 — Combo engine — Duo / Trio / Quad
- Track last N skills used in sequence
- Check against combo lookup table (ScriptableObject)
- Instinct scales combo trigger chance
- Bond above 60% increases combo discovery rate
- Combos discovered through use — not all visible upfront
- Placeholder combo table for testing
- **Tags:** Instinct scaling · Discovery system · GDD §14 Locked

### Status System

#### Mo 7 Wk 3–4 — Status effect engine — all 24 statuses
- Duration formula: `base + resonanceModifier − resolveModifier`, minimum 1 turn
- Positive statuses NOT reduced by Resolve
- Vulnerability windows active while status is applied
- Expiry triggers fire on natural expiry only — NOT on cleanse
- Stacking rules per status (DoTs stack up to cap; debuffs refresh)
- Auto-cleanse: if target Resolve > status magnitude rating → cleanse after 1 turn
- Priority order: 7 physical → 7 elemental → 4 signal/aura → 4 universal → 6 positive buffs
- 7 chain results (Rend, Entomb, Paralysis, Scorch, Sap, Dissolve, Shatter)
- 8 mastery bonuses activate when 3+ specific statuses active simultaneously
- **Tags:** GDD §17 Locked v0.7.8 · 24 statuses · 7 chains · 8 mastery bonuses

#### Mo 8 Wk 1–2 — Bond gauge + evolution burst (in battle)
- Bond gauge fills via: skill use, successful timed inputs, hits taken
- Higher bond = faster fill + longer burst duration
- Mid-battle evolution burst: Type K tree skeleton — temporary Surge-tier form, returns after duration
- Burst visual: sprite swap + particle burst
- **Tags:** Bond gauge · Type K burst · GDD §16 + §3 Locked

#### Mo 8 Wk 3 — Capture mechanic
- Capture probability formula based on current HP %
- On success: Phasix added to party or storage box
- Personality shown immediately on capture
- On fail: battle continues
- **Tags:** Probability formula · Storage box · Personality on capture

#### Mo 8 Wk 4 — XP + levelling + stat growth
- XP awarded on battle win (zero on loss — no progress reversal)
- Level threshold table (ScriptableObject — values pending NumericalCalibration.md)
- On level-up: stat recalculation per Temper growth weights + personality modifier + player free points (15%)
- New move unlock check
- Bond small increment on win
- Level-up UI popup
- **Tags:** GDD §5 + §21 Locked · No XP on loss · Temper-weighted growth

#### Mo 9 — Battle VFX + audio hooks + loss state
- Attack animations: sprite shake, flash on hit
- Particle system for hit effects (Asset Store VFX pack)
- HP bar tween on damage (DOTween)
- Screen shake on critical hit
- Loss state: currency/item cost only — zero XP loss, zero bond loss from combat outcome, no stat regression
- Audio placeholder hooks for Phase 5
- **Tags:** DOTween · Particle System · Asset Store VFX · GDD §21 loss state Locked

### Phase 3 Gate — First Playable
> Full encounter → battle → skill trees → status chains → capture/win loop works end-to-end. Skill tree framework live with placeholder content. Timed inputs functional. Bond gates Type F and O. Combo engine fires. This is your vertical slice demo. If this loop isn't fun, find out now — not at month 16.

---

## Phase 4 — Evolution Web + World Depth
**Timeline:** Months 10–15  
**Goal:** Full evolution system, two explorable zones, save system, alpha build  
**GDD sections:** §3 (Evolution — Locked) · §5 (Unnamed pool — Locked) · §19 (World — Partial) · §21 (Progression — Locked v0.7.9)

> Evolution system is fully locked (GDD §3). Three evolution types, branch requirement framework, devolution with unnamed pool growth, and the met-conditionals-persist rule are all specified and ready to implement.

### Evolution System

#### Mo 10 — Standard evolution — branch requirement engine
- Level floor check (anti-exploit only — low threshold, natural play hits it quickly)
- Stat threshold checks: up to 3 stats, both base stats AND unnamed pool count toward thresholds
- Conditional checks: up to 2 per branch, one-time checks that persist forever across all devolution cycles
- Present evolution choice UI when all thresholds met
- **Tags:** GDD §3 Locked · Two-layer stat check · Conditionals persist forever

#### Mo 10–11 — Devolution + unnamed pool growth
- On devolve: base stats reset to tier floor, unnamed pool grows by `excessStats × bondMultiplier`
- Bond fully preserved on devolve
- Aptitude grows on devolve
- All evolution branches reopen
- Skill library preserved — all previously learned skills remain accessible
- High bond gain on devolve/re-evolve cycle (highest single bond gain action)
- **Tags:** GDD §3 + §5 Locked · Pool growth formula · Skill library preserved

#### Mo 11 — Item-gated evolution + Fusion scaffold
- Item-gated: standard threshold met + specific key item consumed on top; item returned on devolve
- Fusion: two creatures at matching tiers, one designated primary; ingredients consumed; new form born
- On fusion devolve: all ingredients returned, primary carries all skills banked, secondaries frozen at fusion moment
- Type N (Fusion) tree scaffold — content pending species roster
- **Tags:** GDD §3 Locked · Item returned on devolve · Type N scaffold

#### Mo 11–12 — Tier slot unlock — skill trees per tier
- UI shows available tree types and active slot loadout per current tier
- Player selects which unlocked trees to equip
- Slot counts enforced: T1=2, T2=3, T3=4, T4=5, T5=5–7
- **Tags:** GDD §3 Locked · Slot loadout UI

### World & Progression

#### Mo 12–13 — World chunk streaming — 2 zones
- WorldChunkManager from Technical Directive
- Build Region 1 (Living World): Primal type always fully visible, full encounter population
- Scaffold Region 2 (Constructed World): Signal type revealed via visual/audio cues only — no text tooltips
- Zone-specific encounter tables, music swap, ambient VFX
- **Tags:** GDD §8 + §19 Locked · Region 1 full reveal · Region 2 cue-only

#### Mo 13 — Progression loop pacing
- Evolution pacing: T1→T2 fast (1–2 sessions), T2→T3 moderate, T3→T4 longer, T4→T5 significant investment
- Session flexibility: 20-min session yields meaningful bond and stat progress; 3-hr session can push an evolution cycle
- No session length punishment
- Loss framework enforced: losing costs currency/items only, no XP or bond loss from combat outcome
- **Tags:** GDD §21 Locked · No progress loss on defeat · Emergent reward

#### Mo 14–15 — NPC system + dialogue + save system
- Interactable NPCs with Yarn Spinner dialogue trees
- Trainer battles (NPCs with Phasix teams)
- Save system: serialize party, storage box, bond values, unnamed pool, world position, evolution conditionals met, skill library to JSON
- Multiple save slots
- Load on startup
- **Tags:** Yarn Spinner · JSON save · SaveManager

### Phase 4 Gate — Alpha Build
> Full evolution web works (standard + item-gated + fusion scaffold). Devolution with pool growth functional. Two zones explorable with correct type reveal logic. Save/load solid. Hand to a friend — first external playtest. Gather feedback before Phase 5.

---

## Phase 5 — Polish, Skill Content & Public Demo
**Timeline:** Months 16–20  
**Goal:** Real skill content, audio, UI polish, enemy AI upgrade, itch.io public demo

> This is when placeholder skill content gets replaced with real designed skills — but only after the species roster design phase is complete. Do not start writing real skill content before the roster exists.

### Milestones

#### Mo 16 — Species roster design phase (design work, not code)
- Design first 6–10 base species with Tempers, Primal types, Signal pools, evolution branches, skill tree assignments
- Output: completed entries in SpeciesRoster.md
- Output: MonsterData ScriptableObject assets and SkillData assets with real content
- This is a design task — no Unity work until roster entries are approved
- **Tags:** GDD §25 Pending · Design before code · 6–10 species minimum · SpeciesRoster.md

#### Mo 16–17 — Real skill content — replace all placeholders
- Populate all 18 tree types with real designed skills from roster phase
- Every skill must scale with at least one attribute (GDD §14 mandate — no flat skills)
- Status-applying skills: duration scales with Resonance, resistance via Resolve
- Expiry triggers implemented per status design
- Combo table populated with real species-specific combinations
- **Tags:** GDD §14 Content pending fills here · Attribute scaling mandatory

#### Mo 17 — Enemy AI upgrade — strategic priority system
- Replace random move selection with priority AI:
  - Heal if HP below 30%
  - Use super-effective Primal type move if available
  - Apply status to create vulnerability window
  - Exploit existing vulnerability window
  - Use appropriate Tempo action economy
- "Think, don't grind" philosophy (GDD §16) — correct loadout beats overleveled wrong loadout
- **Tags:** GDD §16 Philosophy locked · Status loop AI · Type knowledge AI

#### Mo 17–18 — Audio system + full UI pass
- Audio Manager singleton (FMOD or Unity built-in — see DECISIONS.md)
- Zone BGM per region, battle music with intro + loop, SFX per skill type
- Full UI redesign: party screen, storage box, skill tree browser showing all 18 types, bond display showing both stat layers, unnamed pool display (use `[POOL_NAME]` until locked)
- Asset Store or free JRPG audio packs
- **Tags:** AudioManager · UI Toolkit · Unnamed pool UI · FMOD decision

#### Mo 19–20 — Performance pass + public demo build
- Unity Profiler pass: identify and fix bottlenecks
- Object pooling for encounters, projectiles, VFX
- Sprite atlas consolidation
- Addressables for zone assets
- Target: 60fps on mid-tier hardware

**Demo scope (locked before build):**
- 1 zone (Region 1 complete)
- 6–8 catchable Phasix from real roster
- 2 full evolution chains (1 Standard + 1 item-gated)
- All 18 skill tree types populated with real content
- 3 trainer battles with strategic AI
- Full bond system and floor logic
- Save/load functional
- Build: Windows + Mac
- Release: itch.io

### Phase 5 Gate — Public Demo
> Polished demo on itch.io with real skill content, full bond system, working evolution chains, and strategic AI. Real feedback from real players. This gate determines next steps: continue toward full release, seek publisher, or pivot scope.

---

## What Is Not In This Roadmap

These GDD systems are pending design and have no implementation phase assigned yet. They appear in the GDD as pending sections and will require a new roadmap revision when design is complete.

| System | GDD Section | Status | Notes |
|--------|-------------|--------|-------|
| Full species roster (beyond demo 6–10) | §25 | Pending | Designed during Phase 5 for demo; full roster is post-demo work |
| Economy & itemisation | §22 | Pending | Currencies, shops, loot — no phase assigned |
| Survival crafting | §20 | Pending | Resources, gear, base building — no phase assigned |
| NPC / story system | §24 | Pending | Quest content, story beats — scaffold in Phase 4, content post-demo |
| Audio design detail | §27 | Pending | Music and SFX design — technical hooks in Phase 5, full design post-demo |
| Multiplayer | §28 | Pending | No phase assigned |
| Region 3 + Region 4 | §19 | Partial | Ancient World and Broken World — post-demo |
| Enemy design system | §18 | Pending | Boss archetypes, stat scaling — basic AI in Phase 5, full design post-demo |
| All numerical calibration | §29 | Pending | XP curves, bond gain %, pool growth formula — tracked in NumericalCalibration.md |
| Celestial evolution content | §13 | Pending | T4–T5 branches designed per species during roster phase |

---

## Deferred Technical Decisions

These are implementation choices that cannot be made until development reaches that phase. Tracked in DECISIONS.md.

| Decision | Deferred Until | Notes |
|----------|---------------|-------|
| Tile base size (16×16 or 32×32) | Phase 1 Wk 5 | Determined by first Asset Store tileset purchased |
| A* Pathfinding grid cell size | Phase 1 Wk 5 | Depends on tile base size |
| Audio middleware (FMOD vs Unity built-in) | Phase 5 | FMOD preferred for adaptive music; Unity Audio sufficient if scope stays simple |
| Animation tooling (Spine vs Unity 2D Animation) | Post-demo | Deferred until custom art pipeline decision is made |
| Art style lock | Post Phase 3 gate | Lock after vertical slice is playable and fun, not before |
| Additive scene streaming (vs GameObject chunks) | If profiler shows need | Migrate only if memory pressure measured at 200+ chunks |

---

## Roadmap Update Log

| Version | Date | Changes |
|---------|------|---------|
| v1.0 | March 2026 | Initial roadmap — 18 months, 5 phases, generic structure |
| v2.0 | March 2026 | GDD-aligned revision — skill tree system integrated, 9-attribute schema, full bond floor logic, status engine detail, 20-month timeline, all pending items flagged |
