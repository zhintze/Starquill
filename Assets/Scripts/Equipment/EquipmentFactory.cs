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
        private readonly AbilityTable abilityTable;
        private readonly ColorManager colors;

        private static readonly Dictionary<string, StatType[]> PrimaryStatPools = new()
        {
            { "hd", new[] { StatType.CON, StatType.WIS } },
            { "tr", new[] { StatType.CON, StatType.STR } },
            { "ar", new[] { StatType.STR, StatType.DEX } },
            { "lg", new[] { StatType.CON, StatType.STR } },
            { "fe", new[] { StatType.DEX, StatType.CON } },
            { "w", new[] { StatType.STR, StatType.DEX, StatType.INT } },
            { "w08", new[] { StatType.CON, StatType.STR } },
            { "mc", new[] { StatType.STR, StatType.DEX, StatType.CON, StatType.INT, StatType.WIS, StatType.CHA } },
        };

        private static readonly Dictionary<string, StatType[]> SecondaryStatPools = new()
        {
            { "hd", new[] { StatType.INT, StatType.CHA } },
            { "tr", new[] { StatType.WIS, StatType.DEX } },
            { "ar", new[] { StatType.CON, StatType.INT } },
            { "lg", new[] { StatType.DEX, StatType.WIS } },
            { "fe", new[] { StatType.STR, StatType.WIS } },
            { "w", new[] { StatType.CON, StatType.WIS, StatType.CHA } },
            { "w08", new[] { StatType.WIS, StatType.CHA } },
            { "mc", new[] { StatType.STR, StatType.DEX, StatType.CON, StatType.INT, StatType.WIS, StatType.CHA } },
        };

        public EquipmentFactory(EquipmentCatalog catalog, AbilityTable abilityTable, ColorManager colors)
        {
            this.catalog = catalog;
            this.abilityTable = abilityTable;
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

            var (primaryStat, primaryVal, secondaryStat, secondaryVal) =
                GenerateStatPair(prefix, rarity, rng);

            var abilityEntry = abilityTable.RollAbility(entry.ItemType, rng);
            var ability = abilityEntry != null
                ? AwakenedAbility.Create(abilityEntry, rarity) : null;

            var displayName = GenerateDisplayName(rarity, entry.Description);

            return new EquipmentInstance(
                entry.ItemType, itemNum, slot, rarity,
                baseColor, varianceColors,
                primaryStat, primaryVal, secondaryStat, secondaryVal, ability,
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
            string poolKey = IsShield(entry.ItemType) ? entry.ItemType : "w";
            var (primaryStat, primaryVal, secondaryStat, secondaryVal) =
                GenerateStatPair(poolKey, rarity, rng);

            var abilityEntry = abilityTable.RollAbility(entry.ItemType, rng);
            var ability = abilityEntry != null
                ? AwakenedAbility.Create(abilityEntry, rarity) : null;

            var displayName = GenerateDisplayName(rarity, entry.Description);

            return new EquipmentInstance(
                entry.ItemType, itemNum, slot, rarity,
                baseColor, varianceColors,
                primaryStat, primaryVal, secondaryStat, secondaryVal, ability,
                entry.LayerCodes, entry.HiddenLayers, entry.LayerColorVariance,
                entry.Modular, entry.HandType, displayName, layerVariants
            );
        }

        public EquipmentInstance[] CreateRandomLoadout(int questLevel, System.Random rng)
        {
            var loadout = new EquipmentInstance[11];

            loadout[(int)EquipmentSlot.Torso] = CreateRandom("tr", RollRarity(questLevel, rng), rng);
            loadout[(int)EquipmentSlot.Legs] = CreateRandom("lg", RollRarity(questLevel, rng), rng);

            if (rng.NextDouble() < 0.90)
                loadout[(int)EquipmentSlot.Head] = CreateRandom("hd", RollRarity(questLevel, rng), rng);
            if (rng.NextDouble() < 0.80)
                loadout[(int)EquipmentSlot.Arms] = CreateRandom("ar", RollRarity(questLevel, rng), rng);
            if (rng.NextDouble() < 0.70)
                loadout[(int)EquipmentSlot.Feet] = CreateRandom("fe", RollRarity(questLevel, rng), rng);

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
                                offhand.PrimaryStat, offhand.PrimaryValue,
                                offhand.SecondaryStat, offhand.SecondaryValue, offhand.Ability,
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

            loadout[(int)EquipmentSlot.Head] = CreateRandom("hd", Rarity.Common, rng);
            loadout[(int)EquipmentSlot.Torso] = CreateRandom("tr", Rarity.Common, rng);
            loadout[(int)EquipmentSlot.Arms] = CreateRandom("ar", Rarity.Common, rng);
            loadout[(int)EquipmentSlot.Legs] = CreateRandom("lg", Rarity.Common, rng);
            loadout[(int)EquipmentSlot.Feet] = CreateRandom("fe", Rarity.Common, rng);

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
                        offhand.PrimaryStat, offhand.PrimaryValue,
                        offhand.SecondaryStat, offhand.SecondaryValue, offhand.Ability,
                        offhand.LayerCodes, offhand.HiddenLayers, offhand.LayerColorVariance,
                        offhand.Modular, offhand.HandType, offhand.DisplayName
                    );
                }
            }

            loadout[(int)EquipmentSlot.Misc1] = CreateRandom("mc", Rarity.Common, rng);
            loadout[(int)EquipmentSlot.Misc2] = CreateRandom("mc", Rarity.Common, rng);

            return loadout;
        }

        public static (StatType primary, int primaryVal, StatType secondary, int secondaryVal)
            GenerateStatPair(string itemTypePrefix, Rarity rarity, System.Random rng)
        {
            string poolKey = GetPoolKey(itemTypePrefix);

            var primaryPool = PrimaryStatPools.ContainsKey(poolKey)
                ? PrimaryStatPools[poolKey] : PrimaryStatPools["mc"];
            var secondaryPool = SecondaryStatPools.ContainsKey(poolKey)
                ? SecondaryStatPools[poolKey] : SecondaryStatPools["mc"];

            var allStats = (StatType[])Enum.GetValues(typeof(StatType));
            StatType primary = rng.NextDouble() < 0.80
                ? primaryPool[rng.Next(primaryPool.Length)]
                : allStats[rng.Next(allStats.Length)];

            StatType secondary;
            int attempts = 0;
            do
            {
                secondary = rng.NextDouble() < 0.80
                    ? secondaryPool[rng.Next(secondaryPool.Length)]
                    : allStats[rng.Next(allStats.Length)];
                attempts++;
            } while (secondary == primary && attempts < 20);

            if (secondary == primary)
            {
                for (int i = 0; i < allStats.Length; i++)
                    if (allStats[i] != primary) { secondary = allStats[i]; break; }
            }

            var (minBudget, maxBudget) = rarity switch
            {
                Rarity.Common => (4, 6),
                Rarity.Uncommon => (7, 10),
                Rarity.Rare => (11, 14),
                Rarity.Epic => (15, 18),
                Rarity.Legendary => (19, 22),
                _ => (4, 6)
            };

            int budget = rng.Next(minBudget, maxBudget + 1);

            float primaryRatio = 0.65f + (float)(rng.NextDouble() * 0.10);
            int primaryVal = Math.Max(1, (int)Math.Round(budget * primaryRatio));
            int secondaryVal = Math.Max(1, budget - primaryVal);

            if (primaryVal <= secondaryVal)
                primaryVal = secondaryVal + 1;

            return (primary, primaryVal, secondary, secondaryVal);
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

        /// RollRarity with a minimum: results below the floor are raised to it.
        /// Used by quest rewards (elite/hard/boss guaranteed-quality drops).
        public static Rarity RollRarityWithFloor(int questLevel, Rarity? floor, System.Random rng)
        {
            var rolled = RollRarity(questLevel, rng);
            if (floor.HasValue && (int)rolled < (int)floor.Value)
                return floor.Value;
            return rolled;
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

            Enum.TryParse<StatType>(data.primaryStatType, out var primaryStat);
            Enum.TryParse<StatType>(data.secondaryStatType, out var secondaryStat);

            AwakenedAbility ability = null;
            if (data.ability != null)
            {
                var abilityEntry = abilityTable.AllAbilities.Find(a => a.Id == data.ability.abilityId);
                if (abilityEntry != null)
                {
                    var rarity = (Rarity)data.rarity;
                    ability = AwakenedAbility.Create(abilityEntry, rarity);
                    ability.Level = data.ability.level;
                    ability.CurrentXP = data.ability.currentXP;
                }
            }

            var lv = data.layerVariants;
            if (lv == null && modular && weaponEntry != null && weaponEntry.AmountPerLayer.Length > 0)
            {
                lv = new int[weaponEntry.AmountPerLayer.Length];
                for (int i = 0; i < lv.Length; i++)
                    lv[i] = 1;
            }
            else if (lv != null && weaponEntry != null && weaponEntry.AmountPerLayer.Length > 0)
            {
                // Migrate saves that predate weapon data corrections: clamp
                // variants (and array length) to the current per-layer counts
                // so stale rolls never request missing sprites.
                int len = Math.Min(lv.Length, weaponEntry.AmountPerLayer.Length);
                var clamped = new int[weaponEntry.AmountPerLayer.Length];
                for (int i = 0; i < clamped.Length; i++)
                {
                    int v = i < len ? lv[i] : 1;
                    clamped[i] = Math.Clamp(v, 1, Math.Max(1, weaponEntry.AmountPerLayer[i]));
                }
                lv = clamped;
            }

            return new EquipmentInstance(
                data.itemType, data.itemNum, (EquipmentSlot)data.slot, (Rarity)data.rarity,
                baseColor, varianceColors,
                primaryStat, data.primaryValue, secondaryStat, data.secondaryValue, ability,
                layerCodes, hiddenLayers, layerColorVariance,
                modular, handType, GenerateDisplayName((Rarity)data.rarity, description),
                lv
            );
        }

        private static bool IsShield(string itemType)
        {
            return itemType == "w08" || itemType == "w09";
        }

        private static string GetPoolKey(string itemTypePrefix)
        {
            if (itemTypePrefix == "w08" || itemTypePrefix == "w09") return "w08";
            if (itemTypePrefix.StartsWith("w")) return "w";
            if (itemTypePrefix.Length >= 2)
            {
                string prefix = itemTypePrefix.Substring(0, 2);
                if (PrimaryStatPools.ContainsKey(prefix)) return prefix;
            }
            return "mc";
        }
    }
}
