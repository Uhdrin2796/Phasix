/// <summary>
/// Single shared source of truth for "what short code represents this skill on an orb/node"
/// (2026-08-10 follow-up — user: "can we just make it in the battle scene that no names of
/// skills should be there? only... during the hover over of a skill... and the letter that the
/// skill has like C1, C2, etc."). Previously the Party menu's skill web/equip wheel already used
/// a short code (an earlier 2026-08 follow-up), but the battle scene's own skill ring still
/// displayed the full SkillData.SkillName as a permanent label — for a creature with a full
/// 12-skill loadout, several long placeholder names ("Utility Skill 1 (Placeholder)", etc.)
/// crowded around the small clock-face orbs and visibly overlapped. Both screens now call
/// through here — same pattern as SkillTreeColor for color — so they can't diverge again.
/// </summary>
public static class SkillLabelFormatter
{
    /// <summary>
    /// An already-short real name (e.g. "C1"/"C2", hand-renamed earlier in the project) passes
    /// through as-is; every other placeholder gets a generated `{tree-initial}{index-within-tree}`
    /// code — a pure display transform, never written back to the asset. Full identity/mechanics
    /// still live in the hover tooltip (BattleHUDController.BuildSkillTooltipText), unchanged.
    /// </summary>
    public static string GetShortLabel(SkillData skill, SkillDatabase database)
    {
        if (skill.SkillName.Length <= 3) return skill.SkillName;

        char treeInitial = char.ToUpperInvariant(skill.TreeType.ToString()[0]);

        // 'C' is reserved for hand-authored short names (C1/C2) — Corruption is the only tree
        // whose own initial is 'C', and without this override its first two skills would generate
        // "C1"/"C2" too, colliding with the real C1/C2 (live-verified: Corruption_Placeholder1
        // rendered identically to the real C1, which ComboRuleEvaluator's RepeatSameSkill rule
        // specifically references — not just a cosmetic clash, a genuinely different skill wearing
        // the real one's label).
        if (treeInitial == 'C') treeInitial = 'X';

        int indexInTree = 1;
        if (database != null)
        {
            var treeSkills = database.GetByTreeType(skill.TreeType);
            for (int i = 0; i < treeSkills.Count; i++)
            {
                if (treeSkills[i] == skill) { indexInTree = i + 1; break; }
            }
        }

        return $"{treeInitial}{indexInTree}";
    }
}
