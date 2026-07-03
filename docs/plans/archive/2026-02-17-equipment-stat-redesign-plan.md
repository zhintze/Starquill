# Equipment Stat Model Redesign — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the 6-stat StatMods + RolledAffixes equipment system with a 2-stat pair + 1 awakened ability model, with per-tick ability XP leveling and gold accelerator.

**Architecture:** Equipment items get a primary/secondary stat pair (weighted by equipment type prefix) and a single passive ability that boosts one stat and levels up over time. `AffixTable`/`RolledAffix` are removed entirely, replaced by `AbilityTable`/`AwakenedAbility`. All abilities are passive stat boosts for MVP — triggered abilities are data-modeled but not combat-resolved.

**Tech Stack:** C# / Unity 6, NUnit EditMode tests, JSON data files, existing Equipment + Characters + Managers assemblies

**Design doc:** `docs/plans/2026-02-17-equipment-stat-redesign.md`

---

### Task 1: Create AbilityEntry and AbilityTable

**Files:**
- Create: `Assets/Scripts/Equipment/AbilityEntry.cs`
- Create: `Assets/Scripts/Equipment/AbilityTable.cs`
- Create: `Assets/Resources/Data/abilities.json`
- Create: `Assets/Tests/EditMode/Equipment/AbilityTableTests.cs`
- Delete: `Assets/Scripts/Equipment/AffixTable.cs`
- Delete: `Assets/Scripts/Equipment/RolledAffix.cs`
- Delete: `Assets/Resources/Data/affixes.json`
- Delete: `Assets/Tests/EditMode/Equipment/AffixTableTests.cs`

**Context:** `AbilityTable` replaces `AffixTable` as the data loader for equipment modifiers. It loads `abilities.json` and provides `RollAbility()` which picks a weighted-random ability filtered by equipment type prefix. `AbilityEntry` is the read-only catalog entry. `RolledAffix` and `AffixTable` are deleted.

**Step 1: Create `abilities.json`**

Create `Assets/Resources/Data/abilities.json`:

```json
[
  {
    "id": "iron_mind",
    "name": "Iron Mind",
    "description": "+{potency} INT while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "INT",
    "basePotency": 3.0,
    "potencyPerLevel": 2.0,
    "procChance": 1.0,
    "validTypes": ["hd"],
    "weight": 10
  },
  {
    "id": "ironhide",
    "name": "Ironhide",
    "description": "+{potency} CON while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "CON",
    "basePotency": 3.0,
    "potencyPerLevel": 2.0,
    "procChance": 1.0,
    "validTypes": ["tr"],
    "weight": 10
  },
  {
    "id": "rend",
    "name": "Rend",
    "description": "+{potency} STR while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "STR",
    "basePotency": 2.5,
    "potencyPerLevel": 1.5,
    "procChance": 1.0,
    "validTypes": ["ar"],
    "weight": 10
  },
  {
    "id": "arcane_grip",
    "name": "Arcane Grip",
    "description": "+{potency} INT while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "INT",
    "basePotency": 2.5,
    "potencyPerLevel": 1.5,
    "procChance": 1.0,
    "validTypes": ["ar"],
    "weight": 10
  },
  {
    "id": "fleet_foot",
    "name": "Fleet Foot",
    "description": "+{potency} DEX while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "DEX",
    "basePotency": 3.0,
    "potencyPerLevel": 2.0,
    "procChance": 1.0,
    "validTypes": ["lg"],
    "weight": 10
  },
  {
    "id": "steadfast",
    "name": "Steadfast",
    "description": "+{potency} CON while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "CON",
    "basePotency": 2.5,
    "potencyPerLevel": 1.5,
    "procChance": 1.0,
    "validTypes": ["lg"],
    "weight": 10
  },
  {
    "id": "sure_step",
    "name": "Sure Step",
    "description": "+{potency} DEX while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "DEX",
    "basePotency": 3.0,
    "potencyPerLevel": 2.0,
    "procChance": 1.0,
    "validTypes": ["fe"],
    "weight": 10
  },
  {
    "id": "bulwark",
    "name": "Bulwark",
    "description": "+{potency} CON while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "CON",
    "basePotency": 3.5,
    "potencyPerLevel": 2.5,
    "procChance": 1.0,
    "validTypes": ["w08", "w09"],
    "weight": 10
  },
  {
    "id": "keen_edge",
    "name": "Keen Edge",
    "description": "+{potency} STR while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "STR",
    "basePotency": 3.0,
    "potencyPerLevel": 2.0,
    "procChance": 1.0,
    "validTypes": ["w01", "w02", "w03", "w04", "w05", "w06", "w07"],
    "weight": 10
  },
  {
    "id": "arcane_edge",
    "name": "Arcane Edge",
    "description": "+{potency} INT while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "INT",
    "basePotency": 3.0,
    "potencyPerLevel": 2.0,
    "procChance": 1.0,
    "validTypes": ["w01", "w02", "w03", "w04", "w05", "w06", "w07"],
    "weight": 10
  },
  {
    "id": "vital_surge",
    "name": "Vital Surge",
    "description": "+{potency} CON while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "CON",
    "basePotency": 2.5,
    "potencyPerLevel": 1.5,
    "procChance": 1.0,
    "validTypes": ["mc"],
    "weight": 10
  },
  {
    "id": "fortune",
    "name": "Fortune",
    "description": "+{potency} CHA while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "CHA",
    "basePotency": 3.0,
    "potencyPerLevel": 2.0,
    "procChance": 1.0,
    "validTypes": ["mc"],
    "weight": 10
  },
  {
    "id": "resonance",
    "name": "Resonance",
    "description": "+{potency} WIS while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "WIS",
    "basePotency": 3.0,
    "potencyPerLevel": 2.0,
    "procChance": 1.0,
    "validTypes": ["mc"],
    "weight": 10
  },
  {
    "id": "siphon",
    "name": "Siphon",
    "description": "+{potency} STR while equipped",
    "triggerType": "Passive",
    "effectType": "StatBoost",
    "boostedStat": "STR",
    "basePotency": 2.5,
    "potencyPerLevel": 1.5,
    "procChance": 1.0,
    "validTypes": ["mc"],
    "weight": 10
  }
]
```

**Step 2: Create `AbilityEntry.cs`**

Create `Assets/Scripts/Equipment/AbilityEntry.cs`:

```csharp
using System.Collections.Generic;
using Starquill.Core;

namespace Starquill.Equipment
{
    public class AbilityEntry
    {
        public string Id;
        public string Name;
        public string Description;
        public string TriggerType;
        public string EffectType;
        public StatType BoostedStat;
        public float BasePotency;
        public float PotencyPerLevel;
        public float ProcChance;
        public List<string> ValidTypes = new();
        public int Weight;
    }
}
```

**Step 3: Create `AbilityTable.cs`**

Create `Assets/Scripts/Equipment/AbilityTable.cs`:

```csharp
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
                    ProcChance = obj.GetFloat("procChance"),
                    BasePotency = obj.GetFloat("basePotency"),
                    PotencyPerLevel = obj.GetFloat("potencyPerLevel"),
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
            foreach (var a in pool) totalWeight += a.Weight;

            int roll = rng.Next(totalWeight);
            int cumulative = 0;
            foreach (var a in pool)
            {
                cumulative += a.Weight;
                if (roll < cumulative) return a;
            }
            return pool[pool.Count - 1];
        }
    }
}
```

**Step 4: Delete old affix files**

Delete these files (and their .meta files):
- `Assets/Scripts/Equipment/AffixTable.cs`
- `Assets/Scripts/Equipment/RolledAffix.cs`
- `Assets/Resources/Data/affixes.json`
- `Assets/Tests/EditMode/Equipment/AffixTableTests.cs`

**Step 5: Write tests for AbilityTable**

Create `Assets/Tests/EditMode/Equipment/AbilityTableTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    [TestFixture]
    public class AbilityTableTests
    {
        private AbilityTable table;

        [SetUp]
        public void SetUp()
        {
            table = new AbilityTable();
            var asset = Resources.Load<TextAsset>("Data/abilities");
            Assert.IsNotNull(asset, "abilities.json not found in Resources");
            table.LoadFromJson(asset.text);
        }

        [Test]
        public void LoadFromJson_ParsesAll14Abilities()
        {
            Assert.AreEqual(14, table.AllAbilities.Count);
        }

        [Test]
        public void GetValidAbilities_ForTorso_ReturnsIronhide()
        {
            var valid = table.GetValidAbilities("tr01");
            Assert.AreEqual(1, valid.Count);
            Assert.AreEqual("ironhide", valid[0].Id);
        }

        [Test]
        public void GetValidAbilities_ForAccessory_ReturnsFour()
        {
            var valid = table.GetValidAbilities("mc01");
            Assert.AreEqual(4, valid.Count);
        }

        [Test]
        public void GetValidAbilities_ForWeapon_ReturnsTwoNonShield()
        {
            var valid = table.GetValidAbilities("w01");
            Assert.AreEqual(2, valid.Count);
        }

        [Test]
        public void GetValidAbilities_ForShield_ReturnsBulwark()
        {
            var valid = table.GetValidAbilities("w08");
            Assert.AreEqual(1, valid.Count);
            Assert.AreEqual("bulwark", valid[0].Id);
        }

        [Test]
        public void RollAbility_ReturnsValidEntry()
        {
            var rng = new System.Random(42);
            var entry = table.RollAbility("tr01", rng);
            Assert.IsNotNull(entry);
            Assert.AreEqual("ironhide", entry.Id);
        }

        [Test]
        public void RollAbility_ForAccessory_ProducesVariety()
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < 100; i++)
            {
                var entry = table.RollAbility("mc01", new System.Random(i));
                if (entry != null) ids.Add(entry.Id);
            }
            Assert.Greater(ids.Count, 1, "Accessories should roll different abilities");
        }

        [Test]
        public void AllAbilities_HaveValidBoostedStat()
        {
            foreach (var a in table.AllAbilities)
                Assert.IsTrue(System.Enum.IsDefined(typeof(StatType), a.BoostedStat),
                    $"Ability {a.Id} has invalid stat {a.BoostedStat}");
        }

        [Test]
        public void AllAbilities_HavePositivePotency()
        {
            foreach (var a in table.AllAbilities)
            {
                Assert.Greater(a.BasePotency, 0f, $"{a.Id} basePotency");
                Assert.Greater(a.PotencyPerLevel, 0f, $"{a.Id} potencyPerLevel");
            }
        }
    }
}
```

**Step 6: Commit**

```
feat: add AbilityTable + abilities.json, remove AffixTable + affixes
```

---

### Task 2: Create AwakenedAbility

**Files:**
- Create: `Assets/Scripts/Equipment/AwakenedAbility.cs`
- Create: `Assets/Tests/EditMode/Equipment/AwakenedAbilityTests.cs`

**Context:** `AwakenedAbility` is the runtime ability instance stored on each `EquipmentInstance`. It holds mutable state (level, XP) and computes current potency. It does NOT store reference data (name, description) — those come from `AbilityEntry` via `AbilityTable`.

**Step 1: Create `AwakenedAbility.cs`**

Create `Assets/Scripts/Equipment/AwakenedAbility.cs`:

```csharp
using System;
using Starquill.Core;

namespace Starquill.Equipment
{
    [Serializable]
    public class AwakenedAbility
    {
        public string AbilityId;
        public StatType BoostedStat;
        public float BasePotency;
        public float PotencyPerLevel;
        public int Level;
        public int MaxLevel;
        public float CurrentXP;

        public float CurrentPotency => BasePotency + (Level - 1) * PotencyPerLevel;

        public float XPToNextLevel(float baseThreshold, float growthRate)
        {
            return baseThreshold * (float)Math.Pow(growthRate, Level - 1);
        }

        public bool CanLevelUp(float baseThreshold, float growthRate)
        {
            return Level < MaxLevel && CurrentXP >= XPToNextLevel(baseThreshold, growthRate);
        }

        public bool TryLevelUp(float baseThreshold, float growthRate)
        {
            if (!CanLevelUp(baseThreshold, growthRate)) return false;
            CurrentXP -= XPToNextLevel(baseThreshold, growthRate);
            Level++;
            return true;
        }

        public void AddXP(float amount, float baseThreshold, float growthRate)
        {
            if (Level >= MaxLevel) return;
            CurrentXP += amount;
            while (CanLevelUp(baseThreshold, growthRate))
                TryLevelUp(baseThreshold, growthRate);
        }

        public float GoldCostToLevel(float baseCost, float costGrowthRate, float rarityMultiplier)
        {
            if (Level >= MaxLevel) return float.MaxValue;
            return baseCost * (float)Math.Pow(costGrowthRate, Level - 1) * rarityMultiplier;
        }

        public static int MaxLevelForRarity(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => 2,
                Rarity.Uncommon => 3,
                Rarity.Rare => 4,
                Rarity.Epic => 5,
                Rarity.Legendary => 7,
                _ => 2
            };
        }

        public static float PotencyModifierForRarity(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => 0.8f,
                Rarity.Uncommon => 1.0f,
                Rarity.Rare => 1.2f,
                Rarity.Epic => 1.5f,
                Rarity.Legendary => 2.0f,
                _ => 1.0f
            };
        }

        public static AwakenedAbility Create(AbilityEntry entry, Rarity rarity)
        {
            return new AwakenedAbility
            {
                AbilityId = entry.Id,
                BoostedStat = entry.BoostedStat,
                BasePotency = entry.BasePotency * PotencyModifierForRarity(rarity),
                PotencyPerLevel = entry.PotencyPerLevel * PotencyModifierForRarity(rarity),
                Level = 1,
                MaxLevel = MaxLevelForRarity(rarity),
                CurrentXP = 0f
            };
        }
    }
}
```

**Step 2: Write tests**

Create `Assets/Tests/EditMode/Equipment/AwakenedAbilityTests.cs`:

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.Tests.Equipment
{
    [TestFixture]
    public class AwakenedAbilityTests
    {
        private AwakenedAbility MakeAbility(int level = 1, int maxLevel = 3,
            float basePotency = 3f, float potencyPerLevel = 2f, float currentXP = 0f)
        {
            return new AwakenedAbility
            {
                AbilityId = "test",
                BoostedStat = StatType.CON,
                BasePotency = basePotency,
                PotencyPerLevel = potencyPerLevel,
                Level = level,
                MaxLevel = maxLevel,
                CurrentXP = currentXP
            };
        }

        [Test]
        public void CurrentPotency_Level1_ReturnsBase()
        {
            var ability = MakeAbility(level: 1, basePotency: 3f);
            Assert.AreEqual(3f, ability.CurrentPotency, 0.01f);
        }

        [Test]
        public void CurrentPotency_Level3_ScalesCorrectly()
        {
            var ability = MakeAbility(level: 3, basePotency: 3f, potencyPerLevel: 2f);
            // 3 + (3-1)*2 = 7
            Assert.AreEqual(7f, ability.CurrentPotency, 0.01f);
        }

        [Test]
        public void XPToNextLevel_Level1_ReturnsBaseThreshold()
        {
            var ability = MakeAbility(level: 1);
            Assert.AreEqual(100f, ability.XPToNextLevel(100f, 1.8f), 0.01f);
        }

        [Test]
        public void XPToNextLevel_Level2_ScalesExponentially()
        {
            var ability = MakeAbility(level: 2);
            // 100 * 1.8^(2-1) = 180
            Assert.AreEqual(180f, ability.XPToNextLevel(100f, 1.8f), 0.01f);
        }

        [Test]
        public void TryLevelUp_WithEnoughXP_LevelsUp()
        {
            var ability = MakeAbility(level: 1, maxLevel: 3, currentXP: 100f);
            bool result = ability.TryLevelUp(100f, 1.8f);
            Assert.IsTrue(result);
            Assert.AreEqual(2, ability.Level);
            Assert.AreEqual(0f, ability.CurrentXP, 0.01f);
        }

        [Test]
        public void TryLevelUp_AtMaxLevel_ReturnsFalse()
        {
            var ability = MakeAbility(level: 3, maxLevel: 3, currentXP: 999f);
            bool result = ability.TryLevelUp(100f, 1.8f);
            Assert.IsFalse(result);
            Assert.AreEqual(3, ability.Level);
        }

        [Test]
        public void AddXP_AutoLevelsWhenThresholdReached()
        {
            var ability = MakeAbility(level: 1, maxLevel: 5, currentXP: 0f);
            ability.AddXP(300f, 100f, 1.8f);
            // XP needed: L1->L2 = 100, L2->L3 = 180. Total 280. 300-280 = 20 leftover
            Assert.AreEqual(3, ability.Level);
            Assert.AreEqual(20f, ability.CurrentXP, 0.01f);
        }

        [Test]
        public void AddXP_AtMaxLevel_DoesNothing()
        {
            var ability = MakeAbility(level: 3, maxLevel: 3, currentXP: 0f);
            ability.AddXP(999f, 100f, 1.8f);
            Assert.AreEqual(3, ability.Level);
            Assert.AreEqual(0f, ability.CurrentXP, 0.01f);
        }

        [Test]
        public void GoldCostToLevel_ScalesExponentially()
        {
            var ability = MakeAbility(level: 1);
            float cost1 = ability.GoldCostToLevel(100f, 2.0f, 1.0f);
            Assert.AreEqual(100f, cost1, 0.01f);

            ability.Level = 2;
            float cost2 = ability.GoldCostToLevel(100f, 2.0f, 1.0f);
            Assert.AreEqual(200f, cost2, 0.01f);
        }

        [Test]
        public void GoldCostToLevel_AtMaxLevel_ReturnsMaxValue()
        {
            var ability = MakeAbility(level: 3, maxLevel: 3);
            float cost = ability.GoldCostToLevel(100f, 2.0f, 1.0f);
            Assert.AreEqual(float.MaxValue, cost);
        }

        [Test]
        public void GoldCostToLevel_AppliesRarityMultiplier()
        {
            var ability = MakeAbility(level: 1);
            float baseCost = ability.GoldCostToLevel(100f, 2.0f, 1.0f);
            float epicCost = ability.GoldCostToLevel(100f, 2.0f, 2.5f);
            Assert.AreEqual(baseCost * 2.5f, epicCost, 0.01f);
        }

        [Test]
        public void Create_AppliesRarityModifiers()
        {
            var entry = new AbilityEntry
            {
                Id = "test", BoostedStat = StatType.STR,
                BasePotency = 3f, PotencyPerLevel = 2f
            };
            var common = AwakenedAbility.Create(entry, Rarity.Common);
            var legendary = AwakenedAbility.Create(entry, Rarity.Legendary);

            Assert.AreEqual(3f * 0.8f, common.BasePotency, 0.01f);
            Assert.AreEqual(2, common.MaxLevel);
            Assert.AreEqual(3f * 2.0f, legendary.BasePotency, 0.01f);
            Assert.AreEqual(7, legendary.MaxLevel);
        }

        [Test]
        public void MaxLevelForRarity_AllRaritiesHaveValues()
        {
            Assert.AreEqual(2, AwakenedAbility.MaxLevelForRarity(Rarity.Common));
            Assert.AreEqual(3, AwakenedAbility.MaxLevelForRarity(Rarity.Uncommon));
            Assert.AreEqual(4, AwakenedAbility.MaxLevelForRarity(Rarity.Rare));
            Assert.AreEqual(5, AwakenedAbility.MaxLevelForRarity(Rarity.Epic));
            Assert.AreEqual(7, AwakenedAbility.MaxLevelForRarity(Rarity.Legendary));
        }
    }
}
```

**Step 3: Commit**

```
feat: add AwakenedAbility with leveling, XP, and gold cost
```

---

### Task 3: Rewrite EquipmentInstance for Stat Pairs + Ability

**Files:**
- Modify: `Assets/Scripts/Equipment/EquipmentInstance.cs`
- Rewrite: `Assets/Tests/EditMode/Equipment/EquipmentInstanceTests.cs`

**Context:** Replace `Stats StatMods` and `IReadOnlyList<RolledAffix> RolledAffixes` with `PrimaryStat`/`PrimaryValue`/`SecondaryStat`/`SecondaryValue` and `AwakenedAbility Ability`. Update `GetTotalStatMods()` to build a `Stats` from the pair + ability potency.

**Step 1: Rewrite `EquipmentInstance.cs`**

Replace the full file. Key changes:
- Constructor takes `StatType primaryStat, int primaryValue, StatType secondaryStat, int secondaryValue, AwakenedAbility ability` instead of `Stats statMods, List<RolledAffix> rolledAffixes`
- `GetTotalStatMods()` builds a `Stats` from the two stat values plus the ability's `CurrentPotency` added to the ability's `BoostedStat`
- Remove all references to `StatMods`, `RolledAffixes`

```csharp
using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;
using UnityEngine;

namespace Starquill.Equipment
{
    public class EquipmentInstance
    {
        public string ItemType { get; }
        public int ItemNum { get; }
        public EquipmentSlot Slot { get; }
        public Rarity Rarity { get; }
        public Color BaseColor { get; }
        public IReadOnlyDictionary<int, Color> VarianceColors { get; }
        public StatType PrimaryStat { get; }
        public int PrimaryValue { get; }
        public StatType SecondaryStat { get; }
        public int SecondaryValue { get; }
        public AwakenedAbility Ability { get; }
        public int[] LayerCodes { get; }
        public int[] HiddenLayers { get; }
        public int[] LayerColorVariance { get; }
        public bool Modular { get; }
        public string HandType { get; }
        public string DisplayName { get; }
        public int[] LayerVariants { get; }

        public EquipmentInstance(
            string itemType, int itemNum, EquipmentSlot slot, Rarity rarity,
            Color baseColor, Dictionary<int, Color> varianceColors,
            StatType primaryStat, int primaryValue,
            StatType secondaryStat, int secondaryValue,
            AwakenedAbility ability,
            int[] layerCodes, int[] hiddenLayers, int[] layerColorVariance,
            bool modular, string handType, string displayName,
            int[] layerVariants = null)
        {
            ItemType = itemType;
            ItemNum = itemNum;
            Slot = slot;
            Rarity = rarity;
            BaseColor = baseColor;
            VarianceColors = varianceColors ?? new Dictionary<int, Color>();
            PrimaryStat = primaryStat;
            PrimaryValue = primaryValue;
            SecondaryStat = secondaryStat;
            SecondaryValue = secondaryValue;
            Ability = ability;
            LayerCodes = layerCodes ?? Array.Empty<int>();
            HiddenLayers = hiddenLayers ?? Array.Empty<int>();
            LayerColorVariance = layerColorVariance ?? Array.Empty<int>();
            Modular = modular;
            HandType = handType;
            DisplayName = displayName ?? "";
            LayerVariants = layerVariants;
        }

        public Stats GetTotalStatMods()
        {
            var stats = new Stats();
            stats.SetStat(PrimaryStat, PrimaryValue);
            stats.SetStat(SecondaryStat,
                stats.GetStat(SecondaryStat) + SecondaryValue);
            if (Ability != null)
            {
                int current = stats.GetStat(Ability.BoostedStat);
                stats.SetStat(Ability.BoostedStat, current + (int)Ability.CurrentPotency);
            }
            return stats;
        }
    }
}
```

**Step 2: Rewrite tests**

Replace `Assets/Tests/EditMode/Equipment/EquipmentInstanceTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    [TestFixture]
    public class EquipmentInstanceTests
    {
        private AwakenedAbility MakeAbility(StatType stat = StatType.CON,
            float basePotency = 3f, int level = 1)
        {
            return new AwakenedAbility
            {
                AbilityId = "test",
                BoostedStat = stat,
                BasePotency = basePotency,
                PotencyPerLevel = 2f,
                Level = level,
                MaxLevel = 3,
                CurrentXP = 0f
            };
        }

        [Test]
        public void Constructor_SetsAllFields()
        {
            var ability = MakeAbility();
            var instance = new EquipmentInstance(
                itemType: "tr03", itemNum: 5,
                slot: EquipmentSlot.Torso, rarity: Rarity.Rare,
                baseColor: Color.red,
                varianceColors: new Dictionary<int, Color> { { 48, Color.blue } },
                primaryStat: StatType.CON, primaryValue: 8,
                secondaryStat: StatType.STR, secondaryValue: 3,
                ability: ability,
                layerCodes: new[] { 48 },
                hiddenLayers: new int[0],
                layerColorVariance: new int[0],
                modular: false, handType: null,
                displayName: "Rare Sleeveless Shirt"
            );

            Assert.AreEqual("tr03", instance.ItemType);
            Assert.AreEqual(5, instance.ItemNum);
            Assert.AreEqual(EquipmentSlot.Torso, instance.Slot);
            Assert.AreEqual(Rarity.Rare, instance.Rarity);
            Assert.AreEqual(StatType.CON, instance.PrimaryStat);
            Assert.AreEqual(8, instance.PrimaryValue);
            Assert.AreEqual(StatType.STR, instance.SecondaryStat);
            Assert.AreEqual(3, instance.SecondaryValue);
            Assert.AreEqual("Rare Sleeveless Shirt", instance.DisplayName);
        }

        [Test]
        public void GetTotalStatMods_IncludesStatPair()
        {
            var instance = new EquipmentInstance(
                "tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                StatType.CON, 8, StatType.STR, 3, null,
                new[] { 48 }, new int[0], new int[0],
                false, null, "Test"
            );
            var total = instance.GetTotalStatMods();
            Assert.AreEqual(8, total.CON);
            Assert.AreEqual(3, total.STR);
        }

        [Test]
        public void GetTotalStatMods_IncludesAbilityPotency()
        {
            var ability = MakeAbility(StatType.CON, basePotency: 3f, level: 1);
            var instance = new EquipmentInstance(
                "tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                StatType.CON, 8, StatType.STR, 3, ability,
                new[] { 48 }, new int[0], new int[0],
                false, null, "Test"
            );
            var total = instance.GetTotalStatMods();
            Assert.AreEqual(11, total.CON); // 8 + 3 from ability
            Assert.AreEqual(3, total.STR);
        }

        [Test]
        public void GetTotalStatMods_AbilityOnDifferentStat()
        {
            var ability = MakeAbility(StatType.DEX, basePotency: 5f, level: 1);
            var instance = new EquipmentInstance(
                "tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                StatType.CON, 8, StatType.STR, 3, ability,
                new[] { 48 }, new int[0], new int[0],
                false, null, "Test"
            );
            var total = instance.GetTotalStatMods();
            Assert.AreEqual(8, total.CON);
            Assert.AreEqual(3, total.STR);
            Assert.AreEqual(5, total.DEX);
        }

        [Test]
        public void GetTotalStatMods_AbilityLevelScalesPotency()
        {
            // basePotency 3, potencyPerLevel 2, level 3 => 3 + (3-1)*2 = 7
            var ability = MakeAbility(StatType.CON, basePotency: 3f, level: 3);
            var instance = new EquipmentInstance(
                "tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                StatType.CON, 8, StatType.STR, 3, ability,
                new[] { 48 }, new int[0], new int[0],
                false, null, "Test"
            );
            var total = instance.GetTotalStatMods();
            Assert.AreEqual(15, total.CON); // 8 + 7
        }
    }
}
```

**Step 3: Commit**

```
feat: rewrite EquipmentInstance with stat pairs + awakened ability
```

---

### Task 4: Update SerializedEquipment

**Files:**
- Modify: `Assets/Scripts/Equipment/SerializedEquipment.cs`

**Context:** Replace `int[] statMods` and `SerializedAffix[]` with stat pair fields and `SerializedAbility`. Remove `SerializedAffix` class. No backward compatibility needed.

**Step 1: Rewrite `SerializedEquipment.cs`**

```csharp
using System;
using System.Collections.Generic;
using Starquill.Core;
using UnityEngine;

namespace Starquill.Equipment
{
    [Serializable]
    public class SerializedEquipment
    {
        public string itemType;
        public int itemNum;
        public int slot;
        public int rarity;
        public float[] baseColor = new float[4];
        public int[] varianceColorKeys;
        public float[] varianceColorValuesFlat;
        public string primaryStatType;
        public int primaryValue;
        public string secondaryStatType;
        public int secondaryValue;
        public SerializedAbility ability;
        public int[] layerVariants;

        public static SerializedEquipment FromInstance(EquipmentInstance e)
        {
            var se = new SerializedEquipment
            {
                itemType = e.ItemType,
                itemNum = e.ItemNum,
                slot = (int)e.Slot,
                rarity = (int)e.Rarity,
                baseColor = new[] { e.BaseColor.r, e.BaseColor.g, e.BaseColor.b, e.BaseColor.a },
                primaryStatType = e.PrimaryStat.ToString(),
                primaryValue = e.PrimaryValue,
                secondaryStatType = e.SecondaryStat.ToString(),
                secondaryValue = e.SecondaryValue,
                layerVariants = e.LayerVariants
            };

            var vcDict = e.VarianceColors;
            if (vcDict != null && vcDict.Count > 0)
            {
                se.varianceColorKeys = new int[vcDict.Count];
                se.varianceColorValuesFlat = new float[vcDict.Count * 4];
                int i = 0;
                foreach (var kvp in vcDict)
                {
                    se.varianceColorKeys[i] = kvp.Key;
                    se.varianceColorValuesFlat[i * 4] = kvp.Value.r;
                    se.varianceColorValuesFlat[i * 4 + 1] = kvp.Value.g;
                    se.varianceColorValuesFlat[i * 4 + 2] = kvp.Value.b;
                    se.varianceColorValuesFlat[i * 4 + 3] = kvp.Value.a;
                    i++;
                }
            }

            if (e.Ability != null)
            {
                se.ability = new SerializedAbility
                {
                    abilityId = e.Ability.AbilityId,
                    level = e.Ability.Level,
                    currentXP = e.Ability.CurrentXP
                };
            }

            return se;
        }
    }

    [Serializable]
    public class SerializedAbility
    {
        public string abilityId;
        public int level;
        public float currentXP;
    }
}
```

**Step 2: Commit**

```
feat: update SerializedEquipment for stat pairs + ability
```

---

### Task 5: Rewrite EquipmentFactory

**Files:**
- Modify: `Assets/Scripts/Equipment/EquipmentFactory.cs`
- Rewrite: `Assets/Tests/EditMode/Equipment/EquipmentFactoryTests.cs`

**Context:** Replace `GenerateBaseStats()` with `GenerateStatPair()`. Replace affix rolling with ability rolling. Constructor takes `AbilityTable` instead of `AffixTable`. Update `Reconstruct()` to read new serialized format. Update all `CreateRandom`/`CreateRandomWeapon`/`CreateRandomLoadout`/`CreateStarterLoadout` methods. Update all callers.

**Step 1: Rewrite `EquipmentFactory.cs`**

Key changes to the factory:
- Constructor: `EquipmentFactory(EquipmentCatalog catalog, AbilityTable abilityTable, ColorManager colors)`
- New static method `GenerateStatPair(string itemTypePrefix, Rarity rarity, Random rng)` returns `(StatType primary, int primaryVal, StatType secondary, int secondaryVal)`
- Stat pair pools are defined as static dictionaries keyed by prefix pattern
- `CreateRandom()` and `CreateRandomWeapon()` call `GenerateStatPair()` + `abilityTable.RollAbility()` + `AwakenedAbility.Create()`
- `Reconstruct()` reads new serialized fields, looks up `AbilityEntry` by ID to get `PotencyPerLevel`/`BoostedStat`, creates `AwakenedAbility` with saved level/XP

Full replacement file:

```csharp
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

            // Use weapon item type for stat pool lookup (shields vs weapons)
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

            // 80% chance from weighted pool, 20% from any stat
            var allStats = (StatType[])Enum.GetValues(typeof(StatType));
            StatType primary = rng.NextDouble() < 0.80
                ? primaryPool[rng.Next(primaryPool.Length)]
                : allStats[rng.Next(allStats.Length)];

            // Secondary must differ from primary
            StatType secondary;
            int attempts = 0;
            do
            {
                secondary = rng.NextDouble() < 0.80
                    ? secondaryPool[rng.Next(secondaryPool.Length)]
                    : allStats[rng.Next(allStats.Length)];
                attempts++;
            } while (secondary == primary && attempts < 20);

            // Fallback if all attempts matched
            if (secondary == primary)
            {
                for (int i = 0; i < allStats.Length; i++)
                    if (allStats[i] != primary) { secondary = allStats[i]; break; }
            }

            // Budget by rarity
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

            // Split 65-75% primary, 25-35% secondary
            float primaryRatio = 0.65f + (float)(rng.NextDouble() * 0.10);
            int primaryVal = Math.Max(1, (int)Math.Round(budget * primaryRatio));
            int secondaryVal = Math.Max(1, budget - primaryVal);

            // Ensure primary > secondary
            if (primaryVal <= secondaryVal)
            {
                primaryVal = secondaryVal + 1;
            }

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
            // Shield item types match exactly
            if (itemTypePrefix == "w08" || itemTypePrefix == "w09") return "w08";
            // Weapon prefixes (w01-w07) use "w" key
            if (itemTypePrefix.StartsWith("w")) return "w";
            // Armor/accessory prefixes: extract the 2-letter prefix
            if (itemTypePrefix.Length >= 2)
            {
                string prefix = itemTypePrefix.Substring(0, 2);
                if (PrimaryStatPools.ContainsKey(prefix)) return prefix;
            }
            return "mc";
        }
    }
}
```

Note: Add shield stat pools to the static dictionaries — `w08` key:

Add these entries to `PrimaryStatPools` and `SecondaryStatPools`:
```csharp
{ "w08", new[] { StatType.CON, StatType.STR } },
```
```csharp
{ "w08", new[] { StatType.WIS, StatType.CHA } },
```

**Step 2: Rewrite `EquipmentFactoryTests.cs`**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Data;
using Starquill.Display;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    [TestFixture]
    public class EquipmentFactoryTests
    {
        private EquipmentFactory factory;
        private EquipmentCatalog catalog;
        private AbilityTable abilityTable;
        private ColorManager colors;

        [SetUp]
        public void SetUp()
        {
            catalog = new EquipmentCatalog();
            catalog.LoadFromResources();

            abilityTable = new AbilityTable();
            abilityTable.LoadFromResources();

            colors = new ColorManager();
            colors.LoadFromResources();

            factory = new EquipmentFactory(catalog, abilityTable, colors);
        }

        [Test]
        public void CreateRandom_ProducesValidInstance()
        {
            var rng = new System.Random(42);
            var instance = factory.CreateRandom("tr", Rarity.Common, rng);
            Assert.IsNotNull(instance);
            Assert.IsTrue(instance.ItemType.StartsWith("tr"));
            Assert.AreEqual(Rarity.Common, instance.Rarity);
            Assert.Greater(instance.ItemNum, 0);
        }

        [Test]
        public void CreateRandom_HasStatPair()
        {
            var rng = new System.Random(42);
            var instance = factory.CreateRandom("tr", Rarity.Rare, rng);
            Assert.Greater(instance.PrimaryValue, 0);
            Assert.Greater(instance.SecondaryValue, 0);
            Assert.AreNotEqual(instance.PrimaryStat, instance.SecondaryStat);
        }

        [Test]
        public void CreateRandom_PrimaryAlwaysGreaterThanSecondary()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var rng = new System.Random(seed);
                var instance = factory.CreateRandom("tr", Rarity.Rare, rng);
                Assert.Greater(instance.PrimaryValue, instance.SecondaryValue,
                    $"Seed {seed}: primary {instance.PrimaryValue} <= secondary {instance.SecondaryValue}");
            }
        }

        [Test]
        public void CreateRandom_HasAbility()
        {
            var rng = new System.Random(42);
            var instance = factory.CreateRandom("tr", Rarity.Rare, rng);
            Assert.IsNotNull(instance.Ability);
            Assert.AreEqual(1, instance.Ability.Level);
            Assert.AreEqual("ironhide", instance.Ability.AbilityId);
        }

        [Test]
        public void CreateRandom_SetsSlotFromPrefix()
        {
            var rng = new System.Random(42);
            Assert.AreEqual(EquipmentSlot.Torso, factory.CreateRandom("tr", Rarity.Common, rng).Slot);
            Assert.AreEqual(EquipmentSlot.Head, factory.CreateRandom("hd", Rarity.Common, rng).Slot);
            Assert.AreEqual(EquipmentSlot.Arms, factory.CreateRandom("ar", Rarity.Common, rng).Slot);
        }

        [Test]
        public void CreateRandomWeapon_SetsHandType()
        {
            var rng = new System.Random(42);
            var weapon = factory.CreateRandomWeapon(Rarity.Common, rng);
            Assert.IsNotNull(weapon);
            Assert.IsTrue(weapon.ItemType.StartsWith("w"));
            Assert.IsTrue(weapon.HandType == "one_handed" || weapon.HandType == "two_handed");
        }

        [Test]
        public void CreateRandomWeapon_HasAbility()
        {
            var rng = new System.Random(42);
            var weapon = factory.CreateRandomWeapon(Rarity.Rare, rng);
            Assert.IsNotNull(weapon.Ability);
        }

        [Test]
        public void GenerateStatPair_BudgetWithinRarityRange()
        {
            var rng = new System.Random(42);
            for (int seed = 0; seed < 50; seed++)
            {
                var (_, pv, _, sv) = EquipmentFactory.GenerateStatPair("tr", Rarity.Rare, new System.Random(seed));
                int total = pv + sv;
                Assert.GreaterOrEqual(total, 11, $"Seed {seed}: budget {total} < 11");
                Assert.LessOrEqual(total, 14, $"Seed {seed}: budget {total} > 14");
            }
        }

        [Test]
        public void GenerateStatPair_StatsMustDiffer()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var (ps, _, ss, _) = EquipmentFactory.GenerateStatPair("tr", Rarity.Common, new System.Random(seed));
                Assert.AreNotEqual(ps, ss, $"Seed {seed}: primary == secondary ({ps})");
            }
        }

        [Test]
        public void CreateRandomLoadout_FillsPrioritySlots()
        {
            var rng = new System.Random(42);
            var loadout = factory.CreateRandomLoadout(1, rng);
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Torso], "Torso should always be filled");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Legs], "Legs should always be filled");
        }

        [Test]
        public void CreateStarterLoadout_FillsAllCoreSlots()
        {
            var loadout = factory.CreateStarterLoadout(new System.Random(42));
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Head], "Head");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Torso], "Torso");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Arms], "Arms");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Legs], "Legs");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Feet], "Feet");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.MainHand], "MainHand");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Misc1], "Misc1");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Misc2], "Misc2");
        }

        [Test]
        public void CreateStarterLoadout_AllCommonRarity()
        {
            var loadout = factory.CreateStarterLoadout(new System.Random(42));
            for (int i = 0; i < loadout.Length; i++)
                if (loadout[i] != null)
                    Assert.AreEqual(Rarity.Common, loadout[i].Rarity, $"Slot {i}");
        }

        [Test]
        public void RollRarity_AtLevel1_MostlyCommon()
        {
            var rng = new System.Random(42);
            int commonCount = 0;
            for (int i = 0; i < 100; i++)
                if (EquipmentFactory.RollRarity(1, rng) == Rarity.Common) commonCount++;
            Assert.Greater(commonCount, 60, "At level 1, >60% should be Common");
        }

        [Test]
        public void GenerateDisplayName_IncludesRarity()
        {
            var name = EquipmentFactory.GenerateDisplayName(Rarity.Rare, "shirt");
            Assert.IsTrue(name.Contains("Rare"), $"Name '{name}' should contain 'Rare'");
        }
    }
}
```

**Step 3: Commit**

```
feat: rewrite EquipmentFactory with stat pairs + ability rolling
```

---

### Task 6: Update ItemComparer and SellCalculator

**Files:**
- Modify: `Assets/Scripts/Equipment/ItemComparer.cs`
- Modify: `Assets/Scripts/Equipment/SellCalculator.cs`
- Rewrite: `Assets/Tests/EditMode/Equipment/ItemComparerTests.cs`
- Rewrite: `Assets/Tests/EditMode/Equipment/SellCalculatorTests.cs`

**Context:** `ItemComparer` simplifies to `primaryValue + secondaryValue` scoring. `StatDiff` gets per-stat deltas. `SellCalculator` uses ability level instead of affix count.

**Step 1: Rewrite `ItemComparer.cs`**

```csharp
using System.Collections.Generic;
using Starquill.Core;

namespace Starquill.Equipment
{
    public struct StatDiff
    {
        public StatType PrimaryStat;
        public int PrimaryDelta;
        public StatType SecondaryStat;
        public int SecondaryDelta;
        public float TotalDelta;
        public bool IsUpgrade;
    }

    public static class ItemComparer
    {
        public static float ScoreItem(EquipmentInstance item)
        {
            if (item == null) return 0f;
            return item.PrimaryValue + item.SecondaryValue;
        }

        public static StatDiff Compare(EquipmentInstance newItem, EquipmentInstance current)
        {
            float newScore = ScoreItem(newItem);
            float currentScore = ScoreItem(current);
            float delta = newScore - currentScore;

            return new StatDiff
            {
                PrimaryStat = newItem != null ? newItem.PrimaryStat : default,
                PrimaryDelta = (newItem?.PrimaryValue ?? 0) - (current?.PrimaryValue ?? 0),
                SecondaryStat = newItem != null ? newItem.SecondaryStat : default,
                SecondaryDelta = (newItem?.SecondaryValue ?? 0) - (current?.SecondaryValue ?? 0),
                TotalDelta = delta,
                IsUpgrade = delta > 0
            };
        }

        public static EquipmentInstance FindBestForSlot(EquipmentSlot slot,
            IEnumerable<EquipmentInstance> candidates, EquipmentInstance currentlyEquipped)
        {
            float currentScore = ScoreItem(currentlyEquipped);
            EquipmentInstance best = null;
            float bestScore = currentScore;

            foreach (var item in candidates)
            {
                if (item.Slot != slot) continue;
                float score = ScoreItem(item);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = item;
                }
            }
            return best;
        }
    }
}
```

**Step 2: Rewrite `SellCalculator.cs`**

```csharp
using Starquill.Core;

namespace Starquill.Equipment
{
    public static class SellCalculator
    {
        private static readonly double[] RarityBaseValues = { 10, 25, 75, 200, 500 };

        public static double GetSellValue(EquipmentInstance item, int questLevel)
        {
            int rarityIdx = (int)item.Rarity;
            double baseValue = rarityIdx < RarityBaseValues.Length
                ? RarityBaseValues[rarityIdx] : RarityBaseValues[0];

            double value = baseValue * questLevel;

            if (item.Ability != null)
                value *= 1.0 + item.Ability.Level * 0.15;

            return value;
        }
    }
}
```

**Step 3: Rewrite test helpers in both test files**

All test helper `MakeItem()` methods need to use the new `EquipmentInstance` constructor with stat pairs instead of `Stats`/`RolledAffix`. Update `ItemComparerTests.cs` and `SellCalculatorTests.cs` accordingly. See Task 3 test file for the pattern.

**Step 4: Commit**

```
feat: simplify ItemComparer + SellCalculator for stat pair model
```

---

### Task 7: Update EconomyConfig, GameManager, and CharacterInstance

**Files:**
- Modify: `Assets/Scripts/Data/EconomyConfig.cs`
- Modify: `Assets/Scripts/Managers/GameManager.cs`
- Modify: `Assets/Scripts/Characters/CharacterInstance.cs`

**Context:** Add ability leveling config fields to `EconomyConfig`. Wire ability XP ticking into `GameManager.ProcessTick()`. Add gold-accelerated level-up method. `CharacterInstance.GetTotalStats()` already works via `GetTotalStatMods()` — no changes needed there since the method was updated in Task 3.

**Step 1: Add fields to `EconomyConfig.cs`**

Add after the `[Header("Loot")]` section:

```csharp
[Header("Ability Leveling")]
public float abilityBaseXPRate = 1f;
public float abilityBaseXPThreshold = 100f;
public float abilityXPGrowthRate = 1.8f;
public float abilityBaseLevelUpCost = 100f;
public float abilityCostGrowthRate = 2.0f;
```

**Step 2: Update `GameManager.cs`**

In `GameManager.Awake()`, replace:
```csharp
var affixTable = new AffixTable();
affixTable.LoadFromResources();
```
with:
```csharp
var abilityTable = new AbilityTable();
abilityTable.LoadFromResources();
```

And update the `EquipmentFactory` constructor call:
```csharp
equipmentFactory = new EquipmentFactory(equipmentCatalog, abilityTable, registry.Colors);
```

In `GameManager.ProcessTick()`, add ability XP ticking after the combat tick and loot processing. Add at the end of `ProcessTick()`:

```csharp
TickAbilityXP();
```

Add the new method:

```csharp
private void TickAbilityXP()
{
    float xp = economyConfig.abilityBaseXPRate * questLevel;
    foreach (var character in roster.Characters)
    {
        foreach (var equip in character.equipment)
        {
            if (equip?.Ability != null)
            {
                equip.Ability.AddXP(xp,
                    economyConfig.abilityBaseXPThreshold,
                    economyConfig.abilityXPGrowthRate);
            }
        }
    }
}
```

Add a gold-accelerated level-up method:

```csharp
private static readonly float[] RarityGoldMultipliers = { 0.5f, 1.0f, 1.5f, 2.5f, 4.0f };

public bool LevelUpAbility(EquipmentInstance item)
{
    if (item?.Ability == null || item.Ability.Level >= item.Ability.MaxLevel) return false;

    float rarityMult = (int)item.Rarity < RarityGoldMultipliers.Length
        ? RarityGoldMultipliers[(int)item.Rarity] : 1.0f;
    float cost = item.Ability.GoldCostToLevel(
        economyConfig.abilityBaseLevelUpCost,
        economyConfig.abilityCostGrowthRate,
        rarityMult);

    if (gold < cost) return false;

    gold -= cost;
    item.Ability.Level++;
    item.Ability.CurrentXP = 0f;
    OnGoldChanged?.Invoke(gold);
    return true;
}
```

**Step 3: Update `LootDropperTests.cs` SetUp**

In the test file `LootDropperTests.cs`, update the `SetUp()` method to use `AbilityTable` instead of `AffixTable`:

```csharp
var abilityTable = new AbilityTable();
// Load a minimal JSON or leave empty for loot dropper tests
factory = new EquipmentFactory(catalog, abilityTable, colors);
```

**Step 4: Update `AutoEquipperTests.cs` MakeItem helper**

Update the `MakeItem` helper to use the new constructor:

```csharp
private EquipmentInstance MakeItem(EquipmentSlot slot, int str = 0, Rarity rarity = Rarity.Common)
{
    return new EquipmentInstance(
        "hd01", 1, slot, rarity,
        Color.white, new Dictionary<int, Color>(),
        StatType.STR, str, StatType.DEX, 0, null,
        new int[0], new int[0], new int[0],
        false, null, "Test");
}
```

**Step 5: Commit**

```
feat: add ability XP ticking + gold level-up to GameManager
```

---

### Task 8: Update Remaining References and Clean Up

**Files:**
- Grep for any remaining references to `RolledAffix`, `AffixTable`, `StatMods`, `affix` in all `.cs` files
- Fix any compilation errors from the constructor changes
- Update any UI files that reference old fields (but don't change UI behavior — that's Sprint 8)

**Context:** This is a sweep task to catch any remaining references to the old system that weren't covered in Tasks 1-7. Key areas to check:
- `ExploreSceneBuilder.cs` (editor tool)
- Any UI display code that reads `StatMods` or `RolledAffixes`
- `SaveData.cs` if it directly references affix types
- `ItemDisplayData` or similar UI data structs

**Step 1: Search and fix all remaining references**

Run: `grep -rn "RolledAffix\|AffixTable\|AffixEntry\|StatMods\|\.RolledAffixes" Assets/Scripts/ Assets/Tests/ Assets/Editor/`

Fix each hit by updating to the new API.

**Step 2: Verify all tests compile**

All tests should be runnable in the Unity Editor after this task. Since tests can't be run headlessly on Arch Linux, note which tests need manual verification.

**Step 3: Commit**

```
feat: clean up all remaining affix references, complete stat redesign
```

---

### Task 9: Final Verification

**Step 1: Run all tests in Unity Editor**

Open the Unity Editor, navigate to Window > General > Test Runner, and run all EditMode tests. All ~218+ tests should pass.

**Step 2: Play test**

Enter Play mode in the ExploreScene. Verify:
- Characters spawn with equipment that has 2 stats + an ability
- Equipment shows stat pair values (in debug inspector or logs)
- Loot drops have abilities
- Ability XP ticks up over time
- Gold level-up works (if wired to UI — may need Sprint 8)

**Step 3: Commit any final fixes**

```
fix: address test failures from stat redesign
```
