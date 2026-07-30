using System;

/// <summary>
/// 8-attribute value block. Mirrors StatType exactly. Authority: Evolution_System_Directive_v1_1_0.md.
/// </summary>
[Serializable]
public struct StatBlock
{
    public int Vitality;
    public int Force;
    public int Resonance;
    public int Guard;
    public int Ward;
    public int Resolve;
    public int Instinct;
    public int Aura;

    public static StatBlock Zero => new StatBlock();
    public int Total => Vitality + Force + Resonance + Guard + Ward + Resolve + Instinct + Aura;

    public StatBlock(int vitality, int force, int resonance, int guard,
                     int ward, int resolve, int instinct, int aura)
    {
        Vitality = vitality;
        Force = force;
        Resonance = resonance;
        Guard = guard;
        Ward = ward;
        Resolve = resolve;
        Instinct = instinct;
        Aura = aura;
    }

    public StatBlock Clone() =>
        new StatBlock(Vitality, Force, Resonance, Guard, Ward, Resolve, Instinct, Aura);

    public override string ToString() =>
        $"V:{Vitality} F:{Force} R:{Resonance} G:{Guard} W:{Ward} Rs:{Resolve} I:{Instinct} A:{Aura}";
}
