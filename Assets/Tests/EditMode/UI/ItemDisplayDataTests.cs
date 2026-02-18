using NUnit.Framework;
using UnityEngine;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using Starquill.UI;

namespace Starquill.Tests.EditMode.UI
{
    public class ItemDisplayDataTests
    {
        private EquipmentInstance MakeItem(string name, EquipmentSlot slot, Rarity rarity,
            int str = 0, int dex = 0, int intStat = 0)
        {
            // Pick highest value as primary, next as secondary
            StatType primaryStat = StatType.STR;
            int primaryVal = str;
            StatType secondaryStat = StatType.DEX;
            int secondaryVal = dex;

            if (dex > primaryVal) { primaryStat = StatType.DEX; primaryVal = dex; secondaryStat = StatType.STR; secondaryVal = str; }
            if (intStat > primaryVal) { secondaryStat = primaryStat; secondaryVal = primaryVal; primaryStat = StatType.INT; primaryVal = intStat; }
            else if (intStat > secondaryVal) { secondaryStat = StatType.INT; secondaryVal = intStat; }

            return new EquipmentInstance("hd", 1, slot, rarity,
                Color.white, null,
                primaryStat, primaryVal, secondaryStat, secondaryVal, null,
                null, null, null, false, null, name);
        }

        [Test]
        public void FromItem_SetsDisplayName()
        {
            var item = MakeItem("Iron Helm", EquipmentSlot.Head, Rarity.Common, str: 5);
            var data = ItemDisplayData.FromItem(item, 1);
            Assert.AreEqual("Iron Helm", data.DisplayName);
        }

        [Test]
        public void FromItem_SetsSlotName()
        {
            var item = MakeItem("Iron Helm", EquipmentSlot.Head, Rarity.Common, str: 3);
            var data = ItemDisplayData.FromItem(item, 1);
            Assert.AreEqual("Head", data.SlotName);
        }

        [Test]
        public void FromItem_SetsRarityName()
        {
            var item = MakeItem("Blue Sword", EquipmentSlot.MainHand, Rarity.Rare, str: 8);
            var data = ItemDisplayData.FromItem(item, 1);
            Assert.AreEqual("Rare", data.RarityName);
        }

        [Test]
        public void FromItem_SetsRarityColor()
        {
            var item = MakeItem("Green Boots", EquipmentSlot.Feet, Rarity.Uncommon, dex: 4);
            var data = ItemDisplayData.FromItem(item, 1);
            Color expected = new Color(0.3f, 0.8f, 0.3f);
            Assert.AreEqual(expected.r, data.RarityColor.r, 0.001f);
            Assert.AreEqual(expected.g, data.RarityColor.g, 0.001f);
            Assert.AreEqual(expected.b, data.RarityColor.b, 0.001f);
        }

        [Test]
        public void FromItem_CalculatesScore()
        {
            var item = MakeItem("Scored Item", EquipmentSlot.Torso, Rarity.Common, str: 10, dex: 5);
            var data = ItemDisplayData.FromItem(item, 1);
            float expectedScore = ItemComparer.ScoreItem(item);
            Assert.AreEqual(expectedScore, data.Score, 0.001f);
        }

        [Test]
        public void FromItem_CalculatesSellValue()
        {
            var item = MakeItem("Sellable Item", EquipmentSlot.Arms, Rarity.Epic, str: 12);
            int questLevel = 5;
            var data = ItemDisplayData.FromItem(item, questLevel);
            double expectedValue = SellCalculator.GetSellValue(item, questLevel);
            Assert.AreEqual(expectedValue, data.SellValue, 0.01);
        }

        [Test]
        public void FromItem_FormatsStatSummary()
        {
            var item = MakeItem("Multi Stat", EquipmentSlot.Head, Rarity.Common, str: 5, dex: 3);
            var data = ItemDisplayData.FromItem(item, 1);
            Assert.IsTrue(data.StatSummary.Contains("STR"), $"Expected 'STR' in '{data.StatSummary}'");
            Assert.IsTrue(data.StatSummary.Contains("DEX"), $"Expected 'DEX' in '{data.StatSummary}'");
        }

        [Test]
        public void FromItem_NullItem_ReturnsEmpty()
        {
            var data = ItemDisplayData.FromItem(null, 1);
            Assert.AreEqual("", data.DisplayName);
            Assert.AreEqual("", data.SlotName);
            Assert.AreEqual("", data.RarityName);
            Assert.AreEqual("", data.StatSummary);
            Assert.AreEqual(0f, data.Score);
            Assert.AreEqual(0.0, data.SellValue);
        }

        [Test]
        public void SlotNames_AllElevenSlots()
        {
            Assert.AreEqual("Head", ItemDisplayData.GetSlotName(EquipmentSlot.Head));
            Assert.AreEqual("Torso", ItemDisplayData.GetSlotName(EquipmentSlot.Torso));
            Assert.AreEqual("Arms", ItemDisplayData.GetSlotName(EquipmentSlot.Arms));
            Assert.AreEqual("Legs", ItemDisplayData.GetSlotName(EquipmentSlot.Legs));
            Assert.AreEqual("Feet", ItemDisplayData.GetSlotName(EquipmentSlot.Feet));
            Assert.AreEqual("Main Hand", ItemDisplayData.GetSlotName(EquipmentSlot.MainHand));
            Assert.AreEqual("Off Hand", ItemDisplayData.GetSlotName(EquipmentSlot.OffHand));
            Assert.AreEqual("Misc 1", ItemDisplayData.GetSlotName(EquipmentSlot.Misc1));
            Assert.AreEqual("Misc 2", ItemDisplayData.GetSlotName(EquipmentSlot.Misc2));
            Assert.AreEqual("Misc 3", ItemDisplayData.GetSlotName(EquipmentSlot.Misc3));
            Assert.AreEqual("Misc 4", ItemDisplayData.GetSlotName(EquipmentSlot.Misc4));
        }
    }
}
