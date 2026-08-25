using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers EnemyAI's heuristic target/skill selection (2026-08-10 — Phase 3 close-out pass).
    /// Skill-selection tests use only the 4 non-Capture built-in moves (Attack/Charge/Heal/Regen)
    /// to exercise ChooseSkill's bucketing switch without needing SkillTreeCatalog/
    /// PlaceholderSkillResolver's locked tree data — that data is exercised elsewhere
    /// (PlaceholderSkillResolverTests), and ChooseSkill's BuiltInMoveType branch is independent of
    /// it (see the switch in EnemyAI.ChooseSkill).
    /// </summary>
    public class EnemyAITests
    {
        private static BattleParticipant MakeParticipant(int vitality = 20, int currentHpFraction100 = 100)
        {
            var phasix = new PhasixRuntimeData("test-node-guid") { baseStats = new StatBlock { Vitality = vitality, Aura = 10 } };
            var participant = new BattleParticipant(phasix, isPlayerSide: false);
            int damage = participant.MaxHP - Mathf.RoundToInt(participant.MaxHP * currentHpFraction100 / 100f);
            participant.ApplyDamage(damage);
            return participant;
        }

        private static SkillData MakeBuiltInSkill(BuiltInMoveType move, string name = "Test")
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            SetPrivateField(skill, "_builtInMove", move);
            SetPrivateField(skill, "_skillName", name);
            return skill;
        }

        private static SkillDatabase MakeDatabase(params (SkillData skill, string guid)[] entries)
        {
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            var skills = new List<SkillData>();
            var guids = new List<string>();
            foreach ((SkillData skill, string guid) in entries)
            {
                skills.Add(skill);
                guids.Add(guid);
            }
            SetPrivateField(database, "_allSkills", skills);
            SetPrivateField(database, "_guids", guids);
            return database;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Expected private field {fieldName} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        // --- ComputeTargetWeight ---

        [Test]
        public void ComputeTargetWeight_FullHp_NoTypeChart_EqualsFlatFloor()
        {
            BattleParticipant attacker = MakeParticipant();
            BattleParticipant candidate = MakeParticipant(currentHpFraction100: 100);

            float weight = EnemyAI.ComputeTargetWeight(attacker, candidate, null);

            Assert.AreEqual(1f, weight, 0.001f);
        }

        [Test]
        public void ComputeTargetWeight_LowerHpCandidate_ScoresHigherThanFullHp()
        {
            BattleParticipant attacker = MakeParticipant();
            BattleParticipant fullHp = MakeParticipant(currentHpFraction100: 100);
            BattleParticipant damaged = MakeParticipant(currentHpFraction100: 20);

            float fullWeight = EnemyAI.ComputeTargetWeight(attacker, fullHp, null);
            float damagedWeight = EnemyAI.ComputeTargetWeight(attacker, damaged, null);

            Assert.Greater(damagedWeight, fullWeight);
        }

        [Test]
        public void ComputeTargetWeight_ResistedMatchup_StaysPositive()
        {
            var chart = ScriptableObject.CreateInstance<PrimalTypeChart>();
            var attackerSpecies = ScriptableObject.CreateInstance<PhasixData>();
            SetPrivateField(attackerSpecies, "_primalType", PrimalType.Fire);
            var candidateSpecies = ScriptableObject.CreateInstance<PhasixData>();
            SetPrivateField(candidateSpecies, "_primalType", PrimalType.Water); // Fire resisted by Water (0.5x, GDD-locked)

            BattleParticipant attacker = MakeParticipant();
            attacker.RuntimeData.speciesData = attackerSpecies;
            BattleParticipant candidate = MakeParticipant(currentHpFraction100: 100);
            candidate.RuntimeData.speciesData = candidateSpecies;

            float weight = EnemyAI.ComputeTargetWeight(attacker, candidate, chart);

            Assert.Greater(weight, 0f);

            Object.DestroyImmediate(chart);
            Object.DestroyImmediate(attackerSpecies);
            Object.DestroyImmediate(candidateSpecies);
        }

        // --- ChooseTarget ---

        [Test]
        public void ChooseTarget_EmptyList_ReturnsNull()
        {
            BattleParticipant attacker = MakeParticipant();
            Assert.IsNull(EnemyAI.ChooseTarget(attacker, new List<BattleParticipant>(), null));
        }

        [Test]
        public void ChooseTarget_SingleCandidate_ReturnsItDirectly()
        {
            BattleParticipant attacker = MakeParticipant();
            BattleParticipant only = MakeParticipant();

            BattleParticipant chosen = EnemyAI.ChooseTarget(attacker, new List<BattleParticipant> { only }, null);

            Assert.AreSame(only, chosen);
        }

        [Test]
        public void ChooseTarget_MultiCandidate_LowestHpPickedMoreOftenThanUniform()
        {
            BattleParticipant attacker = MakeParticipant();
            BattleParticipant lowHp = MakeParticipant(currentHpFraction100: 5);
            BattleParticipant fullHp = MakeParticipant(currentHpFraction100: 100);
            var candidates = new List<BattleParticipant> { lowHp, fullHp };

            int lowHpPicks = 0;
            const int trials = 200;
            for (int i = 0; i < trials; i++)
            {
                if (EnemyAI.ChooseTarget(attacker, candidates, null) == lowHp) lowHpPicks++;
            }

            // Uniform baseline would be ~50%; the low-HP weighting should push this well above it.
            Assert.Greater(lowHpPicks, trials * 0.6f);
        }

        // --- ChooseSkill ---

        [Test]
        public void ChooseSkill_NullDatabase_ReturnsNullWithDamageIntent()
        {
            BattleParticipant attacker = MakeParticipant();

            SkillData result = EnemyAI.ChooseSkill(attacker, null, out EnemyAI.EnemyMoveIntent intent);

            Assert.IsNull(result);
            Assert.AreEqual(EnemyAI.EnemyMoveIntent.Damage, intent);
        }

        [Test]
        public void ChooseSkill_CaptureEquipped_NeverReturnsCapture()
        {
            BattleParticipant attacker = MakeParticipant();
            SkillData capture = MakeBuiltInSkill(BuiltInMoveType.Capture, "Capture");
            SkillData attack = MakeBuiltInSkill(BuiltInMoveType.Attack, "Attack");
            attacker.RuntimeData.equippedSkillGuids.Add("guid-capture");
            attacker.RuntimeData.equippedSkillGuids.Add("guid-attack");
            SkillDatabase database = MakeDatabase((capture, "guid-capture"), (attack, "guid-attack"));

            for (int i = 0; i < 50; i++)
            {
                SkillData result = EnemyAI.ChooseSkill(attacker, database, out _);
                Assert.AreNotEqual(BuiltInMoveType.Capture, result?.BuiltInMove ?? BuiltInMoveType.None);
            }

            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void ChooseSkill_OnlyCaptureEquipped_ReturnsNullDamageFallback()
        {
            BattleParticipant attacker = MakeParticipant();
            SkillData capture = MakeBuiltInSkill(BuiltInMoveType.Capture, "Capture");
            attacker.RuntimeData.equippedSkillGuids.Add("guid-capture");
            SkillDatabase database = MakeDatabase((capture, "guid-capture"));

            SkillData result = EnemyAI.ChooseSkill(attacker, database, out EnemyAI.EnemyMoveIntent intent);

            Assert.IsNull(result);
            Assert.AreEqual(EnemyAI.EnemyMoveIntent.Damage, intent);

            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void ChooseSkill_FullHpWithAttackAndHealEquipped_AlwaysPrefersDamage()
        {
            BattleParticipant attacker = MakeParticipant(currentHpFraction100: 100);
            SkillData attack = MakeBuiltInSkill(BuiltInMoveType.Attack, "Attack");
            SkillData heal = MakeBuiltInSkill(BuiltInMoveType.Heal, "Heal");
            attacker.RuntimeData.equippedSkillGuids.Add("guid-attack");
            attacker.RuntimeData.equippedSkillGuids.Add("guid-heal");
            SkillDatabase database = MakeDatabase((attack, "guid-attack"), (heal, "guid-heal"));

            for (int i = 0; i < 50; i++)
            {
                SkillData result = EnemyAI.ChooseSkill(attacker, database, out EnemyAI.EnemyMoveIntent intent);
                Assert.AreEqual(EnemyAI.EnemyMoveIntent.Damage, intent);
                Assert.AreEqual(BuiltInMoveType.Attack, result.BuiltInMove);
            }

            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(heal);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void ChooseSkill_LowHpWithHealEquipped_SometimesChoosesSelfSupport()
        {
            BattleParticipant attacker = MakeParticipant(currentHpFraction100: 10); // below EnemySelfCareHpThreshold (0.35)
            SkillData attack = MakeBuiltInSkill(BuiltInMoveType.Attack, "Attack");
            SkillData heal = MakeBuiltInSkill(BuiltInMoveType.Heal, "Heal");
            attacker.RuntimeData.equippedSkillGuids.Add("guid-attack");
            attacker.RuntimeData.equippedSkillGuids.Add("guid-heal");
            SkillDatabase database = MakeDatabase((attack, "guid-attack"), (heal, "guid-heal"));

            int selfSupportPicks = 0;
            const int trials = 200;
            for (int i = 0; i < trials; i++)
            {
                EnemyAI.ChooseSkill(attacker, database, out EnemyAI.EnemyMoveIntent intent);
                if (intent == EnemyAI.EnemyMoveIntent.SelfSupport) selfSupportPicks++;
            }

            // EnemySelfCareChance is 0.5 — expect a meaningful fraction, not never/always.
            Assert.Greater(selfSupportPicks, 0);
            Assert.Less(selfSupportPicks, trials);

            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(heal);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void ChooseSkill_OnlySelfSupportEquipped_ReturnsItEvenAtFullHp()
        {
            BattleParticipant attacker = MakeParticipant(currentHpFraction100: 100);
            SkillData charge = MakeBuiltInSkill(BuiltInMoveType.Charge, "Charge");
            attacker.RuntimeData.equippedSkillGuids.Add("guid-charge");
            SkillDatabase database = MakeDatabase((charge, "guid-charge"));

            SkillData result = EnemyAI.ChooseSkill(attacker, database, out EnemyAI.EnemyMoveIntent intent);

            Assert.AreEqual(EnemyAI.EnemyMoveIntent.SelfSupport, intent);
            Assert.AreEqual(BuiltInMoveType.Charge, result.BuiltInMove);

            Object.DestroyImmediate(charge);
            Object.DestroyImmediate(database);
        }

        // --- TryChooseDodgeStep (2026-08-21, Zone/Positional offense-direction follow-up) ---

        private static BattleParticipant MakeDefender(int instinct = 10, int lane = 4, int position = 3)
        {
            var phasix = new PhasixRuntimeData("test-defender") { baseStats = new StatBlock { Vitality = 20, Aura = 10, Instinct = instinct } };
            var participant = new BattleParticipant(phasix, isPlayerSide: false) { LaneIndex = lane, PositionIndex = position };
            return participant;
        }

        [Test]
        public void TryChooseDodgeStep_NullDefender_ReturnsFalse()
        {
            bool result = EnemyAI.TryChooseDodgeStep(null, new List<ZoneCell>(), new List<BattleParticipant>(),
                EnemyDifficultyTier.AlwaysDodges, out int laneDelta, out int positionDelta);

            Assert.IsFalse(result);
            Assert.AreEqual(0, laneDelta);
            Assert.AreEqual(0, positionDelta);
        }

        [Test]
        public void TryChooseDodgeStep_DeadDefender_ReturnsFalse()
        {
            BattleParticipant defender = MakeDefender();
            defender.ApplyDamage(defender.MaxHP);

            bool result = EnemyAI.TryChooseDodgeStep(defender, new List<ZoneCell>(), new List<BattleParticipant> { defender },
                EnemyDifficultyTier.AlwaysDodges, out _, out _);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryChooseDodgeStep_Rooted_NeverMoves()
        {
            BattleParticipant defender = MakeDefender();
            defender.ApplyStatus(StatusEffectType.Root, durationTurns: 2);
            var defendingSide = new List<BattleParticipant> { defender };
            var markedCells = new List<ZoneCell> { new ZoneCell(defender.LaneIndex, defender.PositionIndex) };

            // AlwaysDodges would guarantee a successful roll if the root check were missing —
            // asserting False here specifically proves Root is checked before the roll, not just
            // that no safe cell happened to exist.
            for (int i = 0; i < 20; i++)
            {
                bool result = EnemyAI.TryChooseDodgeStep(defender, markedCells, defendingSide,
                    EnemyDifficultyTier.AlwaysDodges, out _, out _);
                Assert.IsFalse(result);
            }
        }

        [Test]
        public void TryChooseDodgeStep_AlwaysDodgesTier_PicksFirstSafeOrthogonalCandidate()
        {
            BattleParticipant defender = MakeDefender(lane: 4, position: 3);
            var defendingSide = new List<BattleParticipant> { defender };
            var markedCells = new List<ZoneCell> { new ZoneCell(4, 3) }; // only the defender's own cell is marked

            bool result = EnemyAI.TryChooseDodgeStep(defender, markedCells, defendingSide,
                EnemyDifficultyTier.AlwaysDodges, out int laneDelta, out int positionDelta);

            Assert.IsTrue(result);
            // Fixed candidate order (lane+1, lane-1, position+1, position-1) — lane+1 is unmarked
            // and unoccupied, so it's picked first.
            Assert.AreEqual(1, laneDelta);
            Assert.AreEqual(0, positionDelta);
        }

        [Test]
        public void TryChooseDodgeStep_AllFourNeighborsMarkedReal_ReturnsFalse()
        {
            BattleParticipant defender = MakeDefender(lane: 4, position: 3);
            var defendingSide = new List<BattleParticipant> { defender };
            var markedCells = new List<ZoneCell>
            {
                new ZoneCell(5, 3), new ZoneCell(3, 3), new ZoneCell(4, 4), new ZoneCell(4, 2),
            };

            bool result = EnemyAI.TryChooseDodgeStep(defender, markedCells, defendingSide,
                EnemyDifficultyTier.AlwaysDodges, out int laneDelta, out int positionDelta);

            Assert.IsFalse(result);
            Assert.AreEqual(0, laneDelta);
            Assert.AreEqual(0, positionDelta);
        }

        [Test]
        public void TryChooseDodgeStep_FirstCandidateOccupied_SkipsToNextSafeCandidate()
        {
            BattleParticipant defender = MakeDefender(lane: 4, position: 3);
            BattleParticipant ally = MakeDefender(lane: 5, position: 3); // occupies the lane+1 candidate
            var defendingSide = new List<BattleParticipant> { defender, ally };
            var markedCells = new List<ZoneCell> { new ZoneCell(4, 3) };

            bool result = EnemyAI.TryChooseDodgeStep(defender, markedCells, defendingSide,
                EnemyDifficultyTier.AlwaysDodges, out int laneDelta, out int positionDelta);

            Assert.IsTrue(result);
            Assert.AreEqual(-1, laneDelta); // lane+1 was occupied, lane-1 is the next candidate in order
            Assert.AreEqual(0, positionDelta);
        }

        [Test]
        public void TryChooseDodgeStep_EliteTier_DodgesMoreOftenThanWeakTier()
        {
            const int trials = 300;
            int weakSuccesses = 0;
            int eliteSuccesses = 0;

            for (int i = 0; i < trials; i++)
            {
                BattleParticipant weakDefender = MakeDefender(instinct: 10);
                if (EnemyAI.TryChooseDodgeStep(weakDefender, new List<ZoneCell>(), new List<BattleParticipant> { weakDefender },
                        EnemyDifficultyTier.Weak, out _, out _))
                    weakSuccesses++;

                BattleParticipant eliteDefender = MakeDefender(instinct: 10);
                if (EnemyAI.TryChooseDodgeStep(eliteDefender, new List<ZoneCell>(), new List<BattleParticipant> { eliteDefender },
                        EnemyDifficultyTier.Elite, out _, out _))
                    eliteSuccesses++;
            }

            Assert.Greater(eliteSuccesses, weakSuccesses);
        }
    }
}
