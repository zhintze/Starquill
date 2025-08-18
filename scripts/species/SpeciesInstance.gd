# File: res://scripts/species/SpeciesInstance.gd
extends Resource
class_name SpeciesInstance

@export var base: Species
@export var species_id: String = ""

# Chosen part codes
@export var backArm: String = ""
@export var body: String = ""
@export var ears: String = ""
@export var eyes: String = ""
@export var facialDetail: String = ""
@export var facialHair: String = ""
@export var frontArm: String = ""
@export var hair: String = ""
@export var head: String = ""
@export var legs: String = ""
@export var mouth: String = ""
@export var nose: String = ""

# Lists
@export var otherBodyParts: PackedStringArray = PackedStringArray()
@export var itemRestrictions: PackedStringArray = PackedStringArray()

# Scale
@export var x_scale: float = 1.0
@export var y_scale: float = 1.0

# Colors
@export var skinColor: Color = Color(1,1,1,1)
@export var skinVarianceMap: Dictionary = {}      # layer:int -> Color

# Modular image selections, e.g. { "f01": "0023" }
@export var modular_image_nums: Dictionary = {}

var _rng := RandomNumberGenerator.new()

# Render/build-time
var parts: Dictionary = {}
var skin_modulate_by_layer: Dictionary = {}

func _ready() -> void:
	_rng.randomize()

func from_species(s: Species) -> void:
	base = s
	species_id = s.name

	# Scales
	x_scale = s.x_scale
	y_scale = s.y_scale

	# Pick/copy strings
	backArm = String(s.backArm)
	body = String(s.body)
	ears = _pick_string_or_first(s.ears)
	eyes = _pick_string_or_first(s.eyes)
	facialDetail = _pick_string_or_first(s.facialDetail)
	facialHair = _pick_string_or_first(s.facialHair)
	frontArm = String(s.frontArm)
	hair = _pick_string_from_variant(s.hair)
	head = String(s.head)
	legs = String(s.legs)
	mouth = _pick_string_or_first(s.mouth)
	nose = _pick_string_or_first(s.nose)

	# Lists (packed→packed)
	otherBodyParts = s.otherBodyParts
	itemRestrictions = s.itemRestrictions

	# Colors
	_resolve_skin_colors(s)

	# Modular image numbers
	for code in _present_modular_codes():
		modular_image_nums[code] = SpeciesLoader.pick_modular_image_num(code)

# ---------- helpers ----------

func _present_modular_codes() -> Array[String]:
	var out: Array[String] = []
	for v in [ears, eyes, facialDetail, facialHair, hair, mouth, nose]:
		if v != "" and not _is_static_code(v):
			out.append(v)
	return out

func _is_static_code(v: String) -> bool:
	var re := RegEx.new()
	re.compile("^\\d{4}-\\d{3}$")
	return re.search(v) != null

func _pick_string_from_variant(v: Variant) -> String:
	match typeof(v):
		TYPE_STRING:
			return String(v)
		TYPE_ARRAY:
			var a: Array = v
			if a.is_empty(): return ""
			return String(a[_rng.randi() % a.size()])
		_:
			return ""

func _pick_string_or_first(v: Variant) -> String:
	match typeof(v):
		TYPE_STRING:
			return String(v)
		TYPE_ARRAY:
			var a: Array = v
			if a.is_empty(): return ""
			return String(a[0])
		_:
			return ""

func _resolve_skin_colors(s: Species) -> void:
	# 1) Variance override
	if s.skinVariance_hex.size() > 0 and s.skinVariance_indices.size() > 0:
		var hexes: Array = s.skinVariance_hex
		var chosen_hex := String(hexes[int(_rng.randi() % hexes.size())])
		var col := _safe_color(chosen_hex)
		for layer in s.skinVariance_indices:
			skinVarianceMap[int(layer)] = col

	# 2) Base skin color (palette id or explicit hex list)
	if s.skin_color.size() > 0:
		var key := String(s.skin_color[0])
		var chosen_col := Color(1,1,1,1)
		if SpeciesLoader._is_hex_color(key) or key.begins_with("#"):
			var hex := String(s.skin_color[int(_rng.randi() % s.skin_color.size())])
			chosen_col = _safe_color(hex)
		else:
			var palette: PackedStringArray = SpeciesLoader.get_palette(key)
			if palette.size() > 0:
				var hex2 := String(palette[int(_rng.randi() % palette.size())])
				chosen_col = _safe_color(hex2)
		skinColor = chosen_col

func _safe_color(hex: String) -> Color:
	var t := hex.strip_edges()
	if not t.begins_with("#") and not t.to_lower().begins_with("0x"):
		t = "#" + t
	return Color(t)
