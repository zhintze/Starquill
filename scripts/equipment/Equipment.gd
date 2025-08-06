extends Resource
class_name Equipment

var id: int
var item_type: String
var item_num: int
var layer_codes: Array[int]
var hidden_layers: Array[int]

func get_display_pieces() -> Array[DisplayPiece]:
    return []

func validate() -> bool:
    return true