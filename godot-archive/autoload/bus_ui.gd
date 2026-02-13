## BusUI.gd — HUD/panels/toasts/tutorials
extends Node

signal show_panel(panel_id: String, args: Dictionary)
signal hide_panel(panel_id: String)
signal toast(message: String, kind: String)
signal tutorial_hint(hint_id: String)

# Helpers
func open_panel(panel_id: String, args: Dictionary = {}) -> void:
	show_panel.emit(panel_id, args)

func close_panel(panel_id: String) -> void:
	hide_panel.emit(panel_id)

func show_toast(message: String, kind: String="info") -> void:
	toast.emit(message, kind)

func show_hint(hint_id: String) -> void:
	tutorial_hint.emit(hint_id)
