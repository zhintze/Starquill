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

var isEyesStatic := false
var isNoseStatic := false
var isMouthStatic := false
var isEarsStatic := false
var isFacialHairStatic := false
var isFacialDetailStatic := false

var hairTypes: Array[String]
var skinColorList: SkinColorList
var skinVarianceMap := {}

var itemRestrictions: Array[String]