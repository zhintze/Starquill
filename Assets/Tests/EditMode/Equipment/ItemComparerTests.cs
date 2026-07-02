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
        private EquipmentInstance MakeItem(int primaryVal = 0, int secondaryVal = 0,
            StatType primaryStat = StatType.STR, StatType secondaryStat = StatType.DEX,
            Rarity rarity = Rarity.Common, EquipmentSlot slot = EquipmentSlot.Head)
        {
            return new EquipmentInstance(
                "hd01", 1, slot, rarity,
                Color.white, new Dictionary<int, Color>(),
                primaryStat, primaryVal, secondaryStat, secondaryVal, null,
                new int[0], new int[0], new int[0],
                false, null, "Test");
        }

        [Test]
        public void ScoreItem_SumsPrimaryAndSecondary()
        {
            var item = MakeItem(primaryVal: 8, secondaryVal: 3);
            Assert.AreEqual(11f, ItemComparer.ScoreItem(item), 0.01f);
        }

        [Test]
        public void ScoreItem_IncludesAbilityPotency()
        {
            var item = new EquipmentInstance(
                "hd01", 1, EquipmentSlot.Head, Rarity.Common,
                Color.white, new Dictionary<int, Color>(),
                StatType.STR, 8, StatType.DEX, 3,
                new AwakenedAbility
                {
                    AbilityId = "t", BoostedStat = StatType.CON,
                    BasePotency = 4f, PotencyPerLevel = 1f, Level = 1, MaxLevel = 3
                },
                new int[0], new int[0], new int[0], false, null, "Test");
            Assert.AreEqual(15f, ItemComparer.ScoreItem(item), 0.01f); // 8+3+4
        }

        [Test]
        public void ScoreItem_NullReturnsZero()
        {
            Assert.AreEqual(0f, ItemComparer.ScoreItem(null), 0.01f);
        }

        [Test]
        public void Compare_HigherScoreNewItem_PositiveDelta()
        {
            var current = MakeItem(primaryVal: 3, secondaryVal: 1);
            var newItem = MakeItem(primaryVal: 8, secondaryVal: 3);
            var diff = ItemComparer.Compare(newItem, current);
            Assert.IsTrue(diff.TotalDelta > 0);
            Assert.IsTrue(diff.IsUpgrade);
        }

        [Test]
        public void Compare_LowerScoreNewItem_NegativeDelta()
        {
            var current = MakeItem(primaryVal: 8, secondaryVal: 3);
            var newItem = MakeItem(primaryVal: 3, secondaryVal: 1);
            var diff = ItemComparer.Compare(newItem, current);
            Assert.IsTrue(diff.TotalDelta < 0);
            Assert.IsFalse(diff.IsUpgrade);
        }

        [Test]
        public void Compare_NullCurrent_TreatsAsZero()
        {
            var newItem = MakeItem(primaryVal: 5, secondaryVal: 2);
            var diff = ItemComparer.Compare(newItem, null);
            Assert.IsTrue(diff.TotalDelta > 0);
            Assert.IsTrue(diff.IsUpgrade);
        }

        [Test]
        public void Compare_ReturnsPerStatDeltas()
        {
            var current = MakeItem(primaryVal: 5, secondaryVal: 2);
            var newItem = MakeItem(primaryVal: 8, secondaryVal: 1);
            var diff = ItemComparer.Compare(newItem, current);
            Assert.AreEqual(3, diff.PrimaryDelta);
            Assert.AreEqual(-1, diff.SecondaryDelta);
        }

        [Test]
        public void FindBestForSlot_ReturnsBestScoringItem()
        {
            var candidates = new List<EquipmentInstance>
            {
                MakeItem(primaryVal: 2, secondaryVal: 1),
                MakeItem(primaryVal: 8, secondaryVal: 3),
                MakeItem(primaryVal: 5, secondaryVal: 2)
            };
            var best = ItemComparer.FindBestForSlot(EquipmentSlot.Head, candidates, null);
            Assert.AreEqual(8, best.PrimaryValue);
        }

        [Test]
        public void FindBestForSlot_SkipsItemsWorseThanCurrent()
        {
            var current = MakeItem(primaryVal: 10, secondaryVal: 5);
            var candidates = new List<EquipmentInstance>
            {
                MakeItem(primaryVal: 2, secondaryVal: 1),
                MakeItem(primaryVal: 5, secondaryVal: 2)
            };
            var best = ItemComparer.FindBestForSlot(EquipmentSlot.Head, candidates, current);
            Assert.IsNull(best);
        }
    }
}
