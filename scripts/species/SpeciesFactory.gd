extends Node
class_name SpeciesFactory

# Map part-id to texture path
func _part_path(id: String) -> String:
	# Adjust to your folder layout & prefixes
	# e.g., "0001-038" -> "res://art/parts/0001/0001-038.png"
	if id == "" or id == null:
		return ""
	# simple default: flat folder with exact filenames
	return "res://art/parts/%s.png" % id

func create_instance(species_name: String) -> SpeciesInstance:
	if not SpeciesLoader.by_name.has(species_name):
		push_error("SpeciesFactory: unknown species '%s'" % species_name)
		return null
	var inst := SpeciesInstance.new()
	inst.from_species(SpeciesLoader.by_name[species_name])
	return inst

func create_displayable(species_name: String):
	var inst := create_instance(species_name)
	if inst == null:
		return null
	return SpeciesDisplayable.new(inst, func(id): return _part_path(id))
