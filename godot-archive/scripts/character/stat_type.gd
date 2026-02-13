extends Node
class_name StatType

# The enum defines all stat types. Each entry gets an integer value starting at 0.
# This is used as the "ID" for the stat throughout the code.
enum Type { STR, DEX, CON, INT, WIS, CHA }

# This static function lets us convert a stat type ID into a human-readable name.
# We use this for UI, tooltips, etc.
static func type_to_string(stat_type: int) -> String:
	match stat_type:
		Type.STR: return "Strength"
		Type.DEX: return "Dexterity"
		Type.CON: return "Constitution"
		Type.INT: return "Intelligence"
		Type.WIS: return "Wisdom"
		Type.CHA: return "Charisma"
		_: return "Unknown"


static func type_to_short_string(stat_type: int) -> String:
	match stat_type:
		Type.STR: return "STR"
		Type.DEX: return "DEX"
		Type.CON: return "CON"
		Type.INT: return "INT"
		Type.WIS: return "WIS"
		Type.CHA: return "CHA"
		_: return "Unknown"

# Optional: If you ever need to loop through *all* stat types (for menus, etc.),
# this helper returns them as an array of IDs.
static func all_types() -> Array[int]:
	return [Type.STR, Type.DEX, Type.CON, Type.INT, Type.WIS, Type.CHA]
