extends Resource
class_name ItemStack

signal quantity_changed(old_quantity: int, new_quantity: int)
signal stack_depleted()

@export var item_data: ItemData = null
@export var quantity: int = 1

func _init(p_item_data: ItemData = null, p_quantity: int = 1) -> void:
	item_data = p_item_data
	quantity = p_quantity

func add(amount: int) -> int:
	if item_data == null or not item_data.is_stackable:
		return amount  # Return overflow

	var old_quantity := quantity
	var max_can_add := item_data.max_stack - quantity
	var actual_add := mini(amount, max_can_add)

	quantity += actual_add
	quantity_changed.emit(old_quantity, quantity)

	return amount - actual_add  # Return overflow

func remove(amount: int) -> int:
	var old_quantity := quantity
	var actual_remove := mini(amount, quantity)

	quantity -= actual_remove
	quantity_changed.emit(old_quantity, quantity)

	if quantity <= 0:
		stack_depleted.emit()

	return actual_remove

func can_merge_with(other: ItemStack) -> bool:
	if other == null or item_data == null or other.item_data == null:
		return false

	return item_data.id == other.item_data.id and item_data.is_stackable

func merge_with(other: ItemStack) -> int:
	if not can_merge_with(other):
		return other.quantity

	var overflow := add(other.quantity)
	other.quantity = overflow

	if overflow == 0:
		other.stack_depleted.emit()

	return overflow

func is_empty() -> bool:
	return quantity <= 0

func is_full() -> bool:
	return item_data != null and quantity >= item_data.max_stack
