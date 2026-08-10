using UnityEngine;

/// <summary>
/// Empty EventBus subscriber hook points for battle audio/VFX. GDD §27 "Audio Design" is tagged
/// Pending in its entirety ("Design work not yet started") — no sound/VFX content exists anywhere
/// in the docs to build against, and "VFX"/"particle"/"screen shake" don't appear in the GDD or
/// any Directive at all. This class is pure scaffolding: it subscribes to the battle events real
/// audio/VFX would eventually hang off of, with empty bodies (not even placeholder Debug.Log
/// calls, which would just be console noise during real play) and a TODO per hook.
///
/// One piece of design intent worth preserving for whoever fills these in: GDD §27's banner notes
/// Signal type identity is meant to be expressed through sound/visual cues BEFORE it's understood
/// intellectually (Region 2 has no text feedback for Signal) — "not cosmetic, a core game
/// mechanic." Worth keeping in mind for whichever hook ends up carrying Signal-reveal audio/VFX,
/// once that content exists.
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

    // TODO: pending design — victory fanfare/stinger (GDD §27, no content designed yet)
    private static void OnBattleWon(BattleResult result) { }

    // TODO: pending design — defeat sting (GDD §27, no content designed yet)
    private static void OnBattleLost(BattleResult result) { }

    // TODO: pending design — "got away safely" sting for a successful Flee (GDD §27, no content designed yet)
    private static void OnBattleFled(BattleResult result) { }

    // TODO: pending design — per-skill-tree cast SFX/VFX (GDD §27, no content designed yet)
    private static void OnSkillUsed(PhasixRuntimeData phasix, SkillData skill) { }

    // TODO: pending design — "perfect timing" hit-confirm SFX/VFX (GDD §27, no content designed yet)
    private static void OnTimedInputSuccess(PhasixRuntimeData phasix) { }

    // TODO: pending design — hit impact SFX/VFX, Primal-type-flavored (GDD §27.3, no content designed yet)
    private static void OnDamageTaken(PhasixRuntimeData phasix, int damage) { }

    // TODO: pending design — bond milestone chime, Bond-100 needs "a distinctive audio moment" per GDD §27.5
    private static void OnBondMilestoneReached(PhasixRuntimeData phasix, BondZone zone) { }

    // TODO: pending design — evolution transformation SFX/VFX, "a creature game signature" per GDD §27.5
    private static void OnEvolved(PhasixRuntimeData phasix, PhasixData newForm) { }

    // TODO: pending design — capture success SFX/VFX (GDD §27, no content designed yet)
    private static void OnPhasixCaptured(PhasixRuntimeData phasix) { }
}
