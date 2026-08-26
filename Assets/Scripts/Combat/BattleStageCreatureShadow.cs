using UnityEngine;

/// <summary>
/// First Phase 3 slice (Architecture_Directive_v0_1_0.md Part 3) — a real Scene SpriteRenderer
/// creature that mirrors _playerStageCreatures[0]'s color/position/alive-state, proving the new
/// camera/layer/world-space-positioning infrastructure end-to-end without migrating any of the
/// three real call sites (BattleManager, CombatVfxController, BeatSequenceRunner) that still drive
/// the VisualElement directly. Owned by BattleHUDController, same "plain C# helper, not its own
/// MonoBehaviour singleton" pattern as CombatVfxController.
///
/// Rendered via a RenderTexture bridge (DissolveVfxBridge.cs's already-proven pattern), NOT direct
/// camera compositing — see DECISIONS.md -> [Architecture] 2026-08-26: a direct-to-screen approach
/// (URP Camera Stacking, then reusing Main Camera's own cullingMask) hit two confirmed, unrelated
/// Unity/URP platform bugs in this project's specific setup (2D Renderer doesn't support Camera
/// Stacking at all; Pixel Perfect Camera doesn't reliably respect cullingMask — both root-caused
/// live and corroborated by Unity's own docs/issue tracker). The dedicated capture camera has
/// neither component, sidestepping both. The camera FOLLOWS the shadow instance's computed
/// world-space position every Sync (see BattleHUDController's Inspector Instructions for wiring),
/// tightly framing just the one creature so the captured texture drops straight into
/// _playerStageCreatures[0]'s existing box.
///
/// Known limitation of this slice (by design): this class only mirrors the RESTING lane/position,
/// not the transient offsets Beat Sequence lunges apply to the real VisualElement — flagged in
/// KNOWN_ISSUES.md as an expected, temporary consequence of proving infrastructure before the real
/// migration.
/// </summary>
public class BattleStageCreatureShadow
{
    private readonly Transform _stageOrigin;
    private readonly GameObject _prefab;
    private readonly Camera _captureCamera;

    private GameObject _instance;
    private PhasixPlaceholderVisual _visual;

    public BattleStageCreatureShadow(Transform stageOrigin, GameObject prefab, Camera captureCamera)
    {
        _stageOrigin = stageOrigin;
        _prefab = prefab;
        _captureCamera = captureCamera;
    }

    /// <summary>
    /// Mirrors one player-side BattleParticipant's color/position/alive-state onto the shadow
    /// instance, instantiating it on first use, and moves the capture camera to keep framing it.
    /// No-ops if the prefab, stage origin, or capture camera wasn't wired up (see
    /// BattleHUDController's Inspector Instructions) so an unconfigured scene degrades to today's
    /// VisualElement-only rendering instead of throwing.
    /// </summary>
    public void Sync(BattleParticipant participant)
    {
        if (_prefab == null || _stageOrigin == null || _captureCamera == null || participant == null) return;

        if (_instance == null)
        {
            _instance = Object.Instantiate(_prefab);
            _visual = _instance.GetComponent<PhasixPlaceholderVisual>();
        }

        PhasixData species = participant.RuntimeData.speciesData;
        if (species != null) _visual.SetPrimalType(species.PrimalType);
        _visual.SetVisible(participant.IsAlive);

        Vector3 worldPos = BattleLaneLayout.GetStagePosition(
            _stageOrigin.position, participant.LaneIndex, participant.PositionIndex, isPlayerSide: true);
        _instance.transform.position = worldPos;

        // Camera follows — keeps its own Z (near-clip framing distance) fixed, only recenters X/Y.
        Transform camTransform = _captureCamera.transform;
        camTransform.position = new Vector3(worldPos.x, worldPos.y, camTransform.position.z);
    }

    /// <summary>Destroys the shadow instance — called from BattleHUDController.OnDestroy so a stale shadow doesn't survive into the next battle.</summary>
    public void Teardown()
    {
        if (_instance != null) Object.Destroy(_instance);
        _instance = null;
        _visual = null;
    }
}
