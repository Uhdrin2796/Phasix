using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers AuraStatAllocationSystem/AuraTierCeiling — Common Aura spend, tier ceiling gating (Progression_Directive_v0_1_0.md).</summary>
    public class AuraStatAllocationSystemTests
    {
        private static PhasixRuntimeData MakePhasix(int commonAura, StatBlock baseStats, int aptitude = 0)
            => new PhasixRuntimeData("test-node-guid") { commonAura = commonAura, baseStats = baseStats, aptitude = aptitude };

        [Test]
        public void TryAllocateStatPoint_SpendsAuraAndAddsStat_WhenBelowCeiling()
        {
            var phasix = MakePhasix(commonAura: 5, baseStats: StatBlock.Zero);

            bool success = AuraStatAllocationSystem.TryAllocateStatPoint(phasix, evolutionTier: 1, StatType.Force);

            Assert.IsTrue(success);
            Assert.AreEqual(4, phasix.commonAura);
            Assert.AreEqual(1, phasix.baseStats.Force);
        }

        [Test]
        public void TryAllocateStatPoint_Fails_WhenNotEnoughAura()
        {
            var phasix = MakePhasix(commonAura: 0, baseStats: StatBlock.Zero);

            bool success = AuraStatAllocationSystem.TryAllocateStatPoint(phasix, evolutionTier: 1, StatType.Force);

            Assert.IsFalse(success);
            Assert.AreEqual(0, phasix.baseStats.Force, "A failed allocation must not add the stat point.");
        }

        [Test]
        public void TryAllocateStatPoint_Fails_AtTierCeiling()
        {
            int ceiling = AuraTierCeiling.ComputeCeiling(evolutionTier: 1, aptitude: 0);
            var phasix = MakePhasix(commonAura: 999, baseStats: new StatBlock(ceiling, 0, 0, 0, 0, 0, 0, 0));

            bool success = AuraStatAllocationSystem.TryAllocateStatPoint(phasix, evolutionTier: 1, StatType.Force);

            Assert.IsFalse(success, "Allocation must fail once baseStats.Total is at or above the tier ceiling, however much Aura is available.");
        }

        [Test]
        public void TryAllocateStatPoint_DoesNotSpendAura_WhenBlockedByCeiling()
        {
            int ceiling = AuraTierCeiling.ComputeCeiling(evolutionTier: 1, aptitude: 0);
            var phasix = MakePhasix(commonAura: 999, baseStats: new StatBlock(ceiling, 0, 0, 0, 0, 0, 0, 0));

            AuraStatAllocationSystem.TryAllocateStatPoint(phasix, evolutionTier: 1, StatType.Force);

            Assert.AreEqual(999, phasix.commonAura, "A ceiling-blocked allocation must not spend Aura either.");
        }

        [Test]
        public void ComputeCeiling_HigherAptitude_RaisesCeiling()
        {
            int lowAptitude = AuraTierCeiling.ComputeCeiling(evolutionTier: 2, aptitude: 0);
            int highAptitude = AuraTierCeiling.ComputeCeiling(evolutionTier: 2, aptitude: 10);

            Assert.Greater(highAptitude, lowAptitude, "Higher Aptitude must raise the stat ceiling (Progression_Directive's Function A).");
        }

        [Test]
        public void ComputeCeiling_HigherTier_RaisesCeiling()
        {
            int t1 = AuraTierCeiling.ComputeCeiling(evolutionTier: 1, aptitude: 0);
            int t3 = AuraTierCeiling.ComputeCeiling(evolutionTier: 3, aptitude: 0);

            Assert.Greater(t3, t1, "A higher tier must have a higher ceiling than a lower one, at equal Aptitude.");
        }

        [Test]
        public void GetRemainingCeilingRoom_NeverGoesNegative()
        {
            int ceiling = AuraTierCeiling.ComputeCeiling(evolutionTier: 1, aptitude: 0);
            var phasix = MakePhasix(commonAura: 0, baseStats: new StatBlock(ceiling + 50, 0, 0, 0, 0, 0, 0, 0));

            Assert.AreEqual(0, AuraStatAllocationSystem.GetRemainingCeilingRoom(phasix, evolutionTier: 1));
        }
    }
}
