extends Node2D
class_name CharacterDisplay

var target                        # any object with get_display_pieces() and get_hidden_layers()
var _sprites: Array[Sprite2D] = []

func set_target(displayable) -> void:
	target = displayable
	_refresh()

func _clear_children():
	for s in _sprites:
		if is_instance_valid(s):
			s.queue_free()
	_sprites.clear()

func _refresh():
	_clear_children()
	if target == null or not target.has_method("get_display_pieces"):
		return
	var pieces: Array = target.get_display_pieces()
	var hidden: Array = []
	if target.has_method("get_hidden_layers"):
		hidden = target.get_hidden_layers()

	pieces.sort_custom(func(a, b):
		return (a.layer < b.layer)
	)

	for dp in pieces:
		if dp == null or dp is not DisplayPiece:
			continue
		if hidden.has(dp.layer):
			continue
		var spr := Sprite2D.new()
		spr.texture = dp.texture
		spr.modulate = dp.modulate
		spr.flip_h = dp.flip_h
		spr.flip_v = dp.flip_v
		spr.position = dp.offset
		spr.scale = dp.scale
		add_child(spr)
		_sprites.append(spr)

func refresh():
	_refresh()
