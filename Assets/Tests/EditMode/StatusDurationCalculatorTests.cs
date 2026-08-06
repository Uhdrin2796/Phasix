using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers StatusDurationCalculator's formula (GDD §17.2): base + Resonance modifier - Resolve modifier, min 1, positive statuses skip the Resolve reduction.</summary>
    public class StatusDurationCalculatorTests
    {
        [Test]
        public void ComputeDuration_HigherResonance_ExtendsDuration()
        {
            int low = StatusDurationCalculator.ComputeDuration(baseDurationTurns: 4, applierResonance: 0, targetResolve: 0, isPositiveStatus: false);
            int high = StatusDurationCalculator.ComputeDuration(baseDurationTurns: 4, applierResonance: 30, targetResolve: 0, isPositiveStatus: false);

            Assert.Greater(high, low, "Higher applier Resonance must extend duration.");
        }

        [Test]
        public void ComputeDuration_HigherResolve_ShortensNegativeStatusDuration()
        {
            int low = StatusDurationCalculator.ComputeDuration(baseDurationTurns: 4, applierResonance: 0, targetResolve: 0, isPositiveStatus: false);
            int high = StatusDurationCalculator.ComputeDuration(baseDurationTurns: 4, applierResonance: 0, targetResolve: 30, isPositiveStatus: false);

            Assert.Less(high, low, "Higher target Resolve must shorten a negative status's duration.");
        }

        [Test]
        public void ComputeDuration_PositiveStatus_IgnoresTargetResolve()
        {
            int noResolve = StatusDurationCalculator.ComputeDuration(baseDurationTurns: 4, applierResonance: 0, targetResolve: 0, isPositiveStatus: true);
            int highResolve = StatusDurationCalculator.ComputeDuration(baseDurationTurns: 4, applierResonance: 0, targetResolve: 999, isPositiveStatus: true);

            Assert.AreEqual(noResolve, highResolve, "Positive statuses must not be reduced by target Resolve at all — GDD §17.2 is explicit about this.");
        }

        [Test]
        public void ComputeDuration_NeverDropsBelowOne()
        {
            int duration = StatusDurationCalculator.ComputeDuration(baseDurationTurns: 1, applierResonance: 0, targetResolve: 9999, isPositiveStatus: false);

            Assert.AreEqual(1, duration, "Duration must clamp to a minimum of 1 turn, however high Resolve is.");
        }
    }
}
