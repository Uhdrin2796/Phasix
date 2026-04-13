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
- **Decided:** Multiple Hubs + discrete Realms with conditional hub evolution elements. No single designated main hub. Each hub has a functional specialization creating player routing decisions. Hubs unlock progressively.
- **Why:** Most implementable structure. Hub network provides quest/story anchors. Realms provide discrete emotional zones. Specialization creates meaningful travel decisions at the hub scale, mirroring the path-routing decision inside Realms.
- **Alternatives rejected:** Single main hub (too centralized, limits routing decisions), seamless geography (hard to pace), Wanderer model (too directionless)
- **Date:** April 2026
- **Revisit if:** Hub count or specializations conflict with narrative shape once story develops
- **Ref:** WorldDesign_Directive_v0_1_0.md

### [World] Phasix visibility model
- **Decided:** Allergy framing — perceiving Phasix is a sensitivity, spectrum-based, not a superpower. Only sensitivity-havers can perceive Phasix and engage with the emotional dimension.
- **Why:** Removes chosen-one framing. Makes sensitivity feel human and unremarkable.
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
- **Why:** Prevents the game from drifting toward shadow and struggle as the only emotionally interesting design space.
- **Date:** March 2026
- **Ref:** Progression_Directive_v0_1_0.md

### [Story] Faction framework — working names
- **Decided:** Four working faction philosophies — Suppressors, Amplifiers, Avoiders, Integrators. Names and details are exploratory, flagged for refinement.
- **Why:** Factions as emotional worldviews rather than good/evil alignment. Every faction philosophy is an understandable coping response.
- **Alternatives rejected:** Traditional good/evil factions, no factions (loses conflict source)
- **Date:** March 2026
- **Revisit:** Names and lore details require full design session before GDD entry
- **Ref:** WorldDesign_Directive_v0_1_0.md

### [Lore] Old lore status
- **Decided:** The Fracture event, Phase Dimension details, and original Five Factions lore are retained in LoreBible_Phasix.html as REFERENCE ONLY. These were auto-filled without approval in a prior session and have shifted significantly. Do not implement. Require full revisit.
- **Date:** March 2026

---

## New Entries — April 2026 Design Session

### [World] World structure — Multiple Hubs confirmed
- **Update:** The March 2026 "Hub + Realms" entry above has been superseded by the April 2026 model below.
- **Decided:** Multiple Hubs + discrete Realms with conditional hub evolution elements. No single designated main hub. Each hub has a functional specialization creating player routing decisions. Hubs unlock progressively.
- **Why:** Most implementable structure. Hub network provides quest/story anchors. Realms provide discrete emotional zones. Specialization creates meaningful travel decisions at the hub scale, mirroring the path-routing decision inside Realms.
- **Alternatives rejected:** Single main hub (too centralized, limits routing decisions), seamless geography (hard to pace), Wanderer model (too directionless)
- **Date:** April 2026
- **Revisit if:** Hub count or specializations conflict with narrative shape once story develops
- **Ref:** WorldDesign_Directive_v0_1_0.md

### [World] Blackout and banking system
- **Decided:** On party wipe (blackout), player returns to last visited hub. Phasix are always kept — no permadeath, no forced devolution. Aura, loot, and currency collected since the last hub visit are lost unless banked. Banking at a hub makes resources permanent.
- **Why:** Creates meaningful risk/reward around pushing deeper into a Realm vs. returning to bank. Stakes are resource-based, not roster-based. Emotionally congruent — the things you were reaching for slip away when you fall.
- **Alternatives rejected:** Full permadeath (too punishing for emotional design tone), no stakes on blackout (removes tension), losing Phasix on blackout (conflicts with core design that Phasix are irreplaceable emotional relationships)
- **Date:** April 2026
- **Revisit if:** Playtesting shows unbanked loss feels arbitrary rather than meaningful
- **Ref:** WorldDesign_Directive_v0_1_0.md

### [Art/Tech] Perspective model — overworld and combat
- **Decided:** Overworld uses 3/4 oblique top-down view. Combat uses side-profile diorama view. Orthogonal input maps to diagonal movement in the world.
- **Why:** 3/4 oblique is the natural RPG exploration perspective. Side-profile combat gives maximum visibility of individual Phasix art. Orthogonal-to-diagonal movement mapping is the standard solution for 3/4 perspective awkwardness.
- **Alternatives rejected:** Pure top-down overworld (loses depth), matching perspective for both states (combat loses Phasix showcase opportunity), free 8-directional movement (creates rig complexity and visual awkwardness in 3/4 view)
- **Date:** April 2026
- **Ref:** WorldDesign_Directive_v0_1_0.md, Combat_Directive_v0_1_0.md

### [Art/Tech] Bone rig — two rigs per Phasix, three overworld directions
- **Decided:** Each Phasix has two bone rigs — 3/4 oblique for overworld, side-profile for combat. Overworld rig covers three directions: right-facing (left via flip), up-diagonal, down-diagonal. Additional directions deferred.
- **Why:** Minimum viable rig set for solo dev. Two rigs per Phasix is manageable. Three directions cover all movement cases for now without committing to 8-directional before the pipeline is validated.
- **Alternatives rejected:** 8-directional overworld rig (prohibitive solo), single shared rig for both perspectives (compromises art quality in both states)
- **Date:** April 2026
- **Revisit if:** Up/down-diagonal share too many frames and look wrong in motion; add directions post-prototype
- **Note:** Specific rigging tool (Spine vs Unity 2D Animation) deferred until prototype validates the need. Do not purchase Spine before that point.
- **Ref:** WorldDesign_Directive_v0_1_0.md

---

## New Entries — April 2026 Tilemap Session

### [Camera] Pixel Perfect Camera PPU locked
- **Decided:** Pixel Perfect Camera → Asset Pixels Per Unit = 16, Reference Resolution = 320×180
- **Why:** 16 PPU is the correct baseline for the 320×180 virtual canvas. Established in Phase 1 Wk 5–6.
- **Alternatives rejected:** 32 PPU (doubles world unit scale, mismatches established creature sprite pipeline)
- **Date:** April 2026
- **Revisit if:** Never — PPU is locked to the camera resolution choice

### [Tileset] Tile pixel size — PENDING
- **Status:** Undecided — tile grid PNG not yet sourced
- **Impact:** Determines world unit scale per tile. 16×16 px = 1 unit/tile at 16 PPU. 32×32 px = 2 units/tile at 16 PPU.
- **Impact on A* cell size:** A* node size = tile world unit size (or half for finer navigation)
- **Revisit:** When real tileset PNG is imported — lock this entry immediately

### [Camera] Cinemachine version
- **Decided:** Cinemachine 3.1.x (Unity 6 package). Using `CinemachineCamera` component (not legacy `CinemachineVirtualCamera`). CinemachineConfiner2D for room boundary confinement. CinemachinePixelPerfect extension for lens sync.
- **Why:** Cinemachine 3.x is the current API for Unity 6. Legacy VirtualCamera API is deprecated.
- **Alternatives rejected:** Legacy CinemachineVirtualCamera (deprecated in 3.x)
- **Date:** April 2026

### [Git] Large art assets excluded from git — stored externally
- **Decided:** `Assets/Artwork/Creatures/` and `Assets/Artwork/Tilesets/` are excluded from git via `.gitignore`. Store these folders in Google Drive / OneDrive and copy them into the project locally as needed.
- **Why:** Thousands of individual PNG frames in creature packs hit GitHub LFS rate limits immediately. Raw art packs don't need version history or PR review — they're reference assets, not code.
- **Alternatives rejected:** Git LFS (1GB free limit fills fast with PNG packs, then $5/month); Git Submodules (unnecessary complexity for solo dev)
- **How to restore on a new machine:** Copy `Creatures/` and `Tilesets/` folders from Google Drive into `Assets/Artwork/` before opening the project in Unity.
- **Date:** April 2026

### [Tilemap] Placeholder tiles for test room
- **Decided:** Unity built-in white square sprite used as placeholder for ground and wall tiles. Green (`#4A7C3F`) = ground, dark grey (`#333333`) = walls.
- **Why:** Real tileset PNG not yet sourced. Placeholder lets tilemap, WorldChunkManager, and Cinemachine camera be fully tested now. Swap in real art without script changes.
- **Date:** April 2026
- **Revisit if:** Real tileset is sourced — replace tile sprites in Tile assets, no code changes needed
