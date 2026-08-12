using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// TEMPORARY manual-test tool (2026-08-12, user: "is there a way that i can test the lane
/// movement") — press [ / ] in Play mode during battle to move the slot-0 player creature's
/// BattleParticipant.LaneIndex back/forward one row and immediately re-apply
/// BattleHUDController.RefreshPlayerLaneLayout, so LaneMovementSystem's per-row screen position
/// (GetLaneScreenTop) and depth scale (GetDepthScale) can be previewed live without needing a
/// real Approach/Return Beat Sequence to trigger it — there is no real pre-battle formation/
/// positioning mechanic yet for LaneIndex to come from otherwise. Same "poll until the scene's
/// objects exist" pattern as DebugMovementPresetCycler, since this component lives in
/// BattleScene_Main which loads additively after this one's own Awake would run.
///
/// Also handles the `\` guide-line toggle (2026-08-12, user: "add a debug button for the '\'
/// button to toggle show on and off the lane lines") — same file since both are lane-preview
/// debug tools sharing this component's lifecycle; the guide lines don't depend on a player side
/// existing, so that toggle is checked before the player-side early-return below.
///
/// DELETE THIS FILE once a real formation/positioning UI exists — this is scaffolding for
/// side-by-side comparison of the lane system's visual output only.
///
/// Attached to the same GameObject as BattleManager in BattleScene_Main (2026-08-12) — resolved
/// via GetComponent, not FindFirstObjectByType, since it's guaranteed present on this object.
/// </summary>
[RequireComponent(typeof(BattleManager))]
public class DebugLaneCycler : MonoBehaviour
{
    private BattleManager _battleManager;
    private List<BattleParticipant> _playerSide;
    private bool _guideLinesVisible;

    private void Awake()
    {
        _battleManager = GetComponent<BattleManager>();
    }

    private void Update()
    {
        if (Keyboard.current == null || BattleHUDController.Instance == null) return;

        if (Keyboard.current.backslashKey.wasPressedThisFrame)
        {
            _guideLinesVisible = !_guideLinesVisible;
            BattleHUDController.Instance.SetLaneGuideLinesVisible(_guideLinesVisible);
            Debug.Log($"[DebugLaneCycler] Lane guide lines {(_guideLinesVisible ? "ON" : "OFF")}.");
        }

        _playerSide = _battleManager.PlayerSide;
        if (_playerSide == null || _playerSide.Count == 0) return;

        if (Keyboard.current.leftBracketKey.wasPressedThisFrame) CycleLane(-1);
        if (Keyboard.current.rightBracketKey.wasPressedThisFrame) CycleLane(1);
    }

    private void CycleLane(int delta)
    {
        BattleParticipant participant = _playerSide[0];
        participant.LaneIndex = LaneMovementSystem.ClampLane(participant.LaneIndex + delta);
        BattleHUDController.Instance.RefreshPlayerLaneLayout(_playerSide);
        Debug.Log($"[DebugLaneCycler] Slot 0 creature now in row {participant.LaneIndex} (1=front/largest, {BattleLaneLayout.LaneCount}=back/smallest).");
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 60, 500, 40), "Lane guide lines: \\ to toggle" +
            (_playerSide != null && _playerSide.Count > 0
                ? $" — [ / ] moves slot-0 creature, currently row {_playerSide[0].LaneIndex}/{BattleLaneLayout.LaneCount}"
                : ""));
    }
}
