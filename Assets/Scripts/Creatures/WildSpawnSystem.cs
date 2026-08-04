using System;
using UnityEngine;

/// <summary>
/// Builds PhasixRuntimeData for a wild encounter. Static class — no MonoBehaviour, no scene
/// dependency — matching PersonalitySystem.cs's pattern. Extracted because every future spawn
/// point needs identical construction logic, not a one-off.
///
/// Wk 14-16 scaffold: currentNodeGuid is a placeholder GUID (EvolutionGraphSO doesn't exist yet,
/// Phase 4). origin is set directly to OriginType.Wild — true by definition for a wild
/// encounter, no roll needed.
/// </summary>
public static class WildSpawnSystem
{
    public static PhasixRuntimeData CreateWildInstance(PhasixData species)
    {
        var runtime = new PhasixRuntimeData(Guid.NewGuid().ToString());
        runtime.speciesData = species;
        runtime.origin = OriginType.Wild;
        runtime.personality = PersonalitySystem.RollRandom();
        runtime.baseStats = new StatBlock(species.Vitality, species.Force, species.Resonance,
            species.Guard, species.Ward, species.Resolve, species.Instinct, species.Aura);

        if (species.SignalPool != null && species.SignalPool.Length > 0)
            runtime.activeSignalType = species.SignalPool[UnityEngine.Random.Range(0, species.SignalPool.Length)];

        return runtime;
    }
}
