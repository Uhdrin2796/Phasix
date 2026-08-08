using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers PlaceholderSkillResolver's damage/status derivation for the 36 placeholder skills
    /// (2026-08 session, see DECISIONS.md -> [Combat]) — the classification chains must trace
    /// deterministically to SkillTreeCatalog/StatusEffectCatalog's already-locked data, never a
    /// hand-picked per-skill value.
    /// </summary>
    public class PlaceholderSkillResolverTests
    {
        private static SkillData MakeSkill(SkillTreeType tree, int placeholderIndex)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            SetPrivateField(skill, "_treeType", tree);
            SetPrivateField(skill, "_placeholderIndex", placeholderIndex);
            return skill;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        [TestCase(SkillTreeType.Utility)]
        [TestCase(SkillTreeType.Territory)]
        [TestCase(SkillTreeType.Bastion)]
        [TestCase(SkillTreeType.Corruption)]
        [TestCase(SkillTreeType.Mirror)]
        [TestCase(SkillTreeType.Typing)]
        public void IsDamageSkill_ReturnsTrue_ForTheSixDamageTrees(SkillTreeType tree)
        {
            Assert.IsTrue(PlaceholderSkillResolver.IsDamageSkill(tree));
        }

        [TestCase(SkillTreeType.Aura)]
        [TestCase(SkillTreeType.Passive)]
        [TestCase(SkillTreeType.Synergy)]
        [TestCase(SkillTreeType.Reaction)]
        [TestCase(SkillTreeType.Bond)]
        [TestCase(SkillTreeType.Aspect)]
        [TestCase(SkillTreeType.Resource)]
        [TestCase(SkillTreeType.Evolve)]
        [TestCase(SkillTreeType.Memory)]
        [TestCase(SkillTreeType.Fusion)]
        [TestCase(SkillTreeType.Personality)]
        [TestCase(SkillTreeType.Phantom)]
        public void IsDamageSkill_ReturnsFalse_ForTheTwelveStatusTrees(SkillTreeType tree)
        {
            Assert.IsFalse(PlaceholderSkillResolver.IsDamageSkill(tree));
        }

        [TestCase(SkillTreeType.Utility, DamageCategory.Physical)]
        [TestCase(SkillTreeType.Territory, DamageCategory.Physical)]
        [TestCase(SkillTreeType.Bastion, DamageCategory.Physical)]
        [TestCase(SkillTreeType.Corruption, DamageCategory.Elemental)]
        [TestCase(SkillTreeType.Mirror, DamageCategory.Elemental)]
        [TestCase(SkillTreeType.Typing, DamageCategory.Elemental)]
        public void GetDamageCategory_MatchesExpectedSplit(SkillTreeType tree, DamageCategory expected)
        {
            Assert.AreEqual(expected, PlaceholderSkillResolver.GetDamageCategory(tree));
        }

        [Test]
        public void GetStatusCategory_Aura_ReturnsSignal()
        {
            Assert.AreEqual(StatusEffectCategory.Signal, PlaceholderSkillResolver.GetStatusCategory(SkillTreeType.Aura));
        }

        [Test]
        public void GetStatusCategory_Bond_ReturnsPositive()
        {
            Assert.AreEqual(StatusEffectCategory.Positive, PlaceholderSkillResolver.GetStatusCategory(SkillTreeType.Bond));
        }

        [Test]
        public void GetStatusCategory_Evolve_ReturnsPositive_ViaBondSubstring()
        {
            // Evolve's PrimaryAttribute is "Bond/Aptitude" — the "Bond" branch must fire before falling to Universal.
            Assert.AreEqual(StatusEffectCategory.Positive, PlaceholderSkillResolver.GetStatusCategory(SkillTreeType.Evolve));
        }

        [Test]
        public void GetStatusCategory_Passive_FallsBackToUniversal()
        {
            // Passive's PrimaryAttribute is "All" — matches none of the named branches.
            Assert.AreEqual(StatusEffectCategory.Universal, PlaceholderSkillResolver.GetStatusCategory(SkillTreeType.Passive));
        }

        [Test]
        public void GetStatusCategory_Reaction_FallsBackToUniversal()
        {
            // Reaction's PrimaryAttribute is "Instinct" — matches none of the named branches.
            Assert.AreEqual(StatusEffectCategory.Universal, PlaceholderSkillResolver.GetStatusCategory(SkillTreeType.Reaction));
        }

        [TestCase(SkillTreeType.Aura)]
        [TestCase(SkillTreeType.Passive)]
        [TestCase(SkillTreeType.Bond)]
        [TestCase(SkillTreeType.Reaction)]
        public void GetStatusForSkill_IndexZeroAndOne_Differ(SkillTreeType tree)
        {
            StatusEffectType a = PlaceholderSkillResolver.GetStatusForSkill(tree, 0);
            StatusEffectType b = PlaceholderSkillResolver.GetStatusForSkill(tree, 1);

            Assert.AreNotEqual(a, b, "Every status category has >= 4 members, so index 0 vs 1 must resolve to different statuses.");
        }

        [Test]
        public void GetStatusForSkill_IsDeterministic()
        {
            StatusEffectType first = PlaceholderSkillResolver.GetStatusForSkill(SkillTreeType.Aura, 1);
            StatusEffectType second = PlaceholderSkillResolver.GetStatusForSkill(SkillTreeType.Aura, 1);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Resolve_DamageTree_ReturnsDamageResolution_NoStatus()
        {
            SkillData skill = MakeSkill(SkillTreeType.Bastion, 0);

            PlaceholderSkillResolver.SkillResolution resolution = PlaceholderSkillResolver.Resolve(skill);

            Assert.IsTrue(resolution.DealsDamage);
            Assert.AreEqual(DamageCategory.Physical, resolution.Category);
            Assert.IsFalse(resolution.AppliedStatus.HasValue);
            Assert.IsFalse(resolution.SelfTargeted, "Damage skills target the enemy, never the caster.");

            Object.DestroyImmediate(skill);
        }

        [Test]
        public void Resolve_StatusTree_ReturnsStatusResolution_SelfTargetedMatchesIsPositive()
        {
            SkillData skill = MakeSkill(SkillTreeType.Bond, 0);

            PlaceholderSkillResolver.SkillResolution resolution = PlaceholderSkillResolver.Resolve(skill);

            Assert.IsFalse(resolution.DealsDamage);
            Assert.IsTrue(resolution.AppliedStatus.HasValue);
            bool expectedSelfTargeted = StatusEffectCatalog.Get(resolution.AppliedStatus.Value).IsPositive;
            Assert.AreEqual(expectedSelfTargeted, resolution.SelfTargeted);

            Object.DestroyImmediate(skill);
        }

        [Test]
        public void Resolve_BothPlaceholdersOfADamageTree_AreMechanicallyIdentical()
        {
            SkillData first = MakeSkill(SkillTreeType.Typing, 0);
            SkillData second = MakeSkill(SkillTreeType.Typing, 1);

            PlaceholderSkillResolver.SkillResolution a = PlaceholderSkillResolver.Resolve(first);
            PlaceholderSkillResolver.SkillResolution b = PlaceholderSkillResolver.Resolve(second);

            Assert.AreEqual(a.DealsDamage, b.DealsDamage);
            Assert.AreEqual(a.Category, b.Category);

            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }
    }
}
