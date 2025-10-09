extends Node2D
class_name WorldMap

# World properties
var world_seed: int = 0
var world_size: Vector2i = Vector2i(WorldConstants.WORLD_SIZE, WorldConstants.WORLD_SIZE)
var world_name: String = "New World"

# Chunk management
var loaded_chunks: Dictionary = {}  # chunk_position -> Chunk
var chunk_cache: Array[Chunk] = []  # LRU cache
var chunk_generation_queue: Array[Vector2i] = []

# Party tracking
var party_position: Vector2i = Vector2i.ZERO
var visible_tiles: Array[Vector2i] = []

# Generation parameters
var location_density: float = WorldConstants.LOCATION_DENSITY
var biome_seed_points: Dictionary = {}  # position -> biome_type

# Components
var chunk_container: Node2D
var party_container: Node2D
var effect_container: Node2D

# Thread for async operations
var generation_thread: Thread = null

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
	_update_loaded_chunks()

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
	if not WorldConstants.is_valid_world_position(world_pos):
		return null

	var chunk_pos = WorldConstants.world_to_chunk(world_pos)
	var chunk = get_chunk(chunk_pos)

	if chunk:
		return chunk.get_tile_world(world_pos.x, world_pos.y)

	return null

func set_tile(world_pos: Vector2i, tile: Tile) -> void:
	if not WorldConstants.is_valid_world_position(world_pos):
		return

	var chunk_pos = WorldConstants.world_to_chunk(world_pos)
	var chunk = get_or_create_chunk(chunk_pos)

	if chunk:
		chunk.set_tile_world(world_pos.x, world_pos.y, tile)
		BusWorld.tile_modified.emit(world_pos, tile)

func get_chunk(chunk_pos: Vector2i) -> Chunk:
	return loaded_chunks.get(chunk_pos, null)

func get_or_create_chunk(chunk_pos: Vector2i) -> Chunk:
	var chunk = get_chunk(chunk_pos)

	if not chunk:
		chunk = _generate_chunk(chunk_pos)
		_add_chunk(chunk)

	return chunk

func _generate_chunk(chunk_pos: Vector2i) -> Chunk:
	var chunk = Chunk.new(chunk_pos)

	# Generate tiles for this chunk
	var world_pos = WorldConstants.chunk_to_world(chunk_pos)
	var rng = RandomNumberGenerator.new()
	rng.seed = hash(Vector3i(world_seed, chunk_pos.x, chunk_pos.y))

	# Find nearest biome
	var biome_type = _get_biome_at_position(world_pos)
	chunk.dominant_biome = biome_type

	# Generate tiles based on biome
	for x in range(WorldConstants.CHUNK_SIZE):
		for y in range(WorldConstants.CHUNK_SIZE):
			var tile_world_pos = Vector2i(world_pos.x + x, world_pos.y + y)
			var tile = _generate_tile(tile_world_pos, biome_type, rng)
			chunk.set_tile(x, y, tile)

	# Check for location spawning
	_check_location_spawn(chunk, rng)

	return chunk

func _get_biome_at_position(pos: Vector2i) -> WorldConstants.BiomeType:
	# Find nearest biome seed point
	var nearest_biome = WorldConstants.BiomeType.PLAINS
	var nearest_distance = INF

	for seed_pos in biome_seed_points:
		var distance = WorldConstants.euclidean_distance(pos, seed_pos)
		if distance < nearest_distance:
			nearest_distance = distance
			nearest_biome = biome_seed_points[seed_pos]

	return nearest_biome

func _generate_tile(world_pos: Vector2i, biome: WorldConstants.BiomeType, rng: RandomNumberGenerator) -> Tile:
	var tile = Tile.new()

	# Use Perlin noise for terrain features
	var noise_value = _get_terrain_noise(world_pos)

	# Apply biome-specific rules
	match biome:
		WorldConstants.BiomeType.PLAINS:
			if noise_value > 0.7:
				tile.type = WorldConstants.TileType.TREE
				tile.is_passable = false
			elif noise_value > 0.95:
				tile.type = WorldConstants.TileType.MOUNTAIN
				tile.is_passable = false
			else:
				tile.type = WorldConstants.TileType.GROUND
		WorldConstants.BiomeType.FOREST:
			if noise_value > 0.3:
				tile.type = WorldConstants.TileType.TREE
				tile.is_passable = false
			elif noise_value > 0.9:
				tile.type = WorldConstants.TileType.MOUNTAIN
				tile.is_passable = false
			else:
				tile.type = WorldConstants.TileType.GROUND
		WorldConstants.BiomeType.MOUNTAINS:
			if noise_value > 0.4:
				tile.type = WorldConstants.TileType.MOUNTAIN
				tile.is_passable = false
			elif noise_value > 0.8:
				tile.type = WorldConstants.TileType.TREE
				tile.is_passable = false
			else:
				tile.type = WorldConstants.TileType.GROUND
		WorldConstants.BiomeType.DESERT:
			if noise_value > 0.95:
				tile.type = WorldConstants.TileType.MOUNTAIN
				tile.is_passable = false
			elif noise_value > 0.85:
				tile.type = WorldConstants.TileType.TREE
				tile.is_passable = false
			else:
				tile.type = WorldConstants.TileType.GROUND
		WorldConstants.BiomeType.SWAMP:
			if noise_value > 0.7:
				tile.type = WorldConstants.TileType.TREE
				tile.is_passable = false
			elif noise_value > 0.5:
				tile.type = WorldConstants.TileType.WATER
				tile.is_passable = false
			else:
				tile.type = WorldConstants.TileType.GROUND
		_:
			tile.type = WorldConstants.TileType.GROUND

	tile.biome_id = WorldConstants.BiomeType.keys()[biome]
	tile.elevation = noise_value
	return tile

func _get_terrain_noise(world_pos: Vector2i) -> float:
	# Simple noise generation - would be replaced with proper Perlin/Simplex noise
	var x = world_pos.x * 0.1
	var y = world_pos.y * 0.1
	return abs(sin(x) * cos(y) + sin(x * 0.5) * cos(y * 0.5))

func _check_location_spawn(chunk: Chunk, rng: RandomNumberGenerator) -> void:
	# Check if we should spawn a location in this chunk
	if rng.randf() < location_density * WorldConstants.CHUNK_SIZE * WorldConstants.CHUNK_SIZE:
		var local_x = rng.randi_range(2, WorldConstants.CHUNK_SIZE - 3)
		var local_y = rng.randi_range(2, WorldConstants.CHUNK_SIZE - 3)
		var world_pos = chunk.world_position + Vector2i(local_x, local_y)

		# Check accessibility
		if _is_location_accessible(world_pos):
			_spawn_location(chunk, world_pos, rng)

func _is_location_accessible(world_pos: Vector2i) -> bool:
	# Check if location has at least one accessible neighbor
	var neighbors = [
		Vector2i(0, 1), Vector2i(0, -1),
		Vector2i(1, 0), Vector2i(-1, 0)
	]

	for offset in neighbors:
		var check_pos = world_pos + offset
		var tile = get_tile(check_pos)
		if tile and tile.is_passable:
			return true

	return false

func _spawn_location(chunk: Chunk, world_pos: Vector2i, rng: RandomNumberGenerator) -> void:
	var location = LocationData.new()
	location.name = _generate_location_name(chunk.dominant_biome, rng)
	location.type = _choose_location_type(chunk.dominant_biome, rng)
	location.world_position = world_pos
	location.biome_type = chunk.dominant_biome
	location.generation_seed = rng.seed

	# Set the tile as a location
	var tile = get_tile(world_pos)
	if tile:
		tile.set_location(location)
		chunk.add_location(location)
		BusWorld.location_spawned.emit(world_pos, location)

func _generate_location_name(biome: WorldConstants.BiomeType, rng: RandomNumberGenerator) -> String:
	# Generate procedural location names
	var prefixes = ["North", "South", "East", "West", "Old", "New", "Great", "Lesser"]
	var suffixes = ["haven", "shire", "ford", "bridge", "hill", "vale", "wood", "field"]

	var prefix = prefixes[rng.randi() % prefixes.size()]
	var suffix = suffixes[rng.randi() % suffixes.size()]

	return "%s%s" % [prefix, suffix]

func _choose_location_type(biome: WorldConstants.BiomeType, rng: RandomNumberGenerator) -> WorldConstants.LocationType:
	# Choose location type based on biome
	match biome:
		WorldConstants.BiomeType.PLAINS:
			var types = [
				WorldConstants.LocationType.VILLAGE,
				WorldConstants.LocationType.CAMP,
				WorldConstants.LocationType.SHRINE
			]
			return types[rng.randi() % types.size()]
		WorldConstants.BiomeType.FOREST:
			var types = [
				WorldConstants.LocationType.CAMP,
				WorldConstants.LocationType.SHRINE,
				WorldConstants.LocationType.RUIN
			]
			return types[rng.randi() % types.size()]
		WorldConstants.BiomeType.MOUNTAINS:
			var types = [
				WorldConstants.LocationType.CAVE,
				WorldConstants.LocationType.DUNGEON,
				WorldConstants.LocationType.CASTLE
			]
			return types[rng.randi() % types.size()]
		_:
			return WorldConstants.LocationType.VILLAGE

func _add_chunk(chunk: Chunk) -> void:
	loaded_chunks[chunk.chunk_position] = chunk
	chunk_container.add_child(chunk)
	chunk.position = WorldConstants.tile_to_pixel(chunk.world_position)
	chunk.load_chunk()

	# Manage cache
	_manage_chunk_cache(chunk)

func _remove_chunk(chunk_pos: Vector2i) -> void:
	if chunk_pos in loaded_chunks:
		var chunk = loaded_chunks[chunk_pos]
		chunk.unload_chunk()
		chunk_container.remove_child(chunk)
		loaded_chunks.erase(chunk_pos)

func _manage_chunk_cache(new_chunk: Chunk) -> void:
	chunk_cache.append(new_chunk)

	# Remove oldest chunks if cache is full
	while chunk_cache.size() > WorldConstants.CHUNK_CACHE_SIZE:
		var oldest = chunk_cache.pop_front()
		if oldest and oldest.chunk_position not in loaded_chunks:
			oldest.queue_free()

func _update_loaded_chunks() -> void:
	var party_chunk = WorldConstants.world_to_chunk(party_position)

	# Load chunks within radius
	var chunks_to_load: Array[Vector2i] = []
	for dx in range(-WorldConstants.CHUNK_LOAD_RADIUS, WorldConstants.CHUNK_LOAD_RADIUS + 1):
		for dy in range(-WorldConstants.CHUNK_LOAD_RADIUS, WorldConstants.CHUNK_LOAD_RADIUS + 1):
			var chunk_pos = party_chunk + Vector2i(dx, dy)
			if chunk_pos not in loaded_chunks:
				chunks_to_load.append(chunk_pos)

	# Load new chunks
	for chunk_pos in chunks_to_load:
		get_or_create_chunk(chunk_pos)

	# Unload distant chunks
	var chunks_to_unload: Array[Vector2i] = []
	for chunk_pos in loaded_chunks:
		var distance = WorldConstants.manhattan_distance(chunk_pos, party_chunk)
		if distance > WorldConstants.CHUNK_UNLOAD_RADIUS:
			chunks_to_unload.append(chunk_pos)

	for chunk_pos in chunks_to_unload:
		_remove_chunk(chunk_pos)

func _update_visibility() -> void:
	visible_tiles.clear()

	# Calculate visible tiles in radius around party
	for dx in range(-WorldConstants.VISIBILITY_RADIUS, WorldConstants.VISIBILITY_RADIUS + 1):
		for dy in range(-WorldConstants.VISIBILITY_RADIUS, WorldConstants.VISIBILITY_RADIUS + 1):
			var tile_pos = party_position + Vector2i(dx, dy)
			if WorldConstants.euclidean_distance(tile_pos, party_position) <= WorldConstants.VISIBILITY_RADIUS:
				if WorldConstants.is_valid_world_position(tile_pos):
					visible_tiles.append(tile_pos)

	# Update chunks with visibility
	for chunk in loaded_chunks.values():
		chunk.update_visibility(visible_tiles)

	BusWorld.visibility_updated.emit(visible_tiles)

func move_party(new_position: Vector2i) -> bool:
	if not WorldConstants.is_valid_world_position(new_position):
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
		for chunk in loaded_chunks.values():
			chunk.reveal_area(party_position, radius)
		print("Revealed map with radius: %d" % radius)

# Save/Load functionality
func serialize() -> Dictionary:
	var chunk_data = []
	for chunk_pos in loaded_chunks:
		var chunk = loaded_chunks[chunk_pos]
		if chunk.is_modified:
			chunk_data.append(chunk.serialize())

	return {
		"world_name": world_name,
		"world_seed": world_seed,
		"world_size": {"x": world_size.x, "y": world_size.y},
		"party_position": {"x": party_position.x, "y": party_position.y},
		"modified_chunks": chunk_data,
		"biome_seed_points": biome_seed_points
	}

func deserialize(data: Dictionary) -> void:
	world_name = data.get("world_name", "Unknown World")
	world_seed = data.get("world_seed", 0)

	var size = data.get("world_size", {"x": 10000, "y": 10000})
	world_size = Vector2i(size["x"], size["y"])

	var pos = data.get("party_position", {"x": 5000, "y": 5000})
	party_position = Vector2i(pos["x"], pos["y"])

	biome_seed_points = data.get("biome_seed_points", {})

	# Load modified chunks
	for chunk_data in data.get("modified_chunks", []):
		var chunk = Chunk.new()
		chunk.deserialize(chunk_data)
		_add_chunk(chunk)

	_update_loaded_chunks()
	_update_visibility()