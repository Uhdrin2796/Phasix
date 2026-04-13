# Phasix — Development Changelog
One entry per session. Format: `[DATE] vX.X — What was built / decided / changed.`
Kept in version control. Claude Code reads this to avoid re-litigating settled work.

---

## Format
```
[YYYY-MM-DD] Phase X — Brief title
- Built: [what scripts/systems/assets were created]
- Decided: [any implementation choices made]
- Changed: [anything revised from prior plan]
- Blocked: [anything that couldn't be completed and why]
- Next: [what the next session should pick up]
```

---

## Log

[2026-04-12] Phase 1 Wk 7–8 — Asset organisation + Tilemap + Cinemachine (in progress)
- Built: `Assets/Scripts/World/WorldChunkManager.cs` — chunk activation/deactivation by player proximity via coroutine (not Update); _activateRadius=30, _deactivateRadius=40, _checkInterval=0.5s
- Built: `Assets/Artwork/Characters/` — player/companion rig subfolders moved here (Dark Fluffy, Dark Uhdrin, MercuryVI, mr_bot, mr_chimken_new, mr_chimken_obs)
- Built: `Assets/Artwork/Creatures/Pack_A_2DMonsters/` — craftpix-341189; 10 monsters renamed Monster_01–10; non-Unity source files in _SourceFiles/
- Built: `Assets/Artwork/Creatures/Pack_B_MonsterEnemies/` — craftpix-437811; same structure
- Built: `Assets/Artwork/Creatures/Pack_C_TowerDefense/` — craftpix-168163; 10 monsters renamed Monster_01–10
- Decided: Cinemachine 3.1.x — CinemachineCamera + CinemachineConfiner2D + CinemachinePixelPerfect extension
- Decided: Placeholder tiles for test room — real tileset PNG not yet sourced; swap in later without script changes
- Decided: Pixel Perfect Camera PPU = 16 locked (320×180 reference resolution)
- Decided: craftpix-305231 tileset pack = layered background art (not tile grid); use as SpriteRenderer layers
- Deleted: `__MACOSX` junk folder from craftpix-305231 pack
- Built: Placeholder tiles `S_Ground_Placeholder` + `S_Wall_Placeholder` in `Assets/Tiles/` (Unity built-in square sprites, green + grey)
- Built: `WorldPalette` Tile Palette in `Assets/Tiles/`
- Built: `Chunk_0_0` with Grid → Ground + Walls tilemaps; Sorting Layer: Ground, Orders 0 and 1
- Built: Walls tilemap → TilemapCollider2D + CompositeCollider2D (Composite Operation: Merge, Rigidbody2D Static)
- Built: `RoomBounds` PolygonCollider2D (Is Trigger: true) — 28×18 unit room centered on (0,0) — for CinemachineConfiner2D
- Built: Pixel Perfect Camera on Main Camera — PPU 16, 320×180, Pixel Snapping
- Built: CinemachineCamera (3.1.6) with CinemachineFollow + CinemachineConfiner2D → RoomBounds; tracking Mr_chimken
- Built: `WorldManager` GameObject with WorldChunkManager — Player: Mr_chimken, Chunk: Chunk_0_0
- Decided: RoomBounds PolygonCollider2D must be Is Trigger = true — solid collider ejects player Rigidbody2D on spawn
- Decided: Wall tiles block player movement via TilemapCollider2D; RoomBounds is trigger-only for camera confinement
- Decided: Orthographic size left at 5.625 (correct baseline for 320×180 at 16 PPU)
- Decided: Assets/Artwork/Creatures/ and Assets/Artwork/Tilesets/ excluded from git — store in Google Drive/OneDrive locally
- Next: Source proper top-down terrain tileset PNG; repaint room with real tiles; lock tile pixel size in DECISIONS.md; derive A* cell size

[2026-04-12] Phase 1 Wk 5–6 — mr_chimken player controller + animation flip
- Built: `Assets/Scripts/Player/PlayerController_SideScroll.cs` — new script replacing 8-directional PlayerController for mr_chimken
  - 4-directional movement (unchanged physics — accel/decel, Rigidbody2D, new Input System)
  - Left/right sprite flip via `transform.localScale.x` negation on root (correct for bone rigs — SpriteRenderer.flipX only flips one sprite part)
  - `_rigFacesRight` Inspector bool — set per character based on native art orientation; no code change needed on import
  - `_pixelsPerUnit` Inspector int — camera-level PPU (default 16, matches Pixel Perfect Camera when added)
  - `_targetHeightPixels` Inspector float — set character height in virtual canvas pixels (320×180); auto-scales uniformly via CapsuleCollider2D native height; restores manual scale when set back to 0
  - `OnValidate` — scale applies immediately in Edit mode without entering Play mode
  - `LogDimensions` context menu — right-click component → logs native size, current scale, world size, and target pixel height to Console
  - Original `PlayerController.cs` kept untouched as legacy fallback
- Built: `Assets/Animations/Creatures/MrChimken/MrChimken.controller` — configured with IsMoving Bool param, Idle (default) and Moving states, instant transitions
- Built: `Assets/Animations/Creatures/MrChimken/moving.anim` — converted from Legacy to Mecanim, assigned to Moving state
- Changed: mr_chimken scene GameObject — removed Legacy Animation component, removed old PlayerController, added Animator + PlayerController_SideScroll, deleted stray Mr_chimken (1) duplicate
- Decided: Bone-rigged characters flip via root `transform.localScale.x` negation, not `SpriteRenderer.flipX`
- Decided: Vector/PSD bone rigs import at 100 PPU; pixel art tiles at 16 PPU (matches camera PPU)
- Decided: Camera PPU = 16 (to be set on Pixel Perfect Camera in Wk 7–8)
- Next: Phase 1 Wk 7–8 — Tilemap world + Cinemachine camera; update DECISIONS.md with tile base size and A* cell size

[2026-04-11] Doc Sync — Evolution Directive, Combat Directive, World Design update
- Built: Evolution_System_Directive_v1_1_0.pdf → Assets/Docs/ (new — supersedes GDD §3, primary evolution authority)
- Built: Combat_Directive_v0_1_0.md → Assets/Docs/ (new — 7-lane combat stage, action commands, turn structure)
- Changed: WorldDesign_Directive_v0_1_0.md — added Parts 7–9 (Blackout/Banking, Perspective/Rig, Narrative Arc)
- Changed: DECISIONS.md — updated World structure entry (single Hub → Multiple Hubs); appended 3 new April 2026 entries (Blackout/Banking, Perspective model, Bone rig)
- Changed: DOCUMENT_INDEX.md — updated to v1.1.0; added Evolution and Combat Directives; updated hierarchy; updated Superseded section
- Changed: CLAUDE.md — updated evolution authority reference (Primer §9 → Evolution Directive); fixed fusion scope (T6/T7 only → all tiers, same-tier required only for T6+); added Multiple Hubs to world description; added Blackout/Banking rules; added Scripts/Evolution/ folder; updated Reference Files block
- Changed: ClaudeCode_Primer_v1_1_0.md §5 — devolution cost line updated (free, not Specific Aura cost); §9 — fusion scope corrected
- Changed: Progression_Directive_v0_1_0.md — added supersession note to Devolution Aura Cost section (Evolution Directive wins: devolution is free)
- Changed: NumericalCalibration.md — added Combat System Values section (lane depth scales, action command timing, damage modifiers)
- Decided: Devolution is FREE per Evolution_System_Directive — supersedes Progression_Directive devolution cost rule
- Decided: Multiple Hubs (not single Hub) — each with functional specialization
- Decided: Blackout/Banking system — unbanked resources lost on party wipe, Phasix always kept
- Decided: 3/4 oblique overworld + side-profile diorama combat perspective
- Decided: Two bone rigs per Phasix (3/4 oblique + side-profile); three overworld directions for Phase 1
- Decided: Narrative arc: Innocent → Lost → Home (working, not locked)
- Next: Phase 4 implementation — Evolution system Unity scripts per Evolution_System_Directive_v1_1_0.pdf §9

[2026-03-30] Design Session — Encounter, Progression, World Structure
- Built: Progression_Directive_v0_1_0.md, WorldDesign_Directive_v0_1_0.md, DECISIONS new entries
- Decided: Aura-driven progression replaces XP/leveling entirely
- Decided: Common Aura = stat growth fuel; Specific Aura = evolution gate; Rare Variant = exotic branch gate
- Decided: Free stat allocation + Resonance Bonus layer for emotionally aligned investment
- Decided: Aptitude dual function — (A) raises stat ceiling per tier, (B) unlocks exotic evolution branches
- Decided: Evolution requires three simultaneous gates: Aura + stat minimums + conditionals
- Decided: Hub + Realms world structure with light conditional Hub evolution
- Decided: Phasix visibility = sensitivity/allergy framing (not chosen-one superpower)
- Decided: Three-layer encounter system replaces random encounters entirely
- Decided: Calendar system — story-beat-driven soft time currency, emotional seasonal context
- Decided: Four-faction working framework (Suppressors/Amplifiers/Avoiders/Integrators)
- Decided: Old lore (Fracture, Five Factions) = reference only, requires full revisit
- Next: Doc sync + commit all v1_1_0 working files

[2026-03-27] Phase 1 Wk 5–6 — PhasixAnimatorGenerator (auto-generate Animator Controllers)
- Built: `Assets/Scripts/Editor/PhasixAnimatorGenerator.cs` — EditorWindow under Phasix/Animator Generator…
  - Auto-discovers .anim clips in a target folder by keyword (idle, walk, walk_back, run, attack)
  - Optional clip prefix filter (e.g. "uhdrin") to exclude unrelated clips in shared folders
  - Auto-selects state machine type: 4-state (Idle/Walk/Run/Attack) or 5-state (adds WalkBack) based on clip presence
  - 4-state params: IsMoving (bool), IsRunning (bool), Attack (trigger)
  - 5-state params: IsMoving (bool), IsRunning (bool), IsWalkingBack (bool), IsAttacking (trigger)
  - Idempotent: skips existing controllers rather than overwriting; reports missing clips in status bar
  - Outputs `{CreatureName}_AC.controller` into the chosen animation folder
- Decided: Helpers (EnsureFolder, EnsureParameter, EnsureState, AddBoolTransition) duplicated from PhasixSpriteSetup.cs — both scripts kept self-contained; no shared utility class introduced
- Next: Phase 1 Wk 7–8 — Tilemap world + Cinemachine camera follow (Roadmap)

[2026-03-27] Phase 1 Wk 5–6 — DarkUhdrin animation pipeline (manual, end-to-end)
- Built: `Assets/Animations/Creatures/DarkUhdrin/` — full animation set for DarkUhdrin
  - 5 AnimationClips: uhdrin_idle, uhdrin_walk_forward, uhdrin_walk_back, uhdrin_run, uhdrin_attack (all at 8fps, Samples visible via Show Sample Rate)
  - DarkUhdrin_AC.controller — 5 states, 4 parameters, all transitions wired
- Decided: Switched from Dark Fluffy to Dark Uhdrin (cleaner sprite sheet, uniform grid)
- Decided: Composite sheet sliced via Grid By Cell Count (8×4) — solid background prevents Automatic slicing
- Decided: No uhdrin_walk generic state — replaced with directional walk_forward / walk_back
- Decided: SpriteRenderer.flipX handles left/right mirroring at runtime — no separate left-facing clips needed
- Decided: Run animation loops at full speed only — deceleration handled by Animator transitions in code, not clip frames
- Fixed: DarkUhdrin_AC.controller — added Walk_Back → Idle (IsMoving=false), Run → Walk_Back (IsRunning=false + IsWalkingBack=true), Run → Walk_Forward now requires IsWalkingBack=false, Idle → Walk_Back now requires both IsMoving=true + IsWalkingBack=true
- Deleted: uhdrin_walk.anim (unused generic walk clip)
- Next: Write Editor script to auto-generate Animator Controllers for future creatures (standard state machine pattern now locked in)

[2026-03-24] Phase 1 Wk 5–6 — Sprite sheet import pipeline + Animator Controller setup
- Built: `Assets/Scripts/Editor/PhasixSpriteSetup.cs` — Editor utility with 5 menu steps under Phasix/Sprite Setup/
  - Step 1: Configures Point filter, Multiple mode, PPU=32, no compression on all Dark Fluffy sheets; auto grid-slices running sheets (4×2)
  - Step 3: Creates 4 placeholder AnimationClips (idle 8fps loop, walk 12fps loop, run 16fps loop, attack 10fps no-loop)
  - Step 4: Creates DarkFluffy_AC.controller with Idle/Walk/Run/Attack states, IsMoving+IsRunning (bool) + Attack (trigger) params, all transitions wired
  - Step 5: Drops DarkFluffy_Test GameObject into scene with SpriteRenderer (Sorting Layer: Characters) + Animator
- Built: `Assets/Animations/Creatures/DarkFluffy/` — folder ready for clips and controller
- Decided: PPU = 32 (starting value; adjust after visual check in Unity Editor — see DECISIONS.md)
- Decided: Composite sheets (v1/v2) use Sprite Editor Automatic slice (Step 2, manual); running sheets grid-sliced via script
- Decided: Dark Fluffy v1 vs v2 not yet chosen — user is reviewing both; defer to DECISIONS.md once locked
- Next: In Unity — run Steps 1–5 via Phasix menu; fill clip timelines with frames via Animation window; then verify smoke test

[2026-03-24] Phase 1 Wk 3–4 — PlayerController + Input System
- Built: `Assets/Scripts/Player/PlayerController.cs` — 8-directional top-down movement, Rigidbody2D, new Input System, smooth accel/decel, Animator support, FreezeMovement/UnfreezeMovement API for Phase 3
- Built: `.mcp.json` — Context7 MCP server configured for Unity 6000.3.x docs lookup per session
- Built: `.claude/settings.json` — project-level Claude Code settings, Context7 MCP enabled
- Decided: Input architecture = `InputActionAsset` + manual action subscription (see DECISIONS.md)
- Decided: Using existing `Assets/InputSystem_Actions.inputactions` (Unity default) — has `Player` map + `Move` action already
- Decided: `rb.linearVelocity` used (Unity 6 renamed from legacy `rb.velocity`)
- Decided: `FreezeMovement()` / `UnfreezeMovement()` stubbed as public API for Phase 3 BattleManager
- Next: Inspector setup in Unity Editor — create Player GameObject, test tilemap room, wire script fields. Then Phase 1 Wk 5–6: import Asset Store character pack and wire Animator blend trees

[2026-03] Pre-development planning complete
- Built: GDD v0.7.9, Technical Directive v0.1.0, Roadmap v2 (GDD-aligned, 20-month plan)
- Built: ClaudeCode_Primer.md (311 lines, full GDD constraint set)
- Built: CLAUDE.md (auto-load summary for Claude Code sessions)
- Built: DECISIONS.md (implementation decisions register, initially seeded)
- Built: CHANGELOG.md (this file)
- Decided: Unity 2D URP, Asset Store art pipeline, A* Pathfinding Project (free), DOTween for tweening
- Decided: Development path = Asset Store sprites first, no custom art pipeline until post-demo
- Decided: Claude Code used for scripting with CLAUDE.md auto-loaded per session
- Decided: Creature classifier working name = Phasix (not yet written into GDD — pending confirmation)
- Decided: Art style TBD — Hades-style pre-render pipeline deferred, not ruled out
- Pending: Unity project not yet created — Phase 1 begins next
- Next: Create Unity LTS project, configure 2D URP, set up Git repo, create folder structure

[2026-03] Game renamed
- Decided: Game title changed from "Project Mon-Farm" to "Phasix"
- Note: "Phasix" now serves as both the game title AND the creature classifier name
- Note: GDD still references Mon-Farm internally — pending GDD update to reflect rename
- Note: All Docs/ files updated to reflect new name
