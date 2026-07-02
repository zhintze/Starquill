using System;
using Starquill.Core;
using Starquill.Quests;

namespace Starquill.Destinations
{
    /// Timed wave rush generation (design §3). Waves are deterministic per
    /// (spec.Seed, waveIndex) so an unbounded rush needs no wave list.
    public static class DungeonGenerator
    {
        private const float MiniBossHpMult = 4f;
        public const int MiniBossEvery = 5;                // every 5th wave
        private const double ClassThemedChance = 0.6;

        public static Rarity FloorForDifficulty(int d) => d switch
        {
            1 => Rarity.Uncommon,
            2 => Rarity.Rare,
            3 => Rarity.Epic,
            4 => Rarity.Epic,
            _ => Rarity.Legendary   // D5+ (design D3, steep curve)
        };

        public static float LegendaryMultForDifficulty(int d) => d == 4 ? 2f : 1f;

        /// Single source for the difficulty -> rush length formula: Generate,
        /// the key cards (KeyPresenter), and the run HUD all call this.
        public static float DurationSeconds(int difficulty, float durationBase, float durationPerD)
            => durationBase + durationPerD * (Math.Max(1, difficulty) - 1);

        /// Single source for the difficulty -> enemy HP scaling formula.
        public static float EnemyHpMultiplier(int difficulty, float enemyMultPerD)
            => 1f + enemyMultPerD * (Math.Max(1, difficulty) - 1);

        public static DungeonSpec Generate(KeyInstance key, int runCounter, int questLevel,
            float durationBase, float durationPerD, float enemyMultPerD, float parSecondsPerWave)
        {
            int d = Math.Max(1, key.Difficulty);
            float duration = DurationSeconds(d, durationBase, durationPerD);
            return new DungeonSpec
            {
                Key = key,
                Seed = (runCounter * 8887) ^ (questLevel * 31) ^ (d * 397),
                QuestLevel = questLevel,
                DurationSeconds = duration,
                EnemyHpMultiplier = EnemyHpMultiplier(d, enemyMultPerD),
                RarityFloor = FloorForDifficulty(d),
                LegendaryWeightMult = LegendaryMultForDifficulty(d),
                BaseRolls = Math.Min(6, 2 + d),
                ParWaves = (int)Math.Round(duration / Math.Max(1f, parSecondsPerWave))
            };
        }

        public static WaveSpec MakeWave(DungeonSpec spec, int waveIndex)
        {
            var rng = new Random(spec.Seed ^ (waveIndex * 73856093));
            bool miniBoss = (waveIndex + 1) % MiniBossEvery == 0;
            int count = miniBoss ? 1 : rng.Next(2, 5);

            var types = new StatType[count];
            var arch = spec.Key.Archetype;
            var all = (StatType[])Enum.GetValues(typeof(StatType));
            for (int i = 0; i < count; i++)
            {
                if (arch != null && rng.NextDouble() < ClassThemedChance)
                    types[i] = rng.NextDouble() < 0.5 ? arch.StatA : arch.StatB;
                else
                    types[i] = all[rng.Next(all.Length)];
            }

            return new WaveSpec
            {
                EnemyCount = count,
                EnemyTypes = types,
                HpMultiplier = miniBoss ? MiniBossHpMult : 1f,
                IsBossWave = miniBoss
            };
        }
    }
}
