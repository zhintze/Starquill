## BusAudio.gd — request-only; playback handled by Audio service
extends Node

signal sfx_request(tag: String, args: Dictionary)  # {"stream": AudioStream, "pos": Vector2, "volume": float}
signal music_request(state: String)                 # "combat", "explore", "menu"

# Helpers
func play_sfx(tag: String, args: Dictionary = {}) -> void:
	sfx_request.emit(tag, args)

func set_music(state: String) -> void:
	music_request.emit(state)
