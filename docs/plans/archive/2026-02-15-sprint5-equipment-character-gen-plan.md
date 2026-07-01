# Sprint 5: Equipment & Character Generation — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Port the Godot equipment system to Unity, add rarity/affix generation, build character factory with full random generation, implement a character roster with save/load, and display equipment on paper dolls.

**Architecture:** JSON catalog loading via MiniJSON/SimpleJson (existing pattern). Runtime EquipmentInstance objects created by factory with rolled affixes. CharacterFactory produces fully random characters with equipment. Roster model manages collection + active party. Expanded SaveData serializes everything.

**Tech Stack:** Unity 6, C#, MiniJSON, NUnit, assembly definitions (Core→Data→Equipment/Display→Characters→Managers)

---

## Prerequisites

- Sprint 4 complete, 99 tests passing
- Branch: `unity-idle-clicker`
- Equipment/weapons JSON already in `Assets/Resources/Data/`

## Assembly Dependency Summary

Current:
```
Core (no deps)
Data → Core
Display → Core, Data
Equipment → Core, Data
Characters → Core, Data
Combat → Core, Data
Managers → Core, Data, Combat, Characters, Equipment, Exploration, Display
Tests → all of the above
```

Sprint 5 changes:
- Move MiniJSON.cs + SimpleJson.cs from Display → Core (JSON utilities needed by Equipment)
- Equipment → Core, Data (gains access to MiniJSON/SimpleJson via Core)
- Characters → Core, Data, Equipment (gains access to EquipmentInstance)

---

### Task 1: Move JSON Utilities to Core Assembly

JSON parsing (MiniJSON + SimpleJson) currently lives in `Starquill.Display`. Equipment assembly needs JSON access for catalog loading. Move both files to Core.

**Files:**
- Move: `Assets/Scripts/Display/MiniJSON.cs` → `Assets/Scripts/Core/MiniJSON.cs`
- Move: `Assets/Scripts/Display/SimpleJson.cs` → `Assets/Scripts/Core/SimpleJson.cs`
- Modify: `Assets/Scripts/Display/DisplayDataRegistry.cs` (add using)

**Step 1: Move the files**

Move `Assets/Scripts/Display/MiniJSON.cs` and `Assets/Scripts/Display/MiniJSON.cs.meta` to `Assets/Scripts/Core/`.
Move `Assets/Scripts/Display/SimpleJson.cs` and `Assets/Scripts/Display/SimpleJson.cs.meta` to `Assets/Scripts/Core/`.

**Step 2: Update SimpleJson namespace**

In `Assets/Scripts/Core/SimpleJson.cs`, change:
```csharp
namespace Starquill.Display
```
to:
```csharp
namespace Starquill.Core
```

MiniJSON.cs keeps its `MiniJSON` namespace (third-party library pattern).

**Step 3: Update DisplayDataRegistry.cs**

Add `using Starquill.Core;` at the top of `Assets/Scripts/Display/DisplayDataRegistry.cs` so it can find SimpleJson extension methods.

**Step 4: Verify compilation**

Open Unity Editor. Confirm no compile errors via `Tools > Build Explore Scene` or checking console.

**Step 5: Commit**

```
git add Assets/Scripts/Core/MiniJSON.cs Assets/Scripts/Core/MiniJSON.cs.meta \
  Assets/Scripts/Core/SimpleJson.cs Assets/Scripts/Core/SimpleJson.cs.meta \
  Assets/Scripts/Display/DisplayDataRegistry.cs
git rm Assets/Scripts/Display/MiniJSON.cs Assets/Scripts/Display/MiniJSON.cs.meta \
  Assets/Scripts/Display/SimpleJson.cs Assets/Scripts/Display/SimpleJson.cs.meta
git commit -m "Move JSON utilities (MiniJSON, SimpleJson) from Display to Core assembly"
```

---

### Task 2: Create EquipmentCatalog

Load equipment and weapon data from JSON. This is the game-logic catalog (separate from DisplayDataRegistry's visual-only catalog).

**Files:**
- Create: `Assets/Scripts/Equipment/EquipmentCatalog.cs`
- Create: `Assets/Tests/EditMode/Equipment/EquipmentCatalogTests.cs`

**Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/Equipment/EquipmentCatalogTests.cs`:

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    public class EquipmentCatalogTests
    {
        private EquipmentCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = new EquipmentCatalog();
        }

        [Test]
        public void LoadEquipment_ParsesAllEntries()
        {
            var asset = Resources.Load<TextAsset>("Data/equipment");
            Assert.IsNotNull(asset, "equipment.json not found in Resources");
            catalog.LoadEquipmentJson(asset.text);
            Assert.Greater(catalog.AllEntries.Count, 30);
        }

        [Test]
        public void LoadWeapons_ParsesAllEntries()
        {
            var asset = Resources.Load<TextAsset>("Data/weapons");
            Assert.IsNotNull(asset, "weapons.json not found in Resources");
            catalog.LoadWeaponsJson(asset.text);
            Assert.Greater(catalog.WeaponEntries.Count, 5);
        }

        [Test]
        public void GetByPrefix_ReturnsTorsoItems()
        {
            var asset = Resources.Load<TextAsset>("Data/equipment");
            catalog.LoadEquipmentJson(asset.text);
            var torsoItems = catalog.GetByPrefix("tr");
            Assert.Greater(torsoItems.Count, 10);
            foreach (var item in torsoItems)
                Assert.IsTrue(item.ItemType.StartsWith("tr"));
        }

        [Test]
        public void GetByPrefix_ReturnsHeadItems()
        {
            var asset = Resources.Load<TextAsset>("Data/equipment");
            catalog.LoadEquipmentJson(asset.text);
            var headItems = catalog.GetByPrefix("hd");
            Assert.Greater(headItems.Count, 10);
        }

        [Test]
        public void SlotForPrefix_MapsCorrectly()
        {
            Assert.AreEqual(EquipmentSlot.Head, EquipmentCatalog.SlotForPrefix("hd"));
            Assert.AreEqual(EquipmentSlot.Torso, EquipmentCatalog.SlotForPrefix("tr"));
            Assert.AreEqual(EquipmentSlot.Arms, EquipmentCatalog.SlotForPrefix("ar"));
            Assert.AreEqual(EquipmentSlot.Legs, EquipmentCatalog.SlotForPrefix("lg"));
            Assert.AreEqual(EquipmentSlot.Feet, EquipmentCatalog.SlotForPrefix("fe"));
            Assert.AreEqual(EquipmentSlot.Misc1, EquipmentCatalog.SlotForPrefix("mc"));
            Assert.AreEqual(EquipmentSlot.MainHand, EquipmentCatalog.SlotForPrefix("w"));
        }

        [Test]
        public void WeaponHandType_ParsedCorrectly()
        {
            var asset = Resources.Load<TextAsset>("Data/weapons");
            catalog.LoadWeaponsJson(asset.text);
            var sword = catalog.GetWeapon("w01");
            Assert.IsNotNull(sword);
            Assert.AreEqual("one_handed", sword.HandType);
            var twoHandSword = catalog.GetWeapon("w04");
            Assert.IsNotNull(twoHandSword);
            Assert.AreEqual("two_handed", twoHandSword.HandType);
        }

        [Test]
        public void WeaponAmount_IsArrayForModular()
        {
            var asset = Resources.Load<TextAsset>("Data/weapons");
            catalog.LoadWeaponsJson(asset.text);
            var sword = catalog.GetWeapon("w01");
            Assert.IsTrue(sword.Modular);
            Assert.AreEqual(3, sword.AmountPerLayer.Length);
        }

        [Test]
        public void LoadAll_CombinesEquipmentAndWeapons()
        {
            catalog.LoadFromResources();
            Assert.Greater(catalog.AllEntries.Count, 30);
            Assert.Greater(catalog.WeaponEntries.Count, 5);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Open Unity Editor → Window → General → Test Runner → EditMode → Run All.
Expected: FAIL (EquipmentCatalog class doesn't exist yet).

**Step 3: Implement EquipmentCatalog**

Create `Assets/Scripts/Equipment/EquipmentCatalog.cs`:

```csharp
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
```

**Step 4: Run tests to verify they pass**

Expected: All 8 EquipmentCatalogTests pass.

**Step 5: Commit**

```
git add Assets/Scripts/Equipment/EquipmentCatalog.cs \
  Assets/Tests/EditMode/Equipment/EquipmentCatalogTests.cs
git commit -m "Add EquipmentCatalog with JSON loading and prefix filtering"
```

---

### Task 3: Create EquipmentInstance and RolledAffix

Runtime equipment objects created by the factory. Immutable after creation.

**Files:**
- Create: `Assets/Scripts/Equipment/EquipmentInstance.cs`
- Create: `Assets/Scripts/Equipment/RolledAffix.cs`

**Step 1: Write the failing tests**

Add to a new file `Assets/Tests/EditMode/Equipment/EquipmentInstanceTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    public class EquipmentInstanceTests
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            var instance = new EquipmentInstance(
                itemType: "tr03",
                itemNum: 5,
                slot: EquipmentSlot.Torso,
                rarity: Rarity.Rare,
                baseColor: Color.red,
                varianceColors: new Dictionary<int, Color> { { 48, Color.blue } },
                statMods: new Data.Stats { STR = 3, CON = 2 },
                rolledAffixes: new List<RolledAffix>(),
                layerCodes: new[] { 48 },
                hiddenLayers: new int[0],
                layerColorVariance: new int[0],
                modular: false,
                handType: null,
                displayName: "Rare Sleeveless Shirt"
            );

            Assert.AreEqual("tr03", instance.ItemType);
            Assert.AreEqual(5, instance.ItemNum);
            Assert.AreEqual(EquipmentSlot.Torso, instance.Slot);
            Assert.AreEqual(Rarity.Rare, instance.Rarity);
            Assert.AreEqual(Color.red, instance.BaseColor);
            Assert.AreEqual("Rare Sleeveless Shirt", instance.DisplayName);
        }

        [Test]
        public void GetTotalStatMods_IncludesBaseAndAffixes()
        {
            var affixes = new List<RolledAffix>
            {
                new RolledAffix("affix_str", StatType.STR, 5f, false),
                new RolledAffix("affix_dex", StatType.DEX, 3f, false)
            };
            var instance = new EquipmentInstance(
                "tr03", 1, EquipmentSlot.Torso, Rarity.Rare,
                Color.white, null,
                new Data.Stats { STR = 2 }, affixes,
                new[] { 48 }, new int[0], new int[0],
                false, null, "Test"
            );

            var total = instance.GetTotalStatMods();
            Assert.AreEqual(7, total.STR); // 2 base + 5 affix
            Assert.AreEqual(3, total.DEX); // 0 base + 3 affix
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Expected: FAIL (classes don't exist).

**Step 3: Implement RolledAffix**

Create `Assets/Scripts/Equipment/RolledAffix.cs`:

```csharp
using System;
using Starquill.Core;

namespace Starquill.Equipment
{
    [Serializable]
    public class RolledAffix
    {
        public string AffixId;
        public StatType StatType;
        public float Value;
        public bool IsPercentage;

        public RolledAffix(string affixId, StatType statType, float value, bool isPercentage)
        {
            AffixId = affixId;
            StatType = statType;
            Value = value;
            IsPercentage = isPercentage;
        }
    }
}
```

**Step 4: Implement EquipmentInstance**

Create `Assets/Scripts/Equipment/EquipmentInstance.cs`:

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
        public Stats StatMods { get; }
        public IReadOnlyList<RolledAffix> RolledAffixes { get; }
        public int[] LayerCodes { get; }
        public int[] HiddenLayers { get; }
        public int[] LayerColorVariance { get; }
        public bool Modular { get; }
        public string HandType { get; }
        public string DisplayName { get; }

        public EquipmentInstance(
            string itemType, int itemNum, EquipmentSlot slot, Rarity rarity,
            Color baseColor, Dictionary<int, Color> varianceColors,
            Stats statMods, List<RolledAffix> rolledAffixes,
            int[] layerCodes, int[] hiddenLayers, int[] layerColorVariance,
            bool modular, string handType, string displayName)
        {
            ItemType = itemType;
            ItemNum = itemNum;
            Slot = slot;
            Rarity = rarity;
            BaseColor = baseColor;
            VarianceColors = varianceColors ?? new Dictionary<int, Color>();
            StatMods = statMods ?? new Stats();
            RolledAffixes = rolledAffixes ?? new List<RolledAffix>();
            LayerCodes = layerCodes ?? Array.Empty<int>();
            HiddenLayers = hiddenLayers ?? Array.Empty<int>();
            LayerColorVariance = layerColorVariance ?? Array.Empty<int>();
            Modular = modular;
            HandType = handType;
            DisplayName = displayName ?? "";
        }

        public Stats GetTotalStatMods()
        {
            var total = StatMods.Clone();
            foreach (var affix in RolledAffixes)
            {
                if (!affix.IsPercentage)
                {
                    int current = total.GetStat(affix.StatType);
                    total.SetStat(affix.StatType, current + (int)affix.Value);
                }
            }
            return total;
        }
    }
}
```

**Step 5: Run tests to verify they pass**

Expected: Both EquipmentInstanceTests pass.

**Step 6: Commit**

```
git add Assets/Scripts/Equipment/EquipmentInstance.cs \
  Assets/Scripts/Equipment/RolledAffix.cs \
  Assets/Tests/EditMode/Equipment/EquipmentInstanceTests.cs
git commit -m "Add EquipmentInstance and RolledAffix runtime data classes"
```

---

### Task 4: Create AffixTable

Defines the pool of possible affixes per equipment slot with value ranges per rarity.

**Files:**
- Create: `Assets/Resources/Data/affixes.json`
- Create: `Assets/Scripts/Equipment/AffixTable.cs`
- Create: `Assets/Tests/EditMode/Equipment/AffixTableTests.cs`

**Step 1: Create affixes.json**

Create `Assets/Resources/Data/affixes.json`:

```json
[
  {
    "affix_id": "mighty",
    "display_name": "Mighty",
    "stat_type": "STR",
    "is_percentage": false,
    "valid_slots": ["Head", "Torso", "Arms", "Legs", "Feet", "MainHand", "OffHand", "Misc1", "Misc2", "Misc3", "Misc4"],
    "value_ranges": {
      "Uncommon": { "min": 1, "max": 3 },
      "Rare": { "min": 2, "max": 5 },
      "Epic": { "min": 4, "max": 8 },
      "Legendary": { "min": 6, "max": 12 }
    }
  },
  {
    "affix_id": "nimble",
    "display_name": "Nimble",
    "stat_type": "DEX",
    "is_percentage": false,
    "valid_slots": ["Head", "Torso", "Arms", "Legs", "Feet", "MainHand", "OffHand", "Misc1", "Misc2", "Misc3", "Misc4"],
    "value_ranges": {
      "Uncommon": { "min": 1, "max": 3 },
      "Rare": { "min": 2, "max": 5 },
      "Epic": { "min": 4, "max": 8 },
      "Legendary": { "min": 6, "max": 12 }
    }
  },
  {
    "affix_id": "sturdy",
    "display_name": "Sturdy",
    "stat_type": "CON",
    "is_percentage": false,
    "valid_slots": ["Head", "Torso", "Arms", "Legs", "Feet", "MainHand", "OffHand", "Misc1", "Misc2", "Misc3", "Misc4"],
    "value_ranges": {
      "Uncommon": { "min": 1, "max": 3 },
      "Rare": { "min": 2, "max": 5 },
      "Epic": { "min": 4, "max": 8 },
      "Legendary": { "min": 6, "max": 12 }
    }
  },
  {
    "affix_id": "arcane",
    "display_name": "Arcane",
    "stat_type": "INT",
    "is_percentage": false,
    "valid_slots": ["Head", "Torso", "Arms", "MainHand", "OffHand", "Misc1", "Misc2", "Misc3", "Misc4"],
    "value_ranges": {
      "Uncommon": { "min": 1, "max": 3 },
      "Rare": { "min": 2, "max": 5 },
      "Epic": { "min": 4, "max": 8 },
      "Legendary": { "min": 6, "max": 12 }
    }
  },
  {
    "affix_id": "wise",
    "display_name": "Wise",
    "stat_type": "WIS",
    "is_percentage": false,
    "valid_slots": ["Head", "Torso", "Arms", "MainHand", "OffHand", "Misc1", "Misc2", "Misc3", "Misc4"],
    "value_ranges": {
      "Uncommon": { "min": 1, "max": 3 },
      "Rare": { "min": 2, "max": 5 },
      "Epic": { "min": 4, "max": 8 },
      "Legendary": { "min": 6, "max": 12 }
    }
  },
  {
    "affix_id": "charming",
    "display_name": "Charming",
    "stat_type": "CHA",
    "is_percentage": false,
    "valid_slots": ["Head", "Torso", "Misc1", "Misc2", "Misc3", "Misc4"],
    "value_ranges": {
      "Uncommon": { "min": 1, "max": 3 },
      "Rare": { "min": 2, "max": 5 },
      "Epic": { "min": 4, "max": 8 },
      "Legendary": { "min": 6, "max": 12 }
    }
  }
]
```

**Step 2: Write the failing tests**

Create `Assets/Tests/EditMode/Equipment/AffixTableTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Equipment
{
    public class AffixTableTests
    {
        private AffixTable table;

        [SetUp]
        public void SetUp()
        {
            table = new AffixTable();
            var asset = Resources.Load<TextAsset>("Data/affixes");
            Assert.IsNotNull(asset, "affixes.json not found in Resources");
            table.LoadFromJson(asset.text);
        }

        [Test]
        public void LoadFromJson_ParsesAllAffixes()
        {
            Assert.AreEqual(6, table.AllAffixes.Count);
        }

        [Test]
        public void GetValidAffixes_ForTorso_ReturnsAll()
        {
            var valid = table.GetValidAffixes(EquipmentSlot.Torso);
            Assert.AreEqual(6, valid.Count);
        }

        [Test]
        public void GetValidAffixes_ForFeet_ExcludesINTWISCHA()
        {
            var valid = table.GetValidAffixes(EquipmentSlot.Feet);
            Assert.AreEqual(3, valid.Count); // STR, DEX, CON only
        }

        [Test]
        public void RollAffixes_ReturnsCorrectCount()
        {
            var rng = new System.Random(42);
            var affixes = table.RollAffixes(Rarity.Rare, EquipmentSlot.Torso, rng);
            Assert.AreEqual(2, affixes.Count);
        }

        [Test]
        public void RollAffixes_Common_ReturnsNone()
        {
            var rng = new System.Random(42);
            var affixes = table.RollAffixes(Rarity.Common, EquipmentSlot.Torso, rng);
            Assert.AreEqual(0, affixes.Count);
        }

        [Test]
        public void RollAffixes_NoDuplicateIds()
        {
            var rng = new System.Random(42);
            var affixes = table.RollAffixes(Rarity.Epic, EquipmentSlot.Torso, rng);
            var ids = new HashSet<string>();
            foreach (var a in affixes)
                Assert.IsTrue(ids.Add(a.AffixId), $"Duplicate affix: {a.AffixId}");
        }

        [Test]
        public void RollAffixes_ValuesWithinRange()
        {
            var rng = new System.Random(42);
            for (int i = 0; i < 50; i++)
            {
                var affixes = table.RollAffixes(Rarity.Rare, EquipmentSlot.Torso, new System.Random(i));
                foreach (var a in affixes)
                    Assert.IsTrue(a.Value >= 2 && a.Value <= 5,
                        $"Affix {a.AffixId} value {a.Value} out of Rare range [2,5]");
            }
        }
    }
}
```

**Step 3: Run tests to verify they fail**

Expected: FAIL (AffixTable class doesn't exist).

**Step 4: Implement AffixTable**

Create `Assets/Scripts/Equipment/AffixTable.cs`:

```csharp
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
                // Filter out already-used affixes
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

                // Round to integer for flat stats
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
                Rarity.Epic => 2 + (rng.Next(2) == 0 ? 1 : 0), // 2-3
                Rarity.Legendary => 3,
                _ => 0
            };
        }
    }
}
```

**Step 5: Run tests to verify they pass**

Expected: All 7 AffixTableTests pass.

**Step 6: Commit**

```
git add Assets/Resources/Data/affixes.json Assets/Resources/Data/affixes.json.meta \
  Assets/Scripts/Equipment/AffixTable.cs \
  Assets/Tests/EditMode/Equipment/AffixTableTests.cs
git commit -m "Add AffixTable with JSON loading and rarity-based affix rolling"
```

---

### Task 5: Create EquipmentFactory

Core factory that creates random equipment instances with stats, colors, and affixes.

**Files:**
- Create: `Assets/Scripts/Equipment/EquipmentFactory.cs`
- Create: `Assets/Tests/EditMode/Equipment/EquipmentFactoryTests.cs`
- Modify: `Assets/Scripts/Equipment/Starquill.Equipment.asmdef` (add Display reference for ColorManager)

**Step 1: Update Equipment assembly definition**

Edit `Assets/Scripts/Equipment/Starquill.Equipment.asmdef` to add Display reference (needed for ColorManager):

```json
{
    "name": "Starquill.Equipment",
    "rootNamespace": "Starquill.Equipment",
    "references": ["Starquill.Core", "Starquill.Data", "Starquill.Display"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

**Step 2: Write the failing tests**

Create `Assets/Tests/EditMode/Equipment/EquipmentFactoryTests.cs`:

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
    public class EquipmentFactoryTests
    {
        private EquipmentFactory factory;
        private EquipmentCatalog catalog;
        private AffixTable affixTable;
        private ColorManager colors;

        [SetUp]
        public void SetUp()
        {
            catalog = new EquipmentCatalog();
            catalog.LoadFromResources();

            affixTable = new AffixTable();
            var affixAsset = Resources.Load<TextAsset>("Data/affixes");
            if (affixAsset != null) affixTable.LoadFromJson(affixAsset.text);

            colors = new ColorManager();
            colors.LoadFromResources();

            factory = new EquipmentFactory(catalog, affixTable, colors);
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
        public void CreateRandom_CommonHasNoAffixes()
        {
            var rng = new System.Random(42);
            var instance = factory.CreateRandom("tr", Rarity.Common, rng);
            Assert.AreEqual(0, instance.RolledAffixes.Count);
        }

        [Test]
        public void CreateRandom_RareHasTwoAffixes()
        {
            var rng = new System.Random(42);
            var instance = factory.CreateRandom("tr", Rarity.Rare, rng);
            Assert.AreEqual(2, instance.RolledAffixes.Count);
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
        public void CreateRandomLoadout_FillsPrioritySlots()
        {
            var rng = new System.Random(42);
            var loadout = factory.CreateRandomLoadout(1, rng);
            // Priority slots: Torso (100%), Legs (100%)
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Torso], "Torso should always be filled");
            Assert.IsNotNull(loadout[(int)EquipmentSlot.Legs], "Legs should always be filled");
        }

        [Test]
        public void CreateRandomLoadout_TwoHandedClearsOffHand()
        {
            // Run many times to catch a two-handed weapon roll
            bool foundTwoHanded = false;
            for (int seed = 0; seed < 200; seed++)
            {
                var rng = new System.Random(seed);
                var loadout = factory.CreateRandomLoadout(1, rng);
                var mainHand = loadout[(int)EquipmentSlot.MainHand];
                if (mainHand != null && mainHand.HandType == "two_handed")
                {
                    Assert.IsNull(loadout[(int)EquipmentSlot.OffHand],
                        "Two-handed weapon should clear off-hand");
                    foundTwoHanded = true;
                    break;
                }
            }
            // If no two-handed found in 200 seeds, that's OK — the test logic is still correct
            if (!foundTwoHanded)
                Assert.Pass("No two-handed weapon rolled in 200 seeds (expected occasionally)");
        }

        [Test]
        public void RollRarity_AtLevel1_MostlyCommon()
        {
            var rng = new System.Random(42);
            int commonCount = 0;
            for (int i = 0; i < 100; i++)
            {
                var rarity = EquipmentFactory.RollRarity(1, rng);
                if (rarity == Rarity.Common) commonCount++;
            }
            Assert.Greater(commonCount, 60, "At level 1, >60% should be Common");
        }

        [Test]
        public void GenerateBaseStats_RareHasMoreThanCommon()
        {
            var rng = new System.Random(42);
            var commonStats = EquipmentFactory.GenerateBaseStats(Rarity.Common, rng);
            var rareStats = EquipmentFactory.GenerateBaseStats(Rarity.Rare, rng);
            Assert.GreaterOrEqual(rareStats.Total, commonStats.Total);
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

**Step 3: Run tests to verify they fail**

Expected: FAIL (EquipmentFactory class doesn't exist).

**Step 4: Implement EquipmentFactory**

Create `Assets/Scripts/Equipment/EquipmentFactory.cs`:

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

            // For modular weapons, pick variant per layer
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
                entry.Modular, entry.HandType, displayName
            );
        }

        /// <summary>
        /// Creates a full 11-slot equipment loadout using priority/chance system.
        /// </summary>
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

                    // Off-hand: 30% if main hand is one-handed
                    if (weapon.HandType == "one_handed" && rng.NextDouble() < 0.30)
                    {
                        var offhand = CreateRandomWeapon(RollRarity(questLevel, rng), rng, "one_handed");
                        if (offhand != null)
                        {
                            // Re-create with OffHand slot
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
            // Weights shift with quest level
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

            // Capitalize first letter of description
            string desc = baseDescription;
            if (!string.IsNullOrEmpty(desc))
                desc = char.ToUpper(desc[0]) + desc.Substring(1);

            return string.IsNullOrEmpty(rarityPrefix) ? desc : $"{rarityPrefix} {desc}";
        }

        /// <summary>
        /// Reconstructs an EquipmentInstance from saved data without re-rolling.
        /// </summary>
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
                layerCodes = new int[0];
                hiddenLayers = new int[0];
                layerColorVariance = new int[0];
                modular = false;
            }

            var baseColor = new Color(data.baseColor[0], data.baseColor[1], data.baseColor[2], data.baseColor[3]);
            var varianceColors = new Dictionary<int, Color>();
            if (data.varianceColorKeys != null)
            {
                for (int i = 0; i < data.varianceColorKeys.Length; i++)
                {
                    var vc = data.varianceColorValues[i];
                    varianceColors[data.varianceColorKeys[i]] = new Color(vc[0], vc[1], vc[2], vc[3]);
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
                modular, handType, GenerateDisplayName((Rarity)data.rarity, description)
            );
        }
    }
}
```

**Step 5: Run tests to verify they pass**

Expected: All 10 EquipmentFactoryTests pass.

**Step 6: Commit**

```
git add Assets/Scripts/Equipment/EquipmentFactory.cs \
  Assets/Scripts/Equipment/Starquill.Equipment.asmdef \
  Assets/Tests/EditMode/Equipment/EquipmentFactoryTests.cs
git commit -m "Add EquipmentFactory with random generation, loadouts, and reconstruction"
```

---

### Task 6: Create NameGenerator

Simple JSON-based random name generator for characters.

**Files:**
- Create: `Assets/Resources/Data/names.json`
- Create: `Assets/Scripts/Characters/NameGenerator.cs`

**Step 1: Create names.json**

Create `Assets/Resources/Data/names.json`:

```json
{
  "first_names": [
    "Aldric", "Brenna", "Caelum", "Daria", "Elric",
    "Freya", "Gareth", "Helena", "Idris", "Juno",
    "Kael", "Lyra", "Magnus", "Nyx", "Orin",
    "Petra", "Quinn", "Rowan", "Sera", "Theron",
    "Uma", "Vesper", "Wren", "Xander", "Yara",
    "Zephyr", "Ash", "Bram", "Cass", "Dorian",
    "Ember", "Flint", "Gale", "Hazel", "Iris",
    "Jace", "Kai", "Lux", "Mira", "Nolan",
    "Onyx", "Pike", "Raven", "Sage", "Thorn",
    "Vale", "Wolf", "Astra", "Blaze", "Coral"
  ],
  "epithets": [
    "the Bold", "the Swift", "Ironheart", "Shadowstep",
    "Brightblade", "Stormborn", "the Keen", "Firebrand",
    "Stonefist", "the Wise", "Dawnbringer", "Nightwhisper",
    "the Brave", "Frostweaver", "Thunderstrike", "the Calm",
    "Stargazer", "Windwalker", "the Fierce", "Silvertongue"
  ]
}
```

**Step 2: Implement NameGenerator**

Create `Assets/Scripts/Characters/NameGenerator.cs`:

```csharp
using System.Collections.Generic;
using Starquill.Core;
using UnityEngine;

namespace Starquill.Characters
{
    public class NameGenerator
    {
        private string[] firstNames = { "Hero" };
        private string[] epithets = { "the Brave" };

        public void LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>("Data/names");
            if (asset != null) LoadFromJson(asset.text);
        }

        public void LoadFromJson(string json)
        {
            var parsed = MiniJSON.Json.Deserialize(json);
            if (parsed is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("first_names", out var fn) && fn is List<object> fnList)
                {
                    firstNames = new string[fnList.Count];
                    for (int i = 0; i < fnList.Count; i++)
                        firstNames[i] = fnList[i]?.ToString() ?? "Hero";
                }
                if (dict.TryGetValue("epithets", out var ep) && ep is List<object> epList)
                {
                    epithets = new string[epList.Count];
                    for (int i = 0; i < epList.Count; i++)
                        epithets[i] = epList[i]?.ToString() ?? "";
                }
            }
        }

        public string Generate(System.Random rng)
        {
            string first = firstNames[rng.Next(firstNames.Length)];
            string epithet = epithets[rng.Next(epithets.Length)];
            return $"{first} {epithet}";
        }
    }
}
```

**Step 3: Commit**

```
git add Assets/Resources/Data/names.json Assets/Resources/Data/names.json.meta \
  Assets/Scripts/Characters/NameGenerator.cs
git commit -m "Add NameGenerator with JSON name lists"
```

---

### Task 7: Create CharacterRoster

Manages the player's collection of characters and active party selection.

**Files:**
- Create: `Assets/Scripts/Characters/CharacterRoster.cs`
- Create: `Assets/Tests/EditMode/Characters/CharacterRosterTests.cs`
- Modify: `Assets/Scripts/Characters/Starquill.Characters.asmdef` (add Equipment reference)

**Step 1: Update Characters assembly definition**

Edit `Assets/Scripts/Characters/Starquill.Characters.asmdef`:

```json
{
    "name": "Starquill.Characters",
    "rootNamespace": "Starquill.Characters",
    "references": ["Starquill.Core", "Starquill.Data", "Starquill.Equipment"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

**Step 2: Write the failing tests**

Create `Assets/Tests/EditMode/Characters/CharacterRosterTests.cs`:

```csharp
using NUnit.Framework;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;

namespace Starquill.Tests.Characters
{
    public class CharacterRosterTests
    {
        private CharacterRoster roster;

        [SetUp]
        public void SetUp()
        {
            roster = new CharacterRoster();
        }

        [Test]
        public void AddCharacter_IncreasesCount()
        {
            var c = CreateTestCharacter("c1");
            roster.AddCharacter(c);
            Assert.AreEqual(1, roster.Characters.Count);
        }

        [Test]
        public void AddCharacter_RespectsRosterCap()
        {
            for (int i = 0; i < 20; i++)
                Assert.IsTrue(roster.AddCharacter(CreateTestCharacter($"c{i}")));
            Assert.IsFalse(roster.AddCharacter(CreateTestCharacter("overflow")));
            Assert.AreEqual(20, roster.Characters.Count);
        }

        [Test]
        public void RemoveCharacter_DecreasesCount()
        {
            roster.AddCharacter(CreateTestCharacter("c1"));
            roster.AddCharacter(CreateTestCharacter("c2"));
            roster.RemoveCharacter(0);
            Assert.AreEqual(1, roster.Characters.Count);
        }

        [Test]
        public void SetPartyMember_UpdatesIndices()
        {
            for (int i = 0; i < 8; i++)
                roster.AddCharacter(CreateTestCharacter($"c{i}"));

            roster.SetPartyMember(0, 0);
            roster.SetPartyMember(1, 1);
            roster.SetPartyMember(2, 2);
            roster.SetPartyMember(3, 3);

            var party = roster.GetActiveParty();
            Assert.AreEqual(4, party.Length);
            Assert.AreEqual("c0", party[0].id);
            Assert.AreEqual("c3", party[3].id);
        }

        [Test]
        public void GetActiveParty_ReturnsNullForUnset()
        {
            roster.AddCharacter(CreateTestCharacter("c0"));
            roster.SetPartyMember(0, 0);
            var party = roster.GetActiveParty();
            Assert.IsNotNull(party[0]);
            Assert.IsNull(party[1]);
        }

        [Test]
        public void RemoveCharacter_AdjustsPartyIndices()
        {
            for (int i = 0; i < 4; i++)
                roster.AddCharacter(CreateTestCharacter($"c{i}"));
            roster.SetPartyMember(0, 0);
            roster.SetPartyMember(1, 2);
            roster.SetPartyMember(2, 3);

            // Remove index 1 (c1, not in party)
            roster.RemoveCharacter(1);

            var party = roster.GetActiveParty();
            // Index 0 stays 0 (c0), index 2 becomes 1 (was c2), index 3 becomes 2 (was c3)
            Assert.AreEqual("c0", party[0].id);
            Assert.AreEqual("c2", party[1].id);
            Assert.AreEqual("c3", party[2].id);
        }

        [Test]
        public void InitializeDefaultParty_SelectsFirst4()
        {
            for (int i = 0; i < 8; i++)
                roster.AddCharacter(CreateTestCharacter($"c{i}"));
            roster.InitializeDefaultParty();
            var party = roster.GetActiveParty();
            Assert.AreEqual("c0", party[0].id);
            Assert.AreEqual("c3", party[3].id);
        }

        private CharacterInstance CreateTestCharacter(string id)
        {
            return new CharacterInstance
            {
                id = id,
                displayName = id,
                baseStats = new Stats { STR = 10, DEX = 10, CON = 10, INT = 10, WIS = 10, CHA = 10 }
            };
        }
    }
}
```

**Step 3: Run tests to verify they fail**

Expected: FAIL (CharacterRoster doesn't exist).

**Step 4: Implement CharacterRoster**

Create `Assets/Scripts/Characters/CharacterRoster.cs`:

```csharp
using System.Collections.Generic;

namespace Starquill.Characters
{
    public class CharacterRoster
    {
        public const int RosterCap = 20;
        public List<CharacterInstance> Characters { get; } = new();
        public int[] ActivePartyIndices { get; } = { -1, -1, -1, -1 };

        public bool AddCharacter(CharacterInstance character)
        {
            if (Characters.Count >= RosterCap) return false;
            Characters.Add(character);
            return true;
        }

        public void RemoveCharacter(int index)
        {
            if (index < 0 || index >= Characters.Count) return;
            Characters.RemoveAt(index);

            // Adjust party indices
            for (int i = 0; i < ActivePartyIndices.Length; i++)
            {
                if (ActivePartyIndices[i] == index)
                    ActivePartyIndices[i] = -1;
                else if (ActivePartyIndices[i] > index)
                    ActivePartyIndices[i]--;
            }
        }

        public void SetPartyMember(int partySlot, int rosterIndex)
        {
            if (partySlot < 0 || partySlot >= 4) return;
            if (rosterIndex < 0 || rosterIndex >= Characters.Count) return;
            ActivePartyIndices[partySlot] = rosterIndex;
        }

        public CharacterInstance[] GetActiveParty()
        {
            var party = new CharacterInstance[4];
            for (int i = 0; i < 4; i++)
            {
                int idx = ActivePartyIndices[i];
                party[i] = (idx >= 0 && idx < Characters.Count) ? Characters[idx] : null;
            }
            return party;
        }

        public void InitializeDefaultParty()
        {
            for (int i = 0; i < 4 && i < Characters.Count; i++)
                ActivePartyIndices[i] = i;
        }
    }
}
```

**Step 5: Run tests to verify they pass**

Expected: All 7 CharacterRosterTests pass.

**Step 6: Commit**

```
git add Assets/Scripts/Characters/CharacterRoster.cs \
  Assets/Scripts/Characters/Starquill.Characters.asmdef \
  Assets/Tests/EditMode/Characters/CharacterRosterTests.cs
git commit -m "Add CharacterRoster with party management and index adjustment"
```

---

### Task 8: Update CharacterInstance for EquipmentInstance

Replace `EquipmentDefinition[]` with `EquipmentInstance[]` in CharacterInstance.

**Files:**
- Modify: `Assets/Scripts/Characters/CharacterInstance.cs`

**Step 1: Update CharacterInstance**

Edit `Assets/Scripts/Characters/CharacterInstance.cs` to replace EquipmentDefinition references:

```csharp
using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;

namespace Starquill.Characters
{
    [Serializable]
    public class CharacterInstance
    {
        public string id;
        public string displayName;
        public string speciesId;
        public Stats baseStats;
        public int level = 1;
        public int xp;
        public int speciesKills;
        public EquipmentInstance[] equipment = new EquipmentInstance[11];
        public List<VerbDefinition> equippedVerbs = new();
        public Stats allocatedStats = new();

        public Stats GetTotalStats()
        {
            var total = baseStats.Clone();
            total = total + allocatedStats;
            foreach (var equip in equipment)
            {
                if (equip != null)
                    total = total + equip.GetTotalStatMods();
            }
            return total;
        }

        public int GetVerbSlotCount()
        {
            if (level >= 51) return 5;
            if (level >= 26) return 4;
            if (level >= 11) return 3;
            return 2;
        }

        public void EquipItem(EquipmentInstance item)
        {
            equipment[(int)item.Slot] = item;
        }

        public void UnequipSlot(EquipmentSlot slot)
        {
            equipment[(int)slot] = null;
        }

        public void EquipLoadout(EquipmentInstance[] loadout)
        {
            for (int i = 0; i < loadout.Length && i < equipment.Length; i++)
                equipment[i] = loadout[i];
        }
    }
}
```

Note: This removes the `SpeciesDefinition species` field (replaced by `speciesId` string) and `GetSpeciesAbilityRank()` (species abilities are future scope). The field `speciesId` maps to the species key in DisplayDataRegistry.

**Step 2: Verify compilation**

Confirm no compile errors. GameManager.cs `InitializeStarterParty()` will need updating (done in Task 11).

**Step 3: Commit**

```
git add Assets/Scripts/Characters/CharacterInstance.cs
git commit -m "Update CharacterInstance to use EquipmentInstance[] and speciesId"
```

---

### Task 9: Create CharacterFactory

Produces fully random characters with species, equipment, stats, verbs, and names.

**Files:**
- Create: `Assets/Scripts/Characters/CharacterFactory.cs`
- Create: `Assets/Tests/EditMode/Characters/CharacterFactoryTests.cs`

**Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/Characters/CharacterFactoryTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;
using Starquill.Display;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Characters
{
    public class CharacterFactoryTests
    {
        private CharacterFactory factory;

        [SetUp]
        public void SetUp()
        {
            var registry = DisplayDataRegistry.Instance;
            if (registry.Species.Count == 0) registry.LoadAll();

            var catalog = new EquipmentCatalog();
            catalog.LoadFromResources();

            var affixTable = new AffixTable();
            var affixAsset = Resources.Load<TextAsset>("Data/affixes");
            if (affixAsset != null) affixTable.LoadFromJson(affixAsset.text);

            var equipFactory = new EquipmentFactory(catalog, affixTable, registry.Colors);

            var nameGen = new NameGenerator();
            nameGen.LoadFromResources();

            factory = new CharacterFactory(registry, equipFactory, nameGen);
        }

        [Test]
        public void CreateRandom_ProducesValidCharacter()
        {
            var rng = new System.Random(42);
            var speciesKeys = new List<string>(DisplayDataRegistry.Instance.Species.Keys);
            var character = factory.CreateRandom(speciesKeys[0], 1, 1, rng);

            Assert.IsNotNull(character);
            Assert.IsFalse(string.IsNullOrEmpty(character.id));
            Assert.IsFalse(string.IsNullOrEmpty(character.displayName));
            Assert.IsFalse(string.IsNullOrEmpty(character.speciesId));
            Assert.AreEqual(1, character.level);
            Assert.Greater(character.baseStats.Total, 0);
        }

        [Test]
        public void CreateRandom_HasEquipmentInPrioritySlots()
        {
            var rng = new System.Random(42);
            var speciesKeys = new List<string>(DisplayDataRegistry.Instance.Species.Keys);
            var character = factory.CreateRandom(speciesKeys[0], 1, 1, rng);

            Assert.IsNotNull(character.equipment[(int)EquipmentSlot.Torso], "Torso should be equipped");
            Assert.IsNotNull(character.equipment[(int)EquipmentSlot.Legs], "Legs should be equipped");
        }

        [Test]
        public void CreateRandom_HasVerbs()
        {
            var rng = new System.Random(42);
            var speciesKeys = new List<string>(DisplayDataRegistry.Instance.Species.Keys);
            var character = factory.CreateRandom(speciesKeys[0], 1, 1, rng);

            Assert.AreEqual(2, character.equippedVerbs.Count);
        }

        [Test]
        public void CreateStarterRoster_ProducesRequestedCount()
        {
            var rng = new System.Random(42);
            var roster = factory.CreateStarterRoster(8, rng);
            Assert.AreEqual(8, roster.Count);
        }

        [Test]
        public void CreateStarterRoster_HasSpeciesVariety()
        {
            var rng = new System.Random(42);
            var roster = factory.CreateStarterRoster(8, rng);

            var speciesCounts = new Dictionary<string, int>();
            foreach (var c in roster)
            {
                if (!speciesCounts.ContainsKey(c.speciesId))
                    speciesCounts[c.speciesId] = 0;
                speciesCounts[c.speciesId]++;
            }

            // No species should appear more than 2 times
            foreach (var count in speciesCounts.Values)
                Assert.LessOrEqual(count, 2, "Max 2 of same species in starter roster");
        }

        [Test]
        public void CreateRandom_VerbMatchesHighestStat()
        {
            // Run many seeds to test verb assignment
            var speciesKeys = new List<string>(DisplayDataRegistry.Instance.Species.Keys);
            int matchCount = 0;
            for (int seed = 0; seed < 50; seed++)
            {
                var rng = new System.Random(seed);
                var c = factory.CreateRandom(speciesKeys[0], 1, 1, rng);
                if (c.equippedVerbs.Count > 0)
                {
                    var highestStat = c.baseStats.HighestStat();
                    if (c.equippedVerbs[0].statType == highestStat)
                        matchCount++;
                }
            }
            Assert.Greater(matchCount, 25, "Primary verb should match highest stat most of the time");
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Expected: FAIL (CharacterFactory doesn't exist).

**Step 3: Implement CharacterFactory**

Create `Assets/Scripts/Characters/CharacterFactory.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Starquill.Core;
using Starquill.Data;
using Starquill.Display;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Characters
{
    public class CharacterFactory
    {
        private readonly DisplayDataRegistry registry;
        private readonly EquipmentFactory equipFactory;
        private readonly NameGenerator nameGen;

        // Starter verb pool
        private static readonly (string id, string name, StatType type, float dmg, float cd, bool heal, float healAmt)[] StarterVerbs =
        {
            ("slash", "Slash", StatType.STR, 50f, 2f, false, 0f),
            ("shield_bash", "Shield Bash", StatType.CON, 30f, 3f, false, 0f),
            ("fireball", "Fireball", StatType.INT, 65f, 3f, false, 0f),
            ("heal", "Heal", StatType.WIS, 0f, 4f, true, 40f),
        };

        public CharacterFactory(DisplayDataRegistry registry, EquipmentFactory equipFactory, NameGenerator nameGen)
        {
            this.registry = registry;
            this.equipFactory = equipFactory;
            this.nameGen = nameGen;
        }

        public CharacterInstance CreateRandom(string speciesKey, int level, int questLevel, System.Random rng)
        {
            if (!registry.Species.TryGetValue(speciesKey, out var speciesData))
                return null;

            var character = new CharacterInstance
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 8),
                speciesId = speciesKey,
                level = level,
                displayName = nameGen.Generate(rng),
                baseStats = GenerateBaseStats(speciesData, rng)
            };

            // Generate and equip loadout
            var loadout = equipFactory.CreateRandomLoadout(questLevel, rng);
            character.EquipLoadout(loadout);

            // Assign 2 verbs based on stats
            AssignVerbs(character, rng);

            return character;
        }

        public List<CharacterInstance> CreateStarterRoster(int count, System.Random rng)
        {
            var speciesKeys = new List<string>(registry.Species.Keys);
            if (speciesKeys.Count == 0) return new List<CharacterInstance>();

            var roster = new List<CharacterInstance>();
            var speciesUsage = new Dictionary<string, int>();

            for (int i = 0; i < count; i++)
            {
                // Pick species with variety constraint (max 2 of same)
                var available = speciesKeys.Where(k =>
                    !speciesUsage.ContainsKey(k) || speciesUsage[k] < 2).ToList();
                if (available.Count == 0)
                    available = speciesKeys; // fallback if all maxed

                var key = available[rng.Next(available.Count)];
                if (!speciesUsage.ContainsKey(key)) speciesUsage[key] = 0;
                speciesUsage[key]++;

                var character = CreateRandom(key, 1, 1, rng);
                if (character != null)
                    roster.Add(character);
            }

            return roster;
        }

        private Stats GenerateBaseStats(SpeciesDisplayData speciesData, System.Random rng)
        {
            // Base 30 points distributed across 6 stats with some randomization
            int budget = 30;
            var stats = new Stats();
            var types = (StatType[])Enum.GetValues(typeof(StatType));

            // Give each stat a minimum of 3
            foreach (var type in types)
            {
                stats.SetStat(type, 3);
                budget -= 3;
            }

            // Distribute remaining 12 points randomly
            for (int i = 0; i < budget; i++)
            {
                var type = types[rng.Next(types.Length)];
                stats.SetStat(type, stats.GetStat(type) + 1);
            }

            return stats;
        }

        private void AssignVerbs(CharacterInstance character, System.Random rng)
        {
            var highestStat = character.baseStats.HighestStat();

            // Primary verb: matches highest stat
            var primary = StarterVerbs.Where(v => v.type == highestStat).ToArray();
            if (primary.Length == 0)
                primary = StarterVerbs; // fallback

            var (id1, name1, type1, dmg1, cd1, heal1, ha1) = primary[rng.Next(primary.Length)];
            character.equippedVerbs.Add(CreateVerb(id1, name1, type1, dmg1, cd1, heal1, ha1));

            // Secondary verb: random from remaining
            var remaining = StarterVerbs.Where(v => v.id != id1).ToArray();
            if (remaining.Length > 0)
            {
                var (id2, name2, type2, dmg2, cd2, heal2, ha2) = remaining[rng.Next(remaining.Length)];
                character.equippedVerbs.Add(CreateVerb(id2, name2, type2, dmg2, cd2, heal2, ha2));
            }
        }

        private static VerbDefinition CreateVerb(string id, string name, StatType type,
            float damage, float cooldown, bool isHealing, float healAmount)
        {
            var verb = ScriptableObject.CreateInstance<VerbDefinition>();
            verb.verbId = id;
            verb.displayName = name;
            verb.statType = type;
            verb.baseDamage = damage;
            verb.cooldownTicks = cooldown;
            verb.isHealingVerb = isHealing;
            verb.healAmount = healAmount;
            return verb;
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Expected: All 6 CharacterFactoryTests pass.

**Step 5: Commit**

```
git add Assets/Scripts/Characters/CharacterFactory.cs \
  Assets/Tests/EditMode/Characters/CharacterFactoryTests.cs
git commit -m "Add CharacterFactory with random generation and starter roster"
```

---

### Task 10: Expand SaveData and SaveManager

Add full roster + equipment serialization. Support migration from old save format.

**Files:**
- Modify: `Assets/Scripts/Managers/SaveData.cs`
- Modify: `Assets/Scripts/Managers/SaveManager.cs`
- Create: `Assets/Tests/EditMode/Save/SaveRoundTripTests.cs`

**Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/Save/SaveRoundTripTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using Starquill.Managers;
using UnityEngine;

namespace Starquill.Tests.Save
{
    public class SaveRoundTripTests
    {
        [Test]
        public void SerializedEquipment_RoundTrips()
        {
            var original = new EquipmentInstance(
                "tr03", 5, EquipmentSlot.Torso, Rarity.Rare,
                new Color(0.5f, 0.3f, 0.1f, 1f),
                new Dictionary<int, Color> { { 48, new Color(0.2f, 0.4f, 0.6f, 1f) } },
                new Stats { STR = 3, CON = 2 },
                new List<RolledAffix>
                {
                    new RolledAffix("mighty", StatType.STR, 4f, false),
                    new RolledAffix("sturdy", StatType.CON, 3f, false)
                },
                new[] { 48 }, new int[0], new int[0],
                false, null, "Rare Sleeveless Shirt"
            );

            var serialized = SerializedEquipment.FromInstance(original);
            Assert.AreEqual("tr03", serialized.itemType);
            Assert.AreEqual(5, serialized.itemNum);
            Assert.AreEqual((int)Rarity.Rare, serialized.rarity);
            Assert.AreEqual(2, serialized.affixes.Length);
        }

        [Test]
        public void SerializedCharacter_RoundTrips()
        {
            var character = new CharacterInstance
            {
                id = "test123",
                displayName = "Theron the Bold",
                speciesId = "human",
                level = 3,
                xp = 150,
                speciesKills = 12,
                baseStats = new Stats { STR = 14, DEX = 10, CON = 12, INT = 8, WIS = 9, CHA = 10 },
                allocatedStats = new Stats { STR = 2 }
            };

            var serialized = SerializedCharacter.FromInstance(character);
            Assert.AreEqual("test123", serialized.id);
            Assert.AreEqual("Theron the Bold", serialized.displayName);
            Assert.AreEqual("human", serialized.speciesId);
            Assert.AreEqual(3, serialized.level);
        }

        [Test]
        public void OldSaveFormat_MigratesGracefully()
        {
            // Simulate an old save with just gold
            var oldSave = new SaveData { gold = 1000, currentQuestLevel = 5 };
            Assert.AreEqual(0, oldSave.roster.Count);
            Assert.IsTrue(oldSave.NeedsRosterInitialization());
        }

        [Test]
        public void SaveData_WithRoster_DoesNotNeedInit()
        {
            var save = new SaveData();
            save.roster.Add(new SerializedCharacter { id = "c1" });
            Assert.IsFalse(save.NeedsRosterInitialization());
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Expected: FAIL (SerializedEquipment, SerializedCharacter don't have the new methods).

**Step 3: Update SaveData.cs**

Replace `Assets/Scripts/Managers/SaveData.cs`:

```csharp
using System;
using System.Collections.Generic;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Managers
{
    [Serializable]
    public class SaveData
    {
        public double gold;
        public int highestQuestLevel;
        public int currentQuestLevel = 1;
        public List<SerializedCharacter> roster = new();
        public int[] activePartyIndices = { 0, 1, 2, 3 };
        public PityTracker pityTracker = new();
        public long lastPlayedTimestamp;
        public float fragmentProgress;

        public bool NeedsRosterInitialization()
        {
            return roster == null || roster.Count == 0;
        }
    }

    [Serializable]
    public class SerializedCharacter
    {
        public string id;
        public string displayName;
        public string speciesId;
        public int level;
        public int xp;
        public int speciesKills;
        public int[] baseStats = new int[6];
        public int[] allocatedStats = new int[6];
        public List<string> equippedVerbIds = new();
        public SerializedEquipment[] equipment = new SerializedEquipment[11];
        public SerializedSpeciesVisuals speciesVisuals;

        public static SerializedCharacter FromInstance(CharacterInstance c)
        {
            var sc = new SerializedCharacter
            {
                id = c.id,
                displayName = c.displayName,
                speciesId = c.speciesId,
                level = c.level,
                xp = c.xp,
                speciesKills = c.speciesKills,
                baseStats = StatsToArray(c.baseStats),
                allocatedStats = StatsToArray(c.allocatedStats)
            };

            foreach (var verb in c.equippedVerbs)
                if (verb != null) sc.equippedVerbIds.Add(verb.verbId);

            for (int i = 0; i < c.equipment.Length && i < 11; i++)
            {
                if (c.equipment[i] != null)
                    sc.equipment[i] = SerializedEquipment.FromInstance(c.equipment[i]);
            }

            return sc;
        }

        public CharacterInstance ToInstance()
        {
            return new CharacterInstance
            {
                id = id,
                displayName = displayName,
                speciesId = speciesId,
                level = level,
                xp = xp,
                speciesKills = speciesKills,
                baseStats = ArrayToStats(baseStats),
                allocatedStats = ArrayToStats(allocatedStats)
            };
        }

        private static int[] StatsToArray(Stats s)
        {
            return s == null ? new int[6] : new[] { s.STR, s.DEX, s.CON, s.INT, s.WIS, s.CHA };
        }

        private static Stats ArrayToStats(int[] arr)
        {
            if (arr == null || arr.Length < 6) return new Stats();
            return new Stats { STR = arr[0], DEX = arr[1], CON = arr[2], INT = arr[3], WIS = arr[4], CHA = arr[5] };
        }
    }

    [Serializable]
    public class SerializedEquipment
    {
        public string itemType;
        public int itemNum;
        public int slot;
        public int rarity;
        public float[] baseColor = new float[4];
        public int[] varianceColorKeys;
        public float[][] varianceColorValues;
        public int[] statMods = new int[6];
        public SerializedAffix[] affixes;

        public static SerializedEquipment FromInstance(EquipmentInstance e)
        {
            var se = new SerializedEquipment
            {
                itemType = e.ItemType,
                itemNum = e.ItemNum,
                slot = (int)e.Slot,
                rarity = (int)e.Rarity,
                baseColor = new[] { e.BaseColor.r, e.BaseColor.g, e.BaseColor.b, e.BaseColor.a },
                statMods = new[] {
                    e.StatMods.STR, e.StatMods.DEX, e.StatMods.CON,
                    e.StatMods.INT, e.StatMods.WIS, e.StatMods.CHA
                }
            };

            // Serialize variance colors
            var vcDict = e.VarianceColors;
            if (vcDict != null && vcDict.Count > 0)
            {
                se.varianceColorKeys = new int[vcDict.Count];
                se.varianceColorValues = new float[vcDict.Count][];
                int i = 0;
                foreach (var kvp in vcDict)
                {
                    se.varianceColorKeys[i] = kvp.Key;
                    se.varianceColorValues[i] = new[] { kvp.Value.r, kvp.Value.g, kvp.Value.b, kvp.Value.a };
                    i++;
                }
            }

            // Serialize affixes
            if (e.RolledAffixes != null && e.RolledAffixes.Count > 0)
            {
                se.affixes = new SerializedAffix[e.RolledAffixes.Count];
                for (int i = 0; i < e.RolledAffixes.Count; i++)
                {
                    var a = e.RolledAffixes[i];
                    se.affixes[i] = new SerializedAffix
                    {
                        affixId = a.AffixId,
                        statType = a.StatType.ToString(),
                        value = a.Value,
                        isPercentage = a.IsPercentage
                    };
                }
            }

            return se;
        }
    }

    [Serializable]
    public class SerializedAffix
    {
        public string affixId;
        public string statType;
        public float value;
        public bool isPercentage;
    }

    [Serializable]
    public class SerializedSpeciesVisuals
    {
        public float[] skinColor = new float[4];
        public float[] hairColor = new float[4];
        public float[] eyesColor = new float[4];
        public float[] facialDetailColor = new float[4];
        public string chosenHairGroup;
        public float scaleX = 1f;
        public float scaleY = 1f;
    }
}
```

**Step 4: Update SaveManager.cs**

Note: SaveManager currently uses `JsonUtility` which doesn't support `Dictionary` or jagged arrays well. For the roster save format, we'll switch to MiniJSON for serialization. However, this is a larger change. For now, keep `JsonUtility` and accept that `varianceColorValues` (jagged array) won't serialize directly — we'll flatten it.

Actually, the simplest approach: keep `JsonUtility.ToJson/FromJson` for the SaveData root, and accept that nested jagged arrays (`float[][]`) won't serialize with JsonUtility. Instead, flatten variance colors into a single `float[]` with interleaved key+RGBA values.

Edit `Assets/Scripts/Managers/SaveData.cs` to change `SerializedEquipment.varianceColorValues` from `float[][]` to `float[]` (flattened):

Replace the variance color fields in `SerializedEquipment`:
```csharp
    public int[] varianceColorKeys;
    public float[] varianceColorValuesFlat; // flattened RGBA: [r0,g0,b0,a0,r1,g1,b1,a1,...]
```

And update `FromInstance` accordingly:
```csharp
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
```

Update `EquipmentFactory.Reconstruct` to read the flattened format:
```csharp
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
```

**Step 5: Run tests to verify they pass**

Expected: All 4 SaveRoundTripTests pass.

**Step 6: Commit**

```
git add Assets/Scripts/Managers/SaveData.cs Assets/Scripts/Managers/SaveManager.cs \
  Assets/Tests/EditMode/Save/SaveRoundTripTests.cs
git commit -m "Expand SaveData with roster, equipment, and affix serialization"
```

---

### Task 11: Update GameManager for Roster Integration

Replace hardcoded starter party with CharacterFactory + CharacterRoster.

**Files:**
- Modify: `Assets/Scripts/Managers/GameManager.cs`

**Step 1: Update GameManager**

Edit `Assets/Scripts/Managers/GameManager.cs`:

1. Add fields:
```csharp
        private CharacterRoster roster;
        private CharacterFactory characterFactory;
        private EquipmentCatalog equipmentCatalog;
        private EquipmentFactory equipmentFactory;
```

2. Replace `Awake()` initialization to set up factories:
```csharp
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            saveManager = GetComponent<SaveManager>();
            if (saveManager == null) saveManager = gameObject.AddComponent<SaveManager>();

            // Initialize equipment system
            equipmentCatalog = new EquipmentCatalog();
            equipmentCatalog.LoadFromResources();

            var affixTable = new AffixTable();
            affixTable.LoadFromResources();

            var registry = DisplayDataRegistry.Instance;
            if (registry.Species.Count == 0) registry.LoadAll();

            equipmentFactory = new EquipmentFactory(equipmentCatalog, affixTable, registry.Colors);

            var nameGen = new NameGenerator();
            nameGen.LoadFromResources();

            characterFactory = new CharacterFactory(registry, equipmentFactory, nameGen);
            roster = new CharacterRoster();

            party = new Party();
            verbPool = new VerbPool(economyConfig.verbSlotCount, economyConfig.verbDrawCooldown);
            combatProcessor = new CombatTickProcessor(advantageMatrix, economyConfig);
            exploration = new ExplorationManager(economyConfig);
            pityTracker = new PityTracker();
        }
```

3. Replace `Start()` to load/init roster:
```csharp
        private void Start()
        {
            var save = saveManager.Load();
            gold = save.gold;
            questLevel = save.currentQuestLevel;

            if (save.NeedsRosterInitialization())
                InitializeStarterRoster();
            else
                LoadRosterFromSave(save);

            BuildPartyFromRoster();
            SpawnWave();
            RebuildVerbPool();
        }
```

4. Replace `InitializeStarterParty()` with:
```csharp
        private void InitializeStarterRoster()
        {
            var rng = new System.Random();
            var characters = characterFactory.CreateStarterRoster(8, rng);
            foreach (var c in characters)
                roster.AddCharacter(c);
            roster.InitializeDefaultParty();
        }

        private void LoadRosterFromSave(SaveData save)
        {
            foreach (var sc in save.roster)
            {
                var character = sc.ToInstance();
                // Reconstruct equipment
                for (int i = 0; i < sc.equipment.Length; i++)
                {
                    if (sc.equipment[i] != null && !string.IsNullOrEmpty(sc.equipment[i].itemType))
                        character.equipment[i] = equipmentFactory.Reconstruct(sc.equipment[i]);
                }
                // Reconstruct verbs
                foreach (var verbId in sc.equippedVerbIds)
                    character.equippedVerbs.Add(CharacterFactory.CreateVerbById(verbId));
                roster.AddCharacter(character);
            }
            for (int i = 0; i < save.activePartyIndices.Length && i < 4; i++)
                roster.ActivePartyIndices[i] = save.activePartyIndices[i];
        }

        private void BuildPartyFromRoster()
        {
            party = new Party();
            var activeParty = roster.GetActiveParty();
            foreach (var c in activeParty)
                if (c != null) party.AddMember(c);
        }
```

5. Update save methods to include roster:
```csharp
        private void SaveState()
        {
            saveManager.CurrentSave.gold = gold;
            saveManager.CurrentSave.currentQuestLevel = questLevel;
            saveManager.CurrentSave.roster.Clear();
            foreach (var c in roster.Characters)
                saveManager.CurrentSave.roster.Add(SerializedCharacter.FromInstance(c));
            saveManager.CurrentSave.activePartyIndices = (int[])roster.ActivePartyIndices.Clone();
            saveManager.Save();
        }
```

Replace all 3 occurrences of the inline save code with `SaveState()`.

6. Add public `CreateVerbById` to `CharacterFactory` so GameManager can reconstruct verbs:
```csharp
        public static VerbDefinition CreateVerbById(string verbId)
        {
            foreach (var v in StarterVerbs)
                if (v.id == verbId) return CreateVerb(v.id, v.name, v.type, v.dmg, v.cd, v.heal, v.healAmt);
            return null;
        }
```

7. Add public roster property:
```csharp
        public CharacterRoster Roster => roster;
```

**Step 2: Verify compilation**

Expected: No compile errors.

**Step 3: Commit**

```
git add Assets/Scripts/Managers/GameManager.cs \
  Assets/Scripts/Characters/CharacterFactory.cs
git commit -m "Integrate CharacterRoster and factories into GameManager"
```

---

### Task 12: Wire Display Integration

Bridge EquipmentInstance → EquipmentDisplayInfo so paper dolls show equipment.

**Files:**
- Modify: `Assets/Scripts/UI/ExploreSceneController.cs`

**Step 1: Update SetupPlaceholderParty**

The current `SetupPlaceholderParty()` passes empty equipment lists to `DisplayBuilder.Build()`. Update it to use the roster's equipment data.

Edit `Assets/Scripts/UI/ExploreSceneController.cs`, replace `SetupPlaceholderParty()`:

```csharp
        private void SetupPlaceholderParty()
        {
            if (partySlots == null || partySlots.Length == 0) return;

            var registry = DisplayDataRegistry.Instance;
            if (registry.Species.Count == 0) registry.LoadAll();

            var builder = new DisplayBuilder(registry);
            var resolver = new ImageResolver();

            // If GameManager has a roster, use it
            CharacterInstance[] activeParty = null;
            if (gm != null && gm.Roster != null)
                activeParty = gm.Roster.GetActiveParty();

            var speciesKeys = registry.Species.Keys.ToList();
            if (speciesKeys.Count == 0) return;

            for (int i = 0; i < partySlots.Length && i < 4; i++)
            {
                if (partySlots[i] == null) continue;

                string speciesKey;
                EquipmentInstance[] equipment = null;

                if (activeParty != null && activeParty[i] != null)
                {
                    speciesKey = activeParty[i].speciesId;
                    equipment = activeParty[i].equipment;
                }
                else
                {
                    speciesKey = speciesKeys[i % speciesKeys.Count];
                }

                if (!registry.Species.TryGetValue(speciesKey, out var speciesData))
                {
                    speciesData = registry.Species[speciesKeys[i % speciesKeys.Count]];
                }

                var instance = SpeciesInstanceData.CreateFrom(speciesData, registry);

                // Build equipment display info list
                var equipDisplayList = new List<EquipmentDisplayInfo>();
                if (equipment != null)
                {
                    foreach (var eq in equipment)
                    {
                        if (eq == null) continue;
                        var info = new EquipmentDisplayInfo
                        {
                            ItemType = eq.ItemType,
                            ItemNum = eq.ItemNum,
                            BaseColor = eq.BaseColor,
                            VarianceColors = eq.VarianceColors as Dictionary<int, Color>
                                ?? new Dictionary<int, Color>(eq.VarianceColors),
                            IsOffhand = eq.Slot == EquipmentSlot.OffHand
                        };
                        equipDisplayList.Add(info);
                    }
                }

                var pieces = builder.Build(instance, speciesData, equipDisplayList);

                var displayObj = new GameObject($"CharDisplay_{i}");
                displayObj.transform.SetParent(transform);
                var display = displayObj.AddComponent<CharacterDisplay>();
                display.Initialize(resolver);
                display.SetPieces(pieces);

                partySlots[i].texture = display.Texture;
                characterDisplays.Add(display);
            }
        }
```

**Step 2: Move SetupPlaceholderParty after GameManager init**

Currently `SetupPlaceholderParty()` is called in `Start()` before `gm` is assigned. Move it to after `BindToGameManager()` or into `DeferredInitialSync()`.

In `Start()`, change the order:
```csharp
        private void Start()
        {
            SetupPlaceholderParallax();

            if (damageNumbers != null)
                spawnerRT = (RectTransform)damageNumbers.transform;

            gm = GameManager.Instance;
            if (gm != null)
            {
                BindToGameManager();
                StartCoroutine(DeferredInitialSync());
            }
            else
            {
                SetupPlaceholderParty(); // Only called as fallback
                SetupPlaceholderUI();
            }
        }
```

And add party setup in `DeferredInitialSync()`:
```csharp
        private IEnumerator DeferredInitialSync()
        {
            yield return null;
            if (gm == null) yield break;

            SetupPlaceholderParty(); // Now gm.Roster is available

            if (goldCounter != null)
                goldCounter.SetImmediate(gm.gold);
            // ... rest of sync
        }
```

**Step 3: Verify compilation and test play**

Build scene via Tools > Build Explore Scene. Play and confirm characters render with equipment visible.

**Step 4: Commit**

```
git add Assets/Scripts/UI/ExploreSceneController.cs
git commit -m "Wire equipment display from roster to paper doll rendering"
```

---

### Task 13: Rebuild Scene and Final Verification

**Step 1: Rebuild the scene**

Run `Tools > Build Explore Scene` or execute via MCP.

**Step 2: Save the scene**

Save `Assets/Scenes/ExploreScene`.

**Step 3: Play test**

- Confirm 4 party characters render with visible equipment (hats, armor, weapons)
- Confirm enemies spawn and combat works
- Confirm gold counter updates
- Confirm damage numbers appear

**Step 4: Run all tests**

Open Test Runner → EditMode → Run All.
Expected: ~125+ tests pass (99 existing + ~26 new).

**Step 5: Final commit**

```
git add -A
git commit -m "Sprint 5 complete: equipment factory, character generation, roster with save/load"
```

---

## Test Count Summary

| Test File | Tests |
|-----------|-------|
| EquipmentCatalogTests | 8 |
| EquipmentInstanceTests | 2 |
| AffixTableTests | 7 |
| EquipmentFactoryTests | 10 |
| CharacterRosterTests | 7 |
| CharacterFactoryTests | 6 |
| SaveRoundTripTests | 4 |
| **New total** | **~44** |
| **Existing** | **99** |
| **Grand total** | **~143** |

---

## New/Modified Files Summary

### New Files (16)
- `Assets/Scripts/Equipment/EquipmentCatalog.cs`
- `Assets/Scripts/Equipment/EquipmentInstance.cs`
- `Assets/Scripts/Equipment/EquipmentFactory.cs`
- `Assets/Scripts/Equipment/AffixTable.cs`
- `Assets/Scripts/Equipment/RolledAffix.cs`
- `Assets/Scripts/Characters/CharacterFactory.cs`
- `Assets/Scripts/Characters/CharacterRoster.cs`
- `Assets/Scripts/Characters/NameGenerator.cs`
- `Assets/Resources/Data/affixes.json`
- `Assets/Resources/Data/names.json`
- `Assets/Tests/EditMode/Equipment/EquipmentCatalogTests.cs`
- `Assets/Tests/EditMode/Equipment/EquipmentInstanceTests.cs`
- `Assets/Tests/EditMode/Equipment/AffixTableTests.cs`
- `Assets/Tests/EditMode/Equipment/EquipmentFactoryTests.cs`
- `Assets/Tests/EditMode/Characters/CharacterRosterTests.cs`
- `Assets/Tests/EditMode/Characters/CharacterFactoryTests.cs`
- `Assets/Tests/EditMode/Save/SaveRoundTripTests.cs`

### Modified Files (7)
- `Assets/Scripts/Core/MiniJSON.cs` (moved from Display)
- `Assets/Scripts/Core/SimpleJson.cs` (moved from Display, namespace changed)
- `Assets/Scripts/Display/DisplayDataRegistry.cs` (add using)
- `Assets/Scripts/Characters/CharacterInstance.cs` (EquipmentInstance[], speciesId)
- `Assets/Scripts/Characters/Starquill.Characters.asmdef` (add Equipment ref)
- `Assets/Scripts/Equipment/Starquill.Equipment.asmdef` (add Display ref)
- `Assets/Scripts/Managers/GameManager.cs` (roster integration)
- `Assets/Scripts/Managers/SaveData.cs` (expanded serialization)
- `Assets/Scripts/UI/ExploreSceneController.cs` (equipment display)
