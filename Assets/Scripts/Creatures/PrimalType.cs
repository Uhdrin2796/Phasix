/// <summary>
/// 8 base types + 28 duo merges (36 total). GDD §9 — Locked v0.2.0 / merge names Done v0.7.7.
/// Triple merges intentionally omitted — explicitly still pending the species roster phase.
/// Lives on PhasixData — no evidence of per-individual variance.
/// </summary>
public enum PrimalType
{
    // 8 base types
    Fire, Water, Earth, Wind, Light, Shadow, Life, Lightning,

    // 28 duo merges — names locked v0.7.7 (post naming-conflict-audit renames:
    // Surge->Discharge for Water+Lightning, Pulse->Flash for Light+Lightning)
    Steam, Magma, Blaze, Radiance, Cinder, Ember, Plasma,
    Brine, Frost, Tide, Abyss, Bloom, Discharge,
    Dust, Crystal, Grave, Grove, Forge,
    Gale, Murk, Spore, Storm,
    Eclipse, Dawn, Flash,
    Rot, Void, Spark
}
