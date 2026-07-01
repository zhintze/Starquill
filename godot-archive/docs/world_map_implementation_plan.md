# World Map & Exploration System Implementation Plan

## System Overview
- **Map Size**: 10,000 x 10,000 tiles (100M tiles total)
- **Tile Resolution**: 202x202px source, displayed at 64x64px for mobile optimization
- **Chunk System**: 32x32 tile chunks (9,765 total chunks)
- **Visibility**: 10-tile radius fog of war
- **Performance**: Mobile-first with chunk streaming

## Phase 1: Core World System Architecture (3-4 days)
### 1.1 Data Structures
- Create `WorldMap` class extending Node2D
- Define `Tile` resource with properties:
  - type (enum: GROUND, TREE, MOUNTAIN, WATER, LOCATION)
  - is_passable: bool
  - biome_id: String
  - location_data: LocationData (nullable)
  - visibility_state: enum (HIDDEN, REVEALED, VISIBLE)
- Create `Chunk` class for 32x32 tile sections
- Define `WorldConstants` singleton with:
  - TILE_SIZE = 64
  - CHUNK_SIZE = 32
  - WORLD_SIZE = 10000
  - MOVEMENT_SPEED = 2.0
  - VISIBILITY_RADIUS = 10
  - LOCATION_DENSITY = 1.0/625.0

### 1.2 Bus System Extensions
- Create `BusWorld` for world events:
  - chunk_loaded/unloaded
  - tile_revealed/hidden
  - location_discovered
  - biome_entered
- Create `BusParty` for party management:
  - party_moved
  - formation_changed
  - member_added/removed

## Phase 2: Tile and Chunk Management (4-5 days)
### 2.1 Chunk Streaming System
- Implement `ChunkManager`:
  - Load chunks within 2-chunk radius of party
  - Unload chunks beyond 3-chunk radius
  - Cache recently used chunks (LRU cache, 50 chunks)
  - Async chunk loading with thread pool
- Create `TileRenderer` using TileMapLayer:
  - Batch render visible tiles
  - Update only changed tiles
  - Handle z-ordering for overlapping elements

### 2.2 Coordinate System
- Implement `WorldCoordinate` class:
  - Convert between world/chunk/screen coordinates
  - Handle edge cases at world boundaries
  - Provide distance calculations

## Phase 3: Biome and Procedural Generation (5-6 days)
### 3.1 Biome System
- Create `BiomeManager` with biomes:
  - Plains (40% trees, 5% mountains)
  - Forest (70% trees, 10% mountains)
  - Mountains (20% trees, 60% mountains)
  - Desert (5% trees, 15% mountains)
  - Swamp (30% trees, water tiles)
- Implement Voronoi-based biome distribution:
  - Seed points every 200-500 tiles
  - Smooth transitions between biomes
  - Biome-specific tile generation rules

### 3.2 World Generation
- Create `WorldGenerator`:
  - Perlin noise for terrain elevation
  - Biome-aware tile placement
  - River/lake generation using flow algorithms
  - Path connectivity validation
- Implement seed-based generation for consistency

## Phase 4: Location System (4-5 days)
### 4.1 Location Architecture
- Create base `Location` class:
  - name: String
  - type: LocationType
  - npc_ids: Array[String]
  - shop_data: ShopData (nullable)
  - is_discovered: bool
- Implement location types:
  - `Village`: shops, inns, NPCs
  - `Cave`: dungeon entrance, treasures
  - `Dungeon`: multi-level structure
  - `Shrine`: special encounters

### 4.2 Location Spawning
- Implement `LocationSpawner`:
  - Bell curve distribution (median 50 tiles from player)
  - Accessibility validation (not surrounded by impassable)
  - Minimum distance between locations (15 tiles)
  - Triggered generation on NPC dialogue/events
- Create `LocationFactory` for procedural generation:
  - Random or template-based creation
  - Themed generation based on biome

## Phase 5: Party and Movement System (5-6 days)
### 5.1 Party Management
- Create `PartyManager`:
  - Leader + 3 follower positions
  - Formation types (line, square, diamond)
  - Character spacing and following logic
  - Z-index management for overlapping
- Implement `PartyMember` component:
  - Position in formation
  - Movement interpolation
  - State machine (idle, moving, following)

### 5.2 Movement System
- Implement `MovementController`:
  - Touch input for tap-to-move
  - Swipe gestures for directional movement
  - Movement queue for smooth transitions
- Create `PathfindingManager`:
  - A* implementation with heap optimization
  - Path caching for recent destinations
  - Dynamic obstacle avoidance
  - Max path length (100 tiles) for performance

## Phase 6: Camera and Fog of War (3-4 days)
### 6.1 Camera System
- Implement `WorldCamera`:
  - Follow party with 5-tile edge buffer
  - Smooth scrolling with adjustable speed
  - Pinch-to-zoom (0.5x to 2x scale)
  - Pan gesture for free exploration
  - Boundary clamping at world edges

### 6.2 Fog of War
- Create `VisibilityManager`:
  - Circular visibility (10-tile radius)
  - Three states: hidden (black), revealed (gray), visible (full)
  - Efficient visibility calculation using bresenham
  - Persistent revealed areas
- Implement fog rendering with shader for smooth edges

## Phase 7: Persistence and Save System (4-5 days)
### 7.1 World Persistence
- Create `WorldSaveData` resource:
  - Compressed tile data (RLE encoding)
  - Location states and discoveries
  - Party positions and formation
  - Revealed map areas
  - Random seed for regeneration
- Implement delta saves for modified chunks only

### 7.2 Save/Load System
- Extend existing save system:
  - Async save with progress indicator
  - Auto-save on location entry/exit
  - Multiple save slots (3 minimum)
  - Save versioning for compatibility
- Implement save compression (zstd)

## Phase 8: UI Integration (3-4 days)
### 8.1 HUD Elements
- Create `WorldHUD`:
  - Inventory button (top-left, placeholder)
  - Minimap (top-right, 50x50 tiles view)
  - Coordinate display (debug mode)
  - Party status indicators
- Implement touch controls overlay

### 8.2 World Map Menu
- Create pause menu with:
  - World map overview
  - Discovered locations list
  - Biome statistics
  - Save/Load options

## Phase 9: Polish and Optimization (3-4 days)
### 9.1 Performance Optimization
- Implement object pooling for tiles
- Texture atlasing for tile images
- LOD system for distant chunks
- Profiling and bottleneck elimination

### 9.2 Polish
- Particle effects for movement
- Transition animations between biomes
- Sound effects for different terrain types
- Tutorial for movement controls

## Technical Considerations
- **Memory Budget**: ~200MB for active world data
- **Target FPS**: 60fps on mid-range mobile devices
- **Load Time**: <2 seconds for chunk streaming
- **Save Size**: ~10-20MB per world (compressed)

## Risk Mitigation
- Start with 1000x1000 world for testing
- Implement debug tools early (teleport, reveal map)
- Create performance benchmarks for each phase
- Maintain backwards compatibility for saves

## Total Timeline: 35-42 days