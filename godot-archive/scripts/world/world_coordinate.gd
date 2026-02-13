extends RefCounted
class_name WorldCoordinate

# Static class for coordinate conversions and calculations

# Convert world tile position to chunk position
static func world_to_chunk(world_pos: Vector2i) -> Vector2i:
	return Vector2i(
		floori(float(world_pos.x) / WorldConstants.CHUNK_SIZE),
		floori(float(world_pos.y) / WorldConstants.CHUNK_SIZE)
	)

# Convert chunk position to world tile position (top-left corner)
static func chunk_to_world(chunk_pos: Vector2i) -> Vector2i:
	return chunk_pos * WorldConstants.CHUNK_SIZE

# Convert world tile position to local chunk position
static func world_to_local_chunk(world_pos: Vector2i) -> Vector2i:
	return Vector2i(
		world_pos.x % WorldConstants.CHUNK_SIZE,
		world_pos.y % WorldConstants.CHUNK_SIZE
	)

# Convert tile position to pixel position
static func tile_to_pixel(tile_pos: Vector2i) -> Vector2:
	return Vector2(tile_pos * WorldConstants.TILE_SIZE)

# Convert pixel position to tile position
static func pixel_to_tile(pixel_pos: Vector2) -> Vector2i:
	return Vector2i(
		floori(pixel_pos.x / WorldConstants.TILE_SIZE),
		floori(pixel_pos.y / WorldConstants.TILE_SIZE)
	)

# Convert screen position to world tile position
static func screen_to_world(screen_pos: Vector2, camera_pos: Vector2, camera_zoom: Vector2, viewport_size: Vector2) -> Vector2i:
	var world_pixel_pos = (screen_pos / camera_zoom) + camera_pos - (viewport_size / 2.0 / camera_zoom)
	return pixel_to_tile(world_pixel_pos)

# Check if position is within world bounds
static func is_valid_position(pos: Vector2i) -> bool:
	return pos.x >= 0 and pos.x < WorldConstants.WORLD_SIZE and \
		   pos.y >= 0 and pos.y < WorldConstants.WORLD_SIZE

# Check if chunk position is within world bounds
static func is_valid_chunk(chunk_pos: Vector2i) -> bool:
	var max_chunks = ceili(float(WorldConstants.WORLD_SIZE) / WorldConstants.CHUNK_SIZE)
	return chunk_pos.x >= 0 and chunk_pos.x < max_chunks and \
		   chunk_pos.y >= 0 and chunk_pos.y < max_chunks

# Calculate Manhattan distance between two positions
static func manhattan_distance(a: Vector2i, b: Vector2i) -> int:
	return abs(a.x - b.x) + abs(a.y - b.y)

# Calculate Euclidean distance between two positions
static func euclidean_distance(a: Vector2i, b: Vector2i) -> float:
	var diff = b - a
	return sqrt(float(diff.x * diff.x + diff.y * diff.y))

# Calculate Chebyshev distance (max of absolute differences)
static func chebyshev_distance(a: Vector2i, b: Vector2i) -> int:
	return maxi(abs(a.x - b.x), abs(a.y - b.y))

# Get all chunks in a radius around a center chunk
static func get_chunks_in_radius(center: Vector2i, radius: int) -> Array[Vector2i]:
	var chunks: Array[Vector2i] = []
	for dx in range(-radius, radius + 1):
		for dy in range(-radius, radius + 1):
			var chunk_pos = center + Vector2i(dx, dy)
			if is_valid_chunk(chunk_pos):
				chunks.append(chunk_pos)
	return chunks

# Get all tiles in a radius around a center tile
static func get_tiles_in_radius(center: Vector2i, radius: float) -> Array[Vector2i]:
	var tiles: Array[Vector2i] = []
	var radius_squared = radius * radius
	var int_radius = ceili(radius)

	for dx in range(-int_radius, int_radius + 1):
		for dy in range(-int_radius, int_radius + 1):
			var tile_pos = center + Vector2i(dx, dy)
			if euclidean_distance(center, tile_pos) <= radius and is_valid_position(tile_pos):
				tiles.append(tile_pos)
	return tiles

# Get the 4 cardinal neighbor positions
static func get_cardinal_neighbors(pos: Vector2i) -> Array[Vector2i]:
	var neighbors: Array[Vector2i] = []
	var offsets = [
		Vector2i(0, -1),  # North
		Vector2i(1, 0),   # East
		Vector2i(0, 1),   # South
		Vector2i(-1, 0)   # West
	]
	for offset in offsets:
		var neighbor = pos + offset
		if is_valid_position(neighbor):
			neighbors.append(neighbor)
	return neighbors

# Get all 8 neighbor positions
static func get_all_neighbors(pos: Vector2i) -> Array[Vector2i]:
	var neighbors: Array[Vector2i] = []
	for dx in range(-1, 2):
		for dy in range(-1, 2):
			if dx == 0 and dy == 0:
				continue
			var neighbor = pos + Vector2i(dx, dy)
			if is_valid_position(neighbor):
				neighbors.append(neighbor)
	return neighbors

# Get direction vector from one position to another
static func get_direction(from: Vector2i, to: Vector2i) -> Vector2i:
	var diff = to - from
	return Vector2i(
		signi(diff.x),
		signi(diff.y)
	)

# Clamp position to world bounds
static func clamp_to_world(pos: Vector2i) -> Vector2i:
	return Vector2i(
		clampi(pos.x, 0, WorldConstants.WORLD_SIZE - 1),
		clampi(pos.y, 0, WorldConstants.WORLD_SIZE - 1)
	)

# Get chunk boundary in world coordinates
static func get_chunk_bounds(chunk_pos: Vector2i) -> Rect2i:
	var world_pos = chunk_to_world(chunk_pos)
	return Rect2i(world_pos, Vector2i(WorldConstants.CHUNK_SIZE, WorldConstants.CHUNK_SIZE))

# Check if a tile position is on chunk boundary
static func is_on_chunk_boundary(world_pos: Vector2i) -> bool:
	var local = world_to_local_chunk(world_pos)
	return local.x == 0 or local.x == WorldConstants.CHUNK_SIZE - 1 or \
		   local.y == 0 or local.y == WorldConstants.CHUNK_SIZE - 1

# Get viewport to see optimal number of tiles on screen
static func get_optimal_tile_count(viewport_size: Vector2) -> Vector2i:
	return Vector2i(
		ceili(viewport_size.x / WorldConstants.TILE_SIZE),
		ceili(viewport_size.y / WorldConstants.TILE_SIZE)
	)
