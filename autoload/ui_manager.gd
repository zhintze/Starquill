extends CanvasLayer
class_name UIManager

# Menu stack for modal management
var _menu_stack: Array[Control] = []

# Menu scene references (will be set when scenes are created)
var party_menu_scene: PackedScene = null
var pause_menu_scene: PackedScene = null
var settings_menu_scene: PackedScene = null

func _ready() -> void:
	layer = 100  # Render above game world

	# Menu scenes will be loaded when they are created
	# party_menu_scene = preload("res://scenes/ui/party_menu.tscn")
	# pause_menu_scene = preload("res://scenes/ui/pause_menu.tscn")
	# settings_menu_scene = preload("res://scenes/ui/settings_menu.tscn")

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		if not _menu_stack.is_empty():
			close_top_menu()
			get_viewport().set_input_as_handled()

# Open a menu from scene
func open_menu(menu_scene: PackedScene) -> Control:
	if menu_scene == null:
		push_error("UIManager: Cannot open null menu scene")
		return null

	var menu := menu_scene.instantiate()
	if menu == null:
		push_error("UIManager: Failed to instantiate menu scene")
		return null

	add_child(menu)
	_menu_stack.push_back(menu)

	# Connect signals if menu has them
	if menu.has_signal("opened"):
		menu.opened.connect(_on_menu_opened.bind(menu))
	if menu.has_signal("closed"):
		menu.closed.connect(_on_menu_closed.bind(menu))

	# Call open method if menu has it
	if menu.has_method("open"):
		menu.open()
	else:
		menu.show()

	GameManager.set_mode(&"menu")

	# Get menu name for event
	var menu_name: StringName = &"unknown"
	if menu.has_method("get_menu_name"):
		menu_name = menu.get_menu_name()
	elif "menu_name" in menu:
		menu_name = menu.menu_name

	EventBus.modal_opened.emit(menu_name)

	return menu

# Close the top menu
func close_top_menu() -> void:
	if _menu_stack.is_empty():
		return

	var top_menu := _menu_stack.pop_back()

	# Call close method if menu has it
	if top_menu.has_method("close"):
		top_menu.close()
	else:
		top_menu.queue_free()

# Close all menus
func close_all_menus() -> void:
	while not _menu_stack.is_empty():
		close_top_menu()

# Get the currently active menu
func get_active_menu() -> Control:
	if _menu_stack.is_empty():
		return null
	return _menu_stack[-1]

# Check if any menu is open
func is_menu_open() -> bool:
	return not _menu_stack.is_empty()

# Convenience methods for specific menus (will work once scenes are created)
func open_party_menu() -> Control:
	if party_menu_scene == null:
		push_warning("UIManager: Party menu scene not loaded")
		return null
	return open_menu(party_menu_scene)

func open_pause_menu() -> Control:
	if pause_menu_scene == null:
		push_warning("UIManager: Pause menu scene not loaded")
		return null
	return open_menu(pause_menu_scene)

func open_settings_menu() -> Control:
	if settings_menu_scene == null:
		push_warning("UIManager: Settings menu scene not loaded")
		return null
	return open_menu(settings_menu_scene)

# Signal handlers
func _on_menu_opened(menu: Control) -> void:
	var menu_name := "unknown"
	if "menu_name" in menu:
		menu_name = str(menu.menu_name)
	print("UIManager: Menu opened - %s" % menu_name)

func _on_menu_closed(menu: Control) -> void:
	var menu_name := "unknown"
	if "menu_name" in menu:
		menu_name = str(menu.menu_name)
	print("UIManager: Menu closed - %s" % menu_name)

	menu.queue_free()

	EventBus.modal_closed.emit(StringName(menu_name))

	# Restore gameplay mode if no menus remain
	if _menu_stack.is_empty():
		GameManager.set_mode(&"gameplay")
