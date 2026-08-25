# Phasix — Architecture Directive
**Version:** 0.1.0
**Date:** August 2026
**Status:** Working — full-codebase architecture review and migration plan. Not yet locked.
**Errata (2026-08-23):** Added the precise official-guidance citation backing debt item 1 (Part 1),
added Part 3.5 comparing full/hybrid/minimal migration paths with their actual risk tradeoffs
(DragLine/Ring are gameplay-critical and already tuned — that's the real cost of a full migration,
not just line count), and cross-referenced the new `VFX_Pipeline_Directive_v0_1_0.md`, which is the
detailed authoring companion to this doc's Phase 4. Addition/refinement only — no version bump.
**Related:** Combat_Directive_v0_1_0.md, Attack_Pattern_Directive_v0_1_0.md, VFX_Pipeline_Directive_v0_1_0.md, Phasix_TechnicalDirective_v0.1.0.html, LESSONS_LEARNED.md, KNOWN_ISSUES.md

---

## Overview

A full-codebase review (18,812 lines across `Assets/Scripts`), assessed against official Unity 2D architecture guidance, aimed at one question: what would this look like if a senior Unity developer had built it for longevity and future additions? This isn't a rewrite mandate — it's an honest assessment of what's already right, what's debt, and a phased plan to close the gap without destabilizing what's shipped and working.

---

## Part 1 — Current State Assessment

### What's already right (build on this, don't replace it)

- **EventBus** (`Core/EventBus.cs`) — a clean static pub/sub hub, zero MonoBehaviour/Scene coupling, already correctly used across Bond, Personality, Battle, and Evolution systems. This is exactly the decoupling backbone a senior architecture wants, and it's already there.
- **Overworld rendering already follows the correct pattern** — `CompanionAI`, `PlayerTopDownController` are real Scene GameObjects with `SpriteRenderer`/`Rigidbody2D`, standard idiomatic Unity 2D. The team already knows and uses the right pattern — it just wasn't carried into battle.
- **ScriptableObject-driven data** (`SkillData`, `SpeciesDatabase`, skill assets) — idiomatic Unity data architecture, not fighting the engine.
- **Real test infrastructure** — `Phasix.Tests.EditMode`, correctly referencing `Phasix.Runtime`, actively used (confirmed via `KNOWN_ISSUES.md` AUD-012).
- **DOTween** already integrated for tweening.

### Debt (the real findings)

1. **The entire battle stage is UI Toolkit** — creatures (`_playerStageCreatures`, `_enemyStageCreature`), the projectile, ring, gauge, drag-line, and lane guide are all `VisualElement`/`Painter2D` content inside one `UIDocument`. Unity's official UI-systems comparison page states uGUI is the recommended runtime system by default, with UI Toolkit as the alternative specifically for "multi-resolution menus and HUD in intensive UI projects" — while **uGUI is specifically recommended for "world space UI and VR, and UI that requires customized shaders and materials."** That's exactly creatures and projectiles. More precisely: neither UI system is the right call for them — creatures and projectiles aren't UI at all, they're world content that belongs in the Scene, full stop.
2. **`SpriteGlowController.cs` exists but is dormant.** Built with `[RequireComponent(typeof(SpriteRenderer))]`, correctly written, never actually instantiated — it was built ahead of a GameObject-based creature that doesn't exist in battle yet.
3. **Pixel Perfect Camera doesn't apply to battle content.** CLAUDE.md commits the project to a 320×180 Pixel Perfect Camera, which snaps world-space Transforms as rendered by a real Camera. UI Toolkit screen-space content never participates in that snapping — battle visuals are quietly inconsistent with the project's own stated art direction.
4. **One monolithic runtime assembly.** `Phasix.Runtime.asmdef` covers all 18,812 lines with no further split. This already caused a real, documented bug: `LESSONS_LEARNED.md` records a domain-reload failure where a single compile error anywhere in the assembly blocked *every* new type in the entire codebase from loading, not just the broken file. No compile-time boundary exists between, say, Save and Combat.
5. **Two god-files.** `BattleHUDController.cs` is 3,387 lines; `BattleManager.cs` is 2,562 lines. Both are 3–7x past a reasonable single-responsibility ceiling. `BattleHUDController` in particular almost certainly blends simulation-adjacent logic, input handling, VFX orchestration, and pure UI Toolkit tree manipulation into one file — the same file that owns the `VisualElement` creature references is the one place a Scene-layer migration has to touch, which makes it the highest-risk file in the project by sheer surface area.
6. **No object pooling for projectiles/VFX yet** — already flagged in `Roadmap_v2.md` under Phase 5 polish. Not new, just confirmed in scope here since it's part of the same migration.
7. **Simulation/presentation separation needs auditing, not assumed.** `BattleManager` (simulation) and `BattleHUDController` (presentation) are separate files, which is good, but at 2,562 and 3,387 lines respectively there's real risk they've blended responsibilities rather than staying cleanly on either side of that line. Worth confirming, not assuming, before other migration work builds on top of it.

---

## Part 2 — Target Architecture

### Layer separation

- **World/Simulation layer** — Scene, Camera, GameObjects, `SpriteRenderer`, `Animator`/2D Rigging, Shader Graph materials, Sprite Shape, Particle System/VFX Graph. Everything that *is* the game: creatures, projectiles, all skill VFX. `BattleManager` (pure state/logic, fires events, never touches rendering) plus a new presentation layer of MonoBehaviours that subscribe to `EventBus`/`BattleManager` state and drive Scene content.
- **Interface layer** — uGUI Canvas (screen-space overlay) for genuine interface: Aura/HP readouts, menus, turn order, prompts. Ring, gauge, and drag-line are borderline (input-feedback UI that must track a Scene position) — resolved via the standard `Camera.WorldToScreenPoint()` pattern, not a custom coordinate bridge. This is a one-line, well-worn technique (same one every floating-health-bar implementation uses), not a fragile seam.
- **Communication rule:** simulation never references rendering. Presentation (both layers) only ever reads simulation state via `EventBus` or exposed read-only state — never the reverse.

### Assembly boundaries (proposed split, replacing the single `Phasix.Runtime`)

| Assembly | Contains | Depends on |
|---|---|---|
| `Phasix.Core` | `EventBus`, `GameManager`, shared enums/types | — |
| `Phasix.Creatures` | Species/skill data, `PhasixRuntimeData`, Bond/Personality/Aura systems | Core |
| `Phasix.Combat` | `BattleManager`, `LaneMovementSystem`, `BeatSequenceRunner`, damage/status/combo engines | Core, Creatures |
| `Phasix.Save` | Save data + system | Core, Creatures |
| `Phasix.Audio` | Audio manager/catalog | Core |
| `Phasix.Presentation` | New Scene-layer creature/projectile/VFX controllers | Core, Combat, Creatures |
| `Phasix.UI` | Interface-layer controllers (uGUI) | Core, Combat, Creatures |
| `Phasix.World` | Overworld (already-correct pattern) | Core, Creatures |

Dependency direction is one-way, top-to-bottom in the table — this is what actually fixes the domain-reload issue: a compile error in `Phasix.UI` can no longer block `Phasix.Combat` from loading, since they're separate assemblies.

### File size discipline

No hard rule invented here (that's a team-taste call), but 3,387 and 2,562 lines are well past any reasonable threshold regardless of where the line is drawn. Both should decompose along the responsibility boundaries above — `BattleHUDController` in particular should split into at minimum a pure UI Toolkit interface-layer piece and a separate Scene-layer presentation piece, once creatures/projectile move.

---

## Part 3 — Migration Plan, Phased by Risk

**Phase 1 — Assembly split.** Mechanical, no behavior change, zero visual risk. Immediately fixes the documented domain-reload pain point. Do this first — it's the highest safety-to-value ratio in the whole plan and makes every subsequent phase easier to reason about (compiler-enforced boundaries catch mistakes the other phases might otherwise make silently).

**Phase 2 — Decompose the two god-files**, still with no behavior change — pure extraction/reorganization along the Part 2 boundaries, before any rendering migration touches them. Doing this first means Phase 3 touches several well-scoped files instead of two 3,000-line ones.

**Phase 3 — World-layer migration.** Creatures first (highest visual-quality payoff, and where `SpriteGlowController` finally gets used), then the projectile — exactly the sequencing discussed already. Ring/gauge/drag-line/lane-guide stay UI Toolkit; bridge via `WorldToScreenPoint()`.

**Phase 4 — Shader Graph / 2D Rigging / Sprite Shape adoption**, now that there's real Scene content to attach them to. Order: hand-coded shaders first (proven, no GUI dependency) → Sprite Shape for anything path-shaped (fully scriptable) → 2D Rigging once the Spine-vs-native decision (Roadmap, already flagged, still open) is resolved, since that's a one-time human-driven setup cost per species. **Full authoring flow, tool comparison, and scalability principles for this phase are in `VFX_Pipeline_Directive_v0_1_0.md`** — this phase's job here is sequencing; that doc's job is the how.

**Phase 5 — Object pooling** for projectiles/VFX, already scoped in Roadmap Phase 5 — sequenced last here because it's most valuable once the actual GameObjects being pooled (Phase 3/4) are stable, not before.

### Migration scope — three paths, not one

Phase 3 above assumes the **hybrid** path. Worth naming the alternatives explicitly, since the choice isn't just "how much work" — it changes what's actually at risk:

- **Full migration** — every UI Toolkit battle element (including ring/gauge/drag-line) moves to Scene content. This is the architecturally "purest" end state — no permanent `WorldToScreenPoint` bridge, one rendering paradigm throughout. But ring and drag-line aren't decorative: drag-line drives Dodge/Parry, which is already built, tuned, and had its most recent fix as of the last sync (`"Fix ground-strike VFX re-lighting fake cells right after the blue reveal"`). Both ride on UI Toolkit's mature pointer-event system. Full migration means rebuilding the most gameplay-critical, already-debugged input path on a different event system — real regression risk against something that currently works, not just more files touched.
- **Hybrid (recommended, what Phase 3 describes)** — only creatures and the projectile move; ring/gauge/drag-line/lane-guide stay exactly as they are. Gets 100% of the visual-quality win (Shader Graph, Sprite Shape, eventual Rigging all need creatures/projectile, not the input-feedback elements) while never touching the one system that's already correct.
- **Minimal/no architecture change** — keep everything in UI Toolkit, just improve the `Painter2D` content and `backgroundImage` art itself. Doesn't unlock Shader Graph, Rigging, or Sprite Shape at all (none can attach to a `VisualElement`) — only worth considering if the actual complaint turns out to be "the placeholder shapes are ugly" rather than "the tech is limited." Given Shader Graph/Rigging/Sprite Shape are explicitly wanted (see `VFX_Pipeline_Directive_v0_1_0.md`), this path doesn't satisfy the actual goal.

---

## Part 4 — Open Decisions

1. **Assembly boundary judgment calls** — the Part 2 table is a reasonable starting split, not a definitive one; some systems (e.g. `WildSpawnSystem`, `EncounterTrigger`) could argue for either Creatures or World and deserve a second look during Phase 1.
2. **Spine vs Unity 2D Animation** — already open in Roadmap, directly blocks Phase 4's rigging work specifically.
3. **Does ring/gauge/drag-line ever migrate, or stay UI Toolkit permanently?** Current recommendation is permanently — they're genuinely well-suited to Painter2D and migrating them buys nothing toward visual quality. Worth confirming this isn't just deferred by default.
4. **`BattleManager`/`BattleHUDController`'s actual responsibility split** hasn't been read function-by-function yet — Phase 2 should start with an audit pass before extracting anything, since the assumption that they're already cleanly simulation/presentation-separated is unverified.

---

## Status
Design capture, not a locked spec. This is a large undertaking spanning multiple sessions — Part 3's phase ordering is the recommended sequence, not a mandate to do it all at once.
