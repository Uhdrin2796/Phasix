using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Resolves PhasixRuntimeData's learnedSkillGuids/equippedSkillGuids (stored as Unity asset GUID
/// strings, matching the save-data pattern in Evolution_System_Directive_v1_1_0.md) back to real
/// SkillData assets at runtime, and answers "which 2 placeholder skills does tree X have" for
/// bootstrap-seeding (WildSpawnSystem/DebugPartyBootstrap). One asset, Inspector-populated with
/// every SkillData asset in the project (currently the 36 placeholders in Assets/Data/Skills/).
///
/// GUIDs are captured once, in the Editor, via the "Rebuild GUID Index" context menu — never at
/// runtime, since AssetDatabase is Editor-only and unavailable in builds. The resulting _guids
/// list is ordinary serialized data after that, so runtime lookups never touch AssetDatabase.
/// </summary>
[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Phasix/Combat/Skill Database", order = 11)]
public class SkillDatabase : ScriptableObject
{
    [Tooltip("Every SkillData asset in the project, in any order. Drag all assets from Assets/Data/Skills/ here, then run \"Rebuild GUID Index\".")]
    [SerializeField] private List<SkillData> _allSkills = new List<SkillData>();

    [Tooltip("Parallel to _allSkills — asset GUID at the same index. Populated by \"Rebuild GUID Index\", never hand-edited.")]
    [SerializeField] private List<string> _guids = new List<string>();

    private Dictionary<string, SkillData> _byGuid;
    private Dictionary<SkillData, string> _guidBySkill;
    private Dictionary<SkillTreeType, List<SkillData>> _byTree;

    private void EnsureLookupsBuilt()
    {
        if (_byGuid != null) return;

        _byGuid = new Dictionary<string, SkillData>();
        _guidBySkill = new Dictionary<SkillData, string>();
        _byTree = new Dictionary<SkillTreeType, List<SkillData>>();

        int count = Mathf.Min(_allSkills.Count, _guids.Count);
        for (int i = 0; i < count; i++)
        {
            SkillData skill = _allSkills[i];
            string guid = _guids[i];
            if (skill == null || string.IsNullOrEmpty(guid)) continue;

            _byGuid[guid] = skill;
            _guidBySkill[skill] = guid;

            if (!_byTree.TryGetValue(skill.TreeType, out List<SkillData> list))
            {
                list = new List<SkillData>();
                _byTree[skill.TreeType] = list;
            }
            list.Add(skill);
        }
    }

    public bool TryGetByGuid(string guid, out SkillData skill)
    {
        EnsureLookupsBuilt();
        return _byGuid.TryGetValue(guid, out skill);
    }

    /// <summary>
    /// Every skill registered in this database, paired with its GUID (skipping any entry whose
    /// GUID never resolved — see RebuildGuidIndex). Added for the overworld Party menu's skill
    /// configurator (2026-08 follow-up — user: "all the other skills we have right now... should
    /// be displayed" as equip options, not just what a creature has already learned).
    /// </summary>
    public IEnumerable<(SkillData skill, string guid)> AllSkills
    {
        get
        {
            EnsureLookupsBuilt();
            foreach (KeyValuePair<string, SkillData> entry in _byGuid)
                yield return (entry.Value, entry.Key);
        }
    }

    /// <summary>Reverse lookup, used by bootstrap-seeding (WildSpawnSystem/DebugPartyBootstrap) to turn a resolved SkillData back into the GUID string PhasixRuntimeData's skill lists store.</summary>
    public bool TryGetGuid(SkillData skill, out string guid)
    {
        EnsureLookupsBuilt();
        guid = null;
        return skill != null && _guidBySkill.TryGetValue(skill, out guid);
    }

    /// <summary>Returns this tree's placeholder skills (expected: 2), or an empty list if none are registered.</summary>
    public IReadOnlyList<SkillData> GetByTreeType(SkillTreeType tree)
    {
        EnsureLookupsBuilt();
        return _byTree.TryGetValue(tree, out List<SkillData> list) ? list : System.Array.Empty<SkillData>();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only. Walks _allSkills and writes the parallel _guids list from each asset's real
    /// project GUID. Run once after (re)populating _allSkills in the Inspector — the result is
    /// committed as ordinary serialized data, so AssetDatabase is never called at runtime.
    /// </summary>
    [ContextMenu("Rebuild GUID Index")]
    private void RebuildGuidIndex()
    {
        _guids = new List<string>(_allSkills.Count);
        foreach (SkillData skill in _allSkills)
        {
            string path = skill != null ? AssetDatabase.GetAssetPath(skill) : null;
            _guids.Add(string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
        }

        _byGuid = null;
        _byTree = null;
        EditorUtility.SetDirty(this);
        Debug.Log($"SkillDatabase: rebuilt GUID index for {_guids.Count} entries.");
    }
#endif
}
