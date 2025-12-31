class_name DragDropSlot
extends PanelContainer

## DragDropSlot
## Canonical implementation of the Drop Target Contract for slot-based UI elements.
## See docs/ui_drop_target_contract.md for the full contract specification.
##
## This is the base class for all drag-and-drop enabled slots (inventory, equipment).
## Subclasses: EquipmentSlot, InventorySlot
##
## Drop Target Contract Implementation:
##   - can_accept_drop(data) -> bool : Override to add custom validation
##   - handle_drop(data, source) -> bool : Override to handle the drop
##   - signal item_dropped : Emitted after successful drop
##   - signal drop_rejected : Emitted when drop is rejected
##
## Features:
##   - Visual feedback for all drag states (valid, invalid, hover, dragging-from)
##   - Long-press drag initiation for mobile
##   - Slot data management (set/get/clear)
##   - Selection/highlighting support
##   - Automatic styling via UITheme

# Signals
signal drag_started(slot: DragDropSlot, data: Variant)
signal drag_ended(slot: DragDropSlot)
signal item_dropped(slot: DragDropSlot, data: Variant, source_slot: DragDropSlot)
signal drop_rejected(slot: DragDropSlot, data: Variant, reason: String)
signal slot_clicked(slot: DragDropSlot)
signal slot_hovered(slot: DragDropSlot)
signal slot_unhovered(slot: DragDropSlot)

# Slot visual states
enum SlotState {
	EMPTY,
	FILLED,
	DRAG_HOVER_VALID,
	DRAG_HOVER_INVALID,
	HIGHLIGHTED,
	DISABLED,
	DRAGGING_FROM  # Source slot during drag - recessed appearance
}

# Configuration
@export var slot_size: Vector2 = Vector2(64, 64):
	set(value):
		slot_size = value
		custom_minimum_size = slot_size

@export var can_drag: bool = true
@export var can_drop: bool = true
@export var slot_index: int = -1
@export var long_press_duration: float = 0.4  # Seconds before drag starts

# Current state
var _state: SlotState = SlotState.EMPTY
var _slot_data: Variant = null
var _is_dragging: bool = false
var _drag_preview: Control = null

# Long press tracking
var _long_press_timer: Timer = null
var _is_pressing: bool = false
var _press_position: Vector2 = Vector2.ZERO
var _drag_threshold: float = 10.0  # Pixels of movement before canceling long press

# Scroll detection - allow parent ScrollContainer to detect scrolls
var _scroll_check_timer: Timer = null
var _scroll_check_duration: float = 0.08  # Short delay before claiming input
var _claimed_input: bool = false  # True once we've determined this is a tap, not a scroll

# Internal nodes
var _icon_rect: TextureRect
var _highlight_rect: ColorRect

func _ready() -> void:
	custom_minimum_size = slot_size
	# Use MOUSE_FILTER_PASS to allow ScrollContainer to detect scroll gestures
	# We'll claim input only after confirming it's a tap, not a scroll
	mouse_filter = Control.MOUSE_FILTER_PASS

	_build_internal_structure()
	_setup_long_press_timer()
	_setup_scroll_check_timer()
	_apply_style()

	# Ensure touch-friendly size
	UIScaler.ensure_touch_size(self)

	# Listen for theme changes
	if UIThemeManager:
		UIThemeManager.theme_changed.connect(_on_theme_changed)

func _setup_long_press_timer() -> void:
	_long_press_timer = Timer.new()
	_long_press_timer.name = "LongPressTimer"
	_long_press_timer.one_shot = true
	_long_press_timer.wait_time = long_press_duration
	_long_press_timer.timeout.connect(_on_long_press_triggered)
	add_child(_long_press_timer)

func _setup_scroll_check_timer() -> void:
	_scroll_check_timer = Timer.new()
	_scroll_check_timer.name = "ScrollCheckTimer"
	_scroll_check_timer.one_shot = true
	_scroll_check_timer.wait_time = _scroll_check_duration
	_scroll_check_timer.timeout.connect(_on_scroll_check_complete)
	add_child(_scroll_check_timer)

func _on_scroll_check_complete() -> void:
	# If still pressing and haven't moved much, this is a tap - claim the input
	if _is_pressing and not _claimed_input:
		_claimed_input = true

func _build_internal_structure() -> void:
	# Create icon display
	_icon_rect = TextureRect.new()
	_icon_rect.name = "IconRect"
	_icon_rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_icon_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	_icon_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_icon_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	# Add margin for border
	_icon_rect.offset_left = 4
	_icon_rect.offset_top = 4
	_icon_rect.offset_right = -4
	_icon_rect.offset_bottom = -4
	add_child(_icon_rect)

	# Create highlight overlay (for drag feedback)
	_highlight_rect = ColorRect.new()
	_highlight_rect.name = "HighlightRect"
	_highlight_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_highlight_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_highlight_rect.color = Color.TRANSPARENT
	_highlight_rect.visible = false
	add_child(_highlight_rect)

func _apply_style() -> void:
	var stylebox := UIThemeManager.create_drag_drop_slot_stylebox(_state_to_string())
	add_theme_stylebox_override("panel", stylebox)

func _state_to_string() -> String:
	match _state:
		SlotState.EMPTY:
			return "empty"
		SlotState.FILLED:
			return "filled"
		SlotState.DRAG_HOVER_VALID:
			return "valid_drop"
		SlotState.DRAG_HOVER_INVALID:
			return "invalid_drop"
		SlotState.HIGHLIGHTED:
			return "highlighted"
		SlotState.DISABLED:
			return "empty"  # Use empty style for disabled
		SlotState.DRAGGING_FROM:
			return "dragging_from"
		_:
			return "empty"

func _set_state(new_state: SlotState) -> void:
	if _state == new_state:
		return
	_state = new_state
	_apply_style()
	_update_highlight()

func _update_highlight() -> void:
	match _state:
		SlotState.DRAG_HOVER_VALID:
			_highlight_rect.color = Color(0.3, 0.7, 0.3, 0.3)
			_highlight_rect.visible = true
		SlotState.DRAG_HOVER_INVALID:
			_highlight_rect.color = Color(0.7, 0.3, 0.3, 0.3)
			_highlight_rect.visible = true
		SlotState.HIGHLIGHTED:
			_highlight_rect.color = Color(0.9, 0.7, 0.3, 0.3)
			_highlight_rect.visible = true
		_:
			_highlight_rect.visible = false

# Input handling
func _gui_input(event: InputEvent) -> void:
	if _state == SlotState.DISABLED:
		return

	# Handle click/touch
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_LEFT:
			if mb.pressed:
				_on_slot_pressed(mb.position)
				# Don't accept the event yet - let ScrollContainer see it
				# We'll handle the release if this turns out to be a tap
			else:
				_on_slot_released()
				# Only consume the release if we claimed this input
				if _claimed_input:
					accept_event()

	# Handle motion during press (for long-press cancellation and drag)
	if event is InputEventMouseMotion:
		var motion := event as InputEventMouseMotion
		if _is_pressing and not _is_dragging:
			# Check if moved too far - cancel long press to allow scrolling
			var distance := motion.position.distance_to(_press_position)
			if distance > _drag_threshold:
				_cancel_long_press()
				# Don't accept - let scroll happen
		elif _is_dragging:
			_on_drag_motion(motion)
			accept_event()  # Consume drag motion

func _on_slot_pressed(position: Vector2) -> void:
	_is_pressing = true
	_press_position = position
	_claimed_input = false

	# Start scroll check timer - after this fires we claim the input as a tap
	_scroll_check_timer.start()

	# Start long press timer if we have draggable content
	if can_drag and _slot_data != null:
		_long_press_timer.start()

func _on_slot_released() -> void:
	var was_pressing := _is_pressing
	_is_pressing = false
	_scroll_check_timer.stop()

	# If dragging, end the drag
	if _is_dragging:
		_end_drag()
		return

	# If timer was running (long press not triggered), this is a quick tap
	if was_pressing and not _long_press_timer.is_stopped():
		_long_press_timer.stop()
		# Quick tap - claim input and emit click for selection
		_claimed_input = true
		slot_clicked.emit(self)

func _on_long_press_triggered() -> void:
	# Long press completed - start drag if still pressing
	if _is_pressing and can_drag and _slot_data != null:
		_claimed_input = true  # Claim input for the drag operation
		_start_drag()
		# Emit click as well so the item shows info
		slot_clicked.emit(self)

func _cancel_long_press() -> void:
	_is_pressing = false
	_claimed_input = false
	_long_press_timer.stop()
	_scroll_check_timer.stop()

func _start_drag() -> void:
	if _is_dragging:
		return

	_is_dragging = true
	_drag_preview = _create_drag_preview()

	# Apply recessed visual state and hide the icon
	_set_state(SlotState.DRAGGING_FROM)
	_icon_rect.visible = false

	drag_started.emit(self, _slot_data)

	# Notify DragDropManager if it exists
	if has_node("/root/DragDropManager"):
		var manager = get_node("/root/DragDropManager")
		manager.start_drag(self, _slot_data, _drag_preview)

func _end_drag() -> void:
	if not _is_dragging:
		return

	_is_dragging = false

	# Restore icon visibility and normal state
	_icon_rect.visible = true
	_set_state(SlotState.FILLED if _slot_data else SlotState.EMPTY)

	drag_ended.emit(self)

	# Notify DragDropManager if it exists
	if has_node("/root/DragDropManager"):
		var manager = get_node("/root/DragDropManager")
		manager.end_drag()

func _on_drag_motion(_event: InputEventMouseMotion) -> void:
	# DragDropManager handles preview positioning
	pass

func _create_drag_preview() -> Control:
	# Create a visual preview matching the icon display in the slot
	# Icon rect has 4px margins on each side, so effective size is slot_size - 8
	var icon_display_size := slot_size - Vector2(8, 8)
	var preview := TextureRect.new()
	preview.texture = _icon_rect.texture
	preview.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	preview.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	preview.custom_minimum_size = icon_display_size
	preview.size = icon_display_size
	preview.modulate = Color(1, 1, 1, 0.8)
	preview.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return preview

# Mouse enter/exit for hover states during drag
func _notification(what: int) -> void:
	match what:
		NOTIFICATION_MOUSE_ENTER:
			_on_mouse_entered()
		NOTIFICATION_MOUSE_EXIT:
			_on_mouse_exited()

func _on_mouse_entered() -> void:
	slot_hovered.emit(self)

	# Check if something is being dragged
	if has_node("/root/DragDropManager"):
		var manager = get_node("/root/DragDropManager")
		if manager.is_dragging():
			var drag_data = manager.get_drag_data()
			if can_accept_drop(drag_data):
				_set_state(SlotState.DRAG_HOVER_VALID)
			else:
				_set_state(SlotState.DRAG_HOVER_INVALID)

func _on_mouse_exited() -> void:
	slot_unhovered.emit(self)

	# Cancel long press and scroll check if mouse leaves while pressing (allows scroll to work)
	if _is_pressing and not _is_dragging:
		_cancel_long_press()

	# Restore normal state if we were showing drag hover
	if _state == SlotState.DRAG_HOVER_VALID or _state == SlotState.DRAG_HOVER_INVALID:
		_set_state(SlotState.FILLED if _slot_data else SlotState.EMPTY)

func _on_theme_changed(_new_theme: Resource) -> void:
	_apply_style()

# Drop handling - override in subclasses
func can_accept_drop(data: Variant) -> bool:
	# Base implementation: accept any data if can_drop is true
	return can_drop and data != null

func handle_drop(data: Variant, source_slot: DragDropSlot) -> bool:
	if not can_accept_drop(data):
		drop_rejected.emit(self, data, "Cannot accept this item")
		return false

	# Store the data
	var old_data: Variant = _slot_data
	set_slot_data(data)

	# Emit success signal
	item_dropped.emit(self, data, source_slot)

	# If source slot should be cleared, do it
	if source_slot and source_slot != self:
		# Swap if we had data, otherwise just clear source
		if old_data:
			source_slot.set_slot_data(old_data)
		else:
			source_slot.clear_slot()

	return true

# Public API
func set_slot_data(data: Variant) -> void:
	_slot_data = data
	_update_display()
	_set_state(SlotState.FILLED if data else SlotState.EMPTY)

func get_slot_data() -> Variant:
	return _slot_data

func clear_slot() -> void:
	_slot_data = null
	_icon_rect.texture = null
	_set_state(SlotState.EMPTY)

func is_empty() -> bool:
	return _slot_data == null

func set_highlighted(highlighted: bool) -> void:
	if highlighted and _slot_data:
		_set_state(SlotState.HIGHLIGHTED)
	else:
		_set_state(SlotState.FILLED if _slot_data else SlotState.EMPTY)

func set_disabled(disabled: bool) -> void:
	if disabled:
		_set_state(SlotState.DISABLED)
		mouse_filter = Control.MOUSE_FILTER_IGNORE
	else:
		_set_state(SlotState.FILLED if _slot_data else SlotState.EMPTY)
		mouse_filter = Control.MOUSE_FILTER_PASS

# Override in subclasses to update display based on data type
func _update_display() -> void:
	# Base implementation: if data has an icon property, use it
	if _slot_data and _slot_data is Object:
		if _slot_data.has_method("get_icon"):
			_icon_rect.texture = _slot_data.get_icon()
		elif "icon" in _slot_data:
			_icon_rect.texture = _slot_data.icon
	elif _slot_data is Texture2D:
		_icon_rect.texture = _slot_data

func set_icon(texture: Texture2D) -> void:
	_icon_rect.texture = texture

func get_icon() -> Texture2D:
	return _icon_rect.texture

# Refresh styling
func refresh_style() -> void:
	_apply_style()

# Create a DragDropSlot programmatically
static func create(size: Vector2 = Vector2(64, 64)) -> DragDropSlot:
	var slot := DragDropSlot.new()
	slot.slot_size = size
	return slot
