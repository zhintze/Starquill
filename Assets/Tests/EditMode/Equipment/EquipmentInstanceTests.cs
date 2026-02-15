using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    public class EquipmentInstanceTests
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            var instance = new EquipmentInstance(
                itemType: "tr03",
                itemNum: 5,
                slot: EquipmentSlot.Torso,
                rarity: Rarity.Rare,
                baseColor: Color.red,
                varianceColors: new Dictionary<int, Color> { { 48, Color.blue } },
                statMods: new Data.Stats { STR = 3, CON = 2 },
                rolledAffixes: new List<RolledAffix>(),
                layerCodes: new[] { 48 },
                hiddenLayers: new int[0],
                layerColorVariance: new int[0],
                modular: false,
                handType: null,
                displayName: "Rare Sleeveless Shirt"
            );

            Assert.AreEqual("tr03", instance.ItemType);
            Assert.AreEqual(5, instance.ItemNum);
            Assert.AreEqual(EquipmentSlot.Torso, instance.Slot);
            Assert.AreEqual(Rarity.Rare, instance.Rarity);
            Assert.AreEqual(Color.red, instance.BaseColor);
            Assert.AreEqual("Rare Sleeveless Shirt", instance.DisplayName);
        }

        [Test]
        public void GetTotalStatMods_IncludesBaseAndAffixes()
        {
            var affixes = new List<RolledAffix>
            {
                new RolledAffix("affix_str", StatType.STR, 5f, false),
                new RolledAffix("affix_dex", StatType.DEX, 3f, false)
            };
            var instance = new EquipmentInstance(
                "tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                new Data.Stats { STR = 2 }, affixes,
                new[] { 48 }, new int[0], new int[0],
                false, null, "Test"
            );

            var total = instance.GetTotalStatMods();
            Assert.AreEqual(7, total.STR); // 2 base + 5 affix
            Assert.AreEqual(3, total.DEX); // 0 base + 3 affix
        }
    }
}
