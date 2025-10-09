extends Resource
class_name LocationData

@export var name: String = ""
@export var type: WorldConstants.LocationType = WorldConstants.LocationType.VILLAGE
@export var is_discovered: bool = false
@export var biome_type: WorldConstants.BiomeType = WorldConstants.BiomeType.PLAINS
@export var world_position: Vector2i = Vector2i.ZERO

# NPCs in this location
var npc_ids: Array[String] = []

# Shops and services
var shop_data: Array[ShopData] = []

# Special properties
var has_inn: bool = false
var has_quest_giver: bool = false
var is_safe_zone: bool = true

# Generation parameters (for procedural generation)
var generation_seed: int = 0
var size_category: String = "small"  # small, medium, large

# Metadata for various systems
var metadata: Dictionary = {}

func _init(p_name: String = "", p_type: WorldConstants.LocationType = WorldConstants.LocationType.VILLAGE):
	name = p_name
	type = p_type
	_set_default_properties()

func _set_default_properties() -> void:
	match type:
		WorldConstants.LocationType.VILLAGE:
			has_inn = true
			is_safe_zone = true
			size_category = "medium"
		WorldConstants.LocationType.CAVE:
			has_inn = false
			is_safe_zone = false
			size_category = "small"
		WorldConstants.LocationType.DUNGEON:
			has_inn = false
			is_safe_zone = false
			size_category = "large"
		WorldConstants.LocationType.SHRINE:
			has_inn = false
			is_safe_zone = true
			size_category = "small"
		WorldConstants.LocationType.CASTLE:
			has_inn = true
			is_safe_zone = true
			size_category = "large"
		WorldConstants.LocationType.RUIN:
			has_inn = false
			is_safe_zone = false
			size_category = "medium"
		WorldConstants.LocationType.CAMP:
			has_inn = false
			is_safe_zone = true
			size_category = "small"

func discover() -> void:
	if not is_discovered:
		is_discovered = true
		BusWorld.location_discovered.emit(self)

func add_npc(npc_id: String) -> void:
	if npc_id not in npc_ids:
		npc_ids.append(npc_id)

func remove_npc(npc_id: String) -> void:
	npc_ids.erase(npc_id)

func add_shop(shop: ShopData) -> void:
	shop_data.append(shop)

func get_display_name() -> String:
	if not is_discovered:
		return "???"
	return name

func get_location_icon() -> String:
	# Return appropriate icon path based on type
	match type:
		WorldConstants.LocationType.VILLAGE:
			return "res://assets/images/ui/icons/village.png"
		WorldConstants.LocationType.CAVE:
			return "res://assets/images/ui/icons/cave.png"
		WorldConstants.LocationType.DUNGEON:
			return "res://assets/images/ui/icons/dungeon.png"
		WorldConstants.LocationType.SHRINE:
			return "res://assets/images/ui/icons/shrine.png"
		WorldConstants.LocationType.CASTLE:
			return "res://assets/images/ui/icons/castle.png"
		WorldConstants.LocationType.RUIN:
			return "res://assets/images/ui/icons/ruin.png"
		WorldConstants.LocationType.CAMP:
			return "res://assets/images/ui/icons/camp.png"
		_:
			return ""

# Serialization for saving
func serialize() -> Dictionary:
	var data = {
		"name": name,
		"type": type,
		"is_discovered": is_discovered,
		"biome_type": biome_type,
		"world_position": {"x": world_position.x, "y": world_position.y},
		"npc_ids": npc_ids,
		"has_inn": has_inn,
		"has_quest_giver": has_quest_giver,
		"is_safe_zone": is_safe_zone,
		"generation_seed": generation_seed,
		"size_category": size_category,
		"metadata": metadata
	}

	# Serialize shop data
	var shops = []
	for shop in shop_data:
		shops.append(shop.serialize())
	data["shop_data"] = shops

	return data

func deserialize(data: Dictionary) -> void:
	name = data.get("name", "")
	type = data.get("type", WorldConstants.LocationType.VILLAGE)
	is_discovered = data.get("is_discovered", false)
	biome_type = data.get("biome_type", WorldConstants.BiomeType.PLAINS)

	var pos_data = data.get("world_position", {"x": 0, "y": 0})
	world_position = Vector2i(pos_data["x"], pos_data["y"])

	npc_ids = data.get("npc_ids", [])
	has_inn = data.get("has_inn", false)
	has_quest_giver = data.get("has_quest_giver", false)
	is_safe_zone = data.get("is_safe_zone", true)
	generation_seed = data.get("generation_seed", 0)
	size_category = data.get("size_category", "small")
	metadata = data.get("metadata", {})

	# Deserialize shop data
	shop_data.clear()
	for shop_dict in data.get("shop_data", []):
		var shop = ShopData.new()
		shop.deserialize(shop_dict)
		shop_data.append(shop)