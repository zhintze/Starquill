extends Control
class_name CharacterDisplay

@export var DEBUG_SHOW_COUNTS: bool = false

@onready var bg_panel: Panel = get_node_or_null(^"Panel")
@onready var layer_root: Control = _ensure_layer_root()

var _character: Character
var _piece_nodes: Array[TextureRect] = []
var _content_size: Vector2 = Vector2.ZERO   # unscaled assembled bounds

func _ready() -> void:
	# Root is pickable. Children ignore input so nothing steals events.
	mouse_filter = Control.MOUSE_FILTER_STOP
	if bg_panel:
		bg_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	resized.connect(_on_resized)
	_redraw()

# ---------- Public API ----------
func set_character(c: Character) -> void:
	if _character and _character.model_changed.is_connected(_on_model_changed):
		_character.model_changed.disconnect(_on_model_changed)
	_character = c
	if _character and not _character.model_changed.is_connected(_on_model_changed):
		_character.model_changed.connect(_on_model_changed)
	_redraw()

# ---------- Signals ----------
func _on_model_changed() -> void:
	_redraw()

func _on_resized() -> void:
	_apply_bottom_center_anchor()

# ---------- Build / Layout ----------
func _ensure_layer_root() -> Control:
	var n: Control = get_node_or_null(^"LayerRoot") as Control
	if n != null:
		return n
	n = Control.new()
	n.name = "LayerRoot"
	n.anchor_left = 0.0
	n.anchor_top = 0.0
	n.anchor_right = 0.0
	n.anchor_bottom = 0.0
	n.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(n)
	return n

func _redraw() -> void:
	if _character == null or _character.species == null:
		_hide_all_pieces()
		_update_global_scale()
		_apply_bottom_center_anchor()
		return

	# Ask the Character for the fully merged, sorted list (species + equipment)
	var pieces: Array[DisplayPiece] = _character.get_display_pieces()

	if DEBUG_SHOW_COUNTS:
		var null_tex := 0
		for p in pieces:
			if p.texture == null:
				null_tex += 1
		print("[CharacterDisplay] pieces=", pieces.size(), " null_textures=", null_tex)

	_ensure_piece_nodes(pieces.size())

	_content_size = Vector2.ZERO
	for i in range(pieces.size()):
		var p: DisplayPiece = pieces[i]
		var tr: TextureRect = _piece_nodes[i]

		if p.texture == null:
			tr.visible = false
			continue

		tr.visible = true
		tr.texture = p.texture
		tr.modulate = p.modulate
		tr.position = p.offset
		tr.scale = p.scale
		tr.flip_h = p.flip_h
		tr.flip_v = p.flip_v

		tr.z_as_relative = true
		tr.z_index = 0
		layer_root.move_child(tr, i)

		var ts: Vector2 = p.texture.get_size()
		_content_size.x = max(_content_size.x, tr.position.x + ts.x * tr.scale.x)
		_content_size.y = max(_content_size.y, tr.position.y + ts.y * tr.scale.y)

	for i in range(pieces.size(), _piece_nodes.size()):
		_piece_nodes[i].visible = false

	_update_global_scale()
	call_deferred("_apply_bottom_center_anchor")

func _ensure_piece_nodes(count: int) -> void:
	while _piece_nodes.size() < count:
		var tr: TextureRect = TextureRect.new()
		tr.name = "piece_%d" % _piece_nodes.size()
		tr.stretch_mode = TextureRect.STRETCH_KEEP
		tr.expand_mode = TextureRect.EXPAND_KEEP_SIZE
		tr.mouse_filter = Control.MOUSE_FILTER_IGNORE
		tr.position = Vector2.ZERO
		layer_root.add_child(tr)
		_piece_nodes.append(tr)

func _hide_all_pieces() -> void:
	for n in _piece_nodes:
		n.visible = false

# ---------- Scaling & Anchoring ----------
func _update_global_scale() -> void:
	var scale_species: Vector2 = Vector2.ONE
	var scale_inst: Vector2 = Vector2.ONE
	if _character != null and _character.species != null:
		var inst: SpeciesInstance = _character.species
		if inst.base != null:
			scale_species = Vector2(inst.base.x_scale, inst.base.y_scale)
		scale_inst = Vector2(inst.x_scale, inst.y_scale)
	layer_root.scale = Vector2(scale_species.x * scale_inst.x, scale_species.y * scale_inst.y)

func _apply_bottom_center_anchor() -> void:
	# Feet pivot in local (unscaled) units
	layer_root.pivot_offset = Vector2(_content_size.x * 0.5, _content_size.y)

	# Place feet at the panel's bottom-center (fallback to control)
	if bg_panel:
		var r := Rect2(bg_panel.position, bg_panel.size)
		layer_root.position = Vector2(r.position.x + r.size.x * 0.5, r.position.y + r.size.y)
	else:
		layer_root.position = Vector2(size.x * 0.5, size.y)

# ---------- Hit test ----------
# Make the whole tile clickable so layout changes never kill click area.
func _has_point(point: Vector2) -> bool:
	return Rect2(Vector2.ZERO, size).has_point(point)
