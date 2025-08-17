extends Node
class_name SpeciesLoader

signal species_loaded(count: int)

var by_name: Dictionary = {}   # name -> Species
var all: Array[Species] = []

const SPECIES_JSON_DEFAULT := "res://assets/data/species.json"
const PALETTE_CSV_PATTERN  := "res://documents/skin_color_%s.csv"  # preferred (skin_color_human.csv)
const PALETTE_CSV_FALLBACK := "res://documents/skin_colors.csv"    # flat file; hex-per-line
const MODULAR_PARTS_JSON   := "res://assets/data/speciesModularParts.json"

# Cache
static var _palette_cache: Dictionary = {}   # keyword -> ["#hex", ...]
static var _modular_counts: Dictionary = {}  # "f01"->n, "h02"->n, plus letter totals "h"->sum

func _ready() -> void:
	if all.is_empty():
		load_from_json(SPECIES_JSON_DEFAULT)

# ---------------- Public API ----------------

func load_from_json(path: String) -> void:
	by_name.clear()
	all.clear()

	if not FileAccess.file_exists(path):
		push_error("SpeciesLoader: Missing " + path)
		return

	# modular counts on first load
	if _modular_counts.is_empty():
		_load_modular_counts()

	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		push_error("SpeciesLoader: Cannot open " + path)
		return

	var root = JSON.parse_string(f.get_as_text())
	if typeof(root) != TYPE_ARRAY:
		push_error("SpeciesLoader: Root must be an array in %s" % path)
		return

	for row in root:
		if typeof(row) != TYPE_DICTIONARY:
			continue
		var s := Species.new()
		s.name = String(row.get("name", ""))

		s.backArm = String(row.get("backArm", ""))
		s.body = String(row.get("body", ""))
		s.ears = String(row.get("ears", ""))
		s.eyes = String(row.get("eyes", ""))
		s.facialDetail = String(row.get("facialDetail", ""))
		s.facialHair = String(row.get("facialHair", ""))
		s.frontArm = String(row.get("frontArm", ""))
		s.hair = row.get("hair", null)              # may be String or Array
		s.head = String(row.get("head", ""))
		s.legs = String(row.get("legs", ""))
		s.mouth = String(row.get("mouth", ""))
		s.nose = String(row.get("nose", ""))

		var obp: Array = row.get("otherBodyParts", [])
		s.otherBodyParts = PackedStringArray(obp)

		var ir: Array = row.get("itemRestrictions", [])
		s.itemRestrictions = PackedStringArray(ir)

		var sc: Array = row.get("skin_color", [])
		s.skin_color = PackedStringArray(sc)

		var svh: Array = row.get("skinVariance_hex", [])
		s.skinVariance_hex = PackedStringArray(svh)

		var svi: Array = row.get("skinVariance_indices", [])
		s.skinVariance_indices = PackedInt32Array(svi)

		s.x_scale = float(row.get("x_scale", 1.0))
		s.y_scale = float(row.get("y_scale", 1.0))

		if s.name != "":
			by_name[s.name] = s
			all.append(s)

	emit_signal("species_loaded", all.size())

func get_species(name: String) -> Species:
	return by_name.get(name, null)

func create_instance(species_name: String) -> SpeciesInstance:
	var s := get_species(species_name)
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

# Palettes -------------------------------------------------------------

static func get_palette(keyword: String) -> Array:
	var key := keyword.to_lower()
	if _palette_cache.has(key):
		return _palette_cache[key]
	var path := PALETTE_CSV_PATTERN % key
	var arr: Array = []
	if FileAccess.file_exists(path):
		arr = _read_palette_csv(path)
	elif FileAccess.file_exists(PALETTE_CSV_FALLBACK):
		arr = _read_palette_csv(PALETTE_CSV_FALLBACK)
	else:
		push_warning("SpeciesLoader: Palette CSV not found: %s or %s" % [path, PALETTE_CSV_FALLBACK])
	_palette_cache[key] = arr
	return arr

static func _read_palette_csv(path: String) -> Array:
	var out: Array = []
	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		return out
	while not f.eof_reached():
		var line := f.get_line().strip_edges()
		if line == "" or line.begins_with("#") or line.begins_with("//"):
			continue
		for token in line.split(","):
			var t := String(token).strip_edges()
			if t == "" or t.to_lower() in ["hex","color","col","palette"]:
				continue
			var norm := _normalize_hex(t)
			if norm != "":
				out.append(norm)
	return out

static func _normalize_hex(s: String) -> String:
	var t := s.strip_edges()
	if t == "":
		return ""
	if t.begins_with("0x") or t.begins_with("0X"):
		t = t.substr(2)
	if not t.begins_with("#"):
		t = "#" + t
	if t.length() == 4 or t.length() == 5:
		var r := t[1]; var g := t[2]; var b := t[3]
		if t.length() == 4: return "#" + r + r + g + g + b + b
		var a := t[4]; return "#" + r + r + g + g + b + b + a + a
	if t.length() == 7 or t.length() == 9:
		return t
	push_warning("SpeciesLoader: Ignoring invalid hex '%s'" % s)
	return ""

# Modular counts -------------------------------------------------------

static func _load_modular_counts() -> void:
	if not _modular_counts.is_empty():
		return
	if not FileAccess.file_exists(MODULAR_PARTS_JSON):
		push_warning("SpeciesLoader: Missing " + MODULAR_PARTS_JSON)
		return
	var f := FileAccess.open(MODULAR_PARTS_JSON, FileAccess.READ)
	if f == null:
		return
	var root = JSON.parse_string(f.get_as_text())
	if typeof(root) == TYPE_ARRAY:
		for rec in root:
			if typeof(rec) != TYPE_DICTIONARY: continue
			var typ := String(rec.get("type",""))
			var amt := int(rec.get("amount", 0))
			if typ != "" and amt > 0:
				_modular_counts[typ] = amt
				var letter := typ.substr(0,1)
				_modular_counts[letter] = int(_modular_counts.get(letter, 0)) + amt

static func pick_modular_image_num(type_code: String) -> String:
	_load_modular_counts()
	var n := 0
	if _modular_counts.has(type_code):
		n = int(_modular_counts[type_code])
	else:
		n = int(_modular_counts.get(type_code.substr(0,1), 0))
	if n <= 0: n = 1
	var rng := RandomNumberGenerator.new()
	rng.randomize()
	var choice := 1 + (rng.randi() % n)
	return "%04d" % choice
