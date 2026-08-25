# Phasix — Document Index
**Version:** 1.1.0  
**Date:** April 2026  
**Purpose:** Single source of truth for all project documents. Read this first in any session — Claude Code or Claude chat — to understand what is current, what is superseded, and what is pending.

---

## How To Use This Index

1. Check STATUS column before reading any document
2. ACTIVE = authoritative, implement from this
3. SUPERSEDED = retained for reference only, do not implement
4. REFERENCE = historical or shifted, do not implement
5. PENDING = not yet written, scaffold only

When documents conflict — the more specific Directive always wins over the GDD.

---

## Active Documents

| Document | Version | Location | Covers | Notes |
|---|---|---|---|---|
| GDD_Phasix | v0.8.0 | Docs/GDD_CreatureRPG_v0_8_0.html | Master design document — all core systems | §21 XP/leveling superseded by Progression Directive. §3 tier structure superseded by Evolution Directive. §19, §24 supplemented by World Design Directive. §18 supplemented by Combat Directive. |
| Evolution_System_Directive | v1.1.0 | Docs/Evolution_System_Directive_v1_1_0.pdf | Evolution web, devolution (free), fusion, tier structure, Unity implementation | Supersedes GDD §3 tier structure. Supersedes Progression_Directive devolution cost section. Primary evolution authority. **PDF is canonical source.** |
| Evolution_System_Directive (MCP mirror) | v1.1.0 | Docs/Evolution_System_Directive_v1_1_0.md | Full plain-text Markdown mirror of the PDF above | MCP-readable version. If content conflicts with the PDF, the PDF takes precedence. |
| Progression_Directive | v0.1.0 | Docs/Progression_Directive_v0_1_0.md | Aura system, stat growth, Aptitude, evolution gating | Supersedes GDD §21 XP/leveling model. Note: devolution cost section superseded by Evolution_System_Directive (devolution is now free). |
| WorldDesign_Directive | v0.1.0 | Docs/WorldDesign_Directive_v0_1_0.md | World structure (Multiple Hubs + Realms), encounter initiation, calendar, factions, visibility model, blackout/banking, perspective model, bone rigs, narrative arc | Supplements GDD §19, §24 |
| Combat_Directive | v0.1.0 | Docs/Combat_Directive_v0_1_0.md | Combat perspective, 7-lane stage, action commands, turn structure | Supplements GDD §18. Part 4 defense model superseded 2026-08-05 by full-avoidance Dodge/Parry (see DECISIONS.md -> [Combat]). Part 3's lane-occupancy model refined 2026-08-12: each lane now has 5 discrete, position-exclusive slots (7×5 total), symmetric on both sides per the doc's own errata — but see DECISIONS.md -> [Combat] "Enemy-side position support" for why the actual implementation is player-side only so far. |
| Attack_Pattern_Directive | v0.1.0 | Docs/Attack_Pattern_Directive_v0_1_0.md | Melee Beat Sequence framework (Approach/Windup/Attack), telegraph knobs, ranged/melee archetypes, Strike Points | 2026-08-11: Lane Movement, Telegraph Knob Schema, and Beat Sequence BUILT for one minimal example (`Melee_Slash`, `LaneMovementSystem.cs`, `BeatSequenceRunner.cs`). 2026-08-12: the 7×5 occupancy model (Part 8) is BUILT for pre-battle placement + in-battle repositioning, player-side only (`FormationSystem.cs`, `FormationGridPicker.cs`, `BattleHUDController`'s Move-drag flow — see CHANGELOG.md). Groups 1–2 archetypes (Instant Strike, Feint, Metronome, Jitter, Direct Projectile, Multi-Hit Volley, Charge & Release, Sustained Pressure) all BUILT — see the doc's own Part 1 build-order tracker for exact status. 2026-08-20: Group 3, BOTH items, BUILT — item 7 Zone/Positional (`Ranged_ZoneRow`/`Column`/`DiagonalX`) and item 8 Split Attention (`Ranged_ZoneBurst`/`Ranged_ZoneArrowhead`, the first patterns computed relative to a live target position rather than fixed authored data); see doc's own 2026-08-20 errata (both entries) and DECISIONS.md -> [Combat]. 2026-08-21: Zone/Positional offense direction BUILT — the player can now cast these skills AT the enemy, with an Instinct/bond-scaled + `EnemyDifficultyTier` AI dodge and a new Root-applying "Snare" skill (the first status effect that actually gates gameplay); see doc's own 2026-08-21 errata and DECISIONS.md -> [Combat]. Enemy-side positions (the multi-enemy rendering gap), the reactive Lane Displacement Attack dodge, Strike Points, and the rest of Parts 6/9 remain design-capture only. Diagram companion: Docs/melee_beat_sequence.mermaid. Open gaps: Type E (Reaction) has no trigger point against melee sequences yet, enemy-side positions need multi-enemy battle support first (see doc Part 10 and DECISIONS.md -> [Combat]). |
| Architecture_Directive | v0.1.0 | Docs/Architecture_Directive_v0_1_0.md | Full-codebase architecture review (UI Toolkit vs Scene rendering, assembly split, god-file decomposition), phased migration plan toward official Unity 2D patterns | 2026-08-25: Phase 1 BUILT, at reduced scope — `Phasix.World`/`Phasix.Player`/`Phasix.UI` split out of `Phasix.Runtime` (UI extracted via moving `HudTooltip`/`BattleSummaryController` into `Combat/`), plus the `Combat`↔`Audio` back-reference closed (`Combat/BattleVfxEventHooks.cs`, no new assembly — `Audio` still needs `Creatures`). A full cross-folder reference audit disproved the doc's original 7-assembly proposal: `Core`/`Creatures`/`Combat` remain one mutually-coupled cluster, not a clean one-way graph (see doc's own 2026-08-24/25 errata and `DECISIONS.md` → `[Architecture]`). Phase 3 and `VFX_Pipeline_Directive` remain blocked pending the one remaining piece — the `GameManager` composition-root split (doc's Part 4 item 5(b)). Part 1 still has the full current-state findings (strengths and debt) grounding the plan. |
| VFX_Pipeline_Directive | v0.1.0 | Docs/VFX_Pipeline_Directive_v0_1_0.md | Skill/spell VFX authoring: Shader Graph vs 2D Rigging vs Sprite Shape vs LineRenderer tool comparison, per-tool authoring flow, long-term scalability principles (7, incl. video verification with a concrete pinned tool), and a 9-family Skill Execution & Resolution Lifecycle breakdown (Part 5) covering every real built skill in Assets/Data/Skills/ | 2026-08-24: design capture only, expanded five times across two days — Part 5 replaced a single projectile-only worked example with the full 9-family lifecycle breakdown; Part 2's Shader Graph section replaced with full MCP-capability research (confirmed dead end on CoplayDev, the Custom Function Node hybrid); Part 2's decision heuristic sharpened to state text-only as the explicit default; a Standard Verification Workflow subsection (Part 2) + new principle 7 (Part 4) added for video-based verification; and that workflow pinned to a concrete tool (DevStudio MCP, installed via uvx, no local clone needed) with a backup noted. Companion to Architecture_Directive Phase 4 — blocked on that doc's Phase 3 landing first. Also blocked on the still-open Spine-vs-native decision (Roadmap) for the 2D Rigging section, and on an unresolved discrepancy: no built skill asset actually matches Family 1 (Traveling Projectile)'s shape despite this index previously implying one existed — see the doc's own Part 6 item 1. |
| VFX_WorkedExamples | v0.1.0 | Docs/VFX_WorkedExamples_v0_1_0.md | Two contrasting end-to-end tutorials applying VFX_Pipeline_Directive Part 2's workflow: Example A (Fireball, text-only, the realistic default) and Example B (Corruption status visual, hybrid Custom Function Node, the deliberate exception) — both include real, buildable shader code and a video-based testing loop, not pseudocode | 2026-08-24: tutorial/worked-example doc, not yet built. Testing steps updated to lead with video capture per VFX_Pipeline_Directive Part 4 principle 7. Supersedes the earlier `ShaderGraph_WorkedExample_Fireball_v0_1_0.md` (deleted — paired the hybrid workflow with the wrong effect). Same Architecture_Directive Phase 3 blocker as the rest of the VFX pipeline. |
| Technical_Directive | v0.1.0 | Docs/Phasix_TechnicalDirective_v0.1.0.html | Unity implementation patterns, existing scripts | — |
| CLAUDE.md | v1.1.0 | Project root | Auto-loaded by Claude Code — quick reference summary | Updated April 2026 |
| ClaudeCode_Primer.md | v1.1.0 | Docs/ClaudeCode_Primer_v1_1_0.md | Full system spec for Claude Code sessions | §9 defers to Evolution_System_Directive for evolution authority |
| DECISIONS.md | current | Docs/DECISIONS.md | Implementation decisions not in GDD | Updated April 2026 |
| CHANGELOG.md | current | Docs/CHANGELOG.md | Session log | Updated April 2026 |
| LESSONS_LEARNED.md | current | Docs/LESSONS_LEARNED.md | Issues investigated and resolved — how to avoid/fix next time | Updated April 2026 |
| NumericalCalibration.md | current | Docs/NumericalCalibration.md | All pending numerical values | Updated April 2026 |
| SpeciesRoster.md | template | Docs/SpeciesRoster.md | Species design template — empty | Pending Phase 5 |

---

## Design Mockups — Interaction/Visual Reference Only, NOT Data

Standalone interactive HTML files the user built to demo a UI *interaction pattern* before it's
implemented in Unity. **Do not treat any content inside these as real game data** — node names,
stat requirements, and branch relationships are all randomly generated or hand-picked flavor
text for the demo, not designed values. Read these for HOW something should feel to use, never
for WHAT the numbers/names should be.

| Document | Location | What it demos | Status |
|---|---|---|---|
| evolution_web_mockup.html | Docs/evolution_web_mockup.html | The player's original vision for the **Evolution Web** screen (Phase 4, not yet built): a pannable/zoomable canvas graph of 10 evolution lines × 5 tiers (50 nodes), glowing gradient nodes, curved dashed "crossover" edges between lines, a 3-state Hidden/Sighted/Discovered fog-of-war (undiscovered forms show as "???" silhouettes until evolved into or scouted), a per-line filter bar, and a BFS "Plan Mode" that highlights the shortest evolve/devolve path between two forms with an animated step-by-step overlay. Everything renders on a raw `<canvas>` with hand-rolled pan (drag)/zoom (wheel + pinch) — no framework. | Reference only — mockup, not implemented. 2026-08-09 session ported the pan/zoom + glowing-node + `Painter2D`-drawn-edge INTERACTION pattern from this into Unity for the **skill tree**'s web view first (`Assets/Scripts/UI/SkillWebEdgeVisual.cs`, `OverworldMenuController.BuildSkillArea`) as a proving ground, since the real Evolution Web is blocked — see `DECISIONS.md` → [Creatures] "Evolution_System_Directive_v1_1_0.md has internal inconsistencies." Crossover branches, BFS Plan Mode, and the Hidden fog state were deliberately left out of that skill-tree port (nothing in skills needs them yet) and should be added back in once the real Evolution Web is built against this mockup — see `DECISIONS.md` → [UI] "Skill tree carousel replaced by a pan/zoom skill web" for the exact scope split and its own "Revisit if." |

---

## Skill Files

| File | Purpose | Status |
|---|---|---|
| Skill_phasix_develop | Mon-Farm Dialogue Encounter system — Vorthex prototype | Active — not yet installed in Claude Code |
| Emotion_Sprite_generation_skill | Sprite sheet generation pipeline v2.1 | Active |
| phasix-sprite-brief.skill | Sprite brief template for AI generation and freelancers | Active — not in project files, exists in chat history |

---

## Superseded — Retain As Reference Only

| Document | Superseded By | What Changed |
|---|---|---|
| GDD §3 Tier Structure | Evolution_System_Directive v1.1.0 | Natural lines T1–T5, fusion T6–T7, full branch requirement framework, devolution rules |
| GDD §21 XP/Leveling | Progression_Directive v0.1.0 | XP replaced by Aura, levels replaced by stat allocation, level floor replaced by stat minimum gate |
| Progression_Directive §Devolution Aura Cost | Evolution_System_Directive v1.1.0 | Devolution is now free — no conditions, no cost, no time limit |
| ClaudeCode_Primer §9 (evolution authority) | Evolution_System_Directive v1.1.0 | §9 now defers to the standalone directive; content retained for quick reference |

---

## Reference Only — Do Not Implement

| Content | Location | Status |
|---|---|---|
| The Fracture event lore | LoreBible_Phasix_(obsolete).html | Auto-filled without approval in prior session. Shifted significantly. Requires full revisit before any implementation. |
| Phase Dimension details | LoreBible_Phasix_(obsolete).html | Same as above |
| Original Five Factions | LoreBible_Phasix_(obsolete).html | Same as above. New faction framework (Suppressors/Amplifiers/Avoiders/Integrators) is working replacement — also pending refinement. |
| Elemental Frequencies (Ignis/Virel/Aether/Veil/Flux) | LoreBible_Phasix_(obsolete).html | Deferred — relationship to PrimalType and emotionalType unclear. Do not implement until lore revisit session resolves this. |
| LoreBible_Phasix_(obsolete).html | Docs/LoreBible_Phasix_(obsolete).html | REFERENCE ONLY. Contains: Vorthex encounter prototype (→ Prototypes/ revisit Phase 3), emotional root system (compatible with WorldDesign_Directive), Phasix visibility model (adopted). Deprecated sections: Fracture lore, Phase Dimension, Five Factions, Elemental Frequencies (all pending lore revisit). Do not implement any section without explicit approval. |

---

## Pending Design — Do Not Invent

These gaps exist and must not be filled speculatively. Flag with TODO and scaffold only.

| Gap | Blocking | GDD Ref |
|---|---|---|
| Hub count, physical identities, tonal identities, and specializations | Narrative development session | §19, §24 |
| Realm count and emotional identities | World design session | §19 |
| Hub NPC roster and arcs | World design session | §24 |
| Faction refined names and lore | Dedicated faction design session | WorldDesign Directive |
| Main quest narrative | Story design session | §24 |
| Species roster | Phase 5 | §25 |
| Actual skill content | Post-species-roster | §14 |
| Unnamed pool player-facing name | Naming session | §5 |
| Player-facing term for sensitivity-havers | Naming session | — |
| All NumericalCalibration.md values | Dedicated calibration session | §29 |
| Economy and items | §22 design session | §22 |
| Survival and crafting | §20 design session | §20 |
| Celestial properties | Per-species during roster | §13 |
| Elemental Frequencies reconciliation | Lore revisit session | LoreBible |

---

## Document Hierarchy — Conflict Resolution

When documents contradict each other, this priority order applies:

```
1. Specific Directives (Evolution, Progression, World Design, Combat)
2. GDD active sections
3. DECISIONS.md entries
4. ClaudeCode_Primer.md / CLAUDE.md
```

More specific always wins. Most recent version of a document always wins.
