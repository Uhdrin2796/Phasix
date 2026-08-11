using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers BattleHUDController.ComputeSweepDurationForTravelTime — the pure ring-geometry math
    /// behind the 2026-08-11 combat feedback timing-sync pass (a projectile's real travel time
    /// determines how fast the matching timing ring needs to sweep so its "perfect" instant lands
    /// exactly when the projectile visually connects). Instantiated on an INACTIVE GameObject so
    /// Awake() (which requires a real UIDocument/UXML hierarchy this test doesn't set up) never
    /// runs — safe here because this specific method only reads BattleHUDController's private ring
    /// constants, none of which Awake populates.
    /// </summary>
    public class BattleHUDControllerTests
    {
        private static BattleHUDController MakeInactiveController()
        {
            var go = new GameObject("InactiveBattleHUDController");
            go.SetActive(false);
            return go.AddComponent<BattleHUDController>();
        }

        [Test]
        public void ComputeSweepDurationForTravelTime_MatchesRingGeometryFormula()
        {
            BattleHUDController hud = MakeInactiveController();

            // Ring constants: MarkerStartRadius=60, TargetRadius=30, MarkerMinRadius=2 (private,
            // mirrored here since the formula's correctness is exactly what's under test).
            float expectedPerfectFraction = (60f - 30f) / (60f - 2f);
            float travelDuration = 1f;

            float sweepDuration = hud.ComputeSweepDurationForTravelTime(travelDuration);

            Assert.AreEqual(travelDuration / expectedPerfectFraction, sweepDuration, 0.0001f);

            Object.DestroyImmediate(hud.gameObject);
        }

        [Test]
        public void ComputeSweepDurationForTravelTime_ScalesLinearlyWithTravelTime()
        {
            BattleHUDController hud = MakeInactiveController();

            float sweepAt1s = hud.ComputeSweepDurationForTravelTime(1f);
            float sweepAt2s = hud.ComputeSweepDurationForTravelTime(2f);

            Assert.AreEqual(sweepAt1s * 2f, sweepAt2s, 0.0001f);

            Object.DestroyImmediate(hud.gameObject);
        }

        [Test]
        public void ComputeSweepDurationForTravelTime_ZeroTravelTime_ReturnsZero()
        {
            BattleHUDController hud = MakeInactiveController();

            Assert.AreEqual(0f, hud.ComputeSweepDurationForTravelTime(0f), 0.0001f);

            Object.DestroyImmediate(hud.gameObject);
        }
    }
}
