// ============================================================
// CameraFollow.cs
// Path: Assets/Scripts/Player/CameraFollow.cs
//
// Velocity-based camera lookahead proxy. AUD-006 (repo audit, 2026-08): this
// project's Cinemachine 3.x CinemachineFollow (Body component) has no
// built-in lookahead field — that's a Cinemachine 2.x Composer API
// (m_LookaheadTime) that doesn't exist here. Standard 3.x workaround: don't
// point the camera's Follow target at the player directly — point it at a
// separate proxy Transform that this script eases toward "player position +
// an offset in the player's current movement direction". The camera then
// follows the proxy exactly as before (CinemachineFollow's own damping still
// applies on top of this), so the offset itself is smoothed twice: once
// here (SmoothDamp), once by Cinemachine's own tracker damping. That's
// intentional — a raw 1:1 lookahead snap reads as jittery when the player
// changes direction quickly.
//
// Sits in Player/, not a dedicated Camera/ folder — CLAUDE.md's folder
// structure already names this exact file ("Player/ <- PlayerController,
// CameraFollow").
// ============================================================

using UnityEngine;

/// <summary>
/// Drives this GameObject's position to lead slightly ahead of a target Rigidbody2D's movement
/// direction, scaled by how fast it's currently moving. Assign a CinemachineCamera's Follow
/// target to this object's Transform (not the player directly) to get lookahead.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The Rigidbody2D to read position/velocity from and lead ahead of. Assign the " +
             "player's Rigidbody2D.")]
    [SerializeField] private Rigidbody2D _target;

    [Header("Lookahead")]
    [Tooltip("How far ahead of the target's movement direction this proxy leads at " +
             "_velocityForMaxLookahead or above, in world units. Range: 0.5–3.")]
    [SerializeField] private float _maxLookaheadDistance = 1.5f;

    [Tooltip("Target velocity magnitude (world units/second) at which the lookahead offset " +
             "reaches its max distance. Should roughly match the target's top move speed " +
             "including Sprint (PlayerTopDownController: 5 base x 1.6 sprint = 8). Below this " +
             "speed the offset scales down linearly. Range: 4–10.")]
    [SerializeField] private float _velocityForMaxLookahead = 8f;

    [Tooltip("SmoothDamp time (seconds) easing the offset toward its target value. Lower = " +
             "snappier/more responsive, higher = smoother/laggier. This is on top of whatever " +
             "damping the CinemachineFollow component itself applies. Range: 0.15–0.5.")]
    [SerializeField] private float _smoothTime = 0.25f;

    // Current eased offset and SmoothDamp's own internal velocity state — not the target's
    // velocity, a separate value SmoothDamp needs to track for its spring-damper math.
    private Vector2 _currentOffset;
    private Vector2 _offsetVelocity;

    private void LateUpdate()
    {
        // Camera-follow logic belongs in LateUpdate — runs after all Update/FixedUpdate
        // movement has resolved for the frame, so the proxy never lags a frame behind.
        if (_target == null) return;

        Vector2 velocity = _target.linearVelocity;
        Vector2 targetOffset = Vector2.zero;
        if (velocity.sqrMagnitude > 0.01f)
        {
            float speedFraction = Mathf.Clamp01(velocity.magnitude / _velocityForMaxLookahead);
            targetOffset = velocity.normalized * (_maxLookaheadDistance * speedFraction);
        }

        _currentOffset = Vector2.SmoothDamp(_currentOffset, targetOffset, ref _offsetVelocity, _smoothTime);

        Vector3 targetPosition = _target.transform.position;
        transform.position = new Vector3(
            targetPosition.x + _currentOffset.x,
            targetPosition.y + _currentOffset.y,
            transform.position.z);
    }
}
