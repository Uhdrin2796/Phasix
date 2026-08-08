using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers ComboRuleEvaluator's two NEW, user-directed combo rules (2026-08 session, see
    /// DECISIONS.md -> [Combat]) — RepeatSameSkill (reward repeating the specific granting skill,
    /// e.g. "C1") and TimedInputStreak (reward consecutive PERFECT timed inputs on any equipped
    /// attacking skill while the granting passive, e.g. "C2," is equipped). Neither is GDD content.
    /// </summary>
    public class ComboRuleEvaluatorTests
    {
        private SkillData _skillA;
        private SkillData _skillB;

        [SetUp]
        public void SetUp()
        {
            _skillA = ScriptableObject.CreateInstance<SkillData>();
            _skillB = ScriptableObject.CreateInstance<SkillData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_skillA);
            Object.DestroyImmediate(_skillB);
        }

        [Test]
        public void EvaluateRepeatSameSkill_TwoUsesOfGrantingSkillInARow_ReturnsDuo()
        {
            var sequence = new List<SkillData> { _skillA, _skillA };

            Assert.AreEqual(ComboTier.Duo, ComboRuleEvaluator.EvaluateRepeatSameSkill(sequence, _skillA));
        }

        [Test]
        public void EvaluateRepeatSameSkill_ThreeUsesOfGrantingSkillInARow_ReturnsTrio()
        {
            var sequence = new List<SkillData> { _skillA, _skillA, _skillA };

            Assert.AreEqual(ComboTier.Trio, ComboRuleEvaluator.EvaluateRepeatSameSkill(sequence, _skillA));
        }

        [Test]
        public void EvaluateRepeatSameSkill_FourUsesOfGrantingSkillInARow_ReturnsQuad()
        {
            var sequence = new List<SkillData> { _skillA, _skillA, _skillA, _skillA };

            Assert.AreEqual(ComboTier.Quad, ComboRuleEvaluator.EvaluateRepeatSameSkill(sequence, _skillA));
        }

        [Test]
        public void EvaluateRepeatSameSkill_DifferingEntryInWindow_BreaksTheStreak()
        {
            var sequence = new List<SkillData> { _skillA, _skillB };

            Assert.IsNull(ComboRuleEvaluator.EvaluateRepeatSameSkill(sequence, _skillA));
        }

        [Test]
        public void EvaluateRepeatSameSkill_RepeatingADifferentSkill_DoesNotCount()
        {
            // The streak is tied to the GRANTING skill specifically (e.g. "C1") — repeating some
            // other skill twice must not satisfy this rule, even though it's a "repeat."
            var sequence = new List<SkillData> { _skillB, _skillB };

            Assert.IsNull(ComboRuleEvaluator.EvaluateRepeatSameSkill(sequence, _skillA));
        }

        [Test]
        public void EvaluateRepeatSameSkill_SingleUse_ReturnsNull()
        {
            var sequence = new List<SkillData> { _skillA };

            Assert.IsNull(ComboRuleEvaluator.EvaluateRepeatSameSkill(sequence, _skillA));
        }

        [Test]
        public void EvaluateRepeatSameSkill_EmptyHistory_ReturnsNull()
        {
            Assert.IsNull(ComboRuleEvaluator.EvaluateRepeatSameSkill(new List<SkillData>(), _skillA));
        }

        [Test]
        public void EvaluateRepeatSameSkill_NullGrantingSkill_ReturnsNull()
        {
            var sequence = new List<SkillData> { _skillA, _skillA };

            Assert.IsNull(ComboRuleEvaluator.EvaluateRepeatSameSkill(sequence, null));
        }

        [Test]
        public void EvaluateRepeatSameSkill_FiveInARow_StillReturnsQuad_NotBeyond()
        {
            var sequence = new List<SkillData> { _skillA, _skillA, _skillA, _skillA, _skillA };

            Assert.AreEqual(ComboTier.Quad, ComboRuleEvaluator.EvaluateRepeatSameSkill(sequence, _skillA));
        }

        [Test]
        public void EvaluateTimedInputStreak_TwoPerfectsInARow_ReturnsDuo()
        {
            var sequence = new List<bool> { true, true };

            Assert.AreEqual(ComboTier.Duo, ComboRuleEvaluator.EvaluateTimedInputStreak(sequence));
        }

        [Test]
        public void EvaluateTimedInputStreak_FourPerfectsInARow_ReturnsQuad()
        {
            var sequence = new List<bool> { true, true, true, true };

            Assert.AreEqual(ComboTier.Quad, ComboRuleEvaluator.EvaluateTimedInputStreak(sequence));
        }

        [Test]
        public void EvaluateTimedInputStreak_NonPerfectInWindow_BreaksTheStreak()
        {
            var sequence = new List<bool> { true, false };

            Assert.IsNull(ComboRuleEvaluator.EvaluateTimedInputStreak(sequence));
        }

        [Test]
        public void EvaluateTimedInputStreak_SinglePerfect_ReturnsNull()
        {
            var sequence = new List<bool> { true };

            Assert.IsNull(ComboRuleEvaluator.EvaluateTimedInputStreak(sequence));
        }

        [Test]
        public void EvaluateTimedInputStreak_EmptyHistory_ReturnsNull()
        {
            Assert.IsNull(ComboRuleEvaluator.EvaluateTimedInputStreak(new List<bool>()));
        }

        [Test]
        public void GetRepeatTrailingStreakLength_MatchesActualStreakOfGrantingSkill()
        {
            Assert.AreEqual(0, ComboRuleEvaluator.GetRepeatTrailingStreakLength(new List<SkillData>(), _skillA));
            Assert.AreEqual(1, ComboRuleEvaluator.GetRepeatTrailingStreakLength(new List<SkillData> { _skillA }, _skillA));
            Assert.AreEqual(3, ComboRuleEvaluator.GetRepeatTrailingStreakLength(new List<SkillData> { _skillA, _skillA, _skillA }, _skillA));
        }

        [Test]
        public void GetRepeatTrailingStreakLength_BreaksOnDifferingEntry_CountsOnlyTrailingRun()
        {
            var sequence = new List<SkillData> { _skillA, _skillA, _skillB, _skillA };

            Assert.AreEqual(1, ComboRuleEvaluator.GetRepeatTrailingStreakLength(sequence, _skillA), "Only the trailing run (just the last _skillA) counts, not the earlier _skillA pair.");
        }

        [Test]
        public void GetRepeatTrailingStreakLength_RepeatingADifferentSkill_ReturnsZero()
        {
            var sequence = new List<SkillData> { _skillB, _skillB, _skillB };

            Assert.AreEqual(0, ComboRuleEvaluator.GetRepeatTrailingStreakLength(sequence, _skillA), "Repeating a skill other than the granting skill must not count toward its streak.");
        }

        [Test]
        public void GetRepeatTrailingStreakLength_NullGrantingSkill_ReturnsZero()
        {
            var sequence = new List<SkillData> { _skillA, _skillA };

            Assert.AreEqual(0, ComboRuleEvaluator.GetRepeatTrailingStreakLength(sequence, null));
        }

        [Test]
        public void GetTimedInputTrailingStreakLength_MatchesActualStreak()
        {
            Assert.AreEqual(0, ComboRuleEvaluator.GetTimedInputTrailingStreakLength(new List<bool>()));
            Assert.AreEqual(1, ComboRuleEvaluator.GetTimedInputTrailingStreakLength(new List<bool> { true }));
            Assert.AreEqual(3, ComboRuleEvaluator.GetTimedInputTrailingStreakLength(new List<bool> { true, true, true }));
        }

        [Test]
        public void GetTimedInputTrailingStreakLength_BreaksOnNonPerfect_CountsOnlyTrailingRun()
        {
            var sequence = new List<bool> { true, true, false, true };

            Assert.AreEqual(1, ComboRuleEvaluator.GetTimedInputTrailingStreakLength(sequence), "Only the trailing run (just the last perfect) counts, not the earlier perfect pair.");
        }

        [Test]
        public void GetTimedInputTrailingStreakLength_TrailingNonPerfect_ReturnsZero()
        {
            var sequence = new List<bool> { true, true, false };

            Assert.AreEqual(0, ComboRuleEvaluator.GetTimedInputTrailingStreakLength(sequence));
        }
    }
}
