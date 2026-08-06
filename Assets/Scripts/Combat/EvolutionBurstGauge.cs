/// <summary>
/// Per-battle-participant mid-battle evolution burst state (GDD §9.3 "Bond Gauge and Evolution
/// Burst", Type K / SkillTreeType.Evolve). Plain data — EvolutionBurstSystem owns the rules.
/// </summary>
public class EvolutionBurstGauge
{
    public float FillPercent;
    public bool IsActive;
    public int RemainingDurationTurns;
}
