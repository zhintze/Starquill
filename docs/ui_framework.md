# Starquill UI Framework Documentation

## Overview

Starquill uses a custom programmatic UI framework built on top of Godot 4.x's Control nodes. The framework emphasizes:

- **Code-first construction**: UI is built programmatically in GDScript, not in .tscn scene files
- **Centralized theming**: All visual properties flow from a single theme resource
- **Mobile-first design**: Touch targets, safe areas, and responsive layouts
- **Signal-based communication**: Loose coupling between UI components

---

## Core Principles

### Single Source of Truth for Spacing
All spacing and padding values come from **UITheme tokens only**. UIConstants contains only non-style rules (touch targets, slot sizes, ratios, z-index). See `docs/ui_layout_rules.md` for the complete spacing grammar.

### MarginContainer Padding Pattern
Padding is applied via MarginContainer, not StyleBox content margins. StyleBox `content_margin_*` values are always 0. This ensures predictable, non-compounding padding.

```gdscript
# Standard padding pattern
var panel := PanelContainer.new()
var padding := UIThemeManager.make_margin_container("panel")
panel.add_child(padding)
padding.add_child(content)
```

### Screen Coordinator Pattern
Screens use a coordinator to mediate sibling-to-sibling panel communication. Panels never call each other directly.

```
PartyMenuScreen
├── PartyMenuCoordinator (wires signals between panels)
├── CharacterPanel (emits selection_changed)
└── InventoryPanel (emits selection_changed)
```

### Component Boundaries
- **Parent-child**: Signals only (child emits, parent listens)
- **Sibling-sibling**: Coordinator mediates
- **Global events**: EventBus with `ui.` prefix (rare)

See `docs/ui_ai_rules.md` for complete rules.

---

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                      SCREENS LAYER                          │
│   PartyMenuScreen, MainMenu, SettingsMenu, etc.             │
│   Full-screen containers that compose panels and components │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                      PANELS LAYER                           │
│   CharacterPanel, InventoryPanel, StatsPanel                │
│   Self-contained UI sections with specific functionality    │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    COMPONENTS LAYER                         │
│   ThemedButton, IconButton, DragDropSlot, EquipmentSlot,    │
│   InventorySlot, BookmarkTabBar, ScrollableGrid, BasePanel  │
│   Reusable, styled UI building blocks                       │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                       CORE LAYER                            │
│   UITheme, UIConstants, UIScaler                            │
│   Theme definitions, constants, and utility functions       │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    AUTOLOAD SINGLETONS                      │
│   UIThemeManager, UIManager, DragDropManager                │
│   Global state and coordination                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Autoload Singletons

### UIThemeManager (`autoload/ui_theme_manager.gd`)

**Purpose**: Global singleton for theme management and access.

**Key Responsibilities**:
- Loads and caches the active UITheme resource
- Provides helper methods for accessing theme properties
- Emits `theme_changed` signal when theme switches
- Factory methods for creating StyleBoxFlat instances

**Usage Pattern**:
```gdscript
# Get the current theme
var theme := UIThemeManager.get_theme()

# Access colors directly
var text_color := UIThemeManager.get_text_color()
var bg_color := UIThemeManager.get_bg_primary()

# Create StyleBoxes
var panel_style := UIThemeManager.create_panel_stylebox()
var slot_style := UIThemeManager.create_drag_drop_slot_stylebox("filled")
```

**Available StyleBox Factories**:
| Method | States | Use Case |
|--------|--------|----------|
| `create_panel_stylebox()` | - | Main panel backgrounds |
| `create_button_stylebox(state)` | normal, hover, pressed, disabled | Button states |
| `create_slot_stylebox(filled, highlighted)` | - | Simple slots |
| `create_drag_drop_slot_stylebox(state)` | empty, filled, valid_drop, invalid_drop, highlighted, dragging_from | Drag/drop slots |
| `create_tab_stylebox(state)` | active, inactive, hover | Tab buttons |
| `create_bg_stylebox(type)` | primary, secondary, overlay | Background panels |

---

### UIManager (`autoload/ui_manager.gd`)

**Purpose**: Modal menu stack management.

**Key Responsibilities**:
- Maintains a stack of open menus
- Handles ESC key to close top menu
- Coordinates with GameManager for game state (gameplay vs menu mode)
- Emits events via EventBus

**Usage Pattern**:
```gdscript
# Open a menu
var menu := UIManager.open_menu(party_menu_scene)

# Close menus
UIManager.close_top_menu()
UIManager.close_all_menus()

# Query state
if UIManager.is_menu_open():
    var active := UIManager.get_active_menu()
```

**Rendering Layer**: Uses `CanvasLayer` with `layer = 100` to render above game world.

---

### DragDropManager (`autoload/drag_drop_manager.gd`)

**Purpose**: Centralized drag-and-drop operation management.

**Key Responsibilities**:
- Tracks active drag state (source slot, data, preview)
- Manages drag preview positioning and rendering
- Discovers valid drop targets via duck typing
- Validates drops and coordinates swaps between slots

**Signals**:
```gdscript
signal drag_started(source_slot: Control, data: Variant)
signal drag_ended(source_slot: Control, dropped: bool)
signal drag_cancelled(source_slot: Control)
signal drop_completed(source_slot: Control, target_slot: Control, data: Variant)
```

**Duck Typing Protocol**: A Control is a valid drop target if it has:
- `can_accept_drop(data: Variant) -> bool`
- `handle_drop(data: Variant, source_slot) -> bool`

**Rendering Layer**: Uses `CanvasLayer` with `layer = 200` (above UIManager).

---

## Core Infrastructure

### UITheme (`scripts/ui/core/ui_theme.gd`)

**Purpose**: Resource class defining all visual properties.

**Structure**:
```gdscript
extends Resource
class_name UITheme

# Color Palette
@export var primary_color: Color      # Main accent (parchment/tan)
@export var secondary_color: Color    # Secondary (dark brown)
@export var accent_color: Color       # Highlights (gold)
@export var background_color: Color   # Base background
@export var panel_color: Color        # Panel fills

# Text Colors
@export var text_color: Color         # Primary text
@export var text_color_secondary: Color
@export var text_color_disabled: Color
@export var text_color_highlight: Color

# State Colors
@export var hover_color: Color
@export var pressed_color: Color
@export var selected_color: Color
@export var disabled_color: Color

# Feedback Colors
@export var success_color: Color
@export var warning_color: Color
@export var error_color: Color
@export var info_color: Color

# Semantic Background Colors
@export var bg_primary: Color         # Main panel backgrounds
@export var bg_secondary: Color       # Inner containers
@export var bg_overlay: Color         # Modal overlays

# Border Colors
@export var border_panel: Color       # Panel edges
@export var border_slot: Color        # Slot edges
@export var border_highlight: Color   # Selected/highlighted

# Button Colors
@export var btn_normal: Color
@export var btn_hover: Color
@export var btn_pressed: Color
@export var btn_disabled: Color
@export var btn_text: Color
@export var btn_text_disabled: Color

# Tab Colors
@export var tab_active: Color
@export var tab_inactive: Color
@export var tab_hover: Color
@export var tab_border: Color

# Slot Colors
@export var slot_empty_color: Color
@export var slot_filled_color: Color
@export var slot_valid_drop_color: Color
@export var slot_invalid_drop_color: Color
@export var slot_border_color: Color
@export var slot_border_color_highlight: Color

# Fonts
@export var font_header: Font
@export var font_body: Font
@export var font_button: Font
@export var font_small: Font

# Font Sizes
@export var font_size_header: int = 24
@export var font_size_subheader: int = 18
@export var font_size_body: int = 14
@export var font_size_button: int = 16
@export var font_size_small: int = 12

# Spacing
@export var spacing_tiny: int = 4
@export var spacing_small: int = 8
@export var spacing_medium: int = 16
@export var spacing_large: int = 24
@export var spacing_huge: int = 32

# Padding
@export var padding_panel: int = 16
@export var padding_container: int = 12
@export var padding_button: int = 8

# Panel Styling
@export var panel_border_width: int = 4
@export var panel_corner_radius: int = 8
@export var panel_shadow_size: int = 4
@export var panel_shadow_color: Color

# Button Styling
@export var button_border_width: int = 2
@export var button_corner_radius: int = 6
@export var button_padding_horizontal: int = 16
@export var button_padding_vertical: int = 8
@export var button_hover_scale: float = 1.05

# Animation
@export var transition_duration: float = 0.2
@export var hover_duration: float = 0.1
@export var fade_duration: float = 0.3
```

**Default Theme Location**: `resources/themes/default_theme.tres`

---

### UIConstants (`scripts/ui/core/ui_constants.gd`)

**Purpose**: Static constants for sizing, spacing, and layout.

**Reference Dimensions**:
```gdscript
const REFERENCE_WIDTH: float = 360.0   # Android minimum dp
const REFERENCE_HEIGHT: float = 640.0
```

**Touch Targets** (per platform guidelines):
```gdscript
const MIN_TOUCH_TARGET: float = 48.0        # Android minimum
const MIN_TOUCH_TARGET_SMALL: float = 44.0  # iOS minimum
```

**Slot Sizes**:
```gdscript
const SLOT_SIZE_SMALL: int = 48
const SLOT_SIZE_MEDIUM: int = 64
const SLOT_SIZE_LARGE: int = 80
const SLOT_SIZE_ICON: int = 128   # Current large slot size
```

**Spacing Scale** (4px grid):
```gdscript
const SPACING_NONE: int = 0
const SPACING_XXXS: int = 2
const SPACING_XXS: int = 4
const SPACING_XS: int = 8
const SPACING_SM: int = 12
const SPACING_MD: int = 16
const SPACING_LG: int = 24
const SPACING_XL: int = 32
const SPACING_XXL: int = 48
const SPACING_XXXL: int = 64
```

**Layout Ratios**:
```gdscript
const CHARACTER_PANEL_WIDTH_RATIO: float = 0.4  # 40% of screen
const CONTENT_PANEL_WIDTH_RATIO: float = 0.6    # 60% of screen
const INVENTORY_COLUMNS: int = 2                # Fixed column count
```

---

### UIScaler (`scripts/ui/core/ui_scaler.gd`)

**Purpose**: Responsive scaling utilities for mobile.

**Key Functions**:
```gdscript
# Get scale factor based on reference dimensions
static func get_scale_factor() -> float

# Scale values
static func scale(value: float) -> float
static func scale_vec2(value: Vector2) -> Vector2
static func scale_int(value: int) -> int

# Safe area handling
static func get_safe_area() -> Rect2
static func get_safe_top() -> float
static func get_safe_bottom() -> float
static func get_safe_left() -> float
static func get_safe_right() -> float

# Responsive sizing
static func responsive(min_val: float, max_val: float) -> float

# Touch target enforcement
static func ensure_touch_size(control: Control) -> void

# Device detection
static func is_tablet() -> bool
static func is_landscape() -> bool
```

---

## Component Library

### ThemedButton (`scripts/ui/components/themed_button.gd`)

**Extends**: `Button`

**Purpose**: Base button with programmatic styling from UITheme.

**Button Styles**:
```gdscript
enum ButtonStyle {
    PRIMARY,    # Main action buttons - uses btn_* colors
    SECONDARY,  # Secondary actions - uses bg_secondary
    GHOST,      # Minimal styling (transparent until hover)
    DANGER      # Destructive actions (uses error_color)
}
```

**Features**:
- Hover scale animation (`enable_hover_scale`, `hover_scale_amount`)
- Press feedback animation (`enable_press_feedback`)
- Optional audio (`click_sound`, `hover_sound`)
- Automatic theme change response

**Usage**:
```gdscript
var btn := ThemedButton.create("Save", ThemedButton.ButtonStyle.PRIMARY)
btn.pressed.connect(_on_save_pressed)
add_child(btn)
```

---

### IconButton (`scripts/ui/components/icon_button.gd`)

**Extends**: `ThemedButton`

**Purpose**: Button with icon and optional text.

**Icon Positions**:
```gdscript
enum IconPosition {
    LEFT,   # Icon left of text
    RIGHT,  # Icon right of text
    TOP,    # Icon above text
    ONLY    # Icon only, no text
}
```

**Preset Icons**:
```gdscript
enum PresetIcon {
    NONE, ARROW_LEFT, ARROW_RIGHT, CLOSE, CHECK,
    PLUS, MINUS, TRASH, SORT, INFO
}
```

**Usage**:
```gdscript
# Using preset icon
var close_btn := IconButton.new()
close_btn.preset_icon = IconButton.PresetIcon.CLOSE
close_btn.icon_position = IconButton.IconPosition.ONLY
close_btn.button_style = ThemedButton.ButtonStyle.GHOST

# Using custom icon
var custom_btn := IconButton.create_with_icon(
    my_texture,
    "Label",
    ThemedButton.ButtonStyle.PRIMARY,
    IconButton.IconPosition.LEFT
)
```

---

### BasePanel (`scripts/ui/components/base_panel.gd`)

**Extends**: `NinePatchRect`

**Purpose**: Themed panel with optional paper shader.

**Panel Styles**:
```gdscript
enum PanelStyle {
    PRIMARY,    # Main containers with full border
    SECONDARY,  # Inner panels with lighter styling
    OVERLAY     # Modal overlay background
}
```

**Features**:
- 9-slice texture support (`panel_9slice.png`, `panel_inner_9slice.png`)
- Optional paper grain shader
- Configurable vignette and grain intensity

**Usage**:
```gdscript
var panel := BasePanel.create(BasePanel.PanelStyle.PRIMARY, true)
panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
add_child(panel)
```

---

### DragDropSlot (`scripts/ui/components/drag_drop_slot.gd`)

**Extends**: `PanelContainer`

**Purpose**: Base class for draggable/droppable slots.

**Slot States**:
```gdscript
enum SlotState {
    EMPTY,              # No item
    FILLED,             # Has item
    DRAG_HOVER_VALID,   # Valid drop target during drag
    DRAG_HOVER_INVALID, # Invalid drop target during drag
    HIGHLIGHTED,        # Selected/highlighted
    DISABLED,           # Cannot interact
    DRAGGING_FROM       # Source slot during drag (recessed appearance)
}
```

**Signals**:
```gdscript
signal drag_started(slot: DragDropSlot, data: Variant)
signal drag_ended(slot: DragDropSlot)
signal item_dropped(slot: DragDropSlot, data: Variant, source_slot: DragDropSlot)
signal drop_rejected(slot: DragDropSlot, data: Variant, reason: String)
signal slot_clicked(slot: DragDropSlot)
signal slot_hovered(slot: DragDropSlot)
signal slot_unhovered(slot: DragDropSlot)
```

**Long Press Drag**: Mobile-friendly drag initiation:
- Press and hold for `long_press_duration` (default 0.4s) to start drag
- Moving finger more than `drag_threshold` (10px) cancels long press (allows scrolling)

**Internal Structure**:
```
DragDropSlot (PanelContainer)
├── IconRect (TextureRect) - Item icon display
└── HighlightRect (ColorRect) - Visual feedback overlay
```

**Usage**:
```gdscript
var slot := DragDropSlot.create(Vector2(64, 64))
slot.set_slot_data(my_item)
slot.slot_clicked.connect(_on_slot_clicked)
```

---

### EquipmentSlot (`scripts/ui/components/equipment_slot.gd`)

**Extends**: `DragDropSlot`

**Purpose**: Specialized slot for character equipment with type validation.

**Slot Types**:
```gdscript
enum SlotType {
    HEAD, TORSO, ARMS, LEGS, FEET,
    MAIN_HAND, OFF_HAND,
    MISC_1, MISC_2, MISC_3, MISC_4
}
```

**Equipment Type Validation**:
- Validates equipment prefix against slot type (e.g., "hd" for HEAD, "tr" for TORSO)
- MISC slots accept any equipment type
- Weapon slots accept "w0", "w1" prefixes

**Swap Logic**:
1. Dragged item goes to target slot (validated by `can_accept_drop`)
2. If target had item AND compatible with source -> swap
3. If target had item AND NOT compatible with source -> goes to inventory
4. If target was empty -> source slot cleared

**Additional Signals**:
```gdscript
signal equipment_changed(slot: EquipmentSlot, old_equipment: Variant, new_equipment: Variant)
signal unequip_requested(slot: EquipmentSlot)  # Right-click or long-press
```

---

### InventorySlot (`scripts/ui/components/inventory_slot.gd`)

**Extends**: `DragDropSlot`

**Purpose**: Slot for inventory items.

**Features**:
- Integrates with PlayerData for inventory management
- Accepts any item type
- No slot type restrictions

---

### BookmarkTabBar (`scripts/ui/components/bookmark_tab_bar.gd`)

**Extends**: `HBoxContainer`

**Purpose**: Horizontal tab bar with medieval bookmark styling.

**Features**:
- Active tab appears "forward" with full color
- Inactive tabs are recessed
- Slight overlap for bookmark effect (`separation: -2`)
- Tabs fill width evenly (`SIZE_EXPAND_FILL`)

**Signals**:
```gdscript
signal tab_changed(index: int)
signal tab_hovered(index: int)
```

**Usage**:
```gdscript
var tabs := BookmarkTabBar.create(["Inventory", "Stats", "Journal", "Map"])
tabs.tab_changed.connect(_on_tab_changed)
add_child(tabs)
```

---

### ScrollableGrid (`scripts/ui/components/scrollable_grid.gd`)

**Extends**: `ScrollContainer`

**Purpose**: Scrollable container with auto-generating slot grid.

**Slot Types**:
```gdscript
enum SlotType {
    INVENTORY,  # Creates InventorySlot instances
    EQUIPMENT,  # Creates EquipmentSlot instances
    GENERIC     # Creates DragDropSlot instances
}
```

**Features**:
- Auto-fit columns based on container width (when `columns = 0`)
- Fixed column count support
- Configurable slot size and spacing
- Horizontal scroll disabled, vertical auto

**Signals**:
```gdscript
signal slot_clicked(index: int, slot: DragDropSlot)
signal slot_hovered(index: int, slot: DragDropSlot)
signal slot_unhovered(index: int, slot: DragDropSlot)
signal item_dropped(target_index: int, target_slot: DragDropSlot, source_slot: DragDropSlot)
signal grid_ready
```

**Usage**:
```gdscript
var grid := ScrollableGrid.create(24, ScrollableGrid.SlotType.INVENTORY, Vector2(128, 128))
grid.set_items(inventory_array)
add_child(grid)
```

---

## Layout Patterns

### Container Hierarchy

UI is built using nested Godot containers:

```gdscript
# Typical screen structure
PartyMenuScreen (Control - PRESET_FULL_RECT)
├── Background (ColorRect - bg_overlay)
├── SafeMargin (MarginContainer - 16px all sides)
│   └── MainPanel (BasePanel/NinePatchRect)
│       └── PanelMargin (MarginContainer - padding_panel)
│           └── MainHBox (HBoxContainer)
│               ├── CharacterPanel (40% stretch_ratio)
│               └── ContentContainer (60% stretch_ratio)
└── CloseButton (IconButton - z_index: 100, absolute position)
```

### Size Flags Pattern

```gdscript
# Fill available space
control.size_flags_horizontal = Control.SIZE_EXPAND_FILL
control.size_flags_vertical = Control.SIZE_EXPAND_FILL

# Proportional sizing within HBox/VBox
left_panel.size_flags_stretch_ratio = 0.4   # 40%
right_panel.size_flags_stretch_ratio = 0.6  # 60%
```

### Margin/Padding Pattern

```gdscript
# Using MarginContainer for padding
var margin := MarginContainer.new()
margin.add_theme_constant_override("margin_left", theme.padding_panel)
margin.add_theme_constant_override("margin_right", theme.padding_panel)
margin.add_theme_constant_override("margin_top", theme.padding_panel)
margin.add_theme_constant_override("margin_bottom", theme.padding_panel)
```

### Anchors and Positioning

```gdscript
# Fill parent (most common)
control.set_anchors_preset(Control.PRESET_FULL_RECT)

# Absolute positioning (for overlays like close button)
button.set_anchors_preset(Control.PRESET_TOP_LEFT)
button.position = Vector2(20, 20)
button.z_index = 100  # Ensure above siblings
```

---

## Styling Pattern

### StyleBoxFlat Construction

All visual styling uses `StyleBoxFlat` created via theme methods:

```gdscript
func _apply_style() -> void:
    var theme := UIThemeManager.get_theme()

    # Create and configure StyleBox
    var stylebox := StyleBoxFlat.new()
    stylebox.bg_color = theme.bg_secondary
    stylebox.border_color = theme.border_panel

    # Border widths
    stylebox.border_width_left = theme.panel_border_width
    stylebox.border_width_right = theme.panel_border_width
    stylebox.border_width_top = theme.panel_border_width
    stylebox.border_width_bottom = theme.panel_border_width

    # Corner radii
    stylebox.set_corner_radius_all(theme.panel_corner_radius)
    # Or individually:
    stylebox.corner_radius_top_left = 6
    stylebox.corner_radius_top_right = 6

    # Content margins (padding inside the box)
    stylebox.content_margin_left = theme.padding_panel
    stylebox.content_margin_right = theme.padding_panel
    stylebox.content_margin_top = theme.padding_panel
    stylebox.content_margin_bottom = theme.padding_panel

    # Shadow
    stylebox.shadow_size = theme.panel_shadow_size
    stylebox.shadow_color = theme.panel_shadow_color
    stylebox.shadow_offset = Vector2(2, 2)

    # Apply to control
    panel_container.add_theme_stylebox_override("panel", stylebox)
```

### Theme Change Response

Components should listen for theme changes:

```gdscript
func _ready() -> void:
    _apply_style()
    if UIThemeManager:
        UIThemeManager.theme_changed.connect(_on_theme_changed)

func _on_theme_changed(_new_theme: Resource) -> void:
    _apply_style()
```

---

## Signal Communication Pattern

### Parent-Child Signal Flow

```gdscript
# In PartyMenuScreen
func _connect_signals() -> void:
    _tab_bar.tab_changed.connect(_on_tab_changed)
    _character_panel.character_changed.connect(_on_character_changed)
    _character_panel.equipment_changed.connect(_on_equipment_changed)
    _character_panel.unequip_requested.connect(_on_unequip_requested)

    if _inventory_panel.has_signal("item_selected"):
        _inventory_panel.item_selected.connect(_on_inventory_item_selected)
```

### Cross-Panel Coordination

```gdscript
# Clear selection in one panel when another is selected
func _on_equipment_slot_clicked(slot: EquipmentSlot) -> void:
    if _inventory_panel.has_method("clear_selection"):
        _inventory_panel.clear_selection()

func _on_inventory_item_selected(_index: int, _data: Variant) -> void:
    _character_panel.clear_selection()
```

---

## Image and Texture Resources

### Location Structure

```
resources/
├── themes/
│   └── default_theme.tres        # UITheme resource
├── textures/
│   └── ui/
│       ├── panel_9slice.png      # Main panel 9-slice
│       ├── panel_inner_9slice.png # Inner panel 9-slice
│       ├── tab_bookmark.png      # Tab texture
│       └── icons/
│           ├── arrow_left.png
│           ├── arrow_right.png
│           ├── close.png
│           ├── check.png
│           ├── plus.png
│           ├── minus.png
│           ├── trash.png
│           ├── sort.png
│           └── info.png
└── shaders/
    └── paper_texture.gdshader    # Paper grain effect
```

### 9-Slice Textures

Used with `NinePatchRect` (BasePanel):

```gdscript
# 9-slice margin configuration
patch_margin_left = 12   # 12px corners for 48px texture
patch_margin_top = 12
patch_margin_right = 12
patch_margin_bottom = 12
```

---

## Mobile Considerations

### Touch Target Enforcement

```gdscript
# In component _ready()
UIScaler.ensure_touch_size(self)

# Implementation
static func ensure_touch_size(control: Control) -> void:
    var min_size := get_min_touch_size()  # 48dp minimum
    control.custom_minimum_size = Vector2(
        max(control.custom_minimum_size.x, min_size),
        max(control.custom_minimum_size.y, min_size)
    )
```

### Safe Area Handling

```gdscript
# Apply safe area margins to root container
UIScaler.apply_safe_margins(safe_margin_container)

# Or create pre-configured container
var safe_container := UIScaler.create_safe_margin_container()
```

### Long Press for Drag

Mobile-friendly drag uses long press (0.4s hold) instead of immediate drag:
- Allows scroll gesture to work without triggering drag
- Movement threshold (10px) cancels long press

---

## Component Creation Patterns

### Static Factory Methods

Most components provide static `create()` methods:

```gdscript
# ThemedButton
var btn := ThemedButton.create("Label", ThemedButton.ButtonStyle.PRIMARY)

# IconButton
var icon_btn := IconButton.create_with_preset(
    IconButton.PresetIcon.CLOSE,
    "",
    ThemedButton.ButtonStyle.GHOST,
    IconButton.IconPosition.ONLY
)

# BasePanel
var panel := BasePanel.create(BasePanel.PanelStyle.PRIMARY, true)

# DragDropSlot
var slot := DragDropSlot.create(Vector2(64, 64))

# EquipmentSlot
var equip_slot := EquipmentSlot.create_for_slot(
    EquipmentSlot.SlotType.HEAD,
    Vector2(128, 128)
)

# ScrollableGrid
var grid := ScrollableGrid.create(24, ScrollableGrid.SlotType.INVENTORY)

# BookmarkTabBar
var tabs := BookmarkTabBar.create(["Tab1", "Tab2", "Tab3"])
```

### Programmatic UI Construction

```gdscript
func _build_ui() -> void:
    var theme := UIThemeManager.get_theme()

    # Create container
    var vbox := VBoxContainer.new()
    vbox.name = "MainVBox"
    vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    vbox.size_flags_vertical = Control.SIZE_EXPAND_FILL
    vbox.add_theme_constant_override("separation", theme.spacing_medium)
    add_child(vbox)

    # Create styled panel
    var panel := PanelContainer.new()
    panel.name = "ContentPanel"
    var stylebox := UIThemeManager.create_bg_stylebox("secondary")
    panel.add_theme_stylebox_override("panel", stylebox)
    vbox.add_child(panel)

    # Create label with theme styling
    var label := Label.new()
    label.text = "Title"
    label.add_theme_font_size_override("font_size", theme.font_size_header)
    label.add_theme_color_override("font_color", theme.text_color)
    panel.add_child(label)
```

---

## File Organization

```
scripts/ui/
├── core/
│   ├── ui_constants.gd      # Static sizing constants
│   ├── ui_theme.gd          # UITheme resource class
│   └── ui_scaler.gd         # Responsive scaling utilities
├── components/
│   ├── themed_button.gd     # Base themed button
│   ├── icon_button.gd       # Button with icon
│   ├── base_panel.gd        # 9-slice panel with shader
│   ├── drag_drop_slot.gd    # Base draggable slot
│   ├── equipment_slot.gd    # Equipment-specific slot
│   ├── inventory_slot.gd    # Inventory-specific slot
│   ├── bookmark_tab_bar.gd  # Medieval-style tabs
│   └── scrollable_grid.gd   # Auto-generating slot grid
├── panels/
│   ├── character_panel.gd   # Character display + equipment
│   ├── inventory_panel.gd   # Inventory grid + info
│   └── stats_panel.gd       # Character statistics
└── screens/
    ├── party_menu.gd        # Full party management screen
    ├── main_menu.gd         # Game main menu
    ├── settings_menu.gd     # Settings screen
    └── save_load_menu.gd    # Save/load screen

autoload/
├── ui_theme_manager.gd      # Theme singleton
├── ui_manager.gd            # Menu stack management
└── drag_drop_manager.gd     # Drag-drop coordination
```

---

## Quick Reference

### Creating a New Component

1. Extend appropriate base class (`Control`, `PanelContainer`, `Button`, or existing component)
2. Add `class_name` for global access
3. Define signals for events
4. Implement `_ready()` with `_build_ui()` and `_apply_style()`
5. Connect to `UIThemeManager.theme_changed`
6. Provide static `create()` factory method

### Creating a New Screen

1. Extend `Control`
2. Set `PRESET_FULL_RECT` anchors
3. Add background overlay (`ColorRect` with `bg_overlay`)
4. Add safe margin container
5. Build content hierarchy using panels and components
6. Connect signals between child components
7. Handle `ui_cancel` input for close

### Adding Theme Support

1. Get theme: `var theme := UIThemeManager.get_theme()`
2. Create StyleBox: `UIThemeManager.create_*_stylebox()`
3. Apply overrides: `add_theme_*_override()`
4. Connect to `theme_changed` signal
5. Implement `_on_theme_changed()` to refresh styling
