using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    public class EquipmentCatalogTests
    {
        private EquipmentCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = new EquipmentCatalog();
        }

        [Test]
        public void LoadEquipment_ParsesAllEntries()
        {
            var asset = Resources.Load<TextAsset>("Data/equipment");
            Assert.IsNotNull(asset, "equipment.json not found in Resources");
            catalog.LoadEquipmentJson(asset.text);
            Assert.Greater(catalog.AllEntries.Count, 30);
        }

        [Test]
        public void LoadWeapons_ParsesAllEntries()
        {
            var asset = Resources.Load<TextAsset>("Data/weapons");
            Assert.IsNotNull(asset, "weapons.json not found in Resources");
            catalog.LoadWeaponsJson(asset.text);
            Assert.Greater(catalog.WeaponEntries.Count, 5);
        }

        [Test]
        public void GetByPrefix_ReturnsTorsoItems()
        {
            var asset = Resources.Load<TextAsset>("Data/equipment");
            catalog.LoadEquipmentJson(asset.text);
            var torsoItems = catalog.GetByPrefix("tr");
            Assert.Greater(torsoItems.Count, 10);
            foreach (var item in torsoItems)
                Assert.IsTrue(item.ItemType.StartsWith("tr"));
        }

        [Test]
        public void GetByPrefix_ReturnsHeadItems()
        {
            var asset = Resources.Load<TextAsset>("Data/equipment");
            catalog.LoadEquipmentJson(asset.text);
            var headItems = catalog.GetByPrefix("hd");
            Assert.Greater(headItems.Count, 10);
        }

        [Test]
        public void SlotForPrefix_MapsCorrectly()
        {
            Assert.AreEqual(EquipmentSlot.Head, EquipmentCatalog.SlotForPrefix("hd"));
            Assert.AreEqual(EquipmentSlot.Torso, EquipmentCatalog.SlotForPrefix("tr"));
            Assert.AreEqual(EquipmentSlot.Arms, EquipmentCatalog.SlotForPrefix("ar"));
            Assert.AreEqual(EquipmentSlot.Legs, EquipmentCatalog.SlotForPrefix("lg"));
            Assert.AreEqual(EquipmentSlot.Feet, EquipmentCatalog.SlotForPrefix("fe"));
            Assert.AreEqual(EquipmentSlot.Misc1, EquipmentCatalog.SlotForPrefix("mc"));
            Assert.AreEqual(EquipmentSlot.MainHand, EquipmentCatalog.SlotForPrefix("w"));
        }

        [Test]
        public void WeaponHandType_ParsedCorrectly()
        {
            var asset = Resources.Load<TextAsset>("Data/weapons");
            catalog.LoadWeaponsJson(asset.text);
            var sword = catalog.GetWeapon("w01");
            Assert.IsNotNull(sword);
            Assert.AreEqual("one_handed", sword.HandType);
            var twoHandSword = catalog.GetWeapon("w04");
            Assert.IsNotNull(twoHandSword);
            Assert.AreEqual("two_handed", twoHandSword.HandType);
        }

        [Test]
        public void WeaponAmount_IsArrayForModular()
        {
            var asset = Resources.Load<TextAsset>("Data/weapons");
            catalog.LoadWeaponsJson(asset.text);
            var sword = catalog.GetWeapon("w01");
            Assert.IsTrue(sword.Modular);
            Assert.AreEqual(3, sword.AmountPerLayer.Length);
        }

        [Test]
        public void LoadAll_CombinesEquipmentAndWeapons()
        {
            catalog.LoadFromResources();
            Assert.Greater(catalog.AllEntries.Count, 30);
            Assert.Greater(catalog.WeaponEntries.Count, 5);
        }
    }
}
