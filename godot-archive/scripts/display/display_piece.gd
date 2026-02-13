extends Resource
class_name DisplayPiece

@export var layer: int = 0
@export var texture: Texture2D
@export var modulate: Color = Color(1, 1, 1, 1)
@export var offset: Vector2 = Vector2.ZERO
@export var scale: Vector2 = Vector2.ONE
@export var rotation_degrees: float = 0.0
@export var flip_h: bool = false
@export var flip_v: bool = false
@export var is_offhand_weapon: bool = false  # Special flag for off-hand weapons/shields

static func make(tex: Texture2D, layer_value: int, col: Color) -> DisplayPiece:
	var dp := DisplayPiece.new()
	dp.layer = layer_value
	dp.texture = tex
	dp.modulate = col
	return dp

static func from_path(path: String, layer_value: int, col: Color) -> DisplayPiece:
	var tex := load(path) as Texture2D
	if tex == null:
		return null
	return make(tex, layer_value, col)
