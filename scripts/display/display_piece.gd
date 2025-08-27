extends Resource
class_name DisplayPiece

@export var layer: int = 0
@export var texture: Texture2D
@export var modulate: Color = Color(1, 1, 1, 1)
@export var offset: Vector2 = Vector2.ZERO
@export var scale: Vector2 = Vector2.ONE
@export var flip_h: bool = false
@export var flip_v: bool = false

static func make(tex: Texture2D, layer: int, col: Color) -> DisplayPiece:
	var dp := DisplayPiece.new()
	dp.layer = layer
	dp.texture = tex
	dp.modulate = col
	return dp

static func from_path(path: String, layer: int, col: Color) -> DisplayPiece:
	var tex := load(path) as Texture2D
	if tex == null:
		return null
	return make(tex, layer, col)
