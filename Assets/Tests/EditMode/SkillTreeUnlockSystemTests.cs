using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers SkillTreeUnlockSystem's bond-gated Type F/Type O unlock logic (CLAUDE.md: "Type F trees unlock at 20%. Type O trees unlock at 40%.") and GetEffectiveUnlockedTrees (2026-08 follow-up #8, skill web view).</summary>
    public class SkillTreeUnlockSystemTests
    {
        private static PhasixRuntimeData MakePhasix() => new PhasixRuntimeData("test-node-guid");

        private static PhasixData MakeSpecies(List<SkillTreeType> availableTrees)
        {
            var species = ScriptableObject.CreateInstance<PhasixData>();
            SetPrivateField(species, "_availableTreeTypes", availableTrees);
            return species;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        [Test]
        public void HandleBondMilestoneReached_Familiar_UnlocksBondTree()
        {
            var phasix = MakePhasix();

            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Familiar);

            CollectionAssert.Contains(phasix.unlockedTreeTypes, SkillTreeType.Bond);
        }

        [Test]
        public void HandleBondMilestoneReached_Companion_UnlocksPersonalityTree()
        {
            var phasix = MakePhasix();

            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Companion);

            CollectionAssert.Contains(phasix.unlockedTreeTypes, SkillTreeType.Personality);
        }

        [Test]
        public void HandleBondMilestoneReached_OtherZones_UnlockNothing()
        {
            var phasix = MakePhasix();

            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Partner);
            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Bonded);
            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Complete);

            CollectionAssert.IsEmpty(phasix.unlockedTreeTypes);
        }

        [Test]
        public void HandleBondMilestoneReached_CalledTwiceForSameZone_DoesNotDuplicate()
        {
            var phasix = MakePhasix();

            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Familiar);
            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Familiar);

            Assert.AreEqual(1, phasix.unlockedTreeTypes.Count, "unlockedTreeTypes must never shrink, but it also must never duplicate an entry.");
        }

        // --- 2026-08 follow-up #8: GetEffectiveUnlockedTrees (skill web view + equip gate) ---

        [Test]
        public void GetEffectiveUnlockedTrees_NoOverride_ReturnsRealUnlockedTreeTypes()
        {
            var phasix = MakePhasix();
            phasix.unlockedTreeTypes.Add(SkillTreeType.Bond);

            var result = SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(phasix);

            CollectionAssert.AreEqual(new[] { SkillTreeType.Bond }, result);
        }

        [Test]
        public void GetEffectiveUnlockedTrees_OverrideActive_IgnoresRealListUsesSpeciesAvailableTreesUpToTreeCount()
        {
            var phasix = MakePhasix();
            phasix.unlockedTreeTypes.Add(SkillTreeType.Bond); // real list — must be ignored while override is active
            PhasixData species = MakeSpecies(new List<SkillTreeType> { SkillTreeType.Utility, SkillTreeType.Aura, SkillTreeType.Passive });
            phasix.speciesData = species;
            phasix.DebugTierOverride = 1; // GetTreeCount(1) == 2

            var result = SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(phasix);

            CollectionAssert.AreEqual(new[] { SkillTreeType.Utility, SkillTreeType.Aura }, result);
            CollectionAssert.DoesNotContain(result, SkillTreeType.Bond);
            Object.DestroyImmediate(species);
        }

        [Test]
        public void GetEffectiveUnlockedTrees_OverrideActiveButNoSpeciesData_ReturnsEmpty()
        {
            var phasix = MakePhasix();
            phasix.DebugTierOverride = 3;

            var result = SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(phasix);

            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void GetEffectiveUnlockedTrees_NullRuntime_ReturnsEmpty()
        {
            var result = SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(null);

            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void GetEffectiveUnlockedTrees_DebugUnlockAllTrees_ReturnsAllEighteenGddTrees()
        {
            // 2026-08-09 follow-up — user: "can we also have an unlock all debug so im able to see
            // everything?" Must return every GDD tree regardless of real unlockedTreeTypes, tier
            // override, or species data — a pure "show everything" toggle.
            var phasix = MakePhasix();
            phasix.unlockedTreeTypes.Add(SkillTreeType.Bond); // must be ignored
            phasix.DebugUnlockAllTrees = true;
            // Deliberately no speciesData set — must not throw or return empty despite that,
            // since DebugUnlockAllTrees doesn't depend on species data at all.

            var result = SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(phasix);

            Assert.AreEqual(18, result.Count);
            CollectionAssert.DoesNotContain(result, SkillTreeType.Standard, "Standard isn't a GDD taxonomy tree.");
            CollectionAssert.Contains(result, SkillTreeType.Bond);
            CollectionAssert.Contains(result, SkillTreeType.Phantom);
        }

        [Test]
        public void GetEffectiveUnlockedTrees_DebugUnlockAllTrees_TakesPriorityOverDebugTierOverride()
        {
            var phasix = MakePhasix();
            PhasixData species = MakeSpecies(new List<SkillTreeType> { SkillTreeType.Utility });
            phasix.speciesData = species;
            phasix.DebugTierOverride = 1; // GetTreeCount(1) == 2, and species only has 1 tree anyway
            phasix.DebugUnlockAllTrees = true;

            var result = SkillTreeUnlockSystem.GetEffectiveUnlockedTrees(phasix);

            Assert.AreEqual(18, result.Count, "Unlock All must win over the tier/species-limited simulation.");
            Object.DestroyImmediate(species);
        }
    }
}
