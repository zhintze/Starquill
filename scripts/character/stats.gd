extends Resource
class_name Stats

var values: Dictionary

func _init() -> void:
	values = {
		StatType.Type.STR: 0,
		StatType.Type.DEX: 0,
		StatType.Type.CON: 0,
		StatType.Type.INT: 0,
		StatType.Type.WIS: 0,
		StatType.Type.CHA: 0
	}

func get_stat(stat_type: int) -> int:
	return values.get(stat_type, 0)

func set_stat(stat_type: int, value: int) -> void:
	values[stat_type] = value
	emit_changed()

func add_stat(stat_type: int, amount: int) -> void:
	values[stat_type] = get_stat(stat_type) + amount
	emit_changed()
