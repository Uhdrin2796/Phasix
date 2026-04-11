# Phasix — World Design Directive
**Version:** 0.1.0  
**Date:** March 2026  
**Status:** New — supplements GDD §19 (World), §24 (NPC & Story)  
**GDD Refs:** §19 (World Structure), §21 (Progression Loop), §24 (NPC & Story System)

---

## Overview

This directive documents the world structure, encounter initiation philosophy, calendar system, faction framework, and Phasix visibility model as developed in design sessions. These systems are not yet reflected in the GDD. GDD §19 and §24 remain marked Pending — this document is the design foundation for those sections when they are written.

All faction names, lore details, and world specifics in this document are **working/exploratory** — flagged for refinement before being written into the GDD.

---

## Part 1 — World Structure

### Model: Multiple Hubs + Realms

The world uses multiple Hubs and discrete Realms.

**The Hub Network**
- Multiple hubs exist across the world — no single designated main hub
- Each hub has a **functional specialization** that creates player routing decisions. Players travel to specific hubs depending on what they need or where they are in progression
- Hubs unlock progressively as the player advances — fast travel to unreached hubs is not available
- Hubs evolve conditionally as the player's arc develops — NPC states, visual details, and accessible spaces shift in response to completed story beats
- Does not use a full dynamic world state system — changes are conditional flags, not procedural
- **Tonal direction (candidate, not locked):** Early hubs feel closer to the familiar physical world. Later hubs feel increasingly strange and liminal as the player goes deeper into the emotional dimension. The spectrum from grounded → liminal tracks the emotional arc
- Specific hub identities (visual, tonal, physical layout, specializations) are **pending narrative development** — hub design is deeply tied to story shape and cannot be finalized until the narrative has more form
- Hub specialization candidates (illustrative, not locked): Aura banking/evolution, Creature Trace gear work, faction interactions, fusion facilitation

**The Realms**
- Each realm is a discrete emotional region
- Entered deliberately — a story beat or quest from the hub provides context for why the player is going in
- Inside each realm: randomized paths between minibosses, Phasix encounters, Aura collection
- Realm emotional identities and total realm count are **pending design**

**Internal Realm Structure**
- 2-3 branching paths after each miniboss leading to the next
- Paths are randomized within the realm's emotional theme — not fully random across all content
- Different paths surface different Phasix populations and different Aura availability
- Path routing becomes a quiet resource decision — players who need specific Aura will preferentially take paths through its source population
- Miniboss sequence varies by run; regional boss is fixed

---

## Part 2 — Phasix Visibility Model

### The Allergy Framing

Phasix are not visible to everyone. Perceiving Phasix is a sensitivity — common enough to be unremarkable, but not universal. Most people cannot perceive Phasix at all.

Key properties of this model:

- **Spectrum-based** — sensitivity varies. Some people have strong awareness and deep Phasix relationships. Some have faint peripheral awareness. Most have none.
- **Not a superpower** — it is simply a thing about certain people. Not chosen, not granted, not dramatic.
- **Discovered, not activated** — many people with sensitivity go years not knowing what they were perceiving. The game begins at the moment it becomes undeniable.
- **Sometimes overwhelming** — certain emotional environments hit harder for high-sensitivity individuals. Early game the player character may not want this sensitivity.
- **Invisible to others** — fights, encounters, and emotional dimension events happen in a space most people cannot access or observe.

### JoJo-Adjacent Visibility Logic
Only people with sensitivity can perceive Phasix and each other's Phasix. Regular people coexist with Phasix passively — they may feel emotional residue in spaces where Phasix are active, but they cannot see or engage with them directly. This is the visibility foundation. The creature relationship (multiple Phasix, collectible, evolvable) diverges significantly from that reference.

### Wild vs Attached Phasix
- **Attached Phasix** emerge from a specific person's emotional state. They are extensions of that person's inner life.
- **Wild Phasix** are unanchored emotional energy — no person generated them. They drift through the world, accumulate in emotionally charged spaces, and are potentially volatile.
- Both exist in the overworld. Wild encounters happen in the field. Attached Phasix are encountered through their person.

---

## Part 3 — Encounter Initiation System

Three layered encounter mechanics replace random encounters entirely. Every Phasix presence in the world is earned, felt, or discovered.

### Layer 1 — Emotional Mirroring (Ambient Population)
The overworld responds to the player's accumulated emotional state. Phasix become visible as soft presences — distortions, glows, sounds that don't match the environment — when the player has accumulated enough relevant emotional experience.

- Player does not initiate — they become ready
- Emotional state tracked passively through story choices, dialogue, wins/losses, time in regions
- Creates genuine discovery moments — something is present that wasn't before
- Returning to earlier regions with different emotional context surfaces new encounters
- Drives the ambient Phasix population of the overworld

### Layer 2 — Resonance/Attunement (Rare and Hidden)
Certain Phasix exist in the world as latent presences — phase-shifted, not visible until the player performs an attunement act. Environmental hints signal their presence: flickering light, mismatched sound, a shadow with no source.

- Player actively investigates hints
- Attunement conditions may include: holding a specific Phasix, reaching a bond threshold, having completed a story beat, being in a specific emotional state
- Creates a puzzle-adjacent layer — reading environmental language
- Rare Variant Aura encounters are often gated this way
- Rewards players who pay attention to the world

### Layer 3 — Failure-Triggered (Emotionally Heavy)
Specific Phasix only surface after the player has stumbled — lost a battle, made a harmful dialogue choice, left a region without resolving something. The encounter is unlocked by vulnerability.

- Completely passive trigger — the player does not know it is coming
- The encounter finds them, often in a space they have already been
- Primary access path for shame, grief, self-doubt Phasix
- Creates "the game noticed I struggled" moments
- Safety net needed: at least one failure-adjacent trigger guaranteed through story regardless of player performance

### Combined Layer Intent
```
Emotional Mirroring   — ambient world population, living background layer
Resonance/Attunement  — rare and hidden Phasix, rewards observation
Failure-Triggered     — emotionally heavy Phasix, unlocked by vulnerability
```

No random encounters. No tall grass probability rolls.

---

## Part 4 — Calendar System

### Time As A Soft Current
The game has an in-game calendar. Months carry emotional and social context drawn from collective lived experience — back to school, holiday pressure, summer freedom, the thaw of spring. The world, available quests, Phasix populations, and hub atmosphere all shift with the month.

Time moves whether the player engages or not. Some windows open and close. Nothing is catastrophically missable — it comes back around on the next cycle. But the first time something appears feels different from catching it on the second pass.

### Month Progression
Months advance through **story beats**, not a real-time timer. The month does not change until something meaningful happens that warrants it. This gives the player agency over pacing without removing the consequence of time passing.

### Emotional Season Groups (Working — Pending Refinement)
```
Sept — Nov    The School Year Arc
              Anxiety, performance pressure, belonging/rejection
              Phasix: achievement-coded, anxiety-coded, social-coded

Dec — Feb     The Holiday Arc
              Family pressure, loneliness amplified by expectation of joy
              Phasix: contradiction-coded, isolation-coded, longing-coded

Mar — May     The Thaw Arc
              Restlessness, possibility, fragile hope
              Phasix: hope-coded, anticipation-coded, transition-coded

Jun — Aug     The Summer Arc
              Freedom, unstructure, impermanence, last summers
              Phasix: joy-coded, nostalgia-coded, loss-coded
```

### Content Availability Model
```
Month opens     — content available, Aura flows freely
Month active    — steady availability, no pressure
Month closing   — yields slow, window narrowing
Month passed    — content dormant, rare trickle only
Next cycle      — window reopens, familiar but layered with new depth
```

Nothing disappears. It breathes in and out.

### First Cycle vs Return Cycle
The first time a seasonal window opens everything is fresh. On the return cycle the same window opens but the content is familiar. Some new depth unlocks — things invisible the first time because the player hadn't experienced enough yet. Emotional Mirroring expressed through the calendar.

### Aura Availability Shifts By Month
Certain emotional Aura types are more abundant during their corresponding period. Grief Aura flows more freely in the hollow stretch of January. Joy Aura peaks in July. This creates natural harvest-while-it's-good behavior without hard locks.

---

## Part 5 — Faction Framework

### Design Principle
Factions are not good vs evil. They are **worldviews about how to handle emotional sensitivity** — different philosophies that formed around a shared experience most people don't have. Every faction philosophy is an understandable coping response to the same underlying condition.

### Working Faction Names (Exploratory — Flagged For Refinement)

**The Suppressors**
Believe emotional states should be controlled and mastered. Their Phasix are evolved through dominance — high power, tightly controlled, brittle under sustained pressure. Not villains — people whose sensitivity was so overwhelming that building systems to contain it was a reasonable response.

**The Amplifiers**
Lean into every emotion fully. No regulation, pure expression. Their Phasix are volatile, capable of massive bursts, burn hot and fast. Not reckless — people who found that leaning in fully was the only thing that made sensitivity bearable.

**The Avoiders**
Built entire lives around not feeling certain things. Their Phasix are stunted in specific emotional ranges but overdeveloped in others — eerie and asymmetric. Not weak — people for whom sensitivity was genuinely painful and withdrawal was a rational response.

**The Integrators** (Player-adjacent)
Attempting to hold multiple emotional states simultaneously. Not mastering, suppressing, or avoiding — carrying it all. This is implicitly what the player does through the Aura multi-realm system.

### Faction Presence In The World
- Factions are not openly organized armies — they are communities of sensitivity-havers who found each other
- Certain months bring certain factions into prominence naturally
- Faction conflict emerges from worldview friction, not war
- Faction apex figures (bosses) are people who went very far in one emotional direction — their Phasix reflect that completely

### Boss Design Philosophy
Faction apex bosses are designed around their emotional philosophy as a mechanical identity:

- **Suppressor apex** — methodical, defensive, punishes aggression, rewards patience
- **Amplifier apex** — chaotic, overwhelming, forces the player to find stillness inside noise
- **Avoider apex** — elusive, hard to read, attacks from unexpected angles, requires commitment to reads

Each boss fight teaches something that mirrors its emotional philosophy. Beating them means something beyond winning.

---

## Part 6 — Story Framework

### Design Philosophy
Story is context for gameplay, not the reason to play. Delivered through NPC dialogue, quest framing, and Phasix encounter writing — not cutscene-heavy narrative. The emotional arc is emergent and exploratory, not authored beat-for-beat.

### Arc Shape
The game's arc mirrors a recognizable life shape — not a specific biography, but a universal emotional trajectory. The intent is relatability, not autobiography.

The player character is not defined. Their arc is shaped by what they engage with, what they avoid, and what finds them.

### Conflict Source
Conflict emerges from how different people relate to their own emotional sensitivity. Other people's Phasix — shaped by their worldview and coping philosophy — are in friction with yours. The battle system exists because emotional experiences genuinely conflict.

### No False Ending
The game does not have a climactic boss that resolves everything. Emotional growth is not linear and does not end. The world keeps offering new things to engage with. Players who finish the main story arc continue finding new encounters, new seasonal content, new evolution branches.

### Recurring Crisis Layer
Something periodically disrupts the emotional dimension — not a villain but a spreading condition, an emotional weather event. The player addresses it using their evolved Phasix. It resolves. Life continues. Another comes. Each crisis is self-contained. The world between crises follows the calendar system.

### Main Quest Function
One main quest per realm. Establishes why the player is entering and what they are moving through. Completing it advances the arc and opens the next realm. Kept minimal — enough context that the gameplay feels grounded, not enough to overwhelm.

### Side Quest Function
Optional depth. Companion arcs, rare Phasix hunts, specific Aura collection goals, seasonal events. Never required. Always rewarding.

---

## Part 7 — Blackout & Banking System

### Design Intent
Pushing deeper into a Realm carries real stakes without punishing Phasix loss. The risk/reward is about resources, not roster.

### Rules
- **Blackout** = losing all Phasix HP (party wipe)
- On blackout: player returns to the last hub they visited
- **Kept on blackout:** all Phasix (no permadeath, no forced devolution)
- **Lost on blackout:** all Aura, loot, and currency collected since the last hub visit that has not been banked
- **Banking** = depositing Aura/resources at a hub. Banked resources are permanent and survive blackout
- This makes the decision to push for one more encounter vs. return to bank a meaningful moment of risk assessment

### Design Notes
- The banking trip creates natural pacing — players return to hubs not just for progression but for safety
- Hub specialization interacts with banking: the player must decide *which* hub to bank at, not just *whether* to bank
- Unbanked Aura loss on blackout is emotionally congruent — the things you were reaching for slip away when you fall

---

## Part 8 — Perspective & Rig System

### Visual Perspective Model
```
Overworld (Exploration)   3/4 oblique top-down view
Combat                    Side-profile diorama view (Paper Mario style)
```

### Movement Model
Orthogonal input maps to diagonal movement in the world. Pressing Up moves the character up-right (into the screen in 3/4 perspective). Pressing Right moves them right along the ground plane. This is the standard approach for 3/4 RPGs (Stardew Valley, etc.) — hides perspective awkwardness, feels natural.

### Bone Rig Requirement Per Phasix
Two rigs per Phasix:
```
Rig 1 — 3/4 oblique      Used for overworld traversal
Rig 2 — Side profile     Used for combat
```

### Overworld Rig Directions (Phase 1)
Three directions cover all movement cases for now:
```
Right-facing             Left-facing covered by horizontal flip
Up-diagonal              Moving into screen / away from camera
Down-diagonal            Moving toward camera
```
Up-diagonal and down-diagonal may share one rig with minor variation for now. Additional directions (pure left/right, 8-way) deferred until Phase 1 rigs are validated in engine.

### Combat Rig
Single side-profile rig facing right. Enemy sprites face left (flip). High-detail — this is the view where individual Phasix art and animation are most visible.

### Spine / Animation Tool
Bone rigging confirmed. Specific tool (Spine vs Unity 2D Animation package) deferred until a prototype rig exists and tool fit can be evaluated. Do not purchase Spine until a prototype validates the need.

---

## Part 9 — Narrative Direction (Working, Not Locked)

### Emotional Arc Shape
```
Innocent  →  Lost  →  Home
```
The player begins in a state of innocence — not naivety, but pre-awareness. The sensitivity to Phasix has not yet become undeniable. The arc moves through a period of being lost — geographically, emotionally, in terms of identity — toward finding home.

"Lost" carries multiple simultaneous meanings: geographically without anchor, emotionally without direction, and something *lost* that might be recovered. All readings are active. The game does not define which.

"Home" as an endpoint is intentionally ambiguous. It may be a place, a self, a relationship, a feeling that was inaccessible. **Do not define it prematurely.** The ambiguity is load-bearing.

### Thematic Resonance With Existing Systems
- Phasix as coping mechanisms developed while lost — the question of which ones you carry home is the team-building layer
- Hub tonal spectrum (grounded → liminal) tracks the arc spatially
- Devolution as valid regression maps onto "lost" — growth is not linear
- What worked once may not work in the same way at home

### Status
Narrative direction only — not documented in GDD. Hub identities, player character, and specific story beats all pending narrative development.

---

## Pending Design — Flagged Gaps

The following are explicitly undecided and must not be filled speculatively:

- Hub count, physical identities, tonal identities, and specializations (pending narrative development)
- Total number of realms
- Individual realm emotional identities
- Hub NPC roster and arc designs
- Faction refined names and lore details
- Main quest narrative specifics
- Recurring crisis nature and mechanics
- Player character identity and starting context
- Old lore status (Fracture event, Phase Dimension, Five Factions) — retained as reference, shifted significantly, requires full revisit before any implementation
