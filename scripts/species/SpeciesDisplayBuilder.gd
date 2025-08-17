extends Node
class_name SpeciesDisplayBuilder

const MODULAR_PARTS_JSON := "res://assets/data/speciesModularParts.json"

const SPECIES_LAYER_MAP := {
	"f01": [84],          # eyes
	"f02": [86],          # nose
	"f03": [85],          # mouth
	"f04": [100],         # ears
	"f05": [122],         # facial hair
	"f06": [87],          # face detail
	"h01": [92],          # hair
	"h02": [92, 128],     # hair + hairtop
	"h03": [92, 154],     # hair + detail
	"h04": [92, 128, 154] # hair + detail + hairtop
}

const SKIN_LAYERS := [16, 37, 38, 82, 102]

static func freeze_species_to_instance(sp: Species) -> SpeciesInstance:
	var si := SpeciesInstance.new()
	si.species_id = sp.name
	si.x_scale = sp.x_scale
	si.y_scale = sp.y_scale
	si.skin_modulate_by_layer = _compute_skin_modulates(sp)

	# Route every field via _set_part() to auto-detect static vs modular
	_set_part(si, "backArm",       sp.backArm)
	_set_part(si, "body",          sp.body)
	_set_part(si, "ears",          sp.ears)
	_set_part(si, "eyes",          sp.eyes)
	_set_part(si, "facialDetail",  sp.facialDetail)
	_set_part(si, "facialHair",    sp.facialHair)
	_set_part(si, "frontArm",      sp.frontArm)
	_set_part(si, "hair",          sp.hair)
	_set_part(si, "head",          sp.head)
	_set_part(si, "legs",          sp.legs)
	_set_part(si, "mouth",         sp.mouth)
	_set_part(si, "nose",          sp.nose)
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

# ----------------- internals -----------------

static func _set_part(si: SpeciesInstance, key: String, v: Variant) -> void:
	# String (could be "####-###" static or "f01"/"h02" modular), or Array[String] (modular pool)
	if typeof(v) == TYPE_STRING:
		if v == "":
			return
		var re := RegEx.new(); re.compile("^(\\d{4})-(\\d{3})$")
		var m := re.search(v)
		if m:
			# Static path
			var image_num := m.get_string(1)
			var layer := int(m.get_string(2))
			si.parts[key] = { "kind":"static", "image_num":image_num, "layer":layer }
			return
		# Otherwise treat as modular type code
		_set_modular(si, key, v)
	elif typeof(v) == TYPE_ARRAY:
		# Array of modular type codes
		_set_modular(si, key, v)
	# else ignore

static func _set_modular(si: SpeciesInstance, key: String, v: Variant) -> void:
	var picked_type := ""
	match typeof(v):
		TYPE_STRING:
			picked_type = v
		TYPE_ARRAY:
			if v.size() > 0:
				picked_type = String(v[randi() % v.size()])
		_:
			return
	if picked_type == "":
		return
	if not SPECIES_LAYER_MAP.has(picked_type):
		push_warning("No layer map for modular type %s (%s)" % [picked_type, key])
		return

	var img_num := _pick_modular_image_num(picked_type)
	var layers: Array = SPECIES_LAYER_MAP[picked_type]
	si.parts[key] = { "kind":"modular", "type":picked_type, "image_num":img_num, "layers":layers }

static func _pick_modular_image_num(type_code: String) -> String:
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
	if f == null:
		return counts
	var data = JSON.parse_string(f.get_as_text())
	if typeof(data) == TYPE_DICTIONARY:
		for k in data.keys():
			counts[k] = int(data[k])
	elif typeof(data) == TYPE_ARRAY:
		var re := RegEx.new(); re.compile("^([a-z])(\\d+)$")
		for s in data:
			if typeof(s) != TYPE_STRING: continue
			var m := re.search(s)
			if m:
				counts[m.get_string(1)] = int(m.get_string(2))
	return counts

static func _compute_skin_modulates(sp: Species) -> Dictionary:
	var map: Dictionary = {}
	# 1) Variance overrides
	if sp.skinVariance_hex.size() > 0 and sp.skinVariance_indices.size() > 0:
		var hex_choice: String = String(sp.skinVariance_hex[int(randi() % sp.skinVariance_hex.size())])
		var col := Color(hex_choice)
		for layer in sp.skinVariance_indices:
			map[int(layer)] = col
		return map
	# 2) Normal skin palette or explicit hex list
	if sp.skin_color.size() == 0:
		return map
	if String(sp.skin_color[0]).begins_with("#"):
		var chosen_hex: String = String(sp.skin_color[int(randi() % sp.skin_color.size())])
		var col := Color(chosen_hex)
		for layer in SKIN_LAYERS:
			map[int(layer)] = col
	else:
		var palette: Array = species_loader.get_palette(sp.skin_color[0])
		if palette.size() > 0:
			var chosen_from_palette: String = String(palette[int(randi() % palette.size())])
			var col := Color(chosen_from_palette)
			for layer in SKIN_LAYERS:
				map[int(layer)] = col
	return map

static func _try_add_piece(pieces: Array, path: String, layer: int, mod_by_layer: Dictionary) -> void:
	if not ResourceLoader.exists(path):
		push_warning("Missing texture: " + path)
		return
	var tex := load(path)
	if tex == null:
		push_warning("Failed to load texture: " + path)
		return
	var v = mod_by_layer.get(layer, Color(1,1,1,1))
	var col: Color = v if typeof(v) == TYPE_COLOR else Color(1,1,1,1)
	pieces.append(DisplayPiece.make(tex, layer, col))
