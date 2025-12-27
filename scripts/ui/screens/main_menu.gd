class_name MainMenu
extends Control

## MainMenu
## The game's main menu screen with New Game, Continue, Settings, and Quit buttons
## Uses the modular UI component system

signal new_game_requested
signal continue_requested
signal settings_requested
signal quit_requested

# UI References
var _background: ColorRect
var _panel: NinePatchRect
var _title_label: Label
var _button_container: VBoxContainer
var _new_game_btn: ThemedButton
var _continue_btn: ThemedButton
var _settings_btn: ThemedButton
var _quit_btn: ThemedButton

# State
var _has_save_data: bool = false

func _ready() -> void:
	_build_ui()
	_connect_signals()
	_check_save_data()
	_apply_safe_area()

	# Listen for theme changes
	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

func _build_ui() -> void:
	# Full screen layout
	set_anchors_preset(Control.PRESET_FULL_RECT)

	# Background overlay with paper shader
	_background = ColorRect.new()
	_background.name = "Background"
	_background.set_anchors_preset(Control.PRESET_FULL_RECT)
	_background.color = UIThemeManager.get_bg_overlay()
	add_child(_background)

	# Center container for the menu panel
	var center := CenterContainer.new()
	center.name = "CenterContainer"
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(center)

	# Main panel with paper styling
	_panel = BasePanel.create(BasePanel.PanelStyle.PRIMARY, true)
	_panel.name = "MenuPanel"
	_panel.custom_minimum_size = Vector2(300, 400)
	center.add_child(_panel)

	# Margin container for padding
	var margin := MarginContainer.new()
	margin.name = "MarginContainer"
	margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	var theme := UIThemeManager.get_theme()
	margin.add_theme_constant_override("margin_left", theme.padding_panel)
	margin.add_theme_constant_override("margin_right", theme.padding_panel)
	margin.add_theme_constant_override("margin_top", theme.padding_panel)
	margin.add_theme_constant_override("margin_bottom", theme.padding_panel)
	_panel.add_child(margin)

	# Main vertical layout
	var vbox := VBoxContainer.new()
	vbox.name = "MainVBox"
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	vbox.add_theme_constant_override("separation", theme.spacing_large)
	margin.add_child(vbox)

	# Title
	_title_label = Label.new()
	_title_label.name = "TitleLabel"
	_title_label.text = "Starquill"
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_font_size_override("font_size", theme.font_size_header + 8)
	_title_label.add_theme_color_override("font_color", theme.accent_color)
	vbox.add_child(_title_label)

	# Spacer
	var spacer := Control.new()
	spacer.custom_minimum_size.y = theme.spacing_large
	vbox.add_child(spacer)

	# Button container
	_button_container = VBoxContainer.new()
	_button_container.name = "ButtonContainer"
	_button_container.alignment = BoxContainer.ALIGNMENT_CENTER
	_button_container.add_theme_constant_override("separation", theme.spacing_medium)
	vbox.add_child(_button_container)

	# Create menu buttons
	_new_game_btn = _create_menu_button("New Game")
	_continue_btn = _create_menu_button("Continue")
	_settings_btn = _create_menu_button("Settings")
	_quit_btn = _create_menu_button("Quit", ThemedButton.ButtonStyle.SECONDARY)

	_button_container.add_child(_new_game_btn)
	_button_container.add_child(_continue_btn)
	_button_container.add_child(_settings_btn)

	# Add extra spacing before quit
	var quit_spacer := Control.new()
	quit_spacer.custom_minimum_size.y = theme.spacing_small
	_button_container.add_child(quit_spacer)

	_button_container.add_child(_quit_btn)

func _create_menu_button(label: String, style: ThemedButton.ButtonStyle = ThemedButton.ButtonStyle.PRIMARY) -> ThemedButton:
	var btn := ThemedButton.new()
	btn.text = label
	btn.button_style = style
	btn.custom_minimum_size = Vector2(200, 48)
	btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	return btn

func _connect_signals() -> void:
	_new_game_btn.pressed.connect(_on_new_game_pressed)
	_continue_btn.pressed.connect(_on_continue_pressed)
	_settings_btn.pressed.connect(_on_settings_pressed)
	_quit_btn.pressed.connect(_on_quit_pressed)

func _check_save_data() -> void:
	# Check if there's save data to continue from
	# For now, just check if a save file exists
	var save_path := "user://savegame.save"
	_has_save_data = FileAccess.file_exists(save_path)

	# Disable continue button if no save data
	_continue_btn.disabled = not _has_save_data
	if not _has_save_data:
		_continue_btn.tooltip_text = "No saved game found"

func _apply_safe_area() -> void:
	# Apply safe area margins for mobile devices
	UIScaler.refresh()
	# The center container handles positioning, but we could add margins here if needed

func _on_new_game_pressed() -> void:
	new_game_requested.emit()

	# Default behavior: start new game via Game autoload
	if has_node("/root/Game"):
		var game = get_node("/root/Game")
		if game.has_method("start_new_game"):
			game.start_new_game()

func _on_continue_pressed() -> void:
	continue_requested.emit()

	# Default behavior: load game via save system
	if has_node("/root/Game"):
		var game = get_node("/root/Game")
		if game.has_method("load_game"):
			game.load_game()

func _on_settings_pressed() -> void:
	settings_requested.emit()

	# Open settings menu via UIManager
	if UIManager:
		UIManager.open_settings_menu()

func _on_quit_pressed() -> void:
	quit_requested.emit()

	# Quit the application
	get_tree().quit()

func _on_theme_changed(_new_theme: Resource) -> void:
	# Refresh styling
	var theme := UIThemeManager.get_theme()
	_background.color = theme.bg_overlay
	_title_label.add_theme_font_size_override("font_size", theme.font_size_header + 8)
	_title_label.add_theme_color_override("font_color", theme.accent_color)

# Refresh save data check
func refresh_save_state() -> void:
	_check_save_data()

# Focus the first button when menu opens
func grab_initial_focus() -> void:
	if _has_save_data:
		_continue_btn.grab_focus()
	else:
		_new_game_btn.grab_focus()

func _input(event: InputEvent) -> void:
	# Handle ESC to quit (with confirmation in real implementation)
	if event.is_action_pressed("ui_cancel"):
		_on_quit_pressed()
