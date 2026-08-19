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

    [Tooltip("DEBUG (2026-08-12, extended 2026-08-17): if checked, the spawned wild creature's entire loadout is replaced with the built-in Attack move + the 'Slash' skill + the 'Flame Breath' skill (Sustained Pressure), guaranteeing every enemy turn can trigger the old single-beat Dodge/Parry flow (Attack), the Melee Beat Sequence framework (Slash), or the new hold-to-guard archetype (Flame Breath) so all three can be reliably playtested and compared side by side. Flame Breath was added here rather than a new debug flag, since Sustained Pressure MUST be exercised live (unlike Multi-Hit Volley's deferred defense side) and this is this project's one existing enemy-side debug override mechanism. See WildSpawnSystem.ApplyDebugSkillsOverride. Not for real species content — leave unchecked once the framework is done being tested.")]
    [SerializeField] private bool _debugForceAttackAndSlash = true;

    private GameObject _activeInstance;

    private void OnEnable()
    {
        if (_activeInstance != null) return;
        if (_possibleSpecies == null || _possibleSpecies.Length == 0) return;

        PhasixData species = _possibleSpecies[Random.Range(0, _possibleSpecies.Length)];
        PhasixRuntimeData runtimeData = WildSpawnSystem.CreateWildInstance(species, _skillDatabase);

        // KNOWN_ISSUES.md [DEBUG-001] fix (was missing entirely — the checkbox looked functional
        // but silently did nothing): resolves Attack by BuiltInMoveType (its SkillName is the short
        // label "A", not "Attack" — same lookup style GameManager.ApplyDebugPlaytestLoadout already
        // uses for the player side) and Slash by SkillName, then force-equips both together.
        if (_debugForceAttackAndSlash && _skillDatabase != null)
        {
            var guids = new System.Collections.Generic.List<string>();
            foreach ((SkillData skill, string guid) in _skillDatabase.AllSkills)
            {
                if (skill.BuiltInMove == BuiltInMoveType.Attack) { guids.Add(guid); break; }
            }
            foreach ((SkillData skill, string guid) in _skillDatabase.AllSkills)
            {
                if (skill.SkillName == "Slash") { guids.Add(guid); break; }
            }
            // 2026-08-17: Sustained Pressure ("Flame Breath") — must be exercised live against a
            // real player, unlike Multi-Hit Volley's deliberately-deferred defense side, so it's
            // added to this existing enemy-side debug override rather than needing a new mechanism.
            foreach ((SkillData skill, string guid) in _skillDatabase.AllSkills)
            {
                if (skill.SkillName == "Flame Breath") { guids.Add(guid); break; }
            }
            WildSpawnSystem.ApplyDebugSkillsOverride(runtimeData, _skillDatabase, guids);
        }

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
