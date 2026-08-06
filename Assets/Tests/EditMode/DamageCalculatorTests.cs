using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers DamageCalculator's formula (CLAUDE.md: (AttackerStat / DefenderStat) x skillPower x
    /// primalTypeMultiplier) — stat-pair selection by category, the null-chart fallback, and the
    /// minimum-1-damage floor. PhasixData species stubs are built via ScriptableObject.CreateInstance
    /// + reflection since PhasixData has no public constructor/setters (by design — see its own
    /// doc comment on staying read-only at runtime).
    /// </summary>
    public class DamageCalculatorTests
    {
        private static PhasixData MakeSpecies(PrimalType primalType)
        {
            var species = ScriptableObject.CreateInstance<PhasixData>();
            var field = typeof(PhasixData).GetField("_primalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(species, primalType);
            return species;
        }

        private static BattleParticipant MakeParticipant(int force, int guard, int resonance, int ward, PrimalType primalType, bool isPlayerSide)
        {
            var runtime = new PhasixRuntimeData("test-node-guid")
            {
                baseStats = new StatBlock { Vitality = 999, Force = force, Guard = guard, Resonance = resonance, Ward = ward },
                speciesData = MakeSpecies(primalType)
            };
            return new BattleParticipant(runtime, isPlayerSide);
        }

        [Test]
        public void ComputeDamage_PhysicalCategory_UsesForceAndGuard()
        {
            var attacker = MakeParticipant(force: 20, guard: 5, resonance: 999, ward: 999, primalType: PrimalType.Fire, isPlayerSide: true);
            var target = MakeParticipant(force: 5, guard: 10, resonance: 5, ward: 5, primalType: PrimalType.Fire, isPlayerSide: false);

            int damage = DamageCalculator.ComputeDamage(attacker, target, typeChart: null, category: DamageCategory.Physical, skillPower: 10);

            Assert.AreEqual(20, damage, "(20/10) x 10 x 1.0 (no chart = neutral) must equal 20.");
        }

        [Test]
        public void ComputeDamage_ElementalCategory_UsesResonanceAndWard()
        {
            var attacker = MakeParticipant(force: 999, guard: 999, resonance: 30, ward: 999, primalType: PrimalType.Fire, isPlayerSide: true);
            var target = MakeParticipant(force: 999, guard: 999, resonance: 999, ward: 15, primalType: PrimalType.Fire, isPlayerSide: false);

            int damage = DamageCalculator.ComputeDamage(attacker, target, typeChart: null, category: DamageCategory.Elemental, skillPower: 10);

            Assert.AreEqual(20, damage, "(30/15) x 10 x 1.0 must equal 20.");
        }

        [Test]
        public void ComputeDamage_NullTypeChart_FallsBackToNeutralMultiplier()
        {
            var attacker = MakeParticipant(force: 10, guard: 10, resonance: 10, ward: 10, primalType: PrimalType.Fire, isPlayerSide: true);
            var target = MakeParticipant(force: 10, guard: 10, resonance: 10, ward: 10, primalType: PrimalType.Water, isPlayerSide: false);

            int damage = DamageCalculator.ComputeDamage(attacker, target, typeChart: null, category: DamageCategory.Physical, skillPower: 10);

            Assert.AreEqual(10, damage, "With no chart wired up, damage must fall back to a neutral 1.0x, not crash or apply a real matchup.");
        }

        [Test]
        public void ComputeDamage_AppliesTypeMultiplier_WhenChartProvided()
        {
            var chart = ScriptableObject.CreateInstance<PrimalTypeChart>();
            try
            {
                var attacker = MakeParticipant(force: 10, guard: 10, resonance: 10, ward: 10, primalType: PrimalType.Water, isPlayerSide: true);
                var target = MakeParticipant(force: 10, guard: 10, resonance: 10, ward: 10, primalType: PrimalType.Fire, isPlayerSide: false);

                int damage = DamageCalculator.ComputeDamage(attacker, target, chart, DamageCategory.Physical, skillPower: 10);

                Assert.AreEqual(20, damage, "(10/10) x 10 x 2.0 (Water beats Fire, locked GDD value) must equal 20.");
            }
            finally
            {
                Object.DestroyImmediate(chart);
            }
        }

        [Test]
        public void ComputeDamage_NeverGoesBelowOne_AgainstMassiveDefense()
        {
            var attacker = MakeParticipant(force: 1, guard: 1, resonance: 1, ward: 1, primalType: PrimalType.Fire, isPlayerSide: true);
            var target = MakeParticipant(force: 1, guard: 9999, resonance: 1, ward: 1, primalType: PrimalType.Fire, isPlayerSide: false);

            int damage = DamageCalculator.ComputeDamage(attacker, target, typeChart: null, category: DamageCategory.Physical, skillPower: 1);

            Assert.GreaterOrEqual(damage, 1, "Damage must never round down to zero or negative.");
        }
    }
}
