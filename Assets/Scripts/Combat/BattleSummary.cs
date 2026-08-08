/// <summary>
/// Read-only recap of one battle, shown by BattleSummaryController after a win (2026-08 session —
/// replaces the old spend-here-and-now Aura Allocation screen; see DECISIONS.md -> [Combat]).
/// Built by BattleManager.EndBattle from running totals accumulated during the fight — plain
/// data, no logic of its own. Aura spending itself moved to the new Tab-key overworld menu
/// (PartyMenuController) — this screen is informational only.
/// </summary>
public class BattleSummary
{
    public int TotalDamageDealt;
    public int TotalHealingDone;
    public int TotalAuraGained;
}
