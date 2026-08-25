using UnityEngine;

/// <summary>
/// Sole EventBus subscriber for BattleHUDController's whole-Stage VFX passthroughs (2026-08-25 —
/// split out of Audio/BattleAudioVfxHooks.cs to close the Combat&lt;-&gt;Audio assembly cycle; see
/// DECISIONS.md -> [Architecture]). Subscribes itself once via RuntimeInitializeOnLoadMethod rather
/// than needing a scene object — same pattern Combat/SkillTreeUnlockSystem.cs already establishes
/// for "a static rule that just needs to always be listening." Every BattleHUDController.Instance
/// call below is null-conditional for the same reason BattleAudioVfxHooks.cs's were: some of these
/// events (e.g. OnBondMilestoneReached) can fire from OUTSIDE battle entirely — BattleHUDController
/// only exists while BattleScene_Main is loaded. Per-hit projectile/flash VFX instead goes through
/// BattleHUDController.PlayHitVfx, called directly by BattleManager — see CombatVfxController.cs's
/// class doc comment for why.
/// </summary>
public static class BattleVfxEventHooks
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Subscribe()
    {
        EventBus.OnBattleWon += OnBattleWon;
        EventBus.OnBattleLost += OnBattleLost;
        EventBus.OnBondMilestoneReached += OnBondMilestoneReached;
        EventBus.OnPhasixCaptured += OnPhasixCaptured;
    }

    /// <summary>Public so EditMode tests can call it directly without relying on RuntimeInitializeOnLoadMethod having fired.</summary>
    public static void OnBattleWon(BattleResult result) => BattleHUDController.Instance?.PlayBattleOutcomeVfx(won: true);

    /// <summary>Public so EditMode tests can call it directly without relying on RuntimeInitializeOnLoadMethod having fired.</summary>
    public static void OnBattleLost(BattleResult result) => BattleHUDController.Instance?.PlayBattleOutcomeVfx(won: false);

    /// <summary>Public so EditMode tests can call it directly without relying on RuntimeInitializeOnLoadMethod having fired.</summary>
    public static void OnBondMilestoneReached(PhasixRuntimeData phasix, BondZone zone) => BattleHUDController.Instance?.PlayBondMilestoneVfx();

    /// <summary>Public so EditMode tests can call it directly without relying on RuntimeInitializeOnLoadMethod having fired.</summary>
    public static void OnPhasixCaptured(PhasixRuntimeData phasix) => BattleHUDController.Instance?.PlayCaptureVfx();
}
