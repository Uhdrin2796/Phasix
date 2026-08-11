using UnityEngine;

/// <summary>
/// Sole EventBus subscriber for battle audio/VFX (2026-08-10 — Phase 3 close-out pass). Fans out
/// to AudioManager (all 9 events) and, for the single-participant-or-none events only,
/// BattleHUDController's whole-Stage VFX passthroughs (per-hit projectile/flash VFX instead goes
/// through BattleHUDController.PlayHitVfx, called directly by BattleManager — see
/// CombatVfxController.cs's class doc comment for why). Kept as the sole subscriber here rather
/// than a battle-scene-local component because some of these events (OnBondMilestoneReached) can
/// fire from OUTSIDE battle entirely — every BattleHUDController.Instance call below is
/// null-conditional for exactly that reason (BattleHUDController only exists while
/// BattleScene_Main is loaded).
///
/// GDD §27 "Audio Design" is still tagged Pending in its entirety ("Design work not yet started")
/// — every clip played here is placeholder-quality generated audio (AudioCueCatalog), not real
/// designed content; swapping placeholders for real assets later needs zero code changes here.
///
/// One piece of design intent worth preserving for whoever eventually authors real content: GDD
/// §27's banner notes Signal type identity is meant to be expressed through sound/visual cues
/// BEFORE it's understood intellectually (Region 2 has no text feedback for Signal) — "not
/// cosmetic, a core game mechanic." Worth keeping in mind for whichever hook ends up carrying
/// Signal-reveal audio/VFX, once that content exists.
/// </summary>
public static class BattleAudioVfxHooks
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Subscribe()
    {
        EventBus.OnBattleWon += OnBattleWon;
        EventBus.OnBattleLost += OnBattleLost;
        EventBus.OnBattleFled += OnBattleFled;
        EventBus.OnSkillUsed += OnSkillUsed;
        EventBus.OnTimedInputSuccess += OnTimedInputSuccess;
        EventBus.OnDamageTaken += OnDamageTaken;
        EventBus.OnBondMilestoneReached += OnBondMilestoneReached;
        EventBus.OnEvolved += OnEvolved;
        EventBus.OnPhasixCaptured += OnPhasixCaptured;
    }

    private static void OnBattleWon(BattleResult result)
    {
        AudioManager.Instance?.PlayBattleWon();
        BattleHUDController.Instance?.PlayBattleOutcomeVfx(won: true);
    }

    private static void OnBattleLost(BattleResult result)
    {
        AudioManager.Instance?.PlayBattleLost();
        BattleHUDController.Instance?.PlayBattleOutcomeVfx(won: false);
    }

    private static void OnBattleFled(BattleResult result) => AudioManager.Instance?.PlayBattleFled();

    private static void OnSkillUsed(PhasixRuntimeData phasix, SkillData skill) => AudioManager.Instance?.PlaySkillUsed();

    private static void OnTimedInputSuccess(PhasixRuntimeData phasix) => AudioManager.Instance?.PlayTimedInputSuccess();

    // Fires on every resolved hit including fully-avoided Dodge/Parry (damage == 0 in that case,
    // per BattleEngine.ResolveQueuedActions) — guarded so avoided hits stay silent.
    private static void OnDamageTaken(PhasixRuntimeData phasix, int damage)
    {
        if (damage <= 0) return;
        AudioManager.Instance?.PlayHitImpact(phasix.speciesData != null ? phasix.speciesData.PrimalType : PrimalType.Fire);
    }

    private static void OnBondMilestoneReached(PhasixRuntimeData phasix, BondZone zone)
    {
        AudioManager.Instance?.PlayBondMilestone();
        BattleHUDController.Instance?.PlayBondMilestoneVfx();
    }

    private static void OnEvolved(PhasixRuntimeData phasix, PhasixData newForm) => AudioManager.Instance?.PlayEvolved();

    private static void OnPhasixCaptured(PhasixRuntimeData phasix)
    {
        AudioManager.Instance?.PlayCaptured();
        BattleHUDController.Instance?.PlayCaptureVfx();
    }
}
