@tool
extends Node
@onready var conv := preload("res://scripts/tools/csv_to_json_converter.gd").new()



func _ready():
	# Adjust paths as needed. The CSV must be inside the project for res:// access.
	var equipment_csv_in  := "res://documents/Layer Export - equipment.csv"   # e.g., where you keep CSVs
	var equipment_json_out := "res://assets/data/equipment.json"
	conv.convert_equipment_csv_to_json(equipment_csv_in, equipment_json_out)
	
	var species_csv_in  := "res://documents/Layer Export - species.csv"   # e.g., where you keep CSVs
	var species_json_out := "res://assets/data/species.json"
	conv.convert_species_csv_to_json(species_csv_in, species_json_out)
	
	var speciesModularParts_csv_in  := "res://documents/Layer Export - speciesModularParts.csv"   # e.g., where you keep CSVs
	var speciesModularParts_json_out := "res://assets/data/speciesModularParts.json"
	conv.convert_species_modular_parts_csv_to_json(speciesModularParts_csv_in, speciesModularParts_json_out)
	print("Done.")
