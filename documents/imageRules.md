# Contract (rules the code follows)

## Paths & naming

- Species images live at:
  
  - Static (non-modular): `assets/images/species/{ImageNum}-{Layer}.png`
  
  - Modular: `assets/images/species/{Type}-{ImageNum}-{Layer}.png`

- `{ImageNum}`: always 4 digits (`0001..NNNN`)

- `{Layer}`: always 3 digits

- `Type` is exactly the token in JSON (e.g., `h02`, `f01`, `d04`).

## Which convention to use

- If a value matches `^\d{4}-\d{3}$` → **static** (non-random).

- If a value matches `^[a-z]\d{2}$` (e.g., `h02`, `f04`) or is an **array** of those → **modular** (random image number).

## Layer map (authoritative for species pieces)

(You can extend this table anytime; the builder reads it.)
const SPECIES_LAYER_MAP := {
    "f01": [84],          # eyes
    "f02": [86],          # nose
    "f03": [85],          # mouth
    "f04": [100],         # ears
    "f05": [122],         # facial hair
    "f06": [87],          # face detail
    "h01": [92],          # hair
    "h02": [92, 128],     # hair + hairtop
    "h03": [92, 154],     # hair + detail
    "h04": [92, 128, 154] # hair + detail + hairtop
}

## Modular counts (from `assets/data/speciesModularParts.json`)

- The JSON provides **how many variants exist** for each modular type.

- **Preferred**: counts keyed by the full type (e.g., `"h02": 23`, `"f01": 12`, …).

- **Also supported** (fallback): a coarse map keyed by the letter (e.g., `"h": 23`)—we’ll use this if a full-type key is missing.

## Picking an image number for modular parts

1. If the species field is an array (e.g., `"hair": ["h01","h02","h03","h04"]`), **randomly choose one type from that list**.

2. Look up count for that chosen type (exact key; fallback to its prefix letter).

3. Randomly choose a number in `1..count`, zero-pad to 4 digits.

4. For each layer in `SPECIES_LAYER_MAP[type]`, build the path:
   
   - `assets/images/species/{type}-{image_num}-{layer}.png`

## Static pieces

- A value like `"0001-038"` becomes:  
  `assets/images/species/0001-038.png` (one image = one layer/z).

## Skin color & variance

- If `skinVariance_hex` has values: choose one **random** hex and apply it to **every piece whose layer number is in** `skinVariance_indices`. This **overrides** any normal skin palette color.

- Else, if `skin_color` contains a keyword (e.g., `"human"`), load that palette JSON and choose a random hex; apply to **skin layers** (set below).

- **Skin layers set** (tunable constant): `{16, 37, 38, 82, 102}` → backArm, legs, body, head, frontArm.

## Ordering & scale

- Z ordering = the numeric layer (ascending).

- `x_scale` / `y_scale` apply to the **whole CharacterDisplay node**.

- Missing textures: log a warning and **skip** the piece.


