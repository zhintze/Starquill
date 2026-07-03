# Mobile UI Redesign — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rebuild Starquill's UI on a token-based design system (UiTheme/UiFactory) with a mid-density ItemCard, bottom-sheet detail flow, and mobile-correct sizing on every screen.

**Architecture:** Two foundations (`UiTheme` tokens, `UiFactory` primitives) that all screens compose; `ItemCardBuilder` v2 and a reusable `BottomSheet` replace the drawer-preview and overlay flows; screens convert one at a time, each ending compile-clean and committed.

**Tech Stack:** C# / Unity 6, UGUI + TextMeshPro (procedural), NUnit EditMode, Coplay MCP compile checks.

**Design doc:** `docs/plans/2026-07-01-mobile-ui-redesign-design.md`

**Verification protocol:** headless tests don't work; scripted TestRunnerApi runs crashed the editor (2026-07-01) — do NOT script test runs. After each task: Coplay `check_compile_errors`. At checkpoints: USER runs Test Runner (EditMode, all) in the editor GUI and reports. Scene rebuilds via `Tools > Build Explore Scene` + save.

**Glyph rule (applies to every task):** no Unicode symbols in TMP text (✦⚖💰●○▲▼★ render as boxes). Pips, deltas, coins, rings are `Image` elements. `<alpha=#XX>` is a state change with NO closing tag — never emit `</alpha>`; prefer explicit `<color=#RRGGBBAA>`.

---

### Task 1: UiTheme tokens + theme invariant tests

**Files:** Create `Assets/Scripts/UI/UiTheme.cs`; Test `Assets/Tests/EditMode/UI/UiThemeTests.cs`

Static class, pure constants:
```csharp
public static class UiTheme
{
    // Type scale (1080x1920 canvas). FontFloor is a hard invariant.
    public const float FontDisplay = 44f, FontHeading = 38f, FontBody = 34f, FontCaption = 28f, FontFloor = 28f;
    // Spacing (8px grid)
    public const float Space1 = 8f, Space2 = 16f, Space3 = 24f, Space4 = 32f;
    public const float CardPadding = 24f, ListGutter = 24f;
    // Touch targets
    public const float TouchMin = 120f, NavHeight = 160f, ButtonPrimaryHeight = 130f;
    // Card metrics
    public const float CardHeight = 240f, CardIcon = 200f, DeltaChipWidth = 120f, DeltaChipHeight = 72f;
    // Surfaces
    public static readonly Color Background = Hex("1A1A26"), Card = Hex("232330"), Sheet = Hex("2C2C3A");
    public static readonly Color AccentGreen = Hex("2E8B57");
    public static readonly Color DeltaUp = new(0.3f, 0.9f, 0.3f), DeltaDown = new(0.9f, 0.3f, 0.3f), DeltaNeutral = new(0.6f, 0.6f, 0.6f);
    public static readonly Color TextPrimary = Color.white, TextSecondary = new(1f, 1f, 1f, 0.7f), TextDim = new(0.55f, 0.55f, 0.6f);
    private static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
}
```
Tests: every `Font*` ≥ `FontFloor`; `TouchMin ≥ 120`; `NavHeight/ButtonPrimaryHeight ≥ TouchMin`; `CardHeight ≥ 2 * DeltaChipHeight` (chip fits). Compile check → commit `feat: UiTheme design tokens`.

### Task 2: UiFactory primitives

**Files:** Create `Assets/Scripts/UI/UiFactory.cs`

Static builders returning GameObjects, all sized from UiTheme (signatures are the contract; internals follow existing procedural patterns):
`Text(Transform, string, TextStyle, TextAlignmentOptions)` (TextStyle enum: Display/Heading/Body/Caption + color variants), `Button(Transform, string, ButtonKind, Action)` (Primary/Secondary/Destructive; height per kind; disabled support via returned handle struct with `SetEnabled(bool, string reason)`), `Chip(Transform, string, Color bg)` (rounded Image + Body text), `PipRow(Transform, int level, int max)` (Image dots 20px, filled = white, empty = 30% white), `ProgressBar(Transform)` returning handle with `SetFraction(float)`, `Frame(Transform, Color rarity)` (border Image + inner sprite Image), `SegmentedTabs(Transform, string[], Action<int>)` with active underline, `HeaderBar(Transform, string title, (string, Action)? action)`, `EmptyState(Transform, string)`. Fix pass in same task: grep and remove all `</alpha>` emissions (`ItemCardBuilder`, `ItemDetailPanel`, `EquipmentDrawer`) replacing with `TextSecondary`-colored spans. Compile check → commit `feat: UiFactory component library + alpha tag fixes`.

### Task 3: ComparisonData + tests (TDD)

**Files:** Create `Assets/Scripts/UI/ComparisonData.cs`; Test `Assets/Tests/EditMode/UI/ComparisonDataTests.cs`

Pure struct: `ComparisonData.Build(EquipmentInstance selected, EquipmentInstance equipped)` → `Rows` (per StatType present in either item's `GetTotalStatMods()`: `Stat, EquippedValue, SelectedValue, Delta`), plus `TotalDelta`, `IsLegacyItem` (stat pair values all zero → UI shows "Legacy item"). Write tests FIRST: upgrade, downgrade, equal, equipped-null (empty slot: all deltas = selected values), legacy-zero detection, ability potency included via GetTotalStatMods. Implement minimal. Compile check → commit `feat: ComparisonData for equipped-vs-selected stat rows`.

**CHECKPOINT A: user runs Test Runner — expect all green (276 + ~10 new).**

### Task 4: ItemCardBuilder v2

**Files:** Rewrite `Assets/Scripts/UI/ItemCardBuilder.cs`

Mid-density anatomy per design doc: 240px card on `UiTheme.Card`; left `Frame` 200px with sprite (keep `LoadEquipmentSprite`); middle lines: name (Heading, rarity color, ellipsis), stat pair (Body: primary in `StatTypeColors` color, secondary in TextSecondary), ability row (`{Name}` Body + `PipRow` + potency Caption) or omitted, meta line (Caption dim: `{Slot} · {Rarity} · {sell}g`); right `Chip` delta (`+N`/`-N`/`±0`, DeltaUp/Down/Neutral) sized DeltaChipWidth×Height, vertically centered. Legacy items (`ComparisonData.IsLegacyItem`) show "Legacy item" in place of the stat pair. Keep `CardOptions` shape (drop `Height`); whole card = Button. Compile check → commit `feat: ItemCardBuilder v2 mid-density anatomy`.

### Task 5: BottomSheet + ItemDetailSheet; Loot screen adoption

**Files:** Create `Assets/Scripts/UI/BottomSheet.cs`, `Assets/Scripts/UI/ItemDetailSheet.cs`; Modify `Assets/Scripts/UI/LootScreenController.cs`; Modify `Assets/Editor/ExploreSceneBuilder.cs` (sheet host under LootPanel replacing ItemDetailPanel overlay; sort tabs; header via HeaderBar)

`BottomSheet`: full-screen scrim (55% black, tap = close) + content panel anchored bottom (55% height, `UiTheme.Sheet`), slide coroutine like drawer's, `Open(Action<Transform> fillContent)` / `Close()`, drag-handle Image. `ItemDetailSheet`: fills per design — header, ComparisonData table (rows of `Stat  equipped → selected  chip`), ability block (reuse Task 5 ProgressBar + level-up wiring from `ItemDetailPanel`, cost via `GameManager.GetAbilityLevelUpCost`), 4-portrait target row (`CharacterPortraitRenderer`, gold-ring Image on best match, tap selects), EQUIP primary + `Sell Ng` secondary actions. LootScreen: HeaderBar (`Inventory N/50`, Optimize All action), SegmentedTabs sort (Score/Newest/Rarity — Newest = insertion order reversed), card list via v2, tap → sheet. `ItemDetailPanel` component + its builder hierarchy deleted. Compile check → scene rebuild + save → commit `feat: bottom sheet detail flow, loot screen v2`.

### Task 6: Party screen conversion

**Files:** Modify `Assets/Scripts/UI/PartyScreenController.cs`, `Assets/Scripts/UI/EquipmentSlotsDisplay.cs`, `Assets/Scripts/UI/EquipmentDrawer.cs`, `Assets/Scripts/UI/CharacterFocusDisplay.cs`, `Assets/Editor/ExploreSceneBuilder.cs`

Paper doll band 560px; stat strip → six `Chip`s (32px text); tabs → `SegmentedTabs` 120px. EquipmentSlotsDisplay: 11 slot cards via v2 (`EmptyState`-styled dashed card when empty). Drawer becomes candidate-list-only: delete selected-slot panel fields/logic, Equip/Back buttons, preview state; candidate tap → ItemDetailSheet (EQUIP pre-targeted to focused character). Compile check → scene rebuild + save → commit `feat: party screen v2, drawer simplified to candidate list`.

### Task 7: Explore screen + nav sizing

**Files:** Modify `Assets/Editor/ExploreSceneBuilder.cs` (top bar 140px, nav 160px + active underline, verb cells 340x180), `Assets/Scripts/UI/TopBarDisplay.cs` (gold Display 44 + coin Image, wave Caption 32), `Assets/Scripts/UI/VerbBarDisplay.cs`/`VerbCardAnimator.cs` (labels ≥34, cooldown fill via ProgressBar), `Assets/Scripts/UI/BottomNavDisplay.cs` (active state = accent underline + TextPrimary vs TextDim), `Assets/Scripts/UI/LootToastFeed.cs` (72px rows, Body text, Image arrow chip instead of "▲")

Compile check → scene rebuild + save → commit `feat: explore + nav mobile sizing`.

### Task 8: Verification + docs (CHECKPOINT B)

1. Compile clean; USER runs Test Runner (all green expected)
2. USER: Tools > Clear Save (purges legacy zero-stat items), then play-test checklist: loot cards legible per rarity; sheet comparison table matches deltas; ability XP bar + Level Up; equip from sheet updates paper doll; party tabs/slot cards; nav active states; toast sizing
3. Update `docs/sprint-review.md` (entry: Mobile UI Redesign) + `docs/roadmap.md` (fold into Sprint 8.5 completion); commit `docs: mobile UI redesign complete`
