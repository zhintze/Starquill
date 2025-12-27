class_name BasePanel
extends NinePatchRect

## BasePanel
## A themed panel using 9-slice texture with optional paper shader
## Use for main containers, dialog boxes, and panel backgrounds

signal panel_ready

# Panel style variants
enum PanelStyle {
	PRIMARY,    # Main containers with full border
	SECONDARY,  # Inner panels with lighter styling
	OVERLAY     # Modal overlay background
}

@export var panel_style: PanelStyle = PanelStyle.PRIMARY:
	set(value):
		panel_style = value
		_apply_style()

@export var use_paper_shader: bool = true:
	set(value):
		use_paper_shader = value
		_setup_shader()

@export var paper_grain_intensity: float = 0.15:
	set(value):
		paper_grain_intensity = value
		_update_shader_params()

@export var paper_vignette_intensity: float = 0.1:
	set(value):
		paper_vignette_intensity = value
		_update_shader_params()

# Preloaded resources
var _panel_texture: Texture2D
var _panel_inner_texture: Texture2D
var _paper_shader: Shader
var _shader_material: ShaderMaterial

func _ready() -> void:
	_load_resources()
	_apply_style()
	_setup_shader()
	panel_ready.emit()

func _load_resources() -> void:
	# Load 9-slice textures
	var panel_path := "res://resources/textures/ui/panel_9slice.png"
	var inner_path := "res://resources/textures/ui/panel_inner_9slice.png"
	var shader_path := "res://resources/shaders/paper_texture.gdshader"

	if ResourceLoader.exists(panel_path):
		_panel_texture = load(panel_path)
	if ResourceLoader.exists(inner_path):
		_panel_inner_texture = load(inner_path)
	if ResourceLoader.exists(shader_path):
		_paper_shader = load(shader_path)

func _apply_style() -> void:
	var theme := UIThemeManager.get_theme()

	match panel_style:
		PanelStyle.PRIMARY:
			if _panel_texture:
				texture = _panel_texture
				# 9-slice margins (12px corners for 48px texture)
				patch_margin_left = 12
				patch_margin_top = 12
				patch_margin_right = 12
				patch_margin_bottom = 12
			else:
				_apply_fallback_style(theme.bg_primary, theme.border_panel)

		PanelStyle.SECONDARY:
			if _panel_inner_texture:
				texture = _panel_inner_texture
				# 9-slice margins (8px corners for 32px texture)
				patch_margin_left = 8
				patch_margin_top = 8
				patch_margin_right = 8
				patch_margin_bottom = 8
			else:
				_apply_fallback_style(theme.bg_secondary, theme.border_slot)

		PanelStyle.OVERLAY:
			# Overlay uses no texture, just shader or color
			texture = null
			_apply_fallback_style(theme.bg_overlay, Color.TRANSPARENT)

func _apply_fallback_style(bg_color: Color, border_color: Color) -> void:
	# If no texture available, we can't use NinePatchRect effectively
	# The shader will provide the background color
	modulate = bg_color

func _setup_shader() -> void:
	if use_paper_shader and _paper_shader:
		if not _shader_material:
			_shader_material = ShaderMaterial.new()
			_shader_material.shader = _paper_shader

		material = _shader_material
		_update_shader_params()
	else:
		material = null

func _update_shader_params() -> void:
	if not _shader_material:
		return

	var theme := UIThemeManager.get_theme()

	# Set base color based on panel style
	var base_color: Color
	match panel_style:
		PanelStyle.PRIMARY:
			base_color = theme.bg_primary
		PanelStyle.SECONDARY:
			base_color = theme.bg_secondary
		PanelStyle.OVERLAY:
			base_color = theme.bg_overlay

	_shader_material.set_shader_parameter("base_color", base_color)
	_shader_material.set_shader_parameter("grain_intensity", paper_grain_intensity)
	_shader_material.set_shader_parameter("vignette_intensity", paper_vignette_intensity)
	_shader_material.set_shader_parameter("fiber_intensity", paper_grain_intensity * 0.5)

# Apply a custom tint color to the shader
func set_tint(tint: Color) -> void:
	if _shader_material:
		_shader_material.set_shader_parameter("tint_color", tint)

# Refresh styling (call after theme change)
func refresh_style() -> void:
	_apply_style()
	_update_shader_params()

# Create a BasePanel programmatically
static func create(style: PanelStyle = PanelStyle.PRIMARY, use_shader: bool = true) -> BasePanel:
	var panel := BasePanel.new()
	panel.panel_style = style
	panel.use_paper_shader = use_shader
	return panel
