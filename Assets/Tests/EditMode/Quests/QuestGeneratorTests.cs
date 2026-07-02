using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Quests;

namespace Starquill.Tests.EditMode.Quests
{
    public class QuestGeneratorTests
    {
        private QuestZone zone;

        [SetUp]
        public void SetUp()
        {
            zone = new QuestZone
            {
                Id = "test-zone",
                Name = "Test Zone",
                DominantTypes = new[] { StatType.STR, StatType.CON }
            };
        }

        [Test]
        public void Generate_IsDeterministic()
        {
            var a = QuestGenerator.Generate(zone, 0, 5, 10);
            var b = QuestGenerator.Generate(zone, 0, 5, 10);

            Assert.AreEqual(a.Tier, b.Tier);
            Assert.AreEqual(a.Waves.Length, b.Waves.Length);
            for (int i = 0; i < a.Waves.Length; i++)
            {
                Assert.AreEqual(a.Waves[i].EnemyCount, b.Waves[i].EnemyCount, $"wave {i} count");
                Assert.AreEqual(a.Waves[i].HpMultiplier, b.Waves[i].HpMultiplier, 0.0001f, $"wave {i} hp");
                CollectionAssert.AreEqual(a.Waves[i].EnemyTypes, b.Waves[i].EnemyTypes, $"wave {i} types");
            }
        }

        [Test]
        public void TierFor_MatchesLadder()
        {
            Assert.AreEqual(QuestTier.Normal, QuestGenerator.TierFor(0));
            Assert.AreEqual(QuestTier.Normal, QuestGenerator.TierFor(2));
            Assert.AreEqual(QuestTier.Elite, QuestGenerator.TierFor(3));
            Assert.AreEqual(QuestTier.Normal, QuestGenerator.TierFor(4));
            Assert.AreEqual(QuestTier.Normal, QuestGenerator.TierFor(6));
            Assert.AreEqual(QuestTier.Elite, QuestGenerator.TierFor(7));
            Assert.AreEqual(QuestTier.Hard, QuestGenerator.TierFor(8));
            Assert.AreEqual(QuestTier.Hard, QuestGenerator.TierFor(9));
            Assert.AreEqual(QuestTier.Boss, QuestGenerator.TierFor(10));
        }

        [Test]
        public void Generate_WaveCountsWithinTierRanges()
        {
            for (int level = 1; level <= 50; level++)
            {
                foreach (int idx in new[] { 0, 3, 8, 10 })
                {
                    var spec = QuestGenerator.Generate(zone, 0, idx, level);
                    int n = spec.Waves.Length;
                    switch (spec.Tier)
                    {
                        case QuestTier.Normal: Assert.That(n, Is.InRange(3, 6), $"Normal idx{idx} lvl{level}"); break;
                        case QuestTier.Elite: Assert.That(n, Is.InRange(4, 5), $"Elite lvl{level}"); break;
                        case QuestTier.Hard: Assert.That(n, Is.InRange(5, 7), $"Hard lvl{level}"); break;
                        case QuestTier.Boss: Assert.AreEqual(3, n, $"Boss lvl{level}"); break;
                    }
                }
            }
        }

        [Test]
        public void Generate_HpRampIsMonotonic()
        {
            var spec = QuestGenerator.Generate(zone, 0, 5, 12);
            for (int i = 1; i < spec.Waves.Length; i++)
            {
                if (spec.Waves[i].IsBossWave) continue;
                Assert.GreaterOrEqual(spec.Waves[i].HpMultiplier, spec.Waves[i - 1].HpMultiplier);
            }
        }

        [Test]
        public void Generate_BossQuest_FinalWaveIsBoss()
        {
            var spec = QuestGenerator.Generate(zone, 0, 10, 15);
            var final = spec.Waves[^1];
            Assert.IsTrue(final.IsBossWave);
            Assert.AreEqual(3, final.EnemyCount); // boss + 2 adds
            Assert.GreaterOrEqual(final.HpMultiplier, 8f);
            for (int i = 0; i < spec.Waves.Length - 1; i++)
                Assert.IsFalse(spec.Waves[i].IsBossWave);
        }

        [Test]
        public void Generate_EliteQuest_FinalWaveIsMiniBoss()
        {
            var spec = QuestGenerator.Generate(zone, 0, 3, 15);
            var final = spec.Waves[^1];
            Assert.IsTrue(final.IsBossWave);
            Assert.AreEqual(1, final.EnemyCount);
            Assert.GreaterOrEqual(final.HpMultiplier, 4f);
        }

        [Test]
        public void Generate_HardWaves_HaveMixedTypes()
        {
            for (int level = 1; level <= 20; level++)
            {
                var spec = QuestGenerator.Generate(zone, 0, 9, level);
                foreach (var wave in spec.Waves)
                {
                    var distinct = wave.EnemyTypes.Distinct().Count();
                    Assert.GreaterOrEqual(distinct, 2, $"Hard wave lvl{level}");
                }
            }
        }

        [Test]
        public void Generate_EnemyTypesFavorZoneDominants()
        {
            int dominant = 0, total = 0;
            for (int level = 1; level <= 30; level++)
            {
                var spec = QuestGenerator.Generate(zone, 0, level % 3, level);
                foreach (var wave in spec.Waves)
                {
                    foreach (var t in wave.EnemyTypes)
                    {
                        total++;
                        if (t == StatType.STR || t == StatType.CON) dominant++;
                    }
                }
            }
            Assert.Greater(total, 100, "sample size");
            Assert.Greater((float)dominant / total, 0.6f,
                $"dominant share {dominant}/{total}");
        }

        [Test]
        public void Generate_WaveTypeArraysMatchCounts()
        {
            foreach (int idx in new[] { 0, 3, 8, 10 })
            {
                var spec = QuestGenerator.Generate(zone, 1, idx, 7);
                foreach (var wave in spec.Waves)
                    Assert.AreEqual(wave.EnemyCount, wave.EnemyTypes.Length);
            }
        }

        [Test]
        public void Generate_SetsMetadata()
        {
            var spec = QuestGenerator.Generate(zone, 2, 10, 9);
            Assert.AreEqual(2, spec.ZoneIndex);
            Assert.AreEqual(10, spec.QuestIndex);
            Assert.IsTrue(spec.DisplayName.Contains("Test Zone"));
            Assert.IsTrue(spec.DisplayName.Contains("Boss"));
            Assert.IsNotNull(spec.Reward);
            Assert.AreEqual(2, spec.Reward.LootRolls);
        }
    }
}
