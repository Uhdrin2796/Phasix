using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers SkillLoadoutSystem's equip/unequip/swap rules (2026-08 session, see DECISIONS.md -> [UI]).</summary>
    public class SkillLoadoutSystemTests
    {
        private static PhasixRuntimeData MakeRuntime()
        {
            var runtime = new PhasixRuntimeData("node-guid-1");
            runtime.learnedSkillGuids.Add("skill-a");
            runtime.learnedSkillGuids.Add("skill-b");
            runtime.learnedSkillGuids.Add("skill-c");
            return runtime;
        }

        [Test]
        public void TryEquip_LearnedAndUnderCap_Equips()
        {
            var runtime = MakeRuntime();

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-a", evolutionTier: 1);

            Assert.IsTrue(result);
            CollectionAssert.Contains(runtime.equippedSkillGuids, "skill-a");
        }

        [Test]
        public void TryEquip_AtTierCap_Fails()
        {
            var runtime = MakeRuntime();
            runtime.learnedSkillGuids.Add("skill-d");
            runtime.learnedSkillGuids.Add("skill-e");
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", evolutionTier: 1);
            SkillLoadoutSystem.TryEquip(runtime, "skill-b", evolutionTier: 1);
            SkillLoadoutSystem.TryEquip(runtime, "skill-c", evolutionTier: 1);
            SkillLoadoutSystem.TryEquip(runtime, "skill-d", evolutionTier: 1); // Tier 1 cap is 4 (SkillSlotCapacity)

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-e", evolutionTier: 1);

            Assert.IsFalse(result);
            Assert.AreEqual(4, runtime.equippedSkillGuids.Count);
            CollectionAssert.DoesNotContain(runtime.equippedSkillGuids, "skill-e");
        }

        [Test]
        public void TryEquip_NotLearned_IsNoOp()
        {
            var runtime = MakeRuntime();

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-unknown", evolutionTier: 5);

            Assert.IsFalse(result);
            Assert.AreEqual(0, runtime.equippedSkillGuids.Count);
        }

        [Test]
        public void TryEquip_AlreadyEquipped_IsNoOp()
        {
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", evolutionTier: 5);

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-a", evolutionTier: 5);

            Assert.IsFalse(result);
            Assert.AreEqual(1, runtime.equippedSkillGuids.Count);
        }

        [Test]
        public void Unequip_RemovesFromEquipped_ButNotLearned()
        {
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", evolutionTier: 5);

            SkillLoadoutSystem.Unequip(runtime, "skill-a");

            CollectionAssert.DoesNotContain(runtime.equippedSkillGuids, "skill-a");
            CollectionAssert.Contains(runtime.learnedSkillGuids, "skill-a", "Unequip must not touch learnedSkillGuids — it never shrinks.");
        }

        [Test]
        public void SwapEquipped_PreservesCount_AndSwapsPositions()
        {
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", evolutionTier: 5);
            SkillLoadoutSystem.TryEquip(runtime, "skill-b", evolutionTier: 5);

            SkillLoadoutSystem.SwapEquipped(runtime, 0, 1);

            Assert.AreEqual(2, runtime.equippedSkillGuids.Count);
            Assert.AreEqual("skill-b", runtime.equippedSkillGuids[0]);
            Assert.AreEqual("skill-a", runtime.equippedSkillGuids[1]);
        }

        [Test]
        public void SwapEquipped_OutOfRangeIndex_IsNoOp()
        {
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", evolutionTier: 5);

            SkillLoadoutSystem.SwapEquipped(runtime, 0, 5);

            Assert.AreEqual(1, runtime.equippedSkillGuids.Count);
            Assert.AreEqual("skill-a", runtime.equippedSkillGuids[0]);
        }

        [Test]
        public void TryEquipAt_EmptySlot_Equips()
        {
            var runtime = MakeRuntime();

            bool result = SkillLoadoutSystem.TryEquipAt(runtime, "skill-a", slotIndex: 0, evolutionTier: 1);

            Assert.IsTrue(result);
            Assert.AreEqual("skill-a", runtime.equippedSkillGuids[0]);
        }

        [Test]
        public void TryEquipAt_OccupiedSlot_OverwritesAndOldSkillStaysLearned()
        {
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", evolutionTier: 5);

            bool result = SkillLoadoutSystem.TryEquipAt(runtime, "skill-b", slotIndex: 0, evolutionTier: 5);

            Assert.IsTrue(result);
            Assert.AreEqual(1, runtime.equippedSkillGuids.Count);
            Assert.AreEqual("skill-b", runtime.equippedSkillGuids[0]);
            CollectionAssert.DoesNotContain(runtime.equippedSkillGuids, "skill-a");
            CollectionAssert.Contains(runtime.learnedSkillGuids, "skill-a", "Overwritten skill must stay learned, only unequipped.");
        }

        [Test]
        public void TryEquipAt_OutsideTierRange_IsNoOp()
        {
            var runtime = MakeRuntime();

            bool result = SkillLoadoutSystem.TryEquipAt(runtime, "skill-a", slotIndex: 4, evolutionTier: 1); // Tier 1 cap is 4 (indices 0-3)

            Assert.IsFalse(result);
            Assert.AreEqual(0, runtime.equippedSkillGuids.Count);
        }
    }
}
