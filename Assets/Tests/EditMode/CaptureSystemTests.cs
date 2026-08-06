using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers CaptureSystem.ComputeCaptureChancePercent's shape — lower target HP -> higher chance, always clamped, never a guaranteed capture.</summary>
    public class CaptureSystemTests
    {
        [Test]
        public void ComputeCaptureChancePercent_LowerTargetHP_ProducesHigherChance()
        {
            float fullHP = CaptureSystem.ComputeCaptureChancePercent(targetCurrentHP: 100, targetMaxHP: 100);
            float lowHP = CaptureSystem.ComputeCaptureChancePercent(targetCurrentHP: 5, targetMaxHP: 100);

            Assert.Greater(lowHP, fullHP);
        }

        [Test]
        public void ComputeCaptureChancePercent_NeverExceedsNinetyFivePercent()
        {
            float chance = CaptureSystem.ComputeCaptureChancePercent(targetCurrentHP: 0, targetMaxHP: 100);

            Assert.LessOrEqual(chance, 95f, "Capture must never be guaranteed (100%), even at 0 target HP.");
        }

        [Test]
        public void ComputeCaptureChancePercent_NeverNegative()
        {
            float chance = CaptureSystem.ComputeCaptureChancePercent(targetCurrentHP: 100, targetMaxHP: 100);

            Assert.GreaterOrEqual(chance, 0f);
        }

        [Test]
        public void ComputeCaptureChancePercent_ZeroMaxHP_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CaptureSystem.ComputeCaptureChancePercent(targetCurrentHP: 0, targetMaxHP: 0));
        }
    }
}
