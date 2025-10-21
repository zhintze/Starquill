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

## Godot MCP Integration

This project has Godot MCP (Model Context Protocol) enabled, allowing Claude Code to interact directly with the Godot Editor.

### MCP Server Status
- **Server**: Runs automatically when Godot editor is open
- **Port**: 9080 (WebSocket)
- **Connection**: Configured in Claude Code (`claude mcp list` to verify)

### Available MCP Commands

#### Node Commands (`addons/godot_mcp/commands/node_commands.gd`)
- **create_node**: Create a new node in the current scene
  - Params: `parent_path`, `node_type`, `node_name`
  - Example: Create a Camera2D node
- **delete_node**: Delete a node from the scene
  - Params: `node_path`
- **update_node_property**: Modify a node's property
  - Params: `node_path`, `property_name`, `property_value`
- **get_node_properties**: Get all properties of a node
  - Params: `node_path`
- **list_nodes**: List all nodes in the current scene
  - Returns: Scene tree structure

#### Scene Commands (`addons/godot_mcp/commands/scene_commands.gd`)
- **get_current_scene**: Get the currently open scene
  - Returns: Scene file path and structure
- **get_scene_structure**: Get detailed scene tree structure
  - Params: `scene_path` (optional, uses current if not provided)
- **create_scene**: Create a new scene file
  - Params: `scene_path`, `root_node_type`
- **open_scene**: Open an existing scene in the editor
  - Params: `scene_path`
- **save_scene**: Save the current scene
  - Params: `scene_path` (optional)

#### Script Commands (`addons/godot_mcp/commands/script_commands.gd`)
- **get_current_script**: Get the currently open script
  - Returns: Script path and content
- **get_script**: Read a specific script file
  - Params: `script_path`
- **get_script_metadata**: Get script information (classes, functions, etc.)
  - Params: `script_path`
- **create_script**: Create a new GDScript file
  - Params: `script_path`, `content`, `template` (optional)
- **create_script_template**: Create script from template
  - Params: `script_path`, `template_type`
- **edit_script**: Modify an existing script
  - Params: `script_path`, `content`

#### Editor Commands (`addons/godot_mcp/commands/editor_commands.gd`)
- **get_editor_state**: Get current editor state
  - Returns: Current scene, script, selected nodes, play status
- **get_selected_node**: Get currently selected node
  - Returns: Node info, properties, script path
- **create_resource**: Create a new Godot resource
  - Params: `resource_type`, `resource_path`, `properties`

#### Project Commands (`addons/godot_mcp/commands/project_commands.gd`)
- **get_project_info**: Get project metadata
  - Returns: Project name, version, settings
- **get_project_settings**: Get project configuration
  - Params: `setting_path` (optional)
- **get_project_structure**: Get project directory structure
  - Params: `root_path` (optional)
- **list_project_files**: List files in project
  - Params: `directory`, `pattern` (optional)
- **list_project_resources**: List all resources
  - Params: `resource_type` (optional filter)

#### Editor Script Commands (`addons/godot_mcp/commands/editor_script_commands.gd`)
- **execute_editor_script**: Run GDScript code in editor context
  - Params: `code`
  - Use for one-off editor automation tasks

### MCP Usage Examples

```
# Get current scene structure
@mcp godot-mcp get_current_scene

# Create a new Camera2D node
@mcp godot-mcp create_node --parent_path="/root/MainScene" --node_type="Camera2D" --node_name="MainCamera"

# Read a script file
@mcp godot-mcp get_script --script_path="res://scripts/world/camera_controller.gd"

# Get editor state
@mcp godot-mcp get_editor_state

# List all scenes in project
@mcp godot-mcp list_project_files --directory="res://scenes" --pattern="*.tscn"
```

### MCP Limitations
- No export/build commands (use `./deploy-android.sh` for Android builds)
- Editor must be running for MCP server to be active
- Changes are live in editor but may need manual save
- Best for: Scene manipulation, script reading/writing, project inspection

## Android Development

### Setup
- **Device**: Configured for Jelly Max (JELLYMAX00004650)
- **Debug Keystore**: `~/.local/share/godot/keystores/debug.keystore`
- **Export Preset**: Android (arm64-v8a)
- **Package**: com.starquill.game

### Deployment
```bash
# Build and deploy to connected Android device
./deploy-android.sh

# Manual export (in Godot editor)
Project > Export > Android > Export Project
```

### Android Export Settings
Located in `export_presets.cfg`:
- Platform: Android
- Architecture: arm64-v8a only (for performance)
- Min SDK: Auto (from templates)
- Permissions: Minimal (no special permissions required)

## Key Files for Understanding the System

- `project.godot`: Godot project configuration with autoload definitions
- `scripts/core/config_manager.gd`: Central configuration and boot system
- `scripts/core/starquill_data.gd`: Main data registry and API
- `autoload/game.gd`: Game flow entry points
- `scripts/equipment/equipment_factory.gd`: Equipment creation and randomization
- `scripts/world/camera_controller.gd`: Camera system with pinch-zoom support
- `README.md`: Project scope and design goals