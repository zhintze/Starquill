# Starquill Equipment UI Design — "Changing Room with a Scoreboard"

## Design Philosophy

The dual motivation framing (cosmetic vs. stats) drives the entire layout. The character preview is the **hero element** of the Equipment tab — not tucked away or implied, but taking up real screen estate. Every interaction on this screen visibly changes the character in real-time *before* you commit, so players get that dopamine hit of "oh that looks cool" alongside "oh that's a +12 ATK upgrade."

---

## Screen Layout: Equipment Tab (1080×1920 Portrait)

The screen is divided into four horizontal bands:

```
┌──────────────────────────┐
│  Header Bar (~80px)      │  Character name, class, ◀ ▶ party swap arrows
├──────────────────────────┤
│                          │
│  The Mirror (~700px)     │  Full character render, live preview area
│                          │
│                          │
├──────────────────────────┤
│                          │
│  Slot Cluster (~520px)   │  The spatial grid:
│       [Head]             │
│  [Main][Chest][Misc1]    │       ~120×120 tiles
│  [Off] [Legs] [Misc2]   │       + "Optimize" button below
│       [Shoes][Misc3]     │
│              [Misc4]     │
│                          │
│  [Optimize]              │
├──────────────────────────┤
│  Drawer Bar (~120px)     │  "3 upgrades available · 42/50"
│  ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │  (collapsed state, tap slot to expand)
└──────────────────────────┘
```

Total: 80 + 700 + 520 + 120 = 1420px, leaving ~500px for padding and margins.

### Header Bar (~80px)

Character name, species/class, and left/right arrows (or swipe) to cycle through party members without leaving the Equipment tab.

### The Mirror (~700px)

Full character render, centered, showing all currently equipped gear visually. This is the **live preview** — when a player is browsing an item, the character *temporarily morphs* to show how they'd look wearing it. A subtle shimmer or ghost outline of the old appearance lingers so you can see the before/after simultaneously (like a translucent "ghost" of the previous look slightly offset behind the new one).

### The Slot Cluster (~520px)

A spatial layout that hints at body position — your eye reads top-to-bottom as head-to-toe, weapons on the left (hands), accessories on the right. No silhouette or labels needed; the layout *is* the paper doll.

```
        [Head]
  [Main][Chest][Misc1]
  [Off] [Legs] [Misc2]
        [Shoes][Misc3]
               [Misc4]
```

- Tiles are ~120×120 with the equipment sprite rendered inside.
- Empty slots show a dim silhouette icon (helmet shape, sword shape, etc.) so players understand what goes there.
- The **currently tapped slot** gets a glowing highlight ring.
- If an item is a "best in slot" candidate from inventory, the slot tile gets a subtle **pulsing green pip** in the corner — a passive nudge that says "you have an upgrade available" without requiring any interaction.
- **"Optimize"** button sits below the grid — runs AutoEquipper for the current character.

### The Drawer (~120px collapsed)

Context-sensitive content lives here. By default it's collapsed to a thin bar showing a summary line:

> `"3 upgrades available · 42/50 inventory"`

Tapping a slot slides it up as a **bottom sheet** expanding to ~1150px (~60% of screen), covering the slot cluster and most of the mirror — but the top ~770px still peeks through, so the character's head/torso stays visible during previews. The player sees the gear change on the character while browsing items in the drawer.

---

## The Drawer: Where Everything Happens

When you tap a slot, the drawer slides up and shows:

### Drawer Header

The **currently equipped item** displayed as a horizontal card: sprite on the left, name + rarity color + key stats on the right. If the slot is empty, this shows "Empty Slot — Equip something!" with a gentle prompt.

### Drawer Body: Compatible Inventory Items

A vertical scrollable list of every item in LootInventory that fits this slot. Each row is a card showing:

- **Item sprite** (left)
- **Item name + rarity** (center)
- **Delta badges** (right): Big, chunky stat change indicators. Not tiny arrows — pill-shaped badges like `+12 ATK` in green or `-3 DEF` in red. The *net score change* shows as a larger badge at the far right: `▲ +8` in green or `▼ -4` in red. These should be readable at arm's length.

### Sorting & Filtering Toolbar

A small toolbar at the top of the list with toggle chips:

- **Best First** (default) — sorted by net score improvement, descending
- **Newest** — most recently dropped items first
- **Rarity** — legendary down to common
- A `★ BEST` badge on the single best-scoring item, so it's always visually obvious regardless of sort order

### The Preview Mechanic

**Tapping an item in the drawer doesn't equip it.** It *previews* it.

1. The character in the top band live-updates to show the new look.
2. The drawer header splits into a **side-by-side comparison**: equipped item on the left, previewed item on the right, with stat deltas between them in a clear column (green up arrows, red down arrows, gray dashes for unchanged). This comparison card is the "scoreboard."

Two buttons appear at the bottom of the drawer:

- **"Equip"** (primary, large, green-tinted) — commits the swap. The old item returns to inventory. A quick satisfying animation plays on the character (flash/sparkle).
- **"Back"** (secondary, smaller) — reverts preview, returns to the item list.

This means equipping a known upgrade is exactly **2 taps**: tap the slot → tap the item with the big green `★ BEST` badge. Done.

---

## Loot Screen Flow (Entry Point B)

When a player comes at it from the inventory side (they got a new drop and want to deal with it):

**Tapping an item on the Loot screen** opens a detail panel for that item. Below the stats, show a **"Who wants this?"** section: a horizontal row of character portraits (party members first, highlighted, then a scrollable extension for the full roster). Each portrait shows a **net delta badge** overlay — the score change if this item were equipped on that character. The character who benefits most gets the `★ BEST MATCH` badge and is sorted first.

Tapping a character portrait does two things simultaneously:
1. Shows a quick comparison (current vs. new)
2. Live-previews the item on that character

Then the same "Equip" / "Back" buttons appear.

From the Loot screen, equipping the optimal item is also **2 taps**: tap the item → tap the portrait with the green `★ BEST MATCH` badge.

---

## Auto-Equip: Prominent but Not Pushy

A hybrid approach — per-character and party-wide:

### Per-Character "Optimize" (Equipment Tab)

Sits in the middle band near the slot grid, styled as a secondary button (not the loudest thing on screen, but clearly there). Tapping it runs AutoEquipper for *that character*, and the character preview plays a quick dress-up animation as items shuffle on. A results toast shows:

> `"Equipped 3 upgrades · +47 total score"`

### Party-Wide "Optimize All" (Loot Screen)

Since the Loot screen is the inventory management hub, a top-bar button runs AutoEquipper across the whole party. This is the "I just want to clear my inventory and move on" button. Shows a summary:

> `"Optimized 4 characters · 12 items equipped · +183 total score"`

Auto-equip feels like a shortcut, not the intended experience. The manual flow with live preview is the *fun* path. Auto-equip is the *efficient* path for when you have 30 items and don't want to think.

---

## Design Decisions (Answers to Flow Questions)

### 1. Where does comparison happen?

**Bottom sheet overlay (Option C)**, designed so the character preview remains visible above it. The drawer IS the comparison space. You never leave the Equipment tab.

### 2. Where does "equip to character" happen?

**Hybrid of Options A + C.** From the Loot screen, show a character picker row with smart `★ BEST MATCH` sorting. The selected character context persists — if you were just looking at Character 2 on the Equipment tab and switch to Loot, arrows default to comparing against Character 2.

### 3. How prominent is Auto-Equip?

**Per-character "Optimize" on Equipment tab, party-wide "Optimize All" on Loot screen.** Visible, useful, but not the hero interaction.

### 4. How do green/red arrows work?

**Context-dependent:**
- On the Equipment tab drawer, arrows compare against that character (obvious — you're looking at them).
- On the Loot screen, arrows compare against the **best party match** by default, with the matching character's portrait shown next to the arrow so there's no ambiguity.
- A small toggle lets min-maxers switch to "compare against [specific character]."

---

## Cosmetic Reward Loop: The "Changing Room" Feel

To nail the cosmetic motivation:

- **Swipe between party members** on the Equipment tab without closing the drawer. The comparison recalculates instantly for the new character, and the preview updates. This lets players browse "what would this helmet look like on my mage vs. my tank?" fluidly.
- **"New" badges** on items that dropped since the player last opened inventory. New items subtly glow in the drawer list. Clearing all "new" items (by previewing or equipping them) could give a small gold bonus — reinforcing the habit of checking your loot.

---

## Tap Count Summary

| Player Goal | Path | Taps |
|---|---|---|
| Equip best upgrade to a slot | Equipment tab → tap slot → tap ★ BEST | 2 |
| Compare a specific item | Equipment tab → tap slot → tap item → read comparison | 3 |
| Try on items cosmetically | Equipment tab → tap slot → tap items in list, watch character change | 2+ (browsing) |
| Equip a new drop optimally | Loot screen → tap item → tap ★ BEST MATCH portrait | 2 |
| Auto-optimize one character | Equipment tab → tap Optimize | 1 |
| Auto-optimize whole party | Loot screen → tap Optimize All | 1 |
| Sell junk | Loot screen → tap item → Sell (or multi-select + Sell All) | 2 |

---

## Core Principle

**The character is always visible, the numbers are always big, and the best choice is always highlighted.** Players who care about looks browse freely. Players who care about stats follow the green arrows. Both get there in 2 taps.

---

## Out of Scope (Sprint 7.5)

The following issues are related to the Party screen but are tackled separately in Sprint 7.5, before the Equipment UI work begins:

- **Party portrait darkening** — Party portraits on the portrait strip render darker than roster grid tiles. Likely a rendering/camera/lighting difference between the two CharacterPortraitRenderer setups.
- **Actions tab rework** — The current Actions tab is hard to read and doesn't clearly show what's equipped. Needs a visual overhaul for clarity.
