using System;
using System.Collections.Generic;
using Starquill.Core;
using UnityEngine;

namespace Starquill.Equipment
{
    public class AbilityTable
    {
        public List<AbilityEntry> AllAbilities { get; private set; } = new();

        private readonly Dictionary<string, AbilityEntry> byId = new();

        public void LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>("Data/abilities");
            if (asset != null) LoadFromJson(asset.text);
        }

        public void LoadFromJson(string json)
        {
            AllAbilities.Clear();
            byId.Clear();
            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                var entry = new AbilityEntry
                {
                    Id = obj.GetString("id"),
                    Name = obj.GetString("name"),
                    Description = obj.GetString("description"),
                    TriggerType = obj.GetString("triggerType"),
                    EffectType = obj.GetString("effectType"),
                    BasePotency = obj.GetFloat("basePotency"),
                    PotencyPerLevel = obj.GetFloat("potencyPerLevel"),
                    ProcChance = obj.GetFloat("procChance"),
                    Weight = obj.GetInt("weight")
                };

                var statStr = obj.GetString("boostedStat");
                if (Enum.TryParse<StatType>(statStr, out var st))
                    entry.BoostedStat = st;

                var typesArr = obj.GetStringArray("validTypes");
                foreach (var t in typesArr)
                    entry.ValidTypes.Add(t);

                AllAbilities.Add(entry);
                if (!string.IsNullOrEmpty(entry.Id))
                    byId[entry.Id] = entry;
            }
        }

        public AbilityEntry GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return byId.TryGetValue(id, out var entry) ? entry : null;
        }

        public static string FormatDescription(AbilityEntry entry, float potency)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Description)) return "";
            return entry.Description.Replace("{potency}", potency.ToString("0.#"));
        }

        public List<AbilityEntry> GetValidAbilities(string itemTypePrefix)
        {
            var result = new List<AbilityEntry>();
            foreach (var ability in AllAbilities)
            {
                foreach (var validType in ability.ValidTypes)
                {
                    if (itemTypePrefix.StartsWith(validType))
                    {
                        result.Add(ability);
                        break;
                    }
                }
            }
            return result;
        }

        public AbilityEntry RollAbility(string itemTypePrefix, System.Random rng)
        {
            var pool = GetValidAbilities(itemTypePrefix);
            if (pool.Count == 0) return null;

            int totalWeight = 0;
            foreach (var entry in pool)
                totalWeight += entry.Weight;

            int roll = rng.Next(totalWeight);
            int cumulative = 0;
            foreach (var entry in pool)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                    return entry;
            }

            return pool[pool.Count - 1];
        }
    }
}
