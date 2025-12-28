# Starquill UI Layout Rules

This document defines the layout grammar and boundary rules for Starquill UI.

---

## Spacing Grammar

All spacing uses UITheme tokens. Here is when to use each:

### Spacing Scale (for 1280x720 reference)

| Token | Value | Use Case |
|-------|-------|----------|
| `spacing_tiny` | 8px | Tight gaps between inline elements |
| `spacing_small` | 16px | Inside panels, between closely related items |
| `spacing_medium` | 32px | Between sections, standard separation |
| `spacing_large` | 48px | Between major columns, significant breaks |
| `spacing_huge` | 64px | Maximum separation, rarely used |

### Padding Scale (for 1280x720 reference)

| Token | Value | Use Case |
|-------|-------|----------|
| `padding_button` | 16px | Inside buttons |
| `padding_container` | 24px | Inside minor containers |
| `padding_panel` | 32px | Inside main panels |

---

## When to Use Each Spacing

### Inside a Panel
Use `spacing_small`:
```gdscript
var vbox := UIThemeManager.make_vbox("small")
```

### Between Sections
Use `spacing_medium`:
```gdscript
var sections_vbox := UIThemeManager.make_vbox("medium")
```

### Between Major Columns
Use `spacing_large`:
```gdscript
var main_hbox := UIThemeManager.make_hbox("large")
```

### Screen Edges
Use `padding_panel` via safe margin container:
```gdscript
var safe_margin := UIThemeManager.make_margin_container("panel")
# Plus apply safe area insets from UIScaler
```

---

## Container Rules

### Rule: Always Set Separation Explicitly
Never rely on default separation values.

```gdscript
# CORRECT
var vbox := VBoxContainer.new()
vbox.add_theme_constant_override("separation", theme.spacing_medium)

# WRONG - Implicit separation
var vbox := VBoxContainer.new()
vbox.add_child(item1)
vbox.add_child(item2)  # Unknown separation
```

### Rule: No Magic Numbers
All numeric values come from theme tokens or UIConstants.

```gdscript
# CORRECT
margin.add_theme_constant_override("margin_left", theme.padding_panel)
slot.custom_minimum_size = Vector2(UIConstants.SLOT_SIZE_ICON, UIConstants.SLOT_SIZE_ICON)

# WRONG
margin.add_theme_constant_override("margin_left", 16)
slot.custom_minimum_size = Vector2(128, 128)
```

---

## Padding Rules

### Rule: Padding Lives in MarginContainer
StyleBox content margins are always 0. Padding comes from MarginContainer.

```gdscript
# Standard pattern
var panel := PanelContainer.new()
var stylebox := UIThemeManager.create_bg_stylebox("primary")  # No content margins
panel.add_theme_stylebox_override("panel", stylebox)

var padding := UIThemeManager.make_margin_container("panel")
panel.add_child(padding)

var content := VBoxContainer.new()
padding.add_child(content)
```

### Rule: Panels Never Apply Padding Twice
If a StyleBox has content margins, do not wrap contents in MarginContainer.
(Our standard: StyleBox margins = 0, use MarginContainer)

### Exception: Safe Area Margins
Safe area margins from UIScaler are applied at the screen level, separate from content padding.

```gdscript
# Screen structure
Screen (Control)
├── SafeMargin (MarginContainer) - Safe area insets
│   └── MainPanel (PanelContainer)
│       └── PanelPadding (MarginContainer) - Content padding
│           └── Content
```

---

## Component Boundary Rules

### What Parents CAN Do

1. **Provide data at creation**
   ```gdscript
   var panel := CharacterPanel.new()
   panel.set_character(character_data)
   ```

2. **Call documented public methods**
   ```gdscript
   _character_panel.refresh()
   _character_panel.clear_selection()
   ```

3. **Subscribe to child signals**
   ```gdscript
   _character_panel.selection_changed.connect(_on_char_selection)
   ```

4. **Configure via exports (if using scenes)**
   ```gdscript
   panel.show_equipment = true
   ```

### What Parents CANNOT Do

1. **Modify grandchild nodes**
   ```gdscript
   # FORBIDDEN
   _panel._inner_container._label.text = "Hello"
   ```

2. **Use has_method/has_signal for optional behavior**
   ```gdscript
   # FORBIDDEN
   if panel.has_method("maybe_do_thing"):
       panel.maybe_do_thing()
   ```

3. **Access private variables**
   ```gdscript
   # FORBIDDEN
   var internal = _panel._private_state
   ```

4. **Call undocumented methods**
   ```gdscript
   # FORBIDDEN - if not in public API
   _panel._internal_update()
   ```

---

## UIScaler Rules

### Primary Strategy: Godot Stretch Mode
Use Godot's built-in stretch mode for resolution handling. UIScaler is NOT for general scaling.

### UIScaler IS For:
- Safe area insets (`get_safe_top()`, `get_safe_left()`, etc.)
- Minimum touch target enforcement (`ensure_touch_size()`)
- Device detection (`is_tablet()`, `is_landscape()`)
- Responsive breakpoints

### UIScaler is NOT For:
- Scaling spacing or padding values
- Scaling font sizes
- General "make everything bigger" operations

### Hard Rules:
1. **No manual multiplication by UIScaler for spacing/padding/fonts**
   ```gdscript
   # WRONG
   var scaled_spacing = UIScaler.scale(theme.spacing_medium)

   # CORRECT
   var spacing = theme.spacing_medium  # Use as-is
   ```

2. **Theme tokens are logical pixels, not dynamically scaled**
   Theme values are authored for the reference resolution and adapt via Godot stretch.

3. **Touch target enforcement is the exception**
   ```gdscript
   # CORRECT - UIScaler for touch targets
   UIScaler.ensure_touch_size(button)
   ```

---

## Z-Index Guidelines

Use UIConstants Z-index values:

| Constant | Value | Use Case |
|----------|-------|----------|
| `Z_BACKGROUND` | 0 | Background elements |
| `Z_CONTENT` | 10 | Normal content |
| `Z_PANEL` | 20 | Panels above content |
| `Z_MODAL` | 50 | Modal dialogs |
| `Z_OVERLAY` | 80 | Overlays (close buttons, etc.) |
| `Z_TOOLTIP` | 90 | Tooltips |
| `Z_NOTIFICATION` | 100 | Notifications, toasts |

---

## Layout Hierarchy Pattern

Standard screen structure:

```
Screen (Control - PRESET_FULL_RECT)
├── Background (ColorRect - Z_BACKGROUND)
├── SafeMargin (MarginContainer - safe area insets)
│   └── MainPanel (PanelContainer/BasePanel)
│       └── PanelPadding (MarginContainer - padding_panel)
│           └── MainLayout (HBox/VBox - spacing_large)
│               ├── LeftPanel (PanelContainer)
│               │   └── LeftPadding (MarginContainer - padding_panel)
│               │       └── LeftContent (VBox - spacing_small)
│               └── RightPanel (PanelContainer)
│                   └── RightPadding (MarginContainer - padding_panel)
│                       └── RightContent (VBox - spacing_small)
└── OverlayContainer (OverlayButtonContainer - Z_OVERLAY)
    └── CloseButton
```

---

## Quick Checklist

Before submitting UI code, verify:

- [ ] All spacing uses theme tokens (no magic numbers)
- [ ] Container separations are set explicitly
- [ ] Padding is in MarginContainer (not StyleBox)
- [ ] No absolute positioning (except drag previews)
- [ ] Parent only uses child's public API
- [ ] No has_method/has_signal checks
- [ ] No grandchild manipulation
- [ ] UIScaler only for safe area/touch targets
