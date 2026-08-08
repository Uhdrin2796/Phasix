using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Builds PhasixRuntimeData for a wild encounter. Static class — no MonoBehaviour, no scene
/// dependency — matching PersonalitySystem.cs's pattern. Extracted because every future spawn
/// point needs identical construction logic, not a one-off.
///
/// Wk 14-16 scaffold: currentNodeGuid is a placeholder GUID (EvolutionGraphSO doesn't exist yet,
/// Phase 4). origin is set directly to OriginType.Wild — true by definition for a wild
/// encounter, no roll needed.
/// </summary>
public static class WildSpawnSystem
{
    /// <summary>skillDatabase may be null (e.g. not yet assigned in a given scene) — initial skill seeding is then simply skipped, not an error.</summary>
    public static PhasixRuntimeData CreateWildInstance(PhasixData species, SkillDatabase skillDatabase = null)
    {
        var runtime = new PhasixRuntimeData(Guid.NewGuid().ToString());
        runtime.speciesData = species;
        runtime.origin = OriginType.Wild;
        runtime.personality = PersonalitySystem.RollRandom();
        runtime.baseStats = new StatBlock(species.Vitality, species.Force, species.Resonance,
            species.Guard, species.Ward, species.Resolve, species.Instinct, species.Aura);

        if (species.SignalPool != null && species.SignalPool.Length > 0)
            runtime.activeSignalType = species.SignalPool[UnityEngine.Random.Range(0, species.SignalPool.Length)];

        SeedInitialSkills(runtime, species, skillDatabase);

        return runtime;
    }

    /// <summary>
    /// Seeds unlockedTreeTypes/learnedSkillGuids/equippedSkillGuids from the species' available
    /// trees, up to SkillSlotCapacity's locked tier caps (2026-08 session — see DECISIONS.md ->
    /// [Combat]). This is infrastructure, not a balance decision — it grants access to the
    /// already-placeholder-flagged skills, nothing more. Explicitly a PLACEHOLDER standing in for
    /// a real skill-learning UI/flow that doesn't exist yet: every unlocked tree's 2 placeholder
    /// skills are auto-learned, and equipping is a ROUND-ROBIN across unlocked trees (one skill
    /// per tree per pass, 2026-08 follow-up fix — see DECISIONS.md -> [Combat]) up to the tier's
    /// active-slot capacity, not a real choice the player makes. Shared by
    /// WildSpawnSystem.CreateWildInstance and DebugPartyBootstrap, so both spawn paths seed
    /// identically.
    /// </summary>
    public static void SeedInitialSkills(PhasixRuntimeData runtime, PhasixData species, SkillDatabase skillDatabase)
    {
        if (skillDatabase == null || species.AvailableTreeTypes == null) return;

        // SkillSlotCapacity only covers tiers 1-5 (throws for anything else — fusion tiers are
        // genuinely unresolvable pre-Phase 4, per its own doc comment). A species with an unset/
        // invalid tier (e.g. placeholder data authored before EvolutionTier was filled in) should
        // just skip seeding, not crash the spawn that's asking for it.
        if (species.EvolutionTier < 1 || species.EvolutionTier > 5) return;

        int treeCount = SkillSlotCapacity.GetTreeCount(species.EvolutionTier);
        int slotCap = SkillSlotCapacity.GetActiveSlotRange(species.EvolutionTier).max;

        var learnedByTree = new System.Collections.Generic.List<System.Collections.Generic.List<string>>();
        foreach (SkillTreeType tree in species.AvailableTreeTypes.Take(treeCount))
        {
            runtime.unlockedTreeTypes.Add(tree);

            var guids = new System.Collections.Generic.List<string>();
            foreach (SkillData skill in skillDatabase.GetByTreeType(tree))
            {
                if (!skillDatabase.TryGetGuid(skill, out string guid)) continue;
                runtime.learnedSkillGuids.Add(guid);
                guids.Add(guid);
            }
            learnedByTree.Add(guids);
        }

        // Round-robin equip (2026-08 follow-up fix): one skill per unlocked tree per pass, instead
        // of draining each tree fully before the next gets a turn. With every tree having exactly
        // 2 placeholder skills and a Tier-1 cap of 2, the OLD sequential-fill logic let the FIRST
        // tree alone exhaust the cap, so a species unlocking e.g. Mirror+Reaction ended up with
        // BOTH equipped skills from Mirror and none from Reaction — silently making Reaction's
        // TimedInputStreak-granting skill (C2) permanently unreachable despite being correctly
        // unlocked and learned, contradicting this system's own original design intent (see
        // DECISIONS.md's B.8 bootstrap entry: species were deliberately given Mirror+Reaction
        // specifically so both pre-wired combo defaults would be reachable).
        int maxSkillsPerTree = 0;
        foreach (var guids in learnedByTree) maxSkillsPerTree = Mathf.Max(maxSkillsPerTree, guids.Count);

        for (int pass = 0; pass < maxSkillsPerTree && runtime.equippedSkillGuids.Count < slotCap; pass++)
        {
            foreach (var guids in learnedByTree)
            {
                if (runtime.equippedSkillGuids.Count >= slotCap) break;
                if (pass < guids.Count) runtime.equippedSkillGuids.Add(guids[pass]);
            }
        }
    }
}
