extends Node
class_name EquipmentFactory
# Consider setting this as an Autoload singleton if multiple scenes use it.

var _main_palette: PackedStringArray = PackedStringArray()

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
	if equipment_catalog == null:
		push_error("EquipmentFactory: equipment_catalog autoload missing.")
		return null

	var bucket_any: Variant = equipment_catalog.by_slot_prefix.get(prefix, [])
	if bucket_any == null:
		return null
	var bucket: Array = bucket_any as Array
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
	var out := PackedStringArray()
	# TODO: swap with your CSV loader (e.g., res://documents/color_main.csv)
	# Temporary sensible defaults to prove the pipeline:
	out.push_back("E8D8C3") # light leather
	out.push_back("5A4632") # dark leather
	out.push_back("7B3F00") # brown
	out.push_back("4A6FA5") # steel-blue cloth
	out.push_back("8F1D1D") # deep red
	out.push_back("356859") # teal
	return out
