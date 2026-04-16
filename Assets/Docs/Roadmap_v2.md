# Phasix — Development Roadmap
**Version:** 2.1
**Date:** March 2026 (v2.1 reconciled March 2026)
**Engine:** Unity Latest LTS · 2D URP
**Path:** Asset Store sprites · Claude Code scripting
**Scope:** Complete beginner · 5–10 hrs/week
**GDD ref:** v0.8.0 · Progression_Directive v0.1.0 · WorldDesign_Directive v0.1.0 · Technical Directive v0.1.0

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
**GDD sections:** §5 (Attributes — Locked), §6 (Bond — Locked), §7 (Personality — Locked), §8 (Typing — Locked)
**Directives:** Progression_Directive v0.1.0 (Aura system — supersedes GDD §21) · Primer §9 (evolution rules — supersedes GDD §3)

> All systems in this phase map to fully locked sections. Build exactly what the GDD and Directives specify — no invention.

### Milestones

#### Phase 2 Kickoff — Core: EventBus + GameManager + GameStrings
- Create `Assets/Scripts/Core/EventBus.cs` — static class, typed `event Action<T>` delegates, null-safe Raise helpers
  - Phase 2 live events: `OnBondChanged`, `OnBondMilestoneReached`
  - Phase 3 stubs (defined, no subscribers yet): `OnBattleWon`, `OnBattleLost`, `OnSkillUsed`, `OnTimedInputSuccess`, `OnDamageTaken`
  - Phase 4 stubs: `OnEvolved`, `OnDevolved`, `OnPhasixCaptured`, `OnAuraDropped`
- Create `Assets/Scripts/Core/GameManager.cs` — singleton MonoBehaviour skeleton, `DontDestroyOnLoad`
  - Add `_GameManager` empty GameObject to SampleScene with script attached
  - No logic — placeholder for future system references (BattleManager, SaveManager, etc.)
- Create `Assets/Scripts/Core/GameStrings.cs` — static class with pending display name constants
  - `GameStrings.PoolName` and `GameStrings.SensitivityName` — reference everywhere in UI, never hardcode
  - When names are decided: update the constant here, entire game updates automatically
- **Tags:** Core/ · EventBus static · GameManager singleton · GameStrings constants

#### Wk 9 — PhasixData ScriptableObject — full schema
- Implement all 8 base stats: Vitality, Force, Resonance, Guard, Ward, Resolve, Instinct, Aura
- Temper enum (Edge / Anchor / Flux)
- Personality enum (16 traits)
- Two-layer stat system: base stats layer + unnamed pool layer — both always visible in UI
- Reference `GameStrings.PoolName` for unnamed pool in all UI strings — never hardcode; update the constant in GameStrings.cs when name is decided
- Bond float (0–100), bondFloor float, phaseSaturation float
- **Aptitude** — separate persistent int field (devolution counter, never resets). Grows +1 per devo cycle. Not a base stat — do not put in the 8-stat group.
- **Aura resources** — NOT on the ScriptableObject. Runtime state in save data only: commonAura, specificAuraDict (by emotional type), rareVariantAura. Scaffold the save-data struct here; implementation in Phase 4 save system.
- **Tags:** ScriptableObject · 8 base stats · Aptitude persistent · Unnamed pool · GDD §5 Locked

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
- Store as enum on PhasixData
- **Tags:** 16 traits · Growth modifier · GDD §7 Locked

#### Wk 12–13 — Companion following AI
- Active Phasix follows player using A* Pathfinding Project (free/Lite)
- Idle / Walk / Run animation states driven by distance to player
- Offset from player, obstacle avoidance
- Party system: up to 3 slots, active companion follows, others stored
- **Tags:** A* Pathfinding · Animator · Party system

#### Wk 14–16 — Wild encounter trigger + Primal type reveal
- Zone collider triggers wild encounter (scaffold-level — simple Trigger2D)
- Spawn wild Phasix with PhasixData populated
- Primal type (first Realm reveal axis) always fully visible — no discovery mechanic here
- Signal / Tempo / Celestial axes hidden — no UI for those yet
- Flee or engage choice
- **Note:** This is the scaffold trigger only. The full three-layer encounter system (Emotional Mirroring / Attunement / Failure-Triggered — WorldDesign_Directive) replaces this in Phase 3–4 once the emotional state tracking and calendar systems exist.
- **Tags:** Primal type visible · Trigger2D · GDD §8 Locked

### Phase 2 Gate
> Full PhasixData ScriptableObject live (8 base stats, Aptitude field, unnamed pool, bond, Aura resource struct). Companion follows via pathfinding. Bond zones and floors functional. Wild encounters trigger via scaffold collider. All data matches GDD §5–§7 + Primer §9 schema exactly.

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
- **Active party size:** Use `BattleConfig.ActivePartySize = 3` as prototype constant — never magic-number this value. Revisit at Phase 3 gate before building full battle UI; confirm or revise to final value then.
- **IK driving script:** Defer to this phase. Evaluate Option D — arm IK targets move toward target lane during attack animations. See `LESSONS_LEARNED.md §2D IK` before implementing.
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

#### Mo 8 Wk 4 — Aura system + stat allocation
**GDD §21 XP/leveling is superseded. Implement the Aura system from Progression_Directive v0.1.0.**
- Common Aura drops on battle win (zero on loss — no progression reversal)
- AuraManager (runtime, save data) tracks Common / Specific / RareVariant Aura per Phasix
- Stat allocation UI: player spends Common Aura to assign stat points freely across all 8 attributes
- Resonance Bonus layer: stat points allocated to attributes matching `emotionalType` generate passive bonuses — scaffold the system, exact bonuses pending NumericalCalibration.md
- Stat ceiling enforced per tier: Aptitude 0 baseline + Aptitude bonus ceiling (all values pending NumericalCalibration.md — use placeholder constants with `// TODO`)
- Hitting the stat ceiling is the natural signal to evolve — no level-up popup, no unlock check separate from ceiling
- Bond small increment on win (unchanged)
- **Tags:** Progression_Directive v0.1.0 · No Aura on loss · Free allocation · Resonance Bonus scaffold

#### Mo 9 — Battle VFX + audio hooks + loss state
- Attack animations: sprite shake, flash on hit
- Particle system for hit effects (Asset Store VFX pack)
- HP bar tween on damage (DOTween)
- Screen shake on critical hit
- Loss state: currency/item cost only — zero Aura loss, zero bond loss from combat outcome, no stat regression
- Audio placeholder hooks for Phase 5
- **Tags:** DOTween · Particle System · Asset Store VFX · Progression_Directive loss state

### Phase 3 Gate — First Playable
> Full encounter → battle → skill trees → status chains → capture/win loop works end-to-end. Skill tree framework live with placeholder content. Timed inputs functional. Bond gates Type F and O. Combo engine fires. This is your vertical slice demo. If this loop isn't fun, find out now — not at month 16.

---

## Phase 4 — Evolution Web + World Depth
**Timeline:** Months 10–15  
**Goal:** Full evolution system, two explorable zones, save system, alpha build  
**GDD sections:** §3 (Evolution — Locked) · §5 (Unnamed pool — Locked) · §19 (World — Partial)
**Directives:** Progression_Directive v0.1.0 (Aura system — supersedes GDD §21) · WorldDesign_Directive v0.1.0 (Hub + Realms structure — supplements GDD §19) · Primer §9 (evolution rules)

> Evolution system is fully locked (GDD §3 + Primer §9). Three evolution types, branch requirement framework, devolution with unnamed pool growth, and the met-conditionals-persist rule are all specified and ready to implement. GDD §21 XP/leveling is superseded — use Progression_Directive.

### Evolution System

#### Mo 10 — Standard evolution — branch requirement engine
- Stat minimum check (replaces level floor — Progression_Directive): low threshold, anti-exploit only, natural play hits it quickly
- Stat threshold checks: up to 3 stats, both base stats AND unnamed pool count toward thresholds
- Aura gate: Specific Aura required for T2+; type and quantity pending NumericalCalibration.md — scaffold with `// TODO` constants
- Conditional checks: up to 2 per branch, one-time checks that persist forever across all devolution cycles
- All three gates must be met simultaneously before evolution choice is presented
- **Tags:** Progression_Directive · Primer §9 · Stat minimum replaces level floor · Two-layer stat check · Conditionals persist forever

#### Mo 10–11 — Devolution + unnamed pool growth
- On devolve: base stats reset to tier floor, unnamed pool grows by `excessStats × bondMultiplier`
- **Devolution is FREE — no cost, no conditions, no time limit.** Authority: Evolution_System_Directive_v1_1_0
- Bond fully preserved on devolve
- Aptitude grows +1 on devolve (raises stat ceiling for next cycle — see Progression_Directive Function A)
- Higher Aptitude before devolving = larger unnamed pool gain (side effect of higher stat ceiling = more excess stats)
- All evolution branches reopen
- Skill library preserved — all previously learned skills remain accessible
- High bond gain on devolve/re-evolve cycle (highest single bond gain action)
- **Tags:** Progression_Directive · GDD §3 + §5 Locked · Devolution FREE · Pool growth formula · Aptitude +1 · Skill library preserved

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

#### Mo 12–13 — World chunk streaming — Hub + 2 Realms
- WorldChunkManager from Technical Directive
- Hub scaffold: central space for NPCs, evolution moments, social systems — physical and tonal identity pending world design session; build as placeholder geometry now
- Build first Realm (emotional identity pending world design session): Primal type always fully visible, full encounter population. In type-reveal order this is "Region 1."
- Scaffold second Realm (emotional identity pending world design session): Signal type revealed via visual/audio cues only — no text tooltips. In type-reveal order this is "Region 2."
- Zone-specific encounter tables, music swap, ambient VFX
- **Note:** Realm count, emotional identities, and Hub character are all pending the world design session — build geometry and systems, leave identity slots empty with `// TODO: pending world design session`
- **Tags:** WorldDesign_Directive · GDD §8 + §19 · Hub scaffold · Realm 1 full reveal · Realm 2 cue-only · Identities pending

#### Mo 13 — Progression loop pacing
- Evolution pacing: T1→T2 fast (1–2 sessions), T2→T3 moderate, T3→T4 longer, T4→T5 significant investment
- Session flexibility: 20-min session yields meaningful bond and stat progress; 3-hr session can push an evolution cycle
- No session length punishment
- Loss framework enforced: losing costs currency/items only, no Aura or bond loss from combat outcome
- **Tags:** Progression_Directive · No Aura loss on defeat · No progress loss on defeat · Emergent reward

#### Mo 14–15 — NPC system + dialogue + save system
- Interactable NPCs with Yarn Spinner dialogue trees
- Trainer battles (NPCs with Phasix teams)
- Save system: serialize party, storage box, bond values, unnamed pool, world position, evolution conditionals met, skill library to JSON
- Multiple save slots
- Load on startup
- **Tags:** Yarn Spinner · JSON save · SaveManager

### Phase 4 Gate — Alpha Build
> Full evolution web works (standard + item-gated + fusion scaffold). Devolution with Aura cost and pool growth functional. Aptitude cycling produces measurable stat ceiling increase. Hub + two Realms explorable with correct type reveal logic. Aura drops and Specific Aura evolution gating working end-to-end. Save/load solid. Hand to a friend — first external playtest. Gather feedback before Phase 5.

---

## Phase 5 — Polish, Skill Content & Public Demo
**Timeline:** Months 16–20  
**Goal:** Real skill content, audio, UI polish, enemy AI upgrade, itch.io public demo

> This is when placeholder skill content gets replaced with real designed skills — but only after the species roster design phase is complete. Do not start writing real skill content before the roster exists.

### Milestones

#### Mo 16 — Species roster design phase (design work, not code)
- Design first 6–10 base species with Tempers, Primal types, Signal pools, evolution branches, skill tree assignments
- Output: completed entries in SpeciesRoster.md
- Output: PhasixData ScriptableObject assets and SkillData assets with real content
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
- Full UI redesign: party screen, storage box, skill tree browser showing all 18 types, bond display showing both stat layers, unnamed pool display (reference `GameStrings.PoolName` — update the constant in GameStrings.cs when name is decided)
- Asset Store or free JRPG audio packs
- **Tags:** AudioManager · UI Toolkit · Unnamed pool UI · FMOD decision

#### Mo 19–20 — Performance pass + public demo build
- Unity Profiler pass: identify and fix bottlenecks
- Object pooling for encounters, projectiles, VFX
- Sprite atlas consolidation
- Addressables for zone assets
- Target: 60fps on mid-tier hardware

**Demo scope (locked before build):**
- 1 Realm (first Realm complete — emotional identity pending world design session)
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
| Realms 3+ | §19 | Partial | Additional Realms beyond the two in Phase 4 — emotional identities and count pending world design session |
| Hub identity and content | §19 · §24 | Pending | Physical layout, tonal identity, NPC roster — pending world design session |
| Hub + Realm emotional identities | §19 | Pending | All Realm and Hub identities deferred — WorldDesign_Directive |
| Enemy design system | §18 | Pending | Boss archetypes, stat scaling — basic AI in Phase 5, full design post-demo |
| All numerical calibration | §29 | Pending | Aura system values, bond gain %, pool growth formula, stat ceilings — tracked in NumericalCalibration.md |
| Calendar system | WorldDesign_Directive | Pending | Story-beat-driven month progression, seasonal Aura availability — no phase assigned; integrates in Phase 4 once system exists |
| Three-layer encounter system | WorldDesign_Directive | Pending | Emotional Mirroring / Attunement / Failure-Triggered layers scaffold in Phase 3–4 once calendar and emotional state tracking exist |
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
| v2.1 | March 2026 | Design session sync — Aura system replaces XP/leveling throughout (Progression_Directive), stat minimum replaces level floor, devolution costs Specific Aura, Aptitude mechanics clarified, Hub + Realms replaces Region 1/2/3/4 identity refs (WorldDesign_Directive), 8 base stats (Aptitude removed from stat group), PhasixData replaces MonsterData, loss state refs updated (no Aura loss), pending table updated with Calendar/3-layer encounter/Hub+Realm identity gaps |
