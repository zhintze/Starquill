# Equipment Card List — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the spatial slot cluster with a scrollable vertical card list grouped by category, and guarantee starter equipment for new characters.

**Architecture:** Rewrite `EquipmentSlotsDisplay` from absolute-positioned tiles to a VerticalLayoutGroup with category headers and 140px equipment cards. Each card shows slot label, item name, rarity stripe, and equipment sprite. Add `CreateStarterLoadout` to `EquipmentFactory` to fill all 11 slots. Seed `LootInventory` with test items on first play.

**Tech Stack:** Unity UI (ScrollRect, VerticalLayoutGroup, LayoutElement), TextMeshPro, Resources.Load<Sprite> for equipment sprites, ImageToken for path generation.

---

## Context

**Design doc:** `docs/starquill-equipment-change-of-direction.md` (user's local file)

**Key files:**
- `Assets/Scripts/UI/EquipmentSlotsDisplay.cs` — full rewrite target
- `Assets/Scripts/Equipment/EquipmentFactory.cs` — add CreateStarterLoadout
- `Assets/Scripts/Managers/GameManager.cs` — seed inventory on first play
- `Assets/Editor/ExploreSceneBuilder.cs` — rebuild Equipment tab layout
- `Assets/Scripts/Display/ImageToken.cs` — sprite path generation (already has BuildEquipmentSpritePath)
- `Assets/Scripts/UI/ItemDisplayData.cs` — rarity colors, slot names

**Slot enum (11 slots):**
```
0=Head, 1=Torso, 2=Arms, 3=Legs, 4=Feet,
5=MainHand, 6=OffHand, 7=Misc1, 8=Misc2, 9=Misc3, 10=Misc4
```

**Category grouping for card list:**
- Armor: Head(0), Torso(1), Arms(2), Legs(3), Feet(4)
- Weapons: MainHand(5), OffHand(6)
- Accessories: Misc1(7), Misc2(8), Misc3(9), Misc4(10)

---

### Task 1: CreateStarterLoadout + seed inventory

**Files:**
- Modify: `Assets/Scripts/Equipment/EquipmentFactory.cs`
- Modify: `Assets/Scripts/Managers/GameManager.cs`
- Test: `Assets/Tests/EditMode/Equipment/EquipmentFactoryTests.cs`

**Step 1: Write the failing test**

Add test to existing `EquipmentFactoryTests.cs`:

```csharp
[Test]
public void CreateStarterLoadout_FillsAllCoreSlots()
{
    var loadout = factory.CreateStarterLoadout(new System.Random(42));
    // Armor slots always filled
    Assert.IsNotNull(loadout[(int)EquipmentSlot.Head]);
    Assert.IsNotNull(loadout[(int)EquipmentSlot.Torso]);
    Assert.IsNotNull(loadout[(int)EquipmentSlot.Arms]);
    Assert.IsNotNull(loadout[(int)EquipmentSlot.Legs]);
    Assert.IsNotNull(loadout[(int)EquipmentSlot.Feet]);
    // Weapon always filled
    Assert.IsNotNull(loadout[(int)EquipmentSlot.MainHand]);
    // At least one misc
    int miscCount = 0;
    for (int i = 7; i <= 10; i++)
        if (loadout[i] != null) miscCount++;
    Assert.GreaterOrEqual(miscCount, 1);
}

[Test]
public void CreateStarterLoadout_AllCommonRarity()
{
    var loadout = factory.CreateStarterLoadout(new System.Random(42));
    for (int i = 0; i < loadout.Length; i++)
    {
        if (loadout[i] != null)
            Assert.AreEqual(Rarity.Common, loadout[i].Rarity, $"Slot {i} should be Common");
    }
}
```

**Step 2: Implement CreateStarterLoadout**

Add to `EquipmentFactory.cs`:

```csharp
public EquipmentInstance[] CreateStarterLoadout(System.Random rng)
{
    var loadout = new EquipmentInstance[11];

    // All armor slots filled with Common
    loadout[(int)EquipmentSlot.Head] = CreateRandom("hd", Rarity.Common, rng);
    loadout[(int)EquipmentSlot.Torso] = CreateRandom("tr", Rarity.Common, rng);
    loadout[(int)EquipmentSlot.Arms] = CreateRandom("ar", Rarity.Common, rng);
    loadout[(int)EquipmentSlot.Legs] = CreateRandom("lg", Rarity.Common, rng);
    loadout[(int)EquipmentSlot.Feet] = CreateRandom("fe", Rarity.Common, rng);

    // One-handed weapon + offhand
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
```

**Step 3: Wire starter loadout in CharacterFactory**

In `CharacterFactory.cs`, change `CreateRandom` to use `CreateStarterLoadout` when `level == 1`:
- Replace `equipFactory.CreateRandomLoadout(questLevel, rng)` with `equipFactory.CreateStarterLoadout(rng)` when `questLevel <= 1`

**Step 4: Seed inventory on first play**

In `GameManager.InitializeStarterRoster()`, after creating roster, add 5 random items to `lootInventory`:

```csharp
// Seed inventory with test items
for (int i = 0; i < 5; i++)
{
    var rng = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
    var rarity = (i < 3) ? Rarity.Uncommon : Rarity.Rare;
    var item = equipmentFactory.CreateRandom(
        new[] { "hd", "tr", "ar", "lg", "fe", "mc" }[i % 6], rarity, rng);
    if (item != null) lootInventory.AddItem(item);
}
```

**Step 5: Run tests, commit**

```
git add Assets/Scripts/Equipment/EquipmentFactory.cs Assets/Scripts/Characters/CharacterFactory.cs Assets/Scripts/Managers/GameManager.cs Assets/Tests/EditMode/Equipment/EquipmentFactoryTests.cs
git commit -m "Add CreateStarterLoadout and seed inventory for new characters"
```

---

### Task 2: Rewrite EquipmentSlotsDisplay as card list

**Files:**
- Rewrite: `Assets/Scripts/UI/EquipmentSlotsDisplay.cs`

**This is the main UI rewrite.** Replace the spatial cluster with a scrollable vertical card list grouped by category.

**Public interface stays the same:**
- `Initialize()` — wire auto-equip button
- `SelectSlot(int)` / `ClearSelection()` — highlight management
- `Refresh(CharacterInstance)` — rebuild card list
- Events: `OnSlotTapped(int)`, `OnOptimizeTapped()`

**New addition:** `SetInventory(LootInventory)` — needed to check for upgrade pips.

**New SerializeFields:**
```csharp
[SerializeField] private ScrollRect scrollRect;        // replaces slotsContainer
[SerializeField] private Transform cardContainer;      // VerticalLayoutGroup content
[SerializeField] private Button autoEquipButton;       // stays the same
```

**Category headers:** Simple 40px gray bars with white text ("ARMOR", "WEAPONS", "ACCESSORIES").

**Equipment cards (140px tall):**
```
┌─[6px rarity stripe]────────────────────────────────┐
│  Slot Label (gray, 14px)         ┌──────────────┐  │  140px
│  Item Name (white, 18px bold)    │  [Equipment   │  │
│  Rarity · Lv X (small, colored)  │   Sprite]     │  │
│                         [●]      └──────────────┘  │  ← upgrade pip
└─────────────────────────────────────────────────────┘
```

**Card layout details:**
- Full width, 140px height, LayoutElement preferredHeight=140, flexibleWidth=1
- Left rarity stripe: 6px wide, anchored left edge, full height
- Text area: left 60%, anchored (0,0)→(0.6,1), padded 16px left, 8px top/bottom
  - Slot label: top area, gray (0.5, 0.5, 0.55), fontSize 14
  - Item name: middle, white, fontSize 18, bold
  - Rarity + level: bottom area, rarity color, fontSize 13
- Sprite area: right 35%, anchored (0.65,0.05)→(0.95,0.95)
  - RawImage or Image showing equipment sprite (primary layer)
  - Tinted with item's base color
- Upgrade pip: 12px green circle, bottom-right of text area, only shown if better item exists
- Selected state: lighter background + gold left border instead of rarity stripe

**Empty slot card:**
- Same 140px height
- Dark background (0.12, 0.12, 0.15, 0.5)
- Slot label, "Empty" in italic gray, no sprite

**Slot ordering within categories:**
```csharp
private static readonly int[][] CategorySlots = {
    new[] { 0, 1, 2, 3, 4 },    // Armor: Head, Torso, Arms, Legs, Feet
    new[] { 5, 6 },              // Weapons: MainHand, OffHand
    new[] { 7, 8, 9, 10 },       // Accessories: Misc1-4
};
private static readonly string[] CategoryNames = { "ARMOR", "WEAPONS", "ACCESSORIES" };
```

**Equipment sprite loading:**
- For each equipped item, load the sprite for its first layer code via `ImageToken.BuildEquipmentSpritePath(item.ItemType, item.ItemNum, item.LayerCodes[0])`
- Use `Resources.Load<Sprite>(path)` and display in a UI Image component
- Tint with `item.BaseColor`
- For weapons, use `ImageToken.BuildWeaponSpritePath(item.ItemType, item.LayerCodes[0], item.ItemNum)`

**Step 1: Write the full replacement**

Complete rewrite of `EquipmentSlotsDisplay.cs` with the card list pattern described above.

**Step 2: Verify compilation**

**Step 3: Commit**

```
git add Assets/Scripts/UI/EquipmentSlotsDisplay.cs
git commit -m "Rewrite EquipmentSlotsDisplay as scrollable card list with categories"
```

---

### Task 3: Update ExploreSceneBuilder for card list layout

**Files:**
- Modify: `Assets/Editor/ExploreSceneBuilder.cs`

**Changes to Equipment tab section:**

Replace the spatial cluster container with a ScrollRect + VerticalLayoutGroup:

1. **Remove** the old `equipSlotsContainer` (was positioned at center for spatial grid)
2. **Create** `equipScrollRect` — full-width ScrollRect below the mirror
   - Anchored to fill equipment content area below character display
   - Vertical scrolling only, no horizontal
   - Scroll sensitivity ~30
3. **Create** `equipCardContainer` inside scroll viewport
   - VerticalLayoutGroup: spacing=4, padding=8, childAlignment=upperCenter
   - ContentSizeFitter: verticalFit=PreferredSize
   - childForceExpandWidth=true, childForceExpandHeight=false
4. **Wire** EquipmentSlotsDisplay SerializeFields:
   - `scrollRect` → the ScrollRect component
   - `cardContainer` → the equipCardContainer transform
   - `autoEquipButton` → existing optimize button (keep where it is)

**Step 1: Edit the Equipment tab section in ExploreSceneBuilder**

Replace the spatial slots container creation with ScrollRect + VerticalLayoutGroup setup.

**Step 2: Rebuild scene via MCP execute_script, save**

**Step 3: Commit**

```
git add Assets/Editor/ExploreSceneBuilder.cs Assets/Scenes/ExploreScene.unity
git commit -m "Rebuild Equipment tab with scrollable card list layout"
```

---

### Task 4: Wire upgrade pips

**Files:**
- Modify: `Assets/Scripts/UI/EquipmentSlotsDisplay.cs`
- Modify: `Assets/Scripts/UI/PartyScreenController.cs`

**Step 1: Add inventory awareness**

In `EquipmentSlotsDisplay`, add:
```csharp
private LootInventory inventory;

public void SetInventory(LootInventory inv) { inventory = inv; }
```

In card creation, check for upgrade:
```csharp
bool hasUpgrade = false;
if (inventory != null && item != null)
{
    var best = ItemComparer.FindBestForSlot((EquipmentSlot)slotIndex, inventory.Items, item);
    hasUpgrade = best != null;
}
```

If `hasUpgrade`, add a 12px green circle (Image with circular sprite or just a colored square) at bottom-right of text area.

**Step 2: Wire in PartyScreenController**

In `PartyScreenController.Start()`, after `equipmentSlots.Initialize()`:
```csharp
var gm = GameManager.Instance;
if (equipmentSlots != null && gm != null)
    equipmentSlots.SetInventory(gm.LootInventory);
```

**Step 3: Commit**

```
git add Assets/Scripts/UI/EquipmentSlotsDisplay.cs Assets/Scripts/UI/PartyScreenController.cs
git commit -m "Add upgrade pip indicators to equipment cards"
```

---

### Task 5: Delete old save + visual verification + final commit

**Step 1: Clear PlayerPrefs** (to test starter loadout)

Add temporary debug button or use Unity editor to clear PlayerPrefs, OR modify `GameManager.Start()` temporarily to force `InitializeStarterRoster()`.

Alternatively, the implementer can clear PlayerPrefs from the editor menu (Edit > Clear All PlayerPrefs) before testing.

**Step 2: Play game, navigate to Party > Equipment tab**

Verify:
- All characters have equipment in core slots (head, chest, arms, legs, feet, weapon)
- Equipment cards render with category headers (ARMOR, WEAPONS, ACCESSORIES)
- Cards show slot label, item name, rarity stripe, equipment sprite
- Scrolling works smoothly
- Tapping a card opens the equipment drawer
- Upgrade pips appear on slots where inventory has better items
- Optimize button works
- Loot tab shows seeded inventory items

**Step 3: Capture screenshots for each screen**

**Step 4: Final commit with scene file if needed**
