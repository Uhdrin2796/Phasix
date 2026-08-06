using System.Collections.Generic;
using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers MasteryBonusCatalog.EvaluateAll's trigger conditions (GDD §17.9) — one test per bonus, plus stacking and the "no trigger" baseline.</summary>
    public class MasteryBonusCatalogTests
    {
        private static readonly List<StatusEffectType> None = new List<StatusEffectType>();

        [Test]
        public void EvaluateAll_ThreeDoTsOnTarget_TriggersHemorrhage()
        {
            var target = new List<StatusEffectType> { StatusEffectType.Bleed, StatusEffectType.Burn, StatusEffectType.Wither };

            var triggered = MasteryBonusCatalog.EvaluateAll(None, target);

            CollectionAssert.Contains(triggered, MasteryBonusType.Hemorrhage);
        }

        [Test]
        public void EvaluateAll_ThreeControlsOnTarget_TriggersDominance()
        {
            var target = new List<StatusEffectType> { StatusEffectType.Stun, StatusEffectType.Root, StatusEffectType.Blind };

            var triggered = MasteryBonusCatalog.EvaluateAll(None, target);

            CollectionAssert.Contains(triggered, MasteryBonusType.Dominance);
        }

        [Test]
        public void EvaluateAll_ThreeDebuffsOnTarget_TriggersCollapse()
        {
            var target = new List<StatusEffectType> { StatusEffectType.Fracture, StatusEffectType.Weaken, StatusEffectType.Exposed };

            var triggered = MasteryBonusCatalog.EvaluateAll(None, target);

            CollectionAssert.Contains(triggered, MasteryBonusType.Collapse);
        }

        [Test]
        public void EvaluateAll_ThreeSignalStatusesOnTarget_TriggersOvermaster()
        {
            var target = new List<StatusEffectType> { StatusEffectType.Drain, StatusEffectType.Disrupt, StatusEffectType.Overload };

            var triggered = MasteryBonusCatalog.EvaluateAll(None, target);

            CollectionAssert.Contains(triggered, MasteryBonusType.Overmaster);
        }

        [Test]
        public void EvaluateAll_TwoDoTsPlusOneControl_TriggersPressure()
        {
            var target = new List<StatusEffectType> { StatusEffectType.Bleed, StatusEffectType.Burn, StatusEffectType.Stun };

            var triggered = MasteryBonusCatalog.EvaluateAll(None, target);

            CollectionAssert.Contains(triggered, MasteryBonusType.Pressure);
        }

        [Test]
        public void EvaluateAll_SelfPositivePlusTwoTargetNegatives_TriggersContrast()
        {
            var self = new List<StatusEffectType> { StatusEffectType.Regenerate };
            var target = new List<StatusEffectType> { StatusEffectType.Bleed, StatusEffectType.Weaken };

            var triggered = MasteryBonusCatalog.EvaluateAll(self, target);

            CollectionAssert.Contains(triggered, MasteryBonusType.Contrast);
        }

        [Test]
        public void EvaluateAll_OnePhysicalOneElementalOneSignalOnTarget_TriggersConvergence()
        {
            var target = new List<StatusEffectType> { StatusEffectType.Bleed, StatusEffectType.Burn, StatusEffectType.Drain };

            var triggered = MasteryBonusCatalog.EvaluateAll(None, target);

            CollectionAssert.Contains(triggered, MasteryBonusType.Convergence);
        }

        [Test]
        public void EvaluateAll_ThreeSelfPositiveBuffs_TriggersEnlightened()
        {
            var self = new List<StatusEffectType> { StatusEffectType.Regenerate, StatusEffectType.Fortify, StatusEffectType.Haste };

            var triggered = MasteryBonusCatalog.EvaluateAll(self, None);

            CollectionAssert.Contains(triggered, MasteryBonusType.Enlightened);
        }

        [Test]
        public void EvaluateAll_MultipleQualifyingSets_StacksMultipleBonuses()
        {
            var target = new List<StatusEffectType> { StatusEffectType.Bleed, StatusEffectType.Burn, StatusEffectType.Wither, StatusEffectType.Stun };

            var triggered = MasteryBonusCatalog.EvaluateAll(None, target);

            CollectionAssert.Contains(triggered, MasteryBonusType.Hemorrhage);
            CollectionAssert.Contains(triggered, MasteryBonusType.Pressure);
        }

        [Test]
        public void EvaluateAll_NoQualifyingStatuses_ReturnsEmpty()
        {
            var triggered = MasteryBonusCatalog.EvaluateAll(None, new List<StatusEffectType> { StatusEffectType.Slow });

            CollectionAssert.IsEmpty(triggered);
        }
    }
}
