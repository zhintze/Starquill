class_name InventorySlot
extends DragDropSlot

## InventorySlot
## A slot for inventory items with stack quantity display
## Supports stackable items and quantity management

signal quantity_changed(slot: InventorySlot, old_quantity: int, new_quantity: int)
signal item_used(slot: InventorySlot)

@export var show_quantity: bool = true:
	set(value):
		show_quantity = value
		_update_quantity_display()

@export var max_stack_size: int = 99

# Internal nodes
var _quantity_label: Label
var _quantity: int = 0

func _ready() -> void:
	super._ready()
	_build_inventory_ui()

func _build_inventory_ui() -> void:
	# Quantity label (bottom-right corner)
	_quantity_label = Label.new()
	_quantity_label.name = "QuantityLabel"
	_quantity_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_quantity_label.vertical_alignment = VERTICAL_ALIGNMENT_BOTTOM
	_quantity_label.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	_quantity_label.offset_left = -24
	_quantity_label.offset_top = -16
	_quantity_label.offset_right = -4
	_quantity_label.offset_bottom = -2
	_quantity_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_quantity_label.visible = false
	add_child(_quantity_label)

	_apply_quantity_style()

func _apply_quantity_style() -> void:
	var theme := UIThemeManager.get_theme()

	_quantity_label.add_theme_font_size_override("font_size", theme.font_size_small)
	_quantity_label.add_theme_color_override("font_color", theme.text_color)

	# Add a slight shadow/outline effect for readability
	_quantity_label.add_theme_constant_override("shadow_offset_x", 1)
	_quantity_label.add_theme_constant_override("shadow_offset_y", 1)
	_quantity_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.5))

func _update_quantity_display() -> void:
	if not _quantity_label:
		return

	# Only show quantity if > 1 and slot has data
	var should_show := show_quantity and _quantity > 1 and _slot_data != null
	_quantity_label.visible = should_show

	if should_show:
		_quantity_label.text = str(_quantity)

# Override _update_display for inventory-specific display
func _update_display() -> void:
	super._update_display()

	# Extract quantity from data if available
	if _slot_data:
		# EquipmentInstance doesn't stack - always quantity 1
		if _slot_data is EquipmentInstance:
			_quantity = 1
		elif _slot_data is Object:
			if _slot_data.has_method("get_quantity"):
				_quantity = _slot_data.get_quantity()
			elif "quantity" in _slot_data:
				_quantity = _slot_data.quantity
			elif "count" in _slot_data:
				_quantity = _slot_data.count
			elif "stack_size" in _slot_data:
				_quantity = _slot_data.stack_size
			else:
				_quantity = 1
		else:
			_quantity = 1
	else:
		_quantity = 0

	_update_quantity_display()

# Override can_accept_drop for stacking logic
func can_accept_drop(data: Variant) -> bool:
	if not super.can_accept_drop(data):
		return false

	# If slot is empty, accept anything
	if _slot_data == null:
		return true

	# If slot has data, check if items can stack
	if _can_stack_with(data):
		return _quantity < max_stack_size

	# Different items - allow swap
	return true

func _can_stack_with(other_data: Variant) -> bool:
	if _slot_data == null or other_data == null:
		return false

	# Check if items are the same type and stackable
	if _slot_data is Object and other_data is Object:
		# Check by ID
		if _slot_data.has_method("get_id") and other_data.has_method("get_id"):
			if _slot_data.get_id() != other_data.get_id():
				return false
		elif "id" in _slot_data and "id" in other_data:
			if _slot_data.id != other_data.id:
				return false
		elif "item_id" in _slot_data and "item_id" in other_data:
			if _slot_data.item_id != other_data.item_id:
				return false
		else:
			return false  # Can't determine if same item

		# Check if stackable
		if _slot_data.has_method("is_stackable"):
			return _slot_data.is_stackable()
		elif "stackable" in _slot_data:
			return _slot_data.stackable
		elif "is_stackable" in _slot_data:
			return _slot_data.is_stackable

	return false

# Override handle_drop for stacking behavior
# Rules for equipment-to-inventory:
# 1. Dragged equipment goes to inventory
# 2. If inventory slot had compatible equipment -> swap to equipment slot
# 3. If inventory slot had incompatible equipment -> both stay in inventory
# 4. If inventory slot was empty -> equipment slot is cleared
func handle_drop(data: Variant, source_slot: DragDropSlot) -> bool:
	if not can_accept_drop(data):
		drop_rejected.emit(self, data, "Cannot accept this item")
		return false

	# Handle EquipmentInstance from EquipmentSlot (equipment -> inventory)
	if data is EquipmentInstance and source_slot is EquipmentSlot:
		var old_data: Variant = _slot_data
		var equip_slot := source_slot as EquipmentSlot

		# If we had equipment in this inventory slot, check if it can swap
		if old_data and old_data is EquipmentInstance:
			# Check if old inventory item is compatible with the equipment slot
			if equip_slot._is_compatible_equipment(old_data):
				# Compatible - swap: inventory item goes to equipment slot
				PlayerData.remove_from_inventory(old_data)
				equip_slot.set_equipment(old_data)
			else:
				# Not compatible - both items stay in inventory, clear equipment slot
				equip_slot.clear_equipment()
				# old_data stays in inventory, we'll add dragged item too
		else:
			# Inventory slot was empty - just clear the equipment slot
			equip_slot.clear_equipment()

		# Add the dragged equipment to PlayerData inventory
		# inventory_changed signal will trigger InventoryPanel.refresh() to update UI
		PlayerData.add_to_inventory(data)

		item_dropped.emit(self, data, source_slot)
		return true

	# If empty slot, just set the data
	if _slot_data == null:
		return super.handle_drop(data, source_slot)

	# If same stackable item, merge stacks
	if _can_stack_with(data):
		var incoming_quantity := 1
		if data is Object:
			if data.has_method("get_quantity"):
				incoming_quantity = data.get_quantity()
			elif "quantity" in data:
				incoming_quantity = data.quantity

		var space_available := max_stack_size - _quantity
		var amount_to_add := mini(incoming_quantity, space_available)

		if amount_to_add > 0:
			var old_quantity := _quantity
			_quantity += amount_to_add
			_update_quantity_display()
			quantity_changed.emit(self, old_quantity, _quantity)

			# Update source slot
			if source_slot and source_slot is InventorySlot:
				var source_inv := source_slot as InventorySlot
				var remaining := incoming_quantity - amount_to_add
				if remaining > 0:
					source_inv.set_quantity(remaining)
				else:
					source_inv.clear_slot()

			item_dropped.emit(self, data, source_slot)
			return true

		return false

	# Different items - swap
	return super.handle_drop(data, source_slot)

# Handle double-click for item use
func _gui_input(event: InputEvent) -> void:
	super._gui_input(event)

	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_LEFT and mb.double_click:
			if _slot_data:
				item_used.emit(self)

# Quantity management
func get_quantity() -> int:
	return _quantity

func set_quantity(new_quantity: int) -> void:
	var old := _quantity
	_quantity = clampi(new_quantity, 0, max_stack_size)

	if _quantity <= 0:
		clear_slot()
	else:
		_update_quantity_display()

	if old != _quantity:
		quantity_changed.emit(self, old, _quantity)

func add_quantity(amount: int) -> int:
	var space := max_stack_size - _quantity
	var added := mini(amount, space)
	if added > 0:
		set_quantity(_quantity + added)
	return added

func remove_quantity(amount: int) -> int:
	var removed := mini(amount, _quantity)
	if removed > 0:
		set_quantity(_quantity - removed)
	return removed

# Check if slot can accept more of the same item
func can_add_more() -> bool:
	return _slot_data != null and _quantity < max_stack_size

func get_space_available() -> int:
	if _slot_data == null:
		return max_stack_size
	return max_stack_size - _quantity

# Override clear_slot to reset quantity
func clear_slot() -> void:
	_quantity = 0
	super.clear_slot()
	_update_quantity_display()

# Theme change handler
func _on_theme_changed(_new_theme: Resource) -> void:
	super._on_theme_changed(_new_theme)
	_apply_quantity_style()

# Create an InventorySlot programmatically
static func create_inventory_slot(index: int = -1, size: Vector2 = Vector2(64, 64)) -> InventorySlot:
	var slot := InventorySlot.new()
	slot.slot_index = index
	slot.slot_size = size
	return slot
