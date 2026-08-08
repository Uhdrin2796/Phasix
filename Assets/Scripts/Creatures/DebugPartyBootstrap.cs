using UnityEngine;

/// <summary>
/// TEMPORARY manual-test hook — adds one Phasix to the party on Play start so
/// CompanionAI/PartySystem can be tested with real keyboard input before any real
/// capture system exists (that's Phase 3, Mo 8 Wk 3 per Roadmap_v2.md).
///
/// DELETE THIS FILE once a real capture flow exists and calls PartySystem.AddToParty()
/// itself — this is scaffolding for manual verification only, not a real game system.
/// </summary>
public class DebugPartyBootstrap : MonoBehaviour
{
    [Tooltip("Test PhasixData to spawn into the party on Play start. Assign a test asset from Assets/Data/Species/.")]
    [SerializeField] private PhasixData _testSpeciesData;

    [Tooltip("Assign the project's SkillDatabase asset — used to seed the test companion's starting skills (2026-08 session, see DECISIONS.md -> [Combat]). Optional: if left unassigned, the companion simply spawns with no skills seeded.")]
    [SerializeField] private SkillDatabase _skillDatabase;

    private void Start()
    {
        if (_testSpeciesData == null)
        {
            Debug.LogWarning("[DebugPartyBootstrap] No test PhasixData assigned — skipping.");
            return;
        }

        var runtime = new PhasixRuntimeData("debug-test-companion");
        runtime.speciesData = _testSpeciesData;
        // Without this, baseStats defaults to all-zero (StatBlock is a struct) and
        // BattleParticipant.MaxHP clamps to 1 — the companion would die in one hit in any battle
        // playtest. Mirrors WildSpawnSystem.CreateWildInstance's identical line.
        runtime.baseStats = new StatBlock(_testSpeciesData.Vitality, _testSpeciesData.Force, _testSpeciesData.Resonance,
            _testSpeciesData.Guard, _testSpeciesData.Ward, _testSpeciesData.Resolve, _testSpeciesData.Instinct, _testSpeciesData.Aura);
        WildSpawnSystem.SeedInitialSkills(runtime, _testSpeciesData, _skillDatabase);

        int slot = PartySystem.Instance.AddToParty(runtime);
        Debug.Log($"[DebugPartyBootstrap] Added test Phasix ({_testSpeciesData.SpeciesName}) to party slot {slot}.");
    }
}
