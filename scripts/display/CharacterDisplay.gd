extends Control
class_name CharacterDisplay

@export var name_label_path: NodePath = ^"NameLabel"

@onready var name_label: Label = get_node_or_null(name_label_path)
@onready var layer_root: Control = _ensure_layer_root()

var _character: Character
var _piece_nodes: Array[TextureRect] = []
var _last_piece_count: int = 0

func set_character(c: Character) -> void:
	if _character and _character.model_changed.is_connected(_on_model_changed):
		_character.model_changed.disconnect(_on_model_changed)
	_character = c
	if _character:
		_character.model_changed.connect(_on_model_changed)
	_redraw()

func _on_model_changed() -> void:
	_redraw()

func _ready() -> void:
	if name_label:
		name_label.text = ""
	_redraw()

func _redraw() -> void:
	if _character == null or _character.species == null:
		_hide_all_pieces()
		if name_label: name_label.text = ""
		return

	# Label: show something human-friendly
	if name_label:
		name_label.text = _character.display_name if _character.display_name != "" \
						  else String(_character.species.species_id)

	# Build display pieces from the species instance
	var disp := SpeciesDisplayable.new(_character.species)
	var pieces: Array = disp.get_display_pieces()  # Array[DisplayPiece]

	_apply_pieces(pieces)

# ---------- internal ----------

func _ensure_layer_root() -> Control:
	var n := get_node_or_null("LayerRoot") as Control
	if n: return n
	n = Control.new()
	n.name = "LayerRoot"
	n.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(n)
	_fill_parent(n)  # <— instead of set_anchors_and_margins_preset
	return n

func _apply_pieces(pieces: Array) -> void:
	# ensure pool size
	while _piece_nodes.size() < pieces.size():
		var tr := TextureRect.new()
		tr.name = "piece_%d" % _piece_nodes.size()
		tr.stretch_mode = TextureRect.STRETCH_SCALE
		tr.mouse_filter = Control.MOUSE_FILTER_IGNORE
		layer_root.add_child(tr)
		_fill_parent(tr)
		_piece_nodes.append(tr)

	# apply in order; sibling index controls stacking (later index draws on top)
	for i in pieces.size():
		var p := pieces[i] as DisplayPiece
		var tr := _piece_nodes[i]
		tr.visible = p.texture != null
		if not tr.visible:
			continue
		tr.texture = p.texture
		tr.modulate = p.modulate
		tr.z_as_relative = true
		tr.z_index = 0
		layer_root.move_child(tr, i)

	# hide the rest
	for i in range(pieces.size(), _piece_nodes.size()):
		_piece_nodes[i].visible = false

func _hide_all_pieces() -> void:
	for n in _piece_nodes:
		n.visible = false

# ---------- DisplayPiece helpers ----------
# Adjust these if your DisplayPiece has different shape/names.

func _dp_tex(p) -> Texture2D:
	# Expected: p.tex : Texture2D  OR  p.texture : Texture2D  OR p.path : String
	if p is Dictionary:
		if p.has("tex"): return p["tex"]
		if p.has("texture"): return p["texture"]
		if p.has("path"): return load(String(p["path"])) as Texture2D
		return null
	# Resource/object: try common fields
	if "tex" in p: return p.tex   # if implemented
	if "texture" in p: return p.texture
	if "path" in p: return load(String(p.path)) as Texture2D
	return null

func _dp_layer(p) -> int:
	if p is Dictionary:
		return int(p.get("layer", p.get("z", 0)))
	if "layer" in p: return int(p.layer)
	if "z" in p: return int(p.z)
	return 0

func _dp_color(p) -> Color:
	if p is Dictionary:
		var v = p.get("col", p.get("color", p.get("modulate", Color(1,1,1,1))))
		return (v as Color)
	if "col" in p: return p.col
	if "color" in p: return p.color
	if "modulate" in p: return p.modulate
	return Color(1,1,1,1)
	
# Add this helper anywhere in CharacterDisplay.gd
func _fill_parent(ctrl: Control) -> void:
	ctrl.anchor_left = 0.0
	ctrl.anchor_top = 0.0
	ctrl.anchor_right = 1.0
	ctrl.anchor_bottom = 1.0
	ctrl.offset_left = 0.0
	ctrl.offset_top = 0.0
	ctrl.offset_right = 0.0
	ctrl.offset_bottom = 0.0
