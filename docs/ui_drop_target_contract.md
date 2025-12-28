# Drop Target Contract

This document defines the formal interface contract for drag-and-drop targets in the Starquill UI system.

---

## Overview

Any UI element that accepts dropped items must implement the Drop Target Contract. This ensures consistent behavior across all drag-drop interactions.

---

## Required Methods

### `can_accept_drop(data: Variant) -> bool`

Determines if this target can accept the given drag data.

```gdscript
func can_accept_drop(data: Variant) -> bool:
    # Return true if this target can accept the data
    # Return false to reject the drop

    # Example: Only accept EquipmentInstance
    return data is EquipmentInstance
```

**Parameters:**
- `data`: The data being dragged (typically an item instance)

**Returns:**
- `true`: Target can accept this data (shows valid drop indicator)
- `false`: Target cannot accept this data (shows invalid drop indicator)

---

### `handle_drop(data: Variant, source: Control) -> bool`

Processes the dropped data. Called when user releases drag over a valid target.

```gdscript
func handle_drop(data: Variant, source: Control) -> bool:
    # Process the drop
    # Return true if drop was handled successfully
    # Return false if drop failed

    # Example: Equip item from inventory
    if data is EquipmentInstance:
        set_equipment(data)
        return true
    return false
```

**Parameters:**
- `data`: The data being dropped
- `source`: The source Control that initiated the drag (may be null)

**Returns:**
- `true`: Drop was handled successfully
- `false`: Drop failed (item returns to source)

---

## Required Signals

### `item_dropped(target: Control, data: Variant, source: Control)`

Emitted after a successful drop.

```gdscript
signal item_dropped(target: Control, data: Variant, source: Control)
```

**Parameters:**
- `target`: The drop target (self)
- `data`: The dropped data
- `source`: The source Control

---

### `drop_rejected(target: Control, data: Variant)`

Emitted when a drop is rejected (optional but recommended).

```gdscript
signal drop_rejected(target: Control, data: Variant)
```

**Parameters:**
- `target`: The drop target (self)
- `data`: The rejected data

---

## Canonical Implementation: DragDropSlot

`DragDropSlot` is the canonical implementation of the Drop Target Contract for slot-based UI elements.

**Location:** `scripts/ui/components/drag_drop_slot.gd`

### Features:
- Visual feedback for drag states (valid, invalid, hover)
- Slot data management
- Automatic styling via UITheme
- Selection/highlighting support

### Subclasses:
- `EquipmentSlot`: Equipment-specific slot with type filtering
- `InventorySlot`: Inventory grid slot

---

## Implementing a Custom Drop Target

### Option 1: Extend DragDropSlot (Recommended)

For slot-based targets, extend DragDropSlot:

```gdscript
class_name CustomSlot
extends DragDropSlot

func can_accept_drop(data: Variant) -> bool:
    # Add custom validation
    if not super.can_accept_drop(data):
        return false
    return data is MyCustomType

func handle_drop(data: Variant, source: Control) -> bool:
    # Custom drop handling
    if data is MyCustomType:
        _process_custom_drop(data)
        return true
    return super.handle_drop(data, source)
```

### Option 2: Implement Full Contract

For non-slot targets (like panels), implement the full contract:

```gdscript
class_name DropPanel
extends PanelContainer

signal item_dropped(target: Control, data: Variant, source: Control)
signal drop_rejected(target: Control, data: Variant)

func can_accept_drop(data: Variant) -> bool:
    return data is EquipmentInstance

func handle_drop(data: Variant, source: Control) -> bool:
    if not can_accept_drop(data):
        drop_rejected.emit(self, data)
        return false

    # Process drop
    _add_to_inventory(data)
    item_dropped.emit(self, data, source)
    return true
```

---

## DragDropManager Integration

The `DragDropManager` autoload handles:
- Drag initiation and preview
- Finding valid drop targets
- Calling `can_accept_drop()` for hover feedback
- Calling `handle_drop()` on release
- Emitting global drag events

### Registering as Drop Target

Drop targets are discovered automatically if they implement the contract methods. No explicit registration required.

---

## Visual Feedback States

Drop targets should provide visual feedback:

| State | Condition | Visual |
|-------|-----------|--------|
| Normal | No drag active | Default appearance |
| Valid Drop | `can_accept_drop() == true` | Highlight border (green) |
| Invalid Drop | `can_accept_drop() == false` | Error border (red) |
| Dragging From | This is the drag source | Dimmed/recessed appearance |

Use `UITheme.create_drag_drop_slot_stylebox(state)` for consistent styling.

---

## Example: InventoryPanel as Drop Target

The InventoryPanel implements the contract to accept unequipped items:

```gdscript
# From inventory_panel.gd

func can_accept_drop(data: Variant) -> bool:
    # Accept equipment being unequipped
    return data is EquipmentInstance

func handle_drop(data: Variant, source_slot: Control) -> bool:
    if not data is EquipmentInstance:
        return false
    if not source_slot is EquipmentSlot:
        return false

    # Clear equipment slot and add to inventory
    (source_slot as EquipmentSlot).clear_equipment()
    PlayerData.add_to_inventory(data)
    return true
```

---

## Best Practices

1. **Always validate in `can_accept_drop()`** - Don't assume data type
2. **Return accurate booleans** - Incorrect returns break visual feedback
3. **Emit signals after state changes** - Allows parent coordination
4. **Use UITheme for styling** - Maintains visual consistency
5. **Handle null source gracefully** - Source may not always be available
