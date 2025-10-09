extends Node2D
class_name WorldMap

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

func _connect_signals() -> void:
	# Connect to party bus signals
	BusParty.party_moved.connect(_on_party_moved)
	BusParty.party_teleported.connect(_on_party_teleported)

	# Connect to world bus signals
	BusWorld.world_saved.connect(_on_world_saved)
	BusWorld.debug_teleport.connect(_on_debug_teleport)
	BusWorld.debug_reveal_map.connect(_on_debug_reveal_map)

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
	party_position = Vector2i(world_size.x / 2, world_size.y / 2)

	# Load initial chunks around party
	var party_chunk = WorldCoordinate.world_to_chunk(party_position)
	chunk_manager.update_loaded_chunks(party_chunk)

	# Calculate initial visibility
	_update_visibility()

	is_initialized = true
	BusWorld.world_created.emit(world_name, world_seed)

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
		BusWorld.tile_modified.emit(world_pos, tile)

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

	BusWorld.visibility_updated.emit(visible_tiles)

func move_party(new_position: Vector2i) -> bool:
	if not WorldCoordinate.is_valid_position(new_position):
		return false

	var tile = get_tile(new_position)
	if not tile or not tile.is_passable:
		return false

	var old_position = party_position
	party_position = new_position

	# Update chunks if needed
	_update_loaded_chunks()
	_update_visibility()

	# Check for location entry
	if tile.has_location():
		BusWorld.location_entered.emit(tile.location_data)

	return true

# Signal handlers
func _on_party_moved(from: Vector2i, to: Vector2i) -> void:
	move_party(to)

func _on_party_teleported(to: Vector2i) -> void:
	party_position = to
	_update_loaded_chunks()
	_update_visibility()

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