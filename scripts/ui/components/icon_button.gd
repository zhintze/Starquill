class_name IconButton
extends ThemedButton

## IconButton
## A themed button with an icon and optional text label
## Icons are tinted based on button state

# Icon positioning
enum IconPosition {
	LEFT,   # Icon to the left of text
	RIGHT,  # Icon to the right of text
	TOP,    # Icon above text
	ONLY    # Icon only, no text
}

@export var button_icon: Texture2D:
	set(value):
		button_icon = value
		_update_icon()

@export var icon_size: Vector2 = Vector2(24, 24):
	set(value):
		icon_size = value
		_update_icon()

@export var icon_position: IconPosition = IconPosition.LEFT:
	set(value):
		icon_position = value
		_rebuild_layout()

@export var icon_text_gap: int = 8:
	set(value):
		icon_text_gap = value
		_rebuild_layout()

# Icon tinting
@export var tint_icon_with_text: bool = true

# Preloaded icon paths for convenience
enum PresetIcon {
	NONE,
	ARROW_LEFT,
	ARROW_RIGHT,
	CLOSE,
	CHECK,
	PLUS,
	MINUS,
	TRASH,
	SORT,
	INFO
}

@export var preset_icon: PresetIcon = PresetIcon.NONE:
	set(value):
		preset_icon = value
		_load_preset_icon()

# Internal nodes
var _icon_rect: TextureRect
var _container: BoxContainer
var _label_node: Label
var _is_built: bool = false

func _ready() -> void:
	# Build internal structure before parent _ready
	_build_internal_structure()
	super._ready()
	_update_icon()

func _build_internal_structure() -> void:
	if _is_built:
		return

	# Clear default button text - we'll use our own Label
	text = ""

	# Create container based on icon position
	_rebuild_layout()
	_is_built = true

func _rebuild_layout() -> void:
	# Remove existing container if any
	if _container:
		_container.queue_free()
		_container = null
		_icon_rect = null
		_label_node = null

	# Create appropriate container
	match icon_position:
		IconPosition.LEFT, IconPosition.RIGHT:
			_container = HBoxContainer.new()
		IconPosition.TOP:
			_container = VBoxContainer.new()
		IconPosition.ONLY:
			_container = HBoxContainer.new()

	_container.name = "Container"
	_container.alignment = BoxContainer.ALIGNMENT_CENTER
	_container.add_theme_constant_override("separation", icon_text_gap)
	_container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_container)

	# Anchor container to fill button
	_container.set_anchors_preset(Control.PRESET_FULL_RECT)
	_container.offset_left = 0
	_container.offset_top = 0
	_container.offset_right = 0
	_container.offset_bottom = 0

	# Create icon rect
	_icon_rect = TextureRect.new()
	_icon_rect.name = "Icon"
	_icon_rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_icon_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	_icon_rect.custom_minimum_size = icon_size
	_icon_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Create label (if not icon-only)
	if icon_position != IconPosition.ONLY:
		_label_node = Label.new()
		_label_node.name = "Label"
		_label_node.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		_label_node.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		_label_node.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Add in correct order
	match icon_position:
		IconPosition.LEFT, IconPosition.TOP:
			_container.add_child(_icon_rect)
			if _label_node:
				_container.add_child(_label_node)
		IconPosition.RIGHT:
			if _label_node:
				_container.add_child(_label_node)
			_container.add_child(_icon_rect)
		IconPosition.ONLY:
			_container.add_child(_icon_rect)

	_update_icon()
	_update_label()

func _update_icon() -> void:
	if not _icon_rect:
		return

	_icon_rect.texture = button_icon
	_icon_rect.custom_minimum_size = icon_size

	if tint_icon_with_text:
		_update_icon_tint()

func _update_label() -> void:
	if not _label_node:
		return

	# Get the button's text property (stored internally)
	_label_node.text = get_meta("button_text", "")

	var theme := UIThemeManager.get_theme()
	_label_node.add_theme_font_size_override("font_size", theme.font_size_button)
	if theme.font_button:
		_label_node.add_theme_font_override("font", theme.font_button)

func _update_icon_tint() -> void:
	if not _icon_rect or not tint_icon_with_text:
		_icon_rect.modulate = Color.WHITE
		return

	var theme := UIThemeManager.get_theme()

	if disabled:
		_icon_rect.modulate = theme.btn_text_disabled
	else:
		_icon_rect.modulate = theme.btn_text

func _load_preset_icon() -> void:
	var icon_path: String = ""

	match preset_icon:
		PresetIcon.ARROW_LEFT:
			icon_path = "res://resources/textures/ui/icons/arrow_left.png"
		PresetIcon.ARROW_RIGHT:
			icon_path = "res://resources/textures/ui/icons/arrow_right.png"
		PresetIcon.CLOSE:
			icon_path = "res://resources/textures/ui/icons/close.png"
		PresetIcon.CHECK:
			icon_path = "res://resources/textures/ui/icons/check.png"
		PresetIcon.PLUS:
			icon_path = "res://resources/textures/ui/icons/plus.png"
		PresetIcon.MINUS:
			icon_path = "res://resources/textures/ui/icons/minus.png"
		PresetIcon.TRASH:
			icon_path = "res://resources/textures/ui/icons/trash.png"
		PresetIcon.SORT:
			icon_path = "res://resources/textures/ui/icons/sort.png"
		PresetIcon.INFO:
			icon_path = "res://resources/textures/ui/icons/info.png"
		PresetIcon.NONE:
			button_icon = null
			return

	if icon_path and ResourceLoader.exists(icon_path):
		button_icon = load(icon_path)

# Override to store text in meta and update our label
func set_button_text(new_text: String) -> void:
	set_meta("button_text", new_text)
	_update_label()

func get_button_text() -> String:
	return get_meta("button_text", "")

# Override mouse events to update icon tint
func _on_mouse_entered() -> void:
	super._on_mouse_entered()
	if tint_icon_with_text and _icon_rect:
		var theme := UIThemeManager.get_theme()
		_icon_rect.modulate = theme.btn_text

func _on_mouse_exited() -> void:
	super._on_mouse_exited()
	_update_icon_tint()

func _on_button_down() -> void:
	super._on_button_down()
	if tint_icon_with_text and _icon_rect:
		var theme := UIThemeManager.get_theme()
		_icon_rect.modulate = theme.btn_text

func _on_button_up() -> void:
	super._on_button_up()
	_update_icon_tint()

# Refresh styling
func refresh_style() -> void:
	super.refresh_style()
	_update_icon_tint()
	_update_label()

# Create an IconButton programmatically
static func create_with_icon(
	icon_texture: Texture2D,
	label: String = "",
	style: ButtonStyle = ButtonStyle.PRIMARY,
	position: IconPosition = IconPosition.LEFT
) -> IconButton:
	var btn := IconButton.new()
	btn.button_icon = icon_texture
	btn.button_style = style
	btn.icon_position = position
	if label:
		btn.set_button_text(label)
	return btn

static func create_with_preset(
	preset: PresetIcon,
	label: String = "",
	style: ButtonStyle = ButtonStyle.PRIMARY,
	position: IconPosition = IconPosition.LEFT
) -> IconButton:
	var btn := IconButton.new()
	btn.preset_icon = preset
	btn.button_style = style
	btn.icon_position = position
	if label:
		btn.set_button_text(label)
	return btn
