using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Activates and deactivates world chunks based on player proximity.
/// Uses a timed coroutine — not Update() — to avoid per-frame overhead.
/// Chunks are parented GameObjects named Chunk_X_Y and listed in _allChunks.
/// </summary>
[DefaultExecutionOrder(-10)]
public class WorldChunkManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's Transform. Assign in Inspector.")]
    [SerializeField] private Transform _playerTransform;

    [Header("Chunks")]
    [Tooltip("All world chunks. Assign in Inspector — include currently inactive ones.")]
    [SerializeField] private List<GameObject> _allChunks = new();

    [Header("Tuning")]
    [Tooltip("Distance at which a chunk becomes active. Camera viewport ~20 units at 16 PPU; 30 gives a 5-unit lookahead buffer.")]
    [SerializeField] private float _activateRadius = 30f;

    [Tooltip("Distance at which a chunk deactivates. Must be larger than _activateRadius to prevent toggling on the boundary.")]
    [SerializeField] private float _deactivateRadius = 40f;

    [Tooltip("Seconds between proximity checks. 0.5s is fine for walking speed; lower for faster traversal.")]
    [SerializeField] private float _checkInterval = 0.5f;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        StartCoroutine(ChunkUpdateLoop());
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private IEnumerator ChunkUpdateLoop()
    {
        var wait = new WaitForSeconds(_checkInterval);
        while (true)
        {
            EvaluateChunks();
            yield return wait;
        }
    }

    private void EvaluateChunks()
    {
        if (_playerTransform == null) return;

        Vector3 playerPos = _playerTransform.position;

        foreach (GameObject chunk in _allChunks)
        {
            if (chunk == null) continue;

            float dist = Vector3.Distance(playerPos, chunk.transform.position);
            bool isActive = chunk.activeSelf;

            if (!isActive && dist <= _activateRadius)
                chunk.SetActive(true);
            else if (isActive && dist > _deactivateRadius)
                chunk.SetActive(false);
        }
    }
}
