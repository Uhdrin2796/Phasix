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
}
