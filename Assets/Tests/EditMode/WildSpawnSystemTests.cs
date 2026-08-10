using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers WildSpawnSystem.SeedInitialSkills — specifically the round-robin equip fix (2026-08
    /// follow-up, see DECISIONS.md -> [Combat]): a species with N unlocked trees must get skills
    /// equipped from ALL of them (up to the tier's slot cap), not have the first tree alone
    /// exhaust the cap before later trees get a turn.
    /// </summary>
    public class WildSpawnSystemTests
    {
        private static SkillData MakeSkill(SkillTreeType tree)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            SetPrivateField(skill, "_treeType", tree);
            return skill;
        }

        private static PhasixData MakeSpecies(int tier, List<SkillTreeType> availableTrees)
        {
            var species = ScriptableObject.CreateInstance<PhasixData>();
            SetPrivateField(species, "_evolutionTier", tier);
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
        public void SeedInitialSkills_TwoTreesFourSkillsEach_EquipsFairlyAcrossTrees()
        {
            // Regression case for the round-robin fix: Tier 1 slotCap is 4 (2026-08 rework, see
            // SkillSlotCapacity.GetActiveSlotRange's own doc comment — was 2 before) with two
            // unlocked trees, each contributing enough skills (4) to fill the whole cap alone if
            // given the chance. The OLD sequential-fill logic equipped ALL of the FIRST tree's
            // skills, leaving the second tree's skills learned but never equipped — exactly what
            // made Reaction_Placeholder1 (C2)'s TimedInputStreak grant unreachable in live play
            // despite Reaction being correctly unlocked.
            SkillData mirror1 = MakeSkill(SkillTreeType.Mirror);
            SkillData mirror2 = MakeSkill(SkillTreeType.Mirror);
            SkillData mirror3 = MakeSkill(SkillTreeType.Mirror);
            SkillData mirror4 = MakeSkill(SkillTreeType.Mirror);
            SkillData reaction1 = MakeSkill(SkillTreeType.Reaction);
            SkillData reaction2 = MakeSkill(SkillTreeType.Reaction);
            SkillData reaction3 = MakeSkill(SkillTreeType.Reaction);
            SkillData reaction4 = MakeSkill(SkillTreeType.Reaction);

            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData> { mirror1, mirror2, mirror3, mirror4, reaction1, reaction2, reaction3, reaction4 });
            SetPrivateField(database, "_guids", new List<string> { "mirror1", "mirror2", "mirror3", "mirror4", "reaction1", "reaction2", "reaction3", "reaction4" });

            PhasixData species = MakeSpecies(tier: 1, availableTrees: new List<SkillTreeType> { SkillTreeType.Mirror, SkillTreeType.Reaction });
            var runtime = new PhasixRuntimeData("test-node-guid");

            WildSpawnSystem.SeedInitialSkills(runtime, species, database);

            Assert.AreEqual(4, runtime.equippedSkillGuids.Count, "Slot cap for Tier 1 is 4.");
            Assert.IsTrue(runtime.equippedSkillGuids.Contains("mirror1"), "Mirror's first skill must be equipped.");
            Assert.IsTrue(runtime.equippedSkillGuids.Contains("reaction1"), "Reaction's first skill must ALSO be equipped — this is the regression this test guards against.");
            Assert.IsFalse(runtime.equippedSkillGuids.Contains("mirror3"), "Mirror's third skill should lose out to round-robin fairness once the cap is reached, not get extra slots while Reaction gets fewer.");
            Assert.IsFalse(runtime.equippedSkillGuids.Contains("reaction3"), "Reaction's third skill should also lose out at the cap, mirroring Mirror's treatment.");

            Object.DestroyImmediate(mirror1);
            Object.DestroyImmediate(mirror2);
            Object.DestroyImmediate(mirror3);
            Object.DestroyImmediate(mirror4);
            Object.DestroyImmediate(reaction1);
            Object.DestroyImmediate(reaction2);
            Object.DestroyImmediate(reaction3);
            Object.DestroyImmediate(reaction4);
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(species);
        }

        [Test]
        public void SeedInitialSkills_LearnsAllSkillsFromBothTrees_RegardlessOfEquipCap()
        {
            SkillData mirror1 = MakeSkill(SkillTreeType.Mirror);
            SkillData mirror2 = MakeSkill(SkillTreeType.Mirror);
            SkillData reaction1 = MakeSkill(SkillTreeType.Reaction);
            SkillData reaction2 = MakeSkill(SkillTreeType.Reaction);

            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData> { mirror1, mirror2, reaction1, reaction2 });
            SetPrivateField(database, "_guids", new List<string> { "mirror1", "mirror2", "reaction1", "reaction2" });

            PhasixData species = MakeSpecies(tier: 1, availableTrees: new List<SkillTreeType> { SkillTreeType.Mirror, SkillTreeType.Reaction });
            var runtime = new PhasixRuntimeData("test-node-guid");

            WildSpawnSystem.SeedInitialSkills(runtime, species, database);

            Assert.AreEqual(4, runtime.learnedSkillGuids.Count, "Learning is unaffected by the equip cap — every skill from every unlocked tree is learned.");
            CollectionAssert.AreEquivalent(new[] { "mirror1", "mirror2", "reaction1", "reaction2" }, runtime.learnedSkillGuids);

            Object.DestroyImmediate(mirror1);
            Object.DestroyImmediate(mirror2);
            Object.DestroyImmediate(reaction1);
            Object.DestroyImmediate(reaction2);
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(species);
        }

        [Test]
        public void SeedInitialSkills_SingleUnlockedTree_EquipsUpToCapFromThatTree()
        {
            // Fallback/edge case: with only one tree to round-robin across, behavior must match
            // the old (pre-fix) behavior — no regression for a species with fewer trees than cap.
            SkillData utility1 = MakeSkill(SkillTreeType.Utility);
            SkillData utility2 = MakeSkill(SkillTreeType.Utility);

            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData> { utility1, utility2 });
            SetPrivateField(database, "_guids", new List<string> { "utility1", "utility2" });

            PhasixData species = MakeSpecies(tier: 1, availableTrees: new List<SkillTreeType> { SkillTreeType.Utility });
            var runtime = new PhasixRuntimeData("test-node-guid");

            WildSpawnSystem.SeedInitialSkills(runtime, species, database);

            Assert.AreEqual(2, runtime.equippedSkillGuids.Count);
            CollectionAssert.AreEquivalent(new[] { "utility1", "utility2" }, runtime.equippedSkillGuids);

            Object.DestroyImmediate(utility1);
            Object.DestroyImmediate(utility2);
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(species);
        }
    }
}
