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
