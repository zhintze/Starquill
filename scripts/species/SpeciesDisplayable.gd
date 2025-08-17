extends Resource
class_name SpeciesDisplayable

var inst: SpeciesInstance   # frozen instance
var hidden_layers: Array[int] = []

func _init(si: SpeciesInstance) -> void:
	inst = si

func get_hidden_layers() -> Array:
	return hidden_layers

# Fixed schema for SPECIES layers
const LAYERS := {
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

# Species image folder
const SPECIES_IMG_DIR := "res://assets/images/species"

# Which layers get the base skin tint (when variance not overriding)
const SKIN_LAYERS := [16, 37, 38, 82, 102]

func get_display_pieces() -> Array:
	var pieces: Array[DisplayPiece] = []
	var tint_base := inst.skinColor

	# Apply variance override per-layer (wins over base tint)
	var tint_for_layer: Dictionary = {}
	for layer in SKIN_LAYERS:
		tint_for_layer[layer] = tint_base
	for k in inst.skinVarianceMap.keys():
		tint_for_layer[int(k)] = inst.skinVarianceMap[k]

	# 1) Static parts (####-###)
	_add_static(pieces, inst.backArm, tint_for_layer)
	_add_static(pieces, inst.body, tint_for_layer)
	_add_static(pieces, inst.frontArm, tint_for_layer)
	_add_static(pieces, inst.head, tint_for_layer)
	_add_static(pieces, inst.legs, tint_for_layer)

	# 2) Modular parts (f**, h**)
	_add_modular(pieces, inst.eyes, tint_for_layer)
	_add_modular(pieces, inst.nose, tint_for_layer)
	_add_modular(pieces, inst.mouth, tint_for_layer)
	_add_modular(pieces, inst.ears, tint_for_layer)
	_add_modular(pieces, inst.facialHair, tint_for_layer)
	_add_modular(pieces, inst.facialDetail, tint_for_layer)
	_add_modular(pieces, inst.hair, tint_for_layer)

	# 3) Other body parts if any (treat as static with increasing layers)
	if inst.otherBodyParts.size() > 0:
		var base_layer := 80
		for i in inst.otherBodyParts.size():
			_add_static_explicit(pieces, inst.otherBodyParts[i], base_layer + i, tint_for_layer)

	# Ensure z order (z == layer)
	pieces.sort_custom(func(a, b): return a.layer < b.layer)
	return pieces

# ---------------- helpers ----------------

func _add_static(pieces: Array, code: String, tint_for_layer: Dictionary) -> void:
	if code == "": return
	var re := RegEx.new(); re.compile("^(\\d{4})-(\\d{3})$")
	var m := re.search(code)
	if m == null:
		return   # not static
	var img_num := m.get_string(1)
	var layer := int(m.get_string(2))
	_add_static_explicit(pieces, img_num + "-" + ("%03d" % layer), layer, tint_for_layer)

func _add_static_explicit(pieces: Array, id: String, layer: int, tint_for_layer: Dictionary) -> void:
	var path := "%s/%s.png" % [SPECIES_IMG_DIR, id]
	if not ResourceLoader.exists(path):
		push_warning("Missing texture: " + path)
		return
	var col: Color = (tint_for_layer.get(layer, Color(1,1,1,1)) as Color)
	var dp := DisplayPiece.from_path(path, layer, col)
	if dp != null:
		pieces.append(dp)

func _add_modular(pieces: Array, type_code: String, tint_for_layer: Dictionary) -> void:
	if type_code == "" or not LAYERS.has(type_code):
		return
	# retrieve or pick a 4-digit image number
	var img_num: String = String(inst.modular_image_nums.get(type_code, ""))
	if img_num == "":
		img_num = SpeciesLoader.pick_modular_image_num(type_code)
		inst.modular_image_nums[type_code] = img_num

	for layer in LAYERS[type_code]:
		var path := "%s/%s-%s-%03d.png" % [SPECIES_IMG_DIR, type_code, img_num, int(layer)]
		if not ResourceLoader.exists(path):
			push_warning("Missing texture: " + path)
			continue
		var col: Color = (tint_for_layer.get(int(layer), Color(1,1,1,1)) as Color)
		var dp := DisplayPiece.from_path(path, int(layer), col)
		if dp != null:
			pieces.append(dp)
