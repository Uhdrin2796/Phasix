# Prototypes

This folder contains interactive proof-of-concept documents built during pre-development to validate core encounter design systems. They are **reference only** — not implementation blueprints, but working demonstrations used to test ideas before any Unity code was written.

These should be revisited during **Phase 3 (Months 5–7)** when the battle system and companion AI are being implemented.

---

## Contents

### `EncounterDesign_Vorthex.html`
Full playable prototype of the Vorthex encounter — the canonical first Phasix encounter design. Demonstrates the complete 4-phase system:
1. Encounter dialogue (narrative intro, no choices)
2. Turn-based battle loop with HP tracking
3. Three threshold dialogues at 80% / 55% / 30% HP — Rage/Calm accumulator system
4. Capture outcome with bond type evaluation

Run this in a browser to play through all possible paths. Proves the Rage/Calm/CaptureRate system works and feels meaningful before any Unity code exists.

**Key design validated:** Reluctant Bond is the most common outcome (45.6%) by design — good outcomes must be earned.

---

### `EncounterStateTree_Vorthex.html`
State machine visualizer for the Vorthex encounter. Shows all 125 possible path combinations across the three thresholds, their resulting Rage/Calm values, and bond outcomes. Includes an interactive canvas diagram and a filterable table of every path.

Use this when implementing the FSM in Unity to verify the logic produces the correct output distribution.

---

### `DialogueSystem_VerdantScar.html`
NPC dialogue tree prototype for Researcher Vael (Ashveil faction) in the Verdant Scar region. Demonstrates the Bond Window system (temporal mechanic for when a Phasix is capture-ready) and faction standing as a separate tracked value from Phasix bond %.

This is the reference for the **first-contact NPC encounter** design pattern — player approach (Assert Authority / Show Humility / Invoke the Phasix) produces meaningfully different bond window states and faction reputation outcomes.

---

## Related

- **Skill:** `phasix-dialogue-encounter` — installed Claude Code skill with full encounter design workflow, writing guide, and Unity implementation reference. Use this skill when building any new Phasix encounter.
- **LoreBible:** `Assets/Docs/LoreBible_Phasix.html` — world context, faction detail, and the Five Frequencies lore that frames these encounters.
- **Encounter architecture spec:** inside the installed skill at `references/encounter-architecture.md`
