extends Node
class_name EquipmentFactory
# Consider setting this as an Autoload singleton if multiple scenes use it.

var _main_palette: PackedStringArray = PackedStringArray()

const PALETTE_PATH: String = "res://documents/color_main.csv" # <-- point this at your 1k-color file

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
	var out: PackedStringArray = PackedStringArray()

	if not FileAccess.file_exists(PALETTE_PATH):
		push_warning("[EquipmentFactory] palette file not found: " + PALETTE_PATH + " (using tiny fallback)")
		out.push_back("E8D8C3")
		out.push_back("5A4632")
		out.push_back("7B3F00")
		out.push_back("4A6FA5")
		out.push_back("8F1D1D")
		out.push_back("356859")
		return out

	var f: FileAccess = FileAccess.open(PALETTE_PATH, FileAccess.READ)
	if f == null:
		push_warning("[EquipmentFactory] failed to open: " + PALETTE_PATH)
		return out

	while not f.eof_reached():
		var line: String = f.get_line().strip_edges()
		if line == "" or line.begins_with("#"):
			continue

		var token: String = ""

		if line.find(",") >= 0:
			# CSV-style "r,g,b[,a]"
			var parts: PackedStringArray = line.split(",", false)
			if parts.size() >= 3:
				var r: int = clamp(int(parts[0].strip_edges()), 0, 255)
				var g: int = clamp(int(parts[1].strip_edges()), 0, 255)
				var b: int = clamp(int(parts[2].strip_edges()), 0, 255)
				token = "%02X%02X%02X" % [r, g, b]  # store as RRGGBB
		else:
			token = line

		# Normalize: remove leading '#', upper-case
		if token.begins_with("#"):
			token = token.substr(1)
		token = token.to_upper()

		# Accept 6 or 8 hex chars; keep as 6 (ignore alpha) for now
		if token.length() == 8:
			token = token.substr(0, 6)
		if token.length() == 6 and _is_hex(token):
			out.push_back(token)

	f.close()
	return out



func _is_hex(s: String) -> bool:
	for i in s.length():
		var ch := s[i]
		var ok := (ch >= '0' and ch <= '9') or (ch >= 'A' and ch <= 'F')
		if not ok:
			return false
	return true
