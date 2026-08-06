using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers SkillTreeUnlockSystem's bond-gated Type F/Type O unlock logic (CLAUDE.md: "Type F trees unlock at 20%. Type O trees unlock at 40%.").</summary>
    public class SkillTreeUnlockSystemTests
    {
        private static PhasixRuntimeData MakePhasix() => new PhasixRuntimeData("test-node-guid");

        [Test]
        public void HandleBondMilestoneReached_Familiar_UnlocksBondTree()
        {
            var phasix = MakePhasix();

            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Familiar);

            CollectionAssert.Contains(phasix.unlockedTreeTypes, SkillTreeType.Bond);
        }

        [Test]
        public void HandleBondMilestoneReached_Companion_UnlocksPersonalityTree()
        {
            var phasix = MakePhasix();

            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Companion);

            CollectionAssert.Contains(phasix.unlockedTreeTypes, SkillTreeType.Personality);
        }

        [Test]
        public void HandleBondMilestoneReached_OtherZones_UnlockNothing()
        {
            var phasix = MakePhasix();

            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Partner);
            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Bonded);
            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Complete);

            CollectionAssert.IsEmpty(phasix.unlockedTreeTypes);
        }

        [Test]
        public void HandleBondMilestoneReached_CalledTwiceForSameZone_DoesNotDuplicate()
        {
            var phasix = MakePhasix();

            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Familiar);
            SkillTreeUnlockSystem.HandleBondMilestoneReached(phasix, BondZone.Familiar);

            Assert.AreEqual(1, phasix.unlockedTreeTypes.Count, "unlockedTreeTypes must never shrink, but it also must never duplicate an entry.");
        }
    }
}
