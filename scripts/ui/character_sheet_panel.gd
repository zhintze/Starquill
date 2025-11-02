extends Panel
class_name CharacterSheetPanel

# UI References - to be connected when scene is created
@onready var character_display: CharacterDisplay = $VBoxContainer/CharacterDisplay if has_node("VBoxContainer/CharacterDisplay") else null
@onready var name_label: Label = $VBoxContainer/NameLabel if has_node("VBoxContainer/NameLabel") else null
@onready var stats_container: VBoxContainer = $VBoxContainer/StatsContainer if has_node("VBoxContainer/StatsContainer") else null

# Equipment slot UI references
@onready var head_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/HeadSlot if has_node("VBoxContainer/EquipmentGrid/HeadSlot") else null
@onready var torso_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/TorsoSlot if has_node("VBoxContainer/EquipmentGrid/TorsoSlot") else null
@onready var arms_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/ArmsSlot if has_node("VBoxContainer/EquipmentGrid/ArmsSlot") else null
@onready var legs_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/LegsSlot if has_node("VBoxContainer/EquipmentGrid/LegsSlot") else null
@onready var feet_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/FeetSlot if has_node("VBoxContainer/EquipmentGrid/FeetSlot") else null
@onready var main_hand_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/MainHandSlot if has_node("VBoxContainer/EquipmentGrid/MainHandSlot") else null
@onready var off_hand_slot: EquipmentSlotUI = $VBoxContainer/EquipmentGrid/OffHandSlot if has_node("VBoxContainer/EquipmentGrid/OffHandSlot") else null

# State
var character: Character = null
var inventory_controller: InventoryController

func _ready() -> void:
	# Initialize inventory controller with shared inventory
	var shared_inv: Inventory = null
	if PlayerData.has_method("get_shared_inventory"):
		shared_inv = PlayerData.get_shared_inventory()
	elif "shared_inventory" in PlayerData:
		shared_inv = PlayerData.shared_inventory

	if shared_inv:
		inventory_controller = InventoryController.new(shared_inv)

	# Connect equipment slot signals
	_connect_equipment_slots()

func set_character(p_character: Character) -> void:
	if character and character.model_changed.is_connected(_on_character_changed):
		character.model_changed.disconnect(_on_character_changed)

	character = p_character

	if character:
		character.model_changed.connect(_on_character_changed)
		refresh()

func refresh() -> void:
	if character == null:
		return

	# Update character display
	if character_display:
		character_display.set_character(character)

	# Update name
	if name_label:
		name_label.text = character.display_name

	# Update stats
	_refresh_stats()

	# Update equipment slots
	_refresh_equipment_slots()

func _refresh_stats() -> void:
	if not stats_container or not character:
		return

	# Clear existing stat labels
	for child in stats_container.get_children():
		child.queue_free()

	# Create stat labels for D&D stats
	var stat_names := {
		StatType.Type.STR: "Strength",
		StatType.Type.DEX: "Dexterity",
		StatType.Type.CON: "Constitution",
		StatType.Type.INT: "Intelligence",
		StatType.Type.WIS: "Wisdom",
		StatType.Type.CHA: "Charisma"
	}

	for stat_type in [StatType.Type.STR, StatType.Type.DEX, StatType.Type.CON,
					  StatType.Type.INT, StatType.Type.WIS, StatType.Type.CHA]:
		var label := Label.new()
		var stat_name := stat_names.get(stat_type, "Unknown")
		var stat_value := character.stats.get_stat(stat_type)
		label.text = "%s: %d" % [stat_name, stat_value]
		stats_container.add_child(label)

func _refresh_equipment_slots() -> void:
	if not character:
		return

	if head_slot:
		head_slot.set_equipment(character.head)
	if torso_slot:
		torso_slot.set_equipment(character.torso)
	if arms_slot:
		arms_slot.set_equipment(character.arms)
	if legs_slot:
		legs_slot.set_equipment(character.legs)
	if feet_slot:
		feet_slot.set_equipment(character.feet)
	if main_hand_slot:
		main_hand_slot.set_equipment(character.main_hand)
	if off_hand_slot:
		off_hand_slot.set_equipment(character.off_hand)

func _connect_equipment_slots() -> void:
	if head_slot:
		head_slot.unequip_requested.connect(_on_unequip_requested.bind("head"))
	if torso_slot:
		torso_slot.unequip_requested.connect(_on_unequip_requested.bind("torso"))
	if arms_slot:
		arms_slot.unequip_requested.connect(_on_unequip_requested.bind("arms"))
	if legs_slot:
		legs_slot.unequip_requested.connect(_on_unequip_requested.bind("legs"))
	if feet_slot:
		feet_slot.unequip_requested.connect(_on_unequip_requested.bind("feet"))
	if main_hand_slot:
		main_hand_slot.unequip_requested.connect(_on_unequip_requested.bind("main_hand"))
	if off_hand_slot:
		off_hand_slot.unequip_requested.connect(_on_unequip_requested.bind("off_hand"))

func _on_unequip_requested(slot: String) -> void:
	if not character or not inventory_controller:
		return

	inventory_controller.unequip_equipment(character, slot)

func _on_character_changed() -> void:
	refresh()
