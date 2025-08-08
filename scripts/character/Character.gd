extends Resource
class_name Character

signal model_changed

var id: String
var display_name: String
var skin_color: Color
var stats: Stats
var species: SpeciesInstance

var head: Equipment
var torso: Equipment
var arms: Equipment
var legs: Equipment
var feet: Equipment
var misc1: Equipment
var misc2: Equipment
var misc3: Equipment
var misc4: Equipment

var base_verbs: Array[Verb]

func _init() -> void:
	base_verbs = []
	stats = Stats.new()
	stats.changed.connect(_on_stats_changed)

# Forward stats changes
func _on_stats_changed() -> void:
	emit_signal("model_changed")

# Species set (optional: forward species.stats if present)
func set_species(s: SpeciesInstance) -> void:
	if species and species.stats:
		if species.stats.changed.is_connected(_on_stats_changed):
			species.stats.changed.disconnect(_on_stats_changed)
	species = s
	if species and species.stats:
		species.stats.changed.connect(_on_stats_changed)
	emit_signal("model_changed")


enum EquipSlot { HEAD, TORSO, ARMS, LEGS, FEET, MISC1, MISC2, MISC3, MISC4 }

func set_equipment(slot: int, e: Equipment) -> void:
	match slot:
		EquipSlot.HEAD:  head = e
		EquipSlot.TORSO: torso = e
		EquipSlot.ARMS:  arms = e
		EquipSlot.LEGS:  legs = e
		EquipSlot.FEET:  feet = e
		EquipSlot.MISC1: misc1 = e
		EquipSlot.MISC2: misc2 = e
		EquipSlot.MISC3: misc3 = e
		EquipSlot.MISC4: misc4 = e
		_: return
	emit_signal("model_changed")

func get_equipment(slot: int) -> Equipment:
	match slot:
		EquipSlot.HEAD:  return head
		EquipSlot.TORSO: return torso
		EquipSlot.ARMS:  return arms
		EquipSlot.LEGS:  return legs
		EquipSlot.FEET:  return feet
		EquipSlot.MISC1: return misc1
		EquipSlot.MISC2: return misc2
		EquipSlot.MISC3: return misc3
		EquipSlot.MISC4: return misc4
		_: return null
		
#Dangerous placeholder - needs to put equipment into party inventory
func unequip(slot: int) -> void:
	set_equipment(slot, null)

func get_all_equipment() -> Array[Equipment]:
	var all := [head, torso, arms, legs, feet, misc1, misc2, misc3, misc4]
	return all.filter(func(e): return e != null)

# Stub — return pieces built from species + equipment
func get_display_pieces() -> Array[DisplayPiece]:
	return []
