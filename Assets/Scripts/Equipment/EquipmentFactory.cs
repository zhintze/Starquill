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
            if (entry.Modular && entry.AmountPerLayer.Length > 0)
            {
                var layerVariants = new int[entry.AmountPerLayer.Length];
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
                entry.Modular, entry.HandType, displayName
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
                                offhand.Modular, offhand.HandType, offhand.DisplayName
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

        // Reconstruct is added in Task 10 after SerializedEquipment is defined
    }
}
