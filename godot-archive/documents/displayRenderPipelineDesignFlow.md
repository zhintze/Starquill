# Display Render Pipeline Design flow

1. **Load data**
   
   - `SpeciesLoader.load_from_json(res://assets/data/species.json)` builds `SpeciesLoader.all/by_name`.
   
   - On-demand palette: `SpeciesLoader.get_palette("human")` reads `res://documents/skin_color_human.csv` (or fallback), returning `["#RRGGBB", ...]`.
   
   - Modular counts: `SpeciesLoader.pick_modular_image_num("f01")` uses `assets/data/speciesModularParts.json` to RNG a zero-padded `"####"`.

2. **Freeze a species**
   
   - `SpeciesLoader.create_instance("dwarf")` (or `create_random_instance_from_index`) creates a `SpeciesInstance` by:
     
     - Choosing one option from any arrays (e.g., `hair`).
     
     - Resolving **skinColor** from palette or explicit hex list.
     
     - Applying **skinVarianceMap** overrides by layer if provided.
     
     - Pre-selecting image numbers for all modular codes (`modular_image_nums`).

3. **Assemble display pieces**
   
   - `SpeciesDisplayable` wraps the `SpeciesInstance`.
   
   - `SpeciesDisplayBuilder.build_display_pieces(instance)` materializes `DisplayPiece[]` using the two file-name conventions:
     
     - **Static** parts: `{ImageNum}-{Layer}.png`
     
     - **Modular** parts: `{Type}-{ImageNum}-{Layer}.png`
   
   - Layer numbers are the **z-order**, and per-layer tints apply (skin/variance).

4. **Render**
   
   - `CharacterDisplay.set_target(displayable)` creates child `Sprite2D`s (one per `DisplayPiece`) under the `CharacterDisplay` node, sorted by `layer`.
   
   - Missing textures are logged and skipped (non-fatal).

5. **Scale & restrictions**
   
   - `SpeciesInstance.x_scale / y_scale` can be applied at the root node to scale the whole character (future: equipment obeys same transform).
   
   - `itemRestrictions` are stored in the instance (future: used by equipment resolver).

6. **Result**
   
   - A complete on-screen character built from species JSON, palettes (CSV), and modular/static image parts — **randomized** yet reproducible within the single instance.
