extends Panel
class_name ItemSlotUI

signal slot_clicked()
signal slot_hovered()
signal slot_unhovered()

# UI References - to be connected when scene is created
@onready var icon_texture: TextureRect = $TextureRect if has_node("TextureRect") else null
@onready var quantity_label: Label = $QuantityLabel if has_node("QuantityLabel") else null
@onready var button: Button = $Button if has_node("Button") else null

# State
var item_stack: ItemStack = null
var slot_index: int = -1

func _ready() -> void:
	# Connect button signals if button exists
	if button:
		button.pressed.connect(_on_button_pressed)
		button.mouse_entered.connect(_on_mouse_entered)
		button.mouse_exited.connect(_on_mouse_exited)

	# Set default visual state
	_update_visual()

# Set the item stack for this slot
func set_stack(stack: ItemStack) -> void:
	# Disconnect old stack signals if any
	if item_stack and item_stack.quantity_changed.is_connected(_on_quantity_changed):
		item_stack.quantity_changed.disconnect(_on_quantity_changed)

	item_stack = stack

	# Connect new stack signals
	if item_stack:
		item_stack.quantity_changed.connect(_on_quantity_changed)

	_update_visual()

# Clear the slot
func clear_slot() -> void:
	set_stack(null)

# Update visual representation
func _update_visual() -> void:
	if item_stack == null or item_stack.item_data == null:
		# Empty slot
		if icon_texture:
			icon_texture.texture = null
			icon_texture.visible = false
		if quantity_label:
			quantity_label.visible = false
	else:
		# Slot has item
		if icon_texture:
			icon_texture.texture = item_stack.item_data.icon
			icon_texture.visible = true

		if quantity_label:
			if item_stack.item_data.is_stackable and item_stack.quantity > 1:
				quantity_label.text = str(item_stack.quantity)
				quantity_label.visible = true
			else:
				quantity_label.visible = false

# Get item data for this slot
func get_item_data() -> ItemData:
	if item_stack:
		return item_stack.item_data
	return null

# Check if slot is empty
func is_empty() -> bool:
	return item_stack == null

# Signal handlers
func _on_button_pressed() -> void:
	slot_clicked.emit()

func _on_mouse_entered() -> void:
	slot_hovered.emit()

func _on_mouse_exited() -> void:
	slot_unhovered.emit()

func _on_quantity_changed(_old_quantity: int, _new_quantity: int) -> void:
	_update_visual()
