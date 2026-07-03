using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Display;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    [TestFixture]
    public class EquipmentDisplayMapperTests
    {
        private static EquipmentInstance Make(EquipmentSlot slot,
            string itemType = "it01", int itemNum = 2)
        {
            return new EquipmentInstance(
                itemType, itemNum, slot, Rarity.Common,
                Color.red, new Dictionary<int, Color> { { 3, Color.blue } },
                StatType.STR, 4, StatType.DEX, 2, null,
                new int[0], new int[0], new int[0],
                false, null, "Test", new[] { 7, 8 });
        }

        [Test]
        public void MapsFieldsAndSkipsNullSlots()
        {
            var equipment = new[] { null, Make(EquipmentSlot.Torso, "tr01", 5), null };

            var list = EquipmentDisplayMapper.ToDisplayList(equipment);

            Assert.AreEqual(1, list.Count);
            var info = list[0];
            Assert.AreEqual("tr01", info.ItemType);
            Assert.AreEqual(5, info.ItemNum);
            Assert.AreEqual(Color.red, info.BaseColor);
            Assert.AreEqual(Color.blue, info.VarianceColors[3]);
            Assert.AreEqual(new[] { 7, 8 }, info.LayerVariants);
            Assert.IsFalse(info.IsOffhand);
        }

        [Test]
        public void IncludesHeadByDefault()
        {
            var equipment = new[] { Make(EquipmentSlot.Head, "hd01"), Make(EquipmentSlot.Torso, "tr01") };

            var list = EquipmentDisplayMapper.ToDisplayList(equipment);

            Assert.AreEqual(2, list.Count);
        }

        [Test]
        public void BareHead_ExcludesOnlyHeadSlot()
        {
            var equipment = new[]
            {
                Make(EquipmentSlot.Head, "hd01"),
                Make(EquipmentSlot.Torso, "tr01"),
                Make(EquipmentSlot.MainHand, "wp01"),
                Make(EquipmentSlot.OffHand, "sh01")
            };

            var list = EquipmentDisplayMapper.ToDisplayList(equipment, bareHead: true);

            Assert.AreEqual(3, list.Count);
            foreach (var info in list)
                Assert.AreNotEqual("hd01", info.ItemType);
        }

        [Test]
        public void BareHead_DoesNotMutateSourceEquipment()
        {
            var head = Make(EquipmentSlot.Head, "hd01");
            var equipment = new[] { head, Make(EquipmentSlot.Torso, "tr01") };

            EquipmentDisplayMapper.ToDisplayList(equipment, bareHead: true);

            // Display-only rule: the recruit keeps its head gear.
            Assert.AreSame(head, equipment[0]);
            Assert.AreEqual(EquipmentSlot.Head, equipment[0].Slot);
        }

        [Test]
        public void OffhandFlagSetForOffhandSlot()
        {
            var list = EquipmentDisplayMapper.ToDisplayList(
                new[] { Make(EquipmentSlot.OffHand, "sh01") });

            Assert.IsTrue(list[0].IsOffhand);
        }

        [Test]
        public void NullEquipmentYieldsEmptyList()
        {
            var list = EquipmentDisplayMapper.ToDisplayList(null);

            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }
    }
}
