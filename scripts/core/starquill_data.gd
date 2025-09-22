extends Node
class_name StarquillDataManager

# Unified data registry for Starquill
# Replaces species_loader, equipment_catalog, and equipment_loader
# Handles all static game data loaded from JSON files

# Species data
var _species_all: Array[Species] = []
var _species_by_id: Dictionary = {} # String -> Species

# Equipment catalog data
var _equipment_all: Array[EquipmentCatalog.CatalogItem] = []
var _equipment_by_type: Dictionary = {} # String -> CatalogItem
var _equipment_by_slot_prefix: Dictionary = {
	"hd": [], "tr": [], "ar": [], "lg": [], "fe": [], "mc": []
}

# Handheld catalog data (weapons and shields)
var _handheld_all: Array = []
var _handheld_by_type: Dictionary = {} # String -> Dictionary

# Equipment template data (if needed - currently unused in codebase)
var _equipment_templates_all: Array[Equipment] = []
var _equipment_templates_by_id: Dictionary = {} # String -> Equipment

# Randomization constants for character generation
var randomization_constants: Dictionary = {
	# Facial feature chances (0.0 = never, 1.0 = always)
	"facial_hair_chance": 0.7,      # 70% chance to have facial hair
	"facial_detail_chance": 0.8,    # 80% chance to have facial detail
	
	# Equipment prefix chances (0.0 = never equipped, 1.0 = always equipped)
	"equipment_prefix_chances": {
		"hd": 0.9,  # head - 90% chance
		"tr": 1.0,  # torso - 100% chance (always equipped)
		"ar": 0.8,  # arms - 80% chance
		"lg": 1.0,  # legs - 100% chance (always equipped)
		"fe": 0.7,  # feet - 70% chance
		"mc": 0.3,  # misc - 30% chance per misc slot
		"w": 0.6    # weapons - 60% chance for main hand
	},
	
	# Main-slot equip priorities (1 = highest priority; 0 = no priority -> falls back to percentage)
	"equipment_prefix_priorities": {
		"hd": 0,
		"tr": 2,
		"ar": 0,
		"lg": 1,
		"fe": 0,
		"w": 0   # weapons use chance-based selection, not priority
	},

	# Misc selection weights (interpreted as percentages after normalization when toggle is enabled)
	# These are per-prefix weights for being chosen for a misc slot when enabled
	"equipment_prefix_misc_priorities": { # kept key name for backward compatibility
		"hd": 1.0,
		"tr": 1.0,
		"ar": 1.0,
		"lg": 1.0,
		"fe": 1.0,
		"mc": 1.0
	},

	# Toggle: when true, use the misc weights above (normalized) to choose prefix for misc slots.
	# When false, fall back to per-prefix equip chances for misc selection.
	"use_misc_prefix_percentages": true
}

func _ready() -> void:
	# Data will be loaded by ConfigManager during initialization
	pass

# ================================
# Species API (replaces species_loader)
# ================================

func get_species_count() -> int:
	return _species_all.size()

func get_species_by_id(species_id: String) -> Species:
	return _species_by_id.get(species_id, null)

func get_species_by_index(index: int) -> Species:
	if index < 0 or index >= _species_all.size():
		return null
	return _species_all[index]

func get_all_species() -> Array[Species]:
	return _species_all.duplicate()

func get_species_ids() -> Array[String]:
	var ids: Array[String] = []
	for key in _species_by_id.keys():
		ids.append(str(key))
	return ids

func clear_species() -> void:
	_species_all.clear()
	_species_by_id.clear()

func register_species(species: Species) -> void:
	if species == null:
		return
	_species_all.append(species)
	var key = str(species.name)
	_species_by_id[key] = species

# Species creation helpers (replaces SpeciesFactory functionality)
func create_species_instance(species_id: String) -> SpeciesInstance:
	var species = get_species_by_id(species_id)
	if species == null:
		push_error("StarquillData: Unknown species '%s'. Available: %s" % [
			species_id, str(get_species_ids())
		])
		return null
	
	var instance = SpeciesInstance.new()
	instance.from_species(species)
	return instance

func create_random_species_instance() -> SpeciesInstance:
	if _species_all.is_empty():
		return null
	
	var rng = RandomNumberGenerator.new()
	rng.randomize()
	var index = rng.randi_range(0, _species_all.size() - 1)
	var species = _species_all[index]
	
	var instance = SpeciesInstance.new()
	instance.from_species(species)
	return instance

# ================================
# Equipment Catalog API (replaces equipment_catalog)
# ================================

func get_equipment_count() -> int:
	return _equipment_all.size()

func get_equipment_by_type(item_type: String) -> EquipmentCatalog.CatalogItem:
	return _equipment_by_type.get(item_type, null)

func get_equipment_by_slot_prefix(prefix: String) -> Array:
	return _equipment_by_slot_prefix.get(prefix, [])

func get_all_equipment() -> Array[EquipmentCatalog.CatalogItem]:
	return _equipment_all.duplicate()

func clear_equipment() -> void:
	_equipment_all.clear()
	_equipment_by_type.clear()
	for key in _equipment_by_slot_prefix.keys():
		_equipment_by_slot_prefix[key] = []

func register_equipment_catalog_item(item: EquipmentCatalog.CatalogItem) -> void:
	if item == null:
		return
	
	_equipment_all.append(item)
	_equipment_by_type[item.item_type] = item
	
	# Add to slot prefix mapping
	var prefix = item.item_type.substr(0, 2).to_lower()
	if _equipment_by_slot_prefix.has(prefix):
		(_equipment_by_slot_prefix[prefix] as Array).append(item)

# Slot mapping helper (replaces EquipmentCatalog.slot_for_item_type)
func get_slot_for_item_type(item_type: String) -> String:
	var prefix = item_type.substr(0, 2).to_lower()
	match prefix:
		"hd": return "head"
		"tr": return "torso" 
		"ar": return "arms"
		"lg": return "legs"
		"fe": return "feet"
		"mc": return "misc"
		"w":  return "main_hand"
		_: return "misc"

# ================================
# Handheld Catalog API (weapons and shields)
# ================================

func get_handheld_count() -> int:
	return _handheld_all.size()

func get_handheld_by_type(item_type: String) -> Dictionary:
	return _handheld_by_type.get(item_type, {})

func get_all_handheld() -> Array:
	return _handheld_all.duplicate()

func clear_handheld() -> void:
	_handheld_all.clear()
	_handheld_by_type.clear()

func register_handheld_catalog_item(item_dict: Dictionary) -> void:
	if item_dict.is_empty():
		return

	_handheld_all.append(item_dict)
	var item_type = item_dict.get("item_type", "")
	if item_type != "":
		_handheld_by_type[item_type] = item_dict

# ================================
# Equipment Template API (replaces equipment_loader, if needed)
# ================================

func get_equipment_template_count() -> int:
	return _equipment_templates_all.size()

func get_equipment_template_by_id(equipment_id: String) -> Equipment:
	return _equipment_templates_by_id.get(equipment_id, null)

func clear_equipment_templates() -> void:
	_equipment_templates_all.clear()
	_equipment_templates_by_id.clear()

func register_equipment_template(equipment: Equipment) -> void:
	if equipment == null:
		return
	_equipment_templates_all.append(equipment)
	# Assuming Equipment has an item_type field for ID
	if "item_type" in equipment:
		_equipment_templates_by_id[str(equipment.item_type)] = equipment

# ================================
# Data Loading API (called by ConfigManager)
# ================================

func load_species_from_json(path: String) -> void:
	if not FileAccess.file_exists(path):
		push_warning("StarquillData: Species JSON not found: %s" % path)
		return
	
	clear_species()
	
	var text = FileAccess.get_file_as_string(path)
	var data = JSON.parse_string(text)
	
	if typeof(data) != TYPE_ARRAY:
		push_warning("StarquillData: Species JSON root must be an array: %s" % path)
		return
	
	for item_variant in data:
		if typeof(item_variant) != TYPE_DICTIONARY:
			continue
		
		var item = item_variant as Dictionary
		var species = Species.new()
		_map_dict_to_species(item, species)
		register_species(species)
	
	print("StarquillData: Loaded %d species from %s" % [get_species_count(), path])

func load_equipment_catalog_from_json(path: String) -> void:
	if not FileAccess.file_exists(path):
		push_error("StarquillData: Equipment JSON not found: %s" % path)
		return
	
	clear_equipment()
	
	var text = FileAccess.get_file_as_string(path)
	var data = JSON.parse_string(text)
	
	if typeof(data) != TYPE_ARRAY:
		push_error("StarquillData: Equipment JSON root must be an array: %s" % path)
		return
	
	for row_variant in data:
		if typeof(row_variant) != TYPE_DICTIONARY:
			continue
		
		var row = row_variant as Dictionary
		var item = EquipmentCatalog.CatalogItem.new(row)
		
		if item.item_type == "" or item.amount <= 0 or item.layer_codes.is_empty():
			continue
		
		register_equipment_catalog_item(item)
	
	print("StarquillData: Loaded %d equipment items from %s" % [get_equipment_count(), path])

func load_handheld_catalog_from_json(path: String) -> void:
	if not FileAccess.file_exists(path):
		push_error("StarquillData: Handheld JSON not found: %s" % path)
		return

	clear_handheld()

	var text = FileAccess.get_file_as_string(path)
	var data = JSON.parse_string(text)

	if typeof(data) != TYPE_ARRAY:
		push_error("StarquillData: Handheld JSON root must be an array: %s" % path)
		return

	for row_variant in data:
		if typeof(row_variant) != TYPE_DICTIONARY:
			continue

		var item_dict = row_variant as Dictionary

		# Validate required fields
		var valid = true
		var item_type = item_dict.get("item_type", "")
		var layer_codes = item_dict.get("layer_codes", [])
		var modular = item_dict.get("modular", false)
		var amount_data = item_dict.get("amount", 0)

		if item_type == "" or (layer_codes as Array).is_empty():
			valid = false
		elif modular:
			valid = typeof(amount_data) == TYPE_ARRAY and (amount_data as Array).size() > 0
		else:
			valid = (typeof(amount_data) == TYPE_INT or typeof(amount_data) == TYPE_FLOAT) and int(amount_data) > 0

		if not valid:
			continue

		register_handheld_catalog_item(item_dict)

	print("StarquillData: Loaded %d handheld items from %s" % [get_handheld_count(), path])

# ================================
# Helper Methods (from SpeciesLoader)
# ================================

func _map_dict_to_species(data_dict: Dictionary, species: Species) -> void:
	# ID/name
	if data_dict.has("id"):
		species.name = str(data_dict.id)
	elif data_dict.has("name"):
		species.name = str(data_dict.name)
	
	# Scales
	if data_dict.has("x_scale"):
		species.x_scale = float(data_dict.x_scale)
	if data_dict.has("y_scale"):
		species.y_scale = float(data_dict.y_scale)
	
	# String part fields (excluding hair which needs special handling)
	var string_fields = [
		"backArm", "body", "ears", "eyes", "facialDetail", "facialHair",
		"frontArm", "head", "legs", "mouth", "nose"
	]
	for field in string_fields:
		if data_dict.has(field):
			species.set(field, _first_string(data_dict[field]))
	
	# Special handling for hair field - preserve as array if needed
	if data_dict.has("hair"):
		var hair_data = data_dict["hair"]
		if typeof(hair_data) == TYPE_ARRAY:
			# Keep as array for multiple hair options
			species.hair = hair_data
		else:
			# Single hair option, convert to string
			species.hair = _first_string(hair_data)
	
	# Array fields
	if data_dict.has("otherBodyParts"):
		species.otherBodyParts = PackedStringArray(_to_string_array(data_dict.otherBodyParts))
	if data_dict.has("itemRestrictions"):
		species.itemRestrictions = PackedStringArray(_to_string_array(data_dict.itemRestrictions))
	
	# Color fields
	if data_dict.has("skin_color"):
		species.skin_color = _to_string_array(data_dict.skin_color)
	# Handle new skinVariance_sets structure
	if data_dict.has("skinVariance_sets"):
		var sets_array = data_dict.skinVariance_sets as Array
		if sets_array:
			species.skinVariance_sets = []
			for set_data in sets_array:
				if set_data is Dictionary:
					var set_dict = set_data as Dictionary
					if set_dict.has("indices") and set_dict.has("hex_colors"):
						var variance_dict = {
							"indices": _to_int_array(set_dict.indices),
							"hex_colors": _to_string_array(set_dict.hex_colors)
						}
						species.skinVariance_sets.append(variance_dict)
	
	# Backwards compatibility with old format (can be removed later)
	elif data_dict.has("skinVariance_hex") or data_dict.has("skinVariance_indices"):
		var old_hex = _to_string_array(data_dict.get("skinVariance_hex", []))
		var old_indices = _to_int_array(data_dict.get("skinVariance_indices", []))
		if not old_hex.is_empty() and not old_indices.is_empty():
			species.skinVariance_sets = []
			var variance_dict = {
				"indices": old_indices,
				"hex_colors": old_hex
			}
			species.skinVariance_sets.append(variance_dict)

# ================================
# Modular Image Number Generator (from SpeciesLoader)
# ================================

static var _modular_counts: Dictionary = {}

static func _ensure_modular_data_loaded() -> void:
	if not _modular_counts.is_empty():
		return
		
	var path := "res://assets/data/speciesModularParts.json"
	if not FileAccess.file_exists(path):
		push_error("StarquillData: speciesModularParts.json not found at %s" % path)
		return
	
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_error("StarquillData: failed to open speciesModularParts.json")
		return
	
	var json_text := file.get_as_text()
	file.close()
	
	var json := JSON.new()
	var parse_result := json.parse(json_text)
	if parse_result != OK:
		push_error("StarquillData: failed to parse speciesModularParts.json")
		return
	
	var data_array := json.get_data() as Array
	if data_array == null:
		push_error("StarquillData: speciesModularParts.json data is not an array")
		return
	
	# Build lookup dictionary
	for item in data_array:
		if item is Dictionary and item.has("type") and item.has("amount"):
			var type_code: String = str(item.type)
			var amount: int = int(item.amount)
			_modular_counts[type_code] = amount

static func pick_modular_image_num(code: String) -> String:
	_ensure_modular_data_loaded()
	
	var rng := RandomNumberGenerator.new()
	rng.randomize()
	var count := int(_modular_counts.get(code, 0))
	if count <= 0:
		push_warning("StarquillData: no modular count data for code '%s'" % code)
		return "0000"
	var idx := rng.randi_range(0, count - 1)
	return "%04d" % idx

# ================================
# Randomization Constants API
# ================================

func get_facial_hair_chance() -> float:
	return randomization_constants.get("facial_hair_chance", 0.7)

func set_facial_hair_chance(value: float) -> void:
	randomization_constants["facial_hair_chance"] = clamp(value, 0.0, 1.0)

func get_facial_detail_chance() -> float:
	return randomization_constants.get("facial_detail_chance", 0.8)

func set_facial_detail_chance(value: float) -> void:
	randomization_constants["facial_detail_chance"] = clamp(value, 0.0, 1.0)

func get_equipment_prefix_chance(prefix: String) -> float:
	var chances = randomization_constants.get("equipment_prefix_chances", {}) as Dictionary
	return chances.get(prefix, 1.0)

func set_equipment_prefix_chance(prefix: String, value: float) -> void:
	var chances = randomization_constants.get("equipment_prefix_chances", {}) as Dictionary
	chances[prefix] = clamp(value, 0.0, 1.0)
	randomization_constants["equipment_prefix_chances"] = chances

func get_equipment_prefix_misc_priority(prefix: String) -> float:
	var priorities = randomization_constants.get("equipment_prefix_misc_priorities", {}) as Dictionary
	return float(priorities.get(prefix, 1.0))

func set_equipment_prefix_misc_priority(prefix: String, value: float) -> void:
	var priorities = randomization_constants.get("equipment_prefix_misc_priorities", {}) as Dictionary
	priorities[prefix] = max(value, 0.0)  # Non-negative weight
	randomization_constants["equipment_prefix_misc_priorities"] = priorities

func get_all_equipment_prefix_chances() -> Dictionary:
	return randomization_constants.get("equipment_prefix_chances", {}).duplicate()

func get_all_equipment_prefix_misc_priorities() -> Dictionary:
	return randomization_constants.get("equipment_prefix_misc_priorities", {}).duplicate()

# ================================
# Equip Priority API (main slots)
# ================================

func get_equipment_prefix_priority(prefix: String) -> int:
	var table = randomization_constants.get("equipment_prefix_priorities", {}) as Dictionary
	return int(table.get(prefix, 0))

func set_equipment_prefix_priority(prefix: String, value: int) -> void:
	var table = randomization_constants.get("equipment_prefix_priorities", {}) as Dictionary
	table[prefix] = max(value, 0)
	randomization_constants["equipment_prefix_priorities"] = table

func get_all_equipment_prefix_priorities() -> Dictionary:
	return randomization_constants.get("equipment_prefix_priorities", {}).duplicate()

# ================================
# Misc Selection Toggle
# ================================

func get_use_misc_prefix_percentages() -> bool:
	return bool(randomization_constants.get("use_misc_prefix_percentages", false))

func set_use_misc_prefix_percentages(value: bool) -> void:
	randomization_constants["use_misc_prefix_percentages"] = value

# ================================
# Helper methods for data mapping
# ================================

static func _first_string(variant: Variant) -> String:
	match typeof(variant):
		TYPE_STRING:
			return str(variant)
		TYPE_ARRAY:
			var arr = variant as Array
			if arr.is_empty():
				return ""
			return str(arr[0])
		_:
			return ""

static func _to_string_array(variant: Variant) -> Array:
	var result: Array = []
	if typeof(variant) == TYPE_ARRAY:
		for element in variant:
			result.append(str(element))
	elif typeof(variant) == TYPE_STRING:
		result.append(str(variant))
	return result

static func _to_int_array(variant: Variant) -> Array:
	var result: Array = []
	if typeof(variant) == TYPE_ARRAY:
		for element in variant:
			result.append(int(element))
	elif typeof(variant) == TYPE_INT:
		result.append(int(variant))
	return result
