class_name BookmarkTabBar
extends HBoxContainer

## BookmarkTabBar
## A horizontal tab bar with medieval bookmark-style tabs
## Active tab appears "forward" with full color, inactive tabs are recessed

signal tab_changed(index: int)
signal tab_hovered(index: int)

# Tab data structure
class TabData:
	var label: String
	var icon: Texture2D
	var enabled: bool = true
	var metadata: Variant = null

	func _init(p_label: String, p_icon: Texture2D = null, p_enabled: bool = true) -> void:
		label = p_label
		icon = p_icon
		enabled = p_enabled

@export var tabs: Array[String] = []:
	set(value):
		tabs = value
		_rebuild_tabs()

@export var active_tab: int = 0:
	set(value):
		var old_tab := active_tab
		active_tab = clampi(value, 0, max(0, _tab_buttons.size() - 1))
		if old_tab != active_tab:
			_update_tab_states()
			tab_changed.emit(active_tab)

@export var tab_min_width: float = 64.0
@export var tab_max_width: float = 150.0
@export var allow_deselect: bool = false

# Internal
var _tab_buttons: Array[Button] = []
var _tab_data: Array[TabData] = []

func _ready() -> void:
	# Container settings - fill width for even tab distribution
	alignment = BoxContainer.ALIGNMENT_CENTER
	size_flags_horizontal = Control.SIZE_EXPAND_FILL
	add_theme_constant_override("separation", -2)  # Slight overlap for bookmark effect

	if tabs.size() > 0:
		_rebuild_tabs()

	# Listen for theme changes
	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

func _rebuild_tabs() -> void:
	# Clear existing buttons
	for btn in _tab_buttons:
		btn.queue_free()
	_tab_buttons.clear()

	# Build from string array (simple mode)
	_tab_data.clear()
	for tab_label in tabs:
		_tab_data.append(TabData.new(tab_label))

	_create_tab_buttons()

func _create_tab_buttons() -> void:
	for i in range(_tab_data.size()):
		var data := _tab_data[i]
		var btn := _create_tab_button(i, data)
		add_child(btn)
		_tab_buttons.append(btn)

	_update_tab_states()

func _create_tab_button(index: int, data: TabData) -> Button:
	var btn := Button.new()
	btn.name = "Tab_%d" % index
	btn.text = data.label
	btn.toggle_mode = true
	btn.button_group = null  # We'll handle selection ourselves
	btn.disabled = not data.enabled
	btn.custom_minimum_size.x = tab_min_width
	# Fill width evenly - all tabs get equal space
	btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	btn.size_flags_stretch_ratio = 1.0

	# Set icon if provided
	if data.icon:
		btn.icon = data.icon

	# Don't clip text - we want full tab names visible
	btn.clip_text = false

	# Connect signals
	btn.pressed.connect(_on_tab_pressed.bind(index))
	btn.mouse_entered.connect(_on_tab_mouse_entered.bind(index))

	return btn

func _update_tab_states() -> void:
	var theme := UIThemeManager.get_theme()

	for i in range(_tab_buttons.size()):
		var btn := _tab_buttons[i]
		var is_active := (i == active_tab)

		btn.button_pressed = is_active
		_apply_tab_style(btn, is_active, theme)

func _apply_tab_style(btn: Button, is_active: bool, theme: UITheme) -> void:
	# Create bookmark-style StyleBoxes
	var stylebox: StyleBoxFlat

	if is_active:
		stylebox = theme.create_tab_stylebox("active")
		# Active tab is slightly taller (extends down to cover content border)
		btn.custom_minimum_size.y = UIConstants.TAB_HEIGHT + 4
	else:
		stylebox = theme.create_tab_stylebox("inactive")
		btn.custom_minimum_size.y = UIConstants.TAB_HEIGHT

	# Apply to all button states
	btn.add_theme_stylebox_override("normal", stylebox)
	btn.add_theme_stylebox_override("pressed", stylebox)

	# Hover state
	var hover_style := theme.create_tab_stylebox("hover" if not is_active else "active")
	btn.add_theme_stylebox_override("hover", hover_style)

	# Disabled state
	var disabled_style := theme.create_tab_stylebox("inactive")
	disabled_style.bg_color = disabled_style.bg_color.darkened(0.2)
	btn.add_theme_stylebox_override("disabled", disabled_style)

	# Text colors
	if is_active:
		btn.add_theme_color_override("font_color", theme.text_color)
		btn.add_theme_color_override("font_pressed_color", theme.text_color)
		btn.add_theme_color_override("font_hover_color", theme.text_color)
	else:
		btn.add_theme_color_override("font_color", theme.text_color_secondary)
		btn.add_theme_color_override("font_pressed_color", theme.text_color)
		btn.add_theme_color_override("font_hover_color", theme.text_color)

	btn.add_theme_color_override("font_disabled_color", theme.text_color_disabled)

	# Font
	btn.add_theme_font_size_override("font_size", theme.font_size_button)
	if theme.font_button:
		btn.add_theme_font_override("font", theme.font_button)

func _on_tab_pressed(index: int) -> void:
	if allow_deselect and index == active_tab:
		# Deselect current tab
		active_tab = -1
	else:
		active_tab = index

func _on_tab_mouse_entered(index: int) -> void:
	tab_hovered.emit(index)

func _on_theme_changed(_new_theme: Resource) -> void:
	_update_tab_states()

# Public API

func get_active_tab() -> int:
	return active_tab

func set_active_tab(index: int) -> void:
	active_tab = index

func get_tab_count() -> int:
	return _tab_buttons.size()

func get_tab_label(index: int) -> String:
	if index >= 0 and index < _tab_data.size():
		return _tab_data[index].label
	return ""

func set_tab_label(index: int, label: String) -> void:
	if index >= 0 and index < _tab_data.size():
		_tab_data[index].label = label
		if index < _tab_buttons.size():
			_tab_buttons[index].text = label

func set_tab_enabled(index: int, enabled: bool) -> void:
	if index >= 0 and index < _tab_data.size():
		_tab_data[index].enabled = enabled
		if index < _tab_buttons.size():
			_tab_buttons[index].disabled = not enabled

func set_tab_icon(index: int, icon: Texture2D) -> void:
	if index >= 0 and index < _tab_data.size():
		_tab_data[index].icon = icon
		if index < _tab_buttons.size():
			_tab_buttons[index].icon = icon

func add_tab(label: String, icon: Texture2D = null, enabled: bool = true) -> int:
	var data := TabData.new(label, icon, enabled)
	_tab_data.append(data)
	tabs.append(label)

	var btn := _create_tab_button(_tab_data.size() - 1, data)
	add_child(btn)
	_tab_buttons.append(btn)
	_update_tab_states()

	return _tab_data.size() - 1

func remove_tab(index: int) -> void:
	if index < 0 or index >= _tab_buttons.size():
		return

	_tab_buttons[index].queue_free()
	_tab_buttons.remove_at(index)
	_tab_data.remove_at(index)
	tabs.remove_at(index)

	# Adjust active tab if needed
	if active_tab >= _tab_buttons.size():
		active_tab = max(0, _tab_buttons.size() - 1)
	else:
		_update_tab_states()

func clear_tabs() -> void:
	for btn in _tab_buttons:
		btn.queue_free()
	_tab_buttons.clear()
	_tab_data.clear()
	tabs.clear()
	active_tab = 0

# Set tab metadata for custom data
func set_tab_metadata(index: int, metadata: Variant) -> void:
	if index >= 0 and index < _tab_data.size():
		_tab_data[index].metadata = metadata

func get_tab_metadata(index: int) -> Variant:
	if index >= 0 and index < _tab_data.size():
		return _tab_data[index].metadata
	return null

# Refresh styling
func refresh_style() -> void:
	_update_tab_states()

# Create BookmarkTabBar programmatically
static func create(tab_labels: Array[String]) -> BookmarkTabBar:
	var bar := BookmarkTabBar.new()
	bar.tabs = tab_labels
	return bar
