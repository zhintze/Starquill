class_name InventoryPanelNew
extends PanelContainer

## InventoryPanel (Refactored)
## Dynamic inventory grid in inset scroll container
## Displays inventory items with always-visible scrollbar
## Shows 2 empty rows below last item

signal item_selected(index: int, data: Variant)
signal item_used(index: int, data: Variant)
signal item_dropped(target_index: int, source_index: int)
signal sort_requested(sort_type: String)

# Configuration
@export var show_info_panel: bool = true
@export var show_sort_buttons: bool = true
@export var extra_empty_rows: int = 2  # Empty rows below last item

# References
var _inventory: Variant = null  # Inventory object
var _inventory_controller: Variant = null

# UI References
var _main_vbox: VBoxContainer
var _header: HBoxContainer
var _title_label: Label
var _sort_buttons: HBoxContainer
var _inset_container: PanelContainer
var _scroll_container: ScrollContainer
var _grid_container: GridContainer
var _slots: Array[DragDropSlot] = []
var _info_panel: PanelContainer
var _info_name_label: Label
var _info_desc_label: Label
var _info_value_label: Label
var _drag_highlight: ColorRect  # Overlay for drag feedback

# Layout calculations
var _slot_size: float = UIConstants.SLOT_SIZE_ICON
var _slot_spacing: float = UIConstants.SPACING_XS
var _columns: int = 0
var _item_count: int = 0

# Selection tracking
var _selected_slot: DragDropSlot = null
var _selected_item: Variant = null  # For external items (from equipment slots)

func _ready() -> void:
	_build_ui()
	_apply_style()
	_connect_inventory()

	# Listen for theme changes
	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

	# Listen for resize to recalculate slots
	resized.connect(_on_resized)

	# Listen for drag end to hide highlight
	if DragDropManager:
		DragDropManager.drag_ended.connect(_on_drag_ended)

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

	# Header with title and sort buttons
	_build_header()

	# Inventory scroll container with inset style
	_build_inventory_scroll()

	# Info panel (fills remaining space)
	if show_info_panel:
		_build_info_panel()

func _build_header() -> void:
	var theme := UIThemeManager.get_theme()

	_header = HBoxContainer.new()
	_header.name = "Header"
	_header.add_theme_constant_override("separation", theme.spacing_medium)
	_main_vbox.add_child(_header)

	# Title
	_title_label = Label.new()
	_title_label.name = "TitleLabel"
	_title_label.text = "Inventory"
	_title_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_header.add_child(_title_label)

	# Sort buttons
	if show_sort_buttons:
		_sort_buttons = HBoxContainer.new()
		_sort_buttons.name = "SortButtons"
		_sort_buttons.add_theme_constant_override("separation", theme.spacing_tiny)
		_header.add_child(_sort_buttons)

		var sort_btn := IconButton.new()
		sort_btn.preset_icon = IconButton.PresetIcon.SORT
		sort_btn.icon_position = IconButton.IconPosition.ONLY
		sort_btn.button_style = ThemedButton.ButtonStyle.GHOST
		sort_btn.custom_minimum_size = Vector2(32, 32)
		sort_btn.tooltip_text = "Sort by name"
		sort_btn.pressed.connect(_on_sort_name_pressed)
		_sort_buttons.add_child(sort_btn)

func _build_inventory_scroll() -> void:
	var theme := UIThemeManager.get_theme()

	# Inset/recessed container for the scroll area
	_inset_container = PanelContainer.new()
	_inset_container.name = "InsetContainer"
	_inset_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_inset_container.size_flags_vertical = Control.SIZE_EXPAND_FILL

	# Create inset/recessed style
	var inset_style := StyleBoxFlat.new()
	inset_style.bg_color = theme.bg_secondary.darkened(0.15)
	inset_style.set_border_width_all(2)
	inset_style.border_color = theme.bg_secondary.darkened(0.3)
	inset_style.set_corner_radius_all(4)
	# Inset effect - darker on top-left, lighter on bottom-right
	inset_style.shadow_color = Color(0, 0, 0, 0.2)
	inset_style.shadow_size = 2
	inset_style.shadow_offset = Vector2(1, 1)
	inset_style.content_margin_left = 4
	inset_style.content_margin_right = 4
	inset_style.content_margin_top = 4
	inset_style.content_margin_bottom = 4
	_inset_container.add_theme_stylebox_override("panel", inset_style)
	_main_vbox.add_child(_inset_container)

	# Drag highlight overlay (hidden by default)
	_drag_highlight = ColorRect.new()
	_drag_highlight.name = "DragHighlight"
	_drag_highlight.color = Color(theme.accent_color.r, theme.accent_color.g, theme.accent_color.b, 0.15)
	_drag_highlight.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_drag_highlight.visible = false
	_drag_highlight.set_anchors_preset(Control.PRESET_FULL_RECT)
	_inset_container.add_child(_drag_highlight)

	# Scroll container with always-visible scrollbar
	_scroll_container = ScrollContainer.new()
	_scroll_container.name = "ScrollContainer"
	_scroll_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll_container.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_scroll_container.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_scroll_container.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_SHOW_ALWAYS
	_inset_container.add_child(_scroll_container)

	# Grid container for inventory slots
	_grid_container = GridContainer.new()
	_grid_container.name = "InventoryGrid"
	_grid_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_grid_container.add_theme_constant_override("h_separation", int(_slot_spacing))
	_grid_container.add_theme_constant_override("v_separation", int(_slot_spacing))
	_scroll_container.add_child(_grid_container)

func _build_info_panel() -> void:
	var theme := UIThemeManager.get_theme()

	_info_panel = PanelContainer.new()
	_info_panel.name = "InfoPanel"
	# Fill remaining space instead of fixed height
	_info_panel.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_info_panel.custom_minimum_size.y = 60  # Minimum height
	_main_vbox.add_child(_info_panel)

	var info_style := UIThemeManager.create_bg_stylebox("primary")
	info_style.content_margin_left = theme.padding_container
	info_style.content_margin_right = theme.padding_container
	info_style.content_margin_top = theme.padding_container
	info_style.content_margin_bottom = theme.padding_container
	_info_panel.add_theme_stylebox_override("panel", info_style)

	var info_vbox := VBoxContainer.new()
	info_vbox.add_theme_constant_override("separation", theme.spacing_tiny)
	_info_panel.add_child(info_vbox)

	# Item name
	_info_name_label = Label.new()
	_info_name_label.name = "ItemName"
	_info_name_label.text = ""
	info_vbox.add_child(_info_name_label)

	# Item description
	_info_desc_label = Label.new()
	_info_desc_label.name = "ItemDesc"
	_info_desc_label.text = ""
	_info_desc_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_info_desc_label.size_flags_vertical = Control.SIZE_EXPAND_FILL
	info_vbox.add_child(_info_desc_label)

	# Item value
	_info_value_label = Label.new()
	_info_value_label.name = "ItemValue"
	_info_value_label.text = ""
	info_vbox.add_child(_info_value_label)

func _apply_style() -> void:
	var theme := UIThemeManager.get_theme()

	# Title styling
	_title_label.add_theme_font_size_override("font_size", theme.font_size_subheader)
	_title_label.add_theme_color_override("font_color", theme.text_color)

	# Info panel styling
	if _info_name_label:
		_info_name_label.add_theme_font_size_override("font_size", theme.font_size_body)
		_info_name_label.add_theme_color_override("font_color", theme.accent_color)

	if _info_desc_label:
		_info_desc_label.add_theme_font_size_override("font_size", theme.font_size_small)
		_info_desc_label.add_theme_color_override("font_color", theme.text_color_secondary)

	if _info_value_label:
		_info_value_label.add_theme_font_size_override("font_size", theme.font_size_small)
		_info_value_label.add_theme_color_override("font_color", theme.text_color)

func _on_resized() -> void:
	# Recalculate grid layout when container resizes
	call_deferred("_recalculate_grid")

func _recalculate_grid() -> void:
	if not _scroll_container or not _grid_container:
		return

	# Calculate how many columns fit
	var available_width := _scroll_container.size.x - 20  # Account for scrollbar
	var slot_total := _slot_size + _slot_spacing
	_columns = maxi(1, int(available_width / slot_total))
	_grid_container.columns = _columns

	# Calculate slot count: items + 2 extra rows
	_update_slot_count()

func _update_slot_count() -> void:
	# Calculate how many slots we need: item count + extra_empty_rows worth
	var items_count := _item_count
	var rows_for_items := ceili(float(items_count) / float(maxi(1, _columns)))
	var total_rows := rows_for_items + extra_empty_rows
	var target_slot_count := total_rows * _columns

	# Minimum slot count (at least show some empty slots)
	target_slot_count = maxi(target_slot_count, _columns * 3)

	# Add or remove slots as needed
	while _slots.size() < target_slot_count:
		_add_slot()

	# Hide excess slots (don't remove, just hide for performance)
	for i in range(_slots.size()):
		_slots[i].visible = (i < target_slot_count)

func _add_slot() -> void:
	var slot := InventorySlot.new()
	slot.slot_index = _slots.size()
	slot.slot_size = Vector2(_slot_size, _slot_size)
	slot.name = "Slot_%d" % _slots.size()

	# Connect signals
	slot.slot_clicked.connect(_on_slot_clicked.bind(_slots.size()))
	slot.slot_hovered.connect(_on_slot_hovered.bind(_slots.size()))
	slot.slot_unhovered.connect(_on_slot_unhovered.bind(_slots.size()))
	slot.item_dropped.connect(_on_item_dropped_internal.bind(_slots.size()))

	_grid_container.add_child(slot)
	_slots.append(slot)

func _connect_inventory() -> void:
	# Connect to PlayerData equipment inventory
	if has_node("/root/PlayerData"):
		var player_data = get_node("/root/PlayerData")
		if player_data.has_signal("inventory_changed"):
			player_data.inventory_changed.connect(_on_inventory_changed)
		refresh()

func _on_slot_clicked(slot: DragDropSlot, index: int) -> void:
	var data: Variant = slot.get_slot_data()

	# Deselect previous slot
	if _selected_slot and is_instance_valid(_selected_slot):
		_selected_slot.set_highlighted(false)

	# Select new slot (or deselect if clicking same slot)
	if _selected_slot == slot:
		_selected_slot = null
		_selected_item = null
		_clear_info_panel()
	else:
		_selected_slot = slot
		_selected_item = null  # Clear external item
		slot.set_highlighted(true)
		_update_info_panel(data)

	item_selected.emit(index, data)

	# Check if item is usable
	if data and data is Object:
		if data.has_method("is_usable") and data.is_usable():
			item_used.emit(index, data)
		elif "is_usable" in data and data.is_usable:
			item_used.emit(index, data)

func _on_slot_hovered(slot: DragDropSlot, _index: int) -> void:
	# Show hover info only if no slot is selected
	if _selected_slot == null and _selected_item == null:
		var data: Variant = slot.get_slot_data()
		_update_info_panel(data)

func _on_slot_unhovered(_slot: DragDropSlot, _index: int) -> void:
	# Only clear if no slot is selected
	if _selected_slot == null and _selected_item == null:
		_clear_info_panel()

func _on_item_dropped_internal(slot: DragDropSlot, _data: Variant, source_slot: DragDropSlot, index: int) -> void:
	var source_index := source_slot.slot_index if source_slot else -1
	item_dropped.emit(index, source_index)

func _on_sort_name_pressed() -> void:
	sort_by_name()
	sort_requested.emit("name")

func _update_info_panel(data: Variant) -> void:
	if not show_info_panel or not data:
		_clear_info_panel()
		return

	# Extract item info
	var item_name := ""
	var item_type := ""
	var item_stats := ""

	if data is EquipmentInstance:
		# Equipment-specific display
		item_name = data.get_display_name()
		item_type = _get_equipment_type_name(data.item_type)

		# Build stats string
		var stats: Dictionary = data.stat_mods
		if stats.size() > 0:
			var stat_parts: PackedStringArray = []
			for stat_key in stats:
				var value: int = stats[stat_key]
				var sign := "+" if value >= 0 else ""
				stat_parts.append("%s%d %s" % [sign, value, stat_key.capitalize()])
			item_stats = ", ".join(stat_parts)
		else:
			item_stats = "No stat bonuses"

	elif data is Object:
		if data.has_method("get_display_name"):
			item_name = data.get_display_name()
		elif "display_name" in data:
			item_name = data.display_name
		elif "name" in data:
			item_name = data.name

		if "item_type" in data:
			item_type = str(data.item_type)
		elif "type" in data:
			item_type = str(data.type)

		if "description" in data:
			item_stats = data.description
		elif "stats" in data and data.stats is Dictionary:
			var stat_parts: PackedStringArray = []
			for stat_key in data.stats:
				stat_parts.append("%s: %s" % [stat_key.capitalize(), str(data.stats[stat_key])])
			item_stats = ", ".join(stat_parts)

	_info_name_label.text = item_name
	_info_desc_label.text = item_type
	_info_value_label.text = item_stats

func _get_equipment_type_name(item_type: String) -> String:
	var prefix := item_type.substr(0, 2) if item_type.length() >= 2 else item_type
	match prefix:
		"hd":
			return "Head Armor"
		"tr":
			return "Torso Armor"
		"ar":
			return "Arm Armor"
		"lg":
			return "Leg Armor"
		"fe":
			return "Footwear"
		"w0":
			return "One-Handed Weapon"
		"w1":
			return "Two-Handed Weapon"
		_:
			return "Equipment"

func _clear_info_panel() -> void:
	if _info_name_label:
		_info_name_label.text = ""
	if _info_desc_label:
		_info_desc_label.text = ""
	if _info_value_label:
		_info_value_label.text = ""

func _on_inventory_changed() -> void:
	refresh()

func _on_theme_changed(_new_theme: Resource) -> void:
	_apply_style()

# Public API

func set_inventory(inventory: Variant) -> void:
	_inventory = inventory
	if _inventory and _inventory.has_signal("changed"):
		_inventory.changed.connect(_on_inventory_changed)
	refresh()

func get_inventory() -> Variant:
	return _inventory

func refresh() -> void:
	if not _grid_container:
		return

	# Get equipment items from PlayerData inventory
	var items: Array = []
	if has_node("/root/PlayerData"):
		var player_data = get_node("/root/PlayerData")
		items = player_data.inventory

	_item_count = items.size()

	# Ensure we have enough slots
	_recalculate_grid()

	# Populate slots with items
	for i in range(_slots.size()):
		if i < items.size():
			_slots[i].set_slot_data(items[i])
		else:
			_slots[i].clear_slot()

func get_slot(index: int) -> DragDropSlot:
	if index >= 0 and index < _slots.size():
		return _slots[index]
	return null

func get_all_slots() -> Array[DragDropSlot]:
	return _slots

func add_item(data: Variant) -> int:
	# Find first empty slot
	for i in range(_slots.size()):
		if _slots[i].is_empty():
			_slots[i].set_slot_data(data)
			_item_count += 1
			_update_slot_count()
			return i
	return -1

func remove_item(index: int) -> Variant:
	if index >= 0 and index < _slots.size():
		var data: Variant = _slots[index].get_slot_data()
		_slots[index].clear_slot()
		if data:
			_item_count = maxi(0, _item_count - 1)
			_update_slot_count()
		return data
	return null

func clear_all() -> void:
	for slot in _slots:
		slot.clear_slot()
	_item_count = 0
	_update_slot_count()

# Sorting
func sort_by_name() -> void:
	if _inventory_controller and _inventory_controller.has_method("sort_inventory"):
		_inventory_controller.sort_inventory("name")
	elif _inventory and _inventory.has_method("sort_by_name"):
		_inventory.sort_by_name()
	refresh()

func sort_by_type() -> void:
	if _inventory_controller and _inventory_controller.has_method("sort_inventory"):
		_inventory_controller.sort_inventory("type")
	elif _inventory and _inventory.has_method("sort_by_type"):
		_inventory.sort_by_type()
	refresh()

func sort_by_value() -> void:
	if _inventory_controller and _inventory_controller.has_method("sort_inventory"):
		_inventory_controller.sort_inventory("value")
	elif _inventory and _inventory.has_method("sort_by_value"):
		_inventory.sort_by_value()
	refresh()

# Drop target methods - allows dropping equipment anywhere on panel to unequip
func can_accept_drop(data: Variant) -> bool:
	# Only accept EquipmentInstance (for unequipping from equipment slots)
	return data is EquipmentInstance

func handle_drop(data: Variant, source_slot: Control) -> bool:
	# Only handle equipment from equipment slots
	if not data is EquipmentInstance:
		return false
	if not source_slot is EquipmentSlot:
		return false

	# Clear the equipment slot (triggers Character.unequip)
	(source_slot as EquipmentSlot).clear_equipment()

	# Add to inventory
	PlayerData.add_to_inventory(data)

	return true

# Show info for an external item (e.g., from equipment slot)
# Clears any inventory slot selection
func show_item_info(item: Variant) -> void:
	# Deselect any inventory slot
	if _selected_slot and is_instance_valid(_selected_slot):
		_selected_slot.set_highlighted(false)
		_selected_slot = null

	_selected_item = item
	_update_info_panel(item)

# Clear selection and info panel
func clear_selection() -> void:
	if _selected_slot and is_instance_valid(_selected_slot):
		_selected_slot.set_highlighted(false)
	_selected_slot = null
	_selected_item = null
	_clear_info_panel()

# Drag highlight handlers - called by DragDropManager
func _on_mouse_entered() -> void:
	# Show highlight only when dragging equipment
	if DragDropManager and DragDropManager.is_dragging():
		var drag_data: Variant = DragDropManager.get_drag_data()
		if drag_data is EquipmentInstance and _drag_highlight:
			_drag_highlight.visible = true

func _on_mouse_exited() -> void:
	if _drag_highlight:
		_drag_highlight.visible = false

func _on_drag_ended(_source_slot: Control, _dropped: bool) -> void:
	# Hide highlight when any drag ends
	if _drag_highlight:
		_drag_highlight.visible = false

func show_drag_highlight(show: bool) -> void:
	if _drag_highlight:
		_drag_highlight.visible = show
