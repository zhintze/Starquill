extends Node
class_name SpeciesFactory

# Return a ready-to-render displayable for CharacterDisplay
func create_displayable(species_name: String) -> SpeciesDisplayable:
	var s := species_display_builder.get_species(species_name)
	if s == null:
		push_error("SpeciesFactory: unknown species '%s'" % species_name)
		return null
	var si := species_display_builder.freeze_species_to_instance(s)
	return SpeciesDisplayable.new(si)

# If you only need the frozen data (e.g., save to disk)
func create_instance(species_name: String) -> SpeciesInstance:
	var s := species_display_builder.get_species(species_name)
	if s == null:
		push_error("SpeciesFactory: unknown species '%s'" % species_name)
		return null
	return species_display_builder.freeze_species_to_instance(s)
