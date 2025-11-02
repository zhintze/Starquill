# Architecture Improvement Review (Detailed)

This document expands on the five key design and structure improvements proposed for your Godot project before adding new scenes, UI modal menus, and inventory systems.

---

## 1. Centralize Game State Management (GameManager autoload)

### What / Why
- A single source of truth for session-wide facts: current scene/level, player profile, run flags (e.g., is_paused), and high-level mode (e.g., in_dialogue, in_inventory).
- Prevents state scattering across scenes and UI trees.
- GOF: **Singleton** (Godot autoload), optionally **State** if you split modes into pluggable states later.

### Smells Addressed
- Hidden coupling between scenes and UI.
- Difficult transitions (scene → menu → resume).
- Race conditions with pause/input handling spread across nodes.

### Minimal Implementation Sketch
```gdscript
# autoload: GameManager.gd
extends Node
class_name GameManager

signal scene_changed(new_scene: StringName)
signal paused_changed(is_paused: bool)
signal mode_changed(mode: StringName)

var _current_scene: StringName = &""
var _is_paused: bool = false
var _mode: StringName = &"gameplay"

func set_scene(scene_name: StringName) -> void:
    if _current_scene == scene_name:
        return
    _current_scene = scene_name
    emit_signal("scene_changed", scene_name)

func set_paused(value: bool) -> void:
    if _is_paused == value:
        return
    _is_paused = value
    get_tree().paused = value
    emit_signal("paused_changed", value)

func set_mode(value: StringName) -> void:
    if _mode == value:
        return
    _mode = value
    emit_signal("mode_changed", value)
```
---

## 2. Enforce Clear MVC Boundaries (or prep for ECS later)

### What / Why
- Separate **Model** (data/state), **View** (Nodes/UI), **Controller** (input + orchestration).
- Prevents UI nodes from mutating core data directly.
- Fits Godot naturally: Views are scenes; Models are Resources; Controllers are thin scripts mediating signals.
- GOF: **Observer**, **Mediator**, **Strategy**, **Factory**.

### Minimal Implementation Sketch
```gdscript
# model/inventory/Inventory.gd
extends Resource
class_name Inventory

signal changed()

var capacity: int = 24
var items: Array[ItemStack] = []

func add(stack: ItemStack) -> bool:
    if items.size() >= capacity:
        return false
    items.append(stack)
    emit_signal("changed")
    return true
```
---

## 3. Modularize UI Layers (UIManager + BaseMenu)

### What / Why
- A single point to open/close modal UI (inventory, pause, settings) with consistent focus and stacking.
- GOF: **Mediator**, **Template Method**.

### Minimal Implementation Sketch
```gdscript
# autoload: UIManager.gd
extends CanvasLayer
class_name UIManager

var _stack: Array[BaseMenu] = []

func open(menu_scene: PackedScene) -> BaseMenu:
    var menu := menu_scene.instantiate() as BaseMenu
    add_child(menu)
    _stack.push_back(menu)
    menu.open()
    GameManager.set_mode(&"menu")
    return menu

func close_top() -> void:
    if _stack.is_empty():
        return
    var top := _stack.pop_back()
    top.close()
    top.queue_free()
    if _stack.is_empty():
        GameManager.set_mode(&"gameplay")
```

---

## 4. Separate Data from Presentation (data-driven items, characters, dialogs)

### What / Why
- Store item, character, and dialog data in `.tres`, `.res`, or `.json` files.
- Scripts interpret and display data; designers can tweak values safely.
- GOF: **Factory**, **Strategy**, **Builder**.

### Minimal Implementation Sketch
```gdscript
# data/items/ItemData.gd
extends Resource
class_name ItemData
@export var id: StringName
@export var display_name: String
@export var description: String
@export var icon: Texture2D
@export var max_stack: int = 1
@export var effect_script: Script
```

---

## 5. Introduce a Scene Event Bus (decouple cross-system chatter)

### What / Why
- A tiny autoload exposing named signals for shared events.
- GOF: **Observer**.

### Minimal Implementation Sketch
```gdscript
# autoload: EventBus.gd
extends Node
class_name EventBus

signal inventory_opened()
signal inventory_closed()
signal item_picked(stack: ItemStack)
signal warning(kind: StringName)

static func emit_inventory_opened() -> void:
    EventBus.inventory_opened.emit()

static func emit_warning(kind: StringName) -> void:
    EventBus.warning.emit(kind)
```
---

## Minimal Folder Layout
```
autoload/
  GameManager.gd
  UIManager.gd
  EventBus.gd
model/
  inventory/Inventory.gd
  inventory/ItemStack.gd
data/
  items/
view/
  ui/BaseMenu.gd
  ui/InventoryPanel.tscn
controller/
  InventoryController.gd
```

---

## Risks & Mitigations
| Risk | Mitigation |
|------|-------------|
| Over-centralization in GameManager | Keep only scene, pause, mode state |
| Event explosion | Limit and version signals |
| Variant typing warnings | Annotate all types (`StringName`, `Array[T]`, etc.) |

---

## Summary
These patterns will give you:
- Predictable input and pause semantics across menus.
- UI that’s swappable without rewriting game logic.
- Inventory and future systems that are data-driven and testable.
