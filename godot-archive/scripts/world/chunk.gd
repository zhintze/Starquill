extends Node2D
class_name Chunk

const CHUNK_SIZE := WorldConstants.CHUNK_SIZE

# Chunk position in chunk coordinates
var chunk_position: Vector2i = Vector2i.ZERO

# Chunk position in world tile coordinates
var world_position: Vector2i = Vector2i.ZERO

# 2D array of tiles [x][y]
var tiles: Array = []

# Rendering components
var tile_map: TileMapLayer = null
var fog_overlay: Node2D = null

# State tracking
var is_loaded: bool = false
var is_modified: bool = false
var last_access_time: float = 0.0

# Locations in this chunk
var locations: Array[LocationData] = []

# Biome information
var dominant_biome: WorldConstants.BiomeType = WorldConstants.BiomeType.PLAINS
var biome_blend_map: Dictionary = {}  # Position -> biome weights

func _init(p_chunk_pos: Vector2i = Vector2i.ZERO):
	chunk_position = p_chunk_pos
	world_position = WorldConstants.chunk_to_world(p_chunk_pos)
	_initialize_tiles()

func _ready():
	_setup_rendering()

func _initialize_tiles() -> void:
	tiles.clear()
	for x in range(CHUNK_SIZE):
		var column = []
		for y in range(CHUNK_SIZE):
			var tile = Tile.new(WorldConstants.TileType.GROUND)
			column.append(tile)
		tiles.append(column)

func _setup_rendering() -> void:
	# Create TileMapLayer for rendering
	if not tile_map:
		tile_map = TileMapLayer.new()
		tile_map.name = "TileMap"
		tile_map.tile_set = _create_tileset()
		add_child(tile_map)

	# Create fog overlay
	if not fog_overlay:
		fog_overlay = Node2D.new()
		fog_overlay.name = "FogOverlay"
		add_child(fog_overlay)

func _create_tileset() -> TileSet:
	# This would normally load from resources
	# For now, create a basic tileset
	var tileset = TileSet.new()
	tileset.tile_size = Vector2i(WorldConstants.TILE_SIZE, WorldConstants.TILE_SIZE)
	# Additional tileset setup would go here
	return tileset

func get_tile(local_x: int, local_y: int) -> Tile:
	if local_x >= 0 and local_x < CHUNK_SIZE and local_y >= 0 and local_y < CHUNK_SIZE:
		return tiles[local_x][local_y]
	return null

func set_tile(local_x: int, local_y: int, tile: Tile) -> void:
	if local_x >= 0 and local_x < CHUNK_SIZE and local_y >= 0 and local_y < CHUNK_SIZE:
		tiles[local_x][local_y] = tile
		is_modified = true
		_update_tile_visual(local_x, local_y, tile)

func get_tile_world(world_x: int, world_y: int) -> Tile:
	var local_x = world_x - world_position.x
	var local_y = world_y - world_position.y
	return get_tile(local_x, local_y)

func set_tile_world(world_x: int, world_y: int, tile: Tile) -> void:
	var local_x = world_x - world_position.x
	var local_y = world_y - world_position.y
	set_tile(local_x, local_y, tile)

func _update_tile_visual(local_x: int, local_y: int, tile: Tile) -> void:
	if not tile_map:
		return

	# Update the tilemap visual representation
	var atlas_coords = _get_atlas_coords_for_tile(tile)
	if atlas_coords.x >= 0:
		tile_map.set_cell(Vector2i(local_x, local_y), 0, atlas_coords)
	else:
		tile_map.erase_cell(Vector2i(local_x, local_y))

	# Update fog overlay
	_update_fog_for_tile(local_x, local_y, tile)

func _get_atlas_coords_for_tile(tile: Tile) -> Vector2i:
	# Map tile types to atlas coordinates
	# This would be configured based on actual tileset
	match tile.type:
		WorldConstants.TileType.GROUND:
			return Vector2i(0, 0)
		WorldConstants.TileType.TREE:
			return Vector2i(1, 0)
		WorldConstants.TileType.MOUNTAIN:
			return Vector2i(2, 0)
		WorldConstants.TileType.WATER:
			return Vector2i(3, 0)
		WorldConstants.TileType.LOCATION:
			return Vector2i(4, 0)
		WorldConstants.TileType.ROAD:
			return Vector2i(5, 0)
		WorldConstants.TileType.BRIDGE:
			return Vector2i(6, 0)
		_:
			return Vector2i(-1, -1)

func _update_fog_for_tile(local_x: int, local_y: int, tile: Tile) -> void:
	# Update fog of war overlay for this tile
	# Implementation would depend on fog rendering approach
	pass

func update_visibility(visible_positions: Array[Vector2i]) -> void:
	for x in range(CHUNK_SIZE):
		for y in range(CHUNK_SIZE):
			var world_pos = Vector2i(world_position.x + x, world_position.y + y)
			var tile = tiles[x][y]

			if world_pos in visible_positions:
				tile.set_visible(true)
			else:
				tile.set_visible(false)

			_update_fog_for_tile(x, y, tile)

func reveal_area(center: Vector2i, radius: int) -> void:
	for x in range(CHUNK_SIZE):
		for y in range(CHUNK_SIZE):
			var world_pos = Vector2i(world_position.x + x, world_position.y + y)
			var distance = WorldConstants.euclidean_distance(world_pos, center)

			if distance <= radius:
				tiles[x][y].reveal()
				_update_fog_for_tile(x, y, tiles[x][y])

func add_location(location: LocationData) -> void:
	if location not in locations:
		locations.append(location)
		is_modified = true

func remove_location(location: LocationData) -> void:
	locations.erase(location)
	is_modified = true

func get_locations_at(world_pos: Vector2i) -> Array[LocationData]:
	var result: Array[LocationData] = []
	for location in locations:
		if location.world_position == world_pos:
			result.append(location)
	return result

func load_chunk() -> void:
	if not is_loaded:
		is_loaded = true
		last_access_time = Time.get_ticks_msec() / 1000.0
		_refresh_visuals()
		BusWorld.chunk_loaded.emit(chunk_position, self)

func unload_chunk() -> void:
	if is_loaded:
		is_loaded = false
		BusWorld.chunk_unloaded.emit(chunk_position)

func _refresh_visuals() -> void:
	if not tile_map:
		return

	for x in range(CHUNK_SIZE):
		for y in range(CHUNK_SIZE):
			_update_tile_visual(x, y, tiles[x][y])

func mark_accessed() -> void:
	last_access_time = Time.get_ticks_msec() / 1000.0

# Serialization for saving
func serialize() -> Dictionary:
	var tile_data = []
	for x in range(CHUNK_SIZE):
		var column = []
		for y in range(CHUNK_SIZE):
			column.append(tiles[x][y].serialize())
		tile_data.append(column)

	var location_data = []
	for location in locations:
		location_data.append(location.serialize())

	return {
		"chunk_position": {"x": chunk_position.x, "y": chunk_position.y},
		"tiles": tile_data,
		"locations": location_data,
		"dominant_biome": dominant_biome,
		"is_modified": is_modified
	}

func deserialize(data: Dictionary) -> void:
	var pos = data.get("chunk_position", {"x": 0, "y": 0})
	chunk_position = Vector2i(pos["x"], pos["y"])
	world_position = WorldConstants.chunk_to_world(chunk_position)

	# Deserialize tiles
	var tile_data = data.get("tiles", [])
	tiles.clear()
	for x in range(CHUNK_SIZE):
		var column = []
		for y in range(CHUNK_SIZE):
			var tile = Tile.new()
			if x < tile_data.size() and y < tile_data[x].size():
				tile.deserialize(tile_data[x][y])
			column.append(tile)
		tiles.append(column)

	# Deserialize locations
	locations.clear()
	for loc_data in data.get("locations", []):
		var location = LocationData.new()
		location.deserialize(loc_data)
		locations.append(location)

	dominant_biome = data.get("dominant_biome", WorldConstants.BiomeType.PLAINS)
	is_modified = data.get("is_modified", false)