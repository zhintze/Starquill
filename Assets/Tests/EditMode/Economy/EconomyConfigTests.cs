using NUnit.Framework;
using Starquill.Data;
using UnityEngine;

namespace Starquill.Tests.Economy
{
    public class EconomyConfigTests
    {
        private EconomyConfig config;

        [SetUp]
        public void SetUp() { config = ScriptableObject.CreateInstance<EconomyConfig>(); }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(config); }

        [Test]
        public void EnemyHP_Level1_Is50()
        {
            Assert.AreEqual(50f, config.EnemyHP(0), 1f);
        }

        [Test]
        public void EnemyHP_Level10_Scales()
        {
            float hp = config.EnemyHP(10);
            Assert.AreEqual(155f, hp, 5f);
        }

        [Test]
        public void EnemyHP_Level100_BigNumbers()
        {
            float hp = config.EnemyHP(100);
            Assert.Greater(hp, 1_000_000f);
        }

        [Test]
        public void GoldPerKill_Level1_Is5()
        {
            Assert.AreEqual(5f, config.GoldPerKill(0), 0.1f);
        }

        [Test]
        public void GoldPerKill_WithChaBonus()
        {
            float base_ = config.GoldPerKill(10);
            float boosted = config.GoldPerKill(10, chaBonus: 0.2f);
            Assert.AreEqual(base_ * 1.2f, boosted, 0.1f);
        }

        [Test]
        public void UpgradeCost_GrowsFasterThanGold()
        {
            float gold50 = config.GoldPerKill(50);
            float cost50 = config.UpgradeCost(50);
            float gold10 = config.GoldPerKill(10);
            float cost10 = config.UpgradeCost(10);
            float goldRatio = gold50 / gold10;
            float costRatio = cost50 / cost10;
            Assert.Greater(costRatio, goldRatio);
        }

        [Test]
        public void OfflineGold_CapsAt8Hours()
        {
            float uncapped = config.OfflineGold(100f, 100000f);
            float capped = config.OfflineGold(100f, 28800f);
            Assert.AreEqual(capped, uncapped, 0.1f);
        }

        [Test]
        public void OfflineGold_50PercentEfficiency()
        {
            float offline = config.OfflineGold(100f, 3600f);
            float expected = 100f * 3600f * 0.5f;
            Assert.AreEqual(expected, offline, 0.1f);
        }

        [Test]
        public void BaseDropRate_DefaultValue_Is015()
        {
            Assert.AreEqual(0.15f, config.baseDropRate, 0.001f);
        }
    }
}
