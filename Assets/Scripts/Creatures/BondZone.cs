/// <summary>
/// Bond relationship zones. Floor = last milestone reached, never drops below it.
/// Explicit values are the bond-percent thresholds themselves (see
/// PhasixRuntimeData.HasReachedBondZone, which compares bondFloor directly against
/// (float)zone — matches Evolution_System_Directive_v1_1_0.md's exact pattern).
/// Authority: CLAUDE.md Bond section; zone list from Evolution_System_Directive_v1_1_0.md.
/// </summary>
public enum BondZone
{
    Stranger = 0,
    Familiar = 20,
    Companion = 40,
    Partner = 60,
    Bonded = 80,
    Complete = 100
}
