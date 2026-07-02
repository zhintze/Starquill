using NUnit.Framework;
using Starquill.Data;
using Starquill.Exploration;
using UnityEngine;

namespace Starquill.Tests.EditMode.Exploration
{
    public class OfflineEarningsCalculatorTests
    {
        private EconomyConfig config;

        [SetUp]
        public void SetUp() => config = ScriptableObject.CreateInstance<EconomyConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        [Test]
        public void Gold_UnderMinimum_Zero()
        {
            Assert.AreEqual(0, OfflineEarningsCalculator.Gold(59f, 5, config), 0.001);
        }

        [Test]
        public void Gold_AppliesOfflineEfficiency()
        {
            float hour = 3600f;
            double offline = OfflineEarningsCalculator.Gold(hour, 5, config);
            double activeRate = config.GoldPerKill(5) * config.exploreKillsPerMinute / 60f * hour;
            Assert.AreEqual(activeRate * config.offlineEfficiency, offline, activeRate * 0.001);
        }

        [Test]
        public void Gold_CapsAtMaxOfflineSeconds()
        {
            double capped = OfflineEarningsCalculator.Gold(config.maxOfflineSeconds, 5, config);
            double beyond = OfflineEarningsCalculator.Gold(config.maxOfflineSeconds * 3, 5, config);
            Assert.AreEqual(capped, beyond, capped * 0.001);
        }

        [Test]
        public void Gold_ScalesWithQuestLevel()
        {
            Assert.Greater(
                OfflineEarningsCalculator.Gold(3600f, 20, config),
                OfflineEarningsCalculator.Gold(3600f, 1, config));
        }

        [Test]
        public void ChestReward_BaseAndDoubled()
        {
            var (gold, rolls) = OfflineEarningsCalculator.ChestReward(5, config, doubled: false);
            var (gold2, rolls2) = OfflineEarningsCalculator.ChestReward(5, config, doubled: true);
            Assert.AreEqual(config.GoldPerKill(5) * config.chestGoldKillMultiple, gold, 0.001);
            Assert.AreEqual(1, rolls);
            Assert.AreEqual(gold * 2, gold2, 0.001);
            Assert.AreEqual(2, rolls2);
        }
    }
}
