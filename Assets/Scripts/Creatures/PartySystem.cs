using UnityEngine;

/// <summary>
/// Party roster and active-companion spawning (Roadmap_v2.md Wk 12-13). Up to 3 slots;
/// only the active slot has a physical presence in the world — the rest are just data
/// ("stored"). Switching the active slot re-skins and re-targets a single persistent
/// companion GameObject rather than destroying/instantiating one per switch, so this
/// never violates the "no Instantiate/Destroy in a loop" architecture rule even though
/// slot-switching itself isn't pooled.
///
/// A MonoBehaviour singleton (not static, unlike BondSystem/PersonalitySystem) because it
/// owns an Inspector-assigned prefab reference and a live spawned instance — the same
/// category as GameManager, not a stateless rules layer. See DECISIONS.md → [Creatures].
/// </summary>
public class PartySystem : MonoBehaviour
{
    public const int MaxPartySize = 3;

    [Header("Companion")]
    [Tooltip("Prefab representing whichever party slot is active. Assign Phasix_Placeholder.prefab.")]
    [SerializeField] private GameObject _companionPrefab;

    [Tooltip("The player's Transform, followed by the active companion. Assign the player GameObject.")]
    [SerializeField] private Transform _playerTransform;

    [Tooltip("World-space offset from the player where the companion spawns. Must be large enough that the companion's and player's colliders don't start overlapping (their radii sum to ~0.8) — spawning exactly on top of the player produced a large, compounding separation push. Range: 1-2 units.")]
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, -1.2f, 0f);

    public static PartySystem Instance { get; private set; }

    private readonly PhasixRuntimeData[] _slots = new PhasixRuntimeData[MaxPartySize];
    private int _activeSlotIndex = -1;

    private GameObject _companionInstance;
    private CompanionAI _companionAI;
    private PhasixPlaceholderVisual _companionVisual;

    public int ActiveSlotIndex => _activeSlotIndex;
    public PhasixRuntimeData ActiveCompanion => _activeSlotIndex >= 0 ? _slots[_activeSlotIndex] : null;

    /// <summary>The spawned companion's CompanionAI, if a companion is currently active. Null otherwise (no slot activated yet). Used by BattleManager to pause/resume A* pathfinding during battle — see CompanionAI.SetPaused.</summary>
    public CompanionAI ActiveCompanionAI => _companionAI;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>Adds a Phasix to the first empty slot. Returns the slot index, or -1 if the party is full.</summary>
    public int AddToParty(PhasixRuntimeData phasix)
    {
        if (phasix == null) return -1;

        for (int i = 0; i < MaxPartySize; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = phasix;
                if (_activeSlotIndex < 0) SetActiveSlot(i);
                return i;
            }
        }

        return -1;
    }

    /// <summary>Switches which slot's Phasix physically follows the player. No-op if the slot is empty.</summary>
    public void SetActiveSlot(int index)
    {
        if (index < 0 || index >= MaxPartySize || _slots[index] == null) return;

        _activeSlotIndex = index;
        EnsureCompanionInstance();

        _companionVisual.ApplyFromSpeciesData(_slots[index].speciesData);
        _companionAI.SetTarget(_playerTransform);
    }

    public PhasixRuntimeData GetSlot(int index)
    {
        return (index >= 0 && index < MaxPartySize) ? _slots[index] : null;
    }

    /// <summary>
    /// Directly sets a specific slot's contents, bypassing AddToParty's "first empty slot"
    /// semantics (2026-08 session, see DECISIONS.md -> [Save]) — used by SaveSystem to restore a
    /// save's exact slot alignment (a save's slot 2 must load back into slot 2, not wherever
    /// AddToParty would have placed it). Does NOT touch the active companion visual or
    /// _activeSlotIndex; call SetActiveSlot separately once all slots are populated.
    /// </summary>
    public void SetSlot(int index, PhasixRuntimeData phasix)
    {
        if (index < 0 || index >= MaxPartySize) return;
        _slots[index] = phasix;
    }

    private void EnsureCompanionInstance()
    {
        if (_companionInstance != null) return;

        // Never spawn exactly at the player's position — two fully-coincident colliders force
        // the physics engine into a large separation response, which combined with the
        // companion's own AI movement produced a compounding push on the player right at
        // startup. See LESSONS_LEARNED.md → [Physics].
        Vector3 spawnPosition = _playerTransform.position + _spawnOffset;
        _companionInstance = Instantiate(_companionPrefab, spawnPosition, Quaternion.identity);
        _companionInstance.name = "ActiveCompanion";
        _companionAI = _companionInstance.GetComponent<CompanionAI>();
        _companionVisual = _companionInstance.GetComponent<PhasixPlaceholderVisual>();
    }
}
