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
        private EquipmentInstance MakeItem(Rarity rarity, int affixCount = 0)
        {
            var affixes = new List<RolledAffix>();
            for (int i = 0; i < affixCount; i++)
                affixes.Add(new RolledAffix("test", StatType.STR, 5f, false));

            return new EquipmentInstance(
                "hd01", 1, EquipmentSlot.Head, rarity,
                Color.white, new Dictionary<int, Color>(),
                new Stats(), affixes,
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
        public void Affixes_Add10PercentEach()
        {
            var noAffix = SellCalculator.GetSellValue(MakeItem(Rarity.Rare, 0), 1);
            var twoAffix = SellCalculator.GetSellValue(MakeItem(Rarity.Rare, 2), 1);
            Assert.AreEqual(noAffix * 1.2, twoAffix, 0.01);
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
