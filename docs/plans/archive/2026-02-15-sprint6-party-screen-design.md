# Sprint 6: Party Screen — Design Document

## Overview

The Party screen is a character-focused screen where players inspect, manage, and level up individual characters. It features a large paper doll display of the selected character, with three sub-tabs (Roster, Equipment, Actions) for different management tasks. Navigation uses a panel-swap system on the existing Canvas — combat continues running in the background.

## Screen Navigation System

The Explore screen's existing hierarchy gets wrapped in an `ExplorePanel` parent. A new `PartyPanel` sibling sits alongside it under the Canvas. A `ScreenManager` component on the Canvas holds references to all screen panels and exposes `ShowScreen(int index)`.

`BottomNavDisplay` calls `ScreenManager` on tab tap. For Sprint 6, two panels are functional:
- Index 0: Explore
- Index 3: Party

Indexes 1 (Quests), 2 (Loot), 4 (Shop) do nothing or show a placeholder.

**Background combat:** `GameManager` continues ticking when the Party screen is visible. `ExploreSceneController` mutes damage number spawning and verb card animations while `ExplorePanel` is hidden to avoid wasting resources on invisible VFX. When switching back to Explore, the event-driven UI catches up instantly.

**Top bar and bottom nav** remain always-visible; only the middle content swaps.

## Party Screen Layout (1080x1920 Portrait)

### Persistent Header Strip (~80px)

Always visible across all three sub-tabs. Shows:
- Character name (e.g., "Thorgrim")
- Species (e.g., "Dwarf")
- Level (e.g., "Lv 22")
- XP progress bar or text (e.g., "245/500 XP")
- **Level-up button** — always present, glows when eligible, disabled/dimmed when not enough XP

### Character Focus Area (~670px)

The hero display for the selected character:

- **Center: Full-body paper doll** — large RawImage (~400x600) using the existing `CharacterDisplay` + `DisplayBuilder` pipeline, rendered bigger than the Explore screen slots
- **Left edge: Party indicator strip** — 4 small circular head-cropped portraits (~100px diameter) stacked vertically showing active party members, with name and level on each. The portrait matching the selected character gets a gold highlight ring. If the selected character is on the bench, none are highlighted. Tapping a portrait switches focus to that party member.
- **Right side: Character info** — primary stat highlight, species ability (placeholder for now). "Add to Party" / "Remove from Party" / "Swap" button for party management.

**Stat bar** below the paper doll — compact horizontal strip showing all 6 stats. Each displayed as abbreviation + total value with equipment bonus in a different color (e.g., "STR 12+3" where +3 is green). Color-coded per stat type using existing `StatTypeColors`.

### Sub-Tab Bar (~80px)

Three tabs: **Roster** | **Equipment** | **Actions**

### Bottom Content Area (~750px, scrollable)

Content changes based on active sub-tab.

## Sub-Tab: Roster (Default)

Scrollable grid of character cards for all characters in the `CharacterRoster` (up to 20).

**Character cards** (~160x200 each):
- Head-cropped portrait (using per-species head crop data)
- Character name
- Character level
- **Party badge** — active party members show a numbered shield icon (1, 2, 3, 4) matching their party slot. Bench characters have no badge.

**Interactions:**
- **Tap a card** — that character becomes the selected/focused character. The Focus Area, stat bar, and header all update.
- **Party management** is done via the button in the Focus Area (right side):
  - Bench character + party not full → "Add to Party"
  - Party member → "Remove from Party"
  - Bench character + party full → "Swap" (picker to choose which party member to replace)

## Sub-Tab: Equipment

When active, the Focus Area transforms:
- **Party portrait strip (left) and character info (right) hide**
- Equipment slots appear **overlaid around the paper doll**:

```
              [Head]
   [Arms]    (paper     [Misc1]
   [Torso]    doll)     [Misc2]
   [Legs]               [Misc3]
   [Feet]               [Misc4]
        [Main] [Off]
```

- Left side: body-worn gear (Head, Arms, Torso, Legs, Feet) top to bottom
- Right side: 4 Misc slots stacked
- Below feet: Main Hand and Off Hand side by side

Each slot is a compact button (~90x90) with:
- Rarity-colored border (white/green/blue/purple/orange)
- Item type icon or label
- Empty slots show a dimmed silhouette icon for that slot type

**Stat bar remains visible** below the equipment layout.

**Persistent header** (name/level/XP/level-up) remains visible above.

**Placeholder interactions (scaffolding built now, connected after Loot screen sprint):**
- Tapping a slot → opens a **popup modal** frame (empty for now) designed for future item compare/swap
- **"Auto-Equip" button** — visible below equipment layout, disabled/dimmed for Sprint 6

## Sub-Tab: Actions

Focus Area stays in default layout (paper doll center, party portraits left, character info right).

**Top of content area: Equipped Action Slots**
- Horizontal row matching character's verb slot count (2-5 based on level)
- Filled slots: action card with stat type color-coded background, name
- Empty slots: "+" placeholder with dimmed outline
- Locked slots: lock icon with unlock level (e.g., "Lv 26")

**Below: Available Actions**
- Scrollable list of this character's **unlocked but unequipped** actions
- Cards show: action name, stat type color, base damage, target mode (single/cleave/AoE)
- Actions are per-character (not a shared inventory pool)
- New actions unlock as the character levels up

**Interactions (functional in Sprint 6):**
- Tap available action → equips to first empty slot
- Tap equipped action → unequips back to available pool
- All slots full + tap available → prompts which equipped action to swap out

## Level-Up System

Characters accumulate XP from combat kills. Level-up requires the player to visit the Party screen and tap the glowing level-up button.

### XP Curve

```
xpToNextLevel = baseXP * (1 + xpGrowthRate) ^ currentLevel

baseXP = 100
xpGrowthRate = 0.18
```

| Level | XP Required | Cumulative |
|-------|------------|------------|
| 1->2 | 118 | 118 |
| 5->6 | 229 | 870 |
| 10->11 | 523 | 3,200 |
| 25->26 | 6,600 | 48,000 |
| 50->51 | 435K | 2.2M |

### On Level-Up

1. Level increments
2. Stats auto-distribute proportionally to species base stat ratios. Example: species with STR 8, DEX 5, CON 7, INT 3, WIS 4, CHA 3 (30 total) distributes ~3 stat points per level weighted by ratios. Fractional points accumulate internally and apply when they cross whole numbers.
3. Brief animation shows stat gains ("+1 STR, +1 CON" floating text)
4. Action unlock check — if level crosses an unlock threshold, notification appears ("New Action unlocked: Shield Bash!")
5. Verb slot count updates if threshold crossed (3 at lv11, 4 at lv26, 5 at lv51)
6. If still enough XP for another level, button keeps glowing (multi-level catchup)

### Action Unlock Schedule

Data-driven — not limited to species. For Sprint 6, a simple placeholder unlock table maps levels to verb IDs per character. The real unlock system (which may factor in stats, equipment, or other criteria) is implemented later.

`CharacterInstance` gains an `unlockedVerbs` list (all actions the character has unlocked) separate from `equippedVerbs` (the subset slotted for combat). Level-up checks the unlock table and adds newly available actions to `unlockedVerbs`.

## Head Crop Portraits

Per-species head crop data added to `species.json`:
- `head_y_offset` — vertical offset to center the camera on the head region
- `head_zoom` — orthographic size adjustment for framing

A `CharacterPortraitRenderer` component uses these values to render a head-framed portrait into a smaller RenderTexture for use in:
- Party indicator strip (4 portraits on Focus Area left side)
- Roster grid character cards

26 species entries need manual tuning of these values.

## Technical Architecture

### New Scripts

| Script | Assembly | Purpose |
|--------|----------|---------|
| `ScreenManager.cs` | UI | Panel show/hide router, holds references to all screen panels |
| `PartyScreenController.cs` | UI | Main controller — manages sub-tabs, selected character, binds data to UI |
| `CharacterFocusDisplay.cs` | UI | Renders large paper doll + persistent header (name/level/XP/level-up) |
| `PartyPortraitStrip.cs` | UI | 4 small party member head portraits with selection highlight |
| `RosterGridDisplay.cs` | UI | Scrollable grid of all roster characters with party badges |
| `EquipmentSlotsDisplay.cs` | UI | 11 equipment slots arranged around paper doll |
| `ActionLoadoutDisplay.cs` | UI | Equipped action slots + available actions list |
| `CharacterPortraitRenderer.cs` | Display | Renders head-cropped portraits using species head_crop data |

### Modified Scripts

| Script | Changes |
|--------|---------|
| `BottomNavDisplay.cs` | Wire tab taps to `ScreenManager.ShowScreen()` |
| `ExploreSceneController.cs` | Mute damage numbers and verb animations when ExplorePanel hidden |
| `CharacterInstance.cs` | Add `unlockedVerbs`, `LevelUp()` with species-weighted stat distribution, XP curve |
| `ExploreSceneBuilder.cs` | Wrap existing hierarchy in `ExplorePanel`, generate `PartyPanel` hierarchy |

### Modified Data

| File | Changes |
|------|---------|
| `species.json` | Add `head_y_offset` and `head_zoom` per species (26 entries) |

### Data Flow

1. `ScreenManager` shows `PartyPanel`, hides `ExplorePanel`
2. `PartyScreenController` reads `GameManager.Roster` for character data
3. Selecting a character updates `CharacterFocusDisplay` and refreshes active sub-tab
4. Party changes (add/remove/swap) call `CharacterRoster` methods, then refresh UI
5. Level-up calls `CharacterInstance.LevelUp()`, refreshes stats, checks action unlocks
6. Action equip/unequip calls `CharacterInstance` methods, refreshes combat verb pool

### Testing Strategy

| Area | Tests |
|------|-------|
| `ScreenManager` | Panel visibility toggling, correct panel shown per index |
| `CharacterInstance.LevelUp()` | XP curve calculation, species-weighted stat distribution, fractional accumulation, verb slot unlocks |
| `CharacterInstance` actions | Equip/unequip verbs, unlocked vs equipped separation, slot count limits |
| `CharacterPortraitRenderer` | Head crop offset application per species |
| `PartyScreenController` | Character selection updates, sub-tab switching, party swap logic |
| `RosterGridDisplay` | Party badge assignment, character card data binding |

## Scope Boundaries

**In Sprint 6:**
- Screen navigation (panel swap)
- Party screen with all three sub-tabs
- Character focus display with full paper doll
- Head-cropped portraits for party strip and roster
- Roster browsing and party member swapping
- Equipment display (read-only, placeholder modals for future)
- Action equip/unequip (functional)
- Level-up with species-weighted auto-distribution
- Combat muting when Explore panel hidden

**Deferred:**
- Equipment equip/swap/compare (after Loot screen sprint)
- Auto-equip functionality (after Loot screen sprint)
- Real action unlock data (placeholder table for now)
- Talents system
- Coverage indicator
- Species ability display (placeholder)
