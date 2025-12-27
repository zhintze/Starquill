extends Node
# UIThemeManager autoload - no class_name to avoid singleton conflict

## UIThemeManager
## Global singleton for managing UI themes
## Provides easy access to theme properties throughout the game

# Preload UITheme to ensure it's available
const UIThemeClass = preload("res://scripts/ui/core/ui_theme.gd")

signal theme_changed(new_theme: UIThemeClass)

# Current active theme
var current_theme: UIThemeClass = null

# Default theme fallback
var default_theme: UIThemeClass = null

# Theme registry for quick switching
var registered_themes: Dictionary = {}  # theme_name -> UIThemeClass

func _ready() -> void:
	# Load default theme
	_load_default_theme()

# Load the default fantasy theme
func _load_default_theme() -> void:
	# Try to load from resource file first
	var theme_path := "res://resources/themes/default_theme.tres"
	if ResourceLoader.exists(theme_path):
		default_theme = ResourceLoader.load(theme_path) as UIThemeClass
		if default_theme:
			current_theme = default_theme
			registered_themes[default_theme.theme_name] = default_theme
			print("UIThemeManager: Loaded default theme from %s" % theme_path)
			return

	# Fallback: Create a programmatic default theme
	default_theme = _create_fallback_theme()
	current_theme = default_theme
	registered_themes[default_theme.theme_name] = default_theme
	print("UIThemeManager: Created fallback default theme")

# Create a fallback theme with hardcoded values
func _create_fallback_theme() -> UIThemeClass:
	var theme := UIThemeClass.new()
	theme.theme_name = "Fantasy Default"
	theme.theme_description = "Default fantasy/medieval theme"

	# Colors already have good defaults in UITheme resource
	# Just return the theme with those defaults
	return theme

# Set the active theme
func set_theme(theme: UIThemeClass) -> void:
	if theme == null:
		push_warning("UIThemeManager: Attempted to set null theme")
		return

	current_theme = theme
	theme_changed.emit(theme)
	print("UIThemeManager: Theme changed to '%s'" % theme.theme_name)

# Set theme by name from registry
func set_theme_by_name(theme_name: String) -> bool:
	if not registered_themes.has(theme_name):
		push_warning("UIThemeManager: Theme '%s' not found in registry" % theme_name)
		return false

	set_theme(registered_themes[theme_name])
	return true

# Register a theme for quick access
func register_theme(theme: UIThemeClass) -> void:
	if theme == null:
		push_warning("UIThemeManager: Cannot register null theme")
		return

	registered_themes[theme.theme_name] = theme
	print("UIThemeManager: Registered theme '%s'" % theme.theme_name)

# Unregister a theme
func unregister_theme(theme_name: String) -> void:
	if registered_themes.has(theme_name):
		registered_themes.erase(theme_name)
		print("UIThemeManager: Unregistered theme '%s'" % theme_name)

# Get current theme (never returns null)
func get_theme() -> UIThemeClass:
	if current_theme == null:
		return default_theme
	return current_theme

# Quick access helper methods for common theme properties
func get_primary_color() -> Color:
	return get_theme().primary_color

func get_secondary_color() -> Color:
	return get_theme().secondary_color

func get_accent_color() -> Color:
	return get_theme().accent_color

func get_text_color() -> Color:
	return get_theme().text_color

func get_panel_color() -> Color:
	return get_theme().panel_color

func get_background_color() -> Color:
	return get_theme().background_color

# Font helpers
func get_header_font() -> Font:
	var theme := get_theme()
	return theme.get_font_or_default(theme.font_header, theme.font_size_header)

func get_body_font() -> Font:
	var theme := get_theme()
	return theme.get_font_or_default(theme.font_body, theme.font_size_body)

func get_button_font() -> Font:
	var theme := get_theme()
	return theme.get_font_or_default(theme.font_button, theme.font_size_button)

# Size helpers
func get_font_size_header() -> int:
	return get_theme().font_size_header

func get_font_size_body() -> int:
	return get_theme().font_size_body

func get_font_size_button() -> int:
	return get_theme().font_size_button

# Spacing helpers
func get_spacing(size: String = "medium") -> int:
	var theme := get_theme()
	match size:
		"tiny":
			return theme.spacing_tiny
		"small":
			return theme.spacing_small
		"medium":
			return theme.spacing_medium
		"large":
			return theme.spacing_large
		"huge":
			return theme.spacing_huge
		_:
			return theme.spacing_medium

# StyleBox factory methods
func create_panel_stylebox() -> StyleBoxFlat:
	return get_theme().create_panel_stylebox()

func create_button_stylebox(state: String = "normal") -> StyleBoxFlat:
	return get_theme().create_button_stylebox(state)

func create_slot_stylebox(is_filled: bool = false, is_highlighted: bool = false) -> StyleBoxFlat:
	return get_theme().create_slot_stylebox(is_filled, is_highlighted)

func create_drag_drop_slot_stylebox(state: String = "empty") -> StyleBoxFlat:
	return get_theme().create_drag_drop_slot_stylebox(state)

func create_tab_stylebox(state: String = "inactive") -> StyleBoxFlat:
	return get_theme().create_tab_stylebox(state)

func create_overlay_stylebox() -> StyleBoxFlat:
	return get_theme().create_overlay_stylebox()

func create_bg_stylebox(bg_type: String = "primary") -> StyleBoxFlat:
	return get_theme().create_bg_stylebox(bg_type)

# Background color helpers
func get_bg_primary() -> Color:
	return get_theme().bg_primary

func get_bg_secondary() -> Color:
	return get_theme().bg_secondary

func get_bg_overlay() -> Color:
	return get_theme().bg_overlay

# Border color helpers
func get_border_panel() -> Color:
	return get_theme().border_panel

func get_border_slot() -> Color:
	return get_theme().border_slot

func get_border_highlight() -> Color:
	return get_theme().border_highlight

# Button color helpers
func get_btn_normal() -> Color:
	return get_theme().btn_normal

func get_btn_hover() -> Color:
	return get_theme().btn_hover

func get_btn_pressed() -> Color:
	return get_theme().btn_pressed

func get_btn_text() -> Color:
	return get_theme().btn_text

# Slot color helpers
func get_slot_empty_color() -> Color:
	return get_theme().slot_empty_color

func get_slot_filled_color() -> Color:
	return get_theme().slot_filled_color

func get_slot_valid_drop_color() -> Color:
	return get_theme().slot_valid_drop_color

func get_slot_invalid_drop_color() -> Color:
	return get_theme().slot_invalid_drop_color

# Tab color helpers
func get_tab_active() -> Color:
	return get_theme().tab_active

func get_tab_inactive() -> Color:
	return get_theme().tab_inactive

# Animation duration helpers
func get_transition_duration() -> float:
	return get_theme().transition_duration

func get_hover_duration() -> float:
	return get_theme().hover_duration

func get_fade_duration() -> float:
	return get_theme().fade_duration

# Utility: Apply theme to a Control node
func apply_theme_to_control(control: Control) -> void:
	if control == null:
		return

	var theme := get_theme()

	# Apply to Panel
	if control is Panel:
		control.add_theme_stylebox_override("panel", create_panel_stylebox())

	# Apply to Button
	if control is Button:
		control.add_theme_stylebox_override("normal", create_button_stylebox("normal"))
		control.add_theme_stylebox_override("hover", create_button_stylebox("hover"))
		control.add_theme_stylebox_override("pressed", create_button_stylebox("pressed"))
		control.add_theme_stylebox_override("disabled", create_button_stylebox("disabled"))
		control.add_theme_color_override("font_color", theme.text_color)
		control.add_theme_font_override("font", get_button_font())
		control.add_theme_font_size_override("font_size", theme.font_size_button)

	# Apply to Label
	if control is Label:
		control.add_theme_color_override("font_color", theme.text_color)
		control.add_theme_font_override("font", get_body_font())
		control.add_theme_font_size_override("font_size", theme.font_size_body)

# List available themes
func get_available_themes() -> Array[String]:
	var names: Array[String] = []
	for key in registered_themes.keys():
		names.append(key)
	return names
