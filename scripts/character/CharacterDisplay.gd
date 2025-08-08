extends Node2D
class_name CharacterDisplay

var character: Character
var _sprites: Array[Sprite2D] = []

func set_character(c: Character) -> void:
	if character and character.model_changed.is_connected(_on_model_changed):
		character.model_changed.disconnect(_on_model_changed)
	character = c
	if character and not character.model_changed.is_connected(_on_model_changed):
		character.model_changed.connect(_on_model_changed)
	_rebuild()

func _on_model_changed() -> void:
	_rebuild()

func _rebuild() -> void:
	for s in _sprites:
		s.queue_free()
	_sprites.clear()

	if character == null:
		return

	var pieces: Array[DisplayPiece] = character.get_display_pieces()
	pieces.sort_custom(Callable(self, "_cmp_display_pieces"))

	for p in pieces:
		var spr := Sprite2D.new()
		spr.texture = _load_texture_for(p.image_name)
		spr.modulate = p.color
		spr.z_index = p.z_index
		add_child(spr)
		_sprites.append(spr)

func _cmp_display_pieces(a: DisplayPiece, b: DisplayPiece) -> bool:
	if a.layer == b.layer:
		return a.z_index < b.z_index
	return a.layer < b.layer

func _load_texture_for(name: String) -> Texture2D:
	return load("res://art/%s.png" % name) as Texture2D
