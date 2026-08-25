using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Heuristic enemy target/skill selection (2026-08-10 — Phase 3 close-out pass). Replaces the old
/// pure Random.Range target choice + hardcoded basic attack in BattleManager.EnemyTurn with
/// weighted targeting and real use of an enemy's equipped skills, now that the skill system
/// (SkillDatabase, PlaceholderSkillResolver) actually exists.
///
/// Deliberately scoped as "make enemies use the systems that already exist," NOT the real AI
/// decision-making framework Combat_Directive_v0_1_0.md flags as pending design (GDD §18.6) —
/// every weight here is a flat, placeholder BattleConfig constant (same convention as every other
/// unbalanced number in this file), not a difficulty tier or scoring framework. A real framework
/// remains pending design.
/// </summary>
public static class EnemyAI
{
    public enum EnemyMoveIntent
    {
        Damage,
        SelfSupport,
        Debuff,
    }

    /// <summary>
    /// Pure/deterministic — no Random. Weights toward lower current-HP% and type-effective
    /// targets (via PrimalTypeChart.GetMultiplier), with a flat +1f floor so no alive target is
    /// ever fully excluded, only biased against. typeChart == null (or either side's species data
    /// missing) drops the type term to neutral rather than throwing.
    /// </summary>
    public static float ComputeTargetWeight(BattleParticipant attacker, BattleParticipant candidate, PrimalTypeChart typeChart)
    {
        float hpFactor = 1f - (candidate.CurrentHP / (float)candidate.MaxHP);

        float typeFactor = 0f;
        if (typeChart != null && attacker.RuntimeData.speciesData != null && candidate.RuntimeData.speciesData != null)
        {
            float multiplier = typeChart.GetMultiplier(attacker.RuntimeData.speciesData.PrimalType, candidate.RuntimeData.speciesData.PrimalType);
            typeFactor = multiplier - 1f; // 0 = neutral; positive favors super-effective, negative discourages resisted
        }

        float weight = 1f + hpFactor * BattleConfig.EnemyTargetLowHpWeight + typeFactor * BattleConfig.EnemyTargetTypeEffectivenessWeight;
        return Mathf.Max(0.01f, weight);
    }

    /// <summary>
    /// Weighted-random pick over ComputeTargetWeight scores. Null for an empty list (matches
    /// EnemyTurn's existing empty-target guard); a single candidate is returned directly, no roll.
    /// </summary>
    public static BattleParticipant ChooseTarget(BattleParticipant attacker, List<BattleParticipant> aliveCandidates, PrimalTypeChart typeChart)
    {
        if (aliveCandidates == null || aliveCandidates.Count == 0) return null;
        if (aliveCandidates.Count == 1) return aliveCandidates[0];

        var weights = new float[aliveCandidates.Count];
        float total = 0f;
        for (int i = 0; i < aliveCandidates.Count; i++)
        {
            weights[i] = ComputeTargetWeight(attacker, aliveCandidates[i], typeChart);
            total += weights[i];
        }

        float roll = Random.value * total;
        float cumulative = 0f;
        for (int i = 0; i < aliveCandidates.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative) return aliveCandidates[i];
        }

        return aliveCandidates[aliveCandidates.Count - 1]; // floating-point rounding fallback
    }

    /// <summary>
    /// Resolves attacker's RuntimeData.equippedSkillGuids into a chosen move + intent bucket.
    /// Hard-excludes BuiltInMoveType.Capture (never a sensible enemy action) and
    /// BuiltInMoveType.Move (2026-08-12 — no AI logic exists yet for deciding when an enemy should
    /// reposition on the formation grid). Buckets the rest into Damage / SelfSupport / Debuff using
    /// BuiltInMoveType for the remaining built-ins and PlaceholderSkillResolver.Resolve()'s
    /// DealsDamage/SelfTargeted flags for tree skills. Below
    /// BattleConfig.EnemySelfCareHpThreshold, has a BattleConfig.EnemySelfCareChance chance to pick
    /// a self-support move instead of attacking; otherwise prefers Damage, falling back to Debuff,
    /// then SelfSupport if that's genuinely all that's equipped.
    ///
    /// skillDatabase == null, or nothing on the attacker resolves, returns (null, Damage) — callers
    /// fall back to today's hardcoded basic-attack behavior, a fully backward-compatible no-op path.
    /// </summary>
    public static SkillData ChooseSkill(BattleParticipant attacker, SkillDatabase skillDatabase, out EnemyMoveIntent intent)
    {
        intent = EnemyMoveIntent.Damage;
        if (skillDatabase == null) return null;

        var damageOptions = new List<SkillData>();
        var selfSupportOptions = new List<SkillData>();
        var debuffOptions = new List<SkillData>();

        foreach (string guid in attacker.RuntimeData.equippedSkillGuids)
        {
            if (!skillDatabase.TryGetByGuid(guid, out SkillData skill) || skill == null) continue;
            if (skill.BuiltInMove == BuiltInMoveType.Capture) continue;
            // Move (2026-08-12, formation grid system) is also hard-excluded, same as Capture —
            // no AI logic exists yet for deciding when an enemy should reposition. Would otherwise
            // fall through this switch unhandled anyway (no case for it below), but excluding it
            // explicitly here documents that as deliberate, not an oversight.
            if (skill.BuiltInMove == BuiltInMoveType.Move) continue;

            switch (skill.BuiltInMove)
            {
                case BuiltInMoveType.Attack:
                    damageOptions.Add(skill);
                    break;

                case BuiltInMoveType.Charge:
                case BuiltInMoveType.Heal:
                case BuiltInMoveType.Regen:
                    selfSupportOptions.Add(skill);
                    break;

                case BuiltInMoveType.None:
                    // Beat Sequence skills (Attack_Pattern_Directive — Slash, the Group 1 ranged
                    // archetypes) and stacking-rhythm skills (Metronome/Jitter, 2026-08-13) both have
                    // their own dedicated resolution paths (BattleManager.ResolveMeleeBeatSequence /
                    // ResolveStackingRhythmAttack via ResolveEnemyDamageAction), never
                    // PlaceholderSkillResolver — running them through the tree-PrimaryAttribute
                    // damage/status heuristic below is a category error (2026-08-12 bug: retagging
                    // Slash to SkillTreeType.Testing, whose PrimaryAttribute is the inert "N/A"
                    // placeholder value, made IsDamageSkill(Testing) false, so Slash silently
                    // reclassified as a Debuff move — routing into ResolveEnemyDebuffAction instead
                    // of ResolveEnemyDamageAction and skipping the Beat Sequence entirely, no
                    // Approach/Windup/Attack/Return at all). Every one of these skills deals damage,
                    // so bucket directly rather than deriving it from tree metadata that was never
                    // meant to answer this for a skill with its own resolution path.
                    //
                    // 2026-08-20 follow-up: ZonePositionalPattern hit this EXACT same bug on first
                    // playtest — the three new Zone/Positional skills (also SkillTreeType.Testing)
                    // silently fell through to the PlaceholderSkillResolver branch below, got
                    // misclassified as Debuff, and never reached BattleManager.
                    // ResolveZonePositionalAttack at all. Added here alongside the others.
                    if (skill.StackingRhythm != StackingRhythmType.None
                        || (skill.BeatSequence != null && skill.BeatSequence.Count > 0)
                        || (skill.VolleyRingSequence != null && skill.VolleyRingSequence.Count > 0)
                        || skill.HoldInputArchetype != HoldInputArchetype.None
                        || skill.ZonePositionalPattern != ZonePositionalPatternType.None)
                    {
                        damageOptions.Add(skill);
                        break;
                    }

                    PlaceholderSkillResolver.SkillResolution resolution = PlaceholderSkillResolver.Resolve(skill);
                    if (resolution.DealsDamage) damageOptions.Add(skill);
                    else if (resolution.SelfTargeted) selfSupportOptions.Add(skill);
                    else debuffOptions.Add(skill);
                    break;
            }
        }

        bool lowHp = attacker.CurrentHP / (float)attacker.MaxHP < BattleConfig.EnemySelfCareHpThreshold;
        if (lowHp && selfSupportOptions.Count > 0 && Random.value < BattleConfig.EnemySelfCareChance)
        {
            intent = EnemyMoveIntent.SelfSupport;
            return selfSupportOptions[Random.Range(0, selfSupportOptions.Count)];
        }

        if (damageOptions.Count > 0)
        {
            intent = EnemyMoveIntent.Damage;
            return damageOptions[Random.Range(0, damageOptions.Count)];
        }

        if (debuffOptions.Count > 0)
        {
            intent = EnemyMoveIntent.Debuff;
            return debuffOptions[Random.Range(0, debuffOptions.Count)];
        }

        if (selfSupportOptions.Count > 0)
        {
            intent = EnemyMoveIntent.SelfSupport;
            return selfSupportOptions[Random.Range(0, selfSupportOptions.Count)];
        }

        intent = EnemyMoveIntent.Damage;
        return null;
    }

    /// <summary>
    /// Zone/Positional offense-direction follow-up (2026-08-21, Attack_Pattern_Directive Group 3
    /// item 7) — the enemy-side equivalent of a human player's real-time arrow-key dodge during
    /// BattleHUDController.RunZonePositionalWarning's response window. Pure/deterministic given a
    /// Random roll; no MonoBehaviour, EditMode-testable like the rest of this class.
    ///
    /// Rooted (StatusEffectType.Root) defenders never attempt to move — checked here rather than by
    /// the caller so this method is a single, honest "can/did this defender dodge" answer. Dodge
    /// chance reuses TimedInputConfig.ComputeWindowPercent's Instinct/bond-scaling curve (the SAME
    /// formula Dodge/Parry's own tolerance already uses — "higher Instinct = larger window",
    /// CLAUDE.md), read directly as a percent chance rather than a ring-ratio tolerance, then scaled
    /// by the encounter's EnemyDifficultyTier. AlwaysDodges skips the roll entirely — a hard 100%,
    /// giving the "some enemies always dodge" case directly rather than relying on a multiplier
    /// large enough to round to it.
    ///
    /// On a successful roll, checks the 4 orthogonal single-step candidates (fixed order: lane+1,
    /// lane-1, position+1, position-1) via LaneMovementSystem.ClampLane/ClampPosition +
    /// FormationSystem.IsSlotOccupied (same occupancy rule BattleHUDController.
    /// TryStepZonePositionalTarget already enforces for the human path), taking the first that both
    /// isn't occupied and doesn't land on a REAL marked cell (ZoneCell.IsReal — Split Attention's
    /// fake cells are safe to step onto, matching the player-side rule). No candidate found (or the
    /// roll failed, or rooted) returns false and the defender doesn't move.
    /// </summary>
    public static bool TryChooseDodgeStep(BattleParticipant defender, IReadOnlyList<ZoneCell> markedCells,
        List<BattleParticipant> defendingSide, EnemyDifficultyTier tier, out int laneDelta, out int positionDelta)
    {
        laneDelta = 0;
        positionDelta = 0;

        if (defender == null || !defender.IsAlive) return false;
        if (defender.HasStatus(StatusEffectType.Root)) return false;

        if (tier != EnemyDifficultyTier.AlwaysDodges)
        {
            float windowPercent = TimedInputConfig.ComputeWindowPercent(BattleConfig.ZoneDodgeBaseWindowPercent,
                defender.RuntimeData.EffectiveStat(StatType.Instinct), defender.RuntimeData.bondPercent);
            float multiplier = tier switch
            {
                EnemyDifficultyTier.Weak => BattleConfig.ZoneDodgeDifficultyMultiplierWeak,
                EnemyDifficultyTier.Elite => BattleConfig.ZoneDodgeDifficultyMultiplierElite,
                _ => BattleConfig.ZoneDodgeDifficultyMultiplierStandard
            };
            float chance = Mathf.Clamp01((windowPercent / 100f) * multiplier);
            if (Random.value >= chance) return false;
        }

        var others = new List<(int, int)>();
        if (defendingSide != null)
        {
            foreach (BattleParticipant p in defendingSide)
            {
                if (p == defender || !p.IsAlive) continue;
                others.Add((p.LaneIndex, p.PositionIndex));
            }
        }

        (int lane, int position)[] candidates = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        foreach ((int laneStep, int positionStep) in candidates)
        {
            int newLane = LaneMovementSystem.ClampLane(defender.LaneIndex + laneStep);
            int newPosition = LaneMovementSystem.ClampPosition(defender.PositionIndex + positionStep);
            if (newLane == defender.LaneIndex && newPosition == defender.PositionIndex) continue; // clamped to same spot — not a real step
            if (FormationSystem.IsSlotOccupied(others, newLane, newPosition)) continue;
            if (IsCellMarkedReal(markedCells, newLane, newPosition)) continue; // still dangerous, keep looking

            laneDelta = laneStep;
            positionDelta = positionStep;
            return true;
        }

        return false;
    }

    private static bool IsCellMarkedReal(IReadOnlyList<ZoneCell> markedCells, int lane, int position)
    {
        if (markedCells == null) return false;
        foreach (ZoneCell cell in markedCells)
        {
            if (cell.IsReal && cell.Lane == lane && cell.Position == position) return true;
        }
        return false;
    }
}
