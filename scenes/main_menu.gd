extends Control

## Scene Select Menu
## Simple test scene selector using the UI component system

func _ready() -> void:
	print("Main Menu Ready")
	_create_menu()

func _create_menu() -> void:
	# Full screen layout
	set_anchors_preset(Control.PRESET_FULL_RECT)
	anchor_right = 1.0
	anchor_bottom = 1.0
	offset_left = 0
	offset_top = 0
	offset_right = 0
	offset_bottom = 0

	var theme := UIThemeManager.get_theme()

	# Main panel with paper styling - fullscreen
	var panel := BasePanel.create(BasePanel.PanelStyle.PRIMARY, true)
	panel.name = "MenuPanel"
	panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	panel.anchor_right = 1.0
	panel.anchor_bottom = 1.0
	panel.offset_left = 0
	panel.offset_top = 0
	panel.offset_right = 0
	panel.offset_bottom = 0
	add_child(panel)

	# Center container for content
	var center := CenterContainer.new()
	center.name = "CenterContainer"
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	panel.add_child(center)

	# Main vertical layout centered in the panel
	var vbox := VBoxContainer.new()
	vbox.name = "MainVBox"
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	vbox.add_theme_constant_override("separation", theme.spacing_large)
	center.add_child(vbox)

	# Title
	var title := Label.new()
	title.name = "TitleLabel"
	title.text = "Starquill Test Scenes"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", theme.font_size_header + 8)
	title.add_theme_color_override("font_color", theme.text_color)
	vbox.add_child(title)

	# Spacer
	var spacer := Control.new()
	spacer.custom_minimum_size.y = theme.spacing_large
	vbox.add_child(spacer)

	# Button container
	var button_container := VBoxContainer.new()
	button_container.name = "ButtonContainer"
	button_container.alignment = BoxContainer.ALIGNMENT_CENTER
	button_container.add_theme_constant_override("separation", theme.spacing_medium)
	vbox.add_child(button_container)

	# Character Randomizer button
	var char_button := ThemedButton.new()
	char_button.text = "Character Randomizer"
	char_button.button_style = ThemedButton.ButtonStyle.PRIMARY
	char_button.custom_minimum_size = Vector2(UIConstants.MENU_BUTTON_WIDTH, UIConstants.MENU_BUTTON_HEIGHT)
	char_button.pressed.connect(_on_character_pressed)
	button_container.add_child(char_button)

	# World Test button
	var world_button := ThemedButton.new()
	world_button.text = "World Map Test"
	world_button.button_style = ThemedButton.ButtonStyle.PRIMARY
	world_button.custom_minimum_size = Vector2(UIConstants.MENU_BUTTON_WIDTH, UIConstants.MENU_BUTTON_HEIGHT)
	world_button.pressed.connect(_on_world_pressed)
	button_container.add_child(world_button)

	# Extra spacing before exit
	var exit_spacer := Control.new()
	exit_spacer.custom_minimum_size.y = theme.spacing_small
	button_container.add_child(exit_spacer)

	# Exit button
	var exit_button := ThemedButton.new()
	exit_button.text = "Exit"
	exit_button.button_style = ThemedButton.ButtonStyle.SECONDARY
	exit_button.custom_minimum_size = Vector2(UIConstants.MENU_BUTTON_WIDTH, UIConstants.MENU_BUTTON_HEIGHT)
	exit_button.pressed.connect(_on_exit_pressed)
	button_container.add_child(exit_button)

func _on_character_pressed() -> void:
	print("Loading Character Randomizer...")
	get_tree().change_scene_to_file("res://scenes/CharacterRandomizer.tscn")

func _on_world_pressed() -> void:
	print("Loading World Test Scene...")
	var error = get_tree().change_scene_to_file("res://scenes/world/world_test_scene.tscn")
	if error != OK:
		print("ERROR: Failed to load world scene! Error code: ", error)
		push_error("Failed to change to world scene: " + str(error))

func _on_exit_pressed() -> void:
	get_tree().quit()
