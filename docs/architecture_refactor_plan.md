# Starquill Architecture Refactor Plan

## Executive Summary

This plan implements 5 architectural improvements to prepare Starquill for modal UI systems (Party Menu, Inventory, Character Sheets, Pause, Settings). The refactor follows a full systematic approach with EventBus migration.

**Timeline**: ~5-7 implementation sessions
**Approach**: Systematic refactor before adding new features
**Goal**: Clean MVC architecture with centralized state management

---

## Current State Analysis

### Existing Systems
- **Bus Architecture**: 9 separate autoload buses (BusFlow, BusActors, BusCombat, BusInventory, BusUi, BusSave, BusAudio, BusWorld, BusParty)
- **State Management**: Scattered across PlayerData, Game, individual buses
- **Inventory**: Basic Items/Consumables/KeyItems classes, array in PlayerData
- **Party**: Party class with members array, managed by PlayerData
- **UI**: No modal system, no UIManager, no menu infrastructure
- **Data-driven**: Species and equipment use JSON; Items do not

### Gaps to Address
1. No centralized pause/mode state management
2. Multiple bus singletons create coupling and signal explosion
3. Inventory lacks MVC separation (data mixed with logic)
4. No UI modal management infrastructure
5. Items are not data-driven
6. No controller layer for inventory/party operations

---

## Implementation Phases

### Phase 1: Foundation Layer (Sessions 1-2)

#### 1.1 Create GameManager (Session 1, Part A)

**File**: `autoload/game_manager.gd`

```gdscript
extends Node
class_name GameManager

# Signals
signal scene_changed(new_scene: StringName)
signal paused_changed(is_paused: bool)
signal mode_changed(old_mode: StringName, new_mode: StringName)
signal input_mode_changed(new_input_mode: StringName)

# State
var _current_scene: StringName = &""
var _is_paused: bool = false
var _mode: StringName = &"gameplay"  # gameplay, menu, dialogue, combat
var _input_mode: StringName = &"world"  # world, ui, dialogue

# Scene management
func set_scene(scene_name: StringName) -> void:
    if _current_scene == scene_name:
        return
    var old_scene = _current_scene
    _current_scene = scene_name
    scene_changed.emit(scene_name)

func get_current_scene() -> StringName:
    return _current_scene

# Pause management
func set_paused(value: bool) -> void:
    if _is_paused == value:
        return
    _is_paused = value
    get_tree().paused = value
    paused_changed.emit(value)

func is_paused() -> bool:
    return _is_paused

# Mode management
func set_mode(value: StringName) -> void:
    if _mode == value:
        return
    var old_mode = _mode
    _mode = value
    mode_changed.emit(old_mode, value)

    # Auto-pause for menu mode
    if value == &"menu":
        set_paused(true)
    elif old_mode == &"menu" and value == &"gameplay":
        set_paused(false)

func get_mode() -> StringName:
    return _mode

# Input mode management
func set_input_mode(value: StringName) -> void:
    if _input_mode == value:
        return
    _input_mode = value
    input_mode_changed.emit(value)

func get_input_mode() -> StringName:
    return _input_mode

# Convenience checks
func is_in_gameplay() -> bool:
    return _mode == &"gameplay"

func is_in_menu() -> bool:
    return _mode == &"menu"

func is_in_dialogue() -> bool:
    return _mode == &"dialogue"

func is_in_combat() -> bool:
    return _mode == &"combat"
```

**Register in project.godot**:
```ini
[autoload]
GameManager="*res://autoload/game_manager.gd"
```

---

#### 1.2 Create EventBus (Session 1, Part B)

**File**: `autoload/event_bus.gd`

```gdscript
extends Node
class_name EventBus

# ============================================
# FLOW / SCENE SIGNALS (from BusFlow)
# ============================================
signal boot_completed()
signal level_will_change(from_level_id: String, to_level_id: String)
signal level_loaded(level_id: String, root: Node)
signal level_unloaded(level_id: String)

# ============================================
# PARTY SIGNALS (from BusParty)
# ============================================
# Movement
signal party_moved(from_tile: Vector2i, to_tile: Vector2i)
signal party_movement_started(path: Array[Vector2i])
signal party_movement_completed()
signal party_movement_blocked(reason: String)
signal party_teleported(to_tile: Vector2i)

# Members
signal party_member_added(character: Character, position: int)
signal party_member_removed(character: Character)
signal party_member_swapped(position_a: int, position_b: int)
signal party_size_changed(new_size: int)
signal party_leader_changed(new_leader: Character)

# State
signal party_data_saved(data: Dictionary)
signal party_data_loaded(data: Dictionary)

# ============================================
# INVENTORY SIGNALS (from BusInventory)
# ============================================
signal inventory_opened()
signal inventory_closed()
signal item_added(item_data: ItemData, quantity: int)
signal item_removed(item_data: ItemData, quantity: int)
signal item_used(item_data: ItemData, target: Character)
signal equipment_equipped(character: Character, slot: String, equipment: EquipmentInstance)
signal equipment_unequipped(character: Character, slot: String, equipment: EquipmentInstance)

# ============================================
# UI SIGNALS (from BusUi)
# ============================================
signal modal_opened(modal_name: StringName)
signal modal_closed(modal_name: StringName)
signal tooltip_requested(text: String, position: Vector2)
signal tooltip_hidden()
signal notification_shown(message: String, type: String)

# ============================================
# COMBAT SIGNALS (from BusCombat)
# ============================================
signal combat_started(enemies: Array)
signal combat_ended(victory: bool)
signal turn_started(actor: Variant)
signal turn_ended(actor: Variant)
signal damage_dealt(attacker: Variant, target: Variant, amount: int)

# ============================================
# ACTOR SIGNALS (from BusActors)
# ============================================
signal actor_spawned(actor: Variant)
signal actor_despawned(actor: Variant)
signal actor_died(actor: Variant)
signal stat_changed(actor: Variant, stat: String, old_value: int, new_value: int)

# ============================================
# WORLD SIGNALS (from BusWorld)
# ============================================
signal world_created(world_name: String, seed: int)
signal world_saved(world_name: String)
signal tile_modified(pos: Vector2i, tile: Tile)
signal location_entered(location_data: LocationData)
signal visibility_updated(visible_tiles: Array[Vector2i])

# ============================================
# SAVE/LOAD SIGNALS (from BusSave)
# ============================================
signal save_started(slot: int)
signal save_completed(slot: int, success: bool)
signal load_started(slot: int)
signal load_completed(slot: int, success: bool)

# ============================================
# AUDIO SIGNALS (from BusAudio)
# ============================================
signal music_changed(track: String)
signal sfx_played(sound: String)
signal volume_changed(bus: String, volume: float)
```

**Register in project.godot**:
```ini
[autoload]
EventBus="*res://autoload/event_bus.gd"
```

---

### Phase 2: UI Infrastructure (Session 2)

#### 2.1 Create UIManager (Session 2, Part A)

**File**: `autoload/ui_manager.gd`

```gdscript
extends CanvasLayer
class_name UIManager

# Menu stack for modal management
var _menu_stack: Array[BaseMenu] = []

# Menu scene references (set in _ready or via exported vars)
var party_menu_scene: PackedScene
var pause_menu_scene: PackedScene
var settings_menu_scene: PackedScene

func _ready() -> void:
    # Load menu scenes
    party_menu_scene = preload("res://scenes/ui/party_menu.tscn")
    pause_menu_scene = preload("res://scenes/ui/pause_menu.tscn")
    settings_menu_scene = preload("res://scenes/ui/settings_menu.tscn")

    layer = 100  # Render above game world

func _input(event: InputEvent) -> void:
    if event.is_action_pressed("ui_cancel"):
        if not _menu_stack.is_empty():
            close_top_menu()
            get_viewport().set_input_as_handled()

# Open a menu from scene
func open_menu(menu_scene: PackedScene) -> BaseMenu:
    if menu_scene == null:
        push_error("UIManager: Cannot open null menu scene")
        return null

    var menu := menu_scene.instantiate() as BaseMenu
    if menu == null:
        push_error("UIManager: Menu scene does not inherit from BaseMenu")
        return null

    add_child(menu)
    _menu_stack.push_back(menu)

    menu.opened.connect(_on_menu_opened.bind(menu))
    menu.closed.connect(_on_menu_closed.bind(menu))

    menu.open()

    GameManager.set_mode(&"menu")
    EventBus.modal_opened.emit(menu.menu_name)

    return menu

# Close the top menu
func close_top_menu() -> void:
    if _menu_stack.is_empty():
        return

    var top_menu := _menu_stack.pop_back()
    top_menu.close()

# Close all menus
func close_all_menus() -> void:
    while not _menu_stack.is_empty():
        close_top_menu()

# Get the currently active menu
func get_active_menu() -> BaseMenu:
    if _menu_stack.is_empty():
        return null
    return _menu_stack[-1]

# Check if any menu is open
func is_menu_open() -> bool:
    return not _menu_stack.is_empty()

# Convenience methods for specific menus
func open_party_menu() -> BaseMenu:
    return open_menu(party_menu_scene)

func open_pause_menu() -> BaseMenu:
    return open_menu(pause_menu_scene)

func open_settings_menu() -> BaseMenu:
    return open_menu(settings_menu_scene)

# Signal handlers
func _on_menu_opened(menu: BaseMenu) -> void:
    print("UIManager: Menu opened - %s" % menu.menu_name)

func _on_menu_closed(menu: BaseMenu) -> void:
    print("UIManager: Menu closed - %s" % menu.menu_name)
    menu.queue_free()

    EventBus.modal_closed.emit(menu.menu_name)

    # Restore gameplay mode if no menus remain
    if _menu_stack.is_empty():
        GameManager.set_mode(&"gameplay")
```

**Register in project.godot**:
```ini
[autoload]
UIManager="*res://autoload/ui_manager.gd"
```

---

#### 2.2 Create BaseMenu Template (Session 2, Part B)

**File**: `scripts/ui/base_menu.gd`

```gdscript
extends Control
class_name BaseMenu

signal opened()
signal closed()
signal focus_requested()

@export var menu_name: StringName = &"base_menu"
@export var close_on_escape: bool = true
@export var pause_on_open: bool = true

var is_open: bool = false

func _ready() -> void:
    visibility_changed.connect(_on_visibility_changed)
    hide()

# Template method pattern - override in subclasses
func open() -> void:
    if is_open:
        return

    is_open = true
    show()
    _on_open()
    opened.emit()

    if pause_on_open:
        GameManager.set_paused(true)

    # Focus first focusable control
    call_deferred("_grab_focus")

# Template method pattern - override in subclasses
func close() -> void:
    if not is_open:
        return

    is_open = false
    _on_close()
    closed.emit()

    hide()

# Override in subclasses for custom open behavior
func _on_open() -> void:
    pass

# Override in subclasses for custom close behavior
func _on_close() -> void:
    pass

# Find and focus first focusable control
func _grab_focus() -> void:
    var focusable := _find_first_focusable(self)
    if focusable:
        focusable.grab_focus()

# Recursively find first focusable control
func _find_first_focusable(node: Node) -> Control:
    if node is Control and node.focus_mode != Control.FOCUS_NONE:
        return node as Control

    for child in node.get_children():
        var result := _find_first_focusable(child)
        if result:
            return result

    return null

func _on_visibility_changed() -> void:
    if visible:
        focus_requested.emit()

func _input(event: InputEvent) -> void:
    if not is_open:
        return

    if close_on_escape and event.is_action_pressed("ui_cancel"):
        close()
        get_viewport().set_input_as_handled()
```

---

### Phase 3: Model Layer Refactor (Sessions 3-4)

#### 3.1 Create ItemData Resource (Session 3, Part A)

**File**: `model/items/item_data.gd`

```gdscript
extends Resource
class_name ItemData

@export_group("Identity")
@export var id: StringName = &""
@export var display_name: String = ""
@export var description: String = ""
@export var icon: Texture2D = null

@export_group("Stacking")
@export var max_stack: int = 1
@export var is_stackable: bool = false

@export_group("Type")
@export var item_type: StringName = &"misc"  # misc, consumable, key_item, material

@export_group("Value")
@export var sell_value: int = 0
@export var buy_value: int = 0

@export_group("Usage")
@export var is_usable: bool = false
@export var use_in_combat: bool = false
@export var use_in_field: bool = false
@export var effect_script: Script = null  # ItemEffect script

@export_group("Stats (for consumables)")
@export var stat_effects: Dictionary = {}  # StatType -> int

func can_use_in_context(context: StringName) -> bool:
    match context:
        &"combat":
            return use_in_combat
        &"field":
            return use_in_field
        _:
            return false
```

---

#### 3.2 Create ItemStack Resource (Session 3, Part B)

**File**: `model/items/item_stack.gd`

```gdscript
extends Resource
class_name ItemStack

signal quantity_changed(old_quantity: int, new_quantity: int)
signal stack_depleted()

@export var item_data: ItemData = null
@export var quantity: int = 1

func _init(p_item_data: ItemData = null, p_quantity: int = 1) -> void:
    item_data = p_item_data
    quantity = p_quantity

func add(amount: int) -> int:
    if item_data == null or not item_data.is_stackable:
        return amount  # Return overflow

    var old_quantity := quantity
    var max_can_add := item_data.max_stack - quantity
    var actual_add := mini(amount, max_can_add)

    quantity += actual_add
    quantity_changed.emit(old_quantity, quantity)

    return amount - actual_add  # Return overflow

func remove(amount: int) -> int:
    var old_quantity := quantity
    var actual_remove := mini(amount, quantity)

    quantity -= actual_remove
    quantity_changed.emit(old_quantity, quantity)

    if quantity <= 0:
        stack_depleted.emit()

    return actual_remove

func can_merge_with(other: ItemStack) -> bool:
    if other == null or item_data == null or other.item_data == null:
        return false

    return item_data.id == other.item_data.id and item_data.is_stackable

func merge_with(other: ItemStack) -> int:
    if not can_merge_with(other):
        return other.quantity

    var overflow := add(other.quantity)
    other.quantity = overflow

    if overflow == 0:
        other.stack_depleted.emit()

    return overflow

func is_empty() -> bool:
    return quantity <= 0

func is_full() -> bool:
    return item_data != null and quantity >= item_data.max_stack
```

---

#### 3.3 Create Inventory Model (Session 3, Part C)

**File**: `model/inventory/inventory.gd`

```gdscript
extends Resource
class_name Inventory

signal changed()
signal item_added(stack: ItemStack)
signal item_removed(stack: ItemStack)
signal capacity_changed(old_capacity: int, new_capacity: int)

@export var capacity: int = 24
var slots: Array[ItemStack] = []

func _init(p_capacity: int = 24) -> void:
    capacity = p_capacity
    slots.resize(capacity)

# Add item to inventory (smart stacking)
func add_item(item_data: ItemData, amount: int = 1) -> bool:
    if item_data == null or amount <= 0:
        return false

    var remaining := amount

    # Try to stack with existing items first
    if item_data.is_stackable:
        for stack in slots:
            if stack != null and stack.item_data.id == item_data.id and not stack.is_full():
                var added := stack.add(remaining)
                remaining = stack.add(remaining)
                if remaining == 0:
                    changed.emit()
                    return true

    # Create new stacks for remaining quantity
    while remaining > 0:
        var empty_slot_index := _find_empty_slot()
        if empty_slot_index == -1:
            return false  # Inventory full

        var stack_amount := mini(remaining, item_data.max_stack if item_data.is_stackable else 1)
        var new_stack := ItemStack.new(item_data, stack_amount)

        slots[empty_slot_index] = new_stack
        new_stack.stack_depleted.connect(_on_stack_depleted.bind(empty_slot_index))

        item_added.emit(new_stack)
        remaining -= stack_amount

    changed.emit()
    return true

# Remove item by ItemData
func remove_item(item_data: ItemData, amount: int = 1) -> int:
    if item_data == null or amount <= 0:
        return 0

    var remaining := amount

    for i in range(slots.size()):
        var stack := slots[i]
        if stack != null and stack.item_data.id == item_data.id:
            var removed := stack.remove(remaining)
            remaining -= removed

            if stack.is_empty():
                slots[i] = null
                item_removed.emit(stack)

            if remaining == 0:
                break

    if remaining < amount:
        changed.emit()

    return amount - remaining

# Remove item at specific slot index
func remove_at_slot(slot_index: int, amount: int = 1) -> int:
    if slot_index < 0 or slot_index >= slots.size():
        return 0

    var stack := slots[slot_index]
    if stack == null:
        return 0

    var removed := stack.remove(amount)

    if stack.is_empty():
        slots[slot_index] = null
        item_removed.emit(stack)

    changed.emit()
    return removed

# Get item count by ItemData
func get_item_count(item_data: ItemData) -> int:
    if item_data == null:
        return 0

    var total := 0
    for stack in slots:
        if stack != null and stack.item_data.id == item_data.id:
            total += stack.quantity

    return total

# Check if inventory has item
func has_item(item_data: ItemData, amount: int = 1) -> bool:
    return get_item_count(item_data) >= amount

# Get stack at slot index
func get_stack_at(slot_index: int) -> ItemStack:
    if slot_index < 0 or slot_index >= slots.size():
        return null
    return slots[slot_index]

# Find first empty slot
func _find_empty_slot() -> int:
    for i in range(slots.size()):
        if slots[i] == null:
            return i
    return -1

# Get number of empty slots
func get_empty_slot_count() -> int:
    var count := 0
    for stack in slots:
        if stack == null:
            count += 1
    return count

# Check if inventory is full
func is_full() -> bool:
    return get_empty_slot_count() == 0

# Clear inventory
func clear() -> void:
    slots.clear()
    slots.resize(capacity)
    changed.emit()

# Change capacity
func set_capacity(new_capacity: int) -> void:
    if new_capacity == capacity:
        return

    var old_capacity := capacity
    capacity = new_capacity
    slots.resize(capacity)
    capacity_changed.emit(old_capacity, new_capacity)
    changed.emit()

# Signal handler for depleted stacks
func _on_stack_depleted(slot_index: int) -> void:
    if slot_index >= 0 and slot_index < slots.size():
        var stack := slots[slot_index]
        slots[slot_index] = null
        if stack:
            item_removed.emit(stack)
        changed.emit()

# Serialization
func serialize() -> Dictionary:
    var slot_data := []
    for stack in slots:
        if stack != null and stack.item_data != null:
            slot_data.append({
                "item_id": stack.item_data.id,
                "quantity": stack.quantity
            })
        else:
            slot_data.append(null)

    return {
        "capacity": capacity,
        "slots": slot_data
    }

func deserialize(data: Dictionary) -> void:
    capacity = data.get("capacity", 24)
    slots.clear()
    slots.resize(capacity)

    var slot_data := data.get("slots", [])
    for i in range(mini(slot_data.size(), capacity)):
        var stack_data = slot_data[i]
        if stack_data != null and stack_data is Dictionary:
            var item_id := stack_data.get("item_id", &"")
            var quantity := stack_data.get("quantity", 1)

            # Load ItemData from registry (implement ItemRegistry singleton)
            var item_data := ItemRegistry.get_item(item_id)
            if item_data:
                slots[i] = ItemStack.new(item_data, quantity)

    changed.emit()
```

---

#### 3.4 Create InventoryController (Session 4, Part A)

**File**: `controller/inventory_controller.gd`

```gdscript
extends Node
class_name InventoryController

# Reference to model
var inventory: Inventory

func _init(p_inventory: Inventory) -> void:
    inventory = p_inventory

# Use item on target
func use_item(item_data: ItemData, target: Character) -> bool:
    if not _can_use_item(item_data):
        return false

    # Execute item effect
    if item_data.effect_script:
        var effect = item_data.effect_script.new()
        if effect.has_method("apply"):
            effect.apply(target, item_data)

    # Apply stat effects for consumables
    if not item_data.stat_effects.is_empty():
        for stat_type in item_data.stat_effects:
            var amount: int = item_data.stat_effects[stat_type]
            target.stats.add_stat(stat_type, amount)

    # Remove one from inventory
    inventory.remove_item(item_data, 1)

    # Emit event
    EventBus.item_used.emit(item_data, target)

    return true

# Equip equipment to character
func equip_equipment(equipment: EquipmentInstance, character: Character) -> bool:
    if character == null or equipment == null:
        return false

    # Unequip current item in that slot if any
    var slot := StarquillData.get_slot_for_item_type(equipment.item_type)
    var old_equipment := character.get_equipment_in_slot(slot)

    # Equip new item
    var success := character.equip_instance(equipment)

    if success:
        # Remove from inventory (equipment instances)
        PlayerData.remove_from_inventory(equipment)

        # Add old equipment back to inventory if it existed
        if old_equipment != null:
            PlayerData.add_to_inventory(old_equipment)

        EventBus.equipment_equipped.emit(character, slot, equipment)

    return success

# Unequip equipment from character
func unequip_equipment(character: Character, slot: String) -> bool:
    if character == null:
        return false

    var equipment := character.get_equipment_in_slot(slot)
    if equipment == null:
        return false

    # Add to inventory
    PlayerData.add_to_inventory(equipment)

    # Unequip from character
    character.unequip(slot)

    EventBus.equipment_unequipped.emit(character, slot, equipment)

    return true

# Sort inventory by criteria
func sort_inventory(sort_type: StringName) -> void:
    match sort_type:
        &"name":
            _sort_by_name()
        &"type":
            _sort_by_type()
        &"value":
            _sort_by_value()
        _:
            push_warning("Unknown sort type: %s" % sort_type)

# Private helpers
func _can_use_item(item_data: ItemData) -> bool:
    if item_data == null or not item_data.is_usable:
        return false

    var context := GameManager.get_mode()
    return item_data.can_use_in_context(context)

func _sort_by_name() -> void:
    # Implementation depends on sorting algorithm
    inventory.changed.emit()

func _sort_by_type() -> void:
    # Implementation depends on sorting algorithm
    inventory.changed.emit()

func _sort_by_value() -> void:
    # Implementation depends on sorting algorithm
    inventory.changed.emit()
```

---

### Phase 4: View Layer (Sessions 5-6)

This phase creates the actual UI scenes and panels. Due to the complexity of scene files, I'll provide the structure and key scripts.

#### 4.1 Party Menu (Main Modal) (Session 5, Part A)

**File**: `scenes/ui/party_menu.tscn` (Scene)
**Script**: `scripts/ui/party_menu.gd`

```gdscript
extends BaseMenu
class_name PartyMenu

# UI References
@onready var tab_container: TabContainer = $TabContainer
@onready var inventory_panel: InventoryPanel = $TabContainer/Inventory
@onready var character_tabs: TabContainer = $TabContainer/Characters

# State
var current_character_index: int = 0

func _ready() -> void:
    super._ready()
    menu_name = &"party_menu"

    # Set up character tabs
    _setup_character_tabs()

    # Connect signals
    EventBus.party_member_added.connect(_on_party_member_added)
    EventBus.party_member_removed.connect(_on_party_member_removed)

func _on_open() -> void:
    _refresh_party_data()

func _setup_character_tabs() -> void:
    # Clear existing tabs
    for child in character_tabs.get_children():
        child.queue_free()

    # Create tab for each party member
    for i in range(PlayerData.get_party_size()):
        var character := PlayerData.party.members[i]
        var sheet := CharacterSheetPanel.new()
        sheet.set_character(character)
        sheet.name = "Character%d" % i
        character_tabs.add_child(sheet)
        character_tabs.set_tab_title(i, character.display_name)

func _refresh_party_data() -> void:
    inventory_panel.refresh()
    _refresh_character_sheets()

func _refresh_character_sheets() -> void:
    for child in character_tabs.get_children():
        if child is CharacterSheetPanel:
            child.refresh()

func _on_party_member_added(character: Character, position: int) -> void:
    _setup_character_tabs()

func _on_party_member_removed(character: Character) -> void:
    _setup_character_tabs()
```

---

#### 4.2 Inventory Panel (Session 5, Part B)

**Script**: `scripts/ui/inventory_panel.gd`

```gdscript
extends Panel
class_name InventoryPanel

# UI References
@onready var grid_container: GridContainer = $VBoxContainer/GridContainer
@onready var info_panel: Panel = $VBoxContainer/InfoPanel
@onready var item_name_label: Label = $VBoxContainer/InfoPanel/VBoxContainer/NameLabel
@onready var item_desc_label: Label = $VBoxContainer/InfoPanel/VBoxContainer/DescLabel

# State
var inventory: Inventory
var inventory_controller: InventoryController
var selected_slot: int = -1

# Item slot scene
const ITEM_SLOT_SCENE := preload("res://scenes/ui/item_slot.tscn")

func _ready() -> void:
    # Get shared inventory from PlayerData (to be refactored)
    inventory = PlayerData.shared_inventory  # Will create this
    inventory_controller = InventoryController.new(inventory)

    # Connect signals
    inventory.changed.connect(_on_inventory_changed)

    # Initial render
    refresh()

func refresh() -> void:
    _clear_slots()
    _render_slots()

func _clear_slots() -> void:
    for child in grid_container.get_children():
        child.queue_free()

func _render_slots() -> void:
    for i in range(inventory.capacity):
        var slot_ui := ITEM_SLOT_SCENE.instantiate() as ItemSlotUI
        grid_container.add_child(slot_ui)

        var stack := inventory.get_stack_at(i)
        if stack != null:
            slot_ui.set_stack(stack)

        slot_ui.slot_clicked.connect(_on_slot_clicked.bind(i))
        slot_ui.slot_hovered.connect(_on_slot_hovered.bind(i))

func _on_slot_clicked(slot_index: int) -> void:
    selected_slot = slot_index
    var stack := inventory.get_stack_at(slot_index)

    if stack != null and stack.item_data.is_usable:
        _show_use_menu(stack)

func _on_slot_hovered(slot_index: int) -> void:
    var stack := inventory.get_stack_at(slot_index)

    if stack != null:
        item_name_label.text = stack.item_data.display_name
        item_desc_label.text = stack.item_data.description
    else:
        item_name_label.text = ""
        item_desc_label.text = ""

func _show_use_menu(stack: ItemStack) -> void:
    # Show context menu for using item
    # Implementation depends on your menu system
    pass

func _on_inventory_changed() -> void:
    refresh()
```

---

#### 4.3 Character Sheet Panel (Session 6, Part A)

**Script**: `scripts/ui/character_sheet_panel.gd`

```gdscript
extends Panel
class_name CharacterSheetPanel

# UI References
@onready var character_display: CharacterDisplay = $VBoxContainer/CharacterDisplay
@onready var name_label: Label = $VBoxContainer/NameLabel
@onready var stats_container: VBoxContainer = $VBoxContainer/StatsContainer

# Equipment slot UI references
@onready var head_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/HeadSlot
@onready var torso_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/TorsoSlot
@onready var arms_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/ArmsSlot
@onready var legs_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/LegsSlot
@onready var feet_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/FeetSlot
@onready var main_hand_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/MainHandSlot
@onready var off_hand_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/OffHandSlot

# State
var character: Character = null
var inventory_controller: InventoryController

func _ready() -> void:
    # Initialize inventory controller
    inventory_controller = InventoryController.new(PlayerData.shared_inventory)

    # Connect equipment slot signals
    _connect_equipment_slots()

func set_character(p_character: Character) -> void:
    if character and character.model_changed.is_connected(_on_character_changed):
        character.model_changed.disconnect(_on_character_changed)

    character = p_character

    if character:
        character.model_changed.connect(_on_character_changed)
        refresh()

func refresh() -> void:
    if character == null:
        return

    # Update display
    character_display.set_character(character)
    name_label.text = character.display_name

    # Update stats
    _refresh_stats()

    # Update equipment slots
    _refresh_equipment_slots()

func _refresh_stats() -> void:
    # Clear existing stat labels
    for child in stats_container.get_children():
        child.queue_free()

    # Create stat labels
    for stat_type in [StatType.Type.STR, StatType.Type.DEX, StatType.Type.CON,
                      StatType.Type.INT, StatType.Type.WIS, StatType.Type.CHA]:
        var label := Label.new()
        label.text = "%s: %d" % [StatType.get_name(stat_type), character.stats.get_stat(stat_type)]
        stats_container.add_child(label)

func _refresh_equipment_slots() -> void:
    head_slot.set_equipment(character.head)
    torso_slot.set_equipment(character.torso)
    arms_slot.set_equipment(character.arms)
    legs_slot.set_equipment(character.legs)
    feet_slot.set_equipment(character.feet)
    main_hand_slot.set_equipment(character.main_hand)
    off_hand_slot.set_equipment(character.off_hand)

func _connect_equipment_slots() -> void:
    head_slot.unequip_requested.connect(_on_unequip_requested.bind("head"))
    torso_slot.unequip_requested.connect(_on_unequip_requested.bind("torso"))
    arms_slot.unequip_requested.connect(_on_unequip_requested.bind("arms"))
    legs_slot.unequip_requested.connect(_on_unequip_requested.bind("legs"))
    feet_slot.unequip_requested.connect(_on_unequip_requested.bind("feet"))
    main_hand_slot.unequip_requested.connect(_on_unequip_requested.bind("main_hand"))
    off_hand_slot.unequip_requested.connect(_on_unequip_requested.bind("off_hand"))

func _on_unequip_requested(slot: String) -> void:
    inventory_controller.unequip_equipment(character, slot)

func _on_character_changed() -> void:
    refresh()
```

---

#### 4.4 Pause Menu (Session 6, Part B)

**Script**: `scripts/ui/pause_menu.gd`

```gdscript
extends BaseMenu
class_name PauseMenu

@onready var resume_button: Button = $VBoxContainer/ResumeButton
@onready var settings_button: Button = $VBoxContainer/SettingsButton
@onready var quit_button: Button = $VBoxContainer/QuitButton

func _ready() -> void:
    super._ready()
    menu_name = &"pause_menu"

    # Connect buttons
    resume_button.pressed.connect(_on_resume_pressed)
    settings_button.pressed.connect(_on_settings_pressed)
    quit_button.pressed.connect(_on_quit_pressed)

func _on_resume_pressed() -> void:
    close()

func _on_settings_pressed() -> void:
    UIManager.open_settings_menu()

func _on_quit_pressed() -> void:
    # Show confirmation dialog
    # Then quit to main menu
    get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
```

---

### Phase 5: Integration & Migration (Session 7)

#### 5.1 Migrate Bus Signals to EventBus

**Tasks**:
1. Update all `BusFlow.emit()` calls to `EventBus.emit()`
2. Update all signal connections from individual buses to EventBus
3. Remove old bus autoload files
4. Update project.godot to remove old bus autoloads

**Migration Script Template**:
```bash
# Find and replace all bus signal emissions
# BusFlow -> EventBus
find . -name "*.gd" -exec sed -i 's/BusFlow\./EventBus./g' {} \;

# BusParty -> EventBus
find . -name "*.gd" -exec sed -i 's/BusParty\./EventBus./g' {} \;

# ... repeat for all buses
```

**Manual Updates Required**:
- `scripts/world/world_map.gd`: Update all `BusWorld.*` and `BusParty.*` calls
- `autoload/player_data.gd`: Update all `BusParty.*` and `BusInventory.*` calls
- `autoload/game.gd`: Update `BusFlow.*` and `BusAudio.*` calls
- All equipment-related files: Update `BusInventory.*` calls

---

#### 5.2 Update PlayerData with Inventory Model

**File**: `autoload/player_data.gd`

**Changes**:
```gdscript
extends Node

var party: Party = null
var shared_inventory: Inventory = null  # NEW: Inventory model
var current_world_position: Vector2i = Vector2i.ZERO
var gold: int = 0
var game_time: float = 0.0

# Remove old equipment array
# var inventory: Array[EquipmentInstance] = []

func _ready():
    party = Party.new()
    party.name = "PlayerParty"

    # NEW: Initialize shared inventory
    shared_inventory = Inventory.new(24)

func initialize_new_game() -> void:
    party.members.clear()
    shared_inventory.clear()  # NEW: Use Inventory model
    gold = 100
    game_time = 0.0
    is_new_game = true
    EventBus.party_data_loaded.emit({})  # UPDATED: EventBus

# DEPRECATED: Remove these methods
# func add_to_inventory(equipment: EquipmentInstance) -> void
# func remove_from_inventory(equipment: EquipmentInstance) -> void

# Use shared_inventory.add_item() and shared_inventory.remove_item() instead
```

---

#### 5.3 Input Handling Integration

**File**: `scripts/input/input_handler.gd` (NEW)

```gdscript
extends Node

func _input(event: InputEvent) -> void:
    # Check input mode from GameManager
    var input_mode := GameManager.get_input_mode()

    match input_mode:
        &"world":
            _handle_world_input(event)
        &"ui":
            _handle_ui_input(event)
        &"dialogue":
            _handle_dialogue_input(event)
        _:
            pass

func _handle_world_input(event: InputEvent) -> void:
    if event.is_action_pressed("ui_cancel"):
        UIManager.open_pause_menu()
        get_viewport().set_input_as_handled()

    if event.is_action_pressed("open_party_menu"):
        UIManager.open_party_menu()
        get_viewport().set_input_as_handled()

func _handle_ui_input(event: InputEvent) -> void:
    # UI handles its own input via BaseMenu
    pass

func _handle_dialogue_input(event: InputEvent) -> void:
    # Dialogue system handles input
    pass
```

---

## Testing Checklist

### Phase 1: Foundation
- [ ] GameManager state transitions work correctly
- [ ] GameManager pause/unpause affects tree
- [ ] EventBus signals are emitted and received
- [ ] No errors when loading autoloads

### Phase 2: UI Infrastructure
- [ ] UIManager can open/close menus
- [ ] Menu stack operates correctly (LIFO)
- [ ] BaseMenu template methods work
- [ ] Escape key closes top menu

### Phase 3: Model Layer
- [ ] ItemStack stacking logic works
- [ ] Inventory add/remove operations work
- [ ] Inventory signals emit correctly
- [ ] ItemData resources can be created
- [ ] InventoryController methods work

### Phase 4: View Layer
- [ ] Party menu opens and displays correctly
- [ ] Inventory panel shows items
- [ ] Character sheet displays character data
- [ ] Equipment slots show equipped items
- [ ] Pause menu buttons work

### Phase 5: Integration
- [ ] All old bus calls migrated to EventBus
- [ ] PlayerData uses new Inventory model
- [ ] No references to old bus autoloads
- [ ] Input handling routes correctly based on mode
- [ ] Save/load works with new architecture

---

## File Structure After Refactor

```
autoload/
  game_manager.gd          # NEW: Centralized state
  event_bus.gd             # NEW: Unified event bus
  ui_manager.gd            # NEW: Modal management
  player_data.gd           # UPDATED: Uses Inventory model
  game.gd                  # UPDATED: Uses EventBus
  scene_loader.gd          # UPDATED: Uses EventBus
  config_manager.gd        # Unchanged
  color_manager.gd         # Unchanged

model/
  items/
    item_data.gd           # NEW: Item definition resource
    item_stack.gd          # NEW: Stack with quantity
    item_effect.gd         # NEW: Base class for effects
  inventory/
    inventory.gd           # NEW: Inventory model resource

controller/
  inventory_controller.gd  # NEW: Inventory operations
  party_controller.gd      # NEW: Party management logic

scripts/
  ui/
    base_menu.gd           # NEW: Template for menus
    party_menu.gd          # NEW: Main party modal
    inventory_panel.gd     # NEW: Inventory UI
    character_sheet_panel.gd  # NEW: Character sheet
    pause_menu.gd          # NEW: Pause screen
    settings_menu.gd       # NEW: Settings
    item_slot_ui.gd        # NEW: Inventory slot
    equipment_slot_ui.gd   # NEW: Equipment slot
  input/
    input_handler.gd       # NEW: Mode-based input routing

  # Existing files - many will need updates for EventBus
  character/
  species/
  equipment/
  world/
  display/
  core/

data/
  items/                   # NEW: Item .tres resources
    health_potion.tres
    mana_potion.tres
    antidote.tres
    ...

scenes/
  ui/                      # NEW: UI scenes
    party_menu.tscn
    inventory_panel.tscn
    character_sheet_panel.tscn
    pause_menu.tscn
    settings_menu.tscn
    item_slot.tscn
    equipment_slot.tscn
```

---

## Risk Mitigation

### Risk: Breaking existing gameplay
**Mitigation**:
- Test each phase thoroughly before proceeding
- Keep old bus files until Phase 5 migration complete
- Use feature flags if needed

### Risk: Signal explosion in EventBus
**Mitigation**:
- Group signals by domain (party, inventory, etc.)
- Document signal usage in EventBus comments
- Version signals if changing signatures

### Risk: Inventory refactor breaks equipment system
**Mitigation**:
- Equipment stays in Character class initially
- Shared inventory only for consumables/items at first
- Equipment migration is separate, later task

### Risk: UI complexity grows too large
**Mitigation**:
- Keep BaseMenu simple and focused
- Use composition for complex panels
- Separate concerns (model, view, controller)

---

## Success Criteria

1. All 5 architectural improvements implemented
2. Party menu with inventory and character sheets functional
3. Pause menu operational
4. All signals migrated to EventBus
5. No references to old bus autoloads
6. GameManager controls pause/mode state
7. UIManager handles all modal UI
8. Inventory uses MVC architecture
9. All existing gameplay still works
10. No performance regressions

---

## Next Steps After Completion

1. Implement ItemRegistry singleton for loading ItemData from JSON
2. Create item effect scripts for consumables
3. Add drag-and-drop for inventory UI
4. Implement settings menu functionality
5. Add animations to UI transitions
6. Create tutorial/help system using new UI framework
7. Add more complex inventory features (sorting, filtering, tabs)
8. Implement equipment comparison tooltips
9. Add character customization UI
10. Create save/load UI using new modal system

---

## Estimated Timeline

- **Phase 1** (Foundation): 4-6 hours
- **Phase 2** (UI Infrastructure): 3-4 hours
- **Phase 3** (Model Layer): 6-8 hours
- **Phase 4** (View Layer): 8-12 hours
- **Phase 5** (Integration): 4-6 hours
- **Testing & Polish**: 4-6 hours

**Total**: 29-42 hours (5-7 development sessions)

---

## Notes

- This plan assumes you're working solo
- Adjust timeline based on your familiarity with Godot
- Consider creating git branches for each phase
- Document any deviations from plan in this file
- Update CLAUDE.md with new patterns once complete
