extends Node

# Persistent singleton for player party and game state

var party: Party = null
var current_world_position: Vector2i = Vector2i.ZERO
var gold: int = 0
var game_time: float = 0.0
var inventory: Array[EquipmentInstance] = []

# Save/Load state
var current_save_slot: int = -1
var is_new_game: bool = true

func _ready():
	party = Party.new()
	party.name = "PlayerParty"

func initialize_new_game() -> void:
	party.members.clear()
	inventory.clear()
	gold = 100
	game_time = 0.0
	is_new_game = true
	BusParty.party_data_loaded.emit({})

func add_character(character: Character) -> bool:
	if party.members.size() >= WorldConstants.MAX_PARTY_SIZE:
		return false

	party.members.append(character)
	BusParty.member_added.emit(character, party.members.size() - 1)
	BusParty.party_size_changed.emit(party.members.size())
	return true

func remove_character(character: Character) -> void:
	party.members.erase(character)
	BusParty.member_removed.emit(character)
	BusParty.party_size_changed.emit(party.members.size())

func get_party_size() -> int:
	return party.members.size()

func get_party_leader() -> Character:
	if party.members.size() > 0:
		return party.members[0]
	return null

func set_world_position(pos: Vector2i) -> void:
	current_world_position = pos
	BusParty.current_position = pos

func add_to_inventory(equipment: EquipmentInstance) -> void:
	inventory.append(equipment)
	BusInventory.item_added.emit(equipment)

func remove_from_inventory(equipment: EquipmentInstance) -> void:
	inventory.erase(equipment)
	BusInventory.item_removed.emit(equipment)

func serialize() -> Dictionary:
	var member_data = []
	for member in party.members:
		member_data.append(member.serialize())

	var inventory_data = []
	for item in inventory:
		inventory_data.append(item.serialize())

	return {
		"party_members": member_data,
		"inventory": inventory_data,
		"world_position": {"x": current_world_position.x, "y": current_world_position.y},
		"gold": gold,
		"game_time": game_time,
		"save_slot": current_save_slot
	}

func deserialize(data: Dictionary) -> void:
	party.members.clear()
	inventory.clear()

	var member_data = data.get("party_members", [])
	for member_dict in member_data:
		var character = Character.new()
		character.deserialize(member_dict)
		party.members.append(character)

	var inventory_data = data.get("inventory", [])
	for item_dict in inventory_data:
		var equipment = EquipmentInstance.new()
		equipment.deserialize(item_dict)
		inventory.append(equipment)

	var pos = data.get("world_position", {"x": 0, "y": 0})
	current_world_position = Vector2i(pos["x"], pos["y"])

	gold = data.get("gold", 0)
	game_time = data.get("game_time", 0.0)
	current_save_slot = data.get("save_slot", -1)
	is_new_game = false

	BusParty.party_data_loaded.emit(data)
