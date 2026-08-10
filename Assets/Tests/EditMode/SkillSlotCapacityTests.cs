using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers SkillSlotCapacity's tier lookup table. GetTreeCount still matches
    /// Evolution_System_Directive_v1_1_0.md §1 (2/4/5/6/7). GetActiveSlotRange's numbers were
    /// overridden 2026-08 (see SkillSlotCapacity.GetActiveSlotRange's own doc comment +
    /// DECISIONS.md -> [Progression]) to a flat 4/6/8/10/12 progression reaching all 12 wheel
    /// positions at T5 — CLAUDE.md's older "T1=2...T5=5-7" reference is stale for slot count.
    /// </summary>
    public class SkillSlotCapacityTests
    {
        [TestCase(1, 2, 4)]
        [TestCase(2, 4, 6)]
        [TestCase(3, 5, 8)]
        [TestCase(4, 6, 10)]
        [TestCase(5, 7, 12)]
        public void GetTreeCount_And_GetActiveSlotRange_MatchLockedTable(int tier, int expectedTreeCount, int expectedSlots)
        {
            Assert.AreEqual(expectedTreeCount, SkillSlotCapacity.GetTreeCount(tier));
            Assert.AreEqual(expectedSlots, SkillSlotCapacity.GetActiveSlotRange(tier).min);
            Assert.AreEqual(expectedSlots, SkillSlotCapacity.GetActiveSlotRange(tier).max);
        }

        [Test]
        public void GetActiveSlotRange_TierFive_MaxIsTwelve()
        {
            Assert.AreEqual(12, SkillSlotCapacity.GetActiveSlotRange(5).max);
        }

        [Test]
        public void GetTreeCount_TierSix_ThrowsRatherThanInventingAFusionNumber()
        {
            Assert.Throws<System.NotSupportedException>(() => SkillSlotCapacity.GetTreeCount(6));
        }
    }
}
