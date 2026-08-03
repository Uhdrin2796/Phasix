using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// TEMPORARY manual-test tool — press Tab in Play mode to cycle the active companion
/// through the Tier 1 movement presets drafted in DECISIONS.md → [Creatures] "Companion
/// movement/following pattern archetypes". Lets the presets be compared live, on the
/// existing placeholder companion, before any of them are tied to a Personality/species
/// hook.
///
/// DELETE THIS FILE once a real per-species/per-Personality movement hook exists and
/// applies presets itself — this is scaffolding for side-by-side comparison only.
/// </summary>
public class DebugMovementPresetCycler : MonoBehaviour
{
    private static readonly CompanionMovementPreset[] Presets =
    {
        new CompanionMovementPreset
        {
            Name = "Default",
            Pattern = CompanionMovementPatternType.Direct,
            TrailDistance = 1.2f, DirectionTurnSpeed = 180f,
            WalkSpeed = 3f, RunSpeed = 6f,
            IdleDistance = 1f, RunDistance = 5f,
            RepelDistance = 0.7f, RepelStrength = 0.8f,
        },
        new CompanionMovementPreset
        {
            Name = "Close Shadow (glued to you, reacts instantly)",
            Pattern = CompanionMovementPatternType.Direct,
            TrailDistance = 0.4f, DirectionTurnSpeed = 340f,
            WalkSpeed = 4f, RunSpeed = 7f,
            IdleDistance = 0.3f, RunDistance = 3f,
            RepelDistance = 0.5f, RepelStrength = 0.6f,
        },
        new CompanionMovementPreset
        {
            Name = "Wide Wanderer (weaves side to side as it follows)",
            Pattern = CompanionMovementPatternType.Wavy,
            TrailDistance = 2f, DirectionTurnSpeed = 100f,
            WalkSpeed = 2f, RunSpeed = 5f,
            IdleDistance = 2f, RunDistance = 8f,
            RepelDistance = 1f, RepelStrength = 1.2f,
            WaveAmplitude = 1.2f, WaveFrequency = 2.5f,
        },
        new CompanionMovementPreset
        {
            Name = "Eager Runner (frantic — dashes past you, changing angle and length constantly)",
            Pattern = CompanionMovementPatternType.DashThrough,
            TrailDistance = 1f, DirectionTurnSpeed = 270f,
            WalkSpeed = 6f, RunSpeed = 14f,
            IdleDistance = 0.6f, RunDistance = 3f,
            RepelDistance = 0.6f, RepelStrength = 0.8f,
            DashIntervalMin = 2f, DashInterval = 3f, // safety-net max wait only — normal re-targeting now happens on arrival, not this clock
            DashOvershootMin = 1.5f, DashOvershootDistance = 5f,
        },
        new CompanionMovementPreset
        {
            Name = "Steady Anchor (moves, stops, moves, stops)",
            Pattern = CompanionMovementPatternType.StopAndGo,
            TrailDistance = 1.8f, DirectionTurnSpeed = 100f,
            WalkSpeed = 2f, RunSpeed = 4f,
            IdleDistance = 2f, RunDistance = 8f,
            RepelDistance = 0.8f, RepelStrength = 0.9f,
            MoveDuration = 1.2f, PauseDuration = 1f,
        },
        new CompanionMovementPreset
        {
            Name = "Orbiting Moon (circles you tightly, almost no lag)",
            Pattern = CompanionMovementPatternType.Orbit,
            TrailDistance = 1.2f, DirectionTurnSpeed = 180f,
            WalkSpeed = 3f, RunSpeed = 6f,
            IdleDistance = 1f, RunDistance = 5f,
            RepelDistance = 0.7f, RepelStrength = 0.8f,
            OrbitRadius = 2f, OrbitAngularSpeed = 220f,
            OrbitCenterOffset = new Vector2(0f, 1.4f), // shifts the orbit center up to the player's visible body, not their feet-pivot Transform position
            OrbitCatchUpSpeed = 40f, // very high — near-instant tracking, almost no lag between the orbit and the player's current position
        },
    };

    private CompanionAI _companionAI;
    private TextMesh _label;
    private int _currentIndex = 0;

    private void Update()
    {
        if (_companionAI == null)
        {
            var companionGO = GameObject.Find("ActiveCompanion");
            if (companionGO == null) return;
            _companionAI = companionGO.GetComponent<CompanionAI>();
            CreateLabel(companionGO.transform);
            ApplyCurrent();
        }

        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            _currentIndex = (_currentIndex + 1) % Presets.Length;
            ApplyCurrent();
        }
    }

    /// <summary>Floating world-space text above the companion's head — parented to it so it moves along automatically.</summary>
    private void CreateLabel(Transform companionTransform)
    {
        var labelGO = new GameObject("PresetLabel");
        labelGO.transform.SetParent(companionTransform, false);
        labelGO.transform.localPosition = new Vector3(0f, 1f, 0f);
        // TextMesh font sizes are in pixels; scale down to fit world units. Doubled from the
        // original 0.075 to compensate for the companion prefab's root now being scaled to
        // 0.5x — keeps the label's actual on-screen size the same as before that change.
        labelGO.transform.localScale = Vector3.one * 0.15f;

        _label = labelGO.AddComponent<TextMesh>();
        _label.anchor = TextAnchor.LowerCenter;
        _label.alignment = TextAlignment.Center;
        _label.fontSize = 32;
        _label.color = Color.white;

        var renderer = labelGO.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "Characters";
        renderer.sortingOrder = 10; // above the companion's Body/Underglow sprites
    }

    private void ApplyCurrent()
    {
        CompanionMovementPreset preset = Presets[_currentIndex];
        _companionAI.ApplyMovementPreset(preset);
        if (_label != null) _label.text = ShortName(preset.Name);
        Debug.Log($"[DebugMovementPresetCycler] Now using preset {_currentIndex + 1}/{Presets.Length}: {preset.Name}");
    }

    /// <summary>Just the name before any parenthetical description — full names overflow a floating world-space label.</summary>
    private static string ShortName(string fullName)
    {
        int parenIndex = fullName.IndexOf('(');
        return parenIndex > 0 ? fullName.Substring(0, parenIndex).TrimEnd() : fullName;
    }

    private void OnGUI()
    {
        if (_companionAI == null) return;
        string label = $"Movement preset ({_currentIndex + 1}/{Presets.Length}): {Presets[_currentIndex].Name}\nPress Tab to cycle";
        GUI.Label(new Rect(10, 10, 500, 40), label);
    }
}
