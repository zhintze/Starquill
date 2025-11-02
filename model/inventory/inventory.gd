extends Resource
class_name Inventory

signal changed()
signal item_added(stack: ItemStack)
signal item_removed(stack: ItemStack)
signal capacity_changed(old_capacity: int, new_capacity: int)

@export var capacity: int = 24
var slots: Array[ItemStack] = []

func _init(p_capacity: int = 24) -> void:
	capacity = p_capacity
	slots.resize(capacity)

# Add item to inventory (smart stacking)
func add_item(item_data: ItemData, amount: int = 1) -> bool:
	if item_data == null or amount <= 0:
		return false

	var remaining := amount

	# Try to stack with existing items first
	if item_data.is_stackable:
		for stack in slots:
			if stack != null and stack.item_data.id == item_data.id and not stack.is_full():
				var added := stack.add(remaining)
				remaining = added
				if remaining == 0:
					changed.emit()
					return true

	# Create new stacks for remaining quantity
	while remaining > 0:
		var empty_slot_index := _find_empty_slot()
		if empty_slot_index == -1:
			return false  # Inventory full

		var stack_amount := mini(remaining, item_data.max_stack if item_data.is_stackable else 1)
		var new_stack := ItemStack.new(item_data, stack_amount)

		slots[empty_slot_index] = new_stack
		new_stack.stack_depleted.connect(_on_stack_depleted.bind(empty_slot_index))

		item_added.emit(new_stack)
		remaining -= stack_amount

	changed.emit()
	return true

# Remove item by ItemData
func remove_item(item_data: ItemData, amount: int = 1) -> int:
	if item_data == null or amount <= 0:
		return 0

	var remaining := amount

	for i in range(slots.size()):
		var stack := slots[i]
		if stack != null and stack.item_data.id == item_data.id:
			var removed := stack.remove(remaining)
			remaining -= removed

			if stack.is_empty():
				slots[i] = null
				item_removed.emit(stack)

			if remaining == 0:
				break

	if remaining < amount:
		changed.emit()

	return amount - remaining

# Remove item at specific slot index
func remove_at_slot(slot_index: int, amount: int = 1) -> int:
	if slot_index < 0 or slot_index >= slots.size():
		return 0

	var stack := slots[slot_index]
	if stack == null:
		return 0

	var removed := stack.remove(amount)

	if stack.is_empty():
		slots[slot_index] = null
		item_removed.emit(stack)

	changed.emit()
	return removed

# Get item count by ItemData
func get_item_count(item_data: ItemData) -> int:
	if item_data == null:
		return 0

	var total := 0
	for stack in slots:
		if stack != null and stack.item_data.id == item_data.id:
			total += stack.quantity

	return total

# Check if inventory has item
func has_item(item_data: ItemData, amount: int = 1) -> bool:
	return get_item_count(item_data) >= amount

# Get stack at slot index
func get_stack_at(slot_index: int) -> ItemStack:
	if slot_index < 0 or slot_index >= slots.size():
		return null
	return slots[slot_index]

# Find first empty slot
func _find_empty_slot() -> int:
	for i in range(slots.size()):
		if slots[i] == null:
			return i
	return -1

# Get number of empty slots
func get_empty_slot_count() -> int:
	var count := 0
	for stack in slots:
		if stack == null:
			count += 1
	return count

# Check if inventory is full
func is_full() -> bool:
	return get_empty_slot_count() == 0

# Clear inventory
func clear() -> void:
	slots.clear()
	slots.resize(capacity)
	changed.emit()

# Change capacity
func set_capacity(new_capacity: int) -> void:
	if new_capacity == capacity:
		return

	var old_capacity := capacity
	capacity = new_capacity
	slots.resize(capacity)
	capacity_changed.emit(old_capacity, new_capacity)
	changed.emit()

# Signal handler for depleted stacks
func _on_stack_depleted(slot_index: int) -> void:
	if slot_index >= 0 and slot_index < slots.size():
		var stack := slots[slot_index]
		slots[slot_index] = null
		if stack:
			item_removed.emit(stack)
		changed.emit()

# Serialization
func serialize() -> Dictionary:
	var slot_data := []
	for stack in slots:
		if stack != null and stack.item_data != null:
			slot_data.append({
				"item_id": stack.item_data.id,
				"quantity": stack.quantity
			})
		else:
			slot_data.append(null)

	return {
		"capacity": capacity,
		"slots": slot_data
	}

func deserialize(data: Dictionary) -> void:
	capacity = data.get("capacity", 24)
	slots.clear()
	slots.resize(capacity)

	var slot_data := data.get("slots", [])
	for i in range(mini(slot_data.size(), capacity)):
		var stack_data = slot_data[i]
		if stack_data != null and stack_data is Dictionary:
			var item_id := stack_data.get("item_id", &"")
			var quantity := stack_data.get("quantity", 1)

			# TODO: Load ItemData from registry (ItemRegistry.get_item(item_id))
			# For now, skip deserialization until ItemRegistry is implemented
			# var item_data := ItemRegistry.get_item(item_id)
			# if item_data:
			#     slots[i] = ItemStack.new(item_data, quantity)

	changed.emit()
