using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Single shared source of truth for "what color represents this skill tree" (2026-08-09 follow-up
/// — user, after the Party menu's skill web already used per-tree color: "lets have the skill tree
/// be the master color... on the wheel should match", then, once the equip wheel and web agreed,
/// "i want the skill wheel in skill tree menu to sync up with the battle scene"). Both
/// OverworldMenuController's skill web/equip wheel AND BattleHUDController's skill ring call
/// through here — neither has its own independent color logic anymore, so they structurally can't
/// diverge again the way they did twice before (once between the web and the Party menu's own
/// wheel, once between the Party menu and the battle scene).
///
/// Previously each screen had its own scheme: the Party menu's equip wheel used a 7-bucket
/// per-SKILL GUID hash (BattleHUD.uss's .skill-ring-color-0..6), the battle skill ring used a
/// 7-bucket per-RING-POSITION scheme (same USS classes, different assignment), and the skill web
/// used this per-TREE procedural color. All three are now this one method.
/// </summary>
public static class SkillTreeColor
{
    /// <summary>
    /// Testing first (2026-08-12 follow-up — Attack Pattern Directive proof-of-concept skills,
    /// user: "put it to the left of the skill tree that contains the standard skill tree"), then
    /// Standard (5 always-available built-in moves, never tier-gated), then the 18 GDD tree types in
    /// PhasixEnums.cs declaration order. Also the skill web's column display order
    /// (OverworldMenuController) — one canonical order for both layout and color. Both Testing and
    /// Standard are non-GDD, always-unlocked columns — OverworldMenuController's two DisplayOrder
    /// loops (ApplyDefaultFraming/RefreshSkillArea) both need their existing
    /// `== SkillTreeType.Standard` unlock-exemption check extended to `|| == SkillTreeType.Testing`,
    /// same as SkillTreeUnlockSystem/SkillLoadoutSystem already do.
    /// </summary>
    public static readonly SkillTreeType[] DisplayOrder =
    {
        SkillTreeType.Testing,
        SkillTreeType.Standard,
        SkillTreeType.Utility, SkillTreeType.Aura, SkillTreeType.Passive, SkillTreeType.Synergy,
        SkillTreeType.Reaction, SkillTreeType.Bond, SkillTreeType.Aspect, SkillTreeType.Resource,
        SkillTreeType.Corruption, SkillTreeType.Mirror, SkillTreeType.Evolve, SkillTreeType.Territory,
        SkillTreeType.Memory, SkillTreeType.Fusion, SkillTreeType.Personality, SkillTreeType.Typing,
        SkillTreeType.Bastion, SkillTreeType.Phantom,
    };

    private static readonly Color BaseTint = new Color(0.18f, 0.18f, 0.2f, 1f);

    /// <summary>Deterministic per-tree color — HSV hue rotated by the golden-angle conjugate per DisplayOrder index, so adjacent trees land on visually distinct hues instead of a smooth gradient. Procedural rather than a fixed enumerated USS class list, so it scales to all 19 without hand-authored classes.</summary>
    public static Color Get(SkillTreeType treeType)
    {
        int index = System.Array.IndexOf(DisplayOrder, treeType);
        return GetByIndex(index < 0 ? 0 : index);
    }

    public static Color GetByIndex(int displayOrderIndex)
    {
        float hue = (displayOrderIndex * 0.6180339887f) % 1f;
        return Color.HSVToRGB(hue, 0.55f, 0.95f);
    }

    /// <summary>Applies (or clears, if treeType is null — an empty/locked slot) the tint+border visual for a skill's owning tree, identically wherever a skill is shown: the Party menu's skill web, its equip wheel, and the battle scene's skill ring.</summary>
    public static void ApplyVisual(VisualElement element, SkillTreeType? treeType)
    {
        if (!treeType.HasValue)
        {
            element.style.backgroundColor = StyleKeyword.Null;
            element.style.borderTopColor = StyleKeyword.Null;
            element.style.borderBottomColor = StyleKeyword.Null;
            element.style.borderLeftColor = StyleKeyword.Null;
            element.style.borderRightColor = StyleKeyword.Null;
            return;
        }

        Color treeColor = Get(treeType.Value);
        Color tint = Color.Lerp(BaseTint, treeColor, 0.35f);
        element.style.backgroundColor = tint;
        element.style.borderTopColor = treeColor;
        element.style.borderBottomColor = treeColor;
        element.style.borderLeftColor = treeColor;
        element.style.borderRightColor = treeColor;
    }
}
