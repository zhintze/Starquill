extends Node2D

# Test scene for world map system
var world_map: WorldMap
var camera: Camera2D
var debug_label: Label

func _ready():
	print("World Test Scene initializing...")
	_setup_world()
	_setup_camera()
	_setup_debug_ui()
	_connect_signals()

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
	camera.zoom = Vector2(1.0, 1.0)
	add_child(camera)

	# Position camera at party location
	var party_pixel_pos = WorldConstants.tile_to_pixel(world_map.party_position)
	camera.position = party_pixel_pos

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
	controls_label.text = "Controls:\nArrow Keys/WASD - Move\nSpace - Interact\nM - Toggle Minimap\nEsc - Menu"
	controls_label.add_theme_font_size_override("font_size", 12)
	canvas_layer.add_child(controls_label)

func _connect_signals() -> void:
	# Connect to world events
	BusWorld.location_discovered.connect(_on_location_discovered)
	BusWorld.biome_entered.connect(_on_biome_entered)
	BusParty.party_moved.connect(_on_party_moved)

func _process(_delta: float) -> void:
	_update_debug_info()

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
		_update_camera_position()
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

func _update_camera_position() -> void:
	# Smooth camera follow
	var target_pos = WorldConstants.tile_to_pixel(world_map.party_position)
	camera.position = camera.position.lerp(target_pos, 0.2)

func _update_debug_info() -> void:
	if debug_label:
		var info = "World: %s\n" % world_map.world_name
		info += "Position: %v\n" % world_map.party_position
		info += "Chunk: %v\n" % WorldConstants.world_to_chunk(world_map.party_position)
		info += "Loaded Chunks: %d\n" % world_map.loaded_chunks.size()
		info += "Visible Tiles: %d\n" % world_map.visible_tiles.size()

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