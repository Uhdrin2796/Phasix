using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers BattleParticipant.SpendAura/RestoreAura (2026-08-05 — see DECISIONS.md -> [Combat]: attack Aura cost, perfect-defense Aura restore), Heal/ApplyRegen/TickRegen (2026-08-06 — "H"/"R" move options), and status/combo-history tracking (2026-08 session — Combo/Status/Chain/Mastery wiring).</summary>
    public class BattleParticipantTests
    {
        private static BattleParticipant MakeParticipant(int aura, int vitality = 20)
        {
            var phasix = new PhasixRuntimeData("test-node-guid") { baseStats = new StatBlock { Vitality = vitality, Aura = aura } };
            return new BattleParticipant(phasix, isPlayerSide: true);
        }

        [Test]
        public void SpendAura_ReducesCurrentAura()
        {
            var participant = MakeParticipant(aura: 10);

            participant.SpendAura(3);

            Assert.AreEqual(7, participant.CurrentAura);
        }

        [Test]
        public void SpendAura_ClampsAtZero_NeverGoesNegative()
        {
            var participant = MakeParticipant(aura: 2);

            participant.SpendAura(999);

            Assert.AreEqual(0, participant.CurrentAura);
        }

        [Test]
        public void SpendAura_ZeroOrNegativeAmount_IsIgnored()
        {
            var participant = MakeParticipant(aura: 10);

            participant.SpendAura(0);
            participant.SpendAura(-5);

            Assert.AreEqual(10, participant.CurrentAura);
        }

        [Test]
        public void RestoreAura_IncreasesCurrentAura()
        {
            var participant = MakeParticipant(aura: 10);
            participant.SpendAura(6);

            participant.RestoreAura(2);

            Assert.AreEqual(6, participant.CurrentAura);
        }

        [Test]
        public void RestoreAura_ClampsAtMaxAura_NeverExceedsIt()
        {
            var participant = MakeParticipant(aura: 10);

            participant.RestoreAura(999);

            Assert.AreEqual(participant.MaxAura, participant.CurrentAura);
        }

        [Test]
        public void RestoreAura_ZeroOrNegativeAmount_IsIgnored()
        {
            var participant = MakeParticipant(aura: 10);
            participant.SpendAura(4);

            participant.RestoreAura(0);
            participant.RestoreAura(-5);

            Assert.AreEqual(6, participant.CurrentAura);
        }

        [Test]
        public void Heal_IncreasesCurrentHP()
        {
            var participant = MakeParticipant(aura: 10, vitality: 20);
            participant.ApplyDamage(10);

            participant.Heal(4);

            Assert.AreEqual(14, participant.CurrentHP);
        }

        [Test]
        public void Heal_ClampsAtMaxHP_NeverExceedsIt()
        {
            var participant = MakeParticipant(aura: 10, vitality: 20);
            participant.ApplyDamage(2);

            participant.Heal(999);

            Assert.AreEqual(participant.MaxHP, participant.CurrentHP);
        }

        [Test]
        public void Heal_ZeroOrNegativeAmount_IsIgnored()
        {
            var participant = MakeParticipant(aura: 10, vitality: 20);
            participant.ApplyDamage(10);

            participant.Heal(0);
            participant.Heal(-5);

            Assert.AreEqual(10, participant.CurrentHP);
        }

        [Test]
        public void ApplyRegen_SetsTurnsRemainingAndHealPerTurn()
        {
            var participant = MakeParticipant(aura: 10);

            participant.ApplyRegen(healPerTurn: 2, turns: 4);

            Assert.AreEqual(4, participant.RegenTurnsRemaining);
            Assert.AreEqual(2, participant.RegenHealPerTurn);
        }

        [Test]
        public void ApplyRegen_ZeroOrNegativeArguments_IsIgnored()
        {
            var participant = MakeParticipant(aura: 10);

            participant.ApplyRegen(healPerTurn: 0, turns: 4);
            participant.ApplyRegen(healPerTurn: 2, turns: 0);
            participant.ApplyRegen(healPerTurn: -1, turns: -1);

            Assert.AreEqual(0, participant.RegenTurnsRemaining);
            Assert.AreEqual(0, participant.RegenHealPerTurn);
        }

        [Test]
        public void TickRegen_HealsAndCountsDown()
        {
            var participant = MakeParticipant(aura: 10, vitality: 20);
            participant.ApplyDamage(10); // 10/20 HP, room to heal
            participant.ApplyRegen(healPerTurn: 2, turns: 4);

            int healed = participant.TickRegen();

            Assert.AreEqual(2, healed);
            Assert.AreEqual(12, participant.CurrentHP);
            Assert.AreEqual(3, participant.RegenTurnsRemaining);
        }

        [Test]
        public void TickRegen_ReturnsZero_WhenNoActiveRegen()
        {
            var participant = MakeParticipant(aura: 10, vitality: 20);
            participant.ApplyDamage(10);

            int healed = participant.TickRegen();

            Assert.AreEqual(0, healed);
            Assert.AreEqual(10, participant.CurrentHP);
        }

        [Test]
        public void TickRegen_StopsHealing_OnceDurationExpires()
        {
            var participant = MakeParticipant(aura: 10, vitality: 20);
            participant.ApplyDamage(20); // fully damaged, plenty of room across all ticks
            participant.ApplyRegen(healPerTurn: 2, turns: 4);

            participant.TickRegen();
            participant.TickRegen();
            participant.TickRegen();
            participant.TickRegen(); // 4th tick — countdown reaches 0
            int fifthTickHealed = participant.TickRegen(); // no longer active

            Assert.AreEqual(0, participant.RegenTurnsRemaining);
            Assert.AreEqual(0, participant.RegenHealPerTurn);
            Assert.AreEqual(0, fifthTickHealed);
            Assert.AreEqual(8, participant.CurrentHP); // 4 ticks x 2 HP
        }

        [Test]
        public void TickRegen_HealAmountRespectsMaxHPClamp()
        {
            var participant = MakeParticipant(aura: 10, vitality: 20);
            participant.ApplyDamage(1); // only 1 HP of room
            participant.ApplyRegen(healPerTurn: 2, turns: 4);

            int healed = participant.TickRegen();

            Assert.AreEqual(1, healed); // clamped, not the full healPerTurn
            Assert.AreEqual(participant.MaxHP, participant.CurrentHP);
        }

        [Test]
        public void ApplyStatus_AddsNewEntry()
        {
            var participant = MakeParticipant(aura: 10);

            participant.ApplyStatus(StatusEffectType.Burn, 4);

            Assert.AreEqual(1, participant.ActiveStatuses.Count);
            Assert.AreEqual(StatusEffectType.Burn, participant.ActiveStatuses[0].Type);
            Assert.AreEqual(4, participant.ActiveStatuses[0].TurnsRemaining);
        }

        [Test]
        public void ApplyStatus_ReapplyingSameType_OverwritesRatherThanStacks()
        {
            var participant = MakeParticipant(aura: 10);
            participant.ApplyStatus(StatusEffectType.Burn, 4);

            participant.ApplyStatus(StatusEffectType.Burn, 6);

            Assert.AreEqual(1, participant.ActiveStatuses.Count, "Re-casting must refresh the countdown, not add a second timer.");
            Assert.AreEqual(6, participant.ActiveStatuses[0].TurnsRemaining);
        }

        [Test]
        public void ApplyStatus_ZeroOrNegativeDuration_IsIgnored()
        {
            var participant = MakeParticipant(aura: 10);

            participant.ApplyStatus(StatusEffectType.Burn, 0);
            participant.ApplyStatus(StatusEffectType.Burn, -1);

            Assert.AreEqual(0, participant.ActiveStatuses.Count);
        }

        [Test]
        public void TickStatuses_DecrementsAndRemovesAtZero_ReturningExpiredTypes()
        {
            var participant = MakeParticipant(aura: 10);
            participant.ApplyStatus(StatusEffectType.Burn, 1);
            participant.ApplyStatus(StatusEffectType.Regenerate, 3);

            List<StatusEffectType> expired = participant.TickStatuses();

            CollectionAssert.AreEquivalent(new[] { StatusEffectType.Burn }, expired);
            Assert.AreEqual(1, participant.ActiveStatuses.Count);
            Assert.AreEqual(StatusEffectType.Regenerate, participant.ActiveStatuses[0].Type);
            Assert.AreEqual(2, participant.ActiveStatuses[0].TurnsRemaining);
        }

        [Test]
        public void ActiveStatusTypes_ReflectsOnlyCurrentlyActiveEntries()
        {
            var participant = MakeParticipant(aura: 10);
            participant.ApplyStatus(StatusEffectType.Burn, 1);
            participant.ApplyStatus(StatusEffectType.Regenerate, 3);
            participant.TickStatuses(); // Burn expires

            CollectionAssert.AreEquivalent(new[] { StatusEffectType.Regenerate }, participant.ActiveStatusTypes);
        }

        [Test]
        public void RecordSkillTreeUse_AppendsAndTrimsToLastFour()
        {
            var participant = MakeParticipant(aura: 10);

            participant.RecordSkillTreeUse(SkillTreeType.Utility);
            participant.RecordSkillTreeUse(SkillTreeType.Aura);
            participant.RecordSkillTreeUse(SkillTreeType.Passive);
            participant.RecordSkillTreeUse(SkillTreeType.Synergy);
            participant.RecordSkillTreeUse(SkillTreeType.Reaction);

            Assert.AreEqual(4, participant.RecentSkillTrees.Count);
            CollectionAssert.AreEqual(
                new[] { SkillTreeType.Aura, SkillTreeType.Passive, SkillTreeType.Synergy, SkillTreeType.Reaction },
                participant.RecentSkillTrees);
        }

        [Test]
        public void RecordSkillTreeUse_FourDistinctTrees_DetectsQuadViaRealComboEngine()
        {
            var participant = MakeParticipant(aura: 10);

            participant.RecordSkillTreeUse(SkillTreeType.Utility);
            participant.RecordSkillTreeUse(SkillTreeType.Aura);
            participant.RecordSkillTreeUse(SkillTreeType.Passive);
            participant.RecordSkillTreeUse(SkillTreeType.Synergy);

            Assert.AreEqual(ComboTier.Quad, ComboEngine.DetectCombo(participant.RecentSkillTrees));
        }

        [Test]
        public void RecordTimedInputPerfect_AppendsAndTrimsToLastFour()
        {
            var participant = MakeParticipant(aura: 10);

            participant.RecordTimedInputPerfect(true);
            participant.RecordTimedInputPerfect(true);
            participant.RecordTimedInputPerfect(false);
            participant.RecordTimedInputPerfect(true);
            participant.RecordTimedInputPerfect(true);

            Assert.AreEqual(4, participant.RecentTimedInputPerfects.Count);
            CollectionAssert.AreEqual(new[] { true, false, true, true }, participant.RecentTimedInputPerfects);
        }

        [Test]
        public void ActiveComboRules_AlwaysIncludesCrossTreeSequence_ByDefault()
        {
            var participant = MakeParticipant(aura: 10);

            CollectionAssert.Contains(participant.ActiveComboRules, ComboRuleType.CrossTreeSequence);
            Assert.AreEqual(1, participant.ActiveComboRules.Count);
        }

        [Test]
        public void RefreshActiveComboRules_AddsGrantedRule_FromEquippedSkill()
        {
            var phasix = new PhasixRuntimeData("test-node-guid") { baseStats = new StatBlock { Vitality = 20, Aura = 10 } };
            phasix.equippedSkillGuids.Add("guid-mirror-1");
            var participant = new BattleParticipant(phasix, isPlayerSide: true);

            var skill = ScriptableObject.CreateInstance<SkillData>();
            SetPrivateField(skill, "_grantsComboRule", ComboRuleType.RepeatSameSkill);
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            SetPrivateField(database, "_allSkills", new List<SkillData> { skill });
            SetPrivateField(database, "_guids", new List<string> { "guid-mirror-1" });

            participant.RefreshActiveComboRules(database);

            CollectionAssert.Contains(participant.ActiveComboRules, ComboRuleType.CrossTreeSequence);
            CollectionAssert.Contains(participant.ActiveComboRules, ComboRuleType.RepeatSameSkill);

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void RefreshActiveComboRules_NoGrantingSkillEquipped_OnlyHasCrossTreeSequence()
        {
            var phasix = new PhasixRuntimeData("test-node-guid") { baseStats = new StatBlock { Vitality = 20, Aura = 10 } };
            var participant = new BattleParticipant(phasix, isPlayerSide: true);
            var database = ScriptableObject.CreateInstance<SkillDatabase>();

            participant.RefreshActiveComboRules(database);

            Assert.AreEqual(1, participant.ActiveComboRules.Count);
            CollectionAssert.Contains(participant.ActiveComboRules, ComboRuleType.CrossTreeSequence);

            Object.DestroyImmediate(database);
        }

        [Test]
        public void LaneIndex_DefaultsToLaneMovementSystemDefaultStartingLane()
        {
            var participant = MakeParticipant(aura: 10);

            Assert.AreEqual(LaneMovementSystem.DefaultStartingLane, participant.LaneIndex);
        }

        [Test]
        public void PositionIndex_DefaultsToLaneMovementSystemDefaultStartingPosition()
        {
            var participant = MakeParticipant(aura: 10);

            Assert.AreEqual(LaneMovementSystem.DefaultStartingPosition, participant.PositionIndex);
        }

        [Test]
        public void Constructor_SeedsLaneAndPositionFromRuntimeDataPreferredValues()
        {
            var phasix = new PhasixRuntimeData("test-node-guid")
            {
                baseStats = new StatBlock { Vitality = 20, Aura = 10 },
                preferredLaneIndex = 2,
                preferredPositionIndex = 5,
            };

            var participant = new BattleParticipant(phasix, isPlayerSide: true);

            Assert.AreEqual(2, participant.LaneIndex);
            Assert.AreEqual(5, participant.PositionIndex);
        }

        [Test]
        public void Constructor_ClampsOutOfRangePreferredValues()
        {
            var phasix = new PhasixRuntimeData("test-node-guid")
            {
                baseStats = new StatBlock { Vitality = 20, Aura = 10 },
                preferredLaneIndex = 99,
                preferredPositionIndex = -4,
            };

            var participant = new BattleParticipant(phasix, isPlayerSide: true);

            Assert.AreEqual(LaneMovementSystem.ClampLane(99), participant.LaneIndex);
            Assert.AreEqual(LaneMovementSystem.ClampPosition(-4), participant.PositionIndex);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
