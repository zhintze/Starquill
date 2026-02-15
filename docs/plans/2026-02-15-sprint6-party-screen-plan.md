# Sprint 6: Party Screen — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a Party screen with character focus display, roster management, equipment display, action loadout, and level-up — accessible via bottom nav tab switching.

**Architecture:** Panel-swap navigation on the existing Canvas. A `ScreenManager` hides/shows screen panels. The Party screen has a persistent header (name/level/XP), a large paper doll focus area, and three sub-tabs (Roster/Equipment/Actions). Backend changes add level-up with species-weighted stat distribution and per-character unlocked/equipped verb management.

**Tech Stack:** Unity 6 (6000.3.8f1), C#, TextMeshPro, Canvas UI, existing DisplayBuilder/CharacterDisplay compositing pipeline.

**Design doc:** `docs/plans/2026-02-15-sprint6-party-screen-design.md`

---

### Task 1: CharacterInstance — XP Curve and Level-Up

Add level-up logic with species-weighted stat auto-distribution to `CharacterInstance`. XP curve: `xpToNextLevel = 100 * (1.18 ^ currentLevel)`. Stats distribute proportionally to `baseStats` ratios, with fractional accumulation tracked internally.

**Files:**
- Modify: `Assets/Scripts/Characters/CharacterInstance.cs`
- Test: `Assets/Tests/EditMode/Characters/CharacterInstanceTests.cs` (create)

**Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/Characters/CharacterInstanceTests.cs`:

```csharp
using NUnit.Framework;
using Starquill.Characters;
using Starquill.Data;

namespace Starquill.Tests.Characters
{
    public class CharacterInstanceTests
    {
        [Test]
        public void XpToNextLevel_Level1_Returns118()
        {
            var c = CreateCharacter(1);
            Assert.AreEqual(118, c.XpToNextLevel());
        }

        [Test]
        public void XpToNextLevel_Level10_Returns523()
        {
            var c = CreateCharacter(10);
            Assert.AreEqual(523, c.XpToNextLevel());
        }

        [Test]
        public void CanLevelUp_NotEnoughXp_ReturnsFalse()
        {
            var c = CreateCharacter(1);
            c.xp = 50;
            Assert.IsFalse(c.CanLevelUp());
        }

        [Test]
        public void CanLevelUp_EnoughXp_ReturnsTrue()
        {
            var c = CreateCharacter(1);
            c.xp = 200;
            Assert.IsTrue(c.CanLevelUp());
        }

        [Test]
        public void LevelUp_IncrementsLevel()
        {
            var c = CreateCharacter(1);
            c.xp = 200;
            c.LevelUp();
            Assert.AreEqual(2, c.level);
        }

        [Test]
        public void LevelUp_SubtractsXpCost()
        {
            var c = CreateCharacter(1);
            c.xp = 200;
            int cost = c.XpToNextLevel(); // 118
            c.LevelUp();
            Assert.AreEqual(200 - cost, c.xp);
        }

        [Test]
        public void LevelUp_DistributesStatsWeightedByBase()
        {
            // STR-heavy species: STR=10, DEX=5, others=3. Total=30
            var c = new CharacterInstance
            {
                id = "test", displayName = "Test", level = 1, xp = 9999,
                baseStats = new Stats { STR = 10, DEX = 5, CON = 3, INT = 3, WIS = 6, CHA = 3 }
            };

            // Level up 30 times to get statistically meaningful distribution
            var before = c.allocatedStats.Clone();
            for (int i = 0; i < 30; i++)
            {
                c.xp = 999999;
                c.LevelUp();
            }

            // STR should get the most allocation, DEX and WIS moderate, others least
            Assert.Greater(c.allocatedStats.STR, c.allocatedStats.CON,
                "STR (weight 10) should gain more than CON (weight 3)");
            Assert.Greater(c.allocatedStats.STR, c.allocatedStats.DEX,
                "STR (weight 10) should gain more than DEX (weight 5)");
        }

        [Test]
        public void LevelUp_NotEnoughXp_DoesNothing()
        {
            var c = CreateCharacter(1);
            c.xp = 10;
            c.LevelUp();
            Assert.AreEqual(1, c.level);
            Assert.AreEqual(10, c.xp);
        }

        [Test]
        public void LevelUp_DistributesAbout3PointsPerLevel()
        {
            var c = CreateCharacter(1);
            c.xp = 999999;
            var totalBefore = c.allocatedStats.Total;
            c.LevelUp();
            var totalAfter = c.allocatedStats.Total;
            // Each level distributes ~3 stat points (floor of 30/10)
            Assert.GreaterOrEqual(totalAfter - totalBefore, 2);
            Assert.LessOrEqual(totalAfter - totalBefore, 4);
        }

        private CharacterInstance CreateCharacter(int level)
        {
            return new CharacterInstance
            {
                id = "test", displayName = "Test", level = level,
                baseStats = new Stats { STR = 5, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Run in Unity Test Runner (EditMode). Expected: FAIL — `XpToNextLevel`, `CanLevelUp`, `LevelUp` methods don't exist.

**Step 3: Implement level-up in CharacterInstance**

Modify `Assets/Scripts/Characters/CharacterInstance.cs`. Add after the `allocatedStats` field:

```csharp
// Fractional stat accumulation for weighted distribution
private float[] statAccumulator = new float[6];

public int XpToNextLevel()
{
    return (int)(100 * System.Math.Pow(1.18, level));
}

public bool CanLevelUp()
{
    return xp >= XpToNextLevel();
}

public void LevelUp()
{
    int cost = XpToNextLevel();
    if (xp < cost) return;

    xp -= cost;
    level++;

    DistributeStats();
}

private void DistributeStats()
{
    float totalBase = baseStats.Total;
    if (totalBase <= 0) return;

    float pointsPerLevel = 3f;
    var types = (StatType[])System.Enum.GetValues(typeof(StatType));

    for (int i = 0; i < types.Length; i++)
    {
        float weight = baseStats.GetStat(types[i]) / totalBase;
        statAccumulator[i] += weight * pointsPerLevel;

        int whole = (int)statAccumulator[i];
        if (whole > 0)
        {
            allocatedStats.SetStat(types[i], allocatedStats.GetStat(types[i]) + whole);
            statAccumulator[i] -= whole;
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Run in Unity Test Runner (EditMode). Expected: All 9 tests PASS.

**Step 5: Commit**

```bash
git add Assets/Scripts/Characters/CharacterInstance.cs Assets/Tests/EditMode/Characters/CharacterInstanceTests.cs Assets/Tests/EditMode/Characters/CharacterInstanceTests.cs.meta
git commit -m "Add XP curve and species-weighted level-up to CharacterInstance"
```

---

### Task 2: CharacterInstance — Unlocked vs Equipped Verbs

Add `unlockedVerbs` list separate from `equippedVerbs`. Characters unlock actions as they level up. `equippedVerbs` must be a subset of `unlockedVerbs` within verb slot limits.

**Files:**
- Modify: `Assets/Scripts/Characters/CharacterInstance.cs`
- Test: `Assets/Tests/EditMode/Characters/CharacterInstanceTests.cs`

**Step 1: Write the failing tests**

Add to `CharacterInstanceTests.cs`:

```csharp
[Test]
public void UnlockedVerbs_InitiallyEmpty()
{
    var c = CreateCharacter(1);
    Assert.AreEqual(0, c.unlockedVerbs.Count);
}

[Test]
public void EquipVerb_FromUnlocked_Succeeds()
{
    var c = CreateCharacter(1);
    var verb = CreateTestVerb("slash");
    c.unlockedVerbs.Add(verb);
    Assert.IsTrue(c.EquipVerb(verb));
    Assert.AreEqual(1, c.equippedVerbs.Count);
}

[Test]
public void EquipVerb_NotUnlocked_Fails()
{
    var c = CreateCharacter(1);
    var verb = CreateTestVerb("slash");
    Assert.IsFalse(c.EquipVerb(verb));
    Assert.AreEqual(0, c.equippedVerbs.Count);
}

[Test]
public void EquipVerb_SlotsFull_Fails()
{
    var c = CreateCharacter(1); // level 1 = 2 slots
    var v1 = CreateTestVerb("v1");
    var v2 = CreateTestVerb("v2");
    var v3 = CreateTestVerb("v3");
    c.unlockedVerbs.Add(v1);
    c.unlockedVerbs.Add(v2);
    c.unlockedVerbs.Add(v3);
    c.EquipVerb(v1);
    c.EquipVerb(v2);
    Assert.IsFalse(c.EquipVerb(v3));
}

[Test]
public void UnequipVerb_RemovesFromEquipped()
{
    var c = CreateCharacter(1);
    var verb = CreateTestVerb("slash");
    c.unlockedVerbs.Add(verb);
    c.EquipVerb(verb);
    c.UnequipVerb(verb);
    Assert.AreEqual(0, c.equippedVerbs.Count);
    Assert.AreEqual(1, c.unlockedVerbs.Count); // still unlocked
}

[Test]
public void SwapVerb_ReplacesEquipped()
{
    var c = CreateCharacter(1); // 2 slots
    var v1 = CreateTestVerb("v1");
    var v2 = CreateTestVerb("v2");
    var v3 = CreateTestVerb("v3");
    c.unlockedVerbs.Add(v1);
    c.unlockedVerbs.Add(v2);
    c.unlockedVerbs.Add(v3);
    c.EquipVerb(v1);
    c.EquipVerb(v2);

    Assert.IsTrue(c.SwapVerb(0, v3)); // replace slot 0 (v1) with v3
    Assert.AreEqual(v3, c.equippedVerbs[0]);
    Assert.AreEqual(v2, c.equippedVerbs[1]);
}
```

Add helper method to test class:

```csharp
private VerbDefinition CreateTestVerb(string id)
{
    var verb = ScriptableObject.CreateInstance<VerbDefinition>();
    verb.verbId = id;
    verb.displayName = id;
    return verb;
}
```

Add required usings: `using Starquill.Core;` and `using UnityEngine;`.

**Step 2: Run tests to verify they fail**

Expected: FAIL — `unlockedVerbs` field, `EquipVerb`, `UnequipVerb`, `SwapVerb` don't exist.

**Step 3: Implement verb management**

Add to `CharacterInstance.cs`:

```csharp
public List<VerbDefinition> unlockedVerbs = new();

public bool EquipVerb(VerbDefinition verb)
{
    if (!unlockedVerbs.Contains(verb)) return false;
    if (equippedVerbs.Count >= GetVerbSlotCount()) return false;
    if (equippedVerbs.Contains(verb)) return false;
    equippedVerbs.Add(verb);
    return true;
}

public void UnequipVerb(VerbDefinition verb)
{
    equippedVerbs.Remove(verb);
}

public bool SwapVerb(int equippedIndex, VerbDefinition newVerb)
{
    if (equippedIndex < 0 || equippedIndex >= equippedVerbs.Count) return false;
    if (!unlockedVerbs.Contains(newVerb)) return false;
    equippedVerbs[equippedIndex] = newVerb;
    return true;
}
```

**Step 4: Run tests to verify they pass**

Expected: All 6 new tests PASS + previous 9 still PASS.

**Step 5: Commit**

```bash
git add Assets/Scripts/Characters/CharacterInstance.cs Assets/Tests/EditMode/Characters/CharacterInstanceTests.cs
git commit -m "Add unlocked/equipped verb management to CharacterInstance"
```

---

### Task 3: CharacterRoster — Party Swap Helpers

Add convenience methods for swapping party members: `IsInParty`, `GetPartySlot`, `AddToParty`, `RemoveFromParty`, `SwapPartyMember`.

**Files:**
- Modify: `Assets/Scripts/Characters/CharacterRoster.cs`
- Test: `Assets/Tests/EditMode/Characters/CharacterRosterTests.cs`

**Step 1: Write the failing tests**

Add to `CharacterRosterTests.cs`:

```csharp
[Test]
public void IsInParty_ReturnsTrue_WhenInParty()
{
    roster.AddCharacter(CreateTestCharacter("c0"));
    roster.SetPartyMember(0, 0);
    Assert.IsTrue(roster.IsInParty(0));
}

[Test]
public void IsInParty_ReturnsFalse_WhenOnBench()
{
    roster.AddCharacter(CreateTestCharacter("c0"));
    Assert.IsFalse(roster.IsInParty(0));
}

[Test]
public void GetPartySlot_ReturnsSlot_WhenInParty()
{
    roster.AddCharacter(CreateTestCharacter("c0"));
    roster.SetPartyMember(2, 0);
    Assert.AreEqual(2, roster.GetPartySlot(0));
}

[Test]
public void GetPartySlot_ReturnsNegative_WhenNotInParty()
{
    roster.AddCharacter(CreateTestCharacter("c0"));
    Assert.AreEqual(-1, roster.GetPartySlot(0));
}

[Test]
public void AddToParty_FillsFirstEmptySlot()
{
    roster.AddCharacter(CreateTestCharacter("c0"));
    roster.AddCharacter(CreateTestCharacter("c1"));
    roster.SetPartyMember(0, 0);
    Assert.IsTrue(roster.AddToParty(1));
    Assert.AreEqual(1, roster.GetPartySlot(1));
}

[Test]
public void AddToParty_ReturnsFalse_WhenPartyFull()
{
    for (int i = 0; i < 5; i++)
        roster.AddCharacter(CreateTestCharacter($"c{i}"));
    for (int i = 0; i < 4; i++)
        roster.SetPartyMember(i, i);
    Assert.IsFalse(roster.AddToParty(4));
}

[Test]
public void RemoveFromParty_ClearsSlot()
{
    roster.AddCharacter(CreateTestCharacter("c0"));
    roster.SetPartyMember(0, 0);
    roster.RemoveFromParty(0);
    Assert.IsFalse(roster.IsInParty(0));
}

[Test]
public void SwapPartyMember_SwapsBenchAndParty()
{
    roster.AddCharacter(CreateTestCharacter("c0"));
    roster.AddCharacter(CreateTestCharacter("c1"));
    roster.SetPartyMember(0, 0);

    roster.SwapPartyMember(0, 1); // swap party slot 0: c0 out, c1 in

    var party = roster.GetActiveParty();
    Assert.AreEqual("c1", party[0].id);
    Assert.IsFalse(roster.IsInParty(0));
}
```

**Step 2: Run tests to verify they fail**

Expected: FAIL — `IsInParty`, `GetPartySlot`, `AddToParty`, `RemoveFromParty`, `SwapPartyMember` don't exist.

**Step 3: Implement party helpers**

Add to `CharacterRoster.cs`:

```csharp
public bool IsInParty(int rosterIndex)
{
    for (int i = 0; i < ActivePartyIndices.Length; i++)
        if (ActivePartyIndices[i] == rosterIndex) return true;
    return false;
}

public int GetPartySlot(int rosterIndex)
{
    for (int i = 0; i < ActivePartyIndices.Length; i++)
        if (ActivePartyIndices[i] == rosterIndex) return i;
    return -1;
}

public bool AddToParty(int rosterIndex)
{
    if (rosterIndex < 0 || rosterIndex >= Characters.Count) return false;
    if (IsInParty(rosterIndex)) return false;

    for (int i = 0; i < ActivePartyIndices.Length; i++)
    {
        if (ActivePartyIndices[i] == -1)
        {
            ActivePartyIndices[i] = rosterIndex;
            return true;
        }
    }
    return false;
}

public void RemoveFromParty(int rosterIndex)
{
    for (int i = 0; i < ActivePartyIndices.Length; i++)
    {
        if (ActivePartyIndices[i] == rosterIndex)
        {
            ActivePartyIndices[i] = -1;
            return;
        }
    }
}

public void SwapPartyMember(int partySlot, int newRosterIndex)
{
    if (partySlot < 0 || partySlot >= 4) return;
    if (newRosterIndex < 0 || newRosterIndex >= Characters.Count) return;
    ActivePartyIndices[partySlot] = newRosterIndex;
}
```

**Step 4: Run tests to verify they pass**

Expected: All 8 new tests PASS + previous 7 still PASS.

**Step 5: Commit**

```bash
git add Assets/Scripts/Characters/CharacterRoster.cs Assets/Tests/EditMode/Characters/CharacterRosterTests.cs
git commit -m "Add party swap helpers to CharacterRoster"
```

---

### Task 4: ScreenManager — Panel Show/Hide Router

A lightweight component on the Canvas that manages screen panel visibility. Holds references to all screen panels. `ShowScreen(int index)` hides current, shows target.

**Files:**
- Create: `Assets/Scripts/UI/ScreenManager.cs`
- Test: `Assets/Tests/EditMode/UI/ScreenManagerTests.cs` (create)

**Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/UI/ScreenManagerTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Starquill.UI;

namespace Starquill.Tests.UI
{
    public class ScreenManagerTests
    {
        private ScreenManager manager;
        private GameObject[] panels;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("Canvas");
            manager = go.AddComponent<ScreenManager>();
            panels = new GameObject[5];
            for (int i = 0; i < 5; i++)
            {
                panels[i] = new GameObject($"Panel_{i}");
                panels[i].transform.SetParent(go.transform);
            }
            manager.SetPanels(panels);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void ShowScreen_ActivatesTargetPanel()
        {
            manager.ShowScreen(2);
            Assert.IsTrue(panels[2].activeSelf);
        }

        [Test]
        public void ShowScreen_DeactivatesOtherPanels()
        {
            manager.ShowScreen(2);
            Assert.IsFalse(panels[0].activeSelf);
            Assert.IsFalse(panels[1].activeSelf);
            Assert.IsFalse(panels[3].activeSelf);
        }

        [Test]
        public void ShowScreen_InvalidIndex_DoesNothing()
        {
            manager.ShowScreen(0);
            manager.ShowScreen(99);
            Assert.IsTrue(panels[0].activeSelf); // unchanged
        }

        [Test]
        public void ActiveScreenIndex_TracksCurrentScreen()
        {
            manager.ShowScreen(3);
            Assert.AreEqual(3, manager.ActiveScreenIndex);
        }

        [Test]
        public void ShowScreen_NullPanel_SkipsGracefully()
        {
            panels[2].transform.SetParent(null);
            Object.DestroyImmediate(panels[2]);
            panels[2] = null;
            manager.SetPanels(panels);
            manager.ShowScreen(0);
            Assert.AreEqual(0, manager.ActiveScreenIndex);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Expected: FAIL — `ScreenManager` class doesn't exist.

**Step 3: Implement ScreenManager**

Create `Assets/Scripts/UI/ScreenManager.cs`:

```csharp
using System;
using UnityEngine;

namespace Starquill.UI
{
    public class ScreenManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] screenPanels;

        public int ActiveScreenIndex { get; private set; }

        public event Action<int> OnScreenChanged;

        public void SetPanels(GameObject[] panels)
        {
            screenPanels = panels;
        }

        public void ShowScreen(int index)
        {
            if (screenPanels == null) return;
            if (index < 0 || index >= screenPanels.Length) return;
            if (screenPanels[index] == null) return;

            for (int i = 0; i < screenPanels.Length; i++)
            {
                if (screenPanels[i] != null)
                    screenPanels[i].SetActive(i == index);
            }

            ActiveScreenIndex = index;
            OnScreenChanged?.Invoke(index);
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Expected: All 5 tests PASS.

**Step 5: Commit**

```bash
git add Assets/Scripts/UI/ScreenManager.cs Assets/Scripts/UI/ScreenManager.cs.meta Assets/Tests/EditMode/UI/ Assets/Tests/EditMode/UI.meta
git commit -m "Add ScreenManager for panel-swap navigation"
```

---

### Task 5: Wire BottomNavDisplay to ScreenManager

Modify `BottomNavDisplay` to accept a `ScreenManager` reference and call `ShowScreen()` when nav buttons are tapped. Modify `ExploreSceneController` to mute combat VFX when Explore panel is hidden.

**Files:**
- Modify: `Assets/Scripts/UI/BottomNavDisplay.cs`
- Modify: `Assets/Scripts/UI/ExploreSceneController.cs`

**Step 1: Modify BottomNavDisplay**

Add a `ScreenManager` reference and wire button clicks. Replace the full file:

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starquill.UI
{
    public class BottomNavDisplay : MonoBehaviour
    {
        [SerializeField] private Button[] navButtons;
        [SerializeField] private Color activeColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private ScreenManager screenManager;

        private int activeTab;

        public void SetScreenManager(ScreenManager manager)
        {
            screenManager = manager;
        }

        public void SetActiveTab(int index)
        {
            activeTab = index;
            for (int i = 0; i < navButtons.Length; i++)
            {
                if (navButtons[i] == null) continue;
                var colors = navButtons[i].colors;
                colors.normalColor = i == index ? activeColor : inactiveColor;
                navButtons[i].colors = colors;

                var label = navButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.color = i == index ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            }
        }

        public void CreatePlaceholderButtons()
        {
            var tabNames = new[] { "Explore", "Quests", "Loot", "Party", "Shop" };
            navButtons = new Button[tabNames.Length];

            for (int i = 0; i < tabNames.Length; i++)
            {
                var btnObj = new GameObject($"NavBtn_{tabNames[i]}");
                btnObj.transform.SetParent(transform, false);

                var rt = btnObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200, 100);

                var img = btnObj.AddComponent<Image>();
                img.color = inactiveColor;

                var btn = btnObj.AddComponent<Button>();
                navButtons[i] = btn;

                int tabIndex = i;
                btn.onClick.AddListener(() => OnNavButtonClicked(tabIndex));

                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(btnObj.transform, false);
                var labelRT = labelObj.AddComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = Vector2.zero;
                labelRT.offsetMax = Vector2.zero;
                var tmp = labelObj.AddComponent<TextMeshProUGUI>();
                tmp.text = tabNames[i];
                tmp.fontSize = 24;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }

        private void OnNavButtonClicked(int index)
        {
            SetActiveTab(index);
            if (screenManager != null)
                screenManager.ShowScreen(index);
        }
    }
}
```

**Step 2: Add muting to ExploreSceneController**

Add a public method and modify existing handlers. Add these members and methods to `ExploreSceneController`:

```csharp
private bool isMuted;

public void SetMuted(bool muted)
{
    isMuted = muted;
}
```

Then guard damage number spawning in `HandleCombatTick` and `HandleVerbActivated` — add `if (isMuted) return;` at the top of each method. Also guard verb bar updates in `HandleCombatTick`:

In `HandleCombatTick`, add at line 122 (top of method):
```csharp
if (isMuted) return;
```

In `HandleVerbActivated`, add at line 176 (top of method):
```csharp
if (isMuted) return;
```

**Step 3: Run compile check**

Run Coplay MCP `check_compile_errors`. Expected: No errors.

**Step 4: Commit**

```bash
git add Assets/Scripts/UI/BottomNavDisplay.cs Assets/Scripts/UI/ExploreSceneController.cs
git commit -m "Wire BottomNavDisplay to ScreenManager and add ExploreSceneController muting"
```

---

### Task 6: Head Crop Data — Add to species.json and SpeciesDisplayData

Add `head_y_offset` and `head_zoom` fields to each species entry in `species.json`. Add corresponding fields to `SpeciesDisplayData`. These values will be used by the portrait renderer to frame head crops.

**Files:**
- Modify: `Assets/Resources/Data/species.json`
- Modify: `Assets/Scripts/Display/DisplayDataRegistry.cs` (where `SpeciesDisplayData` is defined)
- Test: `Assets/Tests/EditMode/Display/SpeciesHeadCropTests.cs` (create)

**Step 1: Write the failing test**

Create `Assets/Tests/EditMode/Display/SpeciesHeadCropTests.cs`:

```csharp
using NUnit.Framework;
using Starquill.Display;

namespace Starquill.Tests.Display
{
    public class SpeciesHeadCropTests
    {
        [Test]
        public void SpeciesDisplayData_HasHeadCropFields()
        {
            var registry = DisplayDataRegistry.Instance;
            registry.LoadAll();

            Assert.IsTrue(registry.Species.Count > 0, "Species should be loaded");

            foreach (var kvp in registry.Species)
            {
                var species = kvp.Value;
                // head_y_offset should be a reasonable value
                Assert.GreaterOrEqual(species.HeadYOffset, -2f,
                    $"{kvp.Key} HeadYOffset out of range");
                Assert.LessOrEqual(species.HeadYOffset, 2f,
                    $"{kvp.Key} HeadYOffset out of range");
                // head_zoom should be positive
                Assert.Greater(species.HeadZoom, 0f,
                    $"{kvp.Key} HeadZoom should be positive");
            }
        }
    }
}
```

**Step 2: Run test to verify it fails**

Expected: FAIL — `HeadYOffset` and `HeadZoom` properties don't exist on `SpeciesDisplayData`.

**Step 3: Add fields to SpeciesDisplayData**

Read `Assets/Scripts/Display/DisplayDataRegistry.cs` to find the `SpeciesDisplayData` class. Add:

```csharp
public float HeadYOffset { get; set; }
public float HeadZoom { get; set; } = 0.5f;
```

In the JSON parsing code that populates `SpeciesDisplayData`, add parsing for the new fields:

```csharp
HeadYOffset = species.ContainsKey("head_y_offset") ? Convert.ToSingle(species["head_y_offset"]) : 0.35f,
HeadZoom = species.ContainsKey("head_zoom") ? Convert.ToSingle(species["head_zoom"]) : 0.5f,
```

Default values (0.35 y-offset, 0.5 zoom) provide a reasonable head-level framing for standard-scale species.

**Step 4: Add placeholder values to species.json**

Add `"head_y_offset": 0.35, "head_zoom": 0.5` to every species entry in `species.json`. Use default values initially — these will be tuned visually later per species. Species with different scales (fairy 0.4, bridge troll 1.3) will need different values but defaults work as scaffolding.

**Step 5: Run tests to verify they pass**

Expected: PASS.

**Step 6: Commit**

```bash
git add Assets/Resources/Data/species.json Assets/Scripts/Display/DisplayDataRegistry.cs Assets/Tests/EditMode/Display/SpeciesHeadCropTests.cs Assets/Tests/EditMode/Display/SpeciesHeadCropTests.cs.meta Assets/Tests/EditMode/Display.meta
git commit -m "Add head crop offset data to species for portrait rendering"
```

---

### Task 7: CharacterPortraitRenderer — Head-Cropped Portraits

A component similar to `CharacterDisplay` but renders a head-cropped portrait using the species' `head_y_offset` and `head_zoom` to position the compositing camera.

**Files:**
- Create: `Assets/Scripts/Display/CharacterPortraitRenderer.cs`
- Test: `Assets/Tests/EditMode/Display/CharacterPortraitRendererTests.cs` (create)

**Step 1: Write the failing test**

Create `Assets/Tests/EditMode/Display/CharacterPortraitRendererTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Starquill.Display;

namespace Starquill.Tests.Display
{
    public class CharacterPortraitRendererTests
    {
        [Test]
        public void Initialize_CreatesRenderTexture()
        {
            var go = new GameObject("Portrait");
            var renderer = go.AddComponent<CharacterPortraitRenderer>();
            renderer.Initialize(new ImageResolver(), 100);

            Assert.IsNotNull(renderer.Texture);
            Assert.AreEqual(100, renderer.Texture.width);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetHeadCrop_AdjustsCameraPosition()
        {
            var go = new GameObject("Portrait");
            var renderer = go.AddComponent<CharacterPortraitRenderer>();
            renderer.Initialize(new ImageResolver(), 100);
            renderer.SetHeadCrop(0.5f, 0.4f);

            // Camera Y should be offset, ortho size should match zoom
            Assert.AreEqual(0.5f, renderer.CameraYOffset, 0.01f);
            Assert.AreEqual(0.4f, renderer.CameraOrthoSize, 0.01f);

            Object.DestroyImmediate(go);
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Expected: FAIL — `CharacterPortraitRenderer` doesn't exist.

**Step 3: Implement CharacterPortraitRenderer**

Create `Assets/Scripts/Display/CharacterPortraitRenderer.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Starquill.Display
{
    public class CharacterPortraitRenderer : MonoBehaviour
    {
        private RenderTexture renderTexture;
        private Camera compositingCamera;
        private Transform compositingRoot;
        private readonly List<SpriteRenderer> spritePool = new();
        private ImageResolver imageResolver;
        private int compositingLayer = 31;

        public RenderTexture Texture => renderTexture;
        public float CameraYOffset => compositingCamera != null ? compositingCamera.transform.localPosition.y : 0f;
        public float CameraOrthoSize => compositingCamera != null ? compositingCamera.orthographicSize : 1f;

        public void Initialize(ImageResolver resolver, int textureSize = 100)
        {
            imageResolver = resolver;

            var camObj = new GameObject("PortraitCamera");
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0, 0, -10);

            compositingCamera = camObj.AddComponent<Camera>();
            compositingCamera.orthographic = true;
            compositingCamera.orthographicSize = 0.5f;
            compositingCamera.cullingMask = 1 << compositingLayer;
            compositingCamera.clearFlags = CameraClearFlags.SolidColor;
            compositingCamera.backgroundColor = new Color(0, 0, 0, 0);
            compositingCamera.enabled = false;

            compositingRoot = new GameObject("PortraitRoot").transform;
            compositingRoot.SetParent(transform);
            compositingRoot.localPosition = Vector3.zero;

            renderTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
            renderTexture.filterMode = FilterMode.Bilinear;
            renderTexture.Create();
        }

        public void SetHeadCrop(float yOffset, float zoom)
        {
            if (compositingCamera == null) return;
            var pos = compositingCamera.transform.localPosition;
            pos.y = yOffset;
            compositingCamera.transform.localPosition = pos;
            compositingCamera.orthographicSize = zoom;
        }

        public void SetPieces(List<DisplayPiece> pieces)
        {
            if (compositingCamera == null || imageResolver == null) return;

            while (spritePool.Count < pieces.Count)
            {
                var obj = new GameObject($"Layer_{spritePool.Count}");
                obj.transform.SetParent(compositingRoot);
                obj.transform.localPosition = Vector3.zero;
                obj.layer = compositingLayer;
                spritePool.Add(obj.AddComponent<SpriteRenderer>());
            }

            compositingRoot.gameObject.SetActive(true);

            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                var sr = spritePool[i];
                sr.gameObject.SetActive(true);
                sr.sprite = imageResolver.Resolve(piece.SpritePath);
                sr.sortingOrder = piece.Layer;
                sr.color = piece.TintColor;
                sr.flipX = piece.FlipH;
                sr.transform.localPosition = new Vector3(piece.Offset.x / 100f, piece.Offset.y / 100f, 0);
                sr.transform.localScale = new Vector3(piece.Scale.x, piece.Scale.y, 1f);
            }

            for (int i = pieces.Count; i < spritePool.Count; i++)
                spritePool[i].gameObject.SetActive(false);

            compositingCamera.targetTexture = renderTexture;
            compositingCamera.Render();
            compositingRoot.gameObject.SetActive(false);
        }

        public void RebuildFromData(
            SpeciesInstanceData speciesInstance,
            SpeciesDisplayData speciesData,
            List<EquipmentDisplayInfo> equipment,
            DisplayBuilder builder)
        {
            var pieces = builder.Build(speciesInstance, speciesData, equipment);
            SetPieces(pieces);
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Object.Destroy(renderTexture);
            }
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Expected: Both tests PASS.

**Step 5: Commit**

```bash
git add Assets/Scripts/Display/CharacterPortraitRenderer.cs Assets/Scripts/Display/CharacterPortraitRenderer.cs.meta Assets/Tests/EditMode/Display/CharacterPortraitRendererTests.cs Assets/Tests/EditMode/Display/CharacterPortraitRendererTests.cs.meta
git commit -m "Add CharacterPortraitRenderer for head-cropped portraits"
```

---

### Task 8: PartyScreenController — Core Logic

The main controller for the Party screen. Manages selected character, sub-tab switching, and data binding between the roster and UI elements. Pure logic, no UI building — the scene builder (Task 12) creates the hierarchy.

**Files:**
- Create: `Assets/Scripts/UI/PartyScreenController.cs`
- Test: `Assets/Tests/EditMode/UI/PartyScreenControllerTests.cs` (create)

**Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/UI/PartyScreenControllerTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Starquill.Characters;
using Starquill.Data;
using Starquill.UI;

namespace Starquill.Tests.UI
{
    public class PartyScreenControllerTests
    {
        private PartyScreenController controller;
        private CharacterRoster roster;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("PartyScreen");
            controller = go.AddComponent<PartyScreenController>();
            roster = new CharacterRoster();
            for (int i = 0; i < 8; i++)
                roster.AddCharacter(CreateChar($"c{i}"));
            roster.InitializeDefaultParty();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void SetRoster_SetsSelectedToFirstPartyMember()
        {
            controller.SetRoster(roster);
            Assert.AreEqual(0, controller.SelectedRosterIndex);
        }

        [Test]
        public void SelectCharacter_UpdatesIndex()
        {
            controller.SetRoster(roster);
            controller.SelectCharacter(5);
            Assert.AreEqual(5, controller.SelectedRosterIndex);
        }

        [Test]
        public void SelectedCharacter_ReturnsCorrectInstance()
        {
            controller.SetRoster(roster);
            controller.SelectCharacter(3);
            Assert.AreEqual("c3", controller.SelectedCharacter.id);
        }

        [Test]
        public void SelectCharacter_InvalidIndex_DoesNothing()
        {
            controller.SetRoster(roster);
            controller.SelectCharacter(0);
            controller.SelectCharacter(99);
            Assert.AreEqual(0, controller.SelectedRosterIndex);
        }

        [Test]
        public void SetSubTab_UpdatesActiveTab()
        {
            controller.SetRoster(roster);
            controller.SetSubTab(2);
            Assert.AreEqual(2, controller.ActiveSubTab);
        }

        [Test]
        public void OnSelectionChanged_Fires_WhenCharacterSelected()
        {
            controller.SetRoster(roster);
            int firedIndex = -1;
            controller.OnSelectionChanged += idx => firedIndex = idx;
            controller.SelectCharacter(5);
            Assert.AreEqual(5, firedIndex);
        }

        [Test]
        public void OnSubTabChanged_Fires_WhenTabSwitched()
        {
            controller.SetRoster(roster);
            int firedTab = -1;
            controller.OnSubTabChanged += tab => firedTab = tab;
            controller.SetSubTab(1);
            Assert.AreEqual(1, firedTab);
        }

        private CharacterInstance CreateChar(string id)
        {
            return new CharacterInstance
            {
                id = id, displayName = id, level = 1,
                baseStats = new Stats { STR = 5, DEX = 5, CON = 5, INT = 5, WIS = 5, CHA = 5 }
            };
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Expected: FAIL — `PartyScreenController` doesn't exist.

**Step 3: Implement PartyScreenController**

Create `Assets/Scripts/UI/PartyScreenController.cs`:

```csharp
using System;
using UnityEngine;
using Starquill.Characters;

namespace Starquill.UI
{
    public class PartyScreenController : MonoBehaviour
    {
        private CharacterRoster roster;

        public int SelectedRosterIndex { get; private set; }
        public int ActiveSubTab { get; private set; }
        public CharacterInstance SelectedCharacter =>
            roster != null && SelectedRosterIndex >= 0 && SelectedRosterIndex < roster.Characters.Count
                ? roster.Characters[SelectedRosterIndex]
                : null;

        public event Action<int> OnSelectionChanged;
        public event Action<int> OnSubTabChanged;

        public void SetRoster(CharacterRoster newRoster)
        {
            roster = newRoster;

            // Default selection: first active party member
            for (int i = 0; i < roster.ActivePartyIndices.Length; i++)
            {
                if (roster.ActivePartyIndices[i] >= 0)
                {
                    SelectedRosterIndex = roster.ActivePartyIndices[i];
                    return;
                }
            }

            SelectedRosterIndex = roster.Characters.Count > 0 ? 0 : -1;
        }

        public void SelectCharacter(int rosterIndex)
        {
            if (roster == null) return;
            if (rosterIndex < 0 || rosterIndex >= roster.Characters.Count) return;
            SelectedRosterIndex = rosterIndex;
            OnSelectionChanged?.Invoke(rosterIndex);
        }

        public void SetSubTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex > 2) return;
            ActiveSubTab = tabIndex;
            OnSubTabChanged?.Invoke(tabIndex);
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Expected: All 7 tests PASS.

**Step 5: Commit**

```bash
git add Assets/Scripts/UI/PartyScreenController.cs Assets/Scripts/UI/PartyScreenController.cs.meta Assets/Tests/EditMode/UI/PartyScreenControllerTests.cs Assets/Tests/EditMode/UI/PartyScreenControllerTests.cs.meta
git commit -m "Add PartyScreenController with selection and sub-tab management"
```

---

### Task 9: CharacterFocusDisplay — Paper Doll + Header

A UI component that manages the large paper doll render and the persistent header strip (name, species, level, XP, level-up button).

**Files:**
- Create: `Assets/Scripts/UI/CharacterFocusDisplay.cs`

**Step 1: Implement CharacterFocusDisplay**

Create `Assets/Scripts/UI/CharacterFocusDisplay.cs`:

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Display;
using Starquill.Equipment;

namespace Starquill.UI
{
    public class CharacterFocusDisplay : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text speciesLabel;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text xpLabel;
        [SerializeField] private Button levelUpButton;
        [SerializeField] private Image levelUpGlow;

        [Header("Paper Doll")]
        [SerializeField] private RawImage paperDollImage;

        [Header("Stats")]
        [SerializeField] private TMP_Text[] statLabels; // 6 labels, one per stat

        private CharacterDisplay characterDisplay;
        private DisplayBuilder builder;
        private DisplayDataRegistry registry;

        public event Action OnLevelUpPressed;

        public void Initialize(DisplayDataRegistry reg, DisplayBuilder bld)
        {
            registry = reg;
            builder = bld;

            if (levelUpButton != null)
                levelUpButton.onClick.AddListener(() => OnLevelUpPressed?.Invoke());
        }

        public void ShowCharacter(CharacterInstance character)
        {
            if (character == null) return;

            // Header
            if (nameLabel != null) nameLabel.text = character.displayName;
            if (speciesLabel != null) speciesLabel.text = character.speciesId;
            if (levelLabel != null) levelLabel.text = $"Lv {character.level}";
            UpdateXpDisplay(character);
            UpdateLevelUpButton(character);

            // Stats
            UpdateStatBar(character);

            // Paper doll
            RenderPaperDoll(character);
        }

        public void UpdateXpDisplay(CharacterInstance character)
        {
            if (xpLabel != null)
                xpLabel.text = $"{character.xp}/{character.XpToNextLevel()} XP";
        }

        public void UpdateLevelUpButton(CharacterInstance character)
        {
            bool canLevel = character.CanLevelUp();
            if (levelUpButton != null)
                levelUpButton.interactable = canLevel;
            if (levelUpGlow != null)
                levelUpGlow.enabled = canLevel;
        }

        public void UpdateStatBar(CharacterInstance character)
        {
            if (statLabels == null) return;

            var total = character.GetTotalStats();
            var baseS = character.baseStats;
            var alloc = character.allocatedStats;

            var types = new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
            var statTypes = (Starquill.Core.StatType[])Enum.GetValues(typeof(Starquill.Core.StatType));
            var colors = new[] {
                StatTypeColors.GetColor(Starquill.Core.StatType.STR),
                StatTypeColors.GetColor(Starquill.Core.StatType.DEX),
                StatTypeColors.GetColor(Starquill.Core.StatType.CON),
                StatTypeColors.GetColor(Starquill.Core.StatType.INT),
                StatTypeColors.GetColor(Starquill.Core.StatType.WIS),
                StatTypeColors.GetColor(Starquill.Core.StatType.CHA),
            };

            for (int i = 0; i < statLabels.Length && i < 6; i++)
            {
                if (statLabels[i] == null) continue;
                int basePlusAlloc = baseS.GetStat(statTypes[i]) + alloc.GetStat(statTypes[i]);
                int totalVal = total.GetStat(statTypes[i]);
                int equipBonus = totalVal - basePlusAlloc;

                statLabels[i].text = equipBonus > 0
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(colors[i])}>{types[i]}</color> {basePlusAlloc}<color=#4ADE80>+{equipBonus}</color>"
                    : $"<color=#{ColorUtility.ToHtmlStringRGB(colors[i])}>{types[i]}</color> {basePlusAlloc}";
            }
        }

        private void RenderPaperDoll(CharacterInstance character)
        {
            if (paperDollImage == null || registry == null || builder == null) return;

            if (!registry.Species.TryGetValue(character.speciesId, out var speciesData)) return;
            var instance = SpeciesInstanceData.CreateFrom(speciesData, registry);

            var equipList = new List<EquipmentDisplayInfo>();
            foreach (var eq in character.equipment)
            {
                if (eq == null) continue;
                equipList.Add(new EquipmentDisplayInfo
                {
                    ItemType = eq.ItemType,
                    ItemNum = eq.ItemNum,
                    BaseColor = eq.BaseColor,
                    VarianceColors = eq.VarianceColors as Dictionary<int, Color>
                        ?? new Dictionary<int, Color>(eq.VarianceColors),
                    IsOffhand = eq.Slot == EquipmentSlot.OffHand
                });
            }

            if (characterDisplay == null)
            {
                var displayObj = new GameObject("FocusPaperDoll");
                displayObj.transform.SetParent(transform);
                characterDisplay = displayObj.AddComponent<CharacterDisplay>();
                characterDisplay.Initialize(new ImageResolver());
            }

            characterDisplay.RebuildFromData(instance, speciesData, equipList, builder);
            paperDollImage.texture = characterDisplay.Texture;
        }

        private void OnDestroy()
        {
            if (characterDisplay != null)
                Destroy(characterDisplay.gameObject);
        }
    }
}
```

**Step 2: Run compile check**

Expected: No compile errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/CharacterFocusDisplay.cs Assets/Scripts/UI/CharacterFocusDisplay.cs.meta
git commit -m "Add CharacterFocusDisplay with paper doll, header, and stat bar"
```

---

### Task 10: RosterGridDisplay, EquipmentSlotsDisplay, ActionLoadoutDisplay

Three sub-tab content controllers. These are lightweight display scripts that bind data from `PartyScreenController` to UI elements. The scene builder (Task 12) creates their hierarchy.

**Files:**
- Create: `Assets/Scripts/UI/RosterGridDisplay.cs`
- Create: `Assets/Scripts/UI/EquipmentSlotsDisplay.cs`
- Create: `Assets/Scripts/UI/ActionLoadoutDisplay.cs`

**Step 1: Implement RosterGridDisplay**

Create `Assets/Scripts/UI/RosterGridDisplay.cs`:

```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;

namespace Starquill.UI
{
    public class RosterGridDisplay : MonoBehaviour
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Color partyBadgeColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color selectedBorderColor = new Color(1f, 0.9f, 0.3f);

        public event Action<int> OnCharacterTapped;

        public void Refresh(CharacterRoster roster, int selectedIndex)
        {
            if (cardContainer == null) return;

            // Clear existing cards
            for (int i = cardContainer.childCount - 1; i >= 0; i--)
                Destroy(cardContainer.GetChild(i).gameObject);

            for (int i = 0; i < roster.Characters.Count; i++)
            {
                var character = roster.Characters[i];
                var card = CreateCard(character, i, roster.IsInParty(i),
                    roster.GetPartySlot(i), i == selectedIndex);
                card.transform.SetParent(cardContainer, false);
            }
        }

        private GameObject CreateCard(CharacterInstance character, int rosterIndex,
            bool inParty, int partySlot, bool isSelected)
        {
            var card = new GameObject($"Card_{character.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 200);

            var img = card.GetComponent<Image>();
            img.color = isSelected ? selectedBorderColor : new Color(0.2f, 0.2f, 0.25f);

            var btn = card.GetComponent<Button>();
            int idx = rosterIndex;
            btn.onClick.AddListener(() => OnCharacterTapped?.Invoke(idx));

            // Name label
            var nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.transform.SetParent(card.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0);
            nameRT.anchorMax = new Vector2(1, 0.2f);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = $"{character.displayName}\nLv {character.level}";
            nameTmp.fontSize = 16;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;

            // Portrait area (placeholder — will be connected to CharacterPortraitRenderer)
            var portraitObj = new GameObject("Portrait", typeof(RectTransform), typeof(RawImage));
            portraitObj.transform.SetParent(card.transform, false);
            var portraitRT = portraitObj.GetComponent<RectTransform>();
            portraitRT.anchorMin = new Vector2(0.1f, 0.25f);
            portraitRT.anchorMax = new Vector2(0.9f, 0.95f);
            portraitRT.offsetMin = Vector2.zero;
            portraitRT.offsetMax = Vector2.zero;
            portraitObj.GetComponent<RawImage>().color = new Color(0.3f, 0.3f, 0.35f);

            // Party badge
            if (inParty && partySlot >= 0)
            {
                var badgeObj = new GameObject("Badge", typeof(RectTransform));
                badgeObj.transform.SetParent(card.transform, false);
                var badgeRT = badgeObj.GetComponent<RectTransform>();
                badgeRT.anchorMin = new Vector2(0.7f, 0.8f);
                badgeRT.anchorMax = new Vector2(1f, 1f);
                badgeRT.offsetMin = Vector2.zero;
                badgeRT.offsetMax = Vector2.zero;
                var badgeTmp = badgeObj.AddComponent<TextMeshProUGUI>();
                badgeTmp.text = $"{partySlot + 1}";
                badgeTmp.fontSize = 20;
                badgeTmp.fontStyle = FontStyles.Bold;
                badgeTmp.alignment = TextAlignmentOptions.Center;
                badgeTmp.color = partyBadgeColor;
            }

            return card;
        }
    }
}
```

**Step 2: Implement EquipmentSlotsDisplay**

Create `Assets/Scripts/UI/EquipmentSlotsDisplay.cs`:

```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.UI
{
    public class EquipmentSlotsDisplay : MonoBehaviour
    {
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private Button autoEquipButton;

        private static readonly Color[] RarityColors =
        {
            new Color(0.7f, 0.7f, 0.7f),    // Common - gray
            new Color(0.3f, 0.8f, 0.3f),    // Uncommon - green
            new Color(0.3f, 0.5f, 1f),      // Rare - blue
            new Color(0.7f, 0.3f, 0.9f),    // Epic - purple
            new Color(1f, 0.6f, 0.1f),      // Legendary - orange
        };

        private static readonly string[] SlotNames =
        {
            "Head", "Torso", "Arms", "Legs", "Feet",
            "Main Hand", "Off Hand",
            "Misc 1", "Misc 2", "Misc 3", "Misc 4"
        };

        public event Action<int> OnSlotTapped;

        public void Initialize()
        {
            if (autoEquipButton != null)
                autoEquipButton.interactable = false; // placeholder for future
        }

        public void Refresh(CharacterInstance character)
        {
            if (slotsContainer == null || character == null) return;

            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
                Destroy(slotsContainer.GetChild(i).gameObject);

            for (int i = 0; i < character.equipment.Length; i++)
            {
                var slot = CreateSlot(i, character.equipment[i]);
                slot.transform.SetParent(slotsContainer, false);
            }
        }

        private GameObject CreateSlot(int slotIndex, EquipmentInstance item)
        {
            var slot = new GameObject($"Slot_{slotIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(90, 90);

            var img = slot.GetComponent<Image>();
            if (item != null)
            {
                int rarityIdx = (int)item.Rarity;
                img.color = rarityIdx < RarityColors.Length ? RarityColors[rarityIdx] : RarityColors[0];
            }
            else
            {
                img.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);
            }

            var btn = slot.GetComponent<Button>();
            int idx = slotIndex;
            btn.onClick.AddListener(() => OnSlotTapped?.Invoke(idx));

            // Label
            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(slot.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(4, 4);
            labelRT.offsetMax = new Vector2(-4, -4);
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = item != null ? $"{item.DisplayName}" : SlotNames[slotIndex];
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = item != null ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            tmp.enableWordWrapping = true;

            return slot;
        }
    }
}
```

**Step 3: Implement ActionLoadoutDisplay**

Create `Assets/Scripts/UI/ActionLoadoutDisplay.cs`:

```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Data;

namespace Starquill.UI
{
    public class ActionLoadoutDisplay : MonoBehaviour
    {
        [SerializeField] private Transform equippedContainer;
        [SerializeField] private Transform availableContainer;

        public event Action<VerbDefinition> OnAvailableTapped;
        public event Action<int> OnEquippedTapped;

        public void Refresh(CharacterInstance character)
        {
            if (character == null) return;

            RefreshEquipped(character);
            RefreshAvailable(character);
        }

        private void RefreshEquipped(CharacterInstance character)
        {
            if (equippedContainer == null) return;

            for (int i = equippedContainer.childCount - 1; i >= 0; i--)
                Destroy(equippedContainer.GetChild(i).gameObject);

            int slotCount = character.GetVerbSlotCount();
            for (int i = 0; i < slotCount; i++)
            {
                VerbDefinition verb = i < character.equippedVerbs.Count ? character.equippedVerbs[i] : null;
                var card = CreateActionCard(verb, i, true, i >= character.equippedVerbs.Count);
                card.transform.SetParent(equippedContainer, false);
            }

            // Show locked slots
            for (int i = slotCount; i < 5; i++)
            {
                var locked = CreateLockedSlot(i);
                locked.transform.SetParent(equippedContainer, false);
            }
        }

        private void RefreshAvailable(CharacterInstance character)
        {
            if (availableContainer == null) return;

            for (int i = availableContainer.childCount - 1; i >= 0; i--)
                Destroy(availableContainer.GetChild(i).gameObject);

            foreach (var verb in character.unlockedVerbs)
            {
                if (character.equippedVerbs.Contains(verb)) continue;
                var card = CreateActionCard(verb, -1, false, false);
                card.transform.SetParent(availableContainer, false);
            }
        }

        private GameObject CreateActionCard(VerbDefinition verb, int slotIndex, bool isEquipped, bool isEmpty)
        {
            var card = new GameObject($"Action_{(verb != null ? verb.verbId : "empty")}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 80);

            var img = card.GetComponent<Image>();
            if (verb != null)
                img.color = StatTypeColors.GetColor(verb.statType);
            else
                img.color = new Color(0.2f, 0.2f, 0.25f, 0.5f);

            var btn = card.GetComponent<Button>();
            if (isEquipped)
            {
                int idx = slotIndex;
                btn.onClick.AddListener(() => OnEquippedTapped?.Invoke(idx));
            }
            else if (verb != null)
            {
                var v = verb;
                btn.onClick.AddListener(() => OnAvailableTapped?.Invoke(v));
            }

            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(card.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(8, 4);
            labelRT.offsetMax = new Vector2(-8, -4);
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = verb != null ? verb.displayName : (isEmpty ? "+" : "");
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return card;
        }

        private GameObject CreateLockedSlot(int slotIndex)
        {
            int unlockLevel = slotIndex switch { 2 => 11, 3 => 26, 4 => 51, _ => 0 };
            var card = new GameObject($"LockedSlot_{slotIndex}", typeof(RectTransform), typeof(Image));
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 80);
            card.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.3f);

            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(card.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"Lv {unlockLevel}";
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 0.4f, 0.4f);

            return card;
        }
    }
}
```

**Step 2: Run compile check**

Expected: No compile errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/RosterGridDisplay.cs Assets/Scripts/UI/RosterGridDisplay.cs.meta Assets/Scripts/UI/EquipmentSlotsDisplay.cs Assets/Scripts/UI/EquipmentSlotsDisplay.cs.meta Assets/Scripts/UI/ActionLoadoutDisplay.cs Assets/Scripts/UI/ActionLoadoutDisplay.cs.meta
git commit -m "Add roster grid, equipment slots, and action loadout display scripts"
```

---

### Task 11: PartyPortraitStrip — Party Indicator Portraits

The 4 small head-cropped portraits on the left side of the focus area.

**Files:**
- Create: `Assets/Scripts/UI/PartyPortraitStrip.cs`

**Step 1: Implement PartyPortraitStrip**

Create `Assets/Scripts/UI/PartyPortraitStrip.cs`:

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Display;
using Starquill.Equipment;

namespace Starquill.UI
{
    public class PartyPortraitStrip : MonoBehaviour
    {
        [SerializeField] private RawImage[] portraits;
        [SerializeField] private Image[] highlightRings;
        [SerializeField] private TMP_Text[] nameLabels;
        [SerializeField] private TMP_Text[] levelLabels;
        [SerializeField] private Button[] buttons;
        [SerializeField] private Color highlightColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color defaultRingColor = new Color(0.3f, 0.3f, 0.3f);

        private readonly List<CharacterPortraitRenderer> renderers = new();

        public event Action<int> OnPortraitTapped;

        public void Initialize()
        {
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == null) continue;
                    int idx = i;
                    buttons[i].onClick.AddListener(() => OnPortraitTapped?.Invoke(idx));
                }
            }
        }

        public void Refresh(CharacterRoster roster, int selectedRosterIndex,
            DisplayDataRegistry registry, DisplayBuilder builder)
        {
            var party = roster.GetActiveParty();

            for (int i = 0; i < 4; i++)
            {
                var character = i < party.Length ? party[i] : null;
                bool isSelected = character != null &&
                    roster.ActivePartyIndices[i] == selectedRosterIndex;

                if (highlightRings != null && i < highlightRings.Length && highlightRings[i] != null)
                    highlightRings[i].color = isSelected ? highlightColor : defaultRingColor;

                if (nameLabels != null && i < nameLabels.Length && nameLabels[i] != null)
                    nameLabels[i].text = character?.displayName ?? "";

                if (levelLabels != null && i < levelLabels.Length && levelLabels[i] != null)
                    levelLabels[i].text = character != null ? $"Lv {character.level}" : "";

                if (portraits != null && i < portraits.Length && portraits[i] != null)
                {
                    if (character != null && registry != null && builder != null)
                        RenderPortrait(i, character, registry, builder);
                    else
                        portraits[i].texture = null;
                }
            }
        }

        private void RenderPortrait(int index, CharacterInstance character,
            DisplayDataRegistry registry, DisplayBuilder builder)
        {
            while (renderers.Count <= index)
            {
                var obj = new GameObject($"PortraitRenderer_{renderers.Count}");
                obj.transform.SetParent(transform);
                var r = obj.AddComponent<CharacterPortraitRenderer>();
                r.Initialize(new ImageResolver(), 100);
                renderers.Add(r);
            }

            if (!registry.Species.TryGetValue(character.speciesId, out var speciesData)) return;

            renderers[index].SetHeadCrop(speciesData.HeadYOffset, speciesData.HeadZoom);

            var instance = SpeciesInstanceData.CreateFrom(speciesData, registry);
            var equipList = new List<EquipmentDisplayInfo>();
            foreach (var eq in character.equipment)
            {
                if (eq == null) continue;
                equipList.Add(new EquipmentDisplayInfo
                {
                    ItemType = eq.ItemType, ItemNum = eq.ItemNum,
                    BaseColor = eq.BaseColor,
                    VarianceColors = eq.VarianceColors as Dictionary<int, Color>
                        ?? new Dictionary<int, Color>(eq.VarianceColors),
                    IsOffhand = eq.Slot == EquipmentSlot.OffHand
                });
            }

            renderers[index].RebuildFromData(instance, speciesData, equipList, builder);
            portraits[index].texture = renderers[index].Texture;
        }

        private void OnDestroy()
        {
            foreach (var r in renderers)
                if (r != null) Destroy(r.gameObject);
            renderers.Clear();
        }
    }
}
```

**Step 2: Run compile check**

Expected: No compile errors.

**Step 3: Commit**

```bash
git add Assets/Scripts/UI/PartyPortraitStrip.cs Assets/Scripts/UI/PartyPortraitStrip.cs.meta
git commit -m "Add PartyPortraitStrip with head-cropped party portraits"
```

---

### Task 12: ExploreSceneBuilder — Add Panel Wrapping and Party Screen Hierarchy

Modify `ExploreSceneBuilder` to wrap existing Explore UI in an `ExplorePanel`, create a `PartyPanel` sibling with the full Party screen hierarchy, and wire up `ScreenManager` and `BottomNavDisplay`.

**Files:**
- Modify: `Assets/Editor/ExploreSceneBuilder.cs`

**Step 1: Modify ExploreSceneBuilder**

This is a large change. The key structural changes:

1. After `canvasRT` cleanup, create `ExplorePanel` as a child of Canvas — all existing explore content goes inside it
2. Create `PartyPanel` as a sibling of `ExplorePanel` — contains:
   - Header strip (name/species/level/XP/level-up button)
   - Focus area (paper doll center, party portrait strip left, character info right)
   - Stat bar
   - Sub-tab bar (3 buttons: Roster/Equipment/Actions)
   - Content area with 3 child panels (one per sub-tab)
3. Create placeholder panels for indexes 1 (Quests), 2 (Loot), 4 (Shop)
4. Add `ScreenManager` to Canvas, configure with all 5 panels
5. Wire `BottomNavDisplay` to `ScreenManager`
6. Wire `ExploreSceneController.SetMuted()` to `ScreenManager.OnScreenChanged`

The implementation should follow the exact same patterns as the existing builder: `CreatePanel`, `CreateTMPLabel`, `SetPrivateField`, etc.

**Important:** Keep all existing Explore scene building code intact — just nest it under `ExplorePanel`. The PartyPanel hierarchy should create all the containers and labels that the UI scripts reference via `[SerializeField]`.

Reference the design doc `docs/plans/2026-02-15-sprint6-party-screen-design.md` for exact layout dimensions and structure.

**Step 2: Run via Tools > Build Explore Scene**

Verify the scene builds without errors and both panels exist in the hierarchy.

**Step 3: Save scene**

Save via Coplay MCP: `save_scene("Assets/Scenes/ExploreScene")`.

**Step 4: Commit**

```bash
git add Assets/Editor/ExploreSceneBuilder.cs
git commit -m "Extend ExploreSceneBuilder with panel navigation and Party screen hierarchy"
```

---

### Task 13: Integration — Wire PartyScreenController to UI and GameManager

Connect all the pieces: `PartyScreenController` reads roster from `GameManager`, sub-tab switching shows/hides content panels, character selection updates all displays, level-up modifies character and refreshes, action equip/unequip works.

**Files:**
- Modify: `Assets/Scripts/UI/PartyScreenController.cs`
- Modify: `Assets/Scripts/Managers/GameManager.cs` (add `RebuildVerbPool` as public, add `OnRosterChanged` event)

**Step 1: Add public access to GameManager**

In `GameManager.cs`, make `RebuildVerbPool` public and add an event:

```csharp
public event Action OnRosterChanged;

public void RebuildVerbPool()  // change from private to public
```

After any roster change, fire `OnRosterChanged?.Invoke()`.

**Step 2: Expand PartyScreenController with UI wiring**

Add `[SerializeField]` references for all UI components and wire them in an `Initialize` method. Add handlers for:

- Character selection → update focus display, update sub-tab content
- Sub-tab switching → show/hide content panels, swap focus area side content
- Level-up button → call `CharacterInstance.LevelUp()`, refresh displays
- Action equip/unequip → modify character, call `GameManager.RebuildVerbPool()`
- Party add/remove/swap → modify roster, refresh UI, call `GameManager` rebuild

**Step 3: Run play test**

Play the game, switch to Party tab, verify:
- Characters display with paper dolls
- Sub-tabs switch content
- Roster shows all 8 characters with party badges
- Selecting a character updates focus display
- Level-up button state is correct
- Actions tab shows equipped/available verbs

**Step 4: Commit**

```bash
git add Assets/Scripts/UI/PartyScreenController.cs Assets/Scripts/Managers/GameManager.cs
git commit -m "Wire PartyScreenController to GameManager and all UI displays"
```

---

### Task 14: Final — Compile Check, Scene Rebuild, Play Test, All Tests

**Step 1: Check compile errors**

Run Coplay MCP `check_compile_errors`. Expected: None.

**Step 2: Rebuild scene**

Run `ExploreSceneBuilder.Execute()` via MCP, then save scene.

**Step 3: Play test**

Play game via MCP, switch between Explore and Party tabs, verify:
- No runtime errors in console
- Explore tab combat runs, pauses VFX when on Party tab
- Party tab shows characters, all sub-tabs work
- Can select different characters
- Can swap party members
- Level-up button state correct
- Action equip/unequip functional

**Step 4: Run all tests**

Run all tests in Unity Test Runner (EditMode). Expected: All tests pass (143 existing + ~30 new ≈ 173 total).

**Step 5: Commit any final fixes**

```bash
git add -A && git commit -m "Sprint 6 complete: Party screen with navigation, roster, equipment, and actions"
```
