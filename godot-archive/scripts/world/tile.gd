extends Resource
class_name Tile

@export var type: WorldConstants.TileType = WorldConstants.TileType.GROUND
@export var is_passable: bool = true
@export var biome_id: String = ""
@export var visibility_state: WorldConstants.VisibilityState = WorldConstants.VisibilityState.HIDDEN
@export var texture_path: String = ""
@export var elevation: float = 0.0  # For terrain generation
@export var moisture: float = 0.0  # For biome determination

# Optional location data if this tile contains a location
var location_data: LocationData = null

# Movement cost for pathfinding (1.0 = normal, higher = slower)
@export var movement_cost: float = 1.0

# Visual variations for the same tile type
@export var variant_id: int = 0

# Metadata for various systems
var metadata: Dictionary = {}

func _init(p_type: WorldConstants.TileType = WorldConstants.TileType.GROUND):
	type = p_type
	_set_default_properties()

func _set_default_properties() -> void:
	match type:
		WorldConstants.TileType.GROUND:
			is_passable = true
			movement_cost = 1.0
		WorldConstants.TileType.TREE:
			is_passable = false
			movement_cost = 999.0
		WorldConstants.TileType.MOUNTAIN:
			is_passable = false
			movement_cost = 999.0
		WorldConstants.TileType.WATER:
			is_passable = false
			movement_cost = 999.0
		WorldConstants.TileType.LOCATION:
			is_passable = true
			movement_cost = 1.0
		WorldConstants.TileType.ROAD:
			is_passable = true
			movement_cost = 0.5  # Faster movement on roads
		WorldConstants.TileType.BRIDGE:
			is_passable = true
			movement_cost = 1.0

func has_location() -> bool:
	return location_data != null

func set_location(data: LocationData) -> void:
	location_data = data
	type = WorldConstants.TileType.LOCATION

func clear_location() -> void:
	location_data = null
	if type == WorldConstants.TileType.LOCATION:
		type = WorldConstants.TileType.GROUND

func reveal() -> void:
	if visibility_state == WorldConstants.VisibilityState.HIDDEN:
		visibility_state = WorldConstants.VisibilityState.REVEALED

func set_visible(visible: bool) -> void:
	if visible:
		visibility_state = WorldConstants.VisibilityState.VISIBLE
	elif visibility_state == WorldConstants.VisibilityState.VISIBLE:
		visibility_state = WorldConstants.VisibilityState.REVEALED

func is_visible() -> bool:
	return visibility_state == WorldConstants.VisibilityState.VISIBLE

func is_revealed() -> bool:
	return visibility_state != WorldConstants.VisibilityState.HIDDEN

func get_texture_resource() -> Texture2D:
	if texture_path.is_empty():
		return null
	return load(texture_path) as Texture2D

# Serialization for saving
func serialize() -> Dictionary:
	var data = {
		"type": type,
		"is_passable": is_passable,
		"biome_id": biome_id,
		"visibility_state": visibility_state,
		"texture_path": texture_path,
		"elevation": elevation,
		"moisture": moisture,
		"movement_cost": movement_cost,
		"variant_id": variant_id,
		"metadata": metadata
	}
	if location_data:
		data["location_data"] = location_data.serialize()
	return data

func deserialize(data: Dictionary) -> void:
	type = data.get("type", WorldConstants.TileType.GROUND)
	is_passable = data.get("is_passable", true)
	biome_id = data.get("biome_id", "")
	visibility_state = data.get("visibility_state", WorldConstants.VisibilityState.HIDDEN)
	texture_path = data.get("texture_path", "")
	elevation = data.get("elevation", 0.0)
	moisture = data.get("moisture", 0.0)
	movement_cost = data.get("movement_cost", 1.0)
	variant_id = data.get("variant_id", 0)
	metadata = data.get("metadata", {})

	if data.has("location_data"):
		location_data = LocationData.new()
		location_data.deserialize(data["location_data"])