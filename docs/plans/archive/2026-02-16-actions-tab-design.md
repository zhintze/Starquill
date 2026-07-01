# Starquill Actions Tab Design — Verb Loadout & Party Pool

## Design Philosophy

The Actions tab is a sibling of Equipment under the same Party screen. The top of the screen (header + mirror) **persists identically across all three sub-tabs** (Roster, Equipment, Actions). The character is always the visual anchor. Only the content below the mirror changes when switching tabs.

Verbs aren't spatial like equipment — they don't map to a body. They're a loadout, a hand of cards that feeds into a shared combat pool. The layout leans into that: **a short ordered list**, not a spatial cluster. Think playlist, not paper doll.

---

## Screen Layout: Actions Tab (1080×1920 Portrait)

### Header Bar (~80px)

Character name, class, ◀ ▶ party swap arrows. Identical to Equipment tab.

### The Mirror (~700px)

Full character render, identical to Equipment tab. Persists across all sub-tabs so switching tabs never rearranges the top of the screen.

### Party Pool Summary Bar (~60px)

A thin horizontal bar showing the party's total verb pool composition by stat type:

```
⚔STR: 3   🏹DEX: 1   🔥INT: 2   💚WIS: 0
```

- Each stat type uses its verb color.
- A stat at **zero** is highlighted in red or flagged with a warning icon to surface gaps (e.g., no heals if WIS is 0).
- Updates **live** as verbs are equipped/unequipped — slotting in Heal and watching the WIS counter tick from 0 to 1 is a satisfying feedback loop.
- Minimal footprint (~60px) for high-signal information. Instantly answers "why am I never getting heals in combat?"

### Equipped Verb Slots (~350px)

A vertical stack of **horizontal verb cards** (~280×90 each). The number of visible cards matches the character's unlocked slot count (2–5 based on level).

Each equipped verb card shows:

```
┌─┬──────────────────────────────────┐
│▐│  Slash          12 dmg  ⏱ 3s    │
│▐│  ⚔ STR · Single                 │
└─┴──────────────────────────────────┘
 ↑
 Stat-type color stripe (left border)
```

- **Left border stripe** — stat-type color (red for STR, blue for INT, green for WIS, etc.). The color coding from the current design survives but isn't the only distinguishing feature.
- **Icon / stat-type symbol** — placeholder icon or stat glyph on the left.
- **Verb name** — prominent, left-aligned.
- **Damage + cooldown** — right-aligned. Damage as a number, cooldown as a short duration (`3s`).
- **Stat type + target mode** — smaller secondary line. Stat abbreviation and target mode (Single / Cleave / AoE).

**Empty slots** show a tappable `+ Empty Slot` card with a dotted border, inviting the player to open the drawer.

**Locked slots** collapse into a **single compact indicator** below the last real slot:

```
2/5 slots · Next at Lv 11  ▓▓▓▓▓▓░░░░
```

A mini progress bar toward the next unlock. This takes ~50px instead of three gray boxes (~270px), kills the dead space problem at early levels, and gives low-level players something to look forward to.

### Drawer Bar (~80px)

Collapsed by default, showing a summary:

> `"2 verbs available"`

Tapping an equipped verb or an empty slot expands the drawer upward.

---

## The Drawer: Verb Detail & Swap

Same bottom sheet pattern as the Equipment tab — slides up to ~60% of the screen. The character's head/torso still peeks through above the drawer.

### Drawer Header: Selected Verb Detail

If an equipped verb was tapped, its **full stat block** is displayed:

- Verb name + stat type + rarity/tier
- Base damage + stat scaling factor
- Hit count + target mode (Single / Cleave / AoE)
- Cooldown in ticks (translated to seconds)
- Effect: status effect or bonus effect with proc chance, duration, and potency
- Special flags (e.g., WIS healing)

If an empty slot was tapped, the header shows `"Empty Slot — Choose a verb!"`.

### Drawer Body: Available Verbs

A scrollable list of all unlocked-but-not-equipped verbs. Each row is a compact card showing:

- **Verb name + stat type stripe** (left)
- **Key stats** (center): damage, cooldown, target mode
- **Delta badges** (right): Big, chunky pill-shaped comparison indicators against the currently equipped verb in that slot:
  - `+5 DMG` in green or `-3 DMG` in red
  - `−1s CD` in green (faster) or `+2s CD` in red (slower)
  - Effect comparison: shows gained/lost effects (e.g., `+Burn 30%` in green, `-Stun 15%` in red)
  - **Net score badge** at the far right: `▲ +8` in green or `▼ -4` in red

### Sorting & Filtering

Toggle chips at the top of the available list:

- **Best First** (default) — sorted by net score improvement
- **By Type** — grouped by stat type (STR, DEX, INT, etc.)
- **Newest** — most recently unlocked
- A `★ BEST` badge on the single highest-scoring available verb

### Preview & Equip

**Tapping an available verb previews it** — the equipped slot card above the drawer live-updates to show the new verb in place, and the party pool summary bar recalculates (e.g., WIS ticks from 0 to 1 if you're previewing Heal).

Two buttons at the bottom of the drawer:

- **"Equip"** (primary, large, green-tinted) — commits the swap. The old verb returns to the available pool.
- **"Back"** (secondary, smaller) — reverts preview, returns to the list.

Equipping a better verb is **2 taps**: tap the slot → tap the verb with the `★ BEST` badge.

---

## Unequipping

Tapping an equipped verb opens the drawer with that verb's full detail in the header. An **"Unequip"** button (red-tinted, secondary) appears alongside the available verb list. Tapping it returns the verb to the available pool and leaves an empty slot.

---

## The Full Vertical Stack

```
┌──────────────────────────┐
│  Header Bar (~80px)      │  Character name, class, ◀ ▶ arrows
├──────────────────────────┤
│                          │
│  The Mirror (~700px)     │  Full character render
│                          │  (identical across all sub-tabs)
│                          │
├──────────────────────────┤
│  Pool Summary (~60px)    │  ⚔STR:3  🏹DEX:1  🔥INT:2  💚WIS:0
├──────────────────────────┤
│  Equipped Slots (~350px) │  2–5 verb cards stacked vertically
│  ┌────────────────────┐  │
│  │▐ Slash    12⚔  3s │  │  ~280×90, STR red stripe
│  ├────────────────────┤  │
│  │  + Empty Slot      │  │  Tappable, opens drawer
│  ├────────────────────┤  │
│  │  2/5 · Lv 11 ▓▓░░ │  │  Collapsed lock indicator (~50px)
│  └────────────────────┘  │
├──────────────────────────┤
│  Drawer Bar (~80px)      │  "2 verbs available" (collapsed)
└──────────────────────────┘

Total: ~1270px used · ~650px breathing room
```

When the drawer opens, it covers the equipped slots and most of the mirror — character's head/torso still peeks through, maintaining the visual anchor.

---

## Tap Count Summary

| Player Goal | Path | Taps |
|---|---|---|
| Equip best verb to an empty slot | Tap empty slot → tap ★ BEST | 2 |
| Swap to a better verb | Tap equipped slot → tap ★ BEST | 2 |
| Compare verbs in detail | Tap slot → tap verb → read comparison | 3 |
| Unequip a verb | Tap equipped verb → tap Unequip | 2 |
| Check party pool balance | Glance at pool summary bar | 0 |
| See progress to next slot | Glance at locked slot indicator | 0 |

---

## Cross-Tab Consistency

| Element | Roster Tab | Equipment Tab | Actions Tab |
|---|---|---|---|
| Header Bar | ✓ | ✓ | ✓ |
| Mirror (character render) | ✓ | ✓ | ✓ |
| Middle content | Character grid | Slot cluster | Pool bar + verb slots |
| Drawer interaction | — | Bottom sheet | Bottom sheet |
| Delta badges | — | Stat deltas | Damage/cooldown/effect deltas |
| ★ BEST indicator | — | Best in slot | Best available verb |

The top ~780px of the screen is **identical across all three sub-tabs**. Switching tabs only changes the bottom half. The character is always present, always the anchor, always showing the result of your decisions.

---

## Core Principle

The pool summary bar answers "what does my party need?" The verb cards answer "what do I have?" The drawer answers "what's better?" All three are visible or one tap away. A player understands their loadout in 2 seconds and improves it in 2 taps.
