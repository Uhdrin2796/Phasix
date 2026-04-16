/// <summary>
/// Pending player-facing display name constants.
/// All UI code must reference these constants — never hardcode strings for pending names.
/// When a name is decided in a design session: update the constant value here.
/// The entire game updates automatically with no find-replace required.
///
/// Usage:
///   hudLabel.text = GameStrings.PoolName;
///   tooltipText = $"Your {GameStrings.PoolName} grows each time you devolve.";
/// </summary>
public static class GameStrings
{
    /// <summary>
    /// Player-facing label for the unnamed stat pool that persists through devolution.
    /// Grows by (excessStats × bondMultiplier) on each devolution cycle.
    /// TODO: pending naming session — update this constant when name is decided.
    /// </summary>
    public const string PoolName = "[POOL_NAME]";

    /// <summary>
    /// Player-facing term for individuals who can perceive Phasix.
    /// Used in NPC dialogue, UI descriptions, and tutorial text.
    /// TODO: pending naming session — update this constant when name is decided.
    /// </summary>
    public const string SensitivityName = "[SENSITIVITY_NAME]";
}
