extends CanvasLayer

## DragDropManager
## Global singleton for managing drag-and-drop operations
## Tracks current drag, shows ghost preview, validates drop targets
## Note: Uses loose typing to avoid autoload initialization order issues

signal drag_started(source_slot: Control, data: Variant)
signal drag_ended(source_slot: Control, dropped: bool)
signal drag_cancelled(source_slot: Control)
signal drop_completed(source_slot: Control, target_slot: Control, data: Variant)

# Current drag state
var _is_dragging: bool = false
var _source_slot: Control = null
var _drag_data: Variant = null
var _drag_preview: Control = null
var _drag_offset: Vector2 = Vector2.ZERO

# Visual settings
@export var preview_opacity: float = 0.8
@export var preview_scale: float = 0.9
@export var drag_threshold: float = 5.0  # Pixels before drag starts

# Drop target tracking
var _current_hover_target: Control = null
var _valid_drop_targets: Array[Control] = []

func _ready() -> void:
	# Ensure we render above everything
	layer = 200
	process_mode = Node.PROCESS_MODE_ALWAYS

func _process(_delta: float) -> void:
	if _is_dragging and _drag_preview:
		_update_preview_position()

func _input(event: InputEvent) -> void:
	if not _is_dragging:
		return

	# Handle mouse/touch movement
	if event is InputEventMouseMotion:
		_check_hover_target(event.position)

	# Handle release
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_LEFT and not mb.pressed:
			_attempt_drop()

	# Handle cancel (ESC or right-click)
	if event is InputEventKey:
		var key := event as InputEventKey
		if key.pressed and key.keycode == KEY_ESCAPE:
			cancel_drag()

	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_RIGHT and mb.pressed:
			cancel_drag()

func _update_preview_position() -> void:
	if _drag_preview:
		var mouse_pos := get_viewport().get_mouse_position()
		_drag_preview.global_position = mouse_pos - _drag_offset

func _check_hover_target(mouse_pos: Vector2) -> void:
	# Find slot under mouse
	var new_target: Control = null

	for target in _valid_drop_targets:
		if target == _source_slot:
			continue
		if not is_instance_valid(target):
			continue
		if not target.visible:
			continue

		var rect: Rect2 = target.get_global_rect()
		if rect.has_point(mouse_pos):
			new_target = target
			break

	# Update hover state
	if new_target != _current_hover_target:
		if _current_hover_target and is_instance_valid(_current_hover_target):
			if _current_hover_target.has_method("_on_mouse_exited"):
				_current_hover_target._on_mouse_exited()

		_current_hover_target = new_target

		if _current_hover_target:
			if _current_hover_target.has_method("_on_mouse_entered"):
				_current_hover_target._on_mouse_entered()

# Check if a node is a DragDropSlot (duck typing)
func _is_drag_drop_slot(node: Node) -> bool:
	if not node is Control:
		return false
	return node.has_method("can_accept_drop") and node.has_method("handle_drop")

# Start a drag operation
func start_drag(source: Control, data: Variant, preview: Control = null) -> void:
	if _is_dragging:
		cancel_drag()

	_source_slot = source
	_drag_data = data
	_is_dragging = true

	# Set up preview
	if preview:
		_drag_preview = preview
	else:
		_drag_preview = _create_default_preview(source)

	if _drag_preview:
		_drag_preview.modulate.a = preview_opacity
		_drag_preview.scale = Vector2.ONE * preview_scale
		_drag_preview.mouse_filter = Control.MOUSE_FILTER_IGNORE
		_drag_preview.z_index = 100
		add_child(_drag_preview)

		# Center preview on cursor
		_drag_offset = _drag_preview.size * preview_scale / 2.0
		_update_preview_position()

	# Find all valid drop targets in the scene
	_collect_drop_targets()

	drag_started.emit(_source_slot, _drag_data)

func _create_default_preview(source: Control) -> Control:
	var preview := TextureRect.new()

	# Try to get icon from source slot
	if source.has_method("get_icon"):
		preview.texture = source.get_icon()

	preview.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	preview.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED

	# Try to get slot_size from source
	var slot_size := Vector2(48, 48)
	if "slot_size" in source:
		slot_size = source.slot_size

	preview.custom_minimum_size = slot_size
	preview.size = slot_size
	return preview

func _collect_drop_targets() -> void:
	_valid_drop_targets.clear()

	# Find all DragDropSlot nodes in the scene tree
	var root := get_tree().root
	_find_slots_recursive(root)

func _find_slots_recursive(node: Node) -> void:
	# Recurse into children FIRST (post-order traversal)
	# This ensures more specific targets (nested slots) are checked before containers
	for child in node.get_children():
		_find_slots_recursive(child)

	# Then check this node
	if _is_drag_drop_slot(node):
		var slot := node as Control
		# Check if slot can receive drops and isn't the source
		var can_drop := true
		if "can_drop" in slot:
			can_drop = slot.can_drop
		if can_drop and slot != _source_slot:
			_valid_drop_targets.append(slot)

func _attempt_drop() -> void:
	var success := false

	if _current_hover_target and is_instance_valid(_current_hover_target):
		if _current_hover_target.has_method("can_accept_drop"):
			if _current_hover_target.can_accept_drop(_drag_data):
				if _current_hover_target.has_method("handle_drop"):
					success = _current_hover_target.handle_drop(_drag_data, _source_slot)
					if success:
						drop_completed.emit(_source_slot, _current_hover_target, _drag_data)

	end_drag(success)

# End the current drag operation
func end_drag(dropped: bool = false) -> void:
	if not _is_dragging:
		return

	# Clear hover state
	if _current_hover_target and is_instance_valid(_current_hover_target):
		if _current_hover_target.has_method("_on_mouse_exited"):
			_current_hover_target._on_mouse_exited()

	# Remove preview
	if _drag_preview:
		_drag_preview.queue_free()
		_drag_preview = null

	var source := _source_slot

	# Reset state
	_is_dragging = false
	_source_slot = null
	_drag_data = null
	_current_hover_target = null
	_valid_drop_targets.clear()

	drag_ended.emit(source, dropped)

# Cancel the drag and return item to source
func cancel_drag() -> void:
	if not _is_dragging:
		return

	var source := _source_slot
	end_drag(false)
	drag_cancelled.emit(source)

# Query current drag state
func is_dragging() -> bool:
	return _is_dragging

func get_drag_data() -> Variant:
	return _drag_data

func get_source_slot() -> Control:
	return _source_slot

func get_current_target() -> Control:
	return _current_hover_target

# Register a slot as a potential drop target (for dynamic slots)
func register_drop_target(slot: Control) -> void:
	if slot and slot not in _valid_drop_targets:
		_valid_drop_targets.append(slot)

# Unregister a drop target
func unregister_drop_target(slot: Control) -> void:
	_valid_drop_targets.erase(slot)

# Force refresh of drop targets (call after adding new slots)
func refresh_drop_targets() -> void:
	if _is_dragging:
		_collect_drop_targets()
