## game.gd
## Entry points start_new_game() and go_to_next_level(),
## delegating to SceneLoader and audio bus
extends Node

func start_new_game(level_id: String, scene: PackedScene) -> void:
	PlayerData.initialize_new_game()
	_create_test_party()
	SceneLoader.swap_to(level_id, scene)
	EventBus.music_changed.emit("explore")

func _create_test_party() -> void:
	for i in range(4):
		var species_instance = StarquillData.create_random_species_instance()
		var character = CharacterFactory.create_from_species_instance(species_instance)
		character.display_name = "Hero %d" % (i + 1)

		equipment_factory.equip_random_set(character)

		PlayerData.add_character(character)

func go_to_next_level(_from_id: String, to_id: String, next_scene: PackedScene) -> void:
	SceneLoader.swap_to(to_id, next_scene)
