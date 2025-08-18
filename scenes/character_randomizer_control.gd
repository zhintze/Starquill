extends Control
class_name CharacterRandomizerControl

@onready var species_option: OptionButton = $MarginContainer/VBoxContainer/HBoxContainer/SpeciesOption
@onready var randomize_btn: Button       = $MarginContainer/VBoxContainer/HBoxContainer/RandomizeButton
@onready var grid: GridContainer         = $MarginContainer/VBoxContainer/DisplayGrid

const NUM_CHARACTERS: int = 8
const CHARACTER_DISPLAY_SCENE_PATH: String = "res://scenes/CharacterDisplay.tscn"

var _display_scene: PackedScene

func _ready() -> void:
	_display_scene = load(CHARACTER_DISPLAY_SCENE_PATH) as PackedScene
	assert(_display_scene, "CharacterDisplay.tscn not found. Update CHARACTER_DISPLAY_SCENE_PATH.")

	_populate_species_option()
	species_option.item_selected.connect(_on_species_changed)
	randomize_btn.pressed.connect(_on_randomize_pressed)

	_refresh_all_with_current_species()

func _populate_species_option() -> void:
	species_option.clear()
	var keys: Array[String] = SpeciesAPI.list_species_keys()
	for i in range(keys.size()):
		species_option.add_item(keys[i], i)
	species_option.set_meta("keys", keys)

func _current_species_key() -> String:
	var keys: Array = species_option.get_meta("keys") as Array
	var idx: int = species_option.get_selected_id()
	if keys.is_empty(): return ""
	if idx < 0 or idx >= keys.size(): return String(keys[0])
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

	for _i in range(NUM_CHARACTERS):
		var display := _display_scene.instantiate()

		var disp := SpeciesFactory.create_displayable(species_key)
		var si   := SpeciesFactory.create_instance(species_key)
		var ch   := CharacterFactory.create_from_species_instance(si)

		if display.has_method("set_target"):
			display.call("set_target", disp)
		elif display.has_method("set_character"):
			display.call("set_character", ch)
		elif display.has_method("set_species_instance"):
			display.call("set_species_instance", si)
		else:
			push_warning("CharacterDisplay missing set_target/set_character/set_species_instance.")

		grid.add_child(display)
