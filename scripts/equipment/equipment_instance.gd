# res://scripts/equipment/equipment_instance.gd
extends Resource
class_name EquipmentInstance

@export var item_type: String
@export var item_num: int = 1      # 1..amount
@export var stats: Stats           # your existing Stats resource/class

func get_slot_name() -> String:
	return EquipmentCatalog.slot_for_item_type(item_type)
