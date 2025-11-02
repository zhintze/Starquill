## scene_loader.gd
## Keeps the currently active level’s root; 
## removes/queues free, then instantiates new scene; 
## emits level_will_change, level_unloaded, level_loaded.
extends Node

var _current_level_id: String = ""
var _current_root: Node = null

@export var level_holder_path: NodePath = ^"/root/Main/LevelHolder"

func boot_done() -> void:
	EventBus.boot_completed.emit()

func swap_to(level_id: String, packed: PackedScene) -> void:
	var holder: Node = get_node_or_null(level_holder_path)
	if holder == null:
		push_error("SceneLoader: LevelHolder not found at %s" % level_holder_path)
		return

	var from_id := _current_level_id
	if from_id != "":
		EventBus.level_will_change.emit(from_id, level_id)
	else:
		EventBus.level_will_change.emit("", level_id)

	if is_instance_valid(_current_root):
		holder.remove_child(_current_root)
		_current_root.queue_free()
		EventBus.level_unloaded.emit(from_id)

	_current_root = packed.instantiate()
	_current_level_id = level_id
	holder.add_child(_current_root)
	EventBus.level_loaded.emit(level_id, _current_root)
