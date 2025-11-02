extends Node
class_name InventoryController

# Reference to model
var inventory: Inventory

func _init(p_inventory: Inventory) -> void:
	inventory = p_inventory

# Use item on target
func use_item(item_data: ItemData, target: Character) -> bool:
	if not _can_use_item(item_data):
		return false

	# Execute item effect
	if item_data.effect_script:
		var effect = item_data.effect_script.new()
		if effect.has_method("apply"):
			effect.apply(target, item_data)

	# Apply stat effects for consumables
	if not item_data.stat_effects.is_empty():
		for stat_type in item_data.stat_effects:
			var amount: int = item_data.stat_effects[stat_type]
			target.stats.add_stat(stat_type, amount)

	# Remove one from inventory
	inventory.remove_item(item_data, 1)

	# Emit event
	EventBus.item_used.emit(item_data, target)

	return true

# Equip equipment to character
func equip_equipment(equipment: EquipmentInstance, character: Character) -> bool:
	if character == null or equipment == null:
		return false

	# Get slot for this equipment type
	var slot := StarquillData.get_slot_for_item_type(equipment.item_type)

	# Get slot enum value
	var slot_enum := _get_slot_enum(slot)
	if slot_enum == -1:
		return false

	# Get current equipment in that slot
	var old_equipment := character.get_equipment(slot_enum)

	# Equip new item
	var success := character.equip_instance(equipment)

	if success:
		# Remove from inventory (equipment instances stored in PlayerData)
		PlayerData.remove_from_inventory(equipment)

		# Add old equipment back to inventory if it existed
		if old_equipment != null:
			PlayerData.add_to_inventory(old_equipment)

		EventBus.equipment_equipped.emit(character, slot, equipment)

	return success

# Unequip equipment from character
func unequip_equipment(character: Character, slot: String) -> bool:
	if character == null:
		return false

	# Get slot enum value
	var slot_enum := _get_slot_enum(slot)
	if slot_enum == -1:
		return false

	var equipment := character.get_equipment(slot_enum)
	if equipment == null:
		return false

	# Add to inventory
	PlayerData.add_to_inventory(equipment)

	# Unequip from character
	character.unequip(slot_enum)

	EventBus.equipment_unequipped.emit(character, slot, equipment)

	return true

# Sort inventory by criteria
func sort_inventory(sort_type: StringName) -> void:
	match sort_type:
		&"name":
			_sort_by_name()
		&"type":
			_sort_by_type()
		&"value":
			_sort_by_value()
		_:
			push_warning("Unknown sort type: %s" % sort_type)

# Private helpers
func _can_use_item(item_data: ItemData) -> bool:
	if item_data == null or not item_data.is_usable:
		return false

	var context := GameManager.get_mode()
	return item_data.can_use_in_context(context)

func _get_slot_enum(slot: String) -> int:
	match slot:
		"head":
			return Character.EquipSlot.HEAD
		"torso":
			return Character.EquipSlot.TORSO
		"arms":
			return Character.EquipSlot.ARMS
		"legs":
			return Character.EquipSlot.LEGS
		"feet":
			return Character.EquipSlot.FEET
		"main_hand":
			return Character.EquipSlot.MAIN_HAND
		"off_hand":
			return Character.EquipSlot.OFF_HAND
		"misc1":
			return Character.EquipSlot.MISC1
		"misc2":
			return Character.EquipSlot.MISC2
		"misc3":
			return Character.EquipSlot.MISC3
		"misc4":
			return Character.EquipSlot.MISC4
		_:
			return -1

func _sort_by_name() -> void:
	# Get all non-null stacks
	var stacks: Array[ItemStack] = []
	for stack in inventory.slots:
		if stack != null:
			stacks.append(stack)

	# Sort by display name
	stacks.sort_custom(func(a: ItemStack, b: ItemStack) -> bool:
		return a.item_data.display_name < b.item_data.display_name
	)

	# Clear and refill slots
	inventory.slots.clear()
	inventory.slots.resize(inventory.capacity)
	for i in range(stacks.size()):
		inventory.slots[i] = stacks[i]

	inventory.changed.emit()

func _sort_by_type() -> void:
	# Get all non-null stacks
	var stacks: Array[ItemStack] = []
	for stack in inventory.slots:
		if stack != null:
			stacks.append(stack)

	# Sort by type, then name
	stacks.sort_custom(func(a: ItemStack, b: ItemStack) -> bool:
		if a.item_data.item_type != b.item_data.item_type:
			return a.item_data.item_type < b.item_data.item_type
		return a.item_data.display_name < b.item_data.display_name
	)

	# Clear and refill slots
	inventory.slots.clear()
	inventory.slots.resize(inventory.capacity)
	for i in range(stacks.size()):
		inventory.slots[i] = stacks[i]

	inventory.changed.emit()

func _sort_by_value() -> void:
	# Get all non-null stacks
	var stacks: Array[ItemStack] = []
	for stack in inventory.slots:
		if stack != null:
			stacks.append(stack)

	# Sort by value (descending), then name
	stacks.sort_custom(func(a: ItemStack, b: ItemStack) -> bool:
		if a.item_data.sell_value != b.item_data.sell_value:
			return a.item_data.sell_value > b.item_data.sell_value
		return a.item_data.display_name < b.item_data.display_name
	)

	# Clear and refill slots
	inventory.slots.clear()
	inventory.slots.resize(inventory.capacity)
	for i in range(stacks.size()):
		inventory.slots[i] = stacks[i]

	inventory.changed.emit()
