using UnityEngine;

/// <summary>
/// Placeholder-first visual representation for a Phasix (DECISIONS.md → [Art]
/// Placeholder-first pipeline): one shared shape (Circle) for every Phasix, colored
/// systematically from PrimalType via PrimalTypeColor. A second, larger "underglow" disc
/// renders behind the body at a lightened/translucent version of the same color, adding
/// depth beyond a flat tinted disc without introducing per-species shape variation.
///
/// Expects a 2-child prefab structure: Body (front, normal scale) and Underglow (behind,
/// larger scale, lower sorting order) — see Assets/Prefabs/Creatures/Phasix_Placeholder.prefab.
/// </summary>
public class PhasixPlaceholderVisual : MonoBehaviour
{
    [Header("Renderers")]
    [Tooltip("Front sprite renderer — tinted to the full PrimalType color.")]
    [SerializeField] private SpriteRenderer _bodyRenderer;

    [Tooltip("Background halo sprite renderer — tinted to a lightened, translucent version of the same PrimalType color. Must be larger scale and a lower sorting order than Body so it renders behind.")]
    [SerializeField] private SpriteRenderer _underglowRenderer;

    [Header("Underglow Tuning")]
    [Tooltip("How far the underglow color is lerped toward white, 0 (same as body) to 1 (pure white). Keeps the glow reading as 'same hue, softer' rather than a duplicate flat disc.")]
    [Range(0f, 1f)]
    [SerializeField] private float _underglowLightenAmount = 0.35f;

    [Tooltip("Underglow opacity, 0 (invisible) to 1 (opaque). Kept low so it reads as a soft halo behind the body, not a second solid shape.")]
    [Range(0f, 1f)]
    [SerializeField] private float _underglowAlpha = 0.4f;

    private float _bodyBaseScaleY = float.NaN;
    private float _underglowBaseScaleY = float.NaN;
    private Vector3 _bodyBaseScale = Vector3.zero;
    private Vector3 _underglowBaseScale = Vector3.zero;

    /// <summary>Tints Body and Underglow from a PrimalType directly.</summary>
    public void SetPrimalType(PrimalType type)
    {
        _bodyRenderer.color = PrimalTypeColor.GetColor(type);
        _underglowRenderer.color = PrimalTypeColor.GetUnderglowColor(type, _underglowLightenAmount, _underglowAlpha);
    }

    /// <summary>Convenience wrapper for spawner/CompanionAI code that already holds a PhasixData reference.</summary>
    public void ApplyFromSpeciesData(PhasixData data)
    {
        SetPrimalType(data.PrimalType);
    }

    /// <summary>
    /// Scales Body and Underglow's localScale.y by scaleYMultiplier, relative to each renderer's
    /// own original scale (not a hardcoded value — Underglow is intentionally a larger base
    /// scale than Body, per this class's own prefab structure). Caches each renderer's original
    /// scale.y on first call so 1f always restores exactly, no matter how many times this is
    /// called or in which order. Used by CompanionAI's HiddenShadow movement pattern to flatten
    /// the companion into a ground-shadow read while locked onto the player, and restore it once
    /// idle.
    /// </summary>
    public void SetShadowSquash(float scaleYMultiplier)
    {
        if (float.IsNaN(_bodyBaseScaleY))
        {
            _bodyBaseScaleY = _bodyRenderer.transform.localScale.y;
            _underglowBaseScaleY = _underglowRenderer.transform.localScale.y;
        }

        Vector3 bodyScale = _bodyRenderer.transform.localScale;
        bodyScale.y = _bodyBaseScaleY * scaleYMultiplier;
        _bodyRenderer.transform.localScale = bodyScale;

        Vector3 underglowScale = _underglowRenderer.transform.localScale;
        underglowScale.y = _underglowBaseScaleY * scaleYMultiplier;
        _underglowRenderer.transform.localScale = underglowScale;
    }

    /// <summary>
    /// Scales Body and Underglow's full localScale (x and y uniformly) by scaleMultiplier,
    /// relative to each renderer's own original scale — cached lazily on first call, same
    /// convention as SetShadowSquash above, but tracked in its own fields since a "pop" needs
    /// uniform scale rather than SetShadowSquash's deliberately y-only flatten. Used by
    /// CompanionAI's Blink movement pattern for a brief post-teleport flash.
    /// </summary>
    public void SetBlinkFlashScale(float scaleMultiplier)
    {
        if (_bodyBaseScale == Vector3.zero)
        {
            _bodyBaseScale = _bodyRenderer.transform.localScale;
            _underglowBaseScale = _underglowRenderer.transform.localScale;
        }

        _bodyRenderer.transform.localScale = _bodyBaseScale * scaleMultiplier;
        _underglowRenderer.transform.localScale = _underglowBaseScale * scaleMultiplier;
    }

    /// <summary>
    /// Toggles Body and Underglow's SpriteRenderer.enabled — the GameObject itself stays
    /// active, so physics/colliders/scripts keep running, only the rendered sprites blink
    /// off/on. Used by CompanionAI's Blink movement pattern to fully hide the companion for the
    /// duration of a teleport, so the position change never renders as a visible slide.
    /// </summary>
    public void SetVisible(bool visible)
    {
        _bodyRenderer.enabled = visible;
        _underglowRenderer.enabled = visible;
    }
}
