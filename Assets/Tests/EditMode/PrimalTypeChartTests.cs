using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers PrimalTypeChart's lookup logic against the locked GDD values (GDD_CreatureRPG_v0_8_0
    /// Section 9, "Full 8x8 Matchup Chart") and the duo-type parent-averaging fallback. Uses
    /// ScriptableObject.CreateInstance so no .asset file is needed — the chart's default serialized
    /// array already carries the locked values.
    /// </summary>
    public class PrimalTypeChartTests
    {
        private PrimalTypeChart _chart;

        [SetUp]
        public void SetUp()
        {
            _chart = ScriptableObject.CreateInstance<PrimalTypeChart>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_chart);
        }

        [Test]
        public void GetMultiplier_MatchesLockedGddValue_WaterBeatsFire()
        {
            // GDD table: Water row, Fire column = 2.0 (Triangle 1: Water -> Fire -> Life -> Water).
            Assert.AreEqual(2.0f, _chart.GetMultiplier(PrimalType.Water, PrimalType.Fire), 0.001f);
        }

        [Test]
        public void GetMultiplier_MatchesLockedGddValue_FireResistedByWater()
        {
            // GDD table: Fire row, Water column = 0.5 (the reverse of the Water-beats-Fire matchup).
            Assert.AreEqual(0.5f, _chart.GetMultiplier(PrimalType.Fire, PrimalType.Water), 0.001f);
        }

        [Test]
        public void GetMultiplier_MatchesLockedGddValue_LightShadowMutual2x()
        {
            // GDD: "Paired Opposites — Light <-> Shadow — Mutual 2x in both directions."
            Assert.AreEqual(2.0f, _chart.GetMultiplier(PrimalType.Light, PrimalType.Shadow), 0.001f);
            Assert.AreEqual(2.0f, _chart.GetMultiplier(PrimalType.Shadow, PrimalType.Light), 0.001f);
        }

        [Test]
        public void GetMultiplier_SelfMatchup_IsNeutral()
        {
            Assert.AreEqual(1.0f, _chart.GetMultiplier(PrimalType.Fire, PrimalType.Fire), 0.001f);
        }

        [Test]
        public void GetMultiplier_NeverBelowMinimum_AcrossFullChart()
        {
            foreach (PrimalType attacker in new[] { PrimalType.Fire, PrimalType.Water, PrimalType.Earth, PrimalType.Wind, PrimalType.Light, PrimalType.Shadow, PrimalType.Life, PrimalType.Lightning })
            {
                foreach (PrimalType defender in new[] { PrimalType.Fire, PrimalType.Water, PrimalType.Earth, PrimalType.Wind, PrimalType.Light, PrimalType.Shadow, PrimalType.Life, PrimalType.Lightning })
                {
                    float multiplier = _chart.GetMultiplier(attacker, defender);
                    Assert.GreaterOrEqual(multiplier, PrimalTypeChart.MinimumMultiplier,
                        $"{attacker} vs {defender} fell below the no-immunities floor.");
                }
            }
        }

        [Test]
        public void GetMultiplier_DuoType_AveragesBothParents()
        {
            // Steam = Fire + Water. Steam attacking Fire should average (Fire-vs-Fire=1.0, Water-vs-Fire=2.0) = 1.5.
            float expected = (1.0f + 2.0f) / 2f;
            Assert.AreEqual(expected, _chart.GetMultiplier(PrimalType.Steam, PrimalType.Fire), 0.001f);
        }
    }
}
