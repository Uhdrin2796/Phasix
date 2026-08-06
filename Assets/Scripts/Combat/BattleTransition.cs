using System;
using UnityEngine.SceneManagement;

/// <summary>
/// Static bridge from the overworld into BattleScene_Main. Combat_Directive_v0_1_0.md Part 1:
/// "Scene loading uses additive loading for seamless transitions — overworld remains loaded
/// underneath combat" — so PartySystem.Instance (built on the overworld scene) stays valid and
/// reachable from BattleManager once BattleScene_Main is loaded on top of it. No cinematic
/// transition visual yet (Combat_Directive: "pending art direction") — this is a plain scene load.
/// </summary>
public static class BattleTransition
{
    private const string BattleSceneName = "BattleScene_Main";

    /// <summary>Set just before the additive load; read once by BattleManager.Start() in the new scene.</summary>
    public static PhasixRuntimeData PendingEnemy { get; private set; }

    private static Action<BattleResult> _onComplete;

    /// <summary>
    /// Starts a battle against a single wild Phasix. Multi-enemy encounters (trainer battles,
    /// Roadmap_v2 Mo 14-15) aren't built yet — every wild encounter is 1 enemy for this pass.
    /// onComplete fires once, right before the battle scene unloads.
    /// </summary>
    public static void StartWildBattle(PhasixRuntimeData enemy, Action<BattleResult> onComplete)
    {
        PendingEnemy = enemy;
        _onComplete = onComplete;
        SceneManager.LoadScene(BattleSceneName, LoadSceneMode.Additive);
    }

    /// <summary>Called by BattleManager once it has consumed PendingEnemy into its own BattleState.</summary>
    public static void ClearPending()
    {
        PendingEnemy = null;
    }

    /// <summary>Called by BattleManager when the battle ends. Fires and clears the stored callback.</summary>
    public static void CompleteBattle(BattleResult result)
    {
        var callback = _onComplete;
        _onComplete = null;
        callback?.Invoke(result);
    }
}
