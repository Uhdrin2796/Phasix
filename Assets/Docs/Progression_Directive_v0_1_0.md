# Phasix — Progression Directive
**Version:** 0.1.0  
**Date:** March 2026  
**Status:** Supersedes GDD §21 Progression Loop (XP/leveling model)  
**GDD Refs:** §3 (Evolution), §5 (Unnamed Pool), §15 (Stat Scaling), §21 (Progression Loop)

---

## Overview

This directive replaces the XP/leveling model described in GDD §21 with an Aura-driven progression system. The core principle: **Phasix do not gain experience points. They grow through Aura — the emotional energy dropped by other Phasix in battle.**

Aura is both the currency of stat growth and the gate for evolution. The two functions are served by two distinct Aura categories: Common and Specific.

The GDD §21 leveling tables, XP formulas, and level-up stat recalculation logic are **superseded** by this directive. They are retained in the GDD as reference for historical context but should not be implemented as designed.

---

## Aura Categories

### Common Aura
- Dropped by all Phasix in battle regardless of species or region
- The primary, farmable progression resource
- Used to purchase stat increases within the current tier
- Abundant by design — moment-to-moment growth should feel steady
- No evolution gating function

### Specific Aura
- Dropped by particular Phasix species or boss encounters
- Tied to the emotional type of the Phasix that dropped it
- Required to trigger evolution thresholds — not stat growth
- Rarer than Common Aura by design
- Higher tier evolutions require Specific Aura from multiple realms simultaneously

### Rare Variant Aura
- A subtype of Specific Aura
- Found in deeper encounters, hidden sub-regions, or boss drops
- Gates exotic evolution branches not accessible through primary Aura alone
- Each region has a primary Specific Aura and at least one Rare Variant
- Farmable from bosses in larger payloads — boss farming has a defined purpose

---

## Stat Growth — Common Aura Allocation

### Free Allocation Model
Players spend Common Aura to allocate stat points freely across all available attributes. No fixed track. No prescribed growth path. The player shapes their Phasix's stat identity through deliberate investment.

### Resonance Bonus Layer
Stat points allocated to attributes that align with a Phasix's emotional type generate **Resonance Bonuses** — passive buffs that reward emotional coherence without forcing it.

Resonance Bonuses include (specifics pending NumericalCalibration.md):
- Passive abilities aligned with the Phasix's emotional type skill trees
- Improved attribute scaling when using emotionally aligned skills
- Type-specific bonuses that deepen the Phasix's identity within its emotional family

A player who consistently invests in grief-aligned stats on a grief-type Phasix gets meaningfully more out of those points than a player who cross-allocates freely. The system rewards knowing your Phasix.

### Tier Stat Ceiling
Stat growth through Common Aura is capped per tier. A T2 Phasix cannot be stat-pumped to T4 power. Hitting the ceiling is a natural signal that evolution is the next step.

The ceiling is not fixed — it scales with Aptitude. Higher Aptitude raises the ceiling, rewarding deeper development before evolution.

**All specific values, stat caps per tier, and Aura cost curves are pending — tracked in NumericalCalibration.md.**

---

## Aptitude System

Aptitude replaces the level cap mechanic from the original GDD design. It is a persistent stat that grows through devolution cycles and serves two functions.

### How Aptitude Grows
Aptitude increases by 1 each time a Phasix devolves. A Phasix that has never devolved has Aptitude 0. A Phasix that has cycled three times has Aptitude 3. Aptitude is specific to the individual Phasix — it reflects that creature's personal history.

### Function A — Raises Stat Ceiling Per Tier
Higher Aptitude increases how many stat points can be allocated within a tier before hitting the ceiling.

- Low Aptitude Phasix hits its stat ceiling quickly
- High Aptitude Phasix has significantly more room to develop before evolution becomes the only path
- Same underlying intent as the old level cap raise — expressed through stats instead of levels

**Side effect:** Phasix devolved at high Aptitude have accumulated more stats, meaning larger unnamed pool gains (GDD §5). Deeper development before devolution = bigger unnamed pool reward. The systems reinforce each other naturally.

### Function B — Unlocks Exotic Evolution Branches
Certain branches in the evolution graph require a minimum Aptitude value. These branches are not gated by Aura or stats alone — they require history.

- A branch requiring Aptitude 2 means this Phasix has lived through at least two devolution cycles
- The branch reflects accumulated experience, not just accumulated power
- These are among the rarest and most interesting branches in the evolution graph
- Aptitude-gated branches reward players who commit to cycling a single Phasix over time

---

## Evolution Gating — Full Three-Layer Model

Evolution requires all three layers to be satisfied simultaneously.

### Layer 1 — Aura Gate
Correct Specific Aura types collected in required quantities.

```
T1 → T2    Common Aura only (introductory — no regional requirement)
T2 → T3    Specific Aura from current realm (primary type)
T3 → T4    Specific Aura from 2 realms
T4 → T5    Specific Aura from 3+ realms + Rare Variant Aura
T5 → T6+   Fusion only — absorbed into fusion ingredient requirements
```

### Layer 2 — Stat Gate
Minimum stat thresholds must be met within the current tier. Replaces the level floor from GDD §3.

- Stat floors are directly observable — the player can see their stats
- Cannot evolve a neglected Phasix by farming Aura alone
- Specific minimum values pending species design phase

### Layer 3 — Conditional Gate

| Condition Type | Description |
|---|---|
| Aptitude minimum | Exotic branches only — requires devolution history |
| Bond level | Existing GDD condition — unchanged |
| Story/quest flag | Specific narrative beat must be completed |
| Emotional state | Player's tracked emotional context meets a threshold |
| Other conditionals | TBD in species design phase |

### Gate Legibility
All three layers are player-observable. Aura quantities are trackable. Stats are visible. Aptitude is a clear number. Nothing is hidden behind an opaque level number.

---

## Devolution — Aura Cost Model

Devolution costs Aura. It is a meaningful sacrifice, not a free reset.

- Devolving spends a portion of the Specific Aura used to achieve the current tier
- The player retains stat growth banked in the unnamed pool (GDD §5)
- The player retains all learned skills (GDD §3 — skill retention unchanged)
- The player may need to return to a region to rebuild Specific Aura reserves
- Returning to earlier regions with new context surfaces new encounters via Emotional Mirroring

**Exact Aura cost on devolution is pending — tracked in NumericalCalibration.md.**

---

## Positive Emotion Design Principle

No emotion is inherently good or bad. Every emotion is powerful. Every emotion has shadow.

Positive emotion Phasix are not support units. Joy that has been repeatedly interrupted burns intensely and briefly. Wonder that makes everything too much is enormous and slightly terrifying. Love at its most unguarded is among the most powerful and most vulnerable things in the game simultaneously.

### Mixed Aura Evolutions
The most complex emotional states require both positive and negative Aura simultaneously. A Phasix embodying bittersweet nostalgia cannot be evolved with grief Aura alone or joy Aura alone — it requires both. Mixed Aura evolutions are among the rarest branches. They require the player to have genuinely engaged with both ends of the emotional spectrum.

---

## Supersession Notes

### What This Replaces in GDD §21

| GDD §21 Element | Status |
|---|---|
| XP awarded on battle win | Superseded — replaced by Common Aura drop |
| Level threshold table (ScriptableObject) | Superseded — replaced by Aura cost curves |
| Stat recalculation on level-up | Superseded — replaced by free allocation model |
| Level floor in evolution requirements | Superseded — replaced by stat minimum gate |
| Aptitude raises level cap | Superseded — Aptitude now raises stat ceiling + unlocks exotic branches |
| No XP on loss | Retained — no Aura drop on loss, same intent |
| No stat regression on loss | Retained — loss costs resources only |

### What Remains Unchanged
- Battle loop cycle length and reward philosophy
- Exploration and development loop structure
- Unnamed pool growth on devolution (GDD §5)
- Bond milestone system
- Skill retention on devolution (GDD §3)

---

## Pending — NumericalCalibration.md Items Added

- Common Aura drop rates by encounter type
- Common Aura cost per stat point by tier
- Base stat ceiling per tier at Aptitude 0
- Stat ceiling increase per Aptitude point per tier
- Specific Aura drop rates by species and boss type
- Rare Variant Aura drop rates
- Specific Aura quantities required per evolution tier transition
- Aura cost on devolution formula
- Stat minimum thresholds per evolution branch (pending species design)
- Resonance Bonus specific values, passive types, and scaling behavior
