extends Node
class_name SpeciesAPI

static func list_species_keys() -> Array[String]:
	return species_display_builder.list_species_names()

static func create_random_instance(species_key: String) -> SpeciesInstance:
	return SpeciesFactory.create_instance(species_key)
