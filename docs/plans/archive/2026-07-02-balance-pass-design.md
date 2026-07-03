# Balance Pass — Design & Implementation Checklist

**Date:** 2026-07-02 · Source: `docs/balance-analysis.md`, PM rulings: Training is **per-character with catch-up discount**; first prestige target **~2 weeks casual**; XP levels get a **felt multiplier** (curve unchanged).

## Changes

### R1' Training ladder (per-character, catch-up)
- `CharacterInstance.trainingLevel` (+ SerializedCharacter round-trip)
- Cost: `EconomyConfig.UpgradeCost(trainingLevel)` — the orphaned `10 × 1.15^level` formula, finally consumed. **Catch-up:** cost × `trainingCatchUpDiscount (0.5)` when the character is below the roster's highest training level.
- Effect: per-character damage multiplier `(1 + trainingDamagePerLevel)^trainingLevel`, knob `trainingDamagePerLevel = 0.09` (per-character runs hotter than a party-wide ×1.06 because four ladders split one income).
- `GameManager.TrainCharacter(rosterIndex)` / `TrainingCost(character)`; UI: Train button + level readout at the bottom of the party screen's stat column.

### R6' Level multiplier
- Character damage also × `(1 + charLevelDamageBonus)^level`, knob `charLevelDamageBonus = 0.015`. Every level is felt at any depth. (Verb-slot milestones deferred until verb acquisition exists.)

### Damage plumbing
- `CombatTickProcessor.ProcessTick` gains `float[] memberDamageMultipliers` (auto-attack and verb damage both scale by owner's multiplier). GameManager builds the array from party members each tick.

### R2 Gear budgets scale with quest level
- `GenerateStatPair(prefix, rarity, rng, questLevel = 1)`: budget min/max × `(1 + questLevel × gearBudgetPerLevel)`, knob `gearBudgetPerLevel = 0.03`. All factory call sites pass questLevel (starter loadout stays 1).

### R3 CHA gold identity wired
- `CombatTickProcessor`: `chaBonus = Σ party CHA × 0.02` passed to `GoldPerKill` (matches `Party.GetGoldBonus`).

### R4 Boost repricing (minutes-of-income)
- `BoostCost = GoldPerKill(Q) × exploreKillsPerMinute × minutes`; knobs `boostAutoFireIncomeMinutes = 5`, `boostSpeedUpIncomeMinutes = 3`. Replaces the linear `500/300 × Q`.

### R5 ScoreItem includes ability
- `ScoreItem = PrimaryValue + SecondaryValue + Ability.CurrentPotency`.

## Test updates
- New: training cost/catch-up/multiplier math; gear budget scaling; boost income pricing.
- Updated: GenerateStatPair budget-range asserts (questLevel-aware), any score asserts.

## Retune verification
`tools/balance_sim.py` updated to model training + scaled gear; targets: Q20 within the first ~45 min, soft friction from ~Q30, pre-prestige wall ~Q60-70 at ~14-20 played hours (2 weeks casual + offline).
