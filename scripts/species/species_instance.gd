# File: res://scripts/species/SpeciesInstance.gd
extends Resource
class_name SpeciesInstance

# cache for modular counts from JSON
static var __mod_counts_cache: Dictionary = {}
const MODULAR_PARTS_JSON := "res://assets/data/speciesModularParts.json"

# ---------- Color CSV constants ----------
# Removed: now uses ColorManager singleton instead of direct CSV access



# ---------- Persistent colors (new) ----------
var hairColor: Color = Color(1, 1, 1, 1)         # facial hair inherits this
var eyesColor: Color = Color(1, 1, 1, 1)
var facialDetailColor: Color = Color(1, 1, 1, 1)

@export var base: Species
@export var species_id: String = ""

# Chosen part codes (as provided by JSON; some may be group-only)
@export var backArm: String = ""
@export var body: String = ""
@export var ears: String = ""
@export var eyes: String = ""
@export var facialDetail: String = ""
@export var facialHair: String = ""
@export var frontArm: String = ""
@export var hair: String = ""        # chosen from array if needed
@export var head: String = ""
@export var legs: String = ""
@export var mouth: String = ""
@export var nose: String = ""

@export var otherBodyParts: PackedStringArray = PackedStringArray()
@export var itemRestrictions: PackedStringArray = PackedStringArray()

# Skin coloring (resolved)
@export var skinColor: Color = Color(1,1,1,1)
@export var skinVarianceColors: Dictionary = {}  # key: set_index (int) -> Color (chosen color for that variance set)

# Persistent modular selections
#   modular_image_nums: e.g. { "f01": "0043", "h02": "0081" }
@export var modular_image_nums: Dictionary = {}
#   for fields with multiple group options (e.g., hair), remember which group was picked
@export var modular_group_choice: Dictionary = {}   # { "hair": "h02" }

@export var x_scale: float = 1.0
@export var y_scale: float = 1.0

var _rng := RandomNumberGenerator.new()

func from_species(s: Species) -> void:
	base = s
	species_id = s.name

	# copy item restrictions & other parts
	itemRestrictions = s.itemRestrictions
	otherBodyParts = s.otherBodyParts

	# resolve each scalar field (string or group-only)
	backArm = _as_string(s.backArm)
	body = _as_string(s.body)
	ears = _as_string(s.ears)
	eyes = _as_string(s.eyes)
	facialDetail = _as_string(s.facialDetail)
	facialHair = _as_string(s.facialHair)
	frontArm = _as_string(s.frontArm)
	head = _as_string(s.head)
	legs = _as_string(s.legs)
	mouth = _as_string(s.mouth)
	nose = _as_string(s.nose)

	# hair: may be String OR Array[String] of group codes
	hair = _select_hair(s.hair)

	# pick persistent image numbers for any group-only tokens present
	for code in _present_modular_codes():
		if not modular_image_nums.has(code):
			var chosen := StarquillData.pick_modular_image_num(code)
			if chosen == "":
				push_warning("SpeciesInstance: modular type %s has no amounts; cannot pick image num" % [code])
			else:
				modular_image_nums[code] = chosen

	# Randomize and persist per-instance category colors (facial hair inherits hair)
	# New: category colors like skin_color (facialHair inherits hairColor)
	hairColor = _choose_category_color("hair", s.hair_color, Color(1,1,1,1))
	eyesColor = _choose_category_color("eyes", s.eyes_color, Color(1,1,1,1))
	facialDetailColor = _choose_category_color("facialDetail", s.facialDetail_color, Color(1,1,1,1))

	# skin tints
	_choose_skin_colors(s)
	


func _pick_color_from_csv(path: String, fallback: Color) -> Color:
	if not FileAccess.file_exists(path):
		push_warning("SpeciesInstance: palette CSV not found: %s" % path)
		return fallback

	var palette: Array = ColorList.load_palette_from_csv(path)
	if palette.is_empty():
		push_warning("SpeciesInstance: palette empty from %s" % path)
		return fallback

	var v: Variant = palette[_rng.randi_range(0, palette.size() - 1)]
	if v is Color:
		return v
	elif v is String:
		return _safe_color(v)
	else:
		push_warning("SpeciesInstance: non-color palette entry (%s) in %s" % [typeof(v), path])
		return fallback
	
func _as_string(v: Variant) -> String:
	if typeof(v) == TYPE_STRING:
		return String(v)
	return ""

func _select_hair(v: Variant) -> String:
	# hair can be: "" | "h02" | ["h01","h02",...]
	if typeof(v) == TYPE_NIL:
		return ""
	if typeof(v) == TYPE_STRING:
		var s := String(v)
		if s != "" and s.length() == 3:
			# group-only hair; pick image as persistent
			if not modular_image_nums.has(s):
				modular_image_nums[s] = StarquillData.pick_modular_image_num(s)
			modular_group_choice["hair"] = s
		return s
	if typeof(v) == TYPE_ARRAY:
		var arr: Array = v as Array
		if arr.is_empty():
			return ""
		# weighted pick across groups by available amounts (fair chance across all images)
		var groups: Array[String] = []
		var weights: Array[int] = []
		var total := 0
		for g in arr:
			if typeof(g) != TYPE_STRING: 
				continue
			var code := String(g)
			if code.length() != 3:
				continue
			var amt := _get_modular_count(code)
			if amt <= 0:
				continue
			groups.append(code)
			weights.append(amt)
			total += amt
		if groups.is_empty():
			return ""
		var roll := _rng.randi_range(1, total)
		var acc := 0
		var picked := groups[0]
		for i in groups.size():
			acc += int(weights[i])
			if roll <= acc:
				picked = groups[i]
				break
		modular_group_choice["hair"] = picked
		# ensure image num is chosen persistently
		if not modular_image_nums.has(picked):
			modular_image_nums[picked] = StarquillData.pick_modular_image_num(picked)
		return picked
	return ""

func _present_modular_codes() -> Array[String]:
	var out: Array[String] = []
	for token in [ears, eyes, facialDetail, facialHair, hair, mouth, nose]:
		if token is String and String(token).length() == 3:
			out.append(String(token))
	return out

func _choose_skin_colors(s: Species) -> void:
	# Choose base skin color from either a keyword CSV palette or direct hex list.
	var chosen_col := Color(1,1,1,1)

	var sc := s.skin_color
	if sc.size() > 0:
		var first := String(sc[0]).strip_edges()
		var looks_like_hex := first.begins_with("#") or first.to_lower().begins_with("0x") or _is_plain_hex(first)

		if sc.size() == 1 and not looks_like_hex:
			# Treat as keyword -> load palette CSV
			var colors := SkinColorList.get_colors(first)
			if colors.size() > 0:
				var hex := String(colors[int(_rng.randi() % colors.size())])
				chosen_col = _safe_color(hex)
			else:
				# Fallback: treat as hex anyway (will prefix # if needed)
				chosen_col = _safe_color(first)
		else:
			# Treat as list of hexes and pick one
			var pool := PackedStringArray()
			for v in sc:
				var h := String(v).strip_edges()
				if h == "":
					continue
				pool.append(h)
			if pool.size() > 0:
				var hex2 := String(pool[int(_rng.randi() % pool.size())])
				chosen_col = _safe_color(hex2)

	skinColor = chosen_col

	# Process multiple skinVariance sets
	skinVarianceColors.clear()
	if s.skinVariance_sets:
		for i in range(s.skinVariance_sets.size()):
			var variance_set = s.skinVariance_sets[i]
			if variance_set.has("hex_colors") and variance_set["hex_colors"].size() > 0:
				var hex_colors = variance_set["hex_colors"]
				var vhex := String(hex_colors[int(_rng.randi() % hex_colors.size())])
				skinVarianceColors[i] = _safe_color(vhex)


func get_variance_color_for_layer(layer: int) -> Color:
	# Check all variance sets to see if any contain this layer
	if base and base.skinVariance_sets:
		for i in range(base.skinVariance_sets.size()):
			var variance_set = base.skinVariance_sets[i]
			if variance_set.has("indices") and variance_set["indices"].has(layer):
				# Return the chosen color for this set
				return skinVarianceColors.get(i, Color(1,1,1,1))
	return Color(1,1,1,1)  # Default if no variance found

func _safe_color(hex: String) -> Color:
	var t := hex.strip_edges()
	if not t.begins_with("#") and not t.to_lower().begins_with("0x"):
		t = "#" + t
	return Color(t)
	
func _looks_like_hex(s: String) -> bool:
	var t := s.strip_edges()
	return t.begins_with("#") or t.to_lower().begins_with("0x") or _is_plain_hex(t)

func _is_plain_hex(s: String) -> bool:
	# Accept length 3 or 6 hex without #/0x
	var n := s.length()
	if n != 3 and n != 6:
		return false
	for i in n:
		var ch := s[i]
		var c := ch.to_lower()
		var ok := (c >= "0" and c <= "9") or (c >= "a" and c <= "f")
		if not ok:
			return false
	return true
	
func _load_keyword_palette_for_category(cat: String, keyword: String) -> Array[Color]:
	# Use ColorManager to get palettes from JSON
	var palette_name := "main"  # default palette
	
	if keyword == "default":
		palette_name = "main"
	else:
		# Try specific palette names based on category and keyword
		match cat:
			"hair":
				palette_name = "hair_%s" % keyword
			"eyes": 
				palette_name = "eyes_%s" % keyword
			"facialDetail":
				palette_name = "facialDetail_%s" % keyword
			_:
				palette_name = "main"
	
	# Get palette from ColorManager
	var colors = ColorManager.get_palette(palette_name)
	if colors.is_empty():
		push_warning("SpeciesInstance: palette '%s' not found in ColorManager. Falling back to main palette." % palette_name)
		colors = ColorManager.get_palette("main")
	
	return colors


# ===== utilities =====
static func _load_counts() -> void:
	if not __mod_counts_cache.is_empty():
		return
	if not FileAccess.file_exists(MODULAR_PARTS_JSON):
		return
	var f := FileAccess.open(MODULAR_PARTS_JSON, FileAccess.READ)
	if f == null:
		return
	var txt := f.get_as_text()
	f.close()
	var data = JSON.parse_string(txt)
	if typeof(data) != TYPE_ARRAY:
		return
	for e in data:
		if typeof(e) == TYPE_DICTIONARY and e.has("type") and e.has("amount"):
			__mod_counts_cache[String(e["type"]).to_lower()] = int(e["amount"])

static func _get_modular_count(code: String) -> int:
	_load_counts()
	return int(__mod_counts_cache.get(String(code).to_lower(), 0))
	
func _choose_category_color(cat: String, arr: PackedStringArray, fallback: Color) -> Color:
	# If Species provided explicit hex list, pick from that.
	if arr.size() > 0:
		var first := String(arr[0]).strip_edges()
		var looks_hex := _looks_like_hex(first)
		if looks_hex:
			# treat entire array as hex list
			var pool: Array[String] = []
			for h in arr:
				var hs := String(h).strip_edges()
				if _looks_like_hex(hs):
					pool.append(hs)
			if pool.size() > 0:
				var pick := pool[int(_rng.randi() % pool.size())]
				return _safe_color(pick)
		else:
			# treat first entry as keyword
			var kw := first.to_lower()
			var pal := _load_keyword_palette_for_category(cat, kw)
			if pal.size() > 0:
				return pal[int(_rng.randi() % pal.size())]

	# Fallback to main palette from ColorManager
	var pal2 := ColorManager.get_palette("main")
	if pal2.size() > 0:
		return pal2[int(_rng.randi() % pal2.size())]
	return fallback
