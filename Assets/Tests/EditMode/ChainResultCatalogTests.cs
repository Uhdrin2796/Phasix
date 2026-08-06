using System.Collections.Generic;
using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers ChainResultCatalog's recipe matching (GDD §17.8) — both single-recipe and multi-recipe (OR) chain results.</summary>
    public class ChainResultCatalogTests
    {
        [Test]
        public void TryResolve_BleedAndWeaken_ProducesRend()
        {
            bool found = ChainResultCatalog.TryResolve(new List<StatusEffectType> { StatusEffectType.Bleed, StatusEffectType.Weaken }, out ChainResultType result);

            Assert.IsTrue(found);
            Assert.AreEqual(ChainResultType.Rend, result);
        }

        [Test]
        public void TryResolve_BleedAndFracture_AlsoProducesRend_ViaAlternateRecipe()
        {
            bool found = ChainResultCatalog.TryResolve(new List<StatusEffectType> { StatusEffectType.Bleed, StatusEffectType.Fracture }, out ChainResultType result);

            Assert.IsTrue(found);
            Assert.AreEqual(ChainResultType.Rend, result, "Rend has two valid recipes (Bleed+Weaken OR Bleed+Fracture) — either must resolve to it.");
        }

        [Test]
        public void TryResolve_StunAndShock_ProducesParalysis()
        {
            bool found = ChainResultCatalog.TryResolve(new List<StatusEffectType> { StatusEffectType.Stun, StatusEffectType.Shock }, out ChainResultType result);

            Assert.IsTrue(found);
            Assert.AreEqual(ChainResultType.Paralysis, result);
        }

        [Test]
        public void TryResolve_NoMatchingPair_ReturnsFalse()
        {
            bool found = ChainResultCatalog.TryResolve(new List<StatusEffectType> { StatusEffectType.Regenerate, StatusEffectType.Haste }, out _);

            Assert.IsFalse(found, "Two statuses with no recipe together must not produce a chain result.");
        }

        [Test]
        public void GetEffectDescription_ReturnsNonEmptyText_ForEveryChainResult()
        {
            foreach (ChainResultType result in System.Enum.GetValues(typeof(ChainResultType)))
            {
                Assert.IsNotEmpty(ChainResultCatalog.GetEffectDescription(result));
            }
        }
    }
}
