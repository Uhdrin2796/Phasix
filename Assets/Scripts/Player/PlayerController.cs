// ============================================================
// PlayerController.cs
// Path: Assets/Scripts/Player/PlayerController.cs
//
// Handles 8-directional top-down movement for the player.
// Input is read via the Unity new Input System (InputActionAsset).
// Physics velocity is applied in FixedUpdate via Rigidbody2D.
//
// Architecture notes:
//   - Input callback registered in OnEnable / unregistered in OnDisable
//     (event-driven — the callback fires only when input changes, never polled)
//   - _inputVector written from Input System callback, consumed in FixedUpdate
//   - Animator params updated in Update (visual, not physics)
//   - No Destroy/Instantiate, no GetComponent in Update, no heavy logic in Update
//
// Unity version: 6000.3.x
//   - Uses rb.linearVelocity (Unity 6 renamed rb.velocity to rb.linearVelocity)
//   - Uses InputSystem package (com.unity.inputsystem), not legacy Input.GetAxis
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down 8-directional player movement.
/// Reads input via Unity new Input System callbacks.
/// Applies smooth acceleration and deceleration via Rigidbody2D.
/// Drives Animator with facing direction and movement state.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────────

    [Header("Movement")]
    [Tooltip("Maximum movement speed in world units per second. " +
             "Range: 2–8. At 5 u/s the player crosses a 16-tile screen in ~3 seconds.")]
    [SerializeField] private float _moveSpeed = 5f;

    [Tooltip("How quickly the player accelerates to full speed (units/sec²). " +
             "Range: 20–100. Higher = snappier response.")]
    [SerializeField] private float _acceleration = 50f;

    [Tooltip("How quickly the player decelerates when no input is held (units/sec²). " +
             "Range: 20–80. Slightly lower than acceleration gives a subtle brake feel.")]
    [SerializeField] private float _deceleration = 40f;

    // ── Input ─────────────────────────────────────────────────────────────────

    [Header("Input")]
    [Tooltip("The InputActionAsset containing the Player action map. " +
             "Assign InputSystem_Actions.inputactions from Assets/.")]
    [SerializeField] private InputActionAsset _inputActions;

    // ── Animation ─────────────────────────────────────────────────────────────

    [Header("Animation")]
    [Tooltip("Animator component driving the player sprite. " +
             "Can be on this GameObject or a child. Leave empty if no Animator yet.")]
    [SerializeField] private Animator _animator;

    // ── Private State ─────────────────────────────────────────────────────────

    // Cached component references — set in Awake(), never in Update()
    private Rigidbody2D _rb;

    // The resolved Move action from the Player action map
    private InputAction _moveAction;

    // Raw input vector written by Input System callback, consumed in FixedUpdate
    private Vector2 _inputVector;

    // Smoothed velocity applied to the Rigidbody (persists between FixedUpdate calls)
    private Vector2 _currentVelocity;

    // Last non-zero direction, used to keep idle facing correct after the player stops
    private Vector2 _lastFacingDirection = Vector2.down;

    // Whether movement is externally frozen (e.g. during battle transition or cutscene)
    private bool _frozen;

    // ── Animator Parameter Hash IDs ───────────────────────────────────────────
    // Hashing the string once avoids a dictionary lookup inside the Animator every frame.
    private static readonly int AnimParamMoveX    = Animator.StringToHash("MoveX");
    private static readonly int AnimParamMoveY    = Animator.StringToHash("MoveY");
    private static readonly int AnimParamIsMoving = Animator.StringToHash("IsMoving");

    // ─────────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Cache Rigidbody2D — never call GetComponent in Update
        _rb = GetComponent<Rigidbody2D>();

        // Top-down: no gravity, no rotation from physics collisions
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;

        // Continuous detection prevents tunnelling through thin TilemapCollider2D edges
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Interpolation smooths visual position between physics steps at high framerates
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Resolve the Move action from the asset's Player action map.
        // throwIfNotFound: true surfaces misconfiguration immediately in the console.
        _moveAction = _inputActions
            .FindActionMap("Player", throwIfNotFound: true)
            .FindAction("Move",      throwIfNotFound: true);
    }

    private void OnEnable()
    {
        // Subscribe to input callbacks when this component is active.
        // performed fires when input exceeds the actuation threshold.
        // canceled fires when input returns to zero (key released, stick centred).
        _moveAction.performed += HandleMoveInput;
        _moveAction.canceled  += HandleMoveInput;
        _moveAction.Enable();
    }

    private void OnDisable()
    {
        // Always unsubscribe on disable to prevent memory leaks and ghost callbacks
        // if the player object is deactivated (e.g. entering battle scene).
        _moveAction.performed -= HandleMoveInput;
        _moveAction.canceled  -= HandleMoveInput;
        _moveAction.Disable();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INPUT CALLBACK  (called by Input System — not every frame)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stores the raw input vector when the Move action fires.
    /// ReadValue returns Vector2.zero on canceled, the WASD/stick vector on performed.
    /// </summary>
    private void HandleMoveInput(InputAction.CallbackContext context)
    {
        _inputVector = context.ReadValue<Vector2>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE  (visual only — no physics here)
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        // Skip animator updates when frozen; facing direction is already preserved.
        if (_frozen) return;

        bool isMoving = _inputVector.sqrMagnitude > 0.01f;

        // Track facing direction so idle animations hold the last direction
        // the player was walking (idle-down, idle-left, etc.)
        if (isMoving)
        {
            _lastFacingDirection = _inputVector.normalized;
        }

        // Drive Animator parameters — guard against unassigned Animator gracefully
        if (_animator != null)
        {
            _animator.SetFloat(AnimParamMoveX,    _lastFacingDirection.x);
            _animator.SetFloat(AnimParamMoveY,    _lastFacingDirection.y);
            _animator.SetBool (AnimParamIsMoving, isMoving);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FIXED UPDATE  (physics — runs at fixed timestep, independent of framerate)
    // ─────────────────────────────────────────────────────────────────────────

    private void FixedUpdate()
    {
        // When frozen, velocity is already zeroed. Do nothing.
        if (_frozen) return;

        // Normalise here: handles both digital (WASD composite) and analog (joystick).
        // WASD diagonal is already magnitude 1.0 from the composite binder.
        // Analog stick diagonal can be up to 1.41 — normalising clamps it to _moveSpeed.
        Vector2 targetVelocity = _inputVector.normalized * _moveSpeed;

        // Choose acceleration or deceleration rate.
        // sqrMagnitude avoids a sqrt call — we only care about zero vs non-zero.
        float blendRate = targetVelocity.sqrMagnitude > 0.01f
            ? _acceleration
            : _deceleration;

        // MoveTowards steps _currentVelocity toward targetVelocity at blendRate u/s.
        // Time.fixedDeltaTime converts the per-second rate to a per-physics-step delta.
        _currentVelocity = Vector2.MoveTowards(
            _currentVelocity,
            targetVelocity,
            blendRate * Time.fixedDeltaTime
        );

        // Assign to linearVelocity (Unity 6 API — renamed from legacy rb.velocity).
        // The Rigidbody integrates position from velocity automatically.
        _rb.linearVelocity = _currentVelocity;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API  (called by BattleManager, cutscene triggers, etc.)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Immediately halts movement and disables input processing.
    /// Call this before loading the battle scene or starting a cutscene.
    /// </summary>
    public void FreezeMovement()
    {
        _frozen          = true;
        _inputVector     = Vector2.zero;
        _currentVelocity = Vector2.zero;
        _rb.linearVelocity = Vector2.zero;

        // Set animator to idle so the player doesn't appear frozen mid-walk
        if (_animator != null)
        {
            _animator.SetBool(AnimParamIsMoving, false);
        }
    }

    /// <summary>
    /// Re-enables input processing after a FreezeMovement call.
    /// Call this when returning from battle or when a cutscene ends.
    /// Note: if you disabled/re-enabled the component instead of calling Freeze,
    /// input resumes automatically from OnEnable — you don't need this method.
    /// </summary>
    public void UnfreezeMovement()
    {
        _frozen = false;
        // Input will resume on the next performed callback from the Input System.
    }
}
