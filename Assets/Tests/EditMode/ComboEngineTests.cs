using System.Collections.Generic;
using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers ComboEngine's combo detection (GDD §4.2) and its placeholder Instinct/bond scaling curves.</summary>
    public class ComboEngineTests
    {
        [Test]
        public void DetectCombo_TwoDifferentTrees_ReturnsDuo()
        {
            var sequence = new List<SkillTreeType> { SkillTreeType.Utility, SkillTreeType.Aura };

            Assert.AreEqual(ComboTier.Duo, ComboEngine.DetectCombo(sequence));
        }

        [Test]
        public void DetectCombo_ThreeDifferentTrees_ReturnsTrio()
        {
            var sequence = new List<SkillTreeType> { SkillTreeType.Utility, SkillTreeType.Aura, SkillTreeType.Passive };

            Assert.AreEqual(ComboTier.Trio, ComboEngine.DetectCombo(sequence));
        }

        [Test]
        public void DetectCombo_FourDifferentTrees_ReturnsQuad()
        {
            var sequence = new List<SkillTreeType> { SkillTreeType.Utility, SkillTreeType.Aura, SkillTreeType.Passive, SkillTreeType.Synergy };

            Assert.AreEqual(ComboTier.Quad, ComboEngine.DetectCombo(sequence));
        }

        [Test]
        public void DetectCombo_RepeatedTreeInWindow_BreaksTheCombo()
        {
            var sequence = new List<SkillTreeType> { SkillTreeType.Utility, SkillTreeType.Utility };

            Assert.IsNull(ComboEngine.DetectCombo(sequence), "Using the same tree twice in a row must not count as a Duo.");
        }

        [Test]
        public void DetectCombo_SingleSkillUsed_ReturnsNull()
        {
            var sequence = new List<SkillTreeType> { SkillTreeType.Utility };

            Assert.IsNull(ComboEngine.DetectCombo(sequence), "A combo needs at least 2 skills.");
        }

        [Test]
        public void DetectCombo_FiveDistinctTrees_StillReturnsQuad_NotBeyond()
        {
            // Quad (4) is the highest tier the GDD defines — a 5th distinct tree in a row doesn't invent a 5th tier.
            var sequence = new List<SkillTreeType>
            {
                SkillTreeType.Reaction, SkillTreeType.Utility, SkillTreeType.Aura, SkillTreeType.Passive, SkillTreeType.Synergy
            };

            Assert.AreEqual(ComboTier.Quad, ComboEngine.DetectCombo(sequence));
        }

        [Test]
        public void ComputeTriggerChancePercent_HigherInstinct_ProducesHigherChance()
        {
            float low = ComboEngine.ComputeTriggerChancePercent(instinct: 0);
            float high = ComboEngine.ComputeTriggerChancePercent(instinct: 30);

            Assert.Greater(high, low);
        }

        [Test]
        public void ComputeDiscoveryBonusPercent_AtOrBelowSixtyBond_IsZero()
        {
            Assert.AreEqual(0f, ComboEngine.ComputeDiscoveryBonusPercent(60f));
            Assert.AreEqual(0f, ComboEngine.ComputeDiscoveryBonusPercent(30f));
        }

        [Test]
        public void ComputeDiscoveryBonusPercent_AboveSixtyBond_IsPositive()
        {
            Assert.Greater(ComboEngine.ComputeDiscoveryBonusPercent(80f), 0f);
        }

        [Test]
        public void GetDistinctTrailingStreakLength_MatchesActualStreak()
        {
            Assert.AreEqual(0, ComboEngine.GetDistinctTrailingStreakLength(new List<SkillTreeType>()));
            Assert.AreEqual(1, ComboEngine.GetDistinctTrailingStreakLength(new List<SkillTreeType> { SkillTreeType.Utility }));
            Assert.AreEqual(3, ComboEngine.GetDistinctTrailingStreakLength(new List<SkillTreeType> { SkillTreeType.Utility, SkillTreeType.Aura, SkillTreeType.Passive }));
        }

        [Test]
        public void GetDistinctTrailingStreakLength_BreaksOnRepeat_CountsOnlyTrailingRun()
        {
            var sequence = new List<SkillTreeType> { SkillTreeType.Utility, SkillTreeType.Aura, SkillTreeType.Utility, SkillTreeType.Passive };

            Assert.AreEqual(3, ComboEngine.GetDistinctTrailingStreakLength(sequence), "Walking backward from Passive: Passive, Utility, Aura are all distinct (length 3) before the earlier Utility at index 0 repeats and breaks the streak.");
        }

        [Test]
        public void GetDistinctTrailingStreakLength_TrailingRepeat_ReturnsOne()
        {
            var sequence = new List<SkillTreeType> { SkillTreeType.Utility, SkillTreeType.Utility };

            Assert.AreEqual(1, ComboEngine.GetDistinctTrailingStreakLength(sequence));
        }
    }
}
