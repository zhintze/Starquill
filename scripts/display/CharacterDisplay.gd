extends Control
class_name CharacterDisplay

var _target: SpeciesDisplayable
var _character: Character
var _si: SpeciesInstance

func set_target(d: SpeciesDisplayable) -> void:
	_target = d
	print("CD.set_target:", _target)
	_redraw()

func set_character(c: Character) -> void:
	_character = c
	_redraw()

func set_species_instance(si: SpeciesInstance) -> void:
	_si = si
	_redraw()

func _redraw() -> void:
	# Example: name label for smoke test
	if has_node("%NameLabel"):
		var label := %NameLabel as Label
		if _target and _target.instance:
			label.text = String(_target.instance.species_id)
		elif _character and _character.species:
			label.text = String(_character.species.species_id)
		elif _si:
			label.text = String(_si.species_id)
	# TODO: call your real render path using SpeciesDisplayBuilder.build_display_pieces(...)
