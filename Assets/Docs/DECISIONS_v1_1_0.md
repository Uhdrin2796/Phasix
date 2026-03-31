# Phasix — Implementation Decisions Register
Decisions made during development that are NOT in the GDD.
Claude Code reads this to avoid undoing or contradicting settled choices.
Add an entry any time you make a choice that isn't obvious from the GDD.

---

## Format
```
### [System] Decision title
- **Decided:** What was chosen
- **Why:** Rationale
- **Alternatives rejected:** What was considered and why it lost
- **Date:** When decided
- **Revisit if:** Condition under which this should be reconsidered
```

---

## Engine & Infrastructure

### [Engine] Unity version
- **Decided:** Unity Latest LTS at time of project creation
- **Why:** LTS = 2 years of patch support. Stability over features.
- **Alternatives rejected:** Latest Tech Stream — too unstable for a long project
- **Date:** March 2026
- **Revisit if:** LTS support ends before project ships

### [Engine] Render pipeline
- **Decided:** 2D URP (Universal Render Pipeline)
- **Why:** Built-in 2D lighting, shadow casters, sprite sorting. Required for visual effects.
- **Alternatives rejected:** Built-in RP (no 2D lighting), HDRP (overkill for 2D)
- **Date:** March 2026
- **Revisit if:** Never — switching pipelines mid-project is prohibitively expensive

### [Engine] Pixel resolution
- **Decided:** 320×180 reference resolution, scaled up via Pixel Perfect Camera component
- **Why:** Crisp pixel-art look at 16:9. Standard for indie pixel-art games.
- **Alternatives rejected:** Native resolution rendering (blurry at scale)
- **Date:** March 2026
- **Revisit if:** Art style pivots away from pixel-art entirely

### [Input] Input system
- **Decided:** Unity new Input System (not legacy Input.GetAxis)
- **Why:** Gamepad + keyboard support, rebinding support, cleaner architecture
- **Alternatives rejected:** Legacy Input — deprecated, no rebinding
- **Date:** March 2026
- **Revisit if:** Never — new Input System is the correct choice going forward

---

## Art & Assets

### [Art] Art pipeline
- **Decided:** Asset Store sprites for Phase 1–5. No custom art pipeline until post-demo.
- **Why:** Fastest path to playable. Avoids art blocking development.
- **Alternatives rejected:** Hades-style pre-render pipeline (deferred — high overhead, requires Blender skill or outsourcing)
- **Date:** March 2026
- **Revisit if:** Public demo attracts funding or collaborators

### [Art] Art style
- **Decided:** TBD — intentionally kept open. Asset Store style for now.
- **Why:** Art style should be locked after GDD and prototype are stable, not before.
- **Alternatives rejected:** Stardew Valley pixel-art (explicitly set aside as reference), Chibi/Clash Royale style (considered), JJK/MAPPA anime style (considered but not locked)
- **Date:** March 2026
- **Revisit if:** After Phase 3 vertical slice is playable and fun

### [Creature name] Working classifier name
- **Decided:** Phasix (phase + -ix; beings of ever-changing form) — working name only
- **Why:** Needed a working name for development. Not written into GDD yet.
- **Alternatives considered:** Aethrix, Gravix, Quantrix
- **Date:** March 2026
- **Revisit if:** Name needs to be locked before any player-facing text is written

---

## World & Architecture

### [World] Chunk management approach
- **Decided:** GameObject Chunks (all in one scene, SetActive toggling) to start
- **Why:** Simpler, faster to iterate. Sufficient for small-to-medium worlds.
- **Alternatives rejected:** Additive Scene Streaming — migrate to this only if Unity Profiler shows memory pressure at 200+ chunks
- **Date:** March 2026
- **Revisit if:** Profiler shows memory spikes from chunk count

### [Pathfinding] Library
- **Decided:** A* Pathfinding Project (free/Lite tier) — Asset Store
- **Why:** 2D grid graphs, dynamic obstacles, Seeker component, well-documented, free
- **Alternatives rejected:** Unity NavMesh with NavMeshSurface2D — less intuitive for tile-based worlds
- **Date:** March 2026
- **Revisit if:** Free tier hits feature ceiling (upgrade to Pro, not switch library)

---

## Combat & Systems

### [Tweening] Animation library
- **Decided:** DOTween (free version)
- **Why:** Industry standard for Unity tweening. HP bar animations, UI transitions, screen shake.
- **Alternatives rejected:** LeanTween (less documentation), Unity Coroutines only (verbose for tween chains)
- **Date:** March 2026
- **Revisit if:** Never — DOTween is the correct choice

### [Dialogue] Dialogue system
- **Decided:** Yarn Spinner (free)
- **Why:** Open source, well-supported, designed for Unity, good visual node editor
- **Alternatives rejected:** ink (more technical), custom system (unnecessary complexity)
- **Date:** March 2026
- **Revisit if:** Dialogue needs branch complexity beyond Yarn Spinner's capability

### [Battle] Battle scene loading
- **Decided:** Additive scene load for battle (BattleScene_Main)
- **Why:** Keeps overworld loaded in memory for fast return. No full scene swap.
- **Alternatives rejected:** Full scene swap (overworld reload cost)
- **Date:** March 2026
- **Revisit if:** Memory profiling shows overworld + battle too large simultaneously

---

## Data & Save

### [Save] Save format
- **Decided:** JSON serialization via Unity's JsonUtility or Newtonsoft.Json
- **Why:** Human-readable, debuggable, no binary format risk
- **Alternatives rejected:** Binary serialization (hard to debug), PlayerPrefs only (insufficient for complex state)
- **Date:** March 2026
- **Revisit if:** Save file size becomes a concern (migrate to binary then)

### [Data] ScriptableObject write rule
- **Decided:** ScriptableObjects are NEVER written to at runtime
- **Why:** SO changes persist in the Editor and corrupt template data between sessions
- **Enforcement:** Runtime state always lives in plain C# classes serialized to JSON
- **Date:** March 2026
- **Revisit if:** Never — this is a hard architectural rule, not a preference

---

## Pending Decisions (add entries here as choices are made)

### [Audio] Audio middleware
- **Status:** Undecided between FMOD and Unity's built-in Audio system
- **Revisit:** Phase 5, when audio implementation begins
- **Note:** FMOD preferred for adaptive music; Unity Audio sufficient if scope stays simple

### [Animation] Animation tooling
- **Status:** Deferred — Unity's built-in 2D Animation for Asset Store sprites for now
- **Revisit:** After demo, if custom creature animations become a priority
- **Note:** Spine purchase explicitly deferred until GDD and prototype are stable

### [A* grid] Cell size
- **Status:** Undecided — depends on tile size chosen
- **Revisit:** Phase 1, during tilemap setup
- **Note:** Common values: 0.5 units (32px tiles at 16 PPU), 1.0 unit (32px tiles at 32 PPU)

### [Tileset] Tile base size
- **Status:** Undecided — 16×16 or 32×32
- **Revisit:** Phase 1, when first Asset Store tileset is purchased
- **Note:** Technical Directive references 16×16 or 32×32 as valid; choice locks pixel-per-unit settings
---
## New Entries — March 2026 Design Session

### [Progression] XP/leveling replaced by Aura system
- **Decided:** Phasix no longer use XP or levels. Common Aura drives stat growth. Specific Aura gates evolution.
- **Why:** Resource-based progression ties exploration directly to growth. Multi-realm Aura requirements make the evolution web the exploration map.
- **Alternatives rejected:** Pure XP (no meaningful exploration incentive), hybrid XP+Aura (redundant systems)
- **Date:** March 2026
- **Revisit if:** Playtesting shows stat growth feels too slow or disconnected from combat
- **Ref:** Progression_Directive_v0_1_0.md

### [Progression] Aptitude — dual function model
- **Decided:** Aptitude raises stat ceiling per tier (Function A) AND unlocks exotic evolution branches (Function B). Grows through devolution cycles as before.
- **Why:** Preserves original Aptitude design intent (reward devolution cycling) without relying on level caps that no longer exist.
- **Alternatives rejected:** Aptitude as Aura efficiency multiplier, Aptitude as Resonance Bonus scaler (both viable but less impactful)
- **Date:** March 2026
- **Revisit if:** Exotic branch gating via Aptitude feels too punishing for players who prefer linear evolution paths
- **Ref:** Progression_Directive_v0_1_0.md

### [Progression] Evolution requires stat minimums
- **Decided:** Evolution gates include a stat minimum layer (replaces level floor from GDD §3). Must meet Aura requirements AND stat thresholds AND conditionals simultaneously.
- **Why:** Stat floors are player-observable. Cannot rush evolution by farming Aura without actually developing the Phasix.
- **Alternatives rejected:** Aura-only gating (could be farmed without engagement), level floor (superseded)
- **Date:** March 2026
- **Revisit if:** Stat minimums create frustrating bottlenecks in playtesting

### [World] World structure confirmed
- **Decided:** Hub + discrete Realms (Model 1) with light conditional Hub evolution (Model 5 elements)
- **Why:** Most implementable structure. Hub provides quest/story anchor. Realms provide discrete emotional zones. Conditional hub evolution adds soul without system complexity.
- **Alternatives rejected:** Seamless geography (hard to pace), Wanderer model (too directionless)
- **Date:** March 2026
- **Ref:** WorldDesign_Directive_v0_1_0.md

### [World] Phasix visibility model
- **Decided:** Allergy framing — perceiving Phasix is a sensitivity, spectrum-based, not a superpower. Only sensitivity-havers can perceive Phasix and engage with the emotional dimension.
- **Why:** Removes chosen-one framing. Makes sensitivity feel human and unremarkable. JoJo-adjacent visibility logic without the Stand power framing.
- **Alternatives rejected:** Everyone can see Phasix (full world-building complexity), only player can see (too isolating)
- **Date:** March 2026
- **Ref:** WorldDesign_Directive_v0_1_0.md

### [Encounter] Encounter initiation system
- **Decided:** Three layered encounter mechanics replace random encounters entirely — Emotional Mirroring (ambient), Resonance/Attunement (rare/hidden), Failure-Triggered (emotionally heavy)
- **Why:** Every Phasix encounter should feel earned, felt, or discovered. Random encounters conflict with the emotional grounding of the world.
- **Alternatives rejected:** Random probability (anxiety-inducing, no agency), visible overworld spawns alone (functional but loses emotional depth)
- **Date:** March 2026
- **Ref:** WorldDesign_Directive_v0_1_0.md

### [World] Calendar/month system
- **Decided:** Soft time currency driven by story beats. Months carry emotional seasonal context. Content breathes in and out rather than hard resetting. First cycle vs return cycle distinction.
- **Why:** Mirrors the feeling of life — time moves, things drift in and out, nothing is catastrophically missable but windows have weight.
- **Alternatives rejected:** Real-time timer (stressful), player-controlled time (loses urgency entirely), Stardew-style hard season reset (too harsh)
- **Date:** March 2026
- **Ref:** WorldDesign_Directive_v0_1_0.md

### [Design] Positive emotion principle
- **Decided:** No emotion is inherently good or bad. Every emotion is powerful. Every emotion has shadow. Mixed Aura evolutions (requiring both positive and negative Aura) gate the most complex emotional states.
- **Why:** Prevents the game from drifting toward shadow and struggle as the only emotionally interesting design space. Positive emotions are mechanically interesting through their fragility, risk, and complexity — not their cheerfulness.
- **Date:** March 2026
- **Ref:** Progression_Directive_v0_1_0.md

### [Story] Faction framework — working names
- **Decided:** Four working faction philosophies established — Suppressors, Amplifiers, Avoiders, Integrators. Names and details are exploratory, flagged for refinement.
- **Why:** Factions as emotional worldviews rather than good/evil alignment. Every faction philosophy is an understandable coping response to the same underlying sensitivity condition.
- **Alternatives rejected:** Traditional good/evil factions, no factions (loses conflict source)
- **Date:** March 2026
- **Revisit:** Names and lore details require full design session before GDD entry
- **Ref:** WorldDesign_Directive_v0_1_0.md

### [Lore] Old lore status
- **Decided:** The Fracture event, Phase Dimension details, and original Five Factions lore are retained as reference only. These were auto-filled without approval in a prior session and have shifted significantly. Do not implement. Require full revisit.
- **Date:** March 2026
