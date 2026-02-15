using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    [TestFixture]
    public class ItemComparerTests
    {
        private EquipmentInstance MakeItem(int str = 0, int dex = 0, int con = 0,
            Rarity rarity = Rarity.Common, EquipmentSlot slot = EquipmentSlot.Head,
            List<RolledAffix> affixes = null)
        {
            var stats = new Stats { STR = str, DEX = dex, CON = con };
            return new EquipmentInstance(
                "hd01", 1, slot, rarity,
                Color.white, new Dictionary<int, Color>(),
                stats, affixes ?? new List<RolledAffix>(),
                new int[0], new int[0], new int[0],
                false, null, "Test");
        }

        [Test]
        public void ScoreItem_SumsAllStats()
        {
            var item = MakeItem(str: 5, dex: 3, con: 2);
            Assert.AreEqual(10f, ItemComparer.ScoreItem(item), 0.01f);
        }

        [Test]
        public void ScoreItem_IncludesFlatAffixes()
        {
            var affixes = new List<RolledAffix>
            {
                new RolledAffix("mighty", StatType.STR, 4f, false)
            };
            var item = MakeItem(str: 5, affixes: affixes);
            Assert.AreEqual(9f, ItemComparer.ScoreItem(item), 0.01f);
        }

        [Test]
        public void ScoreItem_WeightsPercentageAffixes()
        {
            var affixes = new List<RolledAffix>
            {
                new RolledAffix("fortified", StatType.CON, 2f, true)
            };
            var item = MakeItem(str: 5, affixes: affixes);
            Assert.AreEqual(15f, ItemComparer.ScoreItem(item), 0.01f);
        }

        [Test]
        public void Compare_HigherScoreNewItem_PositiveDelta()
        {
            var current = MakeItem(str: 3);
            var newItem = MakeItem(str: 8);
            var diff = ItemComparer.Compare(newItem, current);
            Assert.IsTrue(diff.TotalDelta > 0);
            Assert.IsTrue(diff.IsUpgrade);
        }

        [Test]
        public void Compare_LowerScoreNewItem_NegativeDelta()
        {
            var current = MakeItem(str: 8);
            var newItem = MakeItem(str: 3);
            var diff = ItemComparer.Compare(newItem, current);
            Assert.IsTrue(diff.TotalDelta < 0);
            Assert.IsFalse(diff.IsUpgrade);
        }

        [Test]
        public void Compare_NullCurrent_TreatsAsZero()
        {
            var newItem = MakeItem(str: 5);
            var diff = ItemComparer.Compare(newItem, null);
            Assert.IsTrue(diff.TotalDelta > 0);
            Assert.IsTrue(diff.IsUpgrade);
        }

        [Test]
        public void FindBestForSlot_ReturnsBestScoringItem()
        {
            var candidates = new List<EquipmentInstance>
            {
                MakeItem(str: 2),
                MakeItem(str: 8),
                MakeItem(str: 5)
            };
            var best = ItemComparer.FindBestForSlot(EquipmentSlot.Head, candidates, null);
            Assert.AreEqual(8, best.StatMods.STR);
        }

        [Test]
        public void FindBestForSlot_SkipsItemsWorseThanCurrent()
        {
            var current = MakeItem(str: 10);
            var candidates = new List<EquipmentInstance>
            {
                MakeItem(str: 2),
                MakeItem(str: 5)
            };
            var best = ItemComparer.FindBestForSlot(EquipmentSlot.Head, candidates, current);
            Assert.IsNull(best);
        }
    }
}
