class_name InventoryPanelNew
extends PanelContainer

## InventoryPanel (Refactored)
## Dynamic inventory grid in inset scroll container.
## Displays inventory items with always-visible scrollbar.
## Shows 2 empty rows below last item.
##
## Uses SlotGridBinder for data binding (see docs/ui_ai_rules.md):
##   - _binder handles slot creation via _create_inventory_slot factory
##   - refresh() calls _binder.bind_items() with calculated extra slots
##   - Binder signals connected for selection and drop handling
##
## SelectablePanel Compliance (duck-typed):
##   - clear_selection(): Clears slot highlighting and info panel
##   - get_selected_item(): Returns selected inventory item or null
##   - show_item_info(item): Shows info for external item (from equipment)
##   - item_selected signal: Emitted when selection changes
## Used by PartyMenuCoordinator for cross-panel selection coordination.
##
## Drop Target Contract Implementation:
##   - can_accept_drop(data): Accepts EquipmentInstance for unequipping
##   - handle_drop(data, source): Handles equipment unequip to inventory
## See docs/ui_drop_target_contract.md for contract details.

signal item_selected(index: int, data: Variant)
signal item_used(index: int, data: Variant)
signal item_dropped(target_index: int, source_index: int)
signal sort_requested(sort_type: String)

# Configuration
@export var show_info_panel: bool = true
@export var extra_empty_rows: int = 2  # Empty rows below last item

# References
var _inventory: Variant = null  # Inventory object
var _inventory_controller: Variant = null

# UI References
var _main_hbox: HBoxContainer
var _inset_container: PanelContainer
var _scroll_container: ScrollContainer
var _grid_container: GridContainer
var _binder: SlotGridBinder  # Handles slot creation and data binding
var _info_panel: PanelContainer
var _info_name_label: Label
var _info_desc_label: Label
var _info_value_label: Label
var _drag_highlight: ColorRect  # Overlay for drag feedback

# Layout calculations
var _slot_size: float = UIConstants.SLOT_SIZE_ICON
var _slot_spacing: float = 0.0  # Set from theme in _ready
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

	# Set slot spacing from theme (small = 8px, appropriate for tight grid)
	_slot_spacing = theme.spacing_small

	# Main content: HBox with scroll (left) and info (right)
	_main_hbox = UIThemeManager.make_hbox("small")
	_main_hbox.name = "MainHBox"
	_main_hbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_main_hbox.size_flags_vertical = Control.SIZE_EXPAND_FILL
	add_child(_main_hbox)

	# Left: Inventory scroll container
	_build_inventory_scroll()

	# Right: Info panel
	if show_info_panel:
		_build_info_panel()

func _build_inventory_scroll() -> void:
	var theme := UIThemeManager.get_theme()

	# Calculate fixed width for 2 columns + scrollbar + padding
	# 2 columns * slot_size + 1 gap + scrollbar (~24px) + inset padding (8*2)
	var scrollbar_width := 24
	var grid_width := (UIConstants.SLOT_SIZE_ICON * 2) + int(_slot_spacing)
	var scroll_area_width := grid_width + scrollbar_width + (UIConstants.INSET_PADDING * 2)

	# Inset/recessed container for the scroll area (left side)
	_inset_container = PanelContainer.new()
	_inset_container.name = "InsetContainer"
	_inset_container.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	_inset_container.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_inset_container.custom_minimum_size = Vector2(scroll_area_width, 0)

	# Create inset/recessed style (no content margins - use MarginContainer)
	var inset_style := StyleBoxFlat.new()
	inset_style.bg_color = theme.bg_secondary.darkened(0.15)
	inset_style.set_border_width_all(2)
	inset_style.border_color = theme.bg_secondary.darkened(0.3)
	inset_style.set_corner_radius_all(4)
	# Inset effect - darker on top-left, lighter on bottom-right
	inset_style.shadow_color = Color(0, 0, 0, 0.2)
	inset_style.shadow_size = 2
	inset_style.shadow_offset = Vector2(1, 1)
	_inset_container.add_theme_stylebox_override("panel", inset_style)
	_main_hbox.add_child(_inset_container)

	# Padding via MarginContainer (per UI rules)
	var inset_margin := MarginContainer.new()
	inset_margin.name = "InsetMargin"
	inset_margin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	inset_margin.size_flags_vertical = Control.SIZE_EXPAND_FILL
	inset_margin.add_theme_constant_override("margin_left", UIConstants.INSET_PADDING)
	inset_margin.add_theme_constant_override("margin_right", UIConstants.INSET_PADDING)
	inset_margin.add_theme_constant_override("margin_top", UIConstants.INSET_PADDING)
	inset_margin.add_theme_constant_override("margin_bottom", UIConstants.INSET_PADDING)
	_inset_container.add_child(inset_margin)

	# Drag highlight overlay (hidden by default)
	_drag_highlight = ColorRect.new()
	_drag_highlight.name = "DragHighlight"
	_drag_highlight.color = Color(theme.accent_color.r, theme.accent_color.g, theme.accent_color.b, 0.15)
	_drag_highlight.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_drag_highlight.visible = false
	_drag_highlight.set_anchors_preset(Control.PRESET_FULL_RECT)
	inset_margin.add_child(_drag_highlight)

	# Scroll container with always-visible scrollbar
	_scroll_container = ScrollContainer.new()
	_scroll_container.name = "ScrollContainer"
	_scroll_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll_container.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_scroll_container.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_scroll_container.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_SHOW_ALWAYS
	inset_margin.add_child(_scroll_container)

	# Grid container for inventory slots
	_grid_container = GridContainer.new()
	_grid_container.name = "InventoryGrid"
	_grid_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_grid_container.add_theme_constant_override("h_separation", int(_slot_spacing))
	_grid_container.add_theme_constant_override("v_separation", int(_slot_spacing))
	_scroll_container.add_child(_grid_container)

	# Set up SlotGridBinder for data binding
	_binder = SlotGridBinder.new()
	_binder.setup(_grid_container, _create_inventory_slot, Vector2(_slot_size, _slot_size))
	_binder.slot_clicked.connect(_on_binder_slot_clicked)
	_binder.slot_hovered.connect(_on_binder_slot_hovered)
	_binder.slot_unhovered.connect(_on_binder_slot_unhovered)
	_binder.item_dropped.connect(_on_binder_item_dropped)

## Slot factory for SlotGridBinder
func _create_inventory_slot(index: int) -> DragDropSlot:
	var slot := InventorySlot.new()
	slot.slot_index = index
	return slot

func _build_info_panel() -> void:
	_info_panel = PanelContainer.new()
	_info_panel.name = "InfoPanel"
	# Fill remaining space (right side of HBox)
	_info_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_info_panel.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_info_panel.size_flags_stretch_ratio = 0.45  # 45% width for info
	_info_panel.custom_minimum_size.x = 100  # Minimum width
	_main_hbox.add_child(_info_panel)

	# StyleBox with no content margins (padding via MarginContainer)
	var info_style := UIThemeManager.create_bg_stylebox("primary")
	_info_panel.add_theme_stylebox_override("panel", info_style)

	# Padding via MarginContainer (per UI rules)
	var info_padding := UIThemeManager.make_margin_container("container")
	_info_panel.add_child(info_padding)

	var info_vbox := UIThemeManager.make_vbox("tiny")
	info_padding.add_child(info_vbox)

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

	# Fixed 2 columns per UI design
	_columns = UIConstants.INVENTORY_COLUMNS
	_grid_container.columns = _columns

func _calculate_extra_slots() -> int:
	# Calculate extra empty slots: extra_empty_rows worth of columns
	# Plus minimum 3 rows total
	var rows_for_items := ceili(float(_item_count) / float(maxi(1, _columns)))
	var total_rows := rows_for_items + extra_empty_rows
	var target_slot_count := total_rows * _columns

	# Minimum slot count (at least show some empty slots)
	target_slot_count = maxi(target_slot_count, _columns * 3)

	return maxi(0, target_slot_count - _item_count)

func _connect_inventory() -> void:
	# Connect to PlayerData equipment inventory
	if has_node("/root/PlayerData"):
		var player_data = get_node("/root/PlayerData")
		if player_data.has_signal("inventory_changed"):
			player_data.inventory_changed.connect(_on_inventory_changed)
		refresh()

# Binder signal handlers
func _on_binder_slot_clicked(index: int, slot: DragDropSlot) -> void:
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

func _on_binder_slot_hovered(index: int, slot: DragDropSlot) -> void:
	# Show hover info only if no slot is selected
	if _selected_slot == null and _selected_item == null:
		var data: Variant = slot.get_slot_data()
		_update_info_panel(data)

func _on_binder_slot_unhovered(_index: int, _slot: DragDropSlot) -> void:
	# Only clear if no slot is selected
	if _selected_slot == null and _selected_item == null:
		_clear_info_panel()

func _on_binder_item_dropped(target_index: int, _data: Variant, source_slot: Control) -> void:
	var source_index: int = -1
	if source_slot and "slot_index" in source_slot:
		source_index = source_slot.slot_index
	item_dropped.emit(target_index, source_index)

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
	if not _grid_container or not _binder:
		return

	# Get equipment items from PlayerData inventory
	var items: Array = []
	if has_node("/root/PlayerData"):
		var player_data = get_node("/root/PlayerData")
		items = player_data.inventory

	_item_count = items.size()

	# Ensure grid has correct columns
	_recalculate_grid()

	# Use binder to bind items with extra empty slots
	var extra_slots := _calculate_extra_slots()
	_binder.bind_items(items, extra_slots)

func get_slot(index: int) -> DragDropSlot:
	if _binder:
		return _binder.get_slot(index)
	return null

func get_all_slots() -> Array[DragDropSlot]:
	if _binder:
		return _binder.get_slots()
	return []

func add_item(data: Variant) -> int:
	if _binder:
		var index := _binder.add_item(data)
		if index >= 0:
			_item_count += 1
		return index
	return -1

func remove_item(index: int) -> Variant:
	if _binder:
		var data: Variant = _binder.remove_item(index)
		if data:
			_item_count = maxi(0, _item_count - 1)
		return data
	return null

func clear_all() -> void:
	if _binder:
		_binder.clear()
	_item_count = 0

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

# Get the currently selected item (from slot or external)
func get_selected_item() -> Variant:
	if _selected_slot and is_instance_valid(_selected_slot):
		return _selected_slot.get_slot_data()
	return _selected_item

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
