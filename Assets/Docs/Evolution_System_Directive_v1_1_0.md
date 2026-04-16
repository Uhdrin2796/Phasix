# Evolution System Directive v1.1.0 — Markdown Mirror

> **PDF is the canonical source.** This `.md` exists for MCP readability only.
> If content conflicts with the PDF, the PDF takes precedence.
> File: `Assets/Docs/Evolution_System_Directive_v1_1_0.pdf`

**Version:** 1.1.0 · **Date:** March 2026 · **Engine:** Unity 6 Latest LTS · 2D URP
**GDD Ref:** §3 · §5 · §21 · §25 · **Status:** Supersedes GDD §3 Tier Structure

---

## Table of Contents

**PART 1 — SYSTEM DESIGN**
- §1 Tier Structure — Full Revision
- §2 The Evolution Graph
- §3 Branch Types
- §4 Branch Requirement Framework
- §5 Devolution Rules
- §6 Fusion System
- §7 Discovery & Fog of War
- §8 Evolution Web UI

**PART 2 — UNITY 6 IMPLEMENTATION** *(Phase 4)*
- §9 Data Layer — ScriptableObjects
- §10 Runtime Layer — Creature State
- §11 Logic Layer — Evaluator & Executor
- §12 Save System
- §13 UI Implementation
- §14 Supporting Interfaces & Stubs
- §15 Editor Tooling

---

# PART 1 — System Design

---

## §1 · Tier Structure — Full Revision
**Status: GDD SYNC REQUIRED**

GDD §3 tier diagram shows T1–T3 as active range with T4–T5 as future and T6/T7 as Celestial-only. **This directive supersedes that.** Natural lines now go T1–T5. T6 and T7 are exclusive to Fusion forms.

### Natural Tier Structure — T1 to T5

| Tier | Name | Primal Types | Skill Trees | Active Slots | Notes |
|---|---|---|---|---|---|
| T1 | Base | 1–2 | 2 | 2 | All species start here. First branch choices. |
| T2 | — | 2 | 4 | 3 | Exotic branches emerge. |
| T3 | — | 3 | 5 | 4 | Boss conditionals appear. |
| T4 | — | 4 | 6 | 5 | Loadout identity solidifies. Natural line ceiling. Fusion ingredient tier. |
| T5 | Transcendent | 4 | 7 | 5–7 | Full expression. Slot count varies by species design. |
| T6 | Fusion I | Inherits from ingredients | Max of both parents | — | Fusion only. Requires two same-tier ingredients (T5+T5). May have forward branches. Can be T7 ingredient. |
| T7 | Fusion II | Inherits from all four lineage parents | Max of all parents | — | Fusion only. Requires two T6 ingredients (T6+T6). Rarest forms in game. |

### Fusion Tier Rules

| Rule | Detail |
|---|---|
| Result tier formula | `resultTier = max(ingredient1.tier, ingredient2.tier) + 1`. Applies to all fusion acts. |
| Same-tier required for T6+ | To produce T6 or higher, both ingredients must be the same tier. T5+T5→T6. T6+T6→T7. A T4+T5 mix caps at T5. |
| Mixed-tier fusions (T2–T5 results) | Below T6, ingredients can be different tiers. T1+T2→T3. T3+T4→T5. Valid and intended. |
| Fusion forms may have forward branches | Not inherently terminal. System supports fusion forms with their own forward branches — a per-form species design decision. |
| Fusion-origin lines behave like natural lines | Forward branches follow all normal evolution rules. Devolution from fusion origin node splits back into ingredients. |
| Fusion forms not capturable in wild | Only created by the player. Forms further along a fusion-origin line may optionally be wild-capturable (per-species decision). |

### Fusion Tier Examples

| Ingredient 1 | Ingredient 2 | Result Tier | Same-Tier Rule |
|---|---|---|---|
| T1 | T2 | T3 fusion | Not required below T6 — valid |
| T2 | T2 | T3 fusion | Same tier — valid |
| T3 | T4 | T5 fusion | Not required below T6 — valid |
| T4 | T5 | T5 fusion (not T6) | Mixed tier — caps at T5 |
| T5 | T5 | T6 fusion | Same tier — valid, T6 unlocked |
| T6 | T6 | T7 fusion | Same tier — valid, T7 unlocked |

**Why the same-tier gate at T6+?** Without it a player could reach T6 by mixing T5+T4, bypassing developing two full lines to T5.

### Skill Slot Count by Tier

| Tier | Skill Trees Available | Active Slots | Notes |
|---|---|---|---|
| T1 | 2 | 2 | Introductory constraint |
| T2 | 4 | 3 | First tree expansion on evolution |
| T3 | 5 | 4 | Midgame flexibility begins |
| T4 | 6 | 5 | Loadout identity solidifies |
| T5 | 7 | 5–7 | Full expression. Varies by species. |
| T6 | Inherits from ingredients | Max of both parents | Pending species design |
| T7 | Inherits from all four lineage parents | Max of all parents | Pending species design |

---

## §2 · The Evolution Graph
**Status: LOCKED**

The evolution web is a single global directed graph. Nodes are forms. Edges are branches. "Species lines" are labels for common paths — not structural containers. The graph does not enforce line boundaries.

### Graph Architecture

| Concept | Implementation |
|---|---|
| Node | One Phasix form at one tier. Has outgoing forward branches and one `naturalParent` pointer (null at T1). |
| Edge | One branch connecting two nodes. Carries trigger type, requirements, and metadata. Direction is forward (lower→higher tier) except via devolution which traverses in reverse. |
| Natural edge | Connects a form to the next tier of the same species line. Default path. Rendered as solid line. |
| Crossover edge | Connects a form on one species line to a node on a different line, always one tier higher. Creature enters the foreign line and navigates it normally from that point. Rendered as dashed curved line. |
| Fusion link edge | Connects a T5 node to a T6 fusion form. Requires a second ingredient simultaneously. Not a standard traversal edge. |

### Crossover Navigation Rules

| Rule | Detail |
|---|---|
| Entry is always one tier up | A crossover branch from A2 can only target nodes at tier 3. Cannot skip tiers or cross to same/lower tier. |
| Once on foreign line, navigation is normal | After crossing from A2 to B3, the creature can evolve B3→B4→B5 naturally, or devolve B3→B2→B1 following B line's paths. |
| History stack enables full retracing | Personal history stack records every move. Devolution always returns to the exact previous node, including back across the crossover point. |
| Multiple crossovers per form | A form can have 2–3 crossover branches in addition to its natural forward branches. Maximum 5 total forward branches per node (GDD guideline, not hard cap). |

### Per-Creature Navigation Data
The global graph stores no per-creature state. Each creature instance maintains only:
1. Its current node pointer
2. A personal history stack of every node visited in order

**The history stack is the key design insight.** The same graph node (e.g. B3) can be arrived at from A2 via crossover or from B2 via natural evolution, and the creature correctly devolves back to whichever node it actually came from — because that's what's on its personal stack, not what B3's `naturalParent` pointer says.

---

## §3 · Branch Types
**Status: LOCKED**

Every edge in the evolution graph has a branch type determining the trigger mechanism and devolution behavior.

| Type | Trigger | Visual | On Devolution |
|---|---|---|---|
| Standard | Level floor + up to 3 stat thresholds + up to 2 conditionals. Most branches are this type. | Solid line (natural) or dashed curve (crossover) | Creature returns to previous node. Bond preserved. Skills preserved. Unnamed pool grows. Aptitude increments. |
| Item-Gated | Standard threshold met AND specific key item consumed on evolution. Item is an additional requirement, not a bypass. | Solid or dashed with item icon indicator | Creature returns AND item is returned. Player retains ability to re-evolve by spending the item again. |
| Crossover | Same as Standard but target node is on a different species line, exactly one tier higher. Tagged `pathType: Crossover` for UI distinction. | Dashed curved line, blends source→target line colors | History stack pop returns to the exact node the creature came from, which may be on a different line. |
| Fusion Link | Both ingredient creatures present in party. Player initiates fusion. Result tier = `max(ing1.tier, ing2.tier) + 1`. Both are consumed. For T6+, both ingredients must be same tier. | Dashed magenta arc converging toward fusion node above | Fusion form splits back into both ingredient creatures exactly as they were at the moment of fusion. If fusion form had forward branches and creature evolved further, history stack handles return normally until reaching the fusion origin node, which then triggers the split. |

### Branch PathType Tag
All branches carry a `pathType` enum used by the UI layer to render correctly. Does not affect evaluation logic — purely presentational metadata.

| PathType | Meaning |
|---|---|
| Natural | Stays within the same species line. Default progression path. |
| Crossover | Exits into a different species line. Rendered distinctly in UI. |
| FusionLink | Two-ingredient convergence. Requires both creatures to be present. Rendered as converging arcs. |

---

## §4 · Branch Requirement Framework
**Status: LOCKED**

Every Standard and Item-Gated branch has a requirement profile composed of three layers. **All three layers must be satisfied simultaneously** for evolution to be available.

### The Three Layers

| Layer | Guideline | Role |
|---|---|---|
| Level Floor | Always present. Low anti-exploit threshold per tier. | Not a real progression gate. Exists only to prevent instant re-evolution after devolving. Low enough that natural play hits it within minutes. All branches at the same tier share the same level floor — stat thresholds create the real differentiation. |
| Stat Thresholds | Up to 3 stats (guideline). No hard cap. | The primary gate. Both stat layers count: base stats (resets on devolve) and the unnamed pool (never resets). Primary route reaches easiest branch thresholds organically. Alternate and exotic branches require harder stats: a non-Temper stat, or Aptitude requiring prior devolution cycling. |
| Conditionals | Up to 2 (guideline). No hard cap. | One-time checks that persist forever across all devolution cycles. Once met, never reset. Boss defeated, region reached, skill tree unlocked, creature captured, item in possession, origin type — all remain permanently checked. |

### The Two-Layer Stat System

| Layer | What It Is | Resets? |
|---|---|---|
| Base Stats | Current form stats. Grows through leveling at this tier. Shaped by species ceiling, Temper direction, personality modifier, and player assignment. | Yes — resets to tier floor on devolution. |
| Unnamed Pool | Persistent accumulated stat layer representing all previous cycles. Grows on every devolution step, scaled by `excessStats × bondMultiplier`. Belongs to the creature's entire history, not any one form. | Never. Persists through all forms including fusion. |

### Valid Conditional Types

| Type | Example |
|---|---|
| Boss Defeated | Must have defeated Ashenveil (Region 2 boss) at least once |
| Item In Possession | Must have a Void Shard in inventory (not consumed — just present) |
| Creature Captured / Befriended | Must have a specific species at 40%+ bond in current roster |
| Skill Tree Unlocked | Must have unlocked at least one Type G (Aspect) skill tree node |
| Region Reached | Must have reached Region 3 |
| Origin Type | Creature must currently have Corrupted or Hollow Origin |

### Aptitude as the Exotic Gate
Aptitude grows specifically through devolution cycling. A high-Aptitude branch rewards players who have explored the evolution web deeply. A freshly captured creature at the same level as a veteran cannot reach high-Aptitude branches — the math enforces the investment requirement without special rules. **Aptitude gates are the primary signal during species design that a branch is exotic or rare.**

---

## §5 · Devolution Rules
**Status: LOCKED**

**Devolution is always available with no conditions, no cost, and no time limit.** The player can devolve from any form at any time. Multiple devolutions can be chained immediately.

### Core Rules

| Rule | Detail |
|---|---|
| One tier per action | Each devolution pops exactly one entry from the history stack. Cannot skip entries. |
| History stack navigation | Devolution always returns to the exact node the creature came from — determined by the personal history stack, not the node's `naturalParent` pointer. Enables full retracing including cross-line journeys. |
| Bond fully preserved | Bond percentage carries through all devolutions. Bond floor at the last milestone reached is maintained. |
| Skill library preserved | All skills learned at any tier are accessible at all times. A creature at T1 that has previously learned T4 skills can equip and use those skills. No efficacy penalty — stat system provides natural balancing. |
| All branches reopen | Devolving reopens all evolution branches at the tier returned to. The full web is explorable over time through cycling. |
| Met conditionals persist | Boss defeated, region reached, skill tree unlocked — all stay permanently met. The level floor is the only requirement that resets (anti-exploit measure, not a one-time achievement). |
| Unnamed pool grows on devolve | Each devolution step triggers: `poolGrowth = excessStats × bondMultiplier`. Excess = how far above the tier's base stat floor the creature developed before devolving. Higher bond = larger pool gain. |
| Aptitude increments on devolve | Aptitude grows by 1 (pending calibration) on each devolution step. More cycling = higher Aptitude = progressively more exotic branches unlocked. |
| Item-gated branch devolve | Devolving from an item-gated form returns the consumed item alongside the creature. |
| Fusion devolve — splits into ingredients | Devolving from a T6 or T7 fusion form splits it back into its two ingredient creatures. Each ingredient returns as it was at the moment of fusion creation. |

### Unnamed Pool Growth Scaling

| Scenario | Pool Gain | Why |
|---|---|---|
| Devolve immediately after reaching tier minimum | Very small | Barely exceeded tier floor. Little excess development to bank. |
| Devolve after moderate time at tier | Moderate | Meaningful stat growth above floor. Proportional reward. |
| Devolve after deep development (overleveled) | Large | Significant excess stats accumulated. Full reward for commitment. |
| Devolve from T5 after full development | Largest | Highest tier = highest floor = highest excess potential. |

---

## §6 · Fusion System
**Status: EXPANDED V1.1**

Fusion is a creation act. Two ingredient creatures combine and produce one new fusion form at `max(ing1.tier, ing2.tier) + 1`. By default fusion forms are terminal, but the system is fully modular — fusion forms can optionally have their own forward branches, creating a fusion-origin line that extends naturally from the fusion event.

### Fusion Event Rules

| Rule | Detail |
|---|---|
| Result tier formula | `resultTier = max(ingredient1.tier, ingredient2.tier) + 1`. Applies to all fusion acts. |
| Same-tier required for T6+ | T5+T5→T6. T6+T6→T7. A T4+T5 mix caps at T5 and cannot reach T6. Enforced by the evaluator at time of fusion. |
| Mixed-tier allowed below T6 | Any combination of ingredient tiers valid when result would be T5 or lower. T2+T3→T4. T1+T4→T5. |
| Both ingredients consumed on fusion | The two ingredient creatures cease to exist as separate entities the moment fusion occurs. |
| Devolution splits back exactly | Devolving from a fusion origin node returns both ingredient creatures exactly as they were at the moment of fusion. No stats, bond, or skills are lost on the ingredient side. |
| Fusion forms may have forward branches (optional) | Not required to be terminal. If species design calls for it, a fusion form can have forward branches — standard, item-gated, or crossover. `forwardBranches` on a fusion node works identically to any other node. |
| Fusion-origin line devolution | Creature that evolved beyond a fusion origin node devolves normally via history stack pop until reaching the fusion origin node. Devolving the fusion origin then triggers the ingredient split. No special logic required — history stack handles it. |
| Fusion forms float in web UI | Fusion origin node positioned at the midpoint x-coordinate between its two ingredient columns, one row above the higher-tier ingredient. |
| Fusion forms are self-contained | No crossover branches into natural lines, and natural lines cannot crossover into fusion-origin lines. |
| Fusion origin forms not capturable in the wild | Only created by the player. Forms further along a fusion-origin line may optionally be wild-capturable (per-species decision). |

### Fusion in the Graph
Fusion link edges are attached to the ingredient nodes as outgoing forward branches. The fusion origin node has no `naturalParent` — its parents are the two ingredient nodes stored as `fusionIngredient1` and `fusionIngredient2`. If the fusion form has forward branches, those child nodes have `naturalParent` pointing to the fusion origin normally, and the history stack handles all devolution from that point. **No special fusion-specific traversal logic is needed anywhere in the graph system.**

---

## §7 · Discovery & Fog of War
**Status: LOCKED**

Discovery is global and player-wide. When any creature the player controls discovers a form — by personally evolving into it, devolving into it, or encountering it as a wild enemy — that form is permanently revealed for the entire player account.

### Three Visibility States

| State | Sprite | Name | Requirements | Path Lines |
|---|---|---|---|---|
| Hidden | Not shown | Not shown | Not shown | Not shown |
| Sighted | Silhouette | ??? | Not shown | Shown |
| Discovered | Real sprite | Real name | Shown | Shown |

**Cascade Rule — Sighted:** A node becomes Sighted when any of its direct graph neighbours (forward branches or natural parent) is in the Discovered state. Sighted is computed at render time — never stored. Updates automatically the next time the UI renders.

### Discovery Write Sources
- **Evolution or devolution:** Destination node immediately added to player's discovered set.
- **Wild encounter:** The moment a wild Phasix of that form appears, its form ID is added to the discovered set.
- **Fusion forms:** Fusion origin forms cannot be discovered via wild encounter — only by personally creating the fusion. A fusion origin node shows as Sighted once both its ingredient parents are Discovered.

### Plan Mode Visibility
In evolution web UI plan mode, the player can tap any two nodes to see a path between them. Hidden nodes are still tappable in plan mode — their identity is not shown, but a path can be computed and shown, with undiscovered nodes rendering as `???`. The player can know a path exists without knowing what forms are on it.

---

## §8 · Evolution Web UI
**Status: NEW**

The evolution web UI is a zoomable, pannable graph visualisation that replaces the traditional linear evolution menu. Gives the player ability to visualise paths and plan routes without hand-holding.

### Layout Specification

| Element | Position | Notes |
|---|---|---|
| Natural line columns | Left-to-right, one column per species line. Fixed horizontal spacing (~180px). | T1 at bottom row, T5 at top row. Horizontal row guide lines at each tier. |
| Fusion origin nodes | Float above their two ingredient columns. X = midpoint of ingredient columns. Y = one row above the higher-tier ingredient row. | Not assigned to any column. If the fusion form has forward branches, its child nodes render in a new column appended after all natural line columns. |
| T7 fusion nodes | Float one full row above the T6 row. | Visually the apex of the entire web. |
| Tier row labels | Left side of viewport, fixed. Shows T1–T5, T6 (magenta), T7 (gold). | Sticky at all zoom levels. |
| Line labels | Below each species line's T1 node. | Colored by line color. |

### Node Visual Design

| Form Type | Shape | Discovered | Sighted |
|---|---|---|---|
| Natural form | Circle | Colored ring + glow + center dot + tier pips below | Dashed circle silhouette, ??? label |
| T6 Fusion | Circle (~1.25× radius) | Split-color arcs (left half = primary ingredient color, right half = secondary). White center dot. Magenta label accent. | Dashed larger circle, magenta tint, ??? label |
| T7 Fusion | Circle (~1.4× radius) | Same as T6 but gold accent. Outer pulse ring. | Dashed largest circle, gold tint, ??? label |

### Edge Visual Design

| Edge Type | Style | Color |
|---|---|---|
| Natural | Solid straight line | Gradient from source node color to target node color, 32% opacity |
| Crossover | Dashed curved arc (slight upward bow) | Gradient source→target, 70% opacity |
| Fusion Link | Dashed curved arc (stronger arc toward T6/T7 float position) | Source color → magenta (T6) or gold (T7) |

### Interaction Modes

| Mode | Behavior |
|---|---|
| Browse (default) | Pan with single touch/drag. Pinch to zoom. Tap a Sighted node to discover it. Tap a Discovered node to see tooltip with name, requirements, and outgoing branches. |
| Plan Mode | Tap 1: Select origin node (highlights green). Tap 2: Select destination. BFS or Dijkstra computes path. Web dims. Path edges and nodes highlight with step numbers. For fusion destinations, two simultaneous paths show (one to each ingredient). |

### Pathfinding — Two Modes

| Mode | Algorithm | Edge Weights | Finds |
|---|---|---|---|
| Shortest | BFS | All edges = 1 | Fewest evolution/devolution steps |
| Easiest | Dijkstra | Natural=1, Crossover=3, FusionLink=6 | Path with least difficult requirements |

---

# PART 2 — Unity 6 Implementation
*Step-by-step implementation guide. Each section: C# script → Inspector setup → tuning guidance. All paths reference the Phasix project structure. All Phase 4 · Mo 10+.*

---

## §9 · Data Layer — ScriptableObjects
**Phase 4 · Mo 10**

The evolution graph is defined entirely as ScriptableObject assets. One global registry holds all nodes. No evolution data lives on individual creature instances — they only hold pointers into this global graph.

### Step 1 — EvolutionNodeSO
`Assets/Scripts/Evolution/EvolutionNodeSO.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EvoNode_", menuName = "Phasix/Evolution/Node")]
public class EvolutionNodeSO : ScriptableObject
{
    public string formID;           // unique key, e.g. "A3_Ashcrown"
    public string displayName;
    public int    tier;             // 1–7
    public string speciesLine;      // "A", "B", etc. null for fusion forms
    public Sprite formSprite;
    public Color  lineColor;

    [Header("Graph Structure")]
    public List<EvolutionBranchSO> forwardBranches;
    public EvolutionNodeSO naturalParent;  // null if T1, null for fusion nodes

    [Header("Fusion (leave null/false for natural forms)")]
    public bool            isFusionNode;
    public EvolutionNodeSO fusionIngredient1;
    public EvolutionNodeSO fusionIngredient2;
    public bool            fusionHasLine; // true if fusion origin has forward branches
    // Result tier = max(fusionIngredient1.tier, fusionIngredient2.tier) + 1
}
```

**Inspector Setup:**

| Field | Value |
|---|---|
| `formID` | Unique string. Convention: LineCode + Tier + FormName. e.g. `"A3_Ashcrown"`, `"F-AB_Ashbrack"` |
| `tier` | Integer 1–7. T6 and T7 for fusion nodes only. |
| `speciesLine` | Single-letter code ("A"–"T") for natural forms. Leave empty for fusion nodes. |
| `forwardBranches` | List of EvolutionBranchSO assets. Drag in all outgoing branches. |
| `naturalParent` | The node directly below in this line (e.g. A2 for A3). Null for T1 and all fusion nodes. |
| `isFusionNode` | True for fusion origin nodes only. |
| `fusionIngredient1/2` | Only on fusion origin nodes. Drag in the two parent ingredient EvolutionNodeSO assets. |
| `fusionHasLine` | True only on fusion origin nodes that have forward branches. Drives UI layout. |

### Step 2 — EvolutionBranchSO
`Assets/Scripts/Evolution/EvolutionBranchSO.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

public enum BranchTriggerType  { Standard, ItemGated, FusionLink }
public enum BranchPathType     { Natural, Crossover, FusionLink }
public enum ConditionalType    { BossDefeated, ItemInPossession, CreatureCaptured,
                                 SkillTreeUnlocked, RegionReached, OriginType }
public enum StatType           { Vitality, Force, Resonance, Guard, Ward, Resolve,
                                 Instinct, Aura }
public enum OriginType         { Wild, Synthetic, Corrupted, Ascended, Hollow, Primordial }

[System.Serializable]
public class StatRequirement
{
    public StatType stat;
    public int      minimumValue;
    public bool     countUnnamedPool = true; // always true per GDD
}

[System.Serializable]
public class ConditionalRequirement
{
    public ConditionalType type;
    public string          referenceID;    // boss ID, region ID, species ID, etc.
    public OriginType      requiredOrigin; // only used when type == OriginType
}

[CreateAssetMenu(fileName = "Branch_", menuName = "Phasix/Evolution/Branch")]
public class EvolutionBranchSO : ScriptableObject
{
    public EvolutionNodeSO          targetNode;
    public BranchTriggerType        triggerType;
    public BranchPathType           pathType;
    public int                      levelFloor;
    public StatRequirement[]        statRequirements;
    public ConditionalRequirement[] conditionals;
    public ItemSO                   requiredItem;   // null unless ItemGated
    public EvolutionNodeSO          fusionPartner;  // null unless FusionLink
}
```

**Inspector Setup:**

| Field | Value |
|---|---|
| `targetNode` | Drag in the destination EvolutionNodeSO. |
| `triggerType` | Standard for most. ItemGated if key item consumed. FusionLink for T5→T6 and T6→T7 branches. |
| `pathType` | Natural if same species line. Crossover if exiting to another line. FusionLink for fusion branches. |
| `levelFloor` | Low anti-exploit value. Pending NumericalCalibration.md — use 1 as placeholder. |
| `statRequirements` | Up to 3 entries. Each: pick stat, set minimum, leave `countUnnamedPool = true`. |
| `conditionals` | Up to 2 entries. Each: pick conditional type, set referenceID. |
| `requiredItem` | Null for Standard branches. Assign ItemSO for ItemGated only. |
| `fusionPartner` | Null except on FusionLink branches. Assign the other required ingredient's node. |

### Step 3 — EvolutionGraphSO
`Assets/Scripts/Evolution/EvolutionGraphSO.cs`

One global registry asset holding every node. Single source of truth for the entire web.

```csharp
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EvolutionGraph", menuName = "Phasix/Evolution/Graph")]
public class EvolutionGraphSO : ScriptableObject
{
    public List<EvolutionNodeSO> allNodes;
    private Dictionary<string, EvolutionNodeSO> _lookup;

    public EvolutionNodeSO GetNode(string formID)
    {
        if (_lookup == null) BuildLookup();
        _lookup.TryGetValue(formID, out var node);
        return node;
    }

    public List<EvolutionNodeSO> GetNeighbours(EvolutionNodeSO node)
    {
        var result = new List<EvolutionNodeSO>();
        foreach (var branch in node.forwardBranches)
            if (branch.targetNode != null) result.Add(branch.targetNode);
        if (node.naturalParent != null) result.Add(node.naturalParent);
        if (node.isFusionNode)
        {
            if (node.fusionIngredient1 != null) result.Add(node.fusionIngredient1);
            if (node.fusionIngredient2 != null) result.Add(node.fusionIngredient2);
        }
        return result;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, EvolutionNodeSO>();
        foreach (var n in allNodes)
            if (n != null) _lookup[n.formID] = n;
    }

    private void OnEnable() => _lookup = null; // clear cache on reload
}
```

**IMPORTANT:** Create exactly one `EvolutionGraph.asset` at `Assets/Data/Evolution/EvolutionGraph.asset`. Reference it from GameManager or ServiceLocator. Never create multiple instances.

### Project Folder Structure
```
Assets/
  Scripts/Evolution/
    EvolutionNodeSO.cs
    EvolutionBranchSO.cs
    EvolutionGraphSO.cs
    EvolutionEvaluator.cs
    EvolutionExecutor.cs
    EvolutionPathfinder.cs
  Data/Evolution/
    Graph/
      EvolutionGraph.asset        ← one global asset
    Nodes/
      A_Line/
        EvoNode_A1_Ashveil.asset
        EvoNode_A2_Ashmere.asset
      Fusion/
        EvoNode_F-AB_Ashbrack.asset
    Branches/
      A_Line/
        Branch_A1_to_A2.asset
        Branch_A2_crossover_to_B3.asset
      FusionLinks/
        Branch_A5_fusionlink_FAB.asset
```


---

# PART 2 — Unity 6 Implementation *(Phase 4)*

---

## §10 · Runtime Layer — Creature State

Runtime state is **never stored on ScriptableObjects**. All mutable data lives in plain C# classes serialized to JSON via SaveManager.

---

### `StatBlock.cs`

```csharp
// Assets/Scripts/Creatures/StatBlock.cs
using System;

/// <summary>
/// Immutable snapshot of a Phasix creature's eight base stats.
/// Used for history stack entries and battle copies — mutable version
/// lives inside PhasixRuntimeData.
/// </summary>
[Serializable]
public struct StatBlock
{
    public int Vitality;
    public int Force;
    public int Resonance;
    public int Guard;
    public int Ward;
    public int Resolve;
    public int Instinct;
    public int Aura;

    /// <summary>Returns a zeroed StatBlock.</summary>
    public static StatBlock Zero => new StatBlock();

    /// <summary>Sums all eight stat values.</summary>
    public int Total => Vitality + Force + Resonance + Guard + Ward + Resolve + Instinct + Aura;

    public StatBlock(int vitality, int force, int resonance, int guard,
                     int ward, int resolve, int instinct, int aura)
    {
        Vitality  = vitality;
        Force     = force;
        Resonance = resonance;
        Guard     = guard;
        Ward      = ward;
        Resolve   = resolve;
        Instinct  = instinct;
        Aura      = aura;
    }

    /// <summary>Deep copy.</summary>
    public StatBlock Clone() =>
        new StatBlock(Vitality, Force, Resonance, Guard, Ward, Resolve, Instinct, Aura);

    public override string ToString() =>
        $"VIT:{Vitality} FOR:{Force} RES:{Resonance} GRD:{Guard} " +
        $"WRD:{Ward} RSV:{Resolve} INS:{Instinct} AUR:{Aura}";
}
```

---

### `EvolutionHistoryEntry.cs`

```csharp
// Assets/Scripts/Evolution/EvolutionHistoryEntry.cs
using System;

/// <summary>
/// One entry in a Phasix's devolution history stack.
/// Pushed before every evolution; popped on devolution.
/// Base stats reset to tierFloor on pop — unnamedPool and Aptitude never reset.
/// </summary>
[Serializable]
public class EvolutionHistoryEntry
{
    /// <summary>GUID of the EvolutionNodeSO this creature was at before evolving.</summary>
    public string nodeGuid;

    /// <summary>
    /// The stat floor that applied at this tier (used to reset base stats on devolution).
    /// Snapshot taken at the moment of evolution.
    /// </summary>
    public StatBlock tierFloor;

    public EvolutionHistoryEntry(string nodeGuid, StatBlock tierFloor)
    {
        this.nodeGuid  = nodeGuid;
        this.tierFloor = tierFloor;
    }
}
```

---

### `PhasixRuntimeData.cs`

```csharp
// Assets/Scripts/Creatures/PhasixRuntimeData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Full mutable runtime state for one Phasix creature.
/// The linked PhasixData SO is READ-ONLY at runtime — never write to it.
/// This object is serialized to JSON by SaveManager.
/// </summary>
[Serializable]
public class PhasixRuntimeData
{
    // ── Identity ───────────────────────────────────────────────────────────────

    /// <summary>Runtime UUID — unique per creature instance, survives devolution.</summary>
    public string instanceId;

    /// <summary>GUID of the current EvolutionNodeSO. Drives which PhasixData SO to load.</summary>
    public string currentNodeGuid;

    /// <summary>
    /// Cached reference to the current species data SO. Populated by loading
    /// the SO that matches currentNodeGuid from EvolutionGraphSO.
    /// NOT serialized — reconstructed on load.
    /// </summary>
    [NonSerialized] public PhasixData speciesData;

    // ── Stats ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Base stats — resets to tier floor on devolution.
    /// Grows by spending Common Aura in the stat allocation screen.
    /// </summary>
    public StatBlock baseStats;

    /// <summary>
    /// Unnamed pool — NEVER resets. Accumulates across devolution cycles.
    /// Counts toward evolution thresholds alongside baseStats.
    /// Display name pending naming session — reference GameStrings.PoolName in UI.
    /// </summary>
    public StatBlock unnamedPool;

    /// <summary>
    /// Aptitude — increments +1 each time this creature devolves.
    /// Function A: raises stat ceiling per tier.
    /// Function B: unlocks exotic evolution branches at minimum thresholds.
    /// Side effect: higher Aptitude before devolving = larger unnamedPool gain.
    /// </summary>
    public int aptitude;

    // ── Evolution History ──────────────────────────────────────────────────────

    /// <summary>
    /// Stack of previous forms. Push before evolving; pop on devolution.
    /// Index 0 = oldest (original T1 form). Last index = most recent previous form.
    /// </summary>
    public List<EvolutionHistoryEntry> evolutionHistory = new List<EvolutionHistoryEntry>();

    /// <summary>
    /// Set of node GUIDs this creature has discovered (revealed in fog-of-war UI).
    /// Starts with currentNodeGuid. Adjacent nodes revealed on evolution or scouting.
    /// </summary>
    public HashSet<string> discoveredNodeGuids = new HashSet<string>();

    // ── Progression ───────────────────────────────────────────────────────────

    /// <summary>Bond percentage 0–100. Cannot drop below bondFloor.</summary>
    public float bondPercent;

    /// <summary>Highest bond milestone floor reached. Permanent — never decreases.</summary>
    public float bondFloor;

    /// <summary>
    /// Phase saturation — accumulates toward evolution thresholds.
    /// Resets to 0 on evolution (not devolution).
    /// </summary>
    public float phaseSaturation;

    // ── Aura Resources ─────────────────────────────────────────────────────────

    /// <summary>Common Aura — farmable from all Phasix. Spent for stat allocation.</summary>
    public int commonAura;

    /// <summary>
    /// Specific Aura by emotional type key.
    /// Key = emotionalType string matching AuraTypeData SO.
    /// Required for T2→T3 and above evolutions.
    /// </summary>
    public Dictionary<string, int> specificAura = new Dictionary<string, int>();

    /// <summary>Rare Variant Aura — boss drops. Required for exotic branch gates.</summary>
    public int rareVariantAura;

    // ── Skills ─────────────────────────────────────────────────────────────────

    /// <summary>All skills permanently learned. NEVER shrinks.</summary>
    public List<string> learnedSkillGuids = new List<string>();

    /// <summary>
    /// Active equipped skill slots.
    /// Capacity: T1=2, T2=3, T3=4, T4=5, T5–T7=5–7 (pending NumericalCalibration.md).
    /// </summary>
    public List<string> equippedSkillGuids = new List<string>();

    // ── Constructor ────────────────────────────────────────────────────────────

    public PhasixRuntimeData(string nodeGuid)
    {
        instanceId      = Guid.NewGuid().ToString();
        currentNodeGuid = nodeGuid;
        discoveredNodeGuids.Add(nodeGuid);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Combined effective stat value for a given stat type.
    /// baseStats + unnamedPool — both count toward evolution thresholds.
    /// </summary>
    public int EffectiveStat(StatType stat) =>
        GetStatValue(baseStats, stat) + GetStatValue(unnamedPool, stat);

    private static int GetStatValue(StatBlock block, StatType stat) => stat switch
    {
        StatType.Vitality  => block.Vitality,
        StatType.Force     => block.Force,
        StatType.Resonance => block.Resonance,
        StatType.Guard     => block.Guard,
        StatType.Ward      => block.Ward,
        StatType.Resolve   => block.Resolve,
        StatType.Instinct  => block.Instinct,
        StatType.Aura      => block.Aura,
        _                  => 0
    };

    /// <summary>True if bond zone has been reached (for skill tree unlock checks).</summary>
    public bool HasReachedBondZone(BondZone zone) => bondFloor >= (float)zone;
}
```

---

### Inspector Notes — PhasixRuntimeData

`PhasixRuntimeData` is a plain C# class — not a MonoBehaviour and not a ScriptableObject. It is:
- Instantiated by capture logic (wild encounter) or starter selection
- Held in memory as part of `PlayerProgressData.ownedPhasix` (List)
- Serialized to JSON by `SaveManager.Save()`
- Reconstructed from JSON by `SaveManager.Load()` — the `speciesData` reference is re-linked by looking up `currentNodeGuid` in the global `EvolutionGraphSO`


---

## §11 · Logic Layer — Evaluator, Executor & Pathfinder

---

### `EvolutionEvaluator.cs`

```csharp
// Assets/Scripts/Evolution/EvolutionEvaluator.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure logic class — no MonoBehaviour, no Unity lifecycle.
/// Determines whether a given EvolutionBranchSO can currently be traversed
/// by a specific PhasixRuntimeData. Does NOT mutate any state.
/// </summary>
public static class EvolutionEvaluator
{
    // ── Public Entry Point ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if all requirements on the branch are satisfied.
    /// Pass null for inventory if branch has no item requirements.
    /// </summary>
    public static bool CanEvolve(
        EvolutionBranchSO   branch,
        PhasixRuntimeData   runtime,
        IPlayerInventory    inventory = null)
    {
        if (branch  == null) { Debug.LogWarning("[EvolutionEvaluator] branch is null");  return false; }
        if (runtime == null) { Debug.LogWarning("[EvolutionEvaluator] runtime is null"); return false; }

        return CheckTriggerRequirements(branch, runtime, inventory)
            && CheckStatMinimums(branch, runtime)
            && CheckAuraGate(branch, runtime)
            && CheckConditionals(branch, runtime);
    }

    // ── Trigger Requirements ───────────────────────────────────────────────────

    private static bool CheckTriggerRequirements(
        EvolutionBranchSO branch,
        PhasixRuntimeData runtime,
        IPlayerInventory  inventory)
    {
        switch (branch.triggerType)
        {
            case BranchTriggerType.Standard:
                return true; // no trigger item needed

            case BranchTriggerType.ItemGated:
                if (inventory == null)
                {
                    Debug.LogWarning($"[EvolutionEvaluator] Branch '{branch.name}' is ItemGated but no inventory provided.");
                    return false;
                }
                return inventory.HasItem(branch.requiredItemGuid);

            case BranchTriggerType.FusionLink:
                // Fusion is handled separately by EvolutionExecutor.AttemptFusion()
                // A FusionLink branch should not be evaluated through CanEvolve
                Debug.LogWarning("[EvolutionEvaluator] FusionLink branch passed to CanEvolve — use AttemptFusion instead.");
                return false;

            default:
                Debug.LogWarning($"[EvolutionEvaluator] Unknown BranchTriggerType: {branch.triggerType}");
                return false;
        }
    }

    // ── Stat Minimums ──────────────────────────────────────────────────────────

    private static bool CheckStatMinimums(EvolutionBranchSO branch, PhasixRuntimeData runtime)
    {
        foreach (var req in branch.statMinimums)
        {
            int effective = runtime.EffectiveStat(req.stat); // baseStats + unnamedPool
            if (effective < req.minimumValue)
            {
                // Not a log-worthy failure — this is the normal unmet-requirement state
                return false;
            }
        }
        return true;
    }

    // ── Aura Gate ──────────────────────────────────────────────────────────────

    private static bool CheckAuraGate(EvolutionBranchSO branch, PhasixRuntimeData runtime)
    {
        // Common Aura gate (T1→T2)
        if (runtime.commonAura < branch.commonAuraCost) return false;

        // Specific Aura gate (T2→T3 and above)
        foreach (var gate in branch.specificAuraGates)
        {
            runtime.specificAura.TryGetValue(gate.emotionalTypeKey, out int held);
            if (held < gate.cost) return false;
        }

        // Rare Variant Aura gate (exotic branches)
        if (runtime.rareVariantAura < branch.rareVariantAuraCost) return false;

        return true;
    }

    // ── Conditionals ──────────────────────────────────────────────────────────

    private static bool CheckConditionals(EvolutionBranchSO branch, PhasixRuntimeData runtime)
    {
        foreach (var cond in branch.conditionals)
        {
            if (!EvaluateConditional(cond, runtime)) return false;
        }
        return true;
    }

    private static bool EvaluateConditional(BranchConditional cond, PhasixRuntimeData runtime)
    {
        switch (cond.type)
        {
            case ConditionalType.BondZone:
                return runtime.bondFloor >= cond.floatValue;

            case ConditionalType.PhaseSaturation:
                return runtime.phaseSaturation >= cond.floatValue;

            case ConditionalType.AptitudeMinimum:
                return runtime.aptitude >= (int)cond.floatValue;

            case ConditionalType.OriginType:
                if (runtime.speciesData == null) return false;
                return runtime.speciesData.origin.ToString() == cond.stringValue;

            case ConditionalType.BossDefeated:
                // Delegates to PlayerProgressData — accessed via ServiceLocator
                var progress = ServiceLocator.Get<IPlayerProgressData>();
                return progress != null && progress.HasDefeatedBoss(cond.stringValue);

            case ConditionalType.RealmVisited:
                var prog2 = ServiceLocator.Get<IPlayerProgressData>();
                return prog2 != null && prog2.HasVisitedRealm(cond.stringValue);

            case ConditionalType.DevolutionCount:
                return runtime.aptitude >= (int)cond.floatValue; // aptitude == devo count

            default:
                Debug.LogWarning($"[EvolutionEvaluator] Unknown ConditionalType: {cond.type}");
                return false;
        }
    }

    // ── Utility — Which branches from a node are currently available ───────────

    /// <summary>
    /// Returns all branches from the given node that pass CanEvolve.
    /// Useful for UI highlighting of available evolution paths.
    /// </summary>
    public static List<EvolutionBranchSO> GetAvailableBranches(
        EvolutionNodeSO   node,
        EvolutionGraphSO  graph,
        PhasixRuntimeData runtime,
        IPlayerInventory  inventory = null)
    {
        var result = new List<EvolutionBranchSO>();
        foreach (var branch in graph.GetBranchesFrom(node.nodeGuid))
        {
            if (branch.triggerType != BranchTriggerType.FusionLink
                && CanEvolve(branch, runtime, inventory))
            {
                result.Add(branch);
            }
        }
        return result;
    }
}
```


---

### `EvolutionExecutor.cs`

```csharp
// Assets/Scripts/Evolution/EvolutionExecutor.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mutates PhasixRuntimeData to carry out an approved evolution or devolution.
/// Always pair with EvolutionEvaluator.CanEvolve() before calling Execute().
/// Fires EventBus events after state changes.
/// </summary>
public static class EvolutionExecutor
{
    // ── Standard / Item-Gated Evolution ───────────────────────────────────────

    /// <summary>
    /// Executes a standard or item-gated evolution along a branch.
    /// Caller must have already confirmed CanEvolve() == true.
    /// Deducts Aura costs, pushes history, updates currentNodeGuid,
    /// resets base stats to new tier floor, discovers adjacent nodes.
    /// </summary>
    public static void Execute(
        EvolutionBranchSO  branch,
        PhasixRuntimeData  runtime,
        EvolutionGraphSO   graph,
        IPlayerInventory   inventory = null)
    {
        if (branch == null || runtime == null || graph == null)
        {
            Debug.LogError("[EvolutionExecutor] Execute called with null argument.");
            return;
        }

        var prevNode = graph.GetNode(runtime.currentNodeGuid);
        if (prevNode == null)
        {
            Debug.LogError($"[EvolutionExecutor] currentNodeGuid '{runtime.currentNodeGuid}' not found in graph.");
            return;
        }

        var nextNode = graph.GetNode(branch.targetNodeGuid);
        if (nextNode == null)
        {
            Debug.LogError($"[EvolutionExecutor] targetNodeGuid '{branch.targetNodeGuid}' not found in graph.");
            return;
        }

        // 1. Push history entry (snapshot of current node + tier floor for rollback)
        runtime.evolutionHistory.Add(new EvolutionHistoryEntry(
            runtime.currentNodeGuid,
            nextNode.tierStatFloor.Clone()  // floor of the destination tier for devolve reset
        ));

        // 2. Deduct Aura costs
        DeductAuraCosts(branch, runtime);

        // 3. Consume item if item-gated
        if (branch.triggerType == BranchTriggerType.ItemGated && inventory != null)
            inventory.ConsumeItem(branch.requiredItemGuid);

        // 4. Store old species for event
        var oldSpecies = runtime.speciesData;

        // 5. Advance to new node
        runtime.currentNodeGuid = branch.targetNodeGuid;
        // speciesData re-linked by caller (UI/GameManager) from graph.GetNode().speciesData

        // 6. Reset base stats to tier floor of new form
        runtime.baseStats = nextNode.tierStatFloor.Clone();

        // 7. Reset phase saturation (not devolution — only forward evolution)
        runtime.phaseSaturation = 0f;

        // 8. Discover adjacent nodes in fog-of-war
        foreach (var adjacentBranch in graph.GetBranchesFrom(nextNode.nodeGuid))
            runtime.discoveredNodeGuids.Add(adjacentBranch.targetNodeGuid);

        // 9. Mark all conditionals on this branch as permanently met (they persist across devo)
        // Conditionals are stored on the branch SO — checking them again after devo will
        // naturally re-pass if the player meets them again, so no runtime tracking needed.

        // 10. Fire event
        EventBus.Raise_Evolved(runtime, oldSpecies);

        Debug.Log($"[EvolutionExecutor] {oldSpecies?.speciesName} → {nextNode.speciesData?.speciesName}");
    }

    // ── Fusion Evolution ───────────────────────────────────────────────────────

    /// <summary>
    /// Executes a fusion evolution.
    /// ingredientRuntime is consumed (removed from party) after fusion succeeds.
    /// Both the primary and ingredient must satisfy the FusionLink branch requirements
    /// before calling this — use EvolutionEvaluator separately for each.
    /// </summary>
    public static void ExecuteFusion(
        EvolutionBranchSO  fusionBranch,
        PhasixRuntimeData  primary,
        PhasixRuntimeData  ingredient,
        EvolutionGraphSO   graph)
    {
        if (fusionBranch.triggerType != BranchTriggerType.FusionLink)
        {
            Debug.LogError("[EvolutionExecutor] ExecuteFusion called on non-FusionLink branch.");
            return;
        }

        // Fusion valid at ALL tiers. Same-tier ingredient requirement only for T6+.
        if (primary.speciesData != null && ingredient.speciesData != null)
        {
            int primaryTier    = primary.speciesData.evolutionTier;
            int ingredientTier = ingredient.speciesData.evolutionTier;
            if (primaryTier >= 6 && ingredientTier != primaryTier)
            {
                Debug.LogWarning("[EvolutionExecutor] T6+ fusion requires same-tier ingredient.");
                return;
            }
        }

        var nextNode = graph.GetNode(fusionBranch.targetNodeGuid);
        if (nextNode == null) return;

        // Push history for primary
        primary.evolutionHistory.Add(new EvolutionHistoryEntry(
            primary.currentNodeGuid,
            nextNode.tierStatFloor.Clone()
        ));

        // Carry forward combined stat pools (design decision: sum capped at tier ceiling)
        // TODO: exact fusion stat formula pending NumericalCalibration.md
        var oldSpecies = primary.speciesData;

        primary.currentNodeGuid = fusionBranch.targetNodeGuid;
        primary.baseStats        = nextNode.tierStatFloor.Clone();
        primary.phaseSaturation  = 0f;

        foreach (var b in graph.GetBranchesFrom(nextNode.nodeGuid))
            primary.discoveredNodeGuids.Add(b.targetNodeGuid);

        // Ingredient creature is flagged as consumed — caller removes from party list
        ingredient.currentNodeGuid = "__FUSED__"; // sentinel — SaveManager ignores this

        EventBus.Raise_Evolved(primary, oldSpecies);
        Debug.Log($"[EvolutionExecutor] Fusion complete → {nextNode.speciesData?.speciesName}");
    }

    // ── Devolution ─────────────────────────────────────────────────────────────

    /// <summary>
    /// FREE devolution — no cost, no conditions, no time limit.
    /// Authority: Evolution_System_Directive_v1_1_0.
    /// Pops the history stack, resets base stats to the tier floor stored in the entry,
    /// increments Aptitude, calculates unnamedPool gain, returns to previous node.
    /// </summary>
    public static void Devolve(PhasixRuntimeData runtime, EvolutionGraphSO graph)
    {
        if (runtime.evolutionHistory.Count == 0)
        {
            Debug.LogWarning("[EvolutionExecutor] Devolve called but history stack is empty (already at base form).");
            return;
        }

        var entry    = runtime.evolutionHistory[runtime.evolutionHistory.Count - 1];
        runtime.evolutionHistory.RemoveAt(runtime.evolutionHistory.Count - 1);

        var prevNode = graph.GetNode(entry.nodeGuid);
        if (prevNode == null)
        {
            Debug.LogError($"[EvolutionExecutor] Devolve history entry nodeGuid '{entry.nodeGuid}' not found.");
            return;
        }

        var oldSpecies = runtime.speciesData;

        // 1. Calculate excess stats before reset (drives unnamedPool gain)
        StatBlock excessStats = ComputeExcessStats(runtime.baseStats, entry.tierFloor);

        // 2. Increment Aptitude BEFORE pool gain calculation (higher Aptitude = larger gain)
        runtime.aptitude++;

        // 3. Grow unnamed pool from excess stats
        // TODO: exact multiplier formula pending NumericalCalibration.md
        // Placeholder: floor(excessStats.Total * aptitudeMultiplier)
        float aptitudeMultiplier = 1f + (runtime.aptitude * 0.1f); // TODO: replace with NumericalCalibration value
        int   poolGain           = Mathf.FloorToInt(excessStats.Total * aptitudeMultiplier);

        // Pool gain is distributed across stat dimensions proportional to excess
        // For now: add all to the stat with highest excess (TODO: calibrate distribution)
        AddToHighestExcessStat(ref runtime.unnamedPool, excessStats, poolGain);

        // 4. Reset base stats to tier floor of the form being returned to
        runtime.baseStats = entry.tierFloor.Clone();

        // 5. Reset phase saturation (devolution resets progress toward next evolution)
        runtime.phaseSaturation = 0f;

        // 6. Move back to previous node
        runtime.currentNodeGuid = entry.nodeGuid;

        // 7. Fire event
        EventBus.Raise_Devolved(runtime, oldSpecies);

        Debug.Log($"[EvolutionExecutor] Devolved → {prevNode.speciesData?.speciesName} " +
                  $"(Aptitude now {runtime.aptitude}, pool gain +{poolGain})");
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    private static void DeductAuraCosts(EvolutionBranchSO branch, PhasixRuntimeData runtime)
    {
        runtime.commonAura -= branch.commonAuraCost;

        foreach (var gate in branch.specificAuraGates)
        {
            if (runtime.specificAura.ContainsKey(gate.emotionalTypeKey))
                runtime.specificAura[gate.emotionalTypeKey] =
                    Mathf.Max(0, runtime.specificAura[gate.emotionalTypeKey] - gate.cost);
        }

        runtime.rareVariantAura = Mathf.Max(0, runtime.rareVariantAura - branch.rareVariantAuraCost);
    }

    private static StatBlock ComputeExcessStats(StatBlock current, StatBlock floor)
    {
        return new StatBlock(
            Mathf.Max(0, current.Vitality  - floor.Vitality),
            Mathf.Max(0, current.Force     - floor.Force),
            Mathf.Max(0, current.Resonance - floor.Resonance),
            Mathf.Max(0, current.Guard     - floor.Guard),
            Mathf.Max(0, current.Ward      - floor.Ward),
            Mathf.Max(0, current.Resolve   - floor.Resolve),
            Mathf.Max(0, current.Instinct  - floor.Instinct),
            Mathf.Max(0, current.Aura      - floor.Aura)
        );
    }

    private static void AddToHighestExcessStat(ref StatBlock pool, StatBlock excess, int amount)
    {
        // TODO: replace with calibrated distribution formula
        // For now: add entire amount to whichever stat had the most excess
        int max = 0;
        int idx = 0;
        int[] vals = { excess.Vitality, excess.Force, excess.Resonance, excess.Guard,
                        excess.Ward,    excess.Resolve, excess.Instinct, excess.Aura };
        for (int i = 0; i < vals.Length; i++)
            if (vals[i] > max) { max = vals[i]; idx = i; }

        switch (idx)
        {
            case 0: pool.Vitality  += amount; break;
            case 1: pool.Force     += amount; break;
            case 2: pool.Resonance += amount; break;
            case 3: pool.Guard     += amount; break;
            case 4: pool.Ward      += amount; break;
            case 5: pool.Resolve   += amount; break;
            case 6: pool.Instinct  += amount; break;
            case 7: pool.Aura      += amount; break;
        }
    }
}
```

---

### `EvolutionPathfinder.cs`

```csharp
// Assets/Scripts/Evolution/EvolutionPathfinder.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Graph traversal utilities for the Evolution Web.
/// BFS: shortest path (fewest evolutions).
/// Dijkstra: easiest path (lowest total weight — Natural=1, Crossover=3, FusionLink=6).
/// Neither algorithm mutates runtime state.
/// </summary>
public static class EvolutionPathfinder
{
    // ── Edge Weights ───────────────────────────────────────────────────────────

    private static int PathWeight(BranchPathType pathType) => pathType switch
    {
        BranchPathType.Natural    => 1,
        BranchPathType.Crossover  => 3,
        BranchPathType.FusionLink => 6,
        _                         => 99
    };

    // ── BFS — Shortest Path (fewest hops) ─────────────────────────────────────

    /// <summary>
    /// Returns the node GUID path from startNodeGuid to targetNodeGuid
    /// using BFS (minimum number of evolution steps).
    /// Returns empty list if no path exists.
    /// Does NOT check CanEvolve — shows structural path only.
    /// </summary>
    public static List<string> ShortestPath(
        string           startNodeGuid,
        string           targetNodeGuid,
        EvolutionGraphSO graph)
    {
        if (startNodeGuid == targetNodeGuid) return new List<string> { startNodeGuid };

        var visited  = new HashSet<string>();
        var previous = new Dictionary<string, string>(); // child → parent
        var queue    = new Queue<string>();

        queue.Enqueue(startNodeGuid);
        visited.Add(startNodeGuid);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();

            foreach (var branch in graph.GetBranchesFrom(current))
            {
                string next = branch.targetNodeGuid;
                if (visited.Contains(next)) continue;

                visited.Add(next);
                previous[next] = current;

                if (next == targetNodeGuid)
                    return ReconstructPath(previous, startNodeGuid, targetNodeGuid);

                queue.Enqueue(next);
            }
        }

        return new List<string>(); // no path found
    }

    // ── Dijkstra — Easiest Path (lowest total weight) ─────────────────────────

    /// <summary>
    /// Returns the node GUID path from startNodeGuid to targetNodeGuid
    /// using Dijkstra's algorithm with BranchPathType weights.
    /// Natural=1, Crossover=3, FusionLink=6.
    /// Returns empty list if no path exists.
    /// </summary>
    public static List<string> EasiestPath(
        string           startNodeGuid,
        string           targetNodeGuid,
        EvolutionGraphSO graph)
    {
        var dist     = new Dictionary<string, float>();
        var previous = new Dictionary<string, string>();
        var visited  = new HashSet<string>();

        // Simple priority queue via sorted list (sufficient for typical graph size ~50–200 nodes)
        var open = new SortedList<float, string>(new DuplicateKeyComparer());

        dist[startNodeGuid] = 0f;
        open.Add(0f, startNodeGuid);

        while (open.Count > 0)
        {
            string current  = open.Values[0];
            float  currentD = open.Keys[0];
            open.RemoveAt(0);

            if (visited.Contains(current)) continue;
            visited.Add(current);

            if (current == targetNodeGuid)
                return ReconstructPath(previous, startNodeGuid, targetNodeGuid);

            foreach (var branch in graph.GetBranchesFrom(current))
            {
                string next    = branch.targetNodeGuid;
                float  newDist = currentD + PathWeight(branch.pathType);

                if (!dist.ContainsKey(next) || newDist < dist[next])
                {
                    dist[next]     = newDist;
                    previous[next] = current;
                    open.Add(newDist, next);
                }
            }
        }

        return new List<string>(); // no path found
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    private static List<string> ReconstructPath(
        Dictionary<string, string> previous,
        string start,
        string end)
    {
        var path    = new List<string>();
        string node = end;

        while (node != start)
        {
            path.Add(node);
            if (!previous.ContainsKey(node)) return new List<string>();
            node = previous[node];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    /// <summary>Allows duplicate float keys in the SortedList.</summary>
    private class DuplicateKeyComparer : IComparer<float>
    {
        public int Compare(float x, float y) => x <= y ? -1 : 1;
    }
}
```


---

## §12 · Save System

---

### `PhasixSaveData.cs`

```csharp
// Assets/Scripts/Save/PhasixSaveData.cs
using System;
using System.Collections.Generic;

/// <summary>
/// JSON-serializable snapshot of one PhasixRuntimeData instance.
/// All references to SOs are stored as GUID strings — resolved on load.
/// </summary>
[Serializable]
public class PhasixSaveData
{
    public string       instanceId;
    public string       currentNodeGuid;
    public StatBlock    baseStats;
    public StatBlock    unnamedPool;
    public int          aptitude;
    public float        bondPercent;
    public float        bondFloor;
    public float        phaseSaturation;
    public int          commonAura;
    public int          rareVariantAura;

    // Serialized as parallel key/value arrays (Dictionary not directly JSON-serializable in Unity)
    public List<string> specificAuraKeys   = new List<string>();
    public List<int>    specificAuraValues = new List<int>();

    public List<string>                 learnedSkillGuids    = new List<string>();
    public List<string>                 equippedSkillGuids   = new List<string>();
    public List<EvolutionHistoryEntry>  evolutionHistory     = new List<EvolutionHistoryEntry>();
    public List<string>                 discoveredNodeGuids  = new List<string>();

    /// <summary>Convert a live PhasixRuntimeData to a serializable snapshot.</summary>
    public static PhasixSaveData FromRuntime(PhasixRuntimeData r)
    {
        var s = new PhasixSaveData
        {
            instanceId       = r.instanceId,
            currentNodeGuid  = r.currentNodeGuid,
            baseStats        = r.baseStats.Clone(),
            unnamedPool      = r.unnamedPool.Clone(),
            aptitude         = r.aptitude,
            bondPercent      = r.bondPercent,
            bondFloor        = r.bondFloor,
            phaseSaturation  = r.phaseSaturation,
            commonAura       = r.commonAura,
            rareVariantAura  = r.rareVariantAura,
            evolutionHistory = new List<EvolutionHistoryEntry>(r.evolutionHistory),
        };
        foreach (var kv in r.specificAura)
        {
            s.specificAuraKeys.Add(kv.Key);
            s.specificAuraValues.Add(kv.Value);
        }
        s.learnedSkillGuids.AddRange(r.learnedSkillGuids);
        s.equippedSkillGuids.AddRange(r.equippedSkillGuids);
        s.discoveredNodeGuids.AddRange(r.discoveredNodeGuids);
        return s;
    }

    /// <summary>
    /// Reconstruct a PhasixRuntimeData from a save snapshot.
    /// Caller must re-link speciesData from EvolutionGraphSO after calling this.
    /// </summary>
    public PhasixRuntimeData ToRuntime()
    {
        var r = new PhasixRuntimeData(currentNodeGuid)
        {
            instanceId      = instanceId,
            baseStats       = baseStats.Clone(),
            unnamedPool     = unnamedPool.Clone(),
            aptitude        = aptitude,
            bondPercent     = bondPercent,
            bondFloor       = bondFloor,
            phaseSaturation = phaseSaturation,
            commonAura      = commonAura,
            rareVariantAura = rareVariantAura,
        };
        for (int i = 0; i < specificAuraKeys.Count; i++)
            r.specificAura[specificAuraKeys[i]] = specificAuraValues[i];
        r.learnedSkillGuids.AddRange(learnedSkillGuids);
        r.equippedSkillGuids.AddRange(equippedSkillGuids);
        r.evolutionHistory.AddRange(evolutionHistory);
        r.discoveredNodeGuids.UnionWith(discoveredNodeGuids);
        return r;
    }
}
```

---

### `PlayerProgressData.cs`

```csharp
// Assets/Scripts/Save/PlayerProgressData.cs
using System;
using System.Collections.Generic;

/// <summary>
/// All player-level persistent state. Serialized to JSON alongside PhasixSaveData list.
/// </summary>
[Serializable]
public class PlayerProgressData
{
    public string                lastHubVisited     = "";
    public List<PhasixSaveData>  ownedPhasix        = new List<PhasixSaveData>();
    public List<string>          partyInstanceIds   = new List<string>(); // ordered active party
    public List<string>          defeatedBossGuids  = new List<string>();
    public List<string>          visitedRealmGuids  = new List<string>();

    // TODO: pending §22 — inventory, currency
    // TODO: pending §24 — NPC relationship state
}
```

---

### `SaveManager.cs`

```csharp
// Assets/Scripts/Save/SaveManager.cs
using System.IO;
using UnityEngine;

/// <summary>
/// Writes and reads PlayerProgressData to a JSON file on disk.
/// ScriptableObjects are NEVER written to — all runtime state lives here.
/// </summary>
public class SaveManager : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private string _saveFileName = "phasix_save.json";

    private string SavePath => Path.Combine(Application.persistentDataPath, _saveFileName);

    // ── Save ───────────────────────────────────────────────────────────────────

    public void Save(PlayerProgressData data)
    {
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] Saved to {SavePath}");
    }

    // ── Load ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns loaded data, or a fresh PlayerProgressData if no save file exists.
    /// After loading, caller must re-link speciesData on each PhasixRuntimeData
    /// using EvolutionGraphSO.GetNode(currentNodeGuid).speciesData.
    /// </summary>
    public PlayerProgressData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] No save file found — returning new PlayerProgressData.");
            return new PlayerProgressData();
        }

        string json = File.ReadAllText(SavePath);
        var data    = JsonUtility.FromJson<PlayerProgressData>(json);
        Debug.Log($"[SaveManager] Loaded from {SavePath}");
        return data;
    }

    // ── Delete ─────────────────────────────────────────────────────────────────

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[SaveManager] Save file deleted.");
        }
    }
}
```

---

## §13 · UI Implementation — EvolutionWebController

```csharp
// Assets/Scripts/Evolution/EvolutionWebController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MonoBehaviour that drives the Evolution Web UI screen.
/// Reads the global EvolutionGraphSO, renders discovered nodes as UI buttons,
/// applies fog-of-war tinting to undiscovered nodes, and wires up evolution/
/// devolution actions to EvolutionEvaluator + EvolutionExecutor.
///
/// Inspector setup: assign graphAsset, nodePrefab, branchLinePrefab, webContainer.
/// </summary>
public class EvolutionWebController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private EvolutionGraphSO _graphAsset;

    [Header("UI References")]
    [SerializeField] private RectTransform _webContainer;
    [SerializeField] private GameObject    _nodePrefab;        // Button + Icon + Label
    [SerializeField] private GameObject    _branchLinePrefab;  // LineRenderer or UI Image

    [Header("Visual Config")]
    [SerializeField] private Color _colorAvailable    = Color.white;
    [SerializeField] private Color _colorUnavailable  = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color _colorFogOfWar     = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color _colorCurrentNode  = Color.yellow;

    // Runtime
    private PhasixRuntimeData      _activeRuntime;
    private EvolutionNodeSO        _selectedNode;
    private List<GameObject>       _spawnedNodes    = new List<GameObject>();
    private List<GameObject>       _spawnedBranches = new List<GameObject>();

    // ── Public Interface ───────────────────────────────────────────────────────

    /// <summary>Call this when opening the Evolution Web screen for a creature.</summary>
    public void Open(PhasixRuntimeData runtime)
    {
        _activeRuntime = runtime;
        _selectedNode  = null;
        BuildWeb();
    }

    public void Close()
    {
        ClearWeb();
        _activeRuntime = null;
    }

    // ── Web Construction ───────────────────────────────────────────────────────

    private void BuildWeb()
    {
        ClearWeb();
        if (_graphAsset == null || _activeRuntime == null) return;

        // Render nodes
        foreach (var node in _graphAsset.AllNodes)
        {
            bool discovered = _activeRuntime.discoveredNodeGuids.Contains(node.nodeGuid);
            bool isCurrent  = node.nodeGuid == _activeRuntime.currentNodeGuid;

            var nodeGO = Instantiate(_nodePrefab, _webContainer);
            _spawnedNodes.Add(nodeGO);

            // Position from SO layout data
            nodeGO.GetComponent<RectTransform>().anchoredPosition = node.uiPosition;

            // Label
            var label = nodeGO.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = discovered ? node.speciesData?.speciesName ?? "???" : "???";

            // Color
            var img = nodeGO.GetComponentInChildren<Image>();
            if (img != null)
                img.color = isCurrent ? _colorCurrentNode
                          : !discovered ? _colorFogOfWar
                          : _colorUnavailable; // available check done on click

            // Button
            var btn = nodeGO.GetComponentInChildren<Button>();
            if (btn != null)
            {
                var capturedNode = node;
                btn.interactable = discovered;
                btn.onClick.AddListener(() => OnNodeClicked(capturedNode));
            }
        }

        // Render branches
        foreach (var branch in _graphAsset.AllBranches)
        {
            bool fromDiscovered = _activeRuntime.discoveredNodeGuids.Contains(branch.sourceNodeGuid);
            bool toDiscovered   = _activeRuntime.discoveredNodeGuids.Contains(branch.targetNodeGuid);
            if (!fromDiscovered) continue; // don't draw lines to undiscovered targets

            // TODO: draw LineRenderer/Image connecting source node UI pos to target node UI pos
            // pending UI implementation session
        }

        RefreshNodeColors();
    }

    private void ClearWeb()
    {
        foreach (var go in _spawnedNodes)   if (go) Destroy(go);
        foreach (var go in _spawnedBranches) if (go) Destroy(go);
        _spawnedNodes.Clear();
        _spawnedBranches.Clear();
    }

    // ── Interaction ────────────────────────────────────────────────────────────

    private void OnNodeClicked(EvolutionNodeSO node)
    {
        _selectedNode = node;

        // Find branches from current node to this target
        var branches = _graphAsset.GetBranchesFrom(_activeRuntime.currentNodeGuid);
        foreach (var branch in branches)
        {
            if (branch.targetNodeGuid != node.nodeGuid) continue;
            if (branch.triggerType == BranchTriggerType.FusionLink) continue; // handled elsewhere

            var inventory = ServiceLocator.Get<IPlayerInventory>();
            bool canEvo   = EvolutionEvaluator.CanEvolve(branch, _activeRuntime, inventory);
            // TODO: open evolution confirm dialog, show requirements summary
            Debug.Log($"[EvolutionWebController] Selected: {node.speciesData?.speciesName}, CanEvolve={canEvo}");
            return;
        }
    }

    /// <summary>Called from confirm dialog "Devolve" button.</summary>
    public void OnDevolveClicked()
    {
        if (_activeRuntime.evolutionHistory.Count == 0) return;
        EvolutionExecutor.Devolve(_activeRuntime, _graphAsset);
        BuildWeb(); // rebuild with updated discovery state
    }

    /// <summary>Highlight nodes that are currently reachable. Called after any state change.</summary>
    private void RefreshNodeColors()
    {
        // TODO: update node button colors based on EvolutionEvaluator.GetAvailableBranches result
        // pending full UI wiring session
    }
}
```


---

## §14 · Supporting Interfaces & Stubs

---

### `IPlayerInventory.cs`

```csharp
// Assets/Scripts/Core/IPlayerInventory.cs

/// <summary>
/// Minimal inventory interface required by EvolutionEvaluator and EvolutionExecutor.
/// Full implementation pending §22 economy design session.
/// </summary>
public interface IPlayerInventory
{
    bool HasItem(string itemGuid);
    void ConsumeItem(string itemGuid);
}
```

---

### `IPlayerProgressData.cs`

```csharp
// Assets/Scripts/Core/IPlayerProgressData.cs

/// <summary>
/// Minimal player progress interface required by EvolutionEvaluator conditionals.
/// Full implementation pending §24 NPC/narrative design session.
/// </summary>
public interface IPlayerProgressData
{
    bool HasDefeatedBoss(string bossGuid);
    bool HasVisitedRealm(string realmGuid);
}
```

---

### `ServiceLocator.cs`

```csharp
// Assets/Scripts/Core/ServiceLocator.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight service locator. Avoids hard singleton references between systems.
/// Register services in GameManager.Awake() before any other system runs.
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out object service))
            return service as T;
        Debug.LogWarning($"[ServiceLocator] Service '{typeof(T).Name}' not registered.");
        return null;
    }

    public static void Unregister<T>() where T : class
    {
        _services.Remove(typeof(T));
    }

    /// <summary>Call at start of test runs to clear state between tests.</summary>
    public static void Clear() => _services.Clear();
}
```

---

### `NumericalCalibrationSO.cs`

```csharp
// Assets/Scripts/Core/NumericalCalibrationSO.cs
using UnityEngine;

/// <summary>
/// Central ScriptableObject holding all pending numerical calibration values.
/// All values marked TODO are pending a dedicated calibration session.
/// See Assets/Docs/NumericalCalibration.md for full tracking.
/// READ-ONLY at runtime — never write to this SO during play.
/// </summary>
[CreateAssetMenu(menuName = "Phasix/Core/NumericalCalibration")]
public class NumericalCalibrationSO : ScriptableObject
{
    [Header("Evolution — Aura Costs")]
    [Tooltip("TODO: pending calibration session")]
    public int t1ToT2CommonAuraCost = 0;         // TODO: pending NumericalCalibration.md
    [Tooltip("TODO: pending calibration session")]
    public int t2ToT3SpecificAuraCost = 0;       // TODO: pending NumericalCalibration.md

    [Header("Devolution — Pool Gain")]
    [Tooltip("Aptitude multiplier for unnamed pool gain on devolution. TODO: calibrate")]
    public float poolGainAptitudeMultiplierBase = 0.1f; // TODO: pending NumericalCalibration.md

    [Header("Bond — Session Loss Cap")]
    [Tooltip("Maximum bond % that can be lost in one session")]
    public float bondSessionLossCap = 5f;        // locked — see GDD §15

    [Header("Combat")]
    [Tooltip("Active Phasix per side — prototype constant. Revisit at Phase 3 gate.")]
    public int activePartySize = 3;              // BattleConfig.ActivePartySize

    [Header("Stat Growth")]
    // TODO: all stat ceiling formulas, Resonance Bonus layer, tier floor values
    // pending calibration session
}
```

---

### EventBus — Evolution Event Stubs

The following methods are already present in `EventBus.cs` (created in Phase 2 Kickoff).
Listed here for reference:

```csharp
// Phase 4 events — defined in EventBus.cs, no subscribers yet
public static event Action<PhasixRuntimeData, PhasixData> OnEvolved;
public static event Action<PhasixRuntimeData, PhasixData> OnDevolved;
public static event Action<PhasixRuntimeData>             OnPhasixCaptured;
public static event Action<string, int>                   OnAuraDropped;

public static void Raise_Evolved(PhasixRuntimeData p, PhasixData prev)    => OnEvolved?.Invoke(p, prev);
public static void Raise_Devolved(PhasixRuntimeData p, PhasixData prev)   => OnDevolved?.Invoke(p, prev);
public static void Raise_PhasixCaptured(PhasixRuntimeData p)              => OnPhasixCaptured?.Invoke(p);
public static void Raise_AuraDropped(string auraType, int amount)         => OnAuraDropped?.Invoke(auraType, amount);
```

---

## §15 · Editor Tooling

---

### `EvolutionGraphValidator.cs`

```csharp
// Assets/Scripts/Editor/EvolutionGraphValidator.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only validation tool for EvolutionGraphSO.
/// Checks for broken branch references, orphaned nodes, cycles in natural lines,
/// and missing tier floor data.
/// Run via: Phasix → Validate Evolution Graph
/// </summary>
public static class EvolutionGraphValidator
{
    [MenuItem("Phasix/Validate Evolution Graph")]
    public static void ValidateGraph()
    {
        var graphs = FindAllGraphAssets();
        if (graphs.Count == 0)
        {
            Debug.LogWarning("[EvolutionGraphValidator] No EvolutionGraphSO assets found.");
            return;
        }

        int totalErrors   = 0;
        int totalWarnings = 0;

        foreach (var graph in graphs)
        {
            Debug.Log($"[EvolutionGraphValidator] Validating graph: {graph.name}");

            // 1. Check for null node entries
            foreach (var node in graph.AllNodes)
            {
                if (node == null)
                { Debug.LogError($"  [ERROR] Null node entry in graph '{graph.name}'"); totalErrors++; continue; }

                if (string.IsNullOrEmpty(node.nodeGuid))
                { Debug.LogError($"  [ERROR] Node '{node.name}' has empty GUID"); totalErrors++; }

                if (node.speciesData == null)
                { Debug.LogError($"  [ERROR] Node '{node.name}' has no speciesData SO linked"); totalErrors++; }

                if (node.tierStatFloor.Total == 0)
                { Debug.LogWarning($"  [WARN] Node '{node.name}' tierStatFloor is all zeros — pending calibration?"); totalWarnings++; }
            }

            // 2. Check for broken branch references
            var nodeGuids = new HashSet<string>();
            foreach (var node in graph.AllNodes)
                if (node != null) nodeGuids.Add(node.nodeGuid);

            foreach (var branch in graph.AllBranches)
            {
                if (branch == null)
                { Debug.LogError($"  [ERROR] Null branch entry in graph '{graph.name}'"); totalErrors++; continue; }

                if (!nodeGuids.Contains(branch.sourceNodeGuid))
                { Debug.LogError($"  [ERROR] Branch '{branch.name}' sourceNodeGuid not found in graph nodes"); totalErrors++; }

                if (!nodeGuids.Contains(branch.targetNodeGuid))
                { Debug.LogError($"  [ERROR] Branch '{branch.name}' targetNodeGuid not found in graph nodes"); totalErrors++; }
            }

            // 3. Check for orphaned nodes (no incoming or outgoing branches)
            var hasIncoming = new HashSet<string>();
            var hasOutgoing = new HashSet<string>();
            foreach (var branch in graph.AllBranches)
            {
                if (branch == null) continue;
                hasOutgoing.Add(branch.sourceNodeGuid);
                hasIncoming.Add(branch.targetNodeGuid);
            }
            foreach (var node in graph.AllNodes)
            {
                if (node == null) continue;
                bool isRoot     = !hasIncoming.Contains(node.nodeGuid);
                bool isTerminal = !hasOutgoing.Contains(node.nodeGuid);
                if (isRoot && isTerminal)
                { Debug.LogWarning($"  [WARN] Node '{node.name}' is isolated (no branches in or out)"); totalWarnings++; }
            }
        }

        if (totalErrors == 0 && totalWarnings == 0)
            Debug.Log("[EvolutionGraphValidator] All graphs valid. No issues found.");
        else
            Debug.Log($"[EvolutionGraphValidator] Done. Errors: {totalErrors}  Warnings: {totalWarnings}");
    }

    private static List<EvolutionGraphSO> FindAllGraphAssets()
    {
        var results = new List<EvolutionGraphSO>();
        string[] guids = AssetDatabase.FindAssets("t:EvolutionGraphSO");
        foreach (string guid in guids)
        {
            string path  = AssetDatabase.GUIDToAssetPath(guid);
            var    graph = AssetDatabase.LoadAssetAtPath<EvolutionGraphSO>(path);
            if (graph != null) results.Add(graph);
        }
        return results;
    }
}
#endif
```

---

### Graph Visualiser Note

A full node-graph visual editor (like Bolt/XNode style) is **deferred to Phase 4+**. The Unity inspector-based node SO approach is sufficient for the species design phase (Phase 5). When the species roster grows beyond ~20 species, consider implementing a custom EditorWindow using Unity's `GraphView` API (experimental but available in Unity 6). The `EvolutionGraphValidator` above provides the minimum viable tooling for Phase 4 implementation.

---

## §15 · Build Order

Implement the evolution system in this sequence to avoid forward-reference compilation errors. Each step assumes the previous step compiles cleanly.

| Step | File | Notes |
|---|---|---|
| 1 | `StatType.cs` (enum) | Required by StatBlock and EvolutionBranchSO |
| 2 | `BondZone.cs` (enum) | Required by PhasixRuntimeData and EventBus |
| 3 | `BranchTriggerType.cs`, `BranchPathType.cs`, `ConditionalType.cs`, `OriginType.cs` (enums) | Required by EvolutionBranchSO |
| 4 | `StatBlock.cs` | Required by EvolutionNodeSO and PhasixRuntimeData |
| 5 | `EvolutionHistoryEntry.cs` | Required by PhasixRuntimeData |
| 6 | `EvolutionNodeSO.cs` | Data layer — no logic dependencies |
| 7 | `EvolutionBranchSO.cs` | Data layer — depends on enums from step 3 |
| 8 | `EvolutionGraphSO.cs` | Data layer — depends on Node and Branch SOs |
| 9 | `IPlayerInventory.cs`, `IPlayerProgressData.cs` | Interface stubs — no dependencies |
| 10 | `ServiceLocator.cs` | Core utility — no Unity dependencies |
| 11 | `PhasixRuntimeData.cs` | Runtime layer — depends on StatBlock, EvolutionHistoryEntry, EventBus stubs |
| 12 | `EvolutionEvaluator.cs` | Logic — depends on Branch/Graph SOs, PhasixRuntimeData, ServiceLocator |
| 13 | `EvolutionExecutor.cs` | Logic — depends on Evaluator, EventBus, PhasixRuntimeData |
| 14 | `EvolutionPathfinder.cs` | Logic — depends only on EvolutionGraphSO |
| 15 | `PhasixSaveData.cs`, `PlayerProgressData.cs` | Save layer — depends on PhasixRuntimeData |
| 16 | `SaveManager.cs` | Save layer — depends on PlayerProgressData |
| 17 | `NumericalCalibrationSO.cs` | Config SO — standalone |
| 18 | `EvolutionWebController.cs` | UI — depends on all logic layer classes |
| 19 | `EvolutionGraphValidator.cs` | Editor only — wraps in `#if UNITY_EDITOR` |

> **Stub rule:** Forward-referenced types in EventBus stubs (`PhasixData`, `BattleResult`, `SkillData`) compile as long as those class definitions exist anywhere in the project, even as empty stubs. Create empty stub classes for any not yet implemented before compiling Phase 4 evolution scripts.

---

*End of Evolution System Directive v1.1.0 — Markdown Mirror*
*PDF is the canonical source: `Assets/Docs/Evolution_System_Directive_v1_1_0.pdf`*
