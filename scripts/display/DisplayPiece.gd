extends Resource
class_name DisplayPiece

@export var layer: int = 0
@export var texture: Texture2D
@export var modulate: Color = Color(1, 1, 1, 1)
@export var offset: Vector2 = Vector2.ZERO
@export var scale: Vector2 = Vector2.ONE
@export var flip_h: bool = false
@export var flip_v: bool = false

static func from_path(path: String, layer: int, modulate: Color = Color.WHITE, offset := Vector2.ZERO, scale := Vector2.ONE, flip_h := false, flip_v := false) -> DisplayPiece:
	var dp := DisplayPiece.new()
	dp.layer = layer
	dp.texture = load(path) as Texture2D
	dp.modulate = modulate
	dp.offset = offset
	dp.scale = scale
	dp.flip_h = flip_h
	dp.flip_v = flip_v
	return dp
