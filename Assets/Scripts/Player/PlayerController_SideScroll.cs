// ============================================================
// PlayerController_SideScroll.cs
// Path: Assets/Scripts/Player/PlayerController.cs
//
// 4-directional top-down movement with left/right sprite flip.
// Input is read via the Unity new Input System (InputActionAsset).
// Physics velocity is applied in FixedUpdate via Rigidbody2D.
// Sprite direction is mirrored by negating transform.localScale.x
// on the root — correct for bone-rigged characters where
// SpriteRenderer.flipX would only affect a single sprite part.
//
// Architecture notes:
//   - Input callback registered in OnEnable / unregistered in OnDisable
//   - _inputVector written from Input System callback, consumed in FixedUpdate
//   - Animator param (IsMoving) and flip updated in Update (visual, not physics)
//   - No Destroy/Instantiate, no GetComponent in Update, no heavy logic in Update
//
// Unity version: 6000.3.x
//   - Uses rb.linearVelocity (Unity 6 renamed rb.velocity to rb.linearVelocity)
//   - Uses InputSystem package (com.unity.inputsystem), not legacy Input.GetAxis
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down 4-directional player movement with left/right sprite flip.
/// Reads input via Unity new Input System callbacks.
/// Applies smooth acceleration and deceleration via Rigidbody2D.
/// Drives Animator with IsMoving and flips the root transform scale
/// to mirror the bone-rigged sprite when moving left vs right.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController_SideScroll : MonoBehaviour
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

    [Tooltip("Check this if the rig/sprite sheet naturally faces RIGHT in its source art. " +
             "Leave unchecked if it naturally faces LEFT (default for most imported rigs). " +
             "This tells the flip logic which direction is 'normal' so it mirrors correctly.")]
    [SerializeField] private bool _rigFacesRight = false;

    // ── Scaling ───────────────────────────────────────────────────────────────

    [Header("Scaling")]
    [Tooltip("Pixels Per Unit for your virtual canvas (320x180).\n\n" +
             "This is the camera-level PPU — how many virtual canvas pixels equal 1 Unity world unit. " +
             "It is NOT the sprite import PPU (set per-sprite in the texture importer).\n\n" +
             "Common values: 16 (loose pixel art), 32 (tighter). " +
             "Match this to the PPU you set on the Pixel Perfect Camera when you add it. " +
             "Default 16: 1 world unit = 16 virtual pixels on the 320x180 canvas.")]
    [SerializeField] private int _pixelsPerUnit = 16;

    [Tooltip("Target height of this character in virtual canvas pixels (320x180 canvas).\n\n" +
             "This controls how tall the character appears on screen. " +
             "Width scales automatically to preserve the original art aspect ratio — " +
             "you only need to set height.\n\n" +
             "Examples at PPU 16:\n" +
             "  32 px → 2.0 world units (small creature, ~18% of screen height)\n" +
             "  48 px → 3.0 world units (medium character, ~27% of screen height)\n" +
             "  64 px → 4.0 world units (large character, ~36% of screen height)\n\n" +
             "Set to 0 to disable and control scale manually via the Transform component.\n" +
             "Requires a CapsuleCollider2D on this GameObject sized to the art's true bounds.")]
    [SerializeField] private float _targetHeightPixels = 0f;

    // ── Private State ─────────────────────────────────────────────────────────

    // Cached component references — set in Awake(), never in Update()
    private Rigidbody2D _rb;

    // Stores the manual Transform scale set before any auto-scale was applied.
    // HideInInspector + SerializeField so it persists across domain reloads in Edit mode
    // without showing up as an editable field (it's managed entirely by the script).
    [HideInInspector] [SerializeField] private Vector3 _manualScale = Vector3.one;

    // True while auto-scale is active — prevents _manualScale from being overwritten
    // by repeated OnValidate calls while _targetHeightPixels is non-zero.
    [HideInInspector] [SerializeField] private bool _autoScaleActive = false;

    // The resolved Move action from the Player action map
    private InputAction _moveAction;

    // Raw input vector written by Input System callback, consumed in FixedUpdate
    private Vector2 _inputVector;

    // Smoothed velocity applied to the Rigidbody (persists between FixedUpdate calls)
    private Vector2 _currentVelocity;

    // Last non-zero direction — used to hold idle facing after the player stops.
    // Initialised from _rigFacesRight in Awake.
    private Vector2 _lastFacingDirection;

    // Base localScale cached at startup — preserves the Inspector-set scale (e.g. 0.1)
    // so the flip only negates X without altering Y/Z.
    private Vector3 _baseScale;

    // Tracks current flip state — initialised from _rigFacesRight in Awake.
    // Avoids writing to transform.localScale every frame when facing hasn't changed.
    private bool _isFacingRight;

    // Whether movement is externally frozen (e.g. during battle transition or cutscene)
    private bool _frozen;

    // ── Animator Parameter Hash IDs ───────────────────────────────────────────
    // Hashing the string once avoids a dictionary lookup inside the Animator every frame.
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

        // Zero out any residual velocity from previous Play sessions
        _rb.linearVelocity = Vector2.zero;

        // Continuous detection prevents tunnelling through thin TilemapCollider2D edges
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Interpolation smooths visual position between physics steps at high framerates
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Apply height-based auto-scale (or fall back to manual Transform scale).
        ApplyTargetHeight();

        // Initialise facing state from Inspector setting.
        // If the rig faces right natively, we start "facing right" with no flip applied.
        // If it faces left natively, we start "facing left" with no flip applied.
        _isFacingRight        = _rigFacesRight;
        _lastFacingDirection  = _rigFacesRight ? Vector2.right : Vector2.left;

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
        // Skip visual updates when frozen; facing direction is already preserved.
        if (_frozen) return;

        bool isMoving = _inputVector.sqrMagnitude > 0.01f;

        // Track facing direction so idle state holds the last direction after stopping.
        if (isMoving)
        {
            _lastFacingDirection = _inputVector.normalized;
        }

        // ── Left/right flip ───────────────────────────────────────────────────
        // Only check horizontal component; up/down movement doesn't change facing.
        // Gate with _isFacingRight flag to avoid writing localScale every frame.
        // Rig faces LEFT by default (unflipped = left). Moving right negates X to flip.
        if (_lastFacingDirection.x > 0.01f && !_isFacingRight)
        {
            _isFacingRight = true;
            transform.localScale = new Vector3(-_baseScale.x, _baseScale.y, _baseScale.z);
        }
        else if (_lastFacingDirection.x < -0.01f && _isFacingRight)
        {
            _isFacingRight = false;
            transform.localScale = _baseScale;
        }

        // ── Animator ─────────────────────────────────────────────────────────
        // Guard against unassigned Animator; safe to run without one during early dev.
        if (_animator != null)
        {
            _animator.SetBool(AnimParamIsMoving, isMoving);
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
        _frozen            = true;
        _inputVector       = Vector2.zero;
        _currentVelocity   = Vector2.zero;
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

    // ─────────────────────────────────────────────────────────────────────────
    // INTERNAL HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes and applies uniform scale so the character hits _targetHeightPixels on the
    /// 320x180 virtual canvas. Converts pixels → world units via _pixelsPerUnit.
    /// Falls back to the current Transform scale when _targetHeightPixels is 0.
    /// Called from Awake (runtime) and OnValidate (Edit mode, fires on Inspector change).
    /// </summary>
    private void ApplyTargetHeight()
    {
        if (_targetHeightPixels > 0f)
        {
            // Switching from manual → auto: snapshot the current Transform scale
            // before we overwrite it so we can restore it if the user sets back to 0.
            if (!_autoScaleActive)
            {
                _manualScale     = transform.localScale;
                _autoScaleActive = true;
            }

            CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
            if (col != null)
            {
                // Convert target pixels to world units, then divide by native local height
                // to get the uniform scale needed to hit exactly that world-unit height.
                float targetWorldHeight = _targetHeightPixels / _pixelsPerUnit;
                float uniformScale      = targetWorldHeight / col.size.y;
                _baseScale              = new Vector3(uniformScale, uniformScale, uniformScale);
                transform.localScale    = _baseScale;
            }
        }
        else
        {
            // Switching from auto → manual: restore the scale that was set before
            // auto-scaling kicked in. Only restore if auto-scale was previously active.
            if (_autoScaleActive)
            {
                transform.localScale = _manualScale;
                _autoScaleActive     = false;
            }

            _baseScale = transform.localScale;
        }
    }

    /// <summary>
    /// Called by Unity in Edit mode whenever an Inspector value changes.
    /// Applies the height scale immediately so you can see the result without entering Play mode.
    /// </summary>
    private void OnValidate()
    {
        ApplyTargetHeight();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DEBUG UTILS  (Editor + Play mode — right-click component → Log Dimensions)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Logs native (local-space) and world-scaled dimensions to the Console.
    /// Right-click this component in the Inspector to invoke.
    /// Useful when calibrating _targetHeightPixels or matching character sizes.
    /// </summary>
    [ContextMenu("Log Dimensions")]
    private void LogDimensions()
    {
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        if (col == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No CapsuleCollider2D found — cannot report dimensions.");
            return;
        }

        Vector2 nativeSize = col.size;
        Vector3 s          = transform.localScale;
        float worldW       = nativeSize.x * Mathf.Abs(s.x);
        float worldH       = nativeSize.y * Mathf.Abs(s.y);

        Debug.Log(
            $"[{gameObject.name}] Dimensions\n" +
            $"  Native (local):  {nativeSize.x:F2} x {nativeSize.y:F2} units\n" +
            $"  Scale applied:   ({s.x:F4}, {s.y:F4})\n" +
            $"  World size:      {worldW:F4} x {worldH:F4} units\n" +
            $"  Target height:   {(_targetHeightPixels > 0f ? _targetHeightPixels + $" px → {_targetHeightPixels / _pixelsPerUnit:F4} world units (auto-scale active)" : "0 (manual scale)")}"
        );
    }
}
