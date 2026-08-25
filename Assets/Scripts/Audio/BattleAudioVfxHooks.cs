using UnityEngine;

/// <summary>
/// Sole EventBus subscriber for battle audio (2026-08-10 — Phase 3 close-out pass; trimmed
/// 2026-08-25 to audio-only — see Combat/BattleVfxEventHooks.cs's class doc comment). Fans out to
/// AudioManager (all 9 events). The whole-Stage VFX passthroughs this class used to also drive
/// directly on BattleHUDController were split into Combat/BattleVfxEventHooks.cs, since a
/// Combat-folder file calling back into an Audio-folder file created a real assembly cycle once
/// Phasix.Combat/Phasix.Audio needed to become separate assemblies — see DECISIONS.md ->
/// [Architecture]. Per-hit projectile/flash VFX still goes through BattleHUDController.PlayHitVfx,
/// called directly by BattleManager — see CombatVfxController.cs's class doc comment for why.
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

    private static void OnBattleWon(BattleResult result) => AudioManager.Instance?.PlayBattleWon();

    private static void OnBattleLost(BattleResult result) => AudioManager.Instance?.PlayBattleLost();

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

    private static void OnBondMilestoneReached(PhasixRuntimeData phasix, BondZone zone) => AudioManager.Instance?.PlayBondMilestone();

    private static void OnEvolved(PhasixRuntimeData phasix, PhasixData newForm) => AudioManager.Instance?.PlayEvolved();

    private static void OnPhasixCaptured(PhasixRuntimeData phasix) => AudioManager.Instance?.PlayCaptured();
}
