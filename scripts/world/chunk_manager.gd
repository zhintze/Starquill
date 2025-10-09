extends Node
class_name ChunkManager

# Chunk loading/unloading management with LRU cache

# Active chunks currently loaded
var loaded_chunks: Dictionary = {}  # chunk_pos -> Chunk

# LRU cache for recently used chunks
var chunk_cache: Array[Chunk] = []
var cache_lookup: Dictionary = {}  # chunk_pos -> cache index

# Generation queue for async chunk generation
var generation_queue: Array[Vector2i] = []
var is_generating: bool = false

# Thread pool for async operations
var thread_pool: Array[Thread] = []
var max_threads: int = 4
var active_threads: int = 0

# Reference to world map
var world_map: WorldMap

# Performance tracking
var chunks_generated_count: int = 0
var chunks_loaded_from_cache: int = 0
var generation_time_total: float = 0.0

# Signals
signal chunk_generation_started(chunk_pos: Vector2i)
signal chunk_generation_completed(chunk_pos: Vector2i, chunk: Chunk)
signal chunk_load_completed(chunk_pos: Vector2i)
signal chunk_unload_completed(chunk_pos: Vector2i)

func _init(p_world_map: WorldMap):
	world_map = p_world_map
	_initialize_thread_pool()

func _ready():
	set_process(true)

func _initialize_thread_pool() -> void:
	for i in range(max_threads):
		var thread = Thread.new()
		thread_pool.append(thread)

func _exit_tree():
	# Clean up threads
	for thread in thread_pool:
		if thread.is_started():
			thread.wait_to_finish()

func update_loaded_chunks(center_chunk: Vector2i) -> void:
	var chunks_to_load = WorldCoordinate.get_chunks_in_radius(center_chunk, WorldConstants.CHUNK_LOAD_RADIUS)
	var chunks_to_check_unload = loaded_chunks.keys()

	# Load new chunks
	for chunk_pos in chunks_to_load:
		if chunk_pos not in loaded_chunks:
			_request_chunk(chunk_pos)

	# Unload distant chunks
	for chunk_pos in chunks_to_check_unload:
		var distance = WorldCoordinate.chebyshev_distance(chunk_pos, center_chunk)
		if distance > WorldConstants.CHUNK_UNLOAD_RADIUS:
			_unload_chunk(chunk_pos)

func get_chunk(chunk_pos: Vector2i) -> Chunk:
	# Check if already loaded
	if chunk_pos in loaded_chunks:
		_update_cache_access(loaded_chunks[chunk_pos])
		return loaded_chunks[chunk_pos]

	# Check cache
	if chunk_pos in cache_lookup:
		var chunk = _get_from_cache(chunk_pos)
		if chunk:
			chunks_loaded_from_cache += 1
			_load_chunk(chunk)
			return chunk

	return null

func _request_chunk(chunk_pos: Vector2i) -> void:
	if chunk_pos in loaded_chunks:
		return

	# Add to generation queue
	if chunk_pos not in generation_queue:
		generation_queue.append(chunk_pos)

	# Process queue if not already generating
	if not is_generating:
		_process_generation_queue()

func _process_generation_queue() -> void:
	if generation_queue.is_empty():
		is_generating = false
		return

	if active_threads >= max_threads:
		return  # Wait for a thread to become available

	is_generating = true
	var chunk_pos = generation_queue.pop_front()

	# Prepare data for thread
	var thread_data = {
		"chunk_pos": chunk_pos,
		"world_seed": world_map.world_seed,
		"location_density": world_map.location_density,
		"biome_seed_points": world_map.biome_seed_points.duplicate()
	}

	# Find available thread
	for thread in thread_pool:
		if not thread.is_started() or not thread.is_alive():
			if thread.is_started():
				thread.wait_to_finish()

			active_threads += 1
			chunk_generation_started.emit(chunk_pos)
			thread.start(_generate_chunk_threaded.bind(thread_data))
			break

func _generate_chunk_threaded(thread_data: Dictionary) -> Chunk:
	var start_time = Time.get_ticks_msec()
	var chunk_pos = thread_data["chunk_pos"]
	var world_seed = thread_data["world_seed"]
	var location_density = thread_data["location_density"]
	var biome_seed_points = thread_data["biome_seed_points"]

	# Generate chunk data
	var chunk = Chunk.new(chunk_pos)
	var world_pos = WorldCoordinate.chunk_to_world(chunk_pos)
	var rng = RandomNumberGenerator.new()
	rng.seed = hash(Vector3i(world_seed, chunk_pos.x, chunk_pos.y))

	# Get biome for this chunk
	var biome_type = _determine_chunk_biome(world_pos, biome_seed_points, rng)
	chunk.dominant_biome = biome_type

	# Generate tiles
	for x in range(WorldConstants.CHUNK_SIZE):
		for y in range(WorldConstants.CHUNK_SIZE):
			var tile_world_pos = Vector2i(world_pos.x + x, world_pos.y + y)
			var tile = _generate_tile(tile_world_pos, biome_type, world_seed, rng)
			chunk.set_tile(x, y, tile)

	# Check for location spawning
	_check_chunk_locations(chunk, location_density, rng)

	var generation_time = (Time.get_ticks_msec() - start_time) / 1000.0
	generation_time_total += generation_time
	chunks_generated_count += 1

	# Call deferred to return to main thread
	call_deferred("_on_chunk_generated", chunk_pos, chunk)

	return chunk

func _on_chunk_generated(chunk_pos: Vector2i, chunk: Chunk) -> void:
	active_threads -= 1

	# Add to loaded chunks
	_load_chunk(chunk)
	chunk_generation_completed.emit(chunk_pos, chunk)

	# Continue processing queue
	if not generation_queue.is_empty():
		_process_generation_queue()
	else:
		is_generating = false

func _load_chunk(chunk: Chunk) -> void:
	if chunk.chunk_position in loaded_chunks:
		return

	loaded_chunks[chunk.chunk_position] = chunk
	world_map.chunk_container.add_child(chunk)
	chunk.position = WorldCoordinate.tile_to_pixel(chunk.world_position)
	chunk.load_chunk()

	# Render chunk tiles
	if world_map.tile_renderer:
		world_map.tile_renderer.render_chunk(chunk)

	_add_to_cache(chunk)
	chunk_load_completed.emit(chunk.chunk_position)

func _unload_chunk(chunk_pos: Vector2i) -> void:
	if chunk_pos not in loaded_chunks:
		return

	var chunk = loaded_chunks[chunk_pos]
	chunk.unload_chunk()
	world_map.chunk_container.remove_child(chunk)
	loaded_chunks.erase(chunk_pos)

	chunk_unload_completed.emit(chunk_pos)

func _add_to_cache(chunk: Chunk) -> void:
	# Remove if already in cache (to re-add at end)
	if chunk.chunk_position in cache_lookup:
		var index = cache_lookup[chunk.chunk_position]
		chunk_cache.remove_at(index)
		cache_lookup.erase(chunk.chunk_position)
		_rebuild_cache_lookup()

	# Add to end of cache
	chunk_cache.append(chunk)
	cache_lookup[chunk.chunk_position] = chunk_cache.size() - 1

	# Trim cache if needed
	while chunk_cache.size() > WorldConstants.CHUNK_CACHE_SIZE:
		var old_chunk = chunk_cache.pop_front()
		cache_lookup.erase(old_chunk.chunk_position)
		if old_chunk.chunk_position not in loaded_chunks:
			old_chunk.queue_free()
		_rebuild_cache_lookup()

func _get_from_cache(chunk_pos: Vector2i) -> Chunk:
	if chunk_pos not in cache_lookup:
		return null

	var index = cache_lookup[chunk_pos]
	if index >= 0 and index < chunk_cache.size():
		return chunk_cache[index]

	return null

func _update_cache_access(chunk: Chunk) -> void:
	chunk.mark_accessed()

	# Move to end of cache (most recently used)
	if chunk.chunk_position in cache_lookup:
		var index = cache_lookup[chunk.chunk_position]
		chunk_cache.remove_at(index)
		chunk_cache.append(chunk)
		_rebuild_cache_lookup()

func _rebuild_cache_lookup() -> void:
	cache_lookup.clear()
	for i in range(chunk_cache.size()):
		cache_lookup[chunk_cache[i].chunk_position] = i

func _determine_chunk_biome(world_pos: Vector2i, biome_seed_points: Dictionary, rng: RandomNumberGenerator) -> int:
	# Find nearest biome seed point
	var nearest_biome = WorldConstants.BiomeType.PLAINS
	var nearest_distance = INF

	for seed_pos in biome_seed_points:
		var distance = WorldCoordinate.euclidean_distance(world_pos, seed_pos)
		if distance < nearest_distance:
			nearest_distance = distance
			nearest_biome = biome_seed_points[seed_pos]

	# Add some variation at biome edges
	if nearest_distance > 100 and rng.randf() < 0.3:
		# Chance to blend with neighboring biome
		var biome_types = WorldConstants.BiomeType.values()
		return biome_types[rng.randi() % biome_types.size()]

	return nearest_biome

func _generate_tile(world_pos: Vector2i, biome: int, world_seed: int, rng: RandomNumberGenerator) -> Tile:
	var tile = Tile.new()

	# Simple noise-based generation
	var noise_value = _simple_noise(world_pos, world_seed)

	# Apply biome-specific rules
	_apply_biome_rules(tile, biome, noise_value)

	tile.biome_id = WorldConstants.BiomeType.keys()[biome]
	tile.elevation = noise_value
	tile.variant_id = rng.randi() % 4  # Visual variation

	return tile

func _simple_noise(pos: Vector2i, seed: int) -> float:
	# Simple pseudo-random noise
	var x = float(pos.x) * 0.1 + seed * 0.001
	var y = float(pos.y) * 0.1 + seed * 0.002
	return abs(sin(x) * cos(y) + sin(x * 0.5) * cos(y * 0.5))

func _apply_biome_rules(tile: Tile, biome: int, noise_value: float) -> void:
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
			else:
				tile.type = WorldConstants.TileType.GROUND

		WorldConstants.BiomeType.MOUNTAINS:
			if noise_value > 0.4:
				tile.type = WorldConstants.TileType.MOUNTAIN
				tile.is_passable = false
			else:
				tile.type = WorldConstants.TileType.GROUND

		WorldConstants.BiomeType.DESERT:
			if noise_value > 0.85:
				tile.type = WorldConstants.TileType.MOUNTAIN
				tile.is_passable = false
			else:
				tile.type = WorldConstants.TileType.GROUND

		WorldConstants.BiomeType.SWAMP:
			if noise_value > 0.6:
				tile.type = WorldConstants.TileType.TREE
				tile.is_passable = false
			elif noise_value > 0.4:
				tile.type = WorldConstants.TileType.WATER
				tile.is_passable = false
			else:
				tile.type = WorldConstants.TileType.GROUND

		_:
			tile.type = WorldConstants.TileType.GROUND

func _check_chunk_locations(chunk: Chunk, location_density: float, rng: RandomNumberGenerator) -> void:
	# Check if we should spawn a location in this chunk
	var spawn_chance = location_density * WorldConstants.CHUNK_SIZE * WorldConstants.CHUNK_SIZE

	if rng.randf() < spawn_chance:
		# Try to find a valid spawn position
		for attempt in range(10):
			var local_x = rng.randi_range(2, WorldConstants.CHUNK_SIZE - 3)
			var local_y = rng.randi_range(2, WorldConstants.CHUNK_SIZE - 3)
			var tile = chunk.get_tile(local_x, local_y)

			if tile and tile.is_passable and tile.type == WorldConstants.TileType.GROUND:
				var world_pos = chunk.world_position + Vector2i(local_x, local_y)
				_spawn_location_at(chunk, world_pos, rng)
				break

func _spawn_location_at(chunk: Chunk, world_pos: Vector2i, rng: RandomNumberGenerator) -> void:
	var location = LocationData.new()
	location.name = _generate_location_name(chunk.dominant_biome, rng)
	location.type = _choose_location_type(chunk.dominant_biome, rng)
	location.world_position = world_pos
	location.biome_type = chunk.dominant_biome
	location.generation_seed = rng.seed

	# Update the tile
	var local_pos = WorldCoordinate.world_to_local_chunk(world_pos)
	var tile = chunk.get_tile(local_pos.x, local_pos.y)
	if tile:
		tile.set_location(location)
		chunk.add_location(location)
		BusWorld.location_spawned.emit(world_pos, location)

func _generate_location_name(biome: int, rng: RandomNumberGenerator) -> String:
	var prefixes = ["North", "South", "East", "West", "Old", "New", "Ancient", "Lost"]
	var middles = ["", "wind", "stone", "iron", "gold", "silver", "crystal", "shadow"]
	var suffixes = ["haven", "shire", "ford", "bridge", "hill", "vale", "wood", "hold"]

	var prefix = prefixes[rng.randi() % prefixes.size()]
	var middle = middles[rng.randi() % middles.size()]
	var suffix = suffixes[rng.randi() % suffixes.size()]

	if middle.is_empty():
		return "%s%s" % [prefix, suffix]
	else:
		return "%s %s%s" % [prefix, middle, suffix]

func _choose_location_type(biome: int, rng: RandomNumberGenerator) -> int:
	match biome:
		WorldConstants.BiomeType.PLAINS:
			var weights = [
				[WorldConstants.LocationType.VILLAGE, 60],
				[WorldConstants.LocationType.CAMP, 20],
				[WorldConstants.LocationType.SHRINE, 20]
			]
			return _weighted_choice(weights, rng)

		WorldConstants.BiomeType.FOREST:
			var weights = [
				[WorldConstants.LocationType.CAMP, 40],
				[WorldConstants.LocationType.SHRINE, 30],
				[WorldConstants.LocationType.RUIN, 30]
			]
			return _weighted_choice(weights, rng)

		WorldConstants.BiomeType.MOUNTAINS:
			var weights = [
				[WorldConstants.LocationType.CAVE, 40],
				[WorldConstants.LocationType.DUNGEON, 30],
				[WorldConstants.LocationType.CASTLE, 30]
			]
			return _weighted_choice(weights, rng)

		_:
			return WorldConstants.LocationType.VILLAGE

func _weighted_choice(weights: Array, rng: RandomNumberGenerator) -> int:
	var total = 0
	for w in weights:
		total += w[1]

	var choice = rng.randi() % total
	var current = 0

	for w in weights:
		current += w[1]
		if choice < current:
			return w[0]

	return weights[0][0]

func get_statistics() -> Dictionary:
	return {
		"loaded_chunks": loaded_chunks.size(),
		"cached_chunks": chunk_cache.size(),
		"chunks_generated": chunks_generated_count,
		"chunks_from_cache": chunks_loaded_from_cache,
		"avg_generation_time": generation_time_total / max(1, chunks_generated_count),
		"generation_queue": generation_queue.size(),
		"active_threads": active_threads
	}