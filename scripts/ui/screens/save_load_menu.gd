class_name SaveLoadMenu
extends Control

## SaveLoadMenu
## Save/Load screen with 6-8 save slots
## Each slot shows: thumbnail placeholder, timestamp, playtime
## Supports both save and load modes

signal menu_closed
signal save_requested(slot_index: int)
signal load_requested(slot_index: int)
signal delete_requested(slot_index: int)

enum Mode {
	SAVE,
	LOAD
}

const SAVE_SLOT_COUNT := 8
const SAVE_DIR := "user://saves/"
const SAVE_FILE_PREFIX := "save_"
const SAVE_FILE_EXT := ".sav"

# Current mode
var _mode: Mode = Mode.LOAD

# Save data cache
var _save_data: Array[Dictionary] = []

# UI References
var _background: ColorRect
var _main_panel: NinePatchRect
var _title_label: Label
var _close_button: IconButton
var _slots_container: VBoxContainer
var _save_slots: Array[Control] = []
var _action_button: ThemedButton

func _ready() -> void:
	_ensure_save_directory()
	_load_save_metadata()
	_build_ui()

	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

func _ensure_save_directory() -> void:
	var dir := DirAccess.open("user://")
	if dir and not dir.dir_exists("saves"):
		dir.make_dir("saves")

func _load_save_metadata() -> void:
	_save_data.clear()
	_save_data.resize(SAVE_SLOT_COUNT)

	for i in SAVE_SLOT_COUNT:
		var path := _get_save_path(i)
		var meta := _read_save_metadata(path)
		_save_data[i] = meta

func _read_save_metadata(path: String) -> Dictionary:
	var file := FileAccess.open(path, FileAccess.READ)
	if not file:
		return {}

	# Try to read as JSON metadata
	var json_string := file.get_line()
	file.close()

	var json := JSON.new()
	if json.parse(json_string) == OK:
		var data = json.get_data()
		if data is Dictionary:
			return data

	return {}

func _get_save_path(index: int) -> String:
	return SAVE_DIR + SAVE_FILE_PREFIX + str(index) + SAVE_FILE_EXT

func _build_ui() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)

	var theme := UIThemeManager.get_theme()

	# Background overlay
	_background = ColorRect.new()
	_background.name = "Background"
	_background.set_anchors_preset(Control.PRESET_FULL_RECT)
	_background.color = theme.bg_overlay
	add_child(_background)

	# Center container
	var center := CenterContainer.new()
	center.name = "CenterContainer"
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(center)

	# Main panel
	_main_panel = BasePanel.create(BasePanel.PanelStyle.PRIMARY, true)
	_main_panel.name = "SaveLoadPanel"
	_main_panel.custom_minimum_size = Vector2(UIConstants.MENU_PANEL_WIDTH, UIConstants.MENU_PANEL_HEIGHT)
	center.add_child(_main_panel)

	# Panel margin
	var margin := MarginContainer.new()
	margin.name = "Margin"
	margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	margin.add_theme_constant_override("margin_left", theme.padding_panel)
	margin.add_theme_constant_override("margin_right", theme.padding_panel)
	margin.add_theme_constant_override("margin_top", theme.padding_panel)
	margin.add_theme_constant_override("margin_bottom", theme.padding_panel)
	_main_panel.add_child(margin)

	# Main VBox
	var main_vbox := VBoxContainer.new()
	main_vbox.name = "MainVBox"
	main_vbox.add_theme_constant_override("separation", theme.spacing_medium)
	margin.add_child(main_vbox)

	# Header
	_build_header(main_vbox)

	# Scrollable slots container
	var scroll := ScrollContainer.new()
	scroll.name = "ScrollContainer"
	scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	main_vbox.add_child(scroll)

	_slots_container = VBoxContainer.new()
	_slots_container.name = "SlotsContainer"
	_slots_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_slots_container.add_theme_constant_override("separation", theme.spacing_small)
	scroll.add_child(_slots_container)

	# Build save slots
	_build_save_slots()

	# Footer
	_build_footer(main_vbox)

	# Update display based on mode
	_update_mode_display()

func _build_header(parent: VBoxContainer) -> void:
	var theme := UIThemeManager.get_theme()

	var header := HBoxContainer.new()
	header.name = "Header"
	parent.add_child(header)

	_title_label = Label.new()
	_title_label.text = "Load Game" if _mode == Mode.LOAD else "Save Game"
	_title_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_title_label.add_theme_font_size_override("font_size", theme.font_size_header)
	_title_label.add_theme_color_override("font_color", theme.accent_color)
	header.add_child(_title_label)

	_close_button = IconButton.new()
	_close_button.preset_icon = IconButton.PresetIcon.CLOSE
	_close_button.icon_position = IconButton.IconPosition.ONLY
	_close_button.button_style = ThemedButton.ButtonStyle.GHOST
	_close_button.custom_minimum_size = Vector2(UIConstants.ICON_BUTTON_SIZE, UIConstants.ICON_BUTTON_SIZE)
	_close_button.pressed.connect(_on_close_pressed)
	header.add_child(_close_button)

func _build_save_slots() -> void:
	_save_slots.clear()

	for i in SAVE_SLOT_COUNT:
		var slot := _create_save_slot(i)
		_slots_container.add_child(slot)
		_save_slots.append(slot)

func _create_save_slot(index: int) -> Control:
	var theme := UIThemeManager.get_theme()
	var data := _save_data[index] if index < _save_data.size() else {}
	var has_save := not data.is_empty()

	# Slot container button
	var slot_btn := Button.new()
	slot_btn.name = "SaveSlot_%d" % index
	slot_btn.custom_minimum_size.y = UIConstants.BUTTON_HEIGHT_LARGE
	slot_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slot_btn.pressed.connect(_on_slot_pressed.bind(index))

	# Style the button (no content margins - use internal MarginContainer)
	var normal_style := StyleBoxFlat.new()
	normal_style.bg_color = theme.bg_secondary
	normal_style.border_color = theme.border_panel
	normal_style.set_border_width_all(1)
	normal_style.set_corner_radius_all(4)
	slot_btn.add_theme_stylebox_override("normal", normal_style)

	var hover_style := normal_style.duplicate()
	hover_style.bg_color = theme.bg_primary
	hover_style.border_color = theme.border_highlight
	slot_btn.add_theme_stylebox_override("hover", hover_style)

	var pressed_style := normal_style.duplicate()
	pressed_style.bg_color = theme.slot_filled_color
	slot_btn.add_theme_stylebox_override("pressed", pressed_style)

	# Padding via MarginContainer (per UI rules)
	var slot_margin := MarginContainer.new()
	slot_margin.name = "SlotMargin"
	slot_margin.mouse_filter = Control.MOUSE_FILTER_IGNORE
	slot_margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	slot_margin.add_theme_constant_override("margin_left", theme.padding_container)
	slot_margin.add_theme_constant_override("margin_right", theme.padding_container)
	slot_margin.add_theme_constant_override("margin_top", theme.padding_container)
	slot_margin.add_theme_constant_override("margin_bottom", theme.padding_container)
	slot_btn.add_child(slot_margin)

	# Content HBox inside margin
	var hbox := HBoxContainer.new()
	hbox.name = "Content"
	hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_theme_constant_override("separation", theme.spacing_medium)
	slot_margin.add_child(hbox)

	# Thumbnail placeholder
	var thumb := ColorRect.new()
	thumb.name = "Thumbnail"
	thumb.custom_minimum_size = Vector2(UIConstants.THUMBNAIL_WIDTH, UIConstants.THUMBNAIL_HEIGHT)
	thumb.color = theme.bg_primary if has_save else Color(0.3, 0.3, 0.3, 0.5)
	thumb.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(thumb)

	# Thumbnail placeholder icon
	var thumb_label := Label.new()
	thumb_label.text = "?" if not has_save else ""
	thumb_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	thumb_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	thumb_label.set_anchors_preset(Control.PRESET_FULL_RECT)
	thumb_label.add_theme_color_override("font_color", theme.text_color_secondary)
	thumb_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	thumb.add_child(thumb_label)

	# Info VBox
	var info_vbox := VBoxContainer.new()
	info_vbox.name = "Info"
	info_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	info_vbox.add_theme_constant_override("separation", theme.spacing_tiny)
	info_vbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(info_vbox)

	# Slot title
	var title := Label.new()
	title.name = "Title"
	if has_save:
		var save_name: String = data.get("name", "Save %d" % (index + 1))
		title.text = save_name
	else:
		title.text = "Empty Slot %d" % (index + 1)
	title.add_theme_font_size_override("font_size", theme.font_size_body)
	title.add_theme_color_override("font_color", theme.text_color if has_save else theme.text_color_secondary)
	title.mouse_filter = Control.MOUSE_FILTER_IGNORE
	info_vbox.add_child(title)

	# Timestamp and playtime
	var details := Label.new()
	details.name = "Details"
	if has_save:
		var timestamp: String = data.get("timestamp", "Unknown")
		var playtime: int = data.get("playtime_seconds", 0)
		var playtime_str := _format_playtime(playtime)
		details.text = "%s | %s" % [timestamp, playtime_str]
	else:
		details.text = "No save data"
	details.add_theme_font_size_override("font_size", theme.font_size_small)
	details.add_theme_color_override("font_color", theme.text_color_secondary)
	details.mouse_filter = Control.MOUSE_FILTER_IGNORE
	info_vbox.add_child(details)

	# Delete button (only if save exists and in appropriate mode)
	if has_save:
		var delete_btn := IconButton.new()
		delete_btn.name = "DeleteButton"
		delete_btn.preset_icon = IconButton.PresetIcon.TRASH
		delete_btn.icon_position = IconButton.IconPosition.ONLY
		delete_btn.button_style = ThemedButton.ButtonStyle.GHOST
		delete_btn.custom_minimum_size = Vector2(UIConstants.ICON_BUTTON_SIZE_SMALL, UIConstants.ICON_BUTTON_SIZE_SMALL)
		delete_btn.tooltip_text = "Delete save"
		delete_btn.pressed.connect(_on_delete_pressed.bind(index))
		hbox.add_child(delete_btn)

	return slot_btn

func _build_footer(parent: VBoxContainer) -> void:
	var theme := UIThemeManager.get_theme()

	var sep := HSeparator.new()
	parent.add_child(sep)

	var footer := HBoxContainer.new()
	footer.name = "Footer"
	footer.alignment = BoxContainer.ALIGNMENT_END
	footer.add_theme_constant_override("separation", theme.spacing_medium)
	parent.add_child(footer)

	# Mode toggle button
	var mode_btn := ThemedButton.new()
	mode_btn.name = "ModeToggle"
	mode_btn.text = "Save Mode" if _mode == Mode.LOAD else "Load Mode"
	mode_btn.button_style = ThemedButton.ButtonStyle.SECONDARY
	mode_btn.pressed.connect(_on_mode_toggle_pressed)
	footer.add_child(mode_btn)

func _format_playtime(seconds: int) -> String:
	var hours := seconds / 3600
	var minutes := (seconds % 3600) / 60
	if hours > 0:
		return "%dh %dm" % [hours, minutes]
	else:
		return "%dm" % minutes

func _update_mode_display() -> void:
	if _title_label:
		_title_label.text = "Load Game" if _mode == Mode.LOAD else "Save Game"

func _refresh_slots() -> void:
	# Remove old slots
	for slot in _save_slots:
		slot.queue_free()
	_save_slots.clear()

	# Reload metadata and rebuild
	_load_save_metadata()
	_build_save_slots()

func _on_slot_pressed(index: int) -> void:
	var data := _save_data[index] if index < _save_data.size() else {}
	var has_save := not data.is_empty()

	match _mode:
		Mode.SAVE:
			# Allow saving to any slot
			save_requested.emit(index)
			_do_save(index)
		Mode.LOAD:
			# Only allow loading if save exists
			if has_save:
				load_requested.emit(index)
				_do_load(index)

func _on_delete_pressed(index: int) -> void:
	delete_requested.emit(index)
	_do_delete(index)

func _on_mode_toggle_pressed() -> void:
	_mode = Mode.SAVE if _mode == Mode.LOAD else Mode.LOAD
	_update_mode_display()

	# Update footer button text
	var footer := _main_panel.get_node_or_null("Margin/MainVBox/Footer")
	if footer:
		var mode_btn := footer.get_node_or_null("ModeToggle")
		if mode_btn:
			mode_btn.text = "Save Mode" if _mode == Mode.LOAD else "Load Mode"

func _on_close_pressed() -> void:
	menu_closed.emit()
	_close_menu()

func _close_menu() -> void:
	if UIManager:
		UIManager.close_top_menu()
	else:
		queue_free()

func _on_theme_changed(_new_theme: Resource) -> void:
	var theme := UIThemeManager.get_theme()
	_background.color = theme.bg_overlay

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		_on_close_pressed()
		get_viewport().set_input_as_handled()

# Save/Load implementation stubs
func _do_save(index: int) -> void:
	# Create save metadata
	var now := Time.get_datetime_dict_from_system()
	var timestamp := "%04d-%02d-%02d %02d:%02d" % [
		now.year, now.month, now.day, now.hour, now.minute
	]

	var meta := {
		"name": "Save %d" % (index + 1),
		"timestamp": timestamp,
		"playtime_seconds": 0,  # Would come from game state
		"version": 1
	}

	# Save to file
	var path := _get_save_path(index)
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file:
		file.store_line(JSON.stringify(meta))
		# Would store actual game state here
		file.close()
		_refresh_slots()

func _do_load(index: int) -> void:
	var path := _get_save_path(index)
	if not FileAccess.file_exists(path):
		return

	# Emit signal - actual loading handled by game systems
	# Would trigger BusSave.load_game(path) or similar
	pass

func _do_delete(index: int) -> void:
	var path := _get_save_path(index)
	if FileAccess.file_exists(path):
		var dir := DirAccess.open(SAVE_DIR)
		if dir:
			dir.remove(SAVE_FILE_PREFIX + str(index) + SAVE_FILE_EXT)
			_refresh_slots()

# Public API

func set_mode(mode: Mode) -> void:
	_mode = mode
	_update_mode_display()

func get_mode() -> Mode:
	return _mode

func refresh() -> void:
	_refresh_slots()

func get_save_count() -> int:
	var count := 0
	for data in _save_data:
		if not data.is_empty():
			count += 1
	return count

func has_any_saves() -> bool:
	return get_save_count() > 0
