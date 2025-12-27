class_name StatsPanel
extends PanelContainer

## StatsPanel
## Displays character stats (STR, DEX, CON, INT, WIS, CHA)
## Shows stat name, value, and modifier

signal stat_clicked(stat_name: String)

# D&D-style stats
const STAT_NAMES: Array[String] = ["STR", "DEX", "CON", "INT", "WIS", "CHA"]
const STAT_FULL_NAMES: Dictionary = {
	"STR": "Strength",
	"DEX": "Dexterity",
	"CON": "Constitution",
	"INT": "Intelligence",
	"WIS": "Wisdom",
	"CHA": "Charisma"
}

# Character data reference
var _character_data: Variant = null

# UI References
var _main_vbox: VBoxContainer
var _title_label: Label
var _stats_container: VBoxContainer
var _stat_rows: Dictionary = {}  # stat_name -> {container, name_label, value_label, modifier_label}

func _ready() -> void:
	_build_ui()
	_apply_style()

	# Listen for theme changes
	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

func _build_ui() -> void:
	# Apply panel styling
	var stylebox := UIThemeManager.create_bg_stylebox("secondary")
	add_theme_stylebox_override("panel", stylebox)

	# Main vertical layout
	_main_vbox = VBoxContainer.new()
	_main_vbox.name = "MainVBox"
	_main_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_main_vbox.size_flags_vertical = Control.SIZE_EXPAND_FILL
	add_child(_main_vbox)

	var theme := UIThemeManager.get_theme()
	_main_vbox.add_theme_constant_override("separation", theme.spacing_medium)

	# Add margin
	var margin := MarginContainer.new()
	margin.name = "Margin"
	margin.add_theme_constant_override("margin_left", theme.padding_panel)
	margin.add_theme_constant_override("margin_right", theme.padding_panel)
	margin.add_theme_constant_override("margin_top", theme.padding_panel)
	margin.add_theme_constant_override("margin_bottom", theme.padding_panel)
	_main_vbox.add_child(margin)

	var content := VBoxContainer.new()
	content.add_theme_constant_override("separation", theme.spacing_medium)
	margin.add_child(content)

	# Title
	_title_label = Label.new()
	_title_label.name = "TitleLabel"
	_title_label.text = "Character Stats"
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	content.add_child(_title_label)

	# Separator
	var sep := HSeparator.new()
	content.add_child(sep)

	# Stats container
	_stats_container = VBoxContainer.new()
	_stats_container.name = "StatsContainer"
	_stats_container.add_theme_constant_override("separation", theme.spacing_small)
	content.add_child(_stats_container)

	# Create stat rows
	for stat_name in STAT_NAMES:
		_create_stat_row(stat_name)

	# Spacer
	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	content.add_child(spacer)

	# Derived stats section
	_build_derived_stats_section(content)

func _create_stat_row(stat_name: String) -> void:
	var theme := UIThemeManager.get_theme()

	var row := HBoxContainer.new()
	row.name = "Row_%s" % stat_name
	row.add_theme_constant_override("separation", theme.spacing_medium)
	_stats_container.add_child(row)

	# Stat name (abbreviated)
	var name_label := Label.new()
	name_label.name = "NameLabel"
	name_label.text = stat_name
	name_label.custom_minimum_size.x = 50
	name_label.tooltip_text = STAT_FULL_NAMES.get(stat_name, stat_name)
	row.add_child(name_label)

	# Full name (smaller, secondary color)
	var full_name_label := Label.new()
	full_name_label.name = "FullNameLabel"
	full_name_label.text = STAT_FULL_NAMES.get(stat_name, "")
	full_name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(full_name_label)

	# Value
	var value_label := Label.new()
	value_label.name = "ValueLabel"
	value_label.text = "10"
	value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	value_label.custom_minimum_size.x = 30
	row.add_child(value_label)

	# Modifier (in parentheses)
	var modifier_label := Label.new()
	modifier_label.name = "ModifierLabel"
	modifier_label.text = "(+0)"
	modifier_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	modifier_label.custom_minimum_size.x = 40
	row.add_child(modifier_label)

	# Store references
	_stat_rows[stat_name] = {
		"container": row,
		"name_label": name_label,
		"full_name_label": full_name_label,
		"value_label": value_label,
		"modifier_label": modifier_label
	}

	# Make row clickable
	var btn := Button.new()
	btn.name = "ClickArea"
	btn.flat = true
	btn.set_anchors_preset(Control.PRESET_FULL_RECT)
	btn.mouse_filter = Control.MOUSE_FILTER_PASS
	btn.pressed.connect(_on_stat_clicked.bind(stat_name))
	row.add_child(btn)
	btn.move_to_front()  # Actually we want it behind, but transparent

func _build_derived_stats_section(parent: VBoxContainer) -> void:
	var theme := UIThemeManager.get_theme()

	# Separator
	var sep := HSeparator.new()
	parent.add_child(sep)

	# Derived stats label
	var derived_label := Label.new()
	derived_label.text = "Derived Stats"
	derived_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	derived_label.add_theme_color_override("font_color", theme.text_color_secondary)
	parent.add_child(derived_label)

	# Derived stats grid
	var grid := GridContainer.new()
	grid.columns = 2
	grid.add_theme_constant_override("h_separation", theme.spacing_large)
	grid.add_theme_constant_override("v_separation", theme.spacing_small)
	parent.add_child(grid)

	# HP
	_add_derived_stat(grid, "HP", "0/0")
	# AC
	_add_derived_stat(grid, "AC", "10")
	# Initiative
	_add_derived_stat(grid, "Init", "+0")
	# Speed
	_add_derived_stat(grid, "Speed", "30")

func _add_derived_stat(parent: GridContainer, label_text: String, value_text: String) -> void:
	var theme := UIThemeManager.get_theme()

	var label := Label.new()
	label.text = label_text + ":"
	label.add_theme_color_override("font_color", theme.text_color_secondary)
	parent.add_child(label)

	var value := Label.new()
	value.name = "Derived_%s" % label_text
	value.text = value_text
	parent.add_child(value)

func _apply_style() -> void:
	var theme := UIThemeManager.get_theme()

	# Title styling
	_title_label.add_theme_font_size_override("font_size", theme.font_size_subheader)
	_title_label.add_theme_color_override("font_color", theme.text_color)

	# Stat row styling
	for stat_name in _stat_rows:
		var row_data: Dictionary = _stat_rows[stat_name]
		row_data["name_label"].add_theme_font_size_override("font_size", theme.font_size_body)
		row_data["name_label"].add_theme_color_override("font_color", theme.accent_color)

		row_data["full_name_label"].add_theme_font_size_override("font_size", theme.font_size_small)
		row_data["full_name_label"].add_theme_color_override("font_color", theme.text_color_secondary)

		row_data["value_label"].add_theme_font_size_override("font_size", theme.font_size_body)
		row_data["value_label"].add_theme_color_override("font_color", theme.text_color)

		row_data["modifier_label"].add_theme_font_size_override("font_size", theme.font_size_small)

func _update_display() -> void:
	if not _character_data:
		_set_default_values()
		return

	# Get stats from character data
	var stats_obj: Variant = null
	if _character_data is Object:
		if _character_data.has_method("get_stats"):
			stats_obj = _character_data.get_stats()
		elif "stats" in _character_data:
			stats_obj = _character_data.stats

	# Update each stat row
	for stat_name in STAT_NAMES:
		var value: int = 10  # Default
		if stats_obj:
			# Handle Stats object with values dictionary
			if stats_obj is Object and "values" in stats_obj:
				var stat_type: int = _get_stat_type_from_name(stat_name)
				value = stats_obj.values.get(stat_type, 10)
			# Handle plain dictionary
			elif stats_obj is Dictionary:
				value = stats_obj.get(stat_name.to_lower(), stats_obj.get(stat_name, 10))
		_set_stat_value(stat_name, value)

	# Update derived stats
	_update_derived_stats()

func _set_default_values() -> void:
	for stat_name in STAT_NAMES:
		_set_stat_value(stat_name, 10)

func _set_stat_value(stat_name: String, value: int) -> void:
	if not _stat_rows.has(stat_name):
		return

	var row_data: Dictionary = _stat_rows[stat_name]
	row_data["value_label"].text = str(value)

	# Calculate modifier (D&D style: (stat - 10) / 2, rounded down)
	var modifier: int = (value - 10) / 2
	var modifier_text: String
	if modifier >= 0:
		modifier_text = "(+%d)" % modifier
	else:
		modifier_text = "(%d)" % modifier

	row_data["modifier_label"].text = modifier_text

	# Color code modifier
	var theme := UIThemeManager.get_theme()
	if modifier > 0:
		row_data["modifier_label"].add_theme_color_override("font_color", theme.success_color)
	elif modifier < 0:
		row_data["modifier_label"].add_theme_color_override("font_color", theme.error_color)
	else:
		row_data["modifier_label"].add_theme_color_override("font_color", theme.text_color_secondary)

func _update_derived_stats() -> void:
	if not _character_data:
		return

	# Get derived stats if available
	if _character_data is Object:
		if _character_data.has_method("get_max_hp"):
			var hp: int = _character_data.get_max_hp() if _character_data.has_method("get_max_hp") else 0
			var current_hp: int = _character_data.get_current_hp() if _character_data.has_method("get_current_hp") else hp
			_set_derived_value("HP", "%d/%d" % [current_hp, hp])

		if _character_data.has_method("get_armor_class"):
			_set_derived_value("AC", str(_character_data.get_armor_class()))

func _set_derived_value(stat_name: String, value: String) -> void:
	var label := _main_vbox.find_child("Derived_%s" % stat_name, true, false)
	if label and label is Label:
		label.text = value

func _on_stat_clicked(stat_name: String) -> void:
	stat_clicked.emit(stat_name)

func _on_theme_changed(_new_theme: Resource) -> void:
	_apply_style()

# Public API

func set_character(character: Variant) -> void:
	_character_data = character
	_update_display()

func get_character() -> Variant:
	return _character_data

func refresh() -> void:
	_update_display()

func get_stat_value(stat_name: String) -> int:
	if _stat_rows.has(stat_name):
		return int(_stat_rows[stat_name]["value_label"].text)
	return 0

func set_stat_value(stat_name: String, value: int) -> void:
	_set_stat_value(stat_name, value)

func _get_stat_type_from_name(stat_name: String) -> int:
	match stat_name:
		"STR": return StatType.Type.STR
		"DEX": return StatType.Type.DEX
		"CON": return StatType.Type.CON
		"INT": return StatType.Type.INT
		"WIS": return StatType.Type.WIS
		"CHA": return StatType.Type.CHA
		_: return 0
