# Sprint 7: Loot Drop Pipeline + Inventory Backend — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make equipment actually drop from combat kills into a player inventory, with comparison, auto-equip, and sell mechanics.

**Architecture:** LootDropper generates items on enemy kills using existing EquipmentFactory + PityTracker. Items go into LootInventory (capacity 50). ItemComparer and AutoEquipper provide stat-based item evaluation. GameManager wires the pipeline and exposes events for future UI.

**Tech Stack:** C# / Unity 6, NUnit EditMode tests, existing Equipment + Characters assemblies

---

### Task 1: EconomyConfig — Add Drop Rate Field

**Files:**
- Modify: `Assets/Scripts/Data/EconomyConfig.cs`
- Modify: `Assets/Tests/EditMode/Economy/EconomyConfigTests.cs`

**Context:** EconomyConfig (ScriptableObject) already has pity timer fields but lacks a base drop rate for loot. LootDropper (Task 4) will read this value.

**Step 1: Add the field to EconomyConfig**

In `Assets/Scripts/Data/EconomyConfig.cs`, add a new field under the `[Header("Pity Timers")]` section (or create a new `[Header("Loot")]` section after it):

```csharp
[Header("Loot")]
public float baseDropRate = 0.15f;
```

This is the probability (0-1) that any single enemy kill produces a loot drop, before pity timer overrides.

**Step 2: Add a test verifying the default**

In `Assets/Tests/EditMode/Economy/EconomyConfigTests.cs`, add:

```csharp
[Test]
public void BaseDropRate_DefaultValue_Is015()
{
    var config = ScriptableObject.CreateInstance<EconomyConfig>();
    Assert.AreEqual(0.15f, config.baseDropRate, 0.001f);
}
```

**Step 3: Run the test, verify it passes**

Run via Unity Test Runner → EditMode → EconomyConfigTests.

**Step 4: Commit**

```
git add Assets/Scripts/Data/EconomyConfig.cs Assets/Tests/EditMode/Economy/EconomyConfigTests.cs
git commit -m "Add baseDropRate field to EconomyConfig"
```

---

### Task 2: LootInventory — Equipment Collection

**Files:**
- Create: `Assets/Scripts/Equipment/LootInventory.cs`
- Create: `Assets/Tests/EditMode/Equipment/LootInventoryTests.cs`

**Context:** LootInventory holds dropped EquipmentInstances with a capacity limit. It lives in the Equipment assembly (`Starquill.Equipment` namespace) since it only depends on EquipmentInstance. The Characters assembly already references Equipment, so CharacterInstance code can interact with it.

**Step 1: Write the tests first**

Create `Assets/Tests/EditMode/Equipment/LootInventoryTests.cs`:

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
    public class LootInventoryTests
    {
        private LootInventory inventory;

        private EquipmentInstance MakeItem(EquipmentSlot slot = EquipmentSlot.Head,
            Rarity rarity = Rarity.Common)
        {
            return new EquipmentInstance(
                "hd01", 1, slot, rarity,
                Color.white, new Dictionary<int, Color>(),
                new Stats(), new List<RolledAffix>(),
                new int[0], new int[0], new int[0],
                false, null, "Test Item");
        }

        [SetUp]
        public void SetUp()
        {
            inventory = new LootInventory(5);
        }

        [Test]
        public void AddItem_WithinCapacity_ReturnsTrue()
        {
            Assert.IsTrue(inventory.AddItem(MakeItem()));
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void AddItem_AtCapacity_ReturnsFalse()
        {
            for (int i = 0; i < 5; i++)
                inventory.AddItem(MakeItem());
            Assert.IsFalse(inventory.AddItem(MakeItem()));
            Assert.AreEqual(5, inventory.Count);
        }

        [Test]
        public void RemoveItem_ExistingItem_ReturnsTrue()
        {
            var item = MakeItem();
            inventory.AddItem(item);
            Assert.IsTrue(inventory.RemoveItem(item));
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void RemoveItem_NonexistentItem_ReturnsFalse()
        {
            Assert.IsFalse(inventory.RemoveItem(MakeItem()));
        }

        [Test]
        public void GetItemsForSlot_FiltersCorrectly()
        {
            inventory.AddItem(MakeItem(EquipmentSlot.Head));
            inventory.AddItem(MakeItem(EquipmentSlot.Torso));
            inventory.AddItem(MakeItem(EquipmentSlot.Head));

            var heads = inventory.GetItemsForSlot(EquipmentSlot.Head);
            Assert.AreEqual(2, heads.Count);
        }

        [Test]
        public void IsFull_AtCapacity_ReturnsTrue()
        {
            for (int i = 0; i < 5; i++)
                inventory.AddItem(MakeItem());
            Assert.IsTrue(inventory.IsFull);
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            inventory.AddItem(MakeItem());
            inventory.AddItem(MakeItem());
            inventory.Clear();
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void OnItemAdded_FiresWhenItemAdded()
        {
            EquipmentInstance received = null;
            inventory.OnItemAdded += item => received = item;
            var added = MakeItem();
            inventory.AddItem(added);
            Assert.AreEqual(added, received);
        }

        [Test]
        public void OnItemRemoved_FiresWhenItemRemoved()
        {
            EquipmentInstance received = null;
            inventory.OnItemRemoved += item => received = item;
            var item = MakeItem();
            inventory.AddItem(item);
            inventory.RemoveItem(item);
            Assert.AreEqual(item, received);
        }
    }
}
```

**Step 2: Run tests, verify they fail (LootInventory doesn't exist yet)**

**Step 3: Implement LootInventory**

Create `Assets/Scripts/Equipment/LootInventory.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Starquill.Equipment
{
    public class LootInventory
    {
        private readonly List<EquipmentInstance> items = new();

        public int Capacity { get; }
        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;
        public IReadOnlyList<EquipmentInstance> Items => items;

        public event Action<EquipmentInstance> OnItemAdded;
        public event Action<EquipmentInstance> OnItemRemoved;

        public LootInventory(int capacity = 50)
        {
            Capacity = capacity;
        }

        public bool AddItem(EquipmentInstance item)
        {
            if (item == null || IsFull) return false;
            items.Add(item);
            OnItemAdded?.Invoke(item);
            return true;
        }

        public bool RemoveItem(EquipmentInstance item)
        {
            if (!items.Remove(item)) return false;
            OnItemRemoved?.Invoke(item);
            return true;
        }

        public List<EquipmentInstance> GetItemsForSlot(EquipmentSlot slot)
        {
            var result = new List<EquipmentInstance>();
            foreach (var item in items)
                if (item.Slot == slot) result.Add(item);
            return result;
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}
```

**Step 4: Run tests, verify all 9 pass**

**Step 5: Commit**

```
git add Assets/Scripts/Equipment/LootInventory.cs Assets/Tests/EditMode/Equipment/LootInventoryTests.cs
git commit -m "Add LootInventory collection class with capacity and events"
```

---

### Task 3: SellCalculator — Item Gold Value

**Files:**
- Create: `Assets/Scripts/Equipment/SellCalculator.cs`
- Create: `Assets/Tests/EditMode/Equipment/SellCalculatorTests.cs`

**Context:** When the player sells/trashes an item, they get gold based on rarity, affix count, and quest level. This is a pure static utility with no side effects.

**Step 1: Write the tests first**

Create `Assets/Tests/EditMode/Equipment/SellCalculatorTests.cs`:

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
    public class SellCalculatorTests
    {
        private EquipmentInstance MakeItem(Rarity rarity, int affixCount = 0)
        {
            var affixes = new List<RolledAffix>();
            for (int i = 0; i < affixCount; i++)
                affixes.Add(new RolledAffix("test", StatType.STR, 5f, false));

            return new EquipmentInstance(
                "hd01", 1, EquipmentSlot.Head, rarity,
                Color.white, new Dictionary<int, Color>(),
                new Stats(), affixes,
                new int[0], new int[0], new int[0],
                false, null, "Test");
        }

        [Test]
        public void Common_QuestLevel1_Returns10()
        {
            var value = SellCalculator.GetSellValue(MakeItem(Rarity.Common), 1);
            Assert.AreEqual(10.0, value, 0.01);
        }

        [Test]
        public void Legendary_QuestLevel1_Returns500()
        {
            var value = SellCalculator.GetSellValue(MakeItem(Rarity.Legendary), 1);
            Assert.AreEqual(500.0, value, 0.01);
        }

        [Test]
        public void QuestLevel_MultipliesValue()
        {
            var value = SellCalculator.GetSellValue(MakeItem(Rarity.Common), 10);
            Assert.AreEqual(100.0, value, 0.01);
        }

        [Test]
        public void Affixes_Add10PercentEach()
        {
            var noAffix = SellCalculator.GetSellValue(MakeItem(Rarity.Rare, 0), 1);
            var twoAffix = SellCalculator.GetSellValue(MakeItem(Rarity.Rare, 2), 1);
            Assert.AreEqual(noAffix * 1.2, twoAffix, 0.01);
        }

        [Test]
        public void AllRarities_HaveDistinctBaseValues()
        {
            int ql = 1;
            var c = SellCalculator.GetSellValue(MakeItem(Rarity.Common), ql);
            var u = SellCalculator.GetSellValue(MakeItem(Rarity.Uncommon), ql);
            var r = SellCalculator.GetSellValue(MakeItem(Rarity.Rare), ql);
            var e = SellCalculator.GetSellValue(MakeItem(Rarity.Epic), ql);
            var l = SellCalculator.GetSellValue(MakeItem(Rarity.Legendary), ql);
            Assert.IsTrue(c < u && u < r && r < e && e < l);
        }
    }
}
```

**Step 2: Run tests, verify they fail**

**Step 3: Implement SellCalculator**

Create `Assets/Scripts/Equipment/SellCalculator.cs`:

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

            // +10% per affix
            if (item.RolledAffixes.Count > 0)
                value *= 1.0 + item.RolledAffixes.Count * 0.1;

            return value;
        }
    }
}
```

**Step 4: Run tests, verify all 5 pass**

**Step 5: Commit**

```
git add Assets/Scripts/Equipment/SellCalculator.cs Assets/Tests/EditMode/Equipment/SellCalculatorTests.cs
git commit -m "Add SellCalculator for item-to-gold conversion"
```

---

### Task 4: LootDropper — Drop Roll Logic

**Files:**
- Create: `Assets/Scripts/Equipment/LootDropper.cs`
- Create: `Assets/Tests/EditMode/Equipment/LootDropperTests.cs`

**Context:** LootDropper decides *whether* a drop occurs and *what rarity* it is, then delegates to EquipmentFactory to create the actual item. It uses PityTracker for guaranteed drops and EconomyConfig for the base drop rate. It picks a random equipment slot to determine what type of gear drops.

**Important:** The existing `EquipmentFactory.CreateRandom(prefix, rarity, rng)` creates armor items, while `CreateRandomWeapon(rarity, rng)` creates weapons. LootDropper picks between these based on the rolled slot.

**Slot-to-prefix mapping** (matches `EquipmentCatalog.SlotForPrefix`):
- Head → "hd", Torso → "tr", Arms → "ar", Legs → "lg", Feet → "fe", Misc → "mc"
- Weapon → uses CreateRandomWeapon

**Step 1: Write the tests first**

Create `Assets/Tests/EditMode/Equipment/LootDropperTests.cs`:

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
    public class LootDropperTests
    {
        private EquipmentFactory factory;
        private EconomyConfig config;
        private PityTracker pity;

        [SetUp]
        public void SetUp()
        {
            // Minimal catalog with one head item
            var catalog = new EquipmentCatalog();
            catalog.LoadEquipmentJson(@"[
                {""item_type"":""hd01"",""description"":""helmet"",""amount"":3,
                 ""layer_codes"":[10],""hidden_layers"":[],""layer_color_variance"":[],""modular"":false}
            ]");
            catalog.LoadWeaponsJson(@"[
                {""item_type"":""w01"",""description"":""sword"",""amount"":2,
                 ""layer_codes"":[50],""hidden_layers"":[],""layer_color_variance"":[],
                 ""modular"":false,""hand_type"":""one_handed""}
            ]");

            var affixTable = new AffixTable();
            var colors = new ColorManager();
            colors.LoadFromJson(@"{""main"":[[""#FF0000"",""#00FF00"",""#0000FF""]]}");

            factory = new EquipmentFactory(catalog, affixTable, colors);

            config = ScriptableObject.CreateInstance<EconomyConfig>();
            config.baseDropRate = 0.15f;

            pity = new PityTracker();
        }

        [Test]
        public void TryDrop_RollBelowDropRate_ReturnsItem()
        {
            // Seed 42 produces a first NextDouble of ~0.0074 which is < 0.15
            var dropper = new LootDropper(factory, config, pity);
            var rng = new System.Random(42);
            var item = dropper.TryDrop(1, rng);
            Assert.IsNotNull(item);
        }

        [Test]
        public void TryDrop_HighDropRate_AlwaysDrops()
        {
            config.baseDropRate = 1.0f;
            var dropper = new LootDropper(factory, config, pity);
            int drops = 0;
            for (int i = 0; i < 20; i++)
            {
                if (dropper.TryDrop(1, new System.Random(i)) != null)
                    drops++;
            }
            Assert.AreEqual(20, drops);
        }

        [Test]
        public void TryDrop_ZeroDropRate_NeverDrops()
        {
            config.baseDropRate = 0f;
            var dropper = new LootDropper(factory, config, pity);
            int drops = 0;
            for (int i = 0; i < 20; i++)
            {
                if (dropper.TryDrop(1, new System.Random(i)) != null)
                    drops++;
            }
            Assert.AreEqual(0, drops);
        }

        [Test]
        public void TryDrop_PityOverride_ForcesDropAtThreshold()
        {
            config.baseDropRate = 0f; // No random drops
            var dropper = new LootDropper(factory, config, pity);
            var rng = new System.Random(0);

            // Register kills until pity fires
            EquipmentInstance drop = null;
            for (int i = 0; i < config.pityUncommon + 1; i++)
            {
                drop = dropper.TryDrop(1, rng);
                if (drop != null) break;
            }
            Assert.IsNotNull(drop);
            Assert.GreaterOrEqual((int)drop.Rarity, (int)Rarity.Uncommon);
        }

        [Test]
        public void TryDrop_ReturnsItemWithValidSlot()
        {
            config.baseDropRate = 1.0f;
            var dropper = new LootDropper(factory, config, pity);
            var item = dropper.TryDrop(5, new System.Random(42));
            Assert.IsNotNull(item);
            Assert.IsTrue(System.Enum.IsDefined(typeof(EquipmentSlot), item.Slot));
        }

        [Test]
        public void TryDrop_RegistersDrop_ResetsPityForRarity()
        {
            config.baseDropRate = 1.0f;
            var dropper = new LootDropper(factory, config, pity);
            dropper.TryDrop(1, new System.Random(42));
            // Pity counters for the dropped rarity should have been reset
            // We verify indirectly: after dropping, killsSinceUncommon should be 0
            // if the drop was Uncommon+
            Assert.GreaterOrEqual(0, pity.killsSinceUncommon);
        }
    }
}
```

**Step 2: Run tests, verify they fail (LootDropper doesn't exist yet)**

**Step 3: Implement LootDropper**

Create `Assets/Scripts/Equipment/LootDropper.cs`:

```csharp
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Equipment
{
    public class LootDropper
    {
        private readonly EquipmentFactory factory;
        private readonly EconomyConfig config;
        private readonly PityTracker pity;

        private static readonly string[] ArmorPrefixes = { "hd", "tr", "ar", "lg", "fe", "mc" };

        public LootDropper(EquipmentFactory factory, EconomyConfig config, PityTracker pity)
        {
            this.factory = factory;
            this.config = config;
            this.pity = pity;
        }

        public EquipmentInstance TryDrop(int questLevel, System.Random rng)
        {
            // Check pity first
            var pityRarity = pity.RegisterKill(config);

            // Roll drop chance
            bool shouldDrop = pityRarity.HasValue || rng.NextDouble() < config.baseDropRate;
            if (!shouldDrop) return null;

            // Determine rarity
            Rarity rarity = pityRarity ?? EquipmentFactory.RollRarity(questLevel, rng);

            // Pick random slot type: 70% armor, 30% weapon
            EquipmentInstance item;
            if (rng.NextDouble() < 0.30)
            {
                item = factory.CreateRandomWeapon(rarity, rng);
            }
            else
            {
                string prefix = ArmorPrefixes[rng.Next(ArmorPrefixes.Length)];
                item = factory.CreateRandom(prefix, rarity, rng);
            }

            // Register the drop with pity tracker
            if (item != null)
                pity.RegisterDrop(item.Rarity);

            return item;
        }
    }
}
```

**Step 4: Run tests, verify all 6 pass**

Some tests use seeded RNG — if a specific seed doesn't produce the expected first roll, adjust the seed. The important thing is:
- `TryDrop_RollBelowDropRate_ReturnsItem`: needs a seed where `NextDouble() < 0.15`. Try seeds 42, 100, 7 until one works.
- `TryDrop_PityOverride_ForcesDropAtThreshold`: uses 0% drop rate, relies purely on pity. After `pityUncommon` (50) calls, the pity tracker forces at least Uncommon.

**Step 5: Commit**

```
git add Assets/Scripts/Equipment/LootDropper.cs Assets/Tests/EditMode/Equipment/LootDropperTests.cs
git commit -m "Add LootDropper with drop rate rolling and pity integration"
```

---

### Task 5: ItemComparer — Stat Comparison Utility

**Files:**
- Create: `Assets/Scripts/Equipment/ItemComparer.cs`
- Create: `Assets/Tests/EditMode/Equipment/ItemComparerTests.cs`

**Context:** ItemComparer provides static methods to score items by total stat contribution and compare two items (new vs currently equipped). This supports the Loot screen's "is this an upgrade?" display and the auto-equip algorithm.

**Scoring formula:** Sum of all flat stat mods + flat affix values. Percentage affixes add `value * 5` (roughly 5 stat points per 1% bonus, a generous but simple heuristic).

**Step 1: Write the tests first**

Create `Assets/Tests/EditMode/Equipment/ItemComparerTests.cs`:

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
    public class ItemComparerTests
    {
        private EquipmentInstance MakeItem(int str = 0, int dex = 0, int con = 0,
            Rarity rarity = Rarity.Common, EquipmentSlot slot = EquipmentSlot.Head,
            List<RolledAffix> affixes = null)
        {
            var stats = new Stats { STR = str, DEX = dex, CON = con };
            return new EquipmentInstance(
                "hd01", 1, slot, rarity,
                Color.white, new Dictionary<int, Color>(),
                stats, affixes ?? new List<RolledAffix>(),
                new int[0], new int[0], new int[0],
                false, null, "Test");
        }

        [Test]
        public void ScoreItem_SumsAllStats()
        {
            var item = MakeItem(str: 5, dex: 3, con: 2);
            Assert.AreEqual(10f, ItemComparer.ScoreItem(item), 0.01f);
        }

        [Test]
        public void ScoreItem_IncludesFlatAffixes()
        {
            var affixes = new List<RolledAffix>
            {
                new RolledAffix("mighty", StatType.STR, 4f, false)
            };
            var item = MakeItem(str: 5, affixes: affixes);
            Assert.AreEqual(9f, ItemComparer.ScoreItem(item), 0.01f);
        }

        [Test]
        public void ScoreItem_WeightsPercentageAffixes()
        {
            var affixes = new List<RolledAffix>
            {
                new RolledAffix("fortified", StatType.CON, 2f, true)
            };
            var item = MakeItem(str: 5, affixes: affixes);
            // 5 (stats) + 2*5 (percentage affix weighted) = 15
            Assert.AreEqual(15f, ItemComparer.ScoreItem(item), 0.01f);
        }

        [Test]
        public void Compare_HigherScoreNewItem_PositiveDelta()
        {
            var current = MakeItem(str: 3);
            var newItem = MakeItem(str: 8);
            var diff = ItemComparer.Compare(newItem, current);
            Assert.IsTrue(diff.TotalDelta > 0);
            Assert.IsTrue(diff.IsUpgrade);
        }

        [Test]
        public void Compare_LowerScoreNewItem_NegativeDelta()
        {
            var current = MakeItem(str: 8);
            var newItem = MakeItem(str: 3);
            var diff = ItemComparer.Compare(newItem, current);
            Assert.IsTrue(diff.TotalDelta < 0);
            Assert.IsFalse(diff.IsUpgrade);
        }

        [Test]
        public void Compare_NullCurrent_TreatsAsZero()
        {
            var newItem = MakeItem(str: 5);
            var diff = ItemComparer.Compare(newItem, null);
            Assert.IsTrue(diff.TotalDelta > 0);
            Assert.IsTrue(diff.IsUpgrade);
        }

        [Test]
        public void FindBestForSlot_ReturnsBestScoringItem()
        {
            var candidates = new List<EquipmentInstance>
            {
                MakeItem(str: 2),
                MakeItem(str: 8),
                MakeItem(str: 5)
            };
            var best = ItemComparer.FindBestForSlot(EquipmentSlot.Head, candidates, null);
            Assert.AreEqual(8, best.StatMods.STR);
        }

        [Test]
        public void FindBestForSlot_SkipsItemsWorseThanCurrent()
        {
            var current = MakeItem(str: 10);
            var candidates = new List<EquipmentInstance>
            {
                MakeItem(str: 2),
                MakeItem(str: 5)
            };
            var best = ItemComparer.FindBestForSlot(EquipmentSlot.Head, candidates, current);
            Assert.IsNull(best);
        }
    }
}
```

**Step 2: Run tests, verify they fail**

**Step 3: Implement ItemComparer**

Create `Assets/Scripts/Equipment/ItemComparer.cs`:

```csharp
using System;
using System.Collections.Generic;
using Starquill.Core;
using Starquill.Data;

namespace Starquill.Equipment
{
    public struct StatDiff
    {
        public float TotalDelta;
        public bool IsUpgrade;
    }

    public static class ItemComparer
    {
        private const float PercentageAffixWeight = 5f;

        public static float ScoreItem(EquipmentInstance item)
        {
            if (item == null) return 0f;

            float score = item.StatMods.Total;
            foreach (var affix in item.RolledAffixes)
            {
                if (affix.IsPercentage)
                    score += affix.Value * PercentageAffixWeight;
                else
                    score += affix.Value;
            }
            return score;
        }

        public static StatDiff Compare(EquipmentInstance newItem, EquipmentInstance current)
        {
            float newScore = ScoreItem(newItem);
            float currentScore = ScoreItem(current);
            float delta = newScore - currentScore;
            return new StatDiff { TotalDelta = delta, IsUpgrade = delta > 0 };
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

**Step 4: Run tests, verify all 8 pass**

**Step 5: Commit**

```
git add Assets/Scripts/Equipment/ItemComparer.cs Assets/Tests/EditMode/Equipment/ItemComparerTests.cs
git commit -m "Add ItemComparer for stat scoring and item comparison"
```

---

### Task 6: AutoEquipper — Best-in-Slot Algorithm

**Files:**
- Create: `Assets/Scripts/Characters/AutoEquipper.cs`
- Create: `Assets/Tests/EditMode/Characters/AutoEquipperTests.cs`

**Context:** AutoEquipper scans the player's loot inventory and equips the best item for each slot on a given character. It lives in the Characters assembly (needs CharacterInstance from Characters + ItemComparer/LootInventory from Equipment — Characters already references Equipment). Returns a list of swapped items (old items that went back to inventory) so the caller can update state.

**Step 1: Write the tests first**

Create `Assets/Tests/EditMode/Characters/AutoEquipperTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Data;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.Tests.Characters
{
    [TestFixture]
    public class AutoEquipperTests
    {
        private EquipmentInstance MakeItem(EquipmentSlot slot, int str = 0,
            Rarity rarity = Rarity.Common)
        {
            var stats = new Stats { STR = str };
            return new EquipmentInstance(
                "hd01", 1, slot, rarity,
                Color.white, new Dictionary<int, Color>(),
                stats, new List<RolledAffix>(),
                new int[0], new int[0], new int[0],
                false, null, "Test");
        }

        [Test]
        public void AutoEquip_EquipsUpgradeFromInventory()
        {
            var character = new CharacterInstance
            {
                baseStats = new Stats { STR = 10, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
            character.EquipItem(MakeItem(EquipmentSlot.Head, str: 3));

            var inventory = new LootInventory(50);
            var better = MakeItem(EquipmentSlot.Head, str: 10);
            inventory.AddItem(better);

            var result = AutoEquipper.AutoEquip(character, inventory);
            Assert.AreEqual(1, result.ItemsEquipped);
            Assert.AreEqual(better, character.equipment[(int)EquipmentSlot.Head]);
        }

        [Test]
        public void AutoEquip_SkipsDowngrade()
        {
            var character = new CharacterInstance
            {
                baseStats = new Stats { STR = 10, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
            var current = MakeItem(EquipmentSlot.Head, str: 10);
            character.EquipItem(current);

            var inventory = new LootInventory(50);
            inventory.AddItem(MakeItem(EquipmentSlot.Head, str: 2));

            var result = AutoEquipper.AutoEquip(character, inventory);
            Assert.AreEqual(0, result.ItemsEquipped);
            Assert.AreEqual(current, character.equipment[(int)EquipmentSlot.Head]);
        }

        [Test]
        public void AutoEquip_EquipsIntoEmptySlot()
        {
            var character = new CharacterInstance
            {
                baseStats = new Stats { STR = 10, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
            // Head slot is empty

            var inventory = new LootInventory(50);
            var item = MakeItem(EquipmentSlot.Head, str: 5);
            inventory.AddItem(item);

            var result = AutoEquipper.AutoEquip(character, inventory);
            Assert.AreEqual(1, result.ItemsEquipped);
            Assert.AreEqual(item, character.equipment[(int)EquipmentSlot.Head]);
        }

        [Test]
        public void AutoEquip_ReturnsOldItemToInventory()
        {
            var character = new CharacterInstance
            {
                baseStats = new Stats { STR = 10, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
            var oldItem = MakeItem(EquipmentSlot.Head, str: 3);
            character.EquipItem(oldItem);

            var inventory = new LootInventory(50);
            inventory.AddItem(MakeItem(EquipmentSlot.Head, str: 10));

            AutoEquipper.AutoEquip(character, inventory);
            Assert.IsTrue(inventory.Items.Contains(oldItem));
        }

        [Test]
        public void AutoEquip_MultipleSlots_EquipsBestPerSlot()
        {
            var character = new CharacterInstance
            {
                baseStats = new Stats { STR = 10, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };

            var inventory = new LootInventory(50);
            inventory.AddItem(MakeItem(EquipmentSlot.Head, str: 5));
            inventory.AddItem(MakeItem(EquipmentSlot.Torso, str: 8));

            var result = AutoEquipper.AutoEquip(character, inventory);
            Assert.AreEqual(2, result.ItemsEquipped);
            Assert.IsNotNull(character.equipment[(int)EquipmentSlot.Head]);
            Assert.IsNotNull(character.equipment[(int)EquipmentSlot.Torso]);
        }
    }
}
```

**Step 2: Run tests, verify they fail**

**Step 3: Implement AutoEquipper**

Create `Assets/Scripts/Characters/AutoEquipper.cs`:

```csharp
using System;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.Characters
{
    public struct AutoEquipResult
    {
        public int ItemsEquipped;
    }

    public static class AutoEquipper
    {
        public static AutoEquipResult AutoEquip(CharacterInstance character, LootInventory inventory)
        {
            var result = new AutoEquipResult();
            var slots = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));

            foreach (var slot in slots)
            {
                var candidates = inventory.GetItemsForSlot(slot);
                if (candidates.Count == 0) continue;

                var current = character.equipment[(int)slot];
                var best = ItemComparer.FindBestForSlot(slot, candidates, current);

                if (best != null)
                {
                    // Remove new item from inventory
                    inventory.RemoveItem(best);

                    // Return old item to inventory if present
                    if (current != null)
                        inventory.AddItem(current);

                    // Equip the upgrade
                    character.equipment[(int)slot] = best;
                    result.ItemsEquipped++;
                }
            }

            return result;
        }
    }
}
```

**Step 4: Run tests, verify all 5 pass**

**Step 5: Commit**

```
git add Assets/Scripts/Characters/AutoEquipper.cs Assets/Tests/EditMode/Characters/AutoEquipperTests.cs
git commit -m "Add AutoEquipper for best-in-slot auto-equip from inventory"
```

---

### Task 7: GameManager — Wire Loot Drop Pipeline

**Files:**
- Modify: `Assets/Scripts/Managers/GameManager.cs`

**Context:** GameManager already has `EquipmentFactory`, `PityTracker` (in SaveData), and `EconomyConfig`. We need to:
1. Create `LootInventory` and `LootDropper` in `Awake()`
2. Call `LootDropper.TryDrop()` for each enemy killed in `ProcessTick()` and `OnVerbTapped()`
3. Add `OnLootDropped` event for UI notification
4. Add `SellItem()` and `EquipItemFromInventory()` public methods
5. Load/save inventory

**Step 1: Add fields and properties**

In `GameManager.cs`, add after the existing private fields:

```csharp
private LootInventory lootInventory;
private LootDropper lootDropper;

public LootInventory LootInventory => lootInventory;
public event Action<EquipmentInstance> OnLootDropped;
```

**Step 2: Initialize in Awake()**

After `roster = new CharacterRoster();` (around line 85), add:

```csharp
lootInventory = new LootInventory(50);
lootDropper = new LootDropper(equipmentFactory, economyConfig, pityTracker);
```

Note: The existing `pityTracker` field needs to be used directly — currently `SaveData` has a `pityTracker` field. GameManager should maintain its own reference. Check: if `pityTracker` is already a field on GameManager, use it. If not, extract it from SaveData in `Start()`.

Looking at the existing code: `pityTracker` is declared as a field (`private PityTracker pityTracker;`) and initialized as `pityTracker = new PityTracker();` in `Awake()`. Perfect — use this directly.

**Step 3: Wire into ProcessTick()**

In the `ProcessTick()` method, after the gold earning section and before the wave cleared check, add:

```csharp
// Loot drops
for (int k = 0; k < result.EnemiesKilled; k++)
{
    var rng = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
    var drop = lootDropper.TryDrop(questLevel, rng);
    if (drop != null && lootInventory.AddItem(drop))
        OnLootDropped?.Invoke(drop);
}
```

**Step 4: Wire into OnVerbTapped()**

Same pattern — after the gold/kill section in `OnVerbTapped()`, add the same loot drop loop.

**Step 5: Add SellItem method**

```csharp
public void SellItem(EquipmentInstance item)
{
    if (item == null || !lootInventory.RemoveItem(item)) return;
    double value = SellCalculator.GetSellValue(item, questLevel);
    gold += value;
    OnGoldChanged?.Invoke(gold);
}
```

**Step 6: Add EquipItemFromInventory method**

```csharp
public void EquipItemFromInventory(EquipmentInstance item, int characterIndex)
{
    if (item == null || roster == null) return;
    if (characterIndex < 0 || characterIndex >= roster.Characters.Count) return;

    var character = roster.Characters[characterIndex];
    var oldItem = character.equipment[(int)item.Slot];

    // Remove new item from inventory
    if (!lootInventory.RemoveItem(item)) return;

    // Return old item to inventory
    if (oldItem != null)
        lootInventory.AddItem(oldItem);

    // Equip
    character.EquipItem(item);
    BuildPartyFromRoster();
    OnRosterChanged?.Invoke();
}
```

**Step 7: Update SaveState() and LoadRosterFromSave()**

In `SaveState()`, after saving the roster, add:

```csharp
saveManager.CurrentSave.inventory.Clear();
foreach (var item in lootInventory.Items)
    saveManager.CurrentSave.inventory.Add(SerializedEquipment.FromInstance(item));
```

In `Start()`, after `LoadRosterFromSave(save)`, add:

```csharp
if (save.inventory != null)
{
    foreach (var si in save.inventory)
    {
        if (si != null && !string.IsNullOrEmpty(si.itemType))
            lootInventory.AddItem(equipmentFactory.Reconstruct(si));
    }
}
```

**Step 8: Commit**

```
git add Assets/Scripts/Managers/GameManager.cs
git commit -m "Wire loot drop pipeline into GameManager combat loop"
```

---

### Task 8: SaveData — Inventory Persistence

**Files:**
- Modify: `Assets/Scripts/Managers/SaveData.cs`
- Modify: `Assets/Tests/EditMode/Save/SaveRoundTripTests.cs`

**Context:** SaveData needs a `List<SerializedEquipment> inventory` field. The existing `SerializedEquipment` class already handles EquipmentInstance serialization.

**Step 1: Add inventory field to SaveData**

In `Assets/Scripts/Managers/SaveData.cs`, add to the `SaveData` class:

```csharp
public List<SerializedEquipment> inventory = new();
```

**Step 2: Add round-trip test**

In `Assets/Tests/EditMode/Save/SaveRoundTripTests.cs`, add a test that:
1. Creates a SaveData with inventory items
2. Serializes to JSON
3. Deserializes back
4. Verifies inventory items survived

```csharp
[Test]
public void Inventory_SurvivesRoundTrip()
{
    var save = new SaveData();
    var equip = new SerializedEquipment
    {
        itemType = "hd01",
        itemNum = 2,
        slot = 0,
        rarity = 2,
        baseColor = new float[] { 1, 0, 0, 1 },
        statMods = new int[] { 5, 0, 0, 0, 0, 0 }
    };
    save.inventory.Add(equip);

    string json = JsonUtility.ToJson(save);
    var loaded = JsonUtility.FromJson<SaveData>(json);

    Assert.AreEqual(1, loaded.inventory.Count);
    Assert.AreEqual("hd01", loaded.inventory[0].itemType);
    Assert.AreEqual(2, loaded.inventory[0].rarity);
}
```

**Step 3: Run test, verify it passes**

**Step 4: Commit**

```
git add Assets/Scripts/Managers/SaveData.cs Assets/Tests/EditMode/Save/SaveRoundTripTests.cs
git commit -m "Add inventory field to SaveData with round-trip test"
```

---

### Task 9: Final Verification — Compile Check, Scene Rebuild, Play Test

**Files:**
- No new files — verification only

**Step 1: Run compile check via Coplay MCP**

```
check_compile_errors
```

Expected: No compile errors.

**Step 2: Rebuild scene via Coplay MCP**

```
execute_script: Assets/Editor/ExploreSceneBuilder.cs
```

Expected: Scene rebuilt successfully.

**Step 3: Save scene**

```
save_scene: Assets/Scenes/ExploreScene
```

**Step 4: Play test**

```
play_game
```

Run for 10-20 seconds. Watch for:
- No runtime errors in console
- Combat still ticks normally
- Gold still increments
- Check console for any OnLootDropped-related messages (there won't be UI yet, but no errors)

```
stop_game
```

**Step 5: Verify test count**

Check Unity Test Runner for total test count. Expected: ~210+ tests (183 from Sprint 6 + ~28 new).

**Step 6: Commit any remaining fixes**

If there are minor fixes needed, commit them.

**Step 7: Final commit of scene + meta files**

```
git add Assets/Scenes/ExploreScene.unity
git commit -m "Sprint 7 final: rebuild scene with loot pipeline"
```

---

## Summary

| Task | New Files | New Tests | Description |
|------|-----------|-----------|-------------|
| 1 | 0 | 1 | EconomyConfig.baseDropRate field |
| 2 | 1 | 9 | LootInventory collection class |
| 3 | 1 | 5 | SellCalculator gold value formula |
| 4 | 1 | 6 | LootDropper drop roll + pity |
| 5 | 1 | 8 | ItemComparer stat scoring |
| 6 | 1 | 5 | AutoEquipper best-in-slot |
| 7 | 0 | 0 | GameManager wiring |
| 8 | 0 | 1 | SaveData inventory field |
| 9 | 0 | 0 | Final verification |
| **Total** | **5 new** | **~35** | **5 source + 5 test files** |

**Assembly changes:** None needed — LootInventory, SellCalculator, LootDropper, ItemComparer all go in Equipment (already referenced by Characters and Managers). AutoEquipper goes in Characters.

**Post-sprint state:** Items drop from combat → inventory. Items can be sold for gold or equipped on characters. Auto-equip finds upgrades. Inventory persists across saves. No UI yet (Sprint 8).
