# Phasix — Numerical Calibration Register
**Version:** 1.1.0 · **Updated:** March 2026
**Status: PENDING — Phase 2 calibration design task**
All values here are PENDING. Do not hardcode any of these in scripts — use named constants or ScriptableObject fields with placeholder values and a `// TODO` comment.

---

## How to Use This File
- When a value is decided through playtesting, fill in the "Final" column and date it
- Update the corresponding script constant or ScriptableObject field
- Add a CHANGELOG entry referencing the calibration decision
- Claude Code reads this file to know which values are still pending vs locked

---

## Attribute Base Values & Growth

### Base stats per tier (starting floor on devo return)
| Tier | Vitality | Force | Resonance | Guard | Ward | Resolve | Instinct | Aura |
|---|---|---|---|---|---|---|---|---|
| T1 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| T2 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| T3 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| T4 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| T5 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |

Note: Aptitude is NOT a base stat — it is a devolution counter stored as its own field. See Aptitude section below.

### Temper growth direction weights
- Edge: Force 88, Instinct 75, Resonance 58, Aura 52, Vitality 48, Guard 35, Ward 28, Resolve 22
- Anchor: Vitality 90, Guard 80, Ward 72, Resolve 68, Force 48, Aura 42, Instinct 35, Resonance 30
- Flux: Resonance 88, Aura 75, Ward 62, Instinct 52, Vitality 44, Force 32, Guard 25, Resolve 22

Growth rate per Common Aura point by weight: PENDING
Personality modifier range: PENDING % acceleration / PENDING % deceleration
Player free allocation weight: ~15% of total growth direction

---

## Aura System (Progression_Directive_v0_1_0.md)

### Common Aura
| Value | Amount | Notes |
|---|---|---|
| Drop per standard wild encounter | PENDING | Should feel steady — farmable |
| Drop per miniboss | PENDING | Larger payload |
| Drop per regional boss | PENDING | Largest standard payload |
| Cost per stat point (T1) | PENDING | |
| Cost per stat point (T2) | PENDING | |
| Cost per stat point (T3) | PENDING | |
| Cost per stat point (T4) | PENDING | |
| Cost per stat point (T5) | PENDING | |

### Stat Ceiling Per Tier (Aptitude 0 baseline)
| Tier | Base Ceiling | Notes |
|---|---|---|
| T1 | PENDING | Low — encourages early evolution |
| T2 | PENDING | |
| T3 | PENDING | |
| T4 | PENDING | |
| T5 | PENDING | Highest — deep development possible |

### Stat Ceiling Increase Per Aptitude Point
| Tier | Increase Per Aptitude | Notes |
|---|---|---|
| T1 | PENDING | |
| T2 | PENDING | |
| T3 | PENDING | |
| T4 | PENDING | |
| T5 | PENDING | |

### Specific Aura
| Value | Amount | Notes |
|---|---|---|
| Drop rate per species encounter | PENDING | Rarer than Common — not guaranteed |
| Drop rate per miniboss | PENDING | Reliable larger payload |
| Boss guaranteed drop | PENDING | Specific + Rare Variant mix |
| Quantities required T2→T3 | PENDING | Single realm type |
| Quantities required T3→T4 | PENDING | Two realm types |
| Quantities required T4→T5 | PENDING | Three+ realm types |

### Rare Variant Aura
| Value | Amount | Notes |
|---|---|---|
| Drop rate hidden encounters | PENDING | Rare by design |
| Drop rate boss encounters | PENDING | Guaranteed but small |
| Quantities required exotic branch | PENDING | Per branch — varies |

### Resonance Bonus Values
| Aligned Investment Level | Passive Bonus Type | Magnitude |
|---|---|---|
| Low alignment (25% points aligned) | PENDING | PENDING |
| Medium alignment (50% points aligned) | PENDING | PENDING |
| High alignment (75%+ points aligned) | PENDING | PENDING |
Specific passive types and scaling behavior: PENDING — requires skill tree content to be designed first

### Devolution Aura Cost
> **Devolution is FREE — no Aura cost, no conditions, no time limit.**
> Authority: Evolution_System_Directive_v1_1_0 (supersedes any prior cost language).
> This table has been removed to prevent implementation of a cost system that contradicts the directive.
> Do not add cost logic to devolution under any circumstances.

---

## Aptitude (Progression_Directive_v0_1_0.md)

Aptitude grows +1 per devolution cycle. It is a devolution counter, not a base stat.

### Aptitude thresholds for exotic evolution branches
These are per-branch values — designed during species roster phase. General guidance:
| Branch Rarity | Aptitude Minimum | Notes |
|---|---|---|
| Rare branch | PENDING | Requires meaningful cycling history |
| Very rare branch | PENDING | |
| Legendary branch | PENDING | |

---

## Evolution Thresholds

### Stat minimums per tier transition (replaces level floors — Progression_Directive_v0_1_0.md)
| Tier Transition | Stat Minimum Gate | Notes |
|---|---|---|
| T1→T2 | PENDING | Low — anti-exploit, natural play hits quickly |
| T2→T3 | PENDING | |
| T3→T4 | PENDING | |
| T4→T5 | PENDING | |

### Evolution pacing targets (Aura model)
| Transition | Target Sessions | Design Intent |
|---|---|---|
| T1→T2 | 1–2 sessions | Fast — builds attachment early |
| T2→T3 | Few sessions | Moderate — first realm exploration required |
| T3→T4 | Notable investment | Multi-realm Aura required |
| T4→T5 | Significant investment | Deep cross-realm + rare variant required |

---

## Unnamed Pool

### Growth formula
```
poolGrowth = (excessStatsAboveFloor × bondMultiplier)
```
Note: With Aptitude raising the stat ceiling, players who devolve at higher Aptitude will have accumulated more stats above the base floor — resulting in larger pool gains. This is intentional.

| Bond % | bondMultiplier |
|---|---|
| 0–19% (Stranger) | PENDING |
| 20–39% (Familiar) | PENDING (small) |
| 40–59% (Companion) | PENDING (moderate) |
| 60–79% (Partner) | PENDING (significant) |
| 80–99% (Bonded) | PENDING (large) |
| 100% (Complete) | PENDING (maximum) |

---

## Bond System Values

### Gain % per action
| Action | Gain | Notes |
|---|---|---|
| Win a battle | PENDING | Medium weight |
| Successful timed input | PENDING | Small per input |
| Craft gear for creature | PENDING | High |
| Complete survival task together | PENDING | Medium |
| Devo/re-evo cycle | PENDING | Highest single action |
| Exploration passive | PENDING | Minor over time |
| Preferred Origin activity | 2× multiplier on base gain | Locked |

### Loss % per event
| Tier | Amount | Examples |
|---|---|---|
| Micro | 0.5–1% | Flee, lose battle, left in reserve |
| Minor | 2–3% | Corruption overuse, repeated fleeing |
| Significant | 5–8% | Sustained Corruption abuse |
| Session cap | 5% max | Locked |

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

### Aura costs (skill energy)
- Base skill cost range: PENDING
- Hold Tempo release scaling: PENDING
- Flow chain condition threshold: PENDING

### Combat length target
- Standard battle: 6–10 turns (locked design intent)
- DoT duration range: 4–6 turns
- Debuff duration range: 3–5 turns
- Control duration range: 1–3 turns
- Chain result duration range: 2–4 turns

### Status magnitude ratings
`if (target.resolve > status.magnitudeRating) → auto-cleanse after 1 turn`
All 24 statuses: PENDING

---

## Signal Interaction Multipliers (logic locked, values pending)

| Interaction | Type | Multiplier |
|---|---|---|
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

## Calendar System (WorldDesign_Directive_v0_1_0.md)

### Month progression pacing
| Value | Amount | Notes |
|---|---|---|
| Story beats required per month advance | PENDING | Should feel earned, not rushed |
| Minimum sessions before month can advance | PENDING | Soft floor |

### Aura availability by season
| Season | Primary Aura Availability | Rare Variant Availability |
|---|---|---|
| School Year Arc (Sept–Nov) | PENDING | PENDING |
| Holiday Arc (Dec–Feb) | PENDING | PENDING |
| Thaw Arc (Mar–May) | PENDING | PENDING |
| Summer Arc (Jun–Aug) | PENDING | PENDING |

### Content window pacing
| Phase | Aura Yield Modifier | Notes |
|---|---|---|
| Month open | 1.0× (full) | Standard availability |
| Month active | 1.0× (full) | No pressure |
| Month closing | PENDING× (reduced) | Window narrowing |
| Month dormant | PENDING× (trickle) | Rare access only |
| Return cycle | 1.0× (full) | Reopens fresh |

---

## Resource Economy (§22 — fully pending)
- Currency types and names: PENDING
- Shop price ranges: PENDING
- Capture item costs and probabilities: PENDING
- Crafting recipes and resource costs: PENDING

---

## Combat System Values (Combat_Directive_v0_1_0.md)

### Lane depth scale values (7-lane system)
| Lane | Position | Scale Multiplier | Notes |
|---|---|---|---|
| Lane 1 | Front (closest) | PENDING | Largest sprites |
| Lane 2 | Near-front | PENDING | |
| Lane 3 | Near | PENDING | |
| Lane 4 | Mid (default) | PENDING | Starting position |
| Lane 5 | Far | PENDING | |
| Lane 6 | Back-far | PENDING | |
| Lane 7 | Back (furthest) | PENDING | Smallest sprites |
Note: Scaling is smooth and continuous between lanes — not stepped.

### Action command timing windows
| Command Type | Timing Window | Notes |
|---|---|---|
| Offensive action command | PENDING | Success = boosted outgoing damage |
| Defensive action command | PENDING | Success = reduced incoming damage |
| Success threshold | PENDING | Frame window for "success" input |

### Action command damage modifiers
| Result | Damage Modifier | Notes |
|---|---|---|
| Offensive success | PENDING (e.g. 1.5×) | Applied after base damage formula |
| Defensive success | PENDING (e.g. 0.5×) | Incoming damage reduction multiplier |
| Miss / no input | 1.0× baseline | No bonus, no penalty |

### Party size
| Value | Amount | Notes |
|---|---|---|
| Active Phasix per side | 3 (prototype) | Stored as `BattleConfig.ActivePartySize = 3`. Revisit at Phase 3 gate before building full battle UI — confirm or revise to final value then. |

---

## SUPERSEDED — Reference Only

### Old XP & Levelling (superseded by Aura system)
These values are superseded by the Aura system. Retained for historical reference only.
- XP required per level: SUPERSEDED
- XP from winning a battle: SUPERSEDED
- XP from losing: 0 (intent retained — no Aura from losses either)
- Loss costs (currency/items): PENDING (still relevant — loss costs resources, not progression)

### Old level floor values (superseded by stat minimums)
| Tier Transition | Level Floor |
|---|---|
| T1→T2 | SUPERSEDED |
| T2→T3 | SUPERSEDED |
| T3→T4 | SUPERSEDED |
| T4→T5 | SUPERSEDED |
