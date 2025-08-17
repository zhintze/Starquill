extends Resource
class_name SpeciesDisplayable

var inst   # SpeciesInstance
var path_fn: Callable            # (String id) -> String
var hidden_layers: Array[int] = []

func _init(_inst, _path_fn: Callable) -> void:
	inst = _inst
	path_fn = _path_fn

func get_hidden_layers() -> Array:
	return hidden_layers

# Map fixed parts to layers; tweak as needed
const LAYERS := {
	"legs": 10,
	"body": 20,
	"backArm": 25,
	"head": 30,
	"ears": 35,
	"eyes": 40,
	"nose": 45,
	"mouth": 50,
	"facialDetail": 55,
	"facialHair": 60,
	"frontArm": 65,
	"hair": 70
}

func _part_to_piece(id: String, layer: int, tint: Color) -> DisplayPiece:
	if id == "" or path_fn.is_null():
		return null
	var path: String = path_fn.call(id)
	if path == "":
		return null
	return DisplayPiece.from_path(path, layer, tint)

func _tint_from_palette() -> Color:
	# If you stored a SkinColorList on the Species (e.g., property "skinColorList"), try to use it.
	var scl = null
	if inst != null and inst.base != null and inst.base.has_method("get"):
		for p in inst.base.get_property_list():
			if typeof(p) == TYPE_DICTIONARY and p.has("name") and p["name"] == "skinColorList":
				scl = inst.base.get("skinColorList")
				break
	if scl != null and scl is SkinColorList and not scl.colors.is_empty():
		return scl.random_color()
	# fallback
	return Color.WHITE

func get_display_pieces() -> Array:
	var pieces: Array = []
	var tint := _tint_from_palette()

	# Fixed parts
	var parts := {
		"legs": inst.legs,
		"body": inst.body,
		"backArm": inst.backArm,
		"head": inst.head,
		"ears": inst.ears,
		"eyes": inst.eyes,
		"nose": inst.nose,
		"mouth": inst.mouth,
		"facialDetail": inst.facialDetail,
		"facialHair": inst.facialHair,
		"frontArm": inst.frontArm
	}

	for key in parts.keys():
		var pid: String = parts[key]
		var layer: int = LAYERS.get(key, 0)
		var dp := _part_to_piece(pid, layer, tint)
		if dp != null:
			pieces.append(dp)

	# Optional hair (randomized string on inst.hair)
	if typeof(inst.hair) == TYPE_STRING and inst.hair != "":
		var dp_h := _part_to_piece(inst.hair, LAYERS.get("hair", 70), tint)
		if dp_h != null:
			pieces.append(dp_h)

	# Other body parts array
	if typeof(inst.otherBodyParts) == TYPE_ARRAY and not inst.otherBodyParts.is_empty():
		var base_layer := 80
		for i in inst.otherBodyParts.size():
			var obp_id: String = inst.otherBodyParts[i]
			var dp_obp := _part_to_piece(obp_id, base_layer + i, tint)
			if dp_obp != null:
				pieces.append(dp_obp)

	# Scale/offset can be set per piece here if you prefer to bake x/y scale
	return pieces
