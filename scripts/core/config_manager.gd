extends Node
class_name StarquillConfigManager

# Unified configuration management for Starquill
# Consolidates GameConstants and BootConfig functionality
# This singleton loads first and provides all configuration data

# Configuration data structure
var _config_data: Dictionary = {}

var _launched: bool = false

func _ready() -> void:
	_initialize_default_config()
	_load_project_overrides()
	_initialize_application()

func _initialize_default_config() -> void:
	_config_data = {
	"data_paths": {
		"species_json_candidates": [
			"res://assets/data/species.json",
			"res://data/species.json"
		],
		"species_dir_candidates": [
			"res://assets/data/species/",
			"res://data/species/"
		],
		"equipment_json_candidates": [
			"res://assets/data/equipment.json",
			"res://data/equipment.json"
		],
		"equipment_dir_candidates": [
			"res://assets/data/equipment/",
			"res://data/equipment/"
		],
		"color_palettes_path": "res://assets/data/color_palettes.json",
		"palette_csv_candidates": [
			"res://documents/color_main.csv"
		]
	},
	"scene_paths": {
		"default_target": "res://scenes/CharacterRandomizer.tscn"
	},
	"layer_constants": {
		"species_layers": {
			# Face groups
			"f01": [84],          # eyes
			"f02": [86],          # nose
			"f03": [85],          # mouth
			"f04": [100],         # ears
			"f05": [122],         # facialHair
			"f06": [87],          # facialDetail
			
			# Hair groups (front/back overlays)
			"h01": [92],
			"h02": [92, 128],
			"h03": [92, 154],
			"h04": [92, 128, 154],
			
			# Dwarf/gnome variants
			"d02": [86],
			"d04": [100],
			"d05": [122],
			"d06": [87],
			"g02": [86],
			
			# Skeleton families
			"s01": [82],   # head
			"s02": [84],   # eyes
			"s03": [86],   # nose
			"s04": [87]    # facialDetail
		}
	},
	"error_messages": {
		"file_not_found": "File not found: %s",
		"invalid_json": "Invalid JSON format in file: %s",
		"empty_registry": "Registry is empty after loading data",
		"palette_missing": "Color palette '%s' not found; falling back to 'main'",
		"species_load_failed": "Failed to load species data from: %s",
		"equipment_load_failed": "Failed to load equipment data from: %s"
	},
	"species_fields": {
		"string_fields": [
			"backArm", "body", "ears", "eyes", "facialDetail", "facialHair",
			"frontArm", "hair", "head", "legs", "mouth", "nose"
		],
		"array_fields": [
			"otherBodyParts", "itemRestrictions",
			"skin_color", "hair_color", "eyes_color", "facialDetail_color",
			"skinVariance_hex"
		]
	},
	"validation": {
		"hex_color_length": 6,
		"min_id_length": 1
	},
	"boot_settings": {
		"target_scene_path": "res://scenes/CharacterRandomizer.tscn",
		"fail_fast": false
	}
}

func _load_project_overrides() -> void:
	# Try to load project-specific config overrides
	var override_path = "res://config/starquill_config.json"
	if FileAccess.file_exists(override_path):
		if load_config_overrides(override_path):
			print("ConfigManager: Loaded project overrides from ", override_path)
	
	# Also check for user-specific local overrides (not committed to git)
	var local_override_path = "res://config/local_config.json"
	if FileAccess.file_exists(local_override_path):
		if load_config_overrides(local_override_path):
			print("ConfigManager: Loaded local overrides from ", local_override_path)

# ================================
# Application Initialization (merged from data_boot.gd)
# ================================

func _initialize_application() -> void:
	var ok_all := true
	ok_all = ok_all and _task_load_species()
	ok_all = ok_all and _task_verify_palette_hint()

	# Hook: place any additional boot tasks here now or later
	# ok_all = ok_all and _task_load_equipment()
	# ok_all = ok_all and _task_load_items()

	# If nothing loaded at all, shout loudly
	if StarquillData.get_species_count() == 0:
		push_error("ConfigManager: Species registry is empty after boot tasks. Check paths or JSON schema.")
		# You can choose to bail here or still launch target; up to you.

	_launch_target_scene(get_target_scene_path())

# -------------------------
# Boot Tasks (modular)
# -------------------------

func _task_load_species() -> bool:
	var species_json_candidates = get_data_paths("species_json_candidates")
	var species_dir_candidates = get_data_paths("species_dir_candidates")
	var fail_fast = get_fail_fast()
	
	# Try JSONs (first hit wins)
	for p in species_json_candidates:
		if FileAccess.file_exists(p):
			StarquillData.load_species_from_json(p)
			_log_species_registry("Species from JSON", p)
			if StarquillData.get_species_count() > 0:
				return true
			elif fail_fast:
				push_error("ConfigManager: JSON '%s' parsed but no species added; stopping (fail_fast)." % p)
				return false

	# Try folders (TODO: implement directory loading in StarquillData if needed)
	for dpath in species_dir_candidates:
		var da := DirAccess.open(dpath)
		if da != null:
			# Directory loading not yet implemented in StarquillData
			push_warning("ConfigManager: Directory loading not implemented for StarquillData: %s" % dpath)
			continue

	push_warning("ConfigManager: No species sources found in any configured paths.")
	return not fail_fast

func _task_verify_palette_hint() -> bool:
	# Non-blocking sanity check: warn if no palette CSV is present.
	var palette_csv_candidates = get_data_paths("palette_csv_candidates")
	
	for p in palette_csv_candidates:
		if FileAccess.file_exists(p):
			print("Palette CSV detected: ", p)
			return true
	push_warning("ConfigManager: No palette CSV found in candidates; SpeciesLoader will use fallback defaults.")
	return true  # never blocks

# -------------------------
# Scene Launch Helpers
# -------------------------

# Defer the scene change out of _ready
func _launch_target_scene(scene_path: String) -> void:
	if _launched:
		return
	_launched = true

	var packed := load(scene_path)
	if not (packed is PackedScene):
		push_error("ConfigManager: Could not load target scene: %s" % scene_path)
		return

	# Defer to avoid "busy adding/removing children"
	call_deferred("_do_change_scene", packed)

func _do_change_scene(packed: PackedScene) -> void:
	# Be resilient: if this node isn't in-tree yet, fallback to Engine.get_main_loop()
	var tree := get_tree()
	if tree == null:
		tree = Engine.get_main_loop() as SceneTree

	# If still null, try again next frame
	if tree == null:
		push_warning("ConfigManager: SceneTree not ready; retrying next frame…")
		call_deferred("_do_change_scene", packed)
		return

	# Optional: skip if we're already in the target scene
	if tree.current_scene and tree.current_scene.scene_file_path == packed.resource_path:
		return

	# Change scenes safely
	tree.change_scene_to_packed(packed)

func _log_species_registry(prefix: String, src: String) -> void:
	print("%s: %s | count=%d | ids=%s" % [
		prefix, src, StarquillData.get_species_count(), str(StarquillData.get_species_ids())
	])

# ================================
# Path Access Methods
# ================================

func get_data_path(key: String) -> String:
	return _config_data.data_paths.get(key, "")

func get_data_paths(key: String) -> Array[String]:
	var paths = _config_data.data_paths.get(key, [])
	if paths is Array:
		var result: Array[String] = []
		for path in paths:
			result.append(str(path))
		return result
	return []

func get_scene_path(key: String) -> String:
	return _config_data.scene_paths.get(key, "")

# ================================
# Layer Access Methods
# ================================

func get_species_layers(layer_code: String) -> Array:
	var layers = _config_data.layer_constants.species_layers.get(layer_code, [])
	if layers is Array:
		return layers
	return []

# ================================
# Error Message Methods
# ================================

func get_error_message(key: String, args: Array = []) -> String:
	var template: String = _config_data.error_messages.get(key, "Unknown error")
	if args.size() > 0:
		return template % args
	return template

# ================================
# Species Field Methods
# ================================

func get_species_string_fields() -> Array[String]:
	return _config_data.species_fields.string_fields.duplicate()

func get_species_array_fields() -> Array[String]:
	return _config_data.species_fields.array_fields.duplicate()

# ================================
# Validation Methods
# ================================

func get_validation_constant(key: String) -> int:
	return _config_data.validation.get(key, 0)

# ================================
# Boot Configuration Methods (BootConfig replacement)
# ================================

func get_target_scene_path() -> String:
	return _config_data.boot_settings.target_scene_path

func get_fail_fast() -> bool:
	return _config_data.boot_settings.fail_fast

func set_target_scene_path(path: String) -> void:
	_config_data.boot_settings.target_scene_path = path

func set_fail_fast(value: bool) -> void:
	_config_data.boot_settings.fail_fast = value

# ================================
# Backwards Compatibility Methods
# ================================

# For code that still references GameConstants style
func get_data_paths_dict() -> Dictionary:
	return _config_data.data_paths

func get_scene_paths_dict() -> Dictionary:
	return _config_data.scene_paths

func get_layer_constants_dict() -> Dictionary:
	return _config_data.layer_constants

# ================================
# Override Support (for future extensibility)
# ================================

func override_config(key_path: String, value: Variant) -> void:
	var keys = key_path.split(".")
	var current = _config_data
	
	for i in range(keys.size() - 1):
		var key = keys[i]
		if not current.has(key):
			current[key] = {}
		current = current[key]
	
	current[keys[-1]] = value

func load_config_overrides(file_path: String) -> bool:
	if not FileAccess.file_exists(file_path):
		return false
	
	var file = FileAccess.open(file_path, FileAccess.READ)
	if file == null:
		return false
	
	var json_text = file.get_as_text()
	var json = JSON.new()
	var parse_result = json.parse(json_text)
	
	if parse_result != OK:
		push_error("ConfigManager: Failed to parse config override file: %s" % file_path)
		return false
	
	var overrides = json.data
	if typeof(overrides) != TYPE_DICTIONARY:
		push_error("ConfigManager: Config override file must contain a dictionary")
		return false
	
	_merge_config_recursive(_config_data, overrides)
	return true

func _merge_config_recursive(target: Dictionary, source: Dictionary) -> void:
	for key in source:
		if target.has(key) and typeof(target[key]) == TYPE_DICTIONARY and typeof(source[key]) == TYPE_DICTIONARY:
			_merge_config_recursive(target[key], source[key])
		else:
			target[key] = source[key]