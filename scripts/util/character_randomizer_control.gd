extends Control
class_name CharacterRandomizerControl

@onready var species_option: OptionButton = $MarginContainer/VBoxContainer/HBoxContainer/SpeciesOption
@onready var randomize_btn: Button       = $MarginContainer/VBoxContainer/HBoxContainer/RandomizeButton
@onready var grid: GridContainer         = $MarginContainer/VBoxContainer/DisplayGrid
@onready var equip_random_btn: Button    = $MarginContainer/VBoxContainer/HBoxContainer/EquipRandomButton

const NUM_CHARACTERS: int = 8
const DISPLAY_SCENE_PATH: String = "res://scenes/CharacterDisplayDebug.tscn"

var _display_scene: PackedScene
var _displays: Array[CharacterDisplay] = []
var _characters: Array[Character] = []

func _ready() -> void:
	randomize()

	_display_scene = load(DISPLAY_SCENE_PATH) as PackedScene
	if _display_scene == null:
		push_error("CharacterRandomizer: failed to load %s" % DISPLAY_SCENE_PATH)
		return

	_ensure_equipment_catalog_loaded()
	_populate_species_option()

	if species_option.item_count > 13:
		species_option.select(13)
	elif species_option.item_count > 0 and species_option.selected < 0:
		species_option.select(0)

	if equip_random_btn:
		equip_random_btn.pressed.connect(_on_equip_random_pressed)

	species_option.item_selected.connect(_on_species_changed)
	randomize_btn.pressed.connect(_on_randomize_pressed)

	_ensure_displays()
	_roll_all(_current_species_key())

# ---------------------------------------------------------
# Equipment randomizer (delegates to EquipmentFactory)
# ---------------------------------------------------------

func _ensure_equipment_catalog_loaded() -> void:
	# Autoload 'equipment_catalog' should exist
	if equipment_catalog == null:
		push_error("[Equip] equipment_catalog autoload not found.")
		return
	# Load once from json if empty
	if equipment_catalog.all.is_empty():
		equipment_catalog.load_from_json("res://assets/data/equipment.json")
		if equipment_catalog.all.is_empty():
			push_warning("[Equip] equipment.json failed to load or is empty.")
		else:
			print("[Equip] Loaded items: ", equipment_catalog.all.size())

func _on_equip_random_pressed() -> void:
	var changed_count: int = 0
	for c in _characters:
		if c == null:
			continue
		var before := c.get_all_equipment_instances().size()
		equipment_factory.equip_random_set(c, 4)  # <-- singleton, not the class
		var after := c.get_all_equipment_instances().size()
		if after > before:
			changed_count += 1
	print("[EquipRandom] changed=", changed_count, "/", _characters.size())

# ---------------------------------------------------------
# Species / grid population
# ---------------------------------------------------------

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

func _ensure_displays() -> void:
	if _display_scene == null:
		push_error("CharacterRandomizer: no display scene loaded; cannot build grid.")
		return
	if _displays.size() == NUM_CHARACTERS:
		return

	for child in grid.get_children():
		child.queue_free()
	_displays.clear()

	for _i in NUM_CHARACTERS:
		var inst := _display_scene.instantiate()
		if inst == null:
			push_error("CharacterRandomizer: failed to instantiate debug display.")
			continue
		grid.add_child(inst)

		var disp: CharacterDisplay = inst as CharacterDisplay
		if disp == null:
			push_error("CharacterRandomizer: CharacterDisplayDebug must extend CharacterDisplay.")
			continue
		_displays.append(disp)

func _roll_all(species_key: String) -> void:
	if species_key == "":
		push_warning("No species available to populate.")
		return

	_characters.clear()
	_characters.resize(NUM_CHARACTERS)
	for i in NUM_CHARACTERS:
		var ch := CharacterFactory.create_random_for_species_key(species_key)
		_characters[i] = ch
		_displays[i].set_character(ch)

func _on_species_changed(_index: int) -> void:
	_roll_all(_current_species_key())

func _on_randomize_pressed() -> void:
	_roll_all(_current_species_key())
