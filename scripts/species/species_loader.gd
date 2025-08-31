# File: res://scripts/species/SpeciesLoader.gd
extends Node
class_name SpeciesLoader
# Ensure this script is Autoloaded as:  Name = species_loader, Path = this file

# ================================
# Instance registry
# ================================
var all: Array = []            # Array[Species]
var by_id: Dictionary = {}     # id/name (String) -> Species

func _ready() -> void:
	# no-op; explicit loading is preferred
	pass

func clear() -> void:
	all.clear()
	by_id.clear()

func register_species(s: Species) -> void:
	if s == null: return
	all.append(s)
	var key := String(s.name)
	by_id[key] = s

func size() -> int:
	return all.size()

func get_by_index(i: int) -> Species:
	if i < 0 or i >= all.size(): return null
	return all[i]

func get_by_id(id: String) -> Species:
	return by_id.get(id, null)

# ================================
# JSON loading (schema tolerant)
# ================================
# Expected file content:
#   [
#     {
#       "id": "human",
#       "x_scale": 1.0, "y_scale": 1.0,
#       "backArm": "0001-000", "body": "0001-000", "ears": "e01", ...
#       "skin_color": ["f6e0c8","e8c6a0"],                 # array of hex OR palette id in first elem
#       "skinVariance_hex": ["2a421c","a4633f"],           # optional
#       "skinVariance_indices": [1,2,3]                    # optional
#     },
#     ...
#   ]
func load_from_json(path: String) -> void:
	if not FileAccess.file_exists(path):
		push_warning("SpeciesLoader: JSON not found: %s" % path)
		return
	var text: String = FileAccess.get_file_as_string(path)
	var data: Variant = JSON.parse_string(text)  # <- explicit Variant

	if typeof(data) != TYPE_ARRAY:
		push_warning("SpeciesLoader: JSON root must be an array: %s" % path)
		return

	for item_v in data:                           # item_v is Variant
		if typeof(item_v) != TYPE_DICTIONARY:
			continue
		var item: Dictionary = item_v            # <- typed Dictionary
		var s: Species = Species.new()           # <- typed Species
		_map_dict_to_species(item, s)
		register_species(s)

# minimal, defensive mapper that tolerates strings or arrays where sensible
func _map_dict_to_species(d: Dictionary, s: Species) -> void:
	# id/name
	if d.has("id"):
		s.name = String(d.id)
	elif d.has("name"):
		s.name = String(d.name)

	# scales
	if d.has("x_scale"): s.x_scale = float(d.x_scale)
	if d.has("y_scale"): s.y_scale = float(d.y_scale)

	# string-ish part fields (store a string; if array provided, take first)
	for k in ["backArm","body","ears","eyes","facialDetail","facialHair","frontArm","hair","head","legs","mouth","nose"]:
		if d.has(k):
			s.set(k, _first_string(d[k]))

	# packed lists that your Species resource expects
	if d.has("otherBodyParts"):
		s.otherBodyParts = PackedStringArray(_to_string_array(d.otherBodyParts))
	if d.has("itemRestrictions"):
		s.itemRestrictions = PackedStringArray(_to_string_array(d.itemRestrictions))

	# colors/palettes (leave as arrays of strings/ints as caller expects)
	if d.has("skin_color"):
		s.skin_color = _to_string_array(d.skin_color)

	if d.has("skinVariance_hex"):
		s.skinVariance_hex = _to_string_array(d.skinVariance_hex)
	if d.has("skinVariance_indices"):
		s.skinVariance_indices = _to_int_array(d.skinVariance_indices)

# helpers for mapping
static func _first_string(v: Variant) -> String:
	match typeof(v):
		TYPE_STRING:
			return String(v)
		TYPE_ARRAY:
			var a: Array = v
			if a.is_empty(): return ""
			return String(a[0])
		_:
			return ""

static func _to_string_array(v: Variant) -> Array:
	var out: Array = []
	if typeof(v) == TYPE_ARRAY:
		for e in v:
			out.append(String(e))
	elif typeof(v) == TYPE_STRING:
		out.append(String(v))
	return out

static func _to_int_array(v: Variant) -> Array:
	var out: Array = []
	if typeof(v) == TYPE_ARRAY:
		for e in v:
			out.append(int(e))
	elif typeof(v) == TYPE_INT:
		out.append(int(v))
	return out

# ================================
# Instance creation helpers
# ================================
func create_random_instance_from_index(i: int) -> SpeciesInstance:
	var s := get_by_index(i)
	if s == null: return null
	return create_instance_from_species(s)

func create_instance_from_species(s: Species) -> SpeciesInstance:
	if s == null: return null
	var inst := SpeciesInstance.new()
	inst.from_species(s)
	return inst

# ================================
# Optional: bulk load Species *.tres/*.res from a folder
# ================================
func load_from_dir(dir_path: String) -> void:
	var d := DirAccess.open(dir_path)
	if d == null:
		push_warning("SpeciesLoader: directory not found: %s" % dir_path)
		return
	d.list_dir_begin()
	while true:
		var name: String = d.get_next()
		if name == "":
			break
		if d.current_is_dir():
			continue
		if not (name.ends_with(".tres") or name.ends_with(".res")):
			continue
		var p: String = dir_path.rstrip("/") + "/" + name
		var res: Resource = ResourceLoader.load(p)   # <- typed Resource
		if res is Species:
			register_species(res)
	d.list_dir_end()


# ================================
# Static palette & modular helpers (now uses ColorManager)
# ================================
static func get_palette(palette_ref: String) -> PackedStringArray:
	# Use ColorManager for all palette operations
	var hex_colors = ColorManager.get_palette_hex(palette_ref)
	var result := PackedStringArray()
	for hex in hex_colors:
		result.append(hex)
	return result

const MODULAR_CODE_COUNTS := {
	"f01": 40,
	"f02": 28,
	"e01": 32
}

static func pick_modular_image_num(code: String) -> String:
	var rng := RandomNumberGenerator.new()
	rng.randomize()
	var count := int(MODULAR_CODE_COUNTS.get(code, 50))
	if count <= 0:
		return "0000"
	var idx := rng.randi_range(0, count - 1)
	return "%04d" % idx

# Color management now handled by ColorManager autoload
