extends Node
class_name SpeciesFactory

static func list_species_keys() -> Array[String]:
	var out: Array[String] = []
	# species_loader is an Autoload with .all: Array[Species]
	for s in species_loader.all:
		if s is Species:
			out.append((s as Species).name)
	out.sort()
	return out

static func create_instance(species_name: String) -> SpeciesInstance:
	var sp: Species = _find_species_by_name(species_name)
	if sp == null:
		push_error("SpeciesFactory: unknown species '%s'" % species_name)
		return null
	return SpeciesDisplayBuilder.freeze_species_to_instance(sp) as SpeciesInstance

static func create_displayable(species_name: String) -> SpeciesDisplayable:
	var si: SpeciesInstance = create_instance(species_name)
	return null if si == null else SpeciesDisplayable.new(si)

static func _find_species_by_name(name: String) -> Species:
	for candidate in species_loader.all:
		if candidate is Species and (candidate as Species).name == name:
			return candidate
	return null
