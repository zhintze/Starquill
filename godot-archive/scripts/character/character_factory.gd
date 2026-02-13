extends Node
class_name CharacterFactory

static func create_from_species_instance(si: SpeciesInstance) -> Character:
	var ch := Character.new()
	ch.species = si
	ch.id = _make_id(si)
	ch.display_name = _make_display_name(si)
	# TODO: when EquipmentFactory exists, equip here:
	# EquipmentFactory.equip_random(ch)
	return ch

static func create_random_for_species_key(species_key: String) -> Character:
	var si := SpeciesFactory.create_instance(species_key)
	if si == null:
		push_error("CharacterFactory: could not create SpeciesInstance for '%s'" % species_key)
		return Character.new()
	return create_from_species_instance(si)

static func _make_id(si: SpeciesInstance) -> String:
	var base := ""
	if si and "species_id" in si and String(si.species_id) != "":
		base = String(si.species_id)
	elif si and "base" in si and si.base and "name" in si.base:
		base = String(si.base.name)
	else:
		base = "unknown"
	return "%s_%d" % [base, Time.get_ticks_usec()]

static func _make_display_name(si: SpeciesInstance) -> String:
	if si and "species_id" in si and String(si.species_id) != "":
		return String(si.species_id)
	if si and "base" in si and si.base and "name" in si.base:
		return String(si.base.name)
	return "Unnamed"
