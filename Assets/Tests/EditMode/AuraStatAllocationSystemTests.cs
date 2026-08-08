using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers AuraStatAllocationSystem/AuraTierCeiling — Common Aura spend, tier ceiling gating (Progression_Directive_v0_1_0.md).</summary>
    public class AuraStatAllocationSystemTests
    {
        private static PhasixRuntimeData MakePhasix(int commonAura, StatBlock baseStats, int aptitude = 0, int auraAllocatedPoints = 0)
            => new PhasixRuntimeData("test-node-guid") { commonAura = commonAura, baseStats = baseStats, aptitude = aptitude, auraAllocatedPoints = auraAllocatedPoints };

        [Test]
        public void TryAllocateStatPoint_SpendsAuraAndAddsStat_WhenBelowCeiling()
        {
            var phasix = MakePhasix(commonAura: 5, baseStats: StatBlock.Zero);

            bool success = AuraStatAllocationSystem.TryAllocateStatPoint(phasix, evolutionTier: 1, StatType.Force);

            Assert.IsTrue(success);
            Assert.AreEqual(4, phasix.commonAura);
            Assert.AreEqual(1, phasix.baseStats.Force);
            Assert.AreEqual(1, phasix.auraAllocatedPoints);
        }

        [Test]
        public void TryAllocateStatPoint_Succeeds_WhenBaseStatsAlreadyExceedCeiling()
        {
            // Regression coverage for the 2026-08 follow-up fix: a species' innate baseStats.Total
            // (e.g. a high-Vitality starter) can already exceed the tier ceiling before any Aura is
            // ever spent. The ceiling must gate auraAllocatedPoints, not baseStats.Total, or every
            // real species would be permanently locked out of allocation.
            int ceiling = AuraTierCeiling.ComputeCeiling(evolutionTier: 1, aptitude: 0);
            var phasix = MakePhasix(commonAura: 5, baseStats: new StatBlock(ceiling + 100, 0, 0, 0, 0, 0, 0, 0));

            bool success = AuraStatAllocationSystem.TryAllocateStatPoint(phasix, evolutionTier: 1, StatType.Force);

            Assert.IsTrue(success, "A high innate baseStats.Total must not block allocation — only auraAllocatedPoints reaching the ceiling should.");
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
            var phasix = MakePhasix(commonAura: 999, baseStats: StatBlock.Zero, auraAllocatedPoints: ceiling);

            bool success = AuraStatAllocationSystem.TryAllocateStatPoint(phasix, evolutionTier: 1, StatType.Force);

            Assert.IsFalse(success, "Allocation must fail once auraAllocatedPoints is at or above the tier ceiling, however much Aura is available.");
        }

        [Test]
        public void TryAllocateStatPoint_DoesNotSpendAuraOrIncrementAllocatedPoints_WhenBlockedByCeiling()
        {
            int ceiling = AuraTierCeiling.ComputeCeiling(evolutionTier: 1, aptitude: 0);
            var phasix = MakePhasix(commonAura: 999, baseStats: StatBlock.Zero, auraAllocatedPoints: ceiling);

            AuraStatAllocationSystem.TryAllocateStatPoint(phasix, evolutionTier: 1, StatType.Force);

            Assert.AreEqual(999, phasix.commonAura, "A ceiling-blocked allocation must not spend Aura either.");
            Assert.AreEqual(ceiling, phasix.auraAllocatedPoints, "A ceiling-blocked allocation must not increment auraAllocatedPoints either.");
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
            var phasix = MakePhasix(commonAura: 0, baseStats: StatBlock.Zero, auraAllocatedPoints: ceiling + 50);

            Assert.AreEqual(0, AuraStatAllocationSystem.GetRemainingCeilingRoom(phasix, evolutionTier: 1));
        }

        [Test]
        public void GetRemainingCeilingRoom_IgnoresBaseStatsTotal()
        {
            // Same regression theme as TryAllocateStatPoint_Succeeds_WhenBaseStatsAlreadyExceedCeiling:
            // remaining room must be driven entirely by auraAllocatedPoints, not baseStats.Total.
            int ceiling = AuraTierCeiling.ComputeCeiling(evolutionTier: 1, aptitude: 0);
            var phasix = MakePhasix(commonAura: 0, baseStats: new StatBlock(ceiling + 500, 0, 0, 0, 0, 0, 0, 0), auraAllocatedPoints: 0);

            Assert.AreEqual(ceiling, AuraStatAllocationSystem.GetRemainingCeilingRoom(phasix, evolutionTier: 1));
        }
    }
}
