extends Window
class_name RandomizationSettingsWin95

signal applied
signal canceled

@onready var tabs: TabContainer = $Root/Tabs

# Facial
@onready var facial_hair_spin: SpinBox = $Root/Tabs/Facial/FacialMargin/FacialGroup/FacialVBox/FacialHairRow/FacialHairSpin
@onready var facial_detail_spin: SpinBox = $Root/Tabs/Facial/FacialMargin/FacialGroup/FacialVBox/FacialDetailRow/FacialDetailSpin

# Equipment
@onready var head_spin: SpinBox = $Root/Tabs/Equipment/EquipMargin/EquipGroup/EquipVBox/HeadRow/HeadSpin
@onready var torso_spin: SpinBox = $Root/Tabs/Equipment/EquipMargin/EquipGroup/EquipVBox/TorsoRow/TorsoSpin
@onready var arms_spin: SpinBox = $Root/Tabs/Equipment/EquipMargin/EquipGroup/EquipVBox/ArmsRow/ArmsSpin
@onready var legs_spin: SpinBox = $Root/Tabs/Equipment/EquipMargin/EquipGroup/EquipVBox/LegsRow/LegsSpin
@onready var feet_spin: SpinBox = $Root/Tabs/Equipment/EquipMargin/EquipGroup/EquipVBox/FeetRow/FeetSpin
@onready var misc_spin: SpinBox = $Root/Tabs/Equipment/EquipMargin/EquipGroup/EquipVBox/MiscRow/MiscSpin

# Profiles
@onready var profile_select: OptionButton = $Root/Tabs/Profiles/ProfilesMargin/ProfilesVBox/ProfilesRow/ProfileSelect
@onready var preset_name: LineEdit = $Root/Tabs/Profiles/ProfilesMargin/ProfilesVBox/SaveRow/PresetName
@onready var delete_btn: Button = $Root/Tabs/Profiles/ProfilesMargin/ProfilesVBox/ProfilesRow/DeletePreset

# Footer
@onready var reset_btn: Button = $Root/Footer/ResetBtn
@onready var apply_btn: Button = $Root/Footer/ApplyBtn
## OK button removed per UX update

const PROFILE_FILE := "user://randomization_settings.json"

func _ready() -> void:
	_populate_from_data()
	_wire_controls()
	_setup_profiles()

func _populate_from_data() -> void:
	facial_hair_spin.value = StarquillData.get_facial_hair_chance() * 100.0
	facial_detail_spin.value = StarquillData.get_facial_detail_chance() * 100.0

	head_spin.value = StarquillData.get_equipment_prefix_chance("hd") * 100.0
	torso_spin.value = StarquillData.get_equipment_prefix_chance("tr") * 100.0
	arms_spin.value = StarquillData.get_equipment_prefix_chance("ar") * 100.0
	legs_spin.value = StarquillData.get_equipment_prefix_chance("lg") * 100.0
	feet_spin.value = StarquillData.get_equipment_prefix_chance("fe") * 100.0
	misc_spin.value = StarquillData.get_equipment_prefix_chance("mc") * 100.0

	_try_load_user_profile()

func _wire_controls() -> void:
	apply_btn.pressed.connect(_on_apply)
	reset_btn.pressed.connect(_reset_defaults)
	close_requested.connect(func(): hide())

## Sliders removed: spin boxes are now the only inputs

func _setup_profiles() -> void:
	profile_select.clear()
	_refresh_preset_list()
	var apply_btn_node = $Root/Tabs/Profiles/ProfilesMargin/ProfilesVBox/ProfilesRow/ProfileApply as Button
	apply_btn_node.pressed.connect(_apply_selected_preset)
	var delete_btn_node = $Root/Tabs/Profiles/ProfilesMargin/ProfilesVBox/ProfilesRow/DeletePreset as Button
	delete_btn_node.pressed.connect(_on_delete_preset_pressed)
	var save_btn_node = $Root/Tabs/Profiles/ProfilesMargin/ProfilesVBox/SaveRow/SavePreset as Button
	save_btn_node.pressed.connect(_on_save_preset_pressed)
	var open_btn_node = $Root/Tabs/Profiles/ProfilesMargin/ProfilesVBox/SaveRow/OpenFolder as Button
	open_btn_node.pressed.connect(_open_preset_folder)

	visibility_changed.connect(func(): if visible: _populate_from_data())
	profile_select.item_selected.connect(_update_delete_enabled)
	_update_delete_enabled(profile_select.get_selected())

func _apply_selected_preset() -> void:
	var idx := profile_select.get_selected()
	if idx < 0 or idx >= _saved_presets.size():
		push_warning("No preset selected or list is empty.")
		return
	var path: String = _saved_presets[idx]
	_load_preset_from_file(path)
	# After loading into controls, immediately apply values
	_on_apply()

func _apply_profile(p: Dictionary) -> void:
	facial_hair_spin.value = float(p.get("facial_hair", facial_hair_spin.value / 100.0)) * 100.0
	facial_detail_spin.value = float(p.get("facial_detail", facial_detail_spin.value / 100.0)) * 100.0
	head_spin.value = float(p.get("hd", head_spin.value / 100.0)) * 100.0
	torso_spin.value = float(p.get("tr", torso_spin.value / 100.0)) * 100.0
	arms_spin.value = float(p.get("ar", arms_spin.value / 100.0)) * 100.0
	legs_spin.value = float(p.get("lg", legs_spin.value / 100.0)) * 100.0
	feet_spin.value = float(p.get("fe", feet_spin.value / 100.0)) * 100.0
	misc_spin.value = float(p.get("mc", misc_spin.value / 100.0)) * 100.0

const PRESET_DIR := "user://randomization_presets"
var _saved_presets: Array[String] = [] # file paths 1:1 with OptionButton items
## Preset location notes (platform examples):
## - Linux: ~/.local/share/godot/app_userdata/Starquill/randomization_presets
##   (User example: /home/keroppi/.local/share/godot/app_userdata/Starquill/randomization_presets/)
## - Windows: %APPDATA%/Godot/app_userdata/Starquill/randomization_presets
## - macOS: ~/Library/Application Support/Godot/app_userdata/Starquill/randomization_presets

func _on_save_preset_pressed() -> void:
	var name := preset_name.text.strip_edges()
	if name == "":
		name = Time.get_datetime_string_from_system().replace(":", "-")
	var safe := _sanitize_name(name)
	var path := PRESET_DIR + "/" + safe + ".json"
	_ensure_preset_dir()
	var data = {
		"facial_hair": facial_hair_spin.value / 100.0,
		"facial_detail": facial_detail_spin.value / 100.0,
		"hd": head_spin.value / 100.0,
		"tr": torso_spin.value / 100.0,
		"ar": arms_spin.value / 100.0,
		"lg": legs_spin.value / 100.0,
		"fe": feet_spin.value / 100.0,
		"mc": misc_spin.value / 100.0
	}
	var f = FileAccess.open(path, FileAccess.WRITE)
	if f:
		f.store_string(JSON.stringify(data, "\t"))
		_refresh_preset_list()
		_select_saved_preset_by_path(path)
		print("[Presets] Saved to: ", ProjectSettings.globalize_path(path))

func _refresh_preset_list() -> void:
	_ensure_preset_dir()
	profile_select.clear()
	_saved_presets.clear()
	var dir := DirAccess.open(PRESET_DIR)
	if dir:
		dir.list_dir_begin()
		var file := dir.get_next()
		while file != "":
			if not dir.current_is_dir() and file.ends_with(".json"):
				var path := PRESET_DIR + "/" + file
				_saved_presets.append(path)
				var display := file.substr(0, file.length() - 5)
				profile_select.add_item(display)
			file = dir.get_next()
		dir.list_dir_end()
	if profile_select.item_count > 0:
		profile_select.select(0)
	_update_delete_enabled(profile_select.get_selected())

func _select_saved_preset_by_path(path: String) -> void:
	var idx := _saved_presets.find(path)
	if idx >= 0:
		profile_select.select(idx)
		_update_delete_enabled(idx)

func _on_delete_preset_pressed() -> void:
	var idx := profile_select.get_selected()
	if idx >= 0 and idx < _saved_presets.size():
		var path: String = _saved_presets[idx]
		if DirAccess.remove_absolute(path) == OK:
			print("[Presets] Deleted: ", ProjectSettings.globalize_path(path))
			_refresh_preset_list()
			if profile_select.item_count > 0:
				profile_select.select(0)
		else:
			push_warning("Failed to delete preset: " + path)

func _update_delete_enabled(idx: int) -> void:
	if delete_btn:
		delete_btn.disabled = (profile_select.item_count == 0) or idx < 0 or idx >= _saved_presets.size()

func _load_preset_from_file(path: String) -> void:
	if not FileAccess.file_exists(path):
		return
	var f = FileAccess.open(path, FileAccess.READ)
	if f == null:
		return
	var text = f.get_as_text()
	var json = JSON.new()
	if json.parse(text) == OK:
		var d = json.get_data()
		if d is Dictionary:
			_apply_profile(d)

func _ensure_preset_dir() -> void:
	if not DirAccess.dir_exists_absolute(PRESET_DIR):
		DirAccess.make_dir_recursive_absolute(PRESET_DIR)

func _open_preset_folder() -> void:
	_ensure_preset_dir()
	var abs_path := ProjectSettings.globalize_path(PRESET_DIR)
	OS.shell_open(abs_path)

func _sanitize_name(name: String) -> String:
	var s := name.strip_edges()
	var banned := ["/", "\\", ":", "*", "?", "\"", "<", ">", "|"]
	for ch in banned:
		s = s.replace(ch, "-")
	if s == "":
		s = "preset"
	return s

func _on_apply() -> void:
	StarquillData.set_facial_hair_chance(float(facial_hair_spin.value) / 100.0)
	StarquillData.set_facial_detail_chance(float(facial_detail_spin.value) / 100.0)
	StarquillData.set_equipment_prefix_chance("hd", float(head_spin.value) / 100.0)
	StarquillData.set_equipment_prefix_chance("tr", float(torso_spin.value) / 100.0)
	StarquillData.set_equipment_prefix_chance("ar", float(arms_spin.value) / 100.0)
	StarquillData.set_equipment_prefix_chance("lg", float(legs_spin.value) / 100.0)
	StarquillData.set_equipment_prefix_chance("fe", float(feet_spin.value) / 100.0)
	StarquillData.set_equipment_prefix_chance("mc", float(misc_spin.value) / 100.0)
	_save_user_profile()
	emit_signal("applied")

func _reset_defaults() -> void:
	var defaults = {
		"facial_hair": 0.7,
		"facial_detail": 0.8,
		"hd": 0.9, "tr": 1.0, "ar": 0.8, "lg": 1.0, "fe": 0.7, "mc": 0.3
	}
	_apply_profile(defaults)

func _try_load_user_profile() -> void:
	if not FileAccess.file_exists(PROFILE_FILE):
		return
	var f = FileAccess.open(PROFILE_FILE, FileAccess.READ)
	if f == null:
		return
	var text = f.get_as_text()
	var json = JSON.new()
	if json.parse(text) == OK:
		var d = json.get_data()
		if d is Dictionary:
			_apply_profile(d)

func _save_user_profile() -> void:
	var data = {
		"facial_hair": float(facial_hair_spin.value) / 100.0,
		"facial_detail": float(facial_detail_spin.value) / 100.0,
		"hd": float(head_spin.value) / 100.0,
		"tr": float(torso_spin.value) / 100.0,
		"ar": float(arms_spin.value) / 100.0,
		"lg": float(legs_spin.value) / 100.0,
		"fe": float(feet_spin.value) / 100.0,
		"mc": float(misc_spin.value) / 100.0
	}
	var f = FileAccess.open(PROFILE_FILE, FileAccess.WRITE)
	if f:
		f.store_string(JSON.stringify(data))
