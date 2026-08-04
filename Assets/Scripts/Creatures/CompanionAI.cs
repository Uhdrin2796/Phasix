using UnityEngine;
using Pathfinding;

/// <summary>
/// Distance-driven Idle/Walk/Run state for the active companion, exposed to an Animator.
/// Framework scaffold only — no real animation content exists yet (placeholder-first art
/// pipeline, DECISIONS.md → [Art]). Two-track rule (Roadmap_v2.md): build the framework
/// now, fill content once real creature animations exist.
/// </summary>
public enum CompanionMovementState { Idle, Walk, Run }

/// <summary>
/// Which algorithm computes the companion's destination each frame — distinct from the
/// numeric tunables in CompanionMovementPreset. Live comparison (via
/// DebugMovementPresetCycler) showed that varying only distance/speed numbers on the same
/// trailing-point algorithm ("Tier 1") didn't read as genuinely different behavior — Close
/// Shadow vs Eager Runner, and Wide Wanderer vs Steady Anchor, felt like the same thing at
/// different speeds. This is the "Tier 2" half of DECISIONS.md's outline: different
/// underlying movement logic, not just different numbers.
///
/// CONVENTION — any pattern added here that bypasses AIPath's own pathfinding (i.e. gets added
/// to the "canMove = false" exclusion list in ApplyMovementPreset, alongside Orbit/
/// HiddenShadow/Blink) MUST also get its own Draw*Gizmos() case in OnDrawGizmos() below. This
/// isn't optional polish: for any pattern that bypasses AIPath, Seeker's own path-line gizmo
/// and AIBase's own destination-circle gizmo have nothing to draw (see the destination-reset
/// and seeker.drawGizmos comments in ApplyMovementPreset) — without a custom gizmo, that
/// pattern's movement becomes completely invisible in the Scene view, with no debug feedback
/// at all. OnDrawGizmos()'s switch has a runtime check that logs a warning if this is missed —
/// don't ignore that warning; add the gizmo case instead of suppressing it.
/// </summary>
public enum CompanionMovementPatternType
{
    /// <summary>The original trailing-point + repel behavior — straight line to the destination.</summary>
    Direct,
    /// <summary>Same trailing point, plus a perpendicular sine-wave offset — a weaving, snake-like path.</summary>
    Wavy,
    /// <summary>Ignores the trailing point; periodically dashes past the player at a changing angle, overshoots, repeats.</summary>
    DashThrough,
    /// <summary>Same trailing point, but alternates between moving and fully halting on a timer.</summary>
    StopAndGo,
    /// <summary>Ignores the trailing point; continuously circles the player at a fixed radius, like a moon orbiting a planet.</summary>
    Orbit,
    /// <summary>Ignores the trailing point; locks onto the player's position with zero lag while they move, then drifts to a swaying idle spot when they stop.</summary>
    HiddenShadow,
    /// <summary>Ignores the trailing point; periodically teleports to a random walkable point within a radius band around the player, then waits before teleporting again.</summary>
    Blink,
}

/// <summary>
/// A bundle of CompanionAI's tunable follow-feel knobs (plus which movement pattern to use)
/// — lets a whole movement "style" be swapped in one call instead of setting each field
/// individually. See DECISIONS.md → [Creatures] "Companion movement/following pattern
/// archetypes" — not yet tied to any specific per-species hook.
/// </summary>
[System.Serializable]
public struct CompanionMovementPreset
{
    public string Name;
    public CompanionMovementPatternType Pattern;
    public float TrailDistance;
    public float DirectionTurnSpeed;
    public float WalkSpeed;
    public float RunSpeed;
    public float IdleDistance;
    public float RunDistance;
    public float RepelDistance;
    public float RepelStrength;

    [Header("Pattern-specific — only used by the matching Pattern")]
    public float WaveAmplitude;      // Wavy: how far side-to-side, in world units
    public float WaveFrequency;      // Wavy: how fast it weaves, roughly radians/sec
    public float DashIntervalMin;    // DashThrough: shortest possible time between dashes, seconds
    public float DashInterval;       // DashThrough: longest possible time between dashes, seconds (randomized each cycle between Min and this)
    public float DashOvershootMin;   // DashThrough: shortest possible overshoot distance, world units
    public float DashOvershootDistance; // DashThrough: longest possible overshoot distance (randomized each cycle between Min and this)
    public float MoveDuration;       // StopAndGo: seconds spent moving per cycle
    public float PauseDuration;      // StopAndGo: seconds spent fully halted per cycle
    public float OrbitRadius;        // Orbit: fixed distance from the player, in world units
    public float OrbitAngularSpeed;  // Orbit: degrees/second — positive = counterclockwise, negative = clockwise
    public Vector2 OrbitCenterOffset; // Orbit: offset from the target's raw Transform position — compensates for a visual/collider pivot that isn't at the sprite's center (e.g. a feet-pivot rig)
    public float OrbitCatchUpSpeed;  // Orbit: how fast the companion's actual position chases its ideal orbit point, world units/sec. High (30+) = near-zero lag; low (3-8) = a looser, visibly trailing orbit.

    // HiddenShadow
    public float ShadowSwayAmplitude;      // HiddenShadow: how far the idle sway drifts side to side, in world units
    public float ShadowSwayFrequency;      // HiddenShadow: how fast the idle sway oscillates, roughly radians/sec
    public float ShadowStationaryDebounce; // HiddenShadow: seconds the player must stay below the movement threshold before the companion leaves Shadow for the swaying idle spot
    public float ShadowReturnLerpDuration; // HiddenShadow: seconds to ease back onto the player when movement resumes, instead of snapping instantly
    public Vector2 ShadowIdleAnchorOffset; // HiddenShadow: offset from the target's raw Transform position where the companion emerges to sway while idle (same pivot-compensation role as OrbitCenterOffset, but also defines the emerge direction/distance since there's no separate radius)
    public Vector2 ShadowLockedOffset;     // HiddenShadow: offset from the target's raw Transform position while glued/moving — lines the squashed shadow up directly under the player's visible feet (same pivot-compensation role as OrbitCenterOffset)

    // Blink
    public float BlinkRadius;         // Blink: max distance from the player a teleport destination can land, world units
    public float BlinkMinRadius;      // Blink: min distance from the player a teleport destination can land, world units — keeps it from blinking on top of them
    public float BlinkIntervalMin;    // Blink: shortest possible time between blinks, seconds
    public float BlinkInterval;       // Blink: longest possible time between blinks, seconds (randomized each cycle between Min and this)
    public float BlinkVanishDuration; // Blink: seconds the companion is fully hidden mid-teleport — long enough for the Rigidbody2D's own interpolation to settle onto the new position before it's shown again, so the move never renders as a slide
    public float BlinkFlashDuration;  // Blink: seconds for the post-teleport pop-scale flash to decay back to normal
    public float BlinkFlashScale;     // Blink: peak scale multiplier immediately after teleporting, decaying to 1 over BlinkFlashDuration
}

/// <summary>
/// Makes the active party companion follow the player using A* Pathfinding Project's
/// GridGraph (Roadmap_v2.md Wk 12-13). Trails behind the player's movement direction
/// rather than following the SAME point (Wk 12-13's "offset from player"), and switches
/// between Walk/Run speed tiers based on how far behind it has fallen — closing the gap
/// faster if it lags, matching "Idle / Walk / Run animation states driven by distance to
/// player."
///
/// Obstacle avoidance is whatever AIPath's path-following around the scanned GridGraph
/// already provides — the free tier of A* Pathfinding Project has no multi-agent RVO local
/// avoidance, which is not needed for a single following companion.
///
/// Reads the target's Rigidbody2D.linearVelocity (not raw Transform position deltas) to
/// determine "which way is the player trying to go" — position deltas are contaminated by
/// any external displacement, including the companion's own collider nudging the player on
/// contact, which produced a feedback loop with zero player input required. See
/// LESSONS_LEARNED.md → [Physics].
/// </summary>
[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(AIPath))]
public class CompanionAI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player transform to follow. Assigned by PartySystem when this companion is activated.")]
    [SerializeField] private Transform _target;

    [Header("Follow Distance")]
    [Tooltip("Distance from the player at which the companion is considered caught up and goes Idle. Range: 0.5-2.")]
    [SerializeField] private float _idleDistance = 1f;

    [Tooltip("Distance from the player beyond which the companion switches from Walk to Run to catch up. Range: 3-8.")]
    [SerializeField] private float _runDistance = 5f;

    [Tooltip("How far behind the player's movement direction the companion trails, in world units. Range: 0.5-2.")]
    [SerializeField] private float _trailDistance = 1.2f;

    [Tooltip("How fast the trail direction can turn to follow the player's changing heading, in degrees/second. Too high makes the follow-point whipsaw when the player curves around an obstacle (looks like getting stuck at corners); too low makes the companion slow to react to real direction changes. Range: 90-360.")]
    [SerializeField] private float _directionTurnSpeed = 180f;

    [Header("Personal Space")]
    [Tooltip("If the player is closer than this AND still closing the distance, the companion nudges its destination away from the player's current position (any direction, not just left/right) so the two don't wedge together in a corner. Range: 0.5-1.")]
    [SerializeField] private float _repelDistance = 0.7f;

    [Tooltip("How far the companion nudges away from the player when repelling, in world units, layered on top of the normal follow destination. Range: 0.5-1.5.")]
    [SerializeField] private float _repelStrength = 0.8f;

    [Header("Speed Tiers")]
    [Tooltip("AIPath.maxSpeed while in the Walk state. Range: 2-5.")]
    [SerializeField] private float _walkSpeed = 3f;

    [Tooltip("AIPath.maxSpeed while in the Run state (catching up). Should exceed the player's own move speed. Range: 5-10.")]
    [SerializeField] private float _runSpeed = 6f;

    [Header("Animation")]
    [Tooltip("Animator driving the companion's visual. Optional — safe to leave empty during early dev.")]
    [SerializeField] private Animator _animator;

    [Header("Hidden Shadow Visual")]
    [Tooltip("How flat the Body/Underglow scale.y goes while HiddenShadow is Locked onto the player. Low enough to read as a flattened ground shadow, high enough the shape doesn't vanish. Range: 0.3-0.6.")]
    [SerializeField] private float _shadowSquashScaleY = 0.45f;

    [Header("Debug Gizmos")]
    [Tooltip("Draws a Scene-view gizmo for the active pattern (Orbit/HiddenShadow/Blink) when this companion is selected. These three bypass AIPath entirely, so there's no destination for Seeker's own path gizmo to draw.")]
    [SerializeField] private bool _showPatternGizmos = true;

    private Seeker _seeker;
    private AIPath _aiPath;
    private Rigidbody2D _rigidbody2D;
    private Rigidbody2D _targetRigidbody2D;
    private Vector3 _lastTargetPosition;
    private Vector3 _smoothedTargetDirection = Vector2.down;

    // Optional — same convention as _animator above. Only PartySystem held a reference before
    // HiddenShadow needed to squash/restore the Body/Underglow scale from CompanionAI itself.
    private PhasixPlaceholderVisual _placeholderVisual;

    // Movement pattern (see CompanionMovementPatternType) and its own tuning + runtime state.
    private CompanionMovementPatternType _pattern = CompanionMovementPatternType.Direct;
    private float _waveAmplitude = 1f;
    private float _waveFrequency = 3f;
    private float _dashIntervalMin = 0.8f;
    private float _dashInterval = 1.2f;
    private float _dashOvershootMin = 2f;
    private float _dashOvershootDistance = 2.5f;
    private float _moveDuration = 1f;
    private float _pauseDuration = 0.8f;
    private float _orbitRadius = 2f;
    private float _orbitAngularSpeed = 60f;
    private Vector2 _orbitCenterOffset = Vector2.zero;
    private float _orbitCatchUpSpeed = 30f;
    private float _shadowSwayAmplitude = 0.2f;
    private float _shadowSwayFrequency = 0.75f;
    private float _shadowStationaryDebounce = 0.4f;
    private float _shadowReturnLerpDuration = 0.15f;
    private Vector2 _shadowIdleAnchorOffset = Vector2.zero;
    private Vector2 _shadowLockedOffset = Vector2.zero;
    private float _blinkRadius = 3.5f;
    private float _blinkMinRadius = 1f;
    private float _blinkIntervalMin = 0.6f;
    private float _blinkInterval = 1.2f;
    private float _blinkVanishDuration = 0.12f;
    private float _blinkFlashDuration = 0.2f;
    private float _blinkFlashScale = 1.4f;

    private float _patternTimer;
    private float _dashAngleRadians;
    private float _currentDashInterval;
    private float _currentDashOvershoot;
    private float _orbitAngleRadians;
    private bool _isPaused;
    private float _currentBlinkInterval;
    private float _blinkFlashTimer;

    /// <summary>
    /// Blink's own phase, tracked separately from CompanionMovementState for the same reason
    /// ShadowPhase is — purely about whether the companion is currently rendered at all, not
    /// about the distance-driven Idle/Walk/Run Animator state.
    /// </summary>
    private enum BlinkPhase { Visible, Vanished }
    private BlinkPhase _blinkPhase = BlinkPhase.Visible;
    private float _blinkVanishTimer;
    private Vector3 _blinkNextDestination;

    /// <summary>
    /// The stop AFTER _blinkNextDestination — a real, committed second teleport, not just a
    /// decorative preview. Both slots are kept filled at all times: whenever
    /// _blinkNextDestination is consumed (the companion actually teleports there),
    /// _blinkPreviewDestination is promoted into _blinkNextDestination and a fresh one is
    /// rolled to refill this slot, sampled around wherever the player is at that moment. An
    /// earlier version sampled this independently every cycle and discarded it without ever
    /// visiting it — looked like a broken "next" marker that was never honored. Now it's
    /// exactly what the next one will become once the current one is consumed.
    /// </summary>
    private Vector3 _blinkPreviewDestination;

    /// <summary>
    /// HiddenShadow's own player-velocity-driven state, tracked separately from
    /// CompanionMovementState — that one is distance-driven and Animator-facing; this one is
    /// purely about which of HiddenShadow's two behaviors (locked to the player vs. swaying at
    /// an idle anchor) is currently active. Deliberately not named "Idle" to avoid confusion
    /// with CompanionMovementState.Idle, which means something unrelated.
    /// </summary>
    private enum ShadowPhase { Locked, Emerged }
    private ShadowPhase _shadowPhase = ShadowPhase.Locked;
    private float _shadowStationaryTimer;
    private float _shadowSwayTimer;
    private Vector3 _shadowIdleAnchor;
    private bool _shadowReturning;
    private Vector3 _shadowReturnStartPosition;
    private float _shadowReturnTimer;

    public CompanionMovementState CurrentState { get; private set; } = CompanionMovementState.Idle;

    private static readonly int AnimParamIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimParamIsRunning = Animator.StringToHash("IsRunning");

    private void Awake()
    {
        _seeker = GetComponent<Seeker>();
        _aiPath = GetComponent<AIPath>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _placeholderVisual = GetComponent<PhasixPlaceholderVisual>();

        // We drive the sprite ourselves (same flip approach as PlayerTopDownController)
        // — AIPath must never touch our transform's rotation.
        _aiPath.orientation = OrientationMode.YAxisForward;
        _aiPath.updateRotation = false;

        // AIPath's built-in fake-gravity/ground-check system defaults to Physics.gravity
        // (3D, meant for sloped terrain) unless explicitly zeroed — a flat top-down 2D
        // game has no use for it, and leaving it on made the companion visibly sink.
        _aiPath.gravity = Vector3.zero;
    }

    private void OnEnable()
    {
        if (_target != null)
        {
            _lastTargetPosition = _target.position;
            _targetRigidbody2D = _target.GetComponent<Rigidbody2D>();
        }

#if UNITY_EDITOR
        // Calling SceneView.RepaintAll() from inside the gizmo draw callback itself turned out
        // NOT to chain into continuous repaints during Play Mode while the Scene tab isn't
        // focused — confirmed via a screen recording showing the gizmo frozen at its very first
        // drawn position for the entire ~8s clip while the companion visibly moved many times
        // over in the Game View. The draw callback only re-runs as a RESULT of a repaint that
        // already happened elsewhere; requesting one from within it is too weak/circular a
        // trigger. EditorApplication.update fires every Editor tick regardless of focus, so
        // driving the repaint from there instead is what actually keeps the gizmo live.
        UnityEditor.EditorApplication.update += RequestSceneViewRepaintIfGizmoRelevant;
#endif
    }

#if UNITY_EDITOR
    private void OnDisable()
    {
        UnityEditor.EditorApplication.update -= RequestSceneViewRepaintIfGizmoRelevant;
    }

    private void RequestSceneViewRepaintIfGizmoRelevant()
    {
        bool patternHasGizmo = _pattern == CompanionMovementPatternType.Orbit
            || _pattern == CompanionMovementPatternType.HiddenShadow
            || _pattern == CompanionMovementPatternType.Blink;

        // No Selection check — see OnDrawGizmos below for why these switched from
        // OnDrawGizmosSelected to always-on OnDrawGizmos.
        if (_showPatternGizmos && patternHasGizmo)
        {
            UnityEditor.SceneView.RepaintAll();
        }
    }
#endif

    /// <summary>Assigns the transform this companion follows. Called by PartySystem on activation.</summary>
    public void SetTarget(Transform target)
    {
        _target = target;
        _lastTargetPosition = target != null ? target.position : transform.position;
        _targetRigidbody2D = target != null ? target.GetComponent<Rigidbody2D>() : null;
    }

    /// <summary>Swaps in a full set of follow-feel tunables AND a movement pattern at once. See CompanionMovementPreset.</summary>
    public void ApplyMovementPreset(CompanionMovementPreset preset)
    {
        _trailDistance = preset.TrailDistance;
        _directionTurnSpeed = preset.DirectionTurnSpeed;
        _walkSpeed = preset.WalkSpeed;
        _runSpeed = preset.RunSpeed;
        _idleDistance = preset.IdleDistance;
        _runDistance = preset.RunDistance;
        _repelDistance = preset.RepelDistance;
        _repelStrength = preset.RepelStrength;

        _pattern = preset.Pattern;
        _waveAmplitude = preset.WaveAmplitude;
        _waveFrequency = preset.WaveFrequency;
        _dashIntervalMin = preset.DashIntervalMin;
        _dashInterval = preset.DashInterval;
        _dashOvershootMin = preset.DashOvershootMin;
        _dashOvershootDistance = preset.DashOvershootDistance;
        _moveDuration = preset.MoveDuration;
        _pauseDuration = preset.PauseDuration;
        _orbitRadius = preset.OrbitRadius;
        _orbitAngularSpeed = preset.OrbitAngularSpeed;
        _orbitCenterOffset = preset.OrbitCenterOffset;
        _orbitCatchUpSpeed = preset.OrbitCatchUpSpeed;
        _shadowSwayAmplitude = preset.ShadowSwayAmplitude;
        _shadowSwayFrequency = preset.ShadowSwayFrequency;
        _shadowStationaryDebounce = preset.ShadowStationaryDebounce;
        _shadowReturnLerpDuration = preset.ShadowReturnLerpDuration;
        _shadowIdleAnchorOffset = preset.ShadowIdleAnchorOffset;
        _shadowLockedOffset = preset.ShadowLockedOffset;
        _blinkRadius = preset.BlinkRadius;
        _blinkMinRadius = preset.BlinkMinRadius;
        _blinkIntervalMin = preset.BlinkIntervalMin;
        _blinkInterval = preset.BlinkInterval;
        _blinkVanishDuration = preset.BlinkVanishDuration;
        _blinkFlashDuration = preset.BlinkFlashDuration;
        _blinkFlashScale = preset.BlinkFlashScale;

        // Orbit, HiddenShadow, and Blink all bypass AIPath's own gradual pathfinding movement
        // entirely (see FixedUpdate/MoveAlongOrbit/MoveAlongHiddenShadow/MoveAlongBlink below) —
        // AIPath must not also be trying to drive the same Rigidbody2D at the same time.
        // Adding a pattern to this list? It also needs a Draw*Gizmos() case in OnDrawGizmos()
        // below — see the CONVENTION on CompanionMovementPatternType and OnDrawGizmos()'s
        // default-case warning, which will fire at runtime if this is missed.
        _aiPath.canMove = _pattern != CompanionMovementPatternType.Orbit
            && _pattern != CompanionMovementPatternType.HiddenShadow
            && _pattern != CompanionMovementPatternType.Blink;

        // AIBase's own OnDrawGizmos (Pathfinding/Core/AI/AIBase.cs) draws a blue circle at
        // aiPath.destination UNCONDITIONALLY — no selection required, always on — whenever
        // destination isn't its "never set" positive-infinity sentinel. Since Orbit/
        // HiddenShadow/Blink never write to destination (see the Update()/ComputeDestination
        // guard below), it was sitting frozen at whatever the last Direct/Wavy/DashThrough/
        // StopAndGo pattern left it at, and being always-on (not OnDrawGizmosSelected like our
        // own pattern gizmos), that stale marker was the ONLY gizmo actually visible without
        // manually selecting the companion — easily mistaken for "the following pathing gizmo
        // still showing" instead of our own gizmos genuinely not appearing at all. Explicitly
        // resetting destination to the sentinel here suppresses AIBase's own marker for the
        // three patterns that don't use it.
        if (!_aiPath.canMove)
        {
            _aiPath.destination = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        }

        // Seeker.OnDrawGizmos (Pathfinding/Core/AI/Seeker.cs) is a SEPARATE always-on gizmo from
        // AIBase's destination circle above — it draws a solid green line along the last
        // successfully calculated path (lastCompletedVectorPath), gated only by its own public
        // drawGizmos toggle. Resetting destination does nothing to clear it: it isn't tied to
        // destination at all, it's whatever path was last actually calculated, and Orbit/
        // HiddenShadow/Blink never calculate a new one to replace it. Left alone, it's exactly
        // the "old path still showing" artifact — a real green line tracing wherever the
        // companion was last actually pathfinding to, frozen from before the pattern switch.
        // Explicitly toggling this off/on alongside canMove keeps it in sync: suppressed for
        // the three patterns that never path, restored for the ones that do.
        _seeker.drawGizmos = _aiPath.canMove;

        // The companion prefab's Rigidbody2D normally has Interpolate enabled (smooths motion
        // between physics ticks for every other pattern). That's exactly what still made Blink's
        // teleport look like a fast slide even after hiding the sprite during the vanish window
        // (SetVisible(true) and MovePosition happen in the same FixedUpdate tick — interpolation
        // doesn't know it was a teleport, it just smooths the render between last tick's position
        // and this tick's, regardless of when the sprite became visible again). Blink's motion is
        // never meant to be smooth (it's either fully hidden or instantly correct), so interpolation
        // is switched off entirely while Blink is active, and restored for every other pattern,
        // which do rely on it for smooth Rigidbody2D.MovePosition-driven motion.
        _rigidbody2D.interpolation = _pattern == CompanionMovementPatternType.Blink
            ? RigidbodyInterpolation2D.None
            : RigidbodyInterpolation2D.Interpolate;

        // Reset runtime state so switching patterns doesn't carry over a stale phase/angle
        // from whatever pattern was active before.
        _patternTimer = 0f;
        _currentDashInterval = 0f; // forces DashThrough to pick a fresh angle/distance on the very next frame
        _isPaused = false;
        _dashAngleRadians = Mathf.Atan2(_smoothedTargetDirection.y, _smoothedTargetDirection.x);

        // Forces the first blink to fire promptly instead of waiting a full interval, clears
        // any in-progress flash from a pattern swap away from Blink mid-decay, and — most
        // importantly — forces the companion visible again in case the preset was swapped away
        // from Blink while it was mid-Vanished, which would otherwise leave it permanently
        // invisible under a non-Blink pattern.
        _currentBlinkInterval = 0f;
        _blinkFlashTimer = 0f;
        _blinkVanishTimer = 0f;
        _blinkPhase = BlinkPhase.Visible;
        _placeholderVisual?.SetBlinkFlashScale(1f);
        _placeholderVisual?.SetVisible(true);

        // Seed the 2-deep destination queue (see _blinkPreviewDestination's doc comment) so
        // both slots are already valid the moment Blink becomes active — MoveAlongBlink only
        // ever consumes/promotes/refills this queue, it never picks _blinkNextDestination
        // itself, so it has to start non-empty. Harmless to reseed even when re-entering Blink
        // after already having a queue — old commitments from a previous Blink session aren't
        // worth preserving across an unrelated pattern in between.
        if (_pattern == CompanionMovementPatternType.Blink)
        {
            _blinkNextDestination = PickBlinkDestination();
            _blinkPreviewDestination = PickBlinkDestination();
        }

        // Start orbiting from wherever the companion currently is relative to the player,
        // rather than snapping to angle 0 — avoids a visible jump the moment Orbit is selected.
        Vector3 currentOffset = FlattenToXY(transform.position - _target.position - (Vector3)_orbitCenterOffset);
        _orbitAngleRadians = currentOffset.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(currentOffset.y, currentOffset.x)
            : 0f;

        if (_pattern == CompanionMovementPatternType.HiddenShadow)
        {
            // Seed HiddenShadow's starting phase from the player's CURRENT velocity rather than
            // always starting Locked — otherwise switching into this pattern while the player
            // already happens to be stationary would snap to the player first and only reach
            // Emerged once the debounce elapses, a visible pop-then-drift that Orbit's own
            // angle-seeding above avoids for the same reason.
            _shadowStationaryTimer = 0f;
            _shadowReturning = false;
            Vector3 initialPlayerVelocity = _targetRigidbody2D != null ? (Vector3)_targetRigidbody2D.linearVelocity : Vector3.zero;
            if (initialPlayerVelocity.sqrMagnitude > 0.01f)
            {
                _shadowPhase = ShadowPhase.Locked;
            }
            else
            {
                _shadowPhase = ShadowPhase.Emerged;
                _shadowIdleAnchor = _target.position + (Vector3)_shadowIdleAnchorOffset;
                _shadowSwayTimer = 0f;
            }
            ApplyShadowSquash(_shadowPhase == ShadowPhase.Locked);
        }
        else
        {
            // Switching away from HiddenShadow (or never having been in it) — make sure the
            // visual isn't left squashed from a previous HiddenShadow session.
            ApplyShadowSquash(false);
        }
    }

    private void Update()
    {
        if (_target == null) return;

        // Read the player's actual Rigidbody2D velocity rather than a raw Transform position
        // delta. Position deltas are contaminated by ANY external displacement, including the
        // companion's own collider physically nudging the player — which .normalized() then
        // amplifies into a full-strength direction, indistinguishable from real input. That
        // created a self-sustaining loop with zero player input: companion nudges player →
        // player's position shifts slightly → this script reads that shift as "the player
        // moved" → reacts and nudges again. linearVelocity reflects the player's OWN control
        // script's intent (PlayerTopDownController re-asserts it every FixedUpdate,
        // overwriting any collision-induced change), so it isn't susceptible to this feedback
        // loop. Falls back to a flattened position delta if the target has no Rigidbody2D
        // (e.g. a non-physics test target).
        Vector3 playerVelocity = _targetRigidbody2D != null
            ? (Vector3)_targetRigidbody2D.linearVelocity
            : FlattenToXY(_target.position - _lastTargetPosition);

        UpdateTrailDirection(playerVelocity);
        _lastTargetPosition = _target.position;

        float distanceToPlayer = Vector2.Distance(transform.position, _target.position);
        UpdateMovementState(distanceToPlayer);

        // Orbit, HiddenShadow, and Blink are handled in FixedUpdate instead (see
        // FixedUpdate/MoveAlongOrbit/MoveAlongHiddenShadow/MoveAlongBlink below) — all three
        // bypass AIPath's destination-seeking entirely (canMove is disabled for these patterns
        // in ApplyMovementPreset) and need to be driven on the physics tick, not the render
        // tick, to track the player's own Rigidbody2D-driven movement tightly.
        if (_pattern != CompanionMovementPatternType.Orbit
            && _pattern != CompanionMovementPatternType.HiddenShadow
            && _pattern != CompanionMovementPatternType.Blink)
        {
            _aiPath.destination = ComputeDestination(playerVelocity, distanceToPlayer);
        }

        if (_animator != null)
        {
            _animator.SetBool(AnimParamIsMoving, CurrentState != CompanionMovementState.Idle);
            _animator.SetBool(AnimParamIsRunning, CurrentState == CompanionMovementState.Run);
        }
    }

    /// <summary>
    /// Drives Orbit and HiddenShadow on the physics tick, matching how the player's own
    /// Rigidbody2D moves (PlayerTopDownController applies velocity in FixedUpdate) —
    /// running these in Update() instead caused a render/physics cadence mismatch that read as
    /// lag once the player started moving, on top of the reactive-chase lag fixed below.
    /// </summary>
    private void FixedUpdate()
    {
        if (_target == null) return;

        switch (_pattern)
        {
            case CompanionMovementPatternType.Orbit:
                MoveAlongOrbit();
                break;
            case CompanionMovementPatternType.HiddenShadow:
                MoveAlongHiddenShadow();
                break;
            case CompanionMovementPatternType.Blink:
                MoveAlongBlink();
                break;
        }
    }

    /// <summary>
    /// User's own diagnosis, confirmed correct: purely reacting to a moving target point
    /// (chasing "where the player + orbit offset is right now" every step) always leaves
    /// some residual lag once the player is moving, no matter how high the catch-up speed —
    /// the destination itself never stops moving. Fixed with a feedforward term: apply the
    /// player's current velocity directly to the companion's own motion (so translation
    /// tracks with ~zero lag, the same way the player moves themselves) plus this step's own
    /// orbital rotation, and use _orbitCatchUpSpeed only for the small residual correction
    /// (drift/rounding, pattern-switch settling) rather than the whole job.
    /// </summary>
    private void MoveAlongOrbit()
    {
        Vector3 playerVelocity = _targetRigidbody2D != null ? (Vector3)_targetRigidbody2D.linearVelocity : Vector3.zero;

        _orbitAngleRadians += _orbitAngularSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 orbitOffset = new Vector3(Mathf.Cos(_orbitAngleRadians), Mathf.Sin(_orbitAngleRadians), 0f) * _orbitRadius;
        Vector3 idealPosition = _target.position + (Vector3)_orbitCenterOffset + orbitOffset;

        Vector3 predictedPosition = (Vector3)transform.position + playerVelocity * Time.fixedDeltaTime;
        Vector3 newPosition = Vector3.MoveTowards(predictedPosition, idealPosition, _orbitCatchUpSpeed * Time.fixedDeltaTime);

        _rigidbody2D.MovePosition(newPosition);
    }

    /// <summary>
    /// HiddenShadow: while the player is moving, locks directly onto their position every
    /// physics tick — a true zero-lag glued shadow, no feedforward/catch-up needed since it's
    /// just a straight position copy. Once the player has been below the movement threshold for
    /// ShadowStationaryDebounce seconds, drifts to an idle anchor behind/above the player and
    /// sways side to side on its own until the player moves again, at which point it eases back
    /// onto the player over ShadowReturnLerpDuration rather than snapping (DECISIONS.md →
    /// [Creatures] Hidden Shadow pattern — open decision, leaning lerp over snap for now).
    /// Uses Rigidbody2D.MovePosition, not a raw transform.position write, for the same reason
    /// MoveAlongOrbit above does — the companion's Rigidbody2D is kinematic with a trigger
    /// collider (DECISIONS.md → [Pathfinding] Companion uses Rigidbody2D (Kinematic)), and
    /// MovePosition is what keeps that collider's physics updates correct.
    /// </summary>
    private void MoveAlongHiddenShadow()
    {
        Vector3 playerVelocity = _targetRigidbody2D != null ? (Vector3)_targetRigidbody2D.linearVelocity : Vector3.zero;
        bool playerIsMoving = playerVelocity.sqrMagnitude > 0.01f; // same threshold as UpdateTrailDirection/ComputeRepelOffset above
        // Where the glued/squashed shadow actually sits — offset from the player's raw Transform
        // position (same pivot-compensation role as OrbitCenterOffset) so it lines up directly
        // under the player's visible feet instead of wherever their Transform pivot happens to be.
        Vector3 lockedPosition = _target.position + (Vector3)_shadowLockedOffset;

        if (playerIsMoving)
        {
            _shadowStationaryTimer = 0f;

            if (_shadowPhase == ShadowPhase.Emerged)
            {
                // Just started moving again after swaying at the idle anchor — begin easing
                // back onto the player from wherever the sway left the companion, rather than
                // snapping straight there. Squash is deliberately NOT applied yet here — see
                // below, it waits until the lerp actually lands on the player.
                _shadowPhase = ShadowPhase.Locked;
                _shadowReturning = true;
                _shadowReturnStartPosition = transform.position;
                _shadowReturnTimer = 0f;
            }

            if (_shadowReturning)
            {
                _shadowReturnTimer += Time.fixedDeltaTime;
                float t = _shadowReturnLerpDuration > 0f ? Mathf.Clamp01(_shadowReturnTimer / _shadowReturnLerpDuration) : 1f;
                _rigidbody2D.MovePosition(Vector3.Lerp(_shadowReturnStartPosition, lockedPosition, t));
                if (t >= 1f)
                {
                    // Only flatten into the squashed shadow once actually coincident with the
                    // player — squashing immediately on the way back (at the old idle anchor,
                    // still some distance out) is what made the squashed shape look "too far
                    // from the player": it read as a flat shadow floating apart from them for
                    // the whole lerp instead of the full companion visibly returning to them.
                    _shadowReturning = false;
                    ApplyShadowSquash(true);
                }
            }
            else
            {
                _rigidbody2D.MovePosition(lockedPosition);
            }
        }
        else
        {
            if (_shadowPhase == ShadowPhase.Locked)
            {
                _shadowStationaryTimer += Time.fixedDeltaTime;
                if (_shadowStationaryTimer >= _shadowStationaryDebounce)
                {
                    _shadowPhase = ShadowPhase.Emerged;
                    _shadowIdleAnchor = _target.position + (Vector3)_shadowIdleAnchorOffset;
                    _shadowSwayTimer = 0f;
                    ApplyShadowSquash(false);
                }
                else
                {
                    // Still within the debounce window — keep locking, no visible change yet.
                    _rigidbody2D.MovePosition(lockedPosition);
                    return;
                }
            }

            _shadowSwayTimer += Time.fixedDeltaTime;
            float sway = Mathf.Sin(_shadowSwayTimer * _shadowSwayFrequency) * _shadowSwayAmplitude;
            _rigidbody2D.MovePosition(_shadowIdleAnchor + new Vector3(sway, 0f, 0f));
        }
    }

    /// <summary>
    /// Flattens the companion's visual to read as a ground shadow while Locked, restores it
    /// while Emerged. Reuses the existing Body/Underglow structure via PhasixPlaceholderVisual
    /// (placeholder-first art pipeline, DECISIONS.md → [Art]) — no new sprite. _placeholderVisual
    /// is optional (same convention as _animator), so this is a no-op if it's absent.
    /// </summary>
    private void ApplyShadowSquash(bool squashed)
    {
        _placeholderVisual?.SetShadowSquash(squashed ? _shadowSquashScaleY : 1f);
    }

    /// <summary>
    /// Blink: waits _currentBlinkInterval seconds while Visible, then fully hides the companion
    /// (SetVisible(false)) rather than moving it immediately — the companion prefab's
    /// Rigidbody2D has Interpolate enabled (Phasix_Placeholder.prefab), which smooths any
    /// MovePosition jump across the next few render frames for normal-movement smoothness, so
    /// an instant teleport with the sprite still showing reads as a fast slide, not a blink.
    /// Hiding first sidesteps that entirely — nothing renders during the position change, so
    /// there's nothing to smooth.
    /// While Vanished, waits _blinkVanishDuration (long enough for the interpolation to fully
    /// settle onto the new position before anything is shown again), performs the actual
    /// teleport, shows the companion, and starts the pop-scale flash. The flash's own decay is
    /// tracked independently so it keeps playing out after the next Visible-phase countdown has
    /// already resumed.
    ///
    /// The destination is picked once, the moment Vanished begins (not deferred to the end of
    /// the vanish window) and cached in _blinkNextDestination — both so the actual teleport at
    /// the end of the window lands exactly where it was decided (no second random roll that
    /// could disagree), and so DrawBlinkGizmos below has something committed to show ahead of
    /// the move, not just after it already happened. _blinkNextDestination and
    /// _blinkPreviewDestination are both kept filled at all times (seeded together in
    /// ApplyMovementPreset) — this method only ever CONSUMES the front of that 2-deep queue and
    /// refills the back, it never picks _blinkNextDestination itself.
    /// </summary>
    private void MoveAlongBlink()
    {
        if (_blinkPhase == BlinkPhase.Visible)
        {
            _patternTimer += Time.fixedDeltaTime;
            if (_patternTimer >= _currentBlinkInterval)
            {
                _patternTimer = 0f;
                _blinkVanishTimer = 0f;
                _blinkPhase = BlinkPhase.Vanished;
                _placeholderVisual?.SetVisible(false);
            }

            if (_blinkFlashTimer > 0f)
            {
                _blinkFlashTimer -= Time.fixedDeltaTime;
                float t = _blinkFlashDuration > 0f ? Mathf.Clamp01(_blinkFlashTimer / _blinkFlashDuration) : 0f;
                _placeholderVisual?.SetBlinkFlashScale(Mathf.Lerp(1f, _blinkFlashScale, t));
            }
        }
        else // Vanished
        {
            _blinkVanishTimer += Time.fixedDeltaTime;
            if (_blinkVanishTimer >= _blinkVanishDuration)
            {
                _rigidbody2D.MovePosition(_blinkNextDestination);
                _currentBlinkInterval = Random.Range(_blinkIntervalMin, Mathf.Max(_blinkInterval, _blinkIntervalMin));

                // Promote the queue: the spot that was "after next" becomes the new immediate
                // next, and a fresh one is rolled to refill the back — sampled around wherever
                // the player actually is right now, the best information available at this
                // decision point (same reasoning _blinkNextDestination itself always used).
                _blinkNextDestination = _blinkPreviewDestination;
                _blinkPreviewDestination = PickBlinkDestination();

                _placeholderVisual?.SetVisible(true);
                _blinkFlashTimer = _blinkFlashDuration;
                _blinkPhase = BlinkPhase.Visible;
            }
        }
    }

    /// <summary>
    /// Samples a random point in the [_blinkMinRadius, _blinkRadius] annulus around the player
    /// and validates it against the A* GridGraph (this project's mandated pathfinding backend,
    /// CLAUDE.md → Hard Architecture Rules) via AstarPath.active.GetNearest — a raw random
    /// point has no guarantee of landing somewhere walkable, unlike Orbit/HiddenShadow which
    /// always stay glued to the (necessarily walkable) player. Rejects a candidate if the
    /// nearest node isn't walkable, or if it's suspiciously far from the sampled point (a sign
    /// the sample landed inside or beyond unwalkable geometry and snapped to a distant open
    /// node instead). Retries a handful of times; if every attempt fails, falls back to the
    /// player's own current position rather than ever teleporting into a wall.
    /// </summary>
    private Vector3 PickBlinkDestination()
    {
        const int maxAttempts = 8;
        const float maxSnapDistance = 0.5f;

        for (int i = 0; i < maxAttempts; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(_blinkMinRadius, Mathf.Max(_blinkRadius, _blinkMinRadius));
            Vector3 candidate = _target.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;

            var nearest = AstarPath.active != null ? AstarPath.active.GetNearest(candidate, NNConstraint.Default) : default;
            if (nearest.node != null && nearest.node.Walkable
                && (FlattenToXY(nearest.position) - candidate).sqrMagnitude <= maxSnapDistance * maxSnapDistance)
            {
                return FlattenToXY(nearest.position);
            }
        }

        return _target.position; // every sample was unwalkable/blocked — stay with the player rather than blink into geometry
    }

    /// <summary>
    /// Tracks the player's recent movement direction (from real velocity, not raw position —
    /// see Update()) so the companion trails behind it, not on top of it. Actually smoothed
    /// (rotates toward the new direction at a capped turn speed) rather than snapping instantly
    /// — an instant snap made the trail point (and therefore the companion's destination)
    /// whipsaw every time the player's heading changed quickly, which is exactly what happens
    /// while curving around an obstacle. That whipsaw is what looked like "getting stuck" near
    /// corners.
    /// </summary>
    private void UpdateTrailDirection(Vector3 playerVelocity)
    {
        if (playerVelocity.sqrMagnitude > 0.01f) // ~0.1 units/sec — real movement, not residual noise
        {
            Vector3 targetDirection = playerVelocity.normalized;
            _smoothedTargetDirection = Vector3.RotateTowards(
                _smoothedTargetDirection,
                targetDirection,
                _directionTurnSpeed * Mathf.Deg2Rad * Time.deltaTime,
                0f
            );
        }
    }

    /// <summary>
    /// Computes this frame's AIPath destination according to the active
    /// CompanionMovementPatternType. Direct/Wavy/StopAndGo all still trail behind the
    /// player's movement direction as their base; DashThrough ignores it entirely in favor
    /// of periodically dashing past the player at a changing angle.
    /// </summary>
    private Vector3 ComputeDestination(Vector3 playerVelocity, float distanceToPlayer)
    {
        if (_pattern == CompanionMovementPatternType.DashThrough)
            return ComputeDashThroughDestination();

        Vector3 followPoint = _target.position - _smoothedTargetDirection * _trailDistance;
        followPoint += ComputeRepelOffset(playerVelocity, distanceToPlayer);

        switch (_pattern)
        {
            case CompanionMovementPatternType.Wavy:
                return ApplyWavyOffset(followPoint);
            case CompanionMovementPatternType.StopAndGo:
                return ApplyStopAndGo(followPoint);
            default:
                return followPoint;
        }
    }

    /// <summary>Adds a perpendicular sine-wave offset to the base destination — a weaving, snake-like path instead of a straight line.</summary>
    private Vector3 ApplyWavyOffset(Vector3 baseDestination)
    {
        _patternTimer += Time.deltaTime;
        Vector3 perpendicular = new Vector3(-_smoothedTargetDirection.y, _smoothedTargetDirection.x, 0f);
        float wave = Mathf.Sin(_patternTimer * _waveFrequency) * _waveAmplitude;
        return baseDestination + perpendicular * wave;
    }

    /// <summary>
    /// Ignores the trailing point entirely. Picks a new dash angle (the player's current
    /// travel direction, randomized by up to ±150°) AND a new overshoot distance (randomized
    /// between _dashOvershootMin and _dashOvershootDistance), then targets a point that far
    /// PAST the player along that angle — so the companion runs toward and past the player,
    /// overshoots by an unpredictable amount, then picks a new angle/distance and does it
    /// again. Both axes (angle and length) varying independently is what reads as erratic
    /// rather than just "fast in one direction."
    ///
    /// Re-targets when AIPath actually reaches the current dash destination, not on a fixed
    /// clock — a clock-based trigger could fire mid-transit and cut the dash off before it
    /// completed, which read as the path "changing mid-run" instead of a clean dash-and-done.
    /// _dashIntervalMin/_dashInterval are now a safety-net MAXIMUM wait (in case a target
    /// becomes unreachable) rather than the primary trigger.
    /// </summary>
    private Vector3 ComputeDashThroughDestination()
    {
        _patternTimer += Time.deltaTime;
        bool reachedCurrentDash = _aiPath.reachedDestination;
        bool timedOut = _patternTimer >= _currentDashInterval;

        if (reachedCurrentDash || timedOut)
        {
            _patternTimer = 0f;
            _currentDashInterval = Random.Range(_dashIntervalMin, Mathf.Max(_dashInterval, _dashIntervalMin));
            _currentDashOvershoot = Random.Range(_dashOvershootMin, Mathf.Max(_dashOvershootDistance, _dashOvershootMin));
            float baseAngle = Mathf.Atan2(_smoothedTargetDirection.y, _smoothedTargetDirection.x);
            float randomOffset = Random.Range(-150f, 150f) * Mathf.Deg2Rad;
            _dashAngleRadians = baseAngle + randomOffset;
        }

        Vector3 dashDirection = new Vector3(Mathf.Cos(_dashAngleRadians), Mathf.Sin(_dashAngleRadians), 0f);
        return _target.position + dashDirection * _currentDashOvershoot;
    }

    /// <summary>Alternates between moving toward the base destination and fully halting, on a timer — a stop-move-stop rhythm instead of continuous gliding.</summary>
    private Vector3 ApplyStopAndGo(Vector3 baseDestination)
    {
        _patternTimer += Time.deltaTime;
        float phaseDuration = _isPaused ? _pauseDuration : _moveDuration;
        if (_patternTimer >= phaseDuration)
        {
            _patternTimer = 0f;
            _isPaused = !_isPaused;
        }

        if (_isPaused)
        {
            _aiPath.maxSpeed = 0f;
            return transform.position;
        }

        return baseDestination;
    }

    /// <summary>
    /// Personal-space nudge: if the player is very close AND still walking toward the
    /// companion (from any of the 360 degrees — this is a free-movement top-down game, not
    /// left/right only), push the destination directly away from the player's current
    /// position. Recomputed fresh every frame from the live relative position, so it works
    /// correctly regardless of which direction the player is actually approaching from.
    /// Prevents the two from wedging together in a corner.
    /// </summary>
    private Vector3 ComputeRepelOffset(Vector3 playerVelocity, float distanceToPlayer)
    {
        if (distanceToPlayer >= _repelDistance || playerVelocity.sqrMagnitude <= 0.01f)
            return Vector3.zero;

        Vector3 awayFromPlayer = FlattenToXY(transform.position - _target.position);
        if (awayFromPlayer.sqrMagnitude <= 0.0001f)
            return Vector3.zero; // exactly overlapping — no well-defined direction to repel toward

        // playerVelocity points in the player's direction of travel; awayFromPlayer points from
        // the player toward the companion. A positive dot product means the player is moving in
        // roughly the same direction as "toward the companion" — i.e. closing in.
        bool playerClosingIn = Vector3.Dot(playerVelocity.normalized, awayFromPlayer.normalized) > 0f;
        if (!playerClosingIn)
            return Vector3.zero;

        return awayFromPlayer.normalized * _repelStrength;
    }

    /// <summary>Drops the Z component — see the comment in Update() for why this matters here.</summary>
    private static Vector3 FlattenToXY(Vector3 v) => new Vector3(v.x, v.y, 0f);

    private void UpdateMovementState(float distanceToPlayer)
    {
        if (distanceToPlayer <= _idleDistance)
        {
            CurrentState = CompanionMovementState.Idle;
            _aiPath.maxSpeed = _walkSpeed;
        }
        else if (distanceToPlayer >= _runDistance)
        {
            CurrentState = CompanionMovementState.Run;
            _aiPath.maxSpeed = _runSpeed;
        }
        else
        {
            CurrentState = CompanionMovementState.Walk;
            _aiPath.maxSpeed = _walkSpeed;
        }
    }

    /// <summary>
    /// Orbit, HiddenShadow, and Blink all bypass AIPath.destination entirely (canMove is
    /// disabled for these three in ApplyMovementPreset), so Seeker's own built-in path gizmo —
    /// which only draws a calculated path — has nothing to show for any of them. Draws a
    /// stand-in gizmo per pattern instead, reading straight from the same private fields each
    /// pattern's own MoveAlong…() method already uses.
    ///
    /// Deliberately OnDrawGizmos, not OnDrawGizmosSelected — AIBase's own OnDrawGizmos (its
    /// destination-circle gizmo, see the destination-reset comment in ApplyMovementPreset)
    /// draws unconditionally, with no selection required, and normal Play-mode testing (cycling
    /// presets via DebugMovementPresetCycler while just watching the game) never actually
    /// selects the companion in the Hierarchy. OnDrawGizmosSelected would have meant these
    /// pattern gizmos silently never appeared during exactly that workflow, while AIBase's own
    /// always-on gizmo kept showing regardless — easy to mistake for "still showing the old
    /// path gizmo" rather than "ours never rendered at all."
    ///
    /// Kept live during Play Mode by RequestSceneViewRepaintIfGizmoRelevant (see OnEnable)
    /// driving repaints from EditorApplication.update, not from in here — see that method's
    /// comment for why.
    ///
    /// The default case below is a deliberate self-enforcing guard for the CONVENTION documented
    /// on CompanionMovementPatternType: any pattern that bypasses AIPath (canMove = false) has
    /// no other gizmo drawing it at all (Seeker's and AIBase's own always-on gizmos both have
    /// nothing to show for it — see ApplyMovementPreset's destination-reset/seeker.drawGizmos
    /// comments), so skipping a gizmo case for a new one wouldn't just be a missing "nice to
    /// have," it'd make that pattern's movement completely invisible in the Scene view. Logging
    /// a warning here means that gap surfaces the moment the new pattern is actually tested in
    /// the Editor, not discovered later by a confused "why can't I see anything" report.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!_showPatternGizmos || _target == null) return;

        switch (_pattern)
        {
            case CompanionMovementPatternType.Orbit:
                DrawOrbitGizmos();
                break;
            case CompanionMovementPatternType.HiddenShadow:
                DrawHiddenShadowGizmos();
                break;
            case CompanionMovementPatternType.Blink:
                DrawBlinkGizmos();
                break;
            default:
                if (!_aiPath.canMove)
                {
                    Debug.LogWarning($"[CompanionAI] Pattern '{_pattern}' bypasses AIPath (canMove = false) but has no gizmo case in OnDrawGizmos() — its movement is completely invisible in the Scene view. Add a Draw{_pattern}Gizmos() case (see Orbit/HiddenShadow/Blink for the pattern), per the CONVENTION documented on CompanionMovementPatternType.", this);
                }
                break;
        }
    }

    /// <summary>
    /// Orbit has one thing worth visualizing: the path itself. Just the circle the companion
    /// travels around the player, re-centering live as the player moves — no extra markers.
    /// </summary>
    private void DrawOrbitGizmos()
    {
        Vector3 center = _target.position + (Vector3)_orbitCenterOffset;
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
        DrawWireCircleXY(center, _orbitRadius);
    }

    /// <summary>
    /// HiddenShadow only has a meaningful "path" to show while Emerged (swaying at an idle
    /// anchor away from the player) — while Locked, it's glued directly to the player's feet
    /// with zero lag, so there's nothing separate to visualize; drawing anything then would
    /// just be redundant with the player's own position. Draws the idle anchor and its sway
    /// range only.
    /// </summary>
    private void DrawHiddenShadowGizmos()
    {
        if (_shadowPhase != ShadowPhase.Emerged) return;

        Gizmos.color = new Color(0.6f, 0.6f, 1f, 0.8f);
        Gizmos.DrawWireSphere(_shadowIdleAnchor, 0.12f);
        Gizmos.DrawLine(_shadowIdleAnchor + Vector3.left * _shadowSwayAmplitude, _shadowIdleAnchor + Vector3.right * _shadowSwayAmplitude);
    }

    /// <summary>
    /// Both _blinkNextDestination and _blinkPreviewDestination are real, committed teleport
    /// targets at all times now (a 2-deep queue — see _blinkPreviewDestination's doc comment),
    /// not just while Vanished, so this draws continuously whenever Blink is the active pattern
    /// — no phase gate. Drawn as a reticle (circle + crosshair) rather than a plain sphere so it
    /// clearly reads as "target," distinct from Orbit's path circle.
    ///
    /// _blinkNextDestination (the one the companion is about to actually teleport to) draws at
    /// full opacity/size; _blinkPreviewDestination (the one after that) at reduced
    /// opacity/size, so the two are visually distinguishable at a glance: solid = happening
    /// imminently, faint = happening after that.
    ///
    /// Deliberately bright magenta, not yellow — A* Pathfinding Project's own AIBase.OnDrawGizmos
    /// (unconditional, always on, completely unrelated to this pattern) draws its own yellow-gold
    /// "ShapeGizmoColor" circle at the companion's CURRENT position for every pattern, all the
    /// time. An earlier version of this reticle used a near-identical yellow-gold, which made it
    /// look like there were two markers of the same kind — one at the old position (AIBase's,
    /// always there) and one at the new (this one) — easy to misread as showing the wrong
    /// position or the wrong order. Magenta can't be confused with AIBase's marker at any zoom.
    /// </summary>
    private void DrawBlinkGizmos()
    {
        Gizmos.color = new Color(1f, 0.1f, 0.9f, 0.95f);
        Gizmos.DrawWireSphere(_blinkNextDestination, 0.2f);
        Gizmos.DrawLine(_blinkNextDestination + Vector3.left * 0.3f, _blinkNextDestination + Vector3.right * 0.3f);
        Gizmos.DrawLine(_blinkNextDestination + Vector3.up * 0.3f, _blinkNextDestination + Vector3.down * 0.3f);

        Gizmos.color = new Color(1f, 0.1f, 0.9f, 0.4f);
        Gizmos.DrawWireSphere(_blinkPreviewDestination, 0.14f);
        Gizmos.DrawLine(_blinkPreviewDestination + Vector3.left * 0.2f, _blinkPreviewDestination + Vector3.right * 0.2f);
        Gizmos.DrawLine(_blinkPreviewDestination + Vector3.up * 0.2f, _blinkPreviewDestination + Vector3.down * 0.2f);
    }

    /// <summary>
    /// Gizmos has no built-in flat-circle primitive (Handles.DrawWireDisc would work, but
    /// lives in UnityEditor, which a plain runtime MonoBehaviour can't reference without
    /// #if UNITY_EDITOR guards) — draws one as a ring of line segments instead.
    /// </summary>
    private static void DrawWireCircleXY(Vector3 center, float radius, int segments = 32)
    {
        if (radius <= 0f) return;

        Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
    }
}
