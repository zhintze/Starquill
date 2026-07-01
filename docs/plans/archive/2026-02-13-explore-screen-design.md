# Explore Screen UI — Design Document

**Sprint:** 3
**Date:** 2026-02-13
**Status:** Complete (Implemented & Verified)

---

## Goal

Build the Explore screen as a static UI layout with live paper doll rendering of the party, parallax scrolling background, placeholder verb cards, and navigation bar. No gameplay wiring — validates the full screen structure before Sprint 4 adds combat.

## Layout (1080x1920 Reference)

```
┌─────────────────────────────────────────┐
│  💰 1.2M   🧩 7/12   Wave 3/5   Q.34  │  ← TopBar (100px, single row)
├─────────────────────────────────────────┤
│ ░░░░░ sky / distant mountains ░░░░░░░░ │  ← BG Layer 0 (scroll 5px/s)
│ ▒▒▒▒▒▒▒ mid hills / trees ▒▒▒▒▒▒▒▒▒▒ │  ← BG Layer 1 (scroll 15px/s)
│ ▓▓▓▓▓▓▓▓▓ near ground ▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │  ← BG Layer 2 (scroll 30px/s)
│                                         │
│  👤👤 (360x360)    ▓▓▓ (240x360)       │  ← Party (staggered) + Enemies
│  👤👤 (440x440)    ▓▓▓                 │
│                                         │
│ 🌿🌿 foreground grass 🌿🌿🌿🌿🌿🌿🌿 │  ← FG Layer (scroll 50px/s, 140px)
├─────────────────────────────────────────┤
│    [Bash]      [Analyze]     [Slash]    │  ← VerbBar (130px, single row, 3 cards)
├─────────────────────────────────────────┤
│  ⚔️  |  📜  |  🎒  |  👥  |  🏪       │  ← BottomNav (120px)
└─────────────────────────────────────────┘
```

**Panel heights (reference 1920):**
- TopBar: 100px (single row)
- CombatArea: ~1570px (flexible fill)
- VerbBar: 130px (single row, 3 cards)
- BottomNav: 120px

## Architecture

### Canvas Configuration
- **Render Mode:** Screen Space - Overlay
- **CanvasScaler:** Scale With Screen Size, reference 1080x1920, match width=0 height=1
- **All panels** anchored so layout adapts to different aspect ratios

### Parallax Background System
Each background/foreground layer is a **RawImage** with a tiling texture. Scrolling uses UV offset manipulation (`rawImage.uvRect`). Textures must have **Wrap Mode: Repeat** for seamless tiling.

**Scroll math (extracted to testable static class):**
- `uvWidth = displayWidth / textureWidth` (shows texture at native resolution)
- Each frame: `offset += scrollSpeed * deltaTime / textureWidth`
- Offset wraps to `[0, 1)` range

**Layer rendering order (Canvas sibling order):**
1. BG layers (sky, mid, ground) — bottom of sibling list
2. Party RawImages + Enemy silhouettes — middle
3. FG layer (grass) — on top of characters, partial transparency

### Party Paper Dolls (Staggered Layout)
Four CharacterDisplay instances (from Sprint 2), each compositing to its own RawImage. Positioned in a 2x2 staggered arrangement:

- **Back row** (2 chars): higher position, smaller (~360x360)
- **Front row** (2 chars): lower position, larger (~440x440)
- Front row overlaps back row edges for depth

Each CharacterDisplay creates a random species from DisplayDataRegistry at startup.

### Enemy Silhouettes
Three dark rounded rectangle Images on the right side of the combat area. Simple placeholder shapes (~240x360 each). No sprite loading.

### Placeholder Data (Sprint 3)
All text and values are hardcoded placeholders:
- Gold: "1.2M"
- Fragments: 7/12
- Wave: "Wave 3/5"
- Quest Level: "Quest Lv 34"
- Verb cards: 3 cards (Bash, Analyze, Slash) with colored backgrounds
- Parallax textures: runtime-generated striped/gradient textures

## Scene Hierarchy

```
ExploreScene
├── EventSystem (InputSystemUIInputModule)
└── Canvas (CanvasScaler 1080x1920)
    ├── CombatAreaPanel (anchor stretch fill middle)
    │   ├── BG_Sky (RawImage, stretch fill, ParallaxLayer speed=5)
    │   ├── BG_Mid (RawImage, anchor bottom, h=840, ParallaxLayer speed=15)
    │   ├── BG_Ground (RawImage, anchor bottom, h=420, ParallaxLayer speed=30)
    │   ├── PartyContainer (anchor left, width ~55%)
    │   │   ├── PartySlot_BackLeft (RawImage 360x360)
    │   │   ├── PartySlot_BackRight (RawImage 360x360)
    │   │   ├── PartySlot_FrontLeft (RawImage 440x440)
    │   │   └── PartySlot_FrontRight (RawImage 440x440)
    │   ├── EnemyContainer (anchor right, width ~35%)
    │   │   ├── EnemySilhouette_0 (Image 240x360)
    │   │   ├── EnemySilhouette_1 (Image 240x360)
    │   │   └── EnemySilhouette_2 (Image 240x360)
    │   └── FG_Grass (RawImage, anchor bottom, h=140, ParallaxLayer speed=50)
    ├── TopBarPanel (anchor top, height 100, HorizontalLayoutGroup)
    │   ├── GoldLabel (TMP_Text, 36pt, gold)
    │   ├── FragmentLabel (TMP_Text, 28pt, cyan)
    │   ├── WaveLabel (TMP_Text, 28pt, white)
    │   └── QuestLevelLabel (TMP_Text, 28pt, white)
    ├── VerbBarPanel (anchor bottom above nav, height 130)
    │   └── VerbGrid (GridLayoutGroup, 3 cols, 1 row)
    │       ├── VerbCard_0..2 (Image bg + TMP name + TMP cooldown)
    ├── BottomNavPanel (anchor bottom, height 120)
    │   └── HorizontalLayoutGroup
    │       ├── NavBtn_Explore (Button + TMP)
    │       ├── NavBtn_Quests (Button + TMP)
    │       ├── NavBtn_Loot (Button + TMP)
    │       ├── NavBtn_Party (Button + TMP)
    │       └── NavBtn_Shop (Button + TMP)
    └── ExploreSceneController (MonoBehaviour on Canvas)
```

## New Files

| File | Assembly | Purpose |
|------|----------|---------|
| `Assets/Scripts/UI/Starquill.UI.asmdef` | — | Assembly definition for UI layer |
| `Assets/Scripts/UI/ParallaxMath.cs` | Starquill.UI | Pure C# scroll math (testable) |
| `Assets/Scripts/UI/ParallaxLayer.cs` | Starquill.UI | MonoBehaviour: RawImage UV scrolling |
| `Assets/Scripts/UI/TopBarDisplay.cs` | Starquill.UI | MonoBehaviour: gold, fragments, wave info, quest level (single row) |
| `Assets/Scripts/UI/VerbBarDisplay.cs` | Starquill.UI | MonoBehaviour: verb card grid with placeholders (max 3 cards) |
| `Assets/Scripts/UI/BottomNavDisplay.cs` | Starquill.UI | MonoBehaviour: 5 tab buttons, highlight active |
| `Assets/Scripts/UI/ExploreSceneController.cs` | Starquill.UI | MonoBehaviour: scene orchestrator |
| `Assets/Tests/EditMode/UI/ParallaxMathTests.cs` | EditModeTests | Tests for scroll math |

## Modified Files

| File | Change |
|------|--------|
| `Assets/Tests/EditMode/EditModeTests.asmdef` | Add `Starquill.UI` reference |

## Asset Specifications (for Artist)

All sizes are in pixels. Reference resolution is 1080x1920 (portrait mobile).

| # | Asset | Description | Size (px) | Format | Notes |
|---|-------|-------------|-----------|--------|-------|
| **Background Layers** |||||
| 1 | `bg_sky.png` | Distant sky, clouds, mountains. Slowest scroll layer. Covers full combat area. | 2160 x 1400 | PNG, RGBA | Tiles horizontally. Left/right edges must match seamlessly. |
| 2 | `bg_mid.png` | Mid-ground hills, trees, structures. Medium scroll. Lower ~60% of combat area. | 2160 x 840 | PNG, RGBA | Tiles horizontally. Upper portion fades to transparent. |
| 3 | `bg_ground.png` | Near ground plane, path, rocks. Faster scroll. Lower ~30% of combat area. | 2160 x 420 | PNG, RGBA | Tiles horizontally. Upper portion fades to transparent. |
| **Foreground Layer** |||||
| 4 | `fg_grass.png` | Grass, flowers, small debris. Fastest scroll. Overlaps bottom of characters. | 2160 x 140 | PNG, RGBA | Tiles horizontally. Mostly transparent with elements along the bottom. |
| **UI Elements** |||||
| 5 | `verb_card_bg.png` | Verb card background. Rounded rectangle with subtle border. | 330 x 100 | PNG, RGBA | 9-sliced. Tinted per verb stat type at runtime. |
| 6 | `nav_icon_explore.png` | Sword/compass icon for Explore tab. | 64 x 64 | PNG, RGBA | Monochrome, tinted at runtime. |
| 7 | `nav_icon_quests.png` | Scroll icon for Quests tab. | 64 x 64 | PNG, RGBA | Monochrome, tinted at runtime. |
| 8 | `nav_icon_loot.png` | Bag/backpack icon for Loot tab. | 64 x 64 | PNG, RGBA | Monochrome, tinted at runtime. |
| 9 | `nav_icon_party.png` | Group/people icon for Party tab. | 64 x 64 | PNG, RGBA | Monochrome, tinted at runtime. |
| 10 | `nav_icon_shop.png` | Storefront icon for Shop tab. | 64 x 64 | PNG, RGBA | Monochrome, tinted at runtime. |
| 11 | `fragment_bar_fill.png` | Fragment progress bar fill texture. | 256 x 32 | PNG, RGBA | 9-sliced. Horizontal fill. |
| 12 | `fragment_bar_bg.png` | Fragment progress bar background/track. | 256 x 32 | PNG, RGBA | 9-sliced. |

**Import settings for parallax textures (bg_sky, bg_mid, bg_ground, fg_grass):**
- Texture Type: Default (not Sprite — used as RawImage texture)
- Wrap Mode: **Repeat** (critical for UV scrolling)
- Filter Mode: Bilinear
- Compression: None
- Max Size: 2048

**Import settings for UI elements (verb_card_bg, nav icons, fragment bars):**
- Texture Type: Sprite
- Sprite Mode: Single
- Filter Mode: Bilinear
- Compression: None

## Not in Scope (Sprint 3)

- GameManager wiring / live combat updates (Sprint 4)
- Verb card tap interactions (Sprint 4)
- Damage numbers, floating icons, boost indicators (Sprint 5)
- Bottom nav screen switching (Sprint 5)
- Real art assets (artist delivers separately)
