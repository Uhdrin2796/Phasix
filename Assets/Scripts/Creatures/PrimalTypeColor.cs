using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps PrimalType to a placeholder display color, per the placeholder-first art pipeline
/// (DECISIONS.md → [Art] Placeholder-first pipeline). The 8 base colors were originally
/// transcribed verbatim from a locked table sourced from the GDD's Primal wheel diagram
/// (§9). The 28 duo-merge parent pairs are transcribed verbatim from the GDD §9 "All 28 duo
/// merged types" table — not invented.
///
/// 2026-08-20 (Attack_Pattern_Directive Part 5 Group 3, Zone/Positional VFX pass — user-directed
/// deliberate content change, not a Claude-invented override of locked values): Fire and Water's
/// base hex values were RAISED in saturation/luminance — see DECISIONS.md -> [Art] for the full
/// before/after and rationale (a 60/30/10 contrast pass across the battle stage: PrimalType colors
/// are the "30%" identity layer and needed more pop against the near-black "60%" stage background,
/// distinct from the "10%" accent reds Zone/Positional's danger signals now use). Every other base
/// color and all 28 duo-merge pairs are untouched.
///
/// Duo-merge colors are computed (50/50 blend of the two parents), never hand-authored, so
/// this stays cheap to extend if triple merges are ever added post-roster.
/// </summary>
public static class PrimalTypeColor
{
    private static readonly Dictionary<PrimalType, Color> BaseColors = new Dictionary<PrimalType, Color>
    {
        { PrimalType.Fire,      HexColor("#E8511A") },
        { PrimalType.Water,     HexColor("#1E90D4") },
        { PrimalType.Earth,     HexColor("#7A5A20") },
        { PrimalType.Wind,      HexColor("#207A40") },
        { PrimalType.Light,     HexColor("#807010") },
        { PrimalType.Shadow,    HexColor("#503070") },
        { PrimalType.Life,      HexColor("#2A7A2A") },
        { PrimalType.Lightning, HexColor("#B09020") },
    };

    // Parent pairs for the 28 duo merges — GDD §9, "All 28 duo merged types" table.
    private static readonly Dictionary<PrimalType, (PrimalType a, PrimalType b)> DuoParents =
        new Dictionary<PrimalType, (PrimalType, PrimalType)>
    {
        { PrimalType.Steam,     (PrimalType.Fire, PrimalType.Water) },
        { PrimalType.Magma,     (PrimalType.Fire, PrimalType.Earth) },
        { PrimalType.Blaze,     (PrimalType.Fire, PrimalType.Wind) },
        { PrimalType.Radiance,  (PrimalType.Fire, PrimalType.Light) },
        { PrimalType.Cinder,    (PrimalType.Fire, PrimalType.Shadow) },
        { PrimalType.Ember,     (PrimalType.Fire, PrimalType.Life) },
        { PrimalType.Plasma,    (PrimalType.Fire, PrimalType.Lightning) },

        { PrimalType.Brine,     (PrimalType.Water, PrimalType.Earth) },
        { PrimalType.Frost,     (PrimalType.Water, PrimalType.Wind) },
        { PrimalType.Tide,      (PrimalType.Water, PrimalType.Light) },
        { PrimalType.Abyss,     (PrimalType.Water, PrimalType.Shadow) },
        { PrimalType.Bloom,     (PrimalType.Water, PrimalType.Life) },
        { PrimalType.Discharge, (PrimalType.Water, PrimalType.Lightning) },

        { PrimalType.Dust,      (PrimalType.Earth, PrimalType.Wind) },
        { PrimalType.Crystal,   (PrimalType.Earth, PrimalType.Light) },
        { PrimalType.Grave,     (PrimalType.Earth, PrimalType.Shadow) },
        { PrimalType.Grove,     (PrimalType.Earth, PrimalType.Life) },
        { PrimalType.Forge,     (PrimalType.Earth, PrimalType.Lightning) },

        { PrimalType.Gale,      (PrimalType.Wind, PrimalType.Light) },
        { PrimalType.Murk,      (PrimalType.Wind, PrimalType.Shadow) },
        { PrimalType.Spore,     (PrimalType.Wind, PrimalType.Life) },
        { PrimalType.Storm,     (PrimalType.Wind, PrimalType.Lightning) },

        { PrimalType.Eclipse,   (PrimalType.Light, PrimalType.Shadow) },
        { PrimalType.Dawn,      (PrimalType.Light, PrimalType.Life) },
        { PrimalType.Flash,     (PrimalType.Light, PrimalType.Lightning) },

        { PrimalType.Rot,       (PrimalType.Shadow, PrimalType.Life) },
        { PrimalType.Void,      (PrimalType.Shadow, PrimalType.Lightning) },

        { PrimalType.Spark,     (PrimalType.Life, PrimalType.Lightning) },
    };

    /// <summary>
    /// The two base-type parents of a duo-merge PrimalType (e.g. Steam -> Fire, Water). Throws for
    /// a base type, which has no parents — check the type first if it might be either. Exposes the
    /// same parent-pair data GetColor() already uses internally, so callers needing the parents for
    /// their own purposes (e.g. PrimalTypeChart.cs resolving duo-type matchups) don't duplicate it.
    /// </summary>
    public static (PrimalType a, PrimalType b) GetDuoParents(PrimalType type) => DuoParents[type];

    /// <summary>Base types return their locked hex color; duo types return a 50/50 blend of their two parents.</summary>
    public static Color GetColor(PrimalType type)
    {
        if (BaseColors.TryGetValue(type, out Color baseColor))
            return baseColor;

        (PrimalType a, PrimalType b) parents = DuoParents[type];
        return Color.Lerp(BaseColors[parents.a], BaseColors[parents.b], 0.5f);
    }

    /// <summary>
    /// Same hue as GetColor(), lightened toward white by <paramref name="lightenAmount"/> and
    /// made translucent via <paramref name="alpha"/> — used for the underglow halo layer so
    /// it reads as "same type, softer" rather than a duplicate disc. Takes the tuning values
    /// as parameters (rather than reading them itself) so this stays a pure lookup function
    /// with no dependency on any specific MonoBehaviour's Inspector fields.
    /// </summary>
    public static Color GetUnderglowColor(PrimalType type, float lightenAmount, float alpha)
    {
        Color baseColor = GetColor(type);
        Color lightened = Color.Lerp(baseColor, Color.white, lightenAmount);
        lightened.a = alpha;
        return lightened;
    }

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
