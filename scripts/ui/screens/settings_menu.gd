class_name SettingsMenu
extends Control

## SettingsMenu
## Settings screen with Audio, Video, and Controls tabs
## Uses BookmarkTabBar for navigation

signal settings_changed(setting: String, value: Variant)
signal menu_closed

# Tab indices
enum SettingsTab {
	AUDIO = 0,
	VIDEO = 1,
	CONTROLS = 2
}

# Settings storage
var _settings: Dictionary = {
	"master_volume": 1.0,
	"music_volume": 0.8,
	"sfx_volume": 1.0,
	"fullscreen": false,
	"vsync": true,
	"screen_shake": true
}

# UI References
var _background: ColorRect
var _main_panel: NinePatchRect
var _tab_bar: BookmarkTabBar
var _content_stack: Control
var _audio_panel: VBoxContainer
var _video_panel: VBoxContainer
var _controls_panel: VBoxContainer
var _close_button: IconButton
var _apply_button: ThemedButton

# Slider references for easy access
var _master_slider: HSlider
var _music_slider: HSlider
var _sfx_slider: HSlider
var _fullscreen_toggle: CheckButton
var _vsync_toggle: CheckButton
var _shake_toggle: CheckButton

func _ready() -> void:
	_load_settings()
	_build_ui()
	_apply_settings_to_ui()

	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

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
	_main_panel.name = "SettingsPanel"
	_main_panel.custom_minimum_size = Vector2(UIConstants.MENU_PANEL_WIDTH, UIConstants.MENU_PANEL_HEIGHT_SMALL)
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

	# Tab bar
	_tab_bar = BookmarkTabBar.new()
	_tab_bar.name = "TabBar"
	_tab_bar.tabs = ["Audio", "Video", "Controls"]
	_tab_bar.active_tab = SettingsTab.AUDIO
	_tab_bar.tab_changed.connect(_on_tab_changed)
	main_vbox.add_child(_tab_bar)

	# Content stack
	_content_stack = Control.new()
	_content_stack.name = "ContentStack"
	_content_stack.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_content_stack.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_content_stack.custom_minimum_size.y = 250
	main_vbox.add_child(_content_stack)

	# Build tab content
	_build_audio_tab()
	_build_video_tab()
	_build_controls_tab()

	# Footer with Apply/Close buttons
	_build_footer(main_vbox)

	# Show initial tab
	_show_tab(SettingsTab.AUDIO)

func _build_header(parent: VBoxContainer) -> void:
	var theme := UIThemeManager.get_theme()

	var header := HBoxContainer.new()
	header.name = "Header"
	parent.add_child(header)

	var title := Label.new()
	title.text = "Settings"
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	title.add_theme_font_size_override("font_size", theme.font_size_header)
	title.add_theme_color_override("font_color", theme.accent_color)
	header.add_child(title)

	_close_button = IconButton.new()
	_close_button.preset_icon = IconButton.PresetIcon.CLOSE
	_close_button.icon_position = IconButton.IconPosition.ONLY
	_close_button.button_style = ThemedButton.ButtonStyle.GHOST
	_close_button.custom_minimum_size = Vector2(UIConstants.ICON_BUTTON_SIZE, UIConstants.ICON_BUTTON_SIZE)
	_close_button.pressed.connect(_on_close_pressed)
	header.add_child(_close_button)

func _build_audio_tab() -> void:
	var theme := UIThemeManager.get_theme()

	_audio_panel = VBoxContainer.new()
	_audio_panel.name = "AudioTab"
	_audio_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	_audio_panel.add_theme_constant_override("separation", theme.spacing_medium)
	_content_stack.add_child(_audio_panel)

	# Master Volume
	_master_slider = _create_slider_row(_audio_panel, "Master Volume", 0.0, 1.0, _settings["master_volume"])
	_master_slider.value_changed.connect(_on_master_volume_changed)

	# Music Volume
	_music_slider = _create_slider_row(_audio_panel, "Music Volume", 0.0, 1.0, _settings["music_volume"])
	_music_slider.value_changed.connect(_on_music_volume_changed)

	# SFX Volume
	_sfx_slider = _create_slider_row(_audio_panel, "SFX Volume", 0.0, 1.0, _settings["sfx_volume"])
	_sfx_slider.value_changed.connect(_on_sfx_volume_changed)

func _build_video_tab() -> void:
	var theme := UIThemeManager.get_theme()

	_video_panel = VBoxContainer.new()
	_video_panel.name = "VideoTab"
	_video_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	_video_panel.add_theme_constant_override("separation", theme.spacing_medium)
	_video_panel.visible = false
	_content_stack.add_child(_video_panel)

	# Fullscreen toggle
	_fullscreen_toggle = _create_toggle_row(_video_panel, "Fullscreen", _settings["fullscreen"])
	_fullscreen_toggle.toggled.connect(_on_fullscreen_toggled)

	# VSync toggle
	_vsync_toggle = _create_toggle_row(_video_panel, "VSync", _settings["vsync"])
	_vsync_toggle.toggled.connect(_on_vsync_toggled)

	# Screen shake toggle
	_shake_toggle = _create_toggle_row(_video_panel, "Screen Shake", _settings["screen_shake"])
	_shake_toggle.toggled.connect(_on_shake_toggled)

func _build_controls_tab() -> void:
	var theme := UIThemeManager.get_theme()

	_controls_panel = VBoxContainer.new()
	_controls_panel.name = "ControlsTab"
	_controls_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	_controls_panel.add_theme_constant_override("separation", theme.spacing_medium)
	_controls_panel.visible = false
	_content_stack.add_child(_controls_panel)

	# Placeholder for key rebinding
	var placeholder := Label.new()
	placeholder.text = "Control rebinding coming soon..."
	placeholder.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	placeholder.add_theme_color_override("font_color", theme.text_color_secondary)
	_controls_panel.add_child(placeholder)

	# Add spacer
	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_controls_panel.add_child(spacer)

	# Reset to defaults button
	var reset_btn := ThemedButton.new()
	reset_btn.text = "Reset to Defaults"
	reset_btn.button_style = ThemedButton.ButtonStyle.SECONDARY
	reset_btn.pressed.connect(_on_reset_defaults)
	_controls_panel.add_child(reset_btn)

func _build_footer(parent: VBoxContainer) -> void:
	var theme := UIThemeManager.get_theme()

	var sep := HSeparator.new()
	parent.add_child(sep)

	var footer := HBoxContainer.new()
	footer.name = "Footer"
	footer.alignment = BoxContainer.ALIGNMENT_END
	footer.add_theme_constant_override("separation", theme.spacing_medium)
	parent.add_child(footer)

	_apply_button = ThemedButton.new()
	_apply_button.text = "Apply"
	_apply_button.button_style = ThemedButton.ButtonStyle.PRIMARY
	_apply_button.custom_minimum_size.x = 100
	_apply_button.pressed.connect(_on_apply_pressed)
	footer.add_child(_apply_button)

func _create_slider_row(parent: VBoxContainer, label_text: String, min_val: float, max_val: float, initial: float) -> HSlider:
	var theme := UIThemeManager.get_theme()

	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", theme.spacing_medium)
	parent.add_child(row)

	var label := Label.new()
	label.text = label_text
	label.custom_minimum_size.x = 120
	label.add_theme_color_override("font_color", theme.text_color)
	row.add_child(label)

	var slider := HSlider.new()
	slider.min_value = min_val
	slider.max_value = max_val
	slider.step = 0.05
	slider.value = initial
	slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slider.custom_minimum_size = Vector2(UIConstants.SLIDER_MIN_WIDTH, UIConstants.SLIDER_HEIGHT)
	row.add_child(slider)

	var value_label := Label.new()
	value_label.name = "ValueLabel"
	value_label.text = "%d%%" % int(initial * 100)
	value_label.custom_minimum_size.x = 50
	value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	value_label.add_theme_color_override("font_color", theme.text_color_secondary)
	row.add_child(value_label)

	# Update value label when slider changes
	slider.value_changed.connect(func(val): value_label.text = "%d%%" % int(val * 100))

	return slider

func _create_toggle_row(parent: VBoxContainer, label_text: String, initial: bool) -> CheckButton:
	var theme := UIThemeManager.get_theme()

	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", theme.spacing_medium)
	parent.add_child(row)

	var label := Label.new()
	label.text = label_text
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label.add_theme_color_override("font_color", theme.text_color)
	row.add_child(label)

	var toggle := CheckButton.new()
	toggle.button_pressed = initial
	row.add_child(toggle)

	return toggle

func _show_tab(tab_index: int) -> void:
	_audio_panel.visible = (tab_index == SettingsTab.AUDIO)
	_video_panel.visible = (tab_index == SettingsTab.VIDEO)
	_controls_panel.visible = (tab_index == SettingsTab.CONTROLS)

func _on_tab_changed(index: int) -> void:
	_show_tab(index)

# Audio callbacks
func _on_master_volume_changed(value: float) -> void:
	_settings["master_volume"] = value
	settings_changed.emit("master_volume", value)

func _on_music_volume_changed(value: float) -> void:
	_settings["music_volume"] = value
	settings_changed.emit("music_volume", value)

func _on_sfx_volume_changed(value: float) -> void:
	_settings["sfx_volume"] = value
	settings_changed.emit("sfx_volume", value)

# Video callbacks
func _on_fullscreen_toggled(pressed: bool) -> void:
	_settings["fullscreen"] = pressed
	settings_changed.emit("fullscreen", pressed)

func _on_vsync_toggled(pressed: bool) -> void:
	_settings["vsync"] = pressed
	settings_changed.emit("vsync", pressed)

func _on_shake_toggled(pressed: bool) -> void:
	_settings["screen_shake"] = pressed
	settings_changed.emit("screen_shake", pressed)

func _on_reset_defaults() -> void:
	_settings = {
		"master_volume": 1.0,
		"music_volume": 0.8,
		"sfx_volume": 1.0,
		"fullscreen": false,
		"vsync": true,
		"screen_shake": true
	}
	_apply_settings_to_ui()

func _on_apply_pressed() -> void:
	_save_settings()
	_apply_settings_to_game()

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

# Settings persistence
func _load_settings() -> void:
	var config := ConfigFile.new()
	var path := "user://settings.cfg"

	if config.load(path) == OK:
		for key in _settings.keys():
			if config.has_section_key("settings", key):
				_settings[key] = config.get_value("settings", key)

func _save_settings() -> void:
	var config := ConfigFile.new()

	for key in _settings.keys():
		config.set_value("settings", key, _settings[key])

	config.save("user://settings.cfg")

func _apply_settings_to_ui() -> void:
	if _master_slider:
		_master_slider.value = _settings["master_volume"]
	if _music_slider:
		_music_slider.value = _settings["music_volume"]
	if _sfx_slider:
		_sfx_slider.value = _settings["sfx_volume"]
	if _fullscreen_toggle:
		_fullscreen_toggle.button_pressed = _settings["fullscreen"]
	if _vsync_toggle:
		_vsync_toggle.button_pressed = _settings["vsync"]
	if _shake_toggle:
		_shake_toggle.button_pressed = _settings["screen_shake"]

func _apply_settings_to_game() -> void:
	# Apply audio settings
	if AudioServer.get_bus_index("Master") >= 0:
		AudioServer.set_bus_volume_db(
			AudioServer.get_bus_index("Master"),
			linear_to_db(_settings["master_volume"])
		)
	if AudioServer.get_bus_index("Music") >= 0:
		AudioServer.set_bus_volume_db(
			AudioServer.get_bus_index("Music"),
			linear_to_db(_settings["music_volume"])
		)
	if AudioServer.get_bus_index("SFX") >= 0:
		AudioServer.set_bus_volume_db(
			AudioServer.get_bus_index("SFX"),
			linear_to_db(_settings["sfx_volume"])
		)

	# Apply video settings
	if _settings["fullscreen"]:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
	else:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)

	DisplayServer.window_set_vsync_mode(
		DisplayServer.VSYNC_ENABLED if _settings["vsync"] else DisplayServer.VSYNC_DISABLED
	)

# Public API
func get_setting(key: String) -> Variant:
	return _settings.get(key)

func set_setting(key: String, value: Variant) -> void:
	_settings[key] = value
	_apply_settings_to_ui()
