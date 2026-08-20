# Phasix — Combat Directive
**Version:** 0.1.0  
**Date:** April 2026  
**Status:** New — supplements GDD combat sections  
**Errata (2026-08-04):** Part 3 corrected "5-lane logic" → "7-lane logic" to match the canonical
7-lane system defined in Part 2 (see `AUDIT_202608.md` AUD-008). Wording fix only — no version bump.
**Errata (2026-08-11):** Part 3 gained the Lane occupancy and Movement cost model paragraphs below,
locked alongside `Attack_Pattern_Directive_v0_1_0.md` (see `DECISIONS.md` -> `[Combat]`). Addition
only — no version bump.
**Errata (2026-08-11, later same session):** The "Pending Design" list's "Whether the 7-lane system
applies symmetrically to both sides" line is resolved — lane *mechanics* are symmetric for both
sides; non-exclusive-occupancy *visual spacing* is player-side only until multi-enemy battles exist
(see `DECISIONS.md` -> `[Combat]` "Enemy-side lane symmetry"). Line removed from the pending list
below. Addition only — no version bump.
**Errata (2026-08-12):** Part 3's Lane occupancy paragraph is refined, not reversed — each lane now
has 5 discrete positions, confirmed symmetric on both player and enemy sides (supersedes the
player-side-only deferral in the errata directly above). See `Attack_Pattern_Directive_v0_1_0.md`
Part 8 for the reactive-dodge mechanics this enables. Addition/refinement only — no version bump.
**Errata (2026-08-17):** Two small corrections to Part 3's Lane occupancy paragraph, caught while
diagnosing a real bug (two party members rendering fully overlapped — see `KNOWN_ISSUES.md`
`[COMBAT-002]`): (1) "position exists beneath that, for movement/collision/visual-spacing purposes
only" undersells what actually shipped — the ORIGINAL free-form "non-exclusive, spaced-apart-in-lane"
spacing math (`LaneMovementSystem.GetInLaneSpacingOffsetPx`) was fully REMOVED once positions became
exclusive, not layered on top of it; "visual spacing" today means only the fixed per-position column
offset (`LaneMovementSystem.GetPositionOffsetPx`), a pure function of position index, nothing more.
(2) "Exact position layout... is pending numerical calibration" is stale — the layout itself
(`GetLaneScreenTop`'s depth-scale formula, `GetPositionOffsetPx`'s column spacing,
`PositionColumnSpacingPx = 150f`) is fully implemented and live; only the specific numeric constants
remain placeholders pending `NumericalCalibration.md`, same status as every other tuning value in
this project, not an unbuilt system. Addition/clarification only — no version bump.
**Errata (2026-08-20):** Part 1's "side-profile diorama view... real background art" describes the
**intended final** perspective — worth flagging clearly that the CURRENT implementation
(`BattleHUDController`) is not that yet: it's a flat UI Toolkit Screen Space Overlay panel over the
frozen overworld (colored circles, no depth/parallax/real camera), a deliberate placeholder-first
stand-in per `DECISIONS.md` -> `[Art] Placeholder-first pipeline`. A full migration boundary — what
stays UI Toolkit forever (fixed HUD chrome: nameplates, bars, log, buttons) vs. what becomes real
scene content later (stage creatures, rings, drag-lines, any stage-anchored VFX), confirmed
technical constraints (URP Bloom and `LineRenderer`/`Physics2D` cannot reach the current UI Toolkit
stage at all), and why now isn't the right time to migrate — is written up in `DECISIONS.md` ->
`[Combat/Art] Battle stage rendering — UI Toolkit overlay now, real diorama scene later`. Addition
only — no version bump.
**GDD Refs:** §18 (Battle System), §18.5 (Wild Creature Behavior), §18.6 (Enemy AI Design)

---

## Overview

This directive documents the combat perspective model, battle stage structure, and tactical positioning system for Phasix. These systems are not yet reflected in the GDD. GDD §18 combat sections remain Pending — this document is the design foundation for those sections when they are written.

All specifics below are **confirmed structural decisions** unless explicitly flagged as working/pending.

---

## Part 1 — Combat Perspective

### Visual Style
Combat uses a **side-profile diorama view** — a wide panoramic stage seen from a fixed side angle. Reference: Paper Mario battle scenes.

This perspective was chosen because:
- Individual Phasix art and animation are maximally visible
- The side-profile combat rig is the high-detail rig — this is where each Phasix gets to be seen
- Clear spatial read of lane positioning and depth

### Transition From Overworld
When the player's overworld sprite contacts an enemy Phasix sprite, a cinematic transition fires and the perspective shifts from 3/4 oblique top-down to the side-profile diorama. Transition visual identity (e.g. "Galaxy Swirl" style wipe) is **pending art direction**.

Scene loading uses additive loading for seamless transitions — overworld remains loaded underneath combat.

---

## Part 2 — Battle Stage Structure

### The 7-Lane Depth System
The battle stage has **7 distinct horizontal lanes** representing depth from front (Lane 1, closest to camera) to back (Lane 7, furthest from camera).

```
Lane 1   Front       Closest to camera — largest sprites
Lane 2   Near-front  —
Lane 3   Near        —
Lane 4   Mid         Default starting lane
Lane 5   Far         —
Lane 6   Back-far    —
Lane 7   Back        Furthest from camera — smallest sprites
```

7 lanes (vs 5) accommodate larger Phasix that have a bigger visual footprint on the field without crowding out smaller ones.

### Depth Scaling
To maintain the 3/4 perspective illusion, sprites **automatically scale** as they move between lanes:
- Moving toward Lane 1 (front): sprite grows
- Moving toward Lane 7 (back): sprite shrinks
- Scaling is smooth and continuous — not stepped

Exact scale values per lane are **pending numerical calibration** (NumericalCalibration.md).

### Phasix Size and Lane Footprint
Phasix vary in physical size — larger Phasix (typically higher tier) have a wider visual footprint that bleeds into adjacent lanes. Size is handled via **center anchor model**:

- Every Phasix occupies exactly **one mechanical lane** regardless of visual size — its anchor lane
- Visual sprite extends into adjacent lanes based on size tier but does not block them mechanically
- Movement, targeting, and collision all resolve against the anchor lane only
- This keeps spatial logic simple and avoids edge cases around large creatures near lane boundaries

Size tiers and visual footprint widths are **pending design** — decided per species during roster population.

### Stage Dimensions
Background art format: **1920×1080 wide panorama** with a "deep floor" — the floor plane extends back to sell the depth illusion.

---

## Part 3 — Tactical Positioning

### Lane Movement
Players and enemies can move **up and down between lanes** as a combat action or reaction. Lane positioning creates tactical decisions:

- **Dodge AoE attacks** — move out of the affected lane range before the attack resolves
- **Protect vulnerable Phasix** — move a defensive Phasix into the lane of an incoming attack
- **Exploit positional abilities** — some Phasix skills may have lane-specific effects or range requirements

**Lane occupancy [DECISION LOCKED, refined 2026-08-12]:** Occupancy is **not exclusive at the lane level** — multiple combatants may share the same lane. Refinement: each lane has **5 discrete positions**, mirrored symmetrically on both player and enemy sides (supersedes the player-side-only deferral in the errata above). A *position* is exclusive — only one combatant per (lane, position) slot — while a *lane* as a whole can hold up to 5 combatants across its positions. This is still primarily a rendering/layout structure, but it now also grounds reactive movement: a dodge can target either an adjacent **lane** or an adjacent **position** within the current lane, and either option can be blocked if the destination slot is already occupied. Damage/targeting/AoE mechanics that operate at lane granularity (Primal typing, Territory AoE, Zone/Positional archetypes) are unaffected — they continue to resolve against the lane index alone; position exists beneath that, for movement/collision/visual-spacing purposes only. Exact position layout (visual spacing, adjacency order) is pending numerical calibration.

**Movement cost model [DECISION LOCKED]:** Whether a given movement request costs an action turn or is free is decided by the context that triggers it (player-initiated reposition, a skill's Approach beat, a reactive dodge, etc.) rather than one fixed rule for all lane movement — the traversal system itself is cost-agnostic. See `Attack_Pattern_Directive_v0_1_0.md` Part 8 for how skill-triggered movement (Approach, automatic return-to-origin) uses this. Exact cost values per movement type remain pending combat system design/calibration.

### Lane Avoidance — Overworld Carry-Over
On the overworld, players can avoid visible enemy Phasix by choosing a lane far from the creature's patrol path. This stealth/avoidance mechanic carries the 7-lane logic into exploration — players who understand lane depth can skip encounters they don't want.

**Note (pending, see AUD-005):** this mechanic requires overworld "lanes" and enemy patrol paths, neither of which exist in the overworld yet — the current wild-creature scaffold is stationary and contact-only. This section describes the intended design; it is not yet implemented.

---

## Part 4 — Action Commands

### Timed Input System
Combat uses **action commands** — timed button presses during attacks and incoming hits:

- **Offensive action command:** Successfully timed press boosts outgoing attack damage. This is
  still the Mario RPG / Paper Mario model. Keeps combat active and attentive rather than passive
  menu selection.
- **Defensive action command — SUPERSEDED 2026-08-05** (user-directed, see DECISIONS.md -> [Combat]):
  the original "successfully timed press reduces incoming damage" model is replaced by a
  **full-avoidance Dodge/Parry system**, inspired by Clair Obscur: Expedition 33. The defender
  picks one of two options before the timing check runs:
  - **Dodge** — wide/easy timing window, success fully avoids the hit, no follow-up.
  - **Parry** — narrow/hard timing window, success fully avoids the hit AND triggers an automatic
    counter-attack against the attacker.
  Both options fail the same way as a total miss on offense: the hit lands at full damage, no
  extra penalty for attempting the harder Parry and missing ("reward, don't punish").

Some attacks/skills may eventually require **multiple action-command beats** in a single
attack — e.g. a multi-hit offensive skill with several timed presses, or a defensive sequence with
more than one Dodge/Parry check. Not built yet (skill tree framework, Step 4, is still scaffold
content only), but `BattleHUDController.RunTimedInput` is deliberately a single-window primitive
so a future multi-beat attack/skill can just call it multiple times in sequence rather than needing
a rewrite.

Exact timing windows, success thresholds, and damage modifiers are **pending numerical calibration** (NumericalCalibration.md).

### Design Intent
Action commands exist to keep the player present during every hit exchange. Even when a turn outcome is predetermined by stats, the player has agency over the margin. Skilled players are meaningfully rewarded without making the system inaccessible.

---

## Part 5 — Turn Structure

### Active Turn-Based
Combat is **active turn-based** — not real-time, not pure wait-based. Players select actions from a menu; execution involves real-time input (action commands).

### Party Size
**3–5 Phasix active on the field per side.** Exact number may vary by encounter type or progression stage. To be narrowed during combat prototyping.

### Action Economy
Actions per turn are **not fixed at 1**. Actions are a resource that scales with:
- Build choices
- Phasix type and type synergies
- Active buffs
- Potentially Phasix-specific traits or abilities

A fresh or unoptimized team might have 1–2 actions per turn. A well-built team with synergies active could have 3–4 or more. This makes action economy a meaningful build axis — not just stat optimization.

Full turn order model, speed priority, and action generation specifics are **pending combat system design**.

---

## Part 6 — Encounter Types in Combat

The three-layer encounter initiation system (Emotional Mirroring, Resonance/Attunement, Failure-Triggered) feeds into combat — but the encounter type may affect the battle framing, opening dialogue, or stakes rather than the mechanical combat rules themselves. Specifics pending.

The **combat-dialogue hybrid** (Vorthex prototype, Skill_phasix_develop) is a specific encounter subtype — not all combat uses the full threshold/tension system. Regular wild Phasix encounters use standard combat rules. The hybrid system is reserved for emotionally significant encounters.

---

## Pending Design — Flagged Gaps

The following are explicitly undecided and must not be filled speculatively:

- Full turn order model and speed priority system
- Exact party size within the 3–5 range
- Action generation specifics — what exactly produces additional actions (type synergy rules, buff types, trait triggers)
- Action command timing windows and damage modifiers (NumericalCalibration.md)
- Depth scale values per lane (NumericalCalibration.md)
- Transition visual identity (art direction pending)
- Status effects and how they interact with lane positioning
- Enemy AI decision-making framework (GDD §18.6 pending)
- Phasix size tiers and visual footprint widths (per species, Phase 5)
