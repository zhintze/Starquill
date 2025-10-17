extends Node2D
class_name TileRenderer

# Efficient tile rendering using TileMapLayer

# TileMapLayers for different rendering layers
var terrain_layer: TileMapLayer
var feature_layer: TileMapLayer  # Trees, mountains, etc
var location_layer: TileMapLayer
var fog_layer: TileMapLayer

# Tileset resource
var tileset: TileSet

# Tile source IDs
var terrain_source_id: int = 0
var feature_source_id: int = 1
var location_source_id: int = 2
var fog_source_id: int = 3

# Atlas coordinates for tile types
var tile_atlas_coords: Dictionary = {}

# Batch update tracking
var tiles_to_update: Array[Vector3i] = []  # x, y, layer
var batch_update_timer: Timer
var batch_update_interval: float = 0.1

signal tileset_loaded()
signal batch_update_completed(tile_count: int)

func _ready():
	_setup_layers()
	_create_tileset()
	_setup_batch_timer()

func _setup_layers() -> void:
	# Create terrain base layer
	terrain_layer = TileMapLayer.new()
	terrain_layer.name = "TerrainLayer"
	terrain_layer.z_index = 0
	add_child(terrain_layer)

	# Create feature layer (trees, mountains)
	feature_layer = TileMapLayer.new()
	feature_layer.name = "FeatureLayer"
	feature_layer.z_index = 1
	feature_layer.y_sort_enabled = true
	add_child(feature_layer)

	# Create location layer
	location_layer = TileMapLayer.new()
	location_layer.name = "LocationLayer"
	location_layer.z_index = 2
	add_child(location_layer)

	# Create fog of war layer
	fog_layer = TileMapLayer.new()
	fog_layer.name = "FogLayer"
	fog_layer.z_index = 10
	fog_layer.modulate = Color(0, 0, 0, 0.8)  # Dark overlay
	add_child(fog_layer)

func _create_tileset() -> void:
	tileset = TileSet.new()
	tileset.tile_size = Vector2i(WorldConstants.TILE_SIZE, WorldConstants.TILE_SIZE)

	# Create atlas sources for different tile types
	_create_terrain_atlas()
	_create_feature_atlas()
	_create_location_atlas()
	_create_fog_atlas()

	# Apply tileset to all layers
	terrain_layer.tile_set = tileset
	feature_layer.tile_set = tileset
	location_layer.tile_set = tileset
	fog_layer.tile_set = tileset

	tileset_loaded.emit()

func _create_terrain_atlas() -> void:
	var atlas_source = TileSetAtlasSource.new()
	var texture = _create_terrain_atlas_texture()
	atlas_source.texture = texture
	atlas_source.texture_region_size = Vector2i(WorldConstants.TILE_SIZE, WorldConstants.TILE_SIZE)

	# Define terrain tiles
	for i in range(4):
		atlas_source.create_tile(Vector2i(i, 0), Vector2i(1, 1))
		tile_atlas_coords["ground_%d" % i] = Vector2i(i, 0)

	# Road tiles
	atlas_source.create_tile(Vector2i(0, 1), Vector2i(1, 1))
	tile_atlas_coords["road"] = Vector2i(0, 1)

	# Bridge tiles
	atlas_source.create_tile(Vector2i(1, 1), Vector2i(1, 1))
	tile_atlas_coords["bridge"] = Vector2i(1, 1)

	# Water tiles
	for i in range(4):
		atlas_source.create_tile(Vector2i(i, 2), Vector2i(1, 1))
		tile_atlas_coords["water_%d" % i] = Vector2i(i, 2)

	tileset.add_source(atlas_source, terrain_source_id)

func _create_feature_atlas() -> void:
	var atlas_source = TileSetAtlasSource.new()
	var texture = _create_features_atlas_texture()
	atlas_source.texture = texture
	atlas_source.texture_region_size = Vector2i(WorldConstants.TILE_SIZE, WorldConstants.TILE_SIZE)

	# Tree variations
	for i in range(4):
		atlas_source.create_tile(Vector2i(i, 0), Vector2i(1, 1))
		tile_atlas_coords["tree_%d" % i] = Vector2i(i, 0)

	# Mountain variations
	for i in range(4):
		atlas_source.create_tile(Vector2i(i, 1), Vector2i(1, 1))
		tile_atlas_coords["mountain_%d" % i] = Vector2i(i, 1)

	tileset.add_source(atlas_source, feature_source_id)

func _create_location_atlas() -> void:
	var atlas_source = TileSetAtlasSource.new()
	var texture = _create_locations_atlas_texture()
	atlas_source.texture = texture
	atlas_source.texture_region_size = Vector2i(WorldConstants.TILE_SIZE, WorldConstants.TILE_SIZE)

	# Location type icons
	var location_types = [
		"village", "cave", "dungeon", "shrine",
		"castle", "ruin", "camp"
	]

	for i in range(location_types.size()):
		atlas_source.create_tile(Vector2i(i, 0), Vector2i(1, 1))
		tile_atlas_coords["location_%s" % location_types[i]] = Vector2i(i, 0)

	tileset.add_source(atlas_source, location_source_id)

func _create_fog_atlas() -> void:
	var atlas_source = TileSetAtlasSource.new()
	var texture = _create_fog_atlas_texture()
	atlas_source.texture = texture
	atlas_source.texture_region_size = Vector2i(WorldConstants.TILE_SIZE, WorldConstants.TILE_SIZE)

	# Fog states
	atlas_source.create_tile(Vector2i(0, 0), Vector2i(1, 1))
	tile_atlas_coords["fog_full"] = Vector2i(0, 0)

	atlas_source.create_tile(Vector2i(1, 0), Vector2i(1, 1))
	tile_atlas_coords["fog_revealed"] = Vector2i(1, 0)

	tileset.add_source(atlas_source, fog_source_id)

func _create_terrain_atlas_texture() -> Texture2D:
	var tile_size = WorldConstants.TILE_SIZE
	var atlas_image = Image.create(tile_size * 4, tile_size * 3, false, Image.FORMAT_RGBA8)
	atlas_image.fill(Color(0, 0, 0, 0))  # Transparent

	# Load flat tile variations (worldmap-flat1 through flat7)
	var flat_tiles = []
	for i in range(1, 8):
		var tile_image = _load_worldmap_texture("flat%d" % i)
		if tile_image:
			flat_tiles.append(tile_image)

	# If no tiles loaded, use placeholder
	if flat_tiles.is_empty():
		return _create_placeholder_atlas()

	# Row 0: Ground variations (use flat1-4)
	for i in range(4):
		var tile_idx = i % flat_tiles.size()
		_blit_tile_to_atlas(atlas_image, flat_tiles[tile_idx], i * tile_size, 0)

	# Row 1: Road and bridge tiles (use flat5-6)
	var road_idx = mini(4, flat_tiles.size() - 1)
	var bridge_idx = mini(5, flat_tiles.size() - 1)
	_blit_tile_to_atlas(atlas_image, flat_tiles[road_idx], 0, tile_size)
	_blit_tile_to_atlas(atlas_image, flat_tiles[bridge_idx], tile_size, tile_size)

	# Row 2: Water tiles (use flat7 or last available)
	var water_idx = mini(6, flat_tiles.size() - 1)
	for i in range(4):
		_blit_tile_to_atlas(atlas_image, flat_tiles[water_idx], i * tile_size, tile_size * 2)

	return ImageTexture.create_from_image(atlas_image)

func _create_features_atlas_texture() -> Texture2D:
	var tile_size = WorldConstants.TILE_SIZE
	var atlas_image = Image.create(tile_size * 4, tile_size * 2, false, Image.FORMAT_RGBA8)
	atlas_image.fill(Color(0, 0, 0, 0))  # Transparent

	var forest_texture = _load_worldmap_texture("forest")
	var mountains_texture = _load_worldmap_texture("mountains")

	# Row 0: Tree variations (forest texture)
	for i in range(4):
		if forest_texture:
			_blit_tile_to_atlas(atlas_image, forest_texture, i * tile_size, 0)

	# Row 1: Mountain variations (mountains texture)
	for i in range(4):
		if mountains_texture:
			_blit_tile_to_atlas(atlas_image, mountains_texture, i * tile_size, tile_size)

	return ImageTexture.create_from_image(atlas_image)

func _create_locations_atlas_texture() -> Texture2D:
	var tile_size = WorldConstants.TILE_SIZE
	var atlas_image = Image.create(tile_size * 7, tile_size, false, Image.FORMAT_RGBA8)
	atlas_image.fill(Color(0, 0, 0, 0))  # Transparent

	var village_texture = _load_worldmap_texture("village")
	var dungeon_texture = _load_worldmap_texture("dungeon")

	var location_textures = [
		village_texture,   # VILLAGE
		dungeon_texture,   # CAVE
		dungeon_texture,   # DUNGEON
		village_texture,   # SHRINE
		village_texture,   # CASTLE
		dungeon_texture,   # RUIN
		village_texture    # CAMP
	]

	for i in range(7):
		var texture = location_textures[i]
		if texture:
			_blit_tile_to_atlas(atlas_image, texture, i * tile_size, 0)

	return ImageTexture.create_from_image(atlas_image)

func _create_fog_atlas_texture() -> Texture2D:
	var tile_size = WorldConstants.TILE_SIZE
	var image = Image.create(tile_size * 2, tile_size, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.1, 0.1, 0.1, 0.9))  # Dark fog

	# Second tile: semi-transparent for revealed
	for x in range(tile_size, tile_size * 2):
		for y in range(tile_size):
			image.set_pixel(x, y, Color(0.1, 0.1, 0.1, 0.5))

	return ImageTexture.create_from_image(image)

func _create_placeholder_atlas() -> Texture2D:
	var image = Image.create(256, 256, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.3, 0.3, 0.3))
	return ImageTexture.create_from_image(image)

func _load_worldmap_texture(type: String) -> Image:
	var texture_path = "res://assets/images/ui/worldmap-%s.png" % type
	if ResourceLoader.exists(texture_path):
		var texture = load(texture_path) as Texture2D
		if texture:
			var image = texture.get_image()
			if image:
				print("TileRenderer: Loaded %s texture %dx%d" % [type, image.get_width(), image.get_height()])
				return image
	print("TileRenderer: Failed to load %s texture" % type)
	return null


func _blit_tile_to_atlas(atlas: Image, tile: Image, x_offset: int, y_offset: int) -> void:
	if not tile:
		return

	var tile_size = WorldConstants.TILE_SIZE

	# Scale tile to match TILE_SIZE if needed
	var scaled_tile = tile
	if tile.get_width() != tile_size or tile.get_height() != tile_size:
		scaled_tile = tile.duplicate()
		scaled_tile.resize(tile_size, tile_size, Image.INTERPOLATE_NEAREST)

	# Blit the scaled tile
	atlas.blit_rect(scaled_tile, Rect2i(0, 0, tile_size, tile_size), Vector2i(x_offset, y_offset))

func _create_placeholder_texture() -> Texture2D:
	var image = Image.create(64, 64, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.5, 0.5, 0.5))
	return ImageTexture.create_from_image(image)

func _setup_batch_timer() -> void:
	batch_update_timer = Timer.new()
	batch_update_timer.wait_time = batch_update_interval
	batch_update_timer.one_shot = false
	batch_update_timer.timeout.connect(_process_batch_updates)
	add_child(batch_update_timer)
	batch_update_timer.start()

func render_chunk(chunk: Chunk) -> void:
	for x in range(WorldConstants.CHUNK_SIZE):
		for y in range(WorldConstants.CHUNK_SIZE):
			var tile = chunk.get_tile(x, y)
			var world_pos = chunk.world_position + Vector2i(x, y)
			render_tile(world_pos, tile)

func render_tile(world_pos: Vector2i, tile: Tile) -> void:
	# Clear all layers at this position first
	clear_tile(world_pos)

	# Render terrain layer
	var terrain_coords = _get_terrain_atlas_coords(tile)
	if terrain_coords.x >= 0:
		terrain_layer.set_cell(world_pos, terrain_source_id, terrain_coords)

	# Render feature layer if needed
	var feature_coords = _get_feature_atlas_coords(tile)
	if feature_coords.x >= 0:
		feature_layer.set_cell(world_pos, feature_source_id, feature_coords)

	# Render location if present
	if tile.has_location():
		var location_coords = _get_location_atlas_coords(tile.location_data)
		if location_coords.x >= 0:
			location_layer.set_cell(world_pos, location_source_id, location_coords)

	# Update fog based on visibility
	update_fog_tile(world_pos, tile.visibility_state)

func clear_tile(world_pos: Vector2i) -> void:
	terrain_layer.erase_cell(world_pos)
	feature_layer.erase_cell(world_pos)
	location_layer.erase_cell(world_pos)

func update_fog_tile(world_pos: Vector2i, visibility_state: int) -> void:
	match visibility_state:
		WorldConstants.VisibilityState.HIDDEN:
			fog_layer.set_cell(world_pos, fog_source_id, tile_atlas_coords.get("fog_full", Vector2i(0, 0)))
		WorldConstants.VisibilityState.REVEALED:
			fog_layer.set_cell(world_pos, fog_source_id, tile_atlas_coords.get("fog_revealed", Vector2i(1, 0)))
		WorldConstants.VisibilityState.VISIBLE:
			fog_layer.erase_cell(world_pos)

func _get_terrain_atlas_coords(tile: Tile) -> Vector2i:
	match tile.type:
		WorldConstants.TileType.GROUND:
			return tile_atlas_coords.get("ground_%d" % tile.variant_id, Vector2i(0, 0))
		WorldConstants.TileType.WATER:
			return tile_atlas_coords.get("water_%d" % tile.variant_id, Vector2i(0, 2))
		WorldConstants.TileType.ROAD:
			return tile_atlas_coords.get("road", Vector2i(0, 1))
		WorldConstants.TileType.BRIDGE:
			return tile_atlas_coords.get("bridge", Vector2i(1, 1))
		_:
			return Vector2i(0, 0)

func _get_feature_atlas_coords(tile: Tile) -> Vector2i:
	match tile.type:
		WorldConstants.TileType.TREE:
			return tile_atlas_coords.get("tree_%d" % tile.variant_id, Vector2i(0, 0))
		WorldConstants.TileType.MOUNTAIN:
			return tile_atlas_coords.get("mountain_%d" % tile.variant_id, Vector2i(0, 1))
		_:
			return Vector2i(-1, -1)  # No feature

func _get_location_atlas_coords(location: LocationData) -> Vector2i:
	var type_name = WorldConstants.LocationType.keys()[location.type].to_lower()
	return tile_atlas_coords.get("location_%s" % type_name, Vector2i(0, 0))

func queue_tile_update(world_pos: Vector2i, layer: int = -1) -> void:
	tiles_to_update.append(Vector3i(world_pos.x, world_pos.y, layer))

func _process_batch_updates() -> void:
	if tiles_to_update.is_empty():
		return

	var update_count = mini(100, tiles_to_update.size())  # Process up to 100 tiles per batch

	for i in range(update_count):
		var update = tiles_to_update[0]
		tiles_to_update.remove_at(0)

		var world_pos = Vector2i(update.x, update.y)
		var layer = update.z

		# Update specific layer or all layers
		if layer >= 0:
			_update_single_layer(world_pos, layer)
		else:
			# Re-render entire tile (would need access to tile data)
			pass

	if update_count > 0:
		batch_update_completed.emit(update_count)

func _update_single_layer(world_pos: Vector2i, layer: int) -> void:
	match layer:
		0:  # Terrain
			pass  # Would need tile data
		1:  # Features
			pass  # Would need tile data
		2:  # Locations
			pass  # Would need tile data
		3:  # Fog
			pass  # Would need visibility data

func update_visibility_batch(visible_tiles: Array[Vector2i], revealed_tiles: Array[Vector2i]) -> void:
	# Update visible tiles
	for pos in visible_tiles:
		fog_layer.erase_cell(pos)

	# Update revealed but not visible tiles
	for pos in revealed_tiles:
		if pos not in visible_tiles:
			fog_layer.set_cell(pos, fog_source_id, tile_atlas_coords.get("fog_revealed", Vector2i(1, 0)))

func get_tile_at_position(screen_pos: Vector2, camera: Camera2D) -> Vector2i:
	var world_pos = camera.get_global_mouse_position()
	return WorldCoordinate.pixel_to_tile(world_pos)

func highlight_tile(world_pos: Vector2i, color: Color = Color.YELLOW) -> void:
	# Could implement tile highlighting for selection/hover
	# Would require an additional overlay layer
	pass

func clear_highlights() -> void:
	# Clear all tile highlights
	pass
