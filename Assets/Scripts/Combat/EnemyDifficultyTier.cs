/// <summary>
/// Per-encounter multiplier on top of an enemy's own Instinct/bond-scaled Zone/Positional dodge
/// chance (EnemyAI.TryChooseDodgeStep) — 2026-08-21, offense-direction follow-up to
/// Attack_Pattern_Directive Group 3 item 7. Set via EncounterTrigger's Inspector field, defaulting
/// to Standard. AlwaysDodges is a hard override (skips the roll entirely, see
/// EnemyAI.TryChooseDodgeStep), giving the "some may always be able to dodge" case directly rather
/// than relying on a multiplier large enough to round to ~100%.
/// </summary>
public enum EnemyDifficultyTier
{
    Weak,
    Standard,
    Elite,
    AlwaysDodges
}
