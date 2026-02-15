using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    public class AffixTableTests
    {
        private AffixTable table;

        [SetUp]
        public void SetUp()
        {
            table = new AffixTable();
            var asset = Resources.Load<TextAsset>("Data/affixes");
            Assert.IsNotNull(asset, "affixes.json not found in Resources");
            table.LoadFromJson(asset.text);
        }

        [Test]
        public void LoadFromJson_ParsesAllAffixes()
        {
            Assert.AreEqual(6, table.AllAffixes.Count);
        }

        [Test]
        public void GetValidAffixes_ForTorso_ReturnsAll()
        {
            var valid = table.GetValidAffixes(EquipmentSlot.Torso);
            Assert.AreEqual(6, valid.Count);
        }

        [Test]
        public void GetValidAffixes_ForFeet_ExcludesINTWISCHA()
        {
            var valid = table.GetValidAffixes(EquipmentSlot.Feet);
            Assert.AreEqual(3, valid.Count);
        }

        [Test]
        public void RollAffixes_ReturnsCorrectCount()
        {
            var rng = new System.Random(42);
            var affixes = table.RollAffixes(Rarity.Rare, EquipmentSlot.Torso, rng);
            Assert.AreEqual(2, affixes.Count);
        }

        [Test]
        public void RollAffixes_Common_ReturnsNone()
        {
            var rng = new System.Random(42);
            var affixes = table.RollAffixes(Rarity.Common, EquipmentSlot.Torso, rng);
            Assert.AreEqual(0, affixes.Count);
        }

        [Test]
        public void RollAffixes_NoDuplicateIds()
        {
            var rng = new System.Random(42);
            var affixes = table.RollAffixes(Rarity.Epic, EquipmentSlot.Torso, rng);
            var ids = new HashSet<string>();
            foreach (var a in affixes)
                Assert.IsTrue(ids.Add(a.AffixId), $"Duplicate affix: {a.AffixId}");
        }

        [Test]
        public void RollAffixes_ValuesWithinRange()
        {
            var rng = new System.Random(42);
            for (int i = 0; i < 50; i++)
            {
                var affixes = table.RollAffixes(Rarity.Rare, EquipmentSlot.Torso, new System.Random(i));
                foreach (var a in affixes)
                    Assert.IsTrue(a.Value >= 2 && a.Value <= 5,
                        $"Affix {a.AffixId} value {a.Value} out of Rare range [2,5]");
            }
        }
    }
}
