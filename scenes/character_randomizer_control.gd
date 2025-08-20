extends Control
class_name CharacterRandomizerControl

@onready var species_option: OptionButton = $MarginContainer/VBoxContainer/HBoxContainer/SpeciesOption
@onready var randomize_btn: Button       = $MarginContainer/VBoxContainer/HBoxContainer/RandomizeButton
@onready var grid: GridContainer         = $MarginContainer/VBoxContainer/DisplayGrid

const NUM_CHARACTERS: int = 8
const CHARACTER_DISPLAY_SCENE := preload("res://scenes/CharacterDisplay.tscn")

var _displays: Array[CharacterDisplay] = []
var _characters: Array[Character] = []

func _ready() -> void:
	_populate_species_option()
	if species_option.item_count > 13: #start with human selected if available
		species_option.select(13)
	elif species_option.item_count > 0 and species_option.selected < 0:
		species_option.select(0)

	species_option.item_selected.connect(_on_species_changed)
	randomize_btn.pressed.connect(_on_randomize_pressed)

	_ensure_displays()
	_roll_all(_current_species_key())

func _populate_species_option() -> void:
	species_option.clear()
	var keys: Array[String] = SpeciesAPI.list_species_keys()
	for i in keys.size():
		species_option.add_item(keys[i], i)
	species_option.set_meta("keys", keys)

func _current_species_key() -> String:
	var keys: Array = species_option.get_meta("keys") as Array
	if keys.is_empty(): return ""
	var idx := species_option.get_selected_id()
	if idx < 0 or idx >= keys.size():
		idx = 0
	return String(keys[idx])

func _on_species_changed(_index: int) -> void:
	_roll_all(_current_species_key())

func _on_randomize_pressed() -> void:
	_roll_all(_current_species_key())

func _ensure_displays() -> void:
	if _displays.size() == NUM_CHARACTERS:
		return
	for child in grid.get_children():
		child.queue_free()
	_displays.clear()
	for _i in NUM_CHARACTERS:
		var d := CHARACTER_DISPLAY_SCENE.instantiate() as CharacterDisplay
		grid.add_child(d)
		_displays.append(d)

func _roll_all(species_key: String) -> void:
	if species_key == "":
		push_warning("No species available to populate.")
		return

	_characters.clear()
	_characters.resize(NUM_CHARACTERS)
	for i in NUM_CHARACTERS:
		var ch := CharacterFactory.create_random_for_species_key(species_key)
		_characters[i] = ch
		_displays[i].set_character(ch)  # bind (no re-instantiation)
