extends Node2D

const SPECIES_JSON_PATH := "res://assets/data/species.json"

var _rng := RandomNumberGenerator.new()

func _ready() -> void:
	_rng.randomize()

	# Ensure species are loaded (harmless if already loaded)
	if species_loader.all.is_empty():
		species_loader.load_from_json(SPECIES_JSON_PATH)

	if species_loader.all.is_empty():
		push_error("RandomSpeciesDemo: No species loaded.")
		return

	# Pick a random species and create a frozen instance
	var idx := _rng.randi_range(0, species_loader.all.size() - 1)
	var inst: SpeciesInstance = species_loader.create_random_instance_from_index(idx)
	if inst == null:
		push_error("RandomSpeciesDemo: Failed to create random instance.")
		return

	var displayable := SpeciesDisplayable.new(inst)
	var cd := CharacterDisplay.new()
	add_child(cd)
	cd.position = get_viewport_rect().size / 2.0
	cd.set_target(displayable)

	print("Rendered species:", inst.species_id)
