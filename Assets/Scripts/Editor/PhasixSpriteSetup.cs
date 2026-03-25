// ============================================================
// PhasixSpriteSetup.cs
// Editor utility: sprite sheet import + Animator Controller setup for Phasix creatures.
// Menu: Phasix/Sprite Setup/
//
// Usage order:
//   Step 1 — Configure Import Settings   (sets Point filter, grid-slices running sheets)
//   Step 2 — Open Sprite Editor          (manual: Automatic-slice the composite sheets)
//   Step 3 — Create Animation Clips      (creates placeholder clips to fill via Animation window)
//   Step 4 — Create Animator Controller  (wires all states, parameters, transitions)
//   Step 5 — Create Test GameObject      (drops DarkFluffy_Test into the scene)
// ============================================================

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;   // ISpriteEditorDataProvider, SpriteDataProviderFactories, SpriteRect (Unity 6)
using UnityEngine;

public static class PhasixSpriteSetup
{
    // ── Paths ────────────────────────────────────────────────────────────────
    private const string ArtRoot   = "Assets/Artwork/Dark Fluffy";
    private const string AnimRoot  = "Assets/Animations/Creatures/DarkFluffy";

    // Composite sheets — two visual sections, use Sprite Editor Automatic slice
    private static readonly string[] CompositeSheets =
    {
        ArtRoot + "/Fluffy Sprite Sheet v1.png",
        ArtRoot + "/Fluffy Sprite Sheet v2.png",
    };

    // Running sheets — uniform 4-column × 2-row grid, sliced automatically here
    private static readonly string[] RunningSheets =
    {
        ArtRoot + "/Dark Fluffy Running Sprite Sheet.png",
        ArtRoot + "/Dark Fluffy Running Sprite Sheet (Dark).png",
    };

    // ── Import constants ─────────────────────────────────────────────────────
    private const int  PPU             = 32;   // pixels per unit; adjust if sprites look too big/small
    private const int  RunningColumns  = 4;
    private const int  RunningRows     = 2;

    // ── Step 1: Import settings + grid-slice running sheets ──────────────────

    [MenuItem("Phasix/Sprite Setup/Step 1 — Configure Import Settings")]
    public static void Step1_ConfigureImportSettings()
    {
        bool anyMissing = false;

        // ── Composite sheets: set to Multiple + pixel-art settings only
        foreach (string path in CompositeSheets)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[PhasixSpriteSetup] Not found — skipping: {path}");
                anyMissing = true;
                continue;
            }

            ApplyPixelArtSettings(importer);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();
            Debug.Log($"[PhasixSpriteSetup] Import settings applied: {Path.GetFileName(path)}");
        }

        // ── Running sheets: Multiple + grid-slice
        foreach (string path in RunningSheets)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[PhasixSpriteSetup] Not found — skipping: {path}");
                anyMissing = true;
                continue;
            }

            ApplyPixelArtSettings(importer);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            // Must reimport first so Unity loads the texture and we can read dimensions
            importer.SaveAndReimport();

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogError($"[PhasixSpriteSetup] Could not load texture after import: {path}");
                continue;
            }

            SliceRunningSheet(importer, tex, path);
            Debug.Log($"[PhasixSpriteSetup] Grid-sliced {RunningColumns}×{RunningRows}: {Path.GetFileName(path)}");
        }

        AssetDatabase.Refresh();

        string nextStep = anyMissing
            ? "⚠ Some files were not found — check the paths in PhasixSpriteSetup.cs.\n\n"
            : "";

        EditorUtility.DisplayDialog(
            "Step 1 Complete",
            $"{nextStep}Import settings applied to all Dark Fluffy sheets.\n\n" +
            "Running sheets have been grid-sliced automatically (4 × 2).\n\n" +
            "─── NEXT: Step 2 (manual) ───\n" +
            "For each COMPOSITE sheet (Fluffy Sprite Sheet v1 / v2):\n" +
            "  1. Select the PNG in the Project window\n" +
            "  2. Inspector → click  Sprite Editor\n" +
            "  3. Top-left dropdown → Slice\n" +
            "  4. Type: Automatic   |   Pivot: Bottom\n" +
            "  5. Click Slice → Apply\n\n" +
            "Then run Step 3.",
            "OK");
    }

    // ── Step 3: Create placeholder Animation Clips ───────────────────────────

    [MenuItem("Phasix/Sprite Setup/Step 3 — Create Animation Clips")]
    public static void Step3_CreateAnimationClips()
    {
        EnsureFolder(AnimRoot);

        // Clip definitions: (name, fps, loop)
        var clips = new (string name, float fps, bool loop)[]
        {
            ("fluffy_idle",   8f,  true),
            ("fluffy_walk",   12f, true),
            ("fluffy_run",    16f, true),
            ("fluffy_attack", 10f, false),
        };

        foreach (var (name, fps, loop) in clips)
        {
            string clipPath = $"{AnimRoot}/{name}.anim";

            // Don't overwrite if already exists
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
            {
                Debug.Log($"[PhasixSpriteSetup] Clip already exists, skipped: {name}.anim");
                continue;
            }

            var clip = new AnimationClip
            {
                frameRate = fps,
                name      = name,
            };

            // Set loop time
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, clipPath);
            Debug.Log($"[PhasixSpriteSetup] Created clip: {clipPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Step 3 Complete",
            "4 placeholder Animation Clips created in:\n" +
            AnimRoot + "\n\n" +
            "─── NEXT: Fill each clip with frames ───\n" +
            "  1. Select DarkFluffy_Test in the Hierarchy\n" +
            "  2. Window → Animation → Animation  (Ctrl+6)\n" +
            "  3. Pick a clip from the dropdown (e.g. fluffy_idle)\n" +
            "  4. In Project, expand the sprite sheet asset\n" +
            "  5. Drag frames onto the Animation timeline in order\n\n" +
            "Repeat for walk, run, attack — then run Step 4.",
            "OK");
    }

    // ── Step 4: Create Animator Controller ───────────────────────────────────

    [MenuItem("Phasix/Sprite Setup/Step 4 — Create Animator Controller")]
    public static void Step4_CreateAnimatorController()
    {
        EnsureFolder(AnimRoot);
        string acPath = $"{AnimRoot}/DarkFluffy_AC.controller";

        // Load existing or create new
        var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(acPath);
        if (ac == null)
        {
            ac = AnimatorController.CreateAnimatorControllerAtPath(acPath);
            Debug.Log($"[PhasixSpriteSetup] Created Animator Controller: {acPath}");
        }
        else
        {
            Debug.Log("[PhasixSpriteSetup] Animator Controller already exists — updating.");
        }

        AnimatorControllerLayer layer = ac.layers[0];
        AnimatorStateMachine sm = layer.stateMachine;

        // ── Parameters ──────────────────────────────────────────────────────
        EnsureParameter(ac, "IsMoving",   AnimatorControllerParameterType.Bool);
        EnsureParameter(ac, "IsRunning",  AnimatorControllerParameterType.Bool);
        EnsureParameter(ac, "Attack",     AnimatorControllerParameterType.Trigger);

        // ── States ───────────────────────────────────────────────────────────
        AnimatorState stateIdle   = EnsureState(sm, "Idle",   LoadClip("fluffy_idle"),   new Vector3(-200,  0));
        AnimatorState stateWalk   = EnsureState(sm, "Walk",   LoadClip("fluffy_walk"),   new Vector3(   0,  80));
        AnimatorState stateRun    = EnsureState(sm, "Run",    LoadClip("fluffy_run"),    new Vector3( 200,  0));
        AnimatorState stateAttack = EnsureState(sm, "Attack", LoadClip("fluffy_attack"), new Vector3(   0, -80));

        sm.defaultState = stateIdle;

        // ── Transitions ───────────────────────────────────────────────────────

        // Idle ↔ Walk (driven by IsMoving)
        AddTransition(sm, stateIdle,  stateWalk,  0f, false, ("IsMoving", true));
        AddTransition(sm, stateWalk,  stateIdle,  0f, false, ("IsMoving", false));

        // Walk ↔ Run (driven by IsRunning)
        AddTransition(sm, stateWalk,  stateRun,   0f, false, ("IsRunning", true));
        AddTransition(sm, stateRun,   stateWalk,  0f, false, ("IsRunning", false));

        // Any State → Attack (trigger)
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(stateAttack);
            t.hasExitTime            = false;
            t.duration               = 0f;
            t.canTransitionToSelf    = false;
            t.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        }

        // Attack → Idle (play full clip, then return)
        {
            AnimatorStateTransition t = stateAttack.AddTransition(stateIdle);
            t.hasExitTime  = true;
            t.exitTime     = 1f;
            t.duration     = 0f;
        }

        // Persist changes
        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Step 4 Complete",
            "DarkFluffy_AC.controller created with:\n" +
            "  • States:      Idle, Walk, Run, Attack\n" +
            "  • Parameters:  IsMoving (bool), IsRunning (bool), Attack (trigger)\n" +
            "  • Transitions: all wired\n\n" +
            "Run Step 5 to drop a test GameObject into the scene.",
            "OK");
    }

    // ── Step 5: Create Test GameObject ───────────────────────────────────────

    [MenuItem("Phasix/Sprite Setup/Step 5 — Create Test GameObject")]
    public static void Step5_CreateTestGameObject()
    {
        // Remove stale test object if it exists
        GameObject existing = GameObject.Find("DarkFluffy_Test");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        var go = new GameObject("DarkFluffy_Test");
        go.transform.position = Vector3.zero;

        // Sprite Renderer
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = GetOrCreateSortingLayer("Characters");

        // Load a default idle sprite to make it visible immediately
        var idleSprite = FindFirstSlicedSprite(CompositeSheets[1]); // prefer v2
        if (idleSprite != null)
            sr.sprite = idleSprite;
        else
            Debug.LogWarning("[PhasixSpriteSetup] No sliced sprites found yet — slice the composite sheet first (Step 2), then re-run Step 5.");

        // Animator
        var animator  = go.AddComponent<Animator>();
        var ac        = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{AnimRoot}/DarkFluffy_AC.controller");
        if (ac != null)
            animator.runtimeAnimatorController = ac;
        else
            Debug.LogWarning("[PhasixSpriteSetup] Animator Controller not found — run Step 4 first.");

        // Mark scene as dirty so the GameObject is saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = go;

        EditorUtility.DisplayDialog(
            "Step 5 Complete",
            "DarkFluffy_Test created in the scene.\n\n" +
            "─── Smoke test ───\n" +
            "  1. Press Play\n" +
            "  2. Select DarkFluffy_Test in Hierarchy\n" +
            "  3. Inspector → Animator → toggle IsMoving / IsRunning\n" +
            "  4. Trigger Attack via the Animator debug panel\n" +
            "  5. Confirm no blur and pivot sits at the feet",
            "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ApplyPixelArtSettings(TextureImporter importer)
    {
        importer.textureType        = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode         = FilterMode.Point;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled       = false;
        importer.maxTextureSize      = 2048;
        importer.spritePivot         = new Vector2(0.5f, 0f); // bottom-centre
        importer.alphaIsTransparency = true;
    }

    // Uses ISpriteEditorDataProvider (Unity 6 API) — replaces deprecated SpriteMetaData / TextureImporter.spritesheet
    // Docs: https://docs.unity3d.com/Packages/com.unity.2d.sprite@1.0/api/UnityEditor.U2D.Sprites.ISpriteEditorDataProvider.html
    private static void SliceRunningSheet(TextureImporter importer, Texture2D tex, string path)
    {
        int cellW = tex.width  / RunningColumns;
        int cellH = tex.height / RunningRows;

        string baseName = Path.GetFileNameWithoutExtension(path)
            .ToLower().Replace(" ", "_");

        // Step A: get the data provider for this importer
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        // Step B: build SpriteRect list (bottom-left origin — row 0 = bottom of image)
        var rects = new List<SpriteRect>();
        int index = 0;

        for (int row = RunningRows - 1; row >= 0; row--)
        {
            for (int col = 0; col < RunningColumns; col++)
            {
                rects.Add(new SpriteRect
                {
                    name      = $"{baseName}_{index}",
                    spriteID  = GUID.Generate(),
                    rect      = new Rect(col * cellW, row * cellH, cellW, cellH),
                    pivot     = new Vector2(0.5f, 0f),
                    alignment = SpriteAlignment.BottomCenter,
                });
                index++;
            }
        }

        // Step C: push rects, apply, then reimport to persist
        dataProvider.SetSpriteRects(rects.ToArray());
        dataProvider.Apply();
        importer.SaveAndReimport();
    }

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

    // Add a transition only if one doesn't already exist between the two states
    private static void AddTransition(AnimatorStateMachine sm,
        AnimatorState from, AnimatorState to, float exitTime, bool hasExitTime,
        (string name, bool value) condition)
    {
        foreach (var t in from.transitions)
            if (t.destinationState == to) return;

        AnimatorStateTransition tr = from.AddTransition(to);
        tr.hasExitTime = hasExitTime;
        tr.exitTime    = exitTime;
        tr.duration    = 0f;
        tr.AddCondition(condition.value
            ? AnimatorConditionMode.If
            : AnimatorConditionMode.IfNot,
            0, condition.name);
    }

    private static AnimationClip LoadClip(string clipName)
    {
        return AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimRoot}/{clipName}.anim");
    }

    // Returns the first sliced Sprite found inside a Multiple-mode sprite sheet
    private static Sprite FindFirstSlicedSprite(string sheetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        foreach (Object a in assets)
            if (a is Sprite s) return s;
        return null;
    }

    // Ensures the sorting layer exists; returns the name unchanged for convenience
    private static string GetOrCreateSortingLayer(string layerName)
    {
        // Sorting layers are serialized in TagManager — read-only check here,
        // Unity doesn't expose a public API to add layers at runtime via script.
        // If "Characters" doesn't exist, create it manually:
        // Edit → Project Settings → Tags and Layers → Sorting Layers → +
        return layerName;
    }
}
