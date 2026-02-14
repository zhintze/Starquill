# Starquill Idle RPG Clicker — Sprint Review

---

## Sprint 1: Backend Architecture & Core Systems

**Date:** 2026-02-12 — 2026-02-13
**Branch:** `unity-idle-clicker`
**Base:** `dev`
**Status:** Complete — pushed to origin, pending PR

### Goal

Stand up the full Unity 6 backend for an idle RPG clicker: data layer, combat engine, economy math, save system, and game loop. No UI or visuals — pure logic with test coverage on all math-critical paths.

### Deliverables

#### Project Setup
- Archived all Godot files to `godot-archive/`
- Initialized Unity 6 (6000.3.8f1) project with IL2CPP, portrait orientation, 1080x1920
- Established 8 assembly definitions enforcing dependency boundaries:
  `Core → Data → Combat/Economy/Characters/Equipment/Exploration → Managers`
- Configured `.gitignore` for Unity

#### Core Enums (7 files)
| File | Purpose |
|------|---------|
| `StatType.cs` | STR, DEX, CON, INT, WIS, CHA with Physical/Mental category extension |
| `Rarity.cs` | Common, Uncommon, Rare, Epic, Legendary |
| `TargetMode.cs` | Single, Cleave, AoE |
| `VerbCategory.cs` | Physical, Mental |
| `StatusEffectType.cs` | None, Stagger, Bleed, Weaken, Expose, Reveal, Confuse |
| `EquipmentSlot.cs` | Head, Torso, Arms, Legs, Feet, MainHand, OffHand, Misc1–4 |
| `Advantage.cs` | Weak, Neutral, Strong |

#### Data ScriptableObjects (11 files)
| File | Purpose |
|------|---------|
| `Stats.cs` | Serializable stat block with GetStat(), SetStat(), Total, HighestStat(), Clone(), operator+ |
| `SpeciesDefinition.cs` | Base stats, species ability, body part IDs, item restrictions |
| `SpeciesAbilityDefinition.cs` | 6 rank thresholds with scaling effects |
| `EquipmentDefinition.cs` | Item ID, slot, stat mods, affixes, layer codes, set membership |
| `AffixDefinition.cs` | Stat modifier ranges, percentage flag, valid slots |
| `EquipmentSetDefinition.cs` | 2pc/3pc/4pc set bonuses |
| `VerbDefinition.cs` | Stat type, base damage, hit count, target mode, proc chance, cooldown, rarity |
| `StatusEffectDefinition.cs` | Stacking rules, overrides, cleanse relationships |
| `EconomyConfig.cs` | 20 tuning knobs + EnemyHP(), GoldPerKill(), UpgradeCost(), OfflineGold() formulas |
| `AdvantageMatrix.cs` | Static 6x6 damage/proc matrices, GetMatchup() returning MatchupResult |
| `QuestZoneDefinition.cs` | Zone ID, dominant enemy types, quest count, dialogue arrays |

#### Combat System (6 files)
| File | Purpose |
|------|---------|
| `DamageCalculator.cs` | Static damage formula: base * statMult * advantage * expose * equip * prestige * boost |
| `DrawnVerb.cs` | Wraps VerbDefinition with owner index, draw time, cooldown tracking |
| `VerbPool.cs` | Card-draw verb system: fill slots, rotate stale verbs (10s), activate with cooldown |
| `EnemyState.cs` | Enemy HP, status effects (apply/tick/query), damage output with Weaken/Stagger |
| `CombatTickResult.cs` | Per-tick output: damage dealt/received, kills, gold, status procs, advantage hits |
| `CombatTickProcessor.cs` | Full tick pipeline: auto-attack → verb resolution → status ticks → enemy attacks → kill check |

#### Equipment (1 file)
| File | Purpose |
|------|---------|
| `PityTracker.cs` | Guaranteed drops at 50/200/1000/5000 kills for Uncommon/Rare/Epic/Legendary |

#### Characters (2 files)
| File | Purpose |
|------|---------|
| `CharacterInstance.cs` | Species, base stats, equipment array, verb slots (level-scaled 2/3/4/5), species ability rank |
| `Party.cs` | Max 4 members, stat aggregation, verb pooling, idle bonuses (CHA→gold, INT→XP, WIS→discovery at 2%/pt) |

#### Exploration (2 files)
| File | Purpose |
|------|---------|
| `ExplorationState.cs` | Enum: Exploring, InQuest, QuestRetreat |
| `ExplorationManager.cs` | State machine with quest discovery (3%), fragment drops (1%), state transitions, events |

#### Managers (3 files)
| File | Purpose |
|------|---------|
| `SaveData.cs` | Serializable save structure: gold, characters, pity tracker, timestamps, fragments, codex |
| `SaveManager.cs` | PlayerPrefs + JsonUtility save/load, offline seconds calculation, delete save |
| `GameManager.cs` | Central MonoBehaviour: 1s tick loop, verb activation, wave spawning, verb pool rebuild, 30s auto-save |

#### Test Coverage (7 files, ~49 tests)
| Test File | Tests | What's Covered |
|-----------|-------|----------------|
| `StatTypeTests.cs` | 6 | Category mapping for all 6 stats |
| `StatsTests.cs` | 4 | GetStat, Total, HighestStat, operator+ |
| `AdvantageMatrixTests.cs` | 12 | All triangle matchups, cross-triangle rules, neutral |
| `DamageCalculatorTests.cs` | 10 | Base damage, low stat, advantage/disadvantage, expose, combos, proc modifiers |
| `VerbPoolTests.cs` | 6 | Fill, exceed, activate, rotate at 10s, cooldown blocking, tick frees |
| `EconomyConfigTests.cs` | 8 | HP scaling, gold per kill, CHA bonus, cost growth, offline cap/efficiency |
| `PityTrackerTests.cs` | 3 | No guarantee before threshold, guaranteed at 50, register drop resets |

### Bugs Found & Fixed

1. **Double Expose multiplier** — Both `EnemyState.TakeDamage()` and `DamageCalculator.Calculate()` applied 1.25x, resulting in 1.5625x. Fixed: removed from `TakeDamage`, added to auto-attack path in `CombatTickProcessor`.

2. **Dead enemies re-counted every tick** — Kill check iterated all dead enemies (including previously dead), awarding duplicate gold. Fixed: kill check now uses the `aliveEnemies` snapshot captured before damage.

### Commits

```
ab10e57f Add idle RPG clicker architecture and design documents
9a5328d8 Add Sprint 1 implementation plan with 19 tasks and test coverage
630eef2f Archive Godot files and prepare for Unity project
e10a38d4 Initialize Unity 6 project with folder structure and test framework
ccbced30 Add core enums: StatType, Rarity, TargetMode, StatusEffectType, EquipmentSlot
66d36916 Add Stats class with tests and SpeciesDefinition ScriptableObjects
63c9c7e1 Add Equipment, Affix, and EquipmentSet ScriptableObject definitions
e539068d Add VerbDefinition and StatusEffectDefinition ScriptableObjects
9563387e Add EconomyConfig and AdvantageMatrix with full test coverage
6f049180 Add DamageCalculator with full damage formula and stat modifier tests
d8906173 Add EconomyConfig formula tests validating HP/gold/cost scaling curves
a797386b Add VerbPool with draw/rotate/cooldown mechanics and tests
9c3b841e Add CombatTickProcessor, EnemyState, and CombatTickResult
6dc52b78 Add PityTracker with guaranteed drop thresholds and tests
0173b442 Add CharacterInstance and Party classes with stat aggregation
b250e454 Add ExplorationManager state machine and QuestZoneDefinition
2b36f486 Add SaveManager with JSON serialization and offline time tracking
e4829e66 Add GameManager with tick loop, verb activation, and auto-save
43b692cb Fix double Expose multiplier and per-tick kill re-counting bugs
90e5cf76 Add Unity-generated meta files and project settings
```

### Stats

- **Source files:** 33 (.cs) across 8 assemblies
- **Test files:** 7 (.cs), ~49 test cases
- **Lines added:** ~1,926 (source + tests + meta)
- **Commits:** 20

### Known Issues

- Unity headless batch mode test runner hangs on Arch Linux due to `ScriptableRuntimeReflectionSystem` GC handle bug. Tests must be run in Unity Editor GUI.
- `GameManager.SpawnWave` uses `UnityEngine.Random` while other classes use `System.Random` with optional seeds. Minor determinism inconsistency for testing.
- `Economy` assembly exists but is empty — `EconomyConfig` lives in `Data` assembly.

### Open Questions for Sprint 2

- Port paper doll display system from Godot or rebuild from scratch in Unity?
- UI framework choice: Unity UI Toolkit vs. traditional Canvas + TextMeshPro?
- Should verb slot UI use drag-and-drop or tap-to-activate?

---

## Sprint 2: Paper Doll Display System

**Date:** 2026-02-13
**Branch:** `unity-idle-clicker`
**Base:** `dev`
**Status:** Complete — all 75 tests passing, visual verification confirmed in Unity Editor

### Goal

Port the Godot layered paper doll character rendering system to Unity using RenderTexture compositing with bilinear filtering for smooth scaling of pencil-drawn art across all mobile resolutions.

### Architecture

A 4-stage DisplayBuilder pipeline produces sorted DisplayPiece lists from character data. Species body part tokens (static, modular full, modular group) resolve to sprite paths via ImageToken. A compositing camera renders all layers into a single 400x400 RenderTexture. The composited texture displays via UI.RawImage with bilinear filtering. All logic except CharacterDisplay is plain C# — fully testable in EditMode.

### Deliverables

#### Asset Migration & Cleanup
- Copied 5 JSON data files to `Assets/Resources/Data/`
- Copied 4,108 sprite PNGs to `Assets/Resources/Images/` (1,284 species + 2,615 equipment + 209 weapons)
- Removed 4,108 Godot `.import` files (not needed by Unity)
- Moved original Godot `assets/` directory to `godot-archive/assets/`
- Bulk-configured all sprites as Sprite type with Bilinear filtering, no compression via SpriteImportFixer editor tool

#### Display System (11 files in `Starquill.Display` assembly)
| File | Purpose |
|------|---------|
| `DisplayPiece.cs` | Plain C# data class: layer, sprite path, tint color, offset, scale, rotation, flip |
| `ImageToken.cs` | Parses 3 token formats (static, modular full, modular group) and constructs sprite resource paths |
| `ColorManager.cs` | Loads color palettes from JSON, hex parsing, random color selection, palette/hex field resolution |
| `MiniJSON.cs` | MIT-licensed single-file JSON parser for Unity (Dictionary/List deserialization) |
| `SimpleJson.cs` | Extension methods for typed access to MiniJSON Dictionary objects |
| `DisplayDataRegistry.cs` | Singleton: loads species, equipment, modular parts from JSON; hardcoded layer mappings |
| `SpeciesInstanceData.cs` | Runtime display state: persistent colors, modular image numbers, weighted hair group selection |
| `DisplayBuilder.cs` | 4-stage pipeline: build species pieces, build equipment pieces, filter hidden layers, merge and sort |
| `ImageResolver.cs` | Sprite loading with Dictionary cache and warning suppression for missing sprites |
| `CharacterDisplay.cs` | MonoBehaviour: compositing camera + RenderTexture + bilinear RawImage output, auto-excludes compositing layer from other cameras |
| `DisplayTestRunner.cs` | Test harness: loads first species, creates random instance, renders via CharacterDisplay |
| `SpriteImportFixer.cs` | Editor tool (Tools menu): bulk-updates all sprite import settings |

#### Assembly Definition Updates
| File | Change |
|------|--------|
| `Starquill.Display.asmdef` | New assembly depending on Core and Data |
| `Starquill.Managers.asmdef` | Added Display reference |
| `EditModeTests.asmdef` | Added Display reference |

#### Test Coverage (3 test files, ~26 tests)
| Test File | Tests | What's Covered |
|-----------|-------|----------------|
| `ImageTokenTests.cs` | 10 | Static/modular full/modular group parsing, sprite path construction, equipment/weapon paths |
| `ColorManagerTests.cs` | 9 | Hex parsing (with/without #), palette loading, fallback, random color, hex array vs keyword resolution |
| `DisplayBuilderTests.cs` | 7 | Species piece creation, modular group expansion, hair/eyes color application, hidden layers, hat deduplication, merge and sort |

### Design Decisions

1. **RenderTexture Compositing** — All character layers (30-50 sprites) render to a single 400x400 off-screen texture. The composited image scales via RawImage with bilinear filtering. This prevents sprite distortion at different mobile resolutions.

2. **Bilinear Filtering** — Chosen over Point/Nearest because the art is pencil-drawn, not pixel art. Bilinear produces smooth scaling without jagged artifacts.

3. **MiniJSON for JSON Parsing** — Unity's `JsonUtility` cannot handle heterogeneous JSON structures (arrays with mixed types). MiniJSON provides `Dictionary<string, object>` / `List<object>` deserialization. `SimpleJson` extension methods add typed accessors.

4. **Hardcoded Layer Mappings** — Species modular group codes (e.g., `h02` → layers [92, 128]) are hardcoded in `DisplayDataRegistry.LoadLayerMappings()`, matching the original Godot `ConfigManager.species_layers` dictionary.

5. **Weighted Hair Selection** — Hair group codes are selected by weighting each code's variant count from `speciesModularParts.json`. More variants = more likely to be chosen.

### Bugs Found & Fixed

1. **Ghost duplicate rendering** — Compositing SpriteRenderers on layer 31 were visible to the main camera, showing a tiny duplicate character behind the RawImage. Fixed: `ExcludeCompositingLayerFromAllCameras()` strips layer 31 from all non-compositing cameras, plus compositing root is deactivated after render.

2. **Sprites not loading** — All 4,108 PNGs imported as Default texture type (`textureType: 0`), causing `Resources.Load<Sprite>()` to return null. Fixed: `SpriteImportFixer` editor tool bulk-updates to Sprite type with Bilinear filtering.

3. **Godot .import files cluttering Resources** — 4,108 `.import` files from Godot's import cache were copied alongside PNGs. Removed them and moved original `assets/` to `godot-archive/assets/`.

4. **Unity failing to open project** — The original Godot `assets/` directory in the project root caused Unity to fail importing `assets/data/config.meta`. Fixed by moving to `godot-archive/`.

### Commits

```
344ebfc6 Add paper doll display system design for Unity port
b89e6d08 Add Sprint 2 implementation plan: paper doll display system (11 tasks)
07b0eccd Migrate asset files to Unity Resources directory
34663dbd Add DisplayPiece data class for paper doll layers
7873faef Add ImageToken parser for species/equipment sprite path construction
f346e99e Add ColorManager with palette loading, hex parsing, and color field resolution
8d233aa5 Add DisplayDataRegistry with JSON parsing for species, equipment, and modular parts
ca105cd4 Add ImageResolver with sprite caching for display pipeline
5dc1b53f Add DisplayBuilder with 4-stage pipeline, species instance data, and tests
a3d8a986 Add Starquill.Display assembly definition and update references
8ec9d73e Add CharacterDisplay with RenderTexture compositing and bilinear output
13c67972 Add DisplayTestRunner for visual verification of paper doll rendering
b5e21c64 Add Sprint 2 paper doll display system to sprint review
d71366a5 Move Godot assets to godot-archive and add Unity meta files
0bd0b3a1 Fix ghost duplicate by excluding compositing layer from all cameras
2b8e06f9 Add editor script for bulk sprite import settings fix
443203c6 Remove Godot .import files, fix sprite imports, add DisplayTest scene
```

### Stats

- **Source files:** 12 (.cs) in Starquill.Display assembly + 1 Editor tool
- **Test files:** 3 (.cs), 26 test cases (75 total with Sprint 1)
- **Asset files:** 4,108 PNGs + 5 JSONs migrated to Resources/
- **Lines added:** ~2,200 (source + tests)
- **Commits:** 17

### Known Issues

- Tests cannot be run in headless batch mode on Arch Linux (same `ScriptableRuntimeReflectionSystem` hang as Sprint 1). Must use Unity Editor GUI.
- `DisplayDataRegistry` is a lazy singleton — not thread-safe, but fine for Unity's single-threaded model.
- Unity 6 `BuildProfileContext` NullReferenceException on Linux — internal Unity bug, non-blocking. Fixed by deleting `Library/BuildProfileContext.asset`.
- Input System conflict: Canvas EventSystem defaults to legacy `StandaloneInputModule`. Must replace with `InputSystemUIInputModule` on new scenes.

---

## Sprint 3: Explore Screen UI

**Date:** 2026-02-13
**Branch:** `unity-idle-clicker`
**Base:** `dev`
**Status:** Complete — scene rendering, visual verification confirmed in Unity Editor

### Goal

Build the Explore screen as a static UI layout with live paper doll rendering of the party, parallax scrolling background, placeholder verb cards, and navigation bar. No gameplay wiring — validates the full screen structure before Sprint 4 adds combat.

### Architecture

Canvas + TextMeshPro UI with a new `Starquill.UI` assembly (9th assembly definition). Parallax scrolling via RawImage UV offset manipulation with `Repeat` wrap mode textures. Four CharacterDisplay instances from Sprint 2 render party paper dolls into staggered RawImage slots. All content is placeholder data — no GameManager wiring. Scene hierarchy built programmatically via `ExploreSceneBuilder` editor script (Tools > Build Explore Scene) which creates all GameObjects, wires SerializeField references via reflection, and marks the scene dirty.

### Deliverables

#### UI System (7 files in `Starquill.UI` assembly)
| File | Purpose |
|------|---------|
| `Starquill.UI.asmdef` | Assembly definition: depends on Core, Data, Display, Unity.TextMeshPro |
| `ParallaxMath.cs` | Pure C# static class: UV width calculation, offset advance with wrapping |
| `ParallaxLayer.cs` | MonoBehaviour: RawImage UV scroll driven by ParallaxMath, SetTexture() API |
| `TopBarDisplay.cs` | Gold, fragments, wave info, quest level (single row, no level label) |
| `VerbBarDisplay.cs` | Verb card grid with colored backgrounds + cooldown text, max 3 cards |
| `BottomNavDisplay.cs` | 5 tab buttons with active/inactive highlighting |
| `ExploreSceneController.cs` | Scene orchestrator: creates CharacterDisplay instances, procedural parallax textures, populates all panels |

#### Editor Tooling (1 file)
| File | Purpose |
|------|---------|
| `ExploreSceneBuilder.cs` | Editor script (Tools menu): builds entire ExploreScene hierarchy programmatically with reflection-based SerializeField wiring |

#### Test Coverage (1 file, 8 tests)
| Test File | Tests | What's Covered |
|-----------|-------|----------------|
| `ParallaxMathTests.cs` | 8 | UV width ratio, same size, zero width, forward offset, wrap at 1, negative speed, negative wrap, zero texture width |

### Layout (Revised — 1080x1920 Reference)

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
- VerbBar: 130px (single row, 3 verb cards)
- BottomNav: 120px

### Design Decisions

1. **ExploreSceneBuilder Editor Script** — Individual MCP tool calls to build the scene were too slow. Created a comprehensive C# editor script that builds the entire hierarchy in one execution, using reflection (`SetPrivateField`) to wire all `[SerializeField]` references programmatically.

2. **Procedural Placeholder Textures** — Rather than requiring actual image files for development, `ExploreSceneController.SetupPlaceholderParallax()` generates striped/gradient textures at runtime. These demonstrate scrolling movement and will be replaced by artist-created assets.

3. **Single Row TopBar** — Originally 2 rows (160px), revised to 1 row (100px) containing gold, fragments, wave info, and quest level. The "Lv 34" label was redundant (quest level already shown) and removed.

4. **3-Card Verb Bar** — Originally 2 rows of 5 cards (240px), revised to 1 row of 3 cards (130px). This gives significantly more space to the combat area while keeping the most relevant verbs visible.

5. **Doubled Character/Enemy Sizes** — Initial sizes (180-220px party, 120x180 enemies) were too small in the combat area. Doubled to 360-440px party slots and 240x360 enemy silhouettes for better visual prominence.

### Bugs Found & Fixed

1. **DisplayBuilder constructor mismatch** — Plan specified `new DisplayBuilder()` but the actual constructor requires a `DisplayDataRegistry` parameter. Fixed to `new DisplayBuilder(registry)`.

2. **DisplayDataRegistry empty species** — `DisplayDataRegistry.Instance.Species` could be empty if `LoadAll()` was never called. Added guard: `if (registry.Species.Count == 0) registry.LoadAll()`.

3. **ParallaxLayer null texture** — `SetTexture(Texture2D)` could throw NullReferenceException if texture is null. Added `if (texture == null) return;` guard.

4. **TMP font NullReferenceException** — TextMeshProUGUI components created before TMP Essential Resources were imported had no default font. Resolved by importing TMP Essential Resources and rebuilding the scene.

5. **Duplicate scene file** — `save_scene` MCP command created `Assets/ExploreScene.unity` instead of `Assets/Scenes/ExploreScene.unity`. Cleaned up duplicate and re-saved to correct path.

### Commits

```
8be74769 Add Sprint 3 explore screen design doc and implementation plan
485447f1 Add Starquill.UI assembly definition and update test references
df394a92 Add ParallaxMath with UV scroll math and 8 tests
036e0e5d Add ParallaxLayer with RawImage UV scrolling
c0333f22 Add TopBarDisplay, VerbBarDisplay, and BottomNavDisplay UI components
b0bd9e6c Add ExploreSceneController with placeholder party, parallax, and UI
ac1cc361 Fix DisplayBuilder constructor, add registry LoadAll guard, and null checks
70d8c936 Clean up project structure: archive Godot files, organize docs
fc7d21f3 Add TextMesh Pro Essential Resources
4344ecad Build ExploreScene with full UI hierarchy and Coplay MCP integration
b200274e Revise Explore screen layout: larger characters, compact UI panels
```

### Stats

- **Source files:** 8 (.cs) in Starquill.UI assembly + 1 Editor tool
- **Test files:** 1 (.cs), 8 test cases (83 total with Sprint 1+2)
- **Lines added:** ~800 (source + tests)
- **Commits:** 11

### Known Issues

- Tests cannot be run in headless batch mode on Arch Linux (same issue as Sprint 1+2). Must use Unity Editor GUI.
- `Economy` assembly still empty — `EconomyConfig` lives in `Data` assembly.
- Input System `activeInputHandler` setting warning (`-1`) on editor startup — non-blocking.
- Coplay MCP assembly update timeout on first import — non-blocking, resolves on second compile.

### Open Questions for Sprint 4

- Wire GameManager tick loop to TopBarDisplay (gold/wave/quest updates)?
- VerbPool → VerbBarDisplay card tap-to-activate interaction model?
- Damage number float-up animation system (TextMeshPro or custom)?
- Should combat area respond to tap/drag for targeting, or auto-target only?
- **Path indicator bar** — a horizontal progress bar below the TopBar that serves dual purpose:
  - **Combat mode:** Visual wave progress (e.g. wave 3/5 fills as enemies are cleared)
  - **Explore mode:** Travel percentage showing party moving along a path toward a guaranteed encounter/location; if a random event hasn't triggered by arrival, the destination encounter fires. Design and implement in Sprint 5 (Full Explore Screen).

---

## Sprint 4: [Planned — Minimum Viable Combat]

*Wire GameManager to Explore screen for live auto-combat, verb activation, wave progression.*

---

## Sprint 5: [Planned — Full Explore Screen]

*Add damage numbers, floating icons, boost indicators, bottom nav screen switching, real art assets, and path indicator bar (dual-mode progress for combat waves and exploration travel).*
