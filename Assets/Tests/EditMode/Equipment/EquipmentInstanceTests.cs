using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    [TestFixture]
    public class EquipmentInstanceTests
    {
        private AwakenedAbility MakeAbility(StatType stat = StatType.CON,
            float basePotency = 3f, int level = 1)
        {
            return new AwakenedAbility
            {
                AbilityId = "test",
                BoostedStat = stat,
                BasePotency = basePotency,
                PotencyPerLevel = 2f,
                Level = level,
                MaxLevel = 3,
                CurrentXP = 0f
            };
        }

        [Test]
        public void Constructor_SetsAllFields()
        {
            var ability = MakeAbility();
            var instance = new EquipmentInstance(
                itemType: "tr03", itemNum: 5,
                slot: EquipmentSlot.Torso, rarity: Rarity.Rare,
                baseColor: Color.red,
                varianceColors: new Dictionary<int, Color> { { 48, Color.blue } },
                primaryStat: StatType.CON, primaryValue: 8,
                secondaryStat: StatType.STR, secondaryValue: 3,
                ability: ability,
                layerCodes: new[] { 48 },
                hiddenLayers: new int[0],
                layerColorVariance: new int[0],
                modular: false, handType: null,
                displayName: "Rare Sleeveless Shirt"
            );

            Assert.AreEqual("tr03", instance.ItemType);
            Assert.AreEqual(5, instance.ItemNum);
            Assert.AreEqual(EquipmentSlot.Torso, instance.Slot);
            Assert.AreEqual(Rarity.Rare, instance.Rarity);
            Assert.AreEqual(StatType.CON, instance.PrimaryStat);
            Assert.AreEqual(8, instance.PrimaryValue);
            Assert.AreEqual(StatType.STR, instance.SecondaryStat);
            Assert.AreEqual(3, instance.SecondaryValue);
            Assert.AreEqual("Rare Sleeveless Shirt", instance.DisplayName);
        }

        [Test]
        public void GetTotalStatMods_IncludesStatPair()
        {
            var instance = new EquipmentInstance(
                "tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                StatType.CON, 8, StatType.STR, 3, null,
                new[] { 48 }, new int[0], new int[0],
                false, null, "Test"
            );
            var total = instance.GetTotalStatMods();
            Assert.AreEqual(8, total.CON);
            Assert.AreEqual(3, total.STR);
        }

        [Test]
        public void GetTotalStatMods_IncludesAbilityPotency()
        {
            var ability = MakeAbility(StatType.CON, basePotency: 3f, level: 1);
            var instance = new EquipmentInstance(
                "tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                StatType.CON, 8, StatType.STR, 3, ability,
                new[] { 48 }, new int[0], new int[0],
                false, null, "Test"
            );
            var total = instance.GetTotalStatMods();
            Assert.AreEqual(11, total.CON); // 8 + 3 from ability
            Assert.AreEqual(3, total.STR);
        }

        [Test]
        public void GetTotalStatMods_AbilityOnDifferentStat()
        {
            var ability = MakeAbility(StatType.DEX, basePotency: 5f, level: 1);
            var instance = new EquipmentInstance(
                "tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                StatType.CON, 8, StatType.STR, 3, ability,
                new[] { 48 }, new int[0], new int[0],
                false, null, "Test"
            );
            var total = instance.GetTotalStatMods();
            Assert.AreEqual(8, total.CON);
            Assert.AreEqual(3, total.STR);
            Assert.AreEqual(5, total.DEX);
        }

        [Test]
        public void GetTotalStatMods_AbilityLevelScalesPotency()
        {
            var ability = MakeAbility(StatType.CON, basePotency: 3f, level: 3);
            var instance = new EquipmentInstance(
                "tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                StatType.CON, 8, StatType.STR, 3, ability,
                new[] { 48 }, new int[0], new int[0],
                false, null, "Test"
            );
            var total = instance.GetTotalStatMods();
            Assert.AreEqual(15, total.CON); // 8 + 7 (3 + (3-1)*2)
        }
    }
}
