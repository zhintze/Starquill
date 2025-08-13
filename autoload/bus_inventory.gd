## BusInventory.gd — items & equipment
extends Node

signal item_added(owner_id: String, item_id: String, qty: int)
signal item_removed(owner_id: String, item_id: String, qty: int)
signal inventory_changed(owner_id: String, snapshot: Dictionary)
signal equipment_equipped(owner_id: String, slot: String, equipment_id: String)
signal equipment_unequipped(owner_id: String, slot: String, equipment_id: String)

# Helpers
func add_item(owner_id: String, item_id: String, qty: int) -> void:
	item_added.emit(owner_id, item_id, qty)

func remove_item(owner_id: String, item_id: String, qty: int) -> void:
	item_removed.emit(owner_id, item_id, qty)

func emit_inventory_snapshot(owner_id: String, snapshot: Dictionary) -> void:
	inventory_changed.emit(owner_id, snapshot)

func equip(owner_id: String, slot: String, equipment_id: String) -> void:
	equipment_equipped.emit(owner_id, slot, equipment_id)

func unequip(owner_id: String, slot: String, equipment_id: String) -> void:
	equipment_unequipped.emit(owner_id, slot, equipment_id)
