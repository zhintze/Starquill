extends Resource
class_name Species

@export var name: String = ""

# Species parts: static "####-###" or modular code like "f01"; `hair` can be a pool.
@export var backArm: String = ""
@export var body: String = ""
@export var ears: String = ""
@export var eyes: String = ""
@export var facialDetail: String = ""
@export var facialHair: String = ""
@export var frontArm: String = ""
@export var hair: Variant = null          # String OR Array[String]
@export var head: String = ""
@export var legs: String = ""
@export var mouth: String = ""
@export var nose: String = ""

@export var otherBodyParts: PackedStringArray = PackedStringArray()
@export var itemRestrictions: PackedStringArray = PackedStringArray()

# Skin coloring
@export var skin_color: PackedStringArray = PackedStringArray()          # ["human"] or ["#hex", ...]
@export var skinVariance_hex: PackedStringArray = PackedStringArray()    # overrides palette if non-empty
@export var skinVariance_indices: PackedInt32Array = PackedInt32Array()  # layers to tint with variance

# Global scale applied to CharacterDisplay root
@export var x_scale: float = 1.0
@export var y_scale: float = 1.0
