/// <summary>
/// Which hold-input archetype a skill uses (2026-08-17, Attack_Pattern_Directive Part 5 Group 2's
/// second/third archetypes: "Charge & Release" + "Sustained Pressure" — "build these two together:
/// both are 'hold input' instead of 'tap input,' diverging only in scoring... Share one new
/// hold-input primitive.") — same tier as StackingRhythmType/CompassPoint. A skill with a non-None
/// value bypasses the normal tap-timing rings (RunTimedInput/RunDefenseTimedInput) entirely in favor
/// of BattleHUDController.RunHoldGesture's press-and-hold-then-release primitive.
///
/// ChargeRelease is an OFFENSE archetype: the player holds to charge, then releases — both the press
/// instant (vs. an authored tell) and the release instant (vs. an authored hold duration) are scored;
/// see SkillData.ChargeReleaseTellSeconds/ChargeReleaseTargetHoldSeconds and
/// BattleManager.ResolveChargeReleaseAttack.
///
/// SustainedPressure is a DEFENSE archetype ("hold-to-guard"): the player reacts to an enemy's
/// attack by holding through its duration, scored the same two-instant way, producing a graduated
/// BattleHUDController.DefenseOutcome.Guard block percentage rather than a binary avoid/hit; see
/// SkillData.SustainedPressureTellSeconds/SustainedPressureHoldSeconds.
/// </summary>
public enum HoldInputArchetype
{
    None,
    ChargeRelease,
    SustainedPressure
}
