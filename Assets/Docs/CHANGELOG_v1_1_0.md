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

[2026-03-30] Design Session — Encounter, Progression, World Structure

- Decided: Aura-driven progression replaces XP/leveling system (Progression_Directive_v0_1_0.md)
- Decided: Common Aura = stat growth fuel; Specific Aura = evolution gate; Rare Variant Aura = exotic branch gate
- Decided: Free stat allocation with Resonance Bonus layer for emotionally aligned investment
- Decided: Aptitude dual function — raises stat ceiling per tier (A) + unlocks exotic branches (B)
- Decided: Evolution requires three simultaneous gates — Aura + stat minimums + conditionals
- Decided: Hub + Realms world structure confirmed with light conditional hub evolution
- Decided: Phasix visibility model — allergy framing, spectrum-based sensitivity, JoJo-adjacent logic
- Decided: Three-layer encounter initiation — Emotional Mirroring, Resonance/Attunement, Failure-Triggered
- Decided: Calendar/month system — story-beat driven, soft content windows, first vs return cycle
- Decided: Four working faction philosophies — Suppressors, Amplifiers, Avoiders, Integrators (exploratory, pending refinement)
- Decided: Positive emotion design principle — no emotion inherently good or bad, mixed Aura evolutions for complex states
- Decided: Old lore (Fracture, Phase Dimension, Five Factions) retained as reference only — shifted, requires full revisit
- Built: Progression_Directive_v0_1_0.md
- Built: WorldDesign_Directive_v0_1_0.md
- Built: DECISIONS new entries (to be merged into DECISIONS.md)
- Pending: Hub physical and tonal identity
- Pending: Realm count and emotional identities
- Pending: Faction name refinement and lore details
- Pending: All new NumericalCalibration.md items from Progression Directive
- Pending: GDD §21 and §19 formal update to reference new directives
- Next: Merge DECISIONS entries, push new directive files to GitHub via Claude Code
