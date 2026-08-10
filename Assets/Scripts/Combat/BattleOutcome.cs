/// <summary>
/// Result of BattleEngine.CheckOutcome — drives the CheckWinLoss state transition. Fled is never
/// returned by CheckOutcome itself (HP-based win/loss only) — BattleManager.PlayerTurn passes it
/// to EndBattle directly on a successful flee attempt, same manual-outcome pattern Capture already
/// uses for BattleOutcome.Won (2026-08-10, see DECISIONS.md -&gt; [Combat]).
/// </summary>
public enum BattleOutcome { InProgress, Won, Lost, Fled }
