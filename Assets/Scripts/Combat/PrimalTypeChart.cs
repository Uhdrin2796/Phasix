using UnityEngine;

/// <summary>
/// The 8x8 base Primal type matchup chart — GDD_CreatureRPG_v0_8_0.html Section 9, "Primal Type
/// System, Locked v0.2.0." Row = attacker, column = defender. Values transcribed verbatim from the
/// GDD's locked "Full 8x8 Matchup Chart" table, not invented — this is authoritative design
/// content, unlike most of Phase 3's other placeholder numbers.
///
/// Duo-merge types (28 of the 36 total PrimalType values) aren't individually charted in the GDD —
/// "8 base types with plain English names... complexity lives in the merged type layer, not the
/// base names." GetMultiplier resolves a duo type to its two base parents (via
/// PrimalTypeColor.GetDuoParents — the same parent-pair data already used for duo color blending)
/// and averages the multiplier across all parent combinations. TODO: pending design — replace this
/// averaging fallback if dedicated duo-vs-duo matchup rules are ever specified.
/// </summary>
[CreateAssetMenu(fileName = "PrimalTypeChart", menuName = "Phasix/Combat/Primal Type Chart", order = 20)]
public class PrimalTypeChart : ScriptableObject
{
    private const int BaseTypeCount = 8; // PrimalType.Fire .. PrimalType.Lightning

    /// <summary>"No immunities — every type deals at least some damage" (CLAUDE.md / GDD).</summary>
    public const float MinimumMultiplier = 0.5f;

    // Row-major: index = attacker * BaseTypeCount + defender, in PrimalType enum order
    // (Fire, Water, Earth, Wind, Light, Shadow, Life, Lightning). Diagonal (self-matchup) is 1.0
    // (neutral) — the GDD table shows "-" there since a type never fights its own name, but a
    // working formula needs some value, and neutral is the only value consistent with "no
    // immunities."
    [SerializeField]
    private float[] _baseMultipliers =
    {
        // Fire   Water  Earth  Wind   Light  Shadow Life   Lightning
        /*Fire*/      1.00f, 0.50f, 1.25f, 2.00f, 0.75f, 1.25f, 2.00f, 1.00f,
        /*Water*/     2.00f, 1.00f, 1.25f, 1.00f, 1.00f, 1.00f, 1.25f, 0.75f,
        /*Earth*/     0.75f, 0.75f, 1.00f, 0.50f, 1.00f, 1.25f, 1.25f, 2.00f,
        /*Wind*/      0.50f, 1.00f, 2.00f, 1.00f, 1.25f, 0.75f, 1.00f, 0.50f,
        /*Light*/     1.25f, 1.00f, 1.00f, 0.75f, 1.00f, 2.00f, 1.25f, 1.00f,
        /*Shadow*/    0.75f, 1.00f, 0.75f, 1.25f, 2.00f, 1.00f, 0.50f, 1.25f,
        /*Life*/      0.50f, 2.00f, 0.75f, 1.00f, 0.75f, 1.25f, 1.00f, 1.00f,
        /*Lightning*/ 1.00f, 1.25f, 0.50f, 2.00f, 1.00f, 0.75f, 1.00f, 1.00f,
    };

    /// <summary>
    /// Multiplier for an attack of PrimalType attacker against a defender of PrimalType defender.
    /// Base types use the locked chart directly; duo types average across their base parents.
    /// </summary>
    public float GetMultiplier(PrimalType attacker, PrimalType defender)
    {
        PrimalType[] attackerBases = ResolveToBaseTypes(attacker);
        PrimalType[] defenderBases = ResolveToBaseTypes(defender);

        float total = 0f;
        foreach (PrimalType a in attackerBases)
        {
            foreach (PrimalType d in defenderBases)
            {
                total += _baseMultipliers[(int)a * BaseTypeCount + (int)d];
            }
        }

        return total / (attackerBases.Length * defenderBases.Length);
    }

    private static PrimalType[] ResolveToBaseTypes(PrimalType type)
    {
        if ((int)type < BaseTypeCount) return new[] { type };

        (PrimalType a, PrimalType b) = PrimalTypeColor.GetDuoParents(type);
        return new[] { a, b };
    }
}
