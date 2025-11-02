extends Panel
class_name InventoryPanel

# UI References - to be connected when scene is created
@onready var grid_container: GridContainer = $VBoxContainer/ScrollContainer/GridContainer if has_node("VBoxContainer/ScrollContainer/GridContainer") else null
@onready var info_panel: Panel = $VBoxContainer/InfoPanel if has_node("VBoxContainer/InfoPanel") else null
@onready var item_name_label: Label = $VBoxContainer/InfoPanel/VBoxContainer/NameLabel if has_node("VBoxContainer/InfoPanel/VBoxContainer/NameLabel") else null
@onready var item_desc_label: Label = $VBoxContainer/InfoPanel/VBoxContainer/DescLabel if has_node("VBoxContainer/InfoPanel/VBoxContainer/DescLabel") else null
@onready var item_value_label: Label = $VBoxContainer/InfoPanel/VBoxContainer/ValueLabel if has_node("VBoxContainer/InfoPanel/VBoxContainer/ValueLabel") else null

# State
var inventory: Inventory
var inventory_controller: InventoryController
var selected_slot: int = -1

# Item slot scene reference
var item_slot_scene: PackedScene = null

func _ready() -> void:
	# Get shared inventory from PlayerData
	# Note: PlayerData.shared_inventory will be created in Phase 5
	if PlayerData.has_method("get_shared_inventory"):
		inventory = PlayerData.get_shared_inventory()
	elif "shared_inventory" in PlayerData:
		inventory = PlayerData.shared_inventory
	else:
		# Fallback: create temporary inventory for testing
		inventory = Inventory.new(24)
		push_warning("InventoryPanel: PlayerData.shared_inventory not found, using temporary inventory")

	# Create controller
	if inventory:
		inventory_controller = InventoryController.new(inventory)

		# Connect signals
		inventory.changed.connect(_on_inventory_changed)

	# Load item slot scene when it's created
	# item_slot_scene = preload("res://scenes/ui/item_slot.tscn")

	# Initial render
	refresh()

func refresh() -> void:
	if not inventory:
		return

	_clear_slots()
	_render_slots()

func _clear_slots() -> void:
	if not grid_container:
		return

	for child in grid_container.get_children():
		child.queue_free()

func _render_slots() -> void:
	if not grid_container or not inventory:
		return

	for i in range(inventory.capacity):
		var slot_ui: ItemSlotUI

		# Use scene if available, otherwise create programmatically
		if item_slot_scene:
			slot_ui = item_slot_scene.instantiate() as ItemSlotUI
		else:
			# Create programmatic slot
			slot_ui = ItemSlotUI.new()
			slot_ui.custom_minimum_size = Vector2(64, 64)

		grid_container.add_child(slot_ui)

		var stack := inventory.get_stack_at(i)
		if stack != null:
			slot_ui.set_stack(stack)

		slot_ui.slot_index = i
		slot_ui.slot_clicked.connect(_on_slot_clicked.bind(i))
		slot_ui.slot_hovered.connect(_on_slot_hovered.bind(i))
		slot_ui.slot_unhovered.connect(_on_slot_unhovered)

func _on_slot_clicked(slot_index: int) -> void:
	selected_slot = slot_index
	var stack := inventory.get_stack_at(slot_index)

	if stack != null and stack.item_data.is_usable:
		_show_use_menu(stack)

func _on_slot_hovered(slot_index: int) -> void:
	var stack := inventory.get_stack_at(slot_index)

	if stack != null and stack.item_data != null:
		if item_name_label:
			item_name_label.text = stack.item_data.display_name
		if item_desc_label:
			item_desc_label.text = stack.item_data.description
		if item_value_label:
			item_value_label.text = "Value: %d gold" % stack.item_data.sell_value
	else:
		_clear_info_panel()

func _on_slot_unhovered() -> void:
	_clear_info_panel()

func _clear_info_panel() -> void:
	if item_name_label:
		item_name_label.text = ""
	if item_desc_label:
		item_desc_label.text = ""
	if item_value_label:
		item_value_label.text = ""

func _show_use_menu(stack: ItemStack) -> void:
	# TODO: Show context menu for using item
	# For now, just print
	print("InventoryPanel: Use item - %s" % stack.item_data.display_name)

	# Could emit signal or show popup menu
	# use_item_requested.emit(stack)

func _on_inventory_changed() -> void:
	refresh()

# Public API for external usage
func sort_by_name() -> void:
	if inventory_controller:
		inventory_controller.sort_inventory(&"name")

func sort_by_type() -> void:
	if inventory_controller:
		inventory_controller.sort_inventory(&"type")

func sort_by_value() -> void:
	if inventory_controller:
		inventory_controller.sort_inventory(&"value")
