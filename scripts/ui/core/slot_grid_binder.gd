class_name SlotGridBinder
extends RefCounted

## SlotGridBinder
## Handles data binding for slot-based grids, separating data concerns from layout.
##
## The binder manages:
## - Slot creation via injectable factory
## - Data binding (items to slots)
## - Signal forwarding from slots
## - Selection tracking
##
## Usage:
##   var binder := SlotGridBinder.new()
##   binder.setup(_grid_container, _create_inventory_slot)
##   binder.bind_items(inventory_items)
##   binder.item_selected.connect(_on_item_selected)
##
## The slot factory is a Callable that creates slots:
##   func _create_inventory_slot(index: int) -> DragDropSlot:
##       var slot := InventorySlot.new()
##       slot.slot_index = index
##       return slot

signal item_selected(index: int, data: Variant)
signal item_dropped(target_index: int, data: Variant, source_slot: Control)
signal slot_clicked(index: int, slot: DragDropSlot)
signal slot_hovered(index: int, slot: DragDropSlot)
signal slot_unhovered(index: int, slot: DragDropSlot)

# Configuration
var _grid: GridContainer
var _slots: Array[DragDropSlot] = []
var _slot_factory: Callable
var _slot_size: Vector2 = Vector2(64, 64)

# Selection tracking
var _selected_slot: DragDropSlot = null
var _selected_index: int = -1

## Setup the binder with a grid container and slot factory
## slot_factory: func(index: int) -> DragDropSlot
func setup(grid: GridContainer, slot_factory: Callable, slot_size: Vector2 = Vector2(64, 64)) -> void:
	_grid = grid
	_slot_factory = slot_factory
	_slot_size = slot_size

## Bind items to slots, creating slots as needed
## Items are bound by index - items[0] goes to slot 0, etc.
func bind_items(items: Array, extra_empty_slots: int = 0) -> void:
	var target_count := items.size() + extra_empty_slots

	# Ensure we have enough slots
	_ensure_slot_count(target_count)

	# Bind items to slots
	for i in range(_slots.size()):
		if i < items.size():
			_slots[i].set_slot_data(items[i])
		else:
			_slots[i].clear_slot()

## Ensure we have at least target_count slots
func _ensure_slot_count(target_count: int) -> void:
	# Add slots if needed
	while _slots.size() < target_count:
		_add_slot()

	# Show/hide excess slots
	for i in range(_slots.size()):
		_slots[i].visible = (i < target_count)

## Add a single slot using the factory
func _add_slot() -> void:
	if not _slot_factory.is_valid():
		push_error("SlotGridBinder: slot_factory is not valid")
		return

	var index := _slots.size()
	var slot: DragDropSlot = _slot_factory.call(index)

	if not slot:
		push_error("SlotGridBinder: slot_factory returned null for index %d" % index)
		return

	slot.slot_size = _slot_size
	slot.name = "Slot_%d" % index

	# Connect signals
	slot.slot_clicked.connect(_on_slot_clicked.bind(index))
	slot.slot_hovered.connect(_on_slot_hovered.bind(index))
	slot.slot_unhovered.connect(_on_slot_unhovered.bind(index))
	slot.item_dropped.connect(_on_item_dropped.bind(index))

	_grid.add_child(slot)
	_slots.append(slot)

## Clear all slots (but keep them in pool for reuse)
func clear() -> void:
	for slot in _slots:
		slot.clear_slot()
	_selected_slot = null
	_selected_index = -1

## Get a slot by index
func get_slot(index: int) -> DragDropSlot:
	if index >= 0 and index < _slots.size():
		return _slots[index]
	return null

## Get all slots
func get_slots() -> Array[DragDropSlot]:
	return _slots

## Get visible slot count
func get_visible_slot_count() -> int:
	var count := 0
	for slot in _slots:
		if slot.visible:
			count += 1
	return count

## Get selected slot
func get_selected_slot() -> DragDropSlot:
	return _selected_slot

## Get selected index
func get_selected_index() -> int:
	return _selected_index

## Clear selection
func clear_selection() -> void:
	if _selected_slot and is_instance_valid(_selected_slot):
		_selected_slot.set_highlighted(false)
	_selected_slot = null
	_selected_index = -1

## Highlight a specific slot
func highlight_slot(index: int, highlighted: bool) -> void:
	var slot := get_slot(index)
	if slot:
		slot.set_highlighted(highlighted)

## Clear all highlights
func clear_highlights() -> void:
	for slot in _slots:
		slot.set_highlighted(false)

## Set data for a specific slot
func set_slot_data(index: int, data: Variant) -> void:
	var slot := get_slot(index)
	if slot:
		slot.set_slot_data(data)

## Get data from a specific slot
func get_slot_data(index: int) -> Variant:
	var slot := get_slot(index)
	if slot:
		return slot.get_slot_data()
	return null

## Get all non-empty item data
func get_items() -> Array:
	var items: Array = []
	for slot in _slots:
		if slot.visible:
			var data: Variant = slot.get_slot_data()
			if data != null:
				items.append(data)
	return items

## Find first empty slot index
func find_empty_slot() -> int:
	for i in range(_slots.size()):
		if _slots[i].visible and _slots[i].is_empty():
			return i
	return -1

## Add item to first empty slot
func add_item(data: Variant) -> int:
	var empty_index := find_empty_slot()
	if empty_index >= 0:
		set_slot_data(empty_index, data)
	return empty_index

## Remove item from slot and return it
func remove_item(index: int) -> Variant:
	var slot := get_slot(index)
	if slot:
		var data: Variant = slot.get_slot_data()
		slot.clear_slot()
		return data
	return null

# Signal handlers
func _on_slot_clicked(slot: DragDropSlot, index: int) -> void:
	var data: Variant = slot.get_slot_data()

	# Deselect previous
	if _selected_slot and is_instance_valid(_selected_slot):
		_selected_slot.set_highlighted(false)

	# Toggle selection
	if _selected_slot == slot:
		_selected_slot = null
		_selected_index = -1
	else:
		_selected_slot = slot
		_selected_index = index
		slot.set_highlighted(true)

	slot_clicked.emit(index, slot)
	item_selected.emit(index, data)

func _on_slot_hovered(slot: DragDropSlot, index: int) -> void:
	slot_hovered.emit(index, slot)

func _on_slot_unhovered(slot: DragDropSlot, index: int) -> void:
	slot_unhovered.emit(index, slot)

func _on_item_dropped(slot: DragDropSlot, data: Variant, source_slot: DragDropSlot, index: int) -> void:
	item_dropped.emit(index, data, source_slot)

## Cleanup - disconnect all signals and clear slots
func cleanup() -> void:
	for i in range(_slots.size()):
		var slot := _slots[i]
		if is_instance_valid(slot):
			if slot.slot_clicked.is_connected(_on_slot_clicked):
				slot.slot_clicked.disconnect(_on_slot_clicked)
			if slot.slot_hovered.is_connected(_on_slot_hovered):
				slot.slot_hovered.disconnect(_on_slot_hovered)
			if slot.slot_unhovered.is_connected(_on_slot_unhovered):
				slot.slot_unhovered.disconnect(_on_slot_unhovered)
			if slot.item_dropped.is_connected(_on_item_dropped):
				slot.item_dropped.disconnect(_on_item_dropped)

	_slots.clear()
	_grid = null
	_selected_slot = null
	_selected_index = -1
