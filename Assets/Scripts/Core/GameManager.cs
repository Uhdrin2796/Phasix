using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central singleton anchor for Phasix game systems, and (2026-08 session, see DECISIONS.md ->
/// [Save]) the single owner of "how does the party get populated at boot" — auto-loads the most
/// recently saved slot if one exists, or seeds a fallback starter Phasix if not (absorbing
/// DebugPartyBootstrap.cs's old responsibility, deleted this session per its own "TEMPORARY...
/// DELETE once superseded" doc comment).
///
/// Owning BOTH the load attempt and the fallback-seed decision in one method sidesteps a real
/// hazard: Unity doesn't guarantee Start() order between different scripts on different
/// GameObjects, so a separate DebugPartyBootstrap script checking "did GameManager already load
/// something?" in its OWN Start() would be a race. Load-then-maybe-seed happening sequentially
/// inside a single method has no such ordering question.
///
/// Runs on SceneManager.sceneLoaded rather than Start() (2026-08 follow-up bugfix — live-verified:
/// the debug "New Game" button reloads the active scene while this object survives via
/// DontDestroyOnLoad, and Start() only ever fires ONCE per component instance — it does NOT
/// re-run just because the scene around a surviving DontDestroyOnLoad object gets reloaded. A
/// Start()-based version silently never re-seeded the party after a debug reset, since nothing
/// called it a second time. sceneLoaded fires for every load, including the very first one at
/// boot, so this one handler covers both cases without a separate first-boot path.
///
/// Inspector Setup:
///   1. Create an empty GameObject in SampleScene named "_GameManager"
///   2. Attach this script to it
///   3. Assign SkillDatabase (Assets/Data/Skills/SkillDatabase.asset), SpeciesDatabase
///      (Assets/Data/Species/SpeciesDatabase.asset), and a Fallback Starter Species (any
///      Assets/Data/Species/ asset — used only when no save exists to load)
///   4. The object persists across all scene loads automatically
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Save/Load")]
    [Tooltip("Assign Assets/Data/Skills/SkillDatabase.asset.")]
    [SerializeField] private SkillDatabase _skillDatabase;

    [Tooltip("Assign Assets/Data/Species/SpeciesDatabase.asset.")]
    [SerializeField] private SpeciesDatabase _speciesDatabase;

    [Header("Fallback Starter")]
    [Tooltip("Seeded into the party ONLY when no save exists to load (first-ever launch, or right after the debug New Game reset skips loading). Assign a test asset from Assets/Data/Species/.")]
    [SerializeField] private PhasixData _fallbackStarterSpecies;

    /// <summary>Set by ResetToNewGame just before reloading the scene — read once by the next Start(), then cleared. Survives the reload because this whole object does (DontDestroyOnLoad).</summary>
    private bool _skipNextLoad;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /// <summary>Fires for every scene load this object survives, including the first one at boot — see the class doc comment for why this replaces a one-shot Start().</summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool loaded = TryAutoLoad();

        // Only seed a fallback if nothing loaded AND the party is still genuinely empty — guards
        // against a scene reload that already has a live party (shouldn't happen today, but keeps
        // this handler idempotent rather than assuming it's always "the party is empty").
        if (!loaded && PartySystem.Instance != null && PartySystem.Instance.GetSlot(0) == null)
        {
            SeedFallbackStarter();
        }
    }

    private bool TryAutoLoad()
    {
        bool skip = _skipNextLoad;
        _skipNextLoad = false;
        if (skip) return false;

        if (PartySystem.Instance == null) return false;
        if (!SaveSystem.TryGetNewestSlot(out int slot)) return false;
        if (!SaveSystem.TryLoad(slot, out SaveFile saveFile)) return false;

        SaveSystem.ApplyToPartySystem(saveFile, PartySystem.Instance, _speciesDatabase);
        Debug.Log($"[GameManager] Loaded save slot {slot} (saved {saveFile.savedAtIso8601}).");
        return true;
    }

    private void SeedFallbackStarter()
    {
        if (_fallbackStarterSpecies == null)
        {
            Debug.LogWarning("[GameManager] No save found and no Fallback Starter Species assigned — party stays empty.");
            return;
        }

        PhasixRuntimeData runtime = WildSpawnSystem.CreateWildInstance(_fallbackStarterSpecies, _skillDatabase);
        ApplyDebugPlaytestLoadout(runtime);
        int slot = PartySystem.Instance.AddToParty(runtime);
        Debug.Log($"[GameManager] No save found — seeded fallback starter ({_fallbackStarterSpecies.SpeciesName}) into slot {slot}.");
    }

    /// <summary>
    /// TEMPORARY debug override (2026-08 follow-up — user: "For now also make the default to
    /// include the C,H,A,R,K,C1 so i can play test them as well") — force-equips all 5 Standard
    /// built-in moves plus C1 on the fallback starter, in that exact order, so the just-built
    /// "built-ins are real, equippable skills" feature has enough pre-equipped variety to
    /// playtest immediately without manually dragging 6 skills into place first. Requires the
    /// fallback starter species' tier to have a SkillSlotCapacity cap that can hold all 6
    /// (Test_FireType bumped to Tier 5 alongside this change) — if the cap can't fit them, this
    /// no-ops rather than silently exceeding the tier-cap invariant everywhere else in the
    /// codebase assumes holds (UI tier-lock display, SkillLoadoutSystem's own checks). DELETE
    /// once playtesting is done — this is not a real starter-loadout design, see
    /// WildSpawnSystem.SeedInitialSkills' own doc comment for the actual (placeholder)
    /// round-robin default this overrides.
    /// </summary>
    private void ApplyDebugPlaytestLoadout(PhasixRuntimeData runtime)
    {
        if (_skillDatabase == null || runtime.speciesData == null) return;

        int maxSlots = SkillSlotCapacity.GetActiveSlotRange(runtime.speciesData.EvolutionTier).max;

        var desiredOrder = new[]
        {
            BuiltInMoveType.Attack, BuiltInMoveType.Charge, BuiltInMoveType.Heal,
            BuiltInMoveType.Regen, BuiltInMoveType.Capture,
        };
        var desiredGuids = new System.Collections.Generic.List<string>();

        foreach (BuiltInMoveType move in desiredOrder)
        {
            foreach ((SkillData skill, string guid) in _skillDatabase.AllSkills)
            {
                if (skill.BuiltInMove == move) { desiredGuids.Add(guid); break; }
            }
        }

        foreach ((SkillData skill, string guid) in _skillDatabase.AllSkills)
        {
            if (skill.SkillName == "C1") { desiredGuids.Add(guid); break; }
        }

        if (desiredGuids.Count > maxSlots)
        {
            Debug.LogWarning($"[GameManager] Debug playtest loadout needs {desiredGuids.Count} slots but this tier only allows {maxSlots} — skipping override, using the normal seeded loadout instead.");
            return;
        }

        foreach (string guid in desiredGuids)
        {
            if (!runtime.learnedSkillGuids.Contains(guid)) runtime.learnedSkillGuids.Add(guid);
        }
        runtime.equippedSkillGuids.Clear();
        runtime.equippedSkillGuids.AddRange(desiredGuids);
    }

    /// <summary>Saves the current party into the given slot (0-2), overwriting whatever was there. Called by the Save tab.</summary>
    public void SaveToSlot(int slot)
    {
        if (PartySystem.Instance == null) return;
        SaveSystem.Save(slot, PartySystem.Instance, _speciesDatabase);
    }

    /// <summary>
    /// Debug "New Game" reset — reloads the active scene without auto-loading any save, so every
    /// subsystem (party, active companion instance, camera, chunks) gets a genuinely fresh start
    /// without hand-unwinding each one individually. Deliberately does NOT delete any save files —
    /// existing saves stay on disk untouched; the next explicit Save still overwrites whichever
    /// slot the player picks, same as any other session.
    /// </summary>
    public void ResetToNewGame()
    {
        _skipNextLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // TODO: Phase 3 — add reference to BattleManager when built
    // TODO: Phase 4 — add reference to AuraManager when built
    // TODO: Phase 4 — add reference to EvolutionManager when built
}
