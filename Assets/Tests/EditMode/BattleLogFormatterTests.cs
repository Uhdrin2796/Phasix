using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers BattleLogFormatter's effectiveness text mapping, timed-input/Dodge/Parry phrasing, and the base/type/timing damage breakdown. Pure static text formatting.</summary>
    public class BattleLogFormatterTests
    {
        private static PhasixRuntimeData MakePhasix() => new PhasixRuntimeData("test-node-guid") { baseStats = new StatBlock { Vitality = 20 } };
        private static BattleParticipant MakeParticipant(bool isPlayerSide) => new BattleParticipant(MakePhasix(), isPlayerSide);

        [Test]
        public void FormatAttack_IncludesTotalDamageNumber()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, pureBaseDamage: 13, damageAfterType: 13, finalDamage: 13, typeMultiplier: 1f, offenseOutcome: BattleHUDController.OffenseOutcome.Good);

            StringAssert.Contains("13 total damage", line);
        }

        [Test]
        public void FormatAttack_SuperEffective_AtDoubleMultiplier()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, 20, 20, typeMultiplier: 2.0f, offenseOutcome: BattleHUDController.OffenseOutcome.Good);

            StringAssert.Contains("super effective", line);
        }

        [Test]
        public void FormatAttack_NotVeryEffective_AtQuarterResist()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, 8, 8, typeMultiplier: 0.75f, offenseOutcome: BattleHUDController.OffenseOutcome.Good);

            StringAssert.Contains("not very effective", line);
        }

        [Test]
        public void FormatAttack_BarelyEffective_AtMinimumMultiplier()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, 5, 5, typeMultiplier: 0.5f, offenseOutcome: BattleHUDController.OffenseOutcome.Good);

            StringAssert.Contains("barely effective", line);
        }

        [Test]
        public void FormatAttack_NeutralMultiplier_HasNoEffectivenessNote()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, 10, 10, typeMultiplier: 1.0f, offenseOutcome: BattleHUDController.OffenseOutcome.Good);

            StringAssert.DoesNotContain("effective", line);
        }

        [Test]
        public void FormatAttack_Perfect_MentionsAttackerTiming()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, 10, 15, typeMultiplier: 1f, offenseOutcome: BattleHUDController.OffenseOutcome.Perfect);

            StringAssert.Contains("timing was perfect", line);
        }

        [Test]
        public void FormatAttack_Miss_MentionsWeakenedBlow()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, 10, 5, typeMultiplier: 1f, offenseOutcome: BattleHUDController.OffenseOutcome.Miss);

            StringAssert.Contains("timing was off", line);
        }

        [Test]
        public void FormatAttack_Good_HasNoTimingNote()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, 10, 10, typeMultiplier: 1f, offenseOutcome: BattleHUDController.OffenseOutcome.Good);

            StringAssert.DoesNotContain("timing was", line);
        }

        [Test]
        public void FormatAttack_NullOutcome_HasNoTimingNote()
        {
            // Represents the Parry counter-attack, which runs no timing check at all.
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, 10, 10, typeMultiplier: 1f, offenseOutcome: null);

            StringAssert.DoesNotContain("timing was", line);
        }

        [Test]
        public void FormatAttack_NullOutcome_OmitsTimingBreakdownTerm()
        {
            // No timed-input check ran at all, so the breakdown shouldn't claim a "timing" term
            // exists (not even a "+0 timing") — see FormatDamageBreakdown's own doc comment.
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, 10, 12, 12, typeMultiplier: 1.2f, offenseOutcome: null);

            StringAssert.DoesNotContain("timing", line);
        }

        [Test]
        public void FormatAttack_Breakdown_ShowsBaseDamageInWhite()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, pureBaseDamage: 10, damageAfterType: 10, finalDamage: 10, typeMultiplier: 1f, offenseOutcome: BattleHUDController.OffenseOutcome.Good);

            StringAssert.Contains("<color=#FFFFFF>10 base</color>", line);
        }

        [Test]
        public void FormatAttack_Breakdown_PositiveTypeDelta_IsGreenWithPlusSign()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, pureBaseDamage: 10, damageAfterType: 16, finalDamage: 16, typeMultiplier: 1.6f, offenseOutcome: BattleHUDController.OffenseOutcome.Good);

            StringAssert.Contains("<color=#5AC864>6 type</color>", line);
            StringAssert.DoesNotContain("-6 type", line);
        }

        [Test]
        public void FormatAttack_Breakdown_NegativeTypeDelta_IsRedWithMinusSign()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, pureBaseDamage: 10, damageAfterType: 5, finalDamage: 5, typeMultiplier: 0.5f, offenseOutcome: BattleHUDController.OffenseOutcome.Good);

            StringAssert.Contains("- <color=#DC3C3C>5 type</color>", line);
        }

        [Test]
        public void FormatAttack_Breakdown_PositiveTimingDelta_IsGreen()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, pureBaseDamage: 10, damageAfterType: 10, finalDamage: 20, typeMultiplier: 1f, offenseOutcome: BattleHUDController.OffenseOutcome.Perfect);

            StringAssert.Contains("<color=#5AC864>10 timing</color>", line);
        }

        [Test]
        public void FormatAttack_Breakdown_NegativeTimingDelta_IsRed()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatAttack(attacker, target, pureBaseDamage: 10, damageAfterType: 10, finalDamage: 5, typeMultiplier: 1f, offenseOutcome: BattleHUDController.OffenseOutcome.Miss);

            StringAssert.Contains("- <color=#DC3C3C>5 timing</color>", line);
        }

        [Test]
        public void FormatDefenseOutcome_Dodged_HasNoDamageNumber()
        {
            var attacker = MakeParticipant(false);
            var target = MakeParticipant(true);

            string line = BattleLogFormatter.FormatDefenseOutcome(attacker, target, pureBaseDamage: 12, damageAfterType: 12, finalDamage: 12, typeMultiplier: 1f, avoided: true, attemptedParry: false);

            StringAssert.Contains("dodges", line);
            StringAssert.DoesNotContain("12 total damage", line);
        }

        [Test]
        public void FormatDefenseOutcome_Parried_MentionsCounterOpening()
        {
            var attacker = MakeParticipant(false);
            var target = MakeParticipant(true);

            string line = BattleLogFormatter.FormatDefenseOutcome(attacker, target, pureBaseDamage: 12, damageAfterType: 12, finalDamage: 12, typeMultiplier: 1f, avoided: true, attemptedParry: true);

            StringAssert.Contains("parries", line);
            StringAssert.Contains("counter", line);
        }

        [Test]
        public void FormatDefenseOutcome_Failed_ReadsAsNormalAttack_RegardlessOfAttemptedParry()
        {
            var attacker = MakeParticipant(false);
            var target = MakeParticipant(true);

            string line = BattleLogFormatter.FormatDefenseOutcome(attacker, target, pureBaseDamage: 9, damageAfterType: 9, finalDamage: 9, typeMultiplier: 1f, avoided: false, attemptedParry: true);

            StringAssert.Contains("9 total damage", line);
            StringAssert.DoesNotContain("dodges", line);
            StringAssert.DoesNotContain("parries", line);
        }

        [Test]
        public void FormatDefenseOutcome_Failed_OmitsTimingBreakdownTerm()
        {
            // An incoming hit that lands is always full (1x) damage — no timed-input multiplier
            // applies to it, only Dodge/Parry's full-avoidance is at stake.
            var attacker = MakeParticipant(false);
            var target = MakeParticipant(true);

            string line = BattleLogFormatter.FormatDefenseOutcome(attacker, target, pureBaseDamage: 9, damageAfterType: 9, finalDamage: 9, typeMultiplier: 1f, avoided: false, attemptedParry: false);

            StringAssert.DoesNotContain("timing", line);
        }

        [Test]
        public void FormatSkillAttack_IncludesSkillNameAndTotalDamage()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatSkillAttack(attacker, target, "Aspect_Placeholder1", pureBaseDamage: 7, damageAfterType: 7, finalDamage: 7, typeMultiplier: 1f, offenseOutcome: BattleHUDController.OffenseOutcome.Good);

            StringAssert.Contains("Aspect_Placeholder1", line);
            StringAssert.Contains("7 total damage", line);
        }

        [Test]
        public void FormatSkillAttack_Perfect_MentionsAttackerTiming()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatSkillAttack(attacker, target, "Aspect_Placeholder1", pureBaseDamage: 7, damageAfterType: 7, finalDamage: 14, typeMultiplier: 1f, offenseOutcome: BattleHUDController.OffenseOutcome.Perfect);

            StringAssert.Contains("timing was perfect", line);
        }

        [Test]
        public void FormatSkillAttack_Miss_MentionsWeakenedBlow()
        {
            var attacker = MakeParticipant(true);
            var target = MakeParticipant(false);

            string line = BattleLogFormatter.FormatSkillAttack(attacker, target, "Aspect_Placeholder1", pureBaseDamage: 6, damageAfterType: 6, finalDamage: 3, typeMultiplier: 1f, offenseOutcome: BattleHUDController.OffenseOutcome.Miss);

            StringAssert.Contains("timing was off", line);
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
