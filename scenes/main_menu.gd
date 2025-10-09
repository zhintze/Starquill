extends Control

# Simple main menu to switch between different test scenes

func _ready():
	print("Main Menu Ready")
	_create_menu()

func _create_menu() -> void:
	# Create vertical container
	var vbox = VBoxContainer.new()
	vbox.name = "MenuContainer"
	vbox.set_anchors_preset(Control.PRESET_CENTER)
	vbox.position = Vector2(-100, -100)
	add_child(vbox)

	# Title
	var title = Label.new()
	title.text = "Starquill Test Scenes"
	title.add_theme_font_size_override("font_size", 24)
	vbox.add_child(title)

	# Add some spacing
	vbox.add_child(HSeparator.new())

	# Character Randomizer button
	var char_button = Button.new()
	char_button.text = "Character Randomizer"
	char_button.pressed.connect(_on_character_pressed)
	vbox.add_child(char_button)

	# World Test button
	var world_button = Button.new()
	world_button.text = "World Map Test"
	world_button.pressed.connect(_on_world_pressed)
	vbox.add_child(world_button)

	# Exit button
	var exit_button = Button.new()
	exit_button.text = "Exit"
	exit_button.pressed.connect(_on_exit_pressed)
	vbox.add_child(exit_button)

func _on_character_pressed() -> void:
	print("Loading Character Randomizer...")
	get_tree().change_scene_to_file("res://scenes/CharacterRandomizer.tscn")

func _on_world_pressed() -> void:
	print("Loading World Test Scene...")
	get_tree().change_scene_to_file("res://scenes/world/world_test_scene.tscn")

func _on_exit_pressed() -> void:
	get_tree().quit()