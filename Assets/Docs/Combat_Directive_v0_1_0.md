# Phasix — Combat Directive
**Version:** 0.1.0  
**Date:** April 2026  
**Status:** New — supplements GDD combat sections  
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

Whether lane movement costs an action turn or is a free reaction is **pending combat system design**.

### Lane Avoidance — Overworld Carry-Over
On the overworld, players can avoid visible enemy Phasix by choosing a lane far from the creature's patrol path. This stealth/avoidance mechanic carries the 5-lane logic into exploration — players who understand lane depth can skip encounters they don't want.

---

## Part 4 — Action Commands

### Timed Input System
Combat uses **action commands** — timed button presses during attacks and incoming hits:

- **Offensive action command:** Successfully timed press boosts outgoing attack damage
- **Defensive action command:** Successfully timed press reduces incoming damage

This is the Mario RPG / Paper Mario model. Keeps combat active and attentive rather than passive menu selection.

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
- Whether lane movement is a free action or costs an action
- Action command timing windows and damage modifiers (NumericalCalibration.md)
- Depth scale values per lane (NumericalCalibration.md)
- Transition visual identity (art direction pending)
- Status effects and how they interact with lane positioning
- Enemy AI decision-making framework (GDD §18.6 pending)
- Whether the 7-lane system applies symmetrically to both sides
- Phasix size tiers and visual footprint widths (per species, Phase 5)
