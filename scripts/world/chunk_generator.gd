extends RefCounted
class_name ChunkGenerator

# Static chunk generation functions for thread safety

static func generate_chunk(thread_data: Dictionary) -> Dictionary:
	var chunk_pos = thread_data["chunk_pos"]
	var world_seed = thread_data["world_seed"]
	var location_density = thread_data["location_density"]
	var biome_seed_points = thread_data["biome_seed_points"]

	# Create chunk
	var chunk = Chunk.new(chunk_pos)
	var world_pos = WorldCoordinate.chunk_to_world(chunk_pos)
	var rng = RandomNumberGenerator.new()
	rng.seed = hash(Vector3i(world_seed, chunk_pos.x, chunk_pos.y))

	# Get biome for this chunk
	var biome_type = _determine_chunk_biome(world_pos, biome_seed_points)
	chunk.dominant_biome = biome_type

	# Generate tiles
	for x in range(WorldConstants.CHUNK_SIZE):
		for y in range(WorldConstants.CHUNK_SIZE):
			var tile_world_pos = Vector2i(world_pos.x + x, world_pos.y + y)
			var tile = _generate_tile(tile_world_pos, biome_type, world_seed, rng)
			chunk.set_tile(x, y, tile)

	# Check for location spawning
	_check_chunk_locations(chunk, location_density, rng)

	return {
		"chunk": chunk,
		"chunk_pos": chunk_pos
	}

static func _determine_chunk_biome(world_pos: Vector2i, biome_seed_points: Dictionary) -> int:
	# Find nearest biome seed point
	var nearest_biome = WorldConstants.BiomeType.PLAINS
	var nearest_distance = INF

	for seed_pos in biome_seed_points:
		var distance = WorldCoordinate.euclidean_distance(world_pos, seed_pos)
		if distance < nearest_distance:
			nearest_distance = distance
			nearest_biome = biome_seed_points[seed_pos]

	return nearest_biome

static func _generate_tile(world_pos: Vector2i, biome: int, world_seed: int, rng: RandomNumberGenerator) -> Tile:
	var tile = Tile.new()

	# Simple noise-based generation
	var noise_value = _simple_noise(world_pos, world_seed)

	# Apply biome-specific rules
	_apply_biome_rules(tile, biome, noise_value)

	tile.biome_id = WorldConstants.BiomeType.keys()[biome]
	tile.elevation = noise_value
	tile.variant_id = rng.randi() % 4  # Visual variation

	return tile

static func _simple_noise(pos: Vector2i, seed: int) -> float:
	# Simple pseudo-random noise
	var x = float(pos.x) * 0.1 + seed * 0.001
	var y = float(pos.y) * 0.1 + seed * 0.002
	return abs(sin(x) * cos(y) + sin(x * 0.5) * cos(y * 0.5))

static func _apply_biome_rules(tile: Tile, biome: int, noise_value: float) -> void:
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

static func _check_chunk_locations(chunk: Chunk, location_density: float, rng: RandomNumberGenerator) -> void:
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

static func _spawn_location_at(chunk: Chunk, world_pos: Vector2i, rng: RandomNumberGenerator) -> void:
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

static func _generate_location_name(biome: int, rng: RandomNumberGenerator) -> String:
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

static func _choose_location_type(biome: int, rng: RandomNumberGenerator) -> int:
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

static func _weighted_choice(weights: Array, rng: RandomNumberGenerator) -> int:
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