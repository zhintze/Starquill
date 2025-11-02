extends BaseMenu
class_name PauseMenu

# UI References - to be connected when scene is created
@onready var resume_button: Button = $VBoxContainer/ResumeButton if has_node("VBoxContainer/ResumeButton") else null
@onready var settings_button: Button = $VBoxContainer/SettingsButton if has_node("VBoxContainer/SettingsButton") else null
@onready var quit_button: Button = $VBoxContainer/QuitButton if has_node("VBoxContainer/QuitButton") else null

func _ready() -> void:
	super._ready()
	menu_name = &"pause_menu"

	# Connect buttons if they exist
	if resume_button:
		resume_button.pressed.connect(_on_resume_pressed)
	if settings_button:
		settings_button.pressed.connect(_on_settings_pressed)
	if quit_button:
		quit_button.pressed.connect(_on_quit_pressed)

func _on_resume_pressed() -> void:
	close()

func _on_settings_pressed() -> void:
	UIManager.open_settings_menu()

func _on_quit_pressed() -> void:
	# TODO: Show confirmation dialog
	# For now, directly quit to main menu
	get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
