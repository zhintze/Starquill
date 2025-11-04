class_name CameraController
extends Node

# Camera controller with smooth dead-zone following and pinch-zoom support
# Designed for world map navigation with touch and mouse input

# Configuration - all exported for easy tuning in Inspector
@export_group("Zoom Settings")
@export var zoom_min: float = 0.5  # Maximum zoom in
@export var zoom_max: float = 2.0  # Maximum zoom out
@export var zoom_default: float = 0.75
@export var zoom_smoothing: float = 5.0  # Lerp speed for smooth zoom transitions

@export_group("Camera Follow Settings")
@export var dead_zone_base_tiles: float = 0.5  # Base dead zone size in tiles
@export var dead_zone_scales_with_zoom: bool = true  # Scale dead zone with zoom level
@export var camera_smoothing: float = 1.5  # Lerp speed for camera position

@export_group("References")
@export var camera: Camera2D
@export var world_map: Node2D  # WorldMap reference for party position

# Internal state
var target_zoom: float
var current_zoom: float

# Touch gesture tracking
var touch_points: Dictionary = {}  # Maps touch index to position
var initial_pinch_distance: float = 0.0
var pinch_zoom_start: float = 1.0

func _ready() -> void:
	if not camera:
		push_error("CameraController: No camera assigned")
		return

	# Initialize zoom
	current_zoom = zoom_default
	target_zoom = zoom_default
	camera.zoom = Vector2(current_zoom, current_zoom)

func _process(delta: float) -> void:
	if not camera:
		return

	# Update zoom smoothly
	_update_zoom(delta)

	# Update camera position with dead-zone following
	_update_camera_position(delta)

func _input(event: InputEvent) -> void:
	if not camera:
		return

	# Handle touch events for pinch-zoom
	if event is InputEventScreenTouch:
		_handle_touch(event)
	elif event is InputEventScreenDrag:
		_handle_drag(event)

func _handle_touch(event: InputEventScreenTouch) -> void:
	if event.pressed:
		# Touch started
		touch_points[event.index] = event.position

		# If this is the second finger, start pinch gesture
		if touch_points.size() == 2:
			_start_pinch()
	else:
		# Touch ended
		touch_points.erase(event.index)

		# If we go back to one or zero touches, end pinch
		if touch_points.size() < 2:
			_end_pinch()

func _handle_drag(event: InputEventScreenDrag) -> void:
	# Update touch point position
	if touch_points.has(event.index):
		touch_points[event.index] = event.position

	# If we have two touches, update pinch zoom
	if touch_points.size() == 2:
		_update_pinch()

func _start_pinch() -> void:
	# Calculate initial distance between two touch points
	var points = touch_points.values()
	if points.size() == 2:
		initial_pinch_distance = points[0].distance_to(points[1])
		pinch_zoom_start = current_zoom

func _update_pinch() -> void:
	# Calculate current distance and zoom delta
	var points = touch_points.values()
	if points.size() == 2 and initial_pinch_distance > 0:
		var current_distance = points[0].distance_to(points[1])
		var distance_ratio = current_distance / initial_pinch_distance

		# Apply zoom change
		target_zoom = clamp(pinch_zoom_start * distance_ratio, zoom_min, zoom_max)

func _end_pinch() -> void:
	# Reset pinch tracking
	initial_pinch_distance = 0.0

func _update_zoom(delta: float) -> void:
	# Smoothly interpolate current zoom toward target
	if abs(current_zoom - target_zoom) > 0.001:
		current_zoom = lerp(current_zoom, target_zoom, zoom_smoothing * delta)
		camera.zoom = Vector2(current_zoom, current_zoom)

func _update_camera_position(delta: float) -> void:
	# Direct camera follow - perfectly centered on character
	var target_position = _get_target_position()
	camera.position = target_position

func _get_target_position() -> Vector2:
	# Get the position we want the camera to follow
	# This should be overridden or configured based on what you're following
	if world_map and world_map.has_method("get_party_pixel_position"):
		return world_map.get_party_pixel_position()
	return camera.position

# Public API for manual zoom control
func set_zoom(new_zoom: float) -> void:
	target_zoom = clamp(new_zoom, zoom_min, zoom_max)

func zoom_in(amount: float = 0.1) -> void:
	set_zoom(target_zoom - amount)

func zoom_out(amount: float = 0.1) -> void:
	set_zoom(target_zoom + amount)

func reset_zoom() -> void:
	set_zoom(zoom_default)

func get_current_zoom() -> float:
	return current_zoom
