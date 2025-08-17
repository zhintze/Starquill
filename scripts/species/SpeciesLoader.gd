extends Node
class_name SpeciesLoader

signal species_loaded(count: int)

var by_name: Dictionary = {}   # name -> Species
var all: Array = []            # Array[Species]

# ---- CONFIG ----
const HUMAN_CSV_PATH := "res://documents/skin_colors.csv"  # rename/move your CSV to this path

# keyword(lowercase) -> SkinColorList
var _palette_registry: Dictionary = {}

static func _has_property(o: Object, prop: String) -> bool:
	for p in o.get_property_list():
		if typeof(p) == TYPE_DICTIONARY and p.has("name") and p["name"] == prop:
			return true
	return false

static func _set_if_present(o: Object, prop: String, val) -> void:
	if _has_property(o, prop):
		o.set(prop, val)

static func _assign_palette_if_present(s: Object, scl: Resource) -> void:
	var candidates := [
		"skinColorList",
		"skin_color_list",
		"skin_palette",
		"skinPalette"
	]
	for prop in candidates:
		if _has_property(s, prop):
			s.set(prop, scl)
			return

# -------- SkinColorList helpers --------

func _string_to_color(hex_like: String) -> Color:
	var t: String = hex_like.strip_edges()
	if t.begins_with("#"):
		t = t.substr(1, t.length() - 1)
	return Color.html(t)

func _make_skin_color_list(id_str: String, hexes: Array) -> SkinColorList:
	var scl := SkinColorList.new()
	scl.id = id_str
	for h in hexes:
		if typeof(h) == TYPE_STRING:
			var hs: String = (h as String).strip_edges()
			if not hs.is_empty():
				var c := _string_to_color(hs)
				scl.colors.append(c)
	return scl

func _duplicate_skin_color_list(src: SkinColorList, new_id: String = "") -> SkinColorList:
	var scl := SkinColorList.new()
	scl.id = (new_id if new_id != "" else src.id)
	for c in src.colors:
		scl.colors.append(c)
	return scl

func _load_human_palette_from_csv(path: String) -> void:
	var scl := SkinColorList.new()
	scl.id = "human"

	if not FileAccess.file_exists(path):
		push_warning("SpeciesLoader: Human CSV not found at %s; using empty palette." % path)
	else:
		var f := FileAccess.open(path, FileAccess.READ)
		if f == null:
			push_warning("SpeciesLoader: Cannot open CSV at %s; using empty palette." % path)
		else:
			while not f.eof_reached():
				var line: String = f.get_line().strip_edges()
				if line.is_empty():
					continue
				# Accept either "hex" header or id,hex rows; prefer last column as color token
				var token: String = line
				if "," in line:
					var parts := line.split(",", false)
					if parts.size() >= 2:
						token = (parts[parts.size() - 1] as String).strip_edges()
					else:
						token = (parts[0] as String).strip_edges()
				if token.begins_with("#"):
					token = token.substr(1, token.length() - 1)
				var lower := token.to_lower()
				if lower == "hex" or lower == "color" or lower == "colour":
					continue
				var c := Color.html(token)
				scl.colors.append(c)

	# Store only lowercase key
	_palette_registry["human"] = scl

func _ensure_default_palettes_loaded() -> void:
	if _palette_registry.is_empty():
		_load_human_palette_from_csv(HUMAN_CSV_PATH)

# --------------------------------------

func load_from_json(path: String) -> void:
	by_name.clear()
	all.clear()

	_ensure_default_palettes_loaded()

	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		push_error("SpeciesLoader: Cannot open %s" % path)
		return

	var text := f.get_as_text()
	var root = JSON.parse_string(text)
	if typeof(root) != TYPE_ARRAY:
		push_error("SpeciesLoader: Root must be an array in %s" % path)
		return

	for entry in root:
		if typeof(entry) != TYPE_DICTIONARY:
			continue

		var s := Species.new()
		for k in entry.keys():
			_set_if_present(s, k, entry[k])

		# Resolve skin_color -> SkinColorList (array of hexes OR keyword)
		if entry.has("skin_color"):
			var val = entry["skin_color"]
			if typeof(val) == TYPE_ARRAY:
				var scl := _make_skin_color_list("inline_%s" % (entry.get("name", "unnamed")), val)
				_assign_palette_if_present(s, scl)
			elif typeof(val) == TYPE_STRING:
				var key_str: String = String(val).strip_edges()
				var key_lc: String = key_str.to_lower()
				if _palette_registry.has(key_lc):
					var cloned := _duplicate_skin_color_list(
						_palette_registry[key_lc],
						"%s_%s" % [key_lc, entry.get("name", "")]
					)
					_assign_palette_if_present(s, cloned)
				else:
					push_warning("SpeciesLoader: Unknown skin_color keyword '%s' for '%s' — leaving palette unset." % [key_str, entry.get("name", "")])

		# name key
		var nm := ""
		if _has_property(s, "name") and typeof(s.name) == TYPE_STRING:
			nm = s.name

		if nm != "":
			by_name[nm] = s
		all.append(s)

	emit_signal("species_loaded", all.size())

func create_random_instance(species_name: String) -> SpeciesInstance:
	var s: Species = by_name.get(species_name, null)
	if s == null:
		push_error("SpeciesLoader: Unknown species '%s'" % species_name)
		return null
	var inst := SpeciesInstance.new()
	inst.from_species(s)
	return inst

func create_random_instance_from_index(idx: int) -> SpeciesInstance:
	if idx < 0 or idx >= all.size():
		push_error("SpeciesLoader: Index out of range %d" % idx)
		return null
	var inst := SpeciesInstance.new()
	inst.from_species(all[idx])
	return inst
