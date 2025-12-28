class_name ScrollableGrid
extends ScrollContainer

## ScrollableGrid
## A scrollable container with a grid of slots.
## Handles LAYOUT only: columns, spacing, slot sizing, auto-fit.
##
## Data Binding:
##   For new code, use SlotGridBinder for data binding instead of the
##   deprecated set_items/get_items methods on this class.
##
##   Example with SlotGridBinder:
##     var binder := SlotGridBinder.new()
##     binder.setup(_scrollable_grid.get_grid(), _create_slot)
##     binder.bind_items(inventory_items)
##
## This class provides:
##   - Slot creation and layout (columns, spacing)
##   - Auto-fit columns based on container width
##   - Scroll container configuration
##
## See docs/ui_ai_rules.md for the full pattern.

signal slot_clicked(index: int, slot: DragDropSlot)
signal slot_hovered(index: int, slot: DragDropSlot)
signal slot_unhovered(index: int, slot: DragDropSlot)
signal item_dropped(target_index: int, target_slot: DragDropSlot, source_slot: DragDropSlot)
signal grid_ready

# Slot type to generate
enum SlotType {
	INVENTORY,
	EQUIPMENT,
	GENERIC
}

@export var slot_count: int = 24:
	set(value):
		slot_count = maxi(0, value)
		if is_inside_tree():
			_rebuild_grid()

@export var slot_type: SlotType = SlotType.INVENTORY

@export var slot_size: Vector2 = Vector2(64, 64):
	set(value):
		slot_size = value
		_update_slot_sizes()

@export var columns: int = 0:  # 0 = auto-fit
	set(value):
		columns = maxi(0, value)
		_update_columns()

@export var spacing: int = 8:
	set(value):
		spacing = maxi(0, value)
		if _grid:
			_grid.add_theme_constant_override("h_separation", spacing)
			_grid.add_theme_constant_override("v_separation", spacing)

# Internal
var _grid: GridContainer
var _slots: Array[DragDropSlot] = []

func _ready() -> void:
	# Configure scroll container
	horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO

	_build_grid()
	_rebuild_grid()

	# Connect to resize for auto-fit columns
	resized.connect(_on_resized)

	grid_ready.emit()

func _build_grid() -> void:
	_grid = GridContainer.new()
	_grid.name = "Grid"
	_grid.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_grid.add_theme_constant_override("h_separation", spacing)
	_grid.add_theme_constant_override("v_separation", spacing)
	add_child(_grid)

	_update_columns()

func _rebuild_grid() -> void:
	# Clear existing slots
	for slot in _slots:
		slot.queue_free()
	_slots.clear()

	# Create new slots
	for i in range(slot_count):
		var slot := _create_slot(i)
		_grid.add_child(slot)
		_slots.append(slot)
		_connect_slot_signals(slot, i)

func _create_slot(index: int) -> DragDropSlot:
	var slot: DragDropSlot

	match slot_type:
		SlotType.INVENTORY:
			slot = InventorySlot.new()
		SlotType.EQUIPMENT:
			slot = EquipmentSlot.new()
		SlotType.GENERIC:
			slot = DragDropSlot.new()

	slot.slot_index = index
	slot.slot_size = slot_size
	slot.name = "Slot_%d" % index

	return slot

func _connect_slot_signals(slot: DragDropSlot, index: int) -> void:
	slot.slot_clicked.connect(_on_slot_clicked.bind(index))
	slot.slot_hovered.connect(_on_slot_hovered.bind(index))
	slot.slot_unhovered.connect(_on_slot_unhovered.bind(index))
	slot.item_dropped.connect(_on_item_dropped.bind(index))

func _on_slot_clicked(slot: DragDropSlot, index: int) -> void:
	slot_clicked.emit(index, slot)

func _on_slot_hovered(slot: DragDropSlot, index: int) -> void:
	slot_hovered.emit(index, slot)

func _on_slot_unhovered(slot: DragDropSlot, index: int) -> void:
	slot_unhovered.emit(index, slot)

func _on_item_dropped(slot: DragDropSlot, _data: Variant, source_slot: DragDropSlot, index: int) -> void:
	item_dropped.emit(index, slot, source_slot)

func _on_resized() -> void:
	if columns == 0:
		_update_columns()

func _update_columns() -> void:
	if not _grid:
		return

	if columns > 0:
		_grid.columns = columns
	else:
		# Auto-fit based on container width
		var available_width := size.x - spacing  # Account for scrollbar
		var slot_total_width := slot_size.x + spacing
		var fit_columns := maxi(1, int(available_width / slot_total_width))
		_grid.columns = fit_columns

func _update_slot_sizes() -> void:
	for slot in _slots:
		slot.slot_size = slot_size

# Public API

func get_slot(index: int) -> DragDropSlot:
	if index >= 0 and index < _slots.size():
		return _slots[index]
	return null

func get_slots() -> Array[DragDropSlot]:
	return _slots

func get_slot_count() -> int:
	return _slots.size()

## Get the internal GridContainer for use with SlotGridBinder
func get_grid() -> GridContainer:
	return _grid

# DEPRECATED: Use SlotGridBinder instead
# Set data for a specific slot
func set_slot_data(index: int, data: Variant) -> void:
	var slot := get_slot(index)
	if slot:
		slot.set_slot_data(data)

# DEPRECATED: Use SlotGridBinder instead
# Get data from a specific slot
func get_slot_data(index: int) -> Variant:
	var slot := get_slot(index)
	if slot:
		return slot.get_slot_data()
	return null

# DEPRECATED: Use SlotGridBinder.bind_items() instead
# Set data for all slots from an array
func set_items(items: Array) -> void:
	for i in range(mini(items.size(), _slots.size())):
		_slots[i].set_slot_data(items[i])

	# Clear remaining slots
	for i in range(items.size(), _slots.size()):
		_slots[i].clear_slot()

# DEPRECATED: Use SlotGridBinder.get_items() instead
# Get all slot data as an array
func get_items() -> Array:
	var items: Array = []
	for slot in _slots:
		items.append(slot.get_slot_data())
	return items

# DEPRECATED: Use SlotGridBinder.get_items() instead
# Get only non-empty slot data
func get_non_empty_items() -> Array:
	var items: Array = []
	for slot in _slots:
		var data: Variant = slot.get_slot_data()
		if data != null:
			items.append(data)
	return items

# DEPRECATED: Use SlotGridBinder.find_empty_slot() instead
# Find first empty slot index
func find_empty_slot() -> int:
	for i in range(_slots.size()):
		if _slots[i].is_empty():
			return i
	return -1

# DEPRECATED: Implement custom search in your code
# Find slot containing specific data (by reference or ID)
func find_slot_with_data(data: Variant) -> int:
	for i in range(_slots.size()):
		var slot_data: Variant = _slots[i].get_slot_data()
		if slot_data == data:
			return i
		# Also check by ID if available
		if slot_data is Object and data is Object:
			if slot_data.has_method("get_id") and data.has_method("get_id"):
				if slot_data.get_id() == data.get_id():
					return i
	return -1

# DEPRECATED: Use SlotGridBinder.add_item() instead
# Add item to first empty slot
func add_item(data: Variant) -> int:
	var empty_index := find_empty_slot()
	if empty_index >= 0:
		set_slot_data(empty_index, data)
	return empty_index

# DEPRECATED: Use SlotGridBinder.remove_item() instead
# Remove item from slot
func remove_item(index: int) -> Variant:
	var slot := get_slot(index)
	if slot:
		var data: Variant = slot.get_slot_data()
		slot.clear_slot()
		return data
	return null

# DEPRECATED: Use SlotGridBinder.clear() instead
# Clear all slots
func clear_all() -> void:
	for slot in _slots:
		slot.clear_slot()

# Highlight a specific slot
func highlight_slot(index: int, highlighted: bool) -> void:
	var slot := get_slot(index)
	if slot:
		slot.set_highlighted(highlighted)

# Clear all highlights
func clear_highlights() -> void:
	for slot in _slots:
		slot.set_highlighted(false)

# Enable/disable all slots
func set_all_enabled(enabled: bool) -> void:
	for slot in _slots:
		slot.set_disabled(not enabled)

# Refresh column count (call after parent resize)
func refresh_layout() -> void:
	_update_columns()

# Create a ScrollableGrid programmatically
static func create(
	count: int = 24,
	type: SlotType = SlotType.INVENTORY,
	size: Vector2 = Vector2(64, 64)
) -> ScrollableGrid:
	var grid := ScrollableGrid.new()
	grid.slot_count = count
	grid.slot_type = type
	grid.slot_size = size
	return grid
