using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers ResonanceBonusEvaluator's Temper-priority-based alignment proxy (see class doc comment for why Temper stands in for the Directive's undesigned emotional-type alignment).</summary>
    public class ResonanceBonusEvaluatorTests
    {
        [Test]
        public void IsAligned_EdgeTemper_ForceIsAligned()
        {
            Assert.IsTrue(ResonanceBonusEvaluator.IsAligned(Temper.Edge, StatType.Force), "Force is Edge's #1 growth priority.");
        }

        [Test]
        public void IsAligned_EdgeTemper_ResolveIsNotAligned()
        {
            Assert.IsFalse(ResonanceBonusEvaluator.IsAligned(Temper.Edge, StatType.Resolve), "Resolve is Edge's lowest growth priority — must not read as aligned.");
        }

        [Test]
        public void IsAligned_AnchorTemper_VitalityIsAligned()
        {
            Assert.IsTrue(ResonanceBonusEvaluator.IsAligned(Temper.Anchor, StatType.Vitality));
        }

        [Test]
        public void IsAligned_FluxTemper_ResonanceIsAligned()
        {
            Assert.IsTrue(ResonanceBonusEvaluator.IsAligned(Temper.Flux, StatType.Resonance));
        }

        [Test]
        public void ComputeBonusMultiplier_Aligned_IsGreaterThanUnaligned()
        {
            float aligned = ResonanceBonusEvaluator.ComputeBonusMultiplier(Temper.Edge, StatType.Force);
            float unaligned = ResonanceBonusEvaluator.ComputeBonusMultiplier(Temper.Edge, StatType.Resolve);

            Assert.Greater(aligned, unaligned);
        }
    }
}
