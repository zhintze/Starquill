using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.Tests.Equipment
{
    [TestFixture]
    public class AwakenedAbilityTests
    {
        private AwakenedAbility MakeAbility(int level = 1, int maxLevel = 3,
            float basePotency = 3f, float potencyPerLevel = 2f, float currentXP = 0f)
        {
            return new AwakenedAbility
            {
                AbilityId = "test",
                BoostedStat = StatType.CON,
                BasePotency = basePotency,
                PotencyPerLevel = potencyPerLevel,
                Level = level,
                MaxLevel = maxLevel,
                CurrentXP = currentXP
            };
        }

        [Test]
        public void CurrentPotency_Level1_ReturnsBase()
        {
            var ability = MakeAbility(level: 1, basePotency: 3f);
            Assert.AreEqual(3f, ability.CurrentPotency, 0.01f);
        }

        [Test]
        public void CurrentPotency_Level3_ScalesCorrectly()
        {
            var ability = MakeAbility(level: 3, basePotency: 3f, potencyPerLevel: 2f);
            Assert.AreEqual(7f, ability.CurrentPotency, 0.01f);
        }

        [Test]
        public void XPToNextLevel_Level1_ReturnsBaseThreshold()
        {
            var ability = MakeAbility(level: 1);
            Assert.AreEqual(100f, ability.XPToNextLevel(100f, 1.8f), 0.01f);
        }

        [Test]
        public void XPToNextLevel_Level2_ScalesExponentially()
        {
            var ability = MakeAbility(level: 2);
            Assert.AreEqual(180f, ability.XPToNextLevel(100f, 1.8f), 0.01f);
        }

        [Test]
        public void TryLevelUp_WithEnoughXP_LevelsUp()
        {
            var ability = MakeAbility(level: 1, maxLevel: 3, currentXP: 100f);
            bool result = ability.TryLevelUp(100f, 1.8f);
            Assert.IsTrue(result);
            Assert.AreEqual(2, ability.Level);
            Assert.AreEqual(0f, ability.CurrentXP, 0.01f);
        }

        [Test]
        public void TryLevelUp_AtMaxLevel_ReturnsFalse()
        {
            var ability = MakeAbility(level: 3, maxLevel: 3, currentXP: 999f);
            bool result = ability.TryLevelUp(100f, 1.8f);
            Assert.IsFalse(result);
            Assert.AreEqual(3, ability.Level);
        }

        [Test]
        public void AddXP_AutoLevelsWhenThresholdReached()
        {
            var ability = MakeAbility(level: 1, maxLevel: 5, currentXP: 0f);
            ability.AddXP(300f, 100f, 1.8f);
            Assert.AreEqual(3, ability.Level);
            Assert.AreEqual(20f, ability.CurrentXP, 0.01f);
        }

        [Test]
        public void AddXP_AtMaxLevel_DoesNothing()
        {
            var ability = MakeAbility(level: 3, maxLevel: 3, currentXP: 0f);
            ability.AddXP(999f, 100f, 1.8f);
            Assert.AreEqual(3, ability.Level);
            Assert.AreEqual(0f, ability.CurrentXP, 0.01f);
        }

        [Test]
        public void GoldCostToLevel_ScalesExponentially()
        {
            var ability = MakeAbility(level: 1);
            float cost1 = ability.GoldCostToLevel(100f, 2.0f, 1.0f);
            Assert.AreEqual(100f, cost1, 0.01f);

            ability.Level = 2;
            float cost2 = ability.GoldCostToLevel(100f, 2.0f, 1.0f);
            Assert.AreEqual(200f, cost2, 0.01f);
        }

        [Test]
        public void GoldCostToLevel_AtMaxLevel_ReturnsMaxValue()
        {
            var ability = MakeAbility(level: 3, maxLevel: 3);
            float cost = ability.GoldCostToLevel(100f, 2.0f, 1.0f);
            Assert.AreEqual(float.MaxValue, cost);
        }

        [Test]
        public void GoldCostToLevel_AppliesRarityMultiplier()
        {
            var ability = MakeAbility(level: 1);
            float baseCost = ability.GoldCostToLevel(100f, 2.0f, 1.0f);
            float epicCost = ability.GoldCostToLevel(100f, 2.0f, 2.5f);
            Assert.AreEqual(baseCost * 2.5f, epicCost, 0.01f);
        }

        [Test]
        public void Create_AppliesRarityModifiers()
        {
            var entry = new AbilityEntry
            {
                Id = "test", BoostedStat = StatType.STR,
                BasePotency = 3f, PotencyPerLevel = 2f
            };
            var common = AwakenedAbility.Create(entry, Rarity.Common);
            var legendary = AwakenedAbility.Create(entry, Rarity.Legendary);

            Assert.AreEqual(3f * 0.8f, common.BasePotency, 0.01f);
            Assert.AreEqual(2, common.MaxLevel);
            Assert.AreEqual(3f * 2.0f, legendary.BasePotency, 0.01f);
            Assert.AreEqual(7, legendary.MaxLevel);
        }

        [Test]
        public void MaxLevelForRarity_AllRaritiesHaveValues()
        {
            Assert.AreEqual(2, AwakenedAbility.MaxLevelForRarity(Rarity.Common));
            Assert.AreEqual(3, AwakenedAbility.MaxLevelForRarity(Rarity.Uncommon));
            Assert.AreEqual(4, AwakenedAbility.MaxLevelForRarity(Rarity.Rare));
            Assert.AreEqual(5, AwakenedAbility.MaxLevelForRarity(Rarity.Epic));
            Assert.AreEqual(7, AwakenedAbility.MaxLevelForRarity(Rarity.Legendary));
        }
    }
}
