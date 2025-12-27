class_name EquipmentSlot
extends DragDropSlot

## EquipmentSlot
## A specialized slot for character equipment
## Validates that only compatible equipment types can be dropped

signal equipment_changed(slot: EquipmentSlot, old_equipment: Variant, new_equipment: Variant)
signal unequip_requested(slot: EquipmentSlot)

# Equipment slot types matching the character system
enum SlotType {
	HEAD,
	TORSO,
	ARMS,
	LEGS,
	FEET,
	MAIN_HAND,
	OFF_HAND,
	MISC_1,
	MISC_2,
	MISC_3,
	MISC_4
}

@export var slot_type: SlotType = SlotType.MISC_1:
	set(value):
		slot_type = value
		_update_slot_label()

@export var show_slot_label: bool = true:
	set(value):
		show_slot_label = value
		_update_slot_label()

@export var show_item_name: bool = true

# Internal nodes
var _slot_label: Label
var _item_name_label: Label
var _empty_label: Label

func _ready() -> void:
	super._ready()
	_build_equipment_ui()
	_update_slot_label()

func _build_equipment_ui() -> void:
	# Slot type label (top)
	_slot_label = Label.new()
	_slot_label.name = "SlotLabel"
	_slot_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_slot_label.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	_slot_label.set_anchors_preset(Control.PRESET_TOP_WIDE)
	_slot_label.offset_top = 2
	_slot_label.offset_bottom = 16
	_slot_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_slot_label)

	# Slot type label shown centered when empty (no "Empty" text)
	_empty_label = Label.new()
	_empty_label.name = "EmptyLabel"
	_empty_label.text = ""  # Will be set to slot type name
	_empty_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_empty_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_empty_label.set_anchors_preset(Control.PRESET_FULL_RECT)
	_empty_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_empty_label)

	# Item name label (bottom)
	_item_name_label = Label.new()
	_item_name_label.name = "ItemNameLabel"
	_item_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_item_name_label.vertical_alignment = VERTICAL_ALIGNMENT_BOTTOM
	_item_name_label.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	_item_name_label.offset_top = -16
	_item_name_label.offset_bottom = -2
	_item_name_label.clip_text = true
	_item_name_label.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	_item_name_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_item_name_label.visible = false
	add_child(_item_name_label)

	_apply_label_styles()

func _apply_label_styles() -> void:
	var theme := UIThemeManager.get_theme()

	# Slot label style (small, secondary color)
	_slot_label.add_theme_font_size_override("font_size", theme.font_size_small)
	_slot_label.add_theme_color_override("font_color", theme.text_color_secondary)

	# Empty label style
	_empty_label.add_theme_font_size_override("font_size", theme.font_size_small)
	_empty_label.add_theme_color_override("font_color", theme.text_color_disabled)

	# Item name style
	_item_name_label.add_theme_font_size_override("font_size", theme.font_size_small)
	_item_name_label.add_theme_color_override("font_color", theme.text_color)

func _update_slot_label() -> void:
	if not _slot_label:
		return

	# Hide the top slot label - we show label centered when empty instead
	_slot_label.visible = false
	_slot_label.text = _get_slot_type_name()

	# Update empty label with slot type name (shown when empty)
	if _empty_label:
		_empty_label.text = _get_slot_type_name()

func _get_slot_type_name() -> String:
	match slot_type:
		SlotType.HEAD:
			return "Head"
		SlotType.TORSO:
			return "Torso"
		SlotType.ARMS:
			return "Arms"
		SlotType.LEGS:
			return "Legs"
		SlotType.FEET:
			return "Feet"
		SlotType.MAIN_HAND:
			return "Main"
		SlotType.OFF_HAND:
			return "Off"
		SlotType.MISC_1, SlotType.MISC_2, SlotType.MISC_3, SlotType.MISC_4:
			return "Misc"
		_:
			return ""

# Convert slot type to string for equipment validation
func get_slot_type_string() -> String:
	match slot_type:
		SlotType.HEAD:
			return "head"
		SlotType.TORSO:
			return "torso"
		SlotType.ARMS:
			return "arms"
		SlotType.LEGS:
			return "legs"
		SlotType.FEET:
			return "feet"
		SlotType.MAIN_HAND:
			return "main_hand"
		SlotType.OFF_HAND:
			return "off_hand"
		SlotType.MISC_1:
			return "misc1"
		SlotType.MISC_2:
			return "misc2"
		SlotType.MISC_3:
			return "misc3"
		SlotType.MISC_4:
			return "misc4"
		_:
			return "misc1"

# Override can_accept_drop to validate equipment type
func can_accept_drop(data: Variant) -> bool:
	if not super.can_accept_drop(data):
		return false

	# Check if data is valid equipment
	if data == null:
		return false

	# Handle EquipmentInstance specifically
	if data is EquipmentInstance:
		return _is_compatible_equipment(data)

	# If data has a slot_type property, validate it
	if data is Object:
		if data.has_method("get_slot_type"):
			var item_slot := data.get_slot_type() as String
			return _is_compatible_slot(item_slot)
		elif "slot_type" in data:
			return _is_compatible_slot(data.slot_type)
		elif "equipment_type" in data:
			return _is_compatible_slot(data.equipment_type)

	# Misc slots accept anything
	if slot_type in [SlotType.MISC_1, SlotType.MISC_2, SlotType.MISC_3, SlotType.MISC_4]:
		return true

	# Default: accept if no type info available
	return true

# Check if EquipmentInstance is compatible with this slot
func _is_compatible_equipment(equipment: EquipmentInstance) -> bool:
	var item_type := equipment.item_type
	var prefix := item_type.substr(0, 2) if item_type.length() >= 2 else item_type

	# Misc slots accept any equipment
	if slot_type in [SlotType.MISC_1, SlotType.MISC_2, SlotType.MISC_3, SlotType.MISC_4]:
		return true

	# Match prefix to slot type
	match slot_type:
		SlotType.HEAD:
			return prefix == "hd"
		SlotType.TORSO:
			return prefix == "tr"
		SlotType.ARMS:
			return prefix == "ar"
		SlotType.LEGS:
			return prefix == "lg"
		SlotType.FEET:
			return prefix == "fe"
		SlotType.MAIN_HAND:
			return prefix in ["w0", "w1"]
		SlotType.OFF_HAND:
			return prefix in ["w0", "w1"]
		_:
			return true

func _is_compatible_slot(item_slot_type: String) -> bool:
	var my_slot := get_slot_type_string()

	# Exact match
	if item_slot_type == my_slot:
		return true

	# Misc slots accept anything (misc1, misc2, misc3, misc4)
	if my_slot.begins_with("misc"):
		return true

	# Some flexibility for weapon slots
	if my_slot == "main_hand" and item_slot_type in ["weapon", "main_hand", "one_hand", "two_hand"]:
		return true
	if my_slot == "off_hand" and item_slot_type in ["shield", "off_hand", "one_hand"]:
		return true

	return false

# Override handle_drop to emit equipment_changed and handle inventory integration
# Rules for equipment swap:
# 1. Dragged item goes into target slot (already validated by can_accept_drop)
# 2. If target had an item AND it's compatible with source slot -> swap
# 3. If target had an item AND it's NOT compatible with source slot -> goes to inventory
# 4. If target was empty -> source slot is cleared
func handle_drop(data: Variant, source_slot: DragDropSlot) -> bool:
	if not can_accept_drop(data):
		drop_rejected.emit(self, data, "Cannot accept this item")
		return false

	# Handle EquipmentInstance specifically
	if data is EquipmentInstance:
		var old_equipment: Variant = _slot_data

		# Remove dragged item from inventory if source was inventory slot
		if source_slot is InventorySlot:
			PlayerData.remove_from_inventory(data)

		# Handle what happens to the displaced equipment (if any)
		if old_equipment and old_equipment is EquipmentInstance:
			if source_slot is InventorySlot:
				# Source was inventory - displaced item goes to inventory
				PlayerData.add_to_inventory(old_equipment)
			elif source_slot is EquipmentSlot:
				var source_equip_slot := source_slot as EquipmentSlot
				# Check if displaced item is compatible with source slot
				if source_equip_slot._is_compatible_equipment(old_equipment):
					# Compatible - swap: put displaced item in source slot
					source_equip_slot.set_equipment(old_equipment)
				else:
					# Not compatible - displaced item goes to inventory, clear source
					PlayerData.add_to_inventory(old_equipment)
					source_equip_slot.clear_equipment()
		else:
			# Target was empty - just clear the source slot
			if source_slot is EquipmentSlot:
				(source_slot as EquipmentSlot).clear_equipment()
			elif source_slot is InventorySlot:
				# Already removed from inventory above, nothing more to do
				pass

		# Set new equipment on this slot (emits equipment_changed -> Character update)
		set_equipment(data)

		item_dropped.emit(self, data, source_slot)
		return true

	# Fall back to base behavior for non-equipment
	var old_equipment: Variant = _slot_data
	if super.handle_drop(data, source_slot):
		equipment_changed.emit(self, old_equipment, data)
		return true

	return false

# Override _update_display for equipment-specific display
func _update_display() -> void:
	super._update_display()

	if _slot_data:
		# Item equipped: hide slot type label, optionally show item name
		_empty_label.visible = false
		_item_name_label.visible = show_item_name

		# Get item name
		var item_name := ""
		if _slot_data is Object:
			if _slot_data.has_method("get_display_name"):
				item_name = _slot_data.get_display_name()
			elif "name" in _slot_data:
				item_name = _slot_data.name
			elif "display_name" in _slot_data:
				item_name = _slot_data.display_name
		_item_name_label.text = item_name
	else:
		# Empty slot: show slot type label centered, hide item name
		_empty_label.visible = show_slot_label
		_item_name_label.visible = false
		_item_name_label.text = ""

# Handle right-click or long-press for unequip
func _gui_input(event: InputEvent) -> void:
	super._gui_input(event)

	# Right-click to unequip
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_RIGHT and mb.pressed:
			if _slot_data:
				unequip_requested.emit(self)

# Set equipment data
func set_equipment(equipment: Variant) -> void:
	var old: Variant = _slot_data
	set_slot_data(equipment)
	if old != equipment:
		equipment_changed.emit(self, old, equipment)

func get_equipment() -> Variant:
	return get_slot_data()

func clear_equipment() -> void:
	var old: Variant = _slot_data
	clear_slot()
	if old:
		equipment_changed.emit(self, old, null)

# Theme change handler
func _on_theme_changed(_new_theme: Resource) -> void:
	super._on_theme_changed(_new_theme)
	_apply_label_styles()

# Create an EquipmentSlot programmatically
static func create_for_slot(type: SlotType, size: Vector2 = Vector2(64, 64)) -> EquipmentSlot:
	var slot := EquipmentSlot.new()
	slot.slot_type = type
	slot.slot_size = size
	return slot
