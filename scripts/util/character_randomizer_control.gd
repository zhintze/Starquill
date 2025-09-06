extends Control
class_name CharacterRandomizerControl

@onready var species_option: OptionButton = $MarginContainer/VBoxContainer/HBoxContainer/SpeciesOption
@onready var randomize_btn: Button       = $MarginContainer/VBoxContainer/HBoxContainer/RandomizeButton
@onready var grid: GridContainer         = $MarginContainer/VBoxContainer/DisplayGrid
@onready var equip_random_btn: Button    = $MarginContainer/VBoxContainer/HBoxContainer/EquipRandomButton
@onready var export_png_btn: Button      = $MarginContainer/VBoxContainer/HBoxContainer/ExportPNGButton

# Equipment search UI
@onready var search_bar: LineEdit = $MarginContainer/VBoxContainer/HBoxContainer/EquipSearchContainer/SearchBar
@onready var search_results: ItemList = $SearchResults
@onready var selected_items: FlowContainer = $MarginContainer/VBoxContainer/SelectedEquipmentContainer/SelectedItems
@onready var no_selection_label: Label = $MarginContainer/VBoxContainer/SelectedEquipmentContainer/NoSelectionLabel

const NUM_CHARACTERS: int = 8
const DISPLAY_SCENE_PATH: String = "res://scenes/CharacterDisplayDebug.tscn"

var _display_scene: PackedScene
var _displays: Array[CharacterDisplay] = []
var _characters: Array[Character] = []

# Forced equipment management
var _forced_equipment: Array[String] = []  # Array of "item_type-variant" strings like "hd01-0001"
var _all_equipment_variants: Array[String] = []  # Cache of all searchable equipment variants

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
	
	if export_png_btn:
		export_png_btn.pressed.connect(_on_export_png_pressed)

	species_option.item_selected.connect(_on_species_changed)
	randomize_btn.pressed.connect(_on_randomize_pressed)
	
	# Connect search bar events
	if search_bar:
		search_bar.text_changed.connect(_on_search_text_changed)
		search_bar.gui_input.connect(_on_search_gui_input)
	if search_results:
		search_results.item_selected.connect(_on_search_item_selected)
	
	_build_equipment_variants_cache()
	_update_selection_ui_visibility()

	_ensure_displays()
	_roll_all(_current_species_key())

# ---------------------------------------------------------
# Equipment randomizer (delegates to EquipmentFactory)
# ---------------------------------------------------------

func _ensure_equipment_catalog_loaded() -> void:
	# Equipment is loaded by ConfigManager during initialization
	# StarquillData is available as autoload
	if StarquillData.get_equipment_count() == 0:
		# Try to load equipment data as fallback
		StarquillData.load_equipment_catalog_from_json("res://assets/data/equipment.json")
		if StarquillData.get_equipment_count() == 0:
			push_warning("[Equip] equipment.json failed to load or is empty.")
		else:
			print("[Equip] Loaded items: ", StarquillData.get_equipment_count())

func _on_equip_random_pressed() -> void:
	var changed_count: int = 0
	for c in _characters:
		if c == null:
			continue
		var before := c.get_all_equipment_instances().size()
		_apply_forced_equipment_to_character(c)  # Apply forced equipment first, then random
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
		# Apply forced equipment to the new character
		_apply_forced_equipment_to_character(ch)
		_characters[i] = ch
		_displays[i].set_character(ch)

func _roll_characters_without_equipment(species_key: String) -> void:
	if species_key == "":
		push_warning("No species available to populate.")
		return

	_characters.clear()
	_characters.resize(NUM_CHARACTERS)
	for i in NUM_CHARACTERS:
		var ch := CharacterFactory.create_random_for_species_key(species_key)
		# Clear any equipment that might have been randomly assigned
		ch.clear_equipment()
		# Only apply forced equipment (if any)
		_apply_forced_equipment_only_to_character(ch)
		_characters[i] = ch
		_displays[i].set_character(ch)

func _on_species_changed(_index: int) -> void:
	_roll_all(_current_species_key())

func _on_randomize_pressed() -> void:
	_roll_characters_without_equipment(_current_species_key())

# ---------------------------------------------------------
# PNG Export Functionality
# ---------------------------------------------------------

func _on_export_png_pressed() -> void:
	var file_dialog = FileDialog.new()
	file_dialog.file_mode = FileDialog.FILE_MODE_OPEN_DIR
	file_dialog.access = FileDialog.ACCESS_FILESYSTEM
	file_dialog.current_dir = OS.get_system_dir(OS.SYSTEM_DIR_DOWNLOADS)
	
	get_tree().root.add_child(file_dialog)
	file_dialog.popup_centered(Vector2i(800, 600))
	
	# Connect signals with dialog reference for cleanup
	var cleanup_func = func(): file_dialog.queue_free()
	file_dialog.dir_selected.connect(func(path: String): _on_export_dir_selected(path, file_dialog))
	file_dialog.canceled.connect(cleanup_func)

func _on_export_dir_selected(dir_path: String, dialog: FileDialog) -> void:
	dialog.queue_free()
	_export_characters_to_png(dir_path)

func _export_characters_to_png(base_dir: String) -> void:
	var timestamp = Time.get_datetime_string_from_system().replace(":", "-").replace(" ", "_")
	var species_key = _current_species_key()
	
	for i in range(_displays.size()):
		var display = _displays[i]
		var character = _characters[i]
		
		if character == null or display == null:
			continue
			
		# Create filename: timestamp_species_characterNumber.png
		var filename = "%s_%s_character%d.png" % [timestamp, species_key, i + 1]
		var full_path = base_dir + "/" + filename
		
		_export_single_character(display, character, full_path)
	
	print("[Export] Exported %d character PNGs to: %s" % [_displays.size(), base_dir])

func _export_single_character(display: CharacterDisplay, character: Character, file_path: String) -> void:
	# Get the character's scale from the species
	var scale_factor = Vector2.ONE
	if character and character.species:
		var inst: SpeciesInstance = character.species
		if inst.base:
			scale_factor = Vector2(inst.base.x_scale, inst.base.y_scale)
		scale_factor *= Vector2(inst.x_scale, inst.y_scale)
	
	# Calculate export size: 200x200 * scale
	var export_size = Vector2i(
		int(200.0 * scale_factor.x),
		int(200.0 * scale_factor.y)
	)
	
	# Create a temporary SubViewport for clean rendering
	var viewport = SubViewport.new()
	viewport.size = export_size
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	viewport.transparent_bg = true
	
	# Create a simple container to hold the character
	var container = Control.new()
	container.size = Vector2(export_size)
	viewport.add_child(container)
	
	# Get display pieces directly from character
	var pieces: Array[DisplayPiece] = character.get_display_pieces()
	
	# Create TextureRect nodes for each piece
	for p in pieces:
		if p.texture == null:
			continue
			
		var tex_rect = TextureRect.new()
		tex_rect.texture = p.texture
		tex_rect.modulate = p.modulate
		tex_rect.position = p.offset * scale_factor
		tex_rect.scale = p.scale * scale_factor
		tex_rect.flip_h = p.flip_h
		tex_rect.flip_v = p.flip_v
		tex_rect.stretch_mode = TextureRect.STRETCH_KEEP
		tex_rect.expand_mode = TextureRect.EXPAND_KEEP_SIZE
		
		container.add_child(tex_rect)
	
	# Add viewport to scene temporarily
	get_tree().root.add_child(viewport)
	
	# Force render and capture
	await get_tree().process_frame
	await get_tree().process_frame
	
	var texture = viewport.get_texture()
	var image = texture.get_image()
	
	# Save as PNG
	var error = image.save_png(file_path)
	if error != OK:
		push_error("Failed to save PNG: %s (Error: %d)" % [file_path, error])
	else:
		print("[Export] Saved: %s" % file_path)
	
	# Cleanup
	viewport.queue_free()

# ---------------------------------------------------------
# Equipment Search and Forced Equipment Management
# ---------------------------------------------------------

func _build_equipment_variants_cache() -> void:
	_all_equipment_variants.clear()
	var equipment_items = StarquillData.get_all_equipment()
	
	for item in equipment_items:
		var item_type: String = item.item_type
		var amount: int = item.amount
		
		# Generate all variants for this equipment type
		for variant in range(1, amount + 1):
			var variant_string = "%s-%04d" % [item_type, variant]
			_all_equipment_variants.append(variant_string)
	
	print("[Search] Built cache of ", _all_equipment_variants.size(), " equipment variants")

func _on_search_text_changed(new_text: String) -> void:
	if new_text.length() == 0:
		search_results.visible = false
		return
	
	search_results.clear()
	var matches: Array[String] = []
	
	# Filter variants that contain the search text
	for variant in _all_equipment_variants:
		if new_text.to_lower() in variant.to_lower():
			matches.append(variant)
		
		# Limit results to prevent performance issues
		if matches.size() >= 20:
			break
	
	# Populate results list
	for match in matches:
		search_results.add_item(match)
	
	if matches.size() > 0:
		_position_dropdown_below_search_bar()
		search_results.visible = true
	else:
		search_results.visible = false

func _on_search_gui_input(event: InputEvent) -> void:
	# Hide results when clicking outside or pressing escape
	if event is InputEventKey:
		var key_event = event as InputEventKey
		if key_event.pressed and key_event.keycode == KEY_ESCAPE:
			search_results.visible = false
			search_bar.release_focus()

func _on_search_item_selected(index: int) -> void:
	if index < 0 or index >= search_results.get_item_count():
		return
	
	var selected_variant = search_results.get_item_text(index)
	_add_forced_equipment(selected_variant)
	
	# Clear search
	search_bar.clear()
	search_results.visible = false
	search_bar.release_focus()

func _add_forced_equipment(variant: String) -> void:
	# Check if already selected
	if variant in _forced_equipment:
		print("[ForceEquip] ", variant, " already selected")
		return
	
	# Validate equipment limits
	if not _validate_equipment_addition(variant):
		return
	
	# Add to forced equipment list
	_forced_equipment.append(variant)
	_create_selected_item_ui(variant)
	_update_selection_ui_visibility()
	
	# Re-apply equipment to all characters
	_apply_forced_equipment_to_all_characters()
	
	print("[ForceEquip] Added ", variant, " (total: ", _forced_equipment.size(), ")")

func _validate_equipment_addition(variant: String) -> bool:
	var item_type = _extract_item_type(variant)
	var slot = StarquillData.get_slot_for_item_type(item_type)
	
	# Count items of same slot type already selected
	var same_slot_count = 0
	for existing_variant in _forced_equipment:
		var existing_type = _extract_item_type(existing_variant)
		var existing_slot = StarquillData.get_slot_for_item_type(existing_type)
		if existing_slot == slot:
			same_slot_count += 1
	
	# Check slot limits: main slot (1) + misc slots (4) = max 5 per slot type
	var max_allowed = 5 if slot != "misc" else 4
	
	if same_slot_count >= max_allowed:
		_show_equipment_limit_warning(variant, slot, same_slot_count)
		return false
	
	return true

func _extract_item_type(variant: String) -> String:
	# Extract "hd01" from "hd01-0001"
	var parts = variant.split("-")
	return parts[0] if parts.size() > 0 else ""

func _show_equipment_limit_warning(variant: String, slot: String, current_count: int) -> void:
	var popup = AcceptDialog.new()
	popup.title = "Equipment Limit Exceeded"
	popup.dialog_text = "Cannot add %s.\nSlot '%s' is already at maximum capacity (%d items).\nRemove an existing item first." % [variant, slot, current_count]
	
	get_tree().root.add_child(popup)
	popup.popup_centered()
	popup.connect("confirmed", func(): popup.queue_free())
	popup.connect("canceled", func(): popup.queue_free())

func _create_selected_item_ui(variant: String) -> void:
	var container = HBoxContainer.new()
	
	# Equipment label
	var label = Label.new()
	label.text = variant
	container.add_child(label)
	
	# Remove button
	var remove_btn = Button.new()
	remove_btn.text = "X"
	remove_btn.custom_minimum_size = Vector2(20, 20)
	remove_btn.pressed.connect(func(): _remove_forced_equipment(variant))
	container.add_child(remove_btn)
	
	# Store reference for later removal
	container.set_meta("variant", variant)
	selected_items.add_child(container)

func _remove_forced_equipment(variant: String) -> void:
	# Remove from forced equipment list
	_forced_equipment.erase(variant)
	
	# Remove UI element
	for child in selected_items.get_children():
		if child.has_meta("variant") and child.get_meta("variant") == variant:
			child.queue_free()
			break
	
	# Update UI visibility
	_update_selection_ui_visibility()
	
	# Re-apply equipment to all characters
	_apply_forced_equipment_to_all_characters()
	
	print("[ForceEquip] Removed ", variant, " (remaining: ", _forced_equipment.size(), ")")

func _apply_forced_equipment_to_all_characters() -> void:
	for character in _characters:
		if character == null:
			continue
		
		_apply_forced_equipment_to_character(character)

func _apply_forced_equipment_to_character(character: Character) -> void:
	# Clear all equipment first
	character.clear_equipment()
	
	# Fill slots with random equipment first
	equipment_factory.equip_random_set(character, 4)
	
	# Apply forced equipment with smart slot assignment
	var prefixes_used: Dictionary = {}  # Track which prefixes have been placed in their natural slots
	
	for variant in _forced_equipment:
		var item_type = _extract_item_type(variant)
		var item_num = _extract_item_num(variant)
		var prefix = item_type.substr(0, 2).to_lower()
		
		# Use EquipmentFactory to create the equipment instance properly
		var catalog_item = StarquillData.get_equipment_by_type(item_type)
		if catalog_item == null:
			push_warning("ForceEquip: No catalog item found for type: %s" % item_type)
			continue
		
		var equipment_instance = equipment_factory.create_from_catalog(catalog_item, int(item_num))
		if equipment_instance == null:
			push_warning("ForceEquip: Failed to create equipment instance for: %s" % variant)
			continue
		
		# Smart slot assignment: first of each prefix goes to natural slot, subsequent ones go to misc
		if prefixes_used.has(prefix):
			# This prefix already used - force into misc slot
			_force_equip_to_misc_slot(character, equipment_instance)
		else:
			# First time seeing this prefix - use natural slot
			prefixes_used[prefix] = true
			character.equip_instance(equipment_instance)

func _apply_forced_equipment_only_to_character(character: Character) -> void:
	# Apply only forced equipment (no random equipment)
	for variant in _forced_equipment:
		var item_type = _extract_item_type(variant)
		var item_num = _extract_item_num(variant)
		
		# Use EquipmentFactory to create the equipment instance properly
		var catalog_item = StarquillData.get_equipment_by_type(item_type)
		if catalog_item == null:
			push_warning("ForceEquip: No catalog item found for type: %s" % item_type)
			continue
		
		var equipment_instance = equipment_factory.create_from_catalog(catalog_item, int(item_num))
		if equipment_instance == null:
			push_warning("ForceEquip: Failed to create equipment instance for: %s" % variant)
			continue
		
		character.equip_instance(equipment_instance)

func _extract_item_num(variant: String) -> String:
	# Extract "0001" from "hd01-0001"
	var parts = variant.split("-")
	return parts[1] if parts.size() > 1 else "0001"

func _force_equip_to_misc_slot(character: Character, equipment_instance: EquipmentInstance) -> void:
	# Directly assign to first available misc slot
	if character.misc1 == null:
		character.misc1 = equipment_instance
	elif character.misc2 == null:
		character.misc2 = equipment_instance
	elif character.misc3 == null:
		character.misc3 = equipment_instance
	elif character.misc4 == null:
		character.misc4 = equipment_instance
	else:
		# All misc slots full - replace misc1 (latest wins)
		character.misc1 = equipment_instance
	
	# Trigger character updates
	character._assign_colors_for_equipment_variants()
	character._recalc_stats()
	character.emit_signal("model_changed")

func _update_selection_ui_visibility() -> void:
	if no_selection_label:
		no_selection_label.visible = _forced_equipment.is_empty()

func _position_dropdown_below_search_bar() -> void:
	if search_bar and search_results:
		var global_rect = search_bar.get_global_rect()
		var local_pos = get_global_transform().affine_inverse() * global_rect.position
		search_results.position = Vector2(local_pos.x, local_pos.y + search_bar.size.y)
		search_results.size = Vector2(search_bar.size.x, 100)
