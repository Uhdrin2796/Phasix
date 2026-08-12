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

### [Input] Tab reserved for a future menu key; DebugMovementPresetCycler rebound to ~ (backquote)
- **Decided:** `DebugMovementPresetCycler` (overworld debug tool, cycles companion movement
  presets) moved from Tab to `~`/backquote (`Keyboard.current.backquoteKey`). Tab itself isn't
  bound to anything yet — reserved for whichever menu screen gets built first (party screen,
  skill tree UI, etc. — all still pending per PhasixGuide.md's "What Is Pending").
- **Why:** User asked for Tab to open "the menu" ahead of any menu screen actually existing this
  session, and for `~` to take over the companion-movement-preset swap Tab already did. No real
  menu was built this pass to wire Tab to — noting the reservation here so a future session
  building the first menu screen picks Tab by default instead of re-litigating the key.
- **Alternatives rejected:** Leaving the cycler on Tab and picking a different key for the future
  menu — rejected, user specifically asked for Tab to be the menu key.
- **Date:** 2026-08-07
- **Revisit if:** A real menu screen is built — bind it to Tab at that point (this entry is the
  reminder). If a second debug tool ever also wants a bare modifier-free key, confirm `~` and Tab
  are still the only two claimed before adding a third.
- **Update (2026-08-07, same session):** The reservation didn't stay theoretical for long — Tab
  is now wired to the new `PartyMenuController` (see the `[Combat/UI]` entry below), which holds
  the Aura-spend screen moved out of the post-battle flow. Still just one menu/section, not the
  multi-tab container this entry's "party screen, skill tree UI, etc." list implied — those
  remain unbuilt, and Tab just opens/closes this one screen for now.

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

### [Combat] Skill tree framework — status/chain/mastery/combo scaffolded as static rules-layer classes, not SOs or live battle-integrated
- **Decided:** Built `Assets/Scripts/Combat/StatusEffectType.cs`/`StatusEffectCategory.cs`/
  `StatusEffectCatalog.cs`, `StatusDurationCalculator.cs`, `ChainResultType.cs`/
  `ChainResultCatalog.cs`, `MasteryBonusType.cs`/`MasteryBonusCatalog.cs`, `ComboTier.cs`/
  `ComboEngine.cs`, `SkillTreeCatalog.cs`, `SkillSlotCapacity.cs`, and
  `SkillTreeUnlockSystem.cs` (bond-gated Type F/Type O unlocks off
  `EventBus.OnBondMilestoneReached`, subscribed via `[RuntimeInitializeOnLoadMethod]` since no
  MonoBehaviour naturally owns this). Plus 36 placeholder `SkillData` assets (2 per
  `SkillTreeType`, generically named/described, `Assets/Data/Skills/`).
  - **`SkillTreeCatalog` is a static class, not one `SkillTreeData` SO asset per type** (the
    plan's original wording) — follows the precedent already set by
    `PersonalityStatModifier.cs` for exactly this shape of locked-taxonomy lookup table (18
    fixed entries, no per-instance Inspector tuning needed, no reason to pay asset-creation
    overhead for data that never varies per project).
  - **Doc correction:** `ClaudeCode_Primer_v1_1_0.md` said "24" statuses; the GDD's own §17
    tables (verified directly, not assumed) sum to 28 (7 Physical + 7 Elemental + 4 Signal + 4
    Universal + 6 Positive). Fixed the Primer line rather than building against the wrong
    number.
  - **Mastery bonus self/target interpretation:** GDD §17.9 doesn't always spell out which side
    (caster/self vs. target) each bonus's trigger statuses live on. Read Contrast as "self
    positive + target negative" (its own text explicitly separates "buff on self" from "on the
    target") and Enlightened as "3 positive buffs on self" (positive buffs are naturally
    self/ally-applied); all other bonuses (Hemorrhage/Dominance/Collapse/Overmaster/Pressure/
    Convergence) read as the target's own active statuses, since §17.9 frames them as applied
    "to a target."
  - **Combo detection rule:** "skills from different trees used in sequence" (GDD §4.2) is
    implemented as: the most recent N skills (N=2/3/4 for Duo/Trio/Quad) must all come from
    distinct `SkillTreeType` trees, checked as a trailing window ending at the just-used skill —
    largest matching window wins (a Quad match implies Trio and Duo also matched, so only the
    highest tier is reported).
  - **Chain result tie-break:** when a target's active statuses satisfy two different chain
    recipes simultaneously, `ChainResultCatalog.TryResolve` returns the first match in
    declaration order — the GDD doesn't address this case, so this is an explicit placeholder,
    not a hidden guess.
  - **NOT built this pass:** a live in-battle skill-selection UI (BattleManager/BattleHUD
    currently only support "Attack" — no move menu beyond that exists yet). The framework above
    is a fully tested rules layer (43 new EditMode tests), verified the same way
    `DamageCalculator`/`PrimalTypeChart` were — not via a new UI, since building one now would
    have nothing but generic placeholder skill content behind it. Live combo-discovery
    playtesting (the plan's Step 4 "Playtest checkpoint") needs that UI first.
- **Why:** Match the plan's Step 4 ask (skill tree framework: SO/data model, bond-gated
  unlocks, combo engine, status effect engine, 2-3 placeholder skills per type) without
  inventing individual skill content ahead of the species roster design session (explicitly
  "Phase 5" per CLAUDE.md), and without building a skill-selection UI that would have nothing
  real to show yet.
- **Alternatives rejected:** One `SkillTreeData.asset` per type (18 assets) instead of a static
  catalog — rejected per the `PersonalityStatModifier` precedent above. Inventing a combo
  trigger-chance/discovery-rate formula that reads as "final" — rejected in favor of an
  explicitly placeholder linear scale mirroring `TimedInputConfig`'s existing style, logged in
  NumericalCalibration.md.
- **Date:** 2026-08-05
- **Revisit if:** The skill-selection battle UI is built (natural next step) — at that point,
  wire `ComboEngine`/`MasteryBonusCatalog`/`ChainResultCatalog` into the actual battle loop and
  do the live Play Mode combo-discovery playtest the plan calls for.
- **Ref:** GDD §4 (taxonomy/combo, Taxonomy Locked), §14.2 (bond zones), §15 (attribute
  scaling, Locked v0.3.0), §17 (status effects, Locked v0.7.8)

### [Combat] Battle pacing — Continue only gates the player-to-enemy turn transition
- **Decided:** `BattleHUDController.WaitForContinue` (the click-to-proceed button) now fires
  exactly ONCE per round, after every alive party member has acted, right before the enemy's
  turn begins. Every other beat — between successive party members' own attacks, an enemy's
  attack announcement, a resolved attack's result, a Parry counter-attack — uses the new
  `BattleHUDController.ShowTimedMessage(message, BattleConfig.AutoMessageDurationSeconds)`
  instead: shows the same message panel, no button, auto-hides after a fixed duration (1.5s
  placeholder) so the player still has time to read what happened without needing to click.
  `PlayerTurn`'s `WaitForContinue` call moved from inside its per-attacker loop to once after the
  loop; `EnemyTurn` has zero `WaitForContinue` calls left at all — its only interactive moment is
  the live Dodge/Parry click itself (`RunDefenseTimedInput`), everything else auto-paces.
- **Why:** User's explicit ask, after the Dodge/Parry live-click rework — the per-beat Continue
  gate (originally added to fix an earlier "did an attack really land" clarity gap) was doing too
  much once Dodge/Parry became a real click each enemy attack; the enemy's turn now reads as one
  continuous, watchable sequence once the player has handed control back to it, rather than a
  string of individual confirmations.
- **Alternatives rejected:** Keeping a Continue click after every beat (the prior model) — user
  found it excessive once defense itself became an interactive click every attack.
- **Date:** 2026-08-05
- **Revisit if:** Playtesting shows 1.5s isn't enough (or is too much) time to read a beat's
  result — `BattleConfig.AutoMessageDurationSeconds` is a single placeholder shared by every
  auto-paced beat, not yet tuned per message length.

### [Combat] Sonny 2-style radial move selection + drag-to-target; converging-ring defense timing
- **Decided:** Two related UI reworks, both user-directed off the Sonny 2 reference already
  anchoring this HUD's layout:
  - **Move selection/targeting:** The boxed `MoveMenu` (Label + "Attack" Button in the
    ActionPanel) is gone. Each player stage slot now has an "Attack" placeholder
    (`.move-option-placeholder`) positioned directly above its creature circle — only one
    option exists today (matches current game content), but it's built as the first slot of a
    radial system future skill options would add more of around the same circle, not a one-off
    special case. `BattleHUDController.ShowMoveSelection` shows it; pressing it
    (`PointerDownEvent`, not `Button.clicked` — see why below) begins a click-and-drag: a live
    line (`DragLineVisual`, Painter2D) follows the cursor from the creature to wherever the
    player drags. Releasing over an enemy (`worldBound` hit-test) confirms that enemy as the
    target and fires the same callback `ShowMoveMenu` used to; releasing anywhere else cancels
    back to the placeholder to retry. `_root.CapturePointer`/`ReleasePointer` make the drag
    robust regardless of what's visually under the cursor mid-drag.
    - **Why a plain VisualElement, not a Button, for the placeholder:** Unity's `Button` uses a
      `Clickable` manipulator that captures the pointer on press for its own click-tracking —
      that capture would compete with the drag's own `_root.CapturePointer` call and break
      global move/up tracking. A plain `VisualElement` styled to look like a button (no built-in
      click machinery) sidesteps the conflict entirely.
    - **Single-enemy-slot limitation, honestly scoped:** target hit-testing checks against
      `_enemyStageCreature` directly (the one enemy slot that exists) rather than a general
      per-target lookup — multi-enemy battles (trainer fights, Roadmap_v2 Mo 14-15) will need a
      real slot-to-participant map when they're built; not invented ahead of that need.
  - **Defense timing:** `RunDefenseTimedInput` no longer uses the horizontal timing bar at all —
    that bar is offense-only now. Defense uses a new `RingVisual` (Painter2D), reparented above
    whichever player stage slot is the current defense target ("above the phasix," per the
    user's ask), drawing the same Dodge/Parry zone math as before (nested, percent-based) but as
    two concentric zone BANDS (thick strokes at the band's midpoint radius, approximating a
    filled annulus without needing even-odd path fills) plus a thin marker ring that converges
    from `RingMaxRadius` (60px) toward `RingMinRadius` (4px) over the sweep duration, instead of
    a marker sweeping left-to-right across a bar. `PercentToRadius` maps 0% (just started) to
    `RingMaxRadius` and 100% (fully converged) to `RingMinRadius` — same directionality the old
    marker's left-position had, just driving a shrinking radius. The success-check MATH is
    byte-for-byte unchanged (still percent-vs-zoneStart/width comparisons); only the visual
    representation moved from Cartesian to polar.
  - **Bug found and fixed during this rework:** hiding only `TimingBarZone` (not the whole
    `TimingBarTrack`) during defense left the track's gray background bar (and stale marker
    position from the previous offense pass) visibly leaking through behind the DEFEND label,
    since defense no longer touches that track's children at all. Fixed by hiding/showing the
    whole `TimingBarTrack` in both `RunTimedInput`/`RunDefenseTimedInput`, not just its zone
    highlight — caught via a live screenshot during verification, not assumed away.
- **Why:** User's explicit ask, citing Sonny 2's radial-options-around-the-character move
  selection (over a boxed menu) and wanting defense's timing visual to match that same "circle
  perimeter" visual language rather than a straight bar.
- **Alternatives rejected:** A generalized N-option angle-placement system for the radial move
  options — rejected as premature since only one move (Attack) exists; the single placeholder is
  built at the position (directly above) a future multi-option layout would naturally start
  from, without inventing angle-spacing math for options that don't exist yet.
- **Date:** 2026-08-05
- **Verified:** Live Play Mode — screenshot-confirmed the placeholder position, the drag line
  correctly following a synthetic drag from the player creature to the enemy creature
  (`PointerDownEvent`/`PointerMoveEvent`/`PointerUpEvent` sent via reflection, matching this
  session's established isolation-testing pattern), the drag resolving into a real attack
  (13 damage, logged, HP bar updated), and the converging ring rendering with both zone bands
  visible and no leftover offense-track artifacts after the fix above. 111/111 EditMode tests
  unaffected (this was UI/interaction-layer work; no test changes needed).
- **Ref:** Sonny 2 (Flash-era browser RPG), the user's original layout reference for this HUD

### [Combat] Unified converging-ring timing — offense on the target, ratio tolerance, flash feedback
- **Decided:** Offense moved off the old horizontal timing bar entirely, mirroring defense's ring
  (from the prior entry above) but positioned above the TARGETED ENEMY instead of the attacker —
  "apply similar concept," per the user. Immediately after, the ring mechanic itself was redesigned
  again per further live feedback, replacing "two nested zone bands you can see ahead of time"
  with a fixed reference **target ring** plus a single **white marker ring** that starts wider
  (`RingMarkerStartRadius`, 60px) than the target (`RingTargetRadius`, 30px) and shrinks past it
  down to `RingMarkerMinRadius` (2px) over the sweep — no pre-drawn success zone visible, just the
  moving ring approaching (and passing through) the static one, judged by RADIUS RATIO
  (`marker/target`) at the instant of the click:
  - **Dodge** (left-click): succeeds within `DodgeToleranceHalfWidth` (0.25 base — i.e. ratio
    0.75-1.25) of 1.0. Flashes **orange** on success.
  - **Parry** (right-click): succeeds within the tighter `ParryToleranceHalfWidth` (0.10 base —
    ratio 0.9-1.1). Flashes **green** on success.
  - **Offense** (left-click, no Parry-equivalent precision mode): reuses Dodge's base tolerance
    (0.25). Flashes **green** on success (offense's established color throughout this session).
  - **Any Miss** — wrong tolerance, wrong mouse button, or a timeout with no click at all —
    flashes **red**, then the ring hides after the usual 0.3s hold. Full damage, no extra
    penalty vs. a normal miss — "reward, don't punish" carried forward unchanged.
  - **Instinct/bond scaling preserved via a new formula**, `TimedInputConfig.ComputeToleranceHalfWidth`:
    scales a base tolerance half-width by the SAME curve `ComputeWindowPercent` already used
    (`computedWindowPercent / baseWindowPercent`), so "higher Instinct = larger window" (CLAUDE.md)
    still holds, just applied to a ratio tolerance instead of a bar-position window. At 0
    Instinct/bond this returns exactly the base value the user gave.
  - `RunTimedInput`/`RunDefenseTimedInput`'s signatures changed from `windowPercent` to
    `toleranceHalfWidth` params accordingly; `BattleManager` now calls
    `ComputeToleranceHalfWidth` instead of `ComputeWindowPercent` directly for both.
  - The now-fully-unused horizontal bar apparatus (`TimingBarTrack`/`TimingBarZone`/
    `TimingBarMarker`/`TimingBarButton` and all `.timing-bar-*` CSS) was deleted outright, not
    left as dead code — `ActionAnnouncement`/`ActionAnnouncementLabel` (renamed from
    `TimingBarContainer`/`TimingBarLabel`, since "TimingBar" was now a misnomer) is purely a text
    label host for both phases now.
  - `RingVisual` simplified to match: `PrimaryInnerRadius`/`PrimaryOuterRadius`/
    `SecondaryInnerRadius`/`SecondaryOuterRadius` (the band-drawing fields from the first rework)
    replaced with just `TargetRadius`/`MarkerRadius`/`MarkerColor`.
- **Why:** User's explicit asks, in sequence: (1) make offense match defense's converging-ring
  feel, positioned on the enemy; (2) simplify the ring to a single moving ring against a fixed
  target instead of pre-drawn bands, with specific ratio tolerances and flash colors per outcome;
  (3) add a red flash specifically for an absolute miss, distinguishing it from the colored
  success flashes.
- **Date:** 2026-08-05
- **Verified:** Live Play Mode, each round — compile clean, 114/114 EditMode tests (3 new:
  `ComputeToleranceHalfWidth`'s zero-stat/Instinct-scaling/Dodge-vs-Parry behavior). Confirmed via
  a real drag-to-target attack (12 damage, resolved correctly with the ring on the enemy),
  isolated synthetic-click proofs for every outcome (Dodge/Parry/offense success via a
  deliberately huge tolerance to make timing deterministic; Miss via a deliberately tiny
  tolerance), and a screenshot of the new minimal target+marker visual rendering correctly above
  the defending creature (replacing the earlier two-band screenshot from the prior entry).
- **Ref:** Builds directly on the "Sonny 2-style radial move selection + drag-to-target;
  converging-ring defense timing" entry above — read that one first for the ring's origin.

### [Combat] Ring flash colors redesigned — outcome QUALITY (miss/success/perfect), not outcome TYPE
- **Decided:** The converging ring's flash colors (from the prior "Unified converging-ring
  timing" entry above) changed from per-MOVE colors (Dodge=orange, Parry=green, offense=green) to
  per-OUTCOME-QUALITY colors shared identically by Dodge/Parry/offense:
  - **Red** (`MissFlashColor`, unchanged) — any Miss: wrong tolerance, wrong mouse button, or a
    timeout with no click at all.
  - **Green** (`SuccessFlashColor`) — a normal success (within the full tolerance half-width, but
    not within the tighter "perfect" band below).
  - **Neon purple** (`PerfectFlashColor`, `#B026FF`-ish — chosen to read clearly against the dark
    battle background and against the existing green/red) — a NEW "perfect" tier: the deviation
    from a dead-center ratio match is within `PerfectToleranceFraction` (0.2, i.e. the innermost
    20%) of the outcome's own tolerance half-width. Exposed via new
    `LastTimedInputWasPerfect`/`LastDefenseWasPerfect` bool properties, alongside the existing
    `LastTimedInputSuccess`/`LastDefenseOutcome`.
  - **Not wired to any gameplay bonus yet** — deliberately visual-feedback-only for this pass. A
    "perfect" hit resolves identically to a normal success for damage/avoidance purposes right
    now; the properties above exist so a future pass (bonus damage on a perfect offensive hit, an
    extra-safe counter window on a perfect Parry, etc.) doesn't need to re-derive this math, but
    nothing was invented ahead of an actual ask for what a perfect SHOULD do mechanically.
- **Why:** User's explicit ask, after seeing the prior per-move coloring, asking directly "does
  Dodge give green too?" (it didn't — it was orange) and requesting a redesign around a
  miss/success/perfect quality ladder instead, with the perfect tier reading as a distinct bright
  "reward" color (referencing a neon-purple-lightning visual) that "needs to contrast well."
- **Date:** 2026-08-05
- **Verified:** Live Play Mode, using a new verification technique for this precision-dependent
  case — manually driving the `RunDefenseTimedInput` `IEnumerator` via direct `MoveNext()` calls
  (bypassing `StartCoroutine`/Unity's Update scheduler entirely) to deterministically land the
  marker radius at an exact, chosen ratio before firing a synthetic click, sidestepping this
  session's recurring issue where an unfocused Editor window reports large/unpredictable
  `Time.deltaTime` jumps that make wall-clock-timed live clicks land unpredictably. Confirmed
  exact RGBA matches for all three tiers: a deviation of ~0.00002 (essentially dead-center)
  correctly resolved Parry as perfect (`LastDefenseWasPerfect=true`, marker color exactly
  `PerfectFlashColor`); a deviation of 0.15 (inside Dodge's 0.25 tolerance but outside its 0.05
  perfect band) correctly resolved as a non-perfect Dodge success (exactly `SuccessFlashColor`,
  screenshot-confirmed rendering green); the earlier Miss/red proof from the prior entry stands
  unchanged since Miss logic itself didn't change, only its surrounding tiers did.
- **Ref:** Builds directly on the "Unified converging-ring timing..." entry above.

### [Combat] Second move option + circular Sonny 2-style icons for move selection
- **Decided:** Two related follow-ups to the radial move-selection system above:
  - **A second placeholder move** now exists per party member (`MoveOptionsPerSlot = 2`), both
    still mechanically identical basic attacks — this pass is about the SELECTION UI, not new
    skill content. `BattleHUDController.PositionMoveOptions` computes each option's position via
    trigonometry (evenly spaced across `MoveOptionArcDegrees`, centered on straight up) rather
    than a hardcoded single offset, so a future 3rd/4th option needs no new positioning code —
    generalizes the "first slot of a radial system" comment from the original entry now that a
    second slot actually exists.
  - **Icon style changed from rounded-rect pills with descriptive text ("Attack 1"/"Attack 2")
    to plain circles labeled "ATK"** (both identical text — position alone distinguishes them),
    directly matching the user's Sonny 2 screenshot reference (small round ability icons, no
    description needed). `.move-option-placeholder` is now a fixed 48x48 circle
    (`border-radius: 24px`) instead of a 70x38 pill; `PositionMoveOptions`' self-centering offset
    constants updated to match (half-width/height 24/24 instead of 35/19).
- **Why:** User wanted to see the 2-option layout before wiring Aura costs, then asked for the
  icon style specifically — round, minimal text, matching Sonny 2's actual reference art rather
  than a generic pill-button look.
- **Verified:** Live Play Mode screenshot — two 48px circles now sit cleanly apart above the
  creature with no overlap (the pill shape's larger width previously caused them to touch at the
  same radius/arc); 114/114 EditMode tests unaffected (pure UI sizing/positioning change).
- **Ref:** Builds on the "Sonny 2-style radial move selection..." entry above.

### [Combat] Attack Aura cost + perfect-Dodge/Parry Aura restore
- **Decided:** Attacks now cost Aura, and a perfect Dodge/Parry restores it — the loop the user
  asked for after confirming the 2-attack layout looked right:
  - `BattleParticipant.SpendAura(amount)` (clamped at 0, never blocks the attack — a Phasix can
    always swing even at 0 Aura, the cost just floors out) and `.RestoreAura(amount)` (clamped at
    `MaxAura`) added alongside the existing `ApplyDamage`. `CurrentAura`'s setter changed from
    `{ get; }` to `{ get; private set; }` to allow this.
  - `BattleConfig.AttackAuraCost` (2, placeholder) spent once per attack, right after the target
    is confirmed via drag, in `PlayerTurn` — both placeholder attacks (`MoveOptionsPerSlot`) cost
    the same for now; real per-skill costs are a later pass once real skill content exists.
  - `BattleConfig.PerfectDefenseAuraRestore` (2, placeholder) restored to the DEFENDER in
    `EnemyTurn` when `defended && LastDefenseWasPerfect` — on top of avoiding the hit (and, for a
    perfect Parry, the automatic counter-attack). A battle log line ("X restores Aura!") appends
    after the normal Dodge/Parry result line so the reward reads clearly.
  - `BattleHUDController.RefreshHP` renamed to `RefreshBars` and extended to also refresh Aura
    fill bars — previously Aura was set once in `Initialize` and never touched again since
    nothing consumed it; now it changes during battle, so it needs the same live-refresh
    treatment HP already had.
- **Why:** User's explicit ask, once the 2-attack circular-icon layout was confirmed: "make them
  cost some aura. Perfect dodges and parrys restore aura."
- **Alternatives rejected:** Gating the attack on having enough Aura (blocking it entirely below
  cost) — not requested; chose the simpler "always allowed, floors at 0" behavior instead of
  inventing a block/insufficient-Aura UX that wasn't asked for.
- **Date:** 2026-08-05
- **Verified:** Live Play Mode — a real drag-to-target attack correctly dropped the attacker's
  Aura from 5/5 to 3/5 (screenshot-confirmed bar shortening); `RestoreAura` confirmed live against
  the actual battle-state participant (3 -&gt; 5, clamped at Max), with the HUD bar
  screenshot-confirmed returning to full. `LastDefenseWasPerfect`'s own correctness was already
  proven in the prior "Ring flash colors redesigned" entry via manual coroutine-stepping — this
  pass adds the `RestoreAura` wiring on top of that already-verified signal. 120/120 EditMode
  tests (6 new: `BattleParticipantTests` covering `SpendAura`/`RestoreAura` clamping).
- **Ref:** Builds on the "Second move option + circular Sonny 2-style icons" entry above.

### [Combat] Starting Aura raised; numeric HP/Aura readouts; move orbs pushed further out and shrunk
- **Decided:** Three small follow-up polish items after the Aura cost/restore pass:
  - **Test species Aura bumped 5 -> 20** (`Test_FireType`/`Test_SteamType` `_aura` field) — at
    `AttackAuraCost`=2, a pool of 5 only allowed ~2 attacks before running dry; 20 gives enough
    room to see the cost/restore loop play out over a real multi-round battle, same reasoning as
    the earlier Vitality bump (120/110) from earlier this session.
  - **Numeric "current/max" readouts** added on top of both the HP and Aura fill bars (all 4
    status rows — 3 player slots + the enemy slot), via a new `.bar-value-label` overlay Label
    (absolute-positioned over the bar-back, drawn after the fill so it's always on top,
    `-unity-text-outline` for legibility against either the filled or unfilled portion).
    `SetHPFill`/`SetAuraFill` now take the label alongside the fill element and set both in one
    call.
  - **Move orbs pushed out and shrunk**: `MoveOptionRadius` 60px -> 95px (real separation from the
    creature — "floating orb" feel instead of touching it) and `.move-option-placeholder` 32x32
    (down from 48x48) — `PositionMoveOptions`' self-centering offset constants updated to match
    (16/16 instead of 24/24). Labels changed from "ATK"/"ATK" to "A" (Attack) and "S" (Strike) —
    still mechanically identical basic attacks, but now distinguishable at a glance without
    needing to read a full word on a small orb.
- **Why:** User's explicit ask after seeing the Aura cost/restore mechanic live: numbers on the
  bars to actually see point values (not just fill-bar proportions), more visual breathing room
  between the creature and its move orbs, and shorter single-letter labels matching the smaller
  orb size.
- **Date:** 2026-08-05
- **Verified:** Live Play Mode screenshot — HP reads "120/120", Aura reads "20/20" on both
  status rows; "S"/"A" orbs render smaller with clear separation from the creature circle.
  120/120 EditMode tests unaffected (pure UI/data-value change, no logic touched).
- **Ref:** Builds on the "Attack Aura cost + perfect-Dodge/Parry Aura restore" entry above.

### [Combat] Move orbs distributed around a full circle, not an arc above the creature
- **Decided:** `PositionMoveOptions` changed from spacing options across an 80° arc centered
  straight up (`MoveOptionArcDegrees`, now removed) to spacing them evenly around the FULL 360°
  circle: `angleDegrees = 90 + 360*i/total`. For the current 2-option case this puts "A" directly
  above the creature (90°, unchanged) and "S" directly below it (270°) — opposite points on the
  circle — rather than both clustered near the top. Generalizes correctly for a future 3rd/4th
  option (each 360/N degrees apart, wrapping all the way around).
- **Why:** User's explicit ask — "make sure that the attack orbs are in a circle around the
  phasix," after seeing them both still clustered above it from the prior arc-based layout.
- **Date:** 2026-08-05
- **Verified:** Live Play Mode screenshot — "A" sits directly above and "S" directly below the
  creature, symmetric, no overlap with the status header above or the action panel below.
  120/120 EditMode tests unaffected (pure positioning-math change).
- **Ref:** Builds on the "Starting Aura raised; numeric HP/Aura readouts; move orbs pushed further
  out and shrunk" entry above.

### [Combat] Move orbs fixed at 1/11 o'clock; centered labels; blue/green differentiation
- **Decided:** Reverted the full-360° distribution from the immediately prior entry in favor of
  fixed clock-face positions: `MoveOptionClockHours = { 1f, 11f }`, converted to standard math
  degrees via `angleDegrees = 90 - 30*hour` (12 o'clock = 90°/straight up, each hour = 30°
  clockwise). Both orbs now sit near the top, flanking straight-up, instead of one above and one
  below the creature. Also: `.move-option-label` switched from relying on the parent's flex
  `align-items`/`justify-content` (removed from `.move-option-placeholder`) to an absolute-fill
  overlay (`left/right/top/bottom:0` + `-unity-text-align:middle-center`), guaranteeing the letter
  sits dead-center regardless of Label's own box quirks. New `.move-option-blue` class applied to
  the "S" orb only (`_MoveOption1` in all 3 `PlayerStageSlot` entries); "A" (`_MoveOption0`) keeps
  the original green styling.
- **Why:** User's explicit ask — "put them on the 1 and 11 oclock positions. Make sure the letters
  are centered on the orb. Make one blue and the other green" — after seeing the full-circle
  top/bottom split from the prior entry and preferring both options readable near the creature's
  head instead of one being below it.
- **Alternatives rejected:** Keeping the generalized `360*i/total` formula and special-casing 2
  options to land at 1/11 — rejected in favor of an explicit fixed-position array
  (`MoveOptionClockHours`) since the user specified exact clock positions, not a formula; a future
  3rd/4th option will need its own explicit clock-hour value added to the array rather than an
  auto-computed even split.
- **Date:** 2026-08-05
- **Verified:** Live Play Mode screenshot (1920px) — blue "S" orb at 11 o'clock, green "A" orb at
  1 o'clock, both letters visually centered within their circles. 120/120 EditMode tests
  unaffected (pure positioning/styling change).
- **Ref:** Builds on the "Move orbs distributed around a full circle, not an arc above the
  creature" entry above.

### [Combat] Move-option label centering fix — Unity's default Label margin/padding was the real cause
- **Found:** The prior "absolute-fill + `-unity-text-align:middle-center`" pass on
  `.move-option-label` still rendered off-center. Measured live via `execute_code` (querying
  `Label.layout`/`resolvedStyle`): Unity's default runtime theme applies a non-zero, ASYMMETRIC
  margin (2/4/4/2 px, left/right/top/bottom) and padding (1/2/4/4 px) to every Label. With only
  `left:0;right:0;top:0;bottom:0` set, that inherited margin still shifted the box right+down
  inside the 0-inset rect — centering math was correct, the box being centered wasn't the true
  32x32 (minus border) area.
- **Decided:** Added explicit `margin: 0; padding: 0;` to `.move-option-label`. Confirmed via the
  same live measurement approach (not just eyeballing a screenshot) that the label's layout box
  now matches the option's content box exactly.
- **Why:** User's explicit ask — "the lettering is still not in the center of the orbs" — after
  the first centering attempt visually still looked off. Root-caused with real layout data instead
  of guessing at more CSS changes.
- **Date:** 2026-08-06
- **Verified:** Live `execute_code` layout query before/after the fix, plus a live Play Mode
  screenshot. 120/120 EditMode tests unaffected (pure styling change).
- **Ref:** Builds on the "Move orbs fixed at 1/11 o'clock; centered labels; blue/green
  differentiation" entry above.

### [Combat] "S" (Strike) replaced with "C" (Charge) — restores Aura instead of attacking
- **Decided:** The second move option is no longer a second basic attack. Renamed "S" -> "C"
  (blue orb unchanged). Clicking it does NOT start the click-and-drag targeting flow — it
  resolves immediately on click (new `BattleHUDController.SelectCharge`, registered instead of
  `BeginDrag` for option index 1 only) and restores `BattleConfig.ChargeAuraRestore` (10) Aura to
  the acting participant via the existing `BattleParticipant.RestoreAura`, with no target, no
  timed input, and no damage — it consumes that attacker's action for the turn
  (`BattleManager.PlayerTurn`'s new `chargeSelected` branch, `continue`s to the next party
  member instead of falling through to the attack/Aura-cost/timed-input path). New
  `ShowMoveSelection` overload takes an `onChargeSelected` callback alongside the existing
  `onTargetConfirmed`.
- **Why:** User's explicit ask — "next change the blue orb into a letter 'C' and let it be a
  charge mechanic. So when you click on it, the player does not attack but it restores 10 mana."
- **Alternatives rejected:** Gating Charge behind the same drag-to-target gesture as Attack (for
  UI consistency) — rejected because Charge has no target; forcing a drag onto an enemy for a
  self-targeted action would be confusing, not consistent. A single click matches "this move
  needs no target" more honestly.
- **Date:** 2026-08-06
- **Verified:** Live Play Mode via `execute_code` — drained a participant's Aura to 5/20,
  clicked "C", confirmed Aura became 15/20 (10 restored, correctly additive) and the enemy's HP
  stayed at 120/120 (no attack occurred). Battle log correctly read "... charges, restoring
  Aura!". 120/120 EditMode tests unaffected (BattleParticipant.RestoreAura's own clamping was
  already covered by BattleParticipantTests).
- **Ref:** Builds on "Attack Aura cost + perfect-Dodge/Parry Aura restore" above.

### [Combat/Performance] Companion's A* pathfinding (CompanionAI/AIPath) paused during battle
- **Found:** User noticed live in the Editor console that the companion's A* Pathfinding
  Project search kept running while a battle was in progress. Root cause: `BattleManager.Start()`
  hides the overworld camera and `WildEncounterCreature` freezes the player's own movement/input
  (`PlayerTopDownController.FreezeMovement`), but nothing paused the active companion — its
  `CompanionAI.Update()` kept calling `_aiPath.destination = ComputeDestination(...)` every frame,
  and `AIPath` itself keeps re-searching a path on its own internal `repathRate` timer regardless
  of whether the destination actually changed. With the player frozen the companion has nothing
  meaningful to path toward for the whole battle — pure wasted computation, invisible the whole
  time since the overworld camera is hidden.
- **Decided:** New `CompanionAI.SetPaused(bool)` — disables the component itself (stops
  Update/FixedUpdate) AND disables the `AIPath` component specifically (disabling CompanionAI
  alone isn't enough; AIPath's repath timer is independent of it), zeroing the Rigidbody2D's
  velocity on pause. New `PartySystem.ActiveCompanionAI` exposes the spawned companion's
  `CompanionAI` (previously private-only). `BattleManager.Start()` calls
  `PartySystem.Instance?.ActiveCompanionAI?.SetPaused(true)` right alongside where it already
  hides the overworld camera; `BattleManager.EndBattle()` calls `SetPaused(false)` right alongside
  where it already restores the camera — same symmetric pair, same trigger points.
- **Why:** User's explicit question — "does that need to be computing while in battle scene? if
  its not computationally expensive we can leave it otherwise we should pause that computation."
  Answer: not expensive for a single companion, but genuinely unnecessary (nothing to path toward,
  nothing visible), so paused rather than left running on the "not expensive so it's fine"
  reasoning alone — matches CLAUDE.md's "No heavy logic in Update()" spirit even at small scale.
- **Alternatives rejected:** Disabling the whole companion GameObject (`SetActive(false)`) —
  rejected because that would also tear down/re-run `Awake()` state unnecessarily and is a bigger
  hammer than needed; disabling just the two components that actually do per-frame work is more
  precise and mirrors the existing `FreezeMovement`/`UnfreezeMovement` pattern already used for
  the player in the same code path.
- **Date:** 2026-08-06
- **Verified:** Live `execute_code` — confirmed `CompanionAI.enabled` and `AIPath.enabled` both
  `true` before engaging a battle, both `false` immediately after `HandleEngage` loads
  `BattleScene_Main`. 120/120 EditMode tests unaffected (no EditMode coverage of Play-mode-only
  companion/battle wiring).
- **Ref:** Independent of the move-orb changes above — flagged mid-session by the user while
  reviewing the Editor console during battle testing.

### [Combat] 12-slot skill ring around the acting creature — "A"/"C" are 2 filled slots, not decorations
- **Decided:** 12 dark grey, unlabeled, non-interactive placeholder circles
  (`.skill-slot-placeholder`, new `BattleHUDController.PositionSkillSlots`/`SkillSlotCount`/
  `SkillSlotRadius`) distributed one per clock hour (1 through 12) around the acting player
  creature. Went through two rounds of live user feedback to land on the final model: (1) first
  pass put them on their own larger 140px ring outside `MoveOptionRadius` (95px) so nothing
  overlapped "A"/"C" — user then asked why the grey orbs weren't "on the same path" as the
  original A/C spacing they liked; (2) moved to the SAME `SkillSlotRadius = MoveOptionRadius`
  path, which put a small (20px) grey circle directly under each 32px "A"/"C" orb — user then
  clarified the actual model: "the grey orbs are supposed to be slots then the A and C orbs are
  supposed to be slotted into the 1 and 11 o'clock slots," i.e. all 12 should read as one uniform
  set of slots, 2 of which already have a skill filled in, not 12 identical decorations plus 2
  bigger unrelated orbs layered on top. Final state: `.skill-slot-placeholder` is now the SAME
  32x32 size as `.move-option-placeholder`, on the SAME radius/path — 10 read empty (grey), 2
  read filled (colored + lettered). `BattleHUD.uxml` orders the 12 skill slots BEFORE the move
  options in each `PlayerStageSlot` so "A"/"C" paint on top and fully cover the grey slot
  underneath at those two positions, rather than a smaller circle peeking out from behind a
  bigger one. Shown/hidden in lockstep with "A"/"C" via the existing `SetMoveOptionsVisible`.
  Purely visual for now — no click handler, no backing data; future skill content slots in here
  once the skill tree framework has real skills to show (still "taxonomy locked, individual
  skills pending" per CLAUDE.md).
- **Why:** User's explicit ask — "place 12 dark grey placeholder circles around the player. These
  are slots for different skills to be placed. we can keep the A orb in 1 oclock and the c orb in
  the 11 oclock" — refined live across two follow-up corrections (same path, then same size/model)
  once the first two passes didn't match what "slots" was actually supposed to mean.
- **Alternatives rejected:** A separate, larger outer ring for the 12 grey circles (first pass) —
  rejected once the user said they preferred the original A/C spacing. A same-radius-but-smaller
  grey circle peeking out from under "A"/"C" (second pass) — rejected once the user clarified
  "A"/"C" are two of the 12 slots, filled, not a separate decorative layer; the two elements
  needed to be the same size for that reading to hold up visually.
- **Date:** 2026-08-06
- **Verified:** Live Play Mode screenshot (1920px) after each of the three iterations — final
  version shows 12 uniform 32x32 circles evenly ringed around the creature, "A" (green) and "C"
  (blue) fully occupying the 1/11 o'clock slots with no grey visible underneath, the other 10
  empty/grey, no overlap with the status header or action panel. 120/120 EditMode tests
  unaffected throughout (pure visual change, no logic).
- **Ref:** Builds on "Move orbs fixed at 1/11 o'clock..." above; independent of the Charge and
  companion-pause entries.

### [Combat] "H" orb added; "C" reworked to self-target drag; unified MoveKind targeting model
- **Decided:** New third move option "H" (pastel pink, `.move-option-pink`, 12 o'clock) — a
  solo/self-only skill whose actual effect is still undecided (only its targeting interaction was
  requested; placeholder logs `"{name} uses H!"` and ends the turn, no stat change, per CLAUDE.md's
  "no invented content" rule). "C" (Charge) reworked from an immediate-resolve-on-click action
  to use the SAME click-and-drag gesture as "A" (Attack) and "H" — clicking it alone no longer
  does anything; the player must drag and release over a valid target. Introduced a private
  `MoveKind { Attack, Charge, Heal }` enum so `BeginDrag`/`OnDragPointerUp` share one code path:
  Attack's valid drop target is the enemy (`_enemyStageCreature`); Charge and Heal's valid drop
  target is ONLY the caster's own creature (`_playerStageCreatures[fromSlotIndex]`) — dragging
  either onto the enemy is rejected exactly like dragging Attack onto empty space, cancelling
  back to the move options (the only cancel path that exists, unchanged). `ShowMoveSelection`
  signature grew a `BattleParticipant self` parameter and split `onChargeSelected` (no-arg
  `Action`) into `onChargeConfirmed`/`onHealConfirmed` (`Action<BattleParticipant>`, always
  invoked with `self`) so BattleManager's callbacks stay symmetric with Attack's
  `onAttackConfirmed`. Removed the now-obsolete `SelectCharge` method.
- **Why:** User's explicit ask — "add a new orb with character 'H'... when pressed a character
  needs to be selected. This one happens to only make it so you can select the character that is
  casting it... Need to update the C orb so that it acts like the new orb well you click it but
  then choose who to select. In this case its a solo skill so you can only select the player
  casting it." User also called out that "A" already has an implicit cancel (drag-and-release
  elsewhere) and that this should carry over to C/H's new targeting step, not be a separate
  feature.
- **Alternatives rejected:** A distinct two-step "click orb, then click target" interaction
  (separate from the drag gesture) — rejected in favor of reusing the existing click-and-drag
  gesture with a restricted valid-target set; this keeps all three moves feeling consistent (per
  the user's own "click it but then choose who to select" framing, which already described how
  Attack works) and reuses the existing cancel-on-invalid-release behavior for free instead of
  building a second cancel mechanism from scratch.
- **Date:** 2026-08-06
- **Verified:** Live Play Mode via `execute_code` — (1) dragged "H" and released over the
  caster's own creature: battle log correctly read "Test Fire Placeholder uses H!". (2) Dragged
  "C" and released over the ENEMY: correctly rejected — no Aura change, no log entry, move
  options reappeared for a retry, matching Attack's existing cancel behavior exactly. 120/120
  EditMode tests unaffected (pure UI targeting/interaction change, no formula logic touched).
- **Ref:** Builds on "'S' (Strike) replaced with 'C' (Charge)..." and the skill-ring entries
  above.

### [Combat] "H" given a real effect; new "R" (Regen) orb + status-icon-with-countdown system
- **Decided:** "H" (Heal) got its long-pending effect: costs `HealAuraCost` (6) Aura, heals
  `HealAmount` (4) HP instantly on cast (`BattleParticipant.Heal`, clamped at MaxHP). New fourth
  move option "R" (Regen, purple `.move-option-purple`, 2 o'clock) — same solo/self-only
  click-and-drag targeting as C/H: costs `RegenAuraCost` (8) Aura, applies a status
  (`BattleParticipant.ApplyRegen`/`RegenTurnsRemaining`/`RegenHealPerTurn`) that heals
  `RegenHealPerTurn` (2) HP at the END of the player's turn for `RegenDurationTurns` (4) turns,
  via a new `BattleManager.TickPlayerRegen()` called once per PlayerTurn (for every alive
  party member, not just whoever acted that turn) right before the Continue gate — a status cast
  this same turn still gets its first tick immediately rather than waiting a full extra turn.
  Refactored `BattleHUDController`'s move-targeting system from one named callback per move
  (`onAttackConfirmed`/`onChargeConfirmed`/`onHealConfirmed`) to a single
  `Action&lt;int optionIndex, BattleParticipant target&gt; onMoveConfirmed` plus a
  `MoveOptionIsSelfOnly` bool array (index-matched to `MoveOptionClockHours`) — this was the
  trigger point for generalizing (a 4th self-only move would have meant a 4th named callback
  parameter); `BattleManager.PlayerTurn` now switches on `optionIndex` via named
  `MoveOptionAttack/Charge/Heal/Regen` constants instead. New status bar: a small row under each
  player's HP/Aura box (`.status-bar`) with a miniature purple "R" icon (`.status-icon`,
  `.status-icon-purple` — same colors as `.move-option-purple`, a third the size) that appears
  the moment Regen is cast and shows a COUNTDOWN subscript (4, 3, 2, 1, then hidden — not a
  count-up) positioned OUTSIDE the icon's own circular frame at bottom-right
  (`.status-icon-counter-br`). A mirrored `.status-icon-counter-bl` (bottom-left) class is defined
  but unused — reserved for a future START-of-turn effect, per the user's own framing ("skills
  that are on the bottom left as a counter... indicate skills/buffs/debuffs that apply at the
  start of the turn... vs the end of the turn").
- **Why:** User's explicit ask — "the heal should cost 6 aura and heals 4 HP. so i want to
  create another orb put on the 2oclock that that is purple and have it called 'R'... It is a
  regen that costs 8 aura but heals 2 HP at the end of the players turn for 4 turns... We need a
  small icon that looks exactly the like R orb but smaller at the top underneath the health and
  aura box icon. acting like a status bar. when the skill is cast, there will be a small
  subscript that displays the counter that is outside the orb frame but be on the bottom right."
  Countdown direction (4→3→2→1) confirmed separately: "i think a countdown counter is better for
  user intuition."
- **Alternatives rejected:** Keeping one `Action&lt;BattleParticipant&gt;` callback parameter per
  move on `ShowMoveSelection` (mirroring how "H" was added on top of "A"/"C") — rejected once a
  4th self-only move (Regen) made the pattern clearly not scale; generalizing to
  `(optionIndex, target)` plus an index-matched `MoveOptionIsSelfOnly` array means a future 5th
  move needs only a new array entry, UXML element, and switch case — no signature change.
  Counting UP (0→1→2→3, "turns elapsed") instead of down — rejected per the user's explicit
  intuition call for a countdown.
- **Date:** 2026-08-06
- **Verified:** Live Play Mode via `execute_code` — drained a participant to 105/120 HP, 16/20
  Aura; cast "R" (spent to 8/20 Aura); confirmed within the same turn: battle log read "Test Fire
  Placeholder uses R!" then "Test Fire Placeholder regenerates 2 HP!" (immediate first tick), HP
  105→107, `RegenTurnsRemaining` 4→3, status icon `display:Flex` with counter text "3",
  screenshot-confirmed the purple "R" icon sitting under the Aura bar with "3" rendered outside
  its frame at bottom-right. 129/129 EditMode tests (9 new: `BattleParticipantTests` covering
  `Heal`/`ApplyRegen`/`TickRegen`, including max-HP clamping mid-tick and the countdown reaching
  0 and staying inert on a 5th tick).
- **Ref:** Builds on "'H' orb added; 'C' reworked to self-target drag; unified MoveKind targeting
  model" above — that entry's `MoveKind` enum is superseded by `MoveOptionIsSelfOnly` here.

### [Combat] Status-bar polish: countdown legibility + fixed reserved height
- **Decided:** Two fixes to the status bar added in the previous entry. (1) The countdown
  subscript (`.status-icon-counter-br`/`-bl`) moved from `right/left:-5px; bottom:-5px;` to
  `right/left:-2px; bottom:-13px;` — the original offset only barely cleared the icon's bottom
  edge, reading as overlapping/cramped; dropping it further gives real visual separation. (2)
  `.status-bar` given an explicit fixed `height: 26px` — without one, a flex row whose only child
  is `display:none` collapses to 0px, so the whole status-row box visibly grew/shrank every time
  a status activated or expired. Also added an (empty, for now) `EnemySlot0_StatusBar` element
  with the same `.status-bar` class so the enemy's status-row box reserves identical height to
  the player rows, even though no enemy-side status exists yet — "dedicated space for both player
  and enemy."
- **Why:** User's explicit feedback after seeing the first version live — "its hard to read the
  subscript drop it a little lower so its more viewable and not overlapping with the icon. Then
  make it so the text box has a dedicated space for both player and enemy so it doesnt auto
  resize when it appears and disappears."
- **Date:** 2026-08-06
- **Verified:** Live Play Mode via `execute_code` — measured `PlayerSlot0`'s resolved row height
  before casting Regen (161px) and again once the status icon was active (161px, unchanged);
  `EnemySlot0`'s row height matched at 161px in both cases. Screenshot confirmed the "3" counter
  now renders with a clear gap below the "R" icon instead of overlapping its corner. 129/129
  EditMode tests unaffected (pure USS/UXML change, no logic touched).
- **Ref:** Follow-up to "'H' given a real effect; new 'R' (Regen) orb + status-icon-with-countdown
  system" above.

### [Combat] Continue button removed — every beat, including the turn transition, now auto-paces
- **Decided:** Removed the last click-to-proceed gate in the battle loop.
  `BattleHUDController.WaitForContinue` (and its backing `ContinueButton`/`_continuePressed`/
  `_continueButton` fields) deleted outright — `BattleManager.PlayerTurn`'s end-of-turn call
  became a `ShowTimedMessage("Enemy's turn...", BattleConfig.AutoMessageDurationSeconds)` instead,
  matching every other beat in the battle (attack results, enemy announcements, counter-attacks
  already used ShowTimedMessage; only the player-to-enemy transition still gated on a click).
  Also deleted the now-dead `.prompt-button` CSS class (its only remaining user in BattleHUD.uss)
  and the `<ui:Button name="ContinueButton">` UXML element. `ContinuePrompt`/`ContinuePromptLabel`
  and the `.continue-prompt` class stay — `ShowTimedMessage` still uses that same panel/label for
  every auto-paced message, just without a button in it anymore.
- **Why:** User's explicit call after playtesting several turns — "the continue between the
  [turns] might not be needed anymore just a delay to let the player understand that the turns
  have switched."
- **Alternatives rejected:** Renaming `ContinuePrompt`/`.continue-prompt` to something
  delay-neutral (e.g. "beat-message") now that no button lives there — rejected as unnecessary
  churn; the element still does exactly what its name suggests at a glance (a message panel
  between beats), and renaming would touch every `_continuePrompt`/`_continuePromptLabel`
  reference in BattleHUDController for a purely cosmetic gain.
- **Date:** 2026-08-06
- **Verified:** Live Play Mode — confirmed `Q&lt;Button&gt;("ContinueButton")` returns null (element
  genuinely gone from the tree, not just hidden). Cast Regen, then let the battle run with zero
  further clicks: the turn transitioned into EnemyTurn on its own and the enemy's attack
  (undefended, since no Dodge/Parry click was thrown either) resolved and logged automatically —
  confirms the whole loop is now click-optional end to end for pacing (Dodge/Parry clicks remain
  a deliberate exception — those are the actual defense minigame, not turn pacing). 129/129
  EditMode tests unaffected (pure UI/pacing change, no formula logic touched).
- **Ref:** Reverses the "Continue button only gates the turn transition" decision from
  2026-08-05 (see the [Combat] entries mid-file discussing `WaitForContinue`/`ShowTimedMessage`'s
  original split) — that split is now moot since nothing gates on a click anymore.

### [Combat] "R" (Regen) battle log now spells out "Aura Regen" instead of the bare orb letter
- **Decided:** The cast announcement's `AppendBattleLog` line changed from `"{name} uses R!"` to
  `"{name} uses Aura Regen!"`. The on-stage `ShowTimedMessage` announcement (shown briefly over
  the stage while the beat plays out) deliberately KEPT the short "R" form — it's the direct
  echo of the orb the player just pressed, legible for that instant; the persistent battle log
  is read after the fact with no orb visible alongside it, where the full name reads better.
- **Why:** User's explicit ask — "update the R to be aura regen on the battle log," scoped to the
  battle log specifically (not the on-stage announcement).
- **Date:** 2026-08-06
- **Verified:** Live Play Mode screenshot — battle log reads "Test Fire Placeholder uses Aura
  Regen!". 129/129 EditMode tests unaffected (pure string change).
- **Ref:** Same session as the Continue-button removal above.

### [Combat] Phase 3 Gate prep — CaptureSystem and EvolutionBurstSystem wired into the live battle loop
- **Found:** Asked "is there anything else left in the plan" — the plan's final Phase 3 Gate ("play
  the complete encounter → battle → capture/win loop several times back to back... confirm...
  capture attempts feel earned") couldn't actually be run: `BattleManager` only called into
  Attack/Charge/Heal/Regen and Dodge/Parry. Five Step 4/5 systems existed as real, tested classes
  (`CaptureSystem`, `ComboEngine`, `StatusEffectCatalog`/`StatusDurationCalculator`,
  `EvolutionBurstSystem`, `AuraStatAllocationSystem`) but NONE were reachable from live play — no
  Capture button, no way to trigger a combo, no status effects applying in a real battle, no
  evolution burst, no stat-allocation UI.
- **Decided:** Wired exactly two of the five — **CaptureSystem** and **EvolutionBurstSystem** —
  and deliberately left the other three alone:
  - **Left alone, on purpose:** `ComboEngine`/`StatusEffectCatalog`/`ChainResultCatalog`/
    `MasteryBonusCatalog` — this session's own `[Combat] Skill tree framework` entry already says
    "Revisit if: The skill-selection battle UI is built... at that point, wire
    ComboEngine/MasteryBonusCatalog/ChainResultCatalog into the actual battle loop." Wiring them
    now would mean either faking combos through the 4 generic orbs (exactly what that entry says
    not to do) or building a whole new generic status-application layer that wasn't part of this
    ask. `AuraStatAllocationSystem`/`AuraTierCeiling`/`ResonanceBonusEvaluator` spend `commonAura`
    CURRENCY (not the in-battle Aura stat `BattleParticipant` already uses) and are designed as a
    POST-BATTLE progression/menu system per `Progression_Directive_v0_1_0.md` — nothing in the
    design docs puts them inside a turn coroutine.
  - **CaptureSystem — new "K" move option** (gold, `.move-option-gold`, 3 o'clock,
    `MoveOptionCapture = 4`): targets the ENEMY like Attack (`MoveOptionIsSelfOnly[4] = false`,
    reusing the exact same drop-target code path, no new drag logic). No timed input, no Aura
    cost — no capture-item economy exists yet (CLAUDE.md: "Economy and items (§22 pending)"), so
    this is a free attempt rather than inventing a cost mechanism. On success:
    `PartySystem.Instance.AddToParty(target.RuntimeData)` adds the captured creature, then the
    battle ends immediately via `EndBattle(BattleOutcome.Won)` — single-enemy battles only (see
    class doc comment), so capturing the only enemy IS winning. New `_battleEndedEarly` flag lets
    `RunBattleLoop` skip its normal `TryEndBattle()` check (which is HP-based via
    `BattleEngine.CheckOutcome` and has no way to detect a capture, since the enemy's HP never
    changed) and avoid calling `EnemyTurn()` on a scene that's already mid-unload. On failure: logs
    it and `continue`s, ending that attacker's action for the turn — same pattern as Charge/Heal/
    Regen consuming a turn without an attack.
  - **EvolutionBurstSystem — status-only integration**, no stat/damage effect. New
    `BattleParticipant.BurstGauge` (every participant gets one, mirrors how MaxHP/MaxAura aren't
    gated by side either; only actually filled/ticked for player-side participants, same scope
    discipline Regen already established). New `BattleManager.AddBurstFillAndCheckTrigger` wired
    at all three of GDD §9.3's locked fill sources: skill use (every Attack/Charge/Heal/Regen cast
    — deliberately NOT Capture, which reads as a distinct action, not a "skill"), a successful
    offense timed input (bonus fill on top of the skill-use fill), and taking an undefended hit
    (`EnemyTurn`, only when `!defended` — a full Dodge/Parry avoids the hit entirely, so it
    shouldn't count as "taking" one). `EvolutionBurstSystem`'s own doc comment is explicit that
    "ApplyBurstEffects" (what actually changes about the creature during a burst) is genuinely
    undesigned in the GDD, not just placeholder-numbered — so this wiring makes the fill/trigger/
    expiry state machine fully observable (new `.status-icon-orange` "B" icon, same
    countdown-outside-the-frame pattern as Regen's "R" icon) WITHOUT fabricating a stat boost.
    `TickPlayerBurst` (called alongside `TickPlayerRegen`) counts the duration down each player
    turn and logs when it expires.
  - New `BattleConfig.BurstFillPerSkillUse/BurstFillPerTimedInputSuccess/BurstFillPerHitTaken`
    (15/10/10) — placeholder amounts, GDD locks the three SOURCES but gives no numbers.
- **Why:** User's explicit choice, after being shown that wiring all 5 would mean overriding this
  project's own recorded decisions: "Just Capture + Evolution Burst" — the two systems with clean,
  already-documented trigger points in the current loop.
- **Alternatives rejected:** Wiring all 5 (would contradict the Skill tree framework entry's own
  "revisit if" condition, and misplace Aura Stat Allocation outside its designed post-battle
  scope). Inventing a stat/damage effect for Evolution Burst's trigger (rejected — the GDD
  genuinely doesn't specify one; CLAUDE.md's "no invented content" rule applies here same as it
  did for "H" before its real effect was specified).
- **Date:** 2026-08-06
- **Verified:** Live Play Mode — dragged "K" onto the enemy at full HP (10% floor chance):
  correctly failed and logged "capture attempt failed!", turn continued normally into the enemy's
  attack. Isolated `CaptureSystem.AttemptCapture` loop (bypassing RNG luck) confirmed a success
  correctly grows the party via `PartySystem.AddToParty` (1 slot -> 2). Evolution Burst: filled
  the gauge to 100 directly, confirmed `TryTrigger` correctly failed on the first roll at 0% bond
  (below the 40% reliable threshold, a real 60%-chance miss) then succeeded on a retry (2-turn
  duration matches `BaseDurationTurns` at 0% bond); status icon appeared with the "2" countdown
  outside its frame, matching Regen's pattern exactly. `TickPlayerBurst` correctly counted 2->1->0
  and logged "Evolution Burst fades" with the icon hiding on expiry. A live Charge cast (not a
  direct API call) correctly grew the fill by 15 (skill use) + 10 (the same round's auto-played,
  undefended enemy hit) = 25 — confirms the actual `BattleManager` call sites fire correctly, not
  just the underlying systems in isolation. 129/129 EditMode tests passing throughout (no new
  tests added for this pass — both systems' own EditMode coverage, e.g.
  `EvolutionBurstSystemTests`/`CaptureSystemTests`, already existed pre-wiring and covers the
  static logic; this pass only adds call sites, which are exercised by the live verification
  above rather than new unit tests).
- **Ref:** Builds on every prior `[Combat]` entry in this file establishing the move-orb/drag-
  targeting/status-icon patterns this wiring reuses.

### [Combat] Evolution Burst gauge made visible + player-activated (was invisible + automatic)
- **Decided:** Reworked how Evolution Burst is surfaced and triggered, after the user asked "how
  do I access the Evolution Burst" and found there was no visible gauge at all. New visible purple
  `.burst-bar-fill`/`.burst-bar-back` bar directly under each player's Aura bar (bar order is now
  HP -> Aura -> Burst -> status-icon row; the buff/debuff status-icon row shifted down to make
  room). The bar fills live as `AddBurstFill` runs (renamed from `AddBurstFillAndCheckTrigger`,
  which no longer checks a trigger at all — see below). Once `FillPercent` reaches
  `EvolutionBurstSystem.TriggerThreshold` (now `public`, was `private`, so the UI can read the
  same number `ActivateReady` itself checks), the bar's back element gets `.burst-bar-ready` — a
  yellow border outline — and is clickable at ALL times via a new
  `BattleHUDController.BurstBarClicked` event (fires unconditionally on click; NOT gated to the
  owner's turn or move-selection window, since "I think the activation can be on the bar itself"
  reads as a free, always-available action, not a 6th move orb). `BattleManager` subscribes once
  in `Start()` and handles it in `HandleBurstBarClicked`, which calls a new
  `EvolutionBurstSystem.ActivateReady(gauge, bondPercent)` — deliberately NOT the old
  `TryTrigger`, because `ActivateReady` has NO bond-based reliability chance: a click on a bar the
  UI has already marked "ready" must just work, or the affordance lies. `ActivateReady` still only
  succeeds when `FillPercent >= TriggerThreshold` and the gauge isn't already active — "they can
  only activate when the gauge is full" (2026-08-06, user-confirmed) — so an early click (or a
  click on a dead party member, or on an already-bursting gauge) is a complete, harmless no-op.
  Bond still scales the resulting burst's DURATION via the unchanged `ComputeDurationTurns`. On
  success: the fill bar resets to empty/no border, the existing orange "B" status icon shows the
  "evolved state" with its turn countdown (unchanged from the prior pass — this reuses that
  infrastructure exactly, just changes what TRIGGERS it), and the battle log reads "X's Evolution
  Burst ignites!". `TryTrigger` itself is untouched and still has its own passing tests — it's
  simply unused by the live battle loop now, since its bond-gated-chance behavior is a genuinely
  different mechanic from the new click-to-activate one.
- **Why:** User's explicit ask, after finding no way to see or trigger the gauge: "Can you put a
  new bar underneath the aura bar, we can have it be filled as a purple bar. And instead of auto
  triggering, please make it so it becomes an activatable option... the activation can be on the
  bar itself... shift the buff/debuff icons below this new purple bar. When it hits max fill and
  it can be triggered, outline a yellow border around the purple bar and allow it to be
  clickable. When its clicked then it can show the buff that it is in a 'evolved' state." Confirmed
  separately: "please note they can only activate when the gauge is full."
- **Alternatives rejected:** Reusing `TryTrigger` for the click path (would silently fail some
  clicks on a bar the UI just told the player was ready, purely due to bond being below 40% —
  actively misleading given the new explicit "ready" affordance). Gating the bar's clickability to
  only the owner's move-selection window, matching how Attack/Charge/Heal/Regen/Capture work —
  rejected because the user described it as an action on the persistent header bar itself, not a
  move-wheel option, and nothing in the request implied it should consume that character's turn.
- **Date:** 2026-08-06
- **Verified:** Live Play Mode — filled the gauge to 85 directly, then a live Charge cast (+15)
  crossed the threshold; screenshot confirmed the bar fully purple with a visible yellow border.
  Clicked the bar: confirmed the "ignites!" log line, the orange "B" status icon with countdown
  appearing, and the fill bar resetting to empty/unbordered. Separately confirmed clicking a
  30%-filled (not-ready) bar is a complete no-op — `IsActive` stayed false and `FillPercent`
  stayed unchanged at 30, exactly matching "they can only activate when the gauge is full."
  133/133 EditMode tests (4 new: `ActivateReady`'s always-succeeds-when-full-regardless-of-bond,
  resets-fill-and-sets-duration, fails-below-full, and fails-while-already-active).
- **Ref:** Reworks the trigger mechanism from "'H' given a real effect; new 'R' (Regen) orb +
  status-icon-with-countdown system" and "Phase 3 Gate prep" above — those entries' AUTOMATIC
  `TryTrigger`-based integration is superseded by this one; the status-icon/countdown display
  those entries built is unchanged and fully reused here.

### [Combat] Open note — Evolution Burst needs a player-facing "what does this do" preview once designed
- **Found:** User flag, not a decision: "add a note that we need to advise what the burst does.
  Might be configurable like a skill in the skill tree at some point." The yellow-ready Burst bar
  (previous entry) currently promises "something happens" on click with zero preview of what,
  which is fine for a placeholder-effect state but won't hold up once a real effect exists.
- **Recorded for later:** Once `EvolutionBurstSystem`'s "ApplyBurstEffects" (what actually changes
  about the creature — genuinely undesigned, see that class's doc comment) gets a real answer, the
  HUD needs to surface it to the player before/при activation, not just log it after the fact. Also
  worth carrying forward as a live design direction, not yet decided either way: burst effects
  might end up CONFIGURABLE per creature via Type K (Evolve) skill trees rather than one fixed
  universal formula — i.e. which stat(s) a burst boosts and by how much could be a skill-tree
  choice, not a hardcoded constant. No implementation follows from this entry — it exists purely
  so this thread isn't lost before the skill-content pass that would actually resolve it.
- **Date:** 2026-08-06
- **Ref:** Companion note to `EvolutionBurstSystem.cs`'s own class doc comment (same date).

### [Combat] Free-choice creature selection replaces strict turn order; staggered layout; End Turn button
- **Found:** With a 2nd party member (via Capture, from the previous session's wiring), the user
  saw two problems live: (1) player creatures rendered side-by-side, cramped, each one's orb ring
  overlapping the next creature's space; (2) the old strict-turn-order foreach (each party member
  auto-prompted in a fixed sequence) didn't match how the reference games (Sonny 2, Slay the
  Spire 2) let the player freely choose which character acts, in any order.
- **Decided:** Four coordinated changes:
  1. **Free-choice selection** — `BattleManager.PlayerTurn` rewritten from a `foreach` over
     `PlayerSide` to an event-driven `while` loop. New `BattleHUDController.PlayerCreatureClicked`
     event fires whenever a player stage creature (the ball, or anywhere in its empty grey skill-
     slot cluster — anything that ISN'T a specific colored move orb, which calls
     `evt.StopPropagation()` so pressing a move doesn't ALSO count as "just select this creature")
     is pressed. Clicking an unacted creature opens its move wheel and waits for a pick; clicking
     a DIFFERENT creature before (or instead of) picking closes the current wheel and opens the
     new one — "click on another phasix the current phasix orb menu closes, then the new clicked
     phasix menu shows." Nothing is lost by switching, since nothing commits until a drag actually
     confirms a target.
  2. **Already-acted = read-only, not hidden** — new `BattleParticipant.HasActedThisTurn` (reset
     at the start of every `PlayerTurn`) and `BattleHUDController.ShowMoveSelectionReadOnly` — new
     `.move-option-disabled` (opacity 0.35) on every orb, and `BeginDrag` refuses to start a drag
     for a read-only slot. "If the phasix already moved during its turn then it can still show,
     but will be greyed out for active skills." Deliberately greys out ALL current moves — no move
     supports a second action yet — but `HasActedThisTurn` is a plain per-participant flag, not
     baked into any one move's logic, specifically so a future synergy skill or passive ("thinking
     all the current ones will be greyed out, but would like to include possibility for synergy
     skills or special passives that allow multiple actions") only needs its own check against
     this flag, not a restructure of how acted-state is tracked or shown.
  3. **Staggered stage layout** — `.stage-creature` margin-left/right widened 10px -> 28px, plus a
     new `BattleHUDController.ApplyStageCreatureStagger` applying a per-index vertical `translate`
     ({0, -45, 25} px) — a pure rendering transform, doesn't disturb
     PositionMoveOptions/PositionSkillSlots' math (both already relative to each creature's own
     untransformed box). "They need to be offset/staggered so you can see the full character
     similar to sonny 2 or slay the spire 2." Written index-matched, not party-size-matched, so
     the same method would apply to a future multi-slot `EnemyStageArea` — "each phasix needs to
     have its own orb slots revolving around it, both enemy and player" — though only the player
     side actually needed it this pass (still a single enemy slot).
  4. **Dedicated End Turn button** — new `EndTurnButton` (`.end-turn-button`, bottom-right of the
     stage, above the action/log panels) + `BattleHUDController.EndTurnClicked` event, shown only
     during the player's turn (`SetEndTurnButtonVisible`). "An end turn button thats separate from
     the dialogue boxes but is clear. Similar to slay the spire 2." The user explicitly
     distinguished this from the `ContinuePrompt`/`ActionAnnouncement` dialogue boxes, which stay
     for now but are "planned to be removed once there is more UI feedback to player" — the End
     Turn button is NOT on that removal list.
- **Why:** See "Found" above — both issues were observed live with a real 2-member party, not
  hypothetical.
- **Alternatives rejected:** Keeping strict turn order and just fixing the visual overlap —
  rejected because the user's ask was explicitly about the INTERACTION model ("click on the
  players phasix, after clicking the orbs then show"), not just spacing. Hard-disabling
  (hiding/removing) an already-acted creature's wheel entirely instead of showing it read-only —
  rejected, the user specifically said it "can still show, but will be greyed out."
- **Date:** 2026-08-06
- **Verified:** Live Play Mode with a real 2nd party member (added via `PartySystem.AddToParty`,
  mirroring what a real Capture does) — screenshot confirmed the two creatures render staggered
  (no overlap) and the End Turn button renders bottom-right, distinct from the dialogue boxes.
  Clicked creature 0: its wheel opened. Clicked creature 1 while creature 0's wheel was open:
  creature 0's wheel closed, creature 1's opened (confirmed via screenshot). Cast Charge on
  creature 1, then re-clicked it: wheel reopened visibly greyed; attempted to click "H" on it —
  confirmed via `BattleParticipant.CurrentAura` reading unchanged (20 before/after) that
  `BeginDrag` correctly refused the drag. End Turn: a synthetic UI Toolkit click on the `Button`
  element didn't register (a known limitation of simulating `Button`'s internal `Clickable`
  manipulator via `SendEvent` — real mouse input isn't affected, and this is Unity engine
  behavior, not project code), so verified by directly invoking the `EndTurnClicked` delegate
  instead (confirmed exactly 1 subscriber = `BattleManager`'s handler) — `_endTurnRequested`
  correctly flipped `true`, and the subsequent screenshot confirmed the turn genuinely
  transitioned into `EnemyTurn` (an attack resolved) and back into a fresh `PlayerTurn` (End Turn
  button reappeared). 133/133 EditMode tests unaffected (pure turn-flow/UI restructuring, no
  formula logic touched — no new tests added this pass, matching that this is orchestration, not
  new calculable behavior).
- **Ref:** Builds on the whole move-orb/drag-targeting/status-icon foundation established across
  every prior `[Combat]` entry in this file; supersedes the "sequential order, everyone acts"
  framing baked into those entries' own doc comments (updated in-code alongside this change).

### [Combat] Stage-creature layering fixed to match visual depth; selected wheel always frontmost
- **Found:** With the new staggered layout (previous entry) live and a real 2-member party, the
  user caught: "when selecting the front phasix some of the skill orbs were showing behind the
  phasix in a further away lane." Root cause: `ApplyStageCreatureStagger` only applied a `translate`
  (rendering-only, doesn't reorder siblings) — UI Toolkit draws children in fixed document order
  (`PlayerStageSlot0`, `1`, `2`), which no longer matched visual depth once slot 1 (`StaggerY =
  -45`, moved up/back) was staggered to sit "behind" slot 0 (`StaggerY = 0`). Document order still
  drew slot 1 after slot 0, so slot 1's ball rendered on top of slot 0's orb ring even though slot
  1 was meant to read as further away.
- **Decided:** Two-part fix in `BattleHUDController`:
  1. `ApplyStageCreatureStagger` now also calls the new `RestoreStageCreatureDepthOrder` — sorts
     player slots ascending by `StaggerY` and `BringToFront()`s them in that order, so document
     order always matches back-to-front visual depth at rest (furthest-back drawn first, frontmost
     drawn last/on top).
  2. **Correction from the user mid-verification:** the first pass only fixed the static/idle
     ordering. The user then clarified: "the selected character's skill wheel should always be in
     the front over everything so you can see it clearly" — i.e. depth order should NOT apply to
     whichever creature currently has its wheel open, even if that creature is in a "back" lane.
     `ShowMoveSelection`/`ShowMoveSelectionReadOnly` now call `BringToFront()` on the selected
     creature as they open its wheel, temporarily overriding the depth sort; `HideMoveSelection`
     calls `RestoreStageCreatureDepthOrder()` to undo the override once the wheel closes, so idle
     creatures go back to reading correctly by lane depth.
- **Why:** See "Found" above (static ordering) and the user's direct correction (interactive
  override) — both observed/stated live, not hypothetical.
- **Alternatives rejected:** Reordering only once at battle start and never touching it again for
  selection — rejected per the user's explicit correction that the active wheel must always be
  fully visible/clickable regardless of which lane it's in, not just "correctly occluded by depth."
- **Date:** 2026-08-06
- **Verified:** Live Play Mode, real 2-member party. Selected slot 0 (front lane, `StaggerY=0`):
  its wheel rendered fully in front of slot 1's ball — no clipping. Selected slot 1 (back lane,
  `StaggerY=-45`) instead: confirmed via screenshot that slot 1's wheel now renders ON TOP of slot
  0's ball (correctly overriding depth order while active), where before the fix it would have
  drawn behind it. Ran two full turn cycles with both party members acting each turn (via direct
  event invocation, since `Button`/pointer synthetic dispatch has the same test-simulation
  limitation noted in the prior entry): both creatures' `HasActedThisTurn` correctly flipped `true`
  after acting and reset to `false` at the start of each new `PlayerTurn`; re-clicking an
  already-acted creature showed the greyed read-only wheel (confirmed via
  `.move-option-disabled` class check); enemy HP dropped each cycle (two attacks landed per
  cycle); both party members took enemy damage between cycles. No errors, 133/133 EditMode tests
  still pass (pure z-order/ordering change, no formula logic touched).
- **Ref:** Directly follows the previous `[Combat]` entry (free-choice selection/stagger/End Turn
  button) — this closes the visual gap that entry's stagger introduced.

### [Combat] Stage creatures switched from flex layout to fixed absolute positions; background-click closes wheel
- **Found:** The previous entry's `BringToFront()`-based layering fix had a side effect the user
  caught next: "when i click on the other phasix during a play mode the phasix seem to move. or
  adjust spots." Root cause: `.stage-creature` was a normal flex child of `.stage-side-player`
  (`flex-direction: row`) — in UI Toolkit/Yoga, a flex child's DOCUMENT position IS its LAYOUT
  position, so `BringToFront()` (which reorders the element to be the last child) was also moving
  it to the rightmost slot in the row, not just changing paint order. Confirmed via
  `resolvedStyle.left`/`worldBound` reads before/after a click — the just-selected creature's `left`
  jumped to match whichever column BringToFront put it in.
- **Decided:**
  1. `.stage-creature` changed to `position: absolute; top: 0;` (removed `margin-left`/`right`);
     new `BattleHUDController.LayoutPlayerStageCreatures(visibleCount)` sets each visible slot's
     `left` explicitly (`i * 128 + 28`, reproducing the old flex spacing exactly) and sizes
     `PlayerStageArea` to match (needed because `.stage-side-player`'s `translate: -50% -50%`
     centering depends on its own box having a real size, which collapses once its children leave
     normal flow). Called once from `Initialize` — party composition doesn't change mid-battle
     (death fades a creature via `SetStageCreatureAliveState`, it doesn't hide the slot), so a
     single layout pass per battle is sufficient. With creatures no longer flex-flowed,
     `BringToFront()` (used by the prior entry's depth-order fix) now only affects paint order —
     the bug is structurally gone, not just patched around.
  2. Separately, the user also asked: "clicking outside of that should hide any open skill
     wheels." New `BattleHUDController.StageBackgroundClicked` event, registered on `_stage`
     (checks `evt.target == _stage`, so it only fires for genuinely empty background — move-orb
     clicks already `StopPropagation`, and `.stage` fills essentially the whole play area below
     the status header via `flex-grow: 1`). `BattleManager.PlayerTurn` treats it as an extra wake
     condition alongside End Turn / move confirmed / switch-creature: hides the wheel and resets
     to no-selection, without opening anything new. A background click that arrives while nothing
     is selected is explicitly discarded (not left pending) — otherwise it would sit as a stale
     flag and instantly close the very next wheel the player opens.
  3. **Explicitly deferred, not built now:** the user separately noted they'll eventually want to
     place Phasix in various stage positions for strategy ("later on... right now its not
     critical so we can just anchor the pair phasix where they are"). `LayoutPlayerStageCreatures`
     is written as the intended hook for that — it's already "explicit per-slot position," just
     currently always `column i`; a future formation/positioning feature would change what feeds
     that method, not its underlying mechanism. No formation UI/logic exists yet — scaffold only.
- **Why:** See "Found" above (layout regression) and the two direct user asks (background-click
  dismiss; deferred positioning) — all stated live, not hypothetical.
- **Alternatives rejected:** Reparenting the wheel's orbs/skill-slots into a separate shared
  top-level overlay container instead of repositioning the creature itself — rejected as a bigger
  refactor for the same result; keeping the creature in flex layout and finding some other way to
  fake z-order — rejected, UI Toolkit has no z-index independent of document order, so getting
  paint-order control without a layout side effect requires taking the element out of flow one way
  or another.
- **Date:** 2026-08-06
- **Verified:** Live Play Mode, real 2-member party. Recorded `worldBound` for both creatures
  before any click (Slot0 x=306,y=540; Slot1 x=434,y=495), clicked Slot1 then Slot0 in turn, and
  confirmed both `worldBound`s were byte-identical after each click — no movement. Screenshot
  confirmed Slot0's wheel still renders correctly on top of Slot1 (the depth-order fix from the
  previous entry still holds now that it's paint-order-only). Background-click: opened Slot0's
  wheel (confirmed `option.style.display == Flex`), invoked `StageBackgroundClicked` directly,
  confirmed the wheel closed (`display == None`). Stale-flag edge case: invoked
  `StageBackgroundClicked` with nothing selected, then immediately opened Slot1's wheel — confirmed
  it opened and STAYED open (didn't get instantly closed by the earlier no-op click), proving the
  `activeSlot < 0` branch's explicit reset works. 133/133 EditMode tests pass throughout (pure
  layout/event-wiring change, no formula logic touched).
- **Ref:** Directly follows and structurally supersedes the layering-fix mechanism in the previous
  `[Combat]` entry (BringToFront still used for paint order, now safe because of this entry's
  layout change).

### [Combat] Status header decoupled from stage position; compact row sizing; bottom panels halved
- **Found:** Playtesting with a real 3-member party (the current max, `BattleConfig.ActivePartySize
  = PartySystem.MaxPartySize = 3`), the user caught: "when i add more phasixs to my team and they
  get placed in the battle scene the enemy moves as well." Root cause: `.status-header` was a
  normal flex-column sibling of `.stage` inside `.battle-root` — each added `.status-row` grew
  `.status-header`'s own height, which shrank `.stage` (`flex-grow: 1`, same fixed-height parent),
  and `.stage-side`'s `top: 30%` anchor (used by BOTH player and enemy) resolved to a different
  absolute pixel position every time party size changed — moving the enemy even though nothing
  about the enemy itself had changed. Same conversation, the user also flagged the status text as
  "a little too big and taking up all the room... squished," anticipating a future larger party
  (Combat_Directive_v0_1_0.md: 3-5 members, exact size still pending — NOT the same number as the
  7-lane depth system, which is a separate front-to-back positioning mechanic, not party size),
  and asked to shrink the Battle Log/action panel to ~50% height for more open stage room.
- **Decided:**
  1. `.status-header` switched to `position: absolute; top/left/right: 0`, taking it out of
     `.battle-root`'s flex flow entirely — `.stage` (the only remaining flow child) now always
     fills the FULL battle-root height regardless of party size, so nothing about it depends on
     how many status rows are shown anymore.
  2. `.stage-side`'s `top` changed from `30%` to a fixed `480px` — a percentage was only ever
     "stable" by coincidence when `.stage`'s own height was itself stable; now that `.stage` is
     always full-height, a fixed px is the correct/simpler mechanism, not just a workaround. Value
     computed to clear a full 3-row header (~113px/row incl. the resize below, ×3 + header padding
     ≈ 371px) PLUS the group's own `translate: -50% -50%` centering (half its 72px height) PLUS
     the worst-case upward creature stagger (-45px, see the stagger-layering entries above) — see
     inline USS comment for the arithmetic.
  3. Compacted `.status-row` and its children throughout (`row-name` 30px→18px font,
     `hp-bar-back` 24px→16px, `aura-bar-back` 16px→12px, `burst-bar-back` 14px→10px, `.status-bar`
     26px→20px + `.status-icon` 20px→16px + its label/counter fonts 10px→8px, row padding
     10/14px→6/10px, `bar-value-label` 12px→9px) — same information, meaningfully less vertical
     space per row, so more rows (toward the eventual 3-5 range) fit without the header growing
     as fast per member added.
  4. `.action-panel`/`.battle-log-panel` height halved 300px→150px per the user's explicit "about
     50%" ask, with internal font sizes/padding scaled down to match (`turn-label` 28px→18px,
     `battle-log-title` 22px→15px, `battle-log-entry` 19px→13px) so text doesn't crowd the now-
     shorter box. `.end-turn-button`'s `bottom` recalculated (340px→190px) to sit just above the
     now-shorter bottom row.
- **Why:** See "Found" above — both the positional bug and the sizing ask were observed/stated
  live against a real 3-member party, not hypothetical.
- **Alternatives rejected:** Hardcoding `.status-header` to a fixed height (matching 3 rows) and
  leaving it in normal flex flow — rejected in favor of taking it out of flow entirely, since a
  hardcoded height would need to be manually kept in sync with `BattleConfig.ActivePartySize` and
  every row-dimension constant above it, two places to remember instead of the positioning
  mechanism just not depending on content size at all. Scaling stage-side's `top` down further to
  reclaim MORE of the freed vertical space — left as-is (480px) for now; not wrong to revisit once
  there's real creature art rather than placeholder balls.
- **Date:** 2026-08-06
- **Verified:** Live Play Mode. Baseline: started a battle with a 1-member party, recorded enemy
  `worldBound` = `(x:1553, y:480, w:72, h:72)`. Ended that battle, filled the party to 3 members
  (the current max), started a fresh battle — enemy `worldBound` came back byte-identical:
  `(x:1553, y:480, w:72, h:72)`. Screenshot confirmed all 3 status rows render fully legible with
  clear gap above the creature stage (no overlap), creatures sit well clear of both the header and
  the now-shorter bottom row, and the End Turn button no longer overlaps anything. 133/133
  EditMode tests pass (pure USS change, no C# logic touched this pass).
- **Ref:** Builds on the two `[Combat]` layering/positioning entries directly above — same
  underlying "party size shouldn't silently move things" theme, this time in the header/stage
  relationship rather than BringToFront.

### [Combat] Radial nameplate HUD replaces stacked HP/Aura/Burst bars; up to 7 slots per side
- **Found:** User asked directly: "is there another style for hud... to be re-arranged so its
  cleaner and more straight forward?" — an exploratory question, answered with a mockup (Artifact
  tool, not implemented yet) before any code changed, per this session's own "don't implement
  until the user agrees" convention. Iterated through several rounds of user feedback on the
  mockup alone:
  1. First proposal: name overlaid on the HP bar, buffs squeezed next to the name. User corrected:
     "we'll be putting a lot [of buffs] so keeping it underneath makes sense so you can just stack
     from left to right" — buffs need their own full-width row, not a cramped inline strip.
  2. User then proposed the actual shape used: "circular one... arches around the player
     portrait. So half circle for health over the top half, then bottom left half could be the
     aura and the bottom right half could be the evo gauge... limited to a certain max width, then
     after that first row is filled then the 2nd row starts... tied to the top of the screen."
  3. Follow-up corrections on the mockup itself: "put the name on the top side," "add some gaps
     between each of the rings" (none existed in the first radial draft), and "highlight the
     outside perimeter of that arc, its currently driving through the middle" (the first "ready"
     indicator was a floating outer arc, not a border around the segment's actual shape).
  4. Further correction: "there should be a gap between the 3 sections even when all are full"
     (round line caps were bulging past their angle and closing the gap at 100% fill) and "have
     the purple encased around the whole sections perimeter" (the ready highlight needed to be a
     CLOSED outline tracing outer arc + both straight side cuts + inner arc, not just a partial
     outer-edge line).
  5. Architecture ask, confirmed before implementing per the user's explicit "confirm before
     implementing": "make sure that there is a sort of invisible container that this huds sits
     in, so when you have to stack multiple of these huds together... you can just stack the full
     containers side by side." Verified understanding with a 4-player/3-enemy mockup scene before
     writing any game code.
  6. Final sizing ask: "i want to be able to stack 7 containers maximum on each side... adjust
     ratios as you required."
- **Decided:**
  1. New `RadialGaugeVisual` (custom Painter2D `VisualElement`, same pattern as the existing
     `RingVisual`/`DragLineVisual`) draws 3 arcs around a portrait circle: HP the top ~174°, Aura
     the bottom-left ~84°, Evo the bottom-right ~84°, each pair separated by a 6° gap baked into
     the TRACK bounds themselves (183-357, 93-177, 3-87 degrees) and drawn with `LineCap.Butt`
     (not `Round` — round caps were found to bulge past their angle and silently close the gap at
     100% fill, exactly the bug the user caught). `EvoReady` draws a closed gold outline
     (`MoveTo`/`Arc`/`LineTo`/`Arc`/`ClosePath`) tracing the Evo band's full annular-sector
     perimeter — outer arc, both straight radial cuts, inner arc — not a floating arc near it. All
     radii are fractions of the element's own `resolvedStyle` size, so resizing `.nameplate-gauge`
     in USS is the only thing needed to retune scale, no C# constant to keep in sync.
  2. New `BattleHUDController.NameplateRefs`/`BuildNameplate` — one "invisible container" (no
     background, no border, per the user's explicit architecture ask) built PROCEDURALLY in C#
     rather than hand-authored in UXML (7 slots x ~14 sub-elements x 2 sides would have meant an
     enormous, repetitive UXML block) — name on top, `RadialGaugeVisual` + portrait in a
     `.nameplate-ring-wrap`, 3 small color-coded HP/Aura/Evo stat labels, and a `flex-wrap`
     `.nameplate-buffs` row underneath holding Regen/Burst icon sockets — wrapping to a new row on
     its own once the row's width fills, exactly the "first row fills, then the 2nd row starts"
     behavior asked for.
  3. `MaxNameplateSlots = 7`, deliberately SEPARATE from `BattleConfig.ActivePartySize` (still 3,
     the real gameplay cap — Combat_Directive_v0_1_0.md's 3-5 range is still pending, and is a
     DIFFERENT number from the 7-lane DEPTH system, which is a front-to-back positioning mechanic,
     not party size — the two got conflated in conversation, worth remembering). This only
     governs how many nameplate SLOTS the sidebar can display; a real battle today still only
     ever fills the first 3 (or 1 enemy) — the rest just stay hidden.
  4. The stage creature system (balls, move wheels, drag-targeting, timing ring) was left
     completely untouched — the radial nameplate replaces only the status-header sidebar, a
     separate visual system from the interactive stage.
  5. Sizing shrunk hard from the first live-tested pass (64px ring, 12px name) to fit 7 per side
     without overlapping the Battle Log panel: 46px ring, 24px portrait, 10px name, 7px stats,
     12px buff icons, 2px container padding, 3px margin-bottom, `.status-header` padding 16px ->
     8px. `EvoStat`'s text/color now share one `SetEvoStatText` helper (gold "ready" matching the
     ring's own ready outline — the first live pass left the text purple even when ready, an
     inconsistency caught during the 7-slot cross-check, not by the user this time) so the two
     readouts never disagree.
- **Why:** See "Found" above — every shape/spacing/architecture decision was the user's own,
  arrived at through mockup iteration (Artifact tool) before any game code was written, per this
  session's established "confirm before implementing" pattern for exploratory/design questions.
- **Alternatives rejected:** Two earlier radial variants shown as mockup-only, never implemented —
  (B) a portrait-anchored card with the ring/bars beside a circular portrait rather than around
  it, (C) a "fused" single-block bar stack (HP/Aura/Burst sharing one border, no gaps) — user
  picked the arched-ring shape over both once it was drafted. Reparenting buff icons into a
  separate shared overlay container instead of a per-nameplate wrap row — unnecessary once each
  nameplate is already an isolated container with its own stable width to wrap within.
- **Date:** 2026-08-06
- **Verified:** Live Play Mode, `BattleHUDController.Instance.Initialize` called directly with 7
  synthesized player-side and 7 synthesized enemy-side `BattleParticipant`s (varied HP/Aura/Evo
  fill, two forced to a full/`ready` Evo gauge, several given active Regen/Burst status to
  populate the buff row, one slot given both icons at once to confirm they coexist) — bypassing
  `PartySystem`'s real 3-member cap, since this was a HUD-capacity check only, not a gameplay
  change. First pass (64px rings) overflowed the 7th slot into the Battle Log panel — screenshotted,
  cross-compared against the approved mockup reference, and re-sized per the user's "adjust ratios
  as you required" instruction; re-verified with a byte-for-byte repeat of the same 7v7 setup and
  confirmed all 14 nameplates (7 player + 7 enemy) render fully within the header, clear of both
  the stage creatures and the bottom row/End Turn button, with gaps holding at every fill level
  and the gold ready-outline/text agreeing on both full-gauge examples. 133/133 EditMode tests
  pass throughout (RadialGaugeVisual/nameplate wiring only — no formula logic touched).
- **Ref:** Supersedes the `.hp-bar-back`/`.aura-bar-back`/`.burst-bar-back`/`.status-bar`/
  `.status-icon` system from every prior `[Combat]` status-bar entry in this file — those classes
  are gone from BattleHUD.uss, replaced by `.nameplate-*`. `SetRegenStatus`/`SetBurstStatus`/
  `SetBurstFillBar`/`Initialize`/`RefreshBars`'s public signatures are unchanged, so
  `BattleManager.cs` needed zero edits despite the HUD-side rewrite.

### [Combat] Nameplates size dynamically by how many are actually shown, not fixed for worst-case 7
- **Found:** User asked directly, after seeing the 7-slot-verified sizing: "do you think the new
  hud is too small? Should we make it bigger?" The fixed 46px-ring size was calibrated for the
  WORST case (7 stacked), but real gameplay today only ever shows 3 (`PartySystem.MaxPartySize`)
  — meaning the common case was rendering unnecessarily tiny (7px stat text) while leaving most of
  the sidebar's vertical budget empty. Confirmed with a side-by-side mockup (fixed-small vs a
  bigger "sized for 3" version) before touching code; user confirmed: "it looks a lot more
  legible... let's do that."
- **Decided:** New `BattleHUDController.ApplyNameplateSize`, called per-nameplate from
  `Initialize` using that SIDE's own visible count (player and enemy size independently) — linearly
  interpolates ring/portrait/name-font/stat-font/buff-icon-size/padding/margin between two
  calibrated endpoints: `NameplateSizeMinCount` (3) or fewer uses the "Comfortable" size, all the
  way up to `MaxNameplateSlots` (7) using the already-verified "Compact" size (unchanged numbers
  from the previous entry). Applied as inline styles (`np.Container.style.*` etc.) since USS can't
  read a runtime count — the `.nameplate-*` classes still supply the base/fallback values.
  Deliberately capped Comfortable at 72px (not the 90px shown in the mockup) — 90px would grow
  `.status-header`'s height for a 3-member party PAST what `.stage-side`'s fixed `top: 480px`
  clearance was calibrated for (see the header-decoupling entry above), risking overlap with the
  stage creatures at exactly today's most common party size; 72px stays inside that already-
  verified-safe budget without needing to also re-touch stage-creature positioning this pass.
- **Why:** Direct response to the user's question, confirmed via mockup before implementing, per
  this session's "confirm before implementing" pattern.
- **Alternatives rejected:** Using the full 90px "comfortable" size shown in the mockup — would
  require re-deriving `.stage-side`'s clearance offset too (a second system to re-verify) for a
  further size bump; deferred rather than risk reopening an already-verified-safe interaction for
  marginal extra size. A scrollable sidebar with one fixed comfortable size throughout — user
  didn't ask for scrolling, and dynamic sizing means today's actual party (3) never needs it.
- **Date:** 2026-08-06
- **Verified:** Live Play Mode, real 3-member party (today's actual max) — screenshot confirmed
  visibly larger rings/name/stat text than the fixed-46px version, no overlap with the stage
  creatures below. 133/133 EditMode tests pass (pure sizing/interpolation change, no formula logic
  touched). Not re-verified at 7 this pass — `t=1` reproduces the exact same numeric values already
  live-verified in the previous entry, so no behavior change at that endpoint.
- **Ref:** Builds directly on the previous `[Combat]` radial nameplate entry — same
  `NameplateRefs`/`BuildNameplate` structures, this just makes their sizing a function of count
  instead of a single fixed constant.

### [Combat] Loss-on-defeat bond rule — GDD §21.6 vs §14.5 contradiction, resolved to zero bond loss
- **Found:** Two separately-Locked GDD sections directly disagree. §21.6 "Loss State": *"Losing a
  battle never reverses XP, stat growth, bond, or evolution state."* §14.5 "Loss Framework"
  (Locked v0.4.0): lists "fleeing a battle, losing a battle" as a trigger for Micro bond loss
  (−0.5–1%), and `NumericalCalibration.md` already mirrored that table verbatim before this was
  caught. Progression_Directive_v0_1_0.md's supersession list doesn't resolve it either — it
  only confirms "no XP on loss" / "no stat regression on loss," says nothing about bond.
- **Decided:** Zero bond loss on losing a single battle — §21.6 wins, user-confirmed when asked
  directly. Currency/item cost only (exact amount still pending §22 Economy design, per
  `NumericalCalibration.md`). This requires no code change — `BattleManager.EndBattle` already
  only raises `EventBus.Raise_BattleLost` and never touches `bondPercent`; this decision just
  confirms that's correct and should stay that way, rather than something a future combat-loss
  handler should "fix" by wiring in §14.5's Micro loss.
- **Why:** User's explicit call when presented with the conflict — not resolved by any existing
  doc precedence rule (both sections are equally Locked, `DOCUMENT_INDEX.md` gives no tie-break
  for two Locked sections disagreeing with each other).
- **Note:** GDD §14.5's Loss Framework table's "fleeing a battle, losing a battle -> Micro" row is
  now superseded ON THIS SPECIFIC POINT by this decision — the rest of §14.5 (session cap, damping
  above 60%/80% bond, other Micro/Minor/Significant triggers) is unaffected and still governs.
  Full-party-wipe Blackout is a separate, harsher, and uncontested mechanic (WorldDesign_Directive
  Part 7) — it DOES cost unbanked Aura/loot/currency, just not bond; not affected by this decision.
- **Date:** 2026-08-05
- **Revisit if:** A future design pass resolves this contradiction in the GDD source itself —
  reconcile then rather than assuming this decision is permanent just because it's logged here.
- **Ref:** GDD §21.6, §14.5 (conflicting); WorldDesign_Directive_v0_1_0.md Part 7 (Blackout, unaffected)

### [Progression/Combat] Step 5 scaffolding — Aura stat allocation, capture, evolution burst, audio/VFX hooks
- **Decided:** Built `Assets/Scripts/Creatures/AuraTierCeiling.cs`/`AuraStatAllocationSystem.cs`
  (Common Aura -> free stat allocation, gated by a tier+Aptitude ceiling — Progression_Directive's
  "Free Allocation Model"/"Tier Stat Ceiling"), `ResonanceBonusEvaluator.cs` (Resonance Bonus
  alignment check), `CaptureSystem.cs` (capture chance + attempt), and
  `Assets/Scripts/Combat/EvolutionBurstGauge.cs`/`EvolutionBurstSystem.cs` (Type K mid-battle
  burst state machine, GDD §9.3), plus `Assets/Scripts/Audio/BattleAudioVfxHooks.cs` (empty
  EventBus subscriber stubs for future audio/VFX). Unlike Step 4, almost none of this system's
  actual numbers/mappings are locked anywhere — see NumericalCalibration.md's new "Step 5"
  section for the full placeholder list. Two interpretation calls worth flagging specifically:
  - **ResonanceBonusEvaluator uses Temper's growth-priority ranking as a stand-in for the
    Directive's "emotional type" alignment concept**, since no emotional-type -> stat mapping is
    locked or even loosely designed anywhere (emotionalType is an open per-species string, not
    an enumerated taxonomy). Temper already represents "which stats this individual naturally
    grows toward," making it the closest existing locked concept — but it is NOT literally what
    the Directive describes. Flagged for revisit once emotional-type stat alignment is designed.
  - **CaptureSystem has zero locked formula to build against** — not even the mechanic's shape
    is designed beyond "every enemy is capturable" and "capture difficulty should vary" (both
    unelaborated). Built the standard genre-convention shape (lower target HP -> higher chance,
    never guaranteed) as an explicitly temporary placeholder, deliberately omitting any capture
    item parameter since no item type has been designed (§22 Economy is fully pending) — don't
    treat this as a foundation to build real capture balance on without a design pass first.
  - **EvolutionBurstSystem does NOT implement what changes about the creature during a burst** —
    the GDD's 4-bullet §9.3 section never says (stat boost? higher-tier stat block? new moves?).
    Only the gauge fill/trigger/expiry state machine is built; "ApplyBurstEffects" is an explicit
    open hook, not invented.
  - **BattleAudioVfxHooks has literally nothing to hook to** — GDD §27 (Audio) is entirely
    tagged Pending, "VFX" doesn't appear anywhere in any doc. Subscribed to the relevant existing
    EventBus events with empty bodies + TODO comments (not even placeholder `Debug.Log` calls,
    which would just be console noise during real play) rather than inventing sound/VFX content.
- **Why:** Continue the approved Phase 3 plan's Step 5 scope in the same rules-layer-first,
  EditMode-tested style as Step 4, while being honest that this system has almost no locked
  content to anchor placeholders against — flagged explicitly rather than presenting invented
  formulas as more authoritative than they are.
- **Alternatives rejected:** Skipping CaptureSystem/EvolutionBurstSystem entirely until their
  numbers are designed — rejected since the plan explicitly asks for this scaffolding and the
  mechanic SHAPES (not the numbers) are locked enough to scaffold responsibly, matching how
  `DamageCalculator`/`TimedInputConfig` were built before Step 3's real numbers existed.
- **Date:** 2026-08-05
- **Revisit if:** §22 Economy design session happens (capture), emotional-type stat alignment is
  designed (Resonance Bonus), Type K skill content is designed (what a burst actually does), or
  GDD §27 Audio Design work starts (VFX/audio hooks).
- **Ref:** Progression_Directive_v0_1_0.md ("Free Allocation Model," "Resonance Bonus Layer,"
  "Tier Stat Ceiling"), GDD §9.3 (evolution burst), §18.5/§22 (capture, pending), §27 (audio, pending)

### [Combat] Defense model — full-avoidance Dodge/Parry, supersedes Combat_Directive Part 4's damage-reduction model
- **Decided:** The defensive action command is no longer "successfully timed press reduces
  incoming damage" (a multiplier). It's now two distinct full-avoidance options, inspired by
  Clair Obscur: Expedition 33, chosen by the target each time the enemy attacks
  (`BattleHUDController.ChooseDefense`):
  - **Dodge** — wide/easy timing window (`TimedInputConfig.DodgeBaseWindowPercent`, 20% base),
    same 1.2s sweep as offense. Success fully avoids the hit (0× damage). No follow-up.
  - **Parry** — narrow/hard timing window (`ParryBaseWindowPercent`, 6% base), faster 0.7s sweep
    (`ParryMarkerSweepDuration`). Success fully avoids the hit AND triggers an automatic
    counter-attack against the attacker (`BattleManager.EnemyTurn`, no timing check of its own —
    a flat bonus for landing the harder input, not another QTE).
  - Both fail identically to a missed offensive input: the hit lands at full damage (1×), no
    extra penalty for having attempted the harder Parry and missing — matches
    Combat_Directive's existing "reward, don't punish" intent, just carried over to the new model.
  - Mechanically, avoidance is represented as `damageMultiplier: 0f` fed into the same
    `BattleEngine.QueueBasicAttack`/`ResolveQueuedActions` path every other attack uses —
    `BattleEngine` itself has no dodge/parry-specific logic, it just applies whatever multiplier
    it's given.
- **Why:** User explicitly asked to mimic Expedition 33's combat feel after playing the
  earlier flat damage-reduction defense in live testing, then confirmed via AskUserQuestion that
  they wanted (a) full avoidance rather than a reduction multiplier, and (b) two distinct options
  rather than one — specifically Dodge (safe) vs. Parry (risky, rewards a counter).
- **Forward-looking note (user-flagged):** some future attacks/skills may need multiple
  action-command beats in one attack — a multi-hit offensive skill, or a defensive sequence with
  more than one Dodge/Parry check. `BattleHUDController.RunTimedInput` was kept as a single-window
  primitive specifically so a future multi-beat attack could call it multiple times in sequence
  rather than needing a rewrite; no multi-input system exists yet (skill tree framework, Step 4,
  is still scaffold-only).
- **Alternatives rejected:** Keeping the flat damage-reduction defense (user found it didn't
  match the desired feel). A single unified "block" option instead of two distinct
  Dodge/Parry choices (user explicitly asked for two distinct options, not one).
- **Date:** 2026-08-05
- **Revisit if:** Playtesting shows the Parry window is too punishing/too generous relative to
  Dodge once real numerical calibration happens (all window/sweep/multiplier values here are
  still placeholders — see NumericalCalibration.md).
- **Ref:** Combat_Directive_v0_1_0.md Part 4 (superseded section, not the whole document)
- **Update (2026-08-05, same day):** Replaced the choose-then-time two-step flow above
  (`ChooseDefense` button prompt, then a separate `RunTimedInput` pass in whichever mode was
  picked) with a single live click, per user request for it to "feel more live." The mechanic
  itself (full avoidance, Parry = auto-counter, failure = full damage) is unchanged — only the
  INPUT method changed:
  - `BattleHUDController.RunDefenseTimedInput` now shows ONE bar with BOTH the Dodge zone (wide)
    and, positioned as a nested sub-range inside it, the Parry zone (narrow) drawn simultaneously
    — no pre-choice menu.
  - Left-clicking ANYWHERE on screen while the marker is in the Dodge zone succeeds as a Dodge;
    right-clicking ANYWHERE while the marker is in the (narrower, nested) Parry zone succeeds as
    a Parry. Detected via a `PointerDownEvent` callback registered on the HUD's root element
    (covers the full screen), not a button the player has to aim at.
  - Because both zones now share one marker sweep, Parry's difficulty is expressed entirely by
    its narrower nested WIDTH, not a faster sweep speed — `TimedInputConfig.ParryMarkerSweepDuration`
    (0.7s) was removed as dead once there was no separate Parry-only pass left to use it.
  - `BattleHUDController.DefenseChoice` (Dodge/Parry pre-pick enum) and `ChooseDefense` were
    removed; `TimedInputMode.Dodge`/`TimedInputMode.Parry` were removed from `RunTimedInput`
    (offense is now the only mode that method runs, since defense has its own dedicated method).
  - Nesting math: `parryZoneStart = Random.Range(dodgeZoneStart, dodgeZoneStart + dodgeWindowPercent
    - parryWindowPercent)`, requiring `parryWindowPercent &lt;= dodgeWindowPercent` — verified to
    always hold for the current `TimedInputConfig` constants (14-point base gap, same Instinct/bond
    scaling applied to both, same 60% ceiling) up to and including the degenerate case where both
    saturate at the ceiling together (zones become equal-width, not an error).
  - **Why:** User wanted the choice+timing collapsed into a single reactive input rather than a
    menu pick followed by a separate QTE — "click anywhere" removes the need to aim at a small
    button too.
  - **Verified:** Live Play Mode — real battle flow (timeout correctly resolves to Miss, full
    damage, no counter), isolated proofs for both click types (a synthetic left-click
    `PointerDownEvent` sent to the HUD root resolved `Dodge`; a synthetic right-click resolved
    `Parry` — same "guaranteed-width window" isolation technique used earlier this session for
    `RunTimedInput`, since precisely timing a live click via MCP round-trip latency is
    unreliable), and a screenshot confirming both zones render correctly nested on one bar.

### [Combat] ComboEngine/StatusEffectCatalog/ChainResultCatalog/MasteryBonusCatalog wired into live play — overrides the prior "wait for real skill content" stance
- **Decided:** User explicitly overrode this file's own prior decision (the "Skill tree framework"
  entry above and the "Phase 3 Gate prep" entry's "left alone, on purpose" note) and had all four
  rules-layer systems wired into live battle via a placeholder skill-selection UI, accepting it
  will need rework once real skill content is designed. This is a direct, explicit reversal —
  future sessions should treat THIS entry as current, not the earlier "wait" reasoning, which
  remains in place above only as a historical record of why the wait was originally chosen.
  - **The core problem solved:** the 36 placeholder `SkillData` assets (`Aspect_Placeholder1`,
    etc.) needed to become clickable and mechanically resolvable without inventing per-skill
    balance content (`SkillData`'s own doc comment: "Do not flesh out skill content here"). New
    `PlaceholderSkillResolver` derives every skill's damage-category-or-status behavior
    ALGORITHMICALLY from tables that are already GDD-locked (`SkillTreeCatalog.PrimaryAttribute`,
    the damage formula's own Force/Guard↔Physical, Resonance/Ward↔Elemental split,
    `StatusEffectCatalog.Category`) — a fixed priority-chain classification, not a hand-picked
    per-skill assignment. New `SkillData.PlaceholderIndex` (structural, 0 or 1 — "which of this
    tree's 2 placeholders") selects a specific status within a resolved category via fixed
    enum-declaration order, the same tie-break style `ChainResultCatalog` already uses for its own
    ambiguous-match case. Both placeholders of a damage tree resolve identically — differentiating
    them would itself be invented content.
  - **New `SkillDatabase`** (`Assets/Data/Skills/SkillDatabase.asset`) resolves
    `PhasixRuntimeData.equippedSkillGuids`/`learnedSkillGuids` (GUID strings) back to real
    `SkillData` at runtime, and `SkillTreeType` → its 2 placeholder skills for bootstrap-seeding.
    GUIDs are captured once via an Editor-only "Rebuild GUID Index" context menu — never touches
    `AssetDatabase` at runtime.
  - **`BattleHUDController`'s previously-decorative 12-slot skill ring is now half-live:** the 5
    built-in moves (A/C/H/R/K) still occupy clock-hours {1,11,12,2,3}; the remaining 7 hours
    (4-10) are real equipped-skill slots (`PopulateSkillRing`), drag-and-drop exactly like the
    built-in moves, using `PlaceholderSkillResolver.Resolve(skill).SelfTargeted` for the same
    self-vs-enemy hit-test built-ins already used. Empty/locked slots get a new
    `.skill-slot-locked` USS class and no click handler.
  - **Bootstrap seeding (`WildSpawnSystem.SeedInitialSkills`, shared by `EncounterTrigger` and
    `DebugPartyBootstrap`):** auto-learns and auto-equips a species' `AvailableTreeTypes` (up to
    `SkillSlotCapacity`'s locked per-tier caps) — explicitly a placeholder standing in for a real
    skill-learning UI/flow that doesn't exist yet, not a balance decision. Guards against a
    species with an unset/invalid `EvolutionTier` (found live: `Test_FireType`/`Test_SteamType`
    both had `EvolutionTier` defaulted to 0 from before this session, which crashed
    `SkillSlotCapacity.GetTreeCount` — fixed both the test data, since 0 was never actually a
    real tier, AND added a defensive tier-range guard in `SeedInitialSkills` so a similar data gap
    in future placeholder species can't crash a spawn). `Test_FireType.AvailableTreeTypes` set to
    `[Mirror, Reaction]` specifically so this session's new combo-rule defaults (below) are
    actually reachable in Play Mode.
- **New, user-directed mechanic — pluggable combo-rule framework (NOT a GDD transcription, unlike
  everything else in this entry):** the base cross-tree combo (`ComboEngine.DetectCombo`, GDD
  §4.2-locked) is unchanged and always active. User asked for the mechanic itself to be
  extensible — specific skill trees/passives should be able to grant creatures an ALTERNATE combo
  rule, not force every creature to share the one fixed rule. New `ComboRuleType` enum
  (`CrossTreeSequence`/`RepeatSameSkill`/`TimedInputStreak`) plus a new structural (designer-
  assigned, not derived) `SkillData.GrantsComboRule` field — a skill can opt to grant its owner an
  alternate rule while equipped. New `ComboRuleEvaluator.EvaluateRepeatSameSkill` (trailing-window
  same-skill streak — the literal inverse of the base rule) and `EvaluateTimedInputStreak`
  (trailing-window landed-timed-input streak). `BattleParticipant.ActiveComboRules` is computed
  once at battle start from equipped skills. Pre-wired defaults, chosen because they match their
  trees' own locked GDD role text (not arbitrary): `Mirror_Placeholder1` → `RepeatSameSkill`
  (Mirror's role: "Repetition and reverberation effects"), `Reaction_Placeholder1` →
  `TimedInputStreak` (Reaction's role: "Triggered responses, counter-attacks, parries"). All
  three rules are detection + log only — no numeric bonus for any of them, since neither the GDD
  (base rule) nor this new design (the two new rules) defines what a combo mechanically DOES yet.
- **Chain Results / Mastery Bonuses — detect + log only this pass, explicit scope decision:** both
  catalogs' locked GDD flavor text now appears in the battle log on trigger (Chain: only on a
  *change*, via `BattleParticipant.ActiveChainResult`; Mastery: once per bonus per battle, via
  `TriggeredMasteryBonusesThisBattle`, since `MasteryBonusCatalog` itself defers that bookkeeping
  to the caller). Their full numeric effects (e.g. Rend's "+45% physical damage," Scorch bypassing
  Ward) are NOT applied — would require a new modifier threaded through `DamageCalculator`, a
  materially larger, separately-scoped follow-up. User confirmed this scope explicitly when asked.
- **Active-status tracking:** new `BattleParticipant.ActiveStatuses`/`ApplyStatus`/`TickStatuses`
  (overwrite-not-stack on re-apply, mirroring the existing `ApplyRegen` precedent), ticked for
  both sides once per full round in `BattleManager.RunBattleLoop`. Durations come from
  `StatusDurationCalculator.ComputeDuration` fed by `StatusEffectCatalog`'s own already-
  placeholder floor — no new numbers invented for this.
- **Post-battle Aura Stat Allocation screen (new, separate from the above):** new
  `AuraAllocationController` (UI Toolkit, `Assets/UI/AuraAllocation.uxml/.uss`,
  `AuraAllocationPanelSettings.asset` duplicated from `BattleHUDPanelSettings.asset`), shown from
  `BattleManager.EndBattle` (converted to a coroutine so it can block on this screen) on a Won
  outcome only, before the battle scene unloads — one card per living party member, "+1" per stat
  spending `commonAura` via the already-built `AuraStatAllocationSystem`/`AuraTierCeiling`. No
  new mechanic — this system existed and was tested already; this session just gave it the
  post-battle screen `DECISIONS.md`'s own prior notes said it needed. `UIDocument.sortingOrder`
  set to 10 (above `BattleHUD`'s 0) so it renders on top during the brief window both are visible.
- **Why:** User's explicit choice, made mid-planning after being shown that this overrides the
  project's own twice-recorded "wait for real skill content" decision — see the conversation this
  session, not re-litigated here. The generic-derivation approach for skill mechanics and the
  "assign per-asset, not per-tree-inferred" combo-rule flag are how the override avoids becoming
  actual content invention (see PlaceholderSkillResolver's own doc comment for the full
  reasoning).
- **Alternatives rejected:** Giving every skill unique hand-picked damage/status/power values —
  rejected outright, that IS the content-invention CLAUDE.md forbids. Inferring which trees grant
  which alternate combo rule from their locked role text automatically (rather than a manual
  per-asset flag) — rejected because the user explicitly wants trees/passives themselves to
  control this, a designer choice, not an algorithm's inference (Mirror/Reaction were chosen as
  DEFAULTS because their role text fits, not because the system derives them that way).
- **Date:** 2026-08-07
- **Verified:** Live Play Mode (see also `AUDIT_202608.md`-style verification pattern this project
  already uses) — real battle via `BattleTransition.StartWildBattle`, skill ring showed exactly 2
  live slots (both Mirror) + 5 locked for the seeded test companion; a Mirror skill drag-equivalent
  resolved real damage with correct type-effectiveness text; using it twice in a row logged a
  "Duo combo — repeating the same skill" line; manually applying Bleed+Weaken to the enemy logged
  "combine into Rend!" with the catalog's exact locked effect text, confirmed NOT to re-log on a
  second identical check; applying a 3rd DoT (Burn+Wither) logged "achieves Hemorrhage!" with its
  exact locked text, confirmed to log only once even when checked twice; ending the battle Won
  showed the Aura Allocation screen with the correct party member/stats/Aura total; Continue
  correctly hid the HUD, restored the overworld camera, and unloaded `BattleScene_Main` cleanly.
  206/206 EditMode tests pass (up from 133 at session start).
- **Revisit if:** Real skill content/species roster design happens (Phase 5) — at that point
  `PlaceholderSkillResolver`'s derivation and the 36 placeholder assets' structural fields are
  meant to be replaced, not extended. Also revisit Chain/Mastery's detect-only scope once there's
  appetite for the larger `DamageCalculator` modifier work.

### [Combat/UI] Skill-wheel combo-streak counter; post-battle screen reworked into a read-only summary; Aura spending moved to a new Tab-key overworld menu
- **Decided (combo counter):** After exploring "add it to the buff/debuff bar" and "a standalone
  nameplate badge," user settled on the original instinct: a small counter badge shown directly
  on the skill wheel — on the SKILL ITSELF for rules keyed to skill identity (`CrossTreeSequence`/
  `RepeatSameSkill`, badge the just-used skill's ring slot), or on the PASSIVE that grants the
  rule for rules not tied to any one skill (`TimedInputStreak`, which cares about landing timed
  inputs regardless of which skill — badges the granting skill, e.g. `Reaction_Placeholder1`).
  New `ComboEngine.GetDistinctTrailingStreakLength`/`ComboRuleEvaluator.
  GetRepeatTrailingStreakLength`/`GetTimedInputTrailingStreakLength` — raw current streak length
  (1-4, not the capped Duo/Trio/Quad tier `DetectCombo`/`EvaluateX` return), since a live counter
  needs "how long is the streak right now," not just "was a tier just crossed." New
  `BattleHUDController.SetSkillComboCounter`/`ClearAllSkillComboCounters` (dumb display only —
  no combo logic in that class) and `.skill-combo-badge` USS (bottom-right corner subscript,
  same convention as the nameplate buff counter). `BattleManager.RefreshComboCounterBadges`
  (called after every `ResolveSkillAction`) clears all of that creature's badges then re-shows
  whichever are currently >= 2 — simplest way to guarantee a broken streak's stale badge never
  lingers on the wrong skill. Confirmed explicitly per-creature already, by design — each
  `BattleParticipant` tracks its own independent history, never shared across the party.
- **Decided (post-battle screen + Aura spending):** The post-battle screen stops being where
  Aura is SPENT — user: "I think I don't want that to happen after a battle. It should just be
  an after menu for aura gained, damage done, healed... but not where we spend it. Spending
  should be part of some menu." `AuraAllocationController` deleted (along with `AuraAllocation.
  uxml/.uss`) and replaced by `BattleSummaryController` (`Assets/UI/BattleSummary.uxml/.uss`) —
  a small read-only panel: Aura Gained / Damage Dealt / Healing Done, Continue only. New
  `BattleSummary` plain-data class built by `BattleManager.EndBattle`. This surfaced that NO
  Aura-drop-on-win mechanic existed at all — `EventBus.OnAuraDropped` was a stub event with no
  producer, and nothing tracked damage/healing totals during a battle. Both built fresh this
  pass: `BattleConfig.AuraRewardOnWin` (flat 15, first real implementation of Progression_
  Directive's "Common Aura drops from all Phasix in battle" — placeholder number, per this
  project's usual pattern), granted to every surviving party member on `EndBattle(Won)`; new
  `BattleManager._totalDamageDealt`/`_totalHealingDone` running totals, accumulated at every
  player-side `ResolveQueuedActions` call site (basic Attack, skill damage, Parry counter — NOT
  ordinary enemy attacks against the player) and every healing call site (`Heal`, `TickRegen`).
- **Decided (Aura spending's new home):** New `PartyMenuController` (`Assets/UI/PartyMenu.uxml/
  .uss`), living in the OVERWORLD scene (`SampleScene`, not `BattleScene_Main`) — toggled by Tab
  (see the `~`-rebind entry above; Tab is no longer just "reserved," it's now wired to this).
  Reuses the exact per-creature "+1" card UI ported from the deleted `AuraAllocationController`,
  adapted to read `PhasixRuntimeData` directly (no `BattleParticipant` wrapper exists outside a
  battle) and to show every party slot (no alive/dead concept in the overworld). Deliberately
  built as a single-purpose screen, not a multi-tab container — CLAUDE.md: don't build
  abstractions beyond what's needed today. User's own framing: "for now put it into the tab
  menu but maybe in future we'll have a dedicated 'shop' or NPC that allows us to spend" — that
  future system is a DIFFERENT screen/flow to design later, not something to scaffold for now.
- **Why:** All three changes are direct user direction, arrived at through a few rounds of
  back-and-forth in conversation (the combo-indicator placement specifically reversed twice
  before landing back on "on the skill wheel") — not independently inferred.
- **Alternatives rejected:** Merging the combo counter into the nameplate buff/debuff row —
  rejected (twice, after initially recommending it) because a combo is a live streak on the
  ATTACKER with no duration, not a status applied TO a target with a countdown; mixing them
  risked implying the combo does something to stats when (per the entry above) it doesn't yet.
  A standalone nameplate/stage-creature badge — rejected in favor of attaching the counter to
  the mechanic that's actually producing it (the skill or its granting passive).
- **Date:** 2026-08-07 (same session as the entry above)
- **Verified:** Unity MCP reconnected same session — full Editor-side wiring completed
  (`UIRoot_AuraAllocation` renamed to `UIRoot_BattleSummary`, `BattleSummaryController` attached,
  `UIDocument` re-pointed at `BattleSummary.uxml`; new `UIRoot_PartyMenu` GameObject created in
  `SampleScene` with `PartyMenuController` + a new `PartyMenuPanelSettings.asset`), then live
  Play Mode: fired the same skill twice via `ResolveSkillAction` and confirmed the icon-badge
  showed "2" on exactly the used skill's ring slot (the other equipped-but-unused skill stayed
  unbadged); won a battle and confirmed the summary screen's totals (Aura Gained 15, Damage
  Dealt 6, Healing Done 0) matched the actual actions taken; opened the Tab party menu and
  confirmed it rendered the real party member's stats/Aura with working "+1" affordances.
  214/214 EditMode tests pass throughout (up from 206 — 8 new streak-length tests). One test
  assertion bug caught and fixed in this pass (not a product bug): `GetDistinctTrailingStreakLength`
  correctly returns 3, not 2, for a repeat-then-distinct sequence — the original test's expected
  value was wrong, verified by hand-tracing the algorithm against `DetectCombo`'s own windowed
  logic. Also hit one confirmed non-issue: a `NullReferenceException` in `PartyMenuController.
  Open` on the first post-reconnect Play session, caused by a mid-session domain reload racing
  `UIDocument`'s panel construction — did not recur on a fresh Play Mode entry, not a real bug.
- **Follow-up sizing pass (same session, after initial verification):** User found the first-pass
  text sizing hard to read across all three new UI pieces. Standardized on `.battle-log-entry`'s
  13px as the body-text baseline for menu/summary screens (titles ~16px, matching `.battle-log-
  title`'s 15px convention) — applied to `BattleSummary.uss` and `PartyMenu.uss`, with both
  panels/cards grown to fit (summary panel 140px->220px, party cards 96px->168px, stat buttons
  18x12px->32x22px). The skill-wheel combo badge was also redesigned from a bare floating number
  to a proper small solid-circle icon (16px diameter, 11px font) at the skill slot's corner —
  same ICON-shaped visual language as `.nameplate-buff-icon`, sized up from the nameplate's 12px
  version since the skill slot itself (32px) is much bigger. Re-verified live after each change.
- **Revisit if:** The future shop/NPC Aura-spend flow gets designed — `PartyMenuController`'s
  card UI is the reusable piece, but where/how the player accesses spending will change.

### [Combat/UI] C1/C2 skill naming; RepeatSameSkill tied to C1 specifically; TimedInputStreak requires PERFECT hits; richer skill tooltips; badge pushed to a wider radius
- **Decided (naming + mechanics refinement):** User renamed the two combo-granting placeholder
  skills to `C1` (`Mirror_Placeholder1.SkillName`, grants `RepeatSameSkill`) and `C2`
  (`Reaction_Placeholder1.SkillName`, grants `TimedInputStreak`), then clarified both rules'
  actual intended behavior, which changed from the original session's implementation:
  - **`RepeatSameSkill` now only counts repeats of the SPECIFIC granting skill (C1), not "any
    skill repeated."** User: "the repeatsameskill only works on the C1." Previously, repeating
    ANY equipped skill N times in a row satisfied the rule; now `ComboRuleEvaluator.
    EvaluateRepeatSameSkill`/`GetRepeatTrailingStreakLength` take an explicit `grantingSkill`
    parameter and check the trailing window against THAT skill specifically — repeating a
    different equipped skill (even the creature's other Mirror skill) no longer counts.
    `BattleManager.RefreshComboCounterBadges` now badges the granting skill's own slot (not
    `justUsedSkill`, which could differ) for this rule too, matching `TimedInputStreak`'s
    already-existing "badge the passive" pattern.
  - **`TimedInputStreak` now requires PERFECT timed inputs specifically, not merely successful
    ones.** User: "C2 is a passive so it can work with any other attacking skill that gets
    perfect, after a miss it rests." `BattleParticipant.RecentTimedInputSuccesses`/
    `RecordTimedInputResult` renamed to `RecentTimedInputPerfects`/`RecordTimedInputPerfect`;
    `BattleManager.ResolveSkillAction` now records `BattleHUDController.LastTimedInputWasPerfect`
    instead of `LastTimedInputSuccess`. A regular (non-perfect) success no longer extends the
    streak, same as an outright miss — both "rest" it. Still not tied to any one skill's
    identity — any equipped attacking skill's timed input counts while C2 is equipped, per "any
    other attacking skill."
- **Decided (tooltip):** `BattleHUDController.PopulateSkillRing` now sets each populated skill
  slot's native `VisualElement.tooltip` to `"{SkillName}\n{Description}\nAura Cost: {cost}"`
  instead of just the bare name — user: "have a hover over the skill orb and showing a
  description, aura, costs would be good too." Uses UI Toolkit's built-in OS-rendered tooltip,
  not a custom hover panel — flagged as the simple version; a fancier custom popup is a
  separate, bigger UI feature if wanted later.
- **Decided (badge radius):** `.skill-combo-badge`'s offset pushed from `right/bottom: -4px` to
  `-14px` — user: "move the counter further away so its on a further radius away from the skill
  orb." The first icon-badge pass (see the entry above) still visually touched the orb's edge;
  this reads as a clearly separate marker instead.
- **Why:** All direct user follow-up after live-testing the first pass of this session's combo
  work — the original generic "any repeat"/"any success" rules weren't what was actually wanted
  once seen in practice.
- **Also investigated this session:** User reported the Evolution Burst gauge wasn't clickable,
  with `"Access version should be odd when acquiring lock"` spamming the console. Traced this to
  a Unity Editor-level issue, not application code — the assertion has no file/line/stack trace
  (native engine layer, not a C# script), and it persisted even through a clean stop -> idle ->
  recompile cycle with no Play Mode involved, ruling out a Play-Mode-transition-specific cause.
  Most likely explanation: this Editor process degraded from the very large number of forced
  recompiles/domain reloads run across this extended session. No MCP tool exists to restart the
  Unity Editor process itself — recommended the user do so manually (confirmed safe: both scenes
  were clean/non-dirty at the time, all work already saved to disk). `HandleBurstBarClicked`/
  `BurstBarClicked` wiring itself was not touched by any change in this session.
- **Date:** 2026-08-07 (same session as the two entries above)
- **Verified:** Live Play Mode — `C1`/`C2` names confirmed via `SkillData.SkillName`; repeating
  the OTHER equipped Mirror skill twice correctly produced `GetRepeatTrailingStreakLength` of 0
  and no combo log line, while repeating C1 twice correctly logged "Duo combo" and badged C1's
  slot with "2"; tooltip confirmed to read `"C1\n{description}\nAura Cost: 3"`; badge visually
  confirmed at the wider radius via screenshot. 218/218 EditMode tests pass (up from 214 — 4 new
  tests covering the "different skill doesn't count"/"null granting skill" edge cases).
- **Revisit if:** TimedInputStreak's "perfect" requirement should be surfaced better in the UI
  (currently only the ring-flash color during the actual timed input indicates perfect vs.
  regular success — the combo badge is the only after-the-fact signal). If the console assertion
  recurs after a real Editor restart, that would rule out the "degraded long session" theory and
  warrant a fresh investigation.

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

### [Creatures] Evolution_System_Directive_v1_1_0.md internal inconsistencies — RESOLVED (.md mirror)
- **Found (July 2026):** `EvolutionNodeSO`, `EvolutionBranchSO`, `EvolutionGraphSO`, and
  `EvolutionEvaluator`/`EvolutionExecutor` as specified in the Directive have real
  conflicts between declared field names (in the class definitions) and actually-used
  field names (in the logic that references them) — e.g. `EvolutionNodeSO.formID`
  (declared) vs. `node.nodeGuid` (used); `ConditionalType` has 6 declared members but 7
  different members are switched on elsewhere; `BranchConditional` is used as a type but
  never defined (only the similarly-shaped `ConditionalRequirement` is).
- **Fixed (2026-08-10):** Read the full 2005-line `.md` mirror end to end and resolved every
  declared-vs-used mismatch directly in that file (each fix marked inline with a "Consistency fix
  (2026-08-10)" note and a "PDF SYNC REQUIRED" flag, same pattern already used for the Active
  Slots table):
  - `EvolutionNodeSO.formID` → `nodeGuid`; added missing `speciesData`/`tierStatFloor`/
    `uiPosition` fields that were used everywhere but never declared.
  - `EvolutionBranchSO.targetNode`/`requiredItem` (object refs) → `targetNodeGuid`/
    `requiredItemGuid` (GUID strings), matching the project's established
    GUID-string-not-object-reference convention (see the `PhasixRuntimeData` entry above); added
    missing `commonAuraCost`/`specificAuraGates`/`rareVariantAuraCost` fields.
  - `EvolutionGraphSO.GetBranchesFrom(nodeGuid)` added — derives from
    `EvolutionNodeSO.forwardBranches` rather than a second, redundant flat `AllBranches` list +
    `sourceNodeGuid` field (avoids two sources of truth for graph structure); rewrote the two call
    sites (`EvolutionWebController`, `EvolutionGraphValidator`) that assumed the flat list.
  - `ConditionalType`/`EvaluateConditional` rewritten to match §4's own LOCKED 6-member table
    (`BossDefeated, ItemInPossession, CreatureCaptured, SkillTreeUnlocked, RealmReached,
    OriginType`) instead of the unrelated 7-member set the old switch body actually handled;
    `BranchConditional` → `ConditionalRequirement` (the type §9 actually declares); renamed
    `RegionReached` → `RealmReached` to match "Realm" terminology used everywhere else in the
    project (WorldDesign_Directive, `IPlayerProgressData.HasVisitedRealm`).
  - Fixed a duplicate `§15` heading (`Editor Tooling` and `Build Order` both had it) →
    `Build Order` renumbered to `§16`, and added to the previously-incomplete Table of Contents.
  - Verified `EventBus`/`ServiceLocator`/`IPlayerInventory`/`IPlayerProgressData` against the
    *real* `Assets/Scripts/Core/EventBus.cs` — already correct, no changes needed there.
- **Two gaps genuinely NOT resolved** (real missing interfaces, not renames — flagged inline in
  the doc as `// TODO: pending design` rather than invented): the `CreatureCaptured` conditional
  needs a roster-query interface that doesn't exist yet (no way to ask "does the player have
  species X at ≥40% bond"); the `SkillTreeUnlocked` conditional's scope (per-creature vs.
  account-wide) is ambiguous in §4's own example — recommended per-creature via the existing
  `SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(runtime)`, not yet confirmed.
- **Still open:** The `.pdf` (canonical source per the doc's own header) was NOT updated — outside
  Claude Code's reach, same situation as the pre-existing Active Slots table sync gap. Someone
  needs to manually port these fixes into the PDF before Phase 4 implementation treats it as
  authoritative over the `.md` mirror.
- **Date:** Found July 2026, resolved (`.md` mirror only) 2026-08-10.

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

### [Player] Deleted the orphaned 8-directional PlayerController, renamed the live 4-directional one to PlayerTopDownController
- **Decided:** Deleted `PlayerController.cs` (239 lines, 8-directional, `MoveX`/`MoveY` Animator
  params) — it was not attached to anything in `SampleScene.unity` and had been dead since
  whichever commit added `PlayerController_SideScroll.cs` alongside it. Renamed
  `PlayerController_SideScroll.cs` (400 lines, actually attached to the player, 4-directional
  top-down with root scale-flip for bone-rigged sprites) to `PlayerTopDownController.cs` — the
  old name described a movement model ("SideScroll") the class doc-comment itself contradicted
  ("4-directional top-down movement"). Renamed via `git mv` so the script's `.meta` GUID —
  and therefore the scene's serialized `MonoBehaviour` reference — survived the rename intact.
  Updated the 4 hardcoded `PlayerController_SideScroll` type references in
  `WildEncounterCreature.cs` and the 3 comment references in `CompanionAI.cs` to match, and
  fixed the renamed file's header comment (it incorrectly said its own path was
  `PlayerController.cs`).
- **Why:** External repo audit (`AUDIT_202608.md` AUD-004) flagged the dual controllers as a
  correctness trap — tuning the dead file produces no in-game change and no error, and the
  wrong-named live file makes intent unclear to a cold reader.
- **Alternatives rejected:** Keeping the 8-directional controller and porting the bone-rig
  scale-flip logic to it — rejected for scope; no design requirement for 8-directional movement
  exists, and the live 4-directional controller already works and is tuned.
- **Date:** 2026-08-04
- **Revisit if:** Never confirmed with the Unity Editor open — flagged in `KNOWN_ISSUES.md` to
  open the scene once available and confirm no "missing script" warning on the player object.

---

## New Entries — August 2026 Repo Audit Fix Session (Editor-attached pass)

### [World] Overworld "lanes" stay a deferred idea, not implemented alongside AUD-005
- **Decided:** `WildEncounterCreature`'s new patrol/detection system (AUD-005) uses a plain
  radius + facing-cone + line-of-sight check, not discrete overworld "lanes" carrying the battle
  stage's 7-lane depth system into exploration. Confirmed this direction with the user before
  writing any code.
- **Why:** `Combat_Directive_v0_1_0.md` Part 3's "Lane Avoidance — Overworld Carry-Over" section
  sits inside a Part that's otherwise entirely about the *battle stage's* literal lanes (dodge
  AoE, protect a Phasix, positional skills) — it reads as a narrative/thematic bridge ("the
  depth-reading skill you build in battle also helps you read overworld space"), not a technical
  spec for a gridded lane overlay on the overworld. Nothing else in the GDD, World Design
  Directive, or Technical Directive references overworld lanes, and the overworld controller
  (`PlayerTopDownController`) is free 2D movement, not gridded — retrofitting discrete lanes onto
  it would be a substantial, unscoped world-design change with no other support in the docs.
- **Alternatives rejected:** Building a literal overworld lane grid (creatures patrol along a
  lane, player avoidance is "pick a different lane") — rejected as a much bigger, riskier scope
  than a single audit-fix session, and not clearly what the Directive actually intends.
- **Date:** 2026-08-04
- **Revisit if:** A future dedicated world-design session decides overworld lanes should be a
  real spatial concept — at that point `Combat_Directive_v0_1_0.md`'s carry-over section stops
  being aspirational and this decision should be revisited alongside it.

### [Player] Sprint is a hold-while-held multiplier, not a cooldown-gated burst dash
- **Decided:** `PlayerTopDownController`'s new movement verb (AUD-005) is Sprint — hold to move
  at `_moveSpeed * _sprintMultiplier` (1.6x) — using the "Sprint" action already defined (but
  unused) in `InputSystem_Actions.inputactions` (Left Shift / left-stick-press). No stamina
  system, no cooldown, no fixed-duration burst.
- **Why:** The existing input binding is literally named "Sprint" with a plain Button-type action
  (not a Hold interaction), which already implies "faster while held," not "press to burst." Using
  it as-is needed zero new input wiring. A stamina system isn't specified anywhere in the design
  docs, and inventing one for this audit-fix pass would be scope creep beyond "give the player a
  second overworld verb."
- **Alternatives rejected:** Cooldown-gated burst dash (fixed distance/duration, then a recovery
  window) — rejected as needing new input bindings and new tunable state (duration, cooldown) for
  no documented design requirement; the existing "Sprint" action and its name already pointed at
  the simpler model.
- **Date:** 2026-08-04
- **Revisit if:** A future combat/traversal design pass calls for a dash specifically (e.g. an
  i-frame dodge, or a lane-avoidance-style burst) — that would be a different, additional verb,
  not a replacement for Sprint.

---

## New Entries — 2026-08-07 Battle HUD/Aura Follow-up Fixes

### [UI] Skill-orb hover tooltip rebuilt as a custom runtime element, not VisualElement.tooltip
- **Decided:** Replaced `BattleHUDController.PopulateSkillRing`'s `slot.tooltip = ...` assignment
  (same-day earlier work) with a dedicated `_skillTooltip` Label, shown/positioned/hidden via
  `PointerEnterEvent`/`PointerMoveEvent`/`PointerLeaveEvent` handlers registered per skill-ring
  slot, following the cursor at a fixed offset.
- **Why:** User playtesting found the tooltip never appeared. `VisualElement.tooltip` is Editor-
  only — it renders inside Editor-hosted UI (Inspector/EditorWindow panels) but is a documented
  no-op for a runtime `UIDocument` panel, in both Play Mode and a real build. The earlier session's
  implementation compiled cleanly and read correctly by inspection, which is why it shipped
  without anyone catching the gap until live testing.
- **Alternatives rejected:** None seriously considered — this is a straight bugfix once the root
  cause was identified; a custom floating element is the standard UI Toolkit runtime-tooltip
  pattern and mirrors the existing `_dragLine`/`DragLineVisual` precedent already in this file.
- **Date:** 2026-08-07
- **Revisit if:** Never, barring a future Unity version making runtime `.tooltip` support real.

### [UI] Battle HUD scaled via PanelSettings.scale rather than per-element font/size bumps — SUPERSEDED same day, see the entry below
- **Decided:** Raised `BattleHUDPanelSettings.scale` from 1 to 1.35 (still `ConstantPixelSize`
  mode) to address "the HUD is a little too small to read," instead of individually increasing
  the many separately-tuned font-size/width/height/radius constants across `BattleHUD.uss` and
  `BattleHUDController.cs`.
- **Why:** `ConstantPixelSize` with `scale: 1` means 1 UI unit = 1 literal screen pixel regardless
  of the player's actual display — at typical desktop resolutions that reads as genuinely tiny,
  independent of the low-res 320×180 game-world camera (a separate render pass). `PanelSettings.
  scale` uniformly multiplies the whole panel's rendered size while UI Toolkit's own pointer-event
  coordinate conversion accounts for it automatically, so every already-verified-live pixel
  constant (nameplate ring sizing lerp, move-option radius, drag/click hit-testing, etc.) keeps
  its exact relative proportions with zero code changes.
- **Alternatives rejected:** Manually re-tuning every font-size/width/height constant across two
  files — rejected as high-risk given how many of those values carry "verified live" comments
  documenting a specific prior fix (e.g. nameplate sizing, `.stage-side` clearance math); a single
  panel-wide scale achieves the same visual goal with no risk of re-breaking that tuning.
- **Date:** 2026-08-07
- **Revisit if:** 1.35 turns out to be too much/little at the user's actual resolution, or a
  future pass moves to `ScaleWithScreenSize` for proper multi-resolution support — pending, not
  attempted here.

### [UI] Global PanelSettings.scale reverted; only the nameplate readout enlarged
- **Decided:** Reverted `BattleHUDPanelSettings.scale` from 1.35 back to 1 (same day as the entry
  above). "Bigger HUD" scope narrowed to just the health/Aura/Evo nameplate readout — the player
  stage circles and skill wheel needed to stay their original size.
- **Why:** The panel-wide scale was the wrong tool for a request scoped to one specific HUD
  region — it enlarged everything uniformly, including parts the user explicitly wanted untouched.
  Enlarging only the nameplate is achievable within that element's own sizing (font-size/width/
  height on its sub-elements), no panel-wide lever needed.
- **Alternatives rejected:** A smaller panel-wide scale that happens to look right for the
  nameplate while keeping the stage "close enough" — rejected, doesn't actually satisfy "the
  player and skill wheel should've stayed the same size," which asks for an exact, not
  approximate, revert of the stage/wheel's dimensions.
- **Date:** 2026-08-07
- **Revisit if:** Never — this is the corrected scope, not a temporary state.

### [UI] Nameplate HP/Aura/Evo readout rebuilt as a togglable Bars-vs-Radial mockup
- **Decided:** New `BattleHUDController.NameplateStyle` enum (`Radial`/`Bars`), selected by one
  `ActiveNameplateStyle` const (currently `Bars`). Radial is the original circular
  `RadialGaugeVisual` ring + 3 always-on tiny stat labels, kept byte-for-byte as it was. Bars is a
  new mockup — 3 stacked horizontal rectangles (HP/Aura/Evo), no numbers shown by default;
  hovering a bar's track reveals "current/total" via the same shared hover-tooltip infrastructure
  the same-day skill-orb tooltip fix introduced (generalized from `_skillTooltip` to `_hudTooltip`
  for this reuse). `BuildNameplate`/`RefreshNameplateStats`/the new shared `ApplyEvoVisual` helper
  (used by both the refresh loop and `SetBurstFillBar`'s separate call site) all branch on the
  const; neither style's code was deleted when the other was built or touched.
- **Why:** User's explicit ask — "explore another mockup of the health aura and evo bar that
  involves 3 horizontal rectangles stacked vertically... if you hover mouse over the specific bar,
  the current vs. total amount shows up" — plus an explicit instruction to keep the circular
  configuration available in case of a revert, which is why this was built as a togglable
  alternative rather than a replacement.
- **Alternatives rejected:** Deleting the Radial code and replacing it outright — explicitly
  rejected by the user's own instruction to preserve it. Giving Bars its own party-count-based
  size interpolation (mirroring Radial's Comfortable/Compact lerp) — deferred; this is a first-pass
  mockup at a single fixed size, not yet verified against more than the current 3-member party.
- **Date:** 2026-08-07
- **Revisit if:** The user picks a final style (delete whichever branch loses, simplifying every
  method this touched back to unconditional code) — or asks for the Bars style's numbers to show
  always instead of only on hover, or for species-color accenting like the Radial portrait had.

### [UI] StatusHeader moved after Stage in BattleHUD.uxml — fixes nameplate pointer occlusion (and, retroactively, the old "Evo gauge not clickable" issue)
- **Decided:** Reordered `BattleHUD.uxml` so `<StatusHeader>` (the nameplate sidebar) is declared
  AFTER `<Stage>`, instead of before it.
- **Why:** User report — nameplate hover tooltips (the just-added Bars mockup) weren't triggering
  at all in real Play Mode testing. Root-caused via `IPanel.Pick(point)` at the nameplate's own
  on-screen coordinates: `.stage` (`position: relative; flex-grow: 1`, deliberately sized to fill
  the entire `.battle-root` — see that decision) geometrically overlaps the screen region the
  nameplate sits in, and since `.status-header` was the EARLIER sibling in document order, `.stage`
  — painting/picking later — silently won every pointer event across that overlap despite having
  no visible content there. `Pick()` at the nameplate's coordinates returned `.stage`, not any
  nameplate descendant, before the fix; the correct element after. `.status-header` is `position:
  absolute`, so this reorder changes ONLY pick/paint z-order, not layout position. This is almost
  certainly the real cause of the older, previously-misdiagnosed `KNOWN_ISSUES.md` `[EDITOR-001]`
  "Evolution Burst gauge not clickable" report too — same click path (the ring-wrap/Evo-bar's
  `onRingClicked`), same occluded region, closed retroactively alongside this fix. See
  `LESSONS_LEARNED.md` → `[UI Toolkit] A position: absolute sibling declared BEFORE a position:
  relative/flex-grow sibling gets silently occluded for pointer picking...` for the full
  investigation, including why an earlier scripted verification (`SendEvent` with a manually-preset
  `.target`) gave a false positive and didn't catch this.
- **Alternatives rejected:** Setting `.stage.pickingMode = Ignore` — rejected, `.stage` itself needs
  to stay pickable (`StageBackgroundClicked`, stage-creature clicks, drag targets all register
  directly on or under it). Reordering the two top-level siblings achieves the fix with zero
  functional change to `.stage`'s own behavior.
- **Date:** 2026-08-08
- **Revisit if:** A future HUD element is added as a sibling of `.stage`/`.status-header` that also
  needs picking priority over `.stage` — apply the same "declare it later in the UXML" rule rather
  than re-deriving this from scratch.

### [UI] Hover tooltip anchored to the hovered element, not the cursor
- **Decided:** `PositionHudTooltipNear(VisualElement anchor)` replaces the cursor-following
  `PositionHudTooltip(Vector2 pointerPosition)` — the tooltip now appears immediately to the right
  of, and top-aligned with, whichever element is hovered, computed once on `PointerEnterEvent`.
  The `PointerMoveEvent` handlers that used to re-track the cursor on both hover sites (skill orbs,
  nameplate bars) were removed — no longer needed once the anchor is the element, not the pointer.
- **Why:** User's explicit ask, after the nameplate occlusion fix landed but skill-orb hover still
  looked broken: "for the skill hover im expecting it be near/next to the skill we're hovering."
  Investigation (see `LESSONS_LEARNED.md`) confirmed the skill-orb hover mechanism itself already
  fired correctly even under the most faithful scriptable simulation of real mouse input available
  — the likely actual problem was that the OLD cursor-relative offset (+18/+18 px) could land the
  tooltip somewhere the user wasn't looking, especially for skill-ring orbs that sit low/left of
  the stage creature, closer to the screen edge than the nameplate.
- **Date:** 2026-08-08
- **Revisit if:** An anchor sits close enough to the right screen edge that the tooltip would
  render partially off-screen — not handled yet (no edge-detection fallback to the left side).

### [UI] Skill-orb tooltip content generated from each skill's resolved mechanics, not its shared placeholder Description
- **Decided:** New `BuildSkillTooltipText(SkillData)` replaces the tooltip's old content source
  (`skill.Description`, the shared dev-facing placeholder disclaimer identical across all 36
  assets) with output from `PlaceholderSkillResolver.Resolve(skill)` — the same resolution
  `BattleManager.ResolveSkillAction` already uses to actually run the skill. Damage skills show
  resolved category (Physical/Elemental) + the shared placeholder Power; status skills show the
  resolved status name, its real `StatusEffectCatalog` duration range, and Self/Enemy targeting.
- **Why:** User's explicit ask: "use the values from each skill orb to generate your content."
  `skill.Description` is the same disclaimer text on every placeholder asset — showing it in a
  player-facing tooltip wasn't differentiated per skill and wasn't useful battle information.
  `PlaceholderSkillResolver`'s output IS differentiated per skill (traced to already-GDD-locked
  tables — see that class's own doc comment) and is literally the data driving what the skill does
  when cast, so surfacing it in the tooltip is the direct "use this skill's own values" reading of
  the request, and reuses existing resolution logic rather than inventing new per-skill content.
- **Alternatives rejected:** Showing `skill.Description` alongside the resolved summary — rejected,
  it adds no differentiated information (identical boilerplate on every asset) and would just
  clutter the tooltip under the actually useful lines.
- **Date:** 2026-08-08
- **Revisit if:** Real skill content is designed (species roster, GDD §14) and `SkillData.
  Description` becomes real per-skill flavor text — at that point it likely belongs back in the
  tooltip alongside (not instead of) the resolved mechanical summary.

### [UI] Built-in move tooltips (A/C/H/R/K) computed from their own live BattleConfig/CaptureSystem values, not hand-typed
- **Decided:** New `MoveOptionTooltips` — hover text for the 5 built-in moves, computed once from
  the exact named constants (`BattleConfig`, `DamageCalculator`, `CaptureSystem`) each move already
  uses in `BattleManager`, rather than a hardcoded string per move.
- **Why:** The 5 built-in moves aren't `SkillData`-backed, so they have no `PlaceholderSkillResolver`
  entry to read from the way skill-ring orbs do — needed a parallel source, and computing it from
  the real constants (rather than typing numbers by hand) matches the same "use the real values"
  standard and avoids the tooltip drifting out of sync if a constant is retuned later. This
  concretely mattered for Capture: a hand-typed guess at its range (the code's own `Clamp(..., 0,
  95)` ceiling) would have shown 10-95%, but the real achievable range with today's constants is
  10-70% — computing via `CaptureSystem.ComputeCaptureChancePercent` directly caught this.
- **Date:** 2026-08-08
- **Revisit if:** Any of the referenced constants change — the tooltip updates automatically, no
  action needed; only revisit this entry if a move's underlying MECHANIC changes (not just its
  numbers), since the format string per move would need updating too.

### [UI] Skill-ring orb color is owned by ring POSITION, not species PrimalType
- **Decided:** Real skill-ring slots now color via `.skill-ring-color-0`..`-6` (one per ring
  position, applied by `PopulateSkillRing`), replacing the old `PrimalTypeColor.GetColor(species.
  PrimalType)` per-battle lookup. Also added `_playerSkillSlotLabels` — a `Label` per slot, reusing
  `.move-option-label` verbatim, showing the equipped skill's `SkillName`.
- **Why:** User's explicit ask: "make sure the orb has the lettering like all the other orbs and
  is visible. Select whatever colors you see fit." A/C/H/R/K are colored by their fixed clock
  position, not by whatever move happens to occupy it — giving the skill-ring slots the same
  position-owned model keeps both orb families consistent, and reusing `.move-option-label`
  exactly (rather than a near-duplicate class) means C1/C2 read as the SAME visual language as the
  built-in moves, not a separate one. Palette (teal/amber/indigo/rose/lime/cyan/brown) chosen
  deliberately distinct from A/C/H/R/K's own green/blue/pink/purple/gold.
- **Alternatives rejected:** Keeping PrimalType-based coloring and just adding the label — rejected
  along with the label ask itself once it became clear PrimalType coloring was essentially
  arbitrary/uncontrolled (whatever color a given species happens to be), which is a weaker fit for
  "select whatever colors you see fit" than a small number of deliberately chosen, distinguishable
  colors.
- **Date:** 2026-08-08
- **Revisit if:** Real skill content is designed and per-skill icons/identity exist — at that
  point position-owned color may no longer be the right model (an icon might want its own fixed
  color regardless of which ring position it lands in).

### [Combat] Round-robin skill equip across unlocked trees, not sequential-per-tree
- **Decided:** `WildSpawnSystem.SeedInitialSkills` now equips one skill per unlocked tree per
  pass, cycling trees until the tier's equip cap is reached — instead of fully draining each
  tree's learned skills before the next tree gets a turn.
- **Why:** Surfaced by the same-day skill-ring lettering fix making equipped skills visible for
  the first time: with every tree having exactly 2 placeholder skills and Tier 1's 2-slot equip
  cap, sequential-per-tree filling meant the FIRST unlocked tree alone always exhausted the cap.
  The test species unlocks Mirror + Reaction specifically (B.8 bootstrap's explicit intent — "so
  the pre-wired RepeatSameSkill/TimedInputStreak combo defaults are actually reachable and
  playtestable"), but both equip slots were silently going to Mirror, leaving Reaction's C2 (the
  TimedInputStreak grant) learned-but-never-equipped — the exact mechanic B.8 was written to make
  reachable, made permanently unreachable by this equip-order bug.
- **Alternatives rejected:** None seriously considered — round-robin is the direct fix for
  "every unlocked tree should get a fair shot at the equip cap," and required no new fields or
  balance numbers, just reordering which skill gets considered when.
- **Date:** 2026-08-08
- **Ref:** New `WildSpawnSystemTests.cs` covers the exact 2-tree/2-skill/cap-2 regression case,
  the "learning is unaffected by the equip cap" invariant, and the single-tree fallback.
- **Revisit if:** A tier's equip cap ever isn't evenly divisible by tree count in a way that
  matters for fairness (e.g. 3 trees, cap 2) — round-robin still behaves reasonably (first N trees
  in unlock order get one skill each), but this hasn't been explicitly playtested for that case.

### [UI] Evolution Burst ready color set as an inline style, not a USS class alone — a real specificity bug, not just a style choice
- **Decided:** `ApplyEvoVisual`'s Bars branch now sets `np.EvoBarFill.style.backgroundColor`
  directly (reusing the existing `NameplateEvoReadyColor`/`NameplateEvoFillingColor` constants),
  in addition to still toggling the `.nameplate-bar-evo-ready` class for other code to query.
- **Why:** User reported no visible indicator when Evolution Burst is ready. Root cause: the
  ready color was ONLY ever expressed via `.nameplate-bar-evo-ready` (single-class selector), but
  `.nameplate-bar-evo .nameplate-bar-fill` (the base fill color, 2-class descendant selector) has
  equal-or-higher USS specificity and won regardless of declaration order — verified live via
  `resolvedStyle.backgroundColor` staying purple even with the ready class correctly present. An
  attempted CSS-only fix (matching the descendant rule's 2-selector count with a compound selector
  on the same element) was tried first and ALSO didn't win in practice, so rather than keep
  chasing USS's exact specificity/tie-break behavior, the inline style sidesteps the question
  entirely — inline styles always beat stylesheet rules in UI Toolkit, same as CSS.
- **Date:** 2026-08-08
- **Revisit if:** Never — inline style is the correct general answer whenever a runtime state
  needs to unconditionally override any possible stylesheet rule, not a workaround to replace once
  understood better.

### [UI] Evo-ready gets a flashing perimeter highlight, not just a static color change
- **Decided:** `ApplyEvoVisual` adds `.nameplate-bar-evo-track-ready` to the Evo bar's TRACK on a
  not-ready→ready transition, and starts a `VisualElement.schedule`-driven repeating toggle
  (`EvoFlashIntervalMs` = 450ms) of `.nameplate-bar-evo-flash`, alternating the border between two
  gold shades. Paused and cleared on the reverse transition. Guarded by `NameplateRefs.
  EvoFlashActive` so the schedule only starts/stops on an actual state CHANGE — `ApplyEvoVisual`
  runs on every stat refresh, far more often than readiness itself changes.
- **Why:** User's explicit ask: "maybe highlighting the perimeter of the evo box and have it
  flash lightly would be good," after reporting no indicator existed at all for a ready gauge.
- **Alternatives rejected:** A CSS-only pulsing effect (USS transitions) — UI Toolkit's USS
  doesn't support looping keyframe-style animations the way web CSS can; a scheduled class toggle
  is the standard UI Toolkit runtime idiom for this.
- **Date:** 2026-08-08
- **Revisit if:** Never playtested against a real "the flash rate feels too fast/slow" reaction —
  `EvoFlashIntervalMs` is a placeholder value, easy to retune.

### [Combat] Aura tier ceiling gates auraAllocatedPoints, not baseStats.Total
- **Decided:** Added `PhasixRuntimeData.auraAllocatedPoints` (running total of stat points ever
  purchased through `AuraStatAllocationSystem`). `TryAllocateStatPoint`/`GetRemainingCeilingRoom`
  now check this field against `AuraTierCeiling.ComputeCeiling(...)` instead of `baseStats.Total`.
  Neither `AuraTierCeiling` placeholder constant (`BaseCeilingPerTier` = 40, `
  CeilingIncreasePerAptitudePoint` = 4) changed.
- **Why:** User reported the Tab-menu "+1" button silently doing nothing despite having Aura.
  Root cause: `baseStats.Total` includes a species' full innate stats — `Test_FireType`'s alone
  sum to 177 (Vitality 120 + the rest), already far past the tier-1 ceiling of 40, before any
  Aura is ever spent. Gating on `baseStats.Total` therefore blocked every allocation attempt from
  the very first click for any real species; the existing tests never caught this because they
  only exercised the system against synthetic `StatBlock.Zero`/ceiling-relative values, never a
  real species' starting numbers. `Progression_Directive_v0_1_0.md`'s own wording — "Stat growth
  through Common Aura is capped per tier" — supports gating on *growth* (points actually
  purchased) rather than the creature's total stat value inclusive of its starting baseline.
- **Alternatives rejected:** Raising `BaseCeilingPerTier` to some larger placeholder (e.g. 400) so
  `baseStats.Total` clears it — rejected because it's fragile against any future species with an
  even higher innate total, and doesn't match the Directive's literal "growth is capped" wording;
  tracking the invested amount separately is robust to any species' baseline and touches no
  numeric balance value, so it stays a wiring/bugfix, not a calibration decision.
- **Date:** 2026-08-07
- **Revisit if:** Devolution is built — `auraAllocatedPoints` resetting alongside `baseStats` (or
  not) on devolution is unresolved and should be decided then, not assumed from this entry.

---

## New Entries — 2026-08-08 Full Overworld Menu Session

### [Save] Save/Load implemented — JsonUtility, 3 manual slots, auto-continue by file write-time
- **Decided:** New `Assets/Scripts/Save/` (`PhasixSaveData`, `PartySaveData`, `SaveFile`,
  `SaveSystem`). `SaveSystem.Save`/`TryLoad` write/read `Application.persistentDataPath/
  save_slot_{n}.json` via `JsonUtility`. No separate "current slot" marker — `TryGetNewestSlot`
  just compares `File.GetLastWriteTimeUtc` across all 3 slot files and picks the newest, so saving
  to any slot naturally becomes "continue from here" next launch. `Dictionary<string,int>
  specificAura` and `HashSet<string> discoveredNodeGuids` are flattened to parallel `List<T>`s at
  the DTO layer since `JsonUtility` supports neither directly. Species references (`PhasixData`,
  `[NonSerialized]` on `PhasixRuntimeData`) round-trip through a new `SpeciesDatabase` (mirrors the
  existing `SkillDatabase` GUID-index pattern exactly) rather than `AssetDatabase`, which is
  Editor-only and unusable from save/load code that must also work in a build.
- **Why:** [Save] Save format (March 2026, above) already locked JsonUtility as one of two
  acceptable formats; picked here specifically to avoid a new package dependency, since every DTO
  was hand-flattened to be fully JsonUtility-compatible anyway.
- **Alternatives rejected:** A separate "active slot index" file/PlayerPrefs entry as the
  auto-continue marker — rejected as redundant; file write-time is already an unambiguous,
  tamper-resistant ordering with no extra state to keep in sync.
- **Date:** 2026-08-08
- **Revisit if:** A future explicit "load a different slot" UI is added — this pass is
  deliberately save-only, per user direction ("Save-only menu; auto-load last-saved slot on boot").

### [Core] GameManager boot-load runs on SceneManager.sceneLoaded, not Start()
- **Decided:** `GameManager` (persistent singleton, `DontDestroyOnLoad`) resolves "auto-load the
  newest save, or seed a fallback starter" from a `SceneManager.sceneLoaded` handler registered in
  `OnEnable`, not `Start()`.
- **Why:** Live-verified bug: the debug "New Game" button (`GameManager.ResetToNewGame`) reloads
  the active scene while `GameManager` itself survives via `DontDestroyOnLoad`. `Start()` only
  ever fires ONCE per component instance — it does not re-run just because the scene around a
  surviving `DontDestroyOnLoad` object reloads. A `Start()`-based version silently never re-seeded
  the party after a debug reset (console showed no "seeded fallback starter" log, and the old
  save's stats stayed in the freshly-reloaded scene). `sceneLoaded` fires for every load, including
  the very first one at boot, so one handler covers both cases.
- **Alternatives rejected:** Manually re-invoking the load/seed logic from inside
  `ResetToNewGame` before calling `SceneManager.LoadScene` — rejected because the freshly-reloaded
  scene's `PartySystem` doesn't exist yet at that point (it's created by the load itself), so the
  seed would target the SOON-TO-BE-DESTROYED old `PartySystem`, not the new one.
- **Date:** 2026-08-08
- **Revisit if:** Never — this is the correct Unity lifecycle idiom for logic that must re-run
  after any scene (re)load a persistent singleton survives.

### [UI] Tab menu rebuilt as a full Party/Save/Bag/Options shell, replacing the single-purpose Aura screen
- **Decided:** `PartyMenuController`/`PartyMenu.uxml`/`.uss` deleted, replaced by
  `OverworldMenuController`/`OverworldMenu.uxml`/`.uss`. Party tab: roster cards -> click opens a
  per-creature detail view with the ported Aura stat-allocation rows plus an equipped skill ring
  that reuses the battle scene's own orb classes (`.skill-slot-placeholder`/`.skill-ring-color-N`/
  `.move-option-label`), the shared `HudTooltip`, and `DragLineVisual` — so it reads identically to
  battle, per explicit user ask. Save tab: 3 slots, click to overwrite. Bag/Options: static
  "pending design" placeholders — no item or settings system exists yet. An always-visible debug
  "New Game" button lives outside the Tab-toggled menu root so it renders regardless of menu state.
- **Why:** User: "I need a Full menu = party, save, bag, options... clicking on them allows you to
  see stat allocation, possible skills equipped and equipped skills as full icons and placement
  like you see in battle scene so we can drag and drop there."
- **Alternatives rejected:** A from-scratch skill-ring visual for the menu, independent of
  battle's — rejected per the user's explicit ask for the SAME look or feel, and because
  duplicating the orb/color/label/tooltip system would drift out of sync with battle over time.
- **Date:** 2026-08-08
- **Revisit if:** A real Bag/Options system is designed — those tabs are the natural place to
  extend, not a reason to pre-build them now.

### [UI] Skill ring drag/drop: both swap-in-place AND tray re-equip; right-click to unequip
- **Decided:** New `SkillLoadoutSystem` (static rules class, matches `AuraStatAllocationSystem`/
  `BondSystem`'s convention): `TryEquip`/`TryEquipAt`/`Unequip`/`SwapEquipped`. Dragging one
  equipped orb onto another swaps their positions (`SwapEquipped`). Dragging a learned-but-
  unequipped tray skill onto ANY ring orb (occupied or empty) equips it there, overwriting
  whatever was there (`TryEquipAt`) — the overwritten skill simply falls out of
  `equippedSkillGuids` and reappears in the tray, since it's never removed from
  `learnedSkillGuids`. Right-click (UI Toolkit's `ContextClickEvent`) on an equipped orb unequips
  it back to the tray (`Unequip`) without touching `learnedSkillGuids`.
- **Why:** User, when asked to choose between "swap only" and "tray re-equip only": "lets do both
  options and then let right click be to unequip an equipped skill."
- **Alternatives rejected:** A single unified "equip" call that always appends to the end of
  `equippedSkillGuids` — rejected because dropping onto a SPECIFIC occupied ring position needs to
  land at that exact index, not wherever the list's next open slot happens to be, so `TryEquip`
  (append-only, from Part E) was extended with `TryEquipAt` (index-targeted) rather than reused.
- **Date:** 2026-08-08
- **Revisit if:** Never — this is the agreed-on interaction model, not a placeholder.

### [UI] Skill configurator follow-up: per-skill color, full catalog + built-ins, compact labels
- **Decided:** Three follow-up fixes to the Party menu's skill configurator after first-pass user
  feedback. (1) Orb/tray color is now a deterministic hash of the skill's own GUID into the
  7-color palette (`OverworldMenuController.GetSkillColorClass`), not owned by ring position —
  user: "the colors and stuff should be dedicated to that specific skill not just on the slot."
  (2) The "All Skills" tray now also lists the 5 built-in moves (A/C/H/R/K), shown with battle's
  real colors/hover text but not draggable (they're inherent to every creature, not managed by
  `SkillLoadoutSystem`) — user: "the other skills... C, H, A, etc. are all skills that should be
  on the all skills list." (3) Every orb label (ring and tray) is now a short code
  (`GetShortSkillLabel`) instead of the full `SkillName`, which was overflowing a 32px circle —
  user: "It should only show their icon maxium a letter and a number... they should all have the
  hover over description similar to the in battle game." An already-short real name (`C1`/`C2`)
  passes through as-is; everything else gets `{tree-initial}{index-in-tree}` (e.g. `S1`).
- **Bug found and fixed during live verification:** Corruption is the only one of the 18
  `SkillTreeType`s whose own initial is `C` — without an override, its first two skills would
  auto-generate `C1`/`C2` too, colliding with the real, hand-set `C1`/`C2` (which
  `ComboRuleEvaluator`'s `RepeatSameSkill` rule specifically references — not just cosmetic, two
  actually-different skills rendering identically). Live-verified before the fix: unequipping the
  real C1 and finding it in the tray showed a DIFFERENT color than it had on the ring
  (`skill-ring-color-4` vs `skill-ring-color-6`) — root cause was `GetShortSkillLabel` returning
  `"C1"` for Corruption's first skill too, so a tray search for the label `"C1"` sometimes matched
  the wrong skill entirely. Fixed by reserving the letter `C` for hand-authored short names —
  `GetShortSkillLabel` substitutes `X` for Corruption's tree-initial. Re-verified live: ring/tray
  colors now match for the same skill, and exactly one tray entry is labeled `"C1"`.
- **Alternatives rejected:** A 2-letter tree-prefix scheme (e.g. `Co1` for Corruption) — would
  avoid this specific collision but contradicts the user's explicit "maximum a letter and a
  number" format, and other letter collisions (e.g. Aura/Aspect both -> `A`) are accepted as-is
  per the user's own stated tolerance for placeholder imperfection ("a lot of them are just
  placeholders and don't do anything but that's how it should work") — hover always resolves the
  ambiguity with the skill's real name.
- **Date:** 2026-08-08
- **Revisit if:** A skill is ever hand-renamed to a short code starting with a letter other than
  `C` — the same collision class could recur for that letter's tree-initial and would need the
  same kind of reservation.

### [Combat] Generic status-effect (debuff/buff) icons added to the nameplate — previously invisible
- **Decided:** `BattleParticipant.ActiveStatuses` was already fully tracked (`ApplyStatus`/
  `TickStatuses`, wired into `ResolveSkillAction`'s status branch and `ChainResultCatalog`/
  `MasteryBonusCatalog` detection) but had ZERO nameplate visualization — confirmed by grep,
  `BattleHUDController` had no reference to `ActiveStatus`/status icons at all before this pass.
  Added a fixed pool of `StatusIconPoolSize` (4) generic icon slots per nameplate, reusing
  `.nameplate-buff-icon`'s existing visual language (small lettered circle + countdown subscript,
  same as Regen/Burst) — letter is the status type's own first initial, color is by
  `StatusEffectCategory` (5 new placeholder color classes, `.nameplate-status-physical/elemental/
  signal/universal/positive`), hover shows the full name/category/turns-remaining via the shared
  `HudTooltip`. Runs every `RefreshNameplateStats` call (both sides), so an expired status
  (`TickAllStatuses`, once per round) clears on the next refresh.
- **Why:** User: "for c1 on application for debuffs i dont seee any debuffs on the enemy hud,
  please include that." (C1 itself deals damage, not a status — the underlying gap was general:
  no status-applying skill's effect was ever visible on either nameplate.)
- **Alternatives rejected:** A per-status unique color/icon (28 distinct designs) — rejected as
  inventing real content beyond what's GDD-locked; category (5 values, GDD §17's own table
  grouping) is the right granularity for a placeholder pass, same reasoning as the skill-color
  hash needing no per-status hand-authored art. A dynamic (unbounded) icon list — rejected as
  unnecessary complexity; a fixed pool of 4 comfortably covers realistic simultaneous-status counts
  for this project's current scope.
- **Verified live:** Applied Bleed (Physical) + Regenerate (Positive) directly to a live enemy
  participant mid-battle — both appeared with correct letter/color/countdown, hover dispatched the
  correct tooltip text, and unused pool slots stayed hidden.
- **Date:** 2026-08-08
- **Revisit if:** `StatusIconPoolSize` (4) is ever hit in real play (needs bumping) — no test
  currently exercises the overflow case since it's a placeholder-generous cap, not a modeled limit.

### [Combat] Built-in moves (A/C/H/R/K) now interrupt RepeatSameSkill — previously invisible to combo history
- **Decided:** `BattleManager.PlayerTurn` now calls `attacker.RecordSkillUse(null)` whenever a
  built-in move (Attack/Charge/Heal/Regen/Capture) resolves, guarded by `_pendingSkill == null`
  (skill-ring uses still record the real `SkillData` via `ResolveSkillAction`, unchanged). A null
  entry naturally fails `ComboRuleEvaluator.HasTrailingMatch`'s `sequence[i] != target` check,
  correctly breaking any in-progress `RepeatSameSkill` streak.
- **Why:** User: "the other skills dont reset the counter on C1 if it has stacks, only C2
  does... Do all the other skills not count as normal skills?" Root cause: `RecordSkillUse` was
  ONLY ever called from `ResolveSkillAction` (the skill-ring path) — built-in moves left zero
  trace in `RecentSkillsUsed`, so the trailing-match check simply never saw them; a built-in used
  between two C1 casts didn't break the streak, it just left it frozen mid-way, and the next C1
  picked the same streak back up as if nothing happened in between. `RecentSkillTrees`
  (CrossTreeSequence's feed) is deliberately DIFFERENT and untouched here — its own doc comment
  already documents built-in exclusion as intentional (no `SkillTreeType` to record), a separate,
  already-considered design decision this fix doesn't revisit.
- **Alternatives rejected:** A dedicated `RecordNonSkillAction()` method instead of
  `RecordSkillUse(null)` — rejected as unnecessary API surface; `RecentSkillsUsed` is already
  typed `List<SkillData>` (nullable), and `HasTrailingMatch`/`GetRepeatTrailingStreakLength`
  already handle a null entry correctly with no changes needed on that side.
- **Verified live:** Recorded C1, C1 (streak length 2) → simulated built-in move (streak length 0)
  → C1 again (streak length 1, not 3) — confirms the streak genuinely resets rather than pausing.
- **Date:** 2026-08-08
- **Revisit if:** `TimedInputStreak` is ever reported with the same symptom — it wasn't touched by
  this fix (its own doc comment specifies only an actual timed-input miss breaks it, not "any
  other action," a different rule from RepeatSameSkill's).

### [UI] Party-menu equip slots beyond the tier cap now visually/functionally distinct from empty-but-fillable ones
- **Decided:** `OverworldMenuController.BuildSkillArea` computes `maxSlots` (the tier's real
  active-slot cap, `SkillSlotCapacity.GetActiveSlotRange`) again and applies a NEW
  `.skill-slot-tier-locked` class to any equip position at or beyond it — no drag-start/
  right-click registration, an explanatory "Locked / Requires evolution tier N+" hover tooltip
  (`GetTierLockedTooltip`, walks `SkillSlotCapacity` to find the actual required tier), and
  `OnDragUp`'s drop-target hit-test loop now bounds at `maxSlots` instead of the full 7 physical
  positions.
- **Why:** User: "i cant drag and drop the new skills onto the open placeholders either, but i
  can swap them with the existing C1 and C2. We need to have this fixed too." The underlying
  behavior (tier cap enforced) was already correct — `SkillLoadoutSystem.TryEquipAt` already
  refused any index `>= maxSlots` — but visually, a "beyond-cap, will never accept a drop" slot
  and a "within-cap, currently empty, valid drop target" slot both just showed the same plain
  `.skill-slot-locked` grey with no differentiation, so a beyond-cap drop read as broken rather
  than tier-gated.
- **Alternatives rejected:** Removing the tier cap in the party menu (letting any of the 7
  physical positions be filled regardless of tier) — rejected, this is a locked mechanic from
  Evolution_System_Directive's tier progression, not a UI-only restriction to relax. Hiding
  beyond-cap positions entirely instead of showing them locked — rejected, contradicts the
  already-agreed "replica of battle scene" wheel, which also always renders all 7 physical
  positions regardless of tier.
- **Verified live:** For a Tier-1 creature (cap 2): slots 0-1 (C1/C2) correctly NOT tier-locked;
  slots 2-6 correctly tier-locked. Dragging a tray skill onto slot 2 was correctly rejected (no
  change) with the tooltip reading "Locked / Requires evolution tier 2+"; dragging the same skill
  onto slot 0 correctly swapped it in, confirming within-cap drops still work unchanged.
- **Date:** 2026-08-08
- **Revisit if:** Never — matches the established tier-cap mechanic, just makes it legible in the
  one UI surface (drag-to-equip) where its absence of feedback was actually reachable by a player.

### [Combat] Built-in moves (Attack/Charge/Heal/Regen/Capture) became real, equippable/unequippable SkillData
- **Decided:** The 5 built-in battle moves stop being hardcoded, always-available, non-equip-managed
  fixed wheel positions. New `BuiltInMoveType` enum (`None`/`Attack`/`Charge`/`Heal`/`Regen`/
  `Capture`) + `SkillData.BuiltInMove` field mark 5 new real assets (`Standard_Attack.asset` etc.,
  `Assets/Data/Skills/`) registered in `SkillDatabase` like any other skill. New `SkillTreeType.
  Standard` (19th value, NOT GDD taxonomy — same precedent as `ComboRuleType`/`ChainResultType`)
  groups them, per the user's own wording: "if theres not particular skill tree for them they can
  all be grouped in their own as standard." `SkillName` = `"A"`/`"C"`/`"H"`/`"R"`/`"K"` — reuses
  their existing battle-scene letters, so the Party menu's `GetShortSkillLabel` picks them up with
  zero special-casing. `BattleHUDController`'s old fixed-5 wheel positions (`MoveOptionClockHours`/
  `MoveOptionIsSelfOnly`/`MoveOptionTooltips`/`_playerMoveOptions`, 15 UXML `MoveOption` elements)
  are gone — the skill ring is now a uniform 12-slot ring, every position a real equip slot.
  `BattleManager.ResolveSkillAction` dispatches on `skill.BuiltInMove` first (new
  `ResolveBuiltInMove`, the exact pre-existing per-move mechanics relocated verbatim) before ever
  reaching `PlaceholderSkillResolver`. `WildSpawnSystem.SeedInitialSkills` now always learns all 5
  Standard skills (regardless of `species.AvailableTreeTypes`) and seeds Standard FIRST in the
  round-robin equip pass, so Attack claims a slot by default — confirmed with the user as an
  acceptable temporary default pending a real move-pool-assignment design ("We havent done that
  yet so the ones we have are good for now"). `OverworldMenuController`'s old "5 built-ins,
  informational-only, not draggable" tray section is deleted — they now flow through the same
  `SkillDatabase.AllSkills` loop as every other skill: real per-skill color, real drag-to-equip,
  real right-click-to-unequip.
- **Why:** User, after the skill-configurator follow-up work: "dont make them inherent i want
  them to also be selectable. This makes it so players can remove things that they dont need and
  have full customizability for good or for worse."
- **Alternatives rejected:** Keeping built-ins hardcoded but ALSO letting the Party menu show them
  as draggable (a UI-only illusion of equippability with no real backing data) — rejected as
  fundamentally dishonest UI; the user's own framing ("full customizability, for good or for
  worse") specifically implies real consequences (a creature CAN end up with zero offense
  equipped), which requires them to be real, unequippable SkillData, not a cosmetic overlay.
- **Verified live:** Fresh-seeded Tier-1 creature defaults to `[Attack, C1]`. Attack/Charge/Heal/
  Regen/Capture each independently confirmed correct via `ResolveBuiltInMove` (Aura restored, HP
  healed, Regen applied, enemy captured and added to party, Attack damage logged). Party menu:
  all 5 appear in "All Skills" with real colors/labels; right-click-unequip and drag-back-to-equip
  both confirmed round-tripping correctly. A creature with ALL skills unequipped (including
  Attack) completed a full player turn and the following enemy turn with no errors — confirms
  "for good or for worse" doesn't soft-lock the battle loop.
- **Date:** 2026-08-08
- **Revisit if:** A real move-pool-assignment/starter-loadout design is ever built — the
  "Standard seeds first, Attack wins pass 0" default is explicitly a placeholder, not a design.

## New Entries — 2026-08-09 Skill Wheel Follow-up

### [Progression] Equip-slot count per tier changed to flat 4/6/8/10/12, reaching all 12 wheel positions at T5
- **Decided:** `SkillSlotCapacity.GetActiveSlotRange` now returns a flat `(4,4)/(6,6)/(8,8)/(10,10)/(12,12)`
  for T1-T5, replacing the original `(2,2)/(3,3)/(4,4)/(5,5)/(5,7)` table. Every natural tier now
  has a single fixed value — T5's old "5-7, varies by species" range is gone for now.
- **Why:** User: "at max tier they should be able to access all 12 slots. say tier 1 they have
  access to 3 slots, then increasing by 2 every tier." A pure `start=3, +2/tier` progression lands
  on 11 at T5, one short of the 12-position wheel (`BattleHUDController`/`OverworldMenuController`
  both render 12 physical ring slots) — user confirmed shifting the start to 4 instead so T5 lands
  exactly on 12, keeping the clean +2/tier step.
- **Alternatives rejected:** (1) Keep start=3, give T5 an irregular +3 final step (3/5/7/9/12) —
  rejected, user picked the clean start=4 progression instead. (2) Keep start=3, accept 11 at T5 —
  rejected, user explicitly wants all 12 wheel positions reachable.
- **This SUPERSEDES a number sourced from `Evolution_System_Directive_v1_1_0.pdf` §1** (the
  Directive's canonical PDF, per DOCUMENT_INDEX.md, still shows the old table — Claude Code cannot
  edit the PDF directly). The `.md` mirror (`Evolution_System_Directive_v1_1_0.md`) has been
  updated with a "PDF SYNC REQUIRED" note in both its Active Slots tables, same convention this
  file already uses elsewhere ("Status: GDD SYNC REQUIRED"). **The PDF itself still needs a manual
  update outside Claude Code's reach if it's meant to stay the source of truth.** "Skill Trees
  Available" (2/4/5/6/7) is unaffected — only the equip-slot count changed.
- **Future variance hook:** User: "let's do flat tier for now, but please build in the option to
  have it vary once we get more granular on phasix specific design." `GetActiveSlotRange`'s
  `(min, max)` tuple return shape already supports a future per-species range (T5's old 5-7 "varies
  by species" used this same shape) — reintroducing variance later needs only a data change, no
  signature or call-site change.
- **Downstream fix required:** `OverworldMenuController`'s `WheelEquipSlotOffset`/
  `WheelEquipSlotCount` changed from `0`/`7` to `0`/`12` — the previous "5 permanently decorative
  positions beyond the 7-slot ceiling" concept (added earlier the same day per user request) is
  gone; all 12 physical wheel positions are now real equip slots, with positions beyond the
  CURRENT creature's tier cap rendering tier-locked (dim, explanatory tooltip) via the existing
  `maxSlots` check rather than a separate always-decorative branch.
- **Tests updated:** `SkillSlotCapacityTests` (`GetTreeCount_And_GetActiveSlotRange_MatchLockedTable`
  test cases, `GetActiveSlotRange_TierFive_MaxIsTwelve` renamed from `...MaxIsSeven`),
  `SkillLoadoutSystemTests` (`TryEquip_AtTierCap_Fails` now needs 5 learned skills to hit the new
  Tier-1 cap of 4; `TryEquipAt_OutsideTierRange_IsNoOp` now tests `slotIndex: 4`, not `2`),
  `WildSpawnSystemTests` (`SeedInitialSkills_TwoTreesTwoSkillsEach_EquipsOneFromEachTree` renamed
  and rebuilt with 4 skills per tree so the round-robin regression is still exercised against the
  new, larger Tier-1 cap). 239/239 EditMode tests pass after the rewrite.
- **Date:** 2026-08-09
- **Revisit if:** The Directive PDF gets manually updated to match (closes the sync gap noted
  above), or T5's per-species variance gets reintroduced once species design is more granular.

## New Entries — 2026-08-09 Skill Web View Session

### [UI] Skill tree carousel replaced by a pan/zoom skill web — prototype of the Evolution Web concept
- **Decided:** Replaced the Skyrim-style paged carousel (shipped and retired the same prior
  session — see `KNOWN_ISSUES.md` UI-001, closed) with a free pan/zoom "web" view: every
  `SkillTreeType` is a column, its skills a vertical node row connected by a `Painter2D`-drawn
  line, laid out inside one "world" `VisualElement` whose `style.scale`/`style.translate` drive
  drag-pan and cursor-centered wheel-zoom. Nodes are real `VisualElement`s (native hover/click/
  `HudTooltip`), not hand-rolled hit-testing — the edge/glow layer alone
  (`Assets/Scripts/UI/SkillWebEdgeVisual.cs`) is the custom-painted piece, same
  `generateVisualContent`/`MarkDirtyRepaint()` convention as `DragLineVisual.cs`.
- **Why:** User: "we need to fix the skill tree look... this looks awful," then shared their
  original Evolution Web design mockup (`evolution_web.html` — added to the repo 2026-08-10 as
  `Assets/Docs/evolution_web_mockup.html`, see `DOCUMENT_INDEX.md` → "Design Mockups" for full
  context) and asked whether the same pan/zoom node-graph concept could work in Unity. Verified
  live via `unity_reflect` against the actual
  Unity 6000.3.11f1 API (not assumed from training data) that `Painter2D` has near feature-parity
  with the mockup's Canvas 2D techniques (`QuadraticCurveTo`, `SetDashPattern`+`dashOffset`,
  `strokeFillGradient`/`fillGradient` linear+radial gradients, a native `Blur` USS filter) and that
  the project already has an established local convention for exactly this
  (`DragLineVisual.cs`/`RadialGaugeVisual.cs`). A quick live test (throwaway parent/child
  `VisualElement` pair, `style.scale`/`translate` on the parent, read the child's `worldBound`
  before/after) confirmed the transform math this whole view depends on before building anything
  on top of the assumption.
- **Why the skill tree, not the real Evolution Web, first:** The real Evolution Web needs
  `EvolutionNodeSO`/`EvolutionBranchSO`/`EvolutionGraphSO` (Phase 4), which this file's own
  `[Creatures] Evolution_System_Directive_v1_1_0.md has internal inconsistencies` entry (below,
  Creatures — Future Systems section) says must be resolved before Phase 4 implementation starts.
  The skill tree has no such blocker — it's already fully placeholder content. Confirmed with user
  (AskUserQuestion): build the interaction pattern here first, reuse it for Evolution once
  unblocked.
- **Fog-of-war → tier-gating:** The mockup's Hidden/Sighted/Discovered fog-of-war doesn't map to
  skills (nothing is "encountered in the wild"). User chose (AskUserQuestion) to reuse the same
  three-state *visual* language driven by tier-gating instead: a tree not currently unlocked
  renders as a dim, non-interactive silhouette column (mockup's "Sighted") instead of a fully
  browsable one. Only 2 of the mockup's 3 states are used — no "Hidden," every one of the 18 GDD
  trees is always at least visible as a column.
- **Explicitly NOT ported** (mockup features that don't make sense for skills yet): crossover
  dashed branches (no cross-tree skill relationships exist), BFS "Plan Mode" pathfinding +
  animated path (no prerequisite graph between skills exists, and `SkillData` deliberately gets no
  new position/prerequisite field here — that's real skill-tree design, still pending
  project-wide), the Reveal-All/Fog-toggle debug buttons (replaced by the debug tier stepper
  below, a different debug need), and keyboard pan/zoom (confirmed with user — mouse drag + wheel
  only, matching the mockup exactly; the old carousel's arrow-key paging has no clean free-pan
  equivalent).
- **Per-tree color is procedural, not a palette extension:** Considered extending
  `BattleHUD.uss`'s existing `.skill-ring-color-0..6` (7 buckets, currently used per-SKILL identity
  for the equip wheel) to 19 buckets for per-TREE identity in the web view. Rejected — that file is
  shared with the live battle skill wheel, and touching it for a still-tunable placeholder view
  would be needless regression surface for no real benefit. Instead, `GetTreeColor` computes an
  HSV color per column index at runtime (hue rotated by the golden-angle conjugate so adjacent
  columns land on visually distinct hues) as inline style — `BattleHUD.uss` is untouched entirely,
  zero shared-file risk, and the wheel's own per-skill coloring convention is unaffected.
- **Alternatives rejected:** A literal 1:1 canvas-immediate-mode port (redraw everything including
  nodes via `Painter2D` each frame, hand-rolled hit-testing like the mockup's own
  `getHoveredNode()`) — rejected in favor of the hybrid (real `VisualElement` nodes + a thin
  `Painter2D` overlay for edges/glow only), which gets native picking/tooltip for free and matches
  how every other interactive element in this codebase's UI already works.
- **Date:** 2026-08-09
- **Revisit if:** When `Evolution_System_Directive_v1_1_0.md`'s inconsistencies are resolved and
  Phase 4's evolution graph SOs are built, reuse `SkillWebEdgeVisual` and the world-container
  pan/zoom pattern for the real Evolution Web — at that point the deliberately-omitted crossover
  branches and BFS Plan Mode become relevant again and should be added back in.

### [Combat] SkillLoadoutSystem now enforces unlockedTreeTypes — a real gap, not just cosmetic
- **Decided:** `SkillLoadoutSystem.TryEquip`/`TryEquipAt` now take the skill's `SkillTreeType` and
  reject equipping from a tree that isn't unlocked, via new
  `SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(runtime)` (`SkillTreeType.Standard` exempt —
  always available, not one of the 18 GDD taxonomy trees).
- **Why:** Found while building the skill web's tier-gate silhouette state: neither method had
  ever checked `unlockedTreeTypes` at all. Any learned skill from any tree — locked or not — could
  already be equipped; the old carousel UI simply never exposed a path to try it, so the gap was
  invisible. Building the debug tier control (below) required a display/gate consistency
  guarantee anyway, which forced actually closing this.
- **Single source of truth, not two independent checks:** `GetEffectiveUnlockedTrees` is called by
  BOTH the web view's render logic and the equip gate. Without this, the debug tier override could
  make a tree "look unlocked" in the UI while equipping from it still silently failed against the
  real (untouched) `unlockedTreeTypes` — found and fixed during plan review, before implementation,
  specifically because the debug tool's whole purpose is letting the user actually test equipping
  into newly-available slots, not just preview them.
- **Downstream fix required:** Wheel-slot interactivity (drag/right-click/hover) in
  `OverworldMenuController` moved from a build-time decision (register handlers only for slots
  under the tier cap, decided once when `BuildSkillArea` first runs) to a use-time check (every
  handler reads the current, mutable `maxSlots` when invoked) — necessary once tier can change
  live via the debug stepper without leaving the detail view; otherwise a slot tier-locked at open
  time could never become usable later even after its visual state correctly updated.
- **Date:** 2026-08-09
- **Revisit if:** Never expected to reverse — this closes a real correctness gap, not a
  placeholder decision.

### [Creatures] Debug tier override added to PhasixRuntimeData, not PhasixData
- **Decided:** `PhasixRuntimeData.DebugTierOverride` (`int?`, plain C# field, not persisted to
  `PhasixSaveData`/`SaveSystem`) lets a creature's EFFECTIVE evolution tier be walked 1-5 live from
  the Party menu's skill web header, to preview unlocks/slot capacity without a real (Phase 4,
  unbuilt) evolution changing it.
- **Why:** `EvolutionTier` lives on `PhasixData`, a ScriptableObject — CLAUDE.md's hard
  architecture rule is that SOs are read-only at runtime, never written to during play. The debug
  control needed a way to simulate a different tier without violating that, so it lives entirely
  in the runtime-only counterpart instead, same as every other per-individual mutable field.
- **Scope boundary (deliberate):** Only affects the Party-menu skill web view and
  `SkillLoadoutSystem`'s equip gate (both read through `GetEffectiveUnlockedTrees`/
  `DebugTierOverride ?? speciesData.EvolutionTier`). Does NOT affect `BattleHUDController`'s own
  slot-cap reads or `WildSpawnSystem` seeding — both keep reading the real
  `speciesData.EvolutionTier` unaffected. This is a preview/equip-testing tool for one screen, not
  a full simulate-a-different-tier-everywhere mode — kept narrow deliberately, per user's actual
  request ("play around unlocks and available skill slots to equip to").
- **Precedent:** Follows the same "debug tool, ships visible, not stripped from builds" convention
  already established by `DebugMovementPresetCycler`.
- **Date:** 2026-08-09
- **Revisit if:** A real evolution/tier-change system is built (Phase 4) — at that point this
  debug control's job is done by the real mechanic, though it likely stays useful for testing.

### [Creatures] 54 more placeholder SkillData assets — every GDD tree now has 5, not 2
- **Decided:** Generated `{TreeType}_Placeholder3/4/5.asset` for each of the 18 GDD trees (54 new
  assets, following the existing naming/field convention exactly — `_skillName`, `_description`,
  `_treeType`, incrementing `_placeholderIndex` to 2/3/4), registered into
  `SkillDatabase._allSkills`, and re-ran its `RebuildGuidIndex` context menu. 95 total registered
  skills (was 41).
- **Why:** User: "lets add more placeholders in the skill tree so we have a larger context of what
  it could look like at scale. You can just iterate up in numbers." The web view's 19-column grid
  looked sparse/lopsided against the old 2-per-tree count; a uniform 5-per-tree grid previews the
  concept much closer to the reference mockup's density.
- **Confirmed safe before generating anything:** Read `PlaceholderSkillResolver.cs` first —
  `PlaceholderIndex` isn't capped at 0/1, `GetStatusForSkill` already wraps it via modulo against
  each status category's option list (every category has 4+ members), so indices 2/3/4 resolve to
  valid, deterministic status flavors automatically, zero code changes needed. Damage trees (6 of
  18) are mechanically identical regardless of index by design, so extra copies there are just
  more equivalent options. No new design content invented — same as the existing 36.
  `WildSpawnSystemTests`/`SkillDatabaseTests` needed no changes despite the count change — both
  build fully synthetic, isolated fixtures already independent of the real asset count.
- **Date:** 2026-08-09
- **Revisit if:** Real skill content design happens (GDD §14) — these placeholders get replaced,
  not extended further.

### [Combat] equippedSkillGuids is sparse, not compact/front-packed
- **Decided:** An empty string (`""`) entry in `PhasixRuntimeData.equippedSkillGuids` now means
  "no skill in this physical slot." `SkillLoadoutSystem.TryEquipAt`/`SwapEquipped` land exactly at
  the target index (auto-extending the list with empty gaps as needed); `Unequip` clears its slot
  in place instead of `List.Remove`-by-value (which shifted every later entry down one position).
- **Why:** User: "when i add skills from the tree to the wheel it just adds it to the next open
  spot instead of where im dragging and dropping it to." The prior compact/front-packed
  design (`OnDragUp`'s own comment: "there's no real 'slot 4' independent of the list's current
  length") was a deliberate earlier decision, but the user is now explicitly rejecting that
  tradeoff — genuine positional drop semantics are what's expected. This was also the root cause
  of a THIRD report ("I don't have access to all slots at tier 5") — the cap itself was never
  actually broken, but physical positions beyond the front-packed block were unreachable via drag.
- **Why this didn't require touching the battle-side readers:** `BattleHUDController`,
  `BattleManager`, `BattleParticipant` all already resolve guids via
  `SkillDatabase.TryGetByGuid`, which already treats `""`/null as "not found" (existing, tested:
  `SkillDatabaseTests.EmptyOrNullGuidEntries_AreSkipped_DoNotThrow`) — sparse gaps are invisible
  to every reader, only the three mutation methods needed rework.
- **New invariant:** `equippedSkillGuids.Count` is no longer "how many skills are equipped" once
  gaps can exist — it's just how far the sparse list currently extends. Cap checks
  (`TryEquip`/`TryEquipAt`) now use a private `CountEquipped` (real non-empty count) instead.
  `WildSpawnSystem`'s own seeding logic is unaffected — it always builds a fresh, gap-free list via
  sequential `.Add()`, so its existing `.Count`-based cap check stays valid there.
- **Alternatives rejected:** Keeping the compact list and instead re-deriving "visual position" as
  a separate, independent field — rejected as needless indirection; making the storage itself
  positional is simpler and removes an entire class of desync bugs between "where it's stored" and
  "where it's shown."
- **Date:** 2026-08-09
- **Revisit if:** Never expected to reverse.

### [Creatures] Debug "Unlock All" toggle, and why the test species tree pool was widened
- **Decided:** `PhasixRuntimeData.DebugUnlockAllTrees` (bool, session-only) — an "Unlock All: ON/
  OFF" button in the skill web header. `SkillTreeUnlockSystem.GetEffectiveUnlockedTrees` checks it
  first, ahead of `DebugTierOverride`, returning all 18 GDD trees unconditionally when active.
  Separately, `Test_FireType.asset`'s `AvailableTreeTypes` was widened from 2 entries (Mirror,
  Reaction) to all 18 GDD trees.
- **Why both:** User: "I also see a total of 3 trees available even at the tier 5 debug view" —
  root cause was the test species only ever listing 2 of 18 GDD trees, so
  `AvailableTreeTypes.Take(GetTreeCount(tier))` could never return more than 2 no matter how high
  the debug tier went. Widening the test data fixes tier-scaled preview (up to 7 at T5). The user
  then separately asked "can we also have an unlock all debug so im able to see everything?" — a
  distinct, tier-independent "show literally everything" mode, not solved by the data widening
  alone (which is still capped by `GetTreeCount`).
- **Scope kept narrow, matching the earlier DebugTierOverride precedent:** Unlock All only affects
  which trees render as unlocked in the skill web / are equip-gate-passable — it does NOT bypass
  equip SLOT capacity (still governed by tier) and does not touch the real, save-persisted
  `unlockedTreeTypes`.
- **Date:** 2026-08-09
- **Revisit if:** Real species roster design happens (GDD §25) — `Test_FireType`'s widened tree
  list is placeholder scaffolding, not meant to imply every real species has all 18 trees.

### [Combat] SkillTreeColor — one shared color source for the skill web, equip wheel, AND battle skill ring
- **Decided:** New `Assets/Scripts/Combat/SkillTreeColor.cs` (`DisplayOrder`, `Get`/`GetByIndex`,
  `ApplyVisual`). `OverworldMenuController`'s skill web + equip wheel and
  `BattleHUDController.PopulateSkillRing` all call through it — none has an independent color
  scheme anymore.
- **Why:** Three color schemes existed at different points this session for the same concept: the
  web's per-tree procedural color (built first), the Party menu wheel's per-skill GUID-hash
  7-bucket palette (pre-existing, fixed to match the web — see the "master color source" entry
  above), and the battle ring's per-ring-POSITION 7-bucket palette (pre-existing, unrelated). User
  kept finding the next mismatch each time one pair was fixed: first "the color in the skill tree
  does not match... on the scroll wheel" (Party menu web vs. its own wheel), then "i want the
  skill wheel in skill tree menu to sync up with the battle scene" (Party menu vs. battle). Rather
  than fix pairs one at a time again, unified all three into one method so there's structurally
  nothing left to diverge.
- **Removed:** `BattleHUD.uss`'s `.skill-ring-color-0..6` rules and
  `BattleHUDController.SkillRingColorCount` — both fully dead once the battle ring switched to
  `SkillTreeColor.ApplyVisual`'s inline-style approach (the palette was procedural, not an
  enumerable class list, same reasoning as the original web-vs-wheel unification).
- **Hover text needed no equivalent unification** — `BattleHUDController.BuildSkillTooltipText`
  was already `public static` and already the shared source both the Party menu and battle called;
  confirmed via code research before assuming a second fix was needed here too.
- **Date:** 2026-08-09
- **Revisit if:** Never expected to reverse.

### [UI] HudTooltip screen-edge clamping
- **Decided:** `HudTooltip.PositionNear` now flips the tooltip to the LEFT of its anchor (instead
  of always placing it 8px to the right) whenever right-placement would exceed the panel's width,
  using the `.hud-tooltip` USS `max-width` (220px) as a pre-layout worst-case width estimate —
  real rendered width isn't knowable until a layout pass runs after `Show()` sets the label's
  text. Also clamps vertically against panel height.
- **Why:** User: "the text when hovering over the enemy HP, aura etc appears out of screen and
  should be on the left side." The enemy nameplate sits at the panel's right edge
  (`.status-list-enemy`), and the tooltip previously had zero screen-edge awareness at all.
- **Single fix point, multiple beneficiaries:** Every `HudTooltip` consumer routes through this one
  method — nameplate HP/Aura/Evo bars, status-effect icons, both skill rings, and the skill web's
  nodes all got the fix at once. This also explained a separate report ("the hover over for buffs/
  debuffs... does not exist currently") — that wiring already existed
  (`BattleHUDController.RefreshStatusIcons`/`RegisterHover`, an earlier 2026-08 session) but was
  invisible for the identical reason, anchored to the same right-edge nameplate.
- **Date:** 2026-08-09
- **Revisit if:** Never expected to reverse.
- **2026-08-09 follow-up — partially wrong, corrected below:** The "buffs/debuffs already worked"
  conclusion above turned out to be incomplete — verified by calling `HudTooltip.Show()` directly,
  which bypasses the real hover-event path. User reported it still didn't work after this fix
  shipped; the actual bug (status-icon label children swallowing the pick from their own parent)
  and the placement gap issue are both recorded in the next entry.

### [UI] HudTooltip re-snaps to real size after layout; status-icon hover fixed at its real root cause
- **Decided:** `HudTooltip.Show` now places an immediate first-frame estimate (worst-case 220px),
  then re-snaps to the label's actual resolved width/height via a one-shot `GeometryChangedEvent`.
  Separately, `BuildStatusIconSlot`'s (and preventively `BuildBuffIcon`'s) label/counter children
  now set `pickingMode = PickingMode.Ignore`, matching `BuildNameplateBarRow`'s existing pattern.
- **Why:** User: "the placement of the hover for the enemy is a little far from the left side" —
  the 220px worst-case estimate was also being used as the FINAL placement, not just the overflow
  check, leaving a large gap for short text. Separately: "buffs or debuffs on both player and
  enemy are both not showing up" — a real bug (absolutely-positioned label children swallowing the
  pick from their parent icon), previously reported as "already working" based on an incomplete
  test (`HudTooltip.Show()` called directly, which bypasses the real `PointerEnterEvent` path
  entirely — see `LESSONS_LEARNED.md` → [UI Toolkit] for the full diagnostic writeup, including
  why `IPanel.Pick()` via `execute_code` reflection turned out not to be trustworthy here either).
- **Date:** 2026-08-09
- **Revisit if:** Never expected to reverse.

### [Combat] SkillLabelFormatter — battle skill ring now uses the same short code as the Party menu
- **Decided:** New `Assets/Scripts/Combat/SkillLabelFormatter.cs` (`GetShortLabel(skill,
  database)`) is the one shared source for orb/node lettering. `BattleHUDController.
  PopulateSkillRing` now calls it instead of using `SkillData.SkillName` directly;
  `OverworldMenuController`'s own private copy of the same logic was removed in favor of this.
- **Why:** This was the actual root cause of the "2nd Phasix shows all descriptions" report
  chased across the two entries above — not a duplication bug at all. The battle skill ring
  displayed each equipped skill's FULL name as a permanent label; for a full 12-skill loadout,
  several long placeholder names visibly crowded/overlapped around the small clock-face orbs.
  User: "can we just make it in the battle scene that no names of skills should be there? only...
  during the hover over... and the letter that the skill has like C1, C2, etc." The Party menu's
  own skill web/wheel had already solved this identical problem in an earlier 2026-08 session —
  the battle scene's ring just never got the same treatment.
- **Verified live:** Force-equipped 12 real skills on the player's Phasix, started a real battle,
  read every orb's resolved label directly — all 12 are short 2-character codes, none the long
  original names. Screenshot confirms no overlap.
- **Date:** 2026-08-10
- **Revisit if:** Never expected to reverse.

### [UI] DEBUG: Add Party Member button spawns Test_SteamType, not the Fallback Starter species
- **Decided:** New `DebugAddPartyMemberButton` (sibling of `DebugNewGameButton` in `OverworldMenu.
  uxml`'s `DebugBar`) calls a new `OverworldMenuController.DebugAddPartyMember()` method that
  spawns via `WildSpawnSystem.CreateWildInstance(_debugPartyMemberSpecies, _skillDatabase)` then
  `PartySystem.Instance.AddToParty(...)`. `_debugPartyMemberSpecies` is a new serialized
  `PhasixData` field, assigned in the Inspector to `Test_SteamType.asset` — deliberately a
  different species than `GameManager`'s Fallback Starter (`Test_FireType`), so a debug-added
  member is visibly/mechanically distinct from the slot-0 starter when testing multi-Phasix
  screens side by side.
- **Why:** User: "can you add a debug where it says: new game to add a party member so i can test
  it out myself" — testing multi-Phasix scenarios (skill web, battle skill ring color/label parity,
  etc.) previously required actually winning a real capture in battle first. Spawns through the
  same real `WildSpawnSystem.CreateWildInstance` entry point every wild/captured creature uses
  (identical seeded `unlockedTreeTypes`/`learnedSkillGuids`/`equippedSkillGuids`), so it's a
  shortcut to a real state, not a hand-built fake one. No-ops with a console warning (mirrors
  `GameManager.SeedFallbackStarter`'s own missing-reference/full-party handling) rather than
  throwing if the party is already full.
- **Verified live:** Play mode — invoked the real button's registered click handler directly
  (not just the underlying method) at 1/3 party slots filled, confirmed it fills slot 1, then
  slot 2; at 3/3 confirmed it logs "party is full" and no-ops without throwing.
  `manage_ui render_ui` screenshot confirms both debug buttons render correctly stacked.
- **Date:** 2026-08-10
- **Revisit if:** Never expected to reverse — this is a debug-only affordance, not a progression
  system.

### [Combat] Pre-battle Flee/Engage prompt retired; auto-engage on contact, Flee moved into battle at ~80% success
- **Decided:** `WildEncounterCreature.OnTriggerEnter2D` now calls `HandleEngage` directly — no more
  `EncounterPromptController.Show(species, onFlee, onEngage)` choice. Fleeing instead lives inside
  `BattleScene_Main` itself: a new `FleeButton` opposite `EndTurnButton` (mirrored position/size,
  distinct blue-grey color), wired through `BattleHUDController.FleeClicked` →
  `BattleManager._fleeRequested`, resolved in `PlayerTurn` by rolling `BattleConfig.
  FleeSuccessChance` (0.8) once per click. Success ends the battle immediately via a new
  `BattleOutcome.Fled` (manual-outcome pattern, same as Capture's Won path) — no Aura reward, no
  summary screen, and a NEW `EventBus.OnBattleFled` event rather than reusing `OnBattleLost`
  (Fled must never trigger the future loss-state currency/item-cost handler CLAUDE.md's "Loss
  state" rule describes — fleeing is free). Failure still consumes the whole turn, same "uses the
  turn regardless of outcome" convention as every other single-beat move in `BattleConfig`.
- **Why:** User: "When interacting with a phasix instead of flee or engage, automatically engage
  into combat. Where we have an end turn button, on the opposite side have a flee button, lets
  make it like 80% success rate for now."
- **Regression found and fixed during live verification:** The retired prompt's guard
  (`EncounterPromptController.Instance.IsVisible`) turned out to double as a global "an encounter
  is in progress" lock spanning the WHOLE battle (`Show()` sets it, only `Resolve()`'s `Hide()`
  clears it — i.e. after the battle ends, not when the prompt closes), not just "is the prompt
  currently drawn." Removing it alongside the prompt silently dropped that lock: live-verified
  with 3 wild creatures in the test scene, a second creature's contact while the first battle was
  still running additively loaded `BattleScene_Main` a second time. Fixed with a new
  `WildEncounterCreature.s_encounterInProgress` (static bool, same lifetime as the old flag,
  decoupled from any UI element). Full writeup: `LESSONS_LEARNED.md` → [Combat & Encounter Flow].
- **Verified live:** Play mode — auto-engage confirmed (prompt never becomes visible, battle
  loads straight from contact); the new guard confirmed blocking a second concurrent contact (0
  extra scene loads); a seeded-`Random` forced FAILURE logs "Failed to flee!" and the enemy's turn
  plays out normally; a seeded-`Random` forced SUCCESS unloads the battle scene immediately, resets
  the encounter guard, and never shows `BattleSummaryController` (Won-only, correctly skipped).
  256/256 EditMode tests still passing.
- **Alternatives rejected:** Keeping the pre-battle prompt but adding a Flee-in-battle option too
  (redundant — the user's phrasing replaces the prompt, doesn't add to it); reusing
  `BattleOutcome.Lost`/`OnBattleLost` for a successful flee (would incorrectly wire a future loss
  penalty handler to a cost-free outcome).
- **Date:** 2026-08-10
- **Revisit if:** A real capture/escape-item system is designed later (§22, pending) — Flee's flat
  80% may need to become item- or stat-modified at that point, and `EncounterPromptController`
  (currently dead code, not yet deleted — see CHANGELOG's "Next" note) should be fully removed or
  repurposed in the same pass.

### [Items] Open note — Items/Economy (GDD §22) needs a real design pass before any item system is built
- **Found:** Planning for the Phase 3 close-out pass (enemy AI + combat audio/VFX) originally
  scoped an items-in-battle framework as part of the same session. Investigation before building
  anything found zero design backing for it anywhere: `Combat_Directive_v0_1_0.md` never mentions
  item usage in battle at all, not even as a scaffold, and `Assets/Data/Items/` is empty with no
  `Item`/`ItemData`/`Inventory` class anywhere in the codebase. The one item that IS named in the
  docs — the Signal Swap Item (GDD §16.3, "a swap item allows changing the active Signal type
  within the pool") — turned out to be explicitly tagged `PENDING` / "design work not yet started"
  itself, at GDD §22.2, listed as one of three flagship examples of undesigned economy content
  (alongside Force unlock items and Temper Cores). Building any version of this now — even the
  narrow Signal-swap case — would mean inventing what an item costs, where it's acquired, and how
  it's consumed, which is exactly what CLAUDE.md's pending-design rule exists to block. Items were
  dropped from that session's scope entirely; this note exists so the direction question isn't
  silently lost before a real design pass happens.
- **Recorded for later:** Before any item system can be built, needs a real answer from the user
  to: (1) what kinds of items exist — held/passive items vs. throwable/consumable battle items vs.
  key/quest items; (2) what each kind mechanically does; (3) whether they're battle-actions (a new
  move-wheel entry, alongside Attack/Charge/Heal/Regen/Capture) or overworld/prep-menu-only actions
  (more like changing a loadout between battles); (4) how they're acquired — this depends on the
  broader §22 economy design (drops, shops, crafting?), not just the item mechanics themselves. No
  implementation follows from this entry — it exists purely so this thread isn't lost before the
  design pass that resolves it.
- **Date:** 2026-08-10
- **Ref:** CLAUDE.md's "Economy and items (§22 pending)" scope note; GDD §22 banner ("Design work
  not yet started") and §22.2 "Signal Swap Item" (tagged `PENDING`); GDD §16.3.

### [Combat] Open note — Attack visual pattern variety, scoped but not yet built
- **Found:** User question mid-session, deferred while the Dodge/Parry timing-sync work (see the
  2026-08-10/11 CHANGELOG entries) took priority: right now every attack — regardless of skill,
  tree, or built-in move — uses the identical visual (one diamond shape, one straight-line
  trajectory, one speed formula), differing only by Primal-type tint color. No hook anywhere
  distinguishes "kinds" of attacks visually.
- **Recommended approach (not yet built):** derive pattern variety from data that's ALREADY locked
  rather than inventing per-skill flavor — `DamageCategory` (Physical vs. Elemental) is a real,
  decided distinction every damage-dealing skill already has via `PlaceholderSkillResolver`, so a
  first split keyed off that invents nothing new. Concretely:
  1. New enum (e.g. `AttackVisualPattern { Ranged, Melee }`), Physical -> Melee, Elemental ->
     Ranged as a fixed, deterministic mapping — same "algorithm is the content, not invented
     output" precedent `PlaceholderSkillResolver` itself already established.
  2. Thread the pattern through `CombatVfxController.LaunchProjectile`/`ComputeTravelDuration` and
     `BattleHUDController.LaunchSyncedProjectile`, so `BattleManager` can pass the right pattern per
     attack (resolved skill's `Category` for tree skills; the 5 built-ins need their own explicit
     mapping decided — Attack is the only one that currently launches a projectile at all).
  3. **Genuinely open design question, not yet decided:** does "Melee" mean a different
     PROJECTILE shape/trajectory (still a traveling visual, just faster/shorter), or does it mean
     NO projectile at all and the ATTACKER's own stage-creature briefly lunges toward the target
     and back instead? These are architecturally different builds — the first extends
     `CombatProjectileVisual`, the second needs an attacker-side animation entirely separate from
     the projectile pool. Needs a decision at the START of whichever session picks this up, not an
     assumption baked in partway through.
  4. Also worth folding in per the user's own earlier note: "different projectile speeds" as a
     data-driven parameter (not just shape) — `BattleConfig.ProjectileSpeed` is already a single
     flat placeholder; a pattern could carry its own speed multiplier. Keep the multi-hit/rhythm-
     attack future in mind architecturally (already noted in the 2026-08-10 CHANGELOG entry) so
     this doesn't need a rewrite once that's built.
  - **Explicitly out of scope:** per-skill-tree unique visuals, per-specific-skill visuals, any
    variety beyond what `DamageCategory`'s existing binary split can derive — those need real
    skill content decided first (skill design is still pending per CLAUDE.md), same reasoning as
    the `[Items]` note above.
- **Date:** 2026-08-11
- **Ref:** Session that built the timing-synced projectile/dissolve system this would extend
  (`CombatVfxController.cs`, `BattleHUDController.cs`, `CombatProjectileVisual.cs`).

### [Combat] Offense timing reworked to Good/Perfect tiers mirroring Dodge/Parry, Miss now punished
- **Decided:** The player's own attack timing check (`BattleHUDController.RunTimedInput`) no longer
  uses its own single tolerance with a cosmetic-only "perfect" sub-flash. It now shares Defend's
  own tolerance/window constants outright — `TimedInputConfig.DodgeToleranceHalfWidth`/
  `DodgeBaseWindowPercent` for the new "Good" tier, `ParryToleranceHalfWidth`/
  `ParryBaseWindowPercent` for the nested, tighter "Perfect" tier — rather than defining separate
  offense-specific copies of those numbers. New `BattleHUDController.OffenseOutcome { Miss, Good,
  Perfect }` enum (mirrors the existing `DefenseOutcome`) replaces the old
  `LastTimedInputSuccess`/`LastTimedInputWasPerfect` mutable state; those two properties still
  exist but are now computed read-only wrappers over `LastOffenseOutcome`; so `EventBus.
  Raise_TimedInputSuccess`, `BattleParticipant.RecordTimedInputPerfect`, and burst-fill logic
  needed no changes. Damage multiplier is now 3-tiered:
  `TimedInputConfig.MissDamageMultiplier = 0.5f` / `GoodDamageMultiplier = 1.0f` / 
  `PerfectDamageMultiplier = 2.0f`. A miss now applies a real damage penalty for the first time —
  previously it was multiplier 1x, i.e. no penalty at all.
  **Revised same day, second pass:** `GoodDamageMultiplier` was first set to 1.5f (renamed from
  `SuccessDamageMultiplier`, value unchanged from the pre-rework baseline) but the user clarified,
  after playtesting, that green/Good should read as genuinely "standard damage" — lowered to 1.0f
  so ONLY Perfect ever grants a bonus; Good's only job is guaranteeing you avoid the Miss penalty.
  Same pass also fixed `BattleLogFormatter.FormatAttack`/`FormatSkillAttack`, which had shipped
  still only checking a binary success bool — a Good hit was misleadingly logged as "timing was
  perfect!", and skill attacks logged no timing info at all. Both now take a
  `BattleHUDController.OffenseOutcome?` (null = no timing check ran, i.e. the Parry counter-attack)
  and only emit text for the two tiers that deviate from baseline (Perfect / Miss); Good and null
  stay silent by design, matching the new "Good = standard, no comment needed" values.
- **Why:** User-directed: "make the good same as the defend, and the perfect the same timing as
  the parry... trying to reward perfects and punish being bad at the game... reward players for
  being skilled." Reusing Dodge/Parry's own constants (rather than independently-tuned lookalikes)
  guarantees the two sides of combat stay identical by construction, with no drift risk between
  offense and defense difficulty. Punishing a Miss is a deliberate, explicit departure from
  defense's established "reward, don't punish" rule (see the Defense model decision above) — the
  user asked for offense specifically to punish poor timing, unlike defense.
- **Alternatives rejected:** Also rewarding Perfect hits with a bonus status effect (the user's
  "extra damage or extra status" phrasing left this open) — rejected for this pass because
  damage-dealing skills have no inherent status payload today (`PlaceholderSkillResolver`'s
  damage/status tree split is a hard 6-tree/12-tree divide with no damage-tree status field);
  inventing one now would mean picking arbitrary per-attack status content ahead of the real skill
  design pass. Left a `// TODO: pending design` marker at both `BattleManager` call sites instead
  of building a placeholder.
- **Date:** 2026-08-11
- **Revisit if:** Real skill content design happens and a "Perfect grants bonus status" hook
  becomes buildable without inventing content; or `NumericalCalibration.md` locks these multipliers
  to different values during a calibration pass.
- **Ref:** `TimedInputConfig.cs`, `BattleHUDController.cs` (`OffenseOutcome`, `RunTimedInput`),
  `BattleManager.cs` (`ResolveSkillAction`, `ResolveBuiltInMove`'s `Attack` case).

### [Combat] Parry counter-attack's damage now waits for the deflect projectile to actually land
- **Superseded same day, twice, before landing here:** this entry originally shipped as an ADDITIVE
  explicit flash call (`FlashStageElement`/`FlashStageCreatureHit`) placed next to the existing
  deflect-projectile flash rather than replacing it — which fixed the "no flash at all" symptom but
  caused a user-reported double-blink (both flashes fired for the same hit). A same-day patch then
  suppressed the deflect projectile's own arrival flash (`flashOnArrival: false`) to stop the
  double-blink, but that was still a patch on top of a patch: the projectile's launch and the
  counter's damage application were never actually coupled in time, they'd just been arranged to
  independently produce a single visible flash. User then asked directly for the real fix: "I need
  the damage to register the moment the projectile hits the target." Both intermediate mechanisms
  (`FlashStageElement`/`FlashStageCreatureHit`, `AnimateAndResolveImmediately`'s `flashOnArrival`
  flag) were fully removed once this final version landed — see CHANGELOG.md's three same-day
  entries for the blow-by-blow.
- **Decided (final):** `CombatVfxController.ResolveHeldProjectileAsParryDeflect` (and
  `BattleHUDController.ResolveParryDeflect`, pass-through) now returns the projectile's real travel
  duration instead of `void`. Its launch call moved out of its old early position (fired right on
  detecting Parry) down into `BattleManager.ResolveEnemyDamageAction`'s counter-attack section,
  immediately followed by `yield return new WaitForSeconds(deflectTravelDuration)` — the same
  "await the travel time, then apply damage" pattern `RunTimedInput`/`RunDefenseTimedInput` already
  use for every other damage-application path in this file. The projectile's own on-arrival flash
  is unchanged (always fires, no flag) and now lands inside that awaited window, so the projectile
  visually hitting its target, the flash, and the counter's HP-bar update are the same beat by
  construction, not by coincidence.
- **Why:** Every other damage-application path in this file (player Attack, player skill, enemy
  attack) awaits its own projectile/ring immediately before applying damage — the counter-attack
  was the one exception, since it has no timing check of its own to await, so nothing forced its
  visual and its damage into the same beat. This fix gives it the same awaited-duration treatment
  everything else already gets, rather than working around the symptom with a second flash call.
- **Leak-safety preserved:** the launch+await lives in its own `if (isParry)` block, ahead of (and
  not nested inside) the `attacker.IsAlive`-gated damage block — so the held projectile is still
  always resolved/released the instant Parry happens, regardless of whether the counter-attack
  itself ends up running. Confirmed `attacker.IsAlive` cannot actually be false at this point in the
  current single-enemy-battle flow (nothing between Parry detection and here can kill `attacker`),
  so this is a belt-and-suspenders guard, not a load-bearing one.
- **Date:** 2026-08-11
- **Revisit if:** A future multi-enemy battle structure makes `attacker.IsAlive` genuinely reachable
  as false at this point — re-check the leak-safety reasoning above still holds.
- **Ref:** `CombatVfxController.cs` (`ResolveHeldProjectileAsParryDeflect`), `BattleHUDController.cs`
  (`ResolveParryDeflect`), `BattleManager.cs` (`ResolveEnemyDamageAction`'s counter-attack block).
- **Corrected same day, once more:** the launch+await block above was placed right after the
  "defended!" `ShowTimedMessage` wait, which fixed the sync but meant the held projectile sat
  visibly stuck, idle-pulsing at the player's position, for the entire 1.5s of that message before
  it ever moved. User: "if I parry on success just have the attack bounce back" (immediately).
  Moved the whole `if (isParry) { launch+await; if (attacker.IsAlive) { counter damage } }` block
  back to right after `LogDefenseResult`/burst-fill — i.e. the moment Parry is detected, same spot
  the very original pre-2026-08-11 code always launched it from — and moved BOTH `ShowTimedMessage`
  calls ("defended!", "counter-attacks!") to after it instead of straddling it. `ShowTimedMessage`
  shares one UI element across calls (`_continuePrompt`), so they can't run concurrently with the
  action without corrupting each other's display — sequencing them strictly after was the only
  option compatible with that constraint. Net effect: the deflect now bounces back with zero
  perceptible delay, while the damage/flash sync from this entry's main decision is unchanged. A
  configurable "how long the projectile stays stuck" duration per parry type/quality was raised as
  a nice-to-have for later, not built now — flagged with a `// TODO: pending design` marker at the
  launch call rather than inventing numbers for it.

### [Combat] Battle log damage breakdown — base/type/timing, colored, temporary "for visibility" aid
- **Decided:** Every damage-log line (`BattleLogFormatter.FormatAttack`/`FormatSkillAttack`/
  `FormatDefenseOutcome`) now shows a `"(N base + delta type [+ delta timing]) = N total damage"`
  breakdown instead of just the final number. `DamageCalculator.ComputeBaseDamage` (new) returns
  the pre-type stat-ratio damage on its own; the type/timing deltas are computed as differences
  between already-rounded numbers (never independently re-derived), so the three terms always sum
  exactly to the shown total. Colors via UI Toolkit rich text (`TextElement.enableRichText` is on
  by default — verified via Unity docs, not scene-tested first): base white (`#FFFFFF`), increases
  green (`#5AC864`, same hex as `BattleHUDController.SuccessFlashColor`), decreases red (`#DC3C3C`,
  same hex as `MissFlashColor`) — deliberately the same palette as the ring flashes elsewhere in
  this HUD, not a new independent color choice.
- **Why:** User-directed, explicitly framed as temporary ("just for visibility rn") — a debug-style
  aid to see how the timing-multiplier rework's damage numbers are actually being built, not a
  permanent flavor-text addition. Reusing the existing flash palette keeps it visually consistent
  with the rest of combat feedback rather than introducing a third color meaning.
- **Scope:** The timing term is omitted entirely (not shown as "+0") for any log line where no
  timed-input check actually ran — the Parry counter-attack (no timing check on the counter itself)
  and any incoming enemy hit that lands (always flat 1x; only Dodge/Parry's full-avoidance is at
  stake for those, not a graduated multiplier like the player's own attack timing).
- **Date:** 2026-08-11
- **Revisit if:** This breakdown format needs to become permanent/polished UI rather than a
  temporary visibility aid — worth a real design pass on presentation (icons instead of "type"/
  "timing" text labels, etc.) at that point rather than extending this placeholder further.
- **Ref:** `DamageCalculator.cs` (`ComputeBaseDamage`, `ComputeStatRatio`), `BattleLogFormatter.cs`
  (`FormatDamageBreakdown`, `FormatDeltaTerm`), `BattleManager.cs` (all 4 damage-log call sites).

### [Combat] Lane occupancy — non-exclusive, in-lane visual spacing
- **Decided:** Multiple combatants may occupy the same lane simultaneously. When they do, they're
  visually spaced apart along the lane so they read as distinct occupants in a line rather than
  overlapping sprites. This is a rendering/layout rule only — targeting, movement, and collision
  still resolve against the lane index alone, per the existing center-anchor model.
- **Why:** Keeps the traversal/targeting model simple (still just 7 lane indices, no sub-lane slot
  system) while letting the 3–5-per-side party size actually fit across 7 lanes without forcing
  awkward one-combatant-per-lane placement rules. Adopted from `Attack_Pattern_Directive_v0_1_0.md`
  Part 8, which depends on this being locked for its pre-battle placement and zone-targeting design.
- **Alternatives rejected:** Exclusive occupancy (one combatant per lane) — would force an
  8th/9th lane or placement restrictions neither directive nor the GDD asks for, purely to avoid a
  visual-spacing problem that's cheaper to solve in layout code.
- **Date:** 2026-08-11
- **Revisit if:** Exact in-lane spacing values, once picked (NumericalCalibration.md), read as
  cramped at 5-per-side density — may need a soft per-lane occupant cap.
- **Ref:** `Combat_Directive_v0_1_0.md` Part 3, `Attack_Pattern_Directive_v0_1_0.md` Part 8.

### [Combat] Lane movement cost — context-decided, cost-agnostic traversal system
- **Decided:** No single fixed rule governs whether a lane-movement request costs an action turn.
  The calling context decides: a player-initiated reposition, a skill's Approach beat, and a
  reactive dodge can each carry a different cost. The traversal system itself stays cost-agnostic —
  it executes a movement request without knowing or caring why it was triggered.
- **Why:** A single fixed rule (e.g. "movement always costs an action") would be wrong for at least
  one real use case already in scope — an authored Approach beat inside a melee Beat Sequence isn't
  a separate player choice, so charging it as its own action doesn't make sense, while a player
  choosing to reposition outside of any skill plausibly should cost something. Adopted from
  `Attack_Pattern_Directive_v0_1_0.md` Part 8, which needs this settled to build Approach/
  return-to-origin movement without also inventing action-economy rules it doesn't own.
- **Alternatives rejected:** One fixed cost rule for all lane movement — simpler, but the Combat
  Directive's own Part 5 (Action Economy) already treats actions as build-scaling resources, and a
  flat movement tax doesn't compose cleanly with that.
- **Date:** 2026-08-11
- **Revisit if:** Exact per-movement-type cost values (pending numerical calibration) turn out to
  need a shared baseline after all, once the beat-sequence runtime exists to playtest against.
- **Ref:** `Combat_Directive_v0_1_0.md` Part 3, `Attack_Pattern_Directive_v0_1_0.md` Part 8.

### [Combat] Melee Beat Sequences — fully committed once started, no interrupt (design capture only)
- **Decided:** A melee attack's authored beat sequence (Approach/Windup-Real/Windup-Fake/Attack),
  once its first beat begins, always plays to completion. There's no voluntary bail-out and no
  external interrupt — Root/Stun on the attacker and Reaction (Type E) skills do not cut a sequence
  short. A sequence's only two outcomes are: it runs its full authored beat list, or it never starts
  (e.g. the attacker was already Stunned before its turn, an existing, separate mechanic under the
  locked Status system). Automatic return-to-origin fires unconditionally after the final Attack
  beat, returning to whatever lane was recorded once at sequence start — not the lane before the
  most recent Approach.
- **Why:** Simplifies the state machine to a linear/looping structure (no interrupt branch to model
  or test) and makes sequence outcomes predictable for both the player reading tells and the
  designer authoring beat lists. Captured in `Attack_Pattern_Directive_v0_1_0.md` Part 7 /
  `melee_beat_sequence.mermaid` ahead of any runtime implementation — this is a design decision, the
  beat-sequence system itself is not built yet.
- **Alternatives rejected:** None recorded — no prior directive or DECISIONS.md entry ever
  addressed melee-sequence interruptibility, so this is a first decision, not a reversal of one.
- **Date:** 2026-08-11
- **Revisit if:** Type E (Reaction) skill content needs a real trigger point before Phase 5 —
  currently open (`Attack_Pattern_Directive_v0_1_0.md` Part 7's flagged gap / Part 10 item 1): with
  Approach no longer interruptible, Reaction has no described moment to fire against a melee
  sequence. Needs a decision before Type E melee content can be authored.
- **Ref:** `Attack_Pattern_Directive_v0_1_0.md` Part 7 & Part 10, `melee_beat_sequence.mermaid`.

### [Tweening] DOTween actually imported — March 2026 entry was aspirational, not real
- **Decided:** The March 2026 `[Tweening]` entry above ("Decided: DOTween (free version)... Revisit
  if: Never") was never actually acted on — confirmed via `Packages/manifest.json` and a full-project
  grep for `DG.Tweening` returning zero `.cs` hits prior to this session. DOTween is now genuinely
  imported (Demigiant, free — Asset Store listing title is "DOTween (HOTween v2)," HOTween v2 being
  DOTween's own internal/legacy name, not a separate library), needed for the new melee Beat Sequence
  animations (Approach/Windup/Attack/Return, see `Attack_Pattern_Directive_v0_1_0.md` Part 7).
- **Why:** The battle stage is 100% UI Toolkit (`VisualElement.style`), not `Transform`/`SpriteRenderer`
  — there was nothing for the old "aspirational" entry's intended `.DOMove()`/`.DOScale()` shortcuts to
  ever act on, which may be part of why it was never followed up on. The actual import landed as a
  precompiled `DOTween.dll` (not loose `.cs` source) at `Assets/Plugins/Demigiant/DOTween/`, with a
  companion `DOTweenModuleUIToolkit.cs` helper module — directly relevant since tweens here target
  `VisualElement.style` via the generic `DOTween.To(getter, setter, endValue, duration)` overload, not
  the Transform-based one-liners.
- **Verified:** No `.asmdef` was generated under `Assets/Plugins/Demigiant/`. No reference-array edit
  was needed for `Phasix.Runtime.asmdef` — it has `"overrideReferences": false`, so Unity auto-includes
  every precompiled DLL in the project automatically. `Phasix.Tests.EditMode.asmdef` DOES use
  `"overrideReferences": true` with an explicit `precompiledReferences` allowlist (previously just
  `nunit.framework.dll`) — `"DOTween.dll"` was added to that array this session so a future EditMode
  test can reference `DG.Tweening` directly without hitting a confusing "type or namespace not found"
  error. Confirmed via `read_console`: project compiles clean post-import (only pre-existing, unrelated
  warnings).
- **Alternatives rejected:** None re-litigated — DOTween itself was never in question, only whether it
  was actually present, which it wasn't until now.
- **Date:** 2026-08-11
- **Revisit if:** A future tween needs a Transform/SpriteRenderer target instead of `VisualElement`
  (e.g. if the battle stage ever moves off UI Toolkit) — the shortcut extension methods would become
  usable directly at that point instead of the custom `.To()` wrapper.
- **Ref:** `Assets/Plugins/Demigiant/DOTween/`, `Assets/Scripts/Phasix.Runtime.asmdef`,
  `Assets/Tests/EditMode/Phasix.Tests.EditMode.asmdef`.
