# Phasix — Attack Pattern Directive
**Version:** 0.1.0
**Date:** August 2026
**Status:** Working — design exploration, not yet locked. Captures direction for the next coding session.
**Errata (2026-08-12):** Part 2 gained AI Targeting Visualization. Part 5 gained two new archetypes —
Lane Displacement Attack (Reflex-based) and a worked Telegraphed Area Attack example under
Multi-Turn Buildup. Part 8 gained the Tank response option, per-lane/blind-side resolution rule, and
the occupancy-model resolution (7 lanes × 5 positions — see `Combat_Directive_v0_1_0.md`'s matching
2026-08-12 errata). Part 9 resolved the Strike Points/sub-position question — stays independent, no
model change. Part 10 updated to match. Addition/refinement only — no version bump.
**Errata (2026-08-12, later same session):** Part 1 gained a recommended build order for the
still-unbuilt Part 5/6 archetypes, sequencing them by shared infrastructure vs. standalone lift —
see the Status tracker's new subsection. Addition only — no version bump.
**Errata (2026-08-12, later still):** Group 1 built — Instant Strike/Read-the-Tell, Feint,
Metronome/Jitter, Direct Projectile (confirmed already-implemented). Required a new mechanic beyond
what Part 1's "near-free" framing implied: a real pre-emptive timed-input window during Windup
(`ResponseTimingType`, `SkillTreeType.Testing` for isolated test content) — see CHANGELOG.md's
matching entry for full detail. Addition only — no version bump.
**Errata (2026-08-13):** Playtesting Group 1 drove two follow-ups, both superseding this doc's
original Metronome/Jitter framing ("not new systems, just windup-timing data patterns"): (1) Instant
Strike/Feint's real strike resequenced to hop -> tell/ring -> projectile-shot (previously concurrent);
Feint's fake beat lost its ring entirely (no tell precedes it, nothing to react to). (2) Metronome and
Jitter are no longer thin variations of Instant Strike's shape at all — each is now a per-battle
stacking rhythm combo (`StackingRhythmType`, `BattleManager.ResolveStackingRhythmAttack`) with its own
dedicated resolution path that bypasses the Beat Sequence engine entirely: every cast in a battle
requires one more ring beat than the last, alternating dash-forward/dash-back, only a fully-cleared
combo firing a stronger payoff shot. See CHANGELOG.md's matching entry for full detail — this is a
genuine mechanic addition, not something the original archetype descriptions anticipated. Not
reflected in Part 5's archetype table below (left as historical design-intent record) — treat this
errata as the authoritative current behavior for these two skills.
**Errata (2026-08-15):** Group 2 started — Multi-Hit Volley built (first pattern, "Basic Count":
8 hits cycling all 8 compass positions in order). Needed genuinely new infrastructure beyond
"calls RunTimedInput multiple times in sequence" (Part 1's own framing): several rings can now be
open/animating around the target concurrently (new `CompassPoint`-positioned ring pool in
`BattleHUDController`, replacing the single shared `_timingRing` for this archetype), resolved via
a FIFO click-routing session where only the oldest open ring listens for input, and each ring's
required click button is tied to its own marker shape (circle = left, square = right — originally
sweep direction/converging-vs-expanding, superseded same-day once that proved hard to read at a
glance; see the tuning-knobs section below) — see `SkillData.VolleyRingSequence`/`BattleManager
.ResolveMultiHitVolleyAttack`. Offense only this pass — defense (an enemy casting Volley) is
dispatch-wired but blocked on `CombatVfxController._held`
being a single-slot field; see CHANGELOG.md's matching entry for full detail. Not reflected in Part
5's archetype table below (left as historical design-intent record).
**GDD Refs:** §14 (Skill Taxonomy), §16 (Battle System), §17 (Status Effects)
**Related:** Combat_Directive_v0_1_0.md (lane system, timed input system, lane occupancy — authoritative for those mechanics; this doc builds on top)

---

## Overview

This directive captures a reusable **telegraph/attack-pattern framework** so that individual skill content (Phase 5, species roster) can be authored quickly and consistently instead of every move being bespoke. It is a parameterized system: a small set of knobs, and named archetypes that are common knob combinations.

Nothing here overrides existing locked systems (timed input, lane movement, status loop, Primal typing). This doc is the missing layer between "we have a timed-input system" and "here's what 50 different skills actually feel like."

---

## Part 1 — Build Order & Session Guide

Read this before touching code. It's the map for what to build, in what order, and how to know each piece works before moving to the next.

### Dependency chain
1. **Lane Movement / Traversal** (Combat_Directive's Lane Movement section + Part 8 below) — nothing else can be honestly validated without this. Covers: cost-agnostic movement requests, lane occupancy/spacing (now locked in Combat_Directive), pre-battle placement, zone targeting.
2. **Telegraph Knob Schema** (Part 2) — extend the existing `SkillData.cs` stub with the knob fields from the table below. Pure data, no behavior required yet.
3. **Beat Sequence** (Part 7) — Approach/Windup/Attack state machine. Built on #1 for all movement, and the existing timed-input system for the Attack beat's press/guard. Placeholder animation is Transform-tweening, not drawn art (see confirmed decision below).
4. **Strike Points** (Part 9) — layers onto #3 as another beat parameter. Needs #1 for the relative-offset math.

Ranged-side systems that need new work (Zone/Positional, Split Attention, Sustained Pressure, Multi-Turn Buildup, Windup-Applies-Status — see Part 5) depend on #1 for movement/zone response, but are otherwise independent of melee. Build them in any order once #1 exists, based on whichever attack you want to prototype next — there's no dependency between them.

### Validation approach
For each system: build one deliberately **minimal** example and one deliberately **maximal** example before populating the rest of the content catalog (Parts 5–6). The framework should prove it can handle both extremes before content gets layered on.
- **Lane Movement:** a single reposition, and a full multi-hop Approach → Approach → return-to-origin cycle (no interrupt branch — see Part 7).
- **Beat Sequence:** a plain Slash (`Approach → Windup-Real → Attack`), and the Shadow teleport-strike sequence (fakes, multiple approaches, strike points).

### Status tracker
| System | Status | Depends on | Min example | Max example |
|---|---|---|---|---|
| Lane Movement / Traversal | **Built 2026-08-11** — real per-combatant LaneIndex, depth scale, cost-agnostic stepping (`LaneMovementSystem.cs`); non-exclusive occupancy spacing built for the player side, enemy side deferred until multi-enemy battles exist (see DECISIONS.md -> [Combat]) | — | Single reposition — done (Approach) | Multi-hop Approach + return — not yet exercised (Slash is a single-hop case) |
| Telegraph Knob Schema | **Built 2026-08-11** — minimal version: `SkillData.BeatSequence` (ordered `BeatType[]`) plus `BeatSequenceConfig.cs` for shared timing constants. Full Part 2 knob table (input verb, tell predictability, area shape, etc.) NOT built — only what Slash needed | — | One SkillData asset, knobs filled — done (`Melee_Slash`) | — |
| Beat Sequence | **Built 2026-08-11** — `BeatSequenceRunner.cs` (Approach/Windup/Return), `BattleManager.ResolveMeleeBeatSequence`/`ResolveMeleeAttackBeatOffense`/`Defense` | Lane Movement | Slash — done, live in `SkillDatabase` | Shadow teleport-strike — not built |
| Strike Points | Spec'd (Part 9) — not built | Beat Sequence, Lane Movement | Single Front strike | Front→Rear→Flank chain |
| Zone/Positional & Split Attention | Not designed | Lane Movement | — | — |
| Sustained Pressure | Not designed | — | — | — |
| Multi-Turn Buildup | Not designed | — | — | — |
| Windup-Applies-Status | Not designed | Status system (§17, locked) | — | — |

### Recommended build order for Part 5/6 archetypes (added 2026-08-12)
None of Part 5's archetypes are built yet beyond the melee Beat Sequence's own minimal example
(Slash). This order sequences them by how much they reuse what already exists vs. how much genuinely
new mechanical infrastructure each one needs — front-loading the cheap, infrastructure-reusing ones
validates "ranged doesn't need Approach" and the shared timing primitives before committing to the
archetypes that need a wholly new input model or temporal structure.

**Group 1 — BUILT 2026-08-12 (see CHANGELOG.md's matching entry for full detail):**
1. **Instant Strike / Read-the-Tell** — structurally almost identical to Slash's
   `Windup-Real → Attack`, just without the Approach beat (no lane-closing) and no travel time.
   Cheapest possible ranged skill; proves ranged doesn't need Approach at all. **Built** as
   `Ranged_InstantStrike` — this needed one genuinely new mechanic beyond what "near-free" implied: a
   real pre-emptive timed-input window during Windup (new `ResponseTimingType`), since the only ring
   in the Beat Sequence path previously fired on the Attack beat, after Windup already finished.
2. **Feint** — `Windup-Fake` already exists from Slash; applying it to Instant Strike just proves the
   fake/real read generalizes past melee. **Built** as `Ranged_Feint`; depended entirely on #1's
   pre-emptive window (a fake windup with no reactable moment has no mechanical effect) — its
   `WindupFake` beat opens the identical prompt shape but its outcome is always discarded.
3. **Metronome/Learned Rhythm & Jitter/No Fixed Rhythm** — not new systems, just windup-timing data
   patterns (fixed vs. randomized) applied to whatever skill from #1 already exists. **Built** as
   `Ranged_Metronome`/`Ranged_Jitter` — on closer read, Metronome needed zero new plumbing (every Beat
   Sequence skill already plays a fixed duration by default, which already **is** the "steady,
   learnable" archetype); only Jitter needed a new field (`SkillData.WindupJitterRangeSeconds`,
   `BeatSequenceRunner.ComputeWindupDuration`).
4. **Direct Projectile** — check first whether the existing `BuiltInMoveType.Attack` baseline already
   covers this (the Worked Examples table below calls it "current implemented pattern") before
   spending build time on it — may already be effectively done. **Confirmed already done** — in fact
   implemented twice (`ResolveSkillAction`'s placeholder-damage branch and `ResolveBuiltInMove`'s
   `BuiltInMoveType.Attack` case are both full Direct Projectile implementations). No build work.

**Group 2 — one dedicated pass each (new input primitives):**
5. **Multi-Hit Volley** — **BUILT 2026-08-15** (offense only — see errata above and CHANGELOG.md's
   matching entry) as `Ranged_MultiHitVolley` ("Volley"). Turned out to need more than "calls
   `RunTimedInput` multiple times in sequence": each hit needed its own independently-timed ring at
   its own position around the target, with several open concurrently — a genuinely new
   `CompassPoint`-positioned ring-pool + FIFO click-routing system (`BattleHUDController`), not a
   reuse of the existing single-ring primitive. A second pattern (different compass order/timing) is
   planned as pure new-asset authoring, no code changes needed. Defense (an enemy using Volley)
   is dispatch-wired but blocked on a `CombatVfxController` refactor — flagged as a follow-up.
6. **Charge & Release** + **Sustained Pressure** — build these two *together*: both are "hold input"
   instead of "tap input," diverging only in scoring (release timing vs. duration matching). Share
   one new hold-input primitive.

### Multi-Hit Volley — tuning knobs reference (added 2026-08-15, updated same day)
Before authoring another Volley pattern (after "Basic Count," "Double Tap," "Tracking Volley"),
here's what each `SkillData` field actually controls — asked for explicitly so the next variant
doesn't need this rediscovered from scratch. All are per-hit/per-skill, pure data — no code changes
needed to try a new combination.

| Knob | Field | What it changes |
|---|---|---|
| **Hit count** | `VolleyRingSequence`'s length | Overall length/commitment of the cast. Also sets where the left-click/right-click (circle/square marker) split falls — always "first half left-click, second half right-click," so an 8-hit volley splits 4/4, a 4-hit splits 2/2, etc. |
| **Per-hit projectile speed** | `VolleyRingDurationsSeconds[i]` | How urgent that specific hit feels — smaller is harder. Since the 2026-08-15 sync fix, this value **is** the projectile's real travel time; the ring's own displayed sweep is *derived* from it (stretched ~1.9x — see `BattleHUDController.ComputeVolleyRingSweepDuration`), so this one number moves both the shot's speed and the ring's open-time together. They are not independently tunable right now (see "Sync fix tradeoffs" below). |
| **Shot cadence / rhythm — approach** | `VolleyDashForwardDurationsSeconds[i]` | How long the player waits BEFORE hit i+1 launches. A pause belongs here, on the hit AFTER which the gap should appear. |
| **Shot cadence / rhythm — return** | `VolleyDashBackDurationsSeconds[i]` | How long hit i+1's own return takes AFTER it fires, before the next hit's own approach can begin. **Deliberately a separate field from the forward one above** (2026-08-15, same-day fix — a single shared value for both legs can't express "pause only before this hit" without also pausing after it, since one number drove both; see "Double Tap gap fix" below). Keep this short/uniform unless you specifically want extra breathing room AFTER a hit too. Both arrays fall back independently to the flat global default (`BeatSequenceConfig.VolleyDashLegDurationSeconds`) when empty/short. |
| **Position order** | `VolleyRingSequence`'s *values*, not just length | Purely spatial variety — nothing forces the compass-clockwise order every existing pattern uses. Jumping around (e.g. N, S, E, W, NE, SW, SE, NW) or repeating a position reads as more chaotic without changing difficulty or rhythm at all. |

**Left/right-click marker shape (2026-08-15, same day):** originally encoded via animation direction
(a converging ring vs. an expanding one) — user feedback: "its still not clear enough... make it
more distinct," direction needed a couple frames of motion to read. Replaced with marker SHAPE
(`RingVisual.MarkerIsSquare`) instead: left-click rings draw as a circle, right-click as a square,
readable from a single glance. Every ring now animates identically (always shrinking/converging) —
shape carries the click-type signal, motion no longer does, so this isn't a separate tunable knob,
just how the derived left/right split (row above) gets drawn.

**Double Tap gap fix (2026-08-15, same day):** the cadence field used to be ONE shared value per hit
for both its forward and back leg. Putting a long value on hit 3 (to create the pause before it)
also stretched hit 3's OWN return leg by the same amount — creating a second, unintended pause
before hit 4 too. Worked the math: with a single symmetric value per hit there's no combination that
makes gap(1→2) equal gap(3→4) while keeping one real pause in the middle; negative durations would
be required. Splitting into independent forward/back arrays fixed it — put the pause on the
FORWARD leg of whichever hit should have a gap before it, keep every BACK leg uniform, and the gaps
on either side of the pause come out symmetric. Double Tap's actual current values: forward
`[0.16, 0.16, 0.8, 0.16]`, back `[0.16, 0.16, 0.16, 0.16]` — gap(1→2) = gap(3→4) = 0.32s, gap(2→3) =
0.96s (the one real pause).

**Sync fix tradeoffs (2026-08-15, see CHANGELOG.md and DECISIONS.md -> [Combat] for the full
writeup):** the projectile-speed/ring-open-time coupling above was a deliberate choice, not the only
option — three approaches were weighed:
1. **Shrink the projectile to match the ring** (ring duration stays exactly as authored; projectile
   speeds up/slows to land at the ring's natural perfect-instant). Keeps every authored ring number
   literally true, but projectiles can look unnaturally fast relative to the "standard projectile"
   feel the archetype was designed around.
2. **Stretch the ring to match the projectile** (chosen) — projectile keeps its authored speed
   exactly; the ring's displayed sweep is derived/stretched instead. Projectile feel stays correct
   and consistent with the rest of the game's sync philosophy (`ComputeSweepDurationForTravelTime`),
   at the cost of ring-open-time no longer being an independently tunable number.
3. **Decouple start times instead of either duration** (not built) — launch the projectile earlier
   relative to the ring opening, so both keep their exact authored speeds and only their *starting
   offset* changes. Would preserve full independent control of both knobs, but is a real structural
   change (`RunVolleyHit` currently launches both together) and risks the projectile visibly being
   mid-flight before its own telegraph/ring appears — undercutting the "tell, then react" framing.

If a future pattern needs ring-open-time and projectile-speed tuned independently, option 3 is the
one to revisit — not a quick asset tweak, budget it as its own small pass.

**Group 3 — one dedicated pass, builds directly on the 2026-08-12 formation grid work:**
7. **Zone/Positional** — the Lane Selection input model (no timing, just picking lanes) plus a
   visible "marked lanes" tell. The defender's Evade/Tank response reuses the player position system
   already built this session (`FormationSystem.cs`, `LaneMovementSystem.cs`).
8. **Split Attention** — build immediately after #7, not standalone — it's Zone/Positional with two
   simultaneous marks (one or both fake), same infrastructure.

**Group 4 — small, isolate to verify no regression:**
9. **Counter-Bait** — needs tracking "did the player just guard" as a trigger condition. Small, but
   worth its own verification pass since it hooks existing guard state.
10. **Windup-Applies-Status** — check the status engine's current hook points first (does it fire only
    at Attack resolution, or can it trigger mid-Windup?) before starting; likely small if the engine is
    already wired into live battle.

**Save for last — genuinely bigger lifts, don't bundle with anything else:**
11. **Multi-Turn Buildup** (+ the Telegraphed Area Attack worked example) — a new temporal structure
    (decisions made across multiple turns), not a reflex test at all. Biggest lift in Part 5.
12. **Lane Displacement Attack** — gated behind building the reactive dodge-to-adjacent-slot mechanic
    first (see DECISIONS.md -> [Combat] "Enemy-side position support" for why this is also shared
    groundwork any future enemy-side work would need).
13. **Strike Points** (Part 9) — not really a Part 5 archetype; treat as a capstone once a few of the
    above exist, to validate Beat Sequence's own stated maximal case (the Shadow teleport-strike
    example from the Validation approach above).

### Confirmed — placeholder animation approach
Melee beats use **Transform-tweening (DOTween)** on the existing placeholder sprite, not drawn art:
- **Windup (Real/Fake):** scale-squash or backward lean, held for the windup duration — same motion for real and fake, only duration differs (this is also how you'll actually test whether players can tell them apart).
- **Attack:** fast forward lunge (position offset + snap-back) or a rotate-snap.
- **Approach / Return:** position tweens; Return uses the arc-hop shape (Part 7).

This validates timing and state-machine correctness now, with zero art dependency. Real animation replaces the tweens later without any change to Beat Sequence code — same framework-now/content-later pattern as the rest of this doc.

---

## Part 2 — Core Telegraph Knobs

Every attack (ranged or melee) can be described by these parameters, set per-skill:

| Knob | Options |
|------|---------|
| Input verb | tap / hold / move-lane / multi-tap sequence |
| Tell location | on attacker (windup animation) / on battlefield (zone marker) / both |
| Tell predictability | fixed rhythm / randomized / escalating over turns |
| Windup length | instant / short / long / multi-turn |
| Resolve type | travels across space (hit-time = distance) / resolves in place once triggered |
| Response timing | pre-emptive (act during windup) / reactive (act as it lands) |
| Area shape | single lane / radius / multi-lane / full field |
| Feint chance | defined per skill — not a global rule (see Part 7) |

A named "archetype" (Part 5) is just a common setting of these knobs. New attacks don't need new systems — they need new knob values.

### AI Targeting Visualization
For attacker-locks-onto-defender attacks (most single-lane Reflex-based archetypes), the primary target is explicitly locked at the start of Windup, and a visual line connects attacker to target for the duration — makes "who is this attack actually going to hit" unambiguous before the reaction window opens, independent of which Tell location knob setting is used.

---

## Part 3 — Physical vs Elemental (clarification)

Physical and Elemental are **not** the same axis as Primal typing, and they are not mutually exclusive with it:

- **Physical vs Elemental** = which stat pair powers the move (Force/Guard vs Resonance/Ward) and which status list it draws from (physical statuses: Bleed, Stun, Weaken, Root, Exposed, Slow — vs elemental statuses: Burn, Freeze, Shock, Drown, etc.)
- **Primal type** = the creature's elemental identity, and it applies to the damage formula's `primalTypeMultiplier` regardless of whether the move is Physical or Elemental.

Practical result: a Lightning-type creature's claw slash is mechanically Physical (Force/Guard, physical status pool) but still carries Lightning's Primal chart multiplier, and can flavor-wise crackle or apply Shock as a bonus. This "elemental-charged physical move" space is the natural home for **Type P (Typing)** skill trees — "Primal interaction exploits."

---

## Part 4 — Non-Ring-Timing Input Models

The "traditional ring timing mechanic" — a single tap timed against a visual cue (the current implemented baseline, used by most Reflex-based archetypes in Part 5) — is not the only input model in this framework:

| Input Model | Description | Used By |
|---|---|---|
| **Lane Selection (Targeting)** | A discrete choice of which lane(s) to act on — no timing component at all. Used both by an attacker choosing target lane(s) for a zone skill, and by a defender choosing which lane to move into to escape a forming zone. | Zone/Positional, Split Attention, Grapple/Pin, Zone Attack Targeting (Part 8) |
| **Hold-Duration (Sustained Input)** | Press and hold; scored by how well the hold duration matches a target duration, not by timing a single instant. | Sustained Pressure |
| **Multi-Stage Composite (Select, then Time)** | Two phases: a Lane Selection phase with no timing, followed by a normal ring-timed press to execute. | Zone skills using "multi-select one at a time" or "primary + auto-adjacent" targeting (Part 8) |
| **Persistent/Cross-Turn Decision** | Not a different press mechanic — a different temporal structure. The "decision" is strategic (swap Phasix, reposition, prep a counter) made across multiple turns rather than a reflex press in the moment. | Multi-Turn Buildup |

**Not in this table:** continuous drag/path-tracking (an osu-slider-style mechanic) was explored and deliberately **not** adopted — the lane-discrete battlefield doesn't need free 2D tracking, and it would require a separate, fully-designed control scheme per platform. Noted here so its absence isn't mistaken for an oversight.

**Strike Points (Part 9) are not a player input model** — they're deterministic, authored positions executed automatically as part of a skill's animation. The player's only input during a Strike-Point sequence is still the normal timed press on each Attack beat.

---

## Part 5 — Ranged/Elemental Telegraph Archetypes

Grouped by what they test in the player:

**Reflex-based**
- **Direct Projectile** — travels across the lane, tracked in real time. Baseline pattern; uses traditional ring timing.
- **Instant Strike / Read-the-Tell** — no travel time; short windup cue on the attacker, reacted to pre-emptively, not tracked. Still ring timing, just retimed to the tell instead of the payoff.
- **Multi-Hit Volley** — several small hits in sequence, each its own small window. Tests rhythm/consistency.
- **Lane Displacement Attack** — single-lane targeted strike (AI Targeting Visualization applies, Part 2) with a QTE-style reaction window. Success dodges the primary target to an adjacent **lane** or an adjacent **position** within the current lane (player's choice, per the 7×5 occupancy model — Part 8); either option can be blocked if that destination slot is occupied. Failure, no input, or being fully trapped (both dodge options blocked) deals full damage and inflicts a knockback-style status — see Part 8's flagged open item on naming/reconciling that status against the locked §17 list.

**Patience-based**
- **Charge & Release** — long obvious windup, uncertain exact release moment.
- **Multi-Turn Buildup** — windup spans several turns; not a reflex test, a strategic warning (swap Phasix, reposition, prep counter). Uses the Persistent/Cross-Turn input model (Part 4).
  - **Worked example — Telegraphed Area Attack:** Turn 1 marks the affected lanes (primary target's lane + designated collateral lanes) with zero damage — this is Zone/Positional's "tell" (below) stretched across a full player turn instead of resolving same-turn. The player's turn is a strategic branch, not a reflex response: **Evade** (spend movement to fully clear fragile members from marked lanes) or **Tank** (leave defensive members in place, spend the turn on shields/taunts/buffs instead). Turn 2 deals undodgeable damage to whoever still occupies a marked lane at that point — resolved **per lane**, not per originally-targeted character, so a teammate left behind after the primary target escapes still takes the full hit. No reflex prompt appears at any point in this attack.

**Positional**
- **Zone/Positional** — ground/zone tell; response is movement (lane change), not a timed press. Uses the Lane Selection input model (Part 4).
- **Split Attention** — two simultaneous tells, only one (or both) real. Forces attention across multiple lanes. Also Lane Selection.

**Input-variant**
- **Sustained Pressure** — hold-to-guard instead of tap, bonus for matching duration. Uses the Hold-Duration input model (Part 4).

**Learning-based**
- **Metronome/Learned Rhythm** — steady in-battle beat, learnable within the same fight.
- **Jitter/No Fixed Rhythm** — randomized windup every time; punishes memorization.
- **Counter-Bait** — fires specifically off the player's own action (e.g. right after a guard press), punishes turtling.
- **Feint** — tell sometimes lies. Use sparingly — overuse teaches players to ignore all tells.

**Status-layered**
- **Windup-Applies-Status** — the read is interrupting/answering during the windup itself, not just dodging the payoff hit.

### Worked examples so far
| Attack | Archetype(s) | Primal | Notes |
|--------|-------------|--------|-------|
| (baseline) | Direct Projectile | any | current implemented pattern |
| Lightning bolt | Instant Strike / Read-the-Tell | Lightning | miss the tell → Shock (interrupt) |
| Fireball | Direct Projectile + lingering zone | Fire | Burn spreads to adjacent target on expiry |
| Magma Burst | Zone/Positional + Charge & Release | Fire | "erupt" pattern |
| Ember Spray | Multi-Hit Volley | Fire | 2–3 lanes, small windows |
| Flame Breath | Sustained Pressure | Fire | hold-to-guard, boss-scale feel |
| Stoking Flame | Multi-Turn Buildup | Fire | strategic warning, not a reflex test |
| Backdraft | Counter-Bait | Fire | punishes guarding |
| Flare Feint | Feint | Fire | elite/Corrupted use only |
| Plasma (merge) | Instant flash + lingering burn | Fire+Lightning | inherits both parents' knobs |

---

## Part 6 — Physical/Melee Archetypes

All physical/melee attacks depend on the Beat Sequence system (Part 7) and, by extension, Lane Movement (Part 8) — every one of them includes an Approach beat unless the attacker starts already adjacent, which is a battle-setup condition, not a property of the attack itself.

| Attack | Pattern | Notes |
|--------|--------|-------|
| Slash | short-range arc, brief windup | Strike Tempo fit |
| Stab/Lunge | straight-line, single-lane, very short windup, attacker exposed after | pairs well with Type E Reaction |
| Combo Slash | 2–3 quick hits, each own window | Flow Tempo fit |
| Charge/Ram | brace windup → crosses lanes, often + Stun | Anchor/Force Tempers |
| Grapple/Pin | closes gap, applies Root instead of big hit | Zone/Positional, melee version |
| Rake/Rend | lower hit, applies Bleed | sets up Bleed → Rend chain |
| Feint Thrust | melee version of Feint | same overuse caution applies |

---

## Part 7 — Melee Beat Sequence System

Melee attacks are a **sequence of beats**, not a single state, so gap-closing has its own feel per creature/Temper.

### Beats
- **Approach** — closes lane distance.
- **Windup (Real)** — genuine tell before a hit.
- **Windup (Fake)** — same shape, subtly different — a sharp player can read the difference.
- **Attack** — resolve/timed-input moment.

### Confirmed decisions
- **Sequences are fully committed once started — uninterruptible, from any source.** Once a sequence's first beat begins, it always plays to completion. There is no voluntary bail-out (no Retreat), and, as of this revision, **no external interrupt either** — Root/Stun on the attacker and Reaction (Type E) skills do **not** cut a sequence short. This is decided fresh here — no prior directive ever locked Approach as an interrupt point, so there is nothing being reversed. A sequence's only two outcomes are: it runs its full authored beat list, or it doesn't start at all (e.g. the attacker is already Stunned before its turn, which is a pre-existing, separate mechanic — Stun already causes turn loss under the locked Status system, §17).
- **A full sequence resolves within a single enemy turn** (not spread across multiple turns).
- **Fake windup placement/frequency is defined per skill**, granularly — not a global rule. Some creatures never fake; a stalking Shadow-type might chain several.
- **Automatic return-to-origin is a system rule, not authored content, and now fires unconditionally.** After a sequence's final Attack beat resolves, the mover automatically travels back to whatever lane it occupied before the sequence's **first** Approach began — not the lane before its most recent Approach. A sequence with multiple Approach/Teleport hops (e.g. the Shadow example) still returns fully to its pre-sequence lane; the intermediate hops only affect where strikes land, not where the mover ends up. Since sequences can no longer end early via interrupt, every started sequence reaches its Attack beat and triggers this.
- **Consequence for Lane Movement:** the traversal system must record each mover's pre-sequence origin lane once, at the moment a sequence starts — not update it on each subsequent Approach — since the automatic return always targets that single recorded value.
- **Return is visible, not instant** — for now, it plays as a "hop": an arc trajectory with a large radius (reads as a jump) followed by a float back down to the origin lane. Placeholder animation shape, refinable later per creature/Temper — see Part 1 for the general placeholder-animation approach.

### ⚠ Open design gap created by this decision
Removing Approach as an interrupt point removes the trigger mechanism this document had assigned to **Type E (Reaction)** skills — "triggered responses, counter-attacks, parries" no longer has a described moment to fire against a melee sequence. This isn't resolved here; it needs one of: (a) a different trigger point for Type E entirely (e.g. reacting to the Attack beat itself, or to a specific tell during Windup), (b) Type E trees becoming ranged/zone-focused instead of melee-focused, or (c) accepting Type E doesn't counter melee Beat Sequences and scoping it elsewhere. Flagged in Part 10 rather than decided here.

### Data shape
A skill's beat sequence is just an ordered list of beat tags, e.g.:
```
[Approach, Windup-Fake, Approach, Windup-Real, Attack]
```
A simple brawler and a stalking Shadow creature are the same state machine — only the data differs. With no interrupt branch, this is now a simpler linear/looping structure than originally diagrammed — see the updated `melee_beat_sequence.mermaid`.

---

## Part 8 — Lane Movement & Zone Targeting

**Lane occupancy and the movement cost model are now locked in Combat_Directive_v0_1_0.md** (Part 3 — Tactical Positioning → Lane Movement): occupancy is non-exclusive (combatants can share a lane, spaced apart visually to read as a line), and movement cost is decided by the calling context rather than one fixed rule. This section covers what's specific to attack-triggered movement and targeting.

### Pre-Battle Party Placement
- Players can assign a starting lane (L1–L7) to each party member before battle begins — not a fixed default per creature. Since occupancy is non-exclusive, multiple party members can share a lane at placement, subject to the same in-lane spacing rule.
- Sprite scaling at placement must reflect the assigned lane using the **same continuous depth-scaling rule already locked in Combat_Directive** (Lane 1 largest → Lane 7 smallest).
- **Open question:** is enemy starting placement shown to the player before battle begins, or only revealed once battle starts?

### Zone Attack Targeting — Attacking Perspective
When a skill targets a zone (one or more lanes) rather than a single locked target, the targeting model is **skill-dependent**:
- **Fixed lanes** — the skill always targets specific lane(s); no player choice.
- **Multi-select, one at a time** — the player picks each lane the zone attack covers, up to the skill's lane-count limit. Uses the Multi-Stage Composite input model (Part 4): select lanes, then a normal timed press executes.
- **Primary + auto-adjacent** — the player selects one lane, and the skill automatically extends to related lane(s) — symmetric (L3 also hits L2 and L4) or an asymmetric fixed coupling authored per skill (e.g. L3 also affects L5).

### Zone Attack Targeting — Defending Perspective
Once an attacker's zone skill activates, the defender must see which lanes are affected **before** the attack resolves — the concrete implementation of the Zone/Positional archetype's "tell" (Part 5):
- Affected lane(s) are visually marked the moment the skill activates, not hidden until impact.
- The defender's response is a Lane Selection decision (move out of the marked lanes), governed by the normal Lane Movement traversal rules — the player's own reactive movement, not a "sequence" subject to the Beat Sequence commitment rule (Part 7), which applies specifically to attackers' authored sequences.
- **Two valid responses, not just one:** **Evade** (move a character fully out of marked lanes) or **Tank** (leave a defensive character in place and spend the turn on mitigation — shields, taunts, defensive buffs — instead of repositioning). Zone skills don't have to be dodge-or-die; standing and mitigating is a legitimate alternative, especially for Multi-Turn Buildup-style attacks where there's a full turn to decide.
- **Resolution is per-lane, not per-original-target.** A zone attack checks final occupancy of each marked lane when it resolves — it does not care who was originally targeted. If the primary target escapes but a teammate is left behind in a marked lane (own choice or otherwise), that teammate takes the full hit regardless of not being the original target.

### Occupancy model — resolved (7 lanes × 5 positions)
The contradiction is resolved by refinement, not reversal — see Combat_Directive's 2026-08-12 errata. Each lane holds up to **5 discrete positions**, mirrored on both player and enemy sides. Lane-level occupancy stays non-exclusive (unchanged, still true, still built); **position-level occupancy is exclusive** — one combatant per (lane, position) slot. This means:
- A reactive dodge has **two options**: move to an adjacent **lane**, or move to an adjacent **position** within the current lane. Either can be blocked if the destination slot is occupied.
- "Trapped" (Part 5's Lane Displacement Attack) now means both options are blocked — adjacent lane's relevant slot occupied *and* adjacent position(s) within the current lane occupied — not just one axis.
- Everything that operates at lane granularity (damage/Primal typing, Territory AoE, Zone/Positional, Split Attention) is unchanged — position is a layer beneath lane, for movement/collision/spacing only.
- **Un-defers the enemy side:** the 2026-08-11 errata deferred enemy-side visual spacing until multi-enemy battles exist. Since positions are now confirmed symmetric, enemy-side position support needs building alongside player-side, not deferred.

**Resolved:** Strike Points stay independent of the position grid — no change to Part 9's model. Position-grid slots are persistent and occupancy-exclusive; Strike Points are transient (the attacker always returns to its real lane per Part 7's automatic return-to-origin) and purpose-built for animator-driven visual placement, which the proportional offset system already serves well. Snapping to 5 discrete slots would tie animation values to a number that belongs to calibration, and would fight the "place a marker where it looks right" authoring workflow. Loose naming convention worth keeping in mind during authoring: `Front`/`Rear` read as depth-axis (lane), `Flank-Near`/`Flank-Far` read as lateral-axis (position) — a naming echo of the grid's two axes, not a mechanical link to it.

### Open items
- Per-lane cost for "multi-select one at a time" zone skills — fixed lane count per skill, or scaling Aura cost per additional lane selected?
- Interaction between zone-marking and Split Attention's fake tells — does a fake tell get the same visual marking as a real one, or a distinguishable one?
- **New status needed:** the Lane Displacement Attack's failure state ("Displacement" — knockback/shove to a different lane) doesn't map onto any of the 28 statuses in the locked GDD §17 list. Needs either a mapping to an existing status or a proposal to add one — not invented here, since §17 is locked.

---

## Part 9 — Strike Points (Relative Positional Attacks)

For skills that need precise positioning around a target (e.g. a Shadow teleport-strike sequence: front → rear → flank), strike points are authored **per-skill**, not as a general coordinate system for all combatants.

### Why not a full coordinate/angle system
- Combat_Directive locks a fixed side-profile diorama camera — lanes represent depth only, not a full plane. True angles (e.g. 60°, 180°) don't map cleanly onto a fixed side view without also inventing a facing-direction system, which doesn't exist anywhere else in the design.
- The center-anchor model (Combat_Directive) deliberately keeps every combatant's real position at lane-granularity to avoid spatial edge cases. A full coordinate model would ripple into Territory (Type L) AoE, the Zone/Positional and Split Attention archetypes, and the overworld lane-avoidance mechanic.
- **Decision:** base combatant position stays lane-index only (unchanged from Combat_Directive). Positional flourish is scoped to the skill's own choreography data instead.

### Strike Point Model
- A skill defines a small, hand-placed set of named strike points (e.g. `Front`, `Rear`, `Flank-Near`, `Flank-Far`) — not continuous angles, 3–5 authored offsets per skill.
- Each strike point is stored as a **relative offset from the target's current position** — the target could be in any of the 7 lanes when the skill fires. At runtime: `strike position = target's current position + offset`.
- Offsets are expressed **as a proportion of the target's current visual scale**, not fixed world units — this auto-corrects for Combat_Directive's continuous depth-scaling. A flat unit offset would look correct at only the lane it was authored against.
- Each strike point can carry its own bonus/flavor (e.g. a `Rear` strike dealing bonus damage against Exposed).

### Authoring workflow
1. Place a placeholder target at a reference lane (e.g. Lane 4, mid-depth).
2. Place markers for each named strike point against that placeholder — since the camera is fixed, "looks right in Scene view" and "looks right in-game" are the same question.
3. Build/adjust the teleport-dash animation to travel the marker distances — markers are the source of truth.
4. Buildable now during Phase 3 with placeholder markers/animation.

### Example — Shadow teleport-strike sequence
```
[Approach, Strike(Front), Teleport, Strike(Rear), Teleport, Strike(Flank-Near)]
```
Reuses the beat-sequence data shape from Part 7. Per the commitment rule (Part 7), this whole sequence always plays to completion — no bail-out, no external interrupt.

---

## Part 10 — Open Items for Next Coding Session

1. **Type E (Reaction) needs a new trigger point.** Removing Approach interruptibility removed the mechanism this doc had assigned to Reaction skills — see the flagged design gap in Part 7. Needs a decision before Type E content can be authored. Possible connection: the new Dodge/Parry system in Combat_Directive (Parry = auto counter-attack) may be a natural fit — not yet explored.
2. **Occupancy model resolved (7 lanes × 5 positions, Part 8)** — but enemy-side position support now needs building (previously deferred, un-deferred by this resolution), and exact position layout/spacing is pending calibration. Strike Points/sub-position question is resolved: they stay independent (Part 9).
3. Approach lane-distance cost — fixed 1 lane per beat, or variable by creature Instinct/speed? (Pending numerical calibration.)
4. Exact timing window sizes, windup durations, feint detection thresholds, in-lane spacing values — all pending `NumericalCalibration.md`.
5. Map default archetypes to Tempers (Edge/Anchor/Flux) and Tempo types (Strike/Flow/Hold/Split/Stance) so species design (Phase 5) has defaults to start from.
6. Decide whether the beat-sequence system needs its own runtime component now (Phase 3 vertical slice) or waits until Phase 5 content.
7. Standardize the named strike-point set (Front/Rear/Flank-Near/Flank-Far, or a different list) before Phase 5 species authoring begins.
8. Is enemy pre-battle placement shown to the player before battle starts, or hidden/only revealed once combat begins?
9. Per-lane Aura cost for "multi-select one at a time" zone targeting — fixed lane count vs scaling cost?
10. New "Displacement" status (Part 8) needs reconciliation with the locked §17 status list.

---

## Status
This document is a **design capture**, not a locked spec. Treat every archetype/pattern above as a menu to pick from during skill authoring, not a mandate that all must be used.
