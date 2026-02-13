# equipment_def.gd
extends Resource
class_name Equipment

enum Slot { HEAD, TORSO, ARMS, LEGS, FEET, MISC1, MISC2, MISC3, MISC4 }

@export var id: String
@export var name: String
@export var slot: Slot

# Image composition rules:
# - key: string “part code” for asset families (e.g., "helmet", "cloak", "gloves")
# - value: Array[int] layer numbers this item paints on (same approach as species modular map)
@export var layer_map: Dictionary

# Asset file pattern per part (if modular), e.g. "res://assets/images/equipment/helmet/helmet-%03d.png"
@export var path_pattern_by_part: Dictionary

# Optional
@export var tints_by_layer: Dictionary            # layer(int) -> Color/hex String
@export var hide_species_layers: PackedInt32Array # which species layers to suppress
@export var modular_max_by_part: Dictionary       # part -> int (count)
@export var stats_delta: Dictionary               # if equipment modifies stats
@export var tags: PackedStringArray
@export var restricted_species: PackedStringArray # disallow by species id
@export var restricted_item_types: PackedStringArray # e.g. "no_headgear"
