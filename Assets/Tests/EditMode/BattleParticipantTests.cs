using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>Covers BattleParticipant.SpendAura/RestoreAura (2026-08-05 — see DECISIONS.md -> [Combat]: attack Aura cost, perfect-defense Aura restore) and Heal/ApplyRegen/TickRegen (2026-08-06 — "H"/"R" move options).</summary>
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
    }
}
