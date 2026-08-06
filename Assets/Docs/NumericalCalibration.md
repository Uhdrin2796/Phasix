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
Implemented `DamageCalculator.cs` / `TimedInputConfig.cs`, 2026-08-05 — **all values below are code
defaults, not playtested/tuned balance numbers.**
- `timedInputBonus` on success: 1.5× — **placeholder**, `TimedInputConfig.SuccessDamageMultiplier`
- Defense (superseded 2026-08-05, see DECISIONS.md -> [Combat]): no longer a damage-reduction
  multiplier — Dodge/Parry success is full avoidance (0× damage), fed into `BattleEngine` as a
  `damageMultiplier: 0f` on the queued attack, same mechanism as any other multiplier.
- Marker sweep duration: 1.2s, shared by offense and defense — **placeholder**,
  `TimedInputConfig.MarkerSweepDuration`. (`ParryMarkerSweepDuration` — Parry sweeping faster than
  Dodge — was removed 2026-08-05 when Dodge/Parry merged onto one shared bar/ring; Parry's
  difficulty is expressed by its tighter tolerance instead, see below.)
- Basic Attack `skillPower`: 10 — **placeholder** (`DamageCalculator.BasicAttackPower`); real skill
  content with its own power values is Step 4 (Roadmap_v2 Mo 6 Wk 3+)

### Action-command converging ring (superseded the horizontal timing bar, 2026-08-05 — see DECISIONS.md -> [Combat])
Both offense (`RunTimedInput`, on the targeted enemy) and defense (`RunDefenseTimedInput`, on the
defending creature) now use the same converging-ring visual: a fixed reference target ring plus a
white marker ring that starts wider and shrinks past it over the sweep. Success is judged by the
marker/target RADIUS RATIO at click time, not a bar position — **all values below are code
defaults, not playtested/tuned balance numbers.**
- Ring sizing (px, visual only, not gameplay-tuned): target ring `RingTargetRadius` 30, marker
  starts at `RingMarkerStartRadius` 60 and shrinks to `RingMarkerMinRadius` 2 —
  `BattleHUDController`
- Ratio tolerance half-width at 0 Instinct/bond (success = ratio within `[1-half, 1+half]` of the
  target): Offense `OffenseToleranceHalfWidth` 0.25 (reuses Dodge's — no Parry-equivalent
  precision mode), Dodge `DodgeToleranceHalfWidth` 0.25, Parry `ParryToleranceHalfWidth` 0.10 (the
  user's own example numbers: Dodge "1.25 to 0.75", Parry "0.9 to 1.1") — `TimedInputConfig`
- Instinct/bond scaling: `ComputeToleranceHalfWidth` reuses `ComputeWindowPercent`'s existing
  curve (`OffenseBaseWindowPercent 12% + Instinct×0.6% + bondBonus`, etc.) as a proportional scale
  factor on the tolerance half-width — "higher Instinct = larger window" (CLAUDE.md) still holds,
  just applied to a ratio instead of a bar-position window — **placeholder**
- Flash feedback colors on click resolution — reworked 2026-08-05 from per-move colors to
  per-OUTCOME-QUALITY colors shared by Dodge/Parry/offense alike (see DECISIONS.md -> [Combat]):
  green = normal success, neon purple = "perfect" (innermost 20% of the tolerance,
  `PerfectToleranceFraction`), red = any Miss — `BattleHUDController`, visual-only, not gameplay
  values. "Perfect" is exposed (`LastTimedInputWasPerfect`/`LastDefenseWasPerfect`) but not wired
  to any damage/avoidance bonus yet — visual feedback only for this pass.

### Aura costs (skill energy)
- Basic Attack Aura cost: 2 — **placeholder**, `BattleConfig.AttackAuraCost` (2026-08-05, user-
  directed — see DECISIONS.md -> [Combat]: "make them cost some aura"). Both placeholder attacks
  cost the same for now; spending never blocks the attack, it floors at 0 (`BattleParticipant.SpendAura`)
- Perfect Dodge/Parry Aura restore: 2 — **placeholder**, `BattleConfig.PerfectDefenseAuraRestore`
  (user-directed: "Perfect dodges and parrys restore aura"), clamped at MaxAura
- Base skill cost range: PENDING
- Hold Tempo release scaling: PENDING
- Flow chain condition threshold: PENDING

### Step 5 scaffolding — Aura stat allocation, capture, evolution burst
Implemented 2026-08-05 — `AuraTierCeiling.cs`, `AuraStatAllocationSystem.cs`,
`ResonanceBonusEvaluator.cs`, `CaptureSystem.cs`, `EvolutionBurstSystem.cs`. **Almost nothing in
this section is anchored to a locked GDD number — Progression_Directive_v0_1_0.md and GDD §9.3
lock the mechanic shapes but explicitly leave every actual value pending.** See DECISIONS.md ->
[Progression/Combat] for the two interpretation calls (Resonance Bonus's Temper-proxy, Capture's
lack of any locked formula at all) behind these numbers.
- Common Aura cost per stat point: 1 (flat) — **placeholder**, `AuraStatAllocationSystem.AuraCostPerStatPoint`;
  Progression_Directive's own pending list expects this to vary by tier eventually, not stay flat
- Stat ceiling per tier: `tier × 40 + aptitude × 4` — **placeholder**, `AuraTierCeiling`; the
  Directive locks "ceiling scales with Aptitude," not the formula
- Resonance Bonus alignment multiplier: 1.15× when the stat is in the Phasix's Temper's top 3
  growth-priority stats, else 1.0× — **placeholder** magnitude AND placeholder alignment proxy
  (Temper stands in for the Directive's undesigned emotional-type mapping — see DECISIONS.md)
- Capture chance: `10% + (1 - targetHPFraction) × 60%`, clamped 0-95% — **placeholder**,
  `CaptureSystem`; no formula of any kind exists in the GDD to anchor this to, unlike every other
  placeholder in this document which at least has a locked mechanic shape
- Evolution burst trigger threshold: 100% gauge fill — **placeholder**, `EvolutionBurstSystem.TriggerThreshold`
- Evolution burst reliable-trigger bond threshold: 40% (Companion) — **locked**, GDD §14.2's own
  "Evolution burst reliable" language at the Companion milestone
- Evolution burst trigger chance below 40% bond: 40% (even at full gauge) — **placeholder**,
  `EvolutionBurstSystem.UnreliableTriggerChancePercent`; the GDD only implies unreliability below
  the Companion threshold, gives no number
- Evolution burst duration: `2 + bondPercent/100 × 3` turns — **placeholder**,
  `EvolutionBurstSystem.BaseDurationTurns`/`MaxBondDurationBonusTurns`; "higher bond = longer
  duration" IS locked (GDD §9.3), the curve is not

### Combat length target
- Standard battle: 6–10 turns (locked design intent)
- DoT duration range: 4–6 turns
- Debuff duration range: 3–5 turns
- Control duration range: 1–3 turns
- Chain result duration range: 2–4 turns

### Status magnitude ratings
`if (target.resolve > status.magnitudeRating) → auto-cleanse after 1 turn`
All 28 statuses: PENDING (corrected from "24" — see "Skill tree / status / combo framework" below;
magnitude ratings themselves are a separate, still-fully-open number from the duration ranges
already implemented)

### Skill tree / status / combo framework
Implemented 2026-08-05 — `Assets/Scripts/Combat/StatusEffectCatalog.cs`,
`StatusDurationCalculator.cs`, `ChainResultCatalog.cs`, `MasteryBonusCatalog.cs`, `ComboEngine.cs`,
`SkillTreeCatalog.cs`, `SkillSlotCapacity.cs`. **All values below are code defaults, not
playtested/tuned balance numbers — the mechanics/taxonomy are locked (GDD §4, §14, §15, §17),
these numbers are not.**
- Per-status base duration: within GDD's own locked category ranges (DoT 4-6, Debuff 3-5, Control
  1-3 [Stun pinned to 1], Signal 3-5) — exact per-status number **placeholder**,
  `StatusEffectCatalog.Get(type).MinDurationTurns/MaxDurationTurns`
- Universal/Positive category duration range: 3-5 — **placeholder**, no GDD-stated range for these
  two categories at all (chose to match the Debuff range rather than invent a new one)
- Stat-to-duration-modifier conversion (Resonance extends / Resolve shortens): 10 stat points = 1
  turn — **placeholder**, `StatusDurationCalculator.StatPerModifierPoint`; the formula shape
  itself (`base + ResonanceModifier - ResolveModifier, min 1`) IS locked (GDD §17.2)
- Combo trigger chance: `10% + Instinct × 1%`, clamped to 80% max — **placeholder**,
  `ComboEngine.BaseTriggerChancePercent`/`PerInstinctTriggerChancePercent`/`MaxTriggerChancePercent`;
  the GDD locks only "Instinct increases trigger chance," no formula
- Combo discovery bonus: 0% at/below 60% bond, ramping linearly to 30% at 100% bond —
  **placeholder** ramp shape and 30% ceiling, `ComboEngine.MaxDiscoveryBonusPercent`; the "above
  60% bond" threshold itself IS locked (GDD §4.2)
- Chain result tie-break (when a target's active statuses satisfy two different chain recipes at
  once): first match in `ChainResultCatalog`'s declaration order — **placeholder**, not a locked
  resolution rule (the GDD doesn't address simultaneous-match ordering)

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
Implemented 2026-08-05, defense reworked to Dodge/Parry same day (Expedition 33-inspired, see
DECISIONS.md -> [Combat]) — see `TimedInputConfig.cs`. **All placeholder, not playtested balance.**

| Command Type | Timing Window | Sweep Duration | Notes |
|---|---|---|---|
| Offensive action command | `ComputeWindowPercent(attacker.Instinct, attacker.bondPercent)`, base 12% | 1.2s | Success = boosted outgoing damage (1.5×). Uses the *attacker's* stats. |
| Dodge (defensive) | `ComputeWindowPercent(DodgeBaseWindowPercent, defender.Instinct, defender.bondPercent)`, base 20% | 1.2s | Success = fully avoids the hit. Uses the *defender's* stats. Wide/easy — the "safe" option. |
| Parry (defensive) | `ComputeWindowPercent(ParryBaseWindowPercent, defender.Instinct, defender.bondPercent)`, base 6% | 0.7s | Success = fully avoids the hit AND triggers an automatic counter-attack. Uses the *defender's* stats. Narrow/hard, faster sweep — the "risky" option. |
| Success threshold | Click the bar's button while the marker (sweeping over the mode's duration) is inside the randomly-positioned success zone | — | Zone width = the computed window %, position randomized each attempt |

### Action command damage modifiers
| Result | Damage Modifier | Notes |
|---|---|---|
| Offensive success | 1.5× — **placeholder** | `TimedInputConfig.SuccessDamageMultiplier`, applied after base damage formula |
| Dodge success | 0× (full avoidance) | No damage line in the battle log at all |
| Parry success | 0× (full avoidance) + automatic counter-attack | Counter uses the same basic-attack damage formula, no timing check of its own |
| Miss / no input (timeout) — offense | 1.0× baseline | No bonus, no penalty — matches Combat_Directive's "reward, don't punish" intent |
| Miss / no input (timeout) — Dodge or Parry | 1.0× baseline (full hit) | Same "reward, don't punish": a failed Parry attempt costs nothing extra vs. a failed Dodge or a plain miss |

### Party size
| Value | Amount | Notes |
|---|---|---|
| Active Phasix per side | 3 (prototype) | Stored as `BattleConfig.ActivePartySize = 3`. Revisit at Phase 3 gate before building full battle UI — confirm or revise to final value then. |

---

## Overworld Movement (PlayerTopDownController.cs)

### Corner correction nudge
| Value | Amount | Notes |
|---|---|---|
| Corner correction threshold | 3px (0.1875 world units at 16 PPU) — **placeholder, mechanism verified 2026-08-04** | Max lateral clip distance from a wall corner that gets auto-nudged through. Added per `AUDIT_202608.md` AUD-007. The threshold value itself is still an unverified placeholder, but the mechanism was fixed (raycast origin bug) and confirmed working live against real `SampleScene` wall geometry — see `KNOWN_ISSUES.md` closed `[AUD-007]`. Still needs numeric calibration against real doorway/interior geometry once the map-expansion backlog lands. |

### Sprint
| Value | Amount | Notes |
|---|---|---|
| Sprint speed multiplier | 1.6x — **placeholder, playtested 2026-08-04** | Applied to `_moveSpeed` while the "Sprint" action is held. AUD-005. No stamina system. |

---

## Camera Lookahead (CameraFollow.cs)
Added per `AUDIT_202608.md` AUD-006, playtested live 2026-08-04 — see `KNOWN_ISSUES.md` closed
`[AUD-006]`.

| Value | Amount | Notes |
|---|---|---|
| Max lookahead distance | 1.5 world units — **placeholder, playtested** | Confirmed the offset reaches exactly this value at/above `_velocityForMaxLookahead`. |
| Velocity for max lookahead | 8 u/s — **matches player's Sprint top speed exactly** | 5 base move speed x 1.6 sprint multiplier. Update this if either value changes. |
| Smooth time | 0.25s — **placeholder** | `Vector2.SmoothDamp` easing, on top of `CinemachineFollow`'s own `TrackerSettings` damping. |

---

## Wild Creature Patrol/Detection (WildEncounterCreature.cs)
Added per `AUDIT_202608.md` AUD-005, playtested live 2026-08-04 — see `KNOWN_ISSUES.md` closed
`[AUD-005]` for the full design rationale and a collider bug found during playtest.

| Value | Amount | Notes |
|---|---|---|
| Patrol speed | 1 u/s — **placeholder** | Well below player base move speed (5). |
| Patrol half-range | 1.5 world units — **placeholder, placement-dependent** | Straight-line back-and-forth only; tune per spawn point so the path stays clear of walls/decorations. |
| Detection radius | 3 world units — **placeholder** | |
| Detection cone angle | 100° — **placeholder** | Full cone width, centered on current facing/movement direction. |
| Detection check interval | 0.15s — **placeholder** | Throttled, not per-frame. |
| Alert (chase) speed | 2 u/s — **placeholder** | Deliberately kept below player base move speed (5) so a detected creature is never inescapable. |
| Lose-interest delay | 2s — **placeholder** | Seconds without line-of-sight before an alerted creature resumes patrol. |
| Encounter trigger collider (`Phasix_WildEncounter.prefab` `CircleCollider2D`) | offset `{0,2}`, radius `1.2` (local; world ≈ offset `1.0`, radius `0.6`) — **verified live, not a placeholder** | Sized to reliably overlap the player's `CapsuleCollider2D`'s actual world collision band (offset ~1.4 world units above the player's own pivot — see AUD-007), not to visually match the creature's small placeholder sprite. Deliberately generous, since this is a pure trigger with no "sticky movement" downside. |

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
