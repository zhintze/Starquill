extends Node
class_name EquipmentCatalog

# One catalog row lifted from equipment.json
class CatalogItem:
	var item_type: String
	var description: String
	var amount: int
	var layer_codes: PackedInt32Array
	var hidden_layers: PackedInt32Array
	var modular: bool
	var layer_color_variance: PackedInt32Array

	func _init(d: Dictionary) -> void:
		item_type = String(d.get("item_type", ""))
		description = String(d.get("description", ""))
		amount = int(d.get("amount", 0))
		layer_codes = PackedInt32Array(d.get("layer_codes", []))
		hidden_layers = PackedInt32Array(d.get("hidden_layers", []))
		modular = bool(d.get("modular", false))
		layer_color_variance = PackedInt32Array(d.get("layer_color_variance", []))

var all: Array[CatalogItem] = []
var by_type: Dictionary = {} # item_type -> CatalogItem
var by_slot_prefix: Dictionary = { "hd": [], "tr": [], "ar": [], "lg": [], "fe": [], "mc": [] }

func clear() -> void:
	all.clear()
	by_type.clear()
	for k in by_slot_prefix.keys(): by_slot_prefix[k] = []

func load_from_json(path: String) -> void:
	clear()
	if not FileAccess.file_exists(path):
		push_error("EquipmentCatalog: file not found: %s" % path); return
	var txt := FileAccess.get_file_as_string(path)
	var arr := JSON.parse_string(txt) as Array
	if typeof(arr) != TYPE_ARRAY:
		push_error("EquipmentCatalog: root must be array"); return
	for row in (arr as Array):
		if typeof(row) != TYPE_DICTIONARY: continue
		var item := CatalogItem.new(row)
		if item.item_type == "" or item.amount <= 0 or item.layer_codes.is_empty():
			continue
		all.append(item)
		by_type[item.item_type] = item
		var pref := item.item_type.substr(0, 2).to_lower()
		if by_slot_prefix.has(pref):
			(by_slot_prefix[pref] as Array).append(item)

static func slot_for_item_type(item_type: String) -> String:
	var pref := item_type.substr(0, 2).to_lower()
	match pref:
		"hd": return "head"
		"tr": return "torso"
		"ar": return "arms"
		"lg": return "legs"
		"fe": return "feet"
		"mc": return "misc"
		_:    return "misc"
