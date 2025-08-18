extends Node
class_name SpeciesDisplayBuilder

const SPECIES_JSON := "res://assets/data/species.json"
const MODULAR_PARTS_JSON := "res://assets/data/speciesModularParts.json"

const SPECIES_LAYER_MAP := {
	"f01": [84], "f02": [86], "f03": [85], "f04": [100], "f05": [122], "f06": [87],
	"h01": [92], "h02": [92, 128], "h03": [92, 154], "h04": [92, 128, 154]
}
const SKIN_LAYERS := [16, 37, 38, 82, 102]

# --- cache ---
var _loaded: bool = false
var _species_by_name: Dictionary = {}     # String -> Species
var _species_names: Array[String] = []

# --- public instance API (autoload this script as `species_display_builder`) ---
func get_species(name: String) -> Species:
	_ensure_loaded()
	return _species_by_name.get(name, null) as Species

func list_species_names() -> Array[String]:
	_ensure_loaded()
	return _species_names.duplicate()

# --- load once ---
func _ensure_loaded() -> void:
	if _loaded: return
	_loaded = true
	_species_by_name.clear()
	_species_names.clear()

	var text: String = FileAccess.get_file_as_string(SPECIES_JSON)
	if text == "":
		push_error("SpeciesDisplayBuilder: Could not read " + SPECIES_JSON)
		return

	var parsed: Variant = JSON.parse_string(text)
	if parsed == null or typeof(parsed) != TYPE_ARRAY:
		push_error("SpeciesDisplayBuilder: species.json must be an array of species objects")
		return

	var arr: Array = parsed as Array
	for entry in arr:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var e: Dictionary = entry as Dictionary
		if not e.has("name"):
			continue
		var name := String(e["name"])
		var s: Species = _make_species(name, e)
		_species_by_name[name] = s
		_species_names.append(name)

	_species_names.sort()

func _make_species(name: String, data: Dictionary) -> Species:
	var s: Species = Species.new()
	s.name = name
	s.x_scale = float(data.get("x_scale", 1.0))
	s.y_scale = float(data.get("y_scale", 1.0))

	s.skinVariance_hex = data.get("skinVariance_hex", []) as Array
	s.skinVariance_indices = data.get("skinVariance_indices", []) as Array
	s.skin_color = data.get("skin_color", []) as Array

	s.backArm = String(data.get("backArm", ""))
	s.body = String(data.get("body", ""))
	s.ears = String(data.get("ears", ""))
	s.eyes = String(data.get("eyes", ""))
	s.facialDetail = String(data.get("facialDetail", ""))
	s.facialHair = String(data.get("facialHair", ""))
	s.frontArm = String(data.get("frontArm", ""))
	# hair may be array or string => store as Variant and let freeze handle it
	s.hair = data.get("hair", [])
	s.head = String(data.get("head", ""))
	s.legs = String(data.get("legs", ""))
	s.mouth = String(data.get("mouth", ""))
	s.nose = String(data.get("nose", ""))

	# Optional arrays we’re not using yet
	# s.itemRestrictions = data.get("itemRestrictions", []) as Array
	# s.otherBodyParts = data.get("otherBodyParts", []) as Array
	return s

# ================== existing code you already had ==================
# Create a frozen instance directly from a Species (uses SpeciesInstance.from_species)
static func freeze_species_to_instance(sp: Species) -> SpeciesInstance:
	var si := SpeciesInstance.new()
	si.from_species(sp)

	# Build layer→color map for rendering
	si.skin_modulate_by_layer = _compute_skin_modulates_from_instance(si)

	# Route every chosen field via _set_part() (strings only now)
	_set_part(si, "backArm",      si.backArm)
	_set_part(si, "body",         si.body)
	_set_part(si, "ears",         si.ears)
	_set_part(si, "eyes",         si.eyes)
	_set_part(si, "facialDetail", si.facialDetail)
	_set_part(si, "facialHair",   si.facialHair)
	_set_part(si, "frontArm",     si.frontArm)
	_set_part(si, "hair",         si.hair)
	_set_part(si, "head",         si.head)
	_set_part(si, "legs",         si.legs)
	_set_part(si, "mouth",        si.mouth)
	_set_part(si, "nose",         si.nose)
	return si

static func build_display_pieces(si: SpeciesInstance) -> Array[DisplayPiece]:
	var pieces: Array[DisplayPiece] = []
	for key in si.parts.keys():
		var p: Dictionary = si.parts[key]
		var kind := String(p.get("kind", ""))
		match kind:
			"static":
				var image_num := String(p.get("image_num", "0001"))
				var layer: int = int(p.get("layer", 0))
				var tex_path := "res://assets/images/species/%s-%03d.png" % [image_num, layer]
				_try_add_piece(pieces, tex_path, layer, si.skin_modulate_by_layer)
			"modular":
				var type_code := String(p.get("type", ""))
				var image_num := String(p.get("image_num", "0001"))
				var layers: Array = p.get("layers", [])
				for layer_val in layers:
					var layer: int = int(layer_val)
					var tex_path := "res://assets/images/species/%s-%s-%03d.png" % [type_code, image_num, layer]
					_try_add_piece(pieces, tex_path, layer, si.skin_modulate_by_layer)
			_:
				push_warning("SpeciesDisplayBuilder: Unknown part kind %s for key %s" % [kind, key])
	pieces.sort_custom(func(a, b): return a.layer < b.layer)
	return pieces

static func _set_part(si: SpeciesInstance, key: String, v: Variant) -> void:
	if typeof(v) == TYPE_STRING:
		if v == "": return
		var re := RegEx.new(); re.compile("^(\\d{4})-(\\d{3})$")
		var m := re.search(v)
		if m:
			var image_num := m.get_string(1)
			var layer := int(m.get_string(2))
			si.parts[key] = { "kind":"static", "image_num":image_num, "layer":layer }
			return
		_set_modular(si, key, v)
	elif typeof(v) == TYPE_ARRAY:
		_set_modular(si, key, v)

# Prefer instance’s modular choice; fallback to counts
static func _set_modular(si: SpeciesInstance, key: String, v: Variant) -> void:
	var picked_type := ""
	match typeof(v):
		TYPE_STRING:
			picked_type = String(v)
		TYPE_ARRAY:
			var arr: Array = v
			if arr.size() > 0:
				picked_type = String(arr[randi() % arr.size()])
		_:
			return
	if picked_type == "":
		return
	if not SPECIES_LAYER_MAP.has(picked_type):
		push_warning("No layer map for modular type %s (%s)" % [picked_type, key])
		return

	var img_num := ""
	# 1) Use pre-picked number if present on the instance
	if si.modular_image_nums.has(picked_type):
		img_num = String(si.modular_image_nums[picked_type])
	else:
		# 2) Fallback to deterministic picker
		img_num = _pick_modular_image_num(picked_type)

	var layers: Array = SPECIES_LAYER_MAP[picked_type]
	si.parts[key] = { "kind":"modular", "type":picked_type, "image_num":img_num, "layers":layers }


# If SpeciesLoader provides counts/palette, use that
static func _pick_modular_image_num(type_code: String) -> String:
	# Prefer SpeciesLoader if present (your SpeciesInstance already uses it)
	if Engine.has_singleton("SpeciesLoader"):
		var n: int = 0
		# If you have a direct "get_modular_count(type_code)" use that here.
		# Otherwise keep the JSON-based fallback below.
		# n = SpeciesLoader.get_modular_count(type_code)
		if n > 0:
			var choice := 1 + (randi() % n)
			return "%04d" % choice

	# Fallback: JSON counts
	var counts := _load_modular_counts()
	var n := 0
	if counts.has(type_code):
		n = int(counts[type_code])
	else:
		var letter := type_code.substr(0, 1)
		n = int(counts.get(letter, 0))
	if n <= 0:
		push_warning("No count for modular type %s; defaulting to 1" % type_code)
		n = 1
	var choice := 1 + (randi() % n)
	return "%04d" % choice

static func _load_modular_counts() -> Dictionary:
	var counts: Dictionary = {}
	if not FileAccess.file_exists(MODULAR_PARTS_JSON):
		push_warning("Missing modular counts: " + MODULAR_PARTS_JSON)
		return counts
	var f := FileAccess.open(MODULAR_PARTS_JSON, FileAccess.READ)
	if f == null: return counts
	var data : = JSON.parse_string(f.get_as_text()) as Dictionary
	if typeof(data) == TYPE_DICTIONARY:
		for k in data.keys(): counts[k] = int(data[k])
	elif typeof(data) == TYPE_ARRAY:
		var re := RegEx.new(); re.compile("^([a-z])(\\d+)$")
		for s in data:
			if typeof(s) != TYPE_STRING: continue
			var m := re.search(s)
			if m: counts[m.get_string(1)] = int(m.get_string(2))
	return counts

# Build skin_modulate_by_layer from instance fields (not Species)
static func _compute_skin_modulates_from_instance(si: SpeciesInstance) -> Dictionary:
	var map: Dictionary = {}
	# 1) Variance map (already resolved by from_species)
	if si.skinVarianceMap.size() > 0:
		for k in si.skinVarianceMap.keys():
			map[int(k)] = si.skinVarianceMap[k]
		return map
	# 2) Fallback to a uniform skinColor across standard layers
	for layer in SKIN_LAYERS:
		map[int(layer)] = si.skinColor
	return map

static func _try_add_piece(pieces: Array, path: String, layer: int, mod_by_layer: Dictionary) -> void:
	if not ResourceLoader.exists(path):
		push_warning("Missing texture: " + path); return
	var tex := load(path)
	if tex == null:
		push_warning("Failed to load texture: " + path); return
	var v = mod_by_layer.get(layer, Color(1,1,1,1))
	var col: Color = v if typeof(v) == TYPE_COLOR else Color(1,1,1,1)
	pieces.append(DisplayPiece.make(tex, layer, col))
