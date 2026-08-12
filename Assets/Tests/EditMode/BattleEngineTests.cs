using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers BattleEngine's turn-resolution rules: queueing, placeholder damage application, and
    /// win/loss detection (Roadmap_v2 Mo 5 Wk 1-2). BattleEngine is pure static logic over plain
    /// BattleState/BattleParticipant, so no scene/prefab/ScriptableObject setup is needed — matches
    /// BondSystemTests.cs's pattern.
    /// </summary>
    public class BattleEngineTests
    {
        private static PhasixRuntimeData MakePhasix(int vitality)
        {
            return new PhasixRuntimeData("test-node-guid")
            {
                baseStats = new StatBlock { Vitality = vitality }
            };
        }

        private static BattleParticipant MakeParticipant(int vitality, bool isPlayerSide)
        {
            return new BattleParticipant(MakePhasix(vitality), isPlayerSide);
        }

        [Test]
        public void BattleParticipant_MaxHP_MatchesEffectiveVitality()
        {
            var participant = MakeParticipant(vitality: 20, isPlayerSide: true);

            Assert.AreEqual(20, participant.MaxHP);
            Assert.AreEqual(20, participant.CurrentHP);
            Assert.IsTrue(participant.IsAlive);
        }

        [Test]
        public void BattleParticipant_MaxHP_ClampsToAtLeastOne_WhenVitalityIsZero()
        {
            var participant = MakeParticipant(vitality: 0, isPlayerSide: true);

            Assert.AreEqual(1, participant.MaxHP, "MaxHP must never be zero — a 0-Vitality placeholder shouldn't spawn dead.");
        }

        [Test]
        public void ApplyDamage_ClampsAtZero_NeverGoesNegative()
        {
            var participant = MakeParticipant(vitality: 5, isPlayerSide: true);

            participant.ApplyDamage(999);

            Assert.AreEqual(0, participant.CurrentHP);
            Assert.IsFalse(participant.IsAlive);
        }

        [Test]
        public void QueueBasicAttack_NoOp_WhenTargetAlreadyDown()
        {
            var attacker = MakeParticipant(vitality: 10, isPlayerSide: true);
            var target = MakeParticipant(vitality: 10, isPlayerSide: false);
            target.ApplyDamage(999);

            var state = new BattleState(new List<BattleParticipant> { attacker }, new List<BattleParticipant> { target });
            BattleEngine.QueueBasicAttack(state, attacker, target);

            Assert.AreEqual(0, state.QueuedActions.Count, "A dead target must never be queued as an attack target.");
        }

        [Test]
        public void ResolveQueuedActions_AppliesPlaceholderDamage_AndClearsQueue()
        {
            var attacker = MakeParticipant(vitality: 10, isPlayerSide: true);
            var target = MakeParticipant(vitality: 10, isPlayerSide: false);
            var state = new BattleState(new List<BattleParticipant> { attacker }, new List<BattleParticipant> { target });

            BattleEngine.QueueBasicAttack(state, attacker, target);
            BattleEngine.ResolveQueuedActions(state);

            Assert.AreEqual(10 - BattleConfig.PlaceholderAttackDamage, target.CurrentHP);
            Assert.AreEqual(0, state.QueuedActions.Count, "Resolved actions must be cleared from the queue.");
        }

        [Test]
        public void ResolveQueuedActions_SkipsAction_WhenAttackerDiedEarlierInSamePass()
        {
            // Both queued actions are resolved in the same pass: the defender's attack kills
            // firstAttacker first, so firstAttacker's own queued retaliation against the defender
            // must be skipped rather than applied by a now-dead attacker.
            var defender = MakeParticipant(vitality: BattleConfig.PlaceholderAttackDamage, isPlayerSide: true);
            var firstAttacker = MakeParticipant(vitality: BattleConfig.PlaceholderAttackDamage, isPlayerSide: false);

            var state = new BattleState(
                new List<BattleParticipant> { defender },
                new List<BattleParticipant> { firstAttacker });

            BattleEngine.QueueBasicAttack(state, defender, firstAttacker); // kills firstAttacker
            BattleEngine.QueueBasicAttack(state, firstAttacker, defender); // must be skipped — firstAttacker dies first in this pass
            BattleEngine.ResolveQueuedActions(state);

            Assert.IsFalse(firstAttacker.IsAlive);
            Assert.AreEqual(BattleConfig.PlaceholderAttackDamage, defender.CurrentHP,
                "Defender must take no damage — the attacker that would have hit it died earlier in the same resolution pass.");
        }

        [Test]
        public void ResolveQueuedActions_AppliesDamageMultiplier_ForSuccessfulOffensiveTiming()
        {
            var attacker = MakeParticipant(vitality: 10, isPlayerSide: true);
            var target = MakeParticipant(vitality: 30, isPlayerSide: false);
            var state = new BattleState(new List<BattleParticipant> { attacker }, new List<BattleParticipant> { target });

            BattleEngine.QueueBasicAttack(state, attacker, target, TimedInputConfig.GoodDamageMultiplier);
            BattleEngine.ResolveQueuedActions(state);

            int expectedDamage = Mathf.RoundToInt(BattleConfig.PlaceholderAttackDamage * TimedInputConfig.GoodDamageMultiplier);
            Assert.AreEqual(30 - expectedDamage, target.CurrentHP,
                "A successful offensive timing hit must scale damage by GoodDamageMultiplier.");
        }

        [Test]
        public void ResolveQueuedActions_ZeroMultiplier_FullyAvoidsDamage()
        {
            // Dodge/Parry success (Combat_Directive Part 4, Expedition 33-inspired full-avoidance
            // model) is represented as a 0 damageMultiplier — BattleEngine itself doesn't know
            // about dodge/parry, it just applies whatever multiplier it's given.
            var attacker = MakeParticipant(vitality: 10, isPlayerSide: false);
            var target = MakeParticipant(vitality: 30, isPlayerSide: true);
            var state = new BattleState(new List<BattleParticipant> { target }, new List<BattleParticipant> { attacker });

            BattleEngine.QueueBasicAttack(state, attacker, target, damageMultiplier: 0f);
            BattleEngine.ResolveQueuedActions(state);

            Assert.AreEqual(30, target.CurrentHP, "A 0 damageMultiplier (dodge/parry success) must fully avoid the hit.");
        }

        [Test]
        public void CheckOutcome_ReturnsWon_WhenEnemySideWiped()
        {
            var playerSide = new List<BattleParticipant> { MakeParticipant(10, true) };
            var enemy = MakeParticipant(10, false);
            enemy.ApplyDamage(999);
            var enemySide = new List<BattleParticipant> { enemy };

            Assert.AreEqual(BattleOutcome.Won, BattleEngine.CheckOutcome(new BattleState(playerSide, enemySide)));
        }

        [Test]
        public void CheckOutcome_ReturnsLost_WhenPlayerSideWiped()
        {
            var player = MakeParticipant(10, true);
            player.ApplyDamage(999);
            var playerSide = new List<BattleParticipant> { player };
            var enemySide = new List<BattleParticipant> { MakeParticipant(10, false) };

            Assert.AreEqual(BattleOutcome.Lost, BattleEngine.CheckOutcome(new BattleState(playerSide, enemySide)));
        }

        [Test]
        public void CheckOutcome_ReturnsInProgress_WhenBothSidesHaveSurvivors()
        {
            var playerSide = new List<BattleParticipant> { MakeParticipant(10, true) };
            var enemySide = new List<BattleParticipant> { MakeParticipant(10, false) };

            Assert.AreEqual(BattleOutcome.InProgress, BattleEngine.CheckOutcome(new BattleState(playerSide, enemySide)));
        }
    }
}
