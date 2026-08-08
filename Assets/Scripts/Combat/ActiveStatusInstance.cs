/// <summary>
/// One currently-active status effect on a BattleParticipant — plain data, ticked/removed by
/// BattleParticipant.TickStatuses (2026-08 session, see DECISIONS.md -> [Combat]). Not a
/// ScriptableObject or MonoBehaviour — matches the Hard Architecture Rule (runtime state is
/// plain C#, never written to an SO).
/// </summary>
public class ActiveStatusInstance
{
    public StatusEffectType Type;
    public int TurnsRemaining;

    public ActiveStatusInstance(StatusEffectType type, int turnsRemaining)
    {
        Type = type;
        TurnsRemaining = turnsRemaining;
    }
}
