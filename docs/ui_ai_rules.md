# Starquill UI Rules for AI Agents

This document defines strict rules for AI agents working on Starquill UI code. Following these rules ensures consistent, maintainable UI architecture.

---

## Spacing and Padding Rules

### Rule 1: Single Source of Truth for Spacing
**Use UITheme spacing/padding tokens ONLY.**

```gdscript
# CORRECT
var theme := UIThemeManager.get_theme()
vbox.add_theme_constant_override("separation", theme.spacing_medium)

# WRONG - UIConstants spacing constants have been removed
# vbox.add_theme_constant_override("separation", UIConstants.SPACING_MD)

# WRONG - No magic numbers
vbox.add_theme_constant_override("separation", 16)
```

### Rule 2: Never Apply Padding Twice
Padding lives in ONE place per container:
- **MarginContainer** for panel/container padding
- **StyleBox content_margin** is set to 0

```gdscript
# CORRECT - MarginContainer provides padding
var panel := PanelContainer.new()
var margin := UIThemeManager.make_margin_container("panel")
panel.add_child(margin)
margin.add_child(content)

# WRONG - Double padding
var panel := PanelContainer.new()
panel.add_theme_stylebox_override("panel", stylebox_with_margins)  # Has padding
var margin := MarginContainer.new()  # Also has padding
panel.add_child(margin)  # Result: double padding
```

### Rule 3: Use UIFactory Helpers
Prefer factory functions over manual container creation:

```gdscript
# CORRECT
var vbox := UIThemeManager.make_vbox("medium")
var margin := UIThemeManager.make_margin_container("panel")

# ACCEPTABLE but verbose
var vbox := VBoxContainer.new()
var theme := UIThemeManager.get_theme()
vbox.add_theme_constant_override("separation", theme.spacing_medium)
```

---

## Positioning Rules

### Rule 4: No Absolute Positioning
Except for drag previews, never use absolute positioning.

```gdscript
# WRONG
button.position = Vector2(20, 20)
button.set_anchors_preset(Control.PRESET_TOP_LEFT)

# CORRECT - Use OverlayButtonContainer or layout containers
var overlay := OverlayButtonContainer.new()
overlay.corner_position = OverlayButtonContainer.Position.TOP_LEFT
overlay.add_child(button)
```

---

## Communication Rules

### Rule 5: Parent-Child Communication via Signals Only
Child emits signal, parent listens. Parent never reaches into child internals.

```gdscript
# CORRECT
# Child:
signal item_selected(index: int, data: Variant)
func _on_slot_clicked() -> void:
    item_selected.emit(_selected_index, _selected_data)

# Parent:
child_panel.item_selected.connect(_on_child_item_selected)

# WRONG - Parent reaching into child
if child_panel.has_method("get_internal_state"):
    var state = child_panel.get_internal_state()
```

### Rule 6: Sibling-to-Sibling via Coordinator
Panels never call each other directly. Screen coordinator mediates.

```gdscript
# CORRECT - Coordinator pattern
class PartyMenuCoordinator:
    func setup(char_panel, inv_panel) -> void:
        char_panel.selection_changed.connect(_on_char_selection)
        inv_panel.selection_changed.connect(_on_inv_selection)

    func _on_char_selection(item) -> void:
        _inv_panel.clear_selection()

# WRONG - Direct panel-to-panel call
func _on_equipment_clicked():
    _inventory_panel.clear_selection()  # Direct coupling
```

### Rule 7: Global UI Events via EventBus with Prefix
Use EventBus only for truly global events. Prefix with `ui.`

```gdscript
# Acceptable global events:
EventBus.emit_signal("ui_screen_open", "party_menu")
EventBus.emit_signal("ui_toast_show", "Item equipped!")
EventBus.emit_signal("ui_modal_close_all")

# WRONG - Using EventBus for local coordination
EventBus.emit_signal("clear_inventory_selection")  # Should be coordinator
```

---

## Component Rules

### Rule 8: ScrollableGrid is Layout-Only
ScrollableGrid handles layout (columns, spacing, sizing). Data binding is separate.

```gdscript
# CORRECT - Use SlotGridBinder for data
var binder := SlotGridBinder.new()
binder.setup(_grid_container, _create_slot)
binder.bind_items(inventory_items)

# WRONG - ScrollableGrid managing data
_scrollable_grid.set_items(inventory_items)  # Deprecated
```

### Rule 9: Minimal Public API + Signals
Components expose:
- Public setter/getter methods for state
- Signals for events
- Nothing else

```gdscript
# CORRECT - Clean public API
class EquipmentSlot:
    signal equipment_changed(slot, old_item, new_item)
    signal slot_clicked(slot)

    func set_equipment(item) -> void
    func get_equipment() -> Variant
    func clear_equipment() -> void

# WRONG - Exposing internals
class EquipmentSlot:
    var _icon_rect: TextureRect  # Should be private
    func _update_internal_state()  # Should be private
```

---

## Boundary Rules

### What Parents CAN Do
- Provide data at creation time
- Call public methods on direct children
- Subscribe to child signals
- Pass configuration via exports

### What Parents CANNOT Do
- Modify grandchild nodes
- Use `has_method()` or `has_signal()` for optional behavior
- Access private (`_prefixed`) variables
- Call methods not part of documented public API

```gdscript
# WRONG - Reaching into grandchild
_character_panel._portrait_container._icon_rect.texture = my_texture

# WRONG - Optional behavior check
if _inventory_panel.has_method("some_optional_thing"):
    _inventory_panel.some_optional_thing()

# CORRECT - Use documented interface
_character_panel.set_character_portrait(my_texture)
```

---

## Quick Reference

| Aspect | Rule |
|--------|------|
| Spacing source | UITheme tokens only |
| Padding location | MarginContainer (StyleBox margins = 0) |
| Container creation | Use UIFactory helpers |
| Positioning | Layout containers, not absolute |
| Parent-child | Signals |
| Sibling-sibling | Coordinator |
| Global events | EventBus with `ui.` prefix |
| Data binding | SlotGridBinder, not ScrollableGrid |
| Public API | Minimal methods + signals |

---

## EventBus UI Event Names

Reserved global UI events:
- `ui_screen_open` - Open a named screen
- `ui_screen_close` - Close current screen
- `ui_screen_close_all` - Close all screens
- `ui_toast_show` - Show toast message
- `ui_modal_open` - Open modal dialog
- `ui_modal_close` - Close modal dialog
