# Shared Equipment Inventory Implementation Plan

## Overview
Implement a shared party inventory for equipment items with drag-drop between character equipment slots and inventory.

## Target Behavior
1. On party creation, 10 random equipment items are added to shared inventory
2. Equipment items display using DisplayBuilder compositing (same as character display)
3. Items can be dragged from inventory to equipment slots (and vice versa)
4. Unequipping moves item to shared inventory
5. CharacterDisplay updates immediately when equipment changes

---

## Task 1: Create Equipment Icon Renderer

**File:** `scripts/display/equipment_icon_renderer.gd` (NEW)

**Purpose:** Composite EquipmentInstance layers into a single Texture2D for slot icons

**Implementation:**
```gdscript
class_name EquipmentIconRenderer
extends RefCounted

static var _cache: Dictionary = {}  # item_type + item_num -> ImageTexture

static func get_icon(equipment: EquipmentInstance, size: Vector2 = Vector2(64, 64)) -> Texture2D:
    # Check cache first
    var cache_key := "%s_%d" % [equipment.item_type, equipment.item_num]
    if _cache.has(cache_key):
        return _cache[cache_key]

    # Build display pieces using DisplayBuilder
    var builder := StarquillDisplayBuilder.new()
    var result := builder.build_equipment_pieces([equipment], PackedStringArray())
    var pieces: Array[DisplayPiece] = result.pieces

    # Composite pieces into single image
    var icon := _composite_pieces(pieces, size)
    _cache[cache_key] = icon
    return icon

static func _composite_pieces(pieces: Array[DisplayPiece], size: Vector2) -> ImageTexture:
    # Create image, render each piece, return as ImageTexture
```

**Verification:** Create test that renders a helmet icon and verifies non-null texture

---

## Task 2: Add get_icon() Method to EquipmentInstance

**File:** `scripts/equipment/equipment_instance.gd`

**Changes:**
Add method that DragDropSlot._update_display() can call:

```gdscript
func get_icon() -> Texture2D:
    return EquipmentIconRenderer.get_icon(self)
```

**Verification:** Call get_icon() on random equipment, verify returns Texture2D

---

## Task 3: Populate Inventory on Party Creation

**File:** `scenes/world/world_test_scene.gd`

**Changes:**
After creating party members, add 10 random equipment items:

```gdscript
func _create_test_party() -> void:
    # ... existing character creation ...

    # Add 10 random equipment items to shared inventory
    var prefixes := ["hd", "tr", "ar", "lg", "fe", "w"]
    for i in range(10):
        var prefix: String = prefixes[randi() % prefixes.size()]
        var equipment: EquipmentInstance = equipment_factory.create_random_from_prefix(prefix)
        if equipment:
            PlayerData.add_to_inventory(equipment)
    print("Added %d items to shared inventory" % PlayerData.inventory.size())
```

**Verification:** Run game, check console output shows 10 items added

---

## Task 4: Add Inventory Changed Signal to PlayerData

**File:** `autoload/player_data.gd`

**Changes:**
Add signal and emit on inventory changes:

```gdscript
signal inventory_changed()

func add_to_inventory(equipment: EquipmentInstance) -> void:
    inventory.append(equipment)
    inventory_changed.emit()

func remove_from_inventory(equipment: EquipmentInstance) -> void:
    inventory.erase(equipment)
    inventory_changed.emit()
```

**Verification:** Connect to signal, add item, verify signal fires

---

## Task 5: Connect InventoryPanel to Equipment Inventory

**File:** `scripts/ui/panels/inventory_panel.gd`

**Changes:**
1. Change `_connect_inventory()` to use `PlayerData.inventory`
2. Modify `refresh()` to read from `Array[EquipmentInstance]` instead of Inventory
3. Listen to `PlayerData.inventory_changed` signal

```gdscript
func _connect_inventory() -> void:
    if has_node("/root/PlayerData"):
        PlayerData.inventory_changed.connect(_on_inventory_changed)
        refresh()

func refresh() -> void:
    var items: Array = PlayerData.inventory
    _item_count = items.size()
    _recalculate_grid()

    for i in range(_slots.size()):
        if i < items.size():
            _slots[i].set_slot_data(items[i])
        else:
            _slots[i].clear_slot()
```

**Verification:** Open party menu, verify inventory shows equipment items with icons

---

## Task 6: Update InventorySlot for EquipmentInstance

**File:** `scripts/ui/components/inventory_slot.gd`

**Changes:**
Modify `_update_display()` to handle EquipmentInstance:

```gdscript
func _update_display() -> void:
    super._update_display()

    # Handle EquipmentInstance specifically
    if _slot_data is EquipmentInstance:
        _quantity = 1  # Equipment doesn't stack
        _update_quantity_display()
```

The base DragDropSlot will call `get_icon()` which now exists on EquipmentInstance.

**Verification:** Inventory slots show equipment icons correctly

---

## Task 7: Enable Cross-Slot Drag-Drop

**File:** `scripts/ui/components/equipment_slot.gd`

**Changes:**
Modify `can_accept_drop()` to accept EquipmentInstance from inventory:

```gdscript
func can_accept_drop(data: Variant) -> bool:
    if not super.can_accept_drop(data):
        return false

    # Accept EquipmentInstance
    if data is EquipmentInstance:
        return _is_compatible_equipment(data)

    return false

func _is_compatible_equipment(equipment: EquipmentInstance) -> bool:
    var item_type := equipment.item_type
    # Check if equipment type matches slot type
    # Weapons: w01-w99 -> main_hand/off_hand
    # Armor: hd=head, tr=torso, ar=arms, lg=legs, fe=feet
    # Misc slots accept anything
```

**Verification:** Drag equipment from inventory to matching slot, verify it works

---

## Task 8: Implement Equip from Inventory

**File:** `scripts/ui/components/equipment_slot.gd`

**Changes:**
Modify `handle_drop()` to equip item and update character:

```gdscript
func handle_drop(data: Variant, source_slot: DragDropSlot) -> bool:
    if data is EquipmentInstance:
        var old_equipment: Variant = _slot_data

        # Set new equipment on slot
        set_slot_data(data)

        # Remove from inventory if source was inventory slot
        if source_slot is InventorySlot:
            PlayerData.remove_from_inventory(data)

        # If we had old equipment, add to inventory
        if old_equipment:
            PlayerData.add_to_inventory(old_equipment)

        equipment_changed.emit(self, old_equipment, data)
        return true

    return super.handle_drop(data, source_slot)
```

**Verification:** Drag from inventory to equipment slot, verify equipped and removed from inventory

---

## Task 9: Connect Equipment Slots to Character

**File:** `scripts/ui/panels/character_panel.gd`

**Changes:**
When equipment changes via drag-drop, update the actual Character object:

```gdscript
func _on_equipment_changed(_slot: EquipmentSlot, old_item: Variant, new_item: Variant, source_slot: EquipmentSlot) -> void:
    # Update actual character equipment
    if _character_data is Character:
        var slot_name := source_slot.get_slot_type_string()
        if new_item:
            _character_data.equip_instance(new_item, slot_name)
        else:
            _character_data.unequip(slot_name)

    equipment_changed.emit(source_slot, old_item, new_item)
```

**Verification:** Equip item, verify CharacterDisplay updates automatically

---

## Task 10: Implement Unequip to Inventory

**File:** `scripts/ui/panels/character_panel.gd`

**Changes:**
Modify unequip handler to add item to inventory:

```gdscript
func _on_unequip_requested(slot: EquipmentSlot) -> void:
    var equipment: Variant = slot.get_equipment()
    if equipment:
        # Unequip from character
        if _character_data is Character:
            var slot_name := slot.get_slot_type_string()
            _character_data.unequip(slot_name)

        # Add to shared inventory
        if equipment is EquipmentInstance:
            PlayerData.add_to_inventory(equipment)

        # Clear slot display
        slot.clear_equipment()

    unequip_requested.emit(slot)
```

**Verification:** Right-click equipment slot, verify item appears in inventory

---

## Task 11: Final Integration Testing

**Verification steps:**
1. Run game, verify party creates with 10 items in inventory
2. Open party menu, verify inventory shows equipment icons
3. Drag item from inventory to matching equipment slot
4. Verify CharacterDisplay updates with new equipment
5. Verify item removed from inventory
6. Right-click equipment slot to unequip
7. Verify item appears in inventory
8. Verify CharacterDisplay updates (equipment removed)
9. Drag item to different character's equipment slot
10. Verify cross-character equipping works

---

## Files to Create
- `scripts/display/equipment_icon_renderer.gd` - Icon compositing

## Files to Modify
1. `scripts/equipment/equipment_instance.gd` - Add get_icon()
2. `scenes/world/world_test_scene.gd` - Populate inventory on creation
3. `autoload/player_data.gd` - Add inventory_changed signal
4. `scripts/ui/panels/inventory_panel.gd` - Use equipment inventory
5. `scripts/ui/components/inventory_slot.gd` - Handle EquipmentInstance
6. `scripts/ui/components/equipment_slot.gd` - Cross-slot drag-drop
7. `scripts/ui/panels/character_panel.gd` - Connect to character, handle unequip

## Dependencies
- StarquillDisplayBuilder (existing)
- EquipmentFactory (existing)
- PlayerData.inventory (existing)
- DragDropManager (existing)
