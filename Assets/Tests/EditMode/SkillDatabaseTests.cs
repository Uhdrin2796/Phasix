using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers SkillDatabase's runtime GUID/tree lookups (2026-08 session, see DECISIONS.md ->
    /// [Combat]) — the GUID index itself is populated only via the Editor-only "Rebuild GUID
    /// Index" context menu, so these tests set the parallel _allSkills/_guids lists directly via
    /// reflection to exercise the runtime lookup path in isolation.
    /// </summary>
    public class SkillDatabaseTests
    {
        private static SkillData MakeSkill(SkillTreeType tree)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            SetPrivateField(skill, "_treeType", tree);
            return skill;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        [Test]
        public void TryGetByGuid_KnownGuid_ReturnsTrueAndCorrectSkill()
        {
            var skill = MakeSkill(SkillTreeType.Utility);
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData> { skill });
            SetPrivateField(database, "_guids", new List<string> { "guid-1" });

            bool found = database.TryGetByGuid("guid-1", out SkillData result);

            Assert.IsTrue(found);
            Assert.AreSame(skill, result);

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void TryGetByGuid_UnknownGuid_ReturnsFalse()
        {
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData>());
            SetPrivateField(database, "_guids", new List<string>());

            bool found = database.TryGetByGuid("does-not-exist", out SkillData result);

            Assert.IsFalse(found);
            Assert.IsNull(result);

            Object.DestroyImmediate(database);
        }

        [Test]
        public void GetByTreeType_ReturnsExactlyTheSkillsForThatTree()
        {
            var mirror1 = MakeSkill(SkillTreeType.Mirror);
            var mirror2 = MakeSkill(SkillTreeType.Mirror);
            var aura1 = MakeSkill(SkillTreeType.Aura);
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData> { mirror1, mirror2, aura1 });
            SetPrivateField(database, "_guids", new List<string> { "g1", "g2", "g3" });

            IReadOnlyList<SkillData> mirrorSkills = database.GetByTreeType(SkillTreeType.Mirror);

            Assert.AreEqual(2, mirrorSkills.Count);
            CollectionAssert.Contains(mirrorSkills, mirror1);
            CollectionAssert.Contains(mirrorSkills, mirror2);

            Object.DestroyImmediate(mirror1);
            Object.DestroyImmediate(mirror2);
            Object.DestroyImmediate(aura1);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void GetByTreeType_NoSkillsForThatTree_ReturnsEmptyNotNull()
        {
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData>());
            SetPrivateField(database, "_guids", new List<string>());

            IReadOnlyList<SkillData> result = database.GetByTreeType(SkillTreeType.Bastion);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            Object.DestroyImmediate(database);
        }

        [Test]
        public void MismatchedListLengths_DoesNotThrow_IgnoresUnpairedEntries()
        {
            var skill = MakeSkill(SkillTreeType.Utility);
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            // 2 skills, only 1 GUID — the unpaired second entry must be skipped, not throw.
            SetPrivateField(database, "_allSkills", new List<SkillData> { skill, skill });
            SetPrivateField(database, "_guids", new List<string> { "guid-only-one" });

            Assert.DoesNotThrow(() => database.TryGetByGuid("guid-only-one", out _));

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void TryGetGuid_KnownSkill_ReturnsTrueAndCorrectGuid()
        {
            var skill = MakeSkill(SkillTreeType.Utility);
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData> { skill });
            SetPrivateField(database, "_guids", new List<string> { "guid-1" });

            bool found = database.TryGetGuid(skill, out string guid);

            Assert.IsTrue(found);
            Assert.AreEqual("guid-1", guid);

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void TryGetGuid_UnknownSkill_ReturnsFalse()
        {
            var registered = MakeSkill(SkillTreeType.Utility);
            var unregistered = MakeSkill(SkillTreeType.Aura);
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData> { registered });
            SetPrivateField(database, "_guids", new List<string> { "guid-1" });

            bool found = database.TryGetGuid(unregistered, out string guid);

            Assert.IsFalse(found);
            Assert.IsNull(guid);

            Object.DestroyImmediate(registered);
            Object.DestroyImmediate(unregistered);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void EmptyOrNullGuidEntries_AreSkipped_DoNotThrow()
        {
            var skill = MakeSkill(SkillTreeType.Utility);
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData> { skill });
            SetPrivateField(database, "_guids", new List<string> { "" });

            Assert.DoesNotThrow(() => database.TryGetByGuid("", out _));
            Assert.IsFalse(database.TryGetByGuid("", out SkillData result));
            Assert.IsNull(result);

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(database);
        }
    }
}
