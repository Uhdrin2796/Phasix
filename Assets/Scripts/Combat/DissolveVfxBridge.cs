using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Bridges the world-space hand-coded dissolve shader (Assets/Shaders/DissolveEffect.shader) into
/// the UI-Toolkit-rendered battle stage (2026-08-11, user-directed: "the dissolve applies to the
/// phasix itself... using the shader graphics if the dodge timing is timed properly").
/// `BattleHUDPanelSettings.asset` is Screen Space Overlay, which Unity always composites on top of
/// everything any camera renders — so a normal world-space MeshRenderer would be invisible behind
/// the whole HUD, no matter its position. This bridges around that constraint: a dedicated camera
/// (`_DissolveVfxCamera` in SampleScene, culled to the CombatDissolveVfx layer only) continuously
/// renders a single world-space Quad (`_DissolveVfxQuad`, carrying `DissolveEffect.mat`) into a
/// RenderTexture (`Assets/Textures/DissolveCaptureRT.renderTexture`). During the effect, that
/// RenderTexture becomes the defender's own stage-creature VisualElement's background-image — from
/// the panel's perspective it's just another UI image, composited correctly in place. Reverts to
/// the element's normal Primal-type flat background-color once the reappear finishes.
///
/// Singleton in SampleScene (same DontDestroyOnLoad pattern as AudioManager/GameManager) so it
/// survives BattleScene_Main's additive load/unload. Only one dissolve can play at a time — this
/// project's battles are strictly sequential (one live timed input at a time, same reasoning as
/// CombatVfxController's single held-projectile slot), so a shared Quad/MaterialPropertyBlock
/// never needs to support concurrent effects.
///
/// Inspector Setup:
///   1. Attach to the "_DissolveVfxBridge" GameObject in SampleScene
///   2. Assign Dissolve Quad Renderer -> the "_DissolveVfxQuad" GameObject's MeshRenderer
///   3. Assign Capture Texture -> Assets/Textures/DissolveCaptureRT.renderTexture (must match
///      "_DissolveVfxCamera"'s own Target Texture)
/// </summary>
public class DissolveVfxBridge : MonoBehaviour
{
    public static DissolveVfxBridge Instance { get; private set; }

    [Header("Render Bridge")]
    [Tooltip("The _DissolveVfxQuad GameObject's MeshRenderer, carrying Assets/Materials/DissolveEffect.mat.")]
    [SerializeField] private MeshRenderer _dissolveQuadRenderer;

    [Tooltip("Assets/Textures/DissolveCaptureRT.renderTexture — must match _DissolveVfxCamera's own Target Texture.")]
    [SerializeField] private RenderTexture _captureTexture;

    [Header("Timing")]
    [Tooltip("Seconds to fully dissolve out. Placeholder value, not playtested (2026-08-11: sped up from 0.3s per playtest feedback).")]
    [SerializeField] private float _dissolveOutDuration = 0.2f;

    [Tooltip("Seconds spent fully invisible before reappearing. Placeholder value, not playtested (2026-08-11: sped up from 0.15s per playtest feedback).")]
    [SerializeField] private float _dissolveHoldDuration = 0.1f;

    [Tooltip("Seconds to fully reappear. Placeholder value, not playtested (2026-08-11: sped up from 0.3s per playtest feedback).")]
    [SerializeField] private float _dissolveInDuration = 0.2f;

    /// <summary>
    /// Exposed so BattleHUDController can time the incoming projectile's pass-through leg to
    /// exactly match this — the dissolve-out and the projectile crossing the defender's position
    /// need to take the same amount of time to actually read as "phasing out of the way," not two
    /// independently-timed effects that happen to overlap (2026-08-11, user-directed: "the
    /// dissolve effect happens as the projectile passes through").
    /// </summary>
    public float DissolveOutDuration => _dissolveOutDuration;

    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _propertyBlock = new MaterialPropertyBlock();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Plays the dissolve-out / hold / reappear cycle on defenderElement, tinted by whatever
    /// Primal-type color the element already has (SetStageCreatureColor's own inline background —
    /// read directly rather than requiring a separate PrimalType parameter, since that value IS
    /// already exactly the creature's own color; no extra data needs to be plumbed in from
    /// BattleManager). No-op if the bridge isn't wired up (missing Inspector references) or
    /// defenderElement is null.
    /// </summary>
    public void PlayDefenderDissolve(VisualElement defenderElement)
    {
        if (_dissolveQuadRenderer == null || _captureTexture == null || defenderElement == null) return;
        StartCoroutine(DissolveRoutine(defenderElement));
    }

    private IEnumerator DissolveRoutine(VisualElement defenderElement)
    {
        Color restingColor = defenderElement.resolvedStyle.backgroundColor;

        _dissolveQuadRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BaseColorId, restingColor);
        _propertyBlock.SetFloat(DissolveAmountId, 0f);
        _dissolveQuadRenderer.SetPropertyBlock(_propertyBlock);

        // Swap the defender's element to show the captured RenderTexture instead of its normal
        // flat color for the duration of the effect.
        defenderElement.style.backgroundColor = Color.clear;
        defenderElement.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_captureTexture));

        yield return AnimateDissolveAmount(0f, 1f, _dissolveOutDuration);
        yield return new WaitForSeconds(_dissolveHoldDuration);
        yield return AnimateDissolveAmount(1f, 0f, _dissolveInDuration);

        // Revert — back to the element's own flat Primal-type color, RenderTexture cleared.
        defenderElement.style.backgroundImage = StyleKeyword.Null;
        defenderElement.style.backgroundColor = restingColor;
    }

    private IEnumerator AnimateDissolveAmount(float from, float to, float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            _dissolveQuadRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(DissolveAmountId, Mathf.Lerp(from, to, elapsed / safeDuration));
            _dissolveQuadRenderer.SetPropertyBlock(_propertyBlock);
            yield return null;
        }
        _dissolveQuadRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(DissolveAmountId, to);
        _dissolveQuadRenderer.SetPropertyBlock(_propertyBlock);
    }
}
