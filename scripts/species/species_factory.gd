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

static func create_instance(species_id: String) -> SpeciesInstance:
	var s: Species = species_loader.get_by_id(species_id)
	if s == null:
		var known := PackedStringArray()
		for k in species_loader.by_id.keys():
			known.append(String(k))
		push_error("SpeciesFactory: unknown species '%s'. Known: [%s]" % [
			species_id, ", ".join(known)
		])
		return null
	var inst := SpeciesInstance.new()
	inst.from_species(s)
	return inst

static func create_displayable(species_id: String) -> SpeciesDisplayable:
	var inst := create_instance(species_id)
	if inst == null:
		return null
	return SpeciesDisplayable.new(inst)

static func _find_species_by_name(name: String) -> Species:
	for candidate in species_loader.all:
		if candidate is Species and (candidate as Species).name == name:
			return candidate
	return null
