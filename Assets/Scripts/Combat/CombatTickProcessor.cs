using System.Collections.Generic;
using System.Linq;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Combat
{
    public class CombatTickProcessor
    {
        private readonly AdvantageMatrix advantageMatrix;
        private readonly EconomyConfig economyConfig;
        private readonly System.Random rng;

        public CombatTickProcessor(AdvantageMatrix matrix, EconomyConfig config, int? seed = null)
        {
            advantageMatrix = matrix;
            economyConfig = config;
            rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public CombatTickResult ProcessTick(
            List<EnemyState> enemies, Stats[] partyStats, DrawnVerb activatedVerb,
            int questLevel, float prestigeMultiplier = 1f, float boostMultiplier = 1f)
        {
            var result = new CombatTickResult();
            var aliveEnemies = enemies.Where(e => e.IsAlive).ToList();
            if (aliveEnemies.Count == 0) { result.WaveCleared = true; return result; }

            // Auto-attack
            foreach (var stats in partyStats)
            {
                var highestStat = stats.HighestStat();
                float autoAtk = stats.GetStat(highestStat) * economyConfig.autoAttackDPSFraction;
                var target = aliveEnemies[rng.Next(aliveEnemies.Count)];
                target.TakeDamage(autoAtk);
                result.TotalDamageDealt += autoAtk;
            }

            // Verb resolution
            if (activatedVerb != null)
                ResolveVerb(activatedVerb, aliveEnemies, partyStats, result, prestigeMultiplier, boostMultiplier);

            // Tick statuses
            foreach (var enemy in enemies) enemy.TickStatuses();

            // Enemy attacks
            foreach (var enemy in aliveEnemies)
                result.TotalDamageReceived += enemy.GetDamageOutput();

            // Kill check
            foreach (var enemy in enemies.Where(e => !e.IsAlive))
            {
                result.EnemiesKilled++;
                result.GoldEarned += economyConfig.GoldPerKill(questLevel, prestigeMult: prestigeMultiplier, boostMult: boostMultiplier);
            }

            result.WaveCleared = enemies.All(e => !e.IsAlive);
            return result;
        }

        private void ResolveVerb(DrawnVerb drawn, List<EnemyState> aliveEnemies, Stats[] partyStats,
            CombatTickResult result, float prestigeMult, float boostMult)
        {
            var verb = drawn.Verb;
            var charStats = partyStats[drawn.OwnerIndex];
            int charStat = charStats.GetStat(verb.statType);

            List<EnemyState> targets;
            switch (verb.targetMode)
            {
                case TargetMode.AoE:
                    targets = new List<EnemyState>(aliveEnemies);
                    break;
                case TargetMode.Cleave:
                    targets = aliveEnemies.OrderBy(_ => rng.Next()).Take(System.Math.Min(3, aliveEnemies.Count)).ToList();
                    break;
                default:
                    targets = new List<EnemyState> { aliveEnemies[rng.Next(aliveEnemies.Count)] };
                    break;
            }

            foreach (var target in targets)
            {
                var matchup = advantageMatrix.GetMatchup(verb.statType, target.StatType);
                if (matchup.advantage == Advantage.Strong) result.AdvantageHits++;
                else if (matchup.advantage == Advantage.Weak) result.DisadvantageHits++;

                float scalingMod = DamageCalculator.GetStatScalingModifier(charStat);
                float damage = DamageCalculator.Calculate(
                    verb.baseDamage * scalingMod, verb.statScaling, charStat,
                    matchup.damageMultiplier, target.HasStatus(StatusEffectType.Expose),
                    prestigeMultiplier: prestigeMult, boostMultiplier: boostMult);

                for (int i = 0; i < verb.hitCount; i++)
                {
                    float hitDmg = damage / verb.hitCount;
                    target.TakeDamage(hitDmg);
                    result.TotalDamageDealt += hitDmg;
                }

                float procChance = verb.effectProcChance * matchup.statusProcModifier * DamageCalculator.GetStatProcModifier(charStat);
                if (procChance > 0 && rng.NextDouble() <= procChance)
                {
                    target.ApplyStatus(verb.effect, verb.effectDuration);
                    result.StatusProcs++;
                }
            }
        }
    }
}
