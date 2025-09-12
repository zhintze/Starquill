# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Starquill is an open-world fantasy RPG built in Godot 4.4, featuring a "paper doll" visual style with modular character equipment systems. The game focuses on exploration, procedural content generation, and dice-based interactions using D&D-inspired character stats.

## Development Commands

### Building and Exporting
```bash
# Run the game in Godot editor (headless)
godot --headless --verbose -- main.tscn

# Build game executable (uses export presets)
# Built executables are stored in ./build/ directory
# - Starquill.exe (Windows)
# - Starquill.console.exe (Windows console)
# - Starquill.pck (packed game data)
```

### Testing Commands
This project uses Godot's built-in testing system. Run tests through the Godot editor or use:
```bash
# Run tests with timeout
timeout 30 godot --headless --verbose -- main.tscn
```

## Architecture Overview

### Autoload Singletons (Global Systems)
The game uses a bus-based architecture with these autoloaded singletons:

- **ConfigManager**: Central configuration and data loading system
- **StarquillData**: Unified data registry for species and equipment
- **Game**: Main game flow controller (start_new_game, go_to_next_level)
- **SceneLoader**: Scene transition management
- **Bus System**: Event communication between systems
  - `BusFlow`: Level transitions and boot events
  - `BusActors`: Character/actor events
  - `BusCombat`: Combat-related events
  - `BusInventory`: Inventory system events
  - `BusUi`: UI interaction events
  - `BusSave`: Save/load game events
  - `BusAudio`: Audio and music management

### Core Systems

#### Character System (`scripts/character/`)
- **Character**: Main character class with equipment slots and stats
- **Stats**: D&D-inspired stats (STR, DEX, CON, INT, WIS, CHA)
- **Species**: Character species with modular visual parts
- **SpeciesInstance**: Runtime instances of species with randomized attributes

#### Equipment System (`scripts/equipment/`)
- **EquipmentFactory**: Creates equipment instances with randomization
- **EquipmentInstance**: Runtime equipment with colors and stats
- **EquipmentCatalog**: Static equipment definitions loaded from JSON

#### Visual Layer System (`scripts/display/`)
Handles the "paper doll" character rendering with:
- **DisplayBuilder**: Constructs character visuals from equipment layers
- **ColorManager**: Manages color palettes from JSON data
- Layer-based equipment rendering with conflict detection

### Data Management

#### Configuration System
- **ConfigManager** (`scripts/core/config_manager.gd`): Centralized config management
  - Loads species data from `assets/data/species.json`
  - Loads equipment catalog from `assets/data/equipment.json`
  - Manages color palettes from `assets/data/color_palettes.json`
  - Handles project overrides via `config/starquill_config.json`

#### Data Loading Patterns
- JSON-based configuration with fallback paths
- Species data loaded into StarquillData registry on boot
- Equipment randomization with slot-specific restrictions
- Modular image numbering system for character parts

### Project Structure

```
scripts/
├── character/     # Character classes and stats
├── combat/        # Combat mechanics (basic)
├── core/          # Core systems (config, data management)
├── display/       # Visual rendering and layer management
├── equipment/     # Equipment system and factory
├── inventory/     # Inventory management
├── services/      # Utility services
├── species/       # Species definitions and instances
├── tools/         # Development tools
├── ui/            # User interface components
├── util/          # Utility functions
└── verbs/         # Action/verb system

autoload/          # Global singleton scripts
assets/data/       # JSON data files (species, equipment, colors)
scenes/            # Godot scene files
build/             # Compiled game executables
```

## Development Guidelines

### Equipment System Usage
- Use `EquipmentFactory.create_from_catalog()` for specific equipment
- Use `EquipmentFactory.create_random_from_prefix()` for slot-based randomization
- Equipment restrictions: torso (tr01-tr06), legs (lg01-lg02) for main slots
- Misc slots accept any equipment type for variety

### Character Creation
- Use `StarquillData.create_species_instance()` or `create_random_species_instance()`
- Apply equipment via `Character.equip_instance()`
- Character updates trigger via signals: `equipment_changed`, `model_changed`

### Data Loading
- Species and equipment data auto-loads during ConfigManager initialization
- Use StarquillData APIs for accessing loaded data
- Color palettes managed through ColorManager singleton

### Bus System Communication
- Use appropriate bus singletons for cross-system communication
- BusFlow for level/scene transitions
- BusActors for character events
- Emit signals through bus helpers rather than direct signal emission

## Key Files for Understanding the System

- `project.godot`: Godot project configuration with autoload definitions
- `scripts/core/config_manager.gd`: Central configuration and boot system
- `scripts/core/starquill_data.gd`: Main data registry and API
- `autoload/game.gd`: Game flow entry points
- `scripts/equipment/equipment_factory.gd`: Equipment creation and randomization
- `README.md`: Project scope and design goals