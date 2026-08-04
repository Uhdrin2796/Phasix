using UnityEngine;

/// <summary>
/// Lives on the wild creature itself (Phasix_WildEncounter.prefab), not on the spawn point —
/// detection is contact-based (Combat_Directive_v0_1_0.md: "When the player's overworld sprite
/// contacts an enemy Phasix sprite..."), matching the real Pokemon/Digimon-style model rather
/// than an invisible trigger zone. Wk 14-16 scaffold: Engage has no BattleManager to hand off to
/// yet (Phase 3), so it resolves identically to Flee.
///
/// No Rigidbody2D needed here — the player's own Dynamic Rigidbody2D is enough to drive 2D
/// trigger detection between the two colliders.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WildEncounterCreature : MonoBehaviour
{
    private PhasixRuntimeData _runtimeData;
    private bool _contacted;

    /// <summary>Assigned by EncounterTrigger immediately after Instantiate.</summary>
    public void SetRuntimeData(PhasixRuntimeData runtimeData)
    {
        _runtimeData = runtimeData;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_contacted) return;
        if (!other.TryGetComponent<PlayerController_SideScroll>(out var player)) return;

        // Guards against two encounters resolving in the same physics step and clobbering
        // each other's Show() callbacks — only one spawn point exists today so this can't
        // happen organically yet, but the guard is cheap and the failure mode (a soft-locked
        // second creature) isn't.
        if (EncounterPromptController.Instance.IsVisible) return;

        _contacted = true;

        player.FreezeMovement();
        EventBus.Raise_WildEncounterTriggered(_runtimeData);
        EncounterPromptController.Instance.Show(_runtimeData.speciesData, () => HandleFlee(player), () => HandleEngage(player));
    }

    private void HandleFlee(PlayerController_SideScroll player)
    {
        EventBus.Raise_WildEncounterFled(_runtimeData);
        Resolve(player);
    }

    private void HandleEngage(PlayerController_SideScroll player)
    {
        // TODO: no BattleManager exists yet (Phase 3) — real Engage will trigger the
        // Combat_Directive cinematic transition into an additively-loaded BattleScene_Main
        // instead of this. For now, resolves identically to Flee.
        Debug.Log($"[WildEncounterCreature] Engage requested for {_runtimeData.speciesData.SpeciesName} — no BattleManager yet, scaffold resolves as Flee.");
        EventBus.Raise_WildEncounterEngageRequested(_runtimeData);
        Resolve(player);
    }

    private void Resolve(PlayerController_SideScroll player)
    {
        EncounterPromptController.Instance.Hide();
        player.UnfreezeMovement();
        Destroy(gameObject);
    }
}
