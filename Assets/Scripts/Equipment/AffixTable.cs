using System;
using System.Collections.Generic;
using Starquill.Core;
using UnityEngine;

namespace Starquill.Equipment
{
    public class AffixEntry
    {
        public string AffixId;
        public string DisplayName;
        public StatType StatType;
        public bool IsPercentage;
        public HashSet<EquipmentSlot> ValidSlots = new();
        public Dictionary<Rarity, (float min, float max)> ValueRanges = new();
    }

    public class AffixTable
    {
        public List<AffixEntry> AllAffixes { get; private set; } = new();

        public void LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>("Data/affixes");
            if (asset != null) LoadFromJson(asset.text);
        }

        public void LoadFromJson(string json)
        {
            AllAffixes.Clear();
            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                var entry = new AffixEntry
                {
                    AffixId = obj.GetString("affix_id"),
                    DisplayName = obj.GetString("display_name"),
                    IsPercentage = obj.GetBool("is_percentage")
                };

                var statStr = obj.GetString("stat_type");
                if (Enum.TryParse<StatType>(statStr, out var st))
                    entry.StatType = st;

                var slotsArr = obj.GetStringArray("valid_slots");
                foreach (var s in slotsArr)
                    if (Enum.TryParse<EquipmentSlot>(s, out var slot))
                        entry.ValidSlots.Add(slot);

                if (obj.TryGetValue("value_ranges", out var rangesObj)
                    && rangesObj is Dictionary<string, object> ranges)
                {
                    foreach (var kvp in ranges)
                    {
                        if (Enum.TryParse<Rarity>(kvp.Key, out var rarity)
                            && kvp.Value is Dictionary<string, object> rangeDict)
                        {
                            float min = rangeDict.GetFloat("min");
                            float max = rangeDict.GetFloat("max");
                            entry.ValueRanges[rarity] = (min, max);
                        }
                    }
                }

                AllAffixes.Add(entry);
            }
        }

        public List<AffixEntry> GetValidAffixes(EquipmentSlot slot)
        {
            var result = new List<AffixEntry>();
            foreach (var affix in AllAffixes)
                if (affix.ValidSlots.Contains(slot))
                    result.Add(affix);
            return result;
        }

        public List<RolledAffix> RollAffixes(Rarity rarity, EquipmentSlot slot, System.Random rng)
        {
            int count = AffixCountForRarity(rarity, rng);
            if (count == 0) return new List<RolledAffix>();

            var pool = GetValidAffixes(slot);
            if (pool.Count == 0) return new List<RolledAffix>();

            var picked = new List<RolledAffix>();
            var usedIds = new HashSet<string>();

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                var available = new List<AffixEntry>();
                foreach (var a in pool)
                    if (!usedIds.Contains(a.AffixId))
                        available.Add(a);
                if (available.Count == 0) break;

                var chosen = available[rng.Next(available.Count)];
                usedIds.Add(chosen.AffixId);

                float value = 0;
                if (chosen.ValueRanges.TryGetValue(rarity, out var range))
                    value = range.min + (float)(rng.NextDouble() * (range.max - range.min));

                if (!chosen.IsPercentage)
                    value = Mathf.Round(value);

                picked.Add(new RolledAffix(chosen.AffixId, chosen.StatType, value, chosen.IsPercentage));
            }

            return picked;
        }

        public static int AffixCountForRarity(Rarity rarity, System.Random rng)
        {
            return rarity switch
            {
                Rarity.Common => 0,
                Rarity.Uncommon => 1,
                Rarity.Rare => 2,
                Rarity.Epic => 2 + (rng.Next(2) == 0 ? 1 : 0),
                Rarity.Legendary => 3,
                _ => 0
            };
        }
    }
}
