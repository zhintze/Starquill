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

# Add stats from an equipment stat_mods dictionary (string keys like "str", "dex", etc.)
# Equipment-specific stats like "armor" and "damage" are stored separately
var armor: int = 0
var damage: int = 0

func add_from_equipment(stat_mods: Dictionary) -> void:
	for key in stat_mods:
		var value: int = int(stat_mods[key])
		match key.to_lower():
			"str":
				values[StatType.Type.STR] += value
			"dex":
				values[StatType.Type.DEX] += value
			"con":
				values[StatType.Type.CON] += value
			"int":
				values[StatType.Type.INT] += value
			"wis":
				values[StatType.Type.WIS] += value
			"cha":
				values[StatType.Type.CHA] += value
			"armor":
				armor += value
			"damage":
				damage += value
	emit_changed()
