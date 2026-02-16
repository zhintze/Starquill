# Sprint 8: Equipment UI + Loot Screen Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build the Equipment tab spatial slot cluster with drawer-based item management, and the Loot screen with inventory grid, item detail, and equip/sell flow.

**Architecture:** Both screens share common data helpers (ItemDisplayData, slot position mapping) and the same CharacterPortraitRenderer pattern for rendering equipment. The Equipment tab drawer and Loot screen detail panel use the same comparison logic (ItemComparer). GameManager already has EquipItemFromInventory, SellItem, and AutoEquipper backends — this sprint wires them to UI.

**Tech Stack:** Unity 6, C#, TextMeshPro, Unity UI (Canvas/RectTransform), CharacterPortraitRenderer for equipment rendering

**Design Reference:** `docs/plans/2026-02-16-equipment-ui-design.md`

---

### Task 1: ItemDisplayData Helper + Tests

Pure C# data struct that formats an EquipmentInstance for display — same pattern as ActionCardData for verbs.

**Files:**
- Create: `Assets/Scripts/UI/ItemDisplayData.cs`
- Create: `Assets/Tests/EditMode/UI/ItemDisplayDataTests.cs`

**Context:**
- `EquipmentInstance` (Assets/Scripts/Equipment/EquipmentInstance.cs) has: DisplayName, Slot, Rarity, StatMods, RolledAffixes, ItemType, ItemNum
- `ItemComparer.ScoreItem()` returns a float score
- `SellCalculator.GetSellValue(item, questLevel)` returns gold value
- Rarity enum is in `Starquill.Core` namespace
- EquipmentSlot enum is in `Starquill.Core` namespace
- Existing RarityColors array in EquipmentSlotsDisplay.cs lines 15-22 (gray/green/blue/purple/orange)

**Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/UI/ItemDisplayDataTests.cs` in assembly `Starquill.Tests.EditMode`:

```csharp
using NUnit.Framework;
using Starquill.Core;
using Starquill.Equipment;
using Starquill.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Starquill.Tests.EditMode.UI
{
    public class ItemDisplayDataTests
    {
        private EquipmentInstance MakeItem(string name, EquipmentSlot slot, Rarity rarity,
            int str = 0, int dex = 0, int intStat = 0)
        {
            var stats = new Stats();
            if (str > 0) stats.SetStat(StatType.STR, str);
            if (dex > 0) stats.SetStat(StatType.DEX, dex);
            if (intStat > 0) stats.SetStat(StatType.INT, intStat);
            return new EquipmentInstance("hd", 1, slot, rarity,
                Color.white, null, stats, null,
                null, null, null, false, null, name);
        }

        [Test]
        public void FromItem_SetsDisplayName()
        {
            var item = MakeItem("Iron Helm", EquipmentSlot.Head, Rarity.Common);
            var data = ItemDisplayData.FromItem(item, 1);
            Assert.AreEqual("Iron Helm", data.DisplayName);
        }

        [Test]
        public void FromItem_SetsSlotName()
        {
            var item = MakeItem("Iron Helm", EquipmentSlot.Head, Rarity.Common);
            var data = ItemDisplayData.FromItem(item, 1);
            Assert.AreEqual("Head", data.SlotName);
        }

        [Test]
        public void FromItem_SetsRarityName()
        {
            var item = MakeItem("Gold Helm", EquipmentSlot.Head, Rarity.Rare);
            var data = ItemDisplayData.FromItem(item, 1);
            Assert.AreEqual("Rare", data.RarityName);
        }

        [Test]
        public void FromItem_SetsRarityColor()
        {
            var item = MakeItem("Helm", EquipmentSlot.Head, Rarity.Uncommon);
            var data = ItemDisplayData.FromItem(item, 1);
            // Uncommon = green
            Assert.AreEqual(new Color(0.3f, 0.8f, 0.3f), data.RarityColor);
        }

        [Test]
        public void FromItem_CalculatesScore()
        {
            var item = MakeItem("Helm", EquipmentSlot.Head, Rarity.Common, str: 5, dex: 3);
            var data = ItemDisplayData.FromItem(item, 1);
            Assert.AreEqual(ItemComparer.ScoreItem(item), data.Score);
        }

        [Test]
        public void FromItem_CalculatesSellValue()
        {
            var item = MakeItem("Helm", EquipmentSlot.Head, Rarity.Common);
            var data = ItemDisplayData.FromItem(item, 5);
            Assert.AreEqual(SellCalculator.GetSellValue(item, 5), data.SellValue);
        }

        [Test]
        public void FromItem_FormatsStatSummary()
        {
            var item = MakeItem("Helm", EquipmentSlot.Head, Rarity.Common, str: 5, dex: 3);
            var data = ItemDisplayData.FromItem(item, 1);
            // Should contain non-zero stats
            Assert.That(data.StatSummary, Does.Contain("STR"));
            Assert.That(data.StatSummary, Does.Contain("DEX"));
        }

        [Test]
        public void FromItem_NullItem_ReturnsEmpty()
        {
            var data = ItemDisplayData.FromItem(null, 1);
            Assert.AreEqual("", data.DisplayName);
            Assert.AreEqual(0f, data.Score);
        }

        [Test]
        public void SlotNames_AllElevenSlots()
        {
            // Verify all slot names are correct
            Assert.AreEqual("Head", ItemDisplayData.GetSlotName(EquipmentSlot.Head));
            Assert.AreEqual("Main Hand", ItemDisplayData.GetSlotName(EquipmentSlot.MainHand));
            Assert.AreEqual("Off Hand", ItemDisplayData.GetSlotName(EquipmentSlot.OffHand));
            Assert.AreEqual("Misc 1", ItemDisplayData.GetSlotName(EquipmentSlot.Misc1));
        }
    }
}
```

**Step 2: Write minimal implementation**

Create `Assets/Scripts/UI/ItemDisplayData.cs`:

```csharp
using Starquill.Core;
using Starquill.Equipment;
using UnityEngine;

namespace Starquill.UI
{
    public struct ItemDisplayData
    {
        public string DisplayName;
        public string SlotName;
        public string RarityName;
        public Color RarityColor;
        public float Score;
        public double SellValue;
        public string StatSummary;
        public EquipmentSlot Slot;
        public Rarity Rarity;

        private static readonly Color[] RarityColors =
        {
            new Color(0.7f, 0.7f, 0.7f),   // Common
            new Color(0.3f, 0.8f, 0.3f),   // Uncommon
            new Color(0.3f, 0.5f, 1f),     // Rare
            new Color(0.7f, 0.3f, 0.9f),   // Epic
            new Color(1f, 0.6f, 0.1f),     // Legendary
        };

        private static readonly string[] SlotNameLookup =
        {
            "Head", "Torso", "Arms", "Legs", "Feet",
            "Main Hand", "Off Hand",
            "Misc 1", "Misc 2", "Misc 3", "Misc 4"
        };

        public static string GetSlotName(EquipmentSlot slot)
        {
            int idx = (int)slot;
            return idx >= 0 && idx < SlotNameLookup.Length ? SlotNameLookup[idx] : "Unknown";
        }

        public static Color GetRarityColor(Rarity rarity)
        {
            int idx = (int)rarity;
            return idx >= 0 && idx < RarityColors.Length ? RarityColors[idx] : RarityColors[0];
        }

        public static ItemDisplayData FromItem(EquipmentInstance item, int questLevel)
        {
            if (item == null)
                return new ItemDisplayData { DisplayName = "", StatSummary = "" };

            var stats = item.GetTotalStatMods();
            var statParts = new System.Collections.Generic.List<string>();
            var statTypes = new[] { StatType.STR, StatType.DEX, StatType.CON,
                                    StatType.INT, StatType.WIS, StatType.CHA };
            foreach (var st in statTypes)
            {
                int val = stats.GetStat(st);
                if (val != 0) statParts.Add($"{st} +{val}");
            }

            return new ItemDisplayData
            {
                DisplayName = item.DisplayName,
                SlotName = GetSlotName(item.Slot),
                RarityName = item.Rarity.ToString(),
                RarityColor = GetRarityColor(item.Rarity),
                Score = ItemComparer.ScoreItem(item),
                SellValue = SellCalculator.GetSellValue(item, questLevel),
                StatSummary = string.Join("  ", statParts),
                Slot = item.Slot,
                Rarity = item.Rarity,
            };
        }
    }
}
```

**Step 3: Run tests, verify pass**

Run in Unity Editor: Edit Mode tests under `Starquill.Tests.EditMode.UI.ItemDisplayDataTests`

**Step 4: Commit**

```
feat: Add ItemDisplayData helper for equipment display formatting
```

---

### Task 2: Rewrite EquipmentSlotsDisplay with Spatial Cluster Layout

Replace the flat GridLayoutGroup with the spatial cluster from the design doc. Each slot is a 110×110 tile positioned at body-mapped coordinates. Equipped items show rarity-colored background with name label. Empty slots show dim silhouette text.

**Files:**
- Modify: `Assets/Scripts/UI/EquipmentSlotsDisplay.cs` (full rewrite)

**Context:**
- Current EquipmentSlotsDisplay (Assets/Scripts/UI/EquipmentSlotsDisplay.cs) uses GridLayoutGroup + 90×90 cells
- Design doc spatial layout:
  ```
          [Head]
    [Main][Chest][Misc1]
    [Off] [Legs] [Misc2]
          [Shoes][Misc3]
                 [Misc4]
  ```
- Slot cluster area is ~520px tall in design, lives inside EquipmentContent panel
- slotsContainer is the parent Transform (set by ExploreSceneBuilder)
- Each tile: 110×110, rarity-colored border, item name centered, dim gray for empty
- OnSlotTapped event stays the same (int slotIndex)
- Add new field: `selectedSlotIndex` for highlight ring
- Add new event: `OnOptimizeTapped` for Optimize button
- autoEquipButton already exists as SerializeField

**Step 1: Define slot positions**

The spatial cluster is positioned within a container. Use absolute anchored positions relative to container center. The cluster is roughly 3 columns wide × 5 rows. Tile size = 110×110, gap = 8px.

Column positions (center-aligned in container):
- Left col (weapons): x = -118 (Main, Off)
- Center col: x = 0 (Head, Chest, Legs, Shoes)
- Right col (misc): x = 118 (Misc1, Misc2, Misc3, Misc4)

Row positions (top-down from center):
- Row 0: y = 180 (Head only)
- Row 1: y = 62 (Main, Chest, Misc1)
- Row 2: y = -56 (Off, Legs, Misc2)
- Row 3: y = -174 (Shoes, Misc3)
- Row 4: y = -292 (Misc4 only — but this may be too low, adjust to keep cluster compact)

Actually, to keep it tighter:
- Row 0 (Head): y offset from top
- Row 1 (Main/Chest/Misc1): below Head
- Row 2 (Off/Legs/Misc2): below that
- Row 3 (Shoes/Misc3): below that
- Row 4 (Misc4): below Misc3

Exact positions array (slotIndex → Vector2):
```
Head(0):      (0, 180)
Torso(1):     (0, 62)
Arms(2):      — skip, Arms doesn't have a spatial position in the design.
```

Wait — the design says Head/Chest/Legs/Shoes but EquipmentSlot has Head/Torso/Arms/Legs/Feet. The design maps:
- Head → Head (slot 0)
- Chest → Torso (slot 1)
- Arms slot 2 is NOT in the spatial layout... Actually re-reading the user's message: "equipment tiles need to be where the party tiles are (head, chest, legs, shoes) with the same sized ones on the right side (misc1, misc2, misc3, misc4) with main and off hand on the bottom left corner"

So the spatial positions for all 11 slots:
```
Slot 0 (Head):     center-top
Slot 1 (Torso):    center-row2
Slot 2 (Arms):     Not mentioned — maybe hide or put in misc column?
```

Actually looking at the design doc again:
```
        [Head]
  [Main][Chest][Misc1]
  [Off] [Legs] [Misc2]
        [Shoes][Misc3]
               [Misc4]
```

This shows 11 slots: Head, Main, Chest, Misc1, Off, Legs, Misc2, Shoes, Misc3, Misc4 = 10 slots. But we have 11 (Head, Torso, **Arms**, Legs, Feet, MainHand, OffHand, Misc1-4). Arms is missing from the layout!

The spatial layout should map:
- Head (0) → center top
- Torso/Chest (1) → center row 2
- Arms (2) → needs a position. Could go between Main/Off or as a 4th row. Let me put it left of Head, or add a row. Best approach: put Arms next to Head since arms are upper body. Layout becomes:

```
     [Head]  [Arms]
[Main][Chest][Misc1]
[Off] [Legs] [Misc2]
      [Shoes][Misc3]
             [Misc4]
```

This gives us 3 columns, 5 rows, 11 slots. That works perfectly.

**Step 2: Write the rewritten EquipmentSlotsDisplay**

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

        private int selectedSlotIndex = -1;
        private CharacterInstance currentCharacter;

        public event Action<int> OnSlotTapped;
        public event Action OnOptimizeTapped;

        // Slot positions relative to container center (110×110 tiles, 8px gaps)
        // Layout:
        //      [Head]  [Arms]
        // [Main][Chest][Misc1]
        // [Off] [Legs] [Misc2]
        //       [Shoes][Misc3]
        //              [Misc4]
        private static readonly Vector2[] SlotPositions = new Vector2[11]
        {
            new(0, 236),       // 0: Head - center top
            new(0, 118),       // 1: Torso/Chest - center row 2
            new(118, 236),     // 2: Arms - right of head
            new(0, 0),         // 3: Legs - center row 3
            new(0, -118),      // 4: Feet/Shoes - center row 4
            new(-118, 118),    // 5: MainHand - left row 2
            new(-118, 0),      // 6: OffHand - left row 3
            new(118, 118),     // 7: Misc1 - right row 2
            new(118, 0),       // 8: Misc2 - right row 3
            new(118, -118),    // 9: Misc3 - right row 4
            new(118, -236),    // 10: Misc4 - right row 5
        };

        private static readonly string[] SlotLabels =
        {
            "Head", "Chest", "Arms", "Legs", "Shoes",
            "Main", "Off",
            "Misc", "Misc", "Misc", "Misc"
        };

        public void Initialize()
        {
            if (autoEquipButton != null)
            {
                autoEquipButton.onClick.AddListener(() => OnOptimizeTapped?.Invoke());
                autoEquipButton.interactable = true;
            }
        }

        public void SelectSlot(int slotIndex)
        {
            selectedSlotIndex = slotIndex;
            if (currentCharacter != null) Refresh(currentCharacter);
        }

        public void ClearSelection()
        {
            selectedSlotIndex = -1;
            if (currentCharacter != null) Refresh(currentCharacter);
        }

        public void Refresh(CharacterInstance character)
        {
            currentCharacter = character;
            if (slotsContainer == null || character == null) return;

            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
                Destroy(slotsContainer.GetChild(i).gameObject);

            for (int i = 0; i < character.equipment.Length; i++)
            {
                var slot = CreateSlot(i, character.equipment[i], i == selectedSlotIndex);
                slot.transform.SetParent(slotsContainer, false);
            }
        }

        private GameObject CreateSlot(int slotIndex, EquipmentInstance item, bool isSelected)
        {
            var slot = new GameObject($"Slot_{slotIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(110, 110);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            if (slotIndex < SlotPositions.Length)
                rt.anchoredPosition = SlotPositions[slotIndex];

            var img = slot.GetComponent<Image>();
            if (item != null)
            {
                Color rarityColor = ItemDisplayData.GetRarityColor(item.Rarity);
                img.color = isSelected
                    ? Color.Lerp(rarityColor, Color.white, 0.3f)
                    : rarityColor;
            }
            else
            {
                img.color = isSelected
                    ? new Color(0.3f, 0.3f, 0.35f, 0.8f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.5f);
            }

            // Highlight ring for selected slot
            if (isSelected)
            {
                var ring = new GameObject("Ring", typeof(RectTransform), typeof(Image));
                ring.transform.SetParent(slot.transform, false);
                var ringRT = ring.GetComponent<RectTransform>();
                ringRT.anchorMin = Vector2.zero;
                ringRT.anchorMax = Vector2.one;
                ringRT.offsetMin = new Vector2(-3, -3);
                ringRT.offsetMax = new Vector2(3, 3);
                ringRT.SetAsFirstSibling();
                var ringImg = ring.GetComponent<Image>();
                ringImg.color = new Color(1f, 0.84f, 0f, 0.8f); // Gold ring
                // Make ring render behind content by being first sibling
            }

            // Slot label
            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(slot.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(4, 4);
            labelRT.offsetMax = new Vector2(-4, -4);
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = item != null ? item.DisplayName : SlotLabels[slotIndex];
            tmp.fontSize = item != null ? 12 : 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = item != null ? Color.white : new Color(0.4f, 0.4f, 0.45f);
            tmp.textWrappingMode = TextWrappingModes.Normal;

            var btn = slot.GetComponent<Button>();
            int idx = slotIndex;
            btn.onClick.AddListener(() =>
            {
                selectedSlotIndex = idx;
                OnSlotTapped?.Invoke(idx);
            });

            return slot;
        }
    }
}
```

**Step 3: Update ExploreSceneBuilder**

Replace the equipment content section:
- Remove the GridLayoutGroup slotsContainer
- Create a plain RectTransform slotsContainer (no layout group — slots use absolute positions)
- Keep the autoEquipButton but restyle it
- Set slotsContainer to fill the equipment content area with proper centering

In `ExploreSceneBuilder.cs`, find the equipment content creation section (~lines 445-473) and replace:

```csharp
// Equipment Content (sub-tab 1)
var equipContent = CreatePanel("EquipmentContent", contentArea.transform);
StretchFill(equipContent);
equipContent.GetComponent<Image>().color = Color.clear;

// SlotsContainer - spatial cluster (no layout group, slots use absolute positions)
var slotsContainer = new GameObject("SlotsContainer", typeof(RectTransform));
slotsContainer.transform.SetParent(equipContent.transform, false);
var slotsContainerRT = slotsContainer.GetComponent<RectTransform>();
slotsContainerRT.anchorMin = new Vector2(0, 0.15f);  // Leave room for button at bottom
slotsContainerRT.anchorMax = new Vector2(1, 1);
slotsContainerRT.offsetMin = Vector2.zero;
slotsContainerRT.offsetMax = Vector2.zero;
// No layout group — slots use anchoredPosition relative to container center

// AutoEquipButton at bottom
var autoEquipBtnObj = new GameObject("AutoEquipButton", typeof(RectTransform), typeof(Image), typeof(Button));
autoEquipBtnObj.transform.SetParent(equipContent.transform, false);
var autoEquipRT = autoEquipBtnObj.GetComponent<RectTransform>();
autoEquipRT.anchorMin = new Vector2(0.25f, 0);
autoEquipRT.anchorMax = new Vector2(0.75f, 0);
autoEquipRT.pivot = new Vector2(0.5f, 0);
autoEquipRT.anchoredPosition = new Vector2(0, 10);
autoEquipRT.sizeDelta = new Vector2(0, 44);
autoEquipBtnObj.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f);

var autoEquipLabel = new GameObject("Label", typeof(RectTransform));
autoEquipLabel.transform.SetParent(autoEquipBtnObj.transform, false);
StretchFill(autoEquipLabel);
var autoTmp = autoEquipLabel.AddComponent<TMPro.TextMeshProUGUI>();
autoTmp.text = "Optimize";
autoTmp.fontSize = 18;
autoTmp.alignment = TMPro.TextAlignmentOptions.Center;
autoTmp.color = Color.white;
```

**Step 4: Commit**

```
feat: Rewrite EquipmentSlotsDisplay with spatial cluster layout
```

---

### Task 3: Wire Equipment Tab — Optimize Button + Slot Selection

Wire the Optimize button to AutoEquipper, and slot tapping to highlight the selected slot. No drawer yet — just the slot cluster interaction.

**Files:**
- Modify: `Assets/Scripts/UI/PartyScreenController.cs`
- Modify: `Assets/Scripts/Managers/GameManager.cs` (add AutoEquipCharacter method)

**Context:**
- `AutoEquipper.AutoEquip(character, inventory)` in Characters namespace returns `AutoEquipResult { ItemsEquipped }`
- `GameManager` has `LootInventory` property, `Roster`, `BuildPartyFromRoster()`, `OnRosterChanged`
- PartyScreenController has `equipmentSlots` field, already calls `equipmentSlots.Initialize()` in Start()
- Need to wire: `equipmentSlots.OnSlotTapped += HandleEquipmentSlotTapped`
- Need to wire: `equipmentSlots.OnOptimizeTapped += HandleOptimize`
- HandleOptimize: call GameManager.AutoEquipCharacter(characterIndex), refresh display
- HandleEquipmentSlotTapped: update selection highlight, later will open drawer

**Step 1: Add AutoEquipCharacter to GameManager**

```csharp
public int AutoEquipCharacter(int characterIndex)
{
    if (roster == null || characterIndex < 0 || characterIndex >= roster.Characters.Count) return 0;
    var character = roster.Characters[characterIndex];
    var result = AutoEquipper.AutoEquip(character, lootInventory);
    if (result.ItemsEquipped > 0)
    {
        BuildPartyFromRoster();
        OnRosterChanged?.Invoke();
    }
    return result.ItemsEquipped;
}
```

**Step 2: Wire events in PartyScreenController**

Add to Start() after the existing equipment initialization:
```csharp
if (equipmentSlots != null)
{
    equipmentSlots.OnSlotTapped += HandleEquipmentSlotTapped;
    equipmentSlots.OnOptimizeTapped += HandleOptimize;
}
```

Add handlers:
```csharp
private void HandleEquipmentSlotTapped(int slotIndex)
{
    // For now just highlight the slot; drawer comes in Task 5
    if (equipmentSlots != null)
        equipmentSlots.SelectSlot(slotIndex);
}

private void HandleOptimize()
{
    var gm = GameManager.Instance;
    if (gm == null) return;
    int equipped = gm.AutoEquipCharacter(SelectedRosterIndex);
    RefreshAll();
}
```

Add cleanup in OnDestroy():
```csharp
if (equipmentSlots != null)
{
    equipmentSlots.OnSlotTapped -= HandleEquipmentSlotTapped;
    equipmentSlots.OnOptimizeTapped -= HandleOptimize;
}
```

**Step 3: Commit**

```
feat: Wire equipment slot selection and Optimize button to AutoEquipper
```

---

### Task 4: EquipmentDrawer — Bottom Sheet with Item List + Comparison

The core drawer behavior: when a slot is tapped, a bottom sheet slides up showing the currently equipped item and a scrollable list of compatible inventory items with delta badges.

**Files:**
- Create: `Assets/Scripts/UI/EquipmentDrawer.cs`
- Modify: `Assets/Editor/ExploreSceneBuilder.cs` (add drawer panel to Equipment content)

**Context:**
- Design doc: drawer starts collapsed at ~120px, expands to ~60% screen (covers slot cluster, character head still visible)
- Drawer header: equipped item card (name, rarity, stats)
- Drawer body: scrollable list of inventory items for selected slot
- Each item row: name + rarity color + delta badge (green ▲ +N / red ▼ -N)
- ★ BEST badge on highest-scoring item
- Two buttons: "Equip" (green, primary) and "Back" (secondary)
- Uses ItemComparer.Compare() for delta calculation
- Uses ItemComparer.FindBestForSlot() to identify ★ BEST
- Uses LootInventory.GetItemsForSlot() for candidates

**Step 1: Create EquipmentDrawer.cs**

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Core;
using Starquill.Equipment;

namespace Starquill.UI
{
    public class EquipmentDrawer : MonoBehaviour
    {
        [SerializeField] private GameObject drawerPanel;
        [SerializeField] private TMP_Text headerLabel;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private Transform itemListContainer;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button backButton;
        [SerializeField] private RectTransform drawerRect;

        private EquipmentInstance equippedItem;
        private EquipmentInstance previewedItem;
        private int selectedSlotIndex = -1;

        public bool IsOpen { get; private set; }
        public event Action<EquipmentInstance> OnEquipPressed;
        public event Action OnBackPressed;

        public void Initialize()
        {
            if (equipButton != null)
                equipButton.onClick.AddListener(() => OnEquipPressed?.Invoke(previewedItem));
            if (backButton != null)
                backButton.onClick.AddListener(() =>
                {
                    previewedItem = null;
                    OnBackPressed?.Invoke();
                    Close();
                });
            Close();
        }

        public void Open(int slotIndex, EquipmentInstance equipped, List<EquipmentInstance> candidates)
        {
            selectedSlotIndex = slotIndex;
            equippedItem = equipped;
            previewedItem = null;
            IsOpen = true;

            if (drawerPanel != null) drawerPanel.SetActive(true);

            RefreshHeader();
            RefreshItemList(candidates, equipped);
            RefreshButtons();
        }

        public void Close()
        {
            IsOpen = false;
            previewedItem = null;
            if (drawerPanel != null) drawerPanel.SetActive(false);
        }

        public void UpdateSummary(string text)
        {
            if (summaryLabel != null) summaryLabel.text = text;
        }

        private void RefreshHeader()
        {
            if (headerLabel == null) return;

            if (equippedItem != null)
            {
                var data = ItemDisplayData.FromItem(equippedItem, 1);
                headerLabel.text = $"<color=#{ColorUtility.ToHtmlStringRGB(data.RarityColor)}>{data.DisplayName}</color>\n" +
                                   $"<size=14>{data.StatSummary}</size>";
            }
            else
            {
                string slotName = ItemDisplayData.GetSlotName((EquipmentSlot)selectedSlotIndex);
                headerLabel.text = $"Empty {slotName} — Equip something!";
            }
        }

        private void RefreshItemList(List<EquipmentInstance> candidates, EquipmentInstance equipped)
        {
            if (itemListContainer == null) return;

            for (int i = itemListContainer.childCount - 1; i >= 0; i--)
                Destroy(itemListContainer.GetChild(i).gameObject);

            if (candidates == null || candidates.Count == 0)
            {
                var emptyLabel = new GameObject("Empty", typeof(RectTransform));
                emptyLabel.transform.SetParent(itemListContainer, false);
                var tmp = emptyLabel.AddComponent<TextMeshProUGUI>();
                tmp.text = "No items for this slot";
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.5f, 0.5f, 0.55f);
                var le = emptyLabel.AddComponent<LayoutElement>();
                le.preferredHeight = 60;
                le.flexibleWidth = 1;
                return;
            }

            // Find best item for ★ BEST badge
            var best = ItemComparer.FindBestForSlot((EquipmentSlot)selectedSlotIndex, candidates, equipped);

            // Sort by score descending (best first)
            candidates.Sort((a, b) => ItemComparer.ScoreItem(b).CompareTo(ItemComparer.ScoreItem(a)));

            foreach (var item in candidates)
            {
                bool isBest = item == best;
                var row = CreateItemRow(item, equipped, isBest);
                row.transform.SetParent(itemListContainer, false);
            }
        }

        private GameObject CreateItemRow(EquipmentInstance item, EquipmentInstance equipped, bool isBest)
        {
            var row = new GameObject($"Item_{item.DisplayName}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 70);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            le.flexibleWidth = 1;

            var img = row.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f);

            // Rarity stripe (left)
            var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(row.transform, false);
            var stripeRT = stripe.GetComponent<RectTransform>();
            stripeRT.anchorMin = new Vector2(0, 0);
            stripeRT.anchorMax = new Vector2(0, 1);
            stripeRT.pivot = new Vector2(0, 0.5f);
            stripeRT.sizeDelta = new Vector2(5, 0);
            stripeRT.anchoredPosition = Vector2.zero;
            stripe.GetComponent<Image>().color = ItemDisplayData.GetRarityColor(item.Rarity);

            // Item name + rarity (left side)
            var nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.transform.SetParent(row.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0);
            nameRT.anchorMax = new Vector2(0.55f, 1);
            nameRT.offsetMin = new Vector2(12, 4);
            nameRT.offsetMax = new Vector2(0, -4);
            var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            string bestTag = isBest ? " <color=#FFD700>★ BEST</color>" : "";
            nameTmp.text = $"<b>{item.DisplayName}</b>{bestTag}\n<size=12><color=#999>{item.Rarity}</color></size>";
            nameTmp.fontSize = 16;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.color = Color.white;
            nameTmp.richText = true;

            // Delta badge (right side)
            var delta = ItemComparer.Compare(item, equipped);
            var deltaObj = new GameObject("Delta", typeof(RectTransform));
            deltaObj.transform.SetParent(row.transform, false);
            var deltaRT = deltaObj.GetComponent<RectTransform>();
            deltaRT.anchorMin = new Vector2(0.55f, 0);
            deltaRT.anchorMax = new Vector2(1, 1);
            deltaRT.offsetMin = new Vector2(0, 4);
            deltaRT.offsetMax = new Vector2(-8, -4);
            var deltaTmp = deltaObj.AddComponent<TextMeshProUGUI>();
            string sign = delta.TotalDelta >= 0 ? "+" : "";
            string arrow = delta.IsUpgrade ? "▲" : (delta.TotalDelta < 0 ? "▼" : "–");
            Color deltaColor = delta.IsUpgrade ? new Color(0.3f, 0.9f, 0.3f)
                : (delta.TotalDelta < 0 ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.6f, 0.6f, 0.6f));
            deltaTmp.text = $"<size=22>{arrow} {sign}{delta.TotalDelta:F0}</size>";
            deltaTmp.fontSize = 22;
            deltaTmp.alignment = TextAlignmentOptions.MidlineRight;
            deltaTmp.color = deltaColor;

            // Tap to preview
            var btn = row.GetComponent<Button>();
            var capturedItem = item;
            btn.onClick.AddListener(() =>
            {
                previewedItem = capturedItem;
                RefreshButtons();
            });

            return row;
        }

        private void RefreshButtons()
        {
            if (equipButton != null)
                equipButton.interactable = previewedItem != null;
        }
    }
}
```

**Step 2: Add drawer to ExploreSceneBuilder**

After the EquipmentContent section, add a drawer panel:
- DrawerPanel: covers bottom ~60% of PartyPanel, starts hidden
- Contains: header (equipped item info), ScrollView (item list), button bar (Equip + Back)
- Wire to EquipmentDrawer component

**Step 3: Commit**

```
feat: Add EquipmentDrawer bottom sheet with item list and delta badges
```

---

### Task 5: Wire Equipment Drawer to PartyScreenController

Connect slot tapping → drawer open, equip button → GameManager.EquipItemFromInventory, back → close drawer.

**Files:**
- Modify: `Assets/Scripts/UI/PartyScreenController.cs`

**Context:**
- Add `[SerializeField] private EquipmentDrawer equipmentDrawer;`
- HandleEquipmentSlotTapped: open drawer with candidates from GameManager.LootInventory
- HandleDrawerEquip: call GameManager.EquipItemFromInventory, refresh, close drawer
- HandleDrawerBack: close drawer, clear slot selection
- Wire equipmentDrawer.Initialize() in Start()
- Wire events in Start(), unwire in OnDestroy()

**Step 1: Update PartyScreenController**

```csharp
// In Start(), after equipment initialization:
if (equipmentDrawer != null)
{
    equipmentDrawer.Initialize();
    equipmentDrawer.OnEquipPressed += HandleDrawerEquip;
    equipmentDrawer.OnBackPressed += HandleDrawerBack;
}

// Update HandleEquipmentSlotTapped:
private void HandleEquipmentSlotTapped(int slotIndex)
{
    if (equipmentSlots != null)
        equipmentSlots.SelectSlot(slotIndex);

    var gm = GameManager.Instance;
    if (gm == null || equipmentDrawer == null) return;

    var character = SelectedCharacter;
    if (character == null) return;

    var equipped = character.equipment[slotIndex];
    var candidates = gm.LootInventory.GetItemsForSlot((EquipmentSlot)slotIndex);
    equipmentDrawer.Open(slotIndex, equipped, candidates);

    int totalUpgrades = 0;
    int inventoryCount = gm.LootInventory.Count;
    int capacity = gm.LootInventory.Capacity;
    equipmentDrawer.UpdateSummary($"{candidates.Count} items for slot · {inventoryCount}/{capacity} inventory");
}

// New handlers:
private void HandleDrawerEquip(EquipmentInstance item)
{
    var gm = GameManager.Instance;
    if (gm == null || item == null) return;
    gm.EquipItemFromInventory(item, SelectedRosterIndex);
    if (equipmentDrawer != null) equipmentDrawer.Close();
    if (equipmentSlots != null) equipmentSlots.ClearSelection();
    RefreshAll();
}

private void HandleDrawerBack()
{
    if (equipmentSlots != null) equipmentSlots.ClearSelection();
    RefreshAll();
}
```

**Step 2: Commit**

```
feat: Wire equipment drawer to slot tapping and equip flow
```

---

### Task 6: LootScreenController + Inventory Grid

Build the Loot screen from scratch — replacing the placeholder. Shows a grid of inventory items with rarity colors, count/capacity header, and Optimize All button.

**Files:**
- Create: `Assets/Scripts/UI/LootScreenController.cs`
- Modify: `Assets/Editor/ExploreSceneBuilder.cs` (replace LootPlaceholder with real layout)

**Context:**
- Screen index 2 in ScreenManager
- Current placeholder: `CreatePlaceholderPanel("LootPlaceholder", ...)` in ExploreSceneBuilder
- Design: inventory grid (items sorted best-first), Optimize All button at top, item count/capacity
- GameManager: LootInventory, SellItem(), EquipItemFromInventory(), OnLootDropped event
- ItemComparer.ScoreItem() for sorting
- Each grid tile: ~100×100, rarity-colored background, item name text
- Tapping an item will open detail panel (Task 7)

**Step 1: Create LootScreenController**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Equipment;
using Starquill.Managers;

namespace Starquill.UI
{
    public class LootScreenController : MonoBehaviour
    {
        [SerializeField] private TMP_Text inventoryCountLabel;
        [SerializeField] private Transform itemGridContainer;
        [SerializeField] private Button optimizeAllButton;
        [SerializeField] private ScreenManager screenManager;

        public event Action<EquipmentInstance> OnItemTapped;

        public void Initialize()
        {
            if (optimizeAllButton != null)
                optimizeAllButton.onClick.AddListener(HandleOptimizeAll);
            if (screenManager != null)
                screenManager.OnScreenChanged += HandleScreenChanged;
        }

        private void HandleScreenChanged(int screenIndex)
        {
            if (screenIndex == 2) Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            RefreshHeader(gm.LootInventory);
            RefreshGrid(gm.LootInventory);
        }

        private void RefreshHeader(LootInventory inventory)
        {
            if (inventoryCountLabel != null)
                inventoryCountLabel.text = $"Inventory: {inventory.Count}/{inventory.Capacity}";
        }

        private void RefreshGrid(LootInventory inventory)
        {
            if (itemGridContainer == null) return;

            for (int i = itemGridContainer.childCount - 1; i >= 0; i--)
                Destroy(itemGridContainer.GetChild(i).gameObject);

            // Sort by score descending
            var sorted = inventory.Items.OrderByDescending(ItemComparer.ScoreItem).ToList();

            foreach (var item in sorted)
            {
                var tile = CreateItemTile(item);
                tile.transform.SetParent(itemGridContainer, false);
            }
        }

        private GameObject CreateItemTile(EquipmentInstance item)
        {
            var tile = new GameObject($"Item_{item.DisplayName}",
                typeof(RectTransform), typeof(Image), typeof(Button));

            var img = tile.GetComponent<Image>();
            img.color = ItemDisplayData.GetRarityColor(item.Rarity);

            var labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(tile.transform, false);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(4, 4);
            labelRT.offsetMax = new Vector2(-4, -4);
            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{item.DisplayName}\n<size=10>{item.Rarity}</size>";
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            var btn = tile.GetComponent<Button>();
            var capturedItem = item;
            btn.onClick.AddListener(() => OnItemTapped?.Invoke(capturedItem));

            return tile;
        }

        private void HandleOptimizeAll()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Roster == null) return;

            int totalEquipped = 0;
            for (int i = 0; i < gm.Roster.Characters.Count; i++)
                totalEquipped += gm.AutoEquipCharacter(i);

            Refresh();
        }

        private void OnDestroy()
        {
            if (screenManager != null)
                screenManager.OnScreenChanged -= HandleScreenChanged;
            if (optimizeAllButton != null)
                optimizeAllButton.onClick.RemoveAllListeners();
        }
    }
}
```

**Step 2: Update ExploreSceneBuilder**

Replace `CreatePlaceholderPanel("LootPlaceholder", ...)` with:
- LootPanel: full screen panel
- Header bar: inventory count label + Optimize All button
- Item grid: GridLayoutGroup, 100×100 cells, 8px spacing
- Wire to LootScreenController component

**Step 3: Commit**

```
feat: Add LootScreenController with inventory grid and Optimize All
```

---

### Task 7: Item Detail Panel — Sell + Equip to Character

When an item is tapped on the Loot screen, show a detail panel with full stats, sell button, and character picker row ("Who wants this?").

**Files:**
- Create: `Assets/Scripts/UI/ItemDetailPanel.cs`
- Modify: `Assets/Scripts/UI/LootScreenController.cs` (wire item tap → detail panel)
- Modify: `Assets/Editor/ExploreSceneBuilder.cs` (add detail panel to Loot screen)

**Context:**
- Design: tapping item opens detail panel showing stats, rarity, affixes
- "Who wants this?" row: horizontal scroll of party member portraits with net delta badge
- ★ BEST MATCH on character who benefits most
- Sell button with gold value
- Equip button (after selecting a character)
- Uses GameManager.SellItem(), GameManager.EquipItemFromInventory()
- Uses ItemComparer.Compare() for per-character delta
- Use roster.GetActiveParty() for character portraits in picker

**Step 1: Create ItemDetailPanel**

```csharp
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Starquill.Characters;
using Starquill.Core;
using Starquill.Equipment;
using Starquill.Managers;

namespace Starquill.UI
{
    public class ItemDetailPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text itemNameLabel;
        [SerializeField] private TMP_Text itemStatsLabel;
        [SerializeField] private TMP_Text sellValueLabel;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform characterPickerContainer;

        private EquipmentInstance currentItem;

        public event Action OnSold;
        public event Action<EquipmentInstance, int> OnEquipToCharacter;
        public event Action OnClosed;

        public void Initialize()
        {
            if (sellButton != null) sellButton.onClick.AddListener(HandleSell);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            Close();
        }

        public void Show(EquipmentInstance item, int questLevel)
        {
            currentItem = item;
            if (panel != null) panel.SetActive(true);

            var data = ItemDisplayData.FromItem(item, questLevel);

            if (itemNameLabel != null)
                itemNameLabel.text = $"<color=#{ColorUtility.ToHtmlStringRGB(data.RarityColor)}>{data.DisplayName}</color>";

            if (itemStatsLabel != null)
            {
                string stats = $"{data.RarityName} {data.SlotName}\n{data.StatSummary}";
                if (item.RolledAffixes.Count > 0)
                {
                    stats += "\n";
                    foreach (var affix in item.RolledAffixes)
                    {
                        string pct = affix.IsPercentage ? "%" : "";
                        stats += $"\n{affix.StatType} +{affix.Value:F0}{pct}";
                    }
                }
                itemStatsLabel.text = stats;
            }

            if (sellValueLabel != null)
                sellValueLabel.text = $"Sell: {data.SellValue:F0} Gold";

            RefreshCharacterPicker(item);
        }

        public void Close()
        {
            currentItem = null;
            if (panel != null) panel.SetActive(false);
            OnClosed?.Invoke();
        }

        private void RefreshCharacterPicker(EquipmentInstance item)
        {
            if (characterPickerContainer == null) return;

            for (int i = characterPickerContainer.childCount - 1; i >= 0; i--)
                Destroy(characterPickerContainer.GetChild(i).gameObject);

            var gm = GameManager.Instance;
            if (gm == null || gm.Roster == null) return;

            // Find best match across all roster characters
            int bestIdx = -1;
            float bestDelta = float.MinValue;
            for (int i = 0; i < gm.Roster.Characters.Count; i++)
            {
                var c = gm.Roster.Characters[i];
                var equipped = c.equipment[(int)item.Slot];
                var diff = ItemComparer.Compare(item, equipped);
                if (diff.TotalDelta > bestDelta)
                {
                    bestDelta = diff.TotalDelta;
                    bestIdx = i;
                }
            }

            // Show party members first, then others
            var party = gm.Roster.GetActiveParty();
            for (int i = 0; i < gm.Roster.Characters.Count; i++)
            {
                var character = gm.Roster.Characters[i];
                var equipped = character.equipment[(int)item.Slot];
                var diff = ItemComparer.Compare(item, equipped);
                bool isBestMatch = i == bestIdx && bestDelta > 0;

                var card = CreateCharacterPickerCard(character, i, diff, isBestMatch);
                card.transform.SetParent(characterPickerContainer, false);
            }
        }

        private GameObject CreateCharacterPickerCard(CharacterInstance character, int rosterIndex,
            StatDiff diff, bool isBestMatch)
        {
            var card = new GameObject($"Char_{character.displayName}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 80);

            var img = card.GetComponent<Image>();
            img.color = isBestMatch
                ? new Color(0.2f, 0.4f, 0.2f)
                : new Color(0.15f, 0.15f, 0.2f);

            // Name
            var nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.transform.SetParent(card.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.5f);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(4, 0);
            nameRT.offsetMax = new Vector2(-4, -2);
            var nameTmp = nameObj.AddComponent<TMPro.TextMeshProUGUI>();
            string bestTag = isBestMatch ? "\n<color=#FFD700>★ BEST</color>" : "";
            nameTmp.text = $"{character.displayName}{bestTag}";
            nameTmp.fontSize = 11;
            nameTmp.alignment = TMPro.TextAlignmentOptions.Center;
            nameTmp.color = Color.white;
            nameTmp.richText = true;

            // Delta
            var deltaObj = new GameObject("Delta", typeof(RectTransform));
            deltaObj.transform.SetParent(card.transform, false);
            var deltaRT = deltaObj.GetComponent<RectTransform>();
            deltaRT.anchorMin = new Vector2(0, 0);
            deltaRT.anchorMax = new Vector2(1, 0.5f);
            deltaRT.offsetMin = new Vector2(4, 2);
            deltaRT.offsetMax = new Vector2(-4, 0);
            var deltaTmp = deltaObj.AddComponent<TMPro.TextMeshProUGUI>();
            string sign = diff.TotalDelta >= 0 ? "+" : "";
            Color deltaColor = diff.IsUpgrade ? new Color(0.3f, 0.9f, 0.3f)
                : (diff.TotalDelta < 0 ? new Color(0.9f, 0.3f, 0.3f)
                : new Color(0.6f, 0.6f, 0.6f));
            deltaTmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(deltaColor)}>{sign}{diff.TotalDelta:F0}</color>";
            deltaTmp.fontSize = 14;
            deltaTmp.alignment = TMPro.TextAlignmentOptions.Center;
            deltaTmp.richText = true;

            // Tap to equip
            var btn = card.GetComponent<Button>();
            int idx = rosterIndex;
            btn.onClick.AddListener(() => OnEquipToCharacter?.Invoke(currentItem, idx));

            return card;
        }

        private void HandleSell()
        {
            if (currentItem == null) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.SellItem(currentItem);
            Close();
            OnSold?.Invoke();
        }

        private void OnDestroy()
        {
            if (sellButton != null) sellButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        }
    }
}
```

**Step 2: Wire to LootScreenController**

Add `[SerializeField] private ItemDetailPanel itemDetailPanel;` field.
Wire `OnItemTapped += ShowDetail`, `itemDetailPanel.OnSold += Refresh`, `itemDetailPanel.OnEquipToCharacter += HandleEquipToCharacter`, `itemDetailPanel.OnClosed += Refresh`.

**Step 3: Update ExploreSceneBuilder**

Add ItemDetailPanel overlay to Loot screen layout — a panel that covers the bottom ~70% of the Loot screen, starts hidden, contains: item name, stats, sell button, close button, character picker horizontal layout.

**Step 4: Commit**

```
feat: Add ItemDetailPanel with sell, equip-to-character, and comparison
```

---

### Task 8: ExploreSceneBuilder Full Rebuild + Scene Wiring

Rebuild the ExploreSceneBuilder to wire all new components: EquipmentDrawer, LootScreenController, ItemDetailPanel. Ensure all SerializeField references are connected.

**Files:**
- Modify: `Assets/Editor/ExploreSceneBuilder.cs`

**Context:**
- Equipment tab: replace old SlotsContainer GridLayout with spatial cluster, add DrawerPanel
- Loot screen: replace placeholder with full LootPanel (header + grid + detail panel)
- Wire all new components to their SerializeFields using SetPrivateField helper
- Must handle: EquipmentDrawer (drawerPanel, headerLabel, summaryLabel, itemListContainer, equipButton, backButton)
- Must handle: LootScreenController (inventoryCountLabel, itemGridContainer, optimizeAllButton, screenManager)
- Must handle: ItemDetailPanel (panel, itemNameLabel, itemStatsLabel, sellValueLabel, sellButton, closeButton, characterPickerContainer)

**Step 1: Update ExploreSceneBuilder** with complete equipment and loot sections

**Step 2: Run Tools > Build Explore Scene, save scene**

**Step 3: Commit**

```
feat: Rebuild scene with equipment drawer and loot screen layout
```

---

### Task 9: Visual Verification + Integration Test

Play the game, navigate all screens, verify:
1. Equipment tab: spatial slot cluster renders correctly with equipped items
2. Tapping a slot highlights it and opens the drawer (if inventory has items for that slot)
3. Drawer shows compatible items with delta badges and ★ BEST
4. Equip button works — item swaps, drawer closes, character updates
5. Optimize button auto-equips best items
6. Loot screen: inventory grid shows all items with rarity colors
7. Tapping item opens detail panel with stats and sell value
8. Sell button removes item and adds gold
9. Character picker shows delta badges and ★ BEST MATCH
10. Equipping from loot screen works

**Step 1: Rebuild scene via MCP execute_script**
**Step 2: Save scene via MCP**
**Step 3: Play game, capture screenshots of Equipment tab and Loot screen**
**Step 4: Fix any visual issues**
**Step 5: Final commit with scene file**

```
feat: Sprint 8 complete — Equipment UI + Loot Screen
```
