class_name PartyMenuCoordinator
extends RefCounted

## PartyMenuCoordinator
## Mediates communication between sibling panels in the PartyMenuScreen.
## Implements the Screen Coordinator Pattern to avoid direct panel-to-panel coupling.
##
## Usage:
##   var coordinator := PartyMenuCoordinator.new()
##   coordinator.setup(character_panel, inventory_panel, tab_bar)
##
## The coordinator:
## 1. Listens to selection_changed signals from panels
## 2. Clears selection on other panels when one panel's selection changes
## 3. Handles tab changes and panel visibility coordination
##
## This pattern keeps panels decoupled - they emit signals and expose
## public methods, but never call each other directly.

# Panel references
var _character_panel: Control = null  # CharacterPanel
var _inventory_panel: Control = null  # InventoryPanelNew
var _tab_bar: Control = null  # BookmarkTabBar

# Track which panel has active selection
var _active_selection_panel: Control = null

## Initialize the coordinator with panel references
## Call this in PartyMenuScreen._ready() after building UI
func setup(character_panel: Control, inventory_panel: Control, tab_bar: Control = null) -> void:
	_character_panel = character_panel
	_inventory_panel = inventory_panel
	_tab_bar = tab_bar

	_connect_signals()

func _connect_signals() -> void:
	# Connect to character panel selection
	if _character_panel:
		if _character_panel.has_signal("equipment_slot_clicked"):
			_character_panel.equipment_slot_clicked.connect(_on_character_selection)

	# Connect to inventory panel selection
	if _inventory_panel:
		if _inventory_panel.has_signal("item_selected"):
			_inventory_panel.item_selected.connect(_on_inventory_selection)

	# Connect to tab changes
	if _tab_bar:
		if _tab_bar.has_signal("tab_changed"):
			_tab_bar.tab_changed.connect(_on_tab_changed)

## Called when an equipment slot is clicked in CharacterPanel
func _on_character_selection(slot: Variant) -> void:
	# Clear inventory selection when equipment is selected
	if _inventory_panel and _inventory_panel.has_method("clear_selection"):
		# Only clear if character panel actually has a selection now
		if _character_panel and _character_panel.has_method("get_selected_slot"):
			var selected = _character_panel.get_selected_slot()
			if selected != null:
				_inventory_panel.clear_selection()
				_active_selection_panel = _character_panel

				# Show equipment info in inventory panel if available
				if slot and slot.has_method("get_equipment"):
					var equipment = slot.get_equipment()
					if equipment and _inventory_panel.has_method("show_item_info"):
						_inventory_panel.show_item_info(equipment)
			else:
				# Equipment was deselected
				_active_selection_panel = null
				if _inventory_panel.has_method("clear_selection"):
					_inventory_panel.clear_selection()

## Called when an inventory item is selected
func _on_inventory_selection(_index: int, _data: Variant) -> void:
	# Clear character panel selection when inventory item is selected
	if _character_panel and _character_panel.has_method("clear_selection"):
		_character_panel.clear_selection()
		_active_selection_panel = _inventory_panel

## Called when tab changes
func _on_tab_changed(_index: int) -> void:
	# Clear all selections when switching tabs
	clear_all_selections()

## Clear selection on all panels
func clear_all_selections() -> void:
	if _character_panel and _character_panel.has_method("clear_selection"):
		_character_panel.clear_selection()
	if _inventory_panel and _inventory_panel.has_method("clear_selection"):
		_inventory_panel.clear_selection()
	_active_selection_panel = null

## Get which panel currently has an active selection
func get_active_selection_panel() -> Control:
	return _active_selection_panel

## Disconnect all signals (call before disposing)
func cleanup() -> void:
	if _character_panel and _character_panel.has_signal("equipment_slot_clicked"):
		if _character_panel.equipment_slot_clicked.is_connected(_on_character_selection):
			_character_panel.equipment_slot_clicked.disconnect(_on_character_selection)

	if _inventory_panel and _inventory_panel.has_signal("item_selected"):
		if _inventory_panel.item_selected.is_connected(_on_inventory_selection):
			_inventory_panel.item_selected.disconnect(_on_inventory_selection)

	if _tab_bar and _tab_bar.has_signal("tab_changed"):
		if _tab_bar.tab_changed.is_connected(_on_tab_changed):
			_tab_bar.tab_changed.disconnect(_on_tab_changed)

	_character_panel = null
	_inventory_panel = null
	_tab_bar = null
	_active_selection_panel = null
