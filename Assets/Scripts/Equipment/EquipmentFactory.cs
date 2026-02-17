using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;
using Starquill.Display;
using UnityEngine;

namespace Starquill.Equipment
{
    public class EquipmentFactory
    {
        private readonly EquipmentCatalog catalog;
        private readonly AffixTable affixTable;
        private readonly ColorManager colors;

        public EquipmentFactory(EquipmentCatalog catalog, AffixTable affixTable, ColorManager colors)
        {
            this.catalog = catalog;
            this.affixTable = affixTable;
            this.colors = colors;
        }

        public EquipmentInstance CreateRandom(string prefix, Rarity rarity, System.Random rng)
        {
            var entries = catalog.GetByPrefix(prefix);
            if (entries.Count == 0) return null;

            var entry = entries[rng.Next(entries.Count)];
            int itemNum = rng.Next(1, entry.Amount + 1);
            var slot = EquipmentCatalog.SlotForPrefix(prefix);
            var baseColor = colors.GetRandomColor("main", rng);

            var varianceColors = new Dictionary<int, Color>();
            if (entry.LayerColorVariance != null)
            {
                foreach (int layer in entry.LayerColorVariance)
                    varianceColors[layer] = colors.GetRandomColor("main", rng);
            }

            var statMods = GenerateBaseStats(rarity, rng);
            var affixes = affixTable.RollAffixes(rarity, slot, rng);
            var displayName = GenerateDisplayName(rarity, entry.Description);

            return new EquipmentInstance(
                entry.ItemType, itemNum, slot, rarity,
                baseColor, varianceColors, statMods, affixes,
                entry.LayerCodes, entry.HiddenLayers, entry.LayerColorVariance,
                entry.Modular, null, displayName
            );
        }

        public EquipmentInstance CreateRandomWeapon(Rarity rarity, System.Random rng,
            string requiredHandType = null)
        {
            var weapons = catalog.WeaponEntries;
            if (weapons.Count == 0) return null;

            List<WeaponEntry> pool;
            if (requiredHandType != null)
            {
                pool = new List<WeaponEntry>();
                foreach (var w in weapons)
                    if (w.HandType == requiredHandType) pool.Add(w);
            }
            else
            {
                pool = weapons;
            }
            if (pool.Count == 0) return null;

            var entry = pool[rng.Next(pool.Count)];
            var baseColor = colors.GetRandomColor("main", rng);

            int itemNum;
            int[] layerVariants = null;
            if (entry.Modular && entry.AmountPerLayer.Length > 0)
            {
                layerVariants = new int[entry.AmountPerLayer.Length];
                for (int i = 0; i < entry.AmountPerLayer.Length; i++)
                    layerVariants[i] = rng.Next(1, entry.AmountPerLayer[i] + 1);
                itemNum = layerVariants[0];
            }
            else
            {
                itemNum = entry.AmountPerLayer.Length > 0
                    ? rng.Next(1, entry.AmountPerLayer[0] + 1) : 1;
            }

            var varianceColors = new Dictionary<int, Color>();
            if (entry.LayerColorVariance != null)
            {
                foreach (int layer in entry.LayerColorVariance)
                    varianceColors[layer] = colors.GetRandomColor("main", rng);
            }

            var slot = EquipmentSlot.MainHand;
            var statMods = GenerateBaseStats(rarity, rng);
            var affixes = affixTable.RollAffixes(rarity, slot, rng);
            var displayName = GenerateDisplayName(rarity, entry.Description);

            return new EquipmentInstance(
                entry.ItemType, itemNum, slot, rarity,
                baseColor, varianceColors, statMods, affixes,
                entry.LayerCodes, entry.HiddenLayers, entry.LayerColorVariance,
                entry.Modular, entry.HandType, displayName, layerVariants
            );
        }

        public EquipmentInstance[] CreateRandomLoadout(int questLevel, System.Random rng)
        {
            var loadout = new EquipmentInstance[11];

            // Priority slots (always filled)
            loadout[(int)EquipmentSlot.Torso] = CreateRandom("tr", RollRarity(questLevel, rng), rng);
            loadout[(int)EquipmentSlot.Legs] = CreateRandom("lg", RollRarity(questLevel, rng), rng);

            // Chance-based slots
            if (rng.NextDouble() < 0.90)
                loadout[(int)EquipmentSlot.Head] = CreateRandom("hd", RollRarity(questLevel, rng), rng);
            if (rng.NextDouble() < 0.80)
                loadout[(int)EquipmentSlot.Arms] = CreateRandom("ar", RollRarity(questLevel, rng), rng);
            if (rng.NextDouble() < 0.70)
                loadout[(int)EquipmentSlot.Feet] = CreateRandom("fe", RollRarity(questLevel, rng), rng);

            // Weapon (60%)
            if (rng.NextDouble() < 0.60)
            {
                var weapon = CreateRandomWeapon(RollRarity(questLevel, rng), rng);
                if (weapon != null)
                {
                    loadout[(int)EquipmentSlot.MainHand] = weapon;

                    if (weapon.HandType == "one_handed" && rng.NextDouble() < 0.30)
                    {
                        var offhand = CreateRandomWeapon(RollRarity(questLevel, rng), rng, "one_handed");
                        if (offhand != null)
                        {
                            loadout[(int)EquipmentSlot.OffHand] = new EquipmentInstance(
                                offhand.ItemType, offhand.ItemNum, EquipmentSlot.OffHand, offhand.Rarity,
                                offhand.BaseColor, new Dictionary<int, Color>(offhand.VarianceColors),
                                offhand.StatMods, new List<RolledAffix>(offhand.RolledAffixes),
                                offhand.LayerCodes, offhand.HiddenLayers, offhand.LayerColorVariance,
                                offhand.Modular, offhand.HandType, offhand.DisplayName,
                                offhand.LayerVariants
                            );
                        }
                    }
                    else if (weapon.HandType == "two_handed")
                    {
                        loadout[(int)EquipmentSlot.OffHand] = null;
                    }
                }
            }

            // Misc slots (30% each)
            var miscSlots = new[] { EquipmentSlot.Misc1, EquipmentSlot.Misc2, EquipmentSlot.Misc3, EquipmentSlot.Misc4 };
            foreach (var slot in miscSlots)
            {
                if (rng.NextDouble() < 0.30)
                    loadout[(int)slot] = CreateRandom("mc", RollRarity(questLevel, rng), rng);
            }

            return loadout;
        }

        public EquipmentInstance[] CreateStarterLoadout(System.Random rng)
        {
            var loadout = new EquipmentInstance[11];

            // All armor slots
            loadout[(int)EquipmentSlot.Head] = CreateRandom("hd", Rarity.Common, rng);
            loadout[(int)EquipmentSlot.Torso] = CreateRandom("tr", Rarity.Common, rng);
            loadout[(int)EquipmentSlot.Arms] = CreateRandom("ar", Rarity.Common, rng);
            loadout[(int)EquipmentSlot.Legs] = CreateRandom("lg", Rarity.Common, rng);
            loadout[(int)EquipmentSlot.Feet] = CreateRandom("fe", Rarity.Common, rng);

            // Weapon + offhand
            var weapon = CreateRandomWeapon(Rarity.Common, rng, "one_handed");
            if (weapon != null)
            {
                loadout[(int)EquipmentSlot.MainHand] = weapon;
                var offhand = CreateRandomWeapon(Rarity.Common, rng, "one_handed");
                if (offhand != null)
                {
                    loadout[(int)EquipmentSlot.OffHand] = new EquipmentInstance(
                        offhand.ItemType, offhand.ItemNum, EquipmentSlot.OffHand, offhand.Rarity,
                        offhand.BaseColor, new Dictionary<int, Color>(offhand.VarianceColors),
                        offhand.StatMods, new List<RolledAffix>(offhand.RolledAffixes),
                        offhand.LayerCodes, offhand.HiddenLayers, offhand.LayerColorVariance,
                        offhand.Modular, offhand.HandType, offhand.DisplayName
                    );
                }
            }

            // Two misc items
            loadout[(int)EquipmentSlot.Misc1] = CreateRandom("mc", Rarity.Common, rng);
            loadout[(int)EquipmentSlot.Misc2] = CreateRandom("mc", Rarity.Common, rng);

            return loadout;
        }

        public static Rarity RollRarity(int questLevel, System.Random rng)
        {
            float uncommonWeight = 15f + questLevel * 0.5f;
            float rareWeight = 3f + questLevel * 0.3f;
            float epicWeight = 0.5f + questLevel * 0.1f;
            float legendaryWeight = 0.05f + questLevel * 0.02f;
            float commonWeight = 100f - uncommonWeight - rareWeight - epicWeight - legendaryWeight;
            if (commonWeight < 10f) commonWeight = 10f;

            float total = commonWeight + uncommonWeight + rareWeight + epicWeight + legendaryWeight;
            float roll = (float)(rng.NextDouble() * total);

            if (roll < commonWeight) return Rarity.Common;
            roll -= commonWeight;
            if (roll < uncommonWeight) return Rarity.Uncommon;
            roll -= uncommonWeight;
            if (roll < rareWeight) return Rarity.Rare;
            roll -= rareWeight;
            if (roll < epicWeight) return Rarity.Epic;
            return Rarity.Legendary;
        }

        public static Stats GenerateBaseStats(Rarity rarity, System.Random rng)
        {
            var (minBudget, maxBudget, statCount) = rarity switch
            {
                Rarity.Common => (0, 2, 1),
                Rarity.Uncommon => (2, 4, rng.Next(1, 3)),
                Rarity.Rare => (4, 8, 2),
                Rarity.Epic => (8, 14, rng.Next(2, 4)),
                Rarity.Legendary => (14, 20, 3),
                _ => (0, 0, 0)
            };

            int budget = rng.Next(minBudget, maxBudget + 1);
            var stats = new Stats();
            var types = (StatType[])Enum.GetValues(typeof(StatType));

            for (int i = 0; i < statCount && budget > 0; i++)
            {
                var type = types[rng.Next(types.Length)];
                int amount = (i == statCount - 1) ? budget : rng.Next(1, budget);
                stats.SetStat(type, stats.GetStat(type) + amount);
                budget -= amount;
            }

            return stats;
        }

        public static string GenerateDisplayName(Rarity rarity, string baseDescription)
        {
            string rarityPrefix = rarity switch
            {
                Rarity.Common => "",
                Rarity.Uncommon => "Fine",
                Rarity.Rare => "Rare",
                Rarity.Epic => "Epic",
                Rarity.Legendary => "Legendary",
                _ => ""
            };

            string desc = baseDescription;
            if (!string.IsNullOrEmpty(desc))
                desc = char.ToUpper(desc[0]) + desc.Substring(1);

            return string.IsNullOrEmpty(rarityPrefix) ? desc : $"{rarityPrefix} {desc}";
        }

        public EquipmentInstance Reconstruct(SerializedEquipment data)
        {
            var entry = catalog.GetEntry(data.itemType);
            var weaponEntry = catalog.GetWeapon(data.itemType);

            int[] layerCodes, hiddenLayers, layerColorVariance;
            bool modular;
            string handType = null;
            string description = "";

            if (weaponEntry != null)
            {
                layerCodes = weaponEntry.LayerCodes;
                hiddenLayers = weaponEntry.HiddenLayers;
                layerColorVariance = weaponEntry.LayerColorVariance;
                modular = weaponEntry.Modular;
                handType = weaponEntry.HandType;
                description = weaponEntry.Description;
            }
            else if (entry != null)
            {
                layerCodes = entry.LayerCodes;
                hiddenLayers = entry.HiddenLayers;
                layerColorVariance = entry.LayerColorVariance;
                modular = entry.Modular;
                description = entry.Description;
            }
            else
            {
                layerCodes = Array.Empty<int>();
                hiddenLayers = Array.Empty<int>();
                layerColorVariance = Array.Empty<int>();
                modular = false;
            }

            var baseColor = new Color(data.baseColor[0], data.baseColor[1], data.baseColor[2], data.baseColor[3]);
            var varianceColors = new Dictionary<int, Color>();
            if (data.varianceColorKeys != null)
            {
                for (int i = 0; i < data.varianceColorKeys.Length; i++)
                {
                    int offset = i * 4;
                    varianceColors[data.varianceColorKeys[i]] = new Color(
                        data.varianceColorValuesFlat[offset],
                        data.varianceColorValuesFlat[offset + 1],
                        data.varianceColorValuesFlat[offset + 2],
                        data.varianceColorValuesFlat[offset + 3]);
                }
            }

            var statMods = new Stats
            {
                STR = data.statMods[0], DEX = data.statMods[1], CON = data.statMods[2],
                INT = data.statMods[3], WIS = data.statMods[4], CHA = data.statMods[5]
            };

            var affixes = new List<RolledAffix>();
            if (data.affixes != null)
            {
                foreach (var sa in data.affixes)
                {
                    Enum.TryParse<StatType>(sa.statType, out var st);
                    affixes.Add(new RolledAffix(sa.affixId, st, sa.value, sa.isPercentage));
                }
            }

            return new EquipmentInstance(
                data.itemType, data.itemNum, (EquipmentSlot)data.slot, (Rarity)data.rarity,
                baseColor, varianceColors, statMods, affixes,
                layerCodes, hiddenLayers, layerColorVariance,
                modular, handType, GenerateDisplayName((Rarity)data.rarity, description),
                data.layerVariants
            );
        }
    }
}
