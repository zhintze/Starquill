extends Resource

const SPECIES_IMG_DIR := "res://assets/images/species"

var inst: SpeciesInstance   # frozen instance

func _init(si: SpeciesInstance) -> void:
	inst = si

func get_hidden_layers() -> Array:
	# Species does not use hidden layers; equipment will.
	return []

# ===== Layer map for modular group types =====
# Add new group types here; the right side lists all layers that group renders on.
const LAYERS := {
	# Face groups
	"f01": [84],          # eyes
	"f02": [86],          # nose
	"f03": [85],          # mouth
	"f04": [100],         # ears
	"f05": [122],         # facialHair
	"f06": [87],          # facialDetail

	# Hair groups (front/back overlays)
	"h01": [92],
	"h02": [92, 128],
	"h03": [92, 154],
	"h04": [92, 128, 154],

	# Dwarf/gnome variants (mirror of f** families)
	"d02": [86],
	"d04": [100],
	"d05": [122],
	"d06": [87],
	"g02": [86],

	# Skeleton families
	"s01": [82],   # head
	"s02": [84],   # eyes
	"s03": [86],   # nose
	"s04": [87]    # facialDetail
}

func get_display_pieces() -> Array:
	var pieces: Array[DisplayPiece] = []

	var base_skin: Color = inst.skinColor
	var variance_col: Color = inst.get_skin_variance_color()
	var variance_indices := inst.skinVariance_indices

	# Core parts — order doesn't matter; we will sort by layer at the end.
	_add_field("backArm", inst.backArm, pieces, base_skin, variance_col, variance_indices)
	_add_field("body", inst.body, pieces, base_skin, variance_col, variance_indices)
	_add_field("legs", inst.legs, pieces, base_skin, variance_col, variance_indices)
	_add_field("head", inst.head, pieces, base_skin, variance_col, variance_indices)

	_add_field("ears", inst.ears, pieces, base_skin, variance_col, variance_indices)
	_add_field("eyes", inst.eyes, pieces, base_skin, variance_col, variance_indices)
	_add_field("nose", inst.nose, pieces, base_skin, variance_col, variance_indices)
	_add_field("mouth", inst.mouth, pieces, base_skin, variance_col, variance_indices)
	_add_field("facialHair", inst.facialHair, pieces, base_skin, variance_col, variance_indices)
	_add_field("facialDetail", inst.facialDetail, pieces, base_skin, variance_col, variance_indices)

	_add_field("hair", inst.hair, pieces, base_skin, variance_col, variance_indices)

	# Other body parts list: currently static, but future-proof for modular.
	for token in inst.otherBodyParts:
		_add_field("otherBodyParts", String(token), pieces, base_skin, variance_col, variance_indices)

	_add_field("frontArm", inst.frontArm, pieces, base_skin, variance_col, variance_indices)

	# Sort final list by layer
	pieces.sort_custom(func(a, b): return int(a.layer) < int(b.layer))
	return pieces

func _add_field(field_name: String, token: String, pieces: Array, base_skin: Color, variance_col: Color, variance_indices: PackedInt32Array) -> void:
	var parsed := ImageId.parse(token)
	match parsed.get("kind", ImageId.Kind.INVALID):
		ImageId.Kind.EMPTY:
			return  # intentionally empty, no warning
		ImageId.Kind.INVALID:
			push_warning("SpeciesDisplayable: invalid id '%s' for %s" % [token, field_name])
			return
		ImageId.Kind.STATIC:
			var image_num: String = parsed["imageNum"]
			var layer: int = int(parsed["layer"])
			var id_str := ImageId.to_static_id(image_num, layer)
			var path := "%s/%s.png" % [SPECIES_IMG_DIR, id_str]
			_append_piece(path, layer, field_name, base_skin, variance_col, variance_indices, pieces)
			return
		ImageId.Kind.MODULAR_FULL:
			var group: String = parsed["groupType"]
			var image_num2: String = parsed["imageNum"]
			var layer2: int = int(parsed["layer"])
			var id_str2 := ImageId.to_modular_id(group, image_num2, layer2)
			var path2 := "%s/%s.png" % [SPECIES_IMG_DIR, id_str2]
			_append_piece(path2, layer2, field_name, base_skin, variance_col, variance_indices, pieces)
			return
		ImageId.Kind.MODULAR_GROUP_ONLY:
			var type_code: String = parsed["groupType"]
			if not LAYERS.has(type_code):
				push_warning("SpeciesDisplayable: unknown modular type '%s' for %s" % [type_code, field_name])
				return
			var img_num: String = String(inst.modular_image_nums.get(type_code, ""))
			if img_num == "":
				img_num = SpeciesLoader.pick_modular_image_num(type_code)
				if img_num == "":
					push_warning("SpeciesDisplayable: cannot pick image number for type '%s'" % type_code)
					return
				inst.modular_image_nums[type_code] = img_num
			for layer3 in LAYERS[type_code]:
				var id3 := ImageId.to_modular_id(type_code, img_num, int(layer3))
				var path3 := "%s/%s.png" % [SPECIES_IMG_DIR, id3]
				_append_piece(path3, int(layer3), field_name, base_skin, variance_col, variance_indices, pieces)
			return

func _append_piece(path: String, layer: int, field_name: String, base_skin: Color, variance_col: Color, variance_indices: PackedInt32Array, pieces: Array) -> void:
	if not ResourceLoader.exists(path):
		push_warning("Missing texture: " + path)
		return
	var tint := _tint_for(field_name, layer, base_skin, variance_col, variance_indices)
	var dp := DisplayPiece.from_path(path, int(layer), tint)
	if dp != null:
		pieces.append(dp)

func _tint_for(field_name: String, layer: int, base_skin: Color, variance_col: Color, variance_indices: PackedInt32Array) -> Color:
	# Hair/eyes are never skin-tinted. Facial hair should follow hair color (same rule).
	if field_name == "hair" or field_name == "eyes" or field_name == "facialHair":
		return Color(1,1,1,1)
	# Variance overrides base on specific layers
	for i in range(variance_indices.size()):
		if int(variance_indices[i]) == int(layer):
			return variance_col
	# Default: tint with base skin color for all other species pieces
	return base_skin


func build_pieces_with_hidden(hidden_layers: PackedInt32Array) -> Array[DisplayPiece]:
	var pieces: Array[DisplayPiece] = build_pieces()
	if hidden_layers.is_empty():
		return pieces
	var hidden := {}
	for h in hidden_layers:
		hidden[int(h)] = true
	var keep = func(p: DisplayPiece) -> bool:
		return not hidden.has(int(p.layer))
	return pieces.filter(keep)
	
func build_pieces() -> Array[DisplayPiece]:
	# Assumes you already have logic that iterates modular groups and base parts.
	# If you already have equivalent code under a different name, either rename it to this
	# or have this call your internal builder.
	var pieces: Array[DisplayPiece] = []
	# TODO: populate 'pieces' using your existing per-layer path construction.
	# This call site exists to keep Character.get_display_pieces() simple.
	return pieces
