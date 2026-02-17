using System;
using System.Collections.Generic;
using Starquill.Core;
using UnityEngine;

namespace Starquill.Equipment
{
    public class AbilityTable
    {
        public List<AbilityEntry> AllAbilities { get; private set; } = new();

        public void LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>("Data/abilities");
            if (asset != null) LoadFromJson(asset.text);
        }

        public void LoadFromJson(string json)
        {
            AllAbilities.Clear();
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
            }
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
