extends Resource
class_name Species

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
var skinColorList: SkinColorList        # <<— Resource
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
	skinColorList = SkinColorList.new()
	skinVarianceMap = {}
	itemRestrictions = []

func get_display_pieces() -> Array[DisplayPiece]:
	return []

func get_hidden_layers() -> Array[int]:
	return []

func make_instance() -> SpeciesInstance:
	var inst := SpeciesInstance.new()
	inst.base = self
	inst.stats = Stats.new()
	inst.scaleX = scaleX
	inst.scaleY = scaleY
	inst.backArm = backArm
	inst.legs = legs
	inst.body = body
	inst.head = head
	inst.frontArm = frontArm
	inst.eyes = eyes
	inst.nose = nose
	inst.mouth = mouth
	inst.ears = ears
	inst.facialHair = facialHair
	inst.facialDetail = facialDetail
	inst.isEyesStatic = isEyesStatic
	inst.isNoseStatic = isNoseStatic
	inst.isMouthStatic = isMouthStatic
	inst.isEarsStatic = isEarsStatic
	inst.isFacialHairStatic = isFacialHairStatic
	inst.isFacialDetailStatic = isFacialDetailStatic
	inst.hairTypes = hairTypes.duplicate()
	inst.skinVarianceMap = skinVarianceMap.duplicate()
	inst.itemRestrictions = itemRestrictions.duplicate()
	# Pick a valid default skin color if list has entries
	if skinColorList and not skinColorList.colors.is_empty():
		inst.skinColor = skinColorList.colors[0]
	else:
		inst.skinColor = Color(1, 1, 1, 1)
	return inst
