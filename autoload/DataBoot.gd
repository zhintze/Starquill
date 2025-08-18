# File: res://scripts/boot/DataBoot.gd
extends Node

# Point to your editable config. If null, built-in defaults are used.
@export var config: BootConfig

var _launched: bool = false

func _ready() -> void:
	# Load config or fall back to defaults
	if config == null:
		config = BootConfig.new()

	var ok_all := true
	ok_all = ok_all and _task_load_species()
	ok_all = ok_all and _task_verify_palette_hint()

	# Hook: place any additional boot tasks here now or later
	# ok_all = ok_all and _task_load_equipment()
	# ok_all = ok_all and _task_load_items()

	# If nothing loaded at all, shout loudly
	if species_loader.size() == 0:
		push_error("DataBoot: Species registry is empty after boot tasks. Check paths or JSON schema.")
		# You can choose to bail here or still launch target; up to you.

	_launch_target_scene(config.target_scene_path)

# -------------------------
# Boot Tasks (modular)
# -------------------------

func _task_load_species() -> bool:
	# Try JSONs (first hit wins)
	for i in config.species_json_candidates.size():
		var p: String = config.species_json_candidates[i]
		if FileAccess.file_exists(p):
			species_loader.clear()
			species_loader.load_from_json(p)
			_log_registry("Species from JSON", p)
			if species_loader.size() > 0:
				return true
			elif config.fail_fast:
				push_error("DataBoot: JSON '%s' parsed but no species added; stopping (fail_fast)." % p)
				return false

	# Try folders
	for j in config.species_dir_candidates.size():
		var dpath: String = config.species_dir_candidates[j]
		var da := DirAccess.open(dpath)
		if da != null:
			species_loader.clear()
			species_loader.load_from_dir(dpath)
			_log_registry("Species from DIR", dpath)
			if species_loader.size() > 0:
				return true
			elif config.fail_fast:
				push_error("DataBoot: DIR '%s' scanned but no species added; stopping (fail_fast)." % dpath)
				return false

	push_warning("DataBoot: No species sources found in any configured paths.")
	return not config.fail_fast

func _task_verify_palette_hint() -> bool:
	# Non-blocking sanity check: warn if no palette CSV is present.
	for i in config.palette_csv_candidates.size():
		var p: String = config.palette_csv_candidates[i]
		if FileAccess.file_exists(p):
			print("Palette CSV detected: ", p)
			return true
	push_warning("DataBoot: No palette CSV found in candidates; SpeciesLoader will use fallback defaults.")
	return true  # never blocks

# -------------------------
# Helpers
# -------------------------

# Defer the scene change out of _ready
func _launch_target_scene(scene_path: String) -> void:
	if _launched:
		return
	_launched = true

	var packed := load(scene_path)
	if not (packed is PackedScene):
		push_error("DataBoot: Could not load target scene: %s" % scene_path)
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
		push_warning("DataBoot: SceneTree not ready; retrying next frame…")
		call_deferred("_do_change_scene", packed)
		return

	# Optional: skip if we're already in the target scene
	if tree.current_scene and tree.current_scene.scene_file_path == packed.resource_path:
		return

	# Change scenes safely
	tree.change_scene_to_packed(packed)

func _log_registry(prefix: String, src: String) -> void:
	print("%s: %s | count=%d | ids=%s" % [
		prefix, src, species_loader.size(), str(species_loader.by_id.keys())
	])
