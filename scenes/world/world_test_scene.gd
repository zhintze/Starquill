extends Node2D

# Test scene for world map system
var world_map: WorldMap
var camera: Camera2D
var camera_controller: CameraController
var debug_label: Label
var debug_dead_zone: bool = false  # Set to true to visualize camera dead zone

# Pathfinding and movement
var current_path: Array[Vector2i] = []
var path_index: int = 0
var auto_move_delay: float = 0.0
var auto_move_interval: float = 0.0  # Seconds between auto-moves

# Swipe gesture detection
var swipe_start_pos: Vector2 = Vector2.ZERO
var swipe_min_distance: float = 50.0  # Minimum pixels for a swipe
var is_swiping: bool = false
var continuous_move_direction: Vector2i = Vector2i.ZERO  # For continuous movement on swipe
var continuous_move_delay: float = 0.0
var continuous_move_interval: float = 0.0  # Seconds between continuous moves

# Double-tap detection
var last_tap_time: float = 0.0
var last_tap_position: Vector2 = Vector2.ZERO
var double_tap_threshold: float = 0.4  # Max seconds between taps
var double_tap_distance: float = 30.0  # Max pixels between taps

# Touch state tracking
var was_moving_on_touch: bool = false

func _ready():
	_setup_test_party()
	_setup_world()
	_setup_camera()
	_setup_debug_ui()
	_connect_signals()

func _setup_test_party() -> void:
	# Initialize party if not already created
	if PlayerData.party.members.is_empty():
		PlayerData.initialize_new_game()
		for i in range(4):
			var species_instance = StarquillData.create_random_species_instance()
			var character = CharacterFactory.create_from_species_instance(species_instance)
			character.display_name = "Hero %d" % (i + 1)
			equipment_factory.equip_random_set(character)
			PlayerData.add_character(character)
		print("Created test party with %d members" % PlayerData.party.members.size())

func _setup_world() -> void:
	# Create world map instance
	world_map = WorldMap.new(randi(), "Test World")
	world_map.name = "WorldMap"
	add_child(world_map)

	# Connect to movement completion for instant queued moves
	world_map.movement_completed.connect(_on_movement_completed)

func _setup_camera() -> void:
	# Create camera that follows the party
	camera = Camera2D.new()
	camera.name = "WorldCamera"
	camera.enabled = true
	add_child(camera)

	# Create camera controller for smooth following and pinch-zoom
	camera_controller = CameraController.new()
	camera_controller.name = "CameraController"
	camera_controller.camera = camera
	camera_controller.world_map = self  # Pass self so controller can call get_party_pixel_position
	add_child(camera_controller)

	# Start camera centered on character
	if world_map.party_member_positions.size() > 0:
		var target = WorldConstants.tile_to_pixel(world_map.party_member_positions[0])
		camera.position = target
	else:
		var target = WorldConstants.tile_to_pixel(world_map.party_position)
		camera.position = target

func _setup_debug_ui() -> void:
	# Create debug label
	var canvas_layer = CanvasLayer.new()
	canvas_layer.name = "UILayer"
	add_child(canvas_layer)

	debug_label = Label.new()
	debug_label.name = "DebugLabel"
	debug_label.position = Vector2(10, 10)
	debug_label.add_theme_font_size_override("font_size", 14)
	canvas_layer.add_child(debug_label)

	# Add controls info
	var controls_label = Label.new()
	controls_label.name = "ControlsLabel"
	controls_label.position = Vector2(10, 100)
	controls_label.text = "Controls:\nArrow Keys/WASD - Move\nSpace - Interact\n+/- - Zoom In/Out\n0 - Reset Zoom\n\nTouch:\nTap - Path to tile\nSwipe - Move direction\nPinch - Zoom"
	controls_label.add_theme_font_size_override("font_size", 12)
	canvas_layer.add_child(controls_label)

func _connect_signals() -> void:
	# Connect to world events
	EventBus.location_discovered.connect(_on_location_discovered)
	EventBus.biome_entered.connect(_on_biome_entered)
	EventBus.party_moved.connect(_on_party_moved)

func _process(delta: float) -> void:
	_update_debug_info()
	_update_held_keys()
	_update_path_following(delta)
	_update_continuous_movement(delta)
	# Camera movement now handled by CameraController

func _update_held_keys() -> void:
	# Check for held movement keys and set continuous_move_direction
	# Priority: Check in order so diagonal inputs resolve to single direction

	var movement = Vector2i.ZERO

	# Check vertical movement
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		movement.y = -1
	elif Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		movement.y = 1

	# Check horizontal movement
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		movement.x = -1
	elif Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		movement.x = 1

	# Only use one direction at a time (prioritize vertical if both pressed)
	if movement.y != 0:
		continuous_move_direction = Vector2i(0, movement.y)
	elif movement.x != 0:
		continuous_move_direction = Vector2i(movement.x, 0)
	else:
		# No keys held, stop continuous movement
		continuous_move_direction = Vector2i.ZERO

func _update_continuous_movement(delta: float) -> void:
	if continuous_move_direction == Vector2i.ZERO:
		return

	# If party is moving, don't do anything - signal handler will continue movement
	if world_map.is_party_moving:
		return

	# Delay between continuous moves
	continuous_move_delay -= delta
	if continuous_move_delay > 0:
		return

	# Move in the continuous direction
	if _move_party(continuous_move_direction):
		continuous_move_delay = continuous_move_interval
	else:
		# Movement blocked, stop continuous movement
		continuous_move_direction = Vector2i.ZERO

func _update_path_following(delta: float) -> void:
	if current_path.is_empty():
		return

	# Wait for movement animation to complete
	if world_map.is_party_moving:
		return

	# Auto-move delay between steps
	auto_move_delay -= delta
	if auto_move_delay > 0:
		return

	# Move to next tile in path
	if path_index < current_path.size():
		var next_tile = current_path[path_index]
		var direction = next_tile - world_map.party_position

		if _move_party(direction):
			path_index += 1
			auto_move_delay = auto_move_interval
		else:
			# Movement blocked, cancel path
			# Path blocked, cancel silently
			current_path.clear()
			path_index = 0
	else:
		# Path complete
		current_path.clear()
		path_index = 0

func _input(event: InputEvent) -> void:
	# Handle non-movement input
	if event is InputEventKey and event.pressed:
		match event.keycode:
			KEY_SPACE:
				_interact_with_current_tile()
			KEY_M:
				_toggle_minimap()
			KEY_ESCAPE:
				_open_menu()
			KEY_EQUAL, KEY_PLUS, KEY_KP_ADD:  # + key
				if camera_controller:
					camera_controller.zoom_in(0.1)
			KEY_MINUS, KEY_KP_SUBTRACT:  # - key
				if camera_controller:
					camera_controller.zoom_out(0.1)
			KEY_0, KEY_KP_0:  # 0 key
				if camera_controller:
					camera_controller.reset_zoom()
	# Movement keys are now handled by _update_held_keys() in _process()

	# Handle mouse/touch input
	elif event is InputEventMouseButton:
		if event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			_handle_click(event.position)

	# Handle touch gestures (swipe and double-tap)
	elif event is InputEventScreenTouch:
		if event.pressed:
			# Touch started - stop all movement (both continuous and pathfinding)
			var was_moving = continuous_move_direction != Vector2i.ZERO or not current_path.is_empty()
			continuous_move_direction = Vector2i.ZERO
			current_path.clear()
			path_index = 0

			# Store whether we were moving to decide tap behavior later
			was_moving_on_touch = was_moving

			# Touch started
			swipe_start_pos = event.position
			is_swiping = true
		else:
			# Touch ended - check if it was a swipe or tap
			if is_swiping:
				var swipe_end_pos = event.position
				var swipe_vector = swipe_end_pos - swipe_start_pos
				var swipe_distance = swipe_vector.length()

				if swipe_distance >= swipe_min_distance:
					# It's a swipe - start continuous movement
					_handle_swipe(swipe_vector)
				else:
					# It's a tap
					# If we were already moving, the touch already stopped us - don't start pathfinding
					if not was_moving_on_touch:
						# Only check for double-tap if we weren't moving
						_handle_tap(event.position)

				is_swiping = false

	elif event is InputEventScreenDrag:
		# Dragging cancels all movement
		continuous_move_direction = Vector2i.ZERO
		current_path.clear()
		path_index = 0

func _move_party(direction: Vector2i) -> bool:
	# Cancel any existing path when manually moving (but not continuous movement)
	if continuous_move_direction == Vector2i.ZERO:
		current_path.clear()
		path_index = 0

	var new_position = world_map.party_position + direction
	if world_map.move_party(new_position):
		EventBus.party_moved.emit(world_map.party_position - direction, new_position)
		# Camera smoothly follows via CameraController
		return true
	else:
		EventBus.party_movement_blocked.emit("Impassable terrain")
		# Movement blocked
		return false

func _on_movement_completed() -> void:
	# Movement just completed, immediately start next move if continuous movement active
	print("_on_movement_completed called, continuous_move_direction: ", continuous_move_direction)
	if continuous_move_direction != Vector2i.ZERO:
		print("Starting next continuous move: ", continuous_move_direction)
		if _move_party(continuous_move_direction):
			continuous_move_delay = continuous_move_interval
		else:
			# Movement blocked
			continuous_move_direction = Vector2i.ZERO
	else:
		print("No continuous movement")

func _handle_swipe(swipe_vector: Vector2) -> void:
	# Convert swipe to a continuous movement direction
	# Determine primary direction (horizontal or vertical)
	var abs_x = abs(swipe_vector.x)
	var abs_y = abs(swipe_vector.y)

	var direction = Vector2i.ZERO
	if abs_x > abs_y:
		# Horizontal swipe
		direction = Vector2i(1 if swipe_vector.x > 0 else -1, 0)
	else:
		# Vertical swipe
		# Swipe down (positive Y) should move character down (positive Y)
		# Swipe up (negative Y) should move character up (negative Y)
		direction = Vector2i(0, 1 if swipe_vector.y > 0 else -1)

	# Swipe detected

	# Start continuous movement in this direction
	continuous_move_direction = direction
	continuous_move_delay = 0.0  # Start immediately

	# Also do one immediate move
	_move_party(direction)

func _handle_tap(screen_position: Vector2) -> void:
	# Check if this is a double-tap
	var current_time = Time.get_ticks_msec() / 1000.0
	var time_since_last_tap = current_time - last_tap_time
	var distance_from_last_tap = screen_position.distance_to(last_tap_position)

	if time_since_last_tap < double_tap_threshold and distance_from_last_tap < double_tap_distance:
		# This is a double-tap! Path to this tile
		_handle_click(screen_position)
		# Reset tap tracking
		last_tap_time = 0.0
		last_tap_position = Vector2.ZERO
	else:
		# Single tap - just record it
		last_tap_time = current_time
		last_tap_position = screen_position

func _handle_click(screen_position: Vector2) -> void:
	# Convert screen position to world position
	# Use camera's screen center position for more accurate conversion
	var viewport = get_viewport()
	var screen_center = viewport.get_visible_rect().size / 2.0
	var screen_center_world = camera.get_screen_center_position()

	# Calculate offset from screen center and convert to world space
	var offset_from_center = screen_position - screen_center
	var world_position = screen_center_world + (offset_from_center / camera.zoom)

	# Convert to tile coordinates (pixel_to_tile does floor division)
	var tile_position = WorldConstants.pixel_to_tile(world_position)

	EventBus.tile_clicked.emit(tile_position)

	# Find path to clicked tile
	var path = Pathfinder.find_path(world_map, world_map.party_position, tile_position)

	if path.is_empty():
		return

	# Start auto-movement along path
	current_path = path
	path_index = 0
	auto_move_delay = 0.0

func _interact_with_current_tile() -> void:
	var tile = world_map.get_tile(world_map.party_position)
	if tile and tile.has_location():
		print("Interacting with location: %s" % tile.location_data.get_display_name())
		EventBus.location_entered.emit(tile.location_data)

func get_party_pixel_position() -> Vector2:
	# Get character position (with movement interpolation)
	# This method is called by CameraController
	var character_pos: Vector2

	# Get character position (without hop animation)
	if world_map.is_party_moving and world_map.party_member_trail.size() > 0:
		# During movement, interpolate between tiles (no hop tracking)
		var t = world_map.movement_progress
		var start_pos = world_map.party_member_positions[0] if world_map.party_member_positions.size() > 0 else world_map.party_position
		var end_pos = world_map.party_member_trail[0] if world_map.party_member_trail.size() > 0 else start_pos

		var lerp_x = lerp(float(start_pos.x), float(end_pos.x), t)
		var lerp_y = lerp(float(start_pos.y), float(end_pos.y), t)
		character_pos = Vector2(lerp_x * WorldConstants.TILE_SIZE, lerp_y * WorldConstants.TILE_SIZE)
	elif world_map.party_member_positions.size() > 0:
		# Static position
		character_pos = WorldConstants.tile_to_pixel(world_map.party_member_positions[0])
	else:
		character_pos = WorldConstants.tile_to_pixel(world_map.party_position)

	return character_pos

func _update_debug_info() -> void:
	if debug_label:
		var info = "World: %s\n" % world_map.world_name
		info += "Position: %v\n" % world_map.party_position
		info += "Chunk: %v\n" % WorldCoordinate.world_to_chunk(world_map.party_position)
		info += "Loaded Chunks: %d\n" % world_map.chunk_manager.loaded_chunks.size()
		info += "Visible Tiles: %d\n" % world_map.visible_tiles.size()
		if camera_controller:
			info += "Zoom: %.2f\n" % camera_controller.get_current_zoom()

		# Show path info
		if not current_path.is_empty():
			info += "Path: %d/%d tiles\n" % [path_index, current_path.size()]

		var tile = world_map.get_tile(world_map.party_position)
		if tile:
			info += "Tile Type: %s\n" % WorldConstants.TileType.keys()[tile.type]
			info += "Biome: %s\n" % tile.biome_id
			if tile.has_location():
				info += "Location: %s" % tile.location_data.get_display_name()

		debug_label.text = info

func _toggle_minimap() -> void:
	print("Minimap toggle - TODO")

func _open_menu() -> void:
	print("Menu opened - TODO")

# Signal handlers
func _on_location_discovered(location_data: LocationData) -> void:
	print("Location discovered: %s" % location_data.name)

func _on_biome_entered(biome_type: WorldConstants.BiomeType, biome_name: String) -> void:
	print("Entered biome: %s" % biome_name)

func _on_party_moved(from: Vector2i, to: Vector2i) -> void:
	pass  # Movement handled silently
