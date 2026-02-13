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
@export var skin_color: PackedStringArray = PackedStringArray()        # ["human"] or ["#hex", ...]
@export var skinVariance_sets: Array[Dictionary] = []                  # multiple sets of indices + hex colors for variance

# New: category color defaults/keywords (mirror skin_color behavior)
@export var hair_color: PackedStringArray = PackedStringArray()           # e.g. ["default"] or ["dark"] or ["#6a6a6a", "#796a5c"]
@export var eyes_color: PackedStringArray = PackedStringArray()           # e.g. ["default"] or ["blue"]
@export var facialDetail_color: PackedStringArray = PackedStringArray()   # e.g. ["default"] or ["freckles"]


# Global scale applied to CharacterDisplay root
@export var x_scale: float = 1.0
@export var y_scale: float = 1.0
