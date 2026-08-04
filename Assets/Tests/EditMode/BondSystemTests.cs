using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers BondSystem's structural rules: floor logic, session loss cap, and high-bond
    /// damping (CLAUDE.md → Bond). BondSystem is pure static logic over plain PhasixRuntimeData,
    /// so no scene/prefab/ScriptableObject setup is needed. Added per AUDIT_202608.md AUD-012 —
    /// written and hand-verified against BondSystem.cs's source, not yet run (no live Unity
    /// Editor/Test Runner in this session — see KNOWN_ISSUES.md).
    /// </summary>
    public class BondSystemTests
    {
        private static PhasixRuntimeData MakePhasix(float bondPercent, float bondFloor, float sessionBondLoss = 0f)
        {
            return new PhasixRuntimeData("test-node-guid")
            {
                bondPercent = bondPercent,
                bondFloor = bondFloor,
                sessionBondLoss = sessionBondLoss
            };
        }

        [Test]
        public void ApplyBondChange_Loss_NeverDropsBelowFloor()
        {
            // Delta magnitude (4) stays under the 5% session cap, so the floor — not the cap —
            // is what stops this at 28 instead of the naive 30 - 4 = 26.
            var phasix = MakePhasix(bondPercent: 30f, bondFloor: 28f);

            BondSystem.ApplyBondChange(phasix, -4f);

            Assert.AreEqual(28f, phasix.bondPercent, 0.001f,
                "Loss must never push bond below the last milestone floor.");
        }

        [Test]
        public void ApplyBondChange_Loss_RespectsSessionCapAcrossMultipleCalls()
        {
            var phasix = MakePhasix(bondPercent: 50f, bondFloor: 0f);

            BondSystem.ApplyBondChange(phasix, -3f); // uses 3 of the 5% cap
            BondSystem.ApplyBondChange(phasix, -3f); // only 2% of cap remains — second loss is clipped

            Assert.AreEqual(45f, phasix.bondPercent, 0.001f,
                "Cumulative session loss must never exceed the 5% cap, even across multiple calls.");
            Assert.AreEqual(5f, phasix.sessionBondLoss, 0.001f);
        }

        [Test]
        public void ApplyBondChange_Loss_HalvedAbovePartnerThreshold()
        {
            // Above 60% (Partner), losses are halved before the floor/cap are applied.
            var phasix = MakePhasix(bondPercent: 65f, bondFloor: 0f);

            BondSystem.ApplyBondChange(phasix, -4f);

            Assert.AreEqual(63f, phasix.bondPercent, 0.001f, "Losses above 60% must be halved.");
        }

        [Test]
        public void ApplyBondChange_Loss_QuarteredAboveBondedThreshold()
        {
            // Above 80% (Bonded), losses are quartered before the floor/cap are applied.
            var phasix = MakePhasix(bondPercent: 85f, bondFloor: 0f);

            BondSystem.ApplyBondChange(phasix, -4f);

            Assert.AreEqual(84f, phasix.bondPercent, 0.001f, "Losses above 80% must be quartered.");
        }

        [Test]
        public void ApplyBondChange_NoOp_WhenBondIsAlready100Percent()
        {
            var phasix = MakePhasix(bondPercent: 100f, bondFloor: 80f);

            BondSystem.ApplyBondChange(phasix, -10f);
            BondSystem.ApplyBondChange(phasix, 10f);

            Assert.AreEqual(100f, phasix.bondPercent,
                "100% bond must be permanent — immune to any change via ApplyBondChange.");
        }

        [Test]
        public void ApplyBondChange_Gain_RaisesFloorOnMilestoneCross()
        {
            var phasix = MakePhasix(bondPercent: 15f, bondFloor: 0f);

            BondSystem.ApplyBondChange(phasix, 10f); // crosses Familiar (20%)

            Assert.AreEqual(20f, phasix.bondFloor, "Crossing a milestone must raise bondFloor to match.");
        }

        [Test]
        public void ApplyBondChange_Gain_ClampsAt100()
        {
            var phasix = MakePhasix(bondPercent: 95f, bondFloor: 80f);

            BondSystem.ApplyBondChange(phasix, 20f);

            Assert.AreEqual(100f, phasix.bondPercent, "Gain must clamp at 100, never overshoot.");
        }
    }
}
