using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using Starquill.Display;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    public class EquipmentFactoryTests
    {
        private EquipmentFactory factory;
        private EquipmentCatalog catalog;
        private AffixTable affixTable;
        private ColorManager colors;

        [SetUp]
        public void SetUp()
        {
            catalog = new EquipmentCatalog();
            catalog.LoadFromResources();

            affixTable = new AffixTable();
            var affixAsset = Resources.Load<TextAsset>("Data/affixes");
            if (affixAsset != null) affixTable.LoadFromJson(affixAsset.text);

            colors = new ColorManager();
            colors.LoadFromResources();

            factory = new EquipmentFactory(catalog, affixTable, colors);
        }

        [Test]
        public void CreateRandom_ProducesValidInstance()
        {
            var rng = new System.Random(42);
            var instance = factory.CreateRandom("tr", Rarity.Common, rng);
            Assert.IsNotNull(instance);
            Assert.IsTrue(instance.ItemType.StartsWith("tr"));
            Assert.AreEqual(Rarity.Common, instance.Rarity);
            Assert.Greater(instance.ItemNum, 0);
        }

        [Test]
        public void CreateRandom_CommonHasNoAffixes()
        {
            var rng = new System.Random(42);
            var instance = factory.CreateRandom("tr", Rarity.Common, rng);
            Assert.AreEqual(0, instance.RolledAffixes.Count);
        }

        [Test]
        public void CreateRandom_RareHasTwoAffixes()
        {
            var rng = new System.Random(42);
            var instance = factory.CreateRandom("tr", Rarity.Rare, rng);
            Assert.AreEqual(2, instance.RolledAffixes.Count);
        }

        [Test]
        public void CreateRandom_SetsSlotFromPrefix()
        {
            var rng = new System.Random(42);
            Assert.AreEqual(EquipmentSlot.Torso, factory.CreateRandom("tr", Rarity.Common, rng).Slot);
            Assert.AreEqual(EquipmentSlot.Head, factory.CreateRandom("hd", Rarity.Common, rng).Slot);
            Assert.AreEqual(EquipmentSlot.Arms, factory.CreateRandom("ar", Rarity.Common, rng).Slot);
        }

        [Test]
        public void CreateRandomWeapon_SetsHandType()
        {
            var rng = new System.Random(42);
            var weapon = factory.CreateRandomWeapon(Rarity.Common, rng);
            Assert.IsNotNull(weapon);
            Assert.IsTrue(weapon.ItemType.StartsWith("w"));
            Assert.IsTrue(weapon.HandType == "one_handed" || weapon.HandType == "two_handed");
        }

        [Test]
        public void CreateRandomLoadout_FillsPrioritySlots()
        {
            var rng = new System.Random(42);
            var loadout = factory.CreateRandomLoadout(1, rng);
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Torso], "Torso should always be filled");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Legs], "Legs should always be filled");
        }

        [Test]
        public void CreateRandomLoadout_TwoHandedClearsOffHand()
        {
            bool foundTwoHanded = false;
            for (int seed = 0; seed < 200; seed++)
            {
                var rng = new System.Random(seed);
                var loadout = factory.CreateRandomLoadout(1, rng);
                var mainHand = loadout[(int)EquipmentSlot.MainHand];
                if (mainHand != null && mainHand.HandType == "two_handed")
                {
                    Assert.IsNull(loadout[(int)EquipmentSlot.OffHand],
                        "Two-handed weapon should clear off-hand");
                    foundTwoHanded = true;
                    break;
                }
            }
            if (!foundTwoHanded)
                Assert.Pass("No two-handed weapon rolled in 200 seeds (expected occasionally)");
        }

        [Test]
        public void RollRarity_AtLevel1_MostlyCommon()
        {
            var rng = new System.Random(42);
            int commonCount = 0;
            for (int i = 0; i < 100; i++)
            {
                var rarity = EquipmentFactory.RollRarity(1, rng);
                if (rarity == Rarity.Common) commonCount++;
            }
            Assert.Greater(commonCount, 60, "At level 1, >60% should be Common");
        }

        [Test]
        public void GenerateBaseStats_RareHasMoreThanCommon()
        {
            var rng = new System.Random(42);
            var commonStats = EquipmentFactory.GenerateBaseStats(Rarity.Common, rng);
            var rareStats = EquipmentFactory.GenerateBaseStats(Rarity.Rare, rng);
            Assert.GreaterOrEqual(rareStats.Total, commonStats.Total);
        }

        [Test]
        public void GenerateDisplayName_IncludesRarity()
        {
            var name = EquipmentFactory.GenerateDisplayName(Rarity.Rare, "shirt");
            Assert.IsTrue(name.Contains("Rare"), $"Name '{name}' should contain 'Rare'");
        }

        [Test]
        public void CreateStarterLoadout_FillsAllCoreSlots()
        {
            var loadout = factory.CreateStarterLoadout(new System.Random(42));
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Head], "Head");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Torso], "Torso");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Arms], "Arms");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Legs], "Legs");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Feet], "Feet");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.MainHand], "MainHand");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Misc1], "Misc1");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Misc2], "Misc2");
        }

        [Test]
        public void CreateStarterLoadout_AllCommonRarity()
        {
            var loadout = factory.CreateStarterLoadout(new System.Random(42));
            for (int i = 0; i < loadout.Length; i++)
            {
                if (loadout[i] != null)
                    Assert.AreEqual(Rarity.Common, loadout[i].Rarity, $"Slot {i} should be Common");
            }
        }
    }
}
