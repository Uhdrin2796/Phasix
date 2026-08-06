using System.Collections.Generic;

/// <summary>
/// Chain result recipes and effects, GDD §17.8 (Locked v0.7.8), transcribed verbatim. A chain
/// triggers when both statuses of any one recipe pair are simultaneously active on the same
/// target — several results have more than one valid pair (an "OR" of recipes).
///
/// Tie-break note: the GDD doesn't specify what happens if a target's active statuses satisfy
/// two different chain results' recipes at once (e.g. Fracture+Stun -> Shatter AND, separately,
/// Fracture+Weaken -> Rend, both present together). TryResolve returns the first match in
/// declaration order below — a placeholder tie-break, not a locked resolution rule.
/// </summary>
public static class ChainResultCatalog
{
    private readonly struct Recipe
    {
        public readonly ChainResultType Result;
        public readonly StatusEffectType A;
        public readonly StatusEffectType B;

        public Recipe(ChainResultType result, StatusEffectType a, StatusEffectType b)
        {
            Result = result;
            A = a;
            B = b;
        }
    }

    private static readonly Recipe[] Recipes =
    {
        new Recipe(ChainResultType.Rend, StatusEffectType.Bleed, StatusEffectType.Weaken),
        new Recipe(ChainResultType.Rend, StatusEffectType.Bleed, StatusEffectType.Fracture),

        new Recipe(ChainResultType.Entomb, StatusEffectType.Root, StatusEffectType.Freeze),
        new Recipe(ChainResultType.Entomb, StatusEffectType.Drown, StatusEffectType.Freeze),

        new Recipe(ChainResultType.Paralysis, StatusEffectType.Stun, StatusEffectType.Shock),

        new Recipe(ChainResultType.Scorch, StatusEffectType.Burn, StatusEffectType.Exposed),

        new Recipe(ChainResultType.Sap, StatusEffectType.Drain, StatusEffectType.Weaken),
        new Recipe(ChainResultType.Sap, StatusEffectType.Drain, StatusEffectType.Slow),
        new Recipe(ChainResultType.Sap, StatusEffectType.Disrupt, StatusEffectType.Drain),

        new Recipe(ChainResultType.Dissolve, StatusEffectType.Corrode, StatusEffectType.Exposed),
        new Recipe(ChainResultType.Dissolve, StatusEffectType.Corrode, StatusEffectType.Wither),

        new Recipe(ChainResultType.Shatter, StatusEffectType.Fracture, StatusEffectType.Stun),
        new Recipe(ChainResultType.Shatter, StatusEffectType.Freeze, StatusEffectType.Stun),
    };

    private static readonly Dictionary<ChainResultType, string> Effects = new Dictionary<ChainResultType, string>
    {
        { ChainResultType.Rend, "Heavy DoT + compromised defenses. Physical skills deal +45%." },
        { ChainResultType.Entomb, "Complete immobilisation. Strongest CC in the game." },
        { ChainResultType.Paralysis, "Acts every other turn; passive Lightning damage on skip turns; reduced accuracy on act turns." },
        { ChainResultType.Scorch, "Damage bypasses Ward entirely (raw damage)." },
        { ChainResultType.Sap, "Aura loss + reduced damage + slower acting. Most accessible chain." },
        { ChainResultType.Dissolve, "Guard/Ward near-zero. Short duration (2-3 turns)." },
        { ChainResultType.Shatter, "+50% physical damage, no defensive reactions. Hardest to land, highest ceiling." },
    };

    /// <summary>Returns true and sets result if any recipe's pair is fully contained in activeOnTarget.</summary>
    public static bool TryResolve(ICollection<StatusEffectType> activeOnTarget, out ChainResultType result)
    {
        foreach (Recipe recipe in Recipes)
        {
            if (activeOnTarget.Contains(recipe.A) && activeOnTarget.Contains(recipe.B))
            {
                result = recipe.Result;
                return true;
            }
        }

        result = default;
        return false;
    }

    public static string GetEffectDescription(ChainResultType result) => Effects[result];
}
