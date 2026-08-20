using UnityEngine;

/// <summary>
/// Makes a sprite glow via URP Bloom by pushing its material color past normal (HDR) intensity.
/// Generic component, not tied to any one archetype — Zone/Positional's attacker windup
/// (Attack_Pattern_Directive Part 5 Group 3) is its first caller: BeginWarningGlow() when the
/// attacker's preemptive warning starts, EndWarningGlow() once the attack resolves. See the
/// Inspector setup note at the bottom of this file for the matching Global Volume Bloom
/// configuration this depends on.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteGlowController : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Glow Color & Intensity")]
    [Tooltip("HDR color multiplied into the sprite's material. Push the color picker's Intensity " +
             "slider well above 1.0 (try 2-4) so the result exceeds Bloom's Threshold and actually " +
             "blooms — a channel value of exactly 1.0 looks like plain white but produces no glow.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color _glowColor = Color.white;

    [Tooltip("Base multiplier applied on top of _glowColor's own HDR intensity.")]
    [SerializeField] private float _glowIntensity = 1f;

    [Header("Pulsing")]
    [SerializeField] private bool _pulse;

    [Tooltip("Pulse cycles per second when _pulse is enabled.")]
    [SerializeField] private float _pulseFrequency = 1f;

    [Tooltip("Intensity floor the pulse dips to, as a fraction of _glowIntensity — keeps the glow " +
             "from fully vanishing mid-pulse.")]
    [SerializeField] private float _pulseMinFraction = 0.4f;

    private SpriteRenderer _spriteRenderer;
    private Material _runtimeMaterial;
    private Color _baseColor;
    private bool _isGlowing;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // Instantiate a COPY of the sprite's current material so this never edits the shared
        // project asset — every other renderer using the same material is unaffected.
        _runtimeMaterial = new Material(_spriteRenderer.sharedMaterial);
        _spriteRenderer.material = _runtimeMaterial;
        _baseColor = _runtimeMaterial.HasProperty(BaseColorId)
            ? _runtimeMaterial.GetColor(BaseColorId)
            : _runtimeMaterial.color;
    }

    private void Update()
    {
        if (!_isGlowing) return;

        float intensity = _glowIntensity;
        if (_pulse)
        {
            // PingPong sweeps 0..1..0 continuously; remapped into [_pulseMinFraction, 1] so the
            // glow dims rather than fully disappearing at the bottom of each cycle.
            float t = Mathf.PingPong(Time.time * _pulseFrequency, 1f);
            intensity = _glowIntensity * Mathf.Lerp(_pulseMinFraction, 1f, t);
        }

        ApplyGlow(intensity);
    }

    /// <summary>Starts glowing — call when this creature should show a preemptive warning.</summary>
    public void BeginWarningGlow()
    {
        _isGlowing = true;
        ApplyGlow(_pulse ? _glowIntensity * _pulseMinFraction : _glowIntensity);
    }

    /// <summary>Stops glowing and restores the sprite's original color.</summary>
    public void EndWarningGlow()
    {
        _isGlowing = false;
        SetMaterialColor(_baseColor);
    }

    private void ApplyGlow(float intensity)
    {
        // Multiplying an already-HDR _glowColor by a further scalar is what pushes the final
        // per-channel value past Bloom's Threshold — this is the entire mechanism.
        SetMaterialColor(_baseColor * _glowColor * intensity);
    }

    private void SetMaterialColor(Color color)
    {
        // URP Lit/Unlit shaders expose _BaseColor; some URP 2D Renderer sprite shaders (and the
        // legacy Sprites-Default shader) use _Color instead — set whichever the material has.
        if (_runtimeMaterial.HasProperty(BaseColorId)) _runtimeMaterial.SetColor(BaseColorId, color);
        if (_runtimeMaterial.HasProperty(ColorId)) _runtimeMaterial.SetColor(ColorId, color);
    }

    private void OnDestroy()
    {
        // Instance-created material — Unity won't garbage-collect it on its own.
        if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
    }

    // --- Global Volume / Bloom setup (one-time Scene setup, not per-script) ---
    // 1. Select or create a GameObject with a Volume component, Mode = Global, Is Global = true.
    // 2. In its Volume Profile: Add Override -> Post-processing -> Bloom.
    // 3. Enable Bloom's checkbox, set Threshold = 1.0 — pixels at or below 1.0 (ordinary, non-HDR
    //    color) are untouched; this script's _glowColor * _glowIntensity multiplication is what
    //    pushes an affected sprite's rendered channels above that threshold.
    // 4. Set Bloom's own Intensity to taste (try 1-2 to start).
    // 5. Confirm the active URP Renderer asset / Camera has Post-processing (HDR) enabled, or
    //    Bloom will compute but never actually display.
}
