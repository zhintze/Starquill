extends BaseMenu
class_name PartyMenu

# UI References - to be connected when scene is created
@onready var tab_container: TabContainer = $TabContainer if has_node("TabContainer") else null
@onready var inventory_panel: InventoryPanel = $TabContainer/Inventory if has_node("TabContainer/Inventory") else null
@onready var character_tabs: TabContainer = $TabContainer/Characters if has_node("TabContainer/Characters") else null

# State
var current_character_index: int = 0
var character_sheet_panels: Array[CharacterSheetPanel] = []

func _ready() -> void:
	super._ready()
	menu_name = &"party_menu"

	# Set up character tabs
	_setup_character_tabs()

	# Connect signals
	EventBus.party_member_added.connect(_on_party_member_added)
	EventBus.party_member_removed.connect(_on_party_member_removed)

func _on_open() -> void:
	_refresh_party_data()

func _setup_character_tabs() -> void:
	if not character_tabs:
		# Create character tabs programmatically if container doesn't exist
		return

	# Clear existing tabs
	for child in character_tabs.get_children():
		child.queue_free()

	character_sheet_panels.clear()

	# Create tab for each party member
	if not PlayerData or not PlayerData.party:
		return

	for i in range(PlayerData.get_party_size()):
		var character := PlayerData.party.members[i]

		# Create character sheet panel
		var sheet := CharacterSheetPanel.new()
		sheet.set_character(character)
		sheet.name = "Character%d" % i

		character_tabs.add_child(sheet)
		character_tabs.set_tab_title(i, character.display_name)

		character_sheet_panels.append(sheet)

func _refresh_party_data() -> void:
	# Refresh inventory panel
	if inventory_panel:
		inventory_panel.refresh()

	# Refresh character sheets
	_refresh_character_sheets()

func _refresh_character_sheets() -> void:
	for sheet in character_sheet_panels:
		if sheet:
			sheet.refresh()

func _on_party_member_added(character: Character, position: int) -> void:
	_setup_character_tabs()

func _on_party_member_removed(character: Character) -> void:
	_setup_character_tabs()

# Public API for external control
func switch_to_inventory_tab() -> void:
	if tab_container:
		tab_container.current_tab = 0  # Assuming inventory is first tab

func switch_to_character_tab(index: int) -> void:
	if tab_container:
		tab_container.current_tab = 1  # Assuming characters is second tab
	if character_tabs and index >= 0 and index < character_tabs.get_tab_count():
		character_tabs.current_tab = index
