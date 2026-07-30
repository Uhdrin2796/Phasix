using UnityEngine;

/// <summary>
/// Bond gain/loss rules: floor logic, session loss cap, and high-bond damping. Authority:
/// CLAUDE.md Bond section, GDD Section 14 (Locked), Roadmap_v2.md Wk 10.
///
/// This is intentionally a rules-enforcement layer only — it does not know or decide gain
/// or loss AMOUNTS (those come from combat/activity systems, pending NumericalCalibration.md).
/// Callers compute a raw delta (already including any source-specific multipliers, e.g.
/// Origin's "preferred activity 2x gain") and pass it in; BondSystem enforces the structural
/// rules on top of that.
///
/// Static class — no MonoBehaviour, no scene dependency — matching EventBus.cs's pattern
/// and easy to EditMode-test in isolation.
/// </summary>
public static class BondSystem
{
    /// <summary>Session loss cap — locked value, not pending calibration (CLAUDE.md).</summary>
    private const float SessionLossCapPercent = 5f;

    /// <summary>
    /// Applies a raw bond delta (positive = gain, negative = loss) to a creature, enforcing
    /// the floor, session loss cap, and high-bond damping rules. No-op if the creature is
    /// already at 100% bond (permanent, immune to any change via this path).
    ///
    /// NOTE: Does not handle "Origin Change" (GDD Section 14.4), which is the one deliberate
    /// exception allowed to break through a bond floor and lower it. Origin Change must set
    /// bondFloor directly rather than going through ApplyBondChange.
    /// </summary>
    public static void ApplyBondChange(PhasixRuntimeData phasix, float rawDelta)
    {
        if (phasix == null || rawDelta == 0f) return;
        if (phasix.bondPercent >= 100f) return; // 100% bond is a permanent achievement — immune to any change here

        if (rawDelta < 0f)
        {
            ApplyLoss(phasix, rawDelta);
        }
        else
        {
            ApplyGain(phasix, rawDelta);
        }
    }

    /// <summary>Resets the session loss counter. Call on hub visit / bank once that system exists.</summary>
    public static void ResetSessionLoss(PhasixRuntimeData phasix)
    {
        if (phasix != null) phasix.sessionBondLoss = 0f;
    }

    private static void ApplyGain(PhasixRuntimeData phasix, float delta)
    {
        float newBond = Mathf.Min(100f, phasix.bondPercent + delta);
        SetBondPercent(phasix, newBond);
    }

    private static void ApplyLoss(PhasixRuntimeData phasix, float delta)
    {
        float dampedDelta = ApplyDamping(phasix.bondPercent, delta);
        float cappedDelta = ApplySessionCap(phasix, dampedDelta);

        // Floor system: normal loss can never push bond below the last milestone floor.
        float newBond = Mathf.Max(phasix.bondFloor, phasix.bondPercent + cappedDelta);
        SetBondPercent(phasix, newBond);
    }

    /// <summary>Above 60% (Partner): losses halved. Above 80% (Bonded): losses quartered.</summary>
    private static float ApplyDamping(float currentBondPercent, float lossDelta)
    {
        if (currentBondPercent > (float)BondZone.Bonded) return lossDelta * 0.25f;
        if (currentBondPercent > (float)BondZone.Partner) return lossDelta * 0.5f;
        return lossDelta;
    }

    /// <summary>Clamps a (already-damped) loss so cumulative session loss never exceeds the cap.</summary>
    private static float ApplySessionCap(PhasixRuntimeData phasix, float lossDelta)
    {
        float remainingCap = SessionLossCapPercent - phasix.sessionBondLoss;
        if (remainingCap <= 0f) return 0f;

        float requestedLossMagnitude = -lossDelta;
        float actualLossMagnitude = Mathf.Min(requestedLossMagnitude, remainingCap);
        phasix.sessionBondLoss += actualLossMagnitude;
        return -actualLossMagnitude;
    }

    private static void SetBondPercent(PhasixRuntimeData phasix, float newBond)
    {
        newBond = Mathf.Clamp(newBond, 0f, 100f);
        if (Mathf.Approximately(newBond, phasix.bondPercent)) return;

        phasix.bondPercent = newBond;
        EventBus.Raise_BondChanged(phasix, newBond);

        CheckMilestones(phasix);
    }

    /// <summary>Fires OnBondMilestoneReached for every zone newly crossed, low to high, and raises bondFloor to match.</summary>
    private static void CheckMilestones(PhasixRuntimeData phasix)
    {
        CheckMilestone(phasix, BondZone.Familiar);
        CheckMilestone(phasix, BondZone.Companion);
        CheckMilestone(phasix, BondZone.Partner);
        CheckMilestone(phasix, BondZone.Bonded);
        CheckMilestone(phasix, BondZone.Complete);
    }

    private static void CheckMilestone(PhasixRuntimeData phasix, BondZone zone)
    {
        float threshold = (float)zone;
        if (phasix.bondPercent >= threshold && phasix.bondFloor < threshold)
        {
            phasix.bondFloor = threshold;
            EventBus.Raise_BondMilestoneReached(phasix, zone);
        }
    }
}
