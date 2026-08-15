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
    /// active-slot capacity, not a real choice the player makes. Every creature also always learns
    /// the 5 Standard built-in moves (Attack/Charge/Heal/Regen/Capture — 2026-08 follow-up, see
    /// BuiltInMoveType), seeded first so Attack claims the first equip slot by default — a
    /// temporary placeholder default, not a real move-pool-assignment design (confirmed acceptable
    /// with the user). Called from WildSpawnSystem.CreateWildInstance and GameManager's fallback-
    /// starter seed path (SeedFallbackStarter), so every spawn path seeds identically.
    /// </summary>
    public static void SeedInitialSkills(PhasixRuntimeData runtime, PhasixData species, SkillDatabase skillDatabase)
    {
        if (skillDatabase == null) return;

        // SkillSlotCapacity only covers tiers 1-5 (throws for anything else — fusion tiers are
        // genuinely unresolvable pre-Phase 4, per its own doc comment). A species with an unset/
        // invalid tier (e.g. placeholder data authored before EvolutionTier was filled in) should
        // just skip seeding, not crash the spawn that's asking for it.
        if (species.EvolutionTier < 1 || species.EvolutionTier > 5) return;

        int slotCap = SkillSlotCapacity.GetActiveSlotRange(species.EvolutionTier).max;

        var learnedByTree = new System.Collections.Generic.List<System.Collections.Generic.List<string>>();

        // Standard (2026-08 follow-up — the built-in moves Attack/Charge/Heal/Regen/Capture became
        // real, equippable SkillData, see BuiltInMoveType) is NOT one of the species' unlocked
        // trees — every creature always has it, regardless of species.AvailableTreeTypes. Seeded
        // FIRST in learnedByTree so its skills win round-robin pass 0, guaranteeing Attack
        // (registered first in SkillDatabase among the Standard assets) claims an equip slot before
        // any tree skill does — a temporary default confirmed acceptable with the user pending a
        // real move-pool-assignment design; players can freely unequip it afterward.
        // unlockedTreeTypes deliberately does NOT get Standard added — that list means "unlocked
        // skill TREES" in the GDD taxonomy sense, and Standard isn't one of the 18
        // (SkillTreeType.Standard's own doc comment).
        //
        // Move (BuiltInMoveType.Move, added 2026-08-12 alongside the formation grid system, then
        // REMOVED from this seeding the same session — see DECISIONS.md -> [Combat]) is skipped
        // here entirely: it's no longer a skill-ring orb a creature equips at all, it's a dedicated
        // always-present icon (BattleHUDController's new Move-drag flow) unconditionally available
        // to every player creature, so it has no business in learnedSkillGuids/equippedSkillGuids —
        // Standard_Move.asset and BuiltInMoveType.Move still exist as the underlying dispatch
        // identity BattleManager.ResolveBuiltInMove's Move case resolves against, just never
        // resolved through the equip system anymore.
        var standardGuids = new System.Collections.Generic.List<string>();
        foreach (SkillData skill in skillDatabase.GetByTreeType(SkillTreeType.Standard))
        {
            if (skill.BuiltInMove == BuiltInMoveType.Move) continue;
            if (!skillDatabase.TryGetGuid(skill, out string standardGuid)) continue;
            runtime.learnedSkillGuids.Add(standardGuid);
            standardGuids.Add(standardGuid);
        }
        learnedByTree.Add(standardGuids);

        if (species.AvailableTreeTypes != null)
        {
            int treeCount = SkillSlotCapacity.GetTreeCount(species.EvolutionTier);
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

    /// <summary>
    /// TEMPORARY debug override (2026-08-12, user: "give the enemy a slash to try out so i can
    /// see if it works the same way and i can block") — force-equips an exact set of skill GUIDs
    /// on a wild instance, clearing everything else it was seeded with, so EnemyAI.ChooseSkill has
    /// no other option to randomly pick instead of whichever move is currently being playtested.
    /// Mirrors GameManager.ApplyDebugPlaytestLoadout's pattern for the player side — callers
    /// resolve SkillData -&gt; guid themselves (by BuiltInMoveType for built-ins, by SkillName for
    /// tree skills, same as that method) rather than this method doing name-matching itself, so it
    /// stays a plain "set exactly these guids" primitive reusable for any debug skill combination.
    /// No-op if skillDatabase or guids is null. Called from EncounterTrigger's "Debug Override"
    /// Inspector toggle — DELETE both once the Beat Sequence framework has real content and
    /// doesn't need a forced loadout to exercise.
    ///
    /// 2026-08-12 follow-up (user: "can we also give the enemy the standard attack and the slash
    /// so we can at least see what its look") — generalized from a single forced skill to an
    /// ordered list, so EncounterTrigger can force both Attack and Slash at once.
    /// </summary>
    public static void ApplyDebugSkillsOverride(PhasixRuntimeData runtime, SkillDatabase skillDatabase, System.Collections.Generic.IEnumerable<string> guids)
    {
        if (skillDatabase == null || guids == null) return;

        runtime.learnedSkillGuids.Clear();
        runtime.equippedSkillGuids.Clear();
        foreach (string guid in guids)
        {
            if (string.IsNullOrEmpty(guid)) continue;
            runtime.learnedSkillGuids.Add(guid);
            runtime.equippedSkillGuids.Add(guid);
        }
    }
}
