## Game.gd — thin façade for common flows
extends Node

func start_new_game(level_id: String, scene: PackedScene) -> void:
	SceneLoader.swap_to(level_id, scene)
	BusAudio.set_music("explore")

func go_to_next_level(_from_id: String, to_id: String, next_scene: PackedScene) -> void:
	SceneLoader.swap_to(to_id, next_scene)
