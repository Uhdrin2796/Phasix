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
