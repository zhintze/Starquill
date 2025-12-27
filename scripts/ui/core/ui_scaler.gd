class_name UIScaler
extends RefCounted

## UIScaler
## Utility class for responsive UI scaling on mobile devices
## Handles DPI scaling, safe areas, and adaptive sizing

# Cached values (call refresh() when viewport changes)
static var _scale_factor: float = 1.0
static var _safe_area: Rect2 = Rect2()
static var _viewport_size: Vector2 = Vector2.ZERO
static var _initialized: bool = false

# Initialize or refresh scaling calculations
static func refresh() -> void:
	var viewport := Engine.get_main_loop()
	if viewport is SceneTree:
		var root: Window = viewport.root
		if root:
			_viewport_size = root.get_visible_rect().size
			_calculate_scale_factor()
			_calculate_safe_area()
			_initialized = true

static func _calculate_scale_factor() -> void:
	# Calculate scale based on reference width
	# This ensures consistent sizing across different screen densities
	var width_scale := _viewport_size.x / UIConstants.REFERENCE_WIDTH
	var height_scale := _viewport_size.y / UIConstants.REFERENCE_HEIGHT

	# Use the smaller scale to ensure content fits
	_scale_factor = min(width_scale, height_scale)

	# Clamp to reasonable bounds
	_scale_factor = clamp(_scale_factor, 0.5, 3.0)

static func _calculate_safe_area() -> void:
	# Try to get actual safe area from DisplayServer
	var safe_rect := DisplayServer.get_display_safe_area()

	if safe_rect.size.x > 0 and safe_rect.size.y > 0:
		_safe_area = safe_rect
	else:
		# Fallback to full viewport
		_safe_area = Rect2(Vector2.ZERO, _viewport_size)

# Get current scale factor
static func get_scale_factor() -> float:
	if not _initialized:
		refresh()
	return _scale_factor

# Scale a value by the current scale factor
static func scale(value: float) -> float:
	return value * get_scale_factor()

# Scale a Vector2 by the current scale factor
static func scale_vec2(value: Vector2) -> Vector2:
	var factor := get_scale_factor()
	return Vector2(value.x * factor, value.y * factor)

# Scale an integer value (rounded)
static func scale_int(value: int) -> int:
	return int(round(float(value) * get_scale_factor()))

# Get safe area insets
static func get_safe_area() -> Rect2:
	if not _initialized:
		refresh()
	return _safe_area

# Get safe area inset from top
static func get_safe_top() -> float:
	return get_safe_area().position.y

# Get safe area inset from bottom
static func get_safe_bottom() -> float:
	var safe := get_safe_area()
	return _viewport_size.y - (safe.position.y + safe.size.y)

# Get safe area inset from left
static func get_safe_left() -> float:
	return get_safe_area().position.x

# Get safe area inset from right
static func get_safe_right() -> float:
	var safe := get_safe_area()
	return _viewport_size.x - (safe.position.x + safe.size.x)

# Get viewport size
static func get_viewport_size() -> Vector2:
	if not _initialized:
		refresh()
	return _viewport_size

# Get usable content area (viewport minus safe areas)
static func get_content_area() -> Rect2:
	return get_safe_area()

# Calculate responsive value based on screen width
# min_val at 360dp width, max_val at 600dp width, interpolated between
static func responsive(min_val: float, max_val: float) -> float:
	var width := get_viewport_size().x
	var t := inverse_lerp(360.0, 600.0, width)
	t = clamp(t, 0.0, 1.0)
	return lerp(min_val, max_val, t)

# Get minimum touch target size (scaled)
static func get_min_touch_size() -> float:
	return max(scale(UIConstants.MIN_TOUCH_TARGET), UIConstants.MIN_TOUCH_TARGET_SMALL)

# Check if we're on a tablet-sized device
static func is_tablet() -> bool:
	var width := get_viewport_size().x
	return width >= 600.0

# Check if we're in landscape orientation
static func is_landscape() -> bool:
	var size := get_viewport_size()
	return size.x > size.y

# Get appropriate slot size based on screen
static func get_slot_size() -> int:
	if is_tablet():
		return scale_int(UIConstants.SLOT_SIZE_LARGE)
	else:
		return scale_int(UIConstants.SLOT_SIZE_MEDIUM)

# Get number of inventory columns that fit
static func get_inventory_columns() -> int:
	var content := get_content_area()
	var slot_size := get_slot_size()
	var spacing := scale_int(UIConstants.SPACING_XS)
	var available_width := content.size.x * UIConstants.CONTENT_PANEL_WIDTH_RATIO
	var padding := scale_int(UIConstants.PANEL_PADDING) * 2

	available_width -= padding
	var cols := int(available_width / (slot_size + spacing))
	return max(cols, 3)  # Minimum 3 columns

# Apply safe area margins to a Control
static func apply_safe_margins(control: Control) -> void:
	if control == null:
		return

	control.add_theme_constant_override("margin_top", int(get_safe_top()))
	control.add_theme_constant_override("margin_bottom", int(get_safe_bottom()))
	control.add_theme_constant_override("margin_left", int(get_safe_left()))
	control.add_theme_constant_override("margin_right", int(get_safe_right()))

# Create margin container with safe area insets
static func create_safe_margin_container() -> MarginContainer:
	var container := MarginContainer.new()
	container.add_theme_constant_override("margin_top", int(get_safe_top()))
	container.add_theme_constant_override("margin_bottom", int(get_safe_bottom()))
	container.add_theme_constant_override("margin_left", int(get_safe_left()))
	container.add_theme_constant_override("margin_right", int(get_safe_right()))
	return container

# Ensure a control meets minimum touch target size
static func ensure_touch_size(control: Control) -> void:
	if control == null:
		return

	var min_size := get_min_touch_size()
	control.custom_minimum_size = Vector2(
		max(control.custom_minimum_size.x, min_size),
		max(control.custom_minimum_size.y, min_size)
	)
