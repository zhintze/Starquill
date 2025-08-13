extends Resource
class_name SpeciesInstance

var base: Species

# Concrete values chosen/copied from base:
var name: String = ""
var backArm := ""
var body := ""
var ears := ""
var eyes := ""
var facialDetail := ""
var facialHair := ""
var frontArm := ""
var head := ""
var legs := ""
var mouth := ""
var nose := ""

var hair := ""                     # chosen from array if available
var otherBodyParts: Array = []     # copied
var itemRestrictions: Array = []   # copied
var skinVariance_hex := ""         # chosen
var skinVariance_indices = null    # chosen (int or null)
var skin_color := ""               # chosen (may be symbolic like "human")

var x_scale: float = 1.0
var y_scale: float = 1.0

var _rng := RandomNumberGenerator.new()

static func _has_property(o: Object, prop: String) -> bool:
	for p in o.get_property_list():
		if typeof(p) == TYPE_DICTIONARY and p.has("name") and p["name"] == prop:
			return true
	return false

static func _get_if_present(o: Object, prop: String, default_value = null):
	if _has_property(o, prop):
		return o.get(prop)
	return default_value

func _pick(arr: Array):
	if arr.is_empty():
		return null
	return arr[_rng.randi_range(0, arr.size() - 1)]

func _str_or_empty(v) -> String:
	if typeof(v) == TYPE_STRING:
		return v
	return ""

func from_species(s: Species) -> void:
	base = s
	_rng.randomize()

	# Scalars/strings (copy-through if present)
	var tmp
	tmp = _get_if_present(s, "name", "");           name = _str_or_empty(tmp)
	tmp = _get_if_present(s, "backArm", "");        backArm = _str_or_empty(tmp)
	tmp = _get_if_present(s, "body", "");           body = _str_or_empty(tmp)
	tmp = _get_if_present(s, "ears", "");           ears = _str_or_empty(tmp)
	tmp = _get_if_present(s, "eyes", "");           eyes = _str_or_empty(tmp)
	tmp = _get_if_present(s, "facialDetail", "");   facialDetail = _str_or_empty(tmp)
	tmp = _get_if_present(s, "facialHair", "");     facialHair = _str_or_empty(tmp)
	tmp = _get_if_present(s, "frontArm", "");       frontArm = _str_or_empty(tmp)
	tmp = _get_if_present(s, "head", "");           head = _str_or_empty(tmp)
	tmp = _get_if_present(s, "legs", "");           legs = _str_or_empty(tmp)
	tmp = _get_if_present(s, "mouth", "");          mouth = _str_or_empty(tmp)
	tmp = _get_if_present(s, "nose", "");           nose = _str_or_empty(tmp)

	# Arrays (randomize if present; keep empty/null if not)
	var _hair = _get_if_present(s, "hair", [])
	if typeof(_hair) == TYPE_ARRAY and not _hair.is_empty():
		hair = _pick(_hair)
	else:
		hair = ""

	var _obp = _get_if_present(s, "otherBodyParts", [])
	if typeof(_obp) == TYPE_ARRAY:
		otherBodyParts = _obp.duplicate()
	else:
		otherBodyParts = []

	var _restr = _get_if_present(s, "itemRestrictions", [])
	if typeof(_restr) == TYPE_ARRAY:
		itemRestrictions = _restr.duplicate()
	else:
		itemRestrictions = []

	var _sv_hex = _get_if_present(s, "skinVariance_hex", [])
	if typeof(_sv_hex) == TYPE_ARRAY and not _sv_hex.is_empty():
		skinVariance_hex = _pick(_sv_hex)
	else:
		skinVariance_hex = ""

	var _sv_idx = _get_if_present(s, "skinVariance_indices", [])
	if typeof(_sv_idx) == TYPE_ARRAY and not _sv_idx.is_empty():
		skinVariance_indices = _pick(_sv_idx)
	else:
		skinVariance_indices = null

	var _skin = _get_if_present(s, "skin_color", [])
	if typeof(_skin) == TYPE_ARRAY and not _skin.is_empty():
		skin_color = _pick(_skin)
	else:
		skin_color = ""

	# Scales (copy if present; otherwise defaults)
	var sx = _get_if_present(s, "x_scale", 1.0)
	if typeof(sx) in [TYPE_FLOAT, TYPE_INT]:
		x_scale = float(sx)
	else:
		x_scale = 1.0

	var sy = _get_if_present(s, "y_scale", 1.0)
	if typeof(sy) in [TYPE_FLOAT, TYPE_INT]:
		y_scale = float(sy)
	else:
		y_scale = 1.0
