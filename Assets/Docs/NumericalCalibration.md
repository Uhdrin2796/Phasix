# Phasix — Numerical Calibration Register
**Status: PENDING — Phase 2 calibration design task (GDD §29)**
All values here are PENDING. Do not hardcode any of these in scripts — use named constants
or ScriptableObject fields with placeholder values and a // TODO comment.

GDD §29 maps these as "Phase 2 — Numerical Calibration (requires Phase 1 systems complete)."

---

## How to Use This File
- When a value is decided through playtesting, fill in the "Final" column and date it
- Update the corresponding script constant or ScriptableObject field
- Add a CHANGELOG entry referencing the calibration decision
- Claude Code reads this file to know which values are still pending vs locked

---

## Attribute Base Values & Growth

### Base stats per tier (starting floor on devo return)
| Tier | Vitality | Force | Resonance | Guard | Ward | Resolve | Instinct | Aura | Aptitude |
|------|----------|-------|-----------|-------|------|---------|----------|------|----------|
| T1 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| T2 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| T3 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| T4 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| T5 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |

GDD dependency: §5 + §21 progression loop calibration

### Stat growth per level (by Temper direction weight)
- Edge: Force grows at `PENDING × weight(88)` per level, etc.
- Anchor: Vitality grows at `PENDING × weight(90)` per level, etc.
- Flux: Resonance grows at `PENDING × weight(88)` per level, etc.
- Player assignment: `PENDING` free points per level-up (across all attributes)
- Personality modifier range: `PENDING` % acceleration / `PENDING` % deceleration

---

## Unnamed Pool

### Growth formula
```
poolGrowth = (excessStatsAboveFloor × bondMultiplier)
```
| Bond % | bondMultiplier |
|--------|---------------|
| 0–19% (Stranger) | PENDING |
| 20–39% (Familiar) | PENDING (small) |
| 40–59% (Companion) | PENDING (moderate) |
| 60–79% (Partner) | PENDING (significant) |
| 80–99% (Bonded) | PENDING (large) |
| 100% (Complete) | PENDING (maximum) |

GDD note: "20% bond = small gain, 40% = moderate, 60% = significant, 80% = large" — exact multipliers pending.

### Per-tier floor values (minimum base stats before excess calculated)
| Tier | Floor value | Notes |
|------|------------|-------|
| T1→T2 | PENDING | Low — anti-exploit only, not a real wall |
| T2→T3 | PENDING | |
| T3→T4 | PENDING | |
| T4→T5 | PENDING | |

---

## Bond System Values

### Gain % per action
| Action | Gain | Notes |
|--------|------|-------|
| Win a battle | PENDING | Medium weight |
| Successful timed input | PENDING | Small per input |
| Craft gear for creature | PENDING | High — deliberate care |
| Complete survival task together | PENDING | Medium |
| Devo/re-evo cycle | PENDING | Highest single action |
| Exploration passive | PENDING | Minor over time |
| Preferred Origin activity | 2× multiplier on base gain | Locked |

### Loss % per event
| Tier | Amount | Examples |
|------|--------|---------|
| Micro | 0.5–1% | Flee, lose battle, left in reserve |
| Minor | 2–3% | Corruption overuse, repeated fleeing |
| Significant | 5–8% | Sustained Corruption abuse |
| Session cap | 5% max regardless | Locked |

Origin-specific loss signals (locked, rate pending):
- Corrupted: passive/defensive sessions → −PENDING%/session
- Ascended: sessions with no new discoveries → −PENDING%/session
- Hollow: aggressive/chaotic sessions → −PENDING%/session
- Primordial: heavy item/consumable use → −PENDING%/session

---

## Evolution Thresholds

### Level floors per tier (anti-exploit minimums)
| Tier transition | Level floor | Notes |
|-----------------|-------------|-------|
| T1→T2 | PENDING | "Low enough that natural play hits it quickly" (GDD) |
| T2→T3 | PENDING | |
| T3→T4 | PENDING | |
| T4→T5 | PENDING | |

### Evolution pacing targets
| Transition | Target sessions | Design intent |
|-----------|----------------|--------------|
| T1→T2 | 1–2 sessions | Fast — builds attachment early |
| T2→T3 | Few sessions | Moderate — first branch choice |
| T3→T4 | Notable investment | Longer — boss kills and Aptitude gates appear |
| T4→T5 | Significant investment | Long — Transcendent is an achievement |

---

## Battle System Values

### Damage formula constants
```
damage = (attackerStat / defenderStat) × skillPower × primalTypeMultiplier × timedInputBonus
```
- `timedInputBonus` on success: PENDING (e.g. 1.5×)
- `timedGuardReduction` on success: PENDING (e.g. 0.5×)
- Instinct → timing window size formula: PENDING
- Bond flat bonus to timing window: PENDING

### Aura costs
- Base skill cost range: PENDING
- Hold Tempo release scaling: PENDING
- Flow chain condition threshold: PENDING

### Combat length target
- Standard battle: 6–10 turns (locked design intent)
- DoT duration range: 4–6 turns
- Debuff duration range: 3–5 turns
- Control duration range: 1–3 turns
- Chain result duration range: 2–4 turns
- Exact base values: PENDING (calibrated during playtesting)

### Status magnitude ratings (for auto-cleanse check)
`if (target.resolve > status.magnitudeRating) → auto-cleanse after 1 turn`
| Status | Magnitude rating |
|--------|-----------------|
| Bleed | PENDING |
| Stun | PENDING |
| Burn | PENDING |
| (all 24 statuses) | PENDING |

---

## Signal Interaction Multipliers (logic locked, values pending)

| Interaction | Type | Multiplier |
|------------|------|-----------|
| Pulse attacks Current | AMP | PENDING |
| Static attacks Frequency | SUP | PENDING |
| Frequency attacks Static | SUP | PENDING |
| Frequency attacks Echo | AMP | PENDING |
| Silence attacks Pulse | AMP | PENDING |
| Silence attacks Current | SUP | PENDING |
| Overflow attacks Silence | AMP | PENDING |
| Overflow attacks Current | AMP | PENDING |
| Echo attacks Frequency | AMP | PENDING |
| Echo attacks Surge | AMP | PENDING |
| Surge attacks Overflow | AMP | PENDING |
| Catalyst attacks Static | AMP | PENDING |
| Current attacks Pulse | SUP | PENDING |

AMP = attacker's rhythm exploits defender's for bonus effect
SUP = attacker counters and reduces defender's rhythm effectiveness

---

## XP & Levelling

### XP required per level (within a tier)
- Design intent: "Fast early, progressively slower" within each tier
- Exact curve: PENDING (design during Phase 2 calibration)
- XP from winning a battle: PENDING (scales by opponent tier vs player tier)
- XP from losing: 0 (locked — no XP from losses)

### Loss costs (what losing a battle costs)
- Currency loss: PENDING
- Item loss: PENDING
- Bond loss from losing: 0 (losing battle ≠ bond loss; they are separate systems)

---

## Resource Economy (§22 — fully pending)
All economy values pending §22 design:
- Currency types and names: PENDING
- Shop price ranges: PENDING
- Capture item costs and probabilities: PENDING
- Crafting recipes and resource costs: PENDING
- Evolution item costs: PENDING
