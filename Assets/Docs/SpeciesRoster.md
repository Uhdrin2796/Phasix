# Phasix — Species Roster
**Status: PENDING — Phase 5 design task**
Do not populate until Phase 3 vertical slice is complete and fun.
Species design requires the battle system to be playable first.

GDD reference: §25 (Pending), §3 (Evolution web — Locked), §4 (Temper — Locked)

---

## How to Use This File
Each species entry here is the design source before it becomes a ScriptableObject asset.
When an entry is complete and approved, create a `MonsterData` SO in `Assets/Data/Species/`.

---

## Species Entry Template
```
### [Species Name]
**Working name:** (if different from final)
**Primal type:** (8 base or duo-merged)
**Origin:** Wild | Synthetic | Corrupted | Ascended | Hollow | Primordial
**Signal pool:** (3–4 types from: Pulse, Static, Frequency, Silence, Overflow, Echo, Surge, Catalyst, Current)
**Default Tempo:** Strike | Flow | Hold | Split | Stance
**Lore hook:** (1–2 sentences of identity)

#### Tempers (3 required per species)
| Temper Role | Player-Facing Name | Identity |
|-------------|-------------------|---------|
| Edge | [Two-word compound] | |
| Anchor | [Two-word compound] | |
| Flux | [Two-word compound] | |

#### Evolution Web
- **T1 form:** [Name] — base identity
- **T2 branches:**
  - Branch A (Standard): [Name] — requirements: level floor X, [stat] ≥ Y
  - Branch B (Standard): [Name] — requirements: level floor X, [stat] ≥ Y
- **T3 branches:** (from each T2)
  - [Map T2 → T3 paths]
- **T4 branches:** (include any item-gated or exotic paths)
- **T5 Transcendent:** [Name if designed]
- **Fusion paths:** [If any — partner species + primary/secondary ordering]
- **Celestial path:** [T4 or T5 branch if applicable — design with lore]

#### Skill Tree Assignments
Which of the 18 tree types are primary/accessible for this species.
(Content pending — list types only, not individual skills)
- Primary trees: [e.g. Type A, Type I, Type D]
- Secondary trees: [unlocked at higher tiers]

#### Species Attribute Ceiling
(Species ceiling = hard cap per attribute, max ~60% of theoretical max)
| Attribute | Ceiling |
|-----------|---------|
| Vitality | |
| Force | |
| Resonance | |
| Guard | |
| Ward | |
| Resolve | |
| Instinct | |
| Aura | |
| Aptitude | |

#### Notes
```

---

## Roster

*(Empty — populate during Phase 5 species design phase)*

---

## Placeholder Species (for Phase 1–4 testing only)
These are NOT designed species. They are placeholder MonsterData SOs for system testing.
Replace with real species during Phase 5. Do not give these permanent names.

- `PLACEHOLDER_A` — Edge Temper, Fire Primal, Wild Origin
- `PLACEHOLDER_B` — Anchor Temper, Earth Primal, Synthetic Origin
- `PLACEHOLDER_C` — Flux Temper, Lightning Primal, Corrupted Origin
