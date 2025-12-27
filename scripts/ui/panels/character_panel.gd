class_name CharacterPanel
extends PanelContainer

## CharacterPanel
## Left side of party menu showing character portrait and equipment slots
## Layout:
## - Top: Centered Portrait (left) + Equipment scroll column (right)
##   - Equipment column: Head, Torso, Arms, Legs, Feet, Misc1-4 in recessed scroll
## - Middle: Weapon row (Main Hand, Off Hand) centered under portrait
## - Bottom: Navigation arrows (left) + character name (right)

signal character_changed(index: int)
signal equipment_slot_clicked(slot: EquipmentSlot)
signal equipment_changed(slot: EquipmentSlot, old_item: Variant, new_item: Variant)
signal unequip_requested(slot: EquipmentSlot)

# Current state
var _current_character_index: int = 0
var _party_size: int = 4
var _character_data: Variant = null

# Selection tracking
var _selected_slot: EquipmentSlot = null

# UI References
var _main_vbox: VBoxContainer
var _portrait_equipment_hbox: HBoxContainer
var _portrait_container: PanelContainer
var _portrait_display: CharacterDisplay
var _equipment_scroll: ScrollContainer
var _equipment_slots_vbox: VBoxContainer
var _weapon_row: HBoxContainer
var _nav_container: HBoxContainer
var _left_arrow: IconButton
var _right_arrow: IconButton
var _name_label: Label
var _equipment_slots: Dictionary = {}  # SlotType -> EquipmentSlot

func _ready() -> void:
	_build_ui()
	_apply_style()
	_update_display()

	# Listen for theme changes
	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

func _build_ui() -> void:
	# Apply panel styling
	var stylebox := UIThemeManager.create_bg_stylebox("secondary")
	add_theme_stylebox_override("panel", stylebox)

	var theme := UIThemeManager.get_theme()

	# Main vertical layout
	_main_vbox = VBoxContainer.new()
	_main_vbox.name = "MainVBox"
	_main_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_main_vbox.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_main_vbox.add_theme_constant_override("separation", theme.spacing_small)
	add_child(_main_vbox)

	# Top section: Portrait + Equipment scroll column
	_build_portrait_equipment_section()

	# Middle section: Weapon slots row (Main Hand, Off Hand)
	_build_weapon_row()

	# Bottom section: Navigation + Name
	_build_navigation_section()

func _build_portrait_equipment_section() -> void:
	var theme := UIThemeManager.get_theme()

	_portrait_equipment_hbox = HBoxContainer.new()
	_portrait_equipment_hbox.name = "PortraitEquipmentHBox"
	_portrait_equipment_hbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_portrait_equipment_hbox.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_portrait_equipment_hbox.add_theme_constant_override("separation", theme.spacing_small)
	_main_vbox.add_child(_portrait_equipment_hbox)

	# Portrait area (left side, expands) with centered display
	_portrait_container = PanelContainer.new()
	_portrait_container.name = "PortraitContainer"
	_portrait_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_portrait_container.size_flags_vertical = Control.SIZE_EXPAND_FILL

	# Dark background for portrait with padding
	var portrait_style := StyleBoxFlat.new()
	portrait_style.bg_color = theme.bg_secondary.darkened(0.2)
	portrait_style.set_corner_radius_all(6)
	#portrait_style.content_margin_left = 180
	portrait_style.content_margin_right = 180
	#portrait_style.content_margin_top = 18
	portrait_style.content_margin_bottom = 160
	_portrait_container.add_theme_stylebox_override("panel", portrait_style)
	_portrait_equipment_hbox.add_child(_portrait_container)

	# CenterContainer to center the CharacterDisplay
	var center_container := CenterContainer.new()
	center_container.name = "CenterContainer"
	center_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	center_container.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_portrait_container.add_child(center_container)

	# CharacterDisplay for rendering the character (centered)
	_portrait_display = CharacterDisplay.new()
	_portrait_display.name = "CharacterDisplay"
	center_container.add_child(_portrait_display)

	# Equipment column (right side) - recessed scroll container
	var equipment_inset := PanelContainer.new()
	equipment_inset.name = "EquipmentInset"
	equipment_inset.size_flags_vertical = Control.SIZE_EXPAND_FILL

	# Create inset/recessed style for equipment column
	var inset_style := StyleBoxFlat.new()
	inset_style.bg_color = theme.bg_secondary.darkened(0.15)
	inset_style.set_border_width_all(2)
	inset_style.border_color = theme.bg_secondary.darkened(0.3)
	inset_style.set_corner_radius_all(4)
	inset_style.shadow_color = Color(0, 0, 0, 0.2)
	inset_style.shadow_size = 2
	inset_style.shadow_offset = Vector2(1, 1)
	inset_style.content_margin_left = 4
	inset_style.content_margin_right = 4
	inset_style.content_margin_top = 4
	inset_style.content_margin_bottom = 4
	equipment_inset.add_theme_stylebox_override("panel", inset_style)
	_portrait_equipment_hbox.add_child(equipment_inset)

	# Scroll container (hidden scrollbar)
	_equipment_scroll = ScrollContainer.new()
	_equipment_scroll.name = "EquipmentScroll"
	_equipment_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_equipment_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_equipment_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_SHOW_NEVER
	equipment_inset.add_child(_equipment_scroll)

	# VBox for all equipment slots (body + misc)
	_equipment_slots_vbox = VBoxContainer.new()
	_equipment_slots_vbox.name = "EquipmentSlotsVBox"
	_equipment_slots_vbox.add_theme_constant_override("separation", theme.spacing_tiny)
	_equipment_scroll.add_child(_equipment_slots_vbox)

	# Create all equipment slots in vertical column (body + misc)
	var all_slot_types: Array[EquipmentSlot.SlotType] = [
		EquipmentSlot.SlotType.HEAD,
		EquipmentSlot.SlotType.TORSO,
		EquipmentSlot.SlotType.ARMS,
		EquipmentSlot.SlotType.LEGS,
		EquipmentSlot.SlotType.FEET,
		EquipmentSlot.SlotType.MISC_1,
		EquipmentSlot.SlotType.MISC_2,
		EquipmentSlot.SlotType.MISC_3,
		EquipmentSlot.SlotType.MISC_4,
	]

	for slot_type in all_slot_types:
		var slot := _create_equipment_slot(slot_type)
		_equipment_slots_vbox.add_child(slot)
		_equipment_slots[slot_type] = slot

func _build_weapon_row() -> void:
	var theme := UIThemeManager.get_theme()

	_weapon_row = HBoxContainer.new()
	_weapon_row.name = "WeaponRow"
	_weapon_row.alignment = BoxContainer.ALIGNMENT_CENTER
	_weapon_row.add_theme_constant_override("separation", theme.spacing_small)
	_main_vbox.add_child(_weapon_row)

	# Main Hand and Off Hand slots
	var weapon_slot_types: Array[EquipmentSlot.SlotType] = [
		EquipmentSlot.SlotType.MAIN_HAND,
		EquipmentSlot.SlotType.OFF_HAND,
	]

	for slot_type in weapon_slot_types:
		var slot := _create_equipment_slot(slot_type)
		_weapon_row.add_child(slot)
		_equipment_slots[slot_type] = slot

func _build_navigation_section() -> void:
	var theme := UIThemeManager.get_theme()

	_nav_container = HBoxContainer.new()
	_nav_container.name = "NavContainer"
	_nav_container.add_theme_constant_override("separation", theme.spacing_tiny)
	_main_vbox.add_child(_nav_container)

	# Left arrow
	_left_arrow = IconButton.new()
	_left_arrow.name = "LeftArrow"
	_left_arrow.preset_icon = IconButton.PresetIcon.ARROW_LEFT
	_left_arrow.icon_position = IconButton.IconPosition.ONLY
	_left_arrow.button_style = ThemedButton.ButtonStyle.GHOST
	_left_arrow.custom_minimum_size = Vector2(40, 40)
	_left_arrow.pressed.connect(_on_previous_character)
	_nav_container.add_child(_left_arrow)

	# Right arrow (next to left arrow for mobile)
	_right_arrow = IconButton.new()
	_right_arrow.name = "RightArrow"
	_right_arrow.preset_icon = IconButton.PresetIcon.ARROW_RIGHT
	_right_arrow.icon_position = IconButton.IconPosition.ONLY
	_right_arrow.button_style = ThemedButton.ButtonStyle.GHOST
	_right_arrow.custom_minimum_size = Vector2(40, 40)
	_right_arrow.pressed.connect(_on_next_character)
	_nav_container.add_child(_right_arrow)

	# Character name (right of arrows, fills remaining space)
	_name_label = Label.new()
	_name_label.name = "NameLabel"
	_name_label.text = "Character Name"
	_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	_name_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_nav_container.add_child(_name_label)

func _create_equipment_slot(slot_type: EquipmentSlot.SlotType) -> EquipmentSlot:
	var slot := EquipmentSlot.new()
	slot.slot_type = slot_type
	slot.slot_size = Vector2(UIConstants.SLOT_SIZE_ICON, UIConstants.SLOT_SIZE_ICON)
	slot.show_slot_label = true
	slot.show_item_name = false  # Save space

	# Connect signals
	slot.slot_clicked.connect(_on_equipment_slot_clicked.bind(slot))
	slot.equipment_changed.connect(_on_equipment_changed.bind(slot))
	slot.unequip_requested.connect(_on_unequip_requested.bind(slot))

	return slot

func _apply_style() -> void:
	var theme := UIThemeManager.get_theme()

	# Name label styling
	_name_label.add_theme_font_size_override("font_size", theme.font_size_subheader)
	_name_label.add_theme_color_override("font_color", theme.text_color)

	# Panel styling
	var stylebox := UIThemeManager.create_bg_stylebox("secondary")
	add_theme_stylebox_override("panel", stylebox)

func _update_display() -> void:
	# Update arrow states
	_left_arrow.disabled = (_current_character_index <= 0)
	_right_arrow.disabled = (_current_character_index >= _party_size - 1)

	# Update character info
	if _character_data:
		_update_character_display()
	else:
		_name_label.text = "Character %d" % (_current_character_index + 1)
		_portrait_display.set_character(null)

func _update_character_display() -> void:
	# Get character name
	if _character_data is Character:
		_name_label.text = _character_data.display_name if _character_data.display_name else "Character"
		# Set character for portrait display (this auto-connects to model_changed)
		_portrait_display.set_character(_character_data)
	elif _character_data is Object:
		if _character_data.has_method("get_display_name"):
			_name_label.text = _character_data.get_display_name()
		elif "display_name" in _character_data:
			_name_label.text = _character_data.display_name
		elif "name" in _character_data:
			_name_label.text = _character_data.name

	# Update equipment slots
	_update_equipment_display()

func _update_equipment_display() -> void:
	if not _character_data:
		_clear_equipment_slots()
		return

	# Get equipment data from character
	var equipment: Dictionary = {}
	if _character_data is Object:
		if _character_data.has_method("get_all_equipment_with_slots"):
			# Convert Array[Dictionary] to Dictionary keyed by slot name
			var eq_array: Array = _character_data.get_all_equipment_with_slots()
			for eq_data in eq_array:
				if eq_data is Dictionary and eq_data.has("slot") and eq_data.has("equipment"):
					equipment[eq_data["slot"]] = eq_data["equipment"]
		elif "equipment" in _character_data:
			equipment = _character_data.equipment

	# Update each slot
	for slot_type in _equipment_slots:
		var slot: EquipmentSlot = _equipment_slots[slot_type]
		var slot_key := slot.get_slot_type_string()

		if equipment.has(slot_key):
			slot.set_equipment(equipment[slot_key])
		else:
			slot.clear_equipment()

func _clear_equipment_slots() -> void:
	for slot_type in _equipment_slots:
		_equipment_slots[slot_type].clear_equipment()

# Navigation
func _on_previous_character() -> void:
	if _current_character_index > 0:
		_current_character_index -= 1
		_update_display()
		character_changed.emit(_current_character_index)

func _on_next_character() -> void:
	if _current_character_index < _party_size - 1:
		_current_character_index += 1
		_update_display()
		character_changed.emit(_current_character_index)

# Equipment slot events
func _on_equipment_slot_clicked(slot: EquipmentSlot) -> void:
	# Deselect previous slot
	if _selected_slot and is_instance_valid(_selected_slot):
		_selected_slot.set_highlighted(false)

	# Select new slot (or deselect if clicking same slot)
	if _selected_slot == slot:
		_selected_slot = null
	else:
		_selected_slot = slot
		slot.set_highlighted(true)

	equipment_slot_clicked.emit(slot)

func _on_equipment_changed(_slot: EquipmentSlot, old_item: Variant, new_item: Variant, source_slot: EquipmentSlot) -> void:
	# Update actual character equipment model
	if _character_data is Character:
		var equip_slot := _slot_type_to_equip_slot(source_slot.slot_type)
		if new_item and new_item is EquipmentInstance:
			_character_data.set_equipment(equip_slot, new_item)
		else:
			_character_data.unequip(equip_slot)

	equipment_changed.emit(source_slot, old_item, new_item)

# Convert UI SlotType to Character.EquipSlot enum
func _slot_type_to_equip_slot(slot_type: EquipmentSlot.SlotType) -> int:
	match slot_type:
		EquipmentSlot.SlotType.HEAD:
			return Character.EquipSlot.HEAD
		EquipmentSlot.SlotType.TORSO:
			return Character.EquipSlot.TORSO
		EquipmentSlot.SlotType.ARMS:
			return Character.EquipSlot.ARMS
		EquipmentSlot.SlotType.LEGS:
			return Character.EquipSlot.LEGS
		EquipmentSlot.SlotType.FEET:
			return Character.EquipSlot.FEET
		EquipmentSlot.SlotType.MAIN_HAND:
			return Character.EquipSlot.MAIN_HAND
		EquipmentSlot.SlotType.OFF_HAND:
			return Character.EquipSlot.OFF_HAND
		EquipmentSlot.SlotType.MISC_1:
			return Character.EquipSlot.MISC1
		EquipmentSlot.SlotType.MISC_2:
			return Character.EquipSlot.MISC2
		EquipmentSlot.SlotType.MISC_3:
			return Character.EquipSlot.MISC3
		EquipmentSlot.SlotType.MISC_4:
			return Character.EquipSlot.MISC4
		_:
			return Character.EquipSlot.MISC1

func _on_unequip_requested(slot: EquipmentSlot) -> void:
	var equipment: Variant = slot.get_equipment()
	if equipment:
		# Unequip from character model
		if _character_data is Character:
			var equip_slot := _slot_type_to_equip_slot(slot.slot_type)
			_character_data.unequip(equip_slot)

		# Add to shared inventory
		if equipment is EquipmentInstance:
			PlayerData.add_to_inventory(equipment)

		# Clear slot display
		slot.clear_equipment()

	unequip_requested.emit(slot)

func _on_theme_changed(_new_theme: Resource) -> void:
	_apply_style()

# Public API

func set_character(character: Variant) -> void:
	_character_data = character
	_update_display()

func get_character() -> Variant:
	return _character_data

func set_character_index(index: int) -> void:
	_current_character_index = clampi(index, 0, _party_size - 1)
	_update_display()

func get_character_index() -> int:
	return _current_character_index

func set_party_size(size: int) -> void:
	_party_size = maxi(1, size)
	_update_display()

func get_equipment_slot(slot_type: EquipmentSlot.SlotType) -> EquipmentSlot:
	return _equipment_slots.get(slot_type)

func get_all_equipment_slots() -> Array[EquipmentSlot]:
	var slots: Array[EquipmentSlot] = []
	for slot_type in _equipment_slots:
		slots.append(_equipment_slots[slot_type])
	return slots

# Refresh from character data
func refresh() -> void:
	_update_display()

# Clear equipment slot selection
func clear_selection() -> void:
	if _selected_slot and is_instance_valid(_selected_slot):
		_selected_slot.set_highlighted(false)
	_selected_slot = null

# Get the currently selected equipment slot (or null)
func get_selected_slot() -> EquipmentSlot:
	return _selected_slot
