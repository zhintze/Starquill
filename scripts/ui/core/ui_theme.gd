extends Resource
class_name UITheme

## UITheme Resource
## Defines visual styling for all UI elements in Starquill
## Can be saved as .tres files and swapped at runtime for different visual themes

# Theme identification
@export var theme_name: String = "Default"
@export var theme_description: String = ""

# Color Palette
@export_group("Colors")
@export var primary_color: Color = Color(0.8, 0.7, 0.5)  # Parchment/tan
@export var secondary_color: Color = Color(0.4, 0.3, 0.2)  # Dark brown
@export var accent_color: Color = Color(0.2, 0.2, 0.3)
@export var background_color: Color = Color(0.2, 0.15, 0.1, 0.9)  # Dark translucent
@export var panel_color: Color = Color(0.85, 0.75, 0.6)  # Light parchment

@export_subgroup("Text Colors")
@export var text_color: Color = Color(0.1, 0.08, 0.05)  # Dark brown text
@export var text_color_secondary: Color = Color(0.4, 0.35, 0.25)  # Medium brown
@export var text_color_disabled: Color = Color(0.5, 0.5, 0.5)  # Gray
@export var text_color_highlight: Color = Color(0.2, 0.2, 0.3) 

@export_subgroup("State Colors")
@export var hover_color: Color = Color(1.0, 0.9, 0.7)  # Light hover
@export var pressed_color: Color = Color(0.7, 0.6, 0.4)  # Darker when pressed
@export var selected_color: Color = Color(0.9, 0.8, 0.5)  # Selected highlight
@export var disabled_color: Color = Color(0.5, 0.5, 0.5, 0.5)  # Grayed out

@export_subgroup("Feedback Colors")
@export var success_color: Color = Color(0.3, 0.7, 0.3)  # Green
@export var warning_color: Color = Color(0.9, 0.7, 0.2)  # Yellow/gold
@export var error_color: Color = Color(0.8, 0.2, 0.2)  # Red
@export var info_color: Color = Color(0.3, 0.5, 0.8)  # Blue

# Fonts
@export_group("Fonts")
@export var font_header: Font = null  # Large headers
@export var font_body: Font = null  # Regular text
@export var font_button: Font = null  # Button text
@export var font_small: Font = null  # Small labels

@export_subgroup("Font Sizes")
@export var font_size_header: int = 64
@export var font_size_subheader: int = 36
@export var font_size_body: int = 32
@export var font_size_button: int = 32
@export var font_size_small: int = 24

# Textures and Styles
@export_group("Textures")
@export var panel_texture: Texture2D = null  # Background texture for panels
@export var button_texture: Texture2D = null  # Button background texture
@export var border_texture: Texture2D = null  # Ornate border texture
@export var slot_texture: Texture2D = null  # Equipment slot background
@export var icon_frame_texture: Texture2D = null  # Frame around icons

# Panel Styling (doubled for 1280x720 reference)
@export_group("Panel Style")
@export var panel_border_width: int = 8
@export var panel_corner_radius: int = 16
@export var panel_shadow_size: int = 8
@export var panel_shadow_color: Color = Color(0, 0, 0, 0.5)

# Button Styling (doubled for 1280x720 reference)
@export_group("Button Style")
@export var button_border_width: int = 4
@export var button_corner_radius: int = 12
@export var button_padding_horizontal: int = 32
@export var button_padding_vertical: int = 16
@export var button_hover_scale: float = 1.05  # Scale on hover

# Equipment Slot Styling (doubled for 1280x720 reference)
@export_group("Equipment Slot Style")
@export var slot_size: Vector2i = Vector2i(128, 128)
@export var slot_border_width: int = 4
@export var slot_border_color: Color = Color(0.4, 0.3, 0.2)
@export var slot_border_color_highlight: Color = Color(0.9, 0.7, 0.3)
@export var slot_empty_color: Color = Color(0.3, 0.25, 0.2, 0.5)
@export var slot_filled_color: Color = Color(0.5, 0.4, 0.3, 0.7)
@export var slot_valid_drop_color: Color = Color(0.3, 0.6, 0.3, 0.7)
@export var slot_invalid_drop_color: Color = Color(0.6, 0.3, 0.3, 0.7)

# Semantic Background Colors (for easy theme swapping)
@export_group("Background Colors")
@export var bg_primary: Color = Color(0.85, 0.75, 0.6)  # Main panel backgrounds
@export var bg_secondary: Color = Color(0.75, 0.65, 0.5)  # Inner containers
@export var bg_overlay: Color = Color(0.1, 0.08, 0.05, 0.85)  # Modal overlays

# Semantic Border Colors
@export_group("Border Colors")
@export var border_panel: Color = Color(0.4, 0.3, 0.2)  # Panel edges
@export var border_slot: Color = Color(0.35, 0.28, 0.18)  # Slot edges
@export var border_highlight: Color = Color(0.9, 0.7, 0.3)  # Selected/highlighted

# Button State Colors (independent from generic state colors)
@export_group("Button Colors")
@export var btn_normal: Color = Color(0.8, 0.7, 0.5)
@export var btn_hover: Color = Color(0.9, 0.8, 0.6)
@export var btn_pressed: Color = Color(0.65, 0.55, 0.4)
@export var btn_disabled: Color = Color(0.5, 0.45, 0.4, 0.6)
@export var btn_text: Color = Color(0.15, 0.1, 0.05)
@export var btn_text_disabled: Color = Color(0.4, 0.35, 0.3)

# Tab Colors
@export_group("Tab Colors")
@export var tab_active: Color = Color(0.85, 0.75, 0.6)
@export var tab_inactive: Color = Color(0.65, 0.55, 0.45)
@export var tab_hover: Color = Color(0.75, 0.65, 0.5)
@export var tab_border: Color = Color(0.4, 0.3, 0.2)

# Spacing and Layout (doubled for 1280x720 reference)
@export_group("Layout")
@export var spacing_tiny: int = 8
@export var spacing_small: int = 16
@export var spacing_medium: int = 32
@export var spacing_large: int = 48
@export var spacing_huge: int = 64

@export_subgroup("Padding")
@export var padding_panel: int = 32
@export var padding_container: int = 24
@export var padding_button: int = 16

# Animation
@export_group("Animation")
@export var transition_duration: float = 0.2  # seconds
@export var hover_duration: float = 0.1
@export var fade_duration: float = 0.3

# Helper methods to get colors with modulation
func get_color_hover(base_color: Color) -> Color:
	return base_color.lightened(0.1)

func get_color_pressed(base_color: Color) -> Color:
	return base_color.darkened(0.1)

func get_color_disabled(base_color: Color) -> Color:
	return Color(base_color.r, base_color.g, base_color.b, base_color.a * 0.5)

# Helper to get font or fallback to default
func get_font_or_default(font: Font, fallback_size: int) -> Font:
	if font != null:
		return font
	# Return system default font if none specified
	return ThemeDB.fallback_font

# Get complete StyleBox for panels
# NOTE: Content margins are 0 - use MarginContainer for padding
func create_panel_stylebox() -> StyleBoxFlat:
	var stylebox := StyleBoxFlat.new()
	stylebox.bg_color = panel_color
	stylebox.border_width_left = panel_border_width
	stylebox.border_width_right = panel_border_width
	stylebox.border_width_top = panel_border_width
	stylebox.border_width_bottom = panel_border_width
	stylebox.border_color = secondary_color
	stylebox.corner_radius_top_left = panel_corner_radius
	stylebox.corner_radius_top_right = panel_corner_radius
	stylebox.corner_radius_bottom_left = panel_corner_radius
	stylebox.corner_radius_bottom_right = panel_corner_radius
	stylebox.shadow_size = panel_shadow_size
	stylebox.shadow_color = panel_shadow_color
	# Content margins are 0 - padding comes from MarginContainer
	return stylebox

# Get complete StyleBox for buttons (uses new btn_* colors)
func create_button_stylebox(state: String = "normal") -> StyleBoxFlat:
	var stylebox := StyleBoxFlat.new()

	match state:
		"normal":
			stylebox.bg_color = btn_normal
		"hover":
			stylebox.bg_color = btn_hover
		"pressed":
			stylebox.bg_color = btn_pressed
		"disabled":
			stylebox.bg_color = btn_disabled
		_:
			stylebox.bg_color = btn_normal

	stylebox.border_width_left = button_border_width
	stylebox.border_width_right = button_border_width
	stylebox.border_width_top = button_border_width
	stylebox.border_width_bottom = button_border_width
	stylebox.border_color = border_panel
	stylebox.corner_radius_top_left = button_corner_radius
	stylebox.corner_radius_top_right = button_corner_radius
	stylebox.corner_radius_bottom_left = button_corner_radius
	stylebox.corner_radius_bottom_right = button_corner_radius
	stylebox.content_margin_left = button_padding_horizontal
	stylebox.content_margin_right = button_padding_horizontal
	stylebox.content_margin_top = button_padding_vertical
	stylebox.content_margin_bottom = button_padding_vertical
	return stylebox

# Get StyleBox for equipment slots
func create_slot_stylebox(is_filled: bool = false, is_highlighted: bool = false) -> StyleBoxFlat:
	var stylebox := StyleBoxFlat.new()

	if is_filled:
		stylebox.bg_color = slot_filled_color
	else:
		stylebox.bg_color = slot_empty_color

	stylebox.border_width_left = slot_border_width
	stylebox.border_width_right = slot_border_width
	stylebox.border_width_top = slot_border_width
	stylebox.border_width_bottom = slot_border_width

	if is_highlighted:
		stylebox.border_color = slot_border_color_highlight
	else:
		stylebox.border_color = slot_border_color

	return stylebox

# Get StyleBox for drag-drop slot states
func create_drag_drop_slot_stylebox(state: String = "empty") -> StyleBoxFlat:
	var stylebox := StyleBoxFlat.new()

	match state:
		"empty":
			stylebox.bg_color = slot_empty_color
			stylebox.border_color = border_slot
		"filled":
			stylebox.bg_color = slot_filled_color
			stylebox.border_color = border_slot
		"valid_drop":
			stylebox.bg_color = slot_valid_drop_color
			stylebox.border_color = success_color
		"invalid_drop":
			stylebox.bg_color = slot_invalid_drop_color
			stylebox.border_color = error_color
		"highlighted":
			stylebox.bg_color = slot_filled_color
			stylebox.border_color = border_highlight
		"dragging_from":
			# Recessed/inset appearance for source slot during drag
			stylebox.bg_color = slot_empty_color.darkened(0.25)
			stylebox.border_color = border_slot.darkened(0.3)
			# Inner shadow effect for recessed look
			stylebox.shadow_color = Color(0, 0, 0, 0.4)
			stylebox.shadow_size = 3
			stylebox.shadow_offset = Vector2(1, 1)
		_:
			stylebox.bg_color = slot_empty_color
			stylebox.border_color = border_slot

	stylebox.border_width_left = slot_border_width
	stylebox.border_width_right = slot_border_width
	stylebox.border_width_top = slot_border_width
	stylebox.border_width_bottom = slot_border_width
	stylebox.corner_radius_top_left = 8
	stylebox.corner_radius_top_right = 8
	stylebox.corner_radius_bottom_left = 8
	stylebox.corner_radius_bottom_right = 8

	return stylebox

# Get StyleBox for tab buttons
func create_tab_stylebox(state: String = "inactive") -> StyleBoxFlat:
	var stylebox := StyleBoxFlat.new()

	match state:
		"active":
			stylebox.bg_color = tab_active
			stylebox.border_color = tab_border
		"inactive":
			stylebox.bg_color = tab_inactive
			stylebox.border_color = tab_border.darkened(0.1)
		"hover":
			stylebox.bg_color = tab_hover
			stylebox.border_color = tab_border
		_:
			stylebox.bg_color = tab_inactive
			stylebox.border_color = tab_border

	stylebox.border_width_left = 4
	stylebox.border_width_right = 4
	stylebox.border_width_top = 4
	stylebox.border_width_bottom = 0  # No bottom border for bookmark style
	stylebox.corner_radius_top_left = 12
	stylebox.corner_radius_top_right = 12
	stylebox.corner_radius_bottom_left = 0
	stylebox.corner_radius_bottom_right = 0
	stylebox.content_margin_left = 24
	stylebox.content_margin_right = 24
	stylebox.content_margin_top = 16
	stylebox.content_margin_bottom = 16

	return stylebox

# Get StyleBox for overlay backgrounds
func create_overlay_stylebox() -> StyleBoxFlat:
	var stylebox := StyleBoxFlat.new()
	stylebox.bg_color = bg_overlay
	return stylebox

# Get StyleBox using semantic background colors
# NOTE: Content margins are 0 - use MarginContainer for padding
func create_bg_stylebox(bg_type: String = "primary") -> StyleBoxFlat:
	var stylebox := StyleBoxFlat.new()

	match bg_type:
		"primary":
			stylebox.bg_color = bg_primary
		"secondary":
			stylebox.bg_color = bg_secondary
		"overlay":
			stylebox.bg_color = bg_overlay
		_:
			stylebox.bg_color = bg_primary

	stylebox.border_width_left = panel_border_width
	stylebox.border_width_right = panel_border_width
	stylebox.border_width_top = panel_border_width
	stylebox.border_width_bottom = panel_border_width
	stylebox.border_color = border_panel
	stylebox.corner_radius_top_left = panel_corner_radius
	stylebox.corner_radius_top_right = panel_corner_radius
	stylebox.corner_radius_bottom_left = panel_corner_radius
	stylebox.corner_radius_bottom_right = panel_corner_radius

	return stylebox
