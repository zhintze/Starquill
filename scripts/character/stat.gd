extends Resource
class_name Stat

# Which stat is this? (e.g., StatType.Type.STR)
var type: int

# Base value before buffs/debuffs.
var base_value: int = 0

# Optional runtime modifiers (flat). Currently not used
var flat_modifiers: Array[int] = []

# --- Convenience: names derived from type for UI ---
func name_short() -> String:
	return StatType.type_to_short_string(type)

func name_full() -> String:
	return StatType.type_to_string(type)

# --- Value access ---
func get_value() -> int:
	var total := base_value
	for m in flat_modifiers:
		total += m
	return total

func set_base_value(v: int) -> void:
	base_value = v

# --- Modifiers ---
func add_modifier(amount: int) -> void:
	flat_modifiers.append(amount)

func remove_modifier(amount: int) -> void:
	var idx := flat_modifiers.find(amount)
	if idx != -1:
		flat_modifiers.remove_at(idx)

func clear_modifiers() -> void:
	flat_modifiers.clear()

# --- Constructors / helpers ---
static func from_type(stat_type: int, base: int = 0) -> Stat:
	var s := Stat.new()
	s.type = stat_type
	s.base_value = base
	return s
