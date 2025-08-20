extends Control
class_name CharacterRandomizerControl

@onready var species_option: OptionButton = $MarginContainer/VBoxContainer/HBoxContainer/SpeciesOption
@onready var randomize_btn: Button       = $MarginContainer/VBoxContainer/HBoxContainer/RandomizeButton
@onready var grid: GridContainer         = $MarginContainer/VBoxContainer/DisplayGrid

const NUM_CHARACTERS: int = 8
const CHARACTER_DISPLAY_SCENE := preload("res://scenes/CharacterDisplay.tscn")

func _ready() -> void:
	_populate_species_option()
	if species_option.item_count > 0 and species_option.selected < 0:
		species_option.select(0)  # ensure a valid selection

	species_option.item_selected.connect(_on_species_changed)
	randomize_btn.pressed.connect(_on_randomize_pressed)

	_refresh_all_with_current_species()

func _populate_species_option() -> void:
	species_option.clear()
	var keys: Array[String] = SpeciesAPI.list_species_keys()
	for i in keys.size():
		species_option.add_item(keys[i], i)
	species_option.set_meta("keys", keys)

func _current_species_key() -> String:
	var keys: Array = species_option.get_meta("keys") as Array
	if keys.is_empty():
		return ""
	var idx := species_option.get_selected_id()
	if idx < 0 or idx >= keys.size():
		idx = 0
	return String(keys[idx])

func _on_species_changed(_index: int) -> void:
	_refresh_all_with_current_species()

func _on_randomize_pressed() -> void:
	_refresh_all_with_current_species()

func _refresh_all_with_current_species() -> void:
	var species_key := _current_species_key()
	if species_key == "":
		push_warning("No species available to populate.")
		return

	for child in grid.get_children():
		child.queue_free()

	for _i in NUM_CHARACTERS:
		# 1) Instantiate and add to tree first so @onready vars are valid.
		var display := CHARACTER_DISPLAY_SCENE.instantiate() as CharacterDisplay
		grid.add_child(display)

		# 2) Build data and set it (now safe because display is in the tree).
		var disp := SpeciesFactory.create_displayable(species_key)
		var si   := SpeciesFactory.create_instance(species_key)
		var ch   := CharacterFactory.create_from_species_instance(si)

		# Prefer one canonical setter. Use whichever your CharacterDisplay implements.
		display.set_species_instance(si)
		# or: display.set_target(disp)
		# or: display.set_character(ch)
