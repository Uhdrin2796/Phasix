using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

/// <summary>
/// Owns placeholder combat VFX — a pooled traveling projectile + a tint-flash reusing the
/// already-permanent stage-creature elements (2026-08-10 — Phase 3 close-out pass; combat had no
/// visual feedback for attacks/skills landing before this). Constructed and driven by
/// BattleHUDController, not its own scene singleton — it needs direct access to Stage/
/// _playerStageCreatures/_enemyStageCreature, and BattleManager never touches UI Toolkit
/// internals itself, only calls BattleHUDController's public API.
///
/// 2026-08-11 timing-sync pass (user-directed): the projectile is no longer a "resolve, then
/// fire a fixed-duration VFX afterward" effect. Instead, BattleManager launches the projectile
/// CONCURRENTLY with the timing ring (BattleHUDController.RunTimedInput/RunDefenseTimedInput),
/// using ComputeTravelDuration + BattleHUDController.ComputeSweepDurationForTravelTime to size the
/// ring so its "perfect" instant lands exactly when the projectile visually connects.
/// BattleConfig.ProjectileSpeed (px/sec) is the tunable; edge-to-edge distance (center distance
/// minus both the projectile's and target's radii) divided by that speed gives the travel time —
/// so speed is the fixed design lever and everything else (ring speed, projectile screen-space
/// velocity) derives from it per matchup.
///
/// Offense (LaunchProjectile's holdForOutcome=false) always resolves immediately on arrival — a
/// player's own attack never fully misses, timing only affects damage. Defense
/// (holdForOutcome=true) pauses the projectile at the target's edge instead of resolving, because
/// whether the hit actually lands isn't known until RunDefenseTimedInput itself finishes (the
/// player can click — and therefore Dodge/Parry — at any point in the sweep, including after the
/// projectile's own scheduled arrival instant); the caller must follow up with exactly one of
/// ResolveHeldProjectileAsHit/AsDodge/AsParryDeflect once the real outcome is known. Parry reuses
/// the SAME projectile instance, reversed and re-tinted, rather than firing an independent second
/// one for the separately-resolved counter-attack — one continuous visual beat for "parry, then
/// counter," not two overlapping ones.
///
/// Projectile pooling follows the Technical Directive's explicit §12.4 callout ("Projectile and
/// VFX particles during battle" must use UnityEngine.Pool.ObjectPool&lt;T&gt;). The stage-creature
/// hit-flash does NOT need pooling — it reuses elements that already exist permanently for the
/// whole battle, nothing is spawned/despawned for it. The three whole-Stage pulses
/// (PlayOutcomeFlash/PlayNameplateGlow/PlayCaptureFlash) are deliberately simple — the EventBus
/// events that trigger them carry only a PhasixRuntimeData, not a slot index, so there's no
/// reliable way to resolve "which stage element" from BattleAudioVfxHooks' subscriber context;
/// pulsing the whole Stage sidesteps that rather than inventing a lookup this pass doesn't need.
/// </summary>
public class CombatVfxController
{
    private const float HitFlashDuration = 0.2f;
    private const float HitFlashLightenAmount = 0.6f;
    private const float HitFlashAlpha = 0.85f;
    private const float StagePulseDuration = 0.3f;

    /// <summary>
    /// Dodge's projectile cleanup — deliberately brief and unremarkable (2026-08-11, user-directed:
    /// the real "you dodged!" cue is now the DEFENDER's own Phasix dissolving via
    /// DissolveVfxBridge's Shader Graph-equivalent material, not the projectile). This is just a
    /// quick fade so the projectile doesn't hard-pop out of existence, nothing more.
    /// </summary>
    private const float DodgeProjectileFadeDuration = 0.1f;

    /// <summary>Breathing-pulse rate (radians/sec) for a held projectile waiting on RunDefenseTimedInput's real outcome — reads as "poised, about to land" instead of a dead freeze-frame (2026-08-11, user-directed "stuck" feeling fix).</summary>
    private const float IdlePulseSpeed = 6f;
    private const float IdlePulseAmplitude = 0.15f;

    /// <summary>Parry's immediate "you parried!" cue — a bright purple outline flash on the DEFENDER's own stage element, fired the instant the parry registers, separate from the deflected-projectile-hits-the-attacker visual that follows (2026-08-11, user-directed).</summary>
    private const float ParryOutlineFlashDuration = 0.35f;
    private const float ParryOutlineBorderWidth = 5f;
    private static readonly Color ParryOutlineColor = new Color(0.68f, 0.25f, 0.98f, 1f);

    /// <summary>Used only if attacker/target stage elements can't be resolved (shouldn't happen in practice) — keeps the timing ring sane rather than degenerate.</summary>
    private const float FallbackTravelDuration = 0.4f;

    private static readonly Color WonPulseColor = new Color(1f, 0.9f, 0.3f, 0.5f);
    private static readonly Color LostPulseColor = new Color(0.6f, 0.15f, 0.15f, 0.5f);
    private static readonly Color BondPulseColor = new Color(1f, 0.85f, 0.3f, 0.4f);
    private static readonly Color CapturePulseColor = new Color(0.6f, 0.4f, 1f, 0.5f);

    private readonly MonoBehaviour _coroutineHost;
    private readonly VisualElement _stage;
    private readonly VisualElement[] _playerStageCreatures;
    private readonly VisualElement _enemyStageCreature;
    private readonly VisualElement _overlay;
    private readonly ObjectPool<CombatProjectileVisual> _projectilePool;

    /// <summary>The projectile currently paused at its target awaiting ResolveHeldProjectileAsX, or null if nothing is held. Single slot, not a collection — this project's battles are strictly sequential (one live timed input at a time), so only one projectile is ever held at once.</summary>
    private HeldProjectile _held;

    /// <summary>Reference type (not a struct) so ActiveRoutine can be filled in after the fact, once the travel-then-idle coroutine actually starts — see LaunchProjectile's holdForOutcome branch.</summary>
    private class HeldProjectile
    {
        public CombatProjectileVisual Projectile;
        public int AttackerSlotIndex;
        public bool AttackerIsPlayerSide;
        public int TargetSlotIndex;
        public bool TargetIsPlayerSide;
        public PrimalType ColorType;

        /// <summary>The currently running travel-then-idle-pulse coroutine (see AnimateAndHold) — stopped by every Resolve* method before touching the projectile, so an early resolve (a very fast click) can never leave a stale coroutine still animating a pooled element after it's released/repurposed.</summary>
        public Coroutine ActiveRoutine;
    }

    public CombatVfxController(MonoBehaviour coroutineHost, VisualElement stage, VisualElement[] playerStageCreatures, VisualElement enemyStageCreature)
    {
        _coroutineHost = coroutineHost;
        _stage = stage;
        _playerStageCreatures = playerStageCreatures;
        _enemyStageCreature = enemyStageCreature;

        _overlay = new VisualElement { pickingMode = PickingMode.Ignore };
        _overlay.style.position = Position.Absolute;
        _overlay.style.left = 0;
        _overlay.style.top = 0;
        _overlay.style.right = 0;
        _overlay.style.bottom = 0;
        _stage.Add(_overlay);

        _projectilePool = new ObjectPool<CombatProjectileVisual>(
            createFunc: CreatePooledProjectile,
            actionOnGet: p => p.style.display = DisplayStyle.Flex,
            actionOnRelease: p => p.style.display = DisplayStyle.None,
            actionOnDestroy: p => p.RemoveFromHierarchy(),
            collectionCheck: false,
            defaultCapacity: 4,
            maxSize: 8);
    }

    private CombatProjectileVisual CreatePooledProjectile()
    {
        var projectile = new CombatProjectileVisual { style = { display = DisplayStyle.None } };
        _overlay.Add(projectile);
        return projectile;
    }

    private VisualElement GetStageElement(int slotIndex, bool isPlayerSide)
    {
        if (!isPlayerSide) return _enemyStageCreature;
        if (_playerStageCreatures == null || slotIndex < 0 || slotIndex >= _playerStageCreatures.Length) return null;
        return _playerStageCreatures[slotIndex];
    }

    /// <summary>
    /// How long a projectile would take to travel edge-to-edge from attacker to target at
    /// BattleConfig.ProjectileSpeed — pure geometry, does not launch anything. Called by
    /// BattleHUDController.LaunchSyncedProjectile to size the matching ring BEFORE either the ring
    /// or the projectile starts, so both agree on timing from the first frame.
    /// </summary>
    public float ComputeTravelDuration(int attackerSlotIndex, bool attackerIsPlayerSide, int targetSlotIndex, bool targetIsPlayerSide)
    {
        VisualElement attackerElement = GetStageElement(attackerSlotIndex, attackerIsPlayerSide);
        VisualElement targetElement = GetStageElement(targetSlotIndex, targetIsPlayerSide);
        if (attackerElement == null || targetElement == null) return FallbackTravelDuration;

        return ComputeTravelDurationBetween(attackerElement, targetElement);
    }

    private static float ComputeTravelDurationBetween(VisualElement fromElement, VisualElement toElement)
    {
        float centerDistance = Vector2.Distance(fromElement.worldBound.center, toElement.worldBound.center);
        float toRadius = toElement.resolvedStyle.width / 2f;
        float effectiveDistance = Mathf.Max(0f, centerDistance - toRadius - CombatProjectileVisual.Radius);
        float rawDuration = effectiveDistance / BattleConfig.ProjectileSpeed;

        // Capped so a long-distance matchup can't also mean a long silent wait after arrival —
        // see BattleConfig.MaxProjectileTravelDuration's doc comment for why this doesn't disturb
        // ring-perfect alignment.
        return Mathf.Min(rawDuration, BattleConfig.MaxProjectileTravelDuration);
    }

    /// <summary>
    /// Launches a projectile from attacker to target, tinted by colorType, using the SAME
    /// travelDuration the caller already sized the ring against (passed in explicitly rather than
    /// recomputed here, so the ring and the projectile can never drift out of sync). Start/End are
    /// edge-adjusted — End is pulled back from the target's true center by (target radius +
    /// projectile radius) so arrival at progress=1 means the projectile's own outer edge is
    /// touching the target's outer edge, not full center-on-center overlap.
    ///
    /// holdForOutcome=false resolves immediately on arrival (hit-flash + release) — used for
    /// offense, which always connects. holdForOutcome=true instead pauses the projectile at that
    /// arrival point and records it as the currently held projectile; the caller must follow up
    /// with exactly one of ResolveHeldProjectileAsHit/AsDodge/AsParryDeflect. No-ops if either slot
    /// can't be resolved.
    /// </summary>
    public void LaunchProjectile(int attackerSlotIndex, bool attackerIsPlayerSide, int targetSlotIndex, bool targetIsPlayerSide, PrimalType colorType, float travelDuration, bool holdForOutcome)
    {
        VisualElement attackerElement = GetStageElement(attackerSlotIndex, attackerIsPlayerSide);
        VisualElement targetElement = GetStageElement(targetSlotIndex, targetIsPlayerSide);
        if (attackerElement == null || targetElement == null) return;

        CombatProjectileVisual projectile = _projectilePool.Get();
        SetEdgeAdjustedPath(projectile, attackerElement, targetElement);
        projectile.Tint = PrimalTypeColor.GetColor(colorType);
        projectile.SetAlpha(1f);
        projectile.SetProgress(0f);

        if (holdForOutcome)
        {
            var held = new HeldProjectile
            {
                Projectile = projectile,
                AttackerSlotIndex = attackerSlotIndex,
                AttackerIsPlayerSide = attackerIsPlayerSide,
                TargetSlotIndex = targetSlotIndex,
                TargetIsPlayerSide = targetIsPlayerSide,
                ColorType = colorType,
            };
            _held = held;
            held.ActiveRoutine = _coroutineHost.StartCoroutine(AnimateAndHold(projectile, travelDuration));
        }
        else
        {
            _coroutineHost.StartCoroutine(AnimateAndResolveImmediately(projectile, targetElement, travelDuration, colorType));
        }
    }

    private static void SetEdgeAdjustedPath(CombatProjectileVisual projectile, VisualElement fromElement, VisualElement toElement)
    {
        Vector2 fromWorld = fromElement.worldBound.center;
        Vector2 toWorld = toElement.worldBound.center;
        Vector2 direction = (toWorld - fromWorld).normalized;
        float toRadius = toElement.resolvedStyle.width / 2f;
        Vector2 edgeAdjustedToWorld = toWorld - direction * (toRadius + CombatProjectileVisual.Radius);

        projectile.Start = projectile.WorldToLocal(fromWorld);
        projectile.End = projectile.WorldToLocal(edgeAdjustedToWorld);
    }

    private IEnumerator AnimateAndResolveImmediately(CombatProjectileVisual projectile, VisualElement targetElement, float travelDuration, PrimalType colorType)
    {
        yield return AnimateTravel(projectile, travelDuration);
        _projectilePool.Release(projectile);
        FlashStageCreature(targetElement, colorType);
    }

    private IEnumerator AnimateTravel(CombatProjectileVisual projectile, float travelDuration)
    {
        float duration = Mathf.Max(0.01f, travelDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            projectile.SetProgress(elapsed / duration);
            yield return null;
        }
        projectile.SetProgress(1f);
    }

    /// <summary>Travels, then pulses in place indefinitely until externally stopped (see every Resolve* method's ActiveRoutine stop) — the held-and-waiting state a defense-side projectile sits in until RunDefenseTimedInput's real outcome is known.</summary>
    private IEnumerator AnimateAndHold(CombatProjectileVisual projectile, float travelDuration)
    {
        yield return AnimateTravel(projectile, travelDuration);

        while (true)
        {
            float pulse = 1f + Mathf.Sin(Time.time * IdlePulseSpeed) * IdlePulseAmplitude;
            projectile.SetPulseScale(pulse);
            yield return null;
        }
    }

    /// <summary>Resolves the held projectile as a landed hit — flashes the target, releases the projectile. No-op if nothing is currently held.</summary>
    public void ResolveHeldProjectileAsHit()
    {
        if (_held == null) return;
        HeldProjectile held = _held;
        _held = null;
        StopHeldRoutine(held);

        _projectilePool.Release(held.Projectile);
        VisualElement targetElement = GetStageElement(held.TargetSlotIndex, held.TargetIsPlayerSide);
        FlashStageCreature(targetElement, held.ColorType);
    }

    /// <summary>
    /// Resolves the held projectile as a successful Dodge by continuing it THROUGH and past the
    /// defender — rather than stopping at the near edge and fading in place — timed to take
    /// exactly passThroughDuration (the caller passes DissolveVfxBridge.DissolveOutDuration) so
    /// the projectile visually crosses the defender's position in sync with them phasing out
    /// (2026-08-11, user-directed: "the projectile passes through the defending phasix, then on
    /// success the dissolve effect happens as the projectile passes through"). Falls back to a
    /// plain in-place fade if either element can't be resolved. No-op if nothing is currently held.
    /// </summary>
    public void ResolveHeldProjectileAsPassThrough(float passThroughDuration)
    {
        if (_held == null) return;
        HeldProjectile held = _held;
        _held = null;
        StopHeldRoutine(held);

        VisualElement attackerElement = GetStageElement(held.AttackerSlotIndex, held.AttackerIsPlayerSide);
        VisualElement targetElement = GetStageElement(held.TargetSlotIndex, held.TargetIsPlayerSide);
        if (attackerElement == null || targetElement == null)
        {
            _coroutineHost.StartCoroutine(FadeProjectileAndRelease(held.Projectile));
            return;
        }

        CombatProjectileVisual projectile = held.Projectile;

        Vector2 attackerWorld = attackerElement.worldBound.center;
        Vector2 targetWorld = targetElement.worldBound.center;
        Vector2 direction = (targetWorld - attackerWorld).normalized;
        float edgeOffset = targetElement.resolvedStyle.width / 2f + CombatProjectileVisual.Radius;

        // The projectile is already at the near edge (where it was held) — this just re-derives
        // that same point as the new leg's Start, then mirrors it across the defender's center to
        // get the far-edge exit point.
        Vector2 nearEdgeWorld = targetWorld - direction * edgeOffset;
        Vector2 farEdgeWorld = targetWorld + direction * edgeOffset;

        projectile.Start = projectile.WorldToLocal(nearEdgeWorld);
        projectile.End = projectile.WorldToLocal(farEdgeWorld);
        projectile.SetProgress(0f);

        _coroutineHost.StartCoroutine(PassThroughThenFadeRoutine(projectile, passThroughDuration));
    }

    private IEnumerator PassThroughThenFadeRoutine(CombatProjectileVisual projectile, float travelDuration)
    {
        yield return AnimateTravel(projectile, travelDuration);
        yield return FadeProjectileAndRelease(projectile);
    }

    /// <summary>Stops whatever stage of the travel-then-idle-pulse coroutine a held projectile is currently in (still traveling, or already idling) — must run before the projectile is released, repurposed, or its pulse scale reset, or the old coroutine keeps animating a now-pooled-and-reused element.</summary>
    private void StopHeldRoutine(HeldProjectile held)
    {
        if (held.ActiveRoutine != null) _coroutineHost.StopCoroutine(held.ActiveRoutine);
        held.Projectile.SetPulseScale(1f);
    }

    /// <summary>Quick, unremarkable fade-out for the projectile on a Dodge — the real "dodged!" cue plays on the defender itself (DissolveVfxBridge), this just cleans the projectile up quietly.</summary>
    private IEnumerator FadeProjectileAndRelease(CombatProjectileVisual projectile)
    {
        float elapsed = 0f;
        while (elapsed < DodgeProjectileFadeDuration)
        {
            elapsed += Time.deltaTime;
            projectile.SetAlpha(1f - elapsed / DodgeProjectileFadeDuration);
            yield return null;
        }
        _projectilePool.Release(projectile);
    }

    /// <summary>
    /// Flashes the purple "you parried!" outline on the CURRENTLY HELD projectile's target
    /// element (the defender) WITHOUT touching the projectile itself — call this the instant
    /// Parry is detected (2026-08-11 fix: previously bundled into ResolveHeldProjectileAsParryDeflect,
    /// which BattleManager only calls after RunDefenseTimedInput's own ~0.3s ring-flash hold plus
    /// its own damage/logging work, making the flash feel disconnected from the actual click —
    /// live-verified via playtest: "it flashes purple immediately after it shoots the parry
    /// attack," not at the moment of the hit). No-op if nothing is currently held or the element
    /// can't be resolved. Safe to call independently of ResolveHeldProjectileAsParryDeflect, which
    /// still handles the deflect-and-counter projectile separately once BattleManager knows the
    /// counter-attacker's own Primal type.
    /// </summary>
    public void FlashHeldProjectileParryOutline()
    {
        if (_held == null) return;
        VisualElement defenderElement = GetStageElement(_held.TargetSlotIndex, _held.TargetIsPlayerSide);
        if (defenderElement == null) return;
        _coroutineHost.StartCoroutine(ParryOutlineFlashRoutine(defenderElement));
    }

    /// <summary>
    /// Resolves the held projectile as a successful Parry — reuses the same projectile instance,
    /// reversing its path back toward the original attacker and re-tinting it as counterColorType
    /// (the counter-attacker's own Primal type), arriving to flash the original attacker. This
    /// beat IS the counter-attack's hit feedback — BattleManager should not also call
    /// LaunchProjectile separately for that damage. Returns the projectile's real travel duration
    /// (0f if nothing was actually launched) so the caller can await it before applying the
    /// counter's damage — the flash fires here, on arrival, INSIDE that awaited window, so the
    /// projectile's visual hit, this flash, and the caller's subsequent damage/HP-bar update all
    /// land in the same beat (2026-08-11, user-directed: "I need the damage to register the moment
    /// the projectile hits the target" — see BattleManager.ResolveEnemyDamageAction's counter-
    /// attack block for the await). The defender's own outline flash (the "you parried!" cue) is a
    /// SEPARATE, earlier call — see FlashHeldProjectileParryOutline, called by
    /// BattleHUDController.RunDefenseTimedInput immediately on detecting Parry, not here. No-op
    /// (returns 0f) if nothing is currently held or either side's element can't be resolved
    /// (releases the projectile in that case rather than leaving it stuck).
    /// </summary>
    public float ResolveHeldProjectileAsParryDeflect(PrimalType counterColorType)
    {
        if (_held == null) return 0f;
        HeldProjectile held = _held;
        _held = null;
        StopHeldRoutine(held);

        VisualElement returnFromElement = GetStageElement(held.TargetSlotIndex, held.TargetIsPlayerSide);
        VisualElement returnToElement = GetStageElement(held.AttackerSlotIndex, held.AttackerIsPlayerSide);
        if (returnFromElement == null || returnToElement == null)
        {
            _projectilePool.Release(held.Projectile);
            return 0f;
        }

        CombatProjectileVisual projectile = held.Projectile;
        SetEdgeAdjustedPath(projectile, returnFromElement, returnToElement);
        projectile.Tint = PrimalTypeColor.GetColor(counterColorType);
        projectile.SetProgress(0f);

        float travelDuration = ComputeTravelDurationBetween(returnFromElement, returnToElement);
        _coroutineHost.StartCoroutine(AnimateAndResolveImmediately(projectile, returnToElement, travelDuration, counterColorType));
        return travelDuration;
    }

    /// <summary>
    /// colorType is the IMPACT flavor only (the attacker's type, used for the brief bright tint) —
    /// it is NOT what the element reverts to afterward. Reverting must restore whatever color the
    /// struck creature actually had a moment ago (its own Primal type, set once by
    /// SetStageCreatureColor at battle start), not the attacker's — using colorType for both was a
    /// live-verified bug (caught via Play Mode screenshot: a hit permanently repainted the target
    /// to the ATTACKER's color instead of its own). Fixed by capturing the element's real current
    /// background at flash-start and reverting to that literal captured value, so this never needs
    /// to know or guess the "correct" type to revert to.
    /// </summary>
    private void FlashStageCreature(VisualElement element, PrimalType colorType)
    {
        if (element == null) return;
        _coroutineHost.StartCoroutine(HitFlashRoutine(element, colorType));
    }

    /// <summary>
    /// Public passthrough to the private FlashStageCreature/HitFlashRoutine above — added 2026-08-11
    /// for the melee Beat Sequence's Attack beat (BattleManager.ResolveMeleeAttackBeatOffense/
    /// Defense), which has no projectile to carry a hit-flash and needs to trigger one directly.
    /// Also corrects a doc/code discrepancy: DECISIONS.md/CHANGELOG.md's 2026-08-11 "Parry
    /// counter-attack hit-flash" entries described this exact method (and BattleHUDController.
    /// FlashStageCreatureHit below) as already built that session — verified via grep it did not
    /// actually exist until now; see DECISIONS.md -> [Combat] for the correction.
    /// </summary>
    public void FlashStageElement(VisualElement element, PrimalType colorType) => FlashStageCreature(element, colorType);

    private IEnumerator HitFlashRoutine(VisualElement element, PrimalType colorType)
    {
        Color restingColor = element.resolvedStyle.backgroundColor;
        Color flashColor = PrimalTypeColor.GetUnderglowColor(colorType, HitFlashLightenAmount, HitFlashAlpha);

        float elapsed = 0f;
        while (elapsed < HitFlashDuration)
        {
            elapsed += Time.deltaTime;
            element.style.backgroundColor = Color.Lerp(flashColor, restingColor, elapsed / HitFlashDuration);
            yield return null;
        }
        element.style.backgroundColor = restingColor;
    }

    /// <summary>
    /// Parry's immediate feedback — a bright purple ring around the defender's own stage element
    /// (which is already circular via its own USS border-radius, so a plain border renders as a
    /// proper outline, not a square box), fading out over ParryOutlineFlashDuration. Reverts to a
    /// zero-width/clear border rather than a captured "original" value — unlike backgroundColor
    /// (owned by SetStageCreatureColor), nothing else in this codebase sets a border on stage
    /// elements, so there is no prior state to preserve.
    /// </summary>
    private IEnumerator ParryOutlineFlashRoutine(VisualElement element)
    {
        float elapsed = 0f;
        while (elapsed < ParryOutlineFlashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - elapsed / ParryOutlineFlashDuration;
            Color color = ParryOutlineColor;
            color.a = alpha;
            SetUniformBorder(element, ParryOutlineBorderWidth, color);
            yield return null;
        }
        SetUniformBorder(element, 0f, Color.clear);
    }

    private static void SetUniformBorder(VisualElement element, float width, Color color)
    {
        element.style.borderTopWidth = width;
        element.style.borderBottomWidth = width;
        element.style.borderLeftWidth = width;
        element.style.borderRightWidth = width;
        element.style.borderTopColor = color;
        element.style.borderBottomColor = color;
        element.style.borderLeftColor = color;
        element.style.borderRightColor = color;
    }

    /// <summary>Brief whole-Stage color pulse for a battle outcome — gold for a win, red for a loss. Fled plays no pulse (matches the existing "fleeing has zero cost" distinction from a real loss).</summary>
    public void PlayOutcomeFlash(bool won) => _coroutineHost.StartCoroutine(StagePulseRoutine(won ? WonPulseColor : LostPulseColor));

    /// <summary>Brief whole-Stage gold pulse for a bond milestone.</summary>
    public void PlayNameplateGlow() => _coroutineHost.StartCoroutine(StagePulseRoutine(BondPulseColor));

    /// <summary>Brief whole-Stage pulse for a successful capture.</summary>
    public void PlayCaptureFlash() => _coroutineHost.StartCoroutine(StagePulseRoutine(CapturePulseColor));

    private IEnumerator StagePulseRoutine(Color pulseColor)
    {
        float elapsed = 0f;
        while (elapsed < StagePulseDuration)
        {
            elapsed += Time.deltaTime;
            Color faded = pulseColor;
            faded.a = Mathf.Lerp(pulseColor.a, 0f, elapsed / StagePulseDuration);
            _stage.style.backgroundColor = faded;
            yield return null;
        }
        // Clears the inline override entirely rather than restoring a captured value — nothing
        // else sets _stage's background at runtime, so Null correctly reveals whatever its USS
        // class defines (unlike the per-creature flash above, which owns a real inline color).
        _stage.style.backgroundColor = StyleKeyword.Null;
    }
}
