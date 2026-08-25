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
/// Runs on BOTH Start() and SceneManager.sceneLoaded (2026-08-12, two bugs found and fixed one
/// after the other, see RunBootSequence's own doc comment for the second): a Start()-ONLY version
/// silently never re-seeds the party after the debug "New Game" reset, since this object survives
/// scene reloads via DontDestroyOnLoad and Start() only ever fires ONCE per component instance —
/// it does NOT re-run just because the scene around a surviving object gets reloaded. But a
/// sceneLoaded-ONLY version (what this used to be) silently never runs on a genuinely cold Editor
/// Play press either — Unity's Editor does not fire sceneLoaded for the scene that was already
/// open when Play is pressed, only for scenes loaded at runtime via SceneManager.LoadScene. Start()
/// covers the boot sceneLoaded misses; sceneLoaded covers every reload Start() misses. See
/// RunBootSequence for why calling both is safe (no double-seeding).
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

    /// <summary>
    /// Handles every scene load AFTER the very first one — see Start() for why the first one needs
    /// its own separate call. 2026-08-12 bugfix: guarded to LoadSceneMode.Single only —
    /// BattleTransition loads BattleScene_Main additively (the overworld stays loaded underneath
    /// combat), and this handler used to fire for THAT load too, silently re-applying the last
    /// on-disk save over the live PartySystem via TryAutoLoad right before
    /// BattleManager.BuildPlayerSide() reads it — clobbering whatever the player had just set in
    /// the Party menu's formation picker moments earlier (reported as "party members render
    /// stacked in battle" — see DECISIONS.md -> [Save]). The fallback-seed check below is guarded
    /// by the same early-return for the same reason: an additive battle load has no business
    /// re-seeding a starter either, even though nothing currently exercises that specific edge.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        RunBootSequence();
    }

    /// <summary>
    /// 2026-08-12 bugfix (user: "im just press the play button on the unity editor and ive never
    /// had an issue" — reported seeing an empty party requiring the debug Add Party Member button
    /// as a workaround). Root cause, confirmed live via a fresh manage_editor "play" with zero
    /// [GameManager] log lines appearing: Unity's Editor does NOT fire SceneManager.sceneLoaded for
    /// the scene that was already open when Play is pressed — that event only fires for scenes
    /// loaded at runtime via SceneManager.LoadScene (e.g. ResetToNewGame's reload below), which is
    /// why the debug "New Game" flow always worked while a genuinely cold Play press never actually
    /// ran TryAutoLoad/SeedFallbackStarter at all. This is a real, standalone Unity Editor gotcha —
    /// unrelated to this session's earlier additive-battle-load guard fix above, and predates it.
    ///
    /// Fix: also call RunBootSequence from Start(), which — unlike sceneLoaded — DOES reliably fire
    /// once on a genuinely fresh boot. Start() alone can't replace HandleSceneLoaded entirely
    /// (this object survives every later scene reload via DontDestroyOnLoad, and Start() never
    /// re-fires for a surviving instance — see the class doc comment), so both paths stay: Start()
    /// covers the first boot sceneLoaded misses, HandleSceneLoaded covers every reload after it.
    /// Safe to call from both without double-seeding: TryAutoLoad re-running is a harmless
    /// no-op-ish reload (same save into the same slot indices), and SeedFallbackStarter is already
    /// guarded by "is slot 0 still empty" — if Start() already seeded it, a hypothetical later
    /// double-fire (e.g. sceneLoaded firing for scene 0 in an actual built Player, unlike the
    /// Editor) sees a non-empty slot 0 and no-ops.
    /// </summary>
    private void Start()
    {
        RunBootSequence();
    }

    private void RunBootSequence()
    {
        bool loaded = TryAutoLoad();

        // Only seed a fallback if nothing loaded AND the party is still genuinely empty — guards
        // against a scene reload (or a Start()+sceneLoaded double-fire) that already has a live
        // party, keeping this idempotent rather than assuming it's always "the party is empty".
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
    ///
    /// 2026-08-12 follow-up (user: "add [the new melee Beat Sequence skill] on the action bar...
    /// just need a way to be able to live play test it") — Melee_Slash added to the same forced
    /// loadout, same reasoning: the just-built Beat Sequence framework (Approach/Windup/Attack/
    /// Return, real 7-lane movement) had no in-game way to trigger it without manually equipping
    /// it through the Party menu's skill web first. Looked up by SkillName ("Slash"), same pattern
    /// as C1 below, since it's not a BuiltInMoveType.
    ///
    /// 2026-08-12 follow-up #2 — BuiltInMoveType.Move briefly added to desiredOrder alongside the
    /// new formation grid system, then REMOVED the same session once Move stopped being an
    /// equippable skill-ring orb entirely — it's now a dedicated always-present icon
    /// (BattleHUDController's Move-drag flow) unconditionally available to every player creature,
    /// so it needs no force-equip debug hook. See DECISIONS.md -> [Combat].
    ///
    /// 2026-08-12 follow-up #3 (Group 1 archetypes — Instant Strike, Feint, Metronome, Jitter, see
    /// Attack_Pattern_Directive_v0_1_0.md Part 5/Part 1's build order): the four new SkillTreeType.
    /// Testing assets (Ranged_InstantStrike/Feint/Metronome/Jitter) added to the same forced loadout,
    /// same reasoning and same by-SkillName lookup pattern as Slash above — no in-game way to trigger
    /// the new PreEmptive response-timing path without equipping them first. Brings the forced set to
    /// 11 (5 Standard + C1 + Slash + 4 new), still under the fallback starter's Tier-5 cap of 12
    /// (SkillSlotCapacity), so the "skip override if it doesn't fit" guard below won't trigger.
    ///
    /// 2026-08-14 follow-up (Multi-Hit Volley, Attack_Pattern_Directive Part 5 Group 2's first
    /// archetype): "Volley" added the same way, same reasoning. This brings the forced set to
    /// EXACTLY 12 of the Tier-5 cap's 12 slots — zero slack left. A second Volley pattern (already
    /// planned as a follow-up) cannot be added to this same debug loadout without either raising
    /// SkillSlotCapacity's Tier-5 cap or dropping an existing debug skill from this list.
    ///
    /// 2026-08-17 follow-up (Charge & Release, Attack_Pattern_Directive Part 5 Group 2's second
    /// archetype — "Magma Burst") — the flagged 12/12 conflict above came due: adding a 13th name
    /// would trip the "skip override" guard below and silently fall back to the normal seeded
    /// loadout, defeating the whole point of this debug hook. Rather than raising
    /// SkillSlotCapacity's Tier-5 cap (a locked progression value per CLAUDE.md's Aura-requirements
    /// table, not a debug knob) or leaving Charge & Release untestable, "Jitter" was swapped out for
    /// "Magma Burst" — Jitter's own mechanic was already fully built/playtested in an earlier
    /// session, while Charge & Release is the thing that needs live validation right now. Purely a
    /// debug-loadout swap, freely reversible (Jitter's own asset/behavior is untouched) — flag to
    /// the user if Jitter needs to come back onto this list alongside Charge & Release later.
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

        // 2026-08-21: swapped out Slash/Instant Strike/Feint/Metronome/Magma Burst/Volley (all
        // fully built and playtested in earlier sessions) for the 5 Zone/Positional skills + Snare
        // (Root) — same "swap out validated stuff for what needs live validation right now" pattern
        // this method's own class doc comment already establishes for the Jitter->Charge & Release
        // swap. Slot count unchanged (still 7 named + 5 built-ins = 12, this tier's own cap — see
        // maxSlots below), so nothing needed reshuffling elsewhere. Flag to the user if any of the
        // swapped-out six need to come back onto this list later.
        var desiredNames = new[] { "C1", "Fault Line", "Rift Line", "Crossfire", "Overcharge", "Bolt Lance", "Snare" };
        foreach (string name in desiredNames)
        {
            foreach ((SkillData skill, string guid) in _skillDatabase.AllSkills)
            {
                if (skill.SkillName == name) { desiredGuids.Add(guid); break; }
            }
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
