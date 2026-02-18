using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    [TestFixture]
    public class LootInventoryTests
    {
        private LootInventory inventory;

        private EquipmentInstance MakeItem(EquipmentSlot slot = EquipmentSlot.Head,
            Rarity rarity = Rarity.Common)
        {
            return new EquipmentInstance(
                "hd01", 1, slot, rarity,
                Color.white, new Dictionary<int, Color>(),
                StatType.STR, 1, StatType.DEX, 1, null,
                new int[0], new int[0], new int[0],
                false, null, "Test Item");
        }

        [SetUp]
        public void SetUp()
        {
            inventory = new LootInventory(5);
        }

        [Test]
        public void AddItem_WithinCapacity_ReturnsTrue()
        {
            Assert.IsTrue(inventory.AddItem(MakeItem()));
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void AddItem_AtCapacity_ReturnsFalse()
        {
            for (int i = 0; i < 5; i++)
                inventory.AddItem(MakeItem());
            Assert.IsFalse(inventory.AddItem(MakeItem()));
            Assert.AreEqual(5, inventory.Count);
        }

        [Test]
        public void RemoveItem_ExistingItem_ReturnsTrue()
        {
            var item = MakeItem();
            inventory.AddItem(item);
            Assert.IsTrue(inventory.RemoveItem(item));
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void RemoveItem_NonexistentItem_ReturnsFalse()
        {
            Assert.IsFalse(inventory.RemoveItem(MakeItem()));
        }

        [Test]
        public void GetItemsForSlot_FiltersCorrectly()
        {
            inventory.AddItem(MakeItem(EquipmentSlot.Head));
            inventory.AddItem(MakeItem(EquipmentSlot.Torso));
            inventory.AddItem(MakeItem(EquipmentSlot.Head));

            var heads = inventory.GetItemsForSlot(EquipmentSlot.Head);
            Assert.AreEqual(2, heads.Count);
        }

        [Test]
        public void IsFull_AtCapacity_ReturnsTrue()
        {
            for (int i = 0; i < 5; i++)
                inventory.AddItem(MakeItem());
            Assert.IsTrue(inventory.IsFull);
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            inventory.AddItem(MakeItem());
            inventory.AddItem(MakeItem());
            inventory.Clear();
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void OnItemAdded_FiresWhenItemAdded()
        {
            EquipmentInstance received = null;
            inventory.OnItemAdded += item => received = item;
            var added = MakeItem();
            inventory.AddItem(added);
            Assert.AreEqual(added, received);
        }

        [Test]
        public void OnItemRemoved_FiresWhenItemRemoved()
        {
            EquipmentInstance received = null;
            inventory.OnItemRemoved += item => received = item;
            var item = MakeItem();
            inventory.AddItem(item);
            inventory.RemoveItem(item);
            Assert.AreEqual(item, received);
        }
    }
}
