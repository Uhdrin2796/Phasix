/// <summary>
/// Builds human-readable battle log lines from a resolved attack — damage dealt, Primal type
/// effectiveness, and whether the action-command timing succeeded. Offensive attacks (the player's
/// own swing, and the automatic counter-attack on a successful Parry) go through FormatAttack;
/// attacks the target tried to Dodge or Parry go through FormatDefenseOutcome — a successful
/// defense reads completely differently (no damage line at all) than a normal attack, so these
/// aren't a single parameterized method. Pure static text formatting, no MonoBehaviour/scene
/// dependency, easy to EditMode-test.
/// </summary>
public static class BattleLogFormatter
{
    // Damage-breakdown colors (2026-08-11, user-directed — temporary "for visibility rn" aid, not
    // permanent flavor). White matches BattleHUD.uss's ambient log text closely enough to read as
    // "neutral"; green/red are the exact same hex values as BattleHUDController's own
    // SuccessFlashColor/MissFlashColor, so an "increase" here reads the same color as a Good/Perfect
    // ring flash, and a "decrease" reads the same as a Miss flash — one consistent color language.
    private const string BaseDamageColor = "#FFFFFF";
    private const string IncreaseColor = "#5AC864";
    private const string DecreaseColor = "#DC3C3C";

    /// <summary>
    /// Formats an offensive attack — the player's own swing, or an automatic Parry counter-attack.
    /// offenseOutcome is null for the Parry counter-attack (no timing check runs for it at all);
    /// otherwise it's whichever of Miss/Good/Perfect the action-command ring landed on. Only the
    /// two tiers that deviate from baseline get flavor text — Good is the new "standard" damage
    /// (TimedInputConfig.GoodDamageMultiplier, 1.0x) and stays silent, same as a null outcome
    /// (2026-08-11, second pass — see DECISIONS.md -> [Combat]).
    /// </summary>
    public static string FormatAttack(BattleParticipant attacker, BattleParticipant target, int pureBaseDamage, int damageAfterType, int finalDamage, float typeMultiplier, BattleHUDController.OffenseOutcome? offenseOutcome)
    {
        (int, string)? timingTerm = offenseOutcome.HasValue ? (finalDamage - damageAfterType, "timing") : ((int, string)?)null;
        string line = $"{attacker.DisplayName} attacks {target.DisplayName} for {FormatDamageBreakdown(pureBaseDamage, damageAfterType, finalDamage, timingTerm)}!";

        string effectiveness = FormatEffectiveness(typeMultiplier);
        if (!string.IsNullOrEmpty(effectiveness)) line += $" {effectiveness}";

        if (offenseOutcome == BattleHUDController.OffenseOutcome.Perfect) line += $" {attacker.DisplayName}'s timing was perfect — critical hit!";
        else if (offenseOutcome == BattleHUDController.OffenseOutcome.Miss) line += $" {attacker.DisplayName}'s timing was off, weakening the blow!";

        return line;
    }

    /// <summary>
    /// Formats an enemy attack the target tried to Dodge or Parry (Combat_Directive Part 4,
    /// full-avoidance model). A successful defense fully avoids the hit — no damage line — while a
    /// failed one plays out exactly like a normal attack, same as a miss on offense: "reward, don't
    /// punish," no extra penalty text for a failed Dodge/Parry attempt. No timing-multiplier term in
    /// the breakdown — an incoming hit that lands is always full (1x) damage, only Dodge/Parry's
    /// full-avoidance is at stake, not a graduated bonus/penalty like the player's own timing.
    /// </summary>
    public static string FormatDefenseOutcome(BattleParticipant attacker, BattleParticipant target, int pureBaseDamage, int damageAfterType, int finalDamage, float typeMultiplier, bool avoided, bool attemptedParry)
    {
        if (avoided)
        {
            return attemptedParry
                ? $"{target.DisplayName} parries {attacker.DisplayName}'s attack — opening for a counter!"
                : $"{target.DisplayName} dodges {attacker.DisplayName}'s attack!";
        }

        string line = $"{attacker.DisplayName} attacks {target.DisplayName} for {FormatDamageBreakdown(pureBaseDamage, damageAfterType, finalDamage, extraTermOrNull: null)}!";
        string effectiveness = FormatEffectiveness(typeMultiplier);
        if (!string.IsNullOrEmpty(effectiveness)) line += $" {effectiveness}";
        return line;
    }

    /// <summary>
    /// Builds the "(N base + delta type [+ delta &lt;label&gt;]) = N total damage" breakdown segment
    /// (2026-08-11, user-directed: "show the (base damage + type advantage damage + timing bonus
    /// damage)... base damage is white, decreased damage is red, and increased damage is green...
    /// also show the total damage" — a temporary "for visibility rn" aid, not permanent flavor
    /// text). typeDelta/the third term are always exactly consistent with finalDamage by
    /// construction — each is defined as the difference between two already-resolved damage
    /// numbers, not independently computed, so the terms always sum to finalDamage regardless of
    /// how any individual step rounded internally. extraTermOrNull is null when there's no third
    /// multiplier to explain at all (the Parry counter-attack, or any incoming enemy hit) — that
    /// term is omitted entirely rather than shown as a meaningless "+0".
    ///
    /// 2026-08-14 follow-up (Metronome/Jitter beat-stack tier multiplier, user: "make sure that the
    /// battlelog updates the damage log on metronome and jitter. It shows the correct total value
    /// but i need to see the broken out values more clearly") — generalized the third term from a
    /// hardcoded "timing" label to any caller-supplied (delta, label) pair, so the same breakdown
    /// can explain either a timed-input bonus (offense/skill attacks) or a stacking-rhythm tier
    /// bonus (FormatStackingRhythmAttack) instead of silently folding the tier multiplier into the
    /// total with nothing accounting for it.
    /// </summary>
    private static string FormatDamageBreakdown(int pureBaseDamage, int damageAfterType, int finalDamage, (int delta, string label)? extraTermOrNull)
    {
        int typeDelta = damageAfterType - pureBaseDamage;
        string breakdown = $"(<color={BaseDamageColor}>{pureBaseDamage} base</color> {FormatDeltaTerm(typeDelta, "type")}";
        if (extraTermOrNull.HasValue) breakdown += $" {FormatDeltaTerm(extraTermOrNull.Value.delta, extraTermOrNull.Value.label)}";
        breakdown += $") = {finalDamage} total damage";
        return breakdown;
    }

    /// <summary>Formats one signed, colored term of the damage breakdown — green "+ N label" for an increase, red "- N label" for a decrease, white "+ 0 label" for no change.</summary>
    private static string FormatDeltaTerm(int delta, string label)
    {
        if (delta > 0) return $"+ <color={IncreaseColor}>{delta} {label}</color>";
        if (delta < 0) return $"- <color={DecreaseColor}>{-delta} {label}</color>";
        return $"+ <color={BaseDamageColor}>0 {label}</color>";
    }

    /// <summary>Maps a Primal type multiplier to flavor text. Empty string for neutral (1.0x) — no note needed.</summary>
    private static string FormatEffectiveness(float multiplier)
    {
        if (multiplier >= 2.0f) return "It's super effective!";
        if (multiplier > 1.0f) return "It's effective.";
        if (multiplier >= 1.0f) return "";
        if (multiplier > 0.5f) return "It's not very effective...";
        return "It's barely effective...";
    }

    /// <summary>
    /// Formats a placeholder skill-ring attack (2026-08 session — Combo/Status/Chain/Mastery
    /// wiring, see DECISIONS.md -> [Combat]). Same shape as FormatAttack but names the skill used,
    /// since a skill attack isn't the generic built-in "Attack" move. Previously omitted the
    /// action-command outcome entirely — fixed 2026-08-11 (see DECISIONS.md -> [Combat]) since
    /// damage-dealing skills run the exact same timed-input check as the basic Attack and swing by
    /// the same 0.5x-2.0x range; the log should explain why, same as FormatAttack now does.
    /// </summary>
    public static string FormatSkillAttack(BattleParticipant attacker, BattleParticipant target, string skillName, int pureBaseDamage, int damageAfterType, int finalDamage, float typeMultiplier, BattleHUDController.OffenseOutcome offenseOutcome)
    {
        (int, string) timingTerm = (finalDamage - damageAfterType, "timing");
        string line = $"{attacker.DisplayName} uses {skillName} on {target.DisplayName} for {FormatDamageBreakdown(pureBaseDamage, damageAfterType, finalDamage, timingTerm)}!";

        string effectiveness = FormatEffectiveness(typeMultiplier);
        if (!string.IsNullOrEmpty(effectiveness)) line += $" {effectiveness}";

        if (offenseOutcome == BattleHUDController.OffenseOutcome.Perfect) line += $" {attacker.DisplayName}'s timing was perfect — critical hit!";
        else if (offenseOutcome == BattleHUDController.OffenseOutcome.Miss) line += $" {attacker.DisplayName}'s timing was off, weakening the blow!";

        return line;
    }

    /// <summary>Formats a status-effect application from a placeholder skill.</summary>
    public static string FormatStatusApplied(BattleParticipant target, StatusEffectType status, int durationTurns)
    {
        return $"{target.DisplayName} is afflicted with {status} for {durationTurns} turns!";
    }

    /// <summary>
    /// Formats a detected combo (2026-08 session — new, user-directed mechanic on top of
    /// ComboEngine's GDD-locked cross-tree rule; see ComboRuleType/DECISIONS.md -> [Combat]).
    /// Detection + log only — no numeric bonus is applied for any rule type.
    /// </summary>
    public static string FormatComboDetected(BattleParticipant attacker, ComboTier tier, ComboRuleType rule)
    {
        string ruleFlavor = rule switch
        {
            ComboRuleType.RepeatSameSkill => "repeating the same skill",
            ComboRuleType.TimedInputStreak => "a streak of perfect timing",
            _ => "chaining different skill trees",
        };

        return $"{attacker.DisplayName} triggers a {tier} combo — {ruleFlavor}!";
    }

    /// <summary>Formats a newly-triggered chain result (GDD §17.8-locked flavor text) — caller only invokes this on a *change*, not every turn the same pair of statuses stays active.</summary>
    public static string FormatChainResultTriggered(BattleParticipant target, ChainResultType chain)
    {
        return $"{target.DisplayName}'s statuses combine into {chain}! {ChainResultCatalog.GetEffectDescription(chain)}";
    }

    /// <summary>Formats a newly-triggered mastery bonus (GDD §17.9-locked flavor text) — caller only invokes this once per bonus per battle.</summary>
    public static string FormatMasteryBonusTriggered(BattleParticipant attacker, MasteryBonusType bonus)
    {
        return $"{attacker.DisplayName} achieves {bonus}! {MasteryBonusCatalog.GetTriggerDescription(bonus)} {MasteryBonusCatalog.GetEffectDescription(bonus)}";
    }

    /// <summary>
    /// Formats a Metronome/Jitter stacking-rhythm attack that cleared every required beat (2026-08-13
    /// — BattleManager.ResolveStackingRhythmAttack) — names the tier/beat count just cleared and the
    /// resulting damage multiplier alongside the normal breakdown, so the ramping stack is visible in
    /// the log, not just implied by a bigger number.
    ///
    /// 2026-08-14 fix (user: "make sure that the battlelog updates the damage log on metronome and
    /// jitter. It shows the correct total value but i need to see the broken out values more
    /// clearly") — this used to pass extraTermOrNull: null, so the breakdown only ever showed
    /// "base + type" even though finalDamage also includes the tier multiplier's own contribution
    /// (BattleManager applies damageMultiplier: tierMultiplier when queuing the hit) — the base+type
    /// terms silently stopped summing to the shown total once a stack tier above 1 kicked in, with
    /// nothing in the line explaining the gap. Now computes that gap explicitly (finalDamage minus
    /// the pre-tier damageAfterType) and passes it as a labeled "stack" term, same mechanism
    /// FormatAttack/FormatSkillAttack already use for their "timing" term — the three terms now
    /// always sum to the displayed total.
    /// </summary>
    public static string FormatStackingRhythmAttack(BattleParticipant attacker, BattleParticipant target, string skillName, int beatsCleared, float damageMultiplier, int pureBaseDamage, int damageAfterType, int finalDamage, float typeMultiplier)
    {
        string beatWord = beatsCleared == 1 ? "beat" : "beats";
        (int, string) stackTerm = (finalDamage - damageAfterType, "stack");
        string line = $"{attacker.DisplayName}'s {skillName} lands (Tier {beatsCleared} — {beatsCleared} {beatWord} cleared, {damageMultiplier:0.0#}x) on {target.DisplayName} for {FormatDamageBreakdown(pureBaseDamage, damageAfterType, finalDamage, stackTerm)}!";

        string effectiveness = FormatEffectiveness(typeMultiplier);
        if (!string.IsNullOrEmpty(effectiveness)) line += $" {effectiveness}";

        return line;
    }

    /// <summary>Formats a Metronome/Jitter combo broken mid-sequence — no damage; the stack tier stays exactly where it was (BattleParticipant.AdvanceStackingRhythmTier is not called for this outcome).</summary>
    public static string FormatStackingRhythmWhiff(BattleParticipant attacker, string skillName, int beatReached, int beatsRequired)
    {
        return $"{attacker.DisplayName}'s {skillName} breaks rhythm on beat {beatReached}/{beatsRequired} — the attack whiffs!";
    }

    /// <summary>
    /// Formats one hit of a Multi-Hit Volley (2026-08-14, BattleManager.RunVolleyHit) — same shape
    /// as FormatSkillAttack but names which hit of the sequence this is, since (unlike a normal
    /// skill, or Metronome/Jitter's single combined payoff) each Volley hit gets its own log line
    /// and its own independent Miss/Good/Perfect outcome. Callers collect these into an array and
    /// flush them to the battle log together once the whole cast resolves (user: "let the damage
    /// calculate on ring input, then for the battle log just add them all at the end") rather than
    /// appending as each hit completes — this method only builds the string, it doesn't log it.
    /// </summary>
    public static string FormatVolleyHit(BattleParticipant attacker, BattleParticipant target, string skillName,
        int hitNumber, int hitCount, int pureBaseDamage, int damageAfterType, int finalDamage, float typeMultiplier,
        BattleHUDController.OffenseOutcome offenseOutcome)
    {
        (int, string) timingTerm = (finalDamage - damageAfterType, "timing");
        string line = $"{attacker.DisplayName}'s {skillName} hit {hitNumber}/{hitCount} strikes {target.DisplayName} for {FormatDamageBreakdown(pureBaseDamage, damageAfterType, finalDamage, timingTerm)}!";

        string effectiveness = FormatEffectiveness(typeMultiplier);
        if (!string.IsNullOrEmpty(effectiveness)) line += $" {effectiveness}";

        if (offenseOutcome == BattleHUDController.OffenseOutcome.Perfect) line += " Perfect timing!";
        else if (offenseOutcome == BattleHUDController.OffenseOutcome.Miss) line += " Timing was off.";

        return line;
    }
}
