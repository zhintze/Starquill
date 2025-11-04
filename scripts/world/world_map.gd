extends Node2D
class_name WorldMap

signal movement_completed

const CHARACTER_TILE_OFFSET: float = -0.45;

# World properties
var world_seed: int = 0
var world_size: Vector2i = Vector2i(WorldConstants.WORLD_SIZE, WorldConstants.WORLD_SIZE)
var world_name: String = "New World"

# Chunk management
var chunk_manager: ChunkManager
var tile_renderer: TileRenderer

# Party tracking
var party_position: Vector2i = Vector2i.ZERO
var visible_tiles: Array[Vector2i] = []
var revealed_tiles: Array[Vector2i] = []

# Party movement animation
var party_member_positions: Array[Vector2i] = []  # Current tile positions for each member
var party_member_trail: Array[Vector2i] = []  # Movement history trail
var party_member_facing: Array[bool] = []  # True = facing right, False = facing left
var party_member_hop_offsets: Array[float] = []  # Phase offset for each character's hop
var party_member_hop_speeds: Array[float] = []  # Slight speed variation for each character
var is_party_moving: bool = false
var movement_progress: float = 0.0
var movement_duration: float = 0.6  # seconds per tile
var hop_height: float = 20.0  # pixels to hop up
var hop_frequency: float = 3.0  # hops per tile movement (stays same, so hops are slower)

# Generation parameters
var location_density: float = WorldConstants.LOCATION_DENSITY
var biome_seed_points: Dictionary = {}  # position -> biome_type

# Components
var chunk_container: Node2D
var party_container: Node2D
var effect_container: Node2D

# State flags
var is_generating: bool = false
var is_initialized: bool = false

func _init(p_seed: int = 0, p_name: String = "New World"):
	world_seed = p_seed if p_seed != 0 else randi()
	world_name = p_name

func _ready():
	_setup_containers()
	_connect_signals()
	initialize_world()
	set_process(true)

func _setup_containers() -> void:
	# Container for chunks
	chunk_container = Node2D.new()
	chunk_container.name = "Chunks"
	add_child(chunk_container)

	# Create tile renderer
	tile_renderer = TileRenderer.new()
	tile_renderer.name = "TileRenderer"
	add_child(tile_renderer)

	# Create chunk manager
	chunk_manager = ChunkManager.new(self)
	chunk_manager.name = "ChunkManager"
	add_child(chunk_manager)

	# Container for party visuals
	party_container = Node2D.new()
	party_container.name = "Party"
	party_container.z_index = 10
	add_child(party_container)

	# Container for effects (particles, etc)
	effect_container = Node2D.new()
	effect_container.name = "Effects"
	effect_container.z_index = 20
	add_child(effect_container)

	_setup_party_visuals()

func _setup_party_visuals() -> void:
	# Create CharacterDisplay nodes for each party member
	# Higher slot numbers render on top (reverse z-index)
	var display_size = WorldConstants.TILE_SIZE
	for i in range(WorldConstants.MAX_PARTY_SIZE):
		var char_display = CharacterDisplay.new()
		char_display.name = "PartyMember%d" % i
		char_display.z_index = 10 + (WorldConstants.MAX_PARTY_SIZE - i)  # Reverse order
		char_display.visible = false
		char_display.custom_minimum_size = Vector2(display_size, display_size)
		char_display.size = Vector2(display_size, display_size)
		party_container.add_child(char_display)

func _connect_signals() -> void:
	# Connect to party event bus signals
	EventBus.party_moved.connect(_on_party_moved)
	EventBus.party_teleported.connect(_on_party_teleported)
	EventBus.party_member_added.connect(_on_party_member_added)
	EventBus.party_member_removed.connect(_on_party_member_removed)

	# Connect to world event bus signals
	EventBus.world_saved.connect(_on_world_saved)
	EventBus.debug_teleport.connect(_on_debug_teleport)
	EventBus.debug_reveal_map.connect(_on_debug_reveal_map)

func initialize_world() -> void:
	if is_initialized:
		return

	print("Initializing world: %s (seed: %d)" % [world_name, world_seed])

	# Initialize random with world seed
	var rng = RandomNumberGenerator.new()
	rng.seed = world_seed

	# Generate biome seed points
	_generate_biome_seeds(rng)

	# Set initial party position (center of world)
	var initial_pos = Vector2i(world_size.x / 2, world_size.y / 2)
	party_position = _find_valid_spawn_position(initial_pos)

	# Initialize party member positions
	_initialize_party_positions()

	# Load initial chunks around party
	var party_chunk = WorldCoordinate.world_to_chunk(party_position)
	chunk_manager.update_loaded_chunks(party_chunk)

	# Calculate initial visibility
	_update_visibility()

	# Force re-render chunks with updated visibility
	for chunk in chunk_manager.loaded_chunks.values():
		tile_renderer.render_chunk(chunk)

	# Update party visuals
	_update_party_visuals()

	is_initialized = true
	EventBus.world_created.emit(world_name, world_seed)

func _find_valid_spawn_position(start_pos: Vector2i, max_search_radius: int = 50) -> Vector2i:
	# First, ensure chunks are loaded around the start position
	var start_chunk = WorldCoordinate.world_to_chunk(start_pos)
	chunk_manager.update_loaded_chunks(start_chunk)

	# Check if start position is valid
	if _is_valid_spawn_tile(start_pos):
		return start_pos

	# Search in expanding square spiral pattern
	for radius in range(1, max_search_radius + 1):
		# Check tiles in a square ring at this radius
		for dx in range(-radius, radius + 1):
			for dy in range(-radius, radius + 1):
				# Only check the perimeter of the square (not interior)
				if abs(dx) != radius and abs(dy) != radius:
					continue

				var check_pos = start_pos + Vector2i(dx, dy)

				# Load chunk if needed
				var check_chunk = WorldCoordinate.world_to_chunk(check_pos)
				if check_chunk != start_chunk:
					chunk_manager.get_chunk(check_chunk)  # Ensure chunk is loaded

				if _is_valid_spawn_tile(check_pos):
					print("Found valid spawn position at %v (offset %v from center)" % [check_pos, Vector2i(dx, dy)])
					return check_pos

	# Fallback: return start position even if invalid (shouldn't happen)
	print("WARNING: Could not find valid spawn position, using %v" % start_pos)
	return start_pos

func _is_valid_spawn_tile(pos: Vector2i) -> bool:
	if not WorldCoordinate.is_valid_position(pos):
		return false

	# Check if tile is passable
	var tile = get_tile(pos)
	if not tile or not tile.is_passable:
		return false

	# Check that at least 2 cardinal neighbors are passable (so player can move)
	var passable_neighbors = 0
	var cardinal_directions = [
		Vector2i(0, -1),  # North
		Vector2i(1, 0),   # East
		Vector2i(0, 1),   # South
		Vector2i(-1, 0)   # West
	]

	for direction in cardinal_directions:
		var neighbor_pos = pos + direction
		var neighbor_tile = get_tile(neighbor_pos)
		if neighbor_tile and neighbor_tile.is_passable:
			passable_neighbors += 1

	# Require at least 2 passable neighbors to ensure player isn't stuck
	return passable_neighbors >= 2

func _generate_biome_seeds(rng: RandomNumberGenerator) -> void:
	# Generate biome seed points across the world
	var num_biomes = (world_size.x * world_size.y) / (300 * 300)  # One biome per 300x300 area

	for i in range(num_biomes):
		var x = rng.randi_range(0, world_size.x - 1)
		var y = rng.randi_range(0, world_size.y - 1)
		var biome_type = rng.randi() % WorldConstants.BiomeType.size()
		biome_seed_points[Vector2i(x, y)] = biome_type

func get_tile(world_pos: Vector2i) -> Tile:
	if not WorldCoordinate.is_valid_position(world_pos):
		return null

	var chunk_pos = WorldCoordinate.world_to_chunk(world_pos)
	var chunk = chunk_manager.get_chunk(chunk_pos)

	if chunk:
		return chunk.get_tile_world(world_pos.x, world_pos.y)

	return null

func set_tile(world_pos: Vector2i, tile: Tile) -> void:
	if not WorldCoordinate.is_valid_position(world_pos):
		return

	var chunk_pos = WorldCoordinate.world_to_chunk(world_pos)
	var chunk = chunk_manager.get_chunk(chunk_pos)

	if chunk:
		chunk.set_tile_world(world_pos.x, world_pos.y, tile)
		tile_renderer.render_tile(world_pos, tile)
		EventBus.tile_modified.emit(world_pos, tile)

func get_chunk(chunk_pos: Vector2i) -> Chunk:
	return chunk_manager.get_chunk(chunk_pos)

func get_biome_at_position(pos: Vector2i) -> int:
	# Find nearest biome seed point
	var nearest_biome = WorldConstants.BiomeType.PLAINS
	var nearest_distance = INF

	for seed_pos in biome_seed_points:
		var distance = WorldCoordinate.euclidean_distance(pos, seed_pos)
		if distance < nearest_distance:
			nearest_distance = distance
			nearest_biome = biome_seed_points[seed_pos]

	return nearest_biome

func _update_loaded_chunks() -> void:
	var party_chunk = WorldCoordinate.world_to_chunk(party_position)
	chunk_manager.update_loaded_chunks(party_chunk)

func _update_visibility() -> void:
	visible_tiles.clear()

	# Calculate visible tiles in radius around party
	visible_tiles = WorldCoordinate.get_tiles_in_radius(party_position, WorldConstants.VISIBILITY_RADIUS)

	# Update revealed tiles (all previously visible tiles)
	for tile_pos in visible_tiles:
		if tile_pos not in revealed_tiles:
			revealed_tiles.append(tile_pos)
			var tile = get_tile(tile_pos)
			if tile:
				tile.reveal()

	# Update visibility in chunks
	for chunk in chunk_manager.loaded_chunks.values():
		chunk.update_visibility(visible_tiles)
		# Re-render chunk with new visibility
		tile_renderer.render_chunk(chunk)

	# Update fog of war rendering
	tile_renderer.update_visibility_batch(visible_tiles, revealed_tiles)

	EventBus.visibility_updated.emit(visible_tiles)

func _process(delta: float) -> void:
	if is_party_moving:
		_update_movement_animation(delta)

func _initialize_party_positions() -> void:
	party_member_positions.clear()
	party_member_trail.clear()
	party_member_facing.clear()
	party_member_hop_offsets.clear()
	party_member_hop_speeds.clear()

	if not PlayerData or not PlayerData.party:
		return

	var party_size = PlayerData.party.members.size()

	# Initialize all members at leader position, facing right
	for i in range(party_size):
		party_member_positions.append(party_position)
		party_member_facing.append(true)  # Start facing right

		# Add slight phase offset (staggered by 15% of a hop cycle)
		var phase_offset = (i * 0.15) * PI * 2.0
		party_member_hop_offsets.append(phase_offset)

		# Add tiny random speed variation (±5% of base frequency)
		var speed_variance = randf_range(0.95, 1.05)
		party_member_hop_speeds.append(speed_variance)

	# Add initial position to trail
	party_member_trail.append(party_position)

func move_party(new_position: Vector2i) -> bool:
	if is_party_moving:
		return false  # Can't move while already moving

	if not WorldCoordinate.is_valid_position(new_position):
		return false

	var tile = get_tile(new_position)
	if not tile or not tile.is_passable:
		return false

	# Start movement animation
	var old_position = party_position
	party_position = new_position

	# Update trail for followers
	party_member_trail.insert(0, new_position)
	if party_member_trail.size() > WorldConstants.MAX_PARTY_SIZE:
		party_member_trail.resize(WorldConstants.MAX_PARTY_SIZE)

	# Start animation
	is_party_moving = true
	movement_progress = 0.0

	print("New movement starting: from pos[0]=%s to trail[0]=%s, progress=%.2f" % [party_member_positions[0] if party_member_positions.size() > 0 else "none", party_member_trail[0] if party_member_trail.size() > 0 else "none", movement_progress])

	# Update visuals immediately to prevent one-frame snap
	_update_party_visuals_animated()

	# Update chunks if needed
	_update_loaded_chunks()
	_update_visibility()

	# Check for location entry
	if tile.has_location():
		EventBus.location_entered.emit(tile.location_data)

	return true

func _update_movement_animation(delta: float) -> void:
	movement_progress += delta / movement_duration

	if movement_progress >= 1.0:
		# Animation complete
		print("Movement complete: progress=%.2f, pos[0]=%s, trail[0]=%s" % [movement_progress, party_member_positions[0] if party_member_positions.size() > 0 else "none", party_member_trail[0] if party_member_trail.size() > 0 else "none"])
		movement_progress = 0.0
		is_party_moving = false

		# Update final positions FIRST before signal
		for i in range(party_member_positions.size()):
			if i < party_member_trail.size():
				party_member_positions[i] = party_member_trail[i]

		print("After position update: pos[0]=%s" % (party_member_positions[0] if party_member_positions.size() > 0 else "none"))

		# Emit signal for immediate continuation of movement
		movement_completed.emit()
		print("After signal: is_party_moving=%s, progress=%.2f" % [is_party_moving, movement_progress])

	# Update visual positions with animation (always call, even after completion)
	_update_party_visuals_animated()

func _update_party_visuals() -> void:
	if not PlayerData or not PlayerData.party:
		return

	var party_members = PlayerData.party.members

	for i in range(WorldConstants.MAX_PARTY_SIZE):
		var char_display = party_container.get_node("PartyMember%d" % i) as CharacterDisplay
		if i < party_members.size():
			var character = party_members[i]
			char_display.set_character(character)
			char_display.visible = true

			# Use stored position for this member
			var member_world_pos = party_member_positions[i] if i < party_member_positions.size() else party_position
			var pixel_pos = WorldConstants.tile_to_pixel(member_world_pos)

			# Position character at bottom-center of tile
			var half_tile = WorldConstants.TILE_SIZE / 2
			var vertical_offset = WorldConstants.TILE_SIZE * CHARACTER_TILE_OFFSET  # Move down to bottom third of tile
			char_display.position = pixel_pos - Vector2(half_tile, half_tile - vertical_offset)
		else:
			char_display.visible = false

func _update_party_visuals_animated() -> void:
	if not PlayerData or not PlayerData.party:
		return

	var party_members = PlayerData.party.members
	var t = movement_progress  # 0.0 to 1.0

	for i in range(WorldConstants.MAX_PARTY_SIZE):
		var char_display = party_container.get_node("PartyMember%d" % i) as CharacterDisplay
		if i < party_members.size() and i < party_member_positions.size():
			# Get start and end positions for this member
			var start_pos = party_member_positions[i]
			var end_pos = party_member_trail[i] if i < party_member_trail.size() else start_pos

			# Get this character's hop timing
			var phase_offset = party_member_hop_offsets[i] if i < party_member_hop_offsets.size() else 0.0
			var speed_variance = party_member_hop_speeds[i] if i < party_member_hop_speeds.size() else 1.0

			# Calculate continuous hopping with per-character variation
			var hop_progress = (t * hop_frequency * speed_variance) + (phase_offset / (PI * 2.0))
			var hop_offset = abs(sin(hop_progress * PI)) * hop_height

			# Linear interpolation for sliding movement
			var lerp_x = lerp(float(start_pos.x), float(end_pos.x), t)
			var lerp_y = lerp(float(start_pos.y), float(end_pos.y), t)

			var pixel_pos = Vector2(
				lerp_x * WorldConstants.TILE_SIZE,
				lerp_y * WorldConstants.TILE_SIZE
			)

			# Apply hop offset and position at bottom of tile
			var half_tile = WorldConstants.TILE_SIZE / 2
			var vertical_offset = WorldConstants.TILE_SIZE * CHARACTER_TILE_OFFSET # Move down to bottom third of tile
			char_display.position = pixel_pos - Vector2(half_tile, half_tile - vertical_offset + hop_offset)

			# Update horizontal flip based on movement direction
			_update_character_flip(char_display, i, start_pos, end_pos)
		else:
			char_display.visible = false

func _update_character_flip(char_display: CharacterDisplay, member_index: int, start_pos: Vector2i, end_pos: Vector2i) -> void:
	if member_index >= party_member_facing.size():
		return

	var movement_dir = end_pos.x - start_pos.x

	# Only update facing if there's actual horizontal movement
	if movement_dir < 0:  # Moving left
		# Face left
		char_display.set_facing_left(true)
		party_member_facing[member_index] = false
	elif movement_dir > 0:  # Moving right
		# Face right
		char_display.set_facing_left(false)
		party_member_facing[member_index] = true
	# If movement_dir == 0, maintain current facing direction

# Signal handlers
func _on_party_moved(from: Vector2i, to: Vector2i) -> void:
	move_party(to)
	_update_party_visuals()

func _on_party_teleported(to: Vector2i) -> void:
	party_position = to
	_update_loaded_chunks()
	_update_visibility()
	_update_party_visuals()

func _on_party_member_added(_character, _position: int) -> void:
	_update_party_visuals()

func _on_party_member_removed(_character) -> void:
	_update_party_visuals()

func _on_world_saved(_world_name: String) -> void:
	print("World saved: %s" % _world_name)

func _on_debug_teleport(tile_pos: Vector2i) -> void:
	if WorldConstants.DEBUG_MODE:
		party_position = tile_pos
		_update_loaded_chunks()
		_update_visibility()
		print("Debug teleported to: %v" % tile_pos)

func _on_debug_reveal_map(radius: int) -> void:
	if WorldConstants.DEBUG_MODE:
		for chunk in chunk_manager.loaded_chunks.values():
			chunk.reveal_area(party_position, radius)
			tile_renderer.render_chunk(chunk)
		print("Revealed map with radius: %d" % radius)

# Save/Load functionality
func serialize() -> Dictionary:
	var chunk_data = []
	for chunk_pos in chunk_manager.loaded_chunks:
		var chunk = chunk_manager.loaded_chunks[chunk_pos]
		if chunk.is_modified:
			chunk_data.append(chunk.serialize())

	return {
		"world_name": world_name,
		"world_seed": world_seed,
		"world_size": {"x": world_size.x, "y": world_size.y},
		"party_position": {"x": party_position.x, "y": party_position.y},
		"modified_chunks": chunk_data,
		"biome_seed_points": biome_seed_points,
		"revealed_tiles": revealed_tiles
	}

func deserialize(data: Dictionary) -> void:
	world_name = data.get("world_name", "Unknown World")
	world_seed = data.get("world_seed", 0)

	var size = data.get("world_size", {"x": 10000, "y": 10000})
	world_size = Vector2i(size["x"], size["y"])

	var pos = data.get("party_position", {"x": 5000, "y": 5000})
	party_position = Vector2i(pos["x"], pos["y"])

	biome_seed_points = data.get("biome_seed_points", {})
	revealed_tiles = data.get("revealed_tiles", [])

	# Load modified chunks
	# ChunkManager will handle loading these on demand
	# Store them for later retrieval
	# TODO: Implement chunk cache loading in ChunkManager

	_update_loaded_chunks()
	_update_visibility()
