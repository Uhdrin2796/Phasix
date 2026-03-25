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

### [Input] Input architecture for PlayerController
- **Decided:** `InputActionAsset` (`.inputactions` file) referenced via `[SerializeField] private InputActionAsset _inputActions`. Actions resolved manually in `Awake()` via `FindActionMap("Player").FindAction("Move")`. Subscribed in `OnEnable`, unsubscribed in `OnDisable`. No `PlayerInput` component.
- **Asset in use:** `Assets/InputSystem_Actions.inputactions` (Unity default, already had `Player` map + `Move` action)
- **Why:** Explicit per-map enable/disable is required for battle vs overworld context switching in Phase 3. `PlayerInput` component wires via Inspector string names that break on rename.
- **Alternatives rejected:**
  - Inline `InputAction` fields — not rebind-compatible, not multi-scheme scalable, requires per-script duplication
  - `InputSystem.actions` global (Unity 6) — project-wide single asset, no per-component override, no per-map enable/disable control
  - `PlayerInput` component with Invoke Unity Events — Inspector wiring breaks on action rename, harder to control from BattleManager
- **Date:** March 2026
- **Revisit if:** Phase 3 battle system requires more complex input routing than per-map enable/disable covers

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
- **Decided:** Unity built-in Animator + AnimationClips (sprite-swap animation)
- **Why:** Asset Store / custom pixel-art sheets work natively; no third-party dependency needed for Phase 1
- **Alternatives rejected:** Spine (deferred — purchase pending post-demo if custom rigged animation becomes a priority)
- **Date:** March 2026
- **Revisit if:** Creature animations grow too complex for frame-by-frame sprite swap

### [Art] Sprite import settings
- **Decided:** Pixels Per Unit = 32, Filter Mode = Point (no filter), Compression = None on all creature sprite sheets
- **Why:** Pixel-art must not be blurred or dithered by compression. PPU 32 is a reasonable starting value for the 320×180 Pixel Perfect Camera; adjust if sprites look too large/small in the scene.
- **Alternatives rejected:** Default Unity import settings (bilinear filter + compressed — breaks pixel art)
- **Date:** March 2026
- **Revisit if:** Tileset PPU is chosen as 16 instead of 32 — all creature sprites must match to avoid scale mismatch

### [Art] Dark Fluffy sprite version
- **Status:** Undecided — v1 (pink/purple effects) and v2 (blue/cyan effects) both available; user reviewing
- **Note:** Running sheets (white body + dark purple body) are independent of v1/v2 composite choice
- **Revisit:** Once user decides on creature color palette / evolution visual language

### [A* grid] Cell size
- **Status:** Undecided — depends on tile size chosen
- **Revisit:** Phase 1, during tilemap setup
- **Note:** Common values: 0.5 units (32px tiles at 16 PPU), 1.0 unit (32px tiles at 32 PPU)

### [Tileset] Tile base size
- **Status:** Undecided — 16×16 or 32×32
- **Revisit:** Phase 1, when first Asset Store tileset is purchased
- **Note:** Technical Directive references 16×16 or 32×32 as valid; choice locks pixel-per-unit settings
