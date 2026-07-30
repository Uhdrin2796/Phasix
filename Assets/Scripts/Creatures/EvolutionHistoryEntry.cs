using System;

/// <summary>
/// One entry in a creature's personal evolution history stack. Index 0 = oldest,
/// last index = most recent previous form. Authority: Evolution_System_Directive_v1_1_0.md.
/// </summary>
[Serializable]
public class EvolutionHistoryEntry
{
    /// <summary>GUID of the EvolutionNodeSO this creature was at before evolving.</summary>
    public string nodeGuid;

    /// <summary>
    /// The stat floor that applied at this tier (used to reset base stats on devolution).
    /// Snapshot taken at the moment of evolution.
    /// </summary>
    public StatBlock tierFloor;

    public EvolutionHistoryEntry(string nodeGuid, StatBlock tierFloor)
    {
        this.nodeGuid = nodeGuid;
        this.tierFloor = tierFloor;
    }
}
