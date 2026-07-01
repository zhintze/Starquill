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
        private AbilityTable table;
        private EconomyConfig economy;

        [SetUp]
        public void SetUp()
        {
            table = new AbilityTable();
            var asset = Resources.Load<TextAsset>("Data/abilities");
            Assert.IsNotNull(asset, "abilities.json not found in Resources");
            table.LoadFromJson(asset.text);

            economy = ScriptableObject.CreateInstance<EconomyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(economy);
        }

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

        private EquipmentInstance MakeItemWithAbility(AwakenedAbility ability)
        {
            return new EquipmentInstance("tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                StatType.CON, 8, StatType.STR, 3, ability,
                new[] { 48 }, null, null, false, null, "Rare Chain Shirt");
        }

        private AwakenedAbility MakeAbility(int level = 1, int maxLevel = 3, float currentXP = 0f)
        {
            return new AwakenedAbility
            {
                AbilityId = "ironhide",
                BoostedStat = StatType.CON,
                BasePotency = 3f,
                PotencyPerLevel = 2f,
                Level = level,
                MaxLevel = maxLevel,
                CurrentXP = currentXP
            };
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
        public void FromItem_PopulatesStatPairFromItemFields()
        {
            var item = MakeItem("Multi Stat", EquipmentSlot.Head, Rarity.Common, str: 5, dex: 3);
            var data = ItemDisplayData.FromItem(item, 1);
            Assert.AreEqual(StatType.STR, data.PrimaryStat);
            Assert.AreEqual(5, data.PrimaryValue);
            Assert.AreEqual(StatType.DEX, data.SecondaryStat);
            Assert.AreEqual(3, data.SecondaryValue);
        }

        [Test]
        public void FromItem_StatPairExcludesAbilityPotency()
        {
            // Ability boosts CON; PrimaryValue must stay the raw pair value (no double count)
            var data = ItemDisplayData.FromItem(MakeItemWithAbility(MakeAbility()), 1, table, economy);
            Assert.AreEqual(8, data.PrimaryValue);
            Assert.AreEqual(3, data.SecondaryValue);
        }

        [Test]
        public void FromItem_ResolvesAbilityNameAndDescription()
        {
            var data = ItemDisplayData.FromItem(MakeItemWithAbility(MakeAbility()), 1, table, economy);
            Assert.IsTrue(data.HasAbility);
            Assert.AreEqual("Ironhide", data.AbilityName);
            Assert.AreEqual("+3 CON while equipped", data.AbilityDescription);
            Assert.AreEqual(StatType.CON, data.AbilityStat);
            Assert.AreEqual(3f, data.AbilityPotency, 0.01f);
            Assert.AreEqual(1, data.AbilityLevel);
            Assert.AreEqual(3, data.AbilityMaxLevel);
        }

        [Test]
        public void FromItem_NullTable_FallsBackToRawId()
        {
            var data = ItemDisplayData.FromItem(MakeItemWithAbility(MakeAbility()), 1, null, economy);
            Assert.AreEqual("ironhide", data.AbilityName);
            Assert.AreEqual("", data.AbilityDescription);
        }

        [Test]
        public void FromItem_NoAbility_HasAbilityFalse()
        {
            var item = MakeItem("Plain Helm", EquipmentSlot.Head, Rarity.Common, str: 3);
            var data = ItemDisplayData.FromItem(item, 1, table, economy);
            Assert.IsFalse(data.HasAbility);
        }

        [Test]
        public void FromItem_XpFraction_ZeroXp()
        {
            var data = ItemDisplayData.FromItem(MakeItemWithAbility(MakeAbility(currentXP: 0f)), 1, table, economy);
            Assert.AreEqual(0f, data.AbilityXpFraction, 0.001f);
        }

        [Test]
        public void FromItem_XpFraction_HalfThreshold()
        {
            // Level 1 threshold = abilityBaseXPThreshold (100) * growth^0 = 100
            var data = ItemDisplayData.FromItem(MakeItemWithAbility(MakeAbility(currentXP: 50f)), 1, table, economy);
            Assert.AreEqual(0.5f, data.AbilityXpFraction, 0.001f);
        }

        [Test]
        public void FromItem_XpFraction_MaxLevelIsOne()
        {
            var data = ItemDisplayData.FromItem(MakeItemWithAbility(MakeAbility(level: 3, maxLevel: 3)), 1, table, economy);
            Assert.AreEqual(1f, data.AbilityXpFraction, 0.001f);
        }

        [Test]
        public void FromItem_NullEconomy_XpFractionZero()
        {
            var data = ItemDisplayData.FromItem(MakeItemWithAbility(MakeAbility(currentXP: 50f)), 1, table, null);
            Assert.AreEqual(0f, data.AbilityXpFraction, 0.001f);
        }

        [Test]
        public void StatLabel_FormatsSignedValue()
        {
            Assert.AreEqual("CON +7", ItemDisplayData.StatLabel(StatType.CON, 7));
        }

        [Test]
        public void FromItem_NullItem_ReturnsEmpty()
        {
            var data = ItemDisplayData.FromItem(null, 1);
            Assert.AreEqual("", data.DisplayName);
            Assert.AreEqual("", data.SlotName);
            Assert.AreEqual("", data.RarityName);
            Assert.IsFalse(data.HasAbility);
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
