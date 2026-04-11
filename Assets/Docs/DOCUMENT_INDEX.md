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
| Evolution_System_Directive | v1.1.0 | Docs/Evolution_System_Directive_v1_1_0.pdf | Evolution web, devolution (free), fusion, tier structure, Unity implementation | Supersedes GDD §3 tier structure. Supersedes Progression_Directive devolution cost section. Primary evolution authority. |
| Progression_Directive | v0.1.0 | Docs/Progression_Directive_v0_1_0.md | Aura system, stat growth, Aptitude, evolution gating | Supersedes GDD §21 XP/leveling model. Note: devolution cost section superseded by Evolution_System_Directive (devolution is now free). |
| WorldDesign_Directive | v0.1.0 | Docs/WorldDesign_Directive_v0_1_0.md | World structure (Multiple Hubs + Realms), encounter initiation, calendar, factions, visibility model, blackout/banking, perspective model, bone rigs, narrative arc | Supplements GDD §19, §24 |
| Combat_Directive | v0.1.0 | Docs/Combat_Directive_v0_1_0.md | Combat perspective, 7-lane stage, action commands, turn structure | Supplements GDD §18 |
| Technical_Directive | v0.1.0 | Docs/Phasix_TechnicalDirective_v0.1.0.html | Unity implementation patterns, existing scripts | — |
| CLAUDE.md | v1.1.0 | Project root | Auto-loaded by Claude Code — quick reference summary | Updated April 2026 |
| ClaudeCode_Primer.md | v1.1.0 | Docs/ClaudeCode_Primer_v1_1_0.md | Full system spec for Claude Code sessions | §9 defers to Evolution_System_Directive for evolution authority |
| DECISIONS.md | current | Docs/DECISIONS.md | Implementation decisions not in GDD | Updated April 2026 |
| CHANGELOG.md | current | Docs/CHANGELOG.md | Session log | Updated April 2026 |
| NumericalCalibration.md | current | Docs/NumericalCalibration.md | All pending numerical values | Updated April 2026 |
| SpeciesRoster.md | template | Docs/SpeciesRoster.md | Species design template — empty | Pending Phase 5 |

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
| The Fracture event lore | LoreBible_Phasix.html | Auto-filled without approval in prior session. Shifted significantly. Requires full revisit before any implementation. |
| Phase Dimension details | LoreBible_Phasix.html | Same as above |
| Original Five Factions | LoreBible_Phasix.html | Same as above. New faction framework (Suppressors/Amplifiers/Avoiders/Integrators) is working replacement — also pending refinement. |
| Elemental Frequencies (Ignis/Virel/Aether/Veil/Flux) | LoreBible_Phasix.html | Deferred — relationship to PrimalType and emotionalType unclear. Do not implement until lore revisit session resolves this. |
| LoreBible_Phasix.html | Docs/LoreBible_Phasix.html | REFERENCE ONLY. Contains: Vorthex encounter prototype (→ Prototypes/ revisit Phase 3), emotional root system (compatible with WorldDesign_Directive), Phasix visibility model (adopted). Deprecated sections: Fracture lore, Phase Dimension, Five Factions, Elemental Frequencies (all pending lore revisit). Do not implement any section without explicit approval. |

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
