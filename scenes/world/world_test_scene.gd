extends Node2D

# Test scene for world map system
var world_map: WorldMap
var camera: Camera2D
var camera_controller: CameraController
var debug_label: Label
var debug_dead_zone: bool = false  # Set to true to visualize camera dead zone

func _ready():
	print("World Test Scene initializing...")
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

	# Start camera slightly offset so it smoothly slides to character on load
	# This prevents snapping and creates a nice entrance effect
	if world_map.party_member_positions.size() > 0:
		var target = WorldConstants.tile_to_pixel(world_map.party_member_positions[0])
		camera.position = target + Vector2(0, -WorldConstants.TILE_SIZE * 2)  # Start above, slide down
	else:
		var target = WorldConstants.tile_to_pixel(world_map.party_position)
		camera.position = target + Vector2(0, -WorldConstants.TILE_SIZE * 2)

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
	controls_label.text = "Controls:\nArrow Keys/WASD - Move\nSpace - Interact\nM - Toggle Minimap\nEsc - Menu\n+/- - Zoom In/Out\n0 - Reset Zoom\nPinch - Zoom (Touch)"
	controls_label.add_theme_font_size_override("font_size", 12)
	canvas_layer.add_child(controls_label)

func _connect_signals() -> void:
	# Connect to world events
	BusWorld.location_discovered.connect(_on_location_discovered)
	BusWorld.biome_entered.connect(_on_biome_entered)
	BusParty.party_moved.connect(_on_party_moved)

func _process(delta: float) -> void:
	_update_debug_info()
	# Camera movement now handled by CameraController

func _input(event: InputEvent) -> void:
	# Handle movement input
	if event is InputEventKey and event.pressed:
		var movement = Vector2i.ZERO

		match event.keycode:
			KEY_W, KEY_UP:
				movement = Vector2i(0, -1)
			KEY_S, KEY_DOWN:
				movement = Vector2i(0, 1)
			KEY_A, KEY_LEFT:
				movement = Vector2i(-1, 0)
			KEY_D, KEY_RIGHT:
				movement = Vector2i(1, 0)
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

		if movement != Vector2i.ZERO:
			_move_party(movement)

	# Handle mouse/touch input
	elif event is InputEventMouseButton:
		if event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			_handle_click(event.position)

func _move_party(direction: Vector2i) -> void:
	var new_position = world_map.party_position + direction
	if world_map.move_party(new_position):
		BusParty.party_moved.emit(world_map.party_position - direction, new_position)
		# Camera smoothly follows via _smooth_camera_follow in _process
	else:
		BusParty.party_movement_blocked.emit("Impassable terrain")
		print("Movement blocked at position: %v" % new_position)

func _handle_click(screen_position: Vector2) -> void:
	# Convert screen position to world tile position
	var world_position = camera.get_global_mouse_position()
	var tile_position = WorldConstants.pixel_to_tile(world_position)

	print("Clicked tile: %v" % tile_position)
	BusWorld.tile_clicked.emit(tile_position)

	# TODO: Implement pathfinding to clicked tile

func _interact_with_current_tile() -> void:
	var tile = world_map.get_tile(world_map.party_position)
	if tile and tile.has_location():
		print("Interacting with location: %s" % tile.location_data.get_display_name())
		BusWorld.location_entered.emit(tile.location_data)

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
	print("Party moved from %v to %v" % [from, to])
