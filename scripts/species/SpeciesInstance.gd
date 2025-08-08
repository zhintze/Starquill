extends Resource
class_name SpeciesInstance

var base: Species

var type: String
var stats: Stats
var scaleX: float
var scaleY: float

var backArm: String
var legs: String
var body: String
var head: String
var frontArm: String
var eyes: String
var nose: String
var mouth: String
var ears: String
var facialHair: String
var facialDetail: String

var isEyesStatic: bool
var isNoseStatic: bool
var isMouthStatic: bool
var isEarsStatic: bool
var isFacialHairStatic: bool
var isFacialDetailStatic: bool

var hairTypes: Array[String]
var skinColor: Color                     # <<— chosen color
var skinVarianceMap: Dictionary
var itemRestrictions: Array[String]

func _init() -> void:
	stats = Stats.new()
	scaleX = 1.0
	scaleY = 1.0
	isEyesStatic = false
	isNoseStatic = false
	isMouthStatic = false
	isEarsStatic = false
	isFacialHairStatic = false
	isFacialDetailStatic = false
	hairTypes = []
	skinColor = Color(1, 1, 1, 1)
	skinVarianceMap = {}
	itemRestrictions = []

# --- Helpers regarding skin color list ---
func set_skin_color(c: Color) -> bool:
	if base and base.skinColorList and base.skinColorList.has_color(c):
		skinColor = c
		return true
	return false

func pick_random_skin_color() -> void:
	if base and base.skinColorList:
		skinColor = base.skinColorList.random_color()

# Displayable stubs
func get_display_pieces() -> Array[DisplayPiece]:
	return []

func get_hidden_layers() -> Array[int]:
	return []
