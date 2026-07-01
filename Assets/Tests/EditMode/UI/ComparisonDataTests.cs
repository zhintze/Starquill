using NUnit.Framework;
using UnityEngine;
using Starquill.Core;
using Starquill.Equipment;
using Starquill.UI;

namespace Starquill.Tests.EditMode.UI
{
    public class ComparisonDataTests
    {
        private EquipmentInstance MakeItem(StatType primary, int primaryVal,
            StatType secondary, int secondaryVal, AwakenedAbility ability = null)
        {
            return new EquipmentInstance("tr03", 1, EquipmentSlot.Torso, Rarity.Common,
                Color.white, null,
                primary, primaryVal, secondary, secondaryVal, ability,
                null, null, null, false, null, "Test Item");
        }

        [Test]
        public void Build_Upgrade_PositiveDeltas()
        {
            var selected = MakeItem(StatType.CON, 8, StatType.STR, 3);
            var equipped = MakeItem(StatType.CON, 4, StatType.STR, 1);
            var cmp = ComparisonData.Build(selected, equipped);

            var conRow = cmp.Rows.Find(r => r.Stat == StatType.CON);
            Assert.AreEqual(4, conRow.EquippedValue);
            Assert.AreEqual(8, conRow.SelectedValue);
            Assert.AreEqual(4, conRow.Delta);
            Assert.AreEqual(6, cmp.TotalDelta); // (8-4) + (3-1)
        }

        [Test]
        public void Build_Downgrade_NegativeDeltas()
        {
            var selected = MakeItem(StatType.CON, 2, StatType.STR, 1);
            var equipped = MakeItem(StatType.CON, 6, StatType.STR, 4);
            var cmp = ComparisonData.Build(selected, equipped);
            Assert.AreEqual(-7, cmp.TotalDelta);
        }

        [Test]
        public void Build_DifferentStats_RowsForBothSides()
        {
            var selected = MakeItem(StatType.INT, 5, StatType.WIS, 2);
            var equipped = MakeItem(StatType.CON, 4, StatType.STR, 3);
            var cmp = ComparisonData.Build(selected, equipped);

            Assert.AreEqual(4, cmp.Rows.Count); // INT, WIS gained; CON, STR lost
            var conRow = cmp.Rows.Find(r => r.Stat == StatType.CON);
            Assert.AreEqual(4, conRow.EquippedValue);
            Assert.AreEqual(0, conRow.SelectedValue);
            Assert.AreEqual(-4, conRow.Delta);
        }

        [Test]
        public void Build_EmptySlot_DeltasEqualSelectedValues()
        {
            var selected = MakeItem(StatType.CON, 8, StatType.STR, 3);
            var cmp = ComparisonData.Build(selected, null);

            Assert.AreEqual(2, cmp.Rows.Count);
            Assert.AreEqual(11, cmp.TotalDelta);
            foreach (var row in cmp.Rows)
                Assert.AreEqual(0, row.EquippedValue);
        }

        [Test]
        public void Build_IncludesAbilityPotencyViaTotalMods()
        {
            var ability = new AwakenedAbility
            {
                AbilityId = "ironhide", BoostedStat = StatType.CON,
                BasePotency = 3f, PotencyPerLevel = 2f, Level = 1, MaxLevel = 3
            };
            var selected = MakeItem(StatType.CON, 8, StatType.STR, 3, ability);
            var cmp = ComparisonData.Build(selected, null);

            var conRow = cmp.Rows.Find(r => r.Stat == StatType.CON);
            Assert.AreEqual(11, conRow.SelectedValue); // 8 + 3 ability
        }

        [Test]
        public void Build_LegacyZeroStatItem_Flagged()
        {
            var selected = MakeItem(StatType.STR, 0, StatType.STR, 0);
            var cmp = ComparisonData.Build(selected, null);
            Assert.IsTrue(cmp.IsLegacyItem);
        }

        [Test]
        public void Build_NormalItem_NotLegacy()
        {
            var selected = MakeItem(StatType.CON, 8, StatType.STR, 3);
            var cmp = ComparisonData.Build(selected, null);
            Assert.IsFalse(cmp.IsLegacyItem);
        }

        [Test]
        public void Build_NullSelected_EmptyRows()
        {
            var cmp = ComparisonData.Build(null, MakeItem(StatType.CON, 4, StatType.STR, 1));
            Assert.AreEqual(0, cmp.Rows.Count);
            Assert.AreEqual(0, cmp.TotalDelta);
        }
    }
}
