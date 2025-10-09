extends Node

# World dimensions
const WORLD_SIZE := 10000  # 10,000x10,000 tiles
const CHUNK_SIZE := 32  # 32x32 tiles per chunk
const TILE_SIZE := 64  # Display size in pixels (source images are 202x202)

# Movement and visibility
const MOVEMENT_SPEED := 2.0  # Tiles per second
const VISIBILITY_RADIUS := 10  # Tiles visible around party
const CAMERA_EDGE_BUFFER := 5  # Tiles from edge before camera scrolls

# Location spawning
const LOCATION_DENSITY := 1.0 / 625.0  # 1 location per 25x25 tiles
const LOCATION_SPAWN_MEDIAN_DISTANCE := 50  # Bell curve median distance
const LOCATION_MIN_SEPARATION := 15  # Minimum tiles between locations
const LOCATION_SPAWN_STD_DEV := 15.0  # Standard deviation for bell curve

# Performance settings
const CHUNK_LOAD_RADIUS := 2  # Load chunks within this radius
const CHUNK_UNLOAD_RADIUS := 3  # Unload chunks beyond this radius
const CHUNK_CACHE_SIZE := 50  # Number of chunks to keep in LRU cache

# Party settings
const MAX_PARTY_SIZE := 4
const PARTY_FOLLOW_DISTANCE := 1  # Tiles between party members

# Pathfinding
const MAX_PATH_LENGTH := 100  # Maximum tiles for A* pathfinding

# Save system
const AUTO_SAVE_INTERVAL := 60.0  # Seconds between auto-saves
const MAX_SAVE_SLOTS := 3

# Debug settings
const DEBUG_MODE := true  # Enable debug visualizations
const SHOW_CHUNK_BOUNDARIES := false
const SHOW_COORDINATES := false

# Tile types
enum TileType {
	GROUND,
	TREE,
	MOUNTAIN,
	WATER,
	LOCATION,
	ROAD,
	BRIDGE
}

# Visibility states for fog of war
enum VisibilityState {
	HIDDEN,    # Never seen (black)
	REVEALED,  # Previously seen (gray)
	VISIBLE    # Currently visible (full color)
}

# Location types
enum LocationType {
	VILLAGE,
	CAVE,
	DUNGEON,
	SHRINE,
	CASTLE,
	RUIN,
	CAMP
}

# Biome types
enum BiomeType {
	PLAINS,
	FOREST,
	MOUNTAINS,
	DESERT,
	SWAMP,
	TUNDRA,
	VOLCANIC
}

# Formation types for party
enum FormationType {
	LINE,
	SQUARE,
	DIAMOND,
	WEDGE
}

# Helper functions
static func chunk_to_world(chunk_pos: Vector2i) -> Vector2i:
	return chunk_pos * CHUNK_SIZE

static func world_to_chunk(world_pos: Vector2i) -> Vector2i:
	return world_pos / CHUNK_SIZE

static func tile_to_pixel(tile_pos: Vector2i) -> Vector2:
	return Vector2(tile_pos * TILE_SIZE)

static func pixel_to_tile(pixel_pos: Vector2) -> Vector2i:
	return Vector2i(pixel_pos / TILE_SIZE)

static func is_valid_world_position(pos: Vector2i) -> bool:
	return pos.x >= 0 and pos.x < WORLD_SIZE and pos.y >= 0 and pos.y < WORLD_SIZE

static func manhattan_distance(a: Vector2i, b: Vector2i) -> int:
	return abs(a.x - b.x) + abs(a.y - b.y)

static func euclidean_distance(a: Vector2i, b: Vector2i) -> float:
	var diff = b - a
	return sqrt(diff.x * diff.x + diff.y * diff.y)