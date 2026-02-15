using System.Collections.Generic;
using Starquill.Core;
using UnityEngine;

namespace Starquill.Equipment
{
    public class CatalogEntry
    {
        public string ItemType;
        public string Description;
        public int Amount;
        public int[] LayerCodes;
        public int[] HiddenLayers;
        public int[] LayerColorVariance;
        public bool Modular;
    }

    public class WeaponEntry
    {
        public string ItemType;
        public string Description;
        public int[] AmountPerLayer;
        public int[] LayerCodes;
        public int[] HiddenLayers;
        public int[] LayerColorVariance;
        public bool Modular;
        public string HandType;
    }

    public class EquipmentCatalog
    {
        public List<CatalogEntry> AllEntries { get; private set; } = new();
        public List<WeaponEntry> WeaponEntries { get; private set; } = new();

        private readonly Dictionary<string, CatalogEntry> byType = new();
        private readonly Dictionary<string, List<CatalogEntry>> byPrefix = new();
        private readonly Dictionary<string, WeaponEntry> weaponsByType = new();

        public void LoadFromResources()
        {
            var eqAsset = Resources.Load<TextAsset>("Data/equipment");
            if (eqAsset != null) LoadEquipmentJson(eqAsset.text);

            var wpnAsset = Resources.Load<TextAsset>("Data/weapons");
            if (wpnAsset != null) LoadWeaponsJson(wpnAsset.text);
        }

        public void LoadEquipmentJson(string json)
        {
            AllEntries.Clear();
            byType.Clear();
            byPrefix.Clear();

            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                var entry = new CatalogEntry
                {
                    ItemType = obj.GetString("item_type"),
                    Description = obj.GetString("description"),
                    Amount = obj.GetInt("amount", 1),
                    LayerCodes = obj.GetIntArray("layer_codes"),
                    HiddenLayers = obj.GetIntArray("hidden_layers"),
                    LayerColorVariance = obj.GetIntArray("layer_color_variance"),
                    Modular = obj.GetBool("modular")
                };

                if (string.IsNullOrEmpty(entry.ItemType) || entry.Amount <= 0)
                    continue;

                AllEntries.Add(entry);
                byType[entry.ItemType] = entry;

                string prefix = entry.ItemType.Length >= 2
                    ? entry.ItemType.Substring(0, 2).ToLower()
                    : entry.ItemType;
                if (!byPrefix.ContainsKey(prefix))
                    byPrefix[prefix] = new List<CatalogEntry>();
                byPrefix[prefix].Add(entry);
            }
        }

        public void LoadWeaponsJson(string json)
        {
            WeaponEntries.Clear();
            weaponsByType.Clear();

            var list = SimpleJson.ParseArray(json);
            foreach (var obj in list)
            {
                var entry = new WeaponEntry
                {
                    ItemType = obj.GetString("item_type"),
                    Description = obj.GetString("description"),
                    LayerCodes = obj.GetIntArray("layer_codes"),
                    HiddenLayers = obj.GetIntArray("hidden_layers"),
                    LayerColorVariance = obj.GetIntArray("layer_color_variance"),
                    Modular = obj.GetBool("modular"),
                    HandType = obj.GetString("hand_type", "one_handed")
                };

                // Amount can be int or int[] depending on modular
                if (entry.Modular)
                    entry.AmountPerLayer = obj.GetIntArray("amount");
                else
                    entry.AmountPerLayer = new[] { obj.GetInt("amount", 1) };

                if (string.IsNullOrEmpty(entry.ItemType)) continue;

                WeaponEntries.Add(entry);
                weaponsByType[entry.ItemType] = entry;
            }
        }

        public List<CatalogEntry> GetByPrefix(string prefix)
        {
            prefix = prefix.ToLower();
            return byPrefix.TryGetValue(prefix, out var list) ? list : new List<CatalogEntry>();
        }

        public CatalogEntry GetEntry(string itemType)
        {
            return byType.TryGetValue(itemType, out var entry) ? entry : null;
        }

        public WeaponEntry GetWeapon(string itemType)
        {
            return weaponsByType.TryGetValue(itemType, out var entry) ? entry : null;
        }

        public static EquipmentSlot SlotForPrefix(string prefix)
        {
            return prefix.ToLower() switch
            {
                "hd" => EquipmentSlot.Head,
                "tr" => EquipmentSlot.Torso,
                "ar" => EquipmentSlot.Arms,
                "lg" => EquipmentSlot.Legs,
                "fe" => EquipmentSlot.Feet,
                "mc" => EquipmentSlot.Misc1,
                "w" => EquipmentSlot.MainHand,
                _ => EquipmentSlot.Misc1
            };
        }
    }
}
