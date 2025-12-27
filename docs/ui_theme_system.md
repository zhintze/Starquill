# UI Theme System Documentation

## Overview

The UI Theme System provides a centralized, flexible approach to styling all UI elements in Starquill. It consists of two main components:

1. **UITheme** - A Resource class that defines visual properties (colors, fonts, spacing, etc.)
2. **UIThemeManager** - An autoload singleton that manages the active theme and provides convenient access throughout the game

This system allows for easy customization, theme switching, and consistent visual styling across all UI elements.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      UIThemeManager                         │
│                    (Autoload Singleton)                     │
│                                                             │
│  - Holds current active theme                              │
│  - Manages theme registry                                  │
│  - Provides helper methods for quick access                │
│  - Emits signals when theme changes                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ manages
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                         UITheme                             │
│                    (Resource Class)                         │
│                                                             │
│  - Color palettes (primary, secondary, accent, etc.)       │
│  - Font definitions and sizes                              │
│  - Texture references                                      │
│  - Spacing and padding constants                           │
│  - StyleBox factory methods                                │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ used by
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      UI Components                          │
│                                                             │
│  - ThemedPanel                                             │
│  - ThemedButton                                            │
│  - EquipmentSlotUI                                         │
│  - PartyMenu                                               │
│  - etc.                                                    │
└─────────────────────────────────────────────────────────────┘
```

---

## Component Details

### UITheme Resource Class

**Location:** `scripts/ui/core/ui_theme.gd`

A Resource that can be saved as `.tres` files and loaded at runtime. It encapsulates all visual styling properties for the game's UI.

#### Key Properties

**Theme Identification:**
- `theme_name: String` - Display name for the theme
- `theme_description: String` - Description of the theme

**Color Palette:**
- `primary_color` - Main UI color (parchment/tan)
- `secondary_color` - Secondary color for borders (dark brown)
- `accent_color` - Highlight color (gold)
- `background_color` - Background overlay color
- `panel_color` - Panel background color

**Text Colors:**
- `text_color` - Primary text color
- `text_color_secondary` - Secondary text color
- `text_color_disabled` - Disabled text color
- `text_color_highlight` - Highlighted text color

**State Colors:**
- `hover_color` - Color when hovering over elements
- `pressed_color` - Color when elements are pressed
- `selected_color` - Color for selected elements
- `disabled_color` - Color for disabled elements

**Feedback Colors:**
- `success_color` - Green for success states
- `warning_color` - Yellow/gold for warnings
- `error_color` - Red for errors
- `info_color` - Blue for information

**Fonts:**
- `font_header`, `font_body`, `font_button`, `font_small` - Font resources
- `font_size_header`, `font_size_subheader`, `font_size_body`, `font_size_button`, `font_size_small` - Font sizes

**Textures:**
- `panel_texture` - Background texture for panels
- `button_texture` - Button background texture
- `border_texture` - Ornate border texture
- `slot_texture` - Equipment slot background
- `icon_frame_texture` - Frame around icons

**Panel Styling:**
- `panel_border_width` - Width of panel borders (default: 4)
- `panel_corner_radius` - Radius for rounded corners (default: 8)
- `panel_shadow_size` - Drop shadow size (default: 4)
- `panel_shadow_color` - Shadow color

**Button Styling:**
- `button_border_width` - Button border width (default: 2)
- `button_corner_radius` - Button corner radius (default: 6)
- `button_padding_horizontal` - Horizontal padding (default: 16)
- `button_padding_vertical` - Vertical padding (default: 8)
- `button_hover_scale` - Scale factor on hover (default: 1.05)

**Equipment Slot Styling:**
- `slot_size` - Size of equipment slots (default: 64x64)
- `slot_border_width` - Border width (default: 2)
- `slot_border_color` - Normal border color
- `slot_border_color_highlight` - Highlighted border color
- `slot_empty_color` - Color when slot is empty

**Spacing:**
- `spacing_tiny` - 4px
- `spacing_small` - 8px
- `spacing_medium` - 16px
- `spacing_large` - 24px
- `spacing_huge` - 32px

**Padding:**
- `padding_panel` - 16px
- `padding_container` - 12px
- `padding_button` - 8px

**Animation:**
- `transition_duration` - 0.2 seconds
- `hover_duration` - 0.1 seconds
- `fade_duration` - 0.3 seconds

#### Key Methods

**Color Modulation:**
```gdscript
func get_color_hover(base_color: Color) -> Color
func get_color_pressed(base_color: Color) -> Color
func get_color_disabled(base_color: Color) -> Color
```

**Font Helper:**
```gdscript
func get_font_or_default(font: Font, fallback_size: int) -> Font
```

**StyleBox Factory Methods:**
```gdscript
func create_panel_stylebox() -> StyleBoxFlat
func create_button_stylebox(state: String = "normal") -> StyleBoxFlat
func create_slot_stylebox(is_filled: bool = false, is_highlighted: bool = false) -> StyleBoxFlat
```

---

### UIThemeManager Autoload

**Location:** `autoload/ui_theme_manager.gd`

A global singleton that manages themes throughout the game. Automatically loads the default theme on startup.

#### Initialization

On `_ready()`, the manager:
1. Attempts to load `res://resources/themes/default_theme.tres`
2. If successful, sets it as the active theme and registers it
3. If not found, creates a fallback theme programmatically

#### Key Properties

- `current_theme: UIThemeClass` - The currently active theme
- `default_theme: UIThemeClass` - Fallback theme
- `registered_themes: Dictionary` - Registry of available themes by name

#### Signals

```gdscript
signal theme_changed(new_theme: UIThemeClass)
```
Emitted when the active theme changes. UI components can listen to this signal to update their appearance.

#### Core Methods

**Theme Management:**
```gdscript
# Set a theme as active
func set_theme(theme: UIThemeClass) -> void

# Set theme by name from registry
func set_theme_by_name(theme_name: String) -> bool

# Register a new theme
func register_theme(theme: UIThemeClass) -> void

# Unregister a theme
func unregister_theme(theme_name: String) -> void

# Get current theme (never returns null)
func get_theme() -> UIThemeClass

# List all available themes
func get_available_themes() -> Array[String]
```

**Quick Access Helpers:**
```gdscript
# Color getters
func get_primary_color() -> Color
func get_secondary_color() -> Color
func get_accent_color() -> Color
func get_text_color() -> Color
func get_panel_color() -> Color
func get_background_color() -> Color

# Font getters
func get_header_font() -> Font
func get_body_font() -> Font
func get_button_font() -> Font

# Size getters
func get_font_size_header() -> int
func get_font_size_body() -> int
func get_font_size_button() -> int

# Spacing getter
func get_spacing(size: String = "medium") -> int
```

**StyleBox Factory Methods:**
```gdscript
func create_panel_stylebox() -> StyleBoxFlat
func create_button_stylebox(state: String = "normal") -> StyleBoxFlat
func create_slot_stylebox(is_filled: bool = false, is_highlighted: bool = false) -> StyleBoxFlat
```

**Animation Helpers:**
```gdscript
func get_transition_duration() -> float
func get_hover_duration() -> float
func get_fade_duration() -> float
```

**Utility Methods:**
```gdscript
# Apply theme automatically to a Control node
func apply_theme_to_control(control: Control) -> void
```

---

## Usage Examples

### Basic Usage - Accessing Theme Properties

```gdscript
extends Panel

func _ready() -> void:
    # Get colors from theme
    var primary := UIThemeManager.get_primary_color()
    var text := UIThemeManager.get_text_color()

    # Get spacing
    var spacing := UIThemeManager.get_spacing("large")  # Returns 24

    # Get animation duration
    var duration := UIThemeManager.get_transition_duration()  # Returns 0.2
```

### Applying Theme to UI Elements

```gdscript
extends Button

func _ready() -> void:
    # Apply themed styles to button
    add_theme_stylebox_override("normal", UIThemeManager.create_button_stylebox("normal"))
    add_theme_stylebox_override("hover", UIThemeManager.create_button_stylebox("hover"))
    add_theme_stylebox_override("pressed", UIThemeManager.create_button_stylebox("pressed"))
    add_theme_color_override("font_color", UIThemeManager.get_text_color())

    # Or use the convenience method
    UIThemeManager.apply_theme_to_control(self)
```

### Creating Themed Panels

```gdscript
extends Panel

func _ready() -> void:
    # Apply panel theme
    add_theme_stylebox_override("panel", UIThemeManager.create_panel_stylebox())
```

### Equipment Slot Styling

```gdscript
extends Panel
class_name EquipmentSlotUI

var is_filled: bool = false
var is_highlighted: bool = false

func _update_visual() -> void:
    # Update slot appearance based on state
    var stylebox := UIThemeManager.create_slot_stylebox(is_filled, is_highlighted)
    add_theme_stylebox_override("panel", stylebox)
```

### Responding to Theme Changes

```gdscript
extends Control

func _ready() -> void:
    # Listen for theme changes
    UIThemeManager.theme_changed.connect(_on_theme_changed)
    _apply_theme()

func _on_theme_changed(new_theme) -> void:
    _apply_theme()

func _apply_theme() -> void:
    # Reapply theme properties when theme changes
    modulate = UIThemeManager.get_primary_color()
```

### Creating Animations with Theme Durations

```gdscript
extends Control

func fade_in() -> void:
    var tween := create_tween()
    var duration := UIThemeManager.get_fade_duration()
    tween.tween_property(self, "modulate:a", 1.0, duration)

func animate_hover() -> void:
    var tween := create_tween()
    var duration := UIThemeManager.get_hover_duration()
    var scale := UIThemeManager.get_theme().button_hover_scale
    tween.tween_property(self, "scale", Vector2.ONE * scale, duration)
```

---

## Creating Custom Themes

### Method 1: Create a .tres File in Godot Editor

1. Open Godot Editor
2. In FileSystem, navigate to `res://resources/themes/`
3. Right-click > Create New > Resource
4. Select `UITheme` as the resource type
5. Configure all exported properties in the Inspector
6. Save with a descriptive name (e.g., `dark_theme.tres`)

### Method 2: Programmatically Create and Register

```gdscript
func create_custom_theme() -> void:
    var custom_theme := UITheme.new()
    custom_theme.theme_name = "Dark Mode"
    custom_theme.theme_description = "A dark themed UI"

    # Set colors
    custom_theme.primary_color = Color(0.2, 0.2, 0.25)
    custom_theme.secondary_color = Color(0.1, 0.1, 0.15)
    custom_theme.accent_color = Color(0.4, 0.6, 1.0)
    custom_theme.text_color = Color(0.9, 0.9, 0.95)

    # Register the theme
    UIThemeManager.register_theme(custom_theme)

    # Switch to it
    UIThemeManager.set_theme(custom_theme)
```

### Method 3: Duplicate and Modify Default Theme

```gdscript
func create_variant_theme() -> void:
    # Load default theme as base
    var base_theme := ResourceLoader.load("res://resources/themes/default_theme.tres")

    # Duplicate it
    var variant_theme := base_theme.duplicate() as UITheme
    variant_theme.theme_name = "Fantasy Variant"

    # Modify specific properties
    variant_theme.accent_color = Color(0.8, 0.3, 0.3)  # Red accent instead of gold
    variant_theme.button_hover_scale = 1.1  # More pronounced hover

    # Register and use
    UIThemeManager.register_theme(variant_theme)
    UIThemeManager.set_theme_by_name("Fantasy Variant")
```

---

## Default Theme: "Fantasy Default"

**File:** `resources/themes/default_theme.tres`

The default theme uses a medieval fantasy aesthetic:

**Color Scheme:**
- **Parchment/Tan**: Primary UI backgrounds (#D2BE9F)
- **Dark Brown**: Borders and secondary elements (#4B3A2D)
- **Gold**: Accents and highlights (#E1B34F)
- **Dark Translucent**: Overlay backgrounds
- **Light Parchment**: Panel backgrounds

**Visual Style:**
- Rounded corners (8px panels, 6px buttons)
- Drop shadows for depth
- Medium borders (4px panels, 2px buttons)
- Warm, earthy color palette

**Typography:**
- Header: 24px
- Subheader: 18px
- Body: 14px
- Button: 16px
- Small: 12px

**Spacing:**
- Follows 4px baseline grid (4, 8, 16, 24, 32)

---

## Integration Points

### Autoload Configuration

The UIThemeManager is registered in `project.godot`:

```ini
[autoload]
UIThemeManager="*res://autoload/ui_theme_manager.gd"
```

It's positioned after `UIManager` to ensure UI infrastructure is available.

### File Structure

```
scripts/ui/core/
  └── ui_theme.gd           # UITheme resource class

autoload/
  └── ui_theme_manager.gd   # UIThemeManager singleton

resources/themes/
  └── default_theme.tres    # Default fantasy theme resource
```

---

## Future Enhancements

### Planned Features:

1. **Texture Support**: Add actual texture files for panels, buttons, and borders
2. **Font Integration**: Add medieval/fantasy font files
3. **Theme Presets**: Create additional themes (dark mode, high contrast, etc.)
4. **Player Customization**: Settings menu to let players choose themes
5. **Theme Overrides**: Per-menu theme overrides for special screens
6. **Seasonal Themes**: Holiday or seasonal theme variants
7. **Color Blind Modes**: Accessibility-focused color schemes

---

## Best Practices

1. **Always use UIThemeManager** instead of hardcoded colors/sizes
2. **Listen to theme_changed signal** in UI components that need to update
3. **Use spacing constants** instead of magic numbers
4. **Leverage StyleBox factory methods** instead of creating StyleBoxes manually
5. **Register custom themes** at game startup for easy switching
6. **Test themes thoroughly** - ensure all UI elements respect theme changes
7. **Document theme modifications** when creating new themes

---

## Troubleshooting

### Theme Not Loading

**Problem:** UIThemeManager prints "Created fallback default theme" instead of loading from file.

**Solution:** Check that `res://resources/themes/default_theme.tres` exists and is properly formatted.

### Theme Changes Not Reflected

**Problem:** UI elements don't update when theme changes.

**Solution:** Ensure UI components are connected to the `theme_changed` signal and reapply styles.

### Colors Look Wrong

**Problem:** Colors appear different than expected.

**Solution:** Check Color values are in 0.0-1.0 range, not 0-255. Godot uses normalized color values.

### StyleBox Not Applying

**Problem:** Theme overrides not working on Control nodes.

**Solution:** Ensure you're using the correct override names for each Control type (e.g., "panel" for Panel, "normal"/"hover"/"pressed" for Button).

---

## API Reference Summary

### UITheme Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `get_color_hover(base_color)` | `Color` | Lightens color for hover state |
| `get_color_pressed(base_color)` | `Color` | Darkens color for pressed state |
| `get_color_disabled(base_color)` | `Color` | Semi-transparent for disabled state |
| `get_font_or_default(font, fallback_size)` | `Font` | Returns font or system default |
| `create_panel_stylebox()` | `StyleBoxFlat` | Creates styled panel background |
| `create_button_stylebox(state)` | `StyleBoxFlat` | Creates styled button (normal/hover/pressed/disabled) |
| `create_slot_stylebox(is_filled, is_highlighted)` | `StyleBoxFlat` | Creates styled equipment slot |

### UIThemeManager Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `set_theme(theme)` | `void` | Set active theme |
| `set_theme_by_name(name)` | `bool` | Set theme by registered name |
| `register_theme(theme)` | `void` | Register theme in registry |
| `unregister_theme(name)` | `void` | Remove theme from registry |
| `get_theme()` | `UIThemeClass` | Get current theme (never null) |
| `get_available_themes()` | `Array[String]` | List registered theme names |
| `apply_theme_to_control(control)` | `void` | Auto-apply theme to Control |

**Color Getters:** `get_primary_color()`, `get_secondary_color()`, `get_accent_color()`, `get_text_color()`, `get_panel_color()`, `get_background_color()`

**Font Getters:** `get_header_font()`, `get_body_font()`, `get_button_font()`

**Size Getters:** `get_font_size_header()`, `get_font_size_body()`, `get_font_size_button()`

**Utility Getters:** `get_spacing(size)`, `get_transition_duration()`, `get_hover_duration()`, `get_fade_duration()`

**StyleBox Factories:** `create_panel_stylebox()`, `create_button_stylebox(state)`, `create_slot_stylebox(is_filled, is_highlighted)`

---

## Conclusion

The UI Theme System provides a robust foundation for consistent, customizable UI styling throughout Starquill. By centralizing theme management and providing convenient access methods, it ensures that all UI elements can easily adopt and respond to theme changes, making the game's visual style flexible and maintainable.
