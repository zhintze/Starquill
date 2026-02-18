using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    [TestFixture]
    public class SellCalculatorTests
    {
        private EquipmentInstance MakeItem(Rarity rarity, int abilityLevel = 0)
        {
            AwakenedAbility ability = null;
            if (abilityLevel > 0)
            {
                ability = new AwakenedAbility
                {
                    AbilityId = "test",
                    BoostedStat = StatType.STR,
                    BasePotency = 3f,
                    PotencyPerLevel = 2f,
                    Level = abilityLevel,
                    MaxLevel = 7,
                    CurrentXP = 0f
                };
            }

            return new EquipmentInstance(
                "hd01", 1, EquipmentSlot.Head, rarity,
                Color.white, new Dictionary<int, Color>(),
                StatType.STR, 5, StatType.DEX, 2, ability,
                new int[0], new int[0], new int[0],
                false, null, "Test");
        }

        [Test]
        public void Common_QuestLevel1_Returns10()
        {
            var value = SellCalculator.GetSellValue(MakeItem(Rarity.Common), 1);
            Assert.AreEqual(10.0, value, 0.01);
        }

        [Test]
        public void Legendary_QuestLevel1_Returns500()
        {
            var value = SellCalculator.GetSellValue(MakeItem(Rarity.Legendary), 1);
            Assert.AreEqual(500.0, value, 0.01);
        }

        [Test]
        public void QuestLevel_MultipliesValue()
        {
            var value = SellCalculator.GetSellValue(MakeItem(Rarity.Common), 10);
            Assert.AreEqual(100.0, value, 0.01);
        }

        [Test]
        public void AbilityLevel_MultipliesValue()
        {
            var noAbility = SellCalculator.GetSellValue(MakeItem(Rarity.Rare, 0), 1);
            var level2 = SellCalculator.GetSellValue(MakeItem(Rarity.Rare, 2), 1);
            // level2: 75 * 1 * (1 + 2*0.15) = 75 * 1.3 = 97.5
            Assert.AreEqual(75.0, noAbility, 0.01);
            Assert.AreEqual(97.5, level2, 0.01);
        }

        [Test]
        public void AllRarities_HaveDistinctBaseValues()
        {
            int ql = 1;
            var c = SellCalculator.GetSellValue(MakeItem(Rarity.Common), ql);
            var u = SellCalculator.GetSellValue(MakeItem(Rarity.Uncommon), ql);
            var r = SellCalculator.GetSellValue(MakeItem(Rarity.Rare), ql);
            var e = SellCalculator.GetSellValue(MakeItem(Rarity.Epic), ql);
            var l = SellCalculator.GetSellValue(MakeItem(Rarity.Legendary), ql);
            Assert.IsTrue(c < u && u < r && r < e && e < l);
        }
    }
}
