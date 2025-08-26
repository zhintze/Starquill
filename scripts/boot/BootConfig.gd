# File: res://scripts/boot/BootConfig.gd
@tool
extends Resource
class_name BootConfig

# Scene to launch after all boot tasks succeed (or succeed enough).
@export var target_scene_path: String = "res://scenes/CharacterRandomizer.tscn"

# If true, stop on the first failed task. If false, keep going and report.
@export var fail_fast: bool = false

# ===== Species data =====
# Try JSONs first (in order), then folders (in order).
@export var species_json_candidates: PackedStringArray = [
	"res://assets/data/species.json",
	"res://data/species.json"
]
@export var species_dir_candidates: PackedStringArray = [
	"res://data/species",
	"res://assets/species"
]

# ===== Optional: palette sanity (won’t block, just logs) =====
# If set, verify at least one of these exists; otherwise warn and rely on fallback palette.
@export var palette_csv_candidates: PackedStringArray = [
	"res://documents/color_main.csv"
]
