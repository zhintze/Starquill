using System;
using System.Collections.Generic;
using Starquill.Core;

namespace Starquill.Quests
{
    /// Deterministic quest generation: the same (zoneIndex, questIndex,
    /// questLevel) always yields the same spec, so specs are regenerated on
    /// load instead of serialized.
    public static class QuestGenerator
    {
        private const float MiniBossHpMultiplier = 4f;
        private const float BossHpMultiplier = 8f;
        private const double DominantTypeChance = 0.8;

        /// Design ladder: 1-3 Normal, 4 Elite, 5-7 Normal, 8 Elite,
        /// 9-10 Hard, 11 Boss (0-based indices).
        public static QuestTier TierFor(int questIndex)
        {
            switch (questIndex)
            {
                case 3:
                case 7:
                    return QuestTier.Elite;
                case 8:
                case 9:
                    return QuestTier.Hard;
                case 10:
                    return QuestTier.Boss;
                default:
                    return QuestTier.Normal;
            }
        }

        public static QuestSpec Generate(QuestZone zone, int zoneIndex, int questIndex, int questLevel)
        {
            var rng = new Random((zoneIndex * 397) ^ (questIndex * 31) ^ questLevel);
            var tier = TierFor(questIndex);

            var waves = new List<WaveSpec>();
            switch (tier)
            {
                case QuestTier.Normal:
                {
                    // Quests 5-7 (indices 4-6) run slightly longer per design.
                    bool longer = questIndex >= 4;
                    int count = longer ? rng.Next(4, 7) : rng.Next(3, 6);
                    for (int i = 0; i < count; i++)
                        waves.Add(MakeWave(zone, rng, rng.Next(2, 5), Ramp(i, count), false, false));
                    break;
                }
                case QuestTier.Elite:
                {
                    int count = rng.Next(3, 5);
                    for (int i = 0; i < count; i++)
                        waves.Add(MakeWave(zone, rng, rng.Next(2, 5), Ramp(i, count), false, false));
                    waves.Add(new WaveSpec
                    {
                        EnemyCount = 1,
                        EnemyTypes = new[] { PickType(zone, rng, true) },
                        HpMultiplier = MiniBossHpMultiplier,
                        IsBossWave = true
                    });
                    break;
                }
                case QuestTier.Hard:
                {
                    int count = rng.Next(5, 8);
                    for (int i = 0; i < count; i++)
                        waves.Add(MakeWave(zone, rng, rng.Next(3, 6), Ramp(i, count), true, false));
                    break;
                }
                case QuestTier.Boss:
                {
                    for (int i = 0; i < 2; i++)
                        waves.Add(MakeWave(zone, rng, rng.Next(2, 5), Ramp(i, 2), false, false));

                    var bossTypes = new StatType[3];
                    for (int i = 0; i < bossTypes.Length; i++)
                        bossTypes[i] = PickType(zone, rng, true);
                    waves.Add(new WaveSpec
                    {
                        EnemyCount = 3, // boss + 2 adds
                        EnemyTypes = bossTypes,
                        HpMultiplier = BossHpMultiplier,
                        IsBossWave = true
                    });
                    break;
                }
            }

            string name = $"{zone.Name} {questIndex + 1}/{QuestZone.QuestsPerZone}";
            if (tier == QuestTier.Boss) name += " — Boss";

            return new QuestSpec
            {
                ZoneIndex = zoneIndex,
                QuestIndex = questIndex,
                Tier = tier,
                DisplayName = name,
                Waves = waves.ToArray(),
                Reward = QuestRewardSpec.ForTier(tier)
            };
        }

        private static float Ramp(int waveIndex, int waveCount)
        {
            if (waveCount <= 1) return 1f;
            return 1f + 0.3f * waveIndex / (waveCount - 1);
        }

        private static WaveSpec MakeWave(QuestZone zone, Random rng, int enemyCount,
            float hpMultiplier, bool forceMixed, bool isBoss)
        {
            var types = new StatType[enemyCount];
            for (int i = 0; i < enemyCount; i++)
                types[i] = PickType(zone, rng, false);

            if (forceMixed && enemyCount >= 2)
            {
                // Hard waves always contain at least two distinct stat types.
                bool allSame = true;
                for (int i = 1; i < enemyCount; i++)
                    if (types[i] != types[0]) { allSame = false; break; }

                if (allSame)
                {
                    var all = (StatType[])Enum.GetValues(typeof(StatType));
                    StatType other;
                    do { other = all[rng.Next(all.Length)]; } while (other == types[0]);
                    types[enemyCount - 1] = other;
                }
            }

            return new WaveSpec
            {
                EnemyCount = enemyCount,
                EnemyTypes = types,
                HpMultiplier = hpMultiplier,
                IsBossWave = isBoss
            };
        }

        private static StatType PickType(QuestZone zone, Random rng, bool dominantOnly)
        {
            bool useDominant = dominantOnly ||
                (zone.DominantTypes.Length > 0 && rng.NextDouble() < DominantTypeChance);
            if (useDominant && zone.DominantTypes.Length > 0)
                return zone.DominantTypes[rng.Next(zone.DominantTypes.Length)];

            var all = (StatType[])Enum.GetValues(typeof(StatType));
            return all[rng.Next(all.Length)];
        }
    }
}
