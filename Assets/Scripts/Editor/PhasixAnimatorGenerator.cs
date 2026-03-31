// ============================================================
// PhasixAnimatorGenerator.cs
// Editor window: auto-generate Animator Controllers for Phasix creatures.
// Menu: Phasix/Animator Generator…
//
// Usage:
//   1. Enter the creature name (e.g. "DarkUhdrin")
//   2. Confirm or browse to the animation folder
//   3. Optionally enter a clip prefix filter (e.g. "uhdrin")
//   4. Click "Generate Controller"
//
// Clip discovery (by keyword in filename, case-insensitive):
//   idle        → Idle state
//   walk_back   → WalkBack state  (triggers 5-state machine)
//   walk        → WalkForward state (excluding walk_back matches)
//   run         → Run state
//   attack      → Attack state
//
// State machine selection (automatic):
//   walk_back found → 5-state (Idle, WalkForward, WalkBack, Run, Attack)
//   walk_back absent → 4-state (Idle, Walk, Run, Attack)
// ============================================================

using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class PhasixAnimatorGenerator : EditorWindow
{
    // ── UI state ──────────────────────────────────────────────────────────────

    [SerializeField] private string _creatureName  = "";
    [SerializeField] private string _animFolder    = "Assets/Animations/Creatures/";
    [SerializeField] private string _clipPrefix    = "";   // optional; leave blank to match any clip

    private string _lastStatus = "";
    private bool   _lastSuccess;

    // ── Window ────────────────────────────────────────────────────────────────

    [MenuItem("Phasix/Animator Generator\u2026")]   // "…" (U+2026)
    public static void OpenWindow()
    {
        var win = GetWindow<PhasixAnimatorGenerator>("Animator Generator");
        win.minSize = new Vector2(420f, 300f);
    }

    private void OnGUI()
    {
        GUILayout.Space(8f);
        EditorGUILayout.LabelField("Phasix — Animator Controller Generator", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Auto-discovers clips and wires the state machine.", EditorStyles.miniLabel);
        GUILayout.Space(12f);

        // ── Creature Name ────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Creature Name", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _creatureName = EditorGUILayout.TextField(_creatureName);
        if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(_creatureName))
        {
            // Auto-fill folder when name changes
            _animFolder = $"Assets/Animations/Creatures/{_creatureName}/";
        }

        GUILayout.Space(6f);

        // ── Animation Folder ─────────────────────────────────────────────────
        EditorGUILayout.LabelField("Animation Folder (.anim files live here)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        _animFolder = EditorGUILayout.TextField(_animFolder);
        if (GUILayout.Button("Browse…", GUILayout.Width(70f)))
        {
            string picked = EditorUtility.OpenFolderPanel(
                "Select Animation Folder",
                Application.dataPath, "");

            if (!string.IsNullOrEmpty(picked))
            {
                // Convert absolute OS path → project-relative "Assets/…"
                if (picked.StartsWith(Application.dataPath))
                    picked = "Assets" + picked.Substring(Application.dataPath.Length);

                _animFolder = picked.Replace('\\', '/').TrimEnd('/') + "/";
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6f);

        // ── Clip Prefix ───────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Clip Prefix Filter  (optional — e.g. \"uhdrin\")", EditorStyles.boldLabel);
        _clipPrefix = EditorGUILayout.TextField(_clipPrefix);

        GUILayout.Space(16f);

        // ── Generate button ───────────────────────────────────────────────────
        bool ready = !string.IsNullOrEmpty(_creatureName) && !string.IsNullOrEmpty(_animFolder);

        GUI.enabled = ready;
        if (GUILayout.Button("Generate Controller", GUILayout.Height(36f)))
        {
            Generate();
        }
        GUI.enabled = true;

        // ── Status line ───────────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(_lastStatus))
        {
            GUILayout.Space(8f);
            GUIStyle style = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                richText = true,
            };
            Color prevColor = GUI.color;
            GUI.color = _lastSuccess ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.6f);
            EditorGUILayout.LabelField(_lastStatus, style);
            GUI.color = prevColor;
        }
    }

    // ── Generation logic ──────────────────────────────────────────────────────

    private void Generate()
    {
        _lastStatus  = "";
        _lastSuccess = false;

        // ── Validate inputs ──────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(_creatureName))
        {
            SetStatus("Creature Name is required.", false);
            return;
        }

        string folder = _animFolder.Replace('\\', '/').TrimEnd('/') + "/";

        if (!AssetDatabase.IsValidFolder(folder.TrimEnd('/')))
        {
            SetStatus($"Folder not found in project:\n{folder}\nCreate it or click Browse…", false);
            return;
        }

        // ── Discover clips ───────────────────────────────────────────────────
        AnimationClip clipIdle        = FindClip(folder, "idle",       false);
        AnimationClip clipWalkForward = FindClip(folder, "walk",       true);   // excludes walk_back
        AnimationClip clipWalkBack    = FindClip(folder, "walk_back",  false);
        AnimationClip clipRun         = FindClip(folder, "run",        false);
        AnimationClip clipAttack      = FindClip(folder, "attack",     false);

        bool has5State = clipWalkBack != null;

        // ── Output path ───────────────────────────────────────────────────────
        string acPath = $"{folder}{_creatureName}_AC.controller";

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(acPath) != null)
        {
            Debug.LogWarning($"[PhasixAnimatorGenerator] Controller already exists — skipping: {acPath}");
            SetStatus($"⚠ Controller already exists — not overwriting:\n{acPath}\n\nDelete it manually to regenerate.", false);
            return;
        }

        EnsureFolder(folder.TrimEnd('/'));
        var ac = AnimatorController.CreateAnimatorControllerAtPath(acPath);
        Debug.Log($"[PhasixAnimatorGenerator] Created: {acPath}");

        if (has5State)
            Build5StateController(ac, clipIdle, clipWalkForward, clipWalkBack, clipRun, clipAttack);
        else
            Build4StateController(ac, clipIdle, clipWalkForward, clipRun, clipAttack);

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── Report ────────────────────────────────────────────────────────────
        string missingWarnings = BuildMissingReport(
            clipIdle, clipWalkForward, clipWalkBack, clipRun, clipAttack, has5State);

        string machineType = has5State ? "5-state (with WalkBack)" : "4-state";
        SetStatus(
            $"✓ {_creatureName}_AC.controller generated.\n" +
            $"Machine: {machineType}\n" +
            $"Path: {acPath}" +
            missingWarnings,
            string.IsNullOrEmpty(missingWarnings));
    }

    // ── 4-state machine: Idle / Walk / Run / Attack ───────────────────────────

    private static void Build4StateController(
        AnimatorController ac,
        AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip attack)
    {
        EnsureParameter(ac, "IsMoving",  AnimatorControllerParameterType.Bool);
        EnsureParameter(ac, "IsRunning", AnimatorControllerParameterType.Bool);
        EnsureParameter(ac, "Attack",    AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = ac.layers[0].stateMachine;

        AnimatorState stIdle   = EnsureState(sm, "Idle",   idle,   new Vector3(-200f,    0f));
        AnimatorState stWalk   = EnsureState(sm, "Walk",   walk,   new Vector3(    0f,   80f));
        AnimatorState stRun    = EnsureState(sm, "Run",    run,    new Vector3(  200f,    0f));
        AnimatorState stAttack = EnsureState(sm, "Attack", attack, new Vector3(    0f, -80f));

        sm.defaultState = stIdle;

        // Idle ↔ Walk
        AddBoolTransition(sm, stIdle, stWalk, "IsMoving", true);
        AddBoolTransition(sm, stWalk, stIdle, "IsMoving", false);

        // Walk ↔ Run
        AddBoolTransition(sm, stWalk, stRun,  "IsRunning", true);
        AddBoolTransition(sm, stRun,  stWalk, "IsRunning", false);

        // Any → Attack
        AddAnyAttackTransition(sm, stAttack, "Attack");

        // Attack → Idle
        AddExitTimeTransition(stAttack, stIdle);
    }

    // ── 5-state machine: Idle / WalkForward / WalkBack / Run / Attack ─────────

    private static void Build5StateController(
        AnimatorController ac,
        AnimationClip idle, AnimationClip walkFwd, AnimationClip walkBack,
        AnimationClip run,  AnimationClip attack)
    {
        EnsureParameter(ac, "IsMoving",     AnimatorControllerParameterType.Bool);
        EnsureParameter(ac, "IsRunning",    AnimatorControllerParameterType.Bool);
        EnsureParameter(ac, "IsWalkingBack",AnimatorControllerParameterType.Bool);
        EnsureParameter(ac, "IsAttacking",  AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = ac.layers[0].stateMachine;

        AnimatorState stIdle     = EnsureState(sm, "Idle",        idle,    new Vector3(-250f,    0f));
        AnimatorState stWalkFwd  = EnsureState(sm, "WalkForward", walkFwd, new Vector3(    0f,  100f));
        AnimatorState stWalkBack = EnsureState(sm, "WalkBack",    walkBack,new Vector3(    0f,  -60f));
        AnimatorState stRun      = EnsureState(sm, "Run",         run,     new Vector3(  250f,    0f));
        AnimatorState stAttack   = EnsureState(sm, "Attack",      attack,  new Vector3(    0f, -180f));

        sm.defaultState = stIdle;

        // Idle → WalkForward (IsMoving=true AND IsWalkingBack=false)
        {
            var t = stIdle.AddTransition(stWalkFwd);
            t.hasExitTime = false; t.duration = 0.25f;
            t.AddCondition(AnimatorConditionMode.If,    0f, "IsMoving");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsWalkingBack");
        }

        // Idle → WalkBack (IsMoving=true AND IsWalkingBack=true)
        {
            var t = stIdle.AddTransition(stWalkBack);
            t.hasExitTime = false; t.duration = 0.25f;
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsWalkingBack");
        }

        // WalkForward → Idle (IsMoving=false)
        AddBoolTransition(sm, stWalkFwd, stIdle, "IsMoving", false);

        // WalkBack → Idle (IsMoving=false)
        AddBoolTransition(sm, stWalkBack, stIdle, "IsMoving", false);

        // WalkForward ↔ WalkBack
        AddBoolTransition(sm, stWalkFwd,  stWalkBack, "IsWalkingBack", true);
        AddBoolTransition(sm, stWalkBack, stWalkFwd,  "IsWalkingBack", false);

        // Walk → Run (either walk state)
        AddBoolTransition(sm, stWalkFwd,  stRun, "IsRunning", true);
        AddBoolTransition(sm, stWalkBack, stRun, "IsRunning", true);

        // Run → WalkForward (IsRunning=false, IsWalkingBack=false)
        {
            var t = stRun.AddTransition(stWalkFwd);
            t.hasExitTime = false; t.duration = 0.25f;
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsWalkingBack");
        }

        // Run → WalkBack (IsRunning=false, IsWalkingBack=true)
        {
            var t = stRun.AddTransition(stWalkBack);
            t.hasExitTime = false; t.duration = 0.25f;
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");
            t.AddCondition(AnimatorConditionMode.If,    0f, "IsWalkingBack");
        }

        // Any → Attack
        AddAnyAttackTransition(sm, stAttack, "IsAttacking");

        // Attack → Idle
        AddExitTimeTransition(stAttack, stIdle);
    }

    // ── Clip discovery ────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the first .anim in <paramref name="folder"/> whose filename (without extension)
    /// contains <paramref name="keyword"/> (case-insensitive) and optionally starts with
    /// _clipPrefix. When <paramref name="excludeWalkBack"/> is true, filenames also containing
    /// "walk_back" are skipped — used so "walk" doesn't match "walk_back".
    /// </summary>
    private AnimationClip FindClip(string folder, string keyword, bool excludeWalkBack)
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder.TrimEnd('/') });

        foreach (string guid in guids)
        {
            string path     = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            // Optional prefix filter
            if (!string.IsNullOrEmpty(_clipPrefix) &&
                !fileName.StartsWith(_clipPrefix.ToLowerInvariant()))
                continue;

            if (!fileName.Contains(keyword)) continue;

            if (excludeWalkBack && fileName.Contains("walk_back")) continue;

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) return clip;
        }

        return null;
    }

    // ── Shared state machine helpers ──────────────────────────────────────────

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folder = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void EnsureParameter(AnimatorController ac, string paramName,
        AnimatorControllerParameterType type)
    {
        foreach (var p in ac.parameters)
            if (p.name == paramName) return;

        ac.AddParameter(paramName, type);
    }

    private static AnimatorState EnsureState(AnimatorStateMachine sm, string stateName,
        AnimationClip clip, Vector3 position)
    {
        foreach (var cs in sm.states)
            if (cs.state.name == stateName)
            {
                if (clip != null) cs.state.motion = clip;
                return cs.state;
            }

        AnimatorState state = sm.AddState(stateName, position);
        if (clip != null) state.motion = clip;
        return state;
    }

    /// <summary>Adds a bool-conditioned transition (skips if one already exists).</summary>
    private static void AddBoolTransition(AnimatorStateMachine sm,
        AnimatorState from, AnimatorState to, string param, bool value)
    {
        foreach (var t in from.transitions)
            if (t.destinationState == to) return;

        var tr = from.AddTransition(to);
        tr.hasExitTime = false;
        tr.duration    = 0.25f;
        tr.AddCondition(
            value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
            0f, param);
    }

    /// <summary>Adds an AnyState → attack transition driven by a trigger parameter.</summary>
    private static void AddAnyAttackTransition(AnimatorStateMachine sm,
        AnimatorState attackState, string triggerParam)
    {
        var t = sm.AddAnyStateTransition(attackState);
        t.hasExitTime         = false;
        t.duration            = 0f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0f, triggerParam);
    }

    /// <summary>Adds an exit-time transition (play full clip → dest).</summary>
    private static void AddExitTimeTransition(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime    = 1f;
        t.duration    = 0f;
    }

    // ── Status / reporting ────────────────────────────────────────────────────

    private void SetStatus(string message, bool success)
    {
        _lastStatus  = message;
        _lastSuccess = success;
        Repaint();
    }

    /// <summary>
    /// Returns a warning string listing any clips that weren't found.
    /// Returns empty string if everything expected was found.
    /// </summary>
    private static string BuildMissingReport(
        AnimationClip idle, AnimationClip walkFwd, AnimationClip walkBack,
        AnimationClip run,  AnimationClip attack,  bool has5State)
    {
        var missing = new System.Collections.Generic.List<string>();

        if (idle    == null) missing.Add("idle");
        if (walkFwd == null) missing.Add(has5State ? "walk_forward" : "walk");
        if (has5State && walkBack == null) missing.Add("walk_back");
        if (run     == null) missing.Add("run");
        if (attack  == null) missing.Add("attack");

        if (missing.Count == 0) return "";

        return "\n\n⚠ Clips not found (states left unbound):\n  • " +
               string.Join("\n  • ", missing) +
               "\n\nAssign them manually in the Animator window.";
    }
}
