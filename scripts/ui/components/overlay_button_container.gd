class_name OverlayButtonContainer
extends Control

## OverlayButtonContainer
## A container for buttons that overlay content at screen corners.
## Uses theme spacing tokens and safe area insets for consistent positioning.
##
## Usage:
##   var overlay := OverlayButtonContainer.new()
##   overlay.corner_position = OverlayButtonContainer.Position.TOP_RIGHT
##   overlay.add_child(close_button)
##   parent.add_child(overlay)

enum Position {
	TOP_LEFT,
	TOP_RIGHT,
	BOTTOM_LEFT,
	BOTTOM_RIGHT
}

@export var corner_position: Position = Position.TOP_LEFT:
	set(value):
		corner_position = value
		if is_inside_tree():
			_apply_position()

@export var use_safe_area: bool = true

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Set initial size based on expected button size
	custom_minimum_size = Vector2(UIConstants.ICON_BUTTON_SIZE, UIConstants.ICON_BUTTON_SIZE)

	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

	# Apply position after a frame to ensure parent is sized
	await get_tree().process_frame
	_apply_position()

func _apply_position() -> void:
	var theme := UIThemeManager.get_theme()
	var margin := theme.padding_panel

	var safe_top := 0
	var safe_bottom := 0
	var safe_left := 0
	var safe_right := 0

	if use_safe_area and UIScaler:
		safe_top = maxi(0, int(UIScaler.get_safe_top()))
		safe_bottom = maxi(0, int(UIScaler.get_safe_bottom()))
		safe_left = maxi(0, int(UIScaler.get_safe_left()))
		safe_right = maxi(0, int(UIScaler.get_safe_right()))

	# Get parent size
	var parent_size := get_parent_area_size()
	var btn_size := custom_minimum_size

	# Position based on corner - use direct position since we're not stretching
	match corner_position:
		Position.TOP_LEFT:
			position = Vector2(margin + safe_left, margin + safe_top)

		Position.TOP_RIGHT:
			position = Vector2(parent_size.x - btn_size.x - margin - safe_right, margin + safe_top)

		Position.BOTTOM_LEFT:
			position = Vector2(margin + safe_left, parent_size.y - btn_size.y - margin - safe_bottom)

		Position.BOTTOM_RIGHT:
			position = Vector2(parent_size.x - btn_size.x - margin - safe_right, parent_size.y - btn_size.y - margin - safe_bottom)

	size = btn_size

func _notification(what: int) -> void:
	if what == NOTIFICATION_CHILD_ORDER_CHANGED:
		_update_size_from_children()

func _update_size_from_children() -> void:
	var max_size := Vector2(UIConstants.ICON_BUTTON_SIZE, UIConstants.ICON_BUTTON_SIZE)
	for child in get_children():
		if child is Control:
			var ctrl := child as Control
			var child_size: Vector2 = ctrl.custom_minimum_size
			if child_size.x <= 0:
				child_size = ctrl.get_combined_minimum_size()
			max_size.x = max(max_size.x, child_size.x)
			max_size.y = max(max_size.y, child_size.y)

	custom_minimum_size = max_size
	if is_inside_tree():
		call_deferred("_apply_position")

func _on_theme_changed(_new_theme: Resource) -> void:
	_apply_position()

static func create(pos: Position = Position.TOP_LEFT, safe_area: bool = true) -> OverlayButtonContainer:
	var container := OverlayButtonContainer.new()
	container.corner_position = pos
	container.use_safe_area = safe_area
	return container
