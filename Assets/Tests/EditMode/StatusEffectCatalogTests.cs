using System;
using System.Linq;
using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers StatusEffectCatalog's coverage/shape — every StatusEffectType has an entry, and the locked category/positive counts (GDD §17) hold.</summary>
    public class StatusEffectCatalogTests
    {
        [Test]
        public void Get_ReturnsAnEntry_ForEveryStatusEffectType()
        {
            foreach (StatusEffectType type in Enum.GetValues(typeof(StatusEffectType)))
            {
                Assert.DoesNotThrow(() => StatusEffectCatalog.Get(type), $"{type} is missing a StatusEffectCatalog entry.");
            }
        }

        [Test]
        public void AllStatusTypes_TotalTwentyEight()
        {
            int count = Enum.GetValues(typeof(StatusEffectType)).Length;
            Assert.AreEqual(28, count, "GDD §17 tables sum to 28 statuses (7+7+4+4+6) — the Primer's '24' figure is a documented discrepancy, not the source of truth.");
        }

        [Test]
        public void PositiveStatuses_ExactlySix()
        {
            int positiveCount = Enum.GetValues(typeof(StatusEffectType))
                .Cast<StatusEffectType>()
                .Count(t => StatusEffectCatalog.Get(t).IsPositive);

            Assert.AreEqual(6, positiveCount, "GDD §17.7 lists exactly 6 positive statuses.");
        }

        [Test]
        public void HemorrhageDoTSet_MatchesGDDExactly()
        {
            var dots = Enum.GetValues(typeof(StatusEffectType))
                .Cast<StatusEffectType>()
                .Where(t => StatusEffectCatalog.Get(t).IsDoTForMastery)
                .ToList();

            CollectionAssert.AreEquivalent(
                new[] { StatusEffectType.Bleed, StatusEffectType.Burn, StatusEffectType.Wither, StatusEffectType.Drown, StatusEffectType.Corrode },
                dots,
                "Hemorrhage's DoT set (GDD §17.9) is exactly these 5 — Freeze reads as DoT-flavored but is NOT in this set.");
        }
    }
}
