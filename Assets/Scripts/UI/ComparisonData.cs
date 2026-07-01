using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.UI
{
    /// Pure equipped-vs-selected comparison for the item detail sheet.
    /// Values come from GetTotalStatMods (stat pair + ability potency).
    public struct ComparisonData
    {
        public struct Row
        {
            public StatType Stat;
            public int EquippedValue;
            public int SelectedValue;
            public int Delta;
        }

        public List<Row> Rows;
        public int TotalDelta;
        /// Pre-redesign saves reconstruct with a zero stat pair; UI shows "Legacy item".
        public bool IsLegacyItem;

        public static ComparisonData Build(EquipmentInstance selected, EquipmentInstance equipped)
        {
            var cmp = new ComparisonData { Rows = new List<Row>() };
            if (selected == null) return cmp;

            cmp.IsLegacyItem = selected.PrimaryValue == 0 && selected.SecondaryValue == 0;

            var selectedMods = selected.GetTotalStatMods();
            var equippedMods = equipped?.GetTotalStatMods();

            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                int sel = selectedMods.GetStat(stat);
                int eq = equippedMods != null ? equippedMods.GetStat(stat) : 0;
                if (sel == 0 && eq == 0) continue;

                cmp.Rows.Add(new Row
                {
                    Stat = stat,
                    EquippedValue = eq,
                    SelectedValue = sel,
                    Delta = sel - eq
                });
                cmp.TotalDelta += sel - eq;
            }

            return cmp;
        }
    }
}
