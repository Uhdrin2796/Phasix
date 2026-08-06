/// <summary>
/// Stat ceiling per tier, Progression_Directive_v0_1_0.md "Tier Stat Ceiling" section (locked
/// mechanic, no locked formula): "Stat growth through Common Aura is capped per tier... The
/// ceiling is not fixed — it scales with Aptitude. Higher Aptitude raises the ceiling." The
/// Directive's own pending list confirms "Base stat ceiling per tier at Aptitude 0" and "Stat
/// ceiling increase per Aptitude point per tier" are both open — nothing here is a locked number,
/// only the shape (ceiling grows with tier, ceiling grows with Aptitude) is.
///
/// Ceiling is checked against baseStats.Total (the Aura-spendable layer) — NOT unnamedPool, which
/// grows only through devolution and isn't something Aura allocation touches.
/// </summary>
public static class AuraTierCeiling
{
    /// <summary>Placeholder base ceiling per evolution tier, at 0 Aptitude. TODO: pending NumericalCalibration.md.</summary>
    private const int BaseCeilingPerTier = 40;

    /// <summary>Placeholder ceiling increase per Aptitude point, flat regardless of tier. TODO: pending NumericalCalibration.md.</summary>
    private const int CeilingIncreasePerAptitudePoint = 4;

    public static int ComputeCeiling(int evolutionTier, int aptitude)
        => evolutionTier * BaseCeilingPerTier + aptitude * CeilingIncreasePerAptitudePoint;
}
