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

    private Seeker _seeker;
    private AIPath _aiPath;
    private Rigidbody2D _rigidbody2D;
    private Rigidbody2D _targetRigidbody2D;
    private Vector3 _lastTargetPosition;
    private Vector3 _smoothedTargetDirection = Vector2.down;

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

    private float _patternTimer;
    private float _dashAngleRadians;
    private float _currentDashInterval;
    private float _currentDashOvershoot;
    private float _orbitAngleRadians;
    private bool _isPaused;

    public CompanionMovementState CurrentState { get; private set; } = CompanionMovementState.Idle;

    private static readonly int AnimParamIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimParamIsRunning = Animator.StringToHash("IsRunning");

    private void Awake()
    {
        _seeker = GetComponent<Seeker>();
        _aiPath = GetComponent<AIPath>();
        _rigidbody2D = GetComponent<Rigidbody2D>();

        // We drive the sprite ourselves (same flip approach as PlayerController_SideScroll)
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
    }

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

        // Orbit bypasses AIPath's own gradual pathfinding movement entirely (see
        // FixedUpdate/MoveAlongOrbit below) — its lag was exactly what read as "delayed."
        // AIPath must not also be trying to drive the same Rigidbody2D at the same time.
        _aiPath.canMove = _pattern != CompanionMovementPatternType.Orbit;

        // Reset runtime state so switching patterns doesn't carry over a stale phase/angle
        // from whatever pattern was active before.
        _patternTimer = 0f;
        _currentDashInterval = 0f; // forces DashThrough to pick a fresh angle/distance on the very next frame
        _isPaused = false;
        _dashAngleRadians = Mathf.Atan2(_smoothedTargetDirection.y, _smoothedTargetDirection.x);

        // Start orbiting from wherever the companion currently is relative to the player,
        // rather than snapping to angle 0 — avoids a visible jump the moment Orbit is selected.
        Vector3 currentOffset = FlattenToXY(transform.position - _target.position - (Vector3)_orbitCenterOffset);
        _orbitAngleRadians = currentOffset.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(currentOffset.y, currentOffset.x)
            : 0f;
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
        // script's intent (PlayerController_SideScroll re-asserts it every FixedUpdate,
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

        // Orbit is handled in FixedUpdate instead (see FixedUpdate/MoveAlongOrbit below) —
        // it bypasses AIPath's destination-seeking entirely (canMove is disabled for this
        // pattern in ApplyMovementPreset) and needs to be driven on the physics tick, not
        // the render tick, to track the player's own Rigidbody2D-driven movement tightly.
        if (_pattern != CompanionMovementPatternType.Orbit)
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
    /// Drives Orbit on the physics tick, matching how the player's own Rigidbody2D moves
    /// (PlayerController_SideScroll applies velocity in FixedUpdate) — running this in
    /// Update() instead caused a render/physics cadence mismatch that read as lag once the
    /// player started moving, on top of the reactive-chase lag fixed below.
    /// </summary>
    private void FixedUpdate()
    {
        if (_target == null || _pattern != CompanionMovementPatternType.Orbit) return;
        MoveAlongOrbit();
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
}
