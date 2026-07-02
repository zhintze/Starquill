# Sprint D1: Keys + Dungeons Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the key system (drops, pouch, fusion) and key-opened dungeon timed wave rushes, per `docs/plans/2026-07-02-destinations-design.md` (rev 3).

**Architecture:** New `Starquill.Destinations` assembly (plain C#, deterministic generation, quest-assembly pattern) holds key/dungeon domain logic; `Starquill.Core` gains the ColorFamily classifier; `EquipmentFactory`/`LootDropper` gain modifier passthrough; `GameManager` orchestrates drops, dungeon lifecycle, and reward execution; UI extends the Quests screen Destinations section plus an in-run HUD on the explore screen.

**Tech Stack:** Unity 6 (6000.3.8f1), C#, NUnit EditMode tests, Coplay MCP for compile checks and play-mode verification.

---

## Testing reality on this machine (READ FIRST)

Tests **cannot run headless** (Arch batch-mode issue) and scripted TestRunnerApi runs have crashed the editor — **never script test runs**. The TDD loop adapts to:

1. Write the failing test (file under `Assets/Tests/EditMode/Destinations/`).
2. Write the implementation.
3. Compile check: `mcp__coplay-mcp__check_compile_errors` (this is the "run" step; a compile error = red).
4. Commit.
5. **Checkpoint batches:** at the two marked checkpoints, the user runs Window > General > Test Runner > EditMode > Run All and confirms green before proceeding.

Conventions that bite (from CLAUDE.md): qualify `UnityEngine.Random.Range`; `IReadOnlyList.Contains()` needs `using System.Linq;`; no Unicode symbols in TMP text (LiberationSans SDF lacks them — use UiFactory Image pips/chips); zero `sizeDelta` on stretch-anchored roots; no `Co-Authored-By` lines; commit to `unity-idle-clicker` only.

---

### Task 1: ColorFamily enum + classifier (Core)

The classifier must live in **Core** (needed by Display, Equipment, Destinations, UI — lowest common assembly).

**Files:**
- Create: `Assets/Scripts/Core/ColorFamily.cs`
- Test: `Assets/Tests/EditMode/Destinations/ColorFamilyTests.cs`

**Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using Starquill.Core;

namespace Starquill.Tests.Destinations
{
    public class ColorFamilyTests
    {
        [TestCase(1f, 0f, 0f, ColorFamily.Red)]
        [TestCase(0.55f, 0.27f, 0.07f, ColorFamily.Brown)]   // saddle brown (dark orange hue)
        [TestCase(1f, 0.55f, 0f, ColorFamily.Orange)]        // bright orange
        [TestCase(1f, 0.9f, 0.1f, ColorFamily.Yellow)]
        [TestCase(0.1f, 0.7f, 0.2f, ColorFamily.Green)]
        [TestCase(0.1f, 0.3f, 0.9f, ColorFamily.Blue)]
        [TestCase(0.6f, 0.1f, 0.8f, ColorFamily.Purple)]
        [TestCase(0.5f, 0.5f, 0.5f, ColorFamily.Neutral)]    // gray: low saturation
        [TestCase(0.05f, 0.05f, 0.05f, ColorFamily.Neutral)] // near-black: low value
        [TestCase(1f, 1f, 1f, ColorFamily.Neutral)]          // white
        public void Classify_KnownColors(float r, float g, float b, ColorFamily expected)
        {
            Assert.AreEqual(expected, ColorFamilyClassifier.Classify(r, g, b));
        }

        [Test]
        public void Classify_IsTotal_OverRandomColors()
        {
            var rng = new System.Random(42);
            for (int i = 0; i < 1000; i++)
            {
                float r = (float)rng.NextDouble(), g = (float)rng.NextDouble(), b = (float)rng.NextDouble();
                var family = ColorFamilyClassifier.Classify(r, g, b);
                Assert.IsTrue(System.Enum.IsDefined(typeof(ColorFamily), family));
            }
        }
    }
}
```

**Step 2: Write the implementation**

```csharp
namespace Starquill.Core
{
    public enum ColorFamily
    {
        Red, Orange, Brown, Yellow, Green, Blue, Purple, Neutral
    }

    /// Pure RGB -> family bucketing (design D1: 8 HSV buckets over the
    /// "main" palette). Ranges are starting values; tune against the real
    /// palette during the balance pass, keeping Classify total.
    public static class ColorFamilyClassifier
    {
        public static ColorFamily Classify(float r, float g, float b)
        {
            RgbToHsv(r, g, b, out float h, out float s, out float v);

            if (s < 0.15f || v < 0.12f) return ColorFamily.Neutral;

            float deg = h * 360f;
            if (deg < 15f || deg >= 345f) return ColorFamily.Red;
            if (deg < 45f) return v < 0.55f ? ColorFamily.Brown : ColorFamily.Orange;
            if (deg < 70f) return ColorFamily.Yellow;
            if (deg < 170f) return ColorFamily.Green;
            if (deg < 260f) return ColorFamily.Blue;
            return ColorFamily.Purple;
        }

        private static void RgbToHsv(float r, float g, float b,
            out float h, out float s, out float v)
        {
            float max = System.Math.Max(r, System.Math.Max(g, b));
            float min = System.Math.Min(r, System.Math.Min(g, b));
            float delta = max - min;

            v = max;
            s = max <= 0f ? 0f : delta / max;

            if (delta <= 0f) { h = 0f; return; }
            if (max == r) h = ((g - b) / delta % 6f) / 6f;
            else if (max == g) h = ((b - r) / delta + 2f) / 6f;
            else h = ((r - g) / delta + 4f) / 6f;
            if (h < 0f) h += 1f;
        }
    }
}
```

**Step 3: Compile check** — `check_compile_errors`, expect clean.

**Step 4: Commit** — `feat: ColorFamily enum + HSV classifier in Core`

---

### Task 2: ColorManager family filtering (Display)

**Files:**
- Modify: `Assets/Scripts/Display/ColorManager.cs` (add family index + filtered getter)
- Test: `Assets/Tests/EditMode/Destinations/ColorManagerFamilyTests.cs`

**Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Display;

namespace Starquill.Tests.Destinations
{
    public class ColorManagerFamilyTests
    {
        private ColorManager NewManager()
        {
            var cm = new ColorManager();
            // red, dark red, green, gray, brown-ish
            cm.LoadFromJson("{\"main\": [\"FF0000\", \"AA1111\", \"22CC44\", \"888888\", \"8B4513\"]}");
            return cm;
        }

        [Test]
        public void GetRandomColor_WithFamily_ReturnsOnlyFamilyMembers()
        {
            var cm = NewManager();
            var rng = new System.Random(7);
            for (int i = 0; i < 50; i++)
            {
                var c = cm.GetRandomColor("main", rng, ColorFamily.Red);
                Assert.AreEqual(ColorFamily.Red, ColorFamilyClassifier.Classify(c.r, c.g, c.b));
            }
        }

        [Test]
        public void GetRandomColor_EmptyFamily_FallsBackToWholePalette()
        {
            var cm = NewManager(); // palette has no Blue entries
            var rng = new System.Random(7);
            var c = cm.GetRandomColor("main", rng, ColorFamily.Blue);
            Assert.IsNotNull(c); // no throw, any palette color acceptable
        }
    }
}
```

**Step 2: Implement** — in `ColorManager`, add a lazily built per-palette family index and:

```csharp
private readonly Dictionary<string, Dictionary<ColorFamily, Color[]>> familyIndex = new();

public Color GetRandomColor(string paletteName, System.Random rng, ColorFamily? family)
{
    if (!family.HasValue) return GetRandomColor(paletteName, rng);

    if (!familyIndex.TryGetValue(paletteName, out var byFamily))
    {
        byFamily = BuildFamilyIndex(GetPalette(paletteName));
        familyIndex[paletteName] = byFamily;
    }

    if (byFamily.TryGetValue(family.Value, out var members) && members.Length > 0)
        return members[rng.Next(members.Length)];
    return GetRandomColor(paletteName, rng); // family empty in this palette
}

private static Dictionary<ColorFamily, Color[]> BuildFamilyIndex(Color[] palette)
{
    var lists = new Dictionary<ColorFamily, List<Color>>();
    foreach (var c in palette)
    {
        var f = ColorFamilyClassifier.Classify(c.r, c.g, c.b);
        if (!lists.TryGetValue(f, out var list)) { list = new List<Color>(); lists[f] = list; }
        list.Add(c);
    }
    var result = new Dictionary<ColorFamily, Color[]>();
    foreach (var kvp in lists) result[kvp.Key] = kvp.Value.ToArray();
    return result;
}
```

Add `using System.Collections.Generic;` (already present) and `using Starquill.Core;`. **Check the Display asmdef references Starquill.Core** (`Assets/Scripts/Display/Starquill.Display.asmdef`) — add if missing. Clear `familyIndex` in `LoadFromJson`.

**Step 3: Compile check. Step 4: Commit** — `feat: ColorManager palette family filtering`

---

### Task 3: Destinations assembly + ClassArchetype table

**Files:**
- Create: `Assets/Scripts/Destinations/Starquill.Destinations.asmdef`
- Create: `Assets/Scripts/Destinations/ClassArchetype.cs`
- Test: `Assets/Tests/EditMode/Destinations/ClassArchetypeTests.cs`
- Modify: `Assets/Tests/EditMode/EditModeTests.asmdef` (add `"Starquill.Destinations"` to references)

**Step 1: asmdef** (mirrors `Starquill.Quests.asmdef`):

```json
{
    "name": "Starquill.Destinations",
    "rootNamespace": "Starquill.Destinations",
    "references": ["Starquill.Core", "Starquill.Data", "Starquill.Quests"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true
}
```

**Step 2: Write the failing test**

```csharp
using System.Linq;
using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class ClassArchetypeTests
    {
        [Test]
        public void All_ContainsExactly15_OnePerStatPair()
        {
            Assert.AreEqual(15, ClassArchetype.All.Count);
            var pairs = ClassArchetype.All
                .Select(a => (Min: (int)a.StatA < (int)a.StatB ? a.StatA : a.StatB,
                              Max: (int)a.StatA < (int)a.StatB ? a.StatB : a.StatA))
                .Distinct().Count();
            Assert.AreEqual(15, pairs);
        }

        [Test]
        public void ForPair_FindsArchetype_RegardlessOfOrder()
        {
            var a = ClassArchetype.ForPair(StatType.STR, StatType.DEX);
            var b = ClassArchetype.ForPair(StatType.DEX, StatType.STR);
            Assert.AreEqual("Warrior", a.Name);
            Assert.AreSame(a, b);
        }

        [Test]
        public void ById_RoundTrips()
        {
            foreach (var a in ClassArchetype.All)
                Assert.AreSame(a, ClassArchetype.ById(a.Id));
        }
    }
}
```

**Step 3: Implement**

```csharp
using System.Collections.Generic;
using Starquill.Core;

namespace Starquill.Destinations
{
    /// The 15 two-stat build archetypes class keys target (design table §2).
    /// Ids are stable save-format contract — never reorder or reuse.
    public class ClassArchetype
    {
        public int Id { get; }
        public string Name { get; }
        public StatType StatA { get; }
        public StatType StatB { get; }

        private ClassArchetype(int id, string name, StatType a, StatType b)
        { Id = id; Name = name; StatA = a; StatB = b; }

        public static readonly IReadOnlyList<ClassArchetype> All = new[]
        {
            new ClassArchetype(0,  "Warrior",    StatType.STR, StatType.DEX),
            new ClassArchetype(1,  "Juggernaut", StatType.STR, StatType.CON),
            new ClassArchetype(2,  "Battlemage", StatType.STR, StatType.INT),
            new ClassArchetype(3,  "Warden",     StatType.STR, StatType.WIS),
            new ClassArchetype(4,  "Warlord",    StatType.STR, StatType.CHA),
            new ClassArchetype(5,  "Skirmisher", StatType.DEX, StatType.CON),
            new ClassArchetype(6,  "Saboteur",   StatType.DEX, StatType.INT),
            new ClassArchetype(7,  "Ranger",     StatType.DEX, StatType.WIS),
            new ClassArchetype(8,  "Trickster",  StatType.DEX, StatType.CHA),
            new ClassArchetype(9,  "Sentinel",   StatType.CON, StatType.INT),
            new ClassArchetype(10, "Guardian",   StatType.CON, StatType.WIS),
            new ClassArchetype(11, "Champion",   StatType.CON, StatType.CHA),
            new ClassArchetype(12, "Sage",       StatType.INT, StatType.WIS),
            new ClassArchetype(13, "Occultist",  StatType.INT, StatType.CHA),
            new ClassArchetype(14, "Oracle",     StatType.WIS, StatType.CHA),
        };

        public static ClassArchetype ById(int id) =>
            id >= 0 && id < All.Count ? All[id] : null;

        public static ClassArchetype ForPair(StatType a, StatType b)
        {
            foreach (var arch in All)
                if ((arch.StatA == a && arch.StatB == b) || (arch.StatA == b && arch.StatB == a))
                    return arch;
            return null;
        }
    }
}
```

**Step 4: Compile check. Step 5: Commit** — `feat: Starquill.Destinations assembly + 15 class archetypes`

---

### Task 4: KeyInstance

**Files:**
- Create: `Assets/Scripts/Destinations/KeyInstance.cs`
- Test: `Assets/Tests/EditMode/Destinations/KeyInstanceTests.cs`

**Step 1: Failing test**

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class KeyInstanceTests
    {
        [Test]
        public void DisplayName_BaseColorKey()
        {
            var key = new KeyInstance { ColorFamily = ColorFamily.Brown, Difficulty = 1 };
            Assert.AreEqual("Brown Key", key.DisplayName);
        }

        [Test]
        public void DisplayName_TripleFused()
        {
            var key = new KeyInstance
            {
                ColorFamily = ColorFamily.Brown,
                Slot = KeySlot.Head,
                ArchetypeId = 0, // Warrior
                Difficulty = 3
            };
            Assert.AreEqual("Brown Warrior Helm Key", key.DisplayName);
        }

        [Test]
        public void ModifierCount_CountsSetModifiers()
        {
            Assert.AreEqual(1, new KeyInstance { Slot = KeySlot.Weapon }.ModifierCount);
            Assert.AreEqual(2, new KeyInstance { ColorFamily = ColorFamily.Red, ArchetypeId = 4 }.ModifierCount);
        }
    }
}
```

**Step 2: Implement**

```csharp
using Starquill.Core;

namespace Starquill.Destinations
{
    /// Slots a type key can target (design §2). Weapon covers MainHand/OffHand;
    /// Misc covers Misc1-4.
    public enum KeySlot { Head, Torso, Arms, Legs, Feet, Weapon, Misc }

    /// A dungeon key: up to one modifier of each kind + difficulty (design §2).
    public class KeyInstance
    {
        public ColorFamily? ColorFamily;
        public KeySlot? Slot;
        public int ArchetypeId = -1;   // -1 = no class modifier
        public int Difficulty = 1;

        public ClassArchetype Archetype =>
            ArchetypeId >= 0 ? ClassArchetype.ById(ArchetypeId) : null;

        public int ModifierCount =>
            (ColorFamily.HasValue ? 1 : 0) + (Slot.HasValue ? 1 : 0) + (ArchetypeId >= 0 ? 1 : 0);

        private static string SlotWord(KeySlot slot) => slot switch
        {
            KeySlot.Head => "Helm",
            KeySlot.Torso => "Armor",
            KeySlot.Arms => "Gauntlet",
            KeySlot.Legs => "Greaves",
            KeySlot.Feet => "Boot",
            KeySlot.Weapon => "Weapon",
            KeySlot.Misc => "Trinket",
            _ => ""
        };

        public string DisplayName
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (ColorFamily.HasValue) parts.Add(ColorFamily.Value.ToString());
                if (Archetype != null) parts.Add(Archetype.Name);
                if (Slot.HasValue) parts.Add(SlotWord(Slot.Value));
                parts.Add("Key");
                return string.Join(" ", parts);
            }
        }
    }
}
```

**Step 3: Compile check. Step 4: Commit** — `feat: KeyInstance model with display names`

---

### Task 5: KeyPouch

**Files:**
- Create: `Assets/Scripts/Destinations/KeyPouch.cs`
- Test: `Assets/Tests/EditMode/Destinations/KeyPouchTests.cs`

**Step 1: Failing test**

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class KeyPouchTests
    {
        [Test]
        public void Add_Remove_Count()
        {
            var pouch = new KeyPouch();
            var key = new KeyInstance { ColorFamily = ColorFamily.Red };
            pouch.Add(key);
            Assert.AreEqual(1, pouch.Keys.Count);
            Assert.IsTrue(pouch.Remove(key));
            Assert.AreEqual(0, pouch.Keys.Count);
            Assert.IsFalse(pouch.Remove(key));
        }

        [Test]
        public void IsOverSoftCap()
        {
            var pouch = new KeyPouch();
            for (int i = 0; i < 30; i++) pouch.Add(new KeyInstance());
            Assert.IsTrue(pouch.IsOverSoftCap(30));
            Assert.IsFalse(pouch.IsOverSoftCap(31));
        }

        [Test]
        public void SellValue_ScalesWithDifficultyAndQuestLevel()
        {
            double d1 = KeyPouch.SellValue(new KeyInstance { Difficulty = 1 }, questLevel: 10, sellBase: 25f);
            double d3 = KeyPouch.SellValue(new KeyInstance { Difficulty = 3 }, questLevel: 10, sellBase: 25f);
            Assert.AreEqual(250, d1, 0.01);
            Assert.AreEqual(750, d3, 0.01);
        }
    }
}
```

**Step 2: Implement**

```csharp
using System;
using System.Collections.Generic;

namespace Starquill.Destinations
{
    /// The player's key inventory. Soft cap halves the drop rate above it
    /// (enforced by the drop caller) — never blocks (design §2).
    public class KeyPouch
    {
        private readonly List<KeyInstance> keys = new();
        public IReadOnlyList<KeyInstance> Keys => keys;

        public event Action OnChanged;

        public void Add(KeyInstance key)
        {
            if (key == null) return;
            keys.Add(key);
            OnChanged?.Invoke();
        }

        public bool Remove(KeyInstance key)
        {
            if (!keys.Remove(key)) return false;
            OnChanged?.Invoke();
            return true;
        }

        public bool IsOverSoftCap(int softCap) => keys.Count >= softCap;

        /// D6: difficulty-scaled sell pricing.
        public static double SellValue(KeyInstance key, int questLevel, float sellBase)
            => sellBase * Math.Max(1, questLevel) * Math.Max(1, key.Difficulty);
    }
}
```

**Step 3: Compile check. Step 4: Commit** — `feat: KeyPouch inventory with soft cap and sell pricing`

---

### Task 6: KeyFusion

**Files:**
- Create: `Assets/Scripts/Destinations/KeyFusion.cs`
- Test: `Assets/Tests/EditMode/Destinations/KeyFusionTests.cs`

**Step 1: Failing test** — cover every rule from design §2/D4:

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class KeyFusionTests
    {
        private static KeyInstance Color(ColorFamily f, int d = 1) => new() { ColorFamily = f, Difficulty = d };
        private static KeyInstance Slot(KeySlot s, int d = 1) => new() { Slot = s, Difficulty = d };
        private static KeyInstance Class(int id, int d = 1) => new() { ArchetypeId = id, Difficulty = d };

        [Test]
        public void DifferentKinds_CombineModifiers_SumDifficulty()
        {
            var result = KeyFusion.Fuse(Color(ColorFamily.Brown), Slot(KeySlot.Head));
            Assert.AreEqual(ColorFamily.Brown, result.ColorFamily);
            Assert.AreEqual(KeySlot.Head, result.Slot);
            Assert.AreEqual(2, result.Difficulty);
        }

        [Test]
        public void SameVariant_Merges_AsUpgrade()
        {
            var result = KeyFusion.Fuse(Color(ColorFamily.Brown), Color(ColorFamily.Brown));
            Assert.AreEqual(ColorFamily.Brown, result.ColorFamily);
            Assert.AreEqual(2, result.Difficulty);
            Assert.AreEqual(1, result.ModifierCount);
        }

        [Test]
        public void SameKind_DifferentVariant_IsInvalid()
        {
            Assert.IsFalse(KeyFusion.CanFuse(Color(ColorFamily.Brown), Color(ColorFamily.Red), maxDifficulty: 6));
            Assert.IsFalse(KeyFusion.CanFuse(Slot(KeySlot.Head), Slot(KeySlot.Feet), maxDifficulty: 6));
            Assert.IsFalse(KeyFusion.CanFuse(Class(0), Class(1), maxDifficulty: 6));
        }

        [Test]
        public void FusedKeys_WithDistinctKinds_FuseAgain()
        {
            var brownHelm = KeyFusion.Fuse(Color(ColorFamily.Brown), Slot(KeySlot.Head));
            Assert.IsTrue(KeyFusion.CanFuse(brownHelm, Class(0), maxDifficulty: 6));
            var triple = KeyFusion.Fuse(brownHelm, Class(0));
            Assert.AreEqual(3, triple.Difficulty);
            Assert.AreEqual(3, triple.ModifierCount);
        }

        [Test]
        public void DifficultyCap_BlocksFusion()
        {
            Assert.IsFalse(KeyFusion.CanFuse(Color(ColorFamily.Brown, 4), Slot(KeySlot.Head, 3), maxDifficulty: 6));
        }

        [Test]
        public void Cost_IsQuadraticInResultDifficulty()
        {
            // base 250, questLevel 40, D3 result -> 250*40*9 = 90,000 (design §2)
            double cost = KeyFusion.Cost(baseCost: 250f, questLevel: 40, resultDifficulty: 3);
            Assert.AreEqual(90000, cost, 0.01);
        }
    }
}
```

**Step 2: Implement**

```csharp
namespace Starquill.Destinations
{
    /// Fusion rules (design §2, D4): different kinds combine; same kind +
    /// same variant merges (the upgrade path); same kind + different
    /// variant is invalid. Difficulty always sums, capped.
    public static class KeyFusion
    {
        public static bool CanFuse(KeyInstance a, KeyInstance b, int maxDifficulty)
        {
            if (a == null || b == null || a == b) return false;
            if (a.Difficulty + b.Difficulty > maxDifficulty) return false;

            if (a.ColorFamily.HasValue && b.ColorFamily.HasValue && a.ColorFamily != b.ColorFamily)
                return false;
            if (a.Slot.HasValue && b.Slot.HasValue && a.Slot != b.Slot)
                return false;
            if (a.ArchetypeId >= 0 && b.ArchetypeId >= 0 && a.ArchetypeId != b.ArchetypeId)
                return false;
            return true;
        }

        public static KeyInstance Fuse(KeyInstance a, KeyInstance b)
        {
            return new KeyInstance
            {
                ColorFamily = a.ColorFamily ?? b.ColorFamily,
                Slot = a.Slot ?? b.Slot,
                ArchetypeId = a.ArchetypeId >= 0 ? a.ArchetypeId : b.ArchetypeId,
                Difficulty = a.Difficulty + b.Difficulty
            };
        }

        /// D5: quadratic gold cost.
        public static double Cost(float baseCost, int questLevel, int resultDifficulty)
            => baseCost * System.Math.Max(1, questLevel) * resultDifficulty * (double)resultDifficulty;
    }
}
```

**Step 3: Compile check. Step 4: Commit** — `feat: key fusion rules and quadratic cost`

---

### Task 7: KeyRoller (base key drop generation)

**Files:**
- Create: `Assets/Scripts/Destinations/KeyRoller.cs`
- Test: `Assets/Tests/EditMode/Destinations/KeyRollerTests.cs`

**Step 1: Failing test**

```csharp
using NUnit.Framework;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class KeyRollerTests
    {
        [Test]
        public void Roll_AlwaysProducesExactlyOneModifier()
        {
            var rng = new System.Random(11);
            for (int i = 0; i < 200; i++)
                Assert.AreEqual(1, KeyRoller.Roll(rng).ModifierCount);
        }

        [Test]
        public void Roll_DifficultyDistribution_MatchesWeights()
        {
            var rng = new System.Random(11);
            int d1 = 0;
            for (int i = 0; i < 2000; i++)
                if (KeyRoller.Roll(rng).Difficulty == 1) d1++;
            Assert.Greater(d1, 1500); // 85% expected, generous tolerance
        }

        [Test]
        public void Roll_ProducesAllThreeKinds()
        {
            var rng = new System.Random(11);
            bool color = false, slot = false, cls = false;
            for (int i = 0; i < 500; i++)
            {
                var key = KeyRoller.Roll(rng);
                color |= key.ColorFamily.HasValue;
                slot |= key.Slot.HasValue;
                cls |= key.ArchetypeId >= 0;
            }
            Assert.IsTrue(color && slot && cls);
        }
    }
}
```

**Step 2: Implement**

```csharp
using System;
using Starquill.Core;

namespace Starquill.Destinations
{
    /// Rolls base key drops: kind 40/30/30 color/type/class, variant uniform,
    /// difficulty 85/13/2 (design §2, D2). Weights are intentionally compiled
    /// constants until the balance pass proves they need to be knobs.
    public static class KeyRoller
    {
        public static KeyInstance Roll(Random rng)
        {
            var key = new KeyInstance();

            double kind = rng.NextDouble();
            if (kind < 0.40)
                key.ColorFamily = (ColorFamily)rng.Next(Enum.GetValues(typeof(ColorFamily)).Length);
            else if (kind < 0.70)
                key.Slot = (KeySlot)rng.Next(Enum.GetValues(typeof(KeySlot)).Length);
            else
                key.ArchetypeId = rng.Next(ClassArchetype.All.Count);

            double d = rng.NextDouble();
            key.Difficulty = d < 0.85 ? 1 : d < 0.98 ? 2 : 3;
            return key;
        }
    }
}
```

**Step 3: Compile check. Step 4: Commit** — `feat: KeyRoller base key drop generation`

---

### Task 8: EconomyConfig knobs

**Files:**
- Modify: `Assets/Scripts/Data/EconomyConfig.cs` (append after the Training header block)

**Step 1: Add fields** (no test — plain serialized fields):

```csharp
[Header("Destinations: Keys")]
public float keyDropRate = 0.004f;      // per active kill
public int keySoftCap = 30;             // over: drop rate halves
public int keyMaxDifficulty = 6;
public float fusionBaseCost = 250f;     // x questLevel x D^2
public float keySellBase = 25f;         // x questLevel x D

[Header("Destinations: Dungeons")]
public float dungeonDurationBase = 150f;    // seconds, +dungeonDurationPerD per D above 1
public float dungeonDurationPerD = 20f;
public float dungeonEnemyMultPerD = 0.6f;   // D3 steep curve
public float dungeonWaveHpRamp = 0.02f;     // +2% enemy HP per wave cleared
public int dungeonBonusWavesPerRoll = 3;    // waves over par per bonus roll
public int dungeonBonusRollCap = 3;
public float dungeonParSecondsPerWave = 15f;
```

**GOTCHA (from memory):** the `DefaultEconomyConfig.asset` ScriptableObject holds serialized values — new fields pick up script defaults automatically (they're additions, not changes), so no asset surgery is needed here. Verify in the Inspector after the editor recompiles.

**Step 2: Compile check. Step 3: Commit** — `feat: Destinations economy knobs`

---

### Task 9: DropModifiers + EquipmentFactory targeting

**Files:**
- Create: `Assets/Scripts/Equipment/DropModifiers.cs`
- Modify: `Assets/Scripts/Equipment/EquipmentFactory.cs`
- Test: `Assets/Tests/EditMode/Destinations/TargetedRollTests.cs`

**Step 1: DropModifiers** (Equipment namespace — below Managers, above Core, per the circular-dependency rule):

```csharp
using Starquill.Core;

namespace Starquill.Equipment
{
    /// Loot targeting applied by dungeon keys (design §3). GameManager
    /// translates a KeyInstance into this; the factory and dropper stay
    /// ignorant of the Destinations assembly.
    public class DropModifiers
    {
        public ColorFamily? ColorFamily;
        public EquipmentSlot? PreferredSlot;   // Head/Torso/Arms/Legs/Feet/MainHand/Misc1
        public bool PreferWeapon;              // KeySlot.Weapon maps here
        public float SlotWeight = 0.8f;        // chance the preferred slot is used
        public StatType? ForcedStatA;          // class archetype pair; primary rolls
        public StatType? ForcedStatB;          //   50/50 between A and B
    }
}
```

**Step 2: Failing test**

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.Tests.Destinations
{
    public class TargetedRollTests
    {
        [Test]
        public void GenerateStatPair_ForcedPair_UsesBothStats()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 50; i++)
            {
                var (p, pv, s, sv) = EquipmentFactory.GenerateStatPair(
                    "tr", Rarity.Rare, rng, questLevel: 10,
                    forcedStatA: StatType.INT, forcedStatB: StatType.CHA);
                Assert.IsTrue((p == StatType.INT && s == StatType.CHA)
                           || (p == StatType.CHA && s == StatType.INT));
                Assert.Greater(pv, sv);
            }
        }

        [Test]
        public void GenerateStatPair_NoForcing_UnchangedBehavior()
        {
            var rng = new System.Random(3);
            var (p, pv, s, sv) = EquipmentFactory.GenerateStatPair("tr", Rarity.Common, rng);
            Assert.AreNotEqual(p, s);
            Assert.Greater(pv, sv);
        }

        [Test]
        public void RollRarity_LegendaryWeightMultiplier_RaisesLegendaryRate()
        {
            int baseline = CountLegendaries(mult: 1f), boosted = CountLegendaries(mult: 10f);
            Assert.Greater(boosted, baseline);
        }

        private static int CountLegendaries(float mult)
        {
            var rng = new System.Random(9);
            int count = 0;
            for (int i = 0; i < 20000; i++)
                if (EquipmentFactory.RollRarity(50, rng, mult) == Rarity.Legendary) count++;
            return count;
        }
    }
}
```

**Step 3: Implement in `EquipmentFactory`:**

a. `GenerateStatPair` — add optional `StatType? forcedStatA = null, StatType? forcedStatB = null` parameters. At the top of the method, before pool selection:

```csharp
if (forcedStatA.HasValue && forcedStatB.HasValue && forcedStatA != forcedStatB)
{
    bool aFirst = rng.NextDouble() < 0.5;
    var fp = aFirst ? forcedStatA.Value : forcedStatB.Value;
    var fs = aFirst ? forcedStatB.Value : forcedStatA.Value;
    return BuildBudgetedPair(fp, fs, rarity, rng, questLevel, budgetPerLevel);
}
```

Extract the existing budget block (lines 267-288, `(minBudget, maxBudget)` switch through the return) into `private static (...) BuildBudgetedPair(StatType primary, StatType secondary, Rarity rarity, Random rng, int questLevel, float budgetPerLevel)` and call it from both paths. **DRY: do not duplicate the budget math.**

b. `RollRarity(int questLevel, System.Random rng)` — add optional `float legendaryWeightMult = 1f`; multiply `legendaryWeight` by it before totals. `RollRarityWithFloor` gains and forwards the same optional param.

c. `CreateRandom` and `CreateRandomWeapon` — add optional `DropModifiers modifiers = null`. Where colors are rolled (`baseColor` and each variance color), replace `colors.GetRandomColor("main", rng)` with `colors.GetRandomColor("main", rng, modifiers?.ColorFamily)`. Where `GenerateStatPair` is called, pass `modifiers?.ForcedStatA, modifiers?.ForcedStatB`.

**Step 4: Compile check. Step 5: Commit** — `feat: EquipmentFactory targeted rolls (forced stats, color family, legendary weight)`

---

### Task 10: LootDropper modifier passthrough

**Files:**
- Modify: `Assets/Scripts/Equipment/LootDropper.cs`
- Test: append to `Assets/Tests/EditMode/Destinations/TargetedRollTests.cs`

**Step 1: Failing test** — construct `LootDropper` the way existing tests in `Assets/Tests/EditMode/Equipment/` do (copy their catalog/table setup helper), then:

```csharp
[Test]
public void TryDrop_WithPreferredSlot_BiasesSlot()
{
    var dropper = MakeDropper(dropRate: 1f); // helper: config with baseDropRate = 1
    var mods = new DropModifiers { PreferredSlot = EquipmentSlot.Head, SlotWeight = 1f };
    var rng = new System.Random(5);
    for (int i = 0; i < 30; i++)
    {
        var item = dropper.TryDrop(10, rng, mods);
        Assert.IsNotNull(item);
        Assert.AreEqual(EquipmentSlot.Head, item.Slot);
    }
}
```

**Step 2: Implement** — `TryDrop(int questLevel, System.Random rng, DropModifiers modifiers = null)`:

- Replace the 70/30 armor/weapon split when a slot preference exists:

```csharp
EquipmentInstance item;
bool wantWeapon = modifiers?.PreferWeapon == true && rng.NextDouble() < modifiers.SlotWeight;
string forcedPrefix = null;
if (!wantWeapon && modifiers?.PreferredSlot != null && rng.NextDouble() < modifiers.SlotWeight)
    forcedPrefix = PrefixForSlot(modifiers.PreferredSlot.Value);

if (wantWeapon || (forcedPrefix == null && rng.NextDouble() < 0.30))
    item = factory.CreateRandomWeapon(rarity, rng, questLevel: questLevel, modifiers: modifiers);
else
{
    string prefix = forcedPrefix ?? ArmorPrefixes[rng.Next(ArmorPrefixes.Length)];
    item = factory.CreateRandom(prefix, rarity, rng, questLevel, modifiers);
}
```

- Add `private static string PrefixForSlot(EquipmentSlot slot)` mapping Head→"hd", Torso→"tr", Arms→"ar", Legs→"lg", Feet→"fe", anything else→"mc".
- Note: `CreateRandom`/`CreateRandomWeapon` parameter order — match the signatures added in Task 9 (named arguments avoid mistakes).

**Step 3: Compile check. Step 4: Commit** — `feat: LootDropper slot-biased modifier passthrough`

**CHECKPOINT 1: user runs Test Runner (EditMode, Run All). All new Destinations tests + full existing suite green before Task 11.**

---

### Task 11: DungeonGenerator + DungeonRun

**Files:**
- Create: `Assets/Scripts/Destinations/DungeonSpec.cs`
- Create: `Assets/Scripts/Destinations/DungeonGenerator.cs`
- Create: `Assets/Scripts/Destinations/DungeonRun.cs`
- Test: `Assets/Tests/EditMode/Destinations/DungeonTests.cs`

**Step 1: Failing test**

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;

namespace Starquill.Tests.Destinations
{
    public class DungeonTests
    {
        private static KeyInstance BrownWarriorHelm(int d) => new()
        { ColorFamily = ColorFamily.Brown, Slot = KeySlot.Head, ArchetypeId = 0, Difficulty = d };

        [Test]
        public void Generate_IsDeterministic()
        {
            var a = DungeonGenerator.Generate(BrownWarriorHelm(3), runCounter: 5, questLevel: 20,
                durationBase: 150f, durationPerD: 20f, enemyMultPerD: 0.6f, parSecondsPerWave: 15f);
            var b = DungeonGenerator.Generate(BrownWarriorHelm(3), runCounter: 5, questLevel: 20,
                durationBase: 150f, durationPerD: 20f, enemyMultPerD: 0.6f, parSecondsPerWave: 15f);
            Assert.AreEqual(a.Seed, b.Seed);
            Assert.AreEqual(a.DurationSeconds, b.DurationSeconds);
        }

        [Test]
        public void Generate_DifficultyMapping()
        {
            var spec = DungeonGenerator.Generate(BrownWarriorHelm(3), 0, 20, 150f, 20f, 0.6f, 15f);
            Assert.AreEqual(190f, spec.DurationSeconds, 0.01f);       // 150 + 20*2
            Assert.AreEqual(2.2f, spec.EnemyHpMultiplier, 0.001f);    // 1 + 0.6*2
            Assert.AreEqual(Rarity.Epic, spec.RarityFloor);
            Assert.AreEqual(5, spec.BaseRolls);                        // 2 + D
            Assert.AreEqual(12, spec.ParWaves);                        // 190/15 rounded
        }

        [TestCase(1, Rarity.Uncommon, 1f)]
        [TestCase(2, Rarity.Rare, 1f)]
        [TestCase(4, Rarity.Epic, 2f)]     // D4: Epic floor + doubled legendary weight
        [TestCase(5, Rarity.Legendary, 1f)]
        [TestCase(6, Rarity.Legendary, 1f)]
        public void FloorForDifficulty(int d, Rarity floor, float legendaryMult)
        {
            Assert.AreEqual(floor, DungeonGenerator.FloorForDifficulty(d));
            Assert.AreEqual(legendaryMult, DungeonGenerator.LegendaryMultForDifficulty(d));
        }

        [Test]
        public void MakeWave_MiniBossEveryFifth_AndRampsHp()
        {
            var spec = DungeonGenerator.Generate(BrownWarriorHelm(1), 0, 10, 150f, 20f, 0.6f, 15f);
            Assert.IsFalse(DungeonGenerator.MakeWave(spec, 0).IsBossWave);
            Assert.IsTrue(DungeonGenerator.MakeWave(spec, 4).IsBossWave);  // waves 5,10,... (index 4,9,...)
            Assert.IsTrue(DungeonGenerator.MakeWave(spec, 9).IsBossWave);
            // Determinism: same index -> same wave
            var w1 = DungeonGenerator.MakeWave(spec, 3);
            var w2 = DungeonGenerator.MakeWave(spec, 3);
            Assert.AreEqual(w1.EnemyCount, w2.EnemyCount);
            Assert.AreEqual(w1.EnemyTypes[0], w2.EnemyTypes[0]);
        }

        [Test]
        public void MakeWave_ClassKey_ThemesEnemiesToArchetype()
        {
            var key = new KeyInstance { ArchetypeId = 0, Difficulty = 1 }; // Warrior STR/DEX
            var spec = DungeonGenerator.Generate(key, 0, 10, 150f, 20f, 0.6f, 15f);
            int themed = 0, total = 0;
            for (int w = 0; w < 40; w++)
            {
                var wave = DungeonGenerator.MakeWave(spec, w);
                foreach (var t in wave.EnemyTypes)
                { total++; if (t == StatType.STR || t == StatType.DEX) themed++; }
            }
            Assert.Greater(themed / (float)total, 0.45f); // 60% target, loose bound
        }

        [Test]
        public void DungeonRun_TicksDown_And_CountsBonusRolls()
        {
            var spec = DungeonGenerator.Generate(BrownWarriorHelm(1), 0, 10, 150f, 20f, 0.6f, 15f);
            var run = new DungeonRun(spec);
            Assert.IsFalse(run.IsOver);
            run.Tick(200f);
            Assert.IsTrue(run.IsOver);

            for (int i = 0; i < spec.ParWaves + 7; i++) run.WaveCleared();
            // 7 over par, 3 waves per roll -> 2, under cap 3
            Assert.AreEqual(2, run.BonusRolls(wavesPerRoll: 3, cap: 3));
        }
    }
}
```

**Step 2: Implement**

`DungeonSpec.cs`:

```csharp
using Starquill.Core;

namespace Starquill.Destinations
{
    /// A generated dungeon run: deterministic from (key, runCounter,
    /// questLevel) — regenerate on load, never serialize (quest pattern).
    public class DungeonSpec
    {
        public KeyInstance Key;
        public int Seed;
        public int QuestLevel;
        public float DurationSeconds;
        public float EnemyHpMultiplier;
        public Rarity RarityFloor;
        public float LegendaryWeightMult;
        public int BaseRolls;
        public int ParWaves;
    }
}
```

`DungeonGenerator.cs`:

```csharp
using System;
using Starquill.Core;
using Starquill.Quests;

namespace Starquill.Destinations
{
    /// Timed wave rush generation (design §3). Waves are deterministic per
    /// (spec.Seed, waveIndex) so an unbounded rush needs no wave list.
    public static class DungeonGenerator
    {
        private const float MiniBossHpMult = 4f;
        private const int MiniBossEvery = 5;               // every 5th wave
        private const double ClassThemedChance = 0.6;

        public static Rarity FloorForDifficulty(int d) => d switch
        {
            1 => Rarity.Uncommon,
            2 => Rarity.Rare,
            3 => Rarity.Epic,
            4 => Rarity.Epic,
            _ => Rarity.Legendary   // D5+ (design D3, steep curve)
        };

        public static float LegendaryMultForDifficulty(int d) => d == 4 ? 2f : 1f;

        public static DungeonSpec Generate(KeyInstance key, int runCounter, int questLevel,
            float durationBase, float durationPerD, float enemyMultPerD, float parSecondsPerWave)
        {
            int d = Math.Max(1, key.Difficulty);
            float duration = durationBase + durationPerD * (d - 1);
            return new DungeonSpec
            {
                Key = key,
                Seed = (runCounter * 8887) ^ (questLevel * 31) ^ (d * 397),
                QuestLevel = questLevel,
                DurationSeconds = duration,
                EnemyHpMultiplier = 1f + enemyMultPerD * (d - 1),
                RarityFloor = FloorForDifficulty(d),
                LegendaryWeightMult = LegendaryMultForDifficulty(d),
                BaseRolls = Math.Min(6, 2 + d),
                ParWaves = (int)Math.Round(duration / Math.Max(1f, parSecondsPerWave))
            };
        }

        public static WaveSpec MakeWave(DungeonSpec spec, int waveIndex)
        {
            var rng = new Random(spec.Seed ^ (waveIndex * 73856093));
            bool miniBoss = (waveIndex + 1) % MiniBossEvery == 0;
            int count = miniBoss ? 1 : rng.Next(2, 5);

            var types = new StatType[count];
            var arch = spec.Key.Archetype;
            var all = (StatType[])Enum.GetValues(typeof(StatType));
            for (int i = 0; i < count; i++)
            {
                if (arch != null && rng.NextDouble() < ClassThemedChance)
                    types[i] = rng.NextDouble() < 0.5 ? arch.StatA : arch.StatB;
                else
                    types[i] = all[rng.Next(all.Length)];
            }

            return new WaveSpec
            {
                EnemyCount = count,
                EnemyTypes = types,
                HpMultiplier = miniBoss ? MiniBossHpMult : 1f,
                IsBossWave = miniBoss
            };
        }
    }
}
```

`DungeonRun.cs`:

```csharp
using System;

namespace Starquill.Destinations
{
    /// Live state of a timed rush. Pure (no UnityEngine time) for tests;
    /// GameManager feeds Tick(deltaTime).
    public class DungeonRun
    {
        public DungeonSpec Spec { get; }
        public float TimeLeft { get; private set; }
        public int WavesCleared { get; private set; }
        public bool IsOver => TimeLeft <= 0f;

        public DungeonRun(DungeonSpec spec)
        {
            Spec = spec;
            TimeLeft = spec.DurationSeconds;
        }

        public void Tick(float deltaSeconds) => TimeLeft = Math.Max(0f, TimeLeft - deltaSeconds);
        public void WaveCleared() => WavesCleared++;

        public int BonusRolls(int wavesPerRoll, int cap)
        {
            int over = WavesCleared - Spec.ParWaves;
            if (over <= 0) return 0;
            return Math.Min(cap, over / Math.Max(1, wavesPerRoll));
        }
    }
}
```

**Step 3: Compile check. Step 4: Commit** — `feat: dungeon generation and timed run state`

---

### Task 12: Quest key rewards (QuestRewardSpec)

**Files:**
- Modify: `Assets/Scripts/Quests/QuestRewardSpec.cs`
- Test: `Assets/Tests/EditMode/Destinations/QuestKeyRewardTests.cs`

**Step 1: Failing test**

```csharp
using NUnit.Framework;
using Starquill.Quests;

namespace Starquill.Tests.Destinations
{
    public class QuestKeyRewardTests
    {
        [Test]
        public void ForTier_KeyPayouts_MatchDesign()
        {
            Assert.AreEqual(0, QuestRewardSpec.ForTier(QuestTier.Normal).KeyDrops);
            var elite = QuestRewardSpec.ForTier(QuestTier.Elite);
            Assert.AreEqual(1, elite.KeyDrops);
            Assert.AreEqual(1, elite.KeyDifficulty);
            var hard = QuestRewardSpec.ForTier(QuestTier.Hard);
            Assert.AreEqual(1, hard.KeyDrops);
            Assert.AreEqual(0.25f, hard.ExtraKeyChance, 0.001f);
            var boss = QuestRewardSpec.ForTier(QuestTier.Boss);
            Assert.AreEqual(1, boss.KeyDrops);
            Assert.AreEqual(2, boss.KeyDifficulty);
        }
    }
}
```

**Step 2: Implement** — add fields `public int KeyDrops; public float ExtraKeyChance; public int KeyDifficulty = 1;` and extend `ForTier` (Elite: `KeyDrops = 1`; Hard: `KeyDrops = 1, ExtraKeyChance = 0.25f`; Boss: `KeyDrops = 1, KeyDifficulty = 2`). Plain ints/floats only — the Quests assembly must not reference Destinations.

**Step 3: Compile check. Step 4: Commit** — `feat: quest tiers pay key rewards`

---

### Task 13: Save format (SerializedKey + counters)

**Files:**
- Modify: `Assets/Scripts/Managers/SaveData.cs`
- Test: `Assets/Tests/EditMode/Destinations/KeySaveTests.cs`

**Step 1: Failing test**

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Destinations;
using Starquill.Managers;

namespace Starquill.Tests.Destinations
{
    public class KeySaveTests
    {
        [Test]
        public void SerializedKey_RoundTrips()
        {
            var key = new KeyInstance
            { ColorFamily = ColorFamily.Brown, Slot = KeySlot.Head, ArchetypeId = 3, Difficulty = 4 };
            var restored = SerializedKey.FromInstance(key).ToInstance();
            Assert.AreEqual(key.ColorFamily, restored.ColorFamily);
            Assert.AreEqual(key.Slot, restored.Slot);
            Assert.AreEqual(key.ArchetypeId, restored.ArchetypeId);
            Assert.AreEqual(key.Difficulty, restored.Difficulty);
        }

        [Test]
        public void SerializedKey_NullModifiers_RoundTripAsNull()
        {
            var key = new KeyInstance { ArchetypeId = 7 };
            var restored = SerializedKey.FromInstance(key).ToInstance();
            Assert.IsFalse(restored.ColorFamily.HasValue);
            Assert.IsFalse(restored.Slot.HasValue);
        }
    }
}
```

**Step 2: Implement** — in `SaveData.cs` (Managers references Destinations after Task 14's asmdef edit — do that asmdef edit now: add `"Starquill.Destinations"` to `Assets/Scripts/Managers/Starquill.Managers.asmdef` references):

```csharp
// on SaveData:
public List<SerializedKey> keys = new();
public int dungeonRunCounter;

// new class in SaveData.cs:
[Serializable]
public class SerializedKey
{
    public int colorFamily = -1;  // -1 = none, else (int)ColorFamily
    public int slot = -1;         // -1 = none, else (int)KeySlot
    public int archetypeId = -1;
    public int difficulty = 1;

    public static SerializedKey FromInstance(KeyInstance k) => new()
    {
        colorFamily = k.ColorFamily.HasValue ? (int)k.ColorFamily.Value : -1,
        slot = k.Slot.HasValue ? (int)k.Slot.Value : -1,
        archetypeId = k.ArchetypeId,
        difficulty = k.Difficulty
    };

    public KeyInstance ToInstance() => new()
    {
        ColorFamily = colorFamily >= 0 ? (ColorFamily?)colorFamily : null,
        Slot = slot >= 0 ? (KeySlot?)slot : null,
        ArchetypeId = archetypeId,
        Difficulty = difficulty
    };
}
```

Add `using Starquill.Destinations;`. JsonUtility handles `List<SerializedKey>` fine (flat ints, matching the variance-color convention).

**Step 3: Compile check. Step 4: Commit** — `feat: key save serialization + dungeon run counter`

---

### Task 14: GameManager integration

**Files:**
- Modify: `Assets/Scripts/Managers/GameManager.cs`
- Modify: `Assets/Scripts/Exploration/ExplorationState.cs` + `ExplorationManager.cs`
- Test: `Assets/Tests/EditMode/Destinations/DungeonRewardMathTests.cs` (pure parts only)

GameManager is a MonoBehaviour — integration is verified by play-mode smoke (Task 17), but keep reward math testable by extracting it.

**Step 1: ExplorationState** — add `InDungeon` to the enum. In `ExplorationManager`, add `public void EnterDungeon() { State = ExplorationState.InDungeon; }` and `public void DungeonEnded() { State = ExplorationState.Exploring; }`. Note `ProcessWaveCleared` is only called from the Exploring branch of `HandleWaveCleared`, so travel/fragment/discovery stay frozen during dungeons automatically.

**Step 2: GameManager members**

```csharp
private readonly KeyPouch keyPouch = new();
public KeyPouch KeyPouch => keyPouch;
private DungeonRun activeDungeon;
public DungeonRun ActiveDungeon => activeDungeon;
private int dungeonRunCounter;

public event Action OnKeysChanged;               // fired via keyPouch.OnChanged relay in Start()
public event Action<DungeonSpec> OnDungeonStarted;
public event Action<DungeonRun, List<EquipmentInstance>, double> OnDungeonEnded;
```

Add `using Starquill.Destinations;`.

**Step 3: Key drops on kills** — in `ProcessLootDrops(int killCount)`, after the equipment drop roll, add:

```csharp
// Keys drop from active kills only, never inside dungeons (design §2).
if (exploration.State != ExplorationState.InDungeon)
{
    float rate = economyConfig.keyDropRate
        * (keyPouch.IsOverSoftCap(economyConfig.keySoftCap) ? 0.5f : 1f);
    if (rng.NextDouble() < rate)
        keyPouch.Add(KeyRoller.Roll(rng));
}
```

**Step 4: Quest key rewards** — in `CompleteQuest()`, after the loot-roll loop:

```csharp
for (int i = 0; i < reward.KeyDrops; i++)
{
    var key = KeyRoller.Roll(rewardRng);
    key.Difficulty = Math.Max(key.Difficulty, reward.KeyDifficulty);
    keyPouch.Add(key);
}
if (reward.ExtraKeyChance > 0 && rewardRng.NextDouble() < reward.ExtraKeyChance)
    keyPouch.Add(KeyRoller.Roll(rewardRng));
```

**Step 5: Dungeon lifecycle**

```csharp
public bool StartDungeon(KeyInstance key)
{
    if (activeDungeon != null) return false;
    if (exploration.State != ExplorationState.Exploring) return false;
    if (questLog.Phase == QuestPhase.Offered) return false;
    if (!keyPouch.Remove(key)) return false;

    var spec = DungeonGenerator.Generate(key, dungeonRunCounter++, questLevel,
        economyConfig.dungeonDurationBase, economyConfig.dungeonDurationPerD,
        economyConfig.dungeonEnemyMultPerD, economyConfig.dungeonParSecondsPerWave);
    activeDungeon = new DungeonRun(spec);
    exploration.EnterDungeon();
    SpawnWave();
    OnDungeonStarted?.Invoke(spec);
    SaveState();
    return true;
}

private void EndDungeon()
{
    var run = activeDungeon;
    activeDungeon = null;

    var rewards = RollDungeonRewards(run, out double goldBonus);
    gold += goldBonus;
    exploration.DungeonEnded();
    OnGoldChanged?.Invoke(gold);
    OnDungeonEnded?.Invoke(run, rewards, goldBonus);
    SaveState();
    SpawnWave();
}
```

- Tick: in `Update()` (or `ProcessTick`), while `activeDungeon != null`: `activeDungeon.Tick(Time.deltaTime); if (activeDungeon.IsOver) EndDungeon();`
- `HandleWaveCleared()`: add a branch **before** the quest branch:

```csharp
if (exploration.State == ExplorationState.InDungeon && activeDungeon != null)
{
    activeDungeon.WaveCleared();
    if (!activeDungeon.IsOver) SpawnWave();
    return;
}
```

- `SpawnWave()`: add a dungeon branch before the quest branch, mirroring the quest wave loop but sourcing `DungeonGenerator.MakeWave(activeDungeon.Spec, activeDungeon.WavesCleared)` and scaling HP: `economyConfig.EnemyHP(questLevel) * wave.HpMultiplier * spec.EnemyHpMultiplier * (1f + economyConfig.dungeonWaveHpRamp * activeDungeon.WavesCleared)`. Enemy id prefix `"dungeon_enemy"` / `"dungeon_boss"`.
- **Modified drops inside dungeons:** `ProcessLootDrops` builds a `DropModifiers` from `activeDungeon?.Spec.Key` (extract `private static DropModifiers ModifiersFor(KeyInstance key)` mapping: ColorFamily passthrough; `KeySlot.Weapon` → `PreferWeapon = true`; other slots → `PreferredSlot` (Head→Head, …, Misc→Misc1); archetype → ForcedStatA/B) and passes it to `lootDropper.TryDrop(questLevel, rng, mods)`.
- **Reward math** — extract as a static, testable method in a new small class `Assets/Scripts/Managers/DungeonRewardRoller.cs` (Managers assembly) or private static in GameManager; plan choice: **static class `DungeonRewardRoller`** with signature `public static List<EquipmentInstance> Roll(DungeonRun run, EquipmentFactory factory, EconomyConfig config, int questLevel, System.Random rng, out double goldBonus)`. Logic: `int rolls = spec.BaseRolls + run.BonusRolls(config.dungeonBonusWavesPerRoll, config.dungeonBonusRollCap)`; each roll: `RollRarityWithFloor(questLevel, spec.RarityFloor, rng, spec.LegendaryWeightMult)` then targeted create via `ModifiersFor(spec.Key)` (70/30 armor/weapon unless slot-modified — reuse LootDropper's biased pick by calling `factory` with the same mapping; keep the slot-bias helper `public` on LootDropper or duplicate the tiny mapping here, DRY prefers exposing `LootDropper.PrefixForSlot` as `internal`→`public static`). Gold: `goldBonus = config.GoldPerKill(questLevel, 0f, config.prestigeMultiplierBase) * run.WavesCleared * 3` (3 ≈ avg enemies/wave; acceptable v1 estimate, note for balance pass). Overflow → `rewardMailbox` exactly like `CompleteQuest` (GameManager applies inventory/mailbox; the roller only creates items).
- **Mini-boss immediate roll:** when a cleared wave `IsBossWave`, roll 1 floored item immediately through the same targeted path (inventory-or-mailbox).
- **Pause/quit mid-run (design note):** in `OnApplicationPause(true)` and `OnApplicationQuit`, if `activeDungeon != null`, call `EndDungeon()` first — the run banks what it cleared; never lost, never resumed with a stale clock.
- **Save/load:** `SaveState()` writes `keys` (from pouch) + `dungeonRunCounter`; the load path (`Start`, near the mailbox restore at line ~186) restores them. Active runs are deliberately NOT persisted.
- **Fusion + sell entry points:**

```csharp
public bool FuseKeys(KeyInstance a, KeyInstance b) // validates cost + rules, spends gold
public double KeyFusionCost(KeyInstance a, KeyInstance b)
public void SellKey(KeyInstance key)               // KeyPouch.SellValue -> gold
```

**Step 6: Test the extracted math** — `DungeonRewardMathTests.cs`: roll-count math (base + bonus, cap), floor application, mailbox overflow behavior via a full `LootInventory`. Reuse existing test factory helpers.

**Step 7: Compile check. Step 8: Commit** — `feat: GameManager key drops, fusion, dungeon lifecycle`

**CHECKPOINT 2: user runs Test Runner. Full suite green before UI work.**

---

### Task 15: UI — key pouch on the Destinations section

Read these before writing any UI code: `Assets/Scripts/UI/QuestsScreenController.cs` (section building, LockedCard), `Assets/Scripts/UI/UiFactory.cs` + `UiTheme.cs` (primitives, tokens — type floor 28px, touch ≥120px, **no Unicode glyphs in TMP text**), `Assets/Scripts/UI/BottomSheet.cs` and `ItemDetailSheet.cs` (sheet pattern), `ItemCardBuilder.cs` (card pattern).

**Files:**
- Create: `Assets/Scripts/UI/KeyPresenter.cs` (pure: key → display strings/colors; unit-testable)
- Create: `Assets/Scripts/UI/KeyDetailSheet.cs`
- Modify: `Assets/Scripts/UI/QuestsScreenController.cs`
- Test: `Assets/Tests/EditMode/UI/KeyPresenterTests.cs`

**Steps:**
1. `KeyPresenter` (pure static): `Title(KeyInstance)` → DisplayName; `SubLine(KeyInstance)` → "Difficulty 3 · Epic floor · 3:10 rush" (duration/floor via DungeonGenerator statics + config); `AccentColor(KeyInstance)` → representative Color per family (switch, hex constants), neutral gray when no color modifier. Write 3-4 presenter tests first (title, subline floor text per D, accent fallback).
2. In `QuestsScreenController.BuildContent` DESTINATIONS section: replace the three LockedCards with — key pouch header row ("KEYS  n/30"), one tappable card per key (accent color chip + title + subline; `UiFactory` buttons, LayoutElement heights ≥120px), tap → `KeyDetailSheet.Show(key)`. Below the pouch: keep `LockedCard("Locations", ...)`; **delete the Encounters locked card** (mode removed); Dungeons no longer needs a locked card (keys ARE the dungeon entry).
3. `KeyDetailSheet` (BottomSheet subclass, mirror `ItemDetailSheet` structure): key title, modifier chips, difficulty pips (Image-based), dungeon preview lines (duration, floor, enemy mult), buttons: **OPEN DUNGEON** (`GameManager.Instance.StartDungeon(key)` → close sheet), **FUSE** (Task 16), **SELL +Xg** (`SellKey`, close). Subscribe the Quests screen to `OnKeysChanged` for rebuild (follow existing subscription/unsubscribe pattern in the controller).
4. Compile check; commit — `feat: key pouch UI + key detail sheet`.

---

### Task 16: UI — fusion flow

**Files:**
- Create: `Assets/Scripts/UI/KeyFusionSheet.cs`
- Modify: `Assets/Scripts/UI/KeyDetailSheet.cs` (FUSE button opens it)
- Test: extend `KeyPresenterTests.cs` with fusion-preview presenter cases

**Steps:**
1. Presenter first: `KeyPresenter.FusionPreview(a, b, questLevel, config)` → `(bool valid, string resultTitle, string costText, string invalidReason)` wrapping `KeyFusion.CanFuse/Fuse/Cost`. Tests: valid pair preview, same-kind-different-variant invalid reason, cap-exceeded reason, unaffordable is still previewable (button disabled at the view layer).
2. `KeyFusionSheet.Show(baseKey)`: list of fusable partners (`KeyFusion.CanFuse` filter; show invalid ones dimmed with reason line — teaches the rules), tap partner → preview block (result name, D pips, cost, CONFIRM button gated on `gold >= cost`). Confirm → `GameManager.FuseKeys` → refresh sheet or close to detail of the new key.
3. Compile check; commit — `feat: key fusion UI flow`.

---

### Task 17: UI — dungeon HUD + completion sheet, play-mode verification

**Files:**
- Create: `Assets/Scripts/UI/DungeonHudDisplay.cs`
- Create: `Assets/Scripts/UI/DungeonCompletionSheet.cs`
- Modify: `Assets/Scripts/UI/ExploreSceneController.cs` (wire events)

**Steps:**
1. Read `QuestBannerDisplay.cs` first — the dungeon HUD follows its pattern (how it's created/parented, how the wave HUD swaps in). HUD content: dungeon name (key DisplayName), countdown `m:ss`, "Waves n (par p)" line, par-exceeded state tint. Driven by `OnDungeonStarted` + per-frame refresh while `ActiveDungeon != null` (the quest banner shows the pattern for periodic refresh) + `OnDungeonEnded` teardown.
2. `DungeonCompletionSheet` mirrors `QuestCompletionSheet`: waves cleared vs par, bonus-roll callout, gold, reward ItemCards (tap-through to detail), mailbox overflow notice (reuse the existing notice pattern). Subscribe in `ExploreSceneController` to `OnDungeonEnded`.
3. Compile check; commit — `feat: dungeon HUD + completion sheet`.
4. **Play-mode smoke (Coplay MCP):** `play_game`, then `execute_script` to grant test keys (`GameManager.Instance.KeyPouch.Add(...)` incl. a fused D3), `capture_ui_canvas` the Quests screen (pouch), open a key detail + fusion sheet (capture), `StartDungeon` via script, capture HUD mid-run, fast-forward via `activeDungeon.Tick` reflection or wait out a short run, capture completion sheet. Verify drops in-dungeon carry the key color (equip one, look at the doll). `stop_game`. Scripts go in the scratchpad, pattern: `OpenEquipFlow.cs` / `QuestSmokeTest.cs` from earlier sprints.
5. Fix what the smoke run surfaces; commit.

---

### Task 18: Finalize

1. **User runs Test Runner** (full suite; expect prior count + ~35-40 new). Count from Test Runner, don't estimate.
2. Update `docs/implemented-systems.md` (new §Destinations: keys, fusion, dungeons — follow existing section style) and `docs/roadmap.md` (Sprint D1 done; D2 next). Update `docs/gameplay-loop.md` "Where the game is headed" (Destinations partially live).
3. Save scene if the scene changed (it shouldn't — all UI is code-built at runtime; if ExploreSceneBuilder was touched, rebuild + save via Tools menu).
4. Commit docs — `docs: Sprint D1 complete — keys + dungeons live`.

---

## Deferred (explicitly NOT in D1)

- Locations, LocationGenerator, dialogue trees (Sprint D2).
- Key art/icons beyond color chips + text (Sprint D3).
- Balance-sim destinations branch (Sprint D3; knobs are guesses until then).
- Unlock gating (first-Zone-Boss check) — deferred to tutorial work per D10; section is visible in D1.
