using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers SkillSlotCapacity's tier lookup table (Evolution_System_Directive_v1_1_0.md §1 / CLAUDE.md "T1=2...T5=5-7").</summary>
    public class SkillSlotCapacityTests
    {
        [TestCase(1, 2, 2)]
        [TestCase(2, 4, 3)]
        [TestCase(3, 5, 4)]
        [TestCase(4, 6, 5)]
        [TestCase(5, 7, 5)]
        public void GetTreeCount_And_GetActiveSlotRange_MatchLockedTable(int tier, int expectedTreeCount, int expectedMinSlots)
        {
            Assert.AreEqual(expectedTreeCount, SkillSlotCapacity.GetTreeCount(tier));
            Assert.AreEqual(expectedMinSlots, SkillSlotCapacity.GetActiveSlotRange(tier).min);
        }

        [Test]
        public void GetActiveSlotRange_TierFive_MaxIsSeven()
        {
            Assert.AreEqual(7, SkillSlotCapacity.GetActiveSlotRange(5).max);
        }

        [Test]
        public void GetTreeCount_TierSix_ThrowsRatherThanInventingAFusionNumber()
        {
            Assert.Throws<System.NotSupportedException>(() => SkillSlotCapacity.GetTreeCount(6));
        }
    }
}
