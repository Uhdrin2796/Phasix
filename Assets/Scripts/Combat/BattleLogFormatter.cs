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
    /// <summary>Formats an offensive attack — the player's own swing, or an automatic Parry counter-attack.</summary>
    public static string FormatAttack(BattleParticipant attacker, BattleParticipant target, int damage, float typeMultiplier, bool timedInputSuccess)
    {
        string line = $"{attacker.DisplayName} attacks {target.DisplayName} for {damage} damage!";

        string effectiveness = FormatEffectiveness(typeMultiplier);
        if (!string.IsNullOrEmpty(effectiveness)) line += $" {effectiveness}";

        if (timedInputSuccess) line += $" {attacker.DisplayName}'s timing was perfect!";

        return line;
    }

    /// <summary>
    /// Formats an enemy attack the target tried to Dodge or Parry (Combat_Directive Part 4,
    /// full-avoidance model). A successful defense fully avoids the hit — no damage line — while a
    /// failed one plays out exactly like a normal attack, same as a miss on offense: "reward, don't
    /// punish," no extra penalty text for a failed Dodge/Parry attempt.
    /// </summary>
    public static string FormatDefenseOutcome(BattleParticipant attacker, BattleParticipant target, int damage, float typeMultiplier, bool avoided, bool attemptedParry)
    {
        if (avoided)
        {
            return attemptedParry
                ? $"{target.DisplayName} parries {attacker.DisplayName}'s attack — opening for a counter!"
                : $"{target.DisplayName} dodges {attacker.DisplayName}'s attack!";
        }

        string line = $"{attacker.DisplayName} attacks {target.DisplayName} for {damage} damage!";
        string effectiveness = FormatEffectiveness(typeMultiplier);
        if (!string.IsNullOrEmpty(effectiveness)) line += $" {effectiveness}";
        return line;
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
}
