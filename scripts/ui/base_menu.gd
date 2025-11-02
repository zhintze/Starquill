extends Control
class_name BaseMenu

signal opened()
signal closed()
signal focus_requested()

@export var menu_name: StringName = &"base_menu"
@export var close_on_escape: bool = true
@export var pause_on_open: bool = true

var is_open: bool = false

func _ready() -> void:
	visibility_changed.connect(_on_visibility_changed)
	hide()

# Template method pattern - override in subclasses
func open() -> void:
	if is_open:
		return

	is_open = true
	show()
	_on_open()
	opened.emit()

	if pause_on_open:
		GameManager.set_paused(true)

	# Focus first focusable control
	call_deferred("_grab_focus")

# Template method pattern - override in subclasses
func close() -> void:
	if not is_open:
		return

	is_open = false
	_on_close()
	closed.emit()

	hide()

# Override in subclasses for custom open behavior
func _on_open() -> void:
	pass

# Override in subclasses for custom close behavior
func _on_close() -> void:
	pass

# Find and focus first focusable control
func _grab_focus() -> void:
	var focusable := _find_first_focusable(self)
	if focusable:
		focusable.grab_focus()

# Recursively find first focusable control
func _find_first_focusable(node: Node) -> Control:
	if node is Control and node.focus_mode != Control.FOCUS_NONE:
		return node as Control

	for child in node.get_children():
		var result := _find_first_focusable(child)
		if result:
			return result

	return null

func _on_visibility_changed() -> void:
	if visible:
		focus_requested.emit()

func _input(event: InputEvent) -> void:
	if not is_open:
		return

	if close_on_escape and event.is_action_pressed("ui_cancel"):
		close()
		get_viewport().set_input_as_handled()

# Utility method for getting menu name
func get_menu_name() -> StringName:
	return menu_name
