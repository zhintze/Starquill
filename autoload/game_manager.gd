extends Node
class_name GameManager

# Signals
signal scene_changed(new_scene: StringName)
signal paused_changed(is_paused: bool)
signal mode_changed(old_mode: StringName, new_mode: StringName)
signal input_mode_changed(new_input_mode: StringName)

# State
var _current_scene: StringName = &""
var _is_paused: bool = false
var _mode: StringName = &"gameplay"  # gameplay, menu, dialogue, combat
var _input_mode: StringName = &"world"  # world, ui, dialogue

# Scene management
func set_scene(scene_name: StringName) -> void:
	if _current_scene == scene_name:
		return
	var old_scene = _current_scene
	_current_scene = scene_name
	scene_changed.emit(scene_name)

func get_current_scene() -> StringName:
	return _current_scene

# Pause management
func set_paused(value: bool) -> void:
	if _is_paused == value:
		return
	_is_paused = value
	get_tree().paused = value
	paused_changed.emit(value)

func is_paused() -> bool:
	return _is_paused

# Mode management
func set_mode(value: StringName) -> void:
	if _mode == value:
		return
	var old_mode = _mode
	_mode = value
	mode_changed.emit(old_mode, value)

	# Auto-pause for menu mode
	if value == &"menu":
		set_paused(true)
	elif old_mode == &"menu" and value == &"gameplay":
		set_paused(false)

func get_mode() -> StringName:
	return _mode

# Input mode management
func set_input_mode(value: StringName) -> void:
	if _input_mode == value:
		return
	_input_mode = value
	input_mode_changed.emit(value)

func get_input_mode() -> StringName:
	return _input_mode

# Convenience checks
func is_in_gameplay() -> bool:
	return _mode == &"gameplay"

func is_in_menu() -> bool:
	return _mode == &"menu"

func is_in_dialogue() -> bool:
	return _mode == &"dialogue"

func is_in_combat() -> bool:
	return _mode == &"combat"
