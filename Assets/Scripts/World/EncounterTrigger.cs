using UnityEngine;

/// <summary>
/// Spawn point marker for a wild Phasix — despite the name (kept for consistency with
/// CLAUDE.md's folder listing, World/ ← WorldChunkManager, EncounterTrigger, ZoneManager), this
/// does NOT detect the player itself; contact detection lives on WildEncounterCreature, matching
/// the real Combat_Directive contact-based model (a visible creature you walk into).
///
/// OnEnable (not Start) is deliberate: it re-fires every time this GameObject cycles active
/// again via WorldChunkManager's SetActive chunk toggling, letting a wild creature naturally
/// repopulate on revisit with no invented cooldown timer.
/// </summary>
public class EncounterTrigger : MonoBehaviour
{
    [Tooltip("Candidate species for this spawn point. One is picked at random each time a creature spawns here.")]
    [SerializeField] private PhasixData[] _possibleSpecies;

    [Tooltip("Assign Phasix_WildEncounter.prefab.")]
    [SerializeField] private GameObject _wildCreaturePrefab;

    [Tooltip("Assign the project's SkillDatabase asset — used to seed the spawned creature's starting skills (2026-08 session, see DECISIONS.md -> [Combat]). Optional: if left unassigned, the creature simply spawns with no skills seeded.")]
    [SerializeField] private SkillDatabase _skillDatabase;

    [Header("Debug Override (optional)")]
    [Tooltip("Debug-only scene dressing: if checked, overrides the spawned creature's in-world sprite tint instead of using the species' real PrimalType color. Does NOT change the encounter prompt UI swatch, which always reflects the species' actual PrimalType (PhasixPlaceholderVisual.SetColorOverride doc comment explains why). Not for real species content.")]
    [SerializeField] private bool _overrideTintColor;
    [SerializeField] private Color _tintColorOverride = Color.white;

    private GameObject _activeInstance;

    private void OnEnable()
    {
        if (_activeInstance != null) return;
        if (_possibleSpecies == null || _possibleSpecies.Length == 0) return;

        PhasixData species = _possibleSpecies[Random.Range(0, _possibleSpecies.Length)];
        PhasixRuntimeData runtimeData = WildSpawnSystem.CreateWildInstance(species, _skillDatabase);

        // Parented to this spawn point (not scene-root) — this is what ties the spawned
        // creature to WorldChunkManager's SetActive chunk toggling. An unparented Instantiate
        // would stay active even after its chunk deactivates, breaking the "repopulates on
        // revisit" design.
        _activeInstance = Instantiate(_wildCreaturePrefab, transform.position, transform.rotation, transform);

        var visual = _activeInstance.GetComponent<PhasixPlaceholderVisual>();
        visual.ApplyFromSpeciesData(species);
        if (_overrideTintColor) visual.SetColorOverride(_tintColorOverride);

        var creature = _activeInstance.GetComponent<WildEncounterCreature>();
        creature.SetRuntimeData(runtimeData);
    }
}
