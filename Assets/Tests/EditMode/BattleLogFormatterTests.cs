using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers BattleLogFormatter's effectiveness text mapping and timed-input/Dodge/Parry phrasing. Pure static text formatting.</summary>
    public class BattleLogFormatterTests
    {
        private static PhasixRuntimeData MakePhasix() => new PhasixRuntimeData("test-node-guid") { baseStats = new StatBlock { Vitality = 20 } };
        private static BattleParticipant MakeParticipant(bool isPlayerSide) => new BattleParticipant(MakePhasix(), isPlayerSide);

        [Test]
        public void FormatAttack_IncludesDamageNumber()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, damage: 13, typeMultiplier: 1f, timedInputSuccess: false);

            StringAssert.Contains("13 damage", line);
        }

        [Test]
        public void FormatAttack_SuperEffective_AtDoubleMultiplier()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, typeMultiplier: 2.0f, timedInputSuccess: false);

            StringAssert.Contains("super effective", line);
        }

        [Test]
        public void FormatAttack_NotVeryEffective_AtQuarterResist()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, typeMultiplier: 0.75f, timedInputSuccess: false);

            StringAssert.Contains("not very effective", line);
        }

        [Test]
        public void FormatAttack_BarelyEffective_AtMinimumMultiplier()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, typeMultiplier: 0.5f, timedInputSuccess: false);

            StringAssert.Contains("barely effective", line);
        }

        [Test]
        public void FormatAttack_NeutralMultiplier_HasNoEffectivenessNote()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, typeMultiplier: 1.0f, timedInputSuccess: false);

            StringAssert.DoesNotContain("effective", line);
        }

        [Test]
        public void FormatAttack_OffensiveSuccess_MentionsAttackerTiming()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 15, typeMultiplier: 1f, timedInputSuccess: true);

            StringAssert.Contains("timing was perfect", line);
        }

        [Test]
        public void FormatDefenseOutcome_Dodged_HasNoDamageNumber()
        {
            var attacker = MakeParticipant(false);
            var target = MakeParticipant(true);

            string line = BattleLogFormatter.FormatDefenseOutcome(attacker, target, damage: 12, typeMultiplier: 1f, avoided: true, attemptedParry: false);

            StringAssert.Contains("dodges", line);
            StringAssert.DoesNotContain("12 damage", line);
        }

        [Test]
        public void FormatDefenseOutcome_Parried_MentionsCounterOpening()
        {
            var attacker = MakeParticipant(false);
            var target = MakeParticipant(true);

            string line = BattleLogFormatter.FormatDefenseOutcome(attacker, target, damage: 12, typeMultiplier: 1f, avoided: true, attemptedParry: true);

            StringAssert.Contains("parries", line);
            StringAssert.Contains("counter", line);
        }

        [Test]
        public void FormatDefenseOutcome_Failed_ReadsAsNormalAttack_RegardlessOfAttemptedParry()
        {
            var attacker = MakeParticipant(false);
            var target = MakeParticipant(true);

            string line = BattleLogFormatter.FormatDefenseOutcome(attacker, target, damage: 9, typeMultiplier: 1f, avoided: false, attemptedParry: true);

            StringAssert.Contains("9 damage", line);
            StringAssert.DoesNotContain("dodges", line);
            StringAssert.DoesNotContain("parries", line);
        }

        [Test]
        public void FormatSkillAttack_IncludesSkillNameAndDamage()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatSkillAttack(attacker, target, "Aspect_Placeholder1", damage: 7, typeMultiplier: 1f);

            StringAssert.Contains("Aspect_Placeholder1", line);
            StringAssert.Contains("7 damage", line);
        }

        [Test]
        public void FormatStatusApplied_IncludesStatusAndDuration()
        {
            var target = MakeParticipant(true);

            string line = BattleLogFormatter.FormatStatusApplied(target, StatusEffectType.Burn, durationTurns: 4);

            StringAssert.Contains("Burn", line);
            StringAssert.Contains("4", line);
        }

        [Test]
        public void FormatComboDetected_IncludesTier()
        {
            var attacker = MakeParticipant(true);

            string line = BattleLogFormatter.FormatComboDetected(attacker, ComboTier.Trio, ComboRuleType.CrossTreeSequence);

            StringAssert.Contains("Trio", line);
        }

        [Test]
        public void FormatChainResultTriggered_IncludesLockedEffectTextVerbatim()
        {
            var target = MakeParticipant(true);

            string line = BattleLogFormatter.FormatChainResultTriggered(target, ChainResultType.Rend);

            StringAssert.Contains("Rend", line);
            StringAssert.Contains(ChainResultCatalog.GetEffectDescription(ChainResultType.Rend), line);
        }

        [Test]
        public void FormatMasteryBonusTriggered_IncludesLockedTriggerAndEffectTextVerbatim()
        {
            var attacker = MakeParticipant(true);

            string line = BattleLogFormatter.FormatMasteryBonusTriggered(attacker, MasteryBonusType.Hemorrhage);

            StringAssert.Contains(MasteryBonusCatalog.GetTriggerDescription(MasteryBonusType.Hemorrhage), line);
            StringAssert.Contains(MasteryBonusCatalog.GetEffectDescription(MasteryBonusType.Hemorrhage), line);
        }
    }
}
