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

### [Art] Placeholder-first pipeline — colored primitives until the game reaches a working state
- **Decided:** Refines the March 2026 `[Art] Art pipeline` entry further — even Asset Store
  sprite sourcing is now deferred, not just a custom art pipeline. Until the game is
  genuinely playable end-to-end (core systems functional, not just compiling), new visual
  needs are met with Unity's built-in primitive sprites tinted via `SpriteRenderer` color,
  the same approach already locked for tilemap tiles in the April 2026 Tilemap Session.
  - **Mr_chimken (player):** unchanged. Already has a working bone-rigged placeholder with
    IK — not worth touching.
  - **Phasix creatures:** represented by a colored shape derived systematically from
    `PrimalType`, not hand-picked per species (no roster exists yet to hand-pick against
    anyway). Base 8 types reuse the exact hex colors already locked in the GDD's own Primal
    wheel diagram (§9) rather than inventing a new palette:

    | Type | Hex | Type | Hex |
    |---|---|---|---|
    | Fire | `#C04020` | Light | `#807010` |
    | Water | `#1A6A9A` | Shadow | `#503070` |
    | Earth | `#7A5A20` | Life | `#2A7A2A` |
    | Wind | `#207A40` | Lightning | `#B09020` |

    The 28 duo-merge types derive their color by blending their two parent base colors
    (not hand-authored individually) — keeps this systematic and cheap to extend if the
    roster grows.
  - **Tilemap:** no change — keeps the existing green/grey placeholder squares from the
    April 2026 Tilemap Session. A `tileset.PNG` was found in `Assets/Artwork/Tilesets/`
    during this discussion and investigated: it's a 334×512px promotional/cover thumbnail
    (baked-in text labels, rounded panel framing per "UNIVERSE 1/2/3" branding), not a raw
    sliceable tile grid, and its `.meta` confirms it was never sliced. Rejected as a source;
    not worth chasing down a replacement right now either.
- **Why:** The goal is to reach a working, playable prototype as fast as possible without
  art-sourcing blocking gameplay-system development (Bond, Combat, Evolution, etc.). Real
  art and animation work is explicitly deferred to **after** that milestone, not skipped.
- **Alternatives rejected:** Sourcing a minimal real tileset now (the original Wk 7–8 plan)
  — deferred in favor of finishing core systems first. Simplifying Mr_chimken too — rejected
  since it already works and isn't blocking anything.
- **Date:** July 2026
- **Revisit if:** The game reaches a genuinely playable, systems-complete state (all core
  loops functional) — that's the trigger to begin real art/animation work, per the March
  2026 `[Art] Art pipeline` entry this one refines.

---

### [Art] Placeholder Phasix visual — one shape, underglow halo, sorting layer
- **Decided:** All Phasix placeholders use one shared shape (Unity's built-in 2D `Circle`
  sprite) — color from `PrimalTypeColor` is the sole systematic differentiator, no
  per-type shape variation. A second, larger `Underglow` sprite renders behind the `Body`
  sprite, tinted the same color lightened 35% toward white at 40% alpha (both tunable per
  `PhasixPlaceholderVisual` instance) — no separate ground-shadow layer was added.
  Duo-merge colors are a plain 50/50 `Color.Lerp` of the two parent base colors (no
  weighting), matching DECISIONS.md's existing "blend" wording literally. Both renderers
  use the `Characters` sorting layer (existing layer, already used by Mr_chimken) — the
  `Default` layer was tried first and found to render *behind* the tilemap's `Ground`
  layer, making the placeholder invisible despite having the correct color.
- **Why:** One shape keeps this a pure color-driven system with no second mapping to
  invent ahead of a species roster (mirrors the tilemap's own one-shape/color-only
  pattern). The underglow was the user's explicit ask over a ground-shadow alternative.
  50/50 blend is the simplest, most literal reading of the already-locked "blend their two
  parent base colors" wording — no basis existed for an asymmetric weighting.
- **Alternatives rejected:** Shape-per-PrimalType (would need a second full mapping and
  a rule for what the 28 duo types inherit — no justification without a roster). Ground-
  shadow layer instead of underglow — user chose underglow. Leaving sprites on the
  `Default` sorting layer — found to be simply wrong (renders behind ground tiles), not a
  style choice.
- **Date:** July 2026
- **Revisit if:** A species roster eventually wants per-species shape distinction, or the
  underglow tuning values (0.35 lighten / 0.4 alpha) look wrong once real creature
  silhouettes replace the circle.
- **Update (August 2026):** `Body`/`Underglow` `m_SortingOrder` changed from `1`/`0` to
  `-1`/`-2` — both were sitting above `Mr_chimken`'s `SortingGroup` (`sortingOrder: 0`),
  so the companion always rendered in front of the player regardless of Y-position (the
  camera's `Transparency Sort Mode` is `Default`, not Y-axis, so this is a plain numeric
  order, not something Y-sort would resolve on its own). Now both sit below 0 — companion
  renders behind the player — while keeping Underglow behind Body.

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
- **Update, July 2026:** The free version is no longer on the Unity Asset Store (Store
  listing is Pro-only now) — it's downloaded directly from arongranberg.com/astar/download
  instead. Import confirmed live via `unity_reflect` and a real solved `ABPath`.

### [Pathfinding] Grid Graph — dedicated Obstacles layer, cached config + fresh scan on startup
- **Decided:** Created a new `Obstacles` physics layer. `Walls` and `Decorations` tilemap
  colliders moved onto it (were on `Default`, same as the player's own collider — scanning
  against `Default` would have marked the player's own collider as an obstacle). The
  `GridGraph`'s 2D collision mask targets `Obstacles` only, with `collision.diameter = 2`
  and `cutCorners = false` (see the two entries below for why). **Both**
  `AstarPath.scanOnStartup = true` **and** `data.cacheStartup = true` are needed together —
  not `scanOnStartup` alone, corrected from an earlier version of this entry (see Update
  below).
- **Why:** `WorldChunkManager` (Phase 1) dynamically `SetActive`s world chunks based on
  player proximity — a graph that's only ever restored from a stale cached scan would
  silently go wrong the moment chunks toggle in/out. `scanOnStartup = true` re-runs the
  actual collision scan fresh every time Play begins, so walkability always reflects
  whatever's active right now.
- **Update, July 2026 (same session):** The original version of this entry set
  `cacheStartup = false`, believing that "not caching" was how to force a fresh scan.
  That was wrong and was corrected the hard way: without `cacheStartup = true` +
  `SerializeGraphs`/`SetData`, **nothing** about the graph persists across a domain
  reload — not just stale node data, but the graph's own configuration (`diameter`,
  `cutCorners`, dimensions, mask, all of it), since A*'s graphs are restored entirely by
  deserializing the cached blob, not through normal Unity field serialization. Confirmed by
  a domain reload reverting a configured graph to library defaults
  (`cutCorners: True, diameter: 1`) despite `EditorUtility.SetDirty` + a scene save having
  been called. The correct combination is: cache the *configured* graph
  (`cacheStartup = true` + explicit `SerializeGraphs`/`SetData` after any Edit-mode config
  change) so settings survive, AND keep `scanOnStartup = true` so the actual walkability
  data is always freshly recomputed at runtime against whatever's active then. See
  LESSONS_LEARNED.md → [Pathfinding] for the full investigation.
- **Alternatives rejected:** `cacheStartup = false` alone (the original, incorrect version
  of this decision) — loses the configuration itself, not just node data.
- **Date:** July 2026
- **Revisit if:** Rescanning the whole graph on every startup becomes a real load-time cost
  once the world is larger than the current single test room — at that point, consider
  scanning only active chunks, or graph updates on chunk toggle, instead of one full scan.

### [Pathfinding] GraphCollision.diameter is in nodeSize units, not world units — and cutCorners needs to be off
- **Decided:** `collision.diameter = 2` (not `1`, the value first tried) and
  `cutCorners = false` (not the GridGraph default of `true`).
- **Why:** User-reported bug — the companion visibly sat on top of a stump/rock decoration
  it should have routed around. Direct investigation (not guessing) found two compounding
  causes: (1) `GraphCollision.finalRadius = diameter * nodeSize * 0.5` (confirmed by reading
  `Base.cs`/`GridGenerator.cs` source directly) — `diameter` is a multiple of the graph's
  `nodeSize` (0.5 here), not an absolute world-unit value. `diameter = 1` therefore meant an
  actual collision-check radius of only 0.25 world units, small enough to miss decoration
  colliders that don't perfectly fill their tile cell or sit slightly off the grid's own
  node centers (the grid's node positions aren't guaranteed to align with the tilemap's own
  cell centers). Tested empirically across several values against all 96 painted
  Decorations tiles; `diameter = 2` (real radius 0.5) was the smallest value that produced
  zero misclassified tiles, and going higher only eroded the walkable area further with no
  further benefit. (2) `cutCorners = true` (the GridGraph default) allows the path to
  connect two diagonally-adjacent nodes even when both their shared cardinal neighbors are
  blocked — geometrically a gap the agent's real ~0.4 radius can't fit through cleanly,
  which reads as jittering/getting stuck exactly at obstacle corners.
- **Alternatives rejected:** Assuming `diameter` was already in world units (the original,
  incorrect assumption) — silently produced a check radius 4x smaller than intended.
- **Date:** July 2026
- **Revisit if:** A different `nodeSize` is ever chosen — `diameter` must be re-derived as
  `desiredWorldRadius * 2 / nodeSize`, not copied as a literal number.

### [Pathfinding] Companion uses Rigidbody2D (Kinematic) + CircleCollider2D, matching PlayerController_SideScroll's structure
- **Decided:** `Phasix_Placeholder.prefab` has a `Rigidbody2D` (Kinematic body type,
  `gravityScale = 0`, frozen rotation, continuous collision detection, interpolation — same
  values as `PlayerController_SideScroll`'s Rigidbody2D, except Kinematic instead of Dynamic
  since AIPath drives it rather than force/velocity) and a `CircleCollider2D` (radius 0.4,
  matching `AIPath.radius`). `CompanionAI.Awake()` also explicitly sets `_aiPath.gravity =
  Vector3.zero`.
- **Why:** User-reported bug — the companion was visibly sinking. Root cause: `AIPath`'s
  built-in fake-gravity/ground-check system (for 3D/sloped terrain) defaults to
  `Physics.gravity` unless a non-kinematic Rigidbody is present or gravity is explicitly
  zeroed, and neither was true before this fix. Beyond just fixing the symptom, adding a
  Rigidbody2D matching the player's own structure was the more correct fix: A* Pathfinding
  Project's `AIPath` explicitly supports driving a Rigidbody2D instead of the raw Transform
  when one is present (confirmed in `AIBase.cs` source, not assumed) — this also gives the
  companion real Physics2D collision/trigger participation for free, which the next roadmap
  item (Wk 14-16, wild encounter Trigger2D) will likely want anyway.
- **Alternatives rejected:** Just zeroing gravity without adding a Rigidbody2D — fixes the
  falling but leaves the companion structurally inconsistent with every other physics-based
  character in the project, and without real collider participation.
- **Date:** July 2026

### [Creatures] Companion's collider is a trigger — the player must never be physically influenced by it
- **Decided:** `Phasix_Placeholder.prefab`'s `CircleCollider2D.isTrigger = true`, superseding
  the earlier solid-collider setup from earlier in this same session.
- **Why:** Explicit user requirement — the companion must never influence player movement at
  all, under any circumstance; the companion is always the one that accommodates. A solid
  (non-trigger) collider on a Kinematic Rigidbody2D will always physically displace a Dynamic
  body it touches — this is unconditional Unity 2D physics behavior, true regardless of how
  well-tuned the companion's own AI avoidance logic is. The only way to guarantee zero
  physical influence on the player is to remove physical collision entirely and make 100% of
  avoidance the companion's own scripted responsibility (the trailing + repel logic already
  built). Confirmed not a limitation of A* Pathfinding Project — this is collider
  configuration, unrelated to which library drives the companion's movement.
- **Supersedes:** The `[2026-07-31] Verified — player can physically collide with the active
  companion` CHANGELOG entry from earlier this session. That verification was accurate for
  what was built at the time (solid collider), but the user has since clarified that solid
  blocking is NOT the desired design — it directly conflicted with "the companion should
  never influence player movement."
- **Date:** July 2026

### [Creatures] Companion spawns at an offset from the player, never exactly coincident
- **Decided:** `PartySystem` spawns the companion at `_playerTransform.position +
  _spawnOffset` (default `(0, -1.2, 0)`), not directly at the player's position.
- **Why:** User screen-recorded the companion pushing the player around with zero input,
  from the very start of the game. Reproduced and confirmed: spawning two solid, non-trigger
  colliders exactly coincident forces a large physics separation response that compounds
  with the companion's own active path-following, producing several seconds of drift before
  settling — even with the player's own movement script correctly re-asserting zero velocity
  every `FixedUpdate` the whole time (i.e. not a side-effect of any AI logic bug, confirmed
  separately from the fix below).
- **Date:** July 2026

### [Creatures] CompanionAI reads the player's Rigidbody2D.linearVelocity, not Transform position deltas
- **Decided:** `CompanionAI`'s "which way is the player moving" signal comes from
  `_target.GetComponent<Rigidbody2D>().linearVelocity`, falling back to a flattened position
  delta only if the target has no Rigidbody2D.
- **Why:** Position deltas don't distinguish "the player intentionally moved" from "the
  player's Transform changed for any reason" — including being physically nudged by the
  companion's own collider. Combined with `.normalized()` (which is magnitude-blind), this
  created a real feedback loop: companion nudges player → tiny involuntary position shift →
  read as player movement → companion reacts → nudges again, sustainable with zero actual
  input. `Rigidbody2D.linearVelocity` reflects `PlayerController_SideScroll`'s own asserted
  intent (it overwrites the Rigidbody's velocity every `FixedUpdate` regardless of what
  physics did to it in between), so it self-corrects within one physics tick instead of
  persisting.
- **Alternatives rejected:** Raising the position-delta detection threshold — would only
  narrow the window for false triggers, not eliminate the underlying category error (using
  position as a proxy for intent at all).
- **Date:** July 2026

### [Pathfinding] CompanionAI's trail direction is turn-rate-limited, not snapped instantly
- **Decided:** `CompanionAI._smoothedTargetDirection` (the direction the companion trails
  behind) now updates via `Vector3.RotateTowards` at a tunable `_directionTurnSpeed`
  (degrees/sec, default 180), instead of snapping to the player's latest per-frame movement
  delta instantly.
- **Why:** An instant snap meant the companion's computed follow-point whipsawed every time
  the player's heading changed quickly — which is exactly what happens continuously while
  curving around an obstacle. Combined with the GridGraph fixes above, this contributed to
  the "getting stuck near objects" symptom: the destination itself was jittering, not just
  the path to it.
- **Date:** July 2026

### [Pathfinding] AIPath over AILerp for companion following
- **Decided:** `CompanionAI` uses `AIPath` (physics/acceleration-based movement), not
  `AILerp` (constant-speed linear interpolation between path corners).
- **Why:** The player (`PlayerController_SideScroll`) already moves via smoothed
  Rigidbody2D acceleration/deceleration — `AIPath`'s similarly physics-flavored movement
  (`maxAcceleration`, speed ramping) reads as consistent with that, whereas `AILerp`'s
  constant-speed snapping between waypoints would look visually different from the
  player's own movement feel.
- **Alternatives rejected:** `AILerp` — simpler and cheaper, but the wrong movement feel
  for a companion meant to visually match the player's motion style.
- **Date:** July 2026

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
- **Decided:** 0.5 world units per A* node
- **Why:** Half the tile's world-unit size (see `[Tileset] Tile base size` below — 1 unit/tile)
  gives finer navigation resolution, which matters for smooth companion-following pathing
  around obstacles (Wk 12–13). Chunk-based worlds (already decided — `WorldChunkManager`)
  keep the extra node count from being a real performance concern at this world scale.
- **Alternatives rejected:** 1.0 unit (matches tile size 1:1, cheaper to bake, but coarser
  companion movement — not worth it at this scale)
- **Date:** July 2026
- **Revisit if:** Companion pathing looks too grid-locked even at 0.5, or profiling shows
  the extra nodes are actually a cost worth caring about

### [Tileset] Tile base size
- **Decided:** 16×16 px equivalent, at 1 world unit per tile — matches the already-locked
  16 PPU Pixel Perfect Camera exactly (see `[Camera] Pixel Perfect Camera PPU locked`,
  April 2026), no scale mismatch.
- **Why:** The placeholder-first art pipeline (see `[Art] Placeholder-first pipeline`,
  July 2026) means no real tileset PNG is being sourced right now — this locks the world
  *scale* the existing placeholder tiles already occupy, so A* grid size and future real
  tile art both have a stable target instead of staying open indefinitely.
- **Alternatives rejected:** 32×32 px (2 units/tile) — would double world unit scale
  against the creature sprite pipeline, which is already locked at 32 PPU import settings
  matching a 16 PPU camera baseline; keeping tiles at 1 unit avoids that mismatch.
- **Date:** July 2026
- **Revisit if:** A real tileset is eventually sourced at a different native pixel size —
  reconcile then, don't assume 16×16 forever.

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

### [Tileset] Tile pixel size — LOCKED July 2026
- **Decided:** 16×16 px equivalent, 1 world unit per tile. See `[Tileset] Tile base size`
  (Pending Decisions section, July 2026) for the full decision — locked without waiting for
  a real tileset PNG, per the placeholder-first art pipeline.
- **A* cell size:** 0.5 units — see `[A* grid] Cell size` (Pending Decisions section).

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

---

## New Entries — April 2026 IK Session

### [IK] Solver type — LimbSolver2D for arm chains
- **Decided:** `LimbSolver2D` (from `com.unity.2d.animation`) for both arm chains on Mr_chimken. One solver per arm (`shoulder→forearm→tip`).
- **Why:** Arms are exactly 2-bone chains — LimbSolver2D is the correct analytical solver for this. CCD/FABRIK are for longer or unknown-length chains.
- **Alternatives rejected:** `CCDSolver2D` / `FabrikSolver2D` (overkill for 2-bone limbs; slower, no analytical solution)
- **Date:** April 2026
- **Revisit if:** A Phasix creature has arm chains longer than 2 bones — use CCDSolver2D in that case

### [IK] Foundation only — no driving script this session
- **Decided:** IK targets (`IK_Target_Arm_R/L`) left as free-floating GameObjects. No script drives them yet.
- **Why:** Exploring IK behaviour first before committing to a specific use case (mouse-aim, procedural sway, foot planting, etc.).
- **Alternatives rejected:** Mouse-follow script, procedural sway script (deferred until use case is confirmed)
- **Date:** April 2026
- **Revisit if:** A concrete use case is decided — add a driving script at that point

### [IK] solveFromDefaultPose=false + runInEditMode=true required for code-created IK
- **Decided:** All `LimbSolver2D` components created via `unity_execute_code` must have `solveFromDefaultPose=false`. `IKManager2D` must have `runInEditMode=true`.
- **Why:** `solveFromDefaultPose=true` requires `StoreLocalRotations()` to be called first — skipping it causes a degenerate restore cycle that silently breaks the solve. `runInEditMode=false` means targets can't be tested without entering Play mode.
- **Alternatives rejected:** `solveFromDefaultPose=true` with manual `StoreLocalRotations()` call (fragile, extra step, no benefit for foundation setup)
- **Date:** April 2026
- **Revisit if:** Never for code-created setups. Inspector-created IK can use defaults safely.

---

## Tooling & AI Workflow

### [Tooling] Unity MCP server choice
- **Decided:** CoplayDev/unity-mcp (MIT license) as the Unity MCP bridge for Claude Code
- **Why:** Long-term sustainability was the deciding factor over feature count. Coplay is a
  company whose core business is Unity AI tooling — direct incentive to keep maintaining it.
  MIT license means the community can fork it if Coplay ever stops. Largest community
  (~5,800+ stars) gives the best bus factor and the most already-documented fixes.
- **Alternatives rejected:** AnkleBreaker Studio's unity-mcp (268+ tools, broader category
  coverage — Shader Graph, terrain, NavMesh, MPPM multiplayer). Rejected despite more tools:
  it's a side project of a small indie game studio whose actual business is their own games
  (Mithrall, Kickdom), uses a custom non-standard license (attribution + no-resale clauses),
  and has a much smaller community (~341 stars). Unity's own official MCP Server (bundled in
  Unity AI beta) was also considered and rejected for now — $10/mo subscription cost on
  Personal after trial, plus concurrent-connection limits tied to plan tier.
- **Date:** July 2026
- **Revisit if:** Coplay discontinues the open-source MCP server, or Unity's official one
  drops its subscription gate / connection limits post-beta.
- **Security note:** Like all current community Unity MCP servers, this one runs an
  unauthenticated local server (no auth by design, localhost-only). Accepted as a reasonable
  risk for this project — solo dev, no proprietary or critical data at stake. Don't re-flag
  this as a new problem in a future session; it was already weighed and accepted.
- **Migration note:** AnkleBreaker's Unity package and `.mcp.json` entry were removed and the
  CoplayDev git package added to `Packages/manifest.json` in this session. Live-connection
  verification (Package Manager compile + "Configure All Detected Clients" + session restart)
  is a manual follow-up — see `CHANGELOG.md` for status.

---

## Creatures — PhasixData / PhasixRuntimeData

### [Creatures] Temper, Personality, and Origin all live on PhasixRuntimeData, not PhasixData
- **Decided:** All three fields are excluded from `PhasixData` (the SO) despite being listed
  directly in CLAUDE.md's schema block. All three live on `PhasixRuntimeData` instead.
- **Why:** All three are per-individual (rolled at capture, like Personality's "shown on
  capture") and runtime-changeable — Personality via a consumable item ("any personality to
  any other," GDD §7). Temper via Re-Tempering at a Temper Forge using Temper Cores (GDD
  §6.4). Origin via "Origin Change" (GDD §14.4) — a Bond-cost mechanic tied to wheel
  distance (Adjacent = cheap, Opposite = expensive, e.g. Wild→Corrupted costs 15% bond) that
  doubles as the only way to break through a bond floor. Writing any of these to a shared
  `PhasixData` asset at runtime would violate the Hard Architecture Rule (CLAUDE.md:
  "ScriptableObjects = read-only at runtime"). The only alternative — one SO asset per
  Temper/Personality/Origin combination per species-form — would mean up to 3× (Temper),
  18× (Personality), or 6× (Origin) asset multiplication, far worse than moving the fields
  to runtime state.
- **Correction:** Origin was originally kept on `PhasixData` in the first pass of this work
  — GDD §14.4 (Origin Change) wasn't found until a later session. Moved to
  `PhasixRuntimeData` once found, matching Temper/Personality's already-established pattern.
- **Alternatives rejected:** Baking Temper into 3 separate SO assets per species+tier (an
  earlier draft of this decision, corrected after finding GDD §6.4's re-tempering rule).
  Baking Personality onto the SO per the Roadmap's literal wording — rejected because it
  would need a rework the moment the personality-swap item is built. Keeping Origin on the
  SO — rejected once GDD §14.4 confirmed it changes at runtime.
- **Date:** July 2026
- **Revisit if:** Never, unless a future directive states one of these three is fixed at
  creation with no swap/change mechanic (would contradict GDD §6.4/§7/§14.4 as currently
  written).

### [Creatures] PhasixRuntimeData matches Evolution_System_Directive_v1_1_0.md's spec, not CLAUDE.md's literal schema
- **Decided:** `PhasixRuntimeData` uses `StatBlock` (not raw ints) for `baseStats` and
  `unnamedPool`, GUID strings (not `SkillData` object references) for
  `learnedSkillGuids`/`equippedSkillGuids`, plus `currentNodeGuid`/`speciesData`
  pointer fields and an `evolutionHistory` list — none of which appear in CLAUDE.md's
  schema block, which shows plain ints and object-reference lists instead.
- **Why:** `Evolution_System_Directive_v1_1_0.md` is the project's primary evolution
  authority (per `DOCUMENT_INDEX.md`) and already specifies this exact shape, used by
  logic that doesn't exist yet (`EvolutionExecutor`, `SaveManager`) but will when Phase 4
  is built. Matching it now avoids a breaking rework later. `DOCUMENT_INDEX.md`'s own
  precedence rule ("the more specific Directive always wins over the GDD") extends
  naturally to CLAUDE.md's schema block, which is a simplified summary, not the
  authoritative source.
- **Alternatives rejected:** Building `PhasixRuntimeData` to match CLAUDE.md's literal
  schema (raw ints, object-reference skill lists) — faster now, but confirmed to require a
  full rework once Phase 4's evolution graph is built, per the Directive's own field usage.
- **Date:** July 2026
- **Revisit if:** A future revision of the Evolution Directive changes this shape.

### [Creatures] Base stat tier-floor ints stay on PhasixData for now, not EvolutionNodeSO
- **Decided:** The 8 base stat ints (`vitality`, `force`, etc.) remain on `PhasixData` as
  tier-floor seed values, even though `Evolution_System_Directive_v1_1_0.md` implies they
  belong on `EvolutionNodeSO.tierStatFloor` instead (a type that doesn't exist yet).
- **Why:** `EvolutionNodeSO` is Phase 4 scope — not building it now would leave nowhere for
  the Roadmap's literal Wk 9 ask ("Implement all 8 base stats... on PhasixData") to live.
  Flagged as a known future seam, not a design mistake.
- **Date:** July 2026
- **Revisit if:** When `EvolutionNodeSO` is built (Phase 4), decide whether these fields
  migrate there or `PhasixData` keeps them and `EvolutionNodeSO` references `PhasixData`
  for tier-floor stats instead.

### [Creatures] OriginType naming (renamed from CLAUDE.md's "Origin")
- **Decided:** Enum is named `OriginType`, matching `Evolution_System_Directive_v1_1_0.md`'s
  actual usage (`speciesData.origin`, typed `OriginType`), not `Origin` as CLAUDE.md's
  schema names it. Same 6 values either way — free naming alignment, no structural cost.
- **Date:** July 2026

### [Creatures] Real locked GDD names used instead of invented placeholders
- **Decided:** `PrimalType` (8 base + 28 duo merges), `SignalType` (9 types), `Personality`
  (18 traits, not 16), and `SkillTreeType` (18 types, A–R) all use the actual names locked
  in `GDD_CreatureRPG_v0_8_0.html`, verified directly against the document rather than
  invented as `TODO` placeholders.
- **Why:** All four systems are marked `Locked` in the GDD itself — using invented
  placeholders would have contradicted already-settled design content, not respected the
  "no invented content for pending systems" rule (which only applies to genuinely pending
  content, and none of these four are pending).
- **Note:** `Personality`'s GDD prose/changelog says "16 traits" in two places, but the
  actual §7.3 table has 18 rows. Used the verified table count, not the prose summary —
  flagged as a doc discrepancy worth a designer's confirmation pass, not silently resolved
  as if it were never a discrepancy.
- **Date:** July 2026

### [Creatures] Evolution_System_Directive_v1_1_0.md has internal inconsistencies — flagged, not fixed
- **Found:** `EvolutionNodeSO`, `EvolutionBranchSO`, `EvolutionGraphSO`, and
  `EvolutionEvaluator`/`EvolutionExecutor` as specified in the Directive have real
  conflicts between declared field names (in the class definitions) and actually-used
  field names (in the logic that references them) — e.g. `EvolutionNodeSO.formID`
  (declared) vs. `node.nodeGuid` (used); `ConditionalType` has 6 declared members but 7
  different members are switched on elsewhere; `BranchConditional` is used as a type but
  never defined (only the similarly-shaped `ConditionalRequirement` is).
- **Why not fixed now:** None of these types are needed for Phase 2 Wk 9 (PhasixData) —
  they're Phase 4 scope. Fixing them requires a design pass on the source document itself,
  which is out of scope for a code implementation task.
- **Action needed:** Before Phase 4 (Evolution system) implementation starts, resolve these
  inconsistencies in `Evolution_System_Directive_v1_1_0.md` (or its PDF source) first.
- **Date:** July 2026

---

### [Creatures] Personality roll is uniform random; stat-nudge table built now as unwired scaffolding
- **Decided:** `PersonalitySystem.RollRandom()` picks uniformly among all 18 traits — GDD
  §7 doesn't specify any weighting, and no capture system exists yet to suggest one is
  needed. `PersonalityStatModifier`'s trait→stat-nudge table (GDD §7.3, locked) was built
  now even though nothing consumes it yet — the numeric growth formula lives in the
  not-yet-built Aura allocation system (Progression_Directive_v0_1_0.md). User chose to
  build it now rather than defer, since the mapping itself is fully locked content, not
  invented, and the cost of adding it now is low.
- **Why:** Uniform random is the simplest default with zero evidence for any other
  distribution — matches "no invented content" (don't add a weighting rule the GDD never
  specified). The stat-nudge table follows the same forward-reference precedent already
  established for `PhasixRuntimeData`'s unwired evolution-graph fields.
- **Alternatives rejected:** Weighting personality rolls by Temper or PrimalType (no GDD
  basis, would be invented). Deferring the stat-nudge table entirely until Aura allocation
  exists — rejected by the user in favor of capturing the locked data now.
- **Date:** July 2026
- **Revisit if:** A designer specifies non-uniform capture odds, or once the Aura
  allocation system is built and needs to consume `PersonalityStatModifier`.

---

### [Creatures] PartySystem is a MonoBehaviour singleton, not static like BondSystem/PersonalitySystem
- **Decided:** `PartySystem` is a `MonoBehaviour` singleton (`PartySystem.Instance`), not a
  stateless static class.
- **Why:** BondSystem and PersonalitySystem are pure rules-enforcement layers — they take an
  externally-owned `PhasixRuntimeData` and mutate it, with no state or asset references of
  their own. `PartySystem` is a different kind of thing: it owns the party roster itself
  (which Phasix are in which of the 3 slots) AND needs an Inspector-assigned prefab
  reference (the companion visual) plus a live spawned GameObject instance it must track
  across calls. That's the same category as `GameManager` (a scene-resident manager with
  Inspector wiring), not a stateless static utility.
- **Also decided:** Switching the active party slot re-skins and re-targets a single
  persistent companion `GameObject` (via `PhasixPlaceholderVisual.ApplyFromSpeciesData` +
  `CompanionAI.SetTarget`) rather than destroying and instantiating a new one per switch —
  keeps slot-switching from ever looking like the "Instantiate/Destroy in a loop" pattern
  the architecture rules warn against, even though slot switches themselves aren't frequent
  enough to need real pooling.
- **Date:** July 2026
- **Revisit if:** A future save system (Phase 4) needs the party roster to live somewhere
  serializable — `PartySystem` may become the in-memory mirror of that saved state rather
  than the sole source of truth.

---

## Creatures — Future Systems (Designed, Not Yet Built)

Both entries below came out of a design discussion prompted by reviewing the PhasixData
parameter list and asking what makes Temper/Origin/Signal worth caring about together, not
just individually. **Neither is implemented.** Recorded in full — including the options
rejected and the risks raised — so a future session (or a future us) doesn't have to
re-derive the reasoning from scratch before deciding whether to build these.

### [Creatures] Resonance system — Temper/Origin/Signal synchronization (proposed)
- **Status:** Designed, not implemented. No code, no schema changes.
- **Concept:** A computed (not hand-authored) synergy check. Each value in Temper (3),
  Origin (6), and active Signal (9) gets tagged with a shared behavioral keyword —
  draft set: **Aggressive, Defensive, Elemental, Patient**. A creature's rolled
  Temper + Origin + active Signal are compared: if all 3 share a tag, it's **Full
  Resonance** (a real bonus); if 2 share a tag, **Partial Resonance** (a smaller bonus);
  otherwise **Dissonant** — deliberately **no penalty**, only an absence of bonus, so a
  "bad roll" never feels like a defective creature.
- **Draft tag table** (starting proposal, not locked — grounded in each value's existing
  flavor text, not invented from nothing):

  | | Aggressive | Defensive | Elemental | Patient |
  |---|---|---|---|---|
  | **Temper** | Edge | Anchor | Flux | — |
  | **Origin** | Corrupted | Synthetic | Ascended | Hollow, Primordial |
  | **Signal** | Surge, Overflow | Silence, Current | Catalyst, Echo | Frequency, Static, Pulse |

  Open problem: **Wild doesn't fit cleanly** into any of the four tags. Either add a 5th
  tag (e.g. "Natural") or let Wild deliberately sit outside the system as a no-strong-lean
  baseline — undecided.
- **Why considered:** Today, Temper/Origin/Signal are purely additive — nothing rewards or
  reacts to *which combination* a creature rolled. The GDD's own pillar ("Species, Temper,
  Origin, Signal... all shape what a creature is. No two builds are identical") is only
  half true without an interaction layer: no two builds are identical, but they don't
  relate to each other either.
- **Precedents referenced:** Fire Emblem's Support system (personality-compatibility combat
  bonus), Pokémon's Nature/IV system (invisible per-individual modifier, huge competitive
  engagement despite no explicit chart), Persona's Arcana-compatible fusion bonuses,
  Digimon's Vaccine/Data/Virus triangle (directly relevant — Phasix is explicitly
  Digimon-inspired per CLAUDE.md), Magic: The Gathering's color pie, and **Teamfight
  Tactics' trait stacking** — the direct inspiration, though see the note below on how it
  was adapted.
- **Options considered:**
  1. **Full 3-axis matrix** (3×6×9 = 162 explicit combinations) — rejected. The existing
     2-axis systems (Primal 8×8, Signal 9×9) aren't even numerically calibrated yet;
     committing to a 162-cell matrix on top, across three axes at once, is much harder for
     players to discover than two, and risked real design debt against systems that don't
     exist to test it against yet.
  2. **Lightweight curated pairwise tags**, matching the pattern Tempo↔Signal already uses
     live in the GDD (a few called-out "pairs well with" bullets) — viable, but doesn't
     capture a true 3-way interaction.
  3. **Computed shared-keyword-tag system** *(this is the direction taken)* — cheap to
     author (18 tags total, not 162 combinations), scales cleanly if a 4th Origin or 10th
     Signal is ever added, and produces genuine emergent variety per individual catch
     without exhaustive content fill.
  4. **True TFT mechanic (party-composition based)** — considered and explicitly rejected
     in favor of a single-creature check. Real TFT synergies count shared traits *across
     your whole active board*, not within one unit's own traits — that's a genuinely
     different system (team-building strategy layer) from "does this one creature's own
     three rolled axes align." The user confirmed single-creature was the intended
     direction after this distinction was raised.
- **Confirmed supporting fact:** all three axes really do vary per individual (verified,
  not assumed) — Signal explicitly ("Wild spawns can appear with any type from their
  pool," GDD §16.3), Temper implied by Re-Tempering existing at all (GDD §6.4), and Origin
  confirmed by GDD §14.4 "Origin Change" (a Bond-cost mechanic — see the Origin
  architecture fix entry above, which this discussion directly caused).
- **UI concept:** A chip/icon on the party roster or capture-reveal screen — dim outline
  normally, fills at Partial, glows at Full. No tooltip wall of text, matching the
  project's existing "discovery over instruction" philosophy (already used for Signal,
  which also has zero text feedback in combat).
- **Open questions before this can be built:** final tag assignments (especially Wild),
  actual bonus magnitudes (pending NumericalCalibration.md like everything else numeric),
  and where the Resonance tier should actually surface in UI flow.
- **Date:** July 2026
- **Revisit if/when:** Before Combat (Phase 3) or Evolution (Phase 4) numeric calibration
  locks in, since Resonance bonuses would need to interact with the damage/stat formulas
  those phases define.

### [Creatures] Companion movement/following pattern archetypes (outline only — not designed)
- **Scope — open world only.** This entire entry is about **overworld companion-following
  AI** (`CompanionAI`, Wk 12-13) — how the active party Phasix physically moves around the
  player while exploring outside of battle. It has no relationship to Tempo's in-battle
  action economy, skill trees, or any combat system. Do not conflate the two when reading
  this entry later.
- **Status:** Outline for future discussion only. Nothing decided, nothing implemented.
  Written down purely so a future session doesn't restart this from zero — every option
  below is a draft to argue with, not a proposal to build as-is.
- **Concept:** `CompanionAI` (Wk 12-13) already exposes several tunable knobs per instance —
  `_trailDistance`, `_directionTurnSpeed`, `_walkSpeed`/`_runSpeed`,
  `_idleDistance`/`_runDistance`, `_repelDistance`/`_repelStrength`. Right now every Phasix
  uses the same prefab defaults for all of these. The idea: let different Phasix *feel*
  different when following, driven by per-species or per-individual data rather than one
  fixed tuning.
- **Investigated and rejected as the driving hook: `TempoType`.** Checked the actual locked
  definition (GDD §11, "Locked v0.5.1") before assuming it would fit: Tempo is explicitly
  **battle action economy** — "what a creature can structurally do when its turn arrives"
  (one action / chain / bank), chosen before battle and locked for that fight's duration.
  It has nothing to do with movement and reusing it for overworld following would conflict
  with its actual locked meaning, not extend it.
- **Leading candidate hook: Personality (18 traits, already built —
  `PersonalityStatModifier`).** Unlike Tempo, Personality is about temperament (Reckless,
  Calm, Timid, Jolly, Cautious, ...) with no locked meaning outside "stat nudge" — mapping
  temperament to movement *feel* is a natural, low-conflict extension, and the 6 existing
  thematic groups (GDD §7.3: Offensive, Elemental, Defensive, Technical, Resilient,
  Versatile) already suggest natural movement-style clusters:
  - **Offensive** (Reckless, Fierce) → eager, close-following, quick to Run-state
  - **Defensive** (Cautious, Hardy) → measured distance, calmer turn speed, slower to panic-run
  - **Resilient** (Stubborn, Gentle, Patient, Lively) → steady/unflappable, doesn't react to
    every small player movement
  - **Versatile** (Brave, Jolly, Timid, Naive) → most varied — Timid in particular could
    lean toward the "keeps more distance" archetype below rather than the close-follow norm
  - **Elemental** (Quirky, Calm) and **Technical** (Hasty, Careful, Shrewd, Thorough) not yet
    drafted — no strong movement-flavor read on these two groups yet.
- **Tier 1 — parameter presets (buildable now, no new movement code):** just different
  combinations of the knobs `CompanionAI` already has.
  - *Close Shadow* — small trail distance, fast turn speed, low idle threshold (glued to
    the player, reacts instantly)
  - *Wide Wanderer* — large trail distance, slower turn speed (hangs back, unhurried)
  - *Eager Runner* — low idle distance + high run speed (always feels like it's hustling to
    catch up)
  - *Steady Anchor* — large idle distance, doesn't feel rushed to stay close
- **Tier 2 — new behavior patterns (would need actual new logic, not just tuning):**
  - *Orbiting* — circles the player at a radius instead of trailing directly behind
  - *Flanking* — holds a side position (left/right of the player) rather than directly
    behind
  - *Skittish* — inverted repel logic: prefers to keep MORE distance than normal and darts
    away if the player gets too close, rather than the current close-follow norm
  - *Wandering* — periodically breaks off to explore nearby, then returns on its own
  - *Aerial/Flying* — ignores the GridGraph's ground obstacles entirely, moves in straight
    lines (would need its own non-pathfinding movement mode, not an AIPath tuning change)
  - *Bounding* — discrete hop/pause rhythm instead of continuous movement (ties into the
    Idle/Walk/Run Animator scaffold already built, once real animation content exists)
- **Open questions:** whether Personality is the right final hook or a dedicated new field
  is cleaner; the Elemental/Technical group movement reads; whether Tier 2 patterns are
  worth the complexity before a real species roster exists to assign them meaningfully
  (per-species flavor without real species feels like guessing); actual numeric tuning
  values per archetype (would be its own NumericalCalibration.md-style pass).
- **Date:** July 2026
- **Revisit if/when:** the species roster (Phase 5, GDD §25) exists — assigning movement
  flavor to real, designed species is a much better-grounded exercise than guessing ahead
  of it.
- **Try it now:** `CompanionAI.ApplyMovementPreset()` + `DebugMovementPresetCycler.cs`
  (temporary, press Tab in Play mode) let all 5 Tier 1 presets above be compared live on the
  existing placeholder companion, before committing to Personality or any other hook.

### [Creatures] Hidden Shadow pattern — snap-vs-lerp and displacement re-lock
- **Status:** Undecided on two points, implemented with a working default for now:
  1. Snap vs. lerp on return-to-shadow — implemented as lerp (`ShadowReturnLerpDuration`) per
     a lean toward smoother transitions; revisit if it reads mushy/laggy in practice.
  2. Whether the idle anchor should re-lock if the player is displaced (knockback, etc.) while
     Emerged — currently it does not; the anchor stays fixed until the next natural
     Locked→Emerged transition, so a knockback mid-sway could leave the companion swaying at a
     stale position until the player moves again.
- **Revisit:** once Hidden Shadow has been compared live via `DebugMovementPresetCycler` and
  either reads fine as-is or a concrete complaint surfaces.

### [Creatures] Blink pattern — walkable-point sampling and flash-vs-telegraph timing
- **Concept:** a 7th Tier 2 companion movement pattern (not one of the archetypes originally
  sketched above) — periodically teleports the companion to a random point within a radius
  band of the player instead of walking there. Built the same way Orbit/HiddenShadow were:
  new `CompanionMovementPatternType` value, its own `MoveAlong…()` in `CompanionAI.cs`,
  bypasses `AIPath` entirely. Not wired to Personality/species — same as Orbit/HiddenShadow,
  selectable via `DebugMovementPresetCycler` only for now.
- **Status:** Undecided on two points, implemented with a working default for now:
  1. Invalid teleport targets (landed inside/beyond unwalkable geometry) — implemented as
     retry-up-to-8-times-then-fall-back-to-the-player's-own-position via
     `AstarPath.active.GetNearest`. Revisit if blinks visibly fail to escape a tight/cluttered
     space (e.g. always landing back on the player because every retry failed).
  2. Flash-after vs. a telegraph-before-blink — shipped as a pop-scale flash immediately after
     arrival (simpler: no anticipation state to track). Revisit if playtesting wants a warning
     tell before the teleport instead, closer to how real "blink" abilities in other games
     often flash at the origin point first.
- **Revisit:** once Blink has been compared live via `DebugMovementPresetCycler` and either
  reads fine as-is or a concrete complaint surfaces.

### [Creatures] Convention — every AIPath-bypassing companion movement pattern must have a gizmo
- **Status:** Settled, enforced at runtime, not just documentation.
- **Rule:** any `CompanionMovementPatternType` added to `ApplyMovementPreset()`'s
  `_aiPath.canMove = false` exclusion list (i.e. any pattern that bypasses AIPath's own
  pathfinding, the way Orbit/HiddenShadow/Blink do) MUST also get its own `Draw*Gizmos()` case
  in `CompanionAI.OnDrawGizmos()`.
- **Why:** for any pattern that bypasses AIPath, neither A* Pathfinding Project's own gizmos
  (Seeker's path-line, AIBase's destination-circle — both suppressed for these patterns, see
  the `seeker.drawGizmos`/destination-reset comments in `ApplyMovementPreset`) nor anything
  else has anything to draw. Skipping a custom gizmo for a new bypass pattern doesn't just miss
  a nice-to-have — it makes that pattern's movement completely invisible in the Scene view,
  exactly the "why can't I see anything" class of report that took several rounds to fully
  resolve for Orbit/HiddenShadow/Blink (see `CHANGELOG.md` → the August 2026 gizmo-fix entries,
  and `LESSONS_LEARNED.md` → `[Tooling]` for the specific gotchas: `OnDrawGizmosSelected`
  requiring manual selection, third-party always-on gizmos being mistaken for broken custom
  ones, `SceneView.RepaintAll()` not chaining from inside a gizmo draw callback).
- **Enforcement:** `OnDrawGizmos()`'s `switch` has a `default` case that logs a
  `Debug.LogWarning` if `!_aiPath.canMove` and no case handled the current pattern — fires the
  moment the new pattern is tested in the Editor, not discovered later. Verified working via a
  simulated gap (forced `canMove = false` on an unhandled pattern via reflection, confirmed the
  warning fires). This is a runtime safeguard, not just a comment — don't suppress or remove it
  without adding an equivalent check elsewhere.
- **Date:** August 2026

### [Creatures] Defuse / Infuse — creature release + Resonance investment (proposed)
- **Status:** Designed, not implemented. Depends conceptually on the Resonance system above
  (reuses its keyword tags) — should not be built before Resonance is, and ideally not
  before Resonance has actually been played, not just implemented.
- **Concept:** Two paired actions, named to fit the "crystallization of emotional states"
  premise rather than the genre-standard "recycle/fodder" framing (see naming note below).
  - **Defuse** — permanently release an owned Phasix (not in the active party) that you
    don't want. Yields Common Aura + Specific Aura (keyed to its `emotionalType`) always;
    Rare Variant Aura only if its Origin is one of the rarer ones (Ascended/Hollow/
    Primordial read as rare in their own flavor text; Wild/Synthetic/Corrupted don't).
    Amount scales with evolution tier. Also yields **typed Essence** matching whichever
    Resonance keyword the creature's dominant matching axis falls under (or
    untyped/generic Essence at Dissonant).
  - **Infuse** — spend Essence on a *kept* creature to raise its Resonance tier directly
    (Dissonant → Partial → Full) **without changing its actual Temper/Origin/Signal
    values**. Cost scales with the size of the tier jump (same shape as Origin Change's
    wheel-distance cost); keyword-matched Essence should cost less than generic/mismatched
    Essence.
- **Why considered:** Under the Resonance system alone, a Dissonant creature has no
  penalty but also no use beyond being itself — this closes that gap by giving every
  catch, even a "bad roll," a productive purpose.
- **Deliberate non-overlap:** Infuse does **not** reroll Temper/Origin/Signal — those three
  already each have a dedicated, already-designed change mechanic (Temper Forge
  re-tempering GDD §6.4, Origin Change GDD §14.4, the pending Signal swap item GDD §16.3).
  A generic "Essence rerolls anything" system would just compete with those. Infuse
  instead operates on the Resonance tier as its own independent investment track — a
  player has two genuinely different reasons to reach for one tool over the other
  (change *what a creature is*, vs. improve *how well its rolled axes work together*).
- **Precedents referenced:** Path of Exile's Divine Orbs / Diablo's enchanting (targeted
  property nudge rather than full reroll), Summoners War's Devilmon / gacha "fodder"
  feeding (the genre-standard answer to "what do I do with dupes"), Pokémon GO's
  Candy/Transfer system (species-specific currency from release), Diablo's salvaging
  (preserves some value from an investment you're moving on from).
- **Naming — rejected "Recycle":** the word reads as gacha-fodder vocabulary and clashes
  with a game whose core metaphor is emotional bonding, not resource optimization.
  "Defuse"/"Infuse" was chosen instead — Defuse reads as releasing/processing a charged
  emotional state rather than discarding a creature; Infuse reads as channeling that
  released energy into another. Names are a first pass, not final.
- **Constraints as framed:** Defuse is permanent (the one irreversible release-equivalent
  action in the game, distinct from Devolution which is free/reversible and Fusion which
  returns ingredients on devolve). Blocked entirely at 100% bond (Bond-100 is already
  framed as a "permanent achievement" — casually discarding it would undercut that).
  Confirmation gate proposed at Partner bond (60%) or above.
- **Known risks — flagged during design, not resolved:**
  1. **Tonal risk.** Renaming away from "recycle" addresses the surface-level word choice,
     but the underlying mechanic — converting a creature into resources — still nudges
     toward treating creatures as interchangeable units. Worth re-checking the *whole*
     mechanic against tone once actually written up in full, not just the verb.
  2. **Aura economy risk.** Aura currently has exactly one source: battle wins. Adding
     Defuse as a second source risks letting players bypass the intended battle loop
     entirely via catch-and-release farming. Mitigation direction (not yet decided as a
     rule): keep Defuse's Aura yield lower than equivalent battle-farming, so it's a
     convenience for unwanted creatures, not a competing progression path.
  3. **Tension with Resonance's own "no judgment" goal.** Resonance was deliberately built
     with no penalty for Dissonant creatures specifically so a bad roll wouldn't feel bad.
     Defuse reintroduces a keep-or-release valuation on every catch one layer up, even
     though nothing mechanical punishes Dissonant directly. Common in the genre (Pokémon
     players do this constantly) but a real cost against the "bond with what you get"
     framing this project has otherwise leaned into.
  4. **Scope.** This is a second new system stacked on top of Resonance, which is itself
     stacked on top of Bond/Combat/Evolution — none of which are fully built or
     numerically calibrated yet. Recommendation from this discussion: design-and-park,
     don't implement, and prioritize playtesting Resonance alone first once it exists.
  5. **Possible confusion with Fusion.** Both Fusion and Defuse conceptually "combine or
     transform" creatures; they need clearly distinct framing/UI once built so players
     don't conflate "make something new (reversible)" with "release for parts
     (permanent)."
- **Date:** July 2026
- **Revisit if/when:** After the Resonance system above is implemented and playtested on
  its own; before NumericalCalibration.md's Aura economy numbers are finalized, since
  Defuse's yield needs balancing against battle-Aura rates (risk #2 above).

---

## New Entries — August 2026 Wild Encounter Session

### [Encounter] Contact-based trigger model, not an invisible zone collider
- **Decided:** Wild encounters use a visible, stationary wild Phasix standing in the world
  that the player physically walks into (`WildEncounterCreature`'s own trigger `Collider2D`
  detects contact), not an invisible `EncounterTrigger` zone the Roadmap's own Wk 14-16
  wording describes.
- **Why:** `Combat_Directive_v0_1_0.md` (locked, not pending) already specifies: "When the
  player's overworld sprite contacts an enemy Phasix sprite, a cinematic transition
  fires..." — the Pokémon/Digimon-style visible-creature model. Building the simpler
  invisible-zone version now would just mean redoing it later to match the locked spec.
  `EncounterTrigger.cs` is kept as a spawn-point marker only (per CLAUDE.md's existing
  `World/` folder listing), not the detector.
- **Alternatives rejected:** Invisible zone collider on `EncounterTrigger` itself, matching
  the Roadmap's literal Wk 14-16 wording — rejected as building the wrong shape on purpose.
- **Date:** August 2026
- **Revisit if:** Never for this scaffold's contact model — superseded entirely once the
  three-layer encounter system (Emotional Mirroring / Attunement / Failure-Triggered,
  `WorldDesign_Directive_v0_1_0.md`) replaces trigger-based encounters in Phase 3-4.

### [UI] UI Toolkit chosen for runtime UI, with PanelSettings locked to the pixel-perfect convention
- **Decided:** `EncounterPromptController` is the project's first UI script, built in UI
  Toolkit (`UIDocument` + UXML + USS), not uGUI. `EncounterPromptPanelSettings.asset`:
  `Scale Mode = Scale With Screen Size`, `Reference Resolution = 320×180`,
  `Screen Match Mode = Match Width Or Height`, `Match = 0.5`.
- **Why:** Roadmap_v2.md's own Phase 5 milestone (Mo 17-18) already tags the eventual full
  UI pass "UI Toolkit" — building this first screen in uGUI would just mean migrating
  later for no benefit. The `PanelSettings` reference resolution must match the Pixel
  Perfect Camera's own 320×180 (locked under `[Engine] Pixel resolution` above) or the UI
  scales independently of the pixel-art world and looks visually mismatched.
- **Alternatives rejected:** uGUI (Canvas/RectTransform) — would need migrating once the
  Phase 5 UI pass standardizes on UI Toolkit anyway.
- **Date:** August 2026
- **Revisit if:** Never — this locks the UI stack choice for all future UI work, not just
  this scaffold.

### [Creatures] Wild spawn repopulation tied to chunk activation via parented Instantiate + OnEnable
- **Decided:** `EncounterTrigger.OnEnable()` (not `Start()`) spawns the wild creature,
  parented to the spawn point's own transform (`Instantiate(prefab, pos, rot, transform)`).
  No cooldown timer of any kind.
- **Why:** `OnEnable()` re-fires every time the spawn point's GameObject cycles active again
  via `WorldChunkManager`'s `SetActive` chunk toggling — parenting is what ties the spawned
  creature's lifecycle to that toggling (an unparented `Instantiate` would sit at scene root
  and stay active even after its chunk deactivates). This gives natural "repopulates on
  revisit" behavior with no invented numeric cooldown, matching
  `NumericalCalibration.md`'s "don't invent pending numbers" convention.
- **Alternatives rejected:** A numeric respawn-cooldown timer — invents a pending balance
  number with no design authority behind it.
- **Date:** August 2026
- **Revisit if:** Phase 3-4's real encounter system replaces this trigger model entirely.

### [Creatures] No pooling for the wild encounter scaffold
- **Decided:** `WildEncounterCreature` instances are plain `Instantiate`/`Destroy`, no object
  pool.
- **Why:** This fires once per spawn point per chunk-revisit cycle — a low-frequency event,
  not a tight loop or per-frame path, so it isn't the case CLAUDE.md's "no
  `Instantiate`/`Destroy` in a loop" rule is targeting. No pooling infrastructure exists
  anywhere in the project yet, and this scaffold is superseded by Phase 3-4's real encounter
  system regardless, so building pooling now would be throwaway work.
- **Alternatives rejected:** Reusing `PartySystem.EnsureCompanionInstance()`'s
  create-once-and-reskin pattern — not actually analogous, since that method never destroys
  its instance; this scaffold's actual Destroy-then-Instantiate-per-visit pattern is a
  different shape and needed its own justification.
- **Date:** August 2026
- **Revisit if:** A future pass adds many simultaneous spawn points such that
  Instantiate/Destroy frequency becomes profiler-measurable.

### [Creatures] Wild encounters set origin = OriginType.Wild directly, no roll
- **Decided:** `WildSpawnSystem.CreateWildInstance()` sets `runtime.origin =
  OriginType.Wild` directly — true by definition for a wild encounter, no random roll.
- **Why:** Simplest correct behavior for this scaffold. Note: this doesn't resolve the
  existing doc inconsistency between `PhasixEnums.cs`'s comment ("Lives on PhasixData... no
  evidence found of per-individual variance") and `PhasixRuntimeData.cs`'s comment ("Rolled
  per individual on capture... changeable via Origin Change") — flagged here, not fixed, per
  this file's existing `[Creatures] Evolution_System_Directive_v1_1_0.md has internal
  inconsistencies` precedent above.
- **Alternatives rejected:** None considered — Wild origin for a wild encounter isn't a real
  design choice.
- **Date:** August 2026
- **Revisit if:** The `OriginType` per-individual-vs-species doc inconsistency above gets
  resolved — check whether that resolution changes how wild encounters should set origin.

### [Encounter] Engage resolves identically to Flee (confirmed scaffold behavior)
- **Decided:** Clicking Engage in the wild encounter prompt despawns the creature, hides the
  prompt, and unfreezes the player — the exact same cleanup as Flee — plus a `// TODO` log
  line and its own `EventBus.OnWildEncounterEngageRequested` event.
- **Why:** No `BattleManager`/`BattleScene_Main` exists yet (Phase 3), so Engage can't do
  anything real. Resolving identically to Flee guarantees the player is never left frozen
  with a dead prompt on screen. Confirmed directly during planning rather than assumed.
- **Alternatives rejected:** Leaving the creature in place (not destroyed) on Engage, since
  no battle actually happened — rejected because it would let the player immediately
  re-trigger the same creature by touching it again, needing its own re-show guard for no
  real benefit at this scaffold stage.
- **Date:** August 2026
- **Revisit if:** Never for the stub itself — superseded once `BattleManager` exists and
  Engage triggers the real Combat_Directive cinematic transition instead.

### [Creatures] Debug-only sprite tint override on EncounterTrigger, separate from PrimalTypeColor
- **Decided:** Added `PhasixPlaceholderVisual.SetColorOverride(Color)` + `EncounterTrigger`'s
  `_overrideTintColor`/`_tintColorOverride` fields. If set, the spawned creature's in-world
  sprite (Body + Underglow, same lighten/alpha math as the real underglow) uses the override
  color instead of `PrimalTypeColor.GetColor(species.PrimalType)`. Used to give
  `Test_WildSpawnPoint_TopLeft`/`_BottomRight` distinct pastel pink/vibrant purple test
  coloring without touching the locked Primal palette.
- **Why:** `PrimalTypeColor`'s 8 base + 28 duo-merge colors are locked GDD data (transcribed
  verbatim, not inventable) — none of them are pastel pink or vibrant purple, and adding
  arbitrary new entries to that dictionary would corrupt real design data for a one-off test
  visual. An opt-in per-spawn-point override keeps the real pipeline untouched.
- **Important limitation, by design:** this only overrides the in-world sprite. The encounter
  prompt UI swatch (`EncounterPromptController.Show()`) is intentionally unaffected — it still
  reads `PrimalTypeColor.GetColor(species.PrimalType)` directly, since that swatch/world-tint
  agreement is itself a locked decision (see `[UI] UI Toolkit chosen...` above). Walking into
  `Test_WildSpawnPoint_TopLeft` shows a pastel-pink creature but the prompt's color swatch
  will still show Fire's real orange — this is expected, not a bug.
- **Alternatives rejected:** Adding pastel pink/purple as new `PrimalTypeColor` entries —
  rejected, would corrupt locked GDD-sourced design data for a debug-only need.
- **Date:** August 2026
- **Revisit if:** A real design need arises for per-individual (not per-species) color
  variance — at that point this debug override might inform a real mechanic, but should be
  redesigned deliberately rather than repurposed as-is.
