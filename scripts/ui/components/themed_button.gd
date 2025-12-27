class_name ThemedButton
extends Button

## ThemedButton
## A button with programmatic styling from UITheme
## Features hover scale animation and press feedback

signal button_ready

# Button style variants
enum ButtonStyle {
	PRIMARY,    # Main action buttons
	SECONDARY,  # Secondary actions
	GHOST,      # Minimal styling (text only with hover)
	DANGER      # Destructive actions (uses error color)
}

@export var button_style: ButtonStyle = ButtonStyle.PRIMARY:
	set(value):
		button_style = value
		_apply_style()

@export var enable_hover_scale: bool = true
@export var hover_scale_amount: float = 1.05
@export var enable_press_feedback: bool = true

# Audio (optional)
@export var click_sound: AudioStream
@export var hover_sound: AudioStream

# Internal state
var _original_scale: Vector2 = Vector2.ONE
var _is_hovered: bool = false
var _tween: Tween

func _ready() -> void:
	_original_scale = scale
	_apply_style()
	_connect_signals()

	# Ensure minimum touch target size
	UIScaler.ensure_touch_size(self)

	button_ready.emit()

func _connect_signals() -> void:
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)
	button_down.connect(_on_button_down)
	button_up.connect(_on_button_up)
	pressed.connect(_on_pressed)

	# Listen for theme changes
	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

func _apply_style() -> void:
	var theme := UIThemeManager.get_theme()

	# Apply StyleBoxes based on button style
	match button_style:
		ButtonStyle.PRIMARY:
			_apply_primary_style(theme)
		ButtonStyle.SECONDARY:
			_apply_secondary_style(theme)
		ButtonStyle.GHOST:
			_apply_ghost_style(theme)
		ButtonStyle.DANGER:
			_apply_danger_style(theme)

	# Common text styling
	add_theme_font_size_override("font_size", theme.font_size_button)
	if theme.font_button:
		add_theme_font_override("font", theme.font_button)

func _apply_primary_style(theme: UITheme) -> void:
	add_theme_stylebox_override("normal", theme.create_button_stylebox("normal"))
	add_theme_stylebox_override("hover", theme.create_button_stylebox("hover"))
	add_theme_stylebox_override("pressed", theme.create_button_stylebox("pressed"))
	add_theme_stylebox_override("disabled", theme.create_button_stylebox("disabled"))

	add_theme_color_override("font_color", theme.btn_text)
	add_theme_color_override("font_hover_color", theme.btn_text)
	add_theme_color_override("font_pressed_color", theme.btn_text)
	add_theme_color_override("font_disabled_color", theme.btn_text_disabled)

func _apply_secondary_style(theme: UITheme) -> void:
	# Secondary uses lighter colors
	var normal := StyleBoxFlat.new()
	normal.bg_color = theme.bg_secondary
	normal.border_color = theme.border_panel
	_setup_button_stylebox(normal, theme)

	var hover := StyleBoxFlat.new()
	hover.bg_color = theme.bg_secondary.lightened(0.1)
	hover.border_color = theme.border_highlight
	_setup_button_stylebox(hover, theme)

	var pressed := StyleBoxFlat.new()
	pressed.bg_color = theme.bg_secondary.darkened(0.1)
	pressed.border_color = theme.border_panel
	_setup_button_stylebox(pressed, theme)

	var disabled := StyleBoxFlat.new()
	disabled.bg_color = theme.bg_secondary.darkened(0.2)
	disabled.bg_color.a = 0.5
	disabled.border_color = theme.border_panel.darkened(0.2)
	_setup_button_stylebox(disabled, theme)

	add_theme_stylebox_override("normal", normal)
	add_theme_stylebox_override("hover", hover)
	add_theme_stylebox_override("pressed", pressed)
	add_theme_stylebox_override("disabled", disabled)

	add_theme_color_override("font_color", theme.text_color)
	add_theme_color_override("font_hover_color", theme.text_color)
	add_theme_color_override("font_pressed_color", theme.text_color)
	add_theme_color_override("font_disabled_color", theme.text_color_disabled)

func _apply_ghost_style(theme: UITheme) -> void:
	# Ghost buttons have transparent background until hovered
	var normal := StyleBoxFlat.new()
	normal.bg_color = Color.TRANSPARENT
	normal.border_color = Color.TRANSPARENT
	_setup_button_stylebox(normal, theme)

	var hover := StyleBoxFlat.new()
	hover.bg_color = theme.btn_hover
	hover.bg_color.a = 0.3
	hover.border_color = theme.border_panel
	_setup_button_stylebox(hover, theme)

	var pressed := StyleBoxFlat.new()
	pressed.bg_color = theme.btn_pressed
	pressed.bg_color.a = 0.5
	pressed.border_color = theme.border_panel
	_setup_button_stylebox(pressed, theme)

	var disabled := StyleBoxFlat.new()
	disabled.bg_color = Color.TRANSPARENT
	disabled.border_color = Color.TRANSPARENT
	_setup_button_stylebox(disabled, theme)

	add_theme_stylebox_override("normal", normal)
	add_theme_stylebox_override("hover", hover)
	add_theme_stylebox_override("pressed", pressed)
	add_theme_stylebox_override("disabled", disabled)

	add_theme_color_override("font_color", theme.text_color)
	add_theme_color_override("font_hover_color", theme.accent_color)
	add_theme_color_override("font_pressed_color", theme.text_color)
	add_theme_color_override("font_disabled_color", theme.text_color_disabled)

func _apply_danger_style(theme: UITheme) -> void:
	# Danger buttons use error color scheme
	var normal := StyleBoxFlat.new()
	normal.bg_color = theme.error_color.darkened(0.2)
	normal.border_color = theme.error_color.darkened(0.3)
	_setup_button_stylebox(normal, theme)

	var hover := StyleBoxFlat.new()
	hover.bg_color = theme.error_color
	hover.border_color = theme.error_color.lightened(0.1)
	_setup_button_stylebox(hover, theme)

	var pressed := StyleBoxFlat.new()
	pressed.bg_color = theme.error_color.darkened(0.3)
	pressed.border_color = theme.error_color.darkened(0.4)
	_setup_button_stylebox(pressed, theme)

	var disabled := StyleBoxFlat.new()
	disabled.bg_color = theme.error_color.darkened(0.4)
	disabled.bg_color.a = 0.5
	disabled.border_color = theme.error_color.darkened(0.5)
	_setup_button_stylebox(disabled, theme)

	add_theme_stylebox_override("normal", normal)
	add_theme_stylebox_override("hover", hover)
	add_theme_stylebox_override("pressed", pressed)
	add_theme_stylebox_override("disabled", disabled)

	# White text on dark red background
	var text_color := Color.WHITE
	add_theme_color_override("font_color", text_color)
	add_theme_color_override("font_hover_color", text_color)
	add_theme_color_override("font_pressed_color", text_color.darkened(0.1))
	add_theme_color_override("font_disabled_color", text_color.darkened(0.4))

func _setup_button_stylebox(stylebox: StyleBoxFlat, theme: UITheme) -> void:
	stylebox.border_width_left = theme.button_border_width
	stylebox.border_width_right = theme.button_border_width
	stylebox.border_width_top = theme.button_border_width
	stylebox.border_width_bottom = theme.button_border_width
	stylebox.corner_radius_top_left = theme.button_corner_radius
	stylebox.corner_radius_top_right = theme.button_corner_radius
	stylebox.corner_radius_bottom_left = theme.button_corner_radius
	stylebox.corner_radius_bottom_right = theme.button_corner_radius
	stylebox.content_margin_left = theme.button_padding_horizontal
	stylebox.content_margin_right = theme.button_padding_horizontal
	stylebox.content_margin_top = theme.button_padding_vertical
	stylebox.content_margin_bottom = theme.button_padding_vertical

func _on_mouse_entered() -> void:
	_is_hovered = true
	if enable_hover_scale and not disabled:
		_animate_scale(hover_scale_amount)
	if hover_sound and not disabled:
		_play_sound(hover_sound)

func _on_mouse_exited() -> void:
	_is_hovered = false
	if enable_hover_scale:
		_animate_scale(1.0)

func _on_button_down() -> void:
	if enable_press_feedback:
		# Quick scale down for tactile feedback
		_animate_scale(0.95, 0.05)

func _on_button_up() -> void:
	if enable_press_feedback:
		# Return to hover or normal scale
		var target_scale := hover_scale_amount if _is_hovered else 1.0
		_animate_scale(target_scale, 0.1)

func _on_pressed() -> void:
	if click_sound:
		_play_sound(click_sound)

func _animate_scale(target_scale: float, duration: float = -1.0) -> void:
	if _tween:
		_tween.kill()

	if duration < 0:
		duration = UIThemeManager.get_hover_duration()

	_tween = create_tween()
	_tween.set_ease(Tween.EASE_OUT)
	_tween.set_trans(Tween.TRANS_QUAD)
	_tween.tween_property(self, "scale", _original_scale * target_scale, duration)

func _play_sound(sound: AudioStream) -> void:
	# Simple audio playback - assumes BusAudio or similar exists
	# For now, just create a temporary AudioStreamPlayer
	var player := AudioStreamPlayer.new()
	player.stream = sound
	player.bus = "UI"  # Assumes UI audio bus exists
	add_child(player)
	player.play()
	player.finished.connect(player.queue_free)

func _on_theme_changed(_new_theme: Resource) -> void:
	_apply_style()

# Refresh styling manually
func refresh_style() -> void:
	_apply_style()

# Create a ThemedButton programmatically
static func create(label: String, style: ButtonStyle = ButtonStyle.PRIMARY) -> ThemedButton:
	var btn := ThemedButton.new()
	btn.text = label
	btn.button_style = style
	return btn
