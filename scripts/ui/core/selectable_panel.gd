class_name SelectablePanel
extends PanelContainer

## SelectablePanel
## Base class for panels that support item selection
## Provides a formal interface for selection coordination between panels
##
## Usage:
## 1. Extend this class for panels that need selection
## 2. Override clear_selection() to clear visual selection state
## 3. Override get_selected_item() to return current selection
## 4. Emit selection_changed signal when selection changes
##
## The Screen Coordinator pattern uses this interface to coordinate
## selection between sibling panels (e.g., when equipment is selected,
## clear inventory selection and vice versa).

# Emitted when selection changes
# selected_item: The newly selected item, or null if deselected
signal selection_changed(selected_item: Variant)

## Clear any current selection
## Override in subclass to clear visual highlighting and internal state
func clear_selection() -> void:
	# Subclasses should:
	# 1. Clear visual highlight on selected slot/item
	# 2. Set internal selection tracking to null
	# 3. Clear any info panel display
	pass

## Get the currently selected item
## Override in subclass to return the selected item data
func get_selected_item() -> Variant:
	# Subclasses should return the actual selected item data
	return null

## Check if anything is currently selected
func has_selection() -> bool:
	return get_selected_item() != null
