# Phasix — Lessons Learned
Issues that required significant investigation to resolve. Read before debugging similar problems.

---

## Format
```
### [System] Issue title
- **Symptom:** What was observed
- **Root cause:** What actually caused it
- **Fix:** What resolved it
- **Date:** When resolved
```

---

## Physics & Colliders

### [Physics] Player Rigidbody2D ejected on Play start
- **Symptom:** Mr_chimken shot out of the room in a random direction the moment Play was hit. Happened regardless of spawn position and direction. Turning off all wall colliders did not stop it.
- **Root cause:** `RoomBounds` PolygonCollider2D was set as a solid collider (Is Trigger = false). On Play, the physics engine detected Mr_chimken's Rigidbody2D overlapping the solid polygon boundary and resolved the overlap by ejecting him.
- **Fix:** Set **Is Trigger = true** on the RoomBounds PolygonCollider2D. Cinemachine Confiner2D only reads the collider shape — it does not require a solid collider. The trigger flag removes it from physics simulation entirely.
- **Date:** April 2026
- **Key rule:** Any collider used exclusively for camera confinement (CinemachineConfiner2D) must always be **Is Trigger = true**.

### [Physics] Manual collision test showed the player passing through the companion — twice — and both times the test was wrong, not the collision setup
- **Symptom:** Wrote a script-driven physics test (using manual `Physics2D.Simulate()`, per the earlier Play-Mode-doesn't-tick workaround) to confirm the player would be physically blocked by the companion. First attempt: player sailed straight through, ending up on the far side. Disabling `PlayerController_SideScroll` first (to rule out its own `FixedUpdate` fighting the test) made no difference — still passed through.
- **Root cause (two separate test-authoring mistakes, not a real bug):**
  1. `Mr_chimken`'s `CapsuleCollider2D` has a local `offset` of `(0, 14)` — after the character's `(0.1, 0.1, 0.1)` scale, that's +1.4 world units above the root Transform (a bone-rigged torso collider, not centered on the feet-pivot root). Test code positioned both objects' *root transforms* at the same Y, assuming that meant the colliders were vertically aligned — they were actually 1.4 units apart and never overlapped no matter how close the roots got horizontally.
  2. After fixing that and repositioning objects again, `Collider2D.bounds` was read immediately after the `transform.position` assignment, with no intervening physics step — `bounds` doesn't recompute synchronously on a bare Transform write, so it returned stale data from a previous test's end state, silently producing a bogus starting distance for the next test.
- **Fix:** For any manual/script-driven Collider2D test: (1) always check `Collider2D.bounds.center`, not `transform.position`, when reasoning about actual overlap distance — a rigged/asymmetric collider's effective center can be meaningfully offset from its root; (2) call `Physics2D.SyncTransforms()` immediately after any manual `transform.position` change, before reading `Collider2D.bounds`, or you'll read leftover values from before the move.
- **Date:** July 2026
- **Key rule:** The underlying game systems here were fine both times — this was a case of a flawed verification script producing a false negative. When a physics test result looks wrong, verify the *test's own* position/bounds assumptions (via `bounds.center` post-`SyncTransforms()`) before concluding the game logic is broken.

### [Physics] Physics2D.simulationMode = Script is a global, persistent PROJECT setting — not a per-call test toggle
- **Symptom:** After a testing session using `Physics2D.simulationMode =
  SimulationMode2D.Script` (to manually drive `Physics2D.Simulate()` for deterministic
  verification), `git status` showed `ProjectSettings/Physics2DSettings.asset` modified.
  Left as `Script` mode, Unity would stop automatically stepping 2D physics every
  `FixedUpdate` at all — silently breaking all real Rigidbody2D-based movement in actual
  play, not just the object being tested.
- **Root cause:** `Physics2D.simulationMode` is a global project setting
  (`m_SimulationMode` in `Physics2DSettings.asset`), not a scoped/local flag — setting it
  from a test script changes it for the whole project, persistently, until something sets
  it back. It's easy to reach for this to get deterministic manual-stepping control during
  a scripted test (needed earlier this session for `Rigidbody2D.MovePosition` verification)
  and forget it's a standing project-wide change, not something that reverts on its own.
- **Fix:** Explicitly restore `Physics2D.simulationMode = SimulationMode2D.FixedUpdate`
  after any test that changes it. Caught this time by reviewing `git status`/`git diff` on
  `ProjectSettings/*.asset` before reporting work as done — not by noticing broken gameplay
  directly.
- **Date:** July 2026
- **Key rule:** Any test that sets `Physics2D.simulationMode` (or similar global engine
  settings) MUST restore the original value before finishing, and it's worth a
  `git status`/`git diff` sanity check on `ProjectSettings/*.asset` after any session that
  did low-level engine-state manipulation via `execute_code` — an unintended global setting
  change won't show up as a script/console error, only as a silent project settings diff.

### [Physics] "Is it closing in?" dot-product check had the comparison sign backwards
- **Symptom:** `CompanionAI`'s personal-space repel (nudge away from the player when they're
  very close and approaching) did the exact opposite in every test: it repelled when the
  player was moving *away*, and stayed inert when the player was actually closing in.
- **Root cause:** `awayFromPlayer` points from the player toward the companion. If the
  player's movement direction (`playerDelta`) has a *positive* dot product with that vector,
  the player is moving in roughly the same direction as "toward the companion" — i.e.
  closing in. The check was written as `Dot(...) < 0f`, backwards from that.
- **Fix:** `Dot(playerDelta.normalized, awayFromPlayer.normalized) > 0f`. Caught by testing
  the method in isolation (via reflection) against known approach angles with a clearly
  predicted expected sign for each, rather than trusting the logic by inspection.
- **Date:** July 2026
- **Key rule:** For any "is A moving toward B" dot-product check, sanity-test with one
  concrete, easy-to-eyeball case (e.g., B directly east of A, A moving east) before trusting
  the sign — it's very easy to get backwards, and the bug produces confidently-wrong
  behavior in 100% of cases, not an edge-case glitch, so a single test case exposes it fully.

### [Physics] Unflattened Z noise on a "2D" Transform silently corrupts normalized direction math
- **Symptom:** In a real (user-driven) play session, `CompanionAI`'s follow/repel behavior
  effectively stopped working — the companion barely moved and didn't step away from the
  player. Live inspection found `_smoothedTargetDirection` sitting at
  `(-0.51, -0.01, -0.86)` — a huge Z component for a value that's supposed to represent a
  2D top-down movement direction.
- **Root cause:** `CompanionAI` computed its direction/repel math directly from
  `Transform.position` deltas without ever constraining them to the XY plane. Confirmed the
  player's own movement script (`Rigidbody2D.linearVelocity`, a `Vector2`) and Animator
  (`applyRootMotion = false`) don't touch Z — so wherever the Z noise actually originates
  (not conclusively pinned down; candidates include floating-point drift, IK solver
  precision, or something else touching the root Transform), the real lesson is that the
  *consuming* code had no defense against it at all. A `Vector3.normalized` call on a
  mostly-flat vector is extremely sensitive to a small Z component — it can swing the
  resulting direction far from what X/Y alone would suggest — and because this particular
  direction is smoothed frame-to-frame (`RotateTowards`), one bad sample keeps poisoning the
  result for a long stretch afterward rather than self-correcting next frame.
- **Fix:** Explicitly flatten to XY (`new Vector3(v.x, v.y, 0f)`) at the point of computing
  any delta/direction vector that's about to be normalized, in a script that's meant to be
  purely 2D. Don't rely on the source Transform "should" only ever have Z=0 — verify by
  actually flattening in the consumer, since confirming the true upstream source can be a
  much deeper investigation than the two-line defensive fix.
- **Date:** July 2026
- **Key rule:** In a 2D top-down project, any gameplay script doing `.normalized` on a
  position delta should explicitly flatten to XY first, even when the source Transform is
  "supposed to" stay at Z=0 — small, hard-to-trace Z noise from any source becomes
  wildly disproportionate once normalized, and smoothed/blended direction values let one
  bad frame corrupt many frames afterward.

### [Physics] Spawning two colliders exactly coincident causes a large, compounding separation push
- **Symptom:** User screen-recorded starting the game with zero input and watched the
  companion visibly push the player around from the very first moment.
- **Root cause:** `PartySystem.EnsureCompanionInstance()` instantiated the companion at
  `_playerTransform.position` — the exact same point as the player, full 100% collider
  overlap at spawn. Reproduced directly: with the player's own `PlayerController_SideScroll`
  fully active and correctly re-asserting zero velocity every `FixedUpdate` (i.e. NOT a
  test-methodology gap — this was checked first), the player still drifted ~4-6 world units
  over roughly 2 seconds before settling, purely from the initial full-overlap separation
  compounding with the companion's own active path-following.
- **Fix:** Spawn the companion at an explicit offset from the target
  (`PartySystem._spawnOffset`, default `(0, -1.2, 0)`) that's comfortably larger than the
  combined collider radii (~0.8 here), never exactly coincident.
- **Date:** July 2026
- **Key rule:** Never `Instantiate` a solid (non-trigger) collider at the exact same
  position as another solid collider it will immediately need to separate from — even a
  "temporary" full overlap produces an outsized physics response, and if either object has
  its own active movement logic, that response can compound instead of resolving cleanly
  in one step. Always spawn with a deliberate offset comfortably beyond both objects'
  combined collision radii.

### [Physics] A position-delta-based "which way is X moving" signal creates a feedback loop when X can also be externally displaced
- **Symptom:** Companion follow/repel logic derived the player's movement direction from
  `Transform.position` deltas frame-to-frame. In live play, this meant that any time the
  companion's own collider nudged the player (even a barely-visible amount), the resulting
  tiny position shift got picked up as "the player moved," which updated the companion's
  trailing/repel direction, which moved the companion again, which nudged the player again
  — a self-sustaining loop requiring zero real player input to continue indefinitely.
- **Root cause:** `.normalized()` doesn't care about a vector's magnitude, only its
  direction — a millimeter-scale involuntary nudge produces an equally "confident"
  full-strength direction signal as a deliberate WASD keystroke. Position is fundamentally
  the wrong signal to read for "player intent" in any scene where the tracked object can
  also be moved by forces other than its own input (physics collisions, knockback, etc.).
- **Fix:** Read the target's actual `Rigidbody2D.linearVelocity` instead. As long as the
  target's own control script re-asserts its intended velocity every `FixedUpdate`
  (`PlayerController_SideScroll` does — see its `FixedUpdate`), velocity reflects real
  control intent and self-corrects within one physics tick after any external disturbance,
  unlike raw position which just... changed, permanently, with no notion of "was that
  intentional."
- **Date:** July 2026
- **Key rule:** For "is the target intentionally moving, and which way" logic, prefer
  reading velocity (ideally from the target's own control script's asserted/intended
  value) over raw position deltas whenever the target can be displaced by anything other
  than its own will — physics knockback, other scripts, cutscenes, etc. Position deltas
  conflate "moved on purpose" with "moved for any reason at all."

### [Physics] Hiding a sprite around a Rigidbody2D.MovePosition teleport does not stop Rigidbody2D Interpolate from visibly smoothing the jump
- **Symptom:** `CompanionAI`'s new Blink pattern was meant to instantly teleport the companion.
  First fix attempt hid the sprite (`SpriteRenderer.enabled = false`) for a short window, moved
  it via `Rigidbody2D.MovePosition`, then showed it again — but in live testing it still
  visibly "dashed"/eased between the old and new position, indistinguishable from a fast
  `DashThrough`. Polling `transform.position` during testing showed correct, discontinuous
  values the whole time, giving false confidence the fix worked.
- **Root cause:** the companion's `Rigidbody2D` has `Interpolate` enabled (needed for every
  other movement pattern's smooth motion). Interpolation blends the RENDERED transform between
  the position recorded at the previous `FixedUpdate` and the position recorded at the current
  one, over the following render frame(s) — it has no concept of "this was a teleport, don't
  smooth it." Making the sprite visible again in the *same* `FixedUpdate` tick as the
  `MovePosition` call does nothing to prevent this, because the interpolation blend is a
  property of the Rigidbody2D's internal render-transform bookkeeping, not of when the
  `SpriteRenderer` happens to be enabled. Critically, `Transform.position` always reads the
  true, instantaneous physics position — it is never the interpolated value Unity actually
  draws to screen — so verifying via position polling alone cannot catch this class of bug at
  all; only an actual rendered-frame capture (Game View screenshot) revealed it.
- **Fix:** set `Rigidbody2D.interpolation = RigidbodyInterpolation2D.None` for the entire
  duration the teleporting pattern is active (`ApplyMovementPreset()` toggles it based on
  `_pattern`), restoring `.Interpolate` for every other pattern. Verified via actual Game View
  screenshots across a blink cycle (not code reasoning, not position polling) — visible → fully
  hidden with no ghost/streak → visible again at the new spot, zero travel in between.
- **Date:** August 2026
- **Key rule:** Any script that teleports (as opposed to smoothly moves) a Rigidbody2D must
  explicitly manage `.interpolation` — set it to `None` for the teleport, restore
  `.Interpolate` otherwise — rather than trying to hide the visual artifact around a
  `MovePosition` call. And when verifying a rendering-smoothness fix, poll the actual rendered
  frame (a screenshot), not `Transform.position` — interpolation is invisible to the latter by
  design.

---

## Tilemap

### [Tilemap] TilemapCollider2D "Used By Composite" option missing in Unity 6
- **Symptom:** Inspector for TilemapCollider2D had no "Used By Composite" checkbox as documented in older Unity tutorials.
- **Root cause:** Unity 6 renamed and redesigned this field. The checkbox was replaced by a dropdown.
- **Fix:** Set **Composite Operation** dropdown to **Merge** on the TilemapCollider2D component.
- **Date:** April 2026
- **Key rule:** Always query Context7 for Unity 6000.x docs before writing Inspector instructions — do not rely on training data or older tutorials.

---

## Cinemachine

### [Cinemachine] CinemachineRotationComposer not needed for 2D
- **Symptom:** Follow Camera prefab created by GameObject → Cinemachine → Targeted Cameras → Follow Camera includes a CinemachineRotationComposer component by default, which is not needed for 2D top-down games.
- **Root cause:** The Follow Camera template is designed for 3D games.
- **Fix:** Remove CinemachineRotationComposer component after creating the Follow Camera. Leave CinemachineFollow only.
- **Date:** April 2026

---

## 2D IK (LimbSolver2D)

### [IK] LimbSolver2D set up via C# produces no bone movement
- **Symptom:** `IKManager2D` + `LimbSolver2D` added programmatically. All validity checks pass (`chain.isValid=True`, `allChainsHaveTargets=True`). Moving the IK target in Scene view does nothing. Calling `UpdateIK(float)` and `DoUpdateIK(List<Vector3>)` via reflection also produces zero bone rotation change.
- **Root cause 1 — Zero-length tip bone:** Tip Transform created as child of `forearm_R` at `localPosition=(0,0,0)`. `LimbSolver2D` computes bone lengths as world-space distances between chain transforms. A zero-length second segment (`forearm→tip = 0 units`) silently breaks the analytical solve — no error, no output.
- **Root cause 2 — Wrong `UpdateIK` overload:** `UpdateIK(float globalWeight)` silently fails when `solveFromDefaultPose=true` and `StoreLocalRotations()` was never called (stored rotations default to `Quaternion.identity`, producing a degenerate restore→solve cycle). The overload `UpdateIK(List<Vector3> targetPositions, float globalWeight)` — passing target world positions explicitly — works correctly.
- **Root cause 3 — Edit mode disabled:** `IKManager2D.runInEditMode` defaults to `false`. Moving targets in Scene view has no effect until entering Play mode or explicitly setting `runInEditMode = true`.
- **Fix:**
  1. **Non-zero tip offset** — set `IK_Tip.localPosition = new Vector3(forearm.localPosition.x, 0, 0)` so tip is offset by the same length as the upper arm bone.
  2. **`solveFromDefaultPose = false`** on all `LimbSolver2D` components created via code.
  3. **`manager.runInEditMode = true`** on `IKManager2D` for Edit mode preview.
  4. Re-call `chain.Initialize()` after repositioning tip bones so `chain.lengths` recomputes.
- **Date:** April 2026
- **Checklist for any future IK setup via code:**
  - `chain.lengths` — both values must be `> 0` after `Initialize()`, else tip bone has zero offset
  - `solveFromDefaultPose = false` unless you've explicitly called `StoreLocalRotations()` first
  - `runInEditMode = true` on `IKManager2D` for Edit mode preview
  - `LimbSolver2D` always resolves to `transformCount=3` after `Initialize()`: `[shoulder, forearm, tip]` — expected, not a bug
  - Use `UpdateIK(List<Vector3>, float)` overload when forcing solves from code

---

## Art & Assets

### [Assets] Craftpix tower defense tileset (305231) is not a tile grid
- **Symptom:** Pack appeared to be a tileset but tiles could not be used in Unity Tile Palette.
- **Root cause:** The pack contains two types of content — (1) pre-composed full-scene background layers (land, road, river, decor) and (2) isometric path tiles (TAILS folder). Neither is a top-down terrain tile grid compatible with Unity's Rectangular Tilemap.
- **Fix:** Use the background layers as stacked SpriteRenderer GameObjects for backdrop art. Use TAILS tiles as decorative props. Source a separate top-down terrain tile grid PNG for the Tilemap.
- **Date:** April 2026

### [Assets] Craftpix monster packs (341189, 437811, 168163) are frame-by-frame, not sprite sheets
- **Symptom:** Monster sprites are individual PNG files, not sprite sheets.
- **Root cause:** Craftpix packages individual animation frames as separate PNGs rather than a single sprite sheet.
- **Fix:** Import individual PNGs directly. Build AnimationClips by dragging frames into the Animation window in sequence. No TexturePacker or Fresco step needed.
- **Date:** April 2026
- **Key rule:** Do NOT assemble these into a PSD bone rig — they are pre-animated frame-by-frame sprites, not rigs.

---

## A* Pathfinding Project

### [Pathfinding] Configuring AstarPath/GridGraph via script immediately after AddComponent throws NullReferenceException
- **Symptom:** `gameObject.AddComponent<AstarPath>()` followed immediately by `astar.data.AddGraph(typeof(GridGraph))` threw `NullReferenceException` — first on `astar.data` itself being `null`, then (after manually assigning `astar.data = new AstarData()`) inside `AddGraph` at the `graphTypes.Length` check.
- **Root cause:** `AstarPath.Awake()` is what actually initializes `data` and populates `data.graphTypes` (via `FindGraphTypes()`) — and `Awake()` doesn't run synchronously the instant a component is added via an external Editor script/reflection call outside Unity's own lifecycle timing. Constructing a bare `new AstarData()` by hand skips that initialization entirely, and `AddGraph` unconditionally loops over `graphTypes` with no null guard.
- **Fix:** After `AddComponent<AstarPath>()`, force initialization explicitly rather than assuming Unity has called it yet: invoke `Awake()` directly via reflection (`typeof(AstarPath).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).Invoke(astar, null)`), then also call `astar.data.FindGraphTypes()` directly before `AddGraph` — don't rely on `Awake()` alone having done it.
- **Date:** July 2026

### [Pathfinding] AIPath applies fake gravity by default — falls/sinks with no Rigidbody or explicit zero
- **Symptom:** A companion GameObject using `Seeker`+`AIPath` (no Rigidbody2D) visibly sank
  downward over time in the Scene, independent of any pathfinding/following logic.
- **Root cause:** `AIBase.gravity` defaults to `new Vector3(float.NaN, float.NaN, float.NaN)`
  — a sentinel meaning "use `Physics.gravity`" (Unity's 3D gravity, `(0,-9.81,0)` by
  default). Per `AIBase.cs`: gravity is only skipped if `gravity == Vector3.zero` OR a
  **non-kinematic** Rigidbody/Rigidbody2D is attached (letting the real physics engine
  handle gravity instead). A Kinematic Rigidbody2D does NOT suppress it — only Dynamic
  does, or an explicit zero.
- **Fix:** For any purely 2D top-down agent with no vertical axis at all, explicitly set
  `aiPath.gravity = Vector3.zero`. Don't rely on adding *any* Rigidbody2D to suppress it —
  only a non-kinematic one would, and a Kinematic body (the usual choice for an
  AI-pathed agent) still needs the explicit zero.
- **Date:** July 2026
- **Key rule:** Always set `gravity = Vector3.zero` explicitly on `AIPath`/`AIBase` for 2D
  top-down games — don't assume `is2D`/`orientation = YAxisForward` on the graph implies
  gravity is off too; it's a completely separate field with its own default.

### [Pathfinding] RigidbodyType2D enum order is Dynamic=0, Kinematic=1, Static=2 — easy to get backwards
- **Symptom:** Set `Rigidbody2D.bodyType = 2` intending Kinematic (for an AI-pathed
  companion). Read the component back afterward and it reported `bodyType: Static`, not
  Kinematic.
- **Root cause:** `RigidbodyType2D`'s declared field order is `Dynamic, Kinematic, Static`
  — so the integer values are Dynamic=0, Kinematic=1, Static=2. Assumed 2=Kinematic without
  checking; it's actually 1.
- **Fix:** Verified via `unity_reflect get_type UnityEngine.RigidbodyType2D` before
  correcting to `bodyType = 1`. Confirmed via component read-back afterward, not assumed.
- **Date:** July 2026
- **Key rule:** Never assume a Unity enum's underlying integer values from memory — a
  handful of common ones (`SortingLayer` values are unrelated per-project data, not an enum,
  so that one's always fine; but built-in engine enums like this one are worth a quick
  `unity_reflect get_type` check) before setting them by raw int.

### [Pathfinding] Rigidbody2D.MovePosition (Kinematic) needs an actual physics step — manual `FinalizeMovement` calls alone don't move it
- **Symptom:** After adding a Kinematic `Rigidbody2D` to an `AIPath`-driven companion (so
  AIPath would move it via `Rigidbody2D.MovePosition` instead of the raw Transform, for
  consistency with the player's own Rigidbody2D-based structure), the previously-working
  manual verification technique (repeatedly calling `aiPath.MovementUpdate(...)` +
  `aiPath.FinalizeMovement(...)` in a loop — see the Play-Mode-doesn't-tick entry below)
  stopped moving the GameObject at all. Position stayed frozen across 60 manual iterations.
- **Root cause:** Once a Rigidbody2D is present, `FinalizeMovement` calls
  `Rigidbody2D.MovePosition()` internally instead of writing `transform.position` directly.
  `MovePosition` on any Rigidbody2D (Kinematic included) only takes effect during the next
  actual Physics2D simulation step — it does not update the transform synchronously the way
  a direct assignment does. Since nothing in this environment ticks real physics steps
  either (same underlying cause as the Play-Mode-frame-ticking entry below), the queued
  moves never got resolved.
- **Fix:** Call `Physics2D.Simulate(deltaTime)` after each manual
  `FinalizeMovement` — set `Physics2D.simulationMode = SimulationMode2D.Script` once, then
  drive `Physics2D.Simulate(1f/60f)` alongside the existing manual movement-stepping loop.
  This resolves the queued `MovePosition` synchronously, the same way `BlockUntilCalculated`
  resolves a queued path.
- **Date:** July 2026
- **Key rule:** Any time a script-driven agent gains a Rigidbody2D, manual verification
  needs a manual physics step too (`Physics2D.Simulate`) — not just manually invoking the
  script's own Update-equivalent methods.

### [Pathfinding] Manually-scanned GridGraph vanishes on the next domain reload
- **Symptom:** Scanned a `GridGraph` successfully (2280 nodes, 1576 walkable) via script, everything worked in that same session — but after the next `refresh_unity` compile/domain reload, `astar.data.graphs.Length == 0` and `gridGraph == null`. Console showed `Caught exception when loading from zip` from `JsonSerializer.cs` during the reload.
- **Root cause:** A*'s graph data round-trips through a serialized byte blob (`AstarData.data_cachedStartup`), not plain Unity field serialization. Configuring a `GridGraph` purely via runtime C# mutation (as opposed to using the actual AstarPath Inspector GUI, which calls the serialize step for you after every edit) never populates that blob, so there's nothing valid for the next domain reload to restore from.
- **Fix:** After scanning, explicitly persist it: `astar.data.cacheStartup = true; var bytes = astar.data.SerializeGraphs(); astar.data.SetData(bytes);` then `EditorUtility.SetDirty` + save the scene.
- **Correction (same day, later in the same session):** The fix note above originally
  continued by recommending `cacheStartup = false` + `scanOnStartup = true` instead, reasoning
  that a cached bake would go stale against `WorldChunkManager`'s dynamic chunk toggling. That
  reasoning about staleness is still correct, but the specific recommendation was wrong: with
  `cacheStartup = false`, **the graph's configuration itself doesn't persist either** — not
  just node walkability data. A domain reload silently reverted a fully-configured GridGraph
  (`cutCorners = false`, `collision.diameter = 2`) back to library defaults
  (`cutCorners = true`, `diameter = 1`), because without a cached blob to deserialize from,
  `AstarData.graphs` has nothing to restore *at all*. The actually-correct combination: keep
  `cacheStartup = true` (so the configured graph — dimensions, collision mask, diameter,
  cutCorners, everything — reliably survives) **and** `scanOnStartup = true` (so the
  walkability *data* is always freshly recomputed against whatever's actually active at
  startup, addressing the original staleness concern without losing the configuration).
- **Date:** July 2026
- **Key rule:** Don't assume a runtime-configured AstarPath survives a domain reload just because `EditorUtility.SetDirty` was called — A*'s graphs need their own explicit serialize step (`SerializeGraphs`/`SetData`) with `cacheStartup = true`. Setting `cacheStartup = false` to avoid a stale bake throws out the configuration along with the node data — use `scanOnStartup = true` for freshness instead, not `cacheStartup = false`. Verify persistence claims about this library by forcing an actual domain reload and re-reading the values back — don't assume from a single successful save.

### [Pathfinding] GraphCollision.diameter is scaled by nodeSize — not an absolute world-unit value
- **Symptom:** Set `collision.diameter = 1` expecting a 1-world-unit collision check
  diameter. A companion using the resulting `GridGraph` visibly ended up standing on top of
  a decoration/stump collider that should have been marked unwalkable.
- **Root cause:** Read `GraphCollision.Check()` and `GridGenerator.cs` directly rather than
  guessing further. `GraphCollision.Initialize(transform, scale)` is called as
  `collision.Initialize(transform, nodeSize)` — i.e. `scale` in
  `finalRadius = diameter * scale * 0.5f` is the graph's `nodeSize`, not `1`. With
  `nodeSize = 0.5`, `diameter = 1` produced a real check radius of only `0.25` world units —
  small enough to miss decoration colliders that don't perfectly fill their tile cell or
  sit slightly off the grid's own node centers.
- **Fix:** Treat `diameter` as "how many node-widths of clearance," not world units. Derive
  it as `desiredWorldDiameter / nodeSize`. Verified empirically against all 96 painted
  Decorations tiles across several candidate values — `diameter = 2` (real radius `0.5`) was
  the smallest value producing zero misclassified obstacle tiles for this scene; higher
  values only eroded walkable area further with no further correctness benefit.
- **Date:** July 2026
- **Key rule:** Never assume an A* Pathfinding Project numeric field is in plain world
  units — several (like this one) are scaled by `nodeSize` internally. When a collision/size
  value looks like it should be simple, grep the actual source
  (`Assets/AstarPathfindingProject/Generators/Base.cs` and `GridGenerator.cs` for
  `GraphCollision`) before assuming, and verify empirically against real scene geometry
  (e.g. checking every painted obstacle tile's nearest-node walkability), not just a raw
  node walkable/unwalkable count.

### [Pathfinding] GridGraph's default `cutCorners = true` lets a sized agent clip through corners it can't fit through
- **Symptom:** Companion following the player would visibly get stuck/jitter in the same
  specific spots — always right around a decoration object, never in open floor.
- **Root cause:** `cutCorners = true` (GridGraph's default) permits a path to connect two
  diagonally-adjacent nodes even when both of their shared cardinal neighbors are blocked —
  a diagonal "corner clip" that assumes a zero-size point agent. `AIPath.radius` here is
  `0.4` (real, non-zero) — a gap the grid's connectivity considers valid can still be
  physically too tight for the agent, producing jittery, stuck-looking movement exactly at
  obstacle corners.
- **Fix:** Set `cutCorners = false` on the GridGraph and rescan.
- **Date:** July 2026
- **Key rule:** Any GridGraph used by an agent with a real non-zero `radius` should have
  `cutCorners = false` — the default assumes a point agent and will produce corner-clipping
  artifacts for anything larger.

---

## Tooling

### [Tooling] CoplayDev unity-mcp migration — four stacked blockers before the bridge connected
- **Symptom:** After installing the `com.coplaydev.unity-mcp` package and clicking "Configure All Detected Clients," Claude Code still had zero working connection to Unity — no errors surfaced anywhere obvious.
- **Root cause (four separate issues, each hiding the next):**
  1. Claude Code was never in the "detected clients" list — its `ClaudeCodeConfigurator` shells out to the `claude` CLI binary, which isn't on PATH in this desktop-app-hosted setup. `.mcp.json` has to be hand-edited instead of relying on the auto-configure button.
  2. Project `.claude/settings.json` only allow-listed `context7` in `enabledMcpjsonServers` — any `.mcp.json` server not in that array is silently never loaded, regardless of how correct the entry is.
  3. The package's default HTTP transport (`http://127.0.0.1:8080`) collided with an unrelated pre-existing Windows service (`ApplicationWebServer.exe`) already bound to port 8080 — Unity's HTTP listener never actually came up, but the UI didn't make that obvious.
  4. The manually-written stdio command used the wrong package identifier — `mcp-for-unity` is the *executable/tool name*, not the PyPI package. The install source must be `--from mcpforunityserver==<version>` (see `AssetPathUtility.GetUvxCommandParts()` in the package source) with `mcp-for-unity` only as the trailing arg.
  Even after all of that, Unity's own Transport dropdown (Window → MCP for Unity) still defaulted to "HTTP Local" and needed to be switched to "Stdio" to match, then required one manual "Start Session" click for the already-open Editor session (stdio auto-starts via `[InitializeOnLoad]` on every subsequent Editor launch — this was a one-time nudge, not a recurring step).
- **Fix:** `.mcp.json` entry: `{"command": "uvx", "args": ["--from", "mcpforunityserver==10.1.0", "mcp-for-unity", "--transport", "stdio"], "type": "stdio"}`. Add `"unity-mcp"` to `enabledMcpjsonServers` in `.claude/settings.json`. Set Unity's MCP for Unity window Transport to Stdio.
- **Date:** July 2026
- **Key rule:** When a CoplayDev/unity-mcp client isn't in "detected clients," don't assume the package is broken — check whether the client's detector depends on a CLI binary being on PATH, and hand-edit `.mcp.json` instead. Always cross-check `.claude/settings.json`'s `enabledMcpjsonServers` array whenever a project-scoped MCP server silently fails to load.

### [Tooling] Unity domain reload silently doesn't complete while the Editor window lacks focus
- **Symptom:** After creating new scripts via `create_script` and requesting a compile via `refresh_unity`, `read_console` showed zero compile errors — but the new types were completely invisible to `unity_reflect` search and `manage_scriptable_object create` failed with `type_not_found`, even after repeated forced refreshes.
- **Root cause:** Two stacked issues, the second only discoverable by first ruling out the first:
  1. `mcpforunity://editor/state`'s `compilation.last_domain_reload_after_unix_ms` timestamp never advanced across multiple compiles, even though `last_compile_finished_unix_ms` kept updating — meaning compilation was succeeding but the domain reload that actually loads new types into the running AppDomain wasn't happening. `editor.is_focused` was `false` at the time; Unity can defer the post-compile domain reload while its window isn't in focus.
  2. Even after bringing the Editor into focus, the same thing kept happening — because the project's *entire* default assembly (no assembly definition files split it) still had a real compile error elsewhere (`EventBus.cs` referencing a still-undefined `BattleResult` type). A single compile error anywhere in an assembly blocks the *whole* assembly from reloading, not just the file with the error — so `read_console` correctly showed "only that one error," but that one error was enough to prevent every new type in the same assembly (including ones with zero errors of their own) from ever loading.
- **Fix:** Ask the user to click into/focus the Unity Editor window (resolves issue 1). Create a trivial empty stub for any type still blocking full-assembly compilation, even if it's out of scope for the current task — the project's own "Stub rule" (in `Evolution_System_Directive_v1_1_0.md`) explicitly sanctions this: forward-referenced types only need to exist, not be fully implemented, for the rest of the assembly to compile.
- **Date:** July 2026
- **Key rule:** `read_console` showing "only N pre-existing errors" is not proof the rest of your new code loaded successfully — in a project with no assembly definition splitting, any error anywhere blocks domain reload for everything. Cross-check with `unity_reflect search` (or an actual asset-creation attempt) before trusting a clean-looking error diff. Also check `editor.is_focused` in `mcpforunity://editor/state` before assuming a stuck domain reload is a code problem.

### [Tooling] SceneView.RepaintAll() called from inside OnDrawGizmosSelected does not chain into continuous repaints
- **Symptom:** `CompanionAI`'s new per-pattern Scene-view gizmos (Orbit/HiddenShadow/Blink)
  still looked "frozen at a fixed/wrong point" even after adding a `SceneView.RepaintAll()`
  call at the end of `OnDrawGizmosSelected()` specifically to fix that. A single Play-mode
  screenshot taken after the fix looked correct (gizmo centered on the player) — but a user
  screen recording of a full ~8-second Play session showed the gizmo sitting at its very first
  drawn position the entire clip, while the companion visibly moved to several different spots
  in the Game View over that same time.
- **Root cause:** `OnDrawGizmosSelected()` only runs as a *result* of some repaint already
  happening — requesting another repaint from inside it is a weak, circular trigger, not a
  driver of continuous updates. It depends entirely on how often an *external* repaint happens
  to occur, which — unrelated to but compounding this — is itself throttled while the Scene
  tab doesn't have focus (same underlying Editor-throttling theme as the domain-reload entry
  above, but this manifests through the Gizmos/Handles draw pass specifically, and applies to
  a real human developer working in another window too, not just MCP automation). A single
  still screenshot can't distinguish "correctly positioned right now" from "frozen since the
  last real repaint, which happened to be correct at that instant" — only a sequence of
  captures over time, or a live recording, actually tests for staleness.
- **Fix:** Subscribe a repaint-request method to `EditorApplication.update` instead (in
  `OnEnable`, unsubscribed in `OnDisable`, both `#if UNITY_EDITOR`-guarded) — a genuine
  per-Editor-tick callback independent of whether some other repaint happened to occur
  elsewhere. Gate it (selected + relevant pattern) so it doesn't force needless repaints when
  irrelevant. Verified via a *sequence* of Scene View screenshots taken seconds apart — confirmed
  the gizmo's drawn position visibly changed between consecutive captures.
- **Date:** August 2026
- **Key rule:** Never assume `SceneView.RepaintAll()` called from within a gizmo-drawing
  callback will keep that gizmo live — it won't, reliably. Drive continuous Editor-only visual
  updates from `EditorApplication.update` instead. And when verifying "does this update live,"
  a single screenshot only proves correctness at one instant — capture a sequence over real
  time (or ask for/watch a recording) before trusting a live-update claim.

### [Tooling] OnDrawGizmosSelected requires manual Hierarchy selection — normal Play testing never selects anything, so custom gizmos silently never ran, and a third-party base class's own always-on gizmo was mistaken for "still broken"
- **Symptom:** Even after the repaint fix above, the user reported the pattern gizmos "still
  not working" — cycling `DebugMovementPresetCycler` presets while just watching the game
  showed no correct paths for Orbit/HiddenShadow/Blink, and it looked like "still showing the
  following pathing gizmo."
- **Root cause (two compounding issues, the second only visible once the first was ruled out):**
  1. `CompanionAI`'s pattern gizmos were implemented as `OnDrawGizmosSelected` — Unity only
     invokes this when the specific GameObject is selected in the Hierarchy. Normal Play-mode
     testing (pressing Tab to cycle presets, watching the Game/Scene view) never does that, so
     the gizmos weren't stale or mispositioned — they were never running at all. Every previous
     verification pass in this session had manually set `Selection.activeGameObject` via
     `execute_code` before screenshotting, which silently worked around this and never actually
     tested the real user workflow.
  2. What the user WAS seeing wasn't "our old gizmo" — it was A* Pathfinding Project's own
     `AIBase.OnDrawGizmos()` (unconditional, no selection required), which draws a blue circle
     at `aiPath.destination` whenever that field isn't its positive-infinity "unset" sentinel.
     Since Orbit/HiddenShadow/Blink bypass `AIPath` entirely and never write to `destination`,
     it stayed frozen at whatever the last trailing-point pattern (Direct/Wavy/DashThrough/
     StopAndGo) had left it at — reading exactly like a leftover "following" path gizmo,
     because that's precisely what it was, just from a different (third-party, always-on)
     script than the one being debugged.
- **Fix:** switched the custom gizmos to plain `OnDrawGizmos` (always-on) — safe here since
  only one companion instance ever exists (`PartySystem.EnsureCompanionInstance()`), so there's
  no multi-object clutter concern that `OnDrawGizmosSelected` normally guards against.
  Additionally reset `aiPath.destination` to `Vector3.positiveInfinity` whenever a pattern
  bypasses AIPath, suppressing the third-party gizmo's stale marker instead of leaving it to
  confuse future debugging. Re-verified this time by deliberately leaving `Selection` pointed
  at an unrelated object throughout the whole test.
- **Date:** August 2026
- **Key rule:** `OnDrawGizmosSelected` is invisible during any workflow that doesn't manually
  select the object — verify custom gizmos with selection deliberately left elsewhere, not
  just deliberately set to the object under test, or a verification pass can pass while the
  real user-facing behavior is completely broken. Also: when a gizmo looks "stuck showing old
  behavior" in a project using a third-party pathfinding/AI package, check whether that
  package's own base class draws an unconditional gizmo of its own — it's easy to mistake a
  well-behaved third-party debug visual for a broken one you just wrote.
- **Addendum (found later, same session):** A* Pathfinding Project actually has a THIRD
  always-on gizmo in this family, on a different component — `Seeker.OnDrawGizmos()`
  (`Pathfinding/Core/AI/Seeker.cs`) draws a solid green line along `lastCompletedVectorPath`
  (the last path it ever actually calculated), gated only by its own public `drawGizmos` bool.
  It has nothing to do with `AIPath.destination` — resetting that field (the fix above) does
  NOT clear this; the path list just sits there until a new path is calculated, which
  Orbit/HiddenShadow/Blink never do. Also caused "old paths still showing" after switching to
  one of them. Fix: also toggle `seeker.drawGizmos = aiPath.canMove` whenever `canMove` changes.
  **Broader rule this confirms:** a single third-party AI/pathfinding package can have MULTIPLE
  independent always-on gizmo sources across different components (here: two, on `AIBase` and
  `Seeker`) — finding and silencing one doesn't mean you've found all of them; grep the
  package's source for `OnDrawGizmos` (not just `OnDrawGizmosSelected`) broadly before
  declaring "stale gizmo" issues fully resolved.

### [Tooling] Custom gizmo color accidentally matched a third-party base class's own always-on gizmo color
- **Symptom:** After fixing the two issues above (wrong callback, stale destination marker),
  the user still reported the Blink target reticle "showing wrong" / "position inverted."
  Every position-based check (reflection reads of `_blinkNextDestination` vs. the companion's
  actual transform position, `HandleUtility.WorldToGUIPoint` math) said the reticle's
  coordinates were correct.
- **Root cause:** not a position bug at all — a color clash. The reticle's color
  `(1, 0.85, 0.2)` was nearly identical to A* Pathfinding Project's own
  `AIBase.ShapeGizmoColor` (`(0.94, 0.84, 0.12)`, `Pathfinding/Core/AI/AIBase.cs`), which that
  base class draws unconditionally at the companion's CURRENT position for every pattern,
  always (see the entry above). Two same-colored circular markers on screen — one permanently
  at the old position, one at the new — is trivially misread as "wrong position" or "wrong
  order" even though each one, individually, was exactly correct.
- **Fix:** changed the reticle to bright magenta, unmistakably distinct from AIBase's
  yellow-gold at any zoom level.
- **Verification technique that finally nailed this down precisely** (worth reusing for any
  future "is this gizmo actually in the right place" doubt): don't trust position-only
  reflection reads or approximate pixel-counting against grid lines — both left room for doubt
  here. Instead: (1) `Time.timeScale = 0f` after confirming the state of interest via
  reflection, freezing the simulation so there's no risk of the state changing mid-screenshot
  regardless of how much real wall-clock time verification takes; (2) drop temporary, brightly
  colored marker GameObjects (`GameObject.CreatePrimitive(PrimitiveType.Sphere)`, oversized,
  one distinct color per point of interest) at the exact world coordinates being compared
  (e.g. one at `_blinkNextDestination`, one at the companion's actual current position); (3)
  manually set `SceneView.lastActiveSceneView.pivot`/`.size` for a clean, wide, reproducible
  framing instead of relying on `view_target` auto-zoom, which repeatedly produced
  inconsistent, overly-tight crops that were hard to read; (4) screenshot and visually confirm
  the gizmo aligns with the correct marker — this is unambiguous in a way no amount of
  coordinate math or grid-line-counting was. Always destroy the temporary markers and restore
  `Time.timeScale = 1f` afterward.
- **Date:** August 2026
- **Key rule:** When a custom Gizmos color is chosen, check it against colors any already-
  imported third-party package's own gizmos use (especially ones that draw unconditionally, not
  just on selection) — a near-identical color on an unrelated always-on marker can look
  exactly like your own gizmo malfunctioning. And when position correctness is in doubt, drop
  an actual colored marker object at the exact coordinate rather than reasoning about pixel
  offsets against grid lines — freeze `Time.timeScale` first if there's any risk of state
  drifting before the screenshot lands.
- **Addendum — `Time.timeScale = 0` + manually reflection-invoking `FixedUpdate()` reveals
  script-level state perfectly, but NOT `Rigidbody2D.MovePosition`'s effect:** while verifying
  a later fix (Blink's 2-deep destination queue), the same freeze-and-manually-step technique
  produced an apparent false negative — `transform.position` after a manually-invoked
  `MoveAlongBlink()` teleport call did not match the expected destination at all. Root cause:
  `Rigidbody2D.MovePosition()` only ever QUEUES a move to be applied on the next real physics
  step — with `Time.timeScale = 0`, that step never runs, so `transform.position` never catches
  up, even though the correct call already happened and every private field was already
  correctly updated. Confirmed by manually stepping physics once
  (`Physics2D.simulationMode = SimulationMode2D.Script; Physics2D.Simulate(0.02f);`) — the
  Transform immediately snapped to the exact expected position. **Always restore
  `Physics2D.simulationMode = SimulationMode2D.FixedUpdate` afterward** (see the earlier
  "Physics2D.simulationMode is a global persistent PROJECT setting" entry above — same
  footgun). When frozen-time verification involves anything that moves a Rigidbody2D via
  `MovePosition`/`MovePositionAndRotation`, check the field-level state directly via reflection
  first — that's unaffected by physics stepping — before trusting `transform.position` as proof
  of anything.

### [Tooling] Play Mode doesn't tick frames at all while the Editor window is unfocused (MCP automation)
- **Symptom:** Entered Play Mode via `manage_editor(action="play")`, added a party member, moved the player's Transform via `execute_code`, waited several real seconds (both `Bash sleep` between calls and `Thread.Sleep` inside a single call), then checked — `Time.frameCount` was still `1` or `2`. The companion GameObject never moved, `AIPath.destination` read back as `(Infinity, Infinity, Infinity)` (its "never been set" sentinel), and `AIPath.hasPath` stayed `false` even after manually calling `SearchPath()` and sleeping.
- **Root cause:** Unity's Player Loop (`MonoBehaviour.Update`, `FixedUpdate`, and the async path-request delivery A* Pathfinding Project relies on) simply does not tick when the Editor process has no OS focus/visibility in this automation environment — regardless of `Application.runInBackground` / `PlayerSettings.runInBackground` (tried both; no effect). Calling `EditorApplication.QueuePlayerLoopUpdate()` in a loop from inside a synchronous `execute_code` call does not help either — it only *schedules* a tick for whenever the main thread goes idle, but that same call is what's currently occupying the main thread, so it can never actually run before the call returns. This is a distinct issue from the already-documented "domain reload doesn't complete unfocused" entry above — that one is about compilation/domain reload; this one is about Play Mode's per-frame simulation loop.
- **Fix — bypass automatic ticking entirely for verification, don't fight it:**
  1. Any per-frame *decision logic* in your own script (state machines, distance checks, etc.) can be verified by invoking the private `Update()`/`FixedUpdate()` method directly via reflection (`typeof(T).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, null)`) — this proved `CompanionAI`'s state/destination computation was correct long before movement itself could be tested.
  2. A* Pathfinding Project specifically: build and solve a path synchronously with `var path = ABPath.Construct(from, to, null); AstarPath.StartPath(path); path.BlockUntilCalculated();` (this genuinely blocks and completes without needing frame ticks — proven both for a raw path request and for feeding it into an `AIPath` via `aiPath.SetPath(path)`).
  3. To prove actual movement (not just pathing), manually step `AIBase`'s own movement API in a loop: `aiPath.MovementUpdate(1f/60f, out var nextPos, out var nextRot); aiPath.FinalizeMovement(nextPos, nextRot);` — 60 iterations at a 1/60s delta reliably simulates ~1 second of real gameplay movement synchronously.
  4. None of this workaround is needed for an actual human playtesting the game in a focused Editor window or a build — standard Play Mode ticking works completely normally there. It's purely a constraint of driving Unity headlessly through this specific automation path.
- **Date:** July 2026
- **Key rule:** If `Time.frameCount` isn't advancing across separate MCP tool calls despite real wall-clock time passing, stop waiting longer — it will never tick on its own in this environment. Drive the specific subsystem synchronously instead (reflection-invoke your own `Update()`, and for A* Pathfinding Project specifically, `BlockUntilCalculated()` + manual `MovementUpdate`/`FinalizeMovement` stepping).
- **Correction (August 2026, Wild Encounter session):** The "it will never tick on its own"
  claim above is not universally true — in at least one later session, automatic ticking
  (`OnTriggerEnter2D` firing from real physics, `EncounterPromptController.Awake()` running,
  `AstarPath`'s `scanOnStartup` scan completing) all eventually happened correctly on their
  own, with no manual reflection-stepping needed, once enough real wall-clock time had passed
  after `manage_editor(action="play")`. The actual trap: querying state (`X.Instance`,
  `AstarPath.active`, a `MonoBehaviour`'s private fields) **immediately** after entering Play
  reliably returns nulls/defaults for the first several seconds — `mcpforunity://editor/state`
  reports `play_mode.is_changing: true` and `activity.phase: "playmode_transition"` the whole
  time this is settling. This produced a real false alarm: `AstarPath.active == null` and "no
  graphs in the scene" errors immediately post-Play looked exactly like a missing/broken
  `AstarPath` setup, but were purely this settle delay — the same scene's `AstarPath` GameObject
  (see `[Pathfinding]` entries above) had a fully valid, previously-baked `GridGraph` the whole
  time; re-checking a short while later (a few more MCP round-trips' worth of real time, no
  manual stepping) found `AstarPath.active` populated with `graphCount=1` correctly. **Do not
  manually reflection-invoke `Awake()` to "fix" an apparent null right after entering Play** —
  doing so on `EncounterPromptController` in this same session caused a *second*, genuine bug:
  the real automatic `Awake()` fired later anyway, re-running `Hide()` and wiping state that had
  already been set correctly in between (a UI prompt showing then silently resetting to hidden
  for no visible reason). The correct fix is patience, not forcing early initialization by hand.
- **Revised key rule:** Before concluding any singleton/`.active`/static-initialized state is
  genuinely broken immediately after entering Play, first re-check `mcpforunity://editor/state`
  for `play_mode.is_changing == false` (or just retry the same read-only query once or twice
  more with real time between calls) — don't manually force-run `Awake()`/initialization via
  reflection as a workaround, and don't conclude "never ticks" from a check made in the first
  few seconds after `play`.

### [Tooling] Unity MCP stdio connection doesn't survive Unity being closed/reopened mid-session
- **Symptom:** After closing and reopening the Unity Editor while a Claude Code session kept running, `unity-mcp` tools either disappeared from the tool list entirely, or Unity's own "MCP for Unity" window showed a stale "No Session" (red) indicator even when the connection was actually working fine.
- **Root cause:** Two separate things. (1) The transport is stdio — the bridge process is spawned once when the Claude Code session starts and is piped directly to that one specific Unity process. When Unity closes, that pipe dies; the already-running bridge doesn't retry against a new Unity instance when Unity reopens — a fresh Claude Code session is required to spawn a new bridge process. (2) Independently, Unity's "MCP for Unity" window doesn't reliably repaint its Session indicator even after a new client successfully connects — the visual "No Session" state can lag behind the real, working connection.
- **Fix:** If Unity gets closed and reopened mid-session, restart the Claude Code app too (fully quit, reopen) — don't just retry tool calls and expect it to reconnect. Best practice: open Unity first, before starting the Claude Code session, and leave it running for the whole session — then this never comes up. To check connection state, trust a live tool call (e.g. `read_console`, `manage_scene get_active`) over Unity's own Session indicator.
- **Date:** July 2026
- **Key rule:** Unity closing/reopening mid-session always means restart Claude Code too, not just Unity. Never trust the "MCP for Unity" window's Session indicator alone — verify with an actual tool call before concluding the connection is up or down.
