using UnityEngine;

/// <summary>
/// Lives on the wild creature itself (Phasix_WildEncounter.prefab), not on the spawn point —
/// the actual encounter is still contact-based (Combat_Directive_v0_1_0.md: "When the player's
/// overworld sprite contacts an enemy Phasix sprite..."), matching the real Pokemon/Digimon-style
/// model rather than an invisible trigger zone. Wk 14-16 scaffold: Engage has no BattleManager to
/// hand off to yet (Phase 3), so it resolves identically to Flee.
///
/// AUD-005 (repo audit, 2026-08): added Patrol/Alert movement so the creature is something a
/// player can actually notice and route around, not just a stationary contact-trigger. Player
/// avoidance play is: read the patrol path from a distance, stay outside the vision cone, walk
/// around. Deliberately NOT overworld "lanes" carrying the battle stage's 7-lane depth system in
/// — see Combat_Directive_v0_1_0.md Part 3 "Lane Avoidance — Overworld Carry-Over" and
/// DECISIONS.md for why that stays an unresolved future idea rather than something this pass
/// commits to; nothing else in the world design docs references overworld lanes, and retrofitting
/// them onto the free-movement top-down controller would be its own dedicated design session.
///
/// Movement is a plain back-and-forth patrol + straight-line chase toward the player once
/// detected, via a Kinematic Rigidbody2D (MovePosition every FixedUpdate — no AIPath/Seeker,
/// unlike CompanionAI; this creature has no need for pathfinding around obstacles for a first
/// pass, and the prefab intentionally stays "no Rigidbody2D/Seeker/AIPath" heavy). Known
/// limitation: an Alert-state creature chasing in a straight line CAN walk through solid
/// Obstacles-layer geometry, since Kinematic bodies don't get physically stopped by collisions —
/// acceptable for this scaffold since the player's own move speed always exceeds the chase speed
/// (see _alertSpeed tooltip), but worth flagging if a real level has geometry that would make a
/// wall-clipping chase look obviously wrong.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class WildEncounterCreature : MonoBehaviour
{
    private enum State { Patrol, Alert }

    [Header("Patrol")]
    [Tooltip("Speed while patrolling back and forth, in world units/second. Kept well below the " +
             "player's own move speed (5) so a patrolling creature is always easy to outpace on " +
             "foot even before Sprint. Range: 0.5–2.")]
    [SerializeField] private float _patrolSpeed = 1f;

    [Tooltip("Half-length of the back-and-forth patrol path along the local +X/-X axis, centered " +
             "on the creature's spawn position, in world units. Straight-line only for this pass " +
             "— no waypoint list. Placement-dependent: pick a value that keeps the path clear of " +
             "walls/decorations for wherever this spawn point sits. Range: 0.5–3.")]
    [SerializeField] private float _patrolHalfRange = 1.5f;

    [Header("Detection")]
    [Tooltip("How far the creature can notice the player, in world units. Range: 2–6.")]
    [SerializeField] private float _detectionRadius = 3f;

    [Tooltip("Full width of the vision cone centered on the creature's current facing/movement " +
             "direction, in degrees. Range: 60–150.")]
    [SerializeField] private float _detectionAngle = 100f;

    [Tooltip("How often the detection check runs, in seconds. Deliberately throttled rather than " +
             "checked every frame (CLAUDE.md: no heavy logic in Update()). Range: 0.1–0.3.")]
    [SerializeField] private float _detectionCheckInterval = 0.15f;

    [Tooltip("Walls/obstacles that block line of sight for detection — a creature can't see the " +
             "player through a wall even if the radius/cone would otherwise allow it. Defaults " +
             "to the 'Obstacles' layer, same mask PlayerTopDownController's corner correction " +
             "uses.")]
    [SerializeField] private LayerMask _lineOfSightBlockMask = 1 << 8;

    [Header("Alert")]
    [Tooltip("Speed while actively closing on a detected player, in world units/second. Kept " +
             "below the player's base move speed (5) so noticing a creature is never an " +
             "inescapable threat — Alert makes a creature harder to avoid *noticing*, not " +
             "impossible to outrun. Range: 1.5–4.")]
    [SerializeField] private float _alertSpeed = 2f;

    [Tooltip("Seconds without line-of-sight to the player before an alerted creature gives up " +
             "and resumes patrolling. Range: 1–4.")]
    [SerializeField] private float _loseInterestDelay = 2f;

    // Cached in Awake, never in Update.
    private Rigidbody2D _rb;

    private PhasixRuntimeData _runtimeData;
    private bool _contacted;

    private State _state = State.Patrol;
    private Vector2 _spawnPosition;
    private Vector2 _patrolTarget;
    private bool _patrolTowardPositiveX = true;

    // Movement direction from the last FixedUpdate step — doubles as the vision cone's facing
    // direction, so the cone always points where the creature is actually walking.
    private Vector2 _facingDirection = Vector2.down;

    // Resolved once in Awake — no existing "find the player" singleton/convention in the
    // codebase to hook into (CompanionAI's _target is assigned externally by PartySystem, which
    // doesn't apply here since this creature is spawned by EncounterTrigger, not PartySystem).
    // A single FindFirstObjectByType at spawn time is cheap enough not to need one.
    private Transform _playerTransform;

    private float _detectionCheckTimer;
    private float _timeSinceLastSeen;

    // TODO: pending design — placeholder-first art (no real "!" icon asset yet). A plain tinted
    // circle child (AlertIndicator, toggled active/inactive) standing in for a proper alert icon
    // until real UI/VFX art exists — see PhasixPlaceholderVisual's doc comment for the same
    // placeholder-first philosophy applied to the creature's own body sprite.
    private GameObject _alertIndicator;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;

        _spawnPosition = _rb.position;
        _patrolTarget = _spawnPosition + new Vector2(_patrolHalfRange, 0f);

        var playerController = FindFirstObjectByType<PlayerTopDownController>();
        if (playerController != null) _playerTransform = playerController.transform;

        Transform indicator = transform.Find("AlertIndicator");
        if (indicator != null) _alertIndicator = indicator.gameObject;
    }

    private void Update()
    {
        if (_contacted || _playerTransform == null) return;

        _detectionCheckTimer += Time.deltaTime;
        if (_detectionCheckTimer < _detectionCheckInterval) return;

        _detectionCheckTimer = 0f;
        RunDetectionCheck();
    }

    private void FixedUpdate()
    {
        if (_contacted) return;

        if (_state == State.Alert)
        {
            StepToward(_playerTransform.position, _alertSpeed);
            return;
        }

        StepToward(_patrolTarget, _patrolSpeed);

        if (Vector2.Distance(_rb.position, _patrolTarget) < 0.05f)
        {
            _patrolTowardPositiveX = !_patrolTowardPositiveX;
            _patrolTarget = _spawnPosition + new Vector2(_patrolTowardPositiveX ? _patrolHalfRange : -_patrolHalfRange, 0f);
        }
    }

    /// <summary>Moves toward destination at speed, without overshooting, and updates the facing direction.</summary>
    private void StepToward(Vector2 destination, float speed)
    {
        Vector2 toDestination = destination - _rb.position;
        if (toDestination.sqrMagnitude < 0.0001f) return;

        Vector2 direction = toDestination.normalized;
        _facingDirection = direction;

        Vector2 step = direction * speed * Time.fixedDeltaTime;
        if (step.sqrMagnitude > toDestination.sqrMagnitude) step = toDestination;

        _rb.MovePosition(_rb.position + step);
    }

    /// <summary>Radius + facing-cone + line-of-sight check. Throttled — see _detectionCheckInterval.</summary>
    private void RunDetectionCheck()
    {
        Vector2 toPlayer = (Vector2)_playerTransform.position - _rb.position;
        float distance = toPlayer.magnitude;

        bool canSeePlayer = distance <= _detectionRadius
            && Vector2.Angle(_facingDirection, toPlayer) <= _detectionAngle * 0.5f
            && !Physics2D.Linecast(_rb.position, _playerTransform.position, _lineOfSightBlockMask);

        if (canSeePlayer)
        {
            if (_state != State.Alert) SetAlertIndicatorVisible(true);
            _state = State.Alert;
            _timeSinceLastSeen = 0f;
            return;
        }

        if (_state != State.Alert) return;

        _timeSinceLastSeen += _detectionCheckInterval;
        if (_timeSinceLastSeen >= _loseInterestDelay)
        {
            _state = State.Patrol;
            SetAlertIndicatorVisible(false);
        }
    }

    private void SetAlertIndicatorVisible(bool visible)
    {
        if (_alertIndicator != null) _alertIndicator.SetActive(visible);
    }

    /// <summary>Assigned by EncounterTrigger immediately after Instantiate.</summary>
    public void SetRuntimeData(PhasixRuntimeData runtimeData)
    {
        _runtimeData = runtimeData;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_contacted) return;
        if (!other.TryGetComponent<PlayerTopDownController>(out var player)) return;

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

    private void HandleFlee(PlayerTopDownController player)
    {
        EventBus.Raise_WildEncounterFled(_runtimeData);
        Resolve(player);
    }

    private void HandleEngage(PlayerTopDownController player)
    {
        // TODO: no BattleManager exists yet (Phase 3) — real Engage will trigger the
        // Combat_Directive cinematic transition into an additively-loaded BattleScene_Main
        // instead of this. For now, resolves identically to Flee.
        Debug.Log($"[WildEncounterCreature] Engage requested for {_runtimeData.speciesData.SpeciesName} — no BattleManager yet, scaffold resolves as Flee.");
        EventBus.Raise_WildEncounterEngageRequested(_runtimeData);
        Resolve(player);
    }

    private void Resolve(PlayerTopDownController player)
    {
        EncounterPromptController.Instance.Hide();
        player.UnfreezeMovement();
        Destroy(gameObject);
    }
}
