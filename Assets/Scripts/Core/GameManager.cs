using UnityEngine;

/// <summary>
/// Central singleton anchor for Phasix game systems.
/// Skeleton only — no logic at this stage. Systems register themselves here
/// as they are built in later phases.
///
/// Inspector Setup:
///   1. Create an empty GameObject in SampleScene named "_GameManager"
///   2. Attach this script to it
///   3. The object persists across all scene loads automatically
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // TODO: Phase 3 — add reference to BattleManager when built
    // TODO: Phase 4 — add reference to SaveManager when built
    // TODO: Phase 4 — add reference to AuraManager when built
    // TODO: Phase 4 — add reference to EvolutionManager when built
}
