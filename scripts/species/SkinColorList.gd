extends Resource
class_name SkinColorList

@export var id: String = ""
@export var colors: Array[Color] = []

func has_color(c: Color) -> bool:
	for col in colors:
		if col == c:
			return true
	return false

func random_color() -> Color:
	if colors.is_empty():
		return Color(1, 1, 1, 1)
	return colors[randi() % colors.size()]
