extends Panel
class_name EquipmentSlotUI

signal equipment_clicked()
signal unequip_requested()
signal slot_hovered()
signal slot_unhovered()

# UI References - to be connected when scene is created
@onready var slot_label: Label = $SlotLabel if has_node("SlotLabel") else null
@onready var icon_texture: TextureRect = $TextureRect if has_node("TextureRect") else null
@onready var item_name_label: Label = $ItemNameLabel if has_node("ItemNameLabel") else null
@onready var button: Button = $Button if has_node("Button") else null

# Configuration
@export var slot_name: String = "Slot"
@export var slot_type: StringName = &"head"

# State
var equipment: EquipmentInstance = null

func _ready() -> void:
	# Set slot label
	if slot_label:
		slot_label.text = slot_name

	# Connect button signals if button exists
	if button:
		button.pressed.connect(_on_button_pressed)
		button.mouse_entered.connect(_on_mouse_entered)
		button.mouse_exited.connect(_on_mouse_exited)

	_update_visual()

# Set equipment for this slot
func set_equipment(equip: EquipmentInstance) -> void:
	equipment = equip
	_update_visual()

# Clear equipment from slot
func clear_equipment() -> void:
	set_equipment(null)

# Update visual representation
func _update_visual() -> void:
	if equipment == null:
		# Empty slot
		if icon_texture:
			icon_texture.texture = null
			icon_texture.visible = false
		if item_name_label:
			item_name_label.text = ""
			item_name_label.visible = false
	else:
		# Slot has equipment
		# Icon texture would need to be derived from equipment type
		# For now, just show the item type
		if icon_texture:
			# TODO: Load icon based on equipment.item_type
			icon_texture.visible = false

		if item_name_label:
			item_name_label.text = equipment.item_type
			item_name_label.visible = true

# Check if slot is empty
func is_empty() -> bool:
	return equipment == null

# Signal handlers
func _on_button_pressed() -> void:
	if equipment != null:
		# Show context menu or emit signal
		equipment_clicked.emit()
		# For now, just emit unequip request
		unequip_requested.emit()
	else:
		# Empty slot clicked - could open inventory to select item
		equipment_clicked.emit()

func _on_mouse_entered() -> void:
	slot_hovered.emit()

func _on_mouse_exited() -> void:
	slot_unhovered.emit()
