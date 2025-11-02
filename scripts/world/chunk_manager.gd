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

func _process(_delta: float) -> void:
	# Check for completed threads
	for i in range(thread_pool.size()):
		var thread = thread_pool[i]
		if thread.is_started() and not thread.is_alive():
			var result = thread.wait_to_finish()
			if result is Dictionary:
				_handle_thread_result(result)
			active_threads = max(0, active_threads - 1)

			# Continue processing queue if needed
			if not generation_queue.is_empty():
				_process_generation_queue()

func _exit_tree():
	# Stop accepting new generation requests
	generation_queue.clear()

	# Wait for all active threads to complete
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
	for i in range(thread_pool.size()):
		var thread = thread_pool[i]
		if not thread.is_started() or not thread.is_alive():
			if thread.is_started():
				var thread_result = thread.wait_to_finish()
				if thread_result is Dictionary:
					_handle_thread_result(thread_result)

			active_threads += 1
			chunk_generation_started.emit(chunk_pos)
			thread.start(_generate_chunk_threaded.bind(thread_data))
			break

func _generate_chunk_threaded(thread_data: Dictionary) -> Dictionary:
	var start_time = Time.get_ticks_msec()

	# Use static ChunkGenerator for thread safety
	var result = ChunkGenerator.generate_chunk(thread_data)

	var generation_time = (Time.get_ticks_msec() - start_time) / 1000.0

	# Store timing data in result
	result["generation_time"] = generation_time

	return result

func _handle_thread_result(result: Dictionary) -> void:
	if not result.has("chunk") or not result.has("chunk_pos"):
		return

	var chunk = result["chunk"]
	var chunk_pos = result["chunk_pos"]
	var generation_time = result.get("generation_time", 0.0)

	# Update stats
	generation_time_total += generation_time
	chunks_generated_count += 1

	# Add to loaded chunks
	_load_chunk(chunk)

	# Emit location spawned signals for any locations in this chunk
	for location in chunk.locations:
		EventBus.location_spawned.emit(location.world_position, location)

	chunk_generation_completed.emit(chunk_pos, chunk)

	# Update generation state
	if generation_queue.is_empty():
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

# Chunk generation is now handled by ChunkGenerator static class

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
