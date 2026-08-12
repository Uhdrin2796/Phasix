using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers SkillData.BeatSequence (Attack_Pattern_Directive Part 2's minimal telegraph-knob
    /// field, 2026-08-11 — see DECISIONS.md -> [Combat]) — empty-by-default so every pre-existing
    /// skill asset is unaffected, and correctly exposes an injected ordered beat list.
    /// </summary>
    public class SkillDataTests
    {
        private static SkillData MakeSkill()
        {
            return ScriptableObject.CreateInstance<SkillData>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        [Test]
        public void BeatSequence_DefaultsToEmpty()
        {
            SkillData skill = MakeSkill();

            Assert.AreEqual(0, skill.BeatSequence.Count);

            Object.DestroyImmediate(skill);
        }

        [Test]
        public void BeatSequence_ExposesInjectedValues_InOrder()
        {
            SkillData skill = MakeSkill();
            SetPrivateField(skill, "_beatSequence", new[] { BeatType.Approach, BeatType.WindupReal, BeatType.Attack });

            CollectionAssert.AreEqual(new[] { BeatType.Approach, BeatType.WindupReal, BeatType.Attack }, skill.BeatSequence);

            Object.DestroyImmediate(skill);
        }
    }
}
