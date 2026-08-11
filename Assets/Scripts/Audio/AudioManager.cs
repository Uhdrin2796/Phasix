using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Pooled one-shot SFX playback for the 9 combat feedback events wired in BattleAudioVfxHooks.cs
/// (2026-08-10 — Phase 3 close-out pass, first real content behind those previously-empty
/// handlers). Pure playback service — never subscribes to EventBus itself; BattleAudioVfxHooks
/// (and BattleManager, for the couple of directly-called VFX/audio hooks) call these Play*
/// methods, keeping exactly one place owning "when does audio get poked."
///
/// Uses UnityEngine.Pool.ObjectPool&lt;AudioSource&gt; per the Technical Directive's explicit
/// pooling mandate (§12.4: "Any object that is frequently created and destroyed must use a
/// pool... Use [UnityEngine.Pool.ObjectPool&lt;T&gt;] instead of writing your own") — first
/// pooling implementation in the codebase, since nothing else needed one yet.
///
/// Every clip comes from the assigned AudioCueCatalog asset — swapping placeholder generated
/// audio for real assets later is a drag-and-drop on that asset, zero code changes here.
///
/// Inspector Setup:
///   1. Create an empty GameObject in SampleScene named "_AudioManager"
///   2. Attach this script to it
///   3. Assign Assets/Data/Audio/AudioCueCatalog.asset to Cue Catalog
///   4. The object persists across all scene loads automatically (DontDestroyOnLoad), same
///      pattern as GameManager — needed so it survives BattleScene_Main's additive load/unload.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Cue Catalog")]
    [Tooltip("Assign Assets/Data/Audio/AudioCueCatalog.asset. Every Play* call below is a null-safe no-op if the corresponding slot is unauthored (or this reference itself is empty).")]
    [SerializeField] private AudioCueCatalog _cueCatalog;

    [Header("Pooling")]
    [Tooltip("Pooled AudioSource count for overlapping one-shot playback. Combat events can overlap (e.g. a hit landing right as a bond milestone chimes) so more than 1 is needed. Placeholder value, not playtested.")]
    [SerializeField] private int _poolSize = 8;

    private ObjectPool<AudioSource> _sourcePool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sourcePool = new ObjectPool<AudioSource>(
            createFunc: CreatePooledSource,
            actionOnGet: source => source.gameObject.SetActive(true),
            actionOnRelease: source =>
            {
                source.Stop();
                source.gameObject.SetActive(false);
            },
            actionOnDestroy: source => Destroy(source.gameObject),
            collectionCheck: false,
            defaultCapacity: Mathf.Max(1, _poolSize),
            maxSize: Mathf.Max(1, _poolSize));
    }

    private AudioSource CreatePooledSource()
    {
        var sourceObject = new GameObject("PooledAudioSource");
        sourceObject.transform.SetParent(transform);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    public void PlayBattleWon() => PlayClip(_cueCatalog != null ? _cueCatalog.BattleWonClip : null);
    public void PlayBattleLost() => PlayClip(_cueCatalog != null ? _cueCatalog.BattleLostClip : null);
    public void PlayBattleFled() => PlayClip(_cueCatalog != null ? _cueCatalog.BattleFledClip : null);
    public void PlaySkillUsed() => PlayClip(_cueCatalog != null ? _cueCatalog.SkillUsedClip : null);
    public void PlayTimedInputSuccess() => PlayClip(_cueCatalog != null ? _cueCatalog.TimedInputSuccessClip : null);
    public void PlayHitImpact(PrimalType damagedCreatureType) => PlayClip(_cueCatalog != null ? _cueCatalog.GetHitImpactClip(damagedCreatureType) : null);
    public void PlayBondMilestone() => PlayClip(_cueCatalog != null ? _cueCatalog.BondMilestoneClip : null);
    public void PlayEvolved() => PlayClip(_cueCatalog != null ? _cueCatalog.EvolvedClip : null);
    public void PlayCaptured() => PlayClip(_cueCatalog != null ? _cueCatalog.CapturedClip : null);

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return; // catalog slot not yet authored — silent no-op, not an error

        AudioSource source = _sourcePool.Get();
        source.clip = clip;
        source.Play();
        StartCoroutine(ReleaseAfterPlayback(source, clip.length));
    }

    private IEnumerator ReleaseAfterPlayback(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        _sourcePool.Release(source);
    }
}
