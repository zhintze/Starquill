# Paper Doll Display System — Unity Port Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Port Starquill's layered paper doll character rendering system from Godot to Unity, using RenderTexture compositing for clean scaling across all mobile resolutions.

**Architecture:** A 4-stage DisplayBuilder pipeline produces sorted DisplayPiece lists from CharacterInstance data. A compositing camera renders all layers into a single RenderTexture at native resolution. The composited texture is displayed via UI.RawImage with bilinear filtering for smooth scaling. All logic except the final renderer is plain C# with no Unity dependencies, enabling full EditMode test coverage.

**Tech Stack:** Unity 6 (Canvas + SpriteRenderer for compositing, RawImage for display), C# with JSON data loading via Resources.Load, NUnit for testing.

---

## File Structure Overview

```
Assets/
├── Scripts/
│   └── Display/                        # New assembly: Starquill.Display
│       ├── Starquill.Display.asmdef    # Depends on: Core, Data
│       ├── DisplayPiece.cs             # Plain C# class: layer, sprite path, tintColor, offset, scale, rotation, flipH
│       ├── ImageToken.cs               # Parses token strings -> sprite paths (static/modular full/modular group)
│       ├── ImageResolver.cs            # Token -> Sprite loading with Dictionary cache
│       ├── DisplayBuilder.cs           # CharacterInstance -> List<DisplayPiece> (4-stage pipeline)
│       ├── DisplayDataRegistry.cs      # Loads + stores parsed JSON display data (species parts, layer mappings, modular counts)
│       ├── ColorManager.cs             # Palette loading, hex->Color, GetRandomColor()
│       └── CharacterDisplay.cs         # MonoBehaviour: compositing camera + RenderTexture + RawImage output
│
├── Resources/
│   ├── Data/                           # JSON files (copied from assets/data/)
│   │   ├── species.json
│   │   ├── equipment.json
│   │   ├── weapons.json
│   │   ├── color_palettes.json
│   │   └── speciesModularParts.json
│   └── Images/                         # Sprite PNGs (copied from assets/images/)
│       ├── species/
│       ├── equipment/
│       └── weapons/
│
├── Tests/EditMode/
│   └── Display/
│       ├── ImageTokenTests.cs
│       ├── DisplayBuilderTests.cs
│       └── ColorManagerTests.cs
```

---

## 1. Render Pipeline Overview

The system follows the same pipeline as the original Godot implementation:

```
CharacterInstance (species + equipment)
        |
        v
  DisplayBuilder.Build()
        |
  Stage 1: Build species pieces (body parts, hair, eyes, facial details)
  Stage 2: Build equipment pieces (armor, weapons, accessories)
  Stage 3: Filter hidden layers (helmets hiding hair, hat deduplication)
  Stage 4: Sort by layer number ascending
        |
        v
  List<DisplayPiece>  (sorted, filtered, colored)
        |
        v
  CharacterDisplay.SetPieces()
        |
  Compositing camera renders all pieces into RenderTexture (200x200 or 400x400)
        |
        v
  UI.RawImage displays the single composited texture (bilinear filtering)
```

**Key principle:** Everything before `CharacterDisplay` is plain C# with no Unity rendering dependencies. This makes the pipeline fully unit-testable in EditMode.

---

## 2. DisplayPiece

Plain C# data class. No Unity dependencies except `UnityEngine.Color` for tint.

```
DisplayPiece:
  layer: int              # Sort key (0-999), lower = further back
  spritePath: string      # Resource path to load sprite (e.g., "Images/species/h02-0043-092")
  tintColor: Color        # Applied via SpriteRenderer.color (white = no tint)
  offset: Vector2         # Position offset (primarily for weapons)
  scale: Vector2          # Scale multiplier (default 1,1)
  rotation: float         # Rotation in degrees (primarily for offhand weapons)
  flipH: bool             # Horizontal flip (offhand weapons)
```

---

## 3. Image Token System

Three token formats exist for species body parts, inherited from the Godot system:

### Static Tokens
Format: `####-###` (e.g., `0001-016`)
- Fixed image number + fixed layer number
- Resolves to: `Images/species/0001-016`
- Used for: standard body parts like torso base, legs

### Modular Full Tokens
Format: `a##-####-###` (e.g., `f01-0043-087`)
- Type code + image number + layer number
- Resolves to: `Images/species/f01-0043-087`
- Used for: facial features, specific hair variants

### Modular Group Tokens
Format: `a##` (e.g., `h02`)
- Type code only — expands to multiple pieces via layer mapping
- Layer mapping example: `h02` maps to layers [92, 128]
- Image number is picked randomly once per SpeciesInstance and stored persistently in `modularImageNums` dictionary
- With image num 0043: resolves to `Images/species/h02-0043-092` and `Images/species/h02-0043-128`
- Variant count comes from `speciesModularParts.json` (e.g., `h02` has 83 variants)

### Equipment Image Naming
- **Armor/accessories:** `{type}-{itemNum:04d}-{layer:03d}` (e.g., `hd01-0062-130`)
  - Path: `Images/equipment/hd01-0062-130`
- **Weapons:** `{type}-{layer:03d}-{variant:04d}` (e.g., `w01-164-0023`)
  - Path: `Images/weapons/w01-164-0023`
  - Note reversed order vs equipment: layer comes before variant for weapons

`ImageToken.Parse(string token)` detects the format by pattern matching and returns a parsed structure with the token type and components.

---

## 4. ImageResolver

Handles sprite loading with caching.

- `Sprite Resolve(string spritePath)` — loads via `Resources.Load<Sprite>(path)`, caches in `Dictionary<string, Sprite>`
- Same sprite is never loaded twice across any number of characters
- Returns null for missing sprites (logged as warning, rendering skips that piece)

---

## 5. DisplayBuilder — 4-Stage Pipeline

### Stage 1: Build Species Pieces

Iterate the species definition's body part token list. For each token:
1. Parse with `ImageToken.Parse()`
2. If static or modular full: create one `DisplayPiece` with the parsed layer
3. If modular group: look up layer mapping, create one `DisplayPiece` per mapped layer using the instance's persistent image number
4. Assign tint color based on layer category:
   - Body layers: `skinColor` (or `skinVarianceColor` if the layer has a variance override)
   - Hair layers: `hairColor`
   - Eye layers: `eyesColor`
   - Facial detail layers: `facialDetailColor`

### Stage 2: Build Equipment Pieces

For each equipped item on the character:
1. Look up equipment data by item ID (layer codes, image naming)
2. For each layer code: construct the image path, create a `DisplayPiece`
3. Assign tint: use `baseColor` for standard layers, `varianceColors[layerCode]` for layers in `layer_color_variance`
4. Weapons in offhand slot: apply rotation offset, position offset, horizontal flip

### Stage 3: Filter Hidden Layers

1. Collect all `hidden_layers` from equipped items into a `HashSet<int>`
2. Remove species pieces whose layer number is in the hidden set
3. Hat deduplication: if multiple items from `hd01-hd08` are equipped, only the last one survives (latest wins)

### Stage 4: Sort and Return

Merge remaining species pieces + equipment pieces. Sort by `layer` ascending (lower numbers render first / further back). Return `List<DisplayPiece>`.

---

## 6. DisplayDataRegistry

Singleton that loads and stores all display-related JSON data at startup. Replaces the display-related responsibilities of Godot's ConfigManager.

**Data loaded from JSON:**

| Source File | Data Structure | Contents |
|-------------|---------------|----------|
| `species.json` | `SpeciesDisplayData` | Species ID, body part token list, skin color options, modular part codes |
| `equipment.json` | `EquipmentDisplayData` | Item ID, layer codes with layer numbers, hidden_layers array, layer_color_variance |
| `weapons.json` | `WeaponDisplayData` | Weapon type, layer codes, variant counts per layer, offhand transforms |
| `speciesModularParts.json` | `Dictionary<string, int>` | Modular type code to variant count (e.g., `f01` -> 246) |
| `color_palettes.json` | `Dictionary<string, Color[]>` | Named palettes -> color arrays |

**Loading:** Uses `Resources.Load<TextAsset>("Data/filename")` and deserializes with a JSON parser. Runs once at startup.

**Relationship to ScriptableObjects:** The Sprint 1 `SpeciesDefinition` SO holds gameplay data (base stats, ability). `DisplayDataRegistry` holds visual data (body parts, layers). They share `speciesId` as a join key. JSON is the source of truth for visuals; SOs are the source of truth for gameplay.

---

## 7. ColorManager

Loads color palettes from `color_palettes.json` and provides lookup APIs.

**API:**
- `LoadPalettes()` — parse JSON, store as `Dictionary<string, Color[]>`
- `Color GetRandomColor(string paletteName)` — pick a random color from a named palette
- `Color[] GetPalette(string paletteName)` — return the full palette array
- `Color ParseHex(string hex)` — convert hex string to Unity Color

**Palette categories:** Palettes are named (e.g., `"skin_default"`, `"hair_keyword"`, `"eyes_default"`, `"equipment_metal"`). Species and equipment instances pick random colors from appropriate palettes at creation time.

**Color application:** Unity's `SpriteRenderer.color` multiplies the sprite's pixel colors by the tint color. White `(1,1,1,1)` means no tint. This is the same behavior as Godot's `modulate` property.

---

## 8. CharacterDisplay — RenderTexture Compositing

The only MonoBehaviour in the system. Handles the Unity-specific rendering.

### Why RenderTexture

The character sprites are drawn on 200x200 canvases (pencil-drawn art, not pixel art). Scaling these individually to fit mobile screens can cause distortion. By compositing all layers into a single texture first, we:
- Keep layer alignment pixel-perfect during composition
- Scale only the final composited image to screen size
- Use bilinear filtering for smooth scaling (appropriate for pencil-drawn art style)
- Get a massive performance win: 1 draw call per character instead of 30-50

### Compositing Architecture

**Shared compositing camera:** A single `Camera` on a dedicated Unity layer (e.g., layer 31 "Compositing"). It renders manually via `Camera.Render()`, not every frame.

**Per-character RenderTexture:** Each `CharacterDisplay` owns a `RenderTexture` (200x200 or 400x400 for 2x quality). Filter mode set to `Bilinear`.

**Rebuild flow:**
1. `CharacterDisplay.SetPieces(List<DisplayPiece> pieces)` is called
2. Create/reuse child `SpriteRenderer` objects on the compositing layer, one per piece
3. For each piece: assign sprite (via `ImageResolver`), set `sortingOrder = piece.layer`, set `color = piece.tintColor`, apply offset/scale/rotation/flip
4. Position the compositing camera to frame this character's children
5. Set camera's target to this character's `RenderTexture`
6. Call `Camera.Render()` — single manual render
7. Deactivate compositing children (invisible until next rebuild)
8. The `RawImage` (or `SpriteRenderer` on main layer) displays the `RenderTexture`

**Rendering 4 characters:** Sequential compositing. Each character's pieces are activated, rendered, deactivated in turn. Total: 4 manual camera renders per full party rebuild. Since rebuilds only happen on equipment change (not per-frame), this is cheap.

### Display Output

A `UI.RawImage` component on the game's Canvas displays the `RenderTexture`. The RawImage scales freely within its layout container. With bilinear filtering, the pencil-drawn art scales smoothly to any size — small thumbnails, party screen portraits, or full Explore screen characters.

### Re-render Triggers

- Equipment change on any character
- Species instance creation (initial render)
- Color change (if reroll is added later)
- NOT per-frame — the RenderTexture persists as a cached image between rebuilds

---

## 9. Testing Strategy

### EditMode Tests (no scene required)

**ImageTokenTests.cs:**
- Parse static token `0001-016` — verify type, image num, layer
- Parse modular full token `f01-0043-087` — verify type code, image num, layer
- Parse modular group token `h02` — verify type code, no layer/image
- Construct sprite path for each format
- Invalid token handling (empty string, malformed)

**DisplayBuilderTests.cs:**
- Build with species only — verify piece count matches token count, layers sorted ascending
- Build with equipment — verify equipment pieces added with correct layers
- Hidden layers — equip item with `hidden_layers`, verify species pieces removed
- Hat deduplication — equip two hd01-hd08 items, verify only last survives
- Color assignment — verify skin, hair, eye, equipment colors applied to correct pieces
- Modular group expansion — verify one group token expands to multiple pieces per layer mapping

**ColorManagerTests.cs:**
- Load palette JSON — verify palette count and color count
- Hex parsing — verify `#FF0000` becomes red Color
- GetRandomColor — verify returned color is from the named palette
- Missing palette — verify null/default handling

### PlayMode / Manual Tests (deferred to integration)
- CharacterDisplay produces non-empty RenderTexture
- Full pipeline: CharacterInstance -> DisplayBuilder -> CharacterDisplay -> visible output
- Bilinear scaling looks correct at thumbnail, medium, and full sizes

---

## 10. Assembly Dependencies

```
Starquill.Display depends on:
  - Starquill.Core (StatType, EquipmentSlot enums)
  - Starquill.Data (SpeciesDefinition, EquipmentDefinition)

Starquill.Managers updated to also depend on:
  - Starquill.Display (GameManager triggers rebuilds)

EditModeTests updated to also reference:
  - Starquill.Display
```

---

## 11. Asset Migration

The existing image and data files move into Unity's `Resources/` folder:

| Source (current) | Destination (Unity) |
|-----------------|-------------------|
| `assets/data/species.json` | `Assets/Resources/Data/species.json` |
| `assets/data/equipment.json` | `Assets/Resources/Data/equipment.json` |
| `assets/data/weapons.json` | `Assets/Resources/Data/weapons.json` |
| `assets/data/color_palettes.json` | `Assets/Resources/Data/color_palettes.json` |
| `assets/data/speciesModularParts.json` | `Assets/Resources/Data/speciesModularParts.json` |
| `assets/images/species/` | `Assets/Resources/Images/species/` |
| `assets/images/equipment/` | `Assets/Resources/Images/equipment/` |
| `assets/images/weapons/` | `Assets/Resources/Images/weapons/` |

Originals remain in `assets/` for reference. Unity copies are the runtime source.

**Sprite import settings:** All sprite PNGs should be imported with:
- Texture Type: Sprite (2D)
- Filter Mode: Bilinear
- Compression: None (preserve pencil-drawn detail)
- Max Size: 256 (200x200 native fits comfortably)
- Pixels Per Unit: 100 (default)
