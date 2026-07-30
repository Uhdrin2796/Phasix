using UnityEngine;

/// <summary>
/// Minimal stub — individual skill content is pending the skill design phase (GDD §14,
/// requires the full species roster). Exists only so PhasixRuntimeData's skill lists and
/// EventBus.OnSkillUsed compile. Do not flesh out skill content here.
/// TODO: pending design — skill content (GDD §14)
/// </summary>
[CreateAssetMenu(fileName = "New SkillData", menuName = "Phasix/Combat/Skill Data (Stub)", order = 10)]
public class SkillData : ScriptableObject
{
    [Header("Stub — pending full skill design")]
    [SerializeField] private string _skillName;
    [SerializeField] [TextArea] private string _description;
    [SerializeField] private SkillTreeType _treeType;

    public string SkillName => _skillName;
    public string Description => _description;
    public SkillTreeType TreeType => _treeType;
}
