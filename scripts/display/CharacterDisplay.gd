extends Control
class_name CharacterDisplay

# Allow overriding in the editor if the hierarchy changes.
@export var name_label_path: NodePath = ^"NameLabel"

var name_label: Label

var _target: SpeciesDisplayable
var _character: Character
var _si: SpeciesInstance

func _ready() -> void:
	_resolve_name_label()
	if name_label:
		name_label.text = "Test Tile"  # sanity check
	else:
		# Hard stop so we notice the setup issue fast
		push_error("CharacterDisplay: Label not found. Set 'name_label_path' or add a child named 'NameLabel'.")
		return
	_redraw()

func _resolve_name_label() -> void:
	if name_label and is_instance_valid(name_label):
		return
	# First: try the provided path
	if name_label_path != NodePath():
		name_label = get_node_or_null(name_label_path) as Label
		if name_label:
			return
	# Second: try a recursive search by name (works if it's under Panel, etc.)
	name_label = find_child("NameLabel", true, false) as Label

func set_target(d: SpeciesDisplayable) -> void:
	_target = d
	_schedule_redraw()

func set_character(c: Character) -> void:
	_character = c
	_schedule_redraw()

func set_species_instance(si: SpeciesInstance) -> void:
	_si = si
	_schedule_redraw()

func _schedule_redraw() -> void:
	if is_node_ready():
		call_deferred("_redraw")  # ensure @onready + children resolved
	else:
		call_deferred("_redraw")

func _redraw() -> void:
	_resolve_name_label()
	if not name_label:
		return

	if _target and _target.instance:
		name_label.text = str(_target.instance.species_id)
	elif _character and _character.species:
		name_label.text = str(_character.species.species_id)
	elif _si:
		name_label.text = str(_si.species_id)
