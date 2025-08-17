# res://scenes/random_species_demo.gd
extends Node2D

const SPECIES_JSON_PATH := "res://assets/data/species.json"
const PARTS_FILE_TEMPLATE := "res://art/parts/{dir}/{id}.png"

var _rng := RandomNumberGenerator.new()

func _ready() -> void:
	_rng.randomize()

	# Ensure species are loaded (harmless if already loaded)
	if species_loader.all.is_empty():
		species_loader.load_from_json(SPECIES_JSON_PATH)

	if species_loader.all.is_empty():
		push_error("RandomSpeciesDemo: No species loaded.")
		return

	# Pick a random species and create an instance
	var idx := _rng.randi_range(0, species_loader.all.size() - 1)
	var inst := species_loader.create_random_instance_from_index(idx)
	if inst == null:
		push_error("RandomSpeciesDemo: Failed to create random instance.")
		return

	# Build a displayable wrapper using a simple id->path resolver
	var displayable := _make_species_displayable(inst)
	if displayable == null:
		print("Random species created:", inst.name, "(no displayable; check classes)")
		return

	# Render it
	if not ClassDB.class_exists("CharacterDisplay"):
		push_warning("CharacterDisplay class not found (missing class_name?).")
		print("Random species:", inst.name)
		return

	var cd = ClassDB.instantiate("CharacterDisplay")
	add_child(cd)
	cd.position = get_viewport_rect().size / 2.0
	cd.set_target(displayable)

func _make_species_displayable(inst) -> Resource:  # <-- typed return
	if not ClassDB.class_exists("SpeciesDisplayable"):
		return null

	# Simple file-per-sprite resolver. Swap later for an atlas resolver.
	var path_fn := func(id: String) -> String:
		if id == "" or id == null:
			return ""
		var dir: String = id.substr(0, 4)
		return "res://art/parts/{dir}/{id}.png".replace("{dir}", dir).replace("{id}", id)

	# Prefer constructor if your class uses class_name SpeciesDisplayable
	# and defines _init(inst, path_fn)
	if ClassDB.class_exists("SpeciesDisplayable"):
		# Best case: constructor with args
		# return SpeciesDisplayable.new(inst, path_fn)  # <-- use this if your class supports it

		# Universal fallback: instantiate, then set fields / call _init
		var disp := ClassDB.instantiate("SpeciesDisplayable") as Resource
		if disp == null:
			return null

		# If your script defines _init(inst, path_fn), call it:
		if disp.has_method("_init"):
			disp.call("_init", inst, path_fn)
		else:
			# Otherwise set fields directly (they must exist in the script)
			if disp.has_method("set"):
				disp.set("inst", inst)
			disp.set("path_fn", path_fn)
		return disp

	return null
