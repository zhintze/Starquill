extends Node
class_name SpeciesFactory

static func list_species_keys() -> Array[String]:
	var out: Array[String] = []
	# StarquillData is an Autoload with unified data registry
	for s in StarquillData.get_all_species():
		if s is Species:
			out.append((s as Species).name)
	out.sort()
	return out

static func create_instance(species_id: String) -> SpeciesInstance:
	return StarquillData.create_species_instance(species_id)

static func create_displayable(species_id: String) -> SpeciesDisplayable:
	var inst := create_instance(species_id)
	if inst == null:
		return null
	return SpeciesDisplayable.new(inst)

static func _find_species_by_name(name: String) -> Species:
	return StarquillData.get_species_by_id(name)
