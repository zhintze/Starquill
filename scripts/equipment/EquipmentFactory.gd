extends Resource
class_name EquipmentFactory

func create_equipment(data: Dictionary) -> Equipment:
	return Equipment.new()

func create_random_equipment(type: String) -> Equipment:
	return Equipment.new()
