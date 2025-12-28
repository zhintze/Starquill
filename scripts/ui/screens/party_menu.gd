class_name PartyMenuScreen
extends Control

## PartyMenuScreen
## The main party management screen
## Left: CharacterPanel with equipment slots
## Right: Tabbed content (Inventory, Stats, Journal, Map)

signal menu_closed
signal character_changed(index: int)
signal equipment_changed(slot: EquipmentSlot, item: Variant)
signal item_used(index: int, item: Variant)

# Tab indices
enum ContentTab {
	INVENTORY = 0,
	STATS = 1,
	JOURNAL = 2,
	MAP = 3
}

# Party data
var _party_members: Array = []  # Array of character data
var _current_character_index: int = 0

# UI References
var _background: ColorRect
var _main_panel: NinePatchRect
var _main_hbox: HBoxContainer
var _character_panel: CharacterPanel
var _content_container: VBoxContainer
var _tab_bar: BookmarkTabBar
var _content_stack: Control  # Container for swappable content
var _inventory_panel: PanelContainer  # InventoryPanelNew
var _stats_panel: StatsPanel
var _journal_panel: PanelContainer
var _map_panel: PanelContainer
var _close_button: IconButton
var _coordinator: PartyMenuCoordinator

func _ready() -> void:
	_build_ui()
	_connect_signals()
	_setup_coordinator()
	_load_party_data()
	_update_display()

	# Listen for theme changes
	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

func _setup_coordinator() -> void:
	# Create coordinator for cross-panel selection coordination
	# See: scripts/ui/screens/party_menu_coordinator.gd
	_coordinator = PartyMenuCoordinator.new()
	_coordinator.setup(_character_panel, _inventory_panel, _tab_bar)

func _build_ui() -> void:
	# Full screen layout
	set_anchors_preset(Control.PRESET_FULL_RECT)

	var theme := UIThemeManager.get_theme()

	# Background overlay
	_background = ColorRect.new()
	_background.name = "Background"
	_background.set_anchors_preset(Control.PRESET_FULL_RECT)
	_background.color = theme.bg_overlay
	add_child(_background)

	# Safe area margin (fallback if UIScaler not available)
	var safe_margin := UIThemeManager.make_margin_container("panel")
	safe_margin.name = "SafeMargin"
	safe_margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(safe_margin)

	# Main panel with paper styling
	_main_panel = BasePanel.create(BasePanel.PanelStyle.PRIMARY, true)
	_main_panel.name = "MainPanel"
	_main_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_main_panel.size_flags_vertical = Control.SIZE_EXPAND_FILL
	safe_margin.add_child(_main_panel)

	# Panel margin (padding comes from MarginContainer per UI rules)
	var panel_margin := UIThemeManager.make_margin_container("panel")
	panel_margin.name = "PanelMargin"
	panel_margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	_main_panel.add_child(panel_margin)

	# Main horizontal split (character | content)
	_main_hbox = UIThemeManager.make_hbox("medium")
	_main_hbox.name = "MainHBox"
	_main_hbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_main_hbox.size_flags_vertical = Control.SIZE_EXPAND_FILL
	panel_margin.add_child(_main_hbox)

	# Left side: Character Panel
	_build_character_panel()

	# Right side: Tabbed Content
	_build_content_area()

	# Close button at top-left corner using OverlayButtonContainer
	# (avoids absolute positioning per docs/ui_ai_rules.md Rule 4)
	var close_overlay := OverlayButtonContainer.create(
		OverlayButtonContainer.Position.TOP_LEFT,
		true  # use safe area
	)
	close_overlay.name = "CloseButtonOverlay"
	close_overlay.z_index = 100  # Ensure it's above everything
	add_child(close_overlay)

	_close_button = IconButton.new()
	_close_button.name = "CloseButton"
	_close_button.preset_icon = IconButton.PresetIcon.CLOSE
	_close_button.icon_position = IconButton.IconPosition.ONLY
	_close_button.button_style = ThemedButton.ButtonStyle.SECONDARY
	_close_button.custom_minimum_size = Vector2(UIConstants.ICON_BUTTON_SIZE, UIConstants.ICON_BUTTON_SIZE)
	_close_button.pressed.connect(_on_close_pressed)
	close_overlay.add_child(_close_button)

func _build_character_panel() -> void:
	_character_panel = CharacterPanel.new()
	_character_panel.name = "CharacterPanel"
	_character_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_character_panel.size_flags_stretch_ratio = UIConstants.CHARACTER_PANEL_WIDTH_RATIO
	_main_hbox.add_child(_character_panel)

func _build_content_area() -> void:
	# Content container with zero separation (tabs touch their content)
	_content_container = UIThemeManager.make_vbox("tiny")
	_content_container.add_theme_constant_override("separation", 0)  # Override for tab-content connection
	_content_container.name = "ContentContainer"
	_content_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_content_container.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_content_container.size_flags_stretch_ratio = UIConstants.CONTENT_PANEL_WIDTH_RATIO
	_main_hbox.add_child(_content_container)

	# Tab bar
	_tab_bar = BookmarkTabBar.new()
	_tab_bar.name = "TabBar"
	_tab_bar.tabs = ["Inventory", "Stats", "Journal", "Map"]
	_tab_bar.active_tab = ContentTab.INVENTORY
	_content_container.add_child(_tab_bar)

	# Content stack (shows one panel at a time)
	_content_stack = Control.new()
	_content_stack.name = "ContentStack"
	_content_stack.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_content_stack.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_content_stack.clip_contents = true
	_content_container.add_child(_content_stack)

	# Create content panels
	_build_inventory_tab()
	_build_stats_tab()
	_build_journal_tab()
	_build_map_tab()

	# Show initial tab
	_show_tab(ContentTab.INVENTORY)

func _build_inventory_tab() -> void:
	# Create InventoryPanelNew directly (class_name defined in script)
	_inventory_panel = InventoryPanelNew.new()
	_inventory_panel.name = "InventoryTab"
	_inventory_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	_content_stack.add_child(_inventory_panel)

func _create_fallback_inventory() -> void:
	var grid := ScrollableGrid.new()
	grid.slot_count = 24
	grid.slot_type = ScrollableGrid.SlotType.INVENTORY
	grid.set_anchors_preset(Control.PRESET_FULL_RECT)
	_inventory_panel.add_child(grid)

func _build_stats_tab() -> void:
	_stats_panel = StatsPanel.new()
	_stats_panel.name = "StatsTab"
	_stats_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	_stats_panel.visible = false
	_content_stack.add_child(_stats_panel)

func _build_journal_tab() -> void:
	_journal_panel = _create_placeholder_panel("Journal", "Coming Soon")
	_journal_panel.name = "JournalTab"
	_journal_panel.visible = false
	_content_stack.add_child(_journal_panel)

func _build_map_tab() -> void:
	_map_panel = _create_placeholder_panel("Map", "Coming Soon")
	_map_panel.name = "MapTab"
	_map_panel.visible = false
	_content_stack.add_child(_map_panel)

func _create_placeholder_panel(title: String, message: String) -> PanelContainer:
	var panel := PanelContainer.new()
	panel.set_anchors_preset(Control.PRESET_FULL_RECT)

	var stylebox := UIThemeManager.create_bg_stylebox("secondary")
	panel.add_theme_stylebox_override("panel", stylebox)

	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	panel.add_child(center)

	var vbox := UIThemeManager.make_vbox("small")
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	center.add_child(vbox)

	var theme := UIThemeManager.get_theme()

	var title_label := Label.new()
	title_label.text = title
	title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title_label.add_theme_font_size_override("font_size", theme.font_size_header)
	title_label.add_theme_color_override("font_color", theme.text_color)
	vbox.add_child(title_label)

	var msg_label := Label.new()
	msg_label.text = message
	msg_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	msg_label.add_theme_font_size_override("font_size", theme.font_size_body)
	msg_label.add_theme_color_override("font_color", theme.text_color_secondary)
	vbox.add_child(msg_label)

	return panel

func _connect_signals() -> void:
	_tab_bar.tab_changed.connect(_on_tab_changed)
	_character_panel.character_changed.connect(_on_character_changed)
	_character_panel.equipment_changed.connect(_on_equipment_changed)
	_character_panel.unequip_requested.connect(_on_unequip_requested)
	# Note: Cross-panel selection coordination handled by PartyMenuCoordinator

func _load_party_data() -> void:
	# Try to load party from PlayerData
	if has_node("/root/PlayerData"):
		var player_data = get_node("/root/PlayerData")
		if player_data.has_method("get_party_members"):
			_party_members = player_data.get_party_members()
		elif "party" in player_data and player_data.party:
			_party_members = player_data.party.members

	# Set party size
	_character_panel.set_party_size(max(1, _party_members.size()))

	# Load first character
	if _party_members.size() > 0:
		_set_current_character(0)

func _update_display() -> void:
	if _party_members.size() > 0 and _current_character_index < _party_members.size():
		var character = _party_members[_current_character_index]
		_character_panel.set_character(character)
		_stats_panel.set_character(character)

func _show_tab(tab_index: int) -> void:
	# Hide all panels
	_inventory_panel.visible = false
	_stats_panel.visible = false
	_journal_panel.visible = false
	_map_panel.visible = false

	# Show selected panel
	match tab_index:
		ContentTab.INVENTORY:
			_inventory_panel.visible = true
		ContentTab.STATS:
			_stats_panel.visible = true
		ContentTab.JOURNAL:
			_journal_panel.visible = true
		ContentTab.MAP:
			_map_panel.visible = true

func _set_current_character(index: int) -> void:
	_current_character_index = clampi(index, 0, max(0, _party_members.size() - 1))
	_character_panel.set_character_index(_current_character_index)
	_update_display()

# Signal handlers
func _on_tab_changed(index: int) -> void:
	_show_tab(index)

func _on_character_changed(index: int) -> void:
	_set_current_character(index)
	character_changed.emit(index)

func _on_equipment_changed(slot: EquipmentSlot, _old_item: Variant, new_item: Variant) -> void:
	equipment_changed.emit(slot, new_item)

# Note: Cross-panel selection coordination (equipment_slot_clicked, item_selected)
# is handled by PartyMenuCoordinator - see _setup_coordinator()

func _on_unequip_requested(slot: EquipmentSlot) -> void:
	# Move equipment to inventory
	var equipment: Variant = slot.get_equipment()
	if equipment:
		slot.clear_equipment()
		# Add to inventory if possible
		if _inventory_panel.has_method("add_item"):
			_inventory_panel.add_item(equipment)

func _on_close_pressed() -> void:
	menu_closed.emit()
	_close_menu()

func _close_menu() -> void:
	# Use UIManager if available
	if UIManager:
		UIManager.close_top_menu()
	else:
		queue_free()

func _on_theme_changed(_new_theme: Resource) -> void:
	var theme := UIThemeManager.get_theme()
	_background.color = theme.bg_overlay

func _exit_tree() -> void:
	# Clean up coordinator signal connections
	if _coordinator:
		_coordinator.cleanup()
		_coordinator = null

func _input(event: InputEvent) -> void:
	# Close on ESC
	if event.is_action_pressed("ui_cancel"):
		_on_close_pressed()
		get_viewport().set_input_as_handled()

# Public API

func set_party(party: Array) -> void:
	_party_members = party
	_character_panel.set_party_size(party.size())
	_update_display()

func get_current_character() -> Variant:
	if _current_character_index < _party_members.size():
		return _party_members[_current_character_index]
	return null

func set_current_character_index(index: int) -> void:
	_set_current_character(index)

func get_current_character_index() -> int:
	return _current_character_index

func set_active_tab(tab: ContentTab) -> void:
	_tab_bar.set_active_tab(tab)
	_show_tab(tab)

func get_active_tab() -> ContentTab:
	return _tab_bar.get_active_tab() as ContentTab

func refresh() -> void:
	_update_display()
	_character_panel.refresh()
	_stats_panel.refresh()
	if _inventory_panel.has_method("refresh"):
		_inventory_panel.refresh()
