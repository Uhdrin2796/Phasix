using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers TimedInputConfig.ComputeWindowPercent's scaling rules (Combat_Directive_v0_1_0.md
    /// Part 4 / CLAUDE.md: higher Instinct = larger window, bond adds a minor flat bonus, clamped
    /// to a sane range). Pure static math, no scene/prefab setup needed.
    /// </summary>
    public class TimedInputConfigTests
    {
        [Test]
        public void ComputeWindowPercent_HigherInstinct_ProducesLargerWindow()
        {
            float lowInstinct = TimedInputConfig.ComputeWindowPercent(TimedInputConfig.DodgeBaseWindowPercent, instinct: 2, bondPercent: 0f);
            float highInstinct = TimedInputConfig.ComputeWindowPercent(TimedInputConfig.DodgeBaseWindowPercent, instinct: 20, bondPercent: 0f);

            Assert.Greater(highInstinct, lowInstinct, "Higher Instinct must produce a larger timing window.");
        }

        [Test]
        public void ComputeWindowPercent_HigherBond_ProducesLargerWindow()
        {
            float noBond = TimedInputConfig.ComputeWindowPercent(TimedInputConfig.DodgeBaseWindowPercent, instinct: 5, bondPercent: 0f);
            float fullBond = TimedInputConfig.ComputeWindowPercent(TimedInputConfig.DodgeBaseWindowPercent, instinct: 5, bondPercent: 100f);

            Assert.Greater(fullBond, noBond, "Higher bond must produce a larger timing window.");
        }

        [Test]
        public void ComputeWindowPercent_ClampsToMinimum_AtZeroStats()
        {
            float window = TimedInputConfig.ComputeWindowPercent(TimedInputConfig.DodgeBaseWindowPercent, instinct: 0, bondPercent: 0f);

            Assert.GreaterOrEqual(window, 5f, "Window must never shrink below the minimum floor.");
        }

        [Test]
        public void ComputeWindowPercent_ClampsToMaximum_AtExtremeStats()
        {
            float window = TimedInputConfig.ComputeWindowPercent(TimedInputConfig.DodgeBaseWindowPercent, instinct: 9999, bondPercent: 100f);

            Assert.LessOrEqual(window, 60f, "Window must never exceed the maximum ceiling, however high the stats.");
        }

        [Test]
        public void ComputeWindowPercent_ParryBase_ProducesNarrowerWindow_ThanDodgeBase()
        {
            float dodgeWindow = TimedInputConfig.ComputeWindowPercent(TimedInputConfig.DodgeBaseWindowPercent, instinct: 5, bondPercent: 0f);
            float parryWindow = TimedInputConfig.ComputeWindowPercent(TimedInputConfig.ParryBaseWindowPercent, instinct: 5, bondPercent: 0f);

            Assert.Less(parryWindow, dodgeWindow, "Parry is the higher-risk option — its window must stay narrower than Dodge's at the same Instinct/bond.");
        }

        [Test]
        public void ComputeToleranceHalfWidth_AtZeroStats_ReturnsExactlyTheBaseValue()
        {
            float tolerance = TimedInputConfig.ComputeToleranceHalfWidth(
                TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent, instinct: 0, bondPercent: 0f);

            Assert.AreEqual(TimedInputConfig.DodgeToleranceHalfWidth, tolerance, 0.0001f,
                "At 0 Instinct/bond, ComputeWindowPercent returns exactly baseWindowPercent, so the scale factor must be exactly 1.");
        }

        [Test]
        public void ComputeToleranceHalfWidth_HigherInstinct_ProducesWiderTolerance()
        {
            float low = TimedInputConfig.ComputeToleranceHalfWidth(TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent, instinct: 0, bondPercent: 0f);
            float high = TimedInputConfig.ComputeToleranceHalfWidth(TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent, instinct: 20, bondPercent: 0f);

            Assert.Greater(high, low, "Higher Instinct must widen the ring-ratio tolerance, same as it widens the old bar window.");
        }

        [Test]
        public void ComputeToleranceHalfWidth_ParryBase_ProducesNarrowerTolerance_ThanDodgeBase()
        {
            float dodgeTolerance = TimedInputConfig.ComputeToleranceHalfWidth(TimedInputConfig.DodgeToleranceHalfWidth, TimedInputConfig.DodgeBaseWindowPercent, instinct: 5, bondPercent: 0f);
            float parryTolerance = TimedInputConfig.ComputeToleranceHalfWidth(TimedInputConfig.ParryToleranceHalfWidth, TimedInputConfig.ParryBaseWindowPercent, instinct: 5, bondPercent: 0f);

            Assert.Less(parryTolerance, dodgeTolerance, "Parry's tolerance must stay narrower than Dodge's at the same Instinct/bond.");
        }
    }
}
