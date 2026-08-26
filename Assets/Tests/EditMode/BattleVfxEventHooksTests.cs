using NUnit.Framework;

namespace Phasix.Tests.EditMode
{
    /// <summary>
    /// Covers BattleVfxEventHooks' public handlers (2026-08-25, split out of
    /// Audio/BattleAudioVfxHooks.cs to close the Combat&lt;-&gt;Audio assembly cycle — see
    /// DECISIONS.md -> [Architecture]). EditMode tests run with no scene loaded, so
    /// BattleHUDController.Instance is always null here — these confirm the null-conditional guard
    /// holds and none of the handlers throw when called directly, the same condition
    /// SkillTreeUnlockSystemTests.cs already covers for the sibling static-subscriber pattern.
    /// </summary>
    public class BattleVfxEventHooksTests
    {
        private static PhasixRuntimeData MakePhasix() => new PhasixRuntimeData("test-node-guid");

        [Test]
        public void OnBattleWon_NoBattleHUDController_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => BattleVfxEventHooks.OnBattleWon());
        }

        [Test]
        public void OnBattleLost_NoBattleHUDController_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => BattleVfxEventHooks.OnBattleLost());
        }

        [Test]
        public void OnBondMilestoneReached_NoBattleHUDController_DoesNotThrow()
        {
            var phasix = MakePhasix();
            Assert.DoesNotThrow(() => BattleVfxEventHooks.OnBondMilestoneReached(phasix, BondZone.Familiar));
        }

        [Test]
        public void OnPhasixCaptured_NoBattleHUDController_DoesNotThrow()
        {
            var phasix = MakePhasix();
            Assert.DoesNotThrow(() => BattleVfxEventHooks.OnPhasixCaptured(phasix));
        }
    }
}
