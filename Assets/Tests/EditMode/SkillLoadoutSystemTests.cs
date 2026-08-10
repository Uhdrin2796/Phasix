using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers SkillLoadoutSystem's equip/unequip/swap rules (2026-08 session, see DECISIONS.md ->
    /// [UI]). Most tests use SkillTreeType.Standard for the new required treeType argument (2026-08
    /// follow-up #8, skill web view) since Standard is always exempt from the unlocked-tree gate —
    /// keeps these tests exercising exactly what they always did (cap/learned/swap rules) without
    /// incidentally depending on unlockedTreeTypes too. The gate itself is covered separately below.
    /// </summary>
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

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 1);

            Assert.IsTrue(result);
            CollectionAssert.Contains(runtime.equippedSkillGuids, "skill-a");
        }

        [Test]
        public void TryEquip_AtTierCap_Fails()
        {
            var runtime = MakeRuntime();
            runtime.learnedSkillGuids.Add("skill-d");
            runtime.learnedSkillGuids.Add("skill-e");
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 1);
            SkillLoadoutSystem.TryEquip(runtime, "skill-b", SkillTreeType.Standard, evolutionTier: 1);
            SkillLoadoutSystem.TryEquip(runtime, "skill-c", SkillTreeType.Standard, evolutionTier: 1);
            SkillLoadoutSystem.TryEquip(runtime, "skill-d", SkillTreeType.Standard, evolutionTier: 1); // Tier 1 cap is 4 (SkillSlotCapacity)

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-e", SkillTreeType.Standard, evolutionTier: 1);

            Assert.IsFalse(result);
            Assert.AreEqual(4, runtime.equippedSkillGuids.Count);
            CollectionAssert.DoesNotContain(runtime.equippedSkillGuids, "skill-e");
        }

        [Test]
        public void TryEquip_NotLearned_IsNoOp()
        {
            var runtime = MakeRuntime();

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-unknown", SkillTreeType.Standard, evolutionTier: 5);

            Assert.IsFalse(result);
            Assert.AreEqual(0, runtime.equippedSkillGuids.Count);
        }

        [Test]
        public void TryEquip_AlreadyEquipped_IsNoOp()
        {
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 5);

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 5);

            Assert.IsFalse(result);
            Assert.AreEqual(1, runtime.equippedSkillGuids.Count);
        }

        [Test]
        public void Unequip_RemovesFromEquipped_ButNotLearned()
        {
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 5);

            SkillLoadoutSystem.Unequip(runtime, "skill-a");

            CollectionAssert.DoesNotContain(runtime.equippedSkillGuids, "skill-a");
            CollectionAssert.Contains(runtime.learnedSkillGuids, "skill-a", "Unequip must not touch learnedSkillGuids — it never shrinks.");
        }

        [Test]
        public void SwapEquipped_PreservesCount_AndSwapsPositions()
        {
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 5);
            SkillLoadoutSystem.TryEquip(runtime, "skill-b", SkillTreeType.Standard, evolutionTier: 5);

            SkillLoadoutSystem.SwapEquipped(runtime, 0, 1);

            Assert.AreEqual(2, runtime.equippedSkillGuids.Count);
            Assert.AreEqual("skill-b", runtime.equippedSkillGuids[0]);
            Assert.AreEqual("skill-a", runtime.equippedSkillGuids[1]);
        }

        [Test]
        public void SwapEquipped_BeyondCurrentLength_ExtendsAndMovesToExactPosition()
        {
            // 2026-08-09 follow-up — user: "when i add skills from the tree to the wheel it just
            // adds it to the next open spot instead of where im dragging and dropping it to."
            // SwapEquipped with a target beyond the list's current length must now land the moved
            // skill EXACTLY at that index (extending with empty gap entries), not no-op.
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 5);

            SkillLoadoutSystem.SwapEquipped(runtime, 0, 5);

            Assert.AreEqual(6, runtime.equippedSkillGuids.Count);
            Assert.AreEqual(string.Empty, runtime.equippedSkillGuids[0]);
            Assert.AreEqual("skill-a", runtime.equippedSkillGuids[5]);
        }

        [Test]
        public void SwapEquipped_NegativeIndex_IsNoOp()
        {
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 5);

            SkillLoadoutSystem.SwapEquipped(runtime, 0, -1);

            Assert.AreEqual(1, runtime.equippedSkillGuids.Count);
            Assert.AreEqual("skill-a", runtime.equippedSkillGuids[0]);
        }

        [Test]
        public void TryEquipAt_EmptySlot_Equips()
        {
            var runtime = MakeRuntime();

            bool result = SkillLoadoutSystem.TryEquipAt(runtime, "skill-a", SkillTreeType.Standard, slotIndex: 0, evolutionTier: 1);

            Assert.IsTrue(result);
            Assert.AreEqual("skill-a", runtime.equippedSkillGuids[0]);
        }

        [Test]
        public void TryEquipAt_OccupiedSlot_OverwritesAndOldSkillStaysLearned()
        {
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 5);

            bool result = SkillLoadoutSystem.TryEquipAt(runtime, "skill-b", SkillTreeType.Standard, slotIndex: 0, evolutionTier: 5);

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

            bool result = SkillLoadoutSystem.TryEquipAt(runtime, "skill-a", SkillTreeType.Standard, slotIndex: 4, evolutionTier: 1); // Tier 1 cap is 4 (indices 0-3)

            Assert.IsFalse(result);
            Assert.AreEqual(0, runtime.equippedSkillGuids.Count);
        }

        // --- 2026-08 follow-up #8: unlockedTreeTypes equip gate (skill web view) ---

        [Test]
        public void TryEquip_TreeNotUnlocked_IsNoOp()
        {
            var runtime = MakeRuntime();
            // unlockedTreeTypes is empty — Utility is not unlocked.

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Utility, evolutionTier: 5);

            Assert.IsFalse(result);
            Assert.AreEqual(0, runtime.equippedSkillGuids.Count);
        }

        [Test]
        public void TryEquip_TreeUnlocked_Equips()
        {
            var runtime = MakeRuntime();
            runtime.unlockedTreeTypes.Add(SkillTreeType.Utility);

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Utility, evolutionTier: 5);

            Assert.IsTrue(result);
            CollectionAssert.Contains(runtime.equippedSkillGuids, "skill-a");
        }

        [Test]
        public void TryEquip_StandardTree_AlwaysAllowed_EvenWithNoUnlockedTrees()
        {
            var runtime = MakeRuntime();
            // unlockedTreeTypes is empty — Standard must still be equippable, it isn't gated.

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 5);

            Assert.IsTrue(result);
        }

        [Test]
        public void TryEquipAt_TreeNotUnlocked_IsNoOp()
        {
            var runtime = MakeRuntime();

            bool result = SkillLoadoutSystem.TryEquipAt(runtime, "skill-a", SkillTreeType.Utility, slotIndex: 0, evolutionTier: 5);

            Assert.IsFalse(result);
            Assert.AreEqual(0, runtime.equippedSkillGuids.Count);
        }

        [Test]
        public void TryEquipAt_TreeUnlocked_Equips()
        {
            var runtime = MakeRuntime();
            runtime.unlockedTreeTypes.Add(SkillTreeType.Utility);

            bool result = SkillLoadoutSystem.TryEquipAt(runtime, "skill-a", SkillTreeType.Utility, slotIndex: 0, evolutionTier: 5);

            Assert.IsTrue(result);
            Assert.AreEqual("skill-a", runtime.equippedSkillGuids[0]);
        }

        [Test]
        public void TryEquip_DebugTierOverrideUnlocksTree_Equips()
        {
            // Reproduces the exact gap found during plan review: a tree only unlocked via the
            // debug tier override must be equippable for real, not just displayed as unlocked.
            var runtime = MakeRuntime();
            var species = ScriptableObject.CreateInstance<PhasixData>();
            SetPrivateField(species, "_availableTreeTypes", new System.Collections.Generic.List<SkillTreeType> { SkillTreeType.Utility });
            runtime.speciesData = species;
            runtime.DebugTierOverride = 5; // GetTreeCount(5) == 7, so Utility (the only available tree) counts as unlocked

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Utility, evolutionTier: 5);

            Assert.IsTrue(result);
            Object.DestroyImmediate(species);
        }

        // --- 2026-08-09 follow-up #9: positional (sparse) equip slots ---

        [Test]
        public void TryEquipAt_EmptySlotBeyondCurrentLength_LandsExactlyThere_NotAppended()
        {
            // The exact bug reported: dropping onto empty physical slot 5 must land at index 5,
            // not get compacted onto the next open spot (index 1) after the one already-equipped skill.
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 5);

            bool result = SkillLoadoutSystem.TryEquipAt(runtime, "skill-b", SkillTreeType.Standard, slotIndex: 5, evolutionTier: 5);

            Assert.IsTrue(result);
            Assert.AreEqual("skill-a", runtime.equippedSkillGuids[0]);
            Assert.AreEqual("skill-b", runtime.equippedSkillGuids[5]);
            Assert.AreEqual(string.Empty, runtime.equippedSkillGuids[1], "Slots between the two placed skills must stay empty, not get shifted into.");
        }

        [Test]
        public void Unequip_ClearsExactSlot_DoesNotShiftOtherEquippedSkills()
        {
            var runtime = MakeRuntime();
            runtime.learnedSkillGuids.Add("skill-d");
            SkillLoadoutSystem.TryEquipAt(runtime, "skill-a", SkillTreeType.Standard, slotIndex: 0, evolutionTier: 5);
            SkillLoadoutSystem.TryEquipAt(runtime, "skill-b", SkillTreeType.Standard, slotIndex: 1, evolutionTier: 5);
            SkillLoadoutSystem.TryEquipAt(runtime, "skill-c", SkillTreeType.Standard, slotIndex: 2, evolutionTier: 5);

            SkillLoadoutSystem.Unequip(runtime, "skill-b");

            Assert.AreEqual("skill-a", runtime.equippedSkillGuids[0], "Unaffected earlier slot must not move.");
            Assert.AreEqual(string.Empty, runtime.equippedSkillGuids[1], "Unequipped slot must clear in place.");
            Assert.AreEqual("skill-c", runtime.equippedSkillGuids[2], "Later slot must NOT shift down into the cleared gap — this was the pre-fix bug.");
        }

        [Test]
        public void TryEquip_CapCheckCountsRealSkillsNotListLength_SparseGapsDontFalselyBlock()
        {
            // With sparse gaps, equippedSkillGuids.Count can exceed the real equipped count —
            // the cap check must use the real count (CountEquipped), not list length, or a single
            // skill placed at a high index would wrongly appear to fill the whole cap.
            var runtime = MakeRuntime();
            SkillLoadoutSystem.TryEquipAt(runtime, "skill-a", SkillTreeType.Standard, slotIndex: 11, evolutionTier: 5); // Tier 5 cap is 12 (indices 0-11)
            Assert.AreEqual(12, runtime.equippedSkillGuids.Count, "List extends to cover the placed index.");

            bool result = SkillLoadoutSystem.TryEquip(runtime, "skill-b", SkillTreeType.Standard, evolutionTier: 5);

            Assert.IsTrue(result, "Only 1 of 12 slots is really occupied — a second equip must succeed.");
            Assert.AreEqual("skill-b", runtime.equippedSkillGuids[0], "TryEquip fills the first empty gap.");
        }

        [Test]
        public void TryEquipAt_TargetEmptyButRealCapReached_Fails()
        {
            var runtime = MakeRuntime();
            runtime.learnedSkillGuids.Add("skill-d");
            SkillLoadoutSystem.TryEquip(runtime, "skill-a", SkillTreeType.Standard, evolutionTier: 1);
            SkillLoadoutSystem.TryEquip(runtime, "skill-b", SkillTreeType.Standard, evolutionTier: 1);
            SkillLoadoutSystem.TryEquip(runtime, "skill-c", SkillTreeType.Standard, evolutionTier: 1);
            SkillLoadoutSystem.TryEquip(runtime, "skill-d", SkillTreeType.Standard, evolutionTier: 1); // Tier 1 cap is 4, now full

            bool result = SkillLoadoutSystem.TryEquipAt(runtime, "skill-a", SkillTreeType.Standard, slotIndex: 3, evolutionTier: 1);

            // Already equipped elsewhere, and even ignoring that, the cap is genuinely full.
            Assert.IsFalse(result);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
