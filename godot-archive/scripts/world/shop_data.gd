extends Resource
class_name ShopData

@export var shop_name: String = ""
@export var shop_type: String = "general"  # general, weapon, armor, potion, etc.
@export var owner_npc_id: String = ""
@export var price_modifier: float = 1.0  # Multiplier for base prices
@export var reputation_required: int = 0  # Minimum reputation to access

# Inventory management
var item_ids: Array[String] = []
var stock_limits: Dictionary = {}  # item_id -> max quantity
var restock_interval: float = 86400.0  # Seconds until restock (24 hours)
var last_restock_time: float = 0.0

func _init(p_name: String = "", p_type: String = "general"):
	shop_name = p_name
	shop_type = p_type

func add_item(item_id: String, max_stock: int = -1) -> void:
	if item_id not in item_ids:
		item_ids.append(item_id)
	if max_stock > 0:
		stock_limits[item_id] = max_stock

func remove_item(item_id: String) -> void:
	item_ids.erase(item_id)
	stock_limits.erase(item_id)

func should_restock(current_time: float) -> bool:
	return current_time - last_restock_time >= restock_interval

func restock(current_time: float) -> void:
	last_restock_time = current_time
	# Additional restock logic would go here

func get_price(item_id: String, base_price: float) -> int:
	return int(base_price * price_modifier)

# Serialization for saving
func serialize() -> Dictionary:
	return {
		"shop_name": shop_name,
		"shop_type": shop_type,
		"owner_npc_id": owner_npc_id,
		"price_modifier": price_modifier,
		"reputation_required": reputation_required,
		"item_ids": item_ids,
		"stock_limits": stock_limits,
		"restock_interval": restock_interval,
		"last_restock_time": last_restock_time
	}

func deserialize(data: Dictionary) -> void:
	shop_name = data.get("shop_name", "")
	shop_type = data.get("shop_type", "general")
	owner_npc_id = data.get("owner_npc_id", "")
	price_modifier = data.get("price_modifier", 1.0)
	reputation_required = data.get("reputation_required", 0)
	item_ids = data.get("item_ids", [])
	stock_limits = data.get("stock_limits", {})
	restock_interval = data.get("restock_interval", 86400.0)
	last_restock_time = data.get("last_restock_time", 0.0)