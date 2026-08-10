using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Resolves a species/form to and from a stable GUID string for save-data purposes (2026-08
/// session, see DECISIONS.md -> [Save]). PhasixRuntimeData.speciesData is [NonSerialized] — a
/// save file can't hold a direct PhasixData reference, and AssetDatabase (the obvious way to look
/// one up by path) is Editor-only, unusable in a build or from save/load code that must also run
/// outside the Editor. Mirrors SkillDatabase.cs's exact pattern (same problem, same solution,
/// already proven): one asset, Inspector-populated with every PhasixData in the project, GUIDs
/// captured once via an Editor-only context menu, resolved at runtime with zero AssetDatabase
/// calls.
/// </summary>
[CreateAssetMenu(fileName = "SpeciesDatabase", menuName = "Phasix/Creature/Species Database", order = 2)]
public class SpeciesDatabase : ScriptableObject
{
    [Tooltip("Every PhasixData asset in the project, in any order. Drag all assets from Assets/Data/Species/ here, then run \"Rebuild GUID Index\".")]
    [SerializeField] private List<PhasixData> _allSpecies = new List<PhasixData>();

    [Tooltip("Parallel to _allSpecies — asset GUID at the same index. Populated by \"Rebuild GUID Index\", never hand-edited.")]
    [SerializeField] private List<string> _guids = new List<string>();

    private Dictionary<string, PhasixData> _byGuid;
    private Dictionary<PhasixData, string> _guidBySpecies;

    private void EnsureLookupsBuilt()
    {
        if (_byGuid != null) return;

        _byGuid = new Dictionary<string, PhasixData>();
        _guidBySpecies = new Dictionary<PhasixData, string>();

        int count = Mathf.Min(_allSpecies.Count, _guids.Count);
        for (int i = 0; i < count; i++)
        {
            PhasixData species = _allSpecies[i];
            string guid = _guids[i];
            if (species == null || string.IsNullOrEmpty(guid)) continue;

            _byGuid[guid] = species;
            _guidBySpecies[species] = guid;
        }
    }

    public bool TryGetByGuid(string guid, out PhasixData species)
    {
        EnsureLookupsBuilt();
        return _byGuid.TryGetValue(guid, out species);
    }

    /// <summary>Reverse lookup, used by SaveSystem.Save to turn a runtime speciesData reference into the GUID string the save file stores.</summary>
    public bool TryGetGuid(PhasixData species, out string guid)
    {
        EnsureLookupsBuilt();
        guid = null;
        return species != null && _guidBySpecies.TryGetValue(species, out guid);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only. Walks _allSpecies and writes the parallel _guids list from each asset's real
    /// project GUID. Run once after (re)populating _allSpecies in the Inspector — the result is
    /// committed as ordinary serialized data, so AssetDatabase is never called at runtime.
    /// </summary>
    [ContextMenu("Rebuild GUID Index")]
    private void RebuildGuidIndex()
    {
        _guids = new List<string>(_allSpecies.Count);
        foreach (PhasixData species in _allSpecies)
        {
            string path = species != null ? AssetDatabase.GetAssetPath(species) : null;
            _guids.Add(string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
        }

        _byGuid = null;
        _guidBySpecies = null;
        EditorUtility.SetDirty(this);
        Debug.Log($"SpeciesDatabase: rebuilt GUID index for {_guids.Count} entries.");
    }
#endif
}
