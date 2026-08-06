using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers EvolutionBurstSystem's gauge fill/trigger/expiry state machine (GDD §9.3).</summary>
    public class EvolutionBurstSystemTests
    {
        [Test]
        public void AddFill_AccumulatesAndClampsAtHundred()
        {
            var gauge = new EvolutionBurstGauge();

            EvolutionBurstSystem.AddFill(gauge, 60f);
            EvolutionBurstSystem.AddFill(gauge, 60f);

            Assert.AreEqual(100f, gauge.FillPercent, "Fill must clamp at 100, not overshoot.");
        }

        [Test]
        public void AddFill_DoesNothing_WhileAlreadyActive()
        {
            var gauge = new EvolutionBurstGauge { IsActive = true, FillPercent = 0f };

            EvolutionBurstSystem.AddFill(gauge, 50f);

            Assert.AreEqual(0f, gauge.FillPercent, "Gauge must not fill again while a burst is already active.");
        }

        [Test]
        public void TryTrigger_BelowFullGauge_Fails()
        {
            var gauge = new EvolutionBurstGauge { FillPercent = 99f };

            bool triggered = EvolutionBurstSystem.TryTrigger(gauge, bondPercent: 100f);

            Assert.IsFalse(triggered);
            Assert.IsFalse(gauge.IsActive);
        }

        [Test]
        public void TryTrigger_FullGauge_AtOrAboveReliableBond_AlwaysTriggers()
        {
            var gauge = new EvolutionBurstGauge { FillPercent = 100f };

            bool triggered = EvolutionBurstSystem.TryTrigger(gauge, bondPercent: 40f);

            Assert.IsTrue(triggered, "At/above the Companion (40%) bond threshold, burst is 'reliable' per GDD §14.2 — must always trigger when full.");
            Assert.IsTrue(gauge.IsActive);
        }

        [Test]
        public void TryTrigger_Success_ResetsFillAndSetsDuration()
        {
            var gauge = new EvolutionBurstGauge { FillPercent = 100f };

            EvolutionBurstSystem.TryTrigger(gauge, bondPercent: 100f);

            Assert.AreEqual(0f, gauge.FillPercent);
            Assert.Greater(gauge.RemainingDurationTurns, 0);
        }

        [Test]
        public void TryTrigger_AlreadyActive_Fails()
        {
            var gauge = new EvolutionBurstGauge { IsActive = true, FillPercent = 100f };

            bool triggered = EvolutionBurstSystem.TryTrigger(gauge, bondPercent: 100f);

            Assert.IsFalse(triggered, "A burst already in progress must not re-trigger.");
        }

        [Test]
        public void ActivateReady_FullGauge_AlwaysSucceeds_RegardlessOfBond()
        {
            // Unlike TryTrigger, ActivateReady has no reliability chance — a deliberate click on
            // an already-visually-ready bar should never silently fail (2026-08-06, user-directed
            // — see DECISIONS.md -> [Combat]).
            for (int i = 0; i < 50; i++)
            {
                var gauge = new EvolutionBurstGauge { FillPercent = 100f };
                bool activated = EvolutionBurstSystem.ActivateReady(gauge, bondPercent: 0f);
                Assert.IsTrue(activated, "ActivateReady must always succeed on a full gauge, even at 0% bond.");
            }
        }

        [Test]
        public void ActivateReady_Success_ResetsFillAndSetsDuration()
        {
            var gauge = new EvolutionBurstGauge { FillPercent = 100f };

            EvolutionBurstSystem.ActivateReady(gauge, bondPercent: 0f);

            Assert.IsTrue(gauge.IsActive);
            Assert.AreEqual(0f, gauge.FillPercent);
            Assert.Greater(gauge.RemainingDurationTurns, 0);
        }

        [Test]
        public void ActivateReady_BelowFullGauge_Fails()
        {
            var gauge = new EvolutionBurstGauge { FillPercent = 99f };

            bool activated = EvolutionBurstSystem.ActivateReady(gauge, bondPercent: 100f);

            Assert.IsFalse(activated, "They can only activate when the gauge is full.");
            Assert.IsFalse(gauge.IsActive);
        }

        [Test]
        public void ActivateReady_AlreadyActive_Fails()
        {
            var gauge = new EvolutionBurstGauge { IsActive = true, FillPercent = 100f };

            bool activated = EvolutionBurstSystem.ActivateReady(gauge, bondPercent: 100f);

            Assert.IsFalse(activated, "A burst already in progress must not re-activate.");
        }

        [Test]
        public void ComputeDurationTurns_HigherBond_ProducesLongerDuration()
        {
            int lowBond = EvolutionBurstSystem.ComputeDurationTurns(bondPercent: 0f);
            int highBond = EvolutionBurstSystem.ComputeDurationTurns(bondPercent: 100f);

            Assert.Greater(highBond, lowBond, "Higher bond must produce a longer burst duration (GDD §9.3: 'higher bond = ... longer burst duration').");
        }

        [Test]
        public void TickTurn_DecrementsDuration_AndDeactivatesAtZero()
        {
            var gauge = new EvolutionBurstGauge { IsActive = true, RemainingDurationTurns = 1 };

            EvolutionBurstSystem.TickTurn(gauge);

            Assert.IsFalse(gauge.IsActive, "Burst must end and the creature return to base form once duration expires.");
            Assert.AreEqual(0, gauge.RemainingDurationTurns);
        }

        [Test]
        public void TickTurn_InactiveGauge_IsNoOp()
        {
            var gauge = new EvolutionBurstGauge { IsActive = false, RemainingDurationTurns = 0 };

            Assert.DoesNotThrow(() => EvolutionBurstSystem.TickTurn(gauge));
            Assert.AreEqual(0, gauge.RemainingDurationTurns);
        }
    }
}
