# res://scripts/demo/random_species_demo.gd
extends Node2D

const SPECIES_JSON_PATH := "res://assets/data/species.json"
const CHARACTER_DISPLAY_SCENE := preload("res://scenes/CharacterDisplay.tscn")

var _rng := RandomNumberGenerator.new()

func _ready() -> void:
	_rng.randomize()

	# Ensure species are loaded (defensive)
	if StarquillData.get_species_count() == 0:
		if FileAccess.file_exists(SPECIES_JSON_PATH):
			StarquillData.load_species_from_json(SPECIES_JSON_PATH)
			print("After JSON load: count=", StarquillData.get_species_count(), " ids=", StarquillData.get_species_ids())
		else:
			print("JSON missing:", SPECIES_JSON_PATH, " → species will be loaded by ConfigManager")

	if StarquillData.get_species_count() == 0:
		push_error("RandomSpeciesDemo: No species loaded; nothing to render.")
		return

	# Prefer a known id; fall back to random so we always render something
	var target_id := "human"
	var inst: SpeciesInstance = null
	var s := StarquillData.get_species_by_id(target_id)
	if s == null:
		push_warning("Target id '%s' not found. Falling back to random." % target_id)
		inst = StarquillData.create_random_species_instance()
	else:
		inst = StarquillData.create_species_instance(target_id)

	if inst == null:
		push_error("RandomSpeciesDemo: Failed to create instance.")
		return

	# Build a Character from the instance (new constructor path)
	#var ch: Character = CharacterFactory.create_from_species_instance(inst)

	# Instantiate the scene (NOT .new()) so child nodes exist
	#var cd := CHARACTER_DISPLAY_SCENE.instantiate() as CharacterDisplay
	#add_child(cd)
	# Place it roughly center; adjust as needed
	#cd.position = get_viewport_rect().size / 2.0
	# Bind via the new API
	#cd.set_character(ch)
	#print("Rendered species:", inst.species_id)
