extends Node
class_name InputHandler

# Mode-based input routing
# Attach this to main scene or use as autoload

func _input(event: InputEvent) -> void:
	# Check input mode from GameManager
	var input_mode := GameManager.get_input_mode()

	match input_mode:
		&"world":
			_handle_world_input(event)
		&"ui":
			_handle_ui_input(event)
		&"dialogue":
			_handle_dialogue_input(event)
		_:
			pass

func _handle_world_input(event: InputEvent) -> void:
	# World/gameplay input
	if event.is_action_pressed("ui_cancel"):
		UIManager.open_pause_menu()
		get_viewport().set_input_as_handled()
		return

	# TODO: Add keybinding for party menu once defined
	# if event.is_action_pressed("open_party_menu"):
	#     UIManager.open_party_menu()
	#     get_viewport().set_input_as_handled()
	#     return

	# Other world input is handled by world/camera controllers

func _handle_ui_input(event: InputEvent) -> void:
	# UI handles its own input via BaseMenu and UI components
	# This handler is mostly for global shortcuts that work in menus
	pass

func _handle_dialogue_input(event: InputEvent) -> void:
	# Dialogue system handles input
	# TODO: Implement when dialogue system is added
	pass
