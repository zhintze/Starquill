extends Control
class_name CharacterRandomizerControl

@onready var species_option: OptionButton = $MarginContainer/VBoxContainer/HBoxContainer/SpeciesOption
@onready var randomize_btn: Button       = $MarginContainer/VBoxContainer/HBoxContainer/RandomizeButton
@onready var grid: GridContainer         = $MarginContainer/VBoxContainer/DisplayGrid
@onready var equip_random_btn: Button = $MarginContainer/VBoxContainer/HBoxContainer/EquipRandomButton

const NUM_CHARACTERS: int = 8

const DEBUG_DISPLAYS := true
const CHARACTER_DISPLAY_SCENE_DEBUG := preload("res://scenes/CharacterDisplayDebug.tscn")
const CHARACTER_DISPLAY_SCENE_BASE := preload("res://scenes/CharacterDisplay.tscn")
const CHARACTER_DISPLAY_SCENE := CHARACTER_DISPLAY_SCENE_DEBUG if DEBUG_DISPLAYS else CHARACTER_DISPLAY_SCENE_BASE

var _displays: Array[CharacterDisplay] = []
var _characters: Array[Character] = []
var _toast: Label

func _ready() -> void:
	
	randomize()
	# Toast (create if not in scene)
	_toast = $Toast if has_node("Toast") else Label.new()
	if _toast.get_parent() == null:
		_toast.name = "Toast"
		_toast.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		_toast.modulate = Color(1,1,1,0.9)
		add_child(_toast)
		_toast.anchor_left = 1.0
		_toast.anchor_top = 0.0
		_toast.offset_left = -320
		_toast.offset_top = 8
		
		
		
	_populate_species_option()
	if species_option.item_count > 13: #start with human selected if available
		species_option.select(13)
	elif species_option.item_count > 0 and species_option.selected < 0:
		species_option.select(0)
		
	if equip_random_btn:
		equip_random_btn.pressed.connect(_on_equip_random_pressed)

	species_option.item_selected.connect(_on_species_changed)
	randomize_btn.pressed.connect(_on_randomize_pressed)

	_ensure_displays()
	_roll_all(_current_species_key())
	_ensure_equipment_catalog_loaded()
	
func _on_equip_random_pressed() -> void:
	if _characters.is_empty():
		_toast_msg("No characters in grid.")
		return
	if equipment_catalog.all.is_empty():
		_toast_msg("Catalog empty — cannot equip. Check equipment.json load.")
		return
	
	var changed_count: int = 0
	for c in _characters:
		if c:
			var before := c.get_all_equipment_instances().size()
			_equip_random_set(c)
			var after := c.get_all_equipment_instances().size()
			if after > before:
				changed_count += 1
	_toast_msg("Equipped random sets for %d/%d characters." % [changed_count, _characters.size()])
		
# Pick a random equipment instance from a slot prefix bucket, e.g. "hd","tr","ar","lg","fe","mc"
func _rand_from_bucket(prefix: String) -> EquipmentInstance:
	var bucket_any: Variant = equipment_catalog.by_slot_prefix.get(prefix, [])
	if bucket_any == null:
		_toast_msg("Bucket '%s' missing in catalog." % prefix)
		return null
	var bucket: Array = bucket_any as Array
	if bucket.is_empty():
		_toast_msg("Bucket '%s' is empty." % prefix)
		return null

	var idx: int = randi() % bucket.size()
	var cat: EquipmentCatalog.CatalogItem = bucket[idx] as EquipmentCatalog.CatalogItem
	if cat == null:
		_toast_msg("Bucket '%s' item cast failed." % prefix)
		return null

	var ei := EquipmentInstance.new()
	ei.item_type = cat.item_type
	var amt: int = max(1, cat.amount)
	ei.item_num = (randi() % amt) + 1
	return ei


func _equip_random_set(ch: Character) -> void:
	if ch == null: return
	ch.clear_equipment()

	# main slots
	var head := _rand_from_bucket("hd")
	var torso := _rand_from_bucket("tr")
	var arms := _rand_from_bucket("ar")
	var legs := _rand_from_bucket("lg")
	var feet := _rand_from_bucket("fe")
	for ei in [head, torso, arms, legs, feet]:
		if ei != null:
			ch.equip_instance(ei)

	# overflow misc — up to 4 more (mc allowed, plus extra of others as misc if main is filled)
	var pick_pool: Array[String] = ["mc", "hd", "tr", "ar", "lg", "fe"]
	for i in 4:
		var pref: String = pick_pool[randi() % pick_pool.size()]
		var extra := _rand_from_bucket(pref)
		if extra != null:
			ch.equip_instance(extra)

func _clear_equipment(ch: Character) -> void:
	# Adjust to your exact field names (using *_inst as we discussed)
	ch.head_inst = null
	ch.torso_inst = null
	ch.arms_inst = null
	ch.legs_inst = null
	ch.feet_inst = null
	ch.misc1_inst = null
	ch.misc2_inst = null
	ch.misc3_inst = null
	ch.misc4_inst = null
	ch._recalc_stats()
	ch.emit_signal("model_changed")


func _get_all_characters_on_grid() -> Array[Character]:
	# Prefer the model list we already track
	return _characters.filter(func(c): return c != null)

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
		
		
func _toast_msg(msg: String) -> void:
	if _toast:
		_toast.text = msg
	print("[Randomizer] ", msg)

func _ensure_equipment_catalog_loaded() -> void:
	# If catalog is empty, try to load once
	if equipment_catalog.all.is_empty():
		# Adjust path if yours differs:
		equipment_catalog.load_from_json("res://assets/data/equipment.json")
		if equipment_catalog.all.is_empty():
			_toast_msg("No equipment loaded (equipment.json empty or missing).")
		else:
			_toast_msg("Loaded %d equipment items." % equipment_catalog.all.size())
