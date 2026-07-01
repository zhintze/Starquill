# Mobile UI Redesign — Design

**Date:** 2026-07-01
**Status:** Validated with user section-by-section (row anatomy: mid-density cards; depth: layouts + flows)
**Supersedes:** the visual layer of Sprint 8.5 (`2026-07-01-sprint8.5-equipment-ui-readability-design.md`); its data/plumbing layer (AbilityTable exposure, ItemDisplayData, GetAbilityLevelUpCost) carries forward unchanged.

## Problem

The app converged on desktop-density UI at mobile resolution: 11-18px text on a 1080x1920 canvas, 280px cards that are mostly empty, gray-on-gray controls, and three different ad-hoc item-row implementations. Observed defects: TMP renders `</alpha>` literally (invalid tag), decorative Unicode (✦⚖💰●○▲) renders as boxes (missing from LiberationSans SDF), and pre-redesign saves reconstruct items as `STR +0 / STR +0`. Design principles for the fix: Nielsen heuristics (visibility of status, consistency, minimalism, error recovery) and Jakob's law (match mobile RPG inventory conventions).

## Design System (tokens in `UiTheme`)

| Token group | Values |
|---|---|
| Type scale | Display 44 · Heading 38 · Body 34 · Caption 28 · **floor 28** |
| Spacing | 8px grid; paddings 16/24/32; card padding 24; list gutter 24 |
| Touch targets | ≥120px tappable height; nav 160px; primary buttons 130px |
| Surfaces | background `#1A1A26` · card `#232330` · sheet `#2C2C3A` |
| Accent | action green; delta green/red reserved for upgrade/downgrade only |
| Rarity/stat colors | existing palettes (ItemDisplayData / StatTypeColors) |
| Glyphs | ASCII only in text; pips/deltas/coins are Image elements, never Unicode symbols |

## App-wide component library (`UiFactory`)

Procedural-UI primitives replacing copy-pasted `new GameObject` blocks. **These are the general elements for every current and future screen (Quests, Shop):**

- `Text(parent, style)` — styles map to the type scale
- `Button(parent, label, kind)` — Primary / Secondary / Destructive, ≥120px, disabled states with reason text
- `Chip(parent, text, color)` — delta chips, stat chips, status chips
- `PipRow(parent, level, max)` — Image-dot level pips
- `ProgressBar(parent)` — XP bars, wave progress, future quest progress
- `Frame(parent, rarityColor)` — rarity-tinted sprite frames
- `SegmentedTabs(parent, labels)` — party tabs, sort controls, future quest filters
- `HeaderBar(parent, title, action)` — screen headers with optional action button
- `BottomSheet` — scrim + drag handle + slide-up content host (item detail now; quest detail, shop confirmations later)
- `Toast` — the loot feed rows; future quest/offline notifications
- `EmptyState(parent, message)` — empty slots, empty inventory, "coming soon" panels

## Core element: ItemCard (mid-density, 240px)

```
┌──────────────────────────────────────────┐
│ ┌──────┐ Fine Chain Shirt         [+4]  │  name 38 rarity color
│ │sprite│ CON +7    WIS +3               │  stats 34, primary stat-colored,
│ │200px │ [Ironhide ●●○  CON +3]         │   secondary 70% white (real color, no alpha tag)
│ └──────┘ Torso · Fine · 45g             │  meta 28 dim; ability row w/ Image pips
└──────────────────────────────────────────┘
```
Right side: delta chip (120x72 rounded rect, green/red/gray, `+N` 34px), display-only; whole card taps. Used by: loot list, drawer candidate list, party Equipment slot list (converted at last — never touched in 8.5). Empty slots: dashed frame + "Empty · tap to browse".

## Core flow: Bottom sheet (replaces detail overlay + drawer preview/Equip)

Tap any card → sheet slides over dim scrim (~55% height):
1. Header: name, rarity·slot, drag handle; close via handle or scrim tap
2. **Comparison table: per-stat EQUIPPED → SELECTED with delta arrows** (the key missing feature)
3. Ability block: name, potency-substituted description, pip row, XP ProgressBar, `Level Up (Ng)` button; disabled state explains why
4. Target row: 4 party portraits, best-match pre-selected with gold ring
5. Actions: full-width EQUIP primary + `Sell Ng` secondary; sell shows an undo toast
Legacy items (zero stat pair from old saves) label "Legacy item" instead of `STR +0`.

## Per-screen layouts

- **Nav:** 160px bar, 5 equal targets, 32px bold labels, accent underline on active
- **Party:** paper doll band fixed 560px; six 160px stat chips; 120px segmented tabs; Equipment tab = 11 slot cards; slot tap → candidate list (same cards); candidate tap → sheet. Drawer's selected-slot panel, Equip and Back buttons deleted
- **Loot:** header `Inventory N/50` + Optimize All accent button; segmented sort (Score/Newest/Rarity); card list; tap → sheet
- **Explore:** top bar 140px (gold 44px + coin Image, wave/level 32px); verb cards 340x180 with 34px labels + cooldown fill; toast rows 72px/34px. Combat area visuals out of scope (Sprint 12 juice pass)

## Architecture & migration order

New: `UiTheme`, `UiFactory`, `BottomSheet`, `ItemDetailSheet`, `ComparisonData` (pure struct: per-stat rows + deltas — unit-testable). Rebuilt: `ItemCardBuilder` v2. Deleted: drawer preview/equip path, `ItemDetailPanel` overlay body, slot-list ad-hoc rows.

1. UiTheme + UiFactory + defect fixes (alpha tags, glyph policy)
2. ItemCardBuilder v2 + ComparisonData (+ tests)
3. BottomSheet/ItemDetailSheet; Loot screen adoption
4. Party screen conversion; drawer simplification; dead code deletion
5. Explore top bar / verb bar / toasts; nav; ExploreSceneBuilder update; scene rebuild + save
6. Full test suite + play-test checklist (ClearSave first to purge legacy zero-stat items)

## Testing

EditMode: `ComparisonData` (upgrade/downgrade/equal/empty-slot/legacy-zero), chip/pip formatting, theme invariants (type floor 28, targets ≥120). Visual acceptance via play-test checklist per screen.
