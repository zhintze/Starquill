extends Resource
class_name ItemData

@export_group("Identity")
@export var id: StringName = &""
@export var display_name: String = ""
@export var description: String = ""
@export var icon: Texture2D = null

@export_group("Stacking")
@export var max_stack: int = 1
@export var is_stackable: bool = false

@export_group("Type")
@export var item_type: StringName = &"misc"  # misc, consumable, key_item, material

@export_group("Value")
@export var sell_value: int = 0
@export var buy_value: int = 0

@export_group("Usage")
@export var is_usable: bool = false
@export var use_in_combat: bool = false
@export var use_in_field: bool = false
@export var effect_script: Script = null  # ItemEffect script

@export_group("Stats (for consumables)")
@export var stat_effects: Dictionary = {}  # StatType -> int

func can_use_in_context(context: StringName) -> bool:
	match context:
		&"combat":
			return use_in_combat
		&"field":
			return use_in_field
		_:
			return false
