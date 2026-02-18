using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Characters
{
    [TestFixture]
    public class AutoEquipperTests
    {
        private EquipmentInstance MakeItem(EquipmentSlot slot, int str = 0,
            Rarity rarity = Rarity.Common)
        {
            return new EquipmentInstance(
                "hd01", 1, slot, rarity,
                Color.white, new Dictionary<int, Color>(),
                StatType.STR, str, StatType.DEX, 0, null,
                new int[0], new int[0], new int[0],
                false, null, "Test");
        }

        [Test]
        public void AutoEquip_EquipsUpgradeFromInventory()
        {
            var character = new CharacterInstance
            {
                baseStats = new Stats { STR = 10, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
            character.EquipItem(MakeItem(EquipmentSlot.Head, str: 3));

            var inventory = new LootInventory(50);
            var better = MakeItem(EquipmentSlot.Head, str: 10);
            inventory.AddItem(better);

            var result = AutoEquipper.AutoEquip(character, inventory);
            Assert.AreEqual(1, result.ItemsEquipped);
            Assert.AreEqual(better, character.equipment[(int)EquipmentSlot.Head]);
        }

        [Test]
        public void AutoEquip_SkipsDowngrade()
        {
            var character = new CharacterInstance
            {
                baseStats = new Stats { STR = 10, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
            var current = MakeItem(EquipmentSlot.Head, str: 10);
            character.EquipItem(current);

            var inventory = new LootInventory(50);
            inventory.AddItem(MakeItem(EquipmentSlot.Head, str: 2));

            var result = AutoEquipper.AutoEquip(character, inventory);
            Assert.AreEqual(0, result.ItemsEquipped);
            Assert.AreEqual(current, character.equipment[(int)EquipmentSlot.Head]);
        }

        [Test]
        public void AutoEquip_EquipsIntoEmptySlot()
        {
            var character = new CharacterInstance
            {
                baseStats = new Stats { STR = 10, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };

            var inventory = new LootInventory(50);
            var item = MakeItem(EquipmentSlot.Head, str: 5);
            inventory.AddItem(item);

            var result = AutoEquipper.AutoEquip(character, inventory);
            Assert.AreEqual(1, result.ItemsEquipped);
            Assert.AreEqual(item, character.equipment[(int)EquipmentSlot.Head]);
        }

        [Test]
        public void AutoEquip_ReturnsOldItemToInventory()
        {
            var character = new CharacterInstance
            {
                baseStats = new Stats { STR = 10, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
            var oldItem = MakeItem(EquipmentSlot.Head, str: 3);
            character.EquipItem(oldItem);

            var inventory = new LootInventory(50);
            inventory.AddItem(MakeItem(EquipmentSlot.Head, str: 10));

            AutoEquipper.AutoEquip(character, inventory);
            Assert.IsTrue(inventory.Items.Contains(oldItem));
        }

        [Test]
        public void AutoEquip_MultipleSlots_EquipsBestPerSlot()
        {
            var character = new CharacterInstance
            {
                baseStats = new Stats { STR = 10, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };

            var inventory = new LootInventory(50);
            inventory.AddItem(MakeItem(EquipmentSlot.Head, str: 5));
            inventory.AddItem(MakeItem(EquipmentSlot.Torso, str: 8));

            var result = AutoEquipper.AutoEquip(character, inventory);
            Assert.AreEqual(2, result.ItemsEquipped);
            Assert.IsNotNull(character.equipment[(int)EquipmentSlot.Head]);
            Assert.IsNotNull(character.equipment[(int)EquipmentSlot.Torso]);
        }
    }
}
