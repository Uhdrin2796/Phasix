using System.Collections.Generic;

/// <summary>
/// Evaluators for the two NEW, user-directed combo rules (2026-08 session, see DECISIONS.md ->
/// [Combat]) — RepeatSameSkill and TimedInputStreak. Unlike ComboEngine.DetectCombo (GDD
/// §4.2-locked), these are not a GDD transcription; they're new mechanical design, only active
/// for a participant that has a skill equipped granting them (see SkillData.GrantsComboRule,
/// BattleParticipant.ActiveComboRules). Both reuse ComboTier (Duo/Trio/Quad) as the shared combo-
/// strength vocabulary so logging/UI stay uniform across all three rules, and both share
/// ComboEngine's Quad ceiling (window size 4) rather than inventing a different max.
/// </summary>
public static class ComboRuleEvaluator
{
    /// <summary>
    /// Detects the largest tier satisfied by the trailing end of recentSkillsUsed (most-recently-
    /// used skill last) — requires that many consecutive most-recent uses to all be `grantingSkill`
    /// SPECIFICALLY (2026-08 follow-up, user-directed: "the repeatsameskill only works on the
    /// [granting skill]" — not just "any skill repeated," the streak is tied to that one skill's
    /// identity). Using a different skill — including a different one repeated — anywhere in the
    /// window breaks it. `grantingSkill` is null-safe: a null grantingSkill (e.g. the rule somehow
    /// active with no resolvable granting skill) always returns null/0, never throws.
    /// </summary>
    public static ComboTier? EvaluateRepeatSameSkill(IReadOnlyList<SkillData> recentSkillsUsed, SkillData grantingSkill)
    {
        if (HasTrailingMatch(recentSkillsUsed, grantingSkill, 4)) return ComboTier.Quad;
        if (HasTrailingMatch(recentSkillsUsed, grantingSkill, 3)) return ComboTier.Trio;
        if (HasTrailingMatch(recentSkillsUsed, grantingSkill, 2)) return ComboTier.Duo;
        return null;
    }

    private static bool HasTrailingMatch(IReadOnlyList<SkillData> sequence, SkillData target, int windowSize)
    {
        if (target == null || sequence.Count < windowSize) return false;

        for (int i = sequence.Count - windowSize; i < sequence.Count; i++)
        {
            if (sequence[i] != target) return false;
        }
        return true;
    }

    /// <summary>
    /// Detects the largest tier satisfied by the trailing end of recentTimedInputPerfects — a
    /// combo requires that many consecutive most-recent timed inputs to all have been PERFECT
    /// (2026-08 follow-up, user-directed: "works with any other attacking skill that gets
    /// perfect, after a miss it rests" — a merely-successful-but-not-perfect hit does not extend
    /// the streak, same as an outright miss; see BattleParticipant.RecordTimedInputPerfect, fed
    /// from BattleHUDController.LastTimedInputWasPerfect, not LastTimedInputSuccess). Unlike
    /// RepeatSameSkill, this isn't tied to any one skill's identity — any equipped attacking
    /// skill's timed input counts, as long as the granting passive is equipped.
    /// </summary>
    public static ComboTier? EvaluateTimedInputStreak(IReadOnlyList<bool> recentTimedInputPerfects)
    {
        if (HasTrailingSuccessStreak(recentTimedInputPerfects, 4)) return ComboTier.Quad;
        if (HasTrailingSuccessStreak(recentTimedInputPerfects, 3)) return ComboTier.Trio;
        if (HasTrailingSuccessStreak(recentTimedInputPerfects, 2)) return ComboTier.Duo;
        return null;
    }

    private static bool HasTrailingSuccessStreak(IReadOnlyList<bool> sequence, int windowSize)
    {
        if (sequence.Count < windowSize) return false;

        for (int i = sequence.Count - windowSize; i < sequence.Count; i++)
        {
            if (!sequence[i]) return false;
        }
        return true;
    }

    /// <summary>Raw current trailing streak length (not capped at Quad) of `grantingSkill` specifically — drives the live skill-wheel combo-counter badge, same purpose as ComboEngine.GetDistinctTrailingStreakLength but for the repeat rule.</summary>
    public static int GetRepeatTrailingStreakLength(IReadOnlyList<SkillData> sequence, SkillData grantingSkill)
    {
        if (grantingSkill == null) return 0;

        int length = 0;
        for (int i = sequence.Count - 1; i >= 0; i--)
        {
            if (sequence[i] != grantingSkill) break;
            length++;
        }
        return length;
    }

    /// <summary>Raw current trailing perfect-timed-input streak length (not capped at Quad) — drives the live skill-wheel combo-counter badge, same purpose as ComboEngine.GetDistinctTrailingStreakLength but for the timed-input rule.</summary>
    public static int GetTimedInputTrailingStreakLength(IReadOnlyList<bool> recentTimedInputPerfects)
    {
        int length = 0;
        for (int i = recentTimedInputPerfects.Count - 1; i >= 0; i--)
        {
            if (!recentTimedInputPerfects[i]) break;
            length++;
        }
        return length;
    }
}
