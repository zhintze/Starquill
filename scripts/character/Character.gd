extends Node
class_name Character

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

func get_displayables() -> Array[Displayable]:
	return []

func get_display_pieces() -> Array[DisplayPiece]:
	return []

func get_current_verbs() -> Array[Verb]:
	return []

func get_all_equipment() -> Array[Equipment]:
	return [head, torso, arms, legs, feet, misc1, misc2, misc3, misc4]
