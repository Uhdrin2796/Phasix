using UnityEngine;

/// <summary>
/// Scene-view-only visualization of the 7-lane placeholder layout (BattleLaneLayout) — draws a
/// small marker at each lane position so a developer can see the intended formation while working
/// in the Editor. OnDrawGizmos only ever fires in the Scene view, never in the Game view or a
/// build, so this has zero effect on what players see — matches CompanionAI's existing pattern-
/// gizmo convention (see LESSONS_LEARNED.md). Front lane (1) draws largest/brightest, back lane
/// (7) smallest/dimmest, echoing Combat_Directive Part 2's front-grows/back-shrinks depth cue.
/// </summary>
public class BattleStageGizmos : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        for (int lane = 1; lane <= BattleLaneLayout.LaneCount; lane++)
        {
            DrawLaneMarker(lane, isPlayerSide: true);
            DrawLaneMarker(lane, isPlayerSide: false);
        }
    }

    private void DrawLaneMarker(int lane, bool isPlayerSide)
    {
        Vector3 pos = BattleLaneLayout.GetLanePosition(transform.position, lane, isPlayerSide);

        float t = (float)(lane - 1) / (BattleLaneLayout.LaneCount - 1);
        float size = Mathf.Lerp(0.35f, 0.12f, t);
        Color baseColor = isPlayerSide ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.4f, 0.3f);
        Gizmos.color = Color.Lerp(baseColor, new Color(baseColor.r, baseColor.g, baseColor.b, 0.25f), t);

        Gizmos.DrawWireCube(pos, new Vector3(size, size, size));
    }
}
