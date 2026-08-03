using UnityEngine;

/// <summary>
/// Personality mechanics: capture-time roll and item-based swap. Authority: GDD §7
/// (Locked v0.7.6).
///
/// RollRandom() has no weighting rule in the GDD — no capture system exists yet to call
/// it, so this assumes a uniform roll across all 18 traits until a designer says
/// otherwise. ChangePersonality() has no cost or restriction ("any personality to any
/// other," immediate, item consumed on use — GDD §7.2); consuming the triggering item is
/// the caller's responsibility, out of scope here pending the Item system (§22) — same
/// division of responsibility as Origin Change's bond-cost logic living outside
/// BondSystem.
///
/// Static class — no MonoBehaviour, no scene dependency — matching BondSystem.cs's pattern.
/// </summary>
public static class PersonalitySystem
{
    private static readonly Personality[] AllPersonalities =
        (Personality[])System.Enum.GetValues(typeof(Personality));

    /// <summary>Uniform random roll across all 18 traits. Used when a Phasix is captured (GDD §7.2, "shown on capture").</summary>
    public static Personality RollRandom()
    {
        return AllPersonalities[Random.Range(0, AllPersonalities.Length)];
    }

    /// <summary>
    /// Immediate, unconditional personality swap (GDD §7.2 — "any personality to any
    /// other," no spectrum restriction). No-op if already that personality.
    /// </summary>
    public static void ChangePersonality(PhasixRuntimeData phasix, Personality newPersonality)
    {
        if (phasix == null || phasix.personality == newPersonality) return;

        phasix.personality = newPersonality;
        EventBus.Raise_PersonalityChanged(phasix, newPersonality);
    }
}
