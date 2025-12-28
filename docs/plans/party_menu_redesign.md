# Party Menu Redesign Implementation Plan

## Overview
Redesign the party menu UI based on user mockups to improve layout, usability, and mobile-friendliness.

## Target Layout

### Header
```
[X]  Party Menu
```
- X close button on top-left
- Title "Party Menu" left-aligned after button

### Left Panel - Character Panel
```
+------------------------------------------+
|  +------------------+    [Head]          |
|  |                  |    [Torso]         |
|  |   Character      |    [Arms]          |
|  |   Portrait       |    [Legs]          |
|  |   (DisplayBuilder)|   [Feet]          |
|  +------------------+                    |
+------------------------------------------+
|        [Main Hand]  [Off Hand]           |
+------------------------------------------+
|   [Misc1] [Misc2] [Misc3] [Misc4]        |
+------------------------------------------+
|  [<][>]   Character Name                 |
+------------------------------------------+
```

### Right Panel - Tabbed Content
```
+------------------------------------------+
| [Inventory] [Stats] [Journal] [Map]      |  <- fills width evenly
+------------------------------------------+
| +--------------------------------------+ |
| |  Inset/recessed scroll container     | |
| |  [slot][slot][slot][slot][slot]...   | |
| |  [slot][slot][slot][slot][slot]...   | |
| |  ... dynamic rows based on items     | |
| |  ... + 2 empty rows always           | |
| +--------------------------------------+ |
| +--------------------------------------+ |
| |  Item Info Panel (fills remaining)   | |
| +--------------------------------------+ |
+------------------------------------------+
```

---

## Implementation Tasks

### Task 1: Analyze DisplayBuilder Integration
**Files to analyze:**
- `scripts/display/display_builder.gd` - How character rendering works
- `scripts/character/character.gd` - equipment_changed signal
- `scripts/ui/panels/character_panel.gd` - Current implementation

**Goal:** Understand how to embed DisplayBuilder output in the portrait area and ensure it re-renders on equipment changes.

---

### Task 2: Restructure Party Menu Header
**File:** `scripts/ui/screens/party_menu.gd`

**Changes:**
- Move close button from top-right to top-left
- Change title from "Party" to "Party Menu"
- Adjust header HBox to: [CloseButton] [Title with SIZE_EXPAND_FILL]

**Verification:** Run game, open party menu, verify X is top-left and title shows "Party Menu"

---

### Task 3: Restructure Character Panel Layout
**File:** `scripts/ui/panels/character_panel.gd`

**Changes:**
1. Remove current top header with arrows/name
2. Create new layout structure:
   - Main HBox: Portrait (left) + Body equipment column (right)
   - Below: Weapon row (Main Hand, Off Hand)
   - Below: Misc row (Misc1-4)
   - Bottom: Navigation HBox with [<][>] buttons left, name label right

**Verification:** Run game, verify layout matches mockup structure

---

### Task 4: Implement Character Portrait with DisplayBuilder
**File:** `scripts/ui/panels/character_panel.gd`

**Changes:**
1. Create SubViewport + TextureRect for portrait display
2. Integrate DisplayBuilder to render character
3. Connect to character's equipment_changed signal for re-rendering
4. Size portrait to fill available vertical space (ratio matching standard character displays)

**Verification:** Run game, verify character sprite renders in portrait, change equipment slot, verify portrait updates

---

### Task 5: Link Character Data to Display
**File:** `scripts/ui/panels/character_panel.gd`

**Changes:**
1. Get character from PlayerData.party.members[index]
2. Display actual character name from character.display_name
3. Update portrait when character index changes
4. Update all equipment slots from character data

**Verification:** Run game with test party, verify names and portraits show actual party members

---

### Task 6: Widen Tab Bar to Fill Width
**File:** `scripts/ui/components/bookmark_tab_bar.gd`

**Changes:**
1. Set tab buttons to SIZE_EXPAND_FILL with equal stretch ratios
2. Ensure full text displays: "Inventory", "Stats", "Journal", "Map"
3. Tab container should fill parent width

**Verification:** Run game, verify tabs span full width evenly, all text visible

---

### Task 7: Create Inset Scroll Container Style
**File:** `scripts/ui/panels/inventory_panel.gd` or new style in `autoload/ui_theme_manager.gd`

**Changes:**
1. Create dark/recessed StyleBoxFlat for scroll container background
2. Apply inset border effect (darker edges, slightly lighter center)
3. Configure ScrollContainer to always show vertical scrollbar

**Verification:** Run game, verify inventory area has recessed/inset appearance with visible scrollbar

---

### Task 8: Implement Dynamic Inventory Slots
**File:** `scripts/ui/panels/inventory_panel.gd`

**Changes:**
1. Calculate available grid space based on container size
2. Determine how many slots fit (columns x rows)
3. Count actual inventory items
4. Display: item slots + 2 extra empty rows minimum
5. If items exceed visible area, scroll container enables scrolling
6. Listen for resize events to recalculate

**Verification:** Run game at different resolutions, verify slot count adjusts, add items, verify 2 empty rows always visible below last item

---

### Task 9: Make Item Info Panel Fill Remaining Space
**File:** `scripts/ui/panels/inventory_panel.gd`

**Changes:**
1. Remove fixed minimum height from info panel
2. Set SIZE_EXPAND_FILL for vertical sizing
3. Ensure it takes all remaining space after scroll container

**Verification:** Run game, verify info panel expands to fill bottom area

---

### Task 10: Final Integration Testing
**Verification steps:**
1. Open party menu from world map (button or P key)
2. Verify header: X top-left, "Party Menu" title
3. Verify character panel: large portrait, equipment slots in correct positions
4. Verify character switching with arrows updates portrait and name
5. Verify tabs fill width with full text
6. Verify inventory has inset style with always-visible scrollbar
7. Verify dynamic slot count and 2 empty rows rule
8. Verify item info panel fills remaining space
9. Test on different window sizes

---

## Files to Modify

1. `scripts/ui/screens/party_menu.gd` - Header restructure
2. `scripts/ui/panels/character_panel.gd` - Major layout restructure + DisplayBuilder
3. `scripts/ui/components/bookmark_tab_bar.gd` - Tab width changes
4. `scripts/ui/panels/inventory_panel.gd` - Scroll container styling + dynamic slots
5. `autoload/ui_theme_manager.gd` - Possibly add inset style helper

## Dependencies to Analyze First

- `scripts/display/display_builder.gd` - Character rendering
- `scripts/character/character.gd` - Equipment signals
- `autoload/player_data.gd` - Party data access
