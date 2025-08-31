extends Node
class_name EquipmentFactory
# Consider setting this as an Autoload singleton if multiple scenes use it.

var _main_palette: PackedStringArray = PackedStringArray()

# Removed: now uses ColorManager singleton instead of direct CSV access

func _ready() -> void:
	randomize()
	_main_palette = _load_palette()

# -------------------------------
# PUBLIC: Single creation entry points
# -------------------------------

# Create an EquipmentInstance from a specific catalog item.
func create_from_catalog(cat: EquipmentCatalog.CatalogItem, item_num: int = -1) -> EquipmentInstance:
	if cat == null:
		push_error("EquipmentFactory.create_from_catalog: null catalog item")
		return null

	var amt: int = max(1, cat.amount)
	var chosen_num: int = item_num if item_num > 0 else ((randi() % amt) + 1)

	var ei := EquipmentInstance.new()
	ei._init_from_catalog(cat, chosen_num, _main_palette)
	return ei

# Create a random EquipmentInstance from a slot prefix (e.g., "hd","tr","ar","lg","fe","mc","w").
func create_random_from_prefix(prefix: String) -> EquipmentInstance:
	var bucket: Array = StarquillData.get_equipment_by_slot_prefix(prefix)
	if bucket.is_empty():
		return null

	var cat: EquipmentCatalog.CatalogItem = bucket[randi() % bucket.size()] as EquipmentCatalog.CatalogItem
	if cat == null:
		return null

	return create_from_catalog(cat)

# Fill a character with a randomized baseline + extras
func equip_random_set(ch: Character, extras: int = 4) -> void:
	if ch == null:
		return
	ch.clear_equipment()

	var main_prefixes: Array[String] = ["hd", "tr", "ar", "lg", "fe"]
	for p in main_prefixes:
		var ei := create_random_from_prefix(p)
		if ei != null:
			ch.equip_instance(ei)

	var pool: Array[String] = ["mc", "hd", "tr", "ar", "lg", "fe"]
	for _i in extras:
		var ei2 := create_random_from_prefix(pool[randi() % pool.size()])
		if ei2 != null:
			ch.equip_instance(ei2)

	if ch.has_signal("equipment_changed"):
		ch.emit_signal("equipment_changed")

# -------------------------------
# PRIVATE: palette loading
# Replace this with your actual CSV reader.
# Must return hex strings like "RRGGBB" or "#RRGGBB".
# -------------------------------
func _load_palette() -> PackedStringArray:
	# Use ColorManager singleton to get main palette
	var hex_colors = ColorManager.get_palette_hex("main")
	if hex_colors.is_empty():
		push_warning("[EquipmentFactory] main palette not found in ColorManager (using tiny fallback)")
		var out: PackedStringArray = PackedStringArray()
		out.push_back("E8D8C3")
		out.push_back("5A4632")
		out.push_back("7B3F00")
		out.push_back("4A6FA5")
		out.push_back("8F1D1D")
		out.push_back("356859")
		return out

	# Convert array to PackedStringArray and return
	var out_packed: PackedStringArray = PackedStringArray()
	for hex in hex_colors:
		out_packed.push_back(hex)
	return out_packed



# Removed _is_hex function - no longer needed since ColorManager provides validated hex colors
